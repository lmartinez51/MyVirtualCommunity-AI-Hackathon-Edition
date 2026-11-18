import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { OpenMeteoWeatherAdapter } from '../../src/adapters/open-meteo-weather-adapter.js';
import { mapWmoWeatherCodeToCondition } from '../../src/domain/weather.js';

describe('OpenMeteoWeatherAdapter (Unit Tests with Mocked HTTP)', () => {
  it('normalizes valid Open-Meteo current observation and honors bounded cache', async () => {
    let calls = 0;
    let fakeNowMs = 1_763_400_000_000;

    const adapter = new OpenMeteoWeatherAdapter({
      cacheTtlMs: 300_000,
      nowFn: () => fakeNowMs,
      fetchFn: (async () => {
        calls += 1;
        return new Response(
          JSON.stringify({
            latitude: 31.75,
            longitude: -106.48,
            current: {
              time: '2026-11-17T17:30',
              temperature_2m: 21.46,
              relative_humidity_2m: 24,
              apparent_temperature: 20.1,
              is_day: 1,
              weather_code: 2,
              wind_speed_10m: 14.32,
            },
          }),
          { status: 200, headers: { 'Content-Type': 'application/json' } }
        );
      }) as typeof fetch,
    });

    const first = await adapter.getCurrentWeather(false);
    assert.strictEqual(first.status, 'available');
    assert.strictEqual(first.freshness, 'live');
    assert.strictEqual(first.locationName, 'Ciudad Juárez, Chih.');
    assert.strictEqual(first.temperatureCelsius, 21.5);
    assert.strictEqual(first.apparentTemperatureCelsius, 20.1);
    assert.strictEqual(first.relativeHumidityPercent, 24);
    assert.strictEqual(first.windSpeedKmh, 14.3);
    assert.strictEqual(first.weatherCode, 2);
    assert.strictEqual(first.conditionLabel.es, 'Parcialmente nublado');
    assert.strictEqual(first.conditionLabel.en, 'Partly cloudy');
    assert.strictEqual(first.isDay, true);
    assert.strictEqual(first.observedAt, '2026-11-17T17:30');
    assert.strictEqual(calls, 1);

    // Within cache TTL -> returns cached without another HTTP call
    fakeNowMs += 60_000;
    const cached = await adapter.getCurrentWeather(false);
    assert.strictEqual(cached.freshness, 'cached');
    assert.strictEqual(cached.temperatureCelsius, 21.5);
    assert.strictEqual(calls, 1);

    // Explicit bypassCache forces fresh fetch
    fakeNowMs += 1000;
    const refreshed = await adapter.getCurrentWeather(true);
    assert.strictEqual(refreshed.freshness, 'live');
    assert.strictEqual(calls, 2);
  });

  it('degrades explicitly to unavailable without fabricating temperatures on HTTP or network failures', async () => {
    const httpErrAdapter = new OpenMeteoWeatherAdapter({
      fetchFn: (async () => new Response('Service Unavailable', { status: 503 })) as typeof fetch,
    });
    const httpRes = await httpErrAdapter.getCurrentWeather();
    assert.strictEqual(httpRes.status, 'unavailable');
    assert.strictEqual(httpRes.freshness, 'unavailable');
    assert.strictEqual(httpRes.temperatureCelsius, null);
    assert.strictEqual(httpRes.conditionLabel.es, 'No disponible');
    assert.match(httpRes.errorDetail ?? '', /503/);

    const malformedAdapter = new OpenMeteoWeatherAdapter({
      fetchFn: (async () => new Response(JSON.stringify({ ok: true }), { status: 200 })) as typeof fetch,
    });
    const malformedRes = await malformedAdapter.getCurrentWeather();
    assert.strictEqual(malformedRes.status, 'unavailable');
    assert.strictEqual(malformedRes.temperatureCelsius, null);

    const throwAdapter = new OpenMeteoWeatherAdapter({
      fetchFn: (async () => {
        throw new Error('ECONNREFUSED');
      }) as typeof fetch,
    });
    const throwRes = await throwAdapter.getCurrentWeather();
    assert.strictEqual(throwRes.status, 'unavailable');
    assert.match(throwRes.errorDetail ?? '', /ECONNREFUSED/);
  });

  it('maps WMO weather codes cleanly in English and Spanish', () => {
    assert.deepStrictEqual(mapWmoWeatherCodeToCondition(0), { en: 'Clear sky', es: 'Despejado' });
    assert.deepStrictEqual(mapWmoWeatherCodeToCondition(63), { en: 'Rain', es: 'Lluvia' });
    assert.deepStrictEqual(mapWmoWeatherCodeToCondition(95), { en: 'Thunderstorm', es: 'Tormenta eléctrica' });
    assert.deepStrictEqual(mapWmoWeatherCodeToCondition(null), { en: 'Unavailable', es: 'No disponible' });
  });
});
