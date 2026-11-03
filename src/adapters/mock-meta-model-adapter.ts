import { formatDeterministicConciergeResponse } from '../domain/border-concierge.js';
import { generateDeterministicFallbackResponse } from '../domain/synthetic-witness.js';
import {
  classifyAttendantInquiry,
  getDeterministicAttendantResponse,
} from '../domain/business-attendant.js';
import {
  assessNonEmergencyBoundary,
  classifyCivicIssue,
  getDefaultSamTarget,
  getSimulatedDepartment,
  MuseSparkInterpretation,
  SAM_VISUAL_ANALYSIS_DISCLAIMER_EN,
  SamSegmentationResult,
} from '../domain/citizen-report.js';
import { compositeSamMaskOverlay } from '../domain/sam-mask-overlay.js';
import {
  AssistantPromptContext,
  BusinessAttendantPromptContext,
  ConciergePromptContext,
  InterpretReportOptions,
  MetaModelPort,
  MetaModelResponse,
  SamSegmentOptions,
  WitnessPromptContext,
} from '../ports/meta-model-port.js';

export interface MockMetaModelOptions {
  readonly modelName?: string;
  readonly shouldFail?: boolean;
  readonly failureError?: Error;
  readonly customResponse?: (context: AssistantPromptContext) => string;
  readonly customWitnessResponse?: (context: WitnessPromptContext) => string;
  readonly customConciergeResponse?: (context: ConciergePromptContext) => string;
  readonly customAttendantResponse?: (context: BusinessAttendantPromptContext) => string;
  readonly customSegmentResponse?: (options: SamSegmentOptions) => SamSegmentationResult;
  readonly customInterpretResponse?: (options: InterpretReportOptions) => MuseSparkInterpretation;
  readonly samShouldFail?: boolean;
  readonly museShouldFail?: boolean;
  readonly samNoMatch?: boolean;
  readonly samBoxesOnly?: boolean;
}

/**
 * High-fidelity Mock Meta Model Adapter for automated testing and offline development.
 *
 * Requirements:
 * - Requires ZERO internet access and ZERO Meta credits.
 * - Formulates deterministic, grounded responses matching the verified operational status.
 * - Allows testing simulated failures and response overrides.
 */
export class MockMetaModelAdapter implements MetaModelPort {
  public readonly mode = 'local-mock' as const;
  private readonly modelName: string;
  private shouldFail: boolean;
  private failureError: Error | null;
  private customResponse?: (context: AssistantPromptContext) => string;
  private customWitnessResponse?: (context: WitnessPromptContext) => string;
  private customConciergeResponse?: (context: ConciergePromptContext) => string;
  private customAttendantResponse?: (context: BusinessAttendantPromptContext) => string;
  private customSegmentResponse?: (options: SamSegmentOptions) => SamSegmentationResult;
  private customInterpretResponse?: (options: InterpretReportOptions) => MuseSparkInterpretation;
  public samShouldFail: boolean = false;
  public museShouldFail: boolean = false;
  public samNoMatch: boolean = false;
  public samBoxesOnly: boolean = false;
  public lastContextReceived: AssistantPromptContext | null = null;
  public lastWitnessContextReceived: WitnessPromptContext | null = null;
  public lastConciergeContextReceived: ConciergePromptContext | null = null;
  public lastAttendantContextReceived: BusinessAttendantPromptContext | null = null;
  public lastSegmentOptionsReceived: SamSegmentOptions | null = null;
  public lastInterpretOptionsReceived: InterpretReportOptions | null = null;
  public callCount: number = 0;
  public samCallCount: number = 0;
  public museCallCount: number = 0;

