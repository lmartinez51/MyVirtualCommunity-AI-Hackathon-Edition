import { BusinessId, createBusinessId } from '../domain/business-id.js';
import { BusinessSchedule } from '../domain/business-schedule.js';
import {
  BusinessOperationalStatus,
  evaluateBusinessOperationalStatus,
} from '../domain/operational-status.js';
import {
  InvalidEvaluationTimeError,
  parseLocalCivilTimeToUtc,
} from '../domain/civil-time.js';
import { MetaModelPort, MetaModelUsage } from '../ports/meta-model-port.js';
import { WorldCorePort } from '../ports/world-core-port.js';

export { InvalidEvaluationTimeError };

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
  readonly evaluationTime?: string | Date;
  readonly evaluatedAt?: string | Date;
  readonly evaluatedAtOverride?: Date | string | number;
}

export interface BusinessHoursQueryResponse {
  readonly success: true;
  readonly businessId?: string;
  readonly question?: string;
  readonly answer: string;
  readonly verifiedStatus: BusinessOperationalStatus;
  readonly schedule: BusinessSchedule;
  readonly evaluatedAtUtc: string;
  readonly evaluatedAtCivil: string;
  readonly evaluationMode: 'current' | 'specific';
  readonly isAmbiguousEvaluationTime?: boolean;
  readonly disambiguationNotice?: string;
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

    // 4. Resolve optional evaluation time
    let evaluationInstant: Date | undefined = undefined;
    let evaluationMode: 'current' | 'specific' = 'current';
    let isAmbiguousEvaluationTime = false;
    let disambiguationNotice: string | undefined = undefined;

    const rawEvalTime =
      request.evaluationTime ?? request.evaluatedAt ?? request.evaluatedAtOverride;

    if (rawEvalTime !== undefined && rawEvalTime !== null && rawEvalTime !== '') {
      evaluationMode = 'specific';
      if (rawEvalTime instanceof Date) {
        if (Number.isNaN(rawEvalTime.getTime())) {
          throw new InvalidEvaluationTimeError('Provided evaluation Date is invalid (NaN)');
        }
        evaluationInstant = rawEvalTime;
      } else if (typeof rawEvalTime === 'number') {
        if (!Number.isFinite(rawEvalTime) || Number.isNaN(rawEvalTime)) {
          throw new InvalidEvaluationTimeError(`Invalid numeric evaluation timestamp: ${rawEvalTime}`);
        }
        evaluationInstant = new Date(rawEvalTime);
      } else if (typeof rawEvalTime === 'string') {
        // Parse local civil time in America/Ciudad_Juarez
        const parseResult = parseLocalCivilTimeToUtc(rawEvalTime, 'America/Ciudad_Juarez');
        if (!parseResult.success) {
          throw new InvalidEvaluationTimeError(
            parseResult.error ?? `Invalid evaluation time: "${rawEvalTime}"`
          );
        }
        evaluationInstant = parseResult.utcInstant;
        if (parseResult.isAmbiguous) {
          isAmbiguousEvaluationTime = true;
          disambiguationNotice = parseResult.disambiguationNotice;
        }
      } else {
        throw new InvalidEvaluationTimeError(
          `Invalid evaluation time type: expected string, Date, or number; received ${typeof rawEvalTime}`
        );
      }
    }

    // 5. Fetch coherent authoritative snapshot from World Core
    let schedule: BusinessSchedule;
    let verifiedStatus: BusinessOperationalStatus;
    let evaluatedAtUtc: string;

    if (typeof this.worldCore.getBusinessScheduleSnapshot === 'function') {
      const snapshot = await this.worldCore.getBusinessScheduleSnapshot(
        businessId,
        evaluationInstant
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
        evaluationInstant ?? (await this.worldCore.getCurrentWorldTime());
      verifiedStatus = evaluateBusinessOperationalStatus(schedule, evaluationTime);
      evaluatedAtUtc = verifiedStatus.evaluatedAtUtc;
    }

    // 6. Build targeted system prompt according to selected language
    const systemPrompt =
      lang === 'es'
        ? 'Eres un asistente de negocios locales cortés, claro y conciso para la comunidad virtual de Ciudad Juárez y El Paso. ' +
          'Responde en español a las consultas sobre horarios, disponibilidad y admisión para la fecha y hora evaluada. ' +
          'REGLAS CRÍTICAS: ' +
          '1. Basa tu respuesta estricta y autoritativamente en los DATOS OPERATIVOS VERIFICADOS proporcionados abajo. ' +
          '2. Conserva los nombres comerciales canónicos exactamente como se proporcionan (por ejemplo, "Wing Spot Gómez Morín"). No traduzcas, transliteres, normalices ni alteres los nombres comerciales. ' +
          '3. No recalcules el estado operativo por tu cuenta, no inventes horarios ni contradigas los datos verificados. ' +
          '4. Distingue claramente entre si el negocio está operativamente abierto y si la admisión / entrada todavía está permitida a la hora consultada. ' +
          'Cuando el negocio esté abierto pero el ingreso ya no esté permitido (límite de última entrada alcanzado), aclara explícitamente que el local sigue operando para clientes dentro pero no se admiten nuevos ingresos ni comensales.'
        : 'You are a courteous, clear, and concise local-business assistant for the Ciudad Juárez–El Paso virtual community. ' +
          'Respond in American English to customer questions about business hours, operational availability, and admission for the evaluated date and time. ' +
          'CRITICAL RULES: ' +
          '1. Base your answer strictly and authoritatively on the VERIFIED OPERATIONAL DATA provided below. ' +
          '2. Preserve the canonical business name exactly as returned by the World Core (e.g. "Wing Spot Gómez Morín"). Do not translate, transliterate, normalize, or alter business names. ' +
          '3. Do not recalculate operational status, guess, or contradict the verified data. ' +
          '4. Clearly distinguish between whether the business is operationally open and whether admission is permitted at the evaluated time. ' +
          'When the business is open but admission is no longer permitted (last entry cutoff reached), explicitly state that the business is open for existing guests but new admissions, dine-in entries, or orders are denied.';

    // 7. Formulate natural language response using Meta Model grounded in verified data
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
      businessId: request.businessId,
      question: cleanQuery,
      answer: metaResponse.text,
      verifiedStatus,
      schedule,
      evaluatedAtUtc,
      evaluatedAtCivil: verifiedStatus.civilTime.isoCivilString,
      evaluationMode,
      isAmbiguousEvaluationTime: isAmbiguousEvaluationTime ? true : undefined,
      disambiguationNotice,
      modelUsed: metaResponse.model,
      latencyMs,
      language: lang,
      usage: metaResponse.usage,
    };
  }
}
