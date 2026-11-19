import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { PublicExchangeRateAdapter } from '../../src/adapters/public-exchange-rate-adapter.js';

describe('PublicExchangeRateAdapter (Unit Tests with Mocked HTTP)', () => {
  it('parses primary open.er-api.com USD/MXN payload, derives 0.05-rounded border cash spread, and honors cache', async () => {
    let calls = 0;
    let fakeNow = 1_795_000_000_000;
    const adapter = new PublicExchangeRateAdapter({
      nowFn: () => fakeNow,
      cacheTtlMs: 60_000,
      fetchFn: (async () => {
        calls++;
        return new Response(
          JSON.stringify({
            result: 'success',
            time_last_update_utc: 'Thu, 19 Nov 2026 00:02:31 +0000',
            rates: { MXN: 18.725791 },
          }),
          { status: 200 }
        );
      }) as typeof fetch,
    });

    const live = await adapter.getCurrentExchangeRate(false);
    assert.strictEqual(live.status, 'available');
    assert.strictEqual(live.freshness, 'live');
    assert.strictEqual(live.midMarketRate, 18.7258);
    assert.strictEqual(live.buyRate, 18.25);
    assert.strictEqual(live.sellRate, 19.25);
    assert.strictEqual(live.source, 'open.er-api.com');
    assert.strictEqual(calls, 1);

    const cached = await adapter.getCurrentExchangeRate(false);
    assert.strictEqual(cached.freshness, 'cached');
    assert.strictEqual(cached.buyRate, 18.25);
    assert.strictEqual(calls, 1);

    const refreshed = await adapter.getCurrentExchangeRate(true);
    assert.strictEqual(refreshed.freshness, 'live');
    assert.strictEqual(calls, 2);
  });

  it('falls back cleanly to api.frankfurter.app when primary fails', async () => {
    const adapter = new PublicExchangeRateAdapter({
      fetchFn: (async (url: string | URL | Request) => {
        const href = String(url);
        if (href.includes('open.er-api.com')) {
          return new Response('Service Unavailable', { status: 503 });
        }
        return new Response(
          JSON.stringify({
            amount: 1.0,
            base: 'USD',
            date: '2026-11-18',
            rates: { MXN: 18.51 },
          }),
          { status: 200 }
        );
      }) as typeof fetch,
    });

    const obs = await adapter.getCurrentExchangeRate();
    assert.strictEqual(obs.status, 'available');
    assert.strictEqual(obs.source, 'api.frankfurter.app');
    assert.strictEqual(obs.buyRate, 18.0);
    assert.strictEqual(obs.sellRate, 19.0);
    assert.strictEqual(obs.observedAt, '2026-11-18');
  });

  it('returns honest unavailable status when both upstreams fail without fabricating rates', async () => {
    const adapter = new PublicExchangeRateAdapter({
      fetchFn: (async () => {
        throw new Error('Offline');
      }) as typeof fetch,
    });

    const obs = await adapter.getCurrentExchangeRate();
    assert.strictEqual(obs.status, 'unavailable');
    assert.strictEqual(obs.freshness, 'unavailable');
    assert.strictEqual(obs.buyRate, null);
    assert.strictEqual(obs.sellRate, null);
    assert.ok(obs.errorDetail && obs.errorDetail.includes('Offline'));
  });
});