  constructor(options: MockMetaModelOptions = {}) {
    this.modelName = options.modelName ?? 'muse-spark-1.3-mock';
    this.shouldFail = options.shouldFail ?? false;
    this.failureError = options.failureError ?? null;
    this.customResponse = options.customResponse;
    this.customWitnessResponse = options.customWitnessResponse;
    this.customConciergeResponse = options.customConciergeResponse;
    this.customAttendantResponse = options.customAttendantResponse;
    this.customSegmentResponse = options.customSegmentResponse;
    this.customInterpretResponse = options.customInterpretResponse;
    this.samShouldFail = options.samShouldFail ?? false;
    this.museShouldFail = options.museShouldFail ?? false;
    this.samNoMatch = options.samNoMatch ?? false;
    this.samBoxesOnly = options.samBoxesOnly ?? false;
  }

  public setCustomSegmentResponse(fn: (options: SamSegmentOptions) => SamSegmentationResult): void {
    this.customSegmentResponse = fn;
  }

  public setCustomInterpretResponse(fn: (options: InterpretReportOptions) => MuseSparkInterpretation): void {
    this.customInterpretResponse = fn;
  }

  public setShouldFail(shouldFail: boolean, error?: Error): void {
    this.shouldFail = shouldFail;
    this.failureError = error ?? new Error('Simulated Meta Model API failure');
  }

  public setCustomResponse(fn?: (context: AssistantPromptContext) => string): void {
    this.customResponse = fn;
  }

  public setCustomWitnessResponse(fn?: (context: WitnessPromptContext) => string): void {
    this.customWitnessResponse = fn;
  }

  public setCustomAttendantResponse(fn?: (context: BusinessAttendantPromptContext) => string): void {
    this.customAttendantResponse = fn;
  }

  public setCustomConciergeResponse(fn?: (context: ConciergePromptContext) => string): void {
    this.customConciergeResponse = fn;
  }

  public async generateAssistantResponse(
    context: AssistantPromptContext
  ): Promise<MetaModelResponse> {
    this.callCount++;
    this.lastContextReceived = context;

    if (this.shouldFail) {
      throw this.failureError ?? new Error('Simulated Meta Model API failure');
    }

    if (this.customResponse) {
      const customText = this.customResponse(context);
      return {
        text: customText,
        model: this.modelName,
        usage: { promptTokens: 50, completionTokens: 30, totalTokens: 80 },
      };
    }

    const { verifiedData } = context;
    const isEnglish = context.systemPrompt?.includes('American English') ?? false;
    let text = '';

    if (isEnglish) {
      switch (verifiedData.status) {
        case 'OPEN':
          text =
            `Hello! ${verifiedData.businessName} is currently OPEN. ` +
            `Today's hours are from ${verifiedData.openTimeCivil} to ${verifiedData.closeTimeCivil} (${verifiedData.timeZone}). ` +
            `The last permitted entry is at ${verifiedData.cutoffTimeCivil}.`;
          break;

        case 'CLOSING_SOON':
          text =
            `Attention: ${verifiedData.businessName} is currently CLOSING SOON. ` +
            `The business closes at ${verifiedData.closeTimeCivil}. The last entry cutoff was at ${verifiedData.cutoffTimeCivil}, ` +
            `so new admissions or orders are no longer permitted.`;
          break;

        case 'CLOSED':
          text =
            `${verifiedData.businessName} is currently CLOSED. ` +
            `${verifiedData.summaryMessage} ` +
            `Standard operating hours are from ${verifiedData.openTimeCivil} to ${verifiedData.closeTimeCivil}.`;
          break;

        case 'SPECIAL_CLOSURE':
          text =
            `Special Notice: ${verifiedData.businessName} is CLOSED today due to a holiday or special schedule. ` +
            `${verifiedData.summaryMessage}`;
          break;
      }
    } else {
      switch (verifiedData.status) {
        case 'OPEN':
          text =
            `¡Hola! ${verifiedData.businessName} se encuentra actualmente ABIERTO. ` +
            `Hoy atiende de ${verifiedData.openTimeCivil} a ${verifiedData.closeTimeCivil} (${verifiedData.timeZone}). ` +
            `La última hora para ingresar es a las ${verifiedData.cutoffTimeCivil}.`;
          break;

        case 'CLOSING_SOON':
          text =
            `Atención: ${verifiedData.businessName} está por cerrar. ` +
            `El cierre es a las ${verifiedData.closeTimeCivil}. El límite de ingreso fue a las ${verifiedData.cutoffTimeCivil}, ` +
            `por lo que ya no se permite el ingreso a nuevos clientes.`;
          break;

        case 'CLOSED':
          text =
            `Lo sentimos, ${verifiedData.businessName} está actualmente CERRADO. ` +
            `${verifiedData.summaryMessage} ` +
            `Su horario habitual en días de operación es de ${verifiedData.openTimeCivil} a ${verifiedData.closeTimeCivil}.`;
          break;

        case 'SPECIAL_CLOSURE':
          text =
            `Aviso importante: ${verifiedData.businessName} está CERRADO hoy por fecha especial o día festivo. ` +
            `${verifiedData.summaryMessage}`;
          break;
      }
    }

    return {
      text,
      model: this.modelName,
      usage: { promptTokens: 120, completionTokens: 45, totalTokens: 165 },
    };
  }

