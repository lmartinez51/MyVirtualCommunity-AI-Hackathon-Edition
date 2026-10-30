import { existsSync, readFileSync, statSync } from 'node:fs';
import { createServer, IncomingMessage, ServerResponse } from 'node:http';
import { extname, resolve } from 'node:path';
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
import { PASO_DEL_NORTE_CORRIDOR } from '../domain/spatial-corridor.js';
import { CbpBorderWaitTimeAdapter } from '../adapters/cbp-border-wait-time-adapter.js';
import { GetBorderWaitTimesUseCase } from '../application/get-border-wait-times.js';
import {
  InvalidWitnessMessageError,
  TalkToSyntheticWitnessUseCase,
  UnknownWitnessError,
} from '../application/talk-to-synthetic-witness.js';
import {
  AskBorderConciergeUseCase,
  InvalidConciergeMessageError,
} from '../application/ask-border-concierge.js';
import {
  BusinessSnapshotUnavailableError,
  InvalidAttendantMessageError,
  TalkToBusinessAttendantUseCase,
  UnknownBusinessAttendantError,
} from '../application/talk-to-business-attendant.js';
import { BorderCrossingPort } from '../ports/border-crossing-port.js';
import { MetaModelPort } from '../ports/meta-model-port.js';
import { WorldCorePort } from '../ports/world-core-port.js';

export interface AssistantServerOptions {
  readonly config?: AppConfig;
  readonly worldCore?: WorldCorePort;
  readonly metaModel?: MetaModelPort;
  readonly borderCrossingPort?: BorderCrossingPort;
  readonly witnessUseCase?: TalkToSyntheticWitnessUseCase;
  readonly conciergeUseCase?: AskBorderConciergeUseCase;
  readonly businessAttendantUseCase?: TalkToBusinessAttendantUseCase;
  readonly publicDir?: string;
}

