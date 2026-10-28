import { BorderCrossingComparison } from '../domain/border-crossing.js';
import { ConciergeIntent } from '../domain/border-concierge.js';
import { BusinessSchedule } from '../domain/business-schedule.js';
import { BusinessOperationalStatus } from '../domain/operational-status.js';
import { SyntheticWitnessProfile } from '../domain/synthetic-witness.js';

/**
 * Context provided to the Meta Model for grounded, hallucination-free generation.
 *
 * Invariant:
 * The prompt context contains verified, authoritative operational facts.
 * The model's role is to formulate a courteous, user-friendly natural language
 * explanation grounded strictly in these verified facts.
 */
export interface AssistantPromptContext {
  readonly userQuery: string;
  readonly verifiedData: BusinessOperationalStatus;
  readonly schedule: BusinessSchedule;
  readonly systemPrompt?: string;
}

/**
 * Context provided to the Meta Model for synthetic witness NPC conversations.
 *
 * Invariant:
 * The model is bounded strictly to the character's scenario profile and permitted knowledge.
 * The model has zero authority over world state, business schedules, or official border metrics.
 */
export interface WitnessPromptContext {
  readonly witness: SyntheticWitnessProfile;
  readonly userMessage: string;
  readonly language: 'en' | 'es';
}

/**
 * Context provided to the Meta Model for Border Concierge guidance.
 *
 * Invariant:
 * The prompt context contains verified, authoritative CBP observation facts and comparison data.
 * The model has zero authority to invent numbers, change status, or pretend to inspect video feeds.
 */
export interface ConciergePromptContext {
  readonly userMessage: string;
  readonly intent: ConciergeIntent;
  readonly comparison: BorderCrossingComparison;
  readonly language: 'en' | 'es';
}

export interface MetaModelUsageDetails {
  readonly reasoningTokens?: number;
  readonly acceptedPredictionTokens?: number;
  readonly rejectedPredictionTokens?: number;
}

export interface MetaModelUsage {
  readonly promptTokens?: number;
  readonly completionTokens?: number;
  readonly totalTokens?: number;
  readonly reasoningTokens?: number;
  readonly completionTokensDetails?: MetaModelUsageDetails;
}

export interface MetaModelResponse {
  readonly text: string;
  readonly model: string;
  readonly usage?: MetaModelUsage;
  readonly rawResponse?: unknown;
}

/**
 * Port contract for Meta Model API integrations.
 * Can be implemented by real Meta Model API (muse-spark-1.3) or test mock adapters.
 */
export interface MetaModelPort {
  /**
   * Generates a natural language response grounded in verified domain facts.
   */
  generateAssistantResponse(context: AssistantPromptContext): Promise<MetaModelResponse>;

  /**
   * Generates dialogue for a synthetic community witness grounded in their scenario profile.
   */
  generateWitnessResponse?(context: WitnessPromptContext): Promise<MetaModelResponse>;

  /**
   * Generates natural language guidance for the Border Concierge grounded in official CBP observations.
   */
  generateConciergeResponse?(context: ConciergePromptContext): Promise<MetaModelResponse>;
}