  public async generateWitnessResponse(
    context: WitnessPromptContext
  ): Promise<MetaModelResponse> {
    this.callCount++;
    this.lastWitnessContextReceived = context;

    if (this.shouldFail) {
      throw this.failureError ?? new Error('Simulated Meta Model API failure');
    }

    if (this.customWitnessResponse) {
      const customText = this.customWitnessResponse(context);
      return {
        text: customText,
        model: this.modelName,
        usage: { promptTokens: 60, completionTokens: 35, totalTokens: 95 },
      };
    }

    const fallbackText = generateDeterministicFallbackResponse(
      context.witness,
      context.userMessage,
      context.language
    );

    return {
      text: fallbackText,
      model: this.modelName,
      usage: { promptTokens: 60, completionTokens: 35, totalTokens: 95 },
    };
  }

  public async generateConciergeResponse(
    context: ConciergePromptContext
  ): Promise<MetaModelResponse> {
    this.callCount++;
    this.lastConciergeContextReceived = context;

    if (this.shouldFail) {
      throw this.failureError ?? new Error('Simulated Meta Model API failure');
    }

    if (this.customConciergeResponse) {
      const customText = this.customConciergeResponse(context);
      return {
        text: customText,
        model: this.modelName,
        usage: { promptTokens: 70, completionTokens: 40, totalTokens: 110 },
      };
    }

    const deterministicText = formatDeterministicConciergeResponse(
      context.intent,
      context.comparison,
      context.language
    );

    return {
      text: deterministicText,
      model: this.modelName,
      usage: { promptTokens: 70, completionTokens: 40, totalTokens: 110 },
    };
  }

  public async generateBusinessAttendantResponse(
    context: BusinessAttendantPromptContext
  ): Promise<MetaModelResponse> {
    this.callCount++;
    this.lastAttendantContextReceived = context;

    if (this.shouldFail) {
      throw this.failureError ?? new Error('Simulated Meta Model API failure');
    }

    if (this.customAttendantResponse) {
      const customText = this.customAttendantResponse(context);
      return {
        text: customText,
        model: this.modelName,
        usage: { promptTokens: 75, completionTokens: 45, totalTokens: 120 },
      };
    }

    const topic = classifyAttendantInquiry(context.userMessage);
    const deterministicText = getDeterministicAttendantResponse(
      context.attendant,
      context.status,
      context.schedule,
      topic,
      context.userMessage,
      context.language
    );

    return {
      text: deterministicText,
      model: this.modelName,
      usage: { promptTokens: 75, completionTokens: 45, totalTokens: 120 },
    };
  }

