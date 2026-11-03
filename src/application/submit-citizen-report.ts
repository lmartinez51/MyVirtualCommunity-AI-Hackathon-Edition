import {
  CitizenReportRecord,
  DEFAULT_REPORTING_REPRESENTATIVE,
  generateReportId,
  getSimulatedDepartment,
  normalizeIssueCategory,
  SIMULATED_MUNICIPAL_SERVICE_NOTICE_ES,
} from '../domain/citizen-report.js';
import { WorldCorePort } from '../ports/world-core-port.js';
import {
  CitizenReportDraftResult,
  clearCachedDraft,
  getCachedDraft,
} from './create-citizen-report-draft.js';

export class PlayerConfirmationRequiredError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'PlayerConfirmationRequiredError';
  }
}

export class DraftNotFoundError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'DraftNotFoundError';
  }
}

export interface SubmitCitizenReportInput {
  readonly draftId?: unknown;
  readonly confirmedByPlayer: unknown;
  readonly draftData?: unknown;
  readonly confirmedCategory?: unknown;
}

export interface SubmitCitizenReportResult {
  readonly success: true;
  readonly report: CitizenReportRecord;
  readonly receipt: {
    readonly reportId: string;
    readonly category: string;
    readonly location: string;
    readonly locationCoverage?: string;
    readonly representative?: string;
    readonly evidenceStatus: string;
    readonly visualAnalysisStatus: string;
    readonly simulatedDestination: string;
    readonly lifecycleState: string;
    readonly createdAt: string;
  };
  readonly simulationNotice: string;
}

export interface SubmitCitizenReportOptions {
  readonly worldCore: WorldCorePort;
}

export class SubmitCitizenReportUseCase {
  private readonly worldCore: WorldCorePort;

  constructor(options: SubmitCitizenReportOptions) {
    this.worldCore = options.worldCore;
  }

  public async execute(input: SubmitCitizenReportInput): Promise<SubmitCitizenReportResult> {
    if (!input || typeof input !== 'object') {
      throw new Error('Submit payload must be an object.');
    }

    // Requirement 11: Explicit player confirmation required before registration
    if (input.confirmedByPlayer !== true) {
      throw new PlayerConfirmationRequiredError(
        'Player confirmation is strictly required before registering a citizen report in World Core.'
      );
    }

    let draft: CitizenReportDraftResult | null = null;
    const draftIdStr = typeof input.draftId === 'string' ? input.draftId.trim() : null;

    if (draftIdStr) {
      draft = getCachedDraft(draftIdStr);
    }

    if (!draft && input.draftData && typeof input.draftData === 'object') {
      draft = input.draftData as CitizenReportDraftResult;
    }

    if (!draft) {
      throw new DraftNotFoundError(
        `No citizen report draft found for draftId "${draftIdStr}". Create a draft before submitting.`
      );
    }

    const nowIso = new Date().toISOString();
    const reportId = generateReportId();
    const isEs = draft.language === 'es';

    // Allow player to confirm or adjust category
    let confirmedCategory = draft.category;
    if (
      typeof input.confirmedCategory === 'string' &&
      input.confirmedCategory.trim().length > 0
    ) {
      confirmedCategory = normalizeIssueCategory(input.confirmedCategory);
    }

    const simulatedDestination = getSimulatedDepartment(confirmedCategory, draft.language);

    const reportRecord: CitizenReportRecord = {
      reportId,
      citizenDescription: draft.citizenDescription,
      language: draft.language,
      confirmedCategory,
      location: draft.location,
      reportingRepresentative: draft.reportingRepresentative || DEFAULT_REPORTING_REPRESENTATIVE,
      evidence: draft.evidence,
      externalAiProcessingConsent: draft.externalAiProcessingConsent ?? false,
      visualAnalysis: draft.visualAnalysis,
      structuredInterpretation: draft.structuredInterpretation,
      lifecycleState: 'RECEIVED',
      simulatedDestination,
      simulationNotice: draft.simulationNotice || SIMULATED_MUNICIPAL_SERVICE_NOTICE_ES,
      nonEmergencyAssessment: draft.nonEmergencyAssessment,
      createdAt: nowIso,
      updatedAt: nowIso,
    };

    // Authoritative World Core registration
    let registeredRecord: CitizenReportRecord;
    if (typeof this.worldCore.registerCitizenReport === 'function') {
      registeredRecord = await this.worldCore.registerCitizenReport(reportRecord);
    } else {
      registeredRecord = reportRecord;
    }

    if (draftIdStr) {
      clearCachedDraft(draftIdStr);
    }

    const evidenceStatus = draft.evidence
      ? isEs
        ? `Fotografía recibida (${draft.evidence.fileName}, ${(
            draft.evidence.fileSizeBytes /
            (1024 * 1024)
          ).toFixed(2)} MB)`
        : `Photograph received (${draft.evidence.fileName}, ${(
            draft.evidence.fileSizeBytes /
            (1024 * 1024)
          ).toFixed(2)} MB)`
      : isEs
      ? 'Sin evidencia fotográfica (opcional)'
      : 'No photographic evidence (optional)';

    const visualStatus = draft.visualAnalysis
      ? draft.visualAnalysis.status === 'completed'
        ? isEs
          ? `Segmentación completada (${draft.visualAnalysis.boxes.length} región(es) detectada(s))`
          : `Segmentation completed (${draft.visualAnalysis.boxes.length} candidate region(s))`
        : draft.visualAnalysis.status === 'inconclusive'
        ? isEs
          ? 'Segmentación no concluyente (sin detección coincidente)'
          : 'Segmentation inconclusive (no matching detection)'
        : draft.visualAnalysis.provider === 'not-authorized'
        ? isEs
          ? 'No autorizado por usuario (privacidad)'
          : 'Not authorized by user (privacy)'
        : isEs
        ? 'Análisis visual no disponible'
        : 'Visual analysis unavailable'
      : isEs
      ? 'No aplicable'
      : 'Not applicable';

    return {
      success: true,
      report: registeredRecord,
      receipt: {
        reportId: registeredRecord.reportId,
        category: registeredRecord.confirmedCategory,
        location: registeredRecord.location.description,
        locationCoverage: registeredRecord.location.coverageStatus,
        representative: registeredRecord.reportingRepresentative.name,
        evidenceStatus,
        visualAnalysisStatus: visualStatus,
        simulatedDestination: registeredRecord.simulatedDestination,
        lifecycleState: registeredRecord.lifecycleState,
        createdAt: registeredRecord.createdAt,
      },
      simulationNotice: registeredRecord.simulationNotice,
    };
  }
}
