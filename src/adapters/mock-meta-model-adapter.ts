import {
  AssistantPromptContext,
  MetaModelPort,
  MetaModelResponse,
} from '../ports/meta-model-port.js';

export interface MockMetaModelOptions {
  readonly modelName?: string;
  readonly shouldFail?: boolean;
  readonly failureError?: Error;
  readonly customResponse?: (context: AssistantPromptContext) => string;
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
  private readonly modelName: string;
  private shouldFail: boolean;
  private failureError: Error | null;
  private customResponse?: (context: AssistantPromptContext) => string;
  public lastContextReceived: AssistantPromptContext | null = null;
  public callCount: number = 0;

  constructor(options: MockMetaModelOptions = {}) {
    this.modelName = options.modelName ?? 'muse-spark-1.3-mock';
    this.shouldFail = options.shouldFail ?? false;
    this.failureError = options.failureError ?? null;
    this.customResponse = options.customResponse;
  }

  public setShouldFail(shouldFail: boolean, error?: Error): void {
    this.shouldFail = shouldFail;
    this.failureError = error ?? new Error('Simulated Meta Model API failure');
  }

  public setCustomResponse(fn?: (context: AssistantPromptContext) => string): void {
    this.customResponse = fn;
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
    let text: string;

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
}
