import {
  BilingualText,
  classifyWitnessSafetyIntent,
  detectLanguage,
  generateDeterministicFallbackResponse,
  getDeterministicBoundaryResponse,
  OFFICIAL_DATA_NOTICE,
  SYNTHETIC_DISCLOSURE,
  SYNTHETIC_MARKER,
  SYNTHETIC_WITNESS_ROSTER,
  SyntheticProvenance,
  SyntheticWitnessId,
  SyntheticWitnessProfile,
  validateWitnessMessage,
} from '../domain/synthetic-witness.js';
import { MetaModelPort } from '../ports/meta-model-port.js';

export class UnknownWitnessError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'UnknownWitnessError';
  }
}

export class InvalidWitnessMessageError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'InvalidWitnessMessageError';
  }
}

export interface TalkToWitnessInput {
  readonly npcId: string;
  readonly message: string;
  readonly language?: 'en' | 'es';
}

export interface TalkToWitnessResult {
  readonly witnessId: SyntheticWitnessId;
  readonly witnessName: string;
  readonly avatar: string;
  readonly role: BilingualText;
  readonly text: string;
  readonly language: 'en' | 'es';
  readonly provenance: SyntheticProvenance;
}

export interface PublicWitnessProfile {
  readonly id: SyntheticWitnessId;
  readonly name: string;
  readonly avatar: string;
  readonly age: number;
  readonly role: BilingualText;
  readonly background: BilingualText;
  readonly crossingReason: BilingualText;
  readonly communicationStyle: BilingualText;
  readonly syntheticMarker: BilingualText;
  readonly personalWaitMinutes: number;
  readonly suggestedPrompts: Array<{
    readonly id: string;
    readonly label: BilingualText;
    readonly prompt: BilingualText;
  }>;
}

export interface WitnessRosterResult {
  readonly locationId: string;
  readonly locationName: BilingualText;
  readonly syntheticDisclosure: BilingualText;
  readonly witnesses: PublicWitnessProfile[];
}

export interface TalkToSyntheticWitnessOptions {
  readonly metaModel?: MetaModelPort;
}

/**
 * Use case orchestrating interactions with synthetic community witnesses
 * in the simulated pedestrian queue at Paso del Norte.
 *
 * Enforces truth and provenance invariants:
 * - Always marks responses with synthetic simulation provenance.
 * - Confines knowledge strictly to the character's scenario profile.
 * - Gracefully falls back to deterministic scenario responses on model failure.
 * - Never consumes paid API credits when offline or during tests.
 */
export class TalkToSyntheticWitnessUseCase {
  private readonly metaModel?: MetaModelPort;

  constructor(options: TalkToSyntheticWitnessOptions = {}) {
    this.metaModel = options.metaModel;
  }

  /**
   * Retrieves the configured synthetic NPC roster.
   */
  public getRoster(): WitnessRosterResult {
    const witnesses: PublicWitnessProfile[] = Object.values(SYNTHETIC_WITNESS_ROSTER).map(
      (w: SyntheticWitnessProfile) => ({
        id: w.id,
        name: w.name,
        avatar: w.avatar,
        age: w.age,
        role: w.role,
        background: w.background,
        crossingReason: w.crossingReason,
        communicationStyle: w.communicationStyle,
        syntheticMarker: SYNTHETIC_MARKER,
        personalWaitMinutes: w.permittedKnowledge.personalWaitMinutes,
        suggestedPrompts: w.suggestedPrompts,
      })
    );

    return {
      locationId: 'infrastructure:puente-paso-del-norte',
      locationName: {
        en: 'Paso del Norte / Santa Fe International Bridge',
        es: 'Puente Internacional Paso del Norte / Santa Fe',
      },
      syntheticDisclosure: SYNTHETIC_DISCLOSURE,
      witnesses,
    };
  }

  /**
   * Conducts a single-turn conversation with a synthetic NPC witness.
   */
  public async execute(input: TalkToWitnessInput): Promise<TalkToWitnessResult> {
    const validation = validateWitnessMessage(input.message);
    if (!validation.valid || !validation.trimmed) {
      throw new InvalidWitnessMessageError(validation.error || 'Invalid message');
    }
    const cleanMessage = validation.trimmed;

    const witness = SYNTHETIC_WITNESS_ROSTER[input.npcId as SyntheticWitnessId];
    if (!witness) {
      throw new UnknownWitnessError(
        `Unknown synthetic witness "${input.npcId}". Valid IDs: ${Object.keys(SYNTHETIC_WITNESS_ROSTER).join(', ')}`
      );
    }

    const language: 'en' | 'es' =
      input.language === 'en' || input.language === 'es'
        ? input.language
        : detectLanguage(cleanMessage, 'en');

    let responseText: string | null = null;
    let provider: 'meta-muse-spark-1.3' | 'deterministic-fallback' | 'mock-meta' =
      'deterministic-fallback';

    // Application-level safety boundary:
    // Intercept live border inquiries, bridge comparisons, synthetic identity questions,
    // and out-of-scope topics deterministically before invoking any model adapter.
    const safetyCheck = classifyWitnessSafetyIntent(cleanMessage);
    if (safetyCheck.isSafetyBoundary && safetyCheck.category) {
      responseText = getDeterministicBoundaryResponse(witness, safetyCheck.category, language);
      provider = 'deterministic-fallback';
    } else if (this.metaModel && typeof this.metaModel.generateWitnessResponse === 'function') {
      try {
        const modelRes = await this.metaModel.generateWitnessResponse({
          witness,
          userMessage: cleanMessage,
          language,
        });

        if (modelRes && typeof modelRes.text === 'string' && modelRes.text.trim().length > 0) {
          responseText = modelRes.text.trim();
          provider =
            (this.metaModel as any).mode === 'local-mock'
              ? 'mock-meta'
              : 'meta-muse-spark-1.3';
        }
      } catch {
        // Fallback gracefully on any model error, network drop, or timeout
        responseText = null;
      }
    }

    if (!responseText) {
      responseText = generateDeterministicFallbackResponse(witness, cleanMessage, language);
      provider = 'deterministic-fallback';
    }

    return {
      witnessId: witness.id,
      witnessName: witness.name,
      avatar: witness.avatar,
      role: witness.role,
      text: responseText,
      language,
      provenance: {
        type: 'synthetic-witness-simulation',
        marker: SYNTHETIC_MARKER,
        scenarioId: 'paso-del-norte-pedestrian-queue',
        locationId: 'infrastructure:puente-paso-del-norte',
        provider,
        officialDataNotice: OFFICIAL_DATA_NOTICE,
      },
    };
  }
}