  public async segmentImage(options: SamSegmentOptions): Promise<SamSegmentationResult> {
    this.samCallCount++;
    this.lastSegmentOptionsReceived = options;

    if (this.shouldFail || this.samShouldFail) {
      throw this.failureError ?? new Error('SAM 3.1 visual analysis service unavailable');
    }

    if (this.customSegmentResponse) {
      return this.customSegmentResponse(options);
    }

    const prompt = options.promptText ?? 'urban civic issue';

    if (this.samNoMatch) {
      return {
        model: 'sam-3.1',
        status: 'inconclusive',
        detectedConcept: prompt,
        boxes: [],
        maskReference: null,
        maskSummary: `No distinct boundaries detected for "${prompt}" in photograph`,
        analyzedAt: new Date().toISOString(),
        provider: 'sam-3.1-local-mock',
        disclaimer: SAM_VISUAL_ANALYSIS_DISCLAIMER_EN,
        objects: [],
        overlayPngBase64: null,
        hasAuthenticMasks: false,
      };
    }

    const imgW = options.imageDimensions?.width ?? 512;
    const imgH = options.imageDimensions?.height ?? 512;

    if (this.samBoxesOnly) {
      // Box-only representation (Requirement 6: boxes without masks)
      const bBox1 = { ymin: 0.35, xmin: 0.20, ymax: 0.68, xmax: 0.46, label: prompt, confidence: 0.92 };
      const bBox2 = { ymin: 0.40, xmin: 0.54, ymax: 0.72, xmax: 0.80, label: prompt, confidence: 0.85 };
      return {
        model: 'sam-3.1',
        status: 'completed',
        detectedConcept: prompt,
        boxes: [bBox1, bBox2],
        maskReference: null,
        maskSummary: `Segmented 2 candidate box region(s) [boxes only]`,
        maskDimensions: null,
        objectId: 'mock_target_001',
        confidence: 0.92,
        imageDimensions: { width: imgW, height: imgH },
        analyzedAt: new Date().toISOString(),
        provider: 'sam-3.1-local-mock',
        disclaimer: SAM_VISUAL_ANALYSIS_DISCLAIMER_EN,
        objects: [
          { objectId: 'mock_target_001', label: prompt, confidence: 0.92, box: bBox1, colorHex: '#00E5FF' },
          { objectId: 'mock_target_002', label: prompt, confidence: 0.85, box: bBox2, colorHex: '#FF9100' },
        ],
        overlayPngBase64: null,
        hasAuthenticMasks: false,
      };
    }

    // Default mock: authentic visual segmentation with 2 distinct objects and verified masks
    const b1 = {
      left: Math.round(imgW * 0.20),
      top: Math.round(imgH * 0.35),
      right: Math.round(imgW * 0.46),
      bottom: Math.round(imgH * 0.68),
    };
    const w1 = Math.max(2, b1.right - b1.left);
    const h1 = Math.max(2, b1.bottom - b1.top);
    const r1 = new Uint8Array(w1 * h1);
    const cx1 = w1 / 2, cy1 = h1 / 2, rx1 = w1 / 2, ry1 = h1 / 2;
    for (let y = 0; y < h1; y++) {
      for (let x = 0; x < w1; x++) {
        if (((x - cx1) / rx1) ** 2 + ((y - cy1) / ry1) ** 2 <= 0.85) {
          r1[y * w1 + x] = 1;
        }
      }
    }

    const b2 = {
      left: Math.round(imgW * 0.54),
      top: Math.round(imgH * 0.40),
      right: Math.round(imgW * 0.80),
      bottom: Math.round(imgH * 0.72),
    };
    const w2 = Math.max(2, b2.right - b2.left);
    const h2 = Math.max(2, b2.bottom - b2.top);
    const r2 = new Uint8Array(w2 * h2);
    const cx2 = w2 / 2, cy2 = h2 / 2, rx2 = w2 / 2, ry2 = h2 / 2;
    for (let y = 0; y < h2; y++) {
      for (let x = 0; x < w2; x++) {
        if (((x - cx2) / rx2) ** 2 + ((y - cy2) / ry2) ** 2 <= 0.85) {
          r2[y * w2 + x] = 1;
        }
      }
    }

    const comp = await compositeSamMaskOverlay(
      imgW,
      imgH,
      [
        {
          objectId: 'mock_target_001',
          raster: r1,
          maskWidth: w1,
          maskHeight: h1,
          bounds: b1,
          confidence: undefined,
          label: prompt,
        },
        {
          objectId: 'mock_target_002',
          raster: r2,
          maskWidth: w2,
          maskHeight: h2,
          bounds: b2,
          confidence: undefined,
          label: prompt,
        },
      ],
      [],
      prompt
    );

    return {
      model: 'sam-3.1',
      status: 'completed',
      detectedConcept: prompt,
      boxes: comp.objects.map((o) => o.box),
      maskReference: 'mask_rle_candidate_001',
      maskSummary: `Segmented candidate region (${comp.objects.length} mask(s) decoded via SAM 3.1) [simulated mock]`,
      maskDimensions: { width: w1, height: h1 },
      objectId: 'mock_target_001',
      confidence: undefined,
      imageDimensions: { width: imgW, height: imgH },
      analyzedAt: new Date().toISOString(),
      provider: 'sam-3.1-local-mock',
      disclaimer: SAM_VISUAL_ANALYSIS_DISCLAIMER_EN,
      objects: comp.objects,
      overlayPngBase64: comp.overlayPngBase64,
      hasAuthenticMasks: comp.hasAuthenticMasks,
    };
  }

