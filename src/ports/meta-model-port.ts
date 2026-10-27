import { BusinessSchedule } from '../domain/business-schedule.js';
import { BusinessOperationalStatus } from '../domain/operational-status.js';

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
}