export function createAssistantServer(options: AssistantServerOptions = {}) {
  const config = options.config ?? loadConfig();
  const publicDir = options.publicDir ?? resolve(process.cwd(), 'public');

  let worldCore: WorldCorePort;
  let isExplicitRemoteWorldCore = false;
  if (options.worldCore) {
    worldCore = options.worldCore;
    isExplicitRemoteWorldCore =
      (worldCore as any).mode === 'remote' || worldCore instanceof RemoteWorldCoreAdapter;
  } else if (config.worldCore.isRemote && config.worldCore.baseURL) {
    worldCore = new RemoteWorldCoreAdapter({ baseURL: config.worldCore.baseURL });
    isExplicitRemoteWorldCore = true;
  } else {
    worldCore = new InMemoryWorldCoreAdapter();
    isExplicitRemoteWorldCore = false;
  }

  let metaModel: MetaModelPort;
  let isExplicitLiveMeta = false;
  if (options.metaModel) {
    metaModel = options.metaModel;
    isExplicitLiveMeta =
      (metaModel as any).mode === 'live-api' || metaModel instanceof MetaModelApiAdapter;
  } else if (config.meta.isConfigured && config.meta.apiKey) {
    metaModel = new MetaModelApiAdapter({ config: config.meta });
    isExplicitLiveMeta = true;
  } else {
    metaModel = new MockMetaModelAdapter({ modelName: `${config.meta.model}-local-mock` });
    isExplicitLiveMeta = false;
  }

  const useCase = new AnswerBusinessHoursQueryUseCase({
    worldCore,
    metaModel,
  });

  const borderCrossingPort: BorderCrossingPort =
    options.borderCrossingPort ?? new CbpBorderWaitTimeAdapter();

  const borderUseCase = new GetBorderWaitTimesUseCase({
    borderCrossingPort,
  });

  const witnessUseCase =
    options.witnessUseCase ??
    new TalkToSyntheticWitnessUseCase({
      metaModel,
    });

  const conciergeUseCase =
    options.conciergeUseCase ??
    new AskBorderConciergeUseCase({
      getBorderWaitTimesUseCase: borderUseCase,
      metaModel,
    });

  const businessAttendantUseCase =
    options.businessAttendantUseCase ??
    new TalkToBusinessAttendantUseCase({
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
      if (method === 'GET' && !pathname.startsWith('/api/') && pathname !== '/health') {
        const safeRelativePath = pathname === '/' ? 'index.html' : pathname.replace(/^\/+/, '');
        const targetPath = resolve(publicDir, safeRelativePath);

        // Security check: ensure target path is strictly within publicDir and is an existing file
        if (targetPath.startsWith(resolve(publicDir)) && existsSync(targetPath)) {
          try {
            const stat = statSync(targetPath);
            if (stat.isFile()) {
              const ext = extname(targetPath).toLowerCase();
              const mimeMap: Record<string, string> = {
                '.html': 'text/html; charset=utf-8',
                '.css': 'text/css; charset=utf-8',
                '.js': 'application/javascript; charset=utf-8',
                '.json': 'application/json; charset=utf-8',
                '.png': 'image/png',
                '.jpg': 'image/jpeg',
                '.jpeg': 'image/jpeg',
                '.svg': 'image/svg+xml',
                '.ico': 'image/x-icon',
                '.woff': 'font/woff',
                '.woff2': 'font/woff2',
                '.ttf': 'font/ttf',
              };
              const contentType = mimeMap[ext] ?? 'application/octet-stream';
              const content = readFileSync(targetPath);
              res.statusCode = 200;
              res.setHeader('Content-Type', contentType);
              res.setHeader('Content-Length', content.length);
              res.end(content);
              return;
            }
          } catch {
            // Fall through to API or 404
          }
        }
      }

      // 2. Health check
      if (method === 'GET' && pathname === '/health') {
        const worldCoreMode: 'remote' | 'in-memory' = isExplicitRemoteWorldCore ? 'remote' : 'in-memory';
        const metaProvider: 'live-api' | 'local-mock' = isExplicitLiveMeta ? 'live-api' : 'local-mock';

        let reachability: 'connected' | 'disconnected' | 'not-applicable' = 'not-applicable';
        let latencyMs: number | undefined = undefined;

        if (worldCoreMode === 'remote') {
          if (typeof (worldCore as any).probeHealth === 'function') {
            const probeResult = await (worldCore as any).probeHealth(500);
            reachability = probeResult.connected ? 'connected' : 'disconnected';
            latencyMs = probeResult.latencyMs;
          } else {
            // Fallback probe for custom injected WorldCorePort with strict 500ms timeout
            const controller = new AbortController();
            const timeoutHandle = setTimeout(() => controller.abort(), 500);
            const start = performance.now();
            try {
              await Promise.race([
                worldCore.getCurrentWorldTime(),
                new Promise((_, reject) => {
                  controller.signal.addEventListener('abort', () => reject(new Error('timeout')));
                }),
              ]);
              reachability = 'connected';
              latencyMs = Math.round(performance.now() - start);
            } catch {
              reachability = 'disconnected';
              latencyMs = Math.round(performance.now() - start);
            } finally {
              clearTimeout(timeoutHandle);
            }
          }
        } else {
          reachability = 'not-applicable';
        }

        sendJson(res, 200, {
          status: 'ok',
          service: 'MyVirtualCommunity AI Hackathon Assistant',
          metaModelConfigured: config.meta.isConfigured,
          model: config.meta.model,
          metaProvider,
          worldCore: {
            mode: worldCoreMode,
            reachability,
            connected: reachability === 'connected',
            ...(latencyMs !== undefined ? { latencyMs } : {}),
          },
          timestamp: new Date().toISOString(),
        });
        return;
      }

      // 3. GET /api/spatial/corridor (Authoritative Spatial Slice definition)
      if (method === 'GET' && pathname === '/api/spatial/corridor') {
        sendJson(res, 200, {
          success: true,
          corridor: PASO_DEL_NORTE_CORRIDOR,
        });
        return;
      }

      // 4. List registered businesses
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

        const schedule = await worldCore.getBusinessSchedule(parseResult.data);
        sendJson(res, 200, { success: true, status, schedule: schedule ?? undefined });
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

      // 6. GET /api/border/wait-times (Official Pedestrian Wait Times: PDN vs BOTA)
      if (method === 'GET' && pathname === '/api/border/wait-times') {
        const refreshParam = parsedUrl.searchParams.get('refresh') ?? parsedUrl.searchParams.get('bypassCache');
        const bypassCache = refreshParam === 'true' || refreshParam === '1';
        try {
          const comparison = await borderUseCase.execute({ bypassCache });
          sendJson(res, 200, {
            success: true,
            comparison,
          });
        } catch {
          sendJson(res, 502, {
            success: false,
            error: {
              code: 'border-source-error',
              message: 'Failed to retrieve border crossing wait times from official sources.',
            },
          });
        }
        return;
      }

      // 7. GET /api/border/witnesses (Synthetic Community Witnesses Roster)
      if (method === 'GET' && pathname === '/api/border/witnesses') {
        const roster = witnessUseCase.getRoster();
        sendJson(res, 200, {
          success: true,
          ...roster,
        });
        return;
      }

      // 8. POST /api/border/witnesses/chat (Conversations with Synthetic Witnesses)
      if (method === 'POST' && pathname === '/api/border/witnesses/chat') {
        let rawBody: any;
        try {
          rawBody = await readBody(req);
        } catch {
          sendJson(res, 400, {
            success: false,
            error: { code: 'invalid-body', message: 'Request body must be valid JSON' },
          });
          return;
        }

        const { npcId, message, language } = rawBody ?? {};
        if (!npcId || typeof npcId !== 'string') {
          sendJson(res, 400, {
            success: false,
            error: { code: 'missing-witness-id', message: 'Field "npcId" is required.' },
          });
          return;
        }

        try {
          const chatResult = await witnessUseCase.execute({
            npcId,
            message,
            language,
          });
          sendJson(res, 200, {
            success: true,
            response: chatResult,
          });
          return;
        } catch (chatErr) {
          if (chatErr instanceof UnknownWitnessError) {
            sendJson(res, 404, {
              success: false,
              error: { code: 'witness-not-found', message: chatErr.message },
            });
            return;
          }
          if (chatErr instanceof InvalidWitnessMessageError) {
            sendJson(res, 400, {
              success: false,
              error: { code: 'invalid-message', message: chatErr.message },
            });
            return;
          }
          sendJson(res, 500, {
            success: false,
            error: {
              code: 'server-error',
              message: 'Internal server error processing witness conversation.',
            },
          });
          return;
        }
      }

      // 9. GET /api/border/concierge (Border Concierge Profile & Suggested Prompts)
      if (method === 'GET' && pathname === '/api/border/concierge') {
        const profile = conciergeUseCase.getProfile();
        sendJson(res, 200, {
          success: true,
          concierge: profile,
        });
        return;
      }

      // 10. POST /api/border/concierge/chat (Grounded Traveler Guidance)
      if (method === 'POST' && pathname === '/api/border/concierge/chat') {
        let rawBody: any;
        try {
          rawBody = await readBody(req);
        } catch {
          sendJson(res, 400, {
            success: false,
            error: { code: 'invalid-body', message: 'Request body must be valid JSON' },
          });
          return;
        }

        const { message, language } = rawBody ?? {};
        if (typeof message !== 'string' || message.trim().length === 0) {
          sendJson(res, 400, {
            success: false,
            error: {
              code: 'invalid-message',
              message: 'Field "message" must be a non-empty string.',
            },
          });
          return;
        }

        try {
          const result = await conciergeUseCase.execute({
            message,
            language,
          });
          sendJson(res, 200, {
            success: true,
            ...result,
          });
          return;
        } catch (conciergeErr) {
          if (conciergeErr instanceof InvalidConciergeMessageError) {
            sendJson(res, 400, {
              success: false,
              error: { code: 'invalid-message', message: conciergeErr.message },
            });
            return;
          }
          sendJson(res, 500, {
            success: false,
            error: {
              code: 'server-error',
              message: 'Internal server error processing concierge inquiry.',
            },
          });
          return;
        }
      }

      // 11. GET /api/businesses/attendants (Storefront Business Attendants Roster)
      if (
        method === 'GET' &&
        (pathname === '/api/businesses/attendants' || pathname === '/api/business-attendants')
      ) {
        const roster = businessAttendantUseCase.getRoster();
        sendJson(res, 200, {
          success: true,
          ...roster,
        });
        return;
      }

      // 12. POST /api/businesses/attendants/chat (Storefront Business Attendant Interaction)
      if (
        method === 'POST' &&
        (pathname === '/api/businesses/attendants/chat' ||
          pathname === '/api/business-attendants/chat')
      ) {
        let rawBody: any;
        try {
          rawBody = await readBody(req);
        } catch {
          sendJson(res, 400, {
            success: false,
            error: { code: 'invalid-body', message: 'Request body must be valid JSON' },
          });
          return;
        }

        const { attendantId, message, language, evaluationTime } = rawBody ?? {};

        if (!attendantId || typeof attendantId !== 'string') {
          sendJson(res, 400, {
            success: false,
            error: {
              code: 'missing-attendant-id',
              message: 'Field "attendantId" is required and must be a string.',
            },
          });
          return;
        }

        if (typeof message !== 'string' || message.trim().length === 0) {
          sendJson(res, 400, {
            success: false,
            error: {
              code: 'invalid-message',
              message: 'Field "message" must be a non-empty string.',
            },
          });
          return;
        }

        if (message.trim().length > 500) {
          sendJson(res, 400, {
            success: false,
            error: {
              code: 'message-too-long',
              message: 'Message must not exceed 500 characters.',
            },
          });
          return;
        }

        if (
          language !== undefined &&
          language !== null &&
          language !== '' &&
          !['en', 'es'].includes(String(language).toLowerCase())
        ) {
          sendJson(res, 400, {
            success: false,
            error: { code: 'invalid-language', message: 'Language must be "en" or "es".' },
          });
          return;
        }

        try {
          const result = await businessAttendantUseCase.execute({
            attendantId,
            message,
            language,
            evaluationTime,
          });
          sendJson(res, 200, {
            success: true,
            response: result,
          });
          return;
        } catch (attendantErr) {
          if (attendantErr instanceof UnknownBusinessAttendantError) {
            sendJson(res, 404, {
              success: false,
              error: { code: 'attendant-not-found', message: attendantErr.message },
            });
            return;
          }
          if (attendantErr instanceof InvalidAttendantMessageError) {
            sendJson(res, 400, {
              success: false,
              error: { code: 'invalid-message', message: attendantErr.message },
            });
            return;
          }
          if (attendantErr instanceof BusinessSnapshotUnavailableError) {
            sendJson(res, 502, {
              success: false,
              error: {
                code: 'business-source-unavailable',
                message: attendantErr.message,
              },
            });
            return;
          }
          sendJson(res, 500, {
            success: false,
            error: {
              code: 'server-error',
              message: 'Internal server error processing business attendant inquiry.',
            },
          });
          return;
        }
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
