import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { InMemoryWorldCoreAdapter } from '../../src/adapters/in-memory-world-core-adapter.js';
import { MockMetaModelAdapter } from '../../src/adapters/mock-meta-model-adapter.js';
import { PublicExchangeRateAdapter } from '../../src/adapters/public-exchange-rate-adapter.js';
import { createAssistantServer } from '../../src/server/index.js';

describe('GET /api/world/exchange-rate API Integration Tests', () => {
  it('returns authoritative live USD/MXN buy and sell quotes when upstream succeeds', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const exchangeRatePort = new PublicExchangeRateAdapter({
      fetchFn: (async () =>
        new Response(
          JSON.stringify({
            time_last_update_utc: 'Thu, 19 Nov 2026 00:02:31 +0000',
            rates: { MXN: 18.0 },
          }),
          { status: 200 }
        )) as typeof fetch,
    });

    const serverInstance = createAssistantServer({ worldCore, metaModel, exchangeRatePort });
    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      const res = await fetch(`${url}/api/world/exchange-rate`);
      assert.strictEqual(res.status, 200);
      const body = (await res.json()) as any;
      assert.strictEqual(body.success, true);
      assert.strictEqual(body.exchangeRate.status, 'available');
      assert.strictEqual(body.exchangeRate.buyRate, 17.5);
      assert.strictEqual(body.exchangeRate.sellRate, 18.5);
      assert.strictEqual(body.exchangeRate.baseCurrency, 'USD');
      assert.strictEqual(body.exchangeRate.quoteCurrency, 'MXN');
    } finally {
      await serverInstance.stop();
    }
  });

  it('returns explicit unavailable exchange-rate status cleanly when offline without 500 error', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const exchangeRatePort = new PublicExchangeRateAdapter({
      fetchFn: (async () => {
        throw new Error('Network unreachable');
      }) as typeof fetch,
    });

    const serverInstance = createAssistantServer({ worldCore, metaModel, exchangeRatePort });
    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      const res = await fetch(`${url}/api/world/exchange-rate?refresh=true`);
      assert.strictEqual(res.status, 200);
      const body = (await res.json()) as any;
      assert.strictEqual(body.success, true);
      assert.strictEqual(body.exchangeRate.status, 'unavailable');
      assert.strictEqual(body.exchangeRate.buyRate, null);
      assert.strictEqual(body.exchangeRate.sellRate, null);
    } finally {
      await serverInstance.stop();
    }
  });
});
