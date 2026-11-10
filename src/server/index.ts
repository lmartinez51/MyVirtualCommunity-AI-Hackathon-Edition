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
import {
  CITIZEN_REPRESENTATIVE_PROFILE,
  ISSUE_CATEGORY_CATALOG,
} from '../domain/citizen-report.js';
import {
  CreateCitizenReportDraftUseCase,
  InvalidImageEvidenceError,
  InvalidReportDescriptionError,
} from '../application/create-citizen-report-draft.js';
import {
  DraftNotFoundError,
  PlayerConfirmationRequiredError,
  SubmitCitizenReportUseCase,
} from '../application/submit-citizen-report.js';
import { GetCitizenReportUseCase } from '../application/get-citizen-report.js';
import {
  isValidLaneId,
  isValidBarrierState,
  LaneBarrierId,
  BarrierPhysicalState,
  createDefaultBorderLaneBarriers,
} from '../domain/border-lane-barriers.js';
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
  readonly createDraftUseCase?: CreateCitizenReportDraftUseCase;
  readonly submitReportUseCase?: SubmitCitizenReportUseCase;
  readonly getReportUseCase?: GetCitizenReportUseCase;
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
    const defaultReportsPath = resolve(process.cwd(), '.runtime', 'citizen-reports.json');
    worldCore = new InMemoryWorldCoreAdapter({ persistenceFilePath: defaultReportsPath });
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

  const createDraftUseCase =
    options.createDraftUseCase ??
    new CreateCitizenReportDraftUseCase({
      metaModel,
    });

  const submitReportUseCase =
    options.submitReportUseCase ??
    new SubmitCitizenReportUseCase({
      worldCore,
    });

  const getReportUseCase =
    options.getReportUseCase ??
    new GetCitizenReportUseCase({
      worldCore,
    });

  const sendJson = (res: ServerResponse, statusCode: number, payload: unknown) => {
    res.statusCode = statusCode;
    res.setHeader('Content-Type', 'application/json; charset=utf-8');
    res.end(JSON.stringify(payload, null, 2));
  };

  const readBody = async (req: IncomingMessage, maxBytes: number = 64 * 1024): Promise<any> => {
    return new Promise((resolveBody, rejectBody) => {
      let body = '';
      req.on('data', (chunk) => {
        body += chunk;
        if (body.length > maxBytes) {
          rejectBody(new Error(`Request body exceeded ${maxBytes} bytes limit`));
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

      // 4b. GET /api/world/npcs/activity (Authoritative Roster NPC Activity)
      if (method === 'GET' && (pathname === '/api/world/npcs/activity' || pathname === '/api/npcs/activity')) {
        const atParam = parsedUrl.searchParams.get('at');
        if ('getNpcRosterActivity' in worldCore && typeof worldCore.getNpcRosterActivity === 'function') {
          const rosterActivity = await worldCore.getNpcRosterActivity(atParam ?? undefined);
          sendJson(res, 200, {
            success: true,
            ...rosterActivity,
          });
        } else {
          sendJson(res, 501, {
            success: false,
            error: { code: 'not-supported', message: 'World Core does not implement getNpcRosterActivity' },
          });
        }
        return;
      }

      // 4c. GET /api/world/npcs/:id/activity (Individual NPC Authoritative Activity)
      const npcActivityMatch = pathname.match(/^\/api\/(?:world\/)?npcs\/([a-zA-Z0-9_-]+)\/activity$/);
      if (method === 'GET' && npcActivityMatch && npcActivityMatch[1]) {
        const npcId = npcActivityMatch[1];
        const atParam = parsedUrl.searchParams.get('at');
        if ('getNpcActivity' in worldCore && typeof worldCore.getNpcActivity === 'function') {
          const activity = await worldCore.getNpcActivity(npcId, atParam ?? undefined);
          if (!activity) {
            sendJson(res, 404, {
              success: false,
              error: { code: 'npc-not-found', message: `NPC "${npcId}" not found in authoritative roster.` },
            });
            return;
          }
          sendJson(res, 200, {
            success: true,
            activity,
          });
        } else {
          sendJson(res, 501, {
            success: false,
            error: { code: 'not-supported', message: 'World Core does not implement getNpcActivity' },
          });
        }
        return;
      }

      // 4d. GET /api/world/environment (Authoritative Environmental Lighting & World Time)
      if (
        method === 'GET' &&
        (pathname === '/api/world/environment' ||
          pathname === '/api/environment' ||
          pathname === '/api/world/time' ||
          pathname === '/api/time')
      ) {
        const atParam = parsedUrl.searchParams.get('at');
        if ('getWorldEnvironment' in worldCore && typeof worldCore.getWorldEnvironment === 'function') {
          const envSnapshot = await worldCore.getWorldEnvironment(atParam ?? undefined);
          sendJson(res, 200, {
            success: true,
            ...envSnapshot,
          });
        } else {
          sendJson(res, 501, {
            success: false,
            error: { code: 'not-supported', message: 'World Core does not implement getWorldEnvironment' },
          });
        }
        return;
      }

      // 4e. GET /api/world/npcs/movement (Authoritative Roster NPC Waypoint Movement)
      if (method === 'GET' && (pathname === '/api/world/npcs/movement' || pathname === '/api/npcs/movement')) {
        const atParam = parsedUrl.searchParams.get('at');
        if ('getNpcRosterMovement' in worldCore && typeof worldCore.getNpcRosterMovement === 'function') {
          const rosterMovement = await worldCore.getNpcRosterMovement(atParam ?? undefined);
          sendJson(res, 200, {
            success: true,
            ...rosterMovement,
          });
        } else {
          sendJson(res, 501, {
            success: false,
            error: { code: 'not-supported', message: 'World Core does not implement getNpcRosterMovement' },
          });
        }
        return;
      }

      // 4f. GET /api/world/npcs/:id/movement (Individual NPC Authoritative Waypoint Movement)
      const npcMovementMatch = pathname.match(/^\/api\/(?:world\/)?npcs\/([a-zA-Z0-9_-]+)\/movement$/);
      if (method === 'GET' && npcMovementMatch && npcMovementMatch[1]) {
        const npcId = npcMovementMatch[1];
        const atParam = parsedUrl.searchParams.get('at');
        if ('getNpcMovement' in worldCore && typeof worldCore.getNpcMovement === 'function') {
          const movement = await worldCore.getNpcMovement(npcId, atParam ?? undefined);
          if (!movement) {
            sendJson(res, 404, {
              success: false,
              error: { code: 'npc-not-found', message: `NPC "${npcId}" not found in authoritative movement roster.` },
            });
            return;
          }
          sendJson(res, 200, {
            success: true,
            movement,
          });
        } else {
          sendJson(res, 501, {
            success: false,
            error: { code: 'not-supported', message: 'World Core does not implement getNpcMovement' },
          });
        }
        return;
      }

      // 4g. GET /api/world/npcs/interaction-context (Authoritative Roster NPC Interaction Context)
      if (
        method === 'GET' &&
        (pathname === '/api/world/npcs/interaction-context' || pathname === '/api/npcs/interaction-context')
      ) {
        const atParam = parsedUrl.searchParams.get('at');
        if ('getNpcRosterInteractionContext' in worldCore && typeof worldCore.getNpcRosterInteractionContext === 'function') {
          const rosterContext = await worldCore.getNpcRosterInteractionContext(atParam ?? undefined);
          sendJson(res, 200, {
            success: true,
            ...rosterContext,
          });
        } else {
          sendJson(res, 501, {
            success: false,
            error: { code: 'not-supported', message: 'World Core does not implement getNpcRosterInteractionContext' },
          });
        }
        return;
      }

      // 4h. GET /api/world/npcs/:id/interaction-context (Individual NPC Authoritative Interaction Context)
      const npcInteractionContextMatch = pathname.match(/^\/api\/(?:world\/)?npcs\/([a-zA-Z0-9_-]+)\/interaction-context$/);
      if (method === 'GET' && npcInteractionContextMatch && npcInteractionContextMatch[1]) {
        const npcId = npcInteractionContextMatch[1];
        const atParam = parsedUrl.searchParams.get('at');
        if ('getNpcInteractionContext' in worldCore && typeof worldCore.getNpcInteractionContext === 'function') {
          const context = await worldCore.getNpcInteractionContext(npcId, atParam ?? undefined);
          if (!context) {
            sendJson(res, 404, {
              success: false,
              error: { code: 'npc-not-found', message: `NPC "${npcId}" not found in authoritative interaction roster.` },
            });
            return;
          }
          sendJson(res, 200, {
            success: true,
            context,
            interactionContext: context,
          });
        } else {
          sendJson(res, 501, {
            success: false,
            error: { code: 'not-supported', message: 'World Core does not implement getNpcInteractionContext' },
          });
        }
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

      // 6B. GET /api/border/lane-barriers (Authoritative 4-Lane Toll Barriers Status)
      if (method === 'GET' && pathname === '/api/border/lane-barriers') {
        const barriers =
          'getBorderLaneBarriers' in worldCore && typeof worldCore.getBorderLaneBarriers === 'function'
            ? await worldCore.getBorderLaneBarriers()
            : createDefaultBorderLaneBarriers();

        sendJson(res, 200, {
          success: true,
          worldTime: barriers.worldTime,
          lanes: barriers.lanes,
          lanesList: Object.values(barriers.lanes),
        });
        return;
      }

      // 6C. POST /api/border/lane-barriers/:laneId (Update physical barrier status: open / closed)
      const laneBarrierMatch = pathname.match(/^\/api\/border\/lane-barriers\/(lane-[1-4])$/);
      if (method === 'POST' && laneBarrierMatch && laneBarrierMatch[1]) {
        const laneId = laneBarrierMatch[1] as LaneBarrierId;
        let rawBody: any;
        try {
          rawBody = await readBody(req);
        } catch {
          sendJson(res, 400, {
            success: false,
            error: { code: 'invalid-json-body', message: 'Request body must be valid JSON.' },
          });
          return;
        }

        const requestedStatus = rawBody?.status;
        if (!isValidBarrierState(requestedStatus)) {
          sendJson(res, 400, {
            success: false,
            error: {
              code: 'invalid-barrier-status',
              message: `Barrier status must be either 'open' or 'closed'. Received: ${String(requestedStatus)}`,
            },
          });
          return;
        }

        const reason = typeof rawBody?.reason === 'string' ? rawBody.reason.trim() : undefined;

        if ('setBorderLaneBarrierStatus' in worldCore && typeof worldCore.setBorderLaneBarrierStatus === 'function') {
          const updatedLane = await worldCore.setBorderLaneBarrierStatus(laneId, requestedStatus, reason);
          sendJson(res, 200, {
            success: true,
            lane: updatedLane,
          });
          return;
        }

        sendJson(res, 501, {
          success: false,
          error: { code: 'not-implemented', message: 'World Core does not support barrier mutation.' },
        });
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

        const cleanEvalTime =
          evaluationTime !== undefined &&
          evaluationTime !== null &&
          String(evaluationTime).trim() !== ''
            ? evaluationTime
            : undefined;

        try {
          const result = await businessAttendantUseCase.execute({
            attendantId,
            message,
            language,
            evaluationTime: cleanEvalTime,
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

      // 13. GET /api/citizen-reports/representative (Enlace de Atención Ciudadana Profile)
      if (
        method === 'GET' &&
        (pathname === '/api/citizen-reports/representative' ||
          pathname === '/api/citizen-report/representative')
      ) {
        sendJson(res, 200, {
          success: true,
          representative: CITIZEN_REPRESENTATIVE_PROFILE,
        });
        return;
      }

      // 13b. GET /api/citizen-reports/categories (Municipal Issue Categories Catalog)
      if (
        method === 'GET' &&
        (pathname === '/api/citizen-reports/categories' ||
          pathname === '/api/citizen-report/categories')
      ) {
        sendJson(res, 200, {
          success: true,
          categories: ISSUE_CATEGORY_CATALOG,
        });
        return;
      }

      // 14. POST /api/citizen-reports/draft (Draft Preparation with SAM 3.1 & Muse Spark)
      if (
        method === 'POST' &&
        (pathname === '/api/citizen-reports/draft' || pathname === '/api/citizen-report/draft')
      ) {
        let rawBody: any;
        try {
          // Allow up to 10MB payload to accommodate photographic evidence base64 data
          rawBody = await readBody(req, 10 * 1024 * 1024);
        } catch (bodyErr) {
          sendJson(res, 400, {
            success: false,
            error: {
              code: 'invalid-body',
              message: bodyErr instanceof Error ? bodyErr.message : 'Invalid request body or size exceeded',
            },
          });
          return;
        }

        try {
          const draftResult = await createDraftUseCase.execute(rawBody);
          sendJson(res, 200, {
            success: true,
            draft: draftResult,
          });
          return;
        } catch (draftErr) {
          if (draftErr instanceof InvalidReportDescriptionError) {
            sendJson(res, 400, {
              success: false,
              error: { code: 'invalid-description', message: draftErr.message },
            });
            return;
          }
          if (draftErr instanceof InvalidImageEvidenceError) {
            sendJson(res, 400, {
              success: false,
              error: { code: 'invalid-image-evidence', message: draftErr.message },
            });
            return;
          }
          sendJson(res, 500, {
            success: false,
            error: {
              code: 'server-error',
              message: 'Internal server error preparing citizen report draft.',
            },
          });
          return;
        }
      }

      // 15. POST /api/citizen-reports/submit (Authoritative World Core Report Registration)
      if (
        method === 'POST' &&
        (pathname === '/api/citizen-reports/submit' || pathname === '/api/citizen-report/submit')
      ) {
        let rawBody: any;
        try {
          rawBody = await readBody(req, 10 * 1024 * 1024);
        } catch (bodyErr) {
          sendJson(res, 400, {
            success: false,
            error: {
              code: 'invalid-body',
              message: bodyErr instanceof Error ? bodyErr.message : 'Invalid request body',
            },
          });
          return;
        }

        try {
          const submitResult = await submitReportUseCase.execute(rawBody);
          sendJson(res, 201, submitResult);
          return;
        } catch (submitErr) {
          if (submitErr instanceof PlayerConfirmationRequiredError) {
            sendJson(res, 400, {
              success: false,
              error: { code: 'confirmation-required', message: submitErr.message },
            });
            return;
          }
          if (submitErr instanceof DraftNotFoundError) {
            sendJson(res, 404, {
              success: false,
              error: { code: 'draft-not-found', message: submitErr.message },
            });
            return;
          }
          sendJson(res, 500, {
            success: false,
            error: {
              code: 'server-error',
              message: 'Internal server error registering citizen report in World Core.',
            },
          });
          return;
        }
      }

      // 16. GET /api/citizen-reports/:id (Get Report by ID)
      if (
        method === 'GET' &&
        pathname.startsWith('/api/citizen-reports/') &&
        pathname !== '/api/citizen-reports/representative' &&
        pathname !== '/api/citizen-reports/categories' &&
        pathname !== '/api/citizen-reports/draft' &&
        pathname !== '/api/citizen-reports/submit'
      ) {
        const parts = pathname.split('/');
        const reportId = decodeURIComponent(parts[3] ?? '').trim();
        const report = await getReportUseCase.getById(reportId);
        if (!report) {
          sendJson(res, 404, {
            success: false,
            error: { code: 'report-not-found', message: `Citizen report "${reportId}" not found.` },
          });
          return;
        }
        sendJson(res, 200, {
          success: true,
          report,
        });
        return;
      }

      // 17. GET /api/citizen-reports (List Reports)
      if (
        method === 'GET' &&
        (pathname === '/api/citizen-reports' || pathname === '/api/citizen-reports/')
      ) {
        const reports = await getReportUseCase.list();
        sendJson(res, 200, {
          success: true,
          count: reports.length,
          reports,
        });
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
