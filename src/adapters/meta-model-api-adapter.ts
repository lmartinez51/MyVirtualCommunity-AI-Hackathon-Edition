import { MetaConfig } from '../config/index.js';
import {
  AssistantPromptContext,
  MetaModelPort,
  MetaModelResponse,
  MetaModelUsage,
  MetaModelUsageDetails,
} from '../ports/meta-model-port.js';

export class MetaApiError extends Error {
  public readonly statusCode?: number;
  public readonly code?: string;

  constructor(message: string, statusCode?: number, code?: string) {
    super(message);
    this.name = 'MetaApiError';
    this.statusCode = statusCode;
    this.code = code;
  }
}

export interface MetaModelApiAdapterOptions {
  readonly config: MetaConfig;
  readonly fetchFn?: typeof fetch;
  readonly timeoutMs?: number;
}

/**
 * Real Meta Model API Adapter communicating with the Meta Model API
 * (https://api.meta.ai/v1) using native fetch.
 *
 * Supported official endpoints:
 * - POST /chat/completions (OpenAI-compatible chat completion)
 * - GET /models (listing available models)
 *
 * Requirements:
 * - Uses configured model (defaults to muse-spark-1.3).
 * - Standardized on MODEL_API_KEY.
 * - Never prints or leaks the API key in logs, headers, or errors.
 * - Rigorous response validation: validates choices structure and content presence.
 * - Supports injection of fetchFn for 100% offline automated testing.
 * - Zero external dependencies.
 */
export class MetaModelApiAdapter implements MetaModelPort {
  private readonly config: MetaConfig;
  private readonly fetchFn: typeof fetch;
  private readonly timeoutMs: number;

  constructor(options: MetaModelApiAdapterOptions) {
    this.config = options.config;
    this.fetchFn = options.fetchFn ?? globalThis.fetch;
    this.timeoutMs = options.timeoutMs ?? 30000;
  }

  /**
   * Sanitizes any text to prevent accidental leakage of the configured API key or authorization headers.
   * Completely redacts credentials without printing any prefixes, suffixes, or fragments.
   */
  public sanitize(text: string): string {
    let sanitized = text;
    if (this.config.apiKey && this.config.apiKey.length > 0) {
      sanitized = sanitized.replaceAll(this.config.apiKey, '[REDACTED]');
    }
    sanitized = sanitized.replaceAll(/Bearer\s+[A-Za-z0-9_.-]+/gi, 'Bearer [REDACTED]');
    return sanitized;
  }

