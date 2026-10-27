import { existsSync, readFileSync } from 'node:fs';
import { createServer, IncomingMessage, ServerResponse } from 'node:http';
import { join, resolve } from 'node:path';
import { InMemoryWorldCoreAdapter } from '../adapters/in-memory-world-core-adapter.js';
import { MetaModelApiAdapter } from '../adapters/meta-model-api-adapter.js';
import { MockMetaModelAdapter } from '../adapters/mock-meta-model-adapter.js';
import {
  RemoteWorldCoreAdapter,
  WorldCoreConnectionError,
  WorldCoreTimeoutError,
  WorldCoreHttpError,
  WorldCoreResponseValidationError,
} from '../adapters/remote-world-core-adapter.js';
import {
  AnswerBusinessHoursQueryUseCase,
  BusinessNotFoundError,
  InvalidEvaluationTimeError,
  InvalidQueryError,
} from '../application/answer-business-hours-query.js';
import { AppConfig, loadConfig, maskApiKey } from '../config/index.js';
import { InvalidBusinessIdError, parseBusinessId } from '../domain/business-id.js';
import { MetaModelPort } from '../ports/meta-model-port.js';
import { WorldCorePort } from '../ports/world-core-port.js';

export interface AssistantServerOptions {
  readonly config?: AppConfig;
  readonly worldCore?: WorldCorePort;
  readonly metaModel?: MetaModelPort;
  readonly publicDir?: string;
}