  public async interpretCitizenReport(options: InterpretReportOptions): Promise<MuseSparkInterpretation> {
    this.museCallCount++;
    this.lastInterpretOptionsReceived = options;

    if (this.shouldFail || this.museShouldFail) {
      throw this.failureError ?? new Error('Muse Spark interpretation service unavailable');
    }

    if (this.customInterpretResponse) {
      return this.customInterpretResponse(options);
    }

    const isEs = options.language === 'es';
    const desc = options.citizenDescription;
    const loc = options.locationDescription || (isEs ? 'Avenida Benito Juárez (sector virtual del corredor)' : 'Avenida Benito Juárez (virtual corridor sector)');

    const classification = classifyCivicIssue(desc);
    const category = classification.category;
    const dept = getSimulatedDepartment(category, isEs ? 'es' : 'en');
    const visualTarget = getDefaultSamTarget(category);
    const nonEmergency = assessNonEmergencyBoundary(desc);

    return {
      model: 'muse-spark-1.3',
      summary: isEs
        ? `Reporte cívico (${category}): ${desc}`
        : `Civic issue report (${category}): ${desc}`,
      category,
      locationDescription: loc,
      additionalDetails: [
        isEs
          ? `Incidencia clasificada bajo la categoría '${category}'`
          : `Incident classified under category '${category}'`,
        options.visualAnalysis?.status === 'completed'
          ? (isEs ? 'Evidencia fotográfica analizada con segmentación visual coincidente' : 'Photographic evidence analyzed with matching visual segmentation')
          : options.visualAnalysis?.provider === 'not-authorized'
          ? (isEs ? 'Evidencia fotográfica preservada localmente sin análisis externo por privacidad' : 'Photographic evidence preserved locally without external analysis per privacy')
          : options.visualAnalysis
          ? (isEs ? 'Evidencia fotográfica adjunta' : 'Photographic evidence attached')
          : (isEs ? 'Reporte registrado sin evidencia fotográfica' : 'Report registered without photographic evidence'),
      ],
      missingInformation: classification.isAmbiguous
        ? [isEs ? 'Descripción ambigua; se recomienda especificar detalles o ubicación precisa' : 'Description is ambiguous; recommend providing specific details or precise location']
        : [],
      suggestedDepartment: dept,
      suggestedVisualTarget: visualTarget,
      clarificationQuestion: classification.isAmbiguous
        ? (isEs ? '¿Podría proporcionar más detalles específicos sobre la incidencia?' : 'Could you provide more specific details regarding the reported issue?')
        : null,
      isEmergency: nonEmergency.isEmergency,
      interpretedAt: new Date().toISOString(),
      provider: 'muse-spark-1.3-local-mock',
    };
  }
}