  /**
   * Queries the GET /models endpoint to retrieve the list of available model identifiers.
   * Useful for preliminary connectivity and availability checks.
   */
  public async listModels(): Promise<string[]> {
    if (!this.config.apiKey || this.config.apiKey.trim() === '') {
      throw new MetaApiError(
        'MODEL_API_KEY is not configured. Set the MODEL_API_KEY environment variable to query the Meta Model API.'
      );
    }

    const endpoint = `${this.config.baseURL}/models`;
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), this.timeoutMs);

    let response: Response;
    try {
      response = await this.fetchFn(endpoint, {
        method: 'GET',
        headers: {
          Authorization: `Bearer ${this.config.apiKey}`,
        },
        signal: controller.signal,
      });
    } catch (err) {
      clearTimeout(timer);
      const isAbort = err instanceof Error && err.name === 'AbortError';
      const msg = isAbort
        ? `Meta Model API request timed out after ${this.timeoutMs}ms`
        : `Network error connecting to Meta Model API at ${this.config.baseURL}: ${err instanceof Error ? err.message : String(err)}`;
      throw new MetaApiError(this.sanitize(msg));
    } finally {
      clearTimeout(timer);
    }

    let responseData: any;
    try {
      responseData = await response.json();
    } catch {
      throw new MetaApiError(
        `Meta Model API returned non-JSON response from /models with HTTP status ${response.status}`,
        response.status
      );
    }

    if (!response.ok) {
      const errorDetail =
        typeof responseData?.error?.message === 'string'
          ? responseData.error.message
          : typeof responseData?.error === 'string'
          ? responseData.error
          : response.statusText;
      const sanitized = this.sanitize(errorDetail);

      if (response.status === 401) {
        throw new MetaApiError(
          `Meta Model API authentication failed (401 Unauthorized): ${sanitized}`,
          401,
          'UNAUTHORIZED'
        );
      }
      throw new MetaApiError(
        `Meta Model API /models error (${response.status}): ${sanitized}`,
        response.status,
        responseData?.error?.code
      );
    }

    const rawList = Array.isArray(responseData?.data)
      ? responseData.data
      : Array.isArray(responseData?.models)
      ? responseData.models
      : Array.isArray(responseData)
      ? responseData
      : [];

    return rawList
      .map((item: any) => {
        if (typeof item === 'string') return item;
        if (item && typeof item.id === 'string') return item.id;
        return null;
      })
      .filter((id: string | null): id is string => Boolean(id));
  }

  /**
   * Internal shared dispatcher for POST /chat/completions.
   */
  private async executeChatCompletion(
    messages: Array<{ role: string; content: string }>,
    options?: { temperature?: number; reasoningEffort?: 'minimal' | 'low' | 'medium' | 'high' }
  ): Promise<MetaModelResponse> {
    if (!this.config.apiKey || this.config.apiKey.trim() === '') {
      throw new MetaApiError(
        'MODEL_API_KEY is not configured. Set the MODEL_API_KEY environment variable to use the live Meta Model API.'
      );
    }

    const endpoint = `${this.config.baseURL}/chat/completions`;

    const requestBody: Record<string, unknown> = {
      model: this.config.model,
      messages,
      temperature: options?.temperature ?? 0.2,
    };

    if (options?.reasoningEffort) {
      requestBody.reasoning_effort = options.reasoningEffort;
    }

    let response: Response;
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), this.timeoutMs);

    try {
      response = await this.fetchFn(endpoint, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${this.config.apiKey}`,
        },
        body: JSON.stringify(requestBody),
        signal: controller.signal,
      });
    } catch (err) {
      clearTimeout(timer);
      const isAbort = err instanceof Error && err.name === 'AbortError';
      const msg = isAbort
        ? `Meta Model API request timed out after ${this.timeoutMs}ms`
        : `Network error connecting to Meta Model API at ${this.config.baseURL}: ${err instanceof Error ? err.message : String(err)}`;
      throw new MetaApiError(this.sanitize(msg));
    } finally {
      clearTimeout(timer);
    }

    let responseData: any;
    try {
      responseData = await response.json();
    } catch {
      throw new MetaApiError(
        `Meta Model API returned non-JSON response with HTTP status ${response.status}`,
        response.status
      );
    }

    if (!response.ok) {
      const errorDetail =
        typeof responseData?.error?.message === 'string'
          ? responseData.error.message
          : typeof responseData?.error === 'string'
          ? responseData.error
          : response.statusText;
      const sanitizedDetail = this.sanitize(errorDetail);

      if (response.status === 401) {
        throw new MetaApiError(
          `Meta Model API authentication failed (401 Unauthorized): ${sanitizedDetail}`,
          401,
          'UNAUTHORIZED'
        );
      }
      if (response.status === 429) {
        throw new MetaApiError(
          `Meta Model API rate limit exceeded (429 Too Many Requests): ${sanitizedDetail}`,
          429,
          'RATE_LIMITED'
        );
      }
      throw new MetaApiError(
        `Meta Model API error (${response.status}): ${sanitizedDetail}`,
        response.status,
        responseData?.error?.code
      );
    }

    // Strict structural response validation
    if (!responseData || typeof responseData !== 'object') {
      throw new MetaApiError('Meta Model API returned an empty or invalid response payload');
    }

    if (!Array.isArray(responseData.choices) || responseData.choices.length === 0) {
      throw new MetaApiError(
        'Meta Model API response malformed: "choices" array is missing or empty'
      );
    }

    const choice = responseData.choices[0];
    if (!choice || typeof choice !== 'object') {
      throw new MetaApiError('Meta Model API response malformed: choices[0] is not an object');
    }

    const text = choice.message?.content;
    if (typeof text !== 'string') {
      throw new MetaApiError(
        'Meta Model API response malformed: choices[0].message.content is missing or not a string'
      );
    }

    let usage: MetaModelUsage | undefined = undefined;
    if (responseData?.usage && typeof responseData.usage === 'object') {
      const rawUsage = responseData.usage as Record<string, unknown>;
      const rawDetails =
        rawUsage.completion_tokens_details && typeof rawUsage.completion_tokens_details === 'object'
          ? (rawUsage.completion_tokens_details as Record<string, unknown>)
          : undefined;

      const reasoningTokens =
        typeof rawDetails?.reasoning_tokens === 'number'
          ? rawDetails.reasoning_tokens
          : typeof rawUsage.reasoning_tokens === 'number'
          ? rawUsage.reasoning_tokens
          : undefined;

      const completionTokensDetails: MetaModelUsageDetails | undefined =
        rawDetails !== undefined
          ? {
              reasoningTokens,
              acceptedPredictionTokens:
                typeof rawDetails.accepted_prediction_tokens === 'number'
                  ? rawDetails.accepted_prediction_tokens
                  : undefined,
              rejectedPredictionTokens:
                typeof rawDetails.rejected_prediction_tokens === 'number'
                  ? rawDetails.rejected_prediction_tokens
                  : undefined,
            }
          : reasoningTokens !== undefined
          ? { reasoningTokens }
          : undefined;

      usage = {
        promptTokens:
          typeof rawUsage.prompt_tokens === 'number' ? rawUsage.prompt_tokens : undefined,
        completionTokens:
          typeof rawUsage.completion_tokens === 'number' ? rawUsage.completion_tokens : undefined,
        totalTokens:
          typeof rawUsage.total_tokens === 'number' ? rawUsage.total_tokens : undefined,
        reasoningTokens,
        completionTokensDetails,
      };
    }

    return {
      text,
      model: responseData.model || this.config.model,
      usage,
      rawResponse: responseData,
    };
  }

  /**
   * Executes a synthetic, controlled single-prompt completion without world-core context.
   * Ideal for minimal smoke tests and diagnostic handshakes.
   */
  public async generateSimpleCompletion(
    instruction: string,
    options?: { temperature?: number; reasoningEffort?: 'minimal' | 'low' | 'medium' | 'high' }
  ): Promise<MetaModelResponse> {
    return this.executeChatCompletion(
      [{ role: 'user', content: instruction }],
      options
    );
  }

  /**
   * Generates a grounded assistant response based on verified domain facts.
   * Configures reasoning_effort: "minimal" to optimize token efficiency for local business queries.
   */
  public async generateAssistantResponse(
    context: AssistantPromptContext
  ): Promise<MetaModelResponse> {
    const defaultSystemPrompt =
      'You are a courteous, concise local-business assistant for the Ciudad Juárez–El Paso region. ' +
      'You answer customer questions about business hours and operational availability. ' +
      'CRITICAL RULE: You must base your answer strictly and authoritatively on the VERIFIED DATA provided below. ' +
      'Do not guess, assume, or contradict the verified operational status or schedule.';

    const systemPrompt = context.systemPrompt ?? defaultSystemPrompt;

    const groundedUserContent = [
      `USER INQUIRY: "${context.userQuery}"`,
      '',
      '--- VERIFIED OPERATIONAL DATA (AUTHORITATIVE) ---',
      `Business: ${context.verifiedData.businessName} (ID: ${context.verifiedData.businessId})`,
      `Current Status: ${context.verifiedData.status}`,
      `Is Open: ${context.verifiedData.isOpen ? 'YES' : 'NO'}`,
      `New Admissions Permitted: ${context.verifiedData.admissionPermitted ? 'YES' : 'NO'}`,
      `Evaluated Time (Local): ${context.verifiedData.civilTime.isoCivilString} (${context.verifiedData.timeZone})`,
      `Scheduled Hours Today: ${context.verifiedData.openTimeCivil} to ${context.verifiedData.closeTimeCivil}`,
      `Last Entry Cutoff: ${context.verifiedData.cutoffTimeCivil}`,
      `Operating Days: ${context.schedule.operatingDays.map((d) => ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'][d]).join(', ')}`,
      `Official Status Message: ${context.verifiedData.summaryMessage}`,
      '------------------------------------------------',
      '',
      'Please formulate a helpful, courteous response for the user explaining whether the business is open, what the hours are, and any entry restrictions.',
    ].join('\n');

    return this.executeChatCompletion(
      [
        { role: 'system', content: systemPrompt },
        { role: 'user', content: groundedUserContent },
      ],
      { reasoningEffort: 'minimal' }
    );
  }
}
