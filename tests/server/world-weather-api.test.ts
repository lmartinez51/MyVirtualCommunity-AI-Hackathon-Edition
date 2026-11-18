import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { InMemoryWorldCoreAdapter } from '../../src/adapters/in-memory-world-core-adapter.js';
import { MockMetaModelAdapter } from '../../src/adapters/mock-meta-model-adapter.js';
import { OpenMeteoWeatherAdapter } from '../../src/adapters/open-meteo-weather-adapter.js';
import { createAssistantServer } from '../../src/server/index.js';

describe('GET /api/world/weather API Integration Tests', () => {
  it('returns authoritative live weather observation when upstream succeeds', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const weatherPort = new OpenMeteoWeatherAdapter({
      fetchFn: (async () =>
        new Response(
          JSON.stringify({
            current: {
              time: '2026-11-17T17:45',
              temperature_2m: 19.2,
              relative_humidity_2m: 28,
              apparent_temperature: 18.0,
              is_day: 0,
              weather_code: 0,
              wind_speed_10m: 9.4,
            },
          }),
          { status: 200 }
        )) as typeof fetch,
    });

    const serverInstance = createAssistantServer({ worldCore, metaModel, weatherPort });
    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      const res = await fetch(`${url}/api/world/weather`);
      assert.strictEqual(res.status, 200);
      const body = (await res.json()) as any;
      assert.strictEqual(body.success, true);
      assert.strictEqual(body.weather.status, 'available');
      assert.strictEqual(body.weather.temperatureCelsius, 19.2);
      assert.strictEqual(body.weather.conditionLabel.es, 'Despejado');
      assert.strictEqual(body.weather.isDay, false);
    } finally {
      await serverInstance.stop();
    }
  });

  it('returns explicit unavailable weather status cleanly when offline without 500 error', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const weatherPort = new OpenMeteoWeatherAdapter({
      fetchFn: (async () => {
        throw new Error('Network unreachable');
      }) as typeof fetch,
    });

    const serverInstance = createAssistantServer({ worldCore, metaModel, weatherPort });
    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      const res = await fetch(`${url}/api/world/weather?refresh=true`);
      assert.strictEqual(res.status, 200);
      const body = (await res.json()) as any;
      assert.strictEqual(body.success, true);
      assert.strictEqual(body.weather.status, 'unavailable');
      assert.strictEqual(body.weather.temperatureCelsius, null);
    } finally {
      await serverInstance.stop();
    }
  });
});
