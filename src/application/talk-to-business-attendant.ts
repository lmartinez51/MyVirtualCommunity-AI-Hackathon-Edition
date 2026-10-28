import {
  AttendantInquiryTopic,
  BUSINESS_ATTENDANT_ROSTER,
  BusinessAttendantId,
  BusinessAttendantProfile,
  classifyAttendantInquiry,
  formatOperatingHoursDescription,
  getDeterministicAttendantResponse,
} from '../domain/business-attendant.js';
import { formatBorderFetchTime } from '../domain/border-crossing.js';
import { BusinessOperationalStatus, evaluateBusinessOperationalStatus } from '../domain/operational-status.js';
import { BusinessSchedule } from '../domain/business-schedule.js';
import { MetaModelPort } from '../ports/meta-model-port.js';
import { WorldCorePort } from '../ports/world-core-port.js';

export class UnknownBusinessAttendantError extends Error {
  constructor(attendantId: string) {
    super(`Business attendant "${attendantId}" is not registered in the storefront roster`);
    this.name = 'UnknownBusinessAttendantError';
  }
}

export class InvalidAttendantMessageError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'InvalidAttendantMessageError';
  }
}

export class BusinessSnapshotUnavailableError extends Error {
  constructor(businessId: string) {
    super(`Authoritative operational snapshot for business "${businessId}" is currently unavailable from World Core`);
    this.name = 'BusinessSnapshotUnavailableError';
  }
}

export interface TalkToBusinessAttendantRequest {
  readonly attendantId: string;
  readonly message: string;
  readonly language?: 'en' | 'es' | string;
  readonly evaluationTime?: Date | string | number;
}

export interface AuthoritativeStorefrontFacts {
  readonly businessName: string;
  readonly businessId: string;
  readonly address: string;
  readonly status: string;
  readonly isOpen: boolean;
  readonly admissionPermitted: boolean;
  readonly hoursSummary: string;
  readonly openTimeCivil: string;
  readonly closeTimeCivil: string;
  readonly cutoffTimeCivil: string;
  readonly timeZone: string;
  readonly evaluatedAtCivil: string;
  readonly evaluatedAtUtc: string;
  readonly unavailableFactsNotice?: string;
}

export interface TalkToBusinessAttendantResponse {
  readonly attendantId: BusinessAttendantId;
  readonly businessId: string;
  readonly businessCanonicalName: string;
  readonly attendantName: string;
  readonly attendantRole: string;
  readonly text: string;
  readonly topic: AttendantInquiryTopic;
  readonly language: 'en' | 'es';
  readonly virtualDisclosure: string;
  readonly provider: 'meta-muse-spark-1.3' | 'deterministic-fallback' | 'mock-meta';
  readonly authoritativeFacts: AuthoritativeStorefrontFacts;
  readonly latencyMs: number;
}

export interface TalkToBusinessAttendantDependencies {
  readonly worldCore: WorldCorePort;
  readonly metaModel: MetaModelPort;
}

export class TalkToBusinessAttendantUseCase {
  private readonly worldCore: WorldCorePort;
  private readonly metaModel: MetaModelPort;

  constructor(dependencies: TalkToBusinessAttendantDependencies) {
    this.worldCore = dependencies.worldCore;
    this.metaModel = dependencies.metaModel;
  }

  /**
   * Returns the server-controlled roster of storefront attendants.
   */
  public getRoster(): {
    readonly attendants: readonly BusinessAttendantProfile[];
    readonly total: number;
  } {
    const list = Object.values(BUSINESS_ATTENDANT_ROSTER);
    return {
      attendants: list,
      total: list.length,
    };
  }

  /**
   * Resolves attendant by ID from server-side roster.
   */
  public getAttendantById(id: string): BusinessAttendantProfile | null {
    if (!id || typeof id !== 'string') return null;
    const cleanId = id.trim() as BusinessAttendantId;
    return BUSINESS_ATTENDANT_ROSTER[cleanId] ?? null;
  }

