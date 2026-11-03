import {
  BorderCrossingComparison,
  BorderCrossingObservation,
  ComparisonStatement,
  OFFICIAL_CAMERA_METADATA,
} from '../domain/border-crossing.js';
import {
  BORDER_CONCIERGE_PROFILE,
  classifyConciergeIntent,
  ConciergeIntent,
  ConciergeProfile,
  formatDeterministicConciergeResponse,
  isFactualBorderIntent,
  validateConciergeFactualIntegrity,
} from '../domain/border-concierge.js';
import { detectLanguage, OFFICIAL_DATA_NOTICE } from '../domain/synthetic-witness.js';
import { MetaModelPort } from '../ports/meta-model-port.js';
import { GetBorderWaitTimesUseCase } from './get-border-wait-times.js';

export class InvalidConciergeMessageError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'InvalidConciergeMessageError';
  }
}

export interface AskConciergeInput {
  readonly message: unknown;
  readonly language?: 'en' | 'es';
}

export interface AskConciergeResult {
  readonly concierge: ConciergeProfile;
  readonly userQuestion: string;
  readonly language: 'en' | 'es';
  readonly intent: ConciergeIntent;
  readonly text: string;
  readonly provider: 'meta-muse-spark-1.3' | 'deterministic-fallback' | 'mock-meta';
  readonly authoritativeObservations: {
    readonly pasoDelNorte: BorderCrossingObservation;
    readonly bridgeOfTheAmericas: BorderCrossingObservation;
  };
  readonly comparison: BorderCrossingComparison['comparison'];
  readonly cameras: {
    readonly officialAuthorityUrl: string;
    readonly secondaryAuthorityUrl: string;
    readonly provider: string;
    readonly description: ComparisonStatement;
  };
  readonly fetchedAt: string;
  readonly officialDataNotice: ComparisonStatement;
}

export interface AskBorderConciergeDependencies {
  readonly getBorderWaitTimesUseCase: GetBorderWaitTimesUseCase;
  readonly metaModel?: MetaModelPort;
}

/**
 * Use Case: Grounded Border Concierge traveler guidance.
 *
 * Coordinates retrieval of official CBP wait observations and comparison data,
 * classifies user inquiries into discrete orientation intents, and formulates
 * accurate, grounded responses without giving the model authority over numbers,
 * operational status, or camera inspection claims.
 */
export class AskBorderConciergeUseCase {
  private readonly getBorderWaitTimesUseCase: GetBorderWaitTimesUseCase;
  private readonly metaModel?: MetaModelPort;

  constructor(dependencies: AskBorderConciergeDependencies) {
    this.getBorderWaitTimesUseCase = dependencies.getBorderWaitTimesUseCase;
    this.metaModel = dependencies.metaModel;
  }

  /**
   * Retrieves public profile and suggested prompt chips for the Border Concierge.
   */
  public getProfile(): ConciergeProfile {
    return BORDER_CONCIERGE_PROFILE;
  }

  /**
   * Answers a traveler inquiry grounded strictly in verified CBP records.
   */
  public async execute(input: AskConciergeInput): Promise<AskConciergeResult> {
    if (typeof input.message !== 'string') {
      throw new InvalidConciergeMessageError('Message must be a string.');
    }

    const trimmed = input.message.trim();
    if (trimmed.length === 0) {
      throw new InvalidConciergeMessageError('Message cannot be empty.');
    }

    if (trimmed.length > 500) {
      throw new InvalidConciergeMessageError(
        'Message exceeds maximum permitted length of 500 characters.'
      );
    }

    const language: 'en' | 'es' =
      input.language === 'en' || input.language === 'es'
        ? input.language
        : detectLanguage(trimmed, 'en');

    // Reuse existing authoritative border intelligence use case (utilizing cache)
    const comparisonData = await this.getBorderWaitTimesUseCase.execute({ bypassCache: false });

    // Classify inquiry intent
    const intent = classifyConciergeIntent(trimmed);

    let text: string | null = null;
    let provider: 'meta-muse-spark-1.3' | 'deterministic-fallback' | 'mock-meta' =
      'deterministic-fallback';

    // Authoritative Deterministic Routing for Live CBP Measurements & Comparisons:
    // When an inquiry requests current wait times, comparative status, or observation freshness,
    // the authoritative BorderCrossingComparison is the sole source of truth.
    // Routing is handled deterministically without delegating factual conclusions to model generation.
    if (isFactualBorderIntent(intent)) {
      text = formatDeterministicConciergeResponse(intent, comparisonData, language);
      provider = 'deterministic-fallback';
    } else if (this.metaModel && typeof this.metaModel.generateConciergeResponse === 'function') {
      try {
        const modelRes = await this.metaModel.generateConciergeResponse({
          userMessage: trimmed,
          intent,
          comparison: comparisonData,
          language,
        });

        if (modelRes && typeof modelRes.text === 'string' && modelRes.text.trim().length > 0) {
          const candidateText = modelRes.text.trim();
          if (validateConciergeFactualIntegrity(candidateText, comparisonData, intent)) {
            text = candidateText;
            provider =
              (this.metaModel as any).mode === 'local-mock'
                ? 'mock-meta'
                : 'meta-muse-spark-1.3';
          } else {
            // Model introduced ungrounded numbers or unsupported comparative claims! Fall back to deterministic engine.
            text = null;
          }
        }
      } catch {
        // Fall back gracefully to deterministic domain generator on model error/timeout
        text = null;
      }
    }

    if (!text) {
      text = formatDeterministicConciergeResponse(intent, comparisonData, language);
      provider = 'deterministic-fallback';
    }

    return {
      concierge: BORDER_CONCIERGE_PROFILE,
      userQuestion: trimmed,
      language,
      intent,
      text,
      provider,
      authoritativeObservations: comparisonData.observations,
      comparison: comparisonData.comparison,
      cameras: OFFICIAL_CAMERA_METADATA,
      fetchedAt: comparisonData.fetchedAt,
      officialDataNotice: OFFICIAL_DATA_NOTICE,
    };
  }
}