export function createAssistantServer(options: AssistantServerOptions = {}) {
  const config = options.config ?? loadConfig();
  const publicDir = options.publicDir ?? resolve(process.cwd(), 'public');

  let worldCore: WorldCorePort;
  if (options.worldCore) {
    worldCore = options.worldCore;
  } else if (config.worldCore.isRemote && config.worldCore.baseURL) {
    worldCore = new RemoteWorldCoreAdapter({ baseURL: config.worldCore.baseURL });
  } else {
    worldCore = new InMemoryWorldCoreAdapter();
  }

  let metaModel: MetaModelPort;
  if (options.metaModel) {
    metaModel = options.metaModel;
  } else if (config.meta.isConfigured && config.meta.apiKey) {
    metaModel = new MetaModelApiAdapter({ config: config.meta });
  } else {
    metaModel = new MockMetaModelAdapter({ modelName: `${config.meta.model}-local-mock` });
  }

  const useCase = new AnswerBusinessHoursQueryUseCase({
    worldCore,
    metaModel,
  });

  const sendJson = (res: ServerResponse, statusCode: number, payload: unknown) => {
    res.statusCode = statusCode;
    res.setHeader('Content-Type', 'application/json; charset=utf-8');
    res.end(JSON.stringify(payload, null, 2));
  };

  const readBody = async (req: IncomingMessage): Promise<any> => {
    return new Promise((resolveBody, rejectBody) => {
      let body = '';
      req.on('data', (chunk) => {
        body += chunk;
        if (body.length > 64 * 1024) {
          rejectBody(new Error('Request body exceeded 64KB limit'));
        }
      });
      req.on('end', () => {
        if (!body.trim()) {
          resolveBody({});
          return;
        }
        try {
          resolveBody(JSON.parse(body));
        } catch {
          rejectBody(new Error('Invalid JSON in request body'));
        }
      });
      req.on('error', rejectBody);
    });
  };

  const server = createServer(async (req, res) => {
    const method = req.method ?? 'GET';
    const parsedUrl = new URL(req.url ?? '/', `http://${req.headers.host || 'localhost'}`);
    const pathname = parsedUrl.pathname;

    try {
      // 1. Static file serving for browser client
      if (method === 'GET') {
        let filePath: string | null = null;
        let contentType = 'text/plain';

        if (pathname === '/' || pathname === '/index.html') {
          filePath = join(publicDir, 'index.html');
          contentType = 'text/html; charset=utf-8';
        } else if (pathname === '/style.css') {
          filePath = join(publicDir, 'style.css');
          contentType = 'text/css; charset=utf-8';
        } else if (pathname === '/app.js') {
          filePath = join(publicDir, 'app.js');
          contentType = 'application/javascript; charset=utf-8';
        }

        if (filePath && existsSync(filePath)) {
          const content = readFileSync(filePath);
          res.statusCode = 200;
          res.setHeader('Content-Type', contentType);
          res.setHeader('Content-Length', content.length);
          res.end(content);
          return;
        }
      }

      // 2. Health check
      if (method === 'GET' && pathname === '/health') {
        sendJson(res, 200, {
          status: 'ok',
          service: 'MyVirtualCommunity AI Hackathon Assistant',
          metaModelConfigured: config.meta.isConfigured,
          model: config.meta.model,
          timestamp: new Date().toISOString(),
        });
        return;
      }

      // 3. List registered businesses
      if (method === 'GET' && pathname === '/api/businesses') {
        const ids =
          'listBusinessIds' in worldCore &&
          typeof (worldCore as any).listBusinessIds === 'function'
            ? (worldCore as any).listBusinessIds()
            : [];
        const businesses = [];
        for (const id of ids) {
          const schedule = await worldCore.getBusinessSchedule(id);
          if (schedule) {
            businesses.push({
              id: schedule.businessId,
              name: schedule.name,
              timeZone: schedule.timeZone,
              hours: schedule.hours,
            });
          }
        }
        sendJson(res, 200, { businesses });
        return;
      }

      // 4. GET /api/businesses/:id/status
      if (method === 'GET' && pathname.startsWith('/api/businesses/') && pathname.endsWith('/status')) {
        const parts = pathname.split('/');
        const rawId = decodeURIComponent(parts[3] ?? '');
        const parseResult = parseBusinessId(rawId);
        if (!parseResult.success) {
          sendJson(res, 400, {
            success: false,
            error: { code: 'invalid-business-id', message: parseResult.error },
          });
          return;
        }

        const atParam = parsedUrl.searchParams.get('at');
        const status = await worldCore.getBusinessOperationalStatus(
          parseResult.data,
          atParam ?? undefined
        );

        if (!status) {
          sendJson(res, 404, {
            success: false,
            error: { code: 'business-not-found', message: `Business "${rawId}" not found` },
          });
          return;
        }

        sendJson(res, 200, { success: true, status });
        return;
      }

      // 5. POST /api/assistant/query
      if (method === 'POST' && pathname === '/api/assistant/query') {
        const body = await readBody(req);
        if (!body || typeof body !== 'object' || Array.isArray(body)) {
          sendJson(res, 400, {
            success: false,
            error: { code: 'invalid-request', message: 'Request body must be a JSON object' },
          });
          return;
        }

        const { businessId, question, userQuery, language, evaluationTime, evaluatedAt } = body;
        const queryText = question ?? userQuery;

        if (!businessId || typeof businessId !== 'string' || businessId.trim() === '') {
          sendJson(res, 400, {
            success: false,
            error: { code: 'missing-business-id', message: 'businessId is required and must be a non-empty string' },
          });
          return;
        }

        if (!queryText || typeof queryText !== 'string' || queryText.trim() === '') {
          sendJson(res, 400, {
            success: false,
            error: { code: 'missing-question', message: 'question is required and must be a non-empty string' },
          });
          return;
        }

        if (queryText.trim().length > 500) {
          sendJson(res, 400, {
            success: false,
            error: { code: 'question-too-long', message: 'question must not exceed 500 characters' },
          });
          return;
        }

        if (language !== undefined && language !== null && !['en', 'es'].includes(String(language).toLowerCase())) {
          sendJson(res, 400, {
            success: false,
            error: { code: 'invalid-language', message: 'language must be "en" or "es"' },
          });
          return;
        }

        const result = await useCase.execute({
          businessId,
          question: queryText,
          language,
          evaluationTime: evaluationTime ?? evaluatedAt,
        });

        sendJson(res, 200, result);
        return;
      }

      // 404 Route Not Found
      sendJson(res, 404, {
        success: false,
        error: { code: 'not-found', message: 'Route not found' },
      });
    } catch (err) {
      if (err instanceof WorldCoreConnectionError || err instanceof WorldCoreTimeoutError) {
        sendJson(res, 502, {
          success: false,
          error: {
            code: 'world-core-unavailable',
            message: 'The authoritative World Core server is currently unreachable. Please ensure the local World Core simulation is running.',
          },
        });
        return;
      }

      if (err instanceof WorldCoreHttpError) {
        sendJson(res, err.statusCode >= 500 ? 502 : err.statusCode, {
          success: false,
          error: {
            code: err.errorCode || 'world-core-error',
            message: err.message,
          },
        });
        return;
      }

      if (err instanceof WorldCoreResponseValidationError) {
        sendJson(res, 502, {
          success: false,
          error: {
            code: 'world-core-invalid-response',
            message: err.message,
          },
        });
        return;
      }

      if (err instanceof InvalidEvaluationTimeError) {
        sendJson(res, 400, {
          success: false,
          error: { code: 'invalid-evaluation-time', message: err.message },
        });
        return;
      }

      if (err instanceof InvalidBusinessIdError || err instanceof InvalidQueryError) {
        sendJson(res, 400, {
          success: false,
          error: { code: 'invalid-request', message: err.message },
        });
        return;
      }

      if (err instanceof BusinessNotFoundError) {
        sendJson(res, 404, {
          success: false,
          error: { code: 'business-not-found', message: err.message },
        });
        return;
      }

      const errorMessage = err instanceof Error ? err.message : 'Internal Server Error';
      sendJson(res, 500, {
        success: false,
        error: { code: 'internal-error', message: errorMessage },
      });
    }
  });

  return {
    server,
    config,
    worldCore,
    useCase,
    start: (port: number = config.server.port, host: string = config.server.host) => {
      return new Promise<void>((resolve) => {
        server.listen(port, host, () => {
          console.log(`[Assistant Server] Running at http://${host}:${port}`);
          console.log(
            `[Assistant Server] Meta Model: ${config.meta.model} (API Key: ${maskApiKey(config.meta.apiKey)})`
          );
          if (config.worldCore.isRemote) {
            console.log(`[Assistant Server] World Core: Remote (${config.worldCore.baseURL})`);
          } else {
            console.log('[Assistant Server] World Core: In-Memory Development Fixture');
          }
          if (!config.meta.isConfigured) {
            console.log(
              '[Assistant Server] Running in offline mode with MockMetaModelAdapter (zero external dependencies).'
            );
          }
          resolve();
        });
      });
    },
    stop: () => {
      return new Promise<void>((resolve, reject) => {
        server.close((err) => (err ? reject(err) : resolve()));
      });
    },
  };
}

// Direct execution entrypoint
if (process.argv[1]?.endsWith('index.ts') || process.argv[1]?.endsWith('index.js')) {
  const assistantServer = createAssistantServer();
  assistantServer.start();
}