  /**
   * Executes a storefront customer interaction with a selected attendant.
   */
  public async execute(
    request: TalkToBusinessAttendantRequest
  ): Promise<TalkToBusinessAttendantResponse> {
    const startTime = performance.now();

    // 1. Validate attendant ID strictly from server configuration
    if (!request.attendantId || typeof request.attendantId !== 'string') {
      throw new UnknownBusinessAttendantError(String(request.attendantId ?? ''));
    }
    const cleanId = request.attendantId.trim() as BusinessAttendantId;
    const profile = BUSINESS_ATTENDANT_ROSTER[cleanId];
    if (!profile) {
      throw new UnknownBusinessAttendantError(request.attendantId);
    }

    // 2. Validate user message
    if (typeof request.message !== 'string' || request.message.trim().length === 0) {
      throw new InvalidAttendantMessageError('Message must be a non-empty string');
    }
    const cleanMessage = request.message.trim();
    if (cleanMessage.length > 500) {
      throw new InvalidAttendantMessageError('Message must not exceed 500 characters');
    }

    // 3. Resolve language
    let lang: 'en' | 'es' = 'en';
    if (request.language !== undefined && request.language !== null && request.language !== '') {
      const normLang = String(request.language).toLowerCase();
      if (normLang !== 'en' && normLang !== 'es') {
        throw new InvalidAttendantMessageError('Language must be "en" or "es"');
      }
      lang = normLang as 'en' | 'es';
    } else if (/[¿áéíóúñ]/i.test(cleanMessage)) {
      lang = 'es';
    }

    // 4. Resolve authoritative business data from World Core
    let schedule: BusinessSchedule;
    let status: BusinessOperationalStatus;

    try {
      if (typeof this.worldCore.getBusinessScheduleSnapshot === 'function') {
        const snapshot = await this.worldCore.getBusinessScheduleSnapshot(
          profile.businessId,
          request.evaluationTime
        );
        if (!snapshot) {
          throw new BusinessSnapshotUnavailableError(profile.businessId);
        }
        schedule = snapshot.schedule;
        status = snapshot.operationalStatus;
      } else {
        const sched = await this.worldCore.getBusinessSchedule(profile.businessId);
        if (!sched) {
          throw new BusinessSnapshotUnavailableError(profile.businessId);
        }
        schedule = sched;
        const evalTime = request.evaluationTime ?? (await this.worldCore.getCurrentWorldTime());
        status = evaluateBusinessOperationalStatus(schedule, evalTime);
      }
    } catch (coreErr) {
      if (coreErr instanceof BusinessSnapshotUnavailableError) {
        throw coreErr;
      }
      throw new BusinessSnapshotUnavailableError(profile.businessId);
    }

    // 5. Classify customer inquiry topic
    const topic = classifyAttendantInquiry(cleanMessage);

    // 6. Build authoritative facts payload
    const isEs = lang === 'es';
    const hoursSummary = formatOperatingHoursDescription(schedule, lang);
    const evalLocalFormatted = formatBorderFetchTime(status.evaluatedAtUtc, lang);

    let unavailableFactsNotice: string | undefined = undefined;
    if (['inventory', 'medical', 'pricing', 'appointment'].includes(topic)) {
      unavailableFactsNotice = isEs
        ? 'Aviso: El inventario de productos en vivo, precios, cotizaciones, citas médicas y recetas no forman parte de los registros comerciales verificados del sistema. El encargado virtual no puede verificar ni comprometer estas solicitudes.'
        : 'Notice: Live product inventory, pricing, quotes, appointments, and medical prescriptions are not part of the system verified records. The virtual attendant cannot confirm or fulfill these requests.';
    }

    const authoritativeFacts: AuthoritativeStorefrontFacts = {
      businessName: profile.businessCanonicalName,
      businessId: profile.businessId,
      address: profile.verifiedAddress,
      status: status.status,
      isOpen: status.isOpen,
      admissionPermitted: status.admissionPermitted,
      hoursSummary,
      openTimeCivil: status.openTimeCivil,
      closeTimeCivil: status.closeTimeCivil,
      cutoffTimeCivil: status.cutoffTimeCivil,
      timeZone: status.timeZone,
      evaluatedAtCivil: evalLocalFormatted,
      evaluatedAtUtc: status.evaluatedAtUtc,
      unavailableFactsNotice,
    };

    // 7. Route through deterministic or model path
    // For safety boundaries (inventory, medical, pricing, appointments), or when exact status/hours are asked:
    // We enforce deterministic grounding, or invoke model if available with deterministic fallback.
    let responseText: string;
    let provider: 'meta-muse-spark-1.3' | 'deterministic-fallback' | 'mock-meta' = 'deterministic-fallback';

    // If query is directly a boundary violation (medical, inventory, pricing, appointment),
    // we use the deterministic response path to guarantee zero fabrication.
    if (['inventory', 'medical', 'pricing', 'appointment'].includes(topic)) {
      responseText = getDeterministicAttendantResponse(
        profile,
        status,
        schedule,
        topic,
        cleanMessage,
        lang
      );
      provider = 'deterministic-fallback';
    } else if (typeof this.metaModel.generateBusinessAttendantResponse === 'function') {
      try {
        const metaRes = await this.metaModel.generateBusinessAttendantResponse({
          attendant: profile,
          status,
          schedule,
          userMessage: cleanMessage,
          language: lang,
        });

        if (metaRes && typeof metaRes.text === 'string' && metaRes.text.trim().length > 0) {
          responseText = metaRes.text.trim();
          provider = (this.metaModel as any).mode === 'live-api' ? 'meta-muse-spark-1.3' : 'mock-meta';
        } else {
          responseText = getDeterministicAttendantResponse(
            profile,
            status,
            schedule,
            topic,
            cleanMessage,
            lang
          );
          provider = 'deterministic-fallback';
        }
      } catch {
        responseText = getDeterministicAttendantResponse(
          profile,
          status,
          schedule,
          topic,
          cleanMessage,
          lang
        );
        provider = 'deterministic-fallback';
      }
    } else {
      responseText = getDeterministicAttendantResponse(
        profile,
        status,
        schedule,
        topic,
        cleanMessage,
        lang
      );
      provider = 'deterministic-fallback';
    }

    const latencyMs = Math.round(performance.now() - startTime);

    return {
      attendantId: profile.id,
      businessId: profile.businessId,
      businessCanonicalName: profile.businessCanonicalName,
      attendantName: profile.name,
      attendantRole: profile.role[lang],
      text: responseText,
      topic,
      language: lang,
      virtualDisclosure: profile.virtualMarker[lang],
      provider,
      authoritativeFacts,
      latencyMs,
    };
  }
}

