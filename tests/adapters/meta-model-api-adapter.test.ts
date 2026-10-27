import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  MetaApiError,
  MetaModelApiAdapter,
} from '../../src/adapters/meta-model-api-adapter.js';
import { maskApiKey } from '../../src/config/index.js';
import { validateBusinessSchedule } from '../../src/domain/business-schedule.js';
import { evaluateBusinessOperationalStatus } from '../../src/domain/operational-status.js';
import { AssistantPromptContext } from '../../src/ports/meta-model-port.js';

describe('MetaModelApiAdapter (Unit Tests with Mocked HTTP)', () => {
  const secretKey = 'test_secret_meta_key_998877';

  const schedule = validateBusinessSchedule({
    businessId: 'restaurant:wing-spot',
    name: 'Wing Spot Gómez Morín',
    timeZone: 'America/Ciudad_Juarez',
    operatingDays: [1, 2, 3, 4, 5, 6],
    hours: { openHour: 9, openMinute: 0, closeHour: 18, closeMinute: 0 },
    lastEntryCutoffMinutes: 15,
  });

  const verifiedData = evaluateBusinessOperationalStatus(
    schedule,
    '2026-10-26T20:00:00.000Z'
  );

  const context: AssistantPromptContext = {
    userQuery: '¿Están abiertos?',
    verifiedData,
    schedule,
  };

  it('fails fast when API key is missing without making any network call', async () => {
    let fetchCalled = false;
    const mockFetch = async () => {
      fetchCalled = true;
      return new Response('{}');
    };

    const adapter = new MetaModelApiAdapter({
      config: {
        apiKey: null,
        baseURL: 'https://api.meta.ai/v1',
        model: 'muse-spark-1.3',
        isConfigured: false,
      },
      fetchFn: mockFetch as any,
    });

    await assert.rejects(
      () => adapter.generateAssistantResponse(context),
      (err: Error) => {
        assert.ok(err instanceof MetaApiError);
        assert.match(err.message, /MODEL_API_KEY is not configured/);
        return true;
      }
    );

    assert.equal(fetchCalled, false, 'Fetch must not be called when API key is missing');
  });

  it('successfully parses valid Meta Model API response and configures reasoning_effort minimal', async () => {
    let capturedUrl: string = '';
    let capturedHeaders: Record<string, string> = {};
    let capturedBody: any = null;

    const mockFetch = async (url: string | URL | Request, init?: RequestInit) => {
      capturedUrl = String(url);
      capturedHeaders = (init?.headers as Record<string, string>) ?? {};
      capturedBody = JSON.parse(String(init?.body));

      const payload = {
        id: 'chatcmpl-test-123',
        object: 'chat.completion',
        created: 1729960000,
        model: 'muse-spark-1.3',
        choices: [
          {
            index: 0,
            message: {
              role: 'assistant',
              content:
                'Wing Spot Gómez Morín está abierto hoy hasta las 18:00. La última hora para ingresar es a las 17:45.',
            },
            finish_reason: 'stop',
          },
        ],
        usage: {
          prompt_tokens: 150,
          completion_tokens: 35,
          total_tokens: 185,
        },
      };

      return new Response(JSON.stringify(payload), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      });
    };

    const adapter = new MetaModelApiAdapter({
      config: {
        apiKey: secretKey,
        baseURL: 'https://api.meta.ai/v1',
        model: 'muse-spark-1.3',
        isConfigured: true,
      },
      fetchFn: mockFetch as any,
    });

    const response = await adapter.generateAssistantResponse(context);

    assert.equal(capturedUrl, 'https://api.meta.ai/v1/chat/completions');
    assert.equal(capturedHeaders['Authorization'], `Bearer ${secretKey}`);
    assert.equal(capturedBody.model, 'muse-spark-1.3');
    assert.equal(capturedBody.reasoning_effort, 'minimal');
    assert.ok(Array.isArray(capturedBody.messages));
    assert.equal(
      response.text,
      'Wing Spot Gómez Morín está abierto hoy hasta las 18:00. La última hora para ingresar es a las 17:45.'
    );
    assert.equal(response.model, 'muse-spark-1.3');
    assert.equal(response.usage?.totalTokens, 185);
  });

  it('handles synthetic prompt via generateSimpleCompletion', async () => {
    let capturedBody: any = null;

    const mockFetch = async (_url: string | URL | Request, init?: RequestInit) => {
      capturedBody = JSON.parse(String(init?.body));
      const payload = {
        id: 'chatcmpl-synth-1',
        model: 'muse-spark-1.3',
        choices: [
          {
            index: 0,
            message: {
              role: 'assistant',
              content: 'META_API_SMOKE_OK',
            },
          },
        ],
        usage: { prompt_tokens: 12, completion_tokens: 6, total_tokens: 18 },
      };
      return new Response(JSON.stringify(payload), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      });
    };

    const adapter = new MetaModelApiAdapter({
      config: {
        apiKey: secretKey,
        baseURL: 'https://api.meta.ai/v1',
        model: 'muse-spark-1.3',
        isConfigured: true,
      },
      fetchFn: mockFetch as any,
    });

    const result = await adapter.generateSimpleCompletion('Reply with exactly: META_API_SMOKE_OK', {
      reasoningEffort: 'minimal',
    });
    assert.equal(result.text, 'META_API_SMOKE_OK');
    assert.equal(result.model, 'muse-spark-1.3');
    assert.equal(capturedBody.messages[0].content, 'Reply with exactly: META_API_SMOKE_OK');
    assert.equal(capturedBody.reasoning_effort, 'minimal');
  });

  it('guarantees that diagnostic output and errors cannot contain credential fragments', async () => {
    // 1. maskApiKey must never leak prefixes or suffixes
    assert.equal(maskApiKey(secretKey), '[CONFIGURED]');
    assert.equal(maskApiKey(''), '[NOT CONFIGURED]');
    assert.equal(maskApiKey(null), '[NOT CONFIGURED]');
    assert.equal(maskApiKey(undefined), '[NOT CONFIGURED]');

    // Confirm neither the prefix "test_secret" nor suffix "998877" is contained in maskApiKey
    assert.equal(maskApiKey(secretKey).includes('test_secret'), false);
    assert.equal(maskApiKey(secretKey).includes('998877'), false);

    // 2. Adapter sanitizer completely removes keys and Bearer tokens
    const adapter = new MetaModelApiAdapter({
      config: {
        apiKey: secretKey,
        baseURL: 'https://api.meta.ai/v1',
        model: 'muse-spark-1.3',
        isConfigured: true,
      },
    });

    const leakText = `Error calling API with Bearer ${secretKey}: failed key ${secretKey}`;
    const sanitized = adapter.sanitize(leakText);
    assert.equal(sanitized.includes(secretKey), false);
    assert.equal(sanitized.includes('test_secret'), false);
    assert.equal(sanitized.includes('998877'), false);
    assert.equal(sanitized, 'Error calling API with Bearer [REDACTED]: failed key [REDACTED]');
  });

  it('parses usage diagnostics including reasoning_tokens when present', async () => {
    const mockFetch = async () => {
      const payload = {
        id: 'chatcmpl-reasoning-1',
        model: 'muse-spark-1.3',
        choices: [
          {
            index: 0,
            message: { role: 'assistant', content: 'META_API_SMOKE_OK' },
          },
        ],
        usage: {
          prompt_tokens: 18,
          completion_tokens: 398,
          total_tokens: 416,
          completion_tokens_details: {
            reasoning_tokens: 380,
            accepted_prediction_tokens: 0,
            rejected_prediction_tokens: 0,
          },
        },
      };
      return new Response(JSON.stringify(payload), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      });
    };

    const adapter = new MetaModelApiAdapter({
      config: {
        apiKey: secretKey,
        baseURL: 'https://api.meta.ai/v1',
        model: 'muse-spark-1.3',
        isConfigured: true,
      },
      fetchFn: mockFetch as any,
    });

    const result = await adapter.generateSimpleCompletion('Test prompt');
    assert.ok(result.usage);
    assert.equal(result.usage.promptTokens, 18);
    assert.equal(result.usage.completionTokens, 398);
    assert.equal(result.usage.totalTokens, 416);
    assert.equal(result.usage.reasoningTokens, 380);
    assert.deepEqual(result.usage.completionTokensDetails, {
      reasoningTokens: 380,
      acceptedPredictionTokens: 0,
      rejectedPredictionTokens: 0,
    });
  });

  it('parses usage diagnostics when reasoning_tokens details are omitted', async () => {
    const mockFetch = async () => {
      const payload = {
        id: 'chatcmpl-legacy-1',
        model: 'muse-spark-1.3',
        choices: [
          {
            index: 0,
            message: { role: 'assistant', content: 'Standard Response' },
          },
        ],
        usage: {
          prompt_tokens: 25,
          completion_tokens: 50,
          total_tokens: 75,
        },
      };
      return new Response(JSON.stringify(payload), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      });
    };

    const adapter = new MetaModelApiAdapter({
      config: {
        apiKey: secretKey,
        baseURL: 'https://api.meta.ai/v1',
        model: 'muse-spark-1.3',
        isConfigured: true,
      },
      fetchFn: mockFetch as any,
    });

    const result = await adapter.generateSimpleCompletion('Test prompt');
    assert.ok(result.usage);
    assert.equal(result.usage.promptTokens, 25);
    assert.equal(result.usage.completionTokens, 50);
    assert.equal(result.usage.totalTokens, 75);
    assert.equal(result.usage.reasoningTokens, undefined);
    assert.equal(result.usage.completionTokensDetails, undefined);
  });

  it('parses response safely when usage is omitted entirely', async () => {
    const mockFetch = async () => {
      const payload = {
        id: 'chatcmpl-no-usage',
        model: 'muse-spark-1.3',
        choices: [
          {
            index: 0,
            message: { role: 'assistant', content: 'No usage response' },
          },
        ],
      };
      return new Response(JSON.stringify(payload), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      });
    };

    const adapter = new MetaModelApiAdapter({
      config: {
        apiKey: secretKey,
        baseURL: 'https://api.meta.ai/v1',
        model: 'muse-spark-1.3',
        isConfigured: true,
      },
      fetchFn: mockFetch as any,
    });

    const result = await adapter.generateSimpleCompletion('Test prompt');
    assert.equal(result.text, 'No usage response');
    assert.equal(result.usage, undefined);
  });

  it('handles 401 Unauthorized cleanly without leaking API key', async () => {
    const mockFetch = async () => {
      return new Response(
        JSON.stringify({
          error: {
            message: `Invalid token provided: ${secretKey}`,
            type: 'invalid_request_error',
            code: 'invalid_api_key',
          },
        }),
        { status: 401, headers: { 'Content-Type': 'application/json' } }
      );
    };

    const adapter = new MetaModelApiAdapter({
      config: {
        apiKey: secretKey,
        baseURL: 'https://api.meta.ai/v1',
        model: 'muse-spark-1.3',
        isConfigured: true,
      },
      fetchFn: mockFetch as any,
    });

    await assert.rejects(
      () => adapter.generateAssistantResponse(context),
      (err: Error) => {
        assert.ok(err instanceof MetaApiError);
        assert.equal((err as MetaApiError).statusCode, 401);
        // CRITICAL INVARIANT: The raw secret key or fragments MUST NEVER appear in the error message!
        assert.equal(err.message.includes(secretKey), false);
        assert.equal(err.message.includes('test_secret'), false);
        assert.equal(err.message.includes('998877'), false);
        assert.match(err.message, /\[REDACTED\]/);
        return true;
      }
    );
  });

  it('handles 429 Rate Limit error', async () => {
    const mockFetch = async () => {
      return new Response(
        JSON.stringify({
          error: { message: 'Rate limit exceeded, please retry later' },
        }),
        { status: 429, headers: { 'Content-Type': 'application/json' } }
      );
    };

    const adapter = new MetaModelApiAdapter({
      config: {
        apiKey: secretKey,
        baseURL: 'https://api.meta.ai/v1',
        model: 'muse-spark-1.3',
        isConfigured: true,
      },
      fetchFn: mockFetch as any,
    });

    await assert.rejects(
      () => adapter.generateAssistantResponse(context),
      (err: Error) => {
        assert.ok(err instanceof MetaApiError);
        assert.equal((err as MetaApiError).statusCode, 429);
        assert.match(err.message, /rate limit exceeded/i);
        return true;
      }
    );
  });

  it('handles 500 Internal Server error', async () => {
    const mockFetch = async () => {
      return new Response(
        JSON.stringify({
          error: { message: 'Internal server error at upstream cluster' },
        }),
        { status: 500, headers: { 'Content-Type': 'application/json' } }
      );
    };

    const adapter = new MetaModelApiAdapter({
      config: {
        apiKey: secretKey,
        baseURL: 'https://api.meta.ai/v1',
        model: 'muse-spark-1.3',
        isConfigured: true,
      },
      fetchFn: mockFetch as any,
    });

    await assert.rejects(
      () => adapter.generateAssistantResponse(context),
      (err: Error) => {
        assert.ok(err instanceof MetaApiError);
        assert.equal((err as MetaApiError).statusCode, 500);
        assert.match(err.message, /Internal server error/);
        return true;
      }
    );
  });

  it('handles malformed responses (missing or empty choices array)', async () => {
    // 1. Missing choices array
    const adapter1 = new MetaModelApiAdapter({
      config: {
        apiKey: secretKey,
        baseURL: 'https://api.meta.ai/v1',
        model: 'muse-spark-1.3',
        isConfigured: true,
      },
      fetchFn: (async () =>
        new Response(JSON.stringify({ id: '123' }), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        })) as any,
    });

    await assert.rejects(
      () => adapter1.generateAssistantResponse(context),
      (err: Error) => {
        assert.ok(err instanceof MetaApiError);
        assert.match(err.message, /"choices" array is missing or empty/);
        return true;
      }
    );

    // 2. Empty choices array
    const adapter2 = new MetaModelApiAdapter({
      config: {
        apiKey: secretKey,
        baseURL: 'https://api.meta.ai/v1',
        model: 'muse-spark-1.3',
        isConfigured: true,
      },
      fetchFn: (async () =>
        new Response(JSON.stringify({ choices: [] }), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        })) as any,
    });

    await assert.rejects(
      () => adapter2.generateAssistantResponse(context),
      (err: Error) => {
        assert.ok(err instanceof MetaApiError);
        assert.match(err.message, /"choices" array is missing or empty/);
        return true;
      }
    );

    // 3. Choice missing message content
    const adapter3 = new MetaModelApiAdapter({
      config: {
        apiKey: secretKey,
        baseURL: 'https://api.meta.ai/v1',
        model: 'muse-spark-1.3',
        isConfigured: true,
      },
      fetchFn: (async () =>
        new Response(JSON.stringify({ choices: [{ message: {} }] }), {
          status: 200,
          headers: { 'Content-Type': 'application/json' },
        })) as any,
    });

    await assert.rejects(
      () => adapter3.generateAssistantResponse(context),
      (err: Error) => {
        assert.ok(err instanceof MetaApiError);
        assert.match(err.message, /choices\[0\]\.message\.content is missing/);
        return true;
      }
    );
  });

  it('queries GET /models and extracts available model identifiers', async () => {
    let capturedUrl = '';
    const mockFetch = async (url: string | URL | Request) => {
      capturedUrl = String(url);
      const payload = {
        object: 'list',
        data: [
          { id: 'muse-spark-1.3', object: 'model' },
          { id: 'muse-spark-1.2', object: 'model' },
          { id: 'llama-3.3-70b-instruct', object: 'model' },
        ],
      };
      return new Response(JSON.stringify(payload), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      });
    };

    const adapter = new MetaModelApiAdapter({
      config: {
        apiKey: secretKey,
        baseURL: 'https://api.meta.ai/v1',
        model: 'muse-spark-1.3',
        isConfigured: true,
      },
      fetchFn: mockFetch as any,
    });

    const models = await adapter.listModels();
    assert.equal(capturedUrl, 'https://api.meta.ai/v1/models');
    assert.deepEqual(models, ['muse-spark-1.3', 'muse-spark-1.2', 'llama-3.3-70b-instruct']);
  });

  it('handles network / timeout errors', async () => {
    const mockFetch = async () => {
      throw new Error('Connection refused by host');
    };

    const adapter = new MetaModelApiAdapter({
      config: {
        apiKey: secretKey,
        baseURL: 'https://api.meta.ai/v1',
        model: 'muse-spark-1.3',
        isConfigured: true,
      },
      fetchFn: mockFetch as any,
    });

    await assert.rejects(
      () => adapter.generateAssistantResponse(context),
      (err: Error) => {
        assert.ok(err instanceof MetaApiError);
        assert.match(err.message, /Network error connecting to Meta Model API/);
        return true;
      }
    );
  });
});
