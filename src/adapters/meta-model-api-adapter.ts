import { MetaConfig } from '../config/index.js';
import sharp from 'sharp';
import { parseImageStream, recordsOfKind } from '@meta-sam/parser';
import { buildConciergePrompt } from '../domain/border-concierge.js';
import { buildWitnessPrompt } from '../domain/synthetic-witness.js';
import { buildBusinessAttendantPrompt } from '../domain/business-attendant.js';
import {
  BoundingBox,
  IssueCategory,
  MuseSparkInterpretation,
  SAM_MODEL_ID,
  SAM_VISUAL_ANALYSIS_DISCLAIMER_EN,
  SamAnalysisStatus,
  SamDetectedObject,
  SamSegmentationResult,
  normalizeIssueCategory,
  classifyCivicIssue,
  getSimulatedDepartment,
} from '../domain/citizen-report.js';
import { compositeSamMaskOverlay } from '../domain/sam-mask-overlay.js';
import {
  AssistantPromptContext,
  BusinessAttendantPromptContext,
  ConciergePromptContext,
  InterpretReportOptions,
  MetaModelPort,
  MetaModelResponse,
  MetaModelUsage,
  MetaModelUsageDetails,
  SamSegmentOptions,
  WitnessPromptContext,
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
  public readonly mode = 'live-api' as const;
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

  /**
   * Generates dialogue for a synthetic community witness grounded in their scenario profile.
   */
  public async generateWitnessResponse(
    context: WitnessPromptContext
  ): Promise<MetaModelResponse> {
    const { systemPrompt, userContent } = buildWitnessPrompt(
      context.witness,
      context.userMessage,
      context.language
    );

    return this.executeChatCompletion(
      [
        { role: 'system', content: systemPrompt },
        { role: 'user', content: userContent },
      ],
      { temperature: 0.7, reasoningEffort: 'minimal' }
    );
  }

  /**
   * Generates natural language guidance for the Border Concierge grounded in official CBP observations.
   */
  public async generateConciergeResponse(
    context: ConciergePromptContext
  ): Promise<MetaModelResponse> {
    const { systemPrompt, userContent } = buildConciergePrompt(
      context.userMessage,
      context.intent,
      context.comparison,
      context.language
    );

    return this.executeChatCompletion(
      [
        { role: 'system', content: systemPrompt },
        { role: 'user', content: userContent },
      ],
      { temperature: 0.3, reasoningEffort: 'minimal' }
    );
  }

  /**
   * Generates grounded dialogue for a storefront business attendant.
   */
  public async generateBusinessAttendantResponse(
    context: BusinessAttendantPromptContext
  ): Promise<MetaModelResponse> {
    const { systemPrompt, userContent } = buildBusinessAttendantPrompt(
      context.attendant,
      context.status,
      context.schedule,
      context.userMessage,
      context.language
    );

    return this.executeChatCompletion(
      [
        { role: 'system', content: systemPrompt },
        { role: 'user', content: userContent },
      ],
      { temperature: 0.4, reasoningEffort: 'minimal' }
    );
  }

  /**
   * Helper generator to extract parsed JSON events from an SSE ReadableStream.
   */
  private async *sseEventsFromStream(stream: ReadableStream<Uint8Array>): AsyncIterable<any> {
    const reader = stream.getReader();
    const decoder = new TextDecoder('utf-8');
    let buffer = '';

    try {
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split(/\r?\n/);
        buffer = lines.pop() ?? '';

        for (const line of lines) {
          const trimmed = line.trim();
          if (!trimmed || trimmed.startsWith(':')) continue;
          if (trimmed.startsWith('data:')) {
            const dataContent = trimmed.slice(5).trim();
            if (dataContent === '[DONE]') continue;
            try {
              const eventObj = JSON.parse(dataContent);
              if (eventObj && typeof eventObj === 'object' && typeof eventObj.type === 'string') {
                yield eventObj;
              }
            } catch {
              // Ignore invalid/non-JSON data lines
            }
          }
        }
      }

      if (buffer.trim()) {
        const trimmed = buffer.trim();
        if (trimmed.startsWith('data:')) {
          const dataContent = trimmed.slice(5).trim();
          if (dataContent !== '[DONE]') {
            try {
              const eventObj = JSON.parse(dataContent);
              if (eventObj && typeof eventObj === 'object' && typeof eventObj.type === 'string') {
                yield eventObj;
              }
            } catch {}
          }
        }
      }
    } finally {
      reader.releaseLock();
    }
  }

  /**
   * Performs visual segmentation on photographic evidence using SAM 3.1.
   */
  public async segmentImage(options: SamSegmentOptions): Promise<SamSegmentationResult> {
    if (!this.config.apiKey || this.config.apiKey.trim() === '') {
      throw new MetaApiError(
        'MODEL_API_KEY is not configured. Set the MODEL_API_KEY environment variable to use SAM 3.1.'
      );
    }

    const endpoint = `${this.config.baseURL}/responses`;
    const promptText = options.promptText ?? 'road pothole or damaged asphalt surface';
    const timeout = options.timeoutMs ?? this.timeoutMs;

    const imageUrl = options.imageBase64.startsWith('data:')
      ? options.imageBase64
      : `data:${options.mimeType};base64,${options.imageBase64}`;

    const requestBody = {
      model: SAM_MODEL_ID,
      stream: true,
      input: [
        {
          role: 'user',
          content: [
            {
              type: 'input_text',
              text: promptText,
            },
            {
              type: 'input_image',
              image_url: imageUrl,
            },
          ],
        },
      ],
      metadata: {
        include_confidence: 'true',
        mask_encoding: 'one_bit',
      },
    };

    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), timeout);

    let response: Response;
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
        ? `SAM 3.1 request timed out after ${timeout}ms`
        : `Network error connecting to SAM 3.1 API at ${endpoint}: ${err instanceof Error ? err.message : String(err)}`;
      throw new MetaApiError(this.sanitize(msg));
    } finally {
      clearTimeout(timer);
    }

    if (!response.ok) {
      let errorDetail = response.statusText;
      let errorCode: string | undefined;
      try {
        const errJson: any = await response.json();
        if (typeof errJson?.error?.message === 'string') {
          errorDetail = errJson.error.message;
        } else if (typeof errJson?.error === 'string') {
          errorDetail = errJson.error;
        }
        errorCode = errJson?.error?.code;
      } catch {
        try {
          const errText = await response.text();
          if (errText) errorDetail = errText;
        } catch {}
      }

      const sanitizedDetail = this.sanitize(errorDetail);
      if (response.status === 401) {
        throw new MetaApiError(
          `Meta Model API authentication failed (401 Unauthorized): ${sanitizedDetail}`,
          401,
          'UNAUTHORIZED'
        );
      }
      throw new MetaApiError(
        `SAM 3.1 API error (${response.status}): ${sanitizedDetail}`,
        response.status,
        errorCode
      );
    }

    const boxes: BoundingBox[] = [];
    let maskReference: string | null = null;
    let maskSummary: string | undefined = undefined;
    let maskDimensions: { width: number; height: number } | null = null;
    let objectId: string | null = null;
    let overallConfidence: number | null = null;
    const diagnosticsList: string[] = [];
    let overlayPngBase64: string | null = null;
    let detectedObjects: SamDetectedObject[] = [];
    let hasAuthenticMasks = false;

    // Resolve image dimensions for accurate coordinate scaling and overlay composition
    let imgWidth = options.imageDimensions?.width ?? 0;
    let imgHeight = options.imageDimensions?.height ?? 0;
    if (imgWidth <= 0 || imgHeight <= 0) {
      try {
        const meta = await sharp(Buffer.from(options.imageBase64, 'base64')).metadata();
        imgWidth = meta.width ?? 512;
        imgHeight = meta.height ?? 512;
      } catch {
        imgWidth = 512;
        imgHeight = 512;
      }
    }

    const contentType = response.headers.get('content-type') ?? '';

    // Handle streaming SSE response via @meta-sam/parser
    if (response.body && (contentType.includes('text/event-stream') || !contentType.includes('application/json'))) {
      try {
        const stream = parseImageStream(this.sseEventsFromStream(response.body));
        const result = await stream.finalResult;

        if (Array.isArray(result.diagnostics)) {
          for (const d of result.diagnostics) {
            diagnosticsList.push(`${d.severity}: ${d.message}`);
          }
        }

        const boxRecords = recordsOfKind(result.records, 'box');
        const maskRecords = recordsOfKind(result.records, 'mask');

        // Extract confidence if available
        for (const m of maskRecords) {
          const conf = typeof m.confidence === 'number' && !isNaN(m.confidence) ? m.confidence : undefined;
          if (conf !== undefined && overallConfidence === null) {
            overallConfidence = conf;
          }
        }
        for (const b of boxRecords) {
          const conf = typeof b.confidence === 'number' && !isNaN(b.confidence) ? b.confidence : undefined;
          if (conf !== undefined && overallConfidence === null) {
            overallConfidence = conf;
          }
        }

        if (maskRecords.length > 0 || boxRecords.length > 0) {
          try {
            const comp = await compositeSamMaskOverlay(
              imgWidth,
              imgHeight,
              maskRecords.map((m) => ({
                objectId: m.objectId,
                mask: m.mask,
                bounds: m.bounds,
                confidence: m.confidence,
                label: promptText,
              })),
              boxRecords.map((b) => ({
                objectId: b.objectId,
                left: b.left,
                top: b.top,
                right: b.right,
                bottom: b.bottom,
                confidence: b.confidence,
                label: promptText,
              })),
              promptText
            );

            overlayPngBase64 = comp.overlayPngBase64;
            detectedObjects = [...comp.objects];
            hasAuthenticMasks = comp.hasAuthenticMasks;

            for (const obj of detectedObjects) {
              boxes.push(obj.box);
            }

            if (detectedObjects.length > 0 && detectedObjects[0]) {
              const firstObj = detectedObjects[0];
              objectId = firstObj.objectId;
              maskDimensions = firstObj.maskDimensions ?? null;
              if (firstObj.confidence !== undefined && overallConfidence === null) {
                overallConfidence = firstObj.confidence;
              }
              const firstMask = maskRecords[0];
              maskReference = firstMask
                ? (firstMask.identity || `mask_${firstObj.objectId}`)
                : null;
            }

            if (hasAuthenticMasks) {
              maskSummary = `Segmented candidate region (${detectedObjects.length} mask(s) decoded via SAM 3.1)`;
            } else if (boxes.length > 0) {
              maskSummary = `Segmented ${boxes.length} candidate box region(s)`;
            }
          } catch (compErr) {
            diagnosticsList.push(
              `warning: Failed to composite mask overlay: ${
                compErr instanceof Error ? compErr.message : String(compErr)
              }`
            );
          }
        }
      } catch (parseErr) {
        diagnosticsList.push(
          `warning: SAM 3.1 stream parser encountered: ${
            parseErr instanceof Error ? parseErr.message : String(parseErr)
          }`
        );
      }
    } else {
      // Fallback for non-streaming mock/JSON responses
      let responseData: any;
      try {
        responseData = await response.json();
      } catch {
        responseData = null;
      }

      if (Array.isArray(responseData?.output?.boxes) || Array.isArray(responseData?.boxes)) {
        const rawBoxes = responseData?.output?.boxes ?? responseData?.boxes;
        for (const b of rawBoxes) {
          if (typeof b?.ymin === 'number' && typeof b?.xmin === 'number') {
            const conf = typeof b.confidence === 'number' && !isNaN(b.confidence) ? b.confidence : undefined;
            if (conf !== undefined && overallConfidence === null) {
              overallConfidence = conf;
            }
            boxes.push({
              ymin: b.ymin,
              xmin: b.xmin,
              ymax: b.ymax ?? b.ymin + 0.1,
              xmax: b.xmax ?? b.xmin + 0.1,
              label: b.label ?? promptText,
              confidence: conf,
            });
          }
        }
      }
    }

    const status: SamAnalysisStatus =
      boxes.length > 0 || maskReference !== null ? 'completed' : 'inconclusive';

    return {
      model: SAM_MODEL_ID,
      status,
      detectedConcept: promptText,
      boxes,
      maskReference,
      maskSummary:
        maskSummary ??
        (boxes.length > 0
          ? `Segmented ${boxes.length} candidate region(s)`
          : 'No distinct candidate region identified'),
      maskDimensions,
      objectId,
      confidence: overallConfidence,
      imageDimensions: { width: imgWidth, height: imgHeight },
      diagnostics: diagnosticsList.length > 0 ? diagnosticsList : undefined,
      analyzedAt: new Date().toISOString(),
      provider: 'sam-3.1-live-api',
      disclaimer: SAM_VISUAL_ANALYSIS_DISCLAIMER_EN,
      objects: detectedObjects.length > 0 ? detectedObjects : undefined,
      overlayPngBase64,
      hasAuthenticMasks,
    };
  }

  /**
   * Interprets and structures a citizen issue report using Muse Spark 1.3.
   */
  public async interpretCitizenReport(
    options: InterpretReportOptions
  ): Promise<MuseSparkInterpretation> {
    const isEs = options.language === 'es';
    const systemPrompt = isEs
      ? `Eres el sistema de interpretación y estructuración de reportes ciudadanos de MyVirtualCommunity para Ciudad Juárez.
Tu tarea es analizar la descripción provista por el ciudadano y estructurarla en un objeto JSON estricto.
CATEGORÍAS MUNICIPALES DISPONIBLES:
- road-surface-damage: baches, asfalto roto, zanjas viales.
- sidewalk-obstruction: banquetas rotas, obstáculos peatonales, rampas bloqueadas.
- cleaning-and-waste: basura acumulada, tiraderos clandestinos, escombros.
- water-and-drainage: fugas de agua, encharcamientos, drenaje pluvial tapado.
- public-lighting: luminarias apagadas, postes dañados, sectores oscuros.
- traffic-signage: semáforos descompuestos, señalización vial dañada o ausente.
- parks-and-public-spaces: mobiliario urbano, juegos infantiles, plazas públicas.
- trees-and-green-areas: ramas caídas, árboles secos o que obstruyen cables/vía.
- other: cualquier otra inquietud ciudadana legítima no catalogada (nunca es un error).

REGLAS OBLIGATORIAS:
- Clasifica con precisión en una de las categorías anteriores según la descripción ciudadana.
- NO inventes direcciones postales, calles inexistentes ni coordenadas GPS.
- NO certifiques ni inventes que el daño ha sido verificado físicamente por cuadrillas ni ingenieros.
- NO pretendas que una autoridad gubernamental real recibió el reporte.
- El departamento destino sugerido debe corresponder a la dependencia municipal simulada adecuada.
- Si faltan detalles importantes (ej. sentido de circulación, referencias cruzadas), inclúyelos en "missingInformation".
- Devuelve ÚNICAMENTE un objeto JSON válido con los campos: "summary", "category", "locationDescription", "additionalDetails", "missingInformation", "suggestedDepartment", "clarificationQuestion".`
      : `You are the citizen issue report interpretation assistant for MyVirtualCommunity in Ciudad Juárez.
Your task is to analyze the citizen's description and format a concise structured report as a strict JSON object.
AVAILABLE MUNICIPAL CATEGORIES:
- road-surface-damage: potholes, broken pavement, road depressions.
- sidewalk-obstruction: broken sidewalks, accessibility obstructions, blocked pedestrian ramps.
- cleaning-and-waste: accumulated garbage, illegal dumping, litter, debris.
- water-and-drainage: water leaks, standing water, clogged storm drainage.
- public-lighting: broken streetlights, dark zones, exposed electrical lines.
- traffic-signage: broken traffic lights, damaged or missing road signs.
- parks-and-public-spaces: public park benches, playground equipment, plazas.
- trees-and-green-areas: fallen tree branches, dead trees obstructing sidewalks/cables.
- other: other legitimate non-emergency civic concerns (never an error).

MANDATORY CONSTRAINTS:
- Accurately categorize into one of the above municipal categories based on the citizen description.
- DO NOT invent street addresses, non-existent streets, or GPS coordinates.
- DO NOT certify or claim damage severity has been physically verified by engineers or repair crews.
- DO NOT claim a real municipal government authority received this report.
- The suggested department destination must match the appropriate simulated municipal agency.
- If critical details are missing, list them in "missingInformation".
- Output ONLY a valid JSON object with fields: "summary", "category", "locationDescription", "additionalDetails", "missingInformation", "suggestedDepartment", "clarificationQuestion".`;

    const userMessage = `Citizen description: "${options.citizenDescription}"
Reported location: "${options.locationDescription ?? 'Avenida Benito Juárez corridor'}"
Language: "${options.language ?? 'en'}"
Visual analysis status: "${options.visualAnalysis?.status ?? 'none'}"`;

    const modelRes = await this.executeChatCompletion(
      [
        { role: 'system', content: systemPrompt },
        { role: 'user', content: userMessage },
      ],
      { temperature: 0.2, reasoningEffort: 'minimal' }
    );

    let parsed: any = null;
    try {
      let cleanText = modelRes.text.trim();
      if (cleanText.startsWith('```')) {
        cleanText = cleanText.replace(/^```(?:json)?\s*/i, '').replace(/\s*```$/i, '');
      }
      parsed = JSON.parse(cleanText);
    } catch {
      parsed = null;
    }

    let category: IssueCategory;
    if (parsed?.category) {
      category = normalizeIssueCategory(parsed.category);
    } else {
      category = classifyCivicIssue(options.citizenDescription).category;
    }

    const suggestedDepartment =
      typeof parsed?.suggestedDepartment === 'string' && parsed.suggestedDepartment.trim().length > 0
        ? parsed.suggestedDepartment.trim()
        : getSimulatedDepartment(category, isEs ? 'es' : 'en');

    return {
      model: this.config.model,
      summary:
        typeof parsed?.summary === 'string' && parsed.summary.trim().length > 0
          ? parsed.summary.trim()
          : isEs
          ? `Reporte de incidencia: ${options.citizenDescription}`
          : `Citizen issue report: ${options.citizenDescription}`,
      category,
      locationDescription:
        typeof parsed?.locationDescription === 'string' &&
        parsed.locationDescription.trim().length > 0
          ? parsed.locationDescription.trim()
          : options.locationDescription ??
            (isEs
              ? 'Avenida Benito Juárez (sector virtual)'
              : 'Avenida Benito Juárez (virtual sector)'),
      additionalDetails: Array.isArray(parsed?.additionalDetails)
        ? parsed.additionalDetails.map((d: any) => String(d).trim()).filter(Boolean)
        : [isEs ? 'Reporte registrado por ciudadano' : 'Report logged by citizen'],
      missingInformation: Array.isArray(parsed?.missingInformation)
        ? parsed.missingInformation.map((d: any) => String(d).trim()).filter(Boolean)
        : [],
      suggestedDepartment,
      clarificationQuestion:
        typeof parsed?.clarificationQuestion === 'string' &&
        parsed.clarificationQuestion.trim().length > 0
          ? parsed.clarificationQuestion.trim()
          : null,
      interpretedAt: new Date().toISOString(),
      provider: 'muse-spark-1.3-live-api',
    };
  }
}
