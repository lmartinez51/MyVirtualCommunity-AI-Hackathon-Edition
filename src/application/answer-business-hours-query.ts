import { BusinessId, createBusinessId } from '../domain/business-id.js';
import { BusinessSchedule } from '../domain/business-schedule.js';
import {
  BusinessOperationalStatus,
  evaluateBusinessOperationalStatus,
} from '../domain/operational-status.js';
import { MetaModelPort, MetaModelUsage } from '../ports/meta-model-port.js';
import { WorldCorePort } from '../ports/world-core-port.js';

export class BusinessNotFoundError extends Error {
  constructor(businessId: string) {
    super(`Business "${businessId}" was not found in World Core records`);
    this.name = 'BusinessNotFoundError';
  }
}

export class InvalidQueryError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'InvalidQueryError';
  }
}

export type SupportedLanguage = 'en' | 'es';

export interface BusinessHoursQueryRequest {
  readonly businessId: string;
  readonly question?: string;
  readonly userQuery?: string;
  readonly language?: SupportedLanguage | string;
  readonly evaluatedAtOverride?: Date | string | number;
}

export interface BusinessHoursQueryResponse {
  readonly success: true;
  readonly answer: string;
  readonly verifiedStatus: BusinessOperationalStatus;
  readonly schedule: BusinessSchedule;
  readonly evaluatedAtUtc: string;
  readonly modelUsed: string;
  readonly latencyMs: number;
  readonly language: SupportedLanguage;
  readonly usage?: MetaModelUsage;
}

export interface AnswerBusinessHoursQueryDependencies {
  readonly worldCore: WorldCorePort;
  readonly metaModel: MetaModelPort;
}

/**
 * Use case: Answer a customer's business hours / operational status question
 * using authoritative, verified structured data from the World Core and the Meta Model API.
 *
 * Invariant:
 * The language model NEVER determines or invents the business schedule or operational status.
 * All status derivation is computed deterministically in the domain layer before invoking
 * the Meta Model for natural-language formulation.
 */
export class AnswerBusinessHoursQueryUseCase {
  private readonly worldCore: WorldCorePort;
  private readonly metaModel: MetaModelPort;

  constructor(dependencies: AnswerBusinessHoursQueryDependencies) {
    this.worldCore = dependencies.worldCore;
    this.metaModel = dependencies.metaModel;
  }

  public async execute(
    request: BusinessHoursQueryRequest
  ): Promise<BusinessHoursQueryResponse> {
    // 1. Validate query string (supports both 'question' and legacy 'userQuery')
    const rawQuery = request.question ?? request.userQuery;
    if (typeof rawQuery !== 'string' || rawQuery.trim() === '') {
      throw new InvalidQueryError('Question must be a non-empty string');
    }
    const cleanQuery = rawQuery.trim();
    if (cleanQuery.length > 500) {
      throw new InvalidQueryError('Question must not exceed 500 characters');
    }

    // 2. Validate language (defaults to 'en', or infers Spanish if query has Spanish markers and language omitted)
    let lang: SupportedLanguage = 'en';
    if (request.language !== undefined && request.language !== null && request.language !== '') {
      const normalizedLang = String(request.language).toLowerCase();
      if (normalizedLang !== 'en' && normalizedLang !== 'es') {
        throw new InvalidQueryError('Language must be "en" or "es"');
      }
      lang = normalizedLang as SupportedLanguage;
    } else if (/[¿áéíóúñ]/i.test(cleanQuery)) {
      lang = 'es';
    }

    // 3. Validate business ID
    const businessId: BusinessId = createBusinessId(request.businessId);

    // 4. Fetch coherent authoritative snapshot from World Core
    let schedule: BusinessSchedule;
    let verifiedStatus: BusinessOperationalStatus;
    let evaluatedAtUtc: string;

    if (typeof this.worldCore.getBusinessScheduleSnapshot === 'function') {
      const snapshot = await this.worldCore.getBusinessScheduleSnapshot(
        businessId,
        request.evaluatedAtOverride
      );
      if (!snapshot) {
        throw new BusinessNotFoundError(businessId);
      }
      schedule = snapshot.schedule;
      verifiedStatus = snapshot.operationalStatus;
      evaluatedAtUtc = snapshot.worldTime;
    } else {
      const sched = await this.worldCore.getBusinessSchedule(businessId);
      if (!sched) {
        throw new BusinessNotFoundError(businessId);
      }
      schedule = sched;
      const evaluationTime =
        request.evaluatedAtOverride ?? (await this.worldCore.getCurrentWorldTime());
      verifiedStatus = evaluateBusinessOperationalStatus(schedule, evaluationTime);
      evaluatedAtUtc = verifiedStatus.evaluatedAtUtc;
    }

    // 5. Build targeted system prompt according to selected language
    const systemPrompt =
      lang === 'es'
        ? 'Eres un asistente de negocios locales cortés, claro y conciso para la comunidad virtual de Ciudad Juárez y El Paso. ' +
          'Responde en español a las consultas sobre horarios y disponibilidad. ' +
          'REGLAS CRÍTICAS: ' +
          '1. Basa tu respuesta estricta y autoritativamente en los DATOS OPERATIVOS VERIFICADOS proporcionados abajo. ' +
          '2. Conserva los nombres comerciales canónicos exactamente como se proporcionan (por ejemplo, "Wing Spot Gómez Morín"). No traduzcas, transliteres, normalices ni alteres los nombres comerciales. ' +
          '3. No recalcules el estado operativo por tu cuenta, no inventes horarios ni contradigas los datos verificados. ' +
          '4. Cuando el negocio esté abierto pero el ingreso ya no esté permitido (por ejemplo, por cerrar / límite de admisión alcanzado), distingue claramente que el local sigue abierto para los clientes dentro pero ya no se admiten nuevos ingresos.'
        : 'You are a courteous, clear, and concise local-business assistant for the Ciudad Juárez–El Paso virtual community. ' +
          'Respond in American English to customer questions about business hours and operational availability. ' +
          'CRITICAL RULES: ' +
          '1. Base your answer strictly and authoritatively on the VERIFIED OPERATIONAL DATA provided below. ' +
          '2. Preserve the canonical business name exactly as returned by the World Core (e.g. "Wing Spot Gómez Morín"). Do not translate, transliterate, normalize, or alter business names. ' +
          '3. Do not recalculate operational status, guess, or contradict the verified data. ' +
          '4. When the business is open but admission is no longer permitted (e.g. closing soon / last entry cutoff reached), clearly distinguish that the business is currently open for existing guests but no new admissions or dine-in entries are permitted.';

    // 6. Formulate natural language response using Meta Model grounded in verified data
    const startTime = performance.now();
    const metaResponse = await this.metaModel.generateAssistantResponse({
      userQuery: cleanQuery,
      verifiedData: verifiedStatus,
      schedule,
      systemPrompt,
    });
    const latencyMs = Math.round(performance.now() - startTime);

    return {
      success: true,
      answer: metaResponse.text,
      verifiedStatus,
      schedule,
      evaluatedAtUtc,
      modelUsed: metaResponse.model,
      latencyMs,
      language: lang,
      usage: metaResponse.usage,
    };
  }
}
