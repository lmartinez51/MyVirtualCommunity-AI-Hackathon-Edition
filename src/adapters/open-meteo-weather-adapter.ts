/**
 * Open-Meteo Live Weather Adapter for Ciudad Juárez Centro Histórico.
 *
 * Uses the free keyless Open-Meteo Forecast API (`/v1/forecast`) with strict timeouts
 * (`AbortController`), defensive payload validation, bounded in-memory caching (default 5 min),
 * and honest `unavailable` degradation when offline or upstream fails.
 */

import {
  CIUDAD_JUAREZ_CENTRO_COORDS,
  WorldWeatherObservation,
  mapWmoWeatherCodeToCondition,
} from '../domain/weather.js';
import { WeatherPort } from '../ports/weather-port.js';

export interface OpenMeteoWeatherAdapterOptions {
  readonly baseURL?: string;
  readonly fetchFn?: typeof fetch;
  readonly timeoutMs?: number;
  readonly cacheTtlMs?: number;
  readonly nowFn?: () => number;
}

interface CachedWeatherEntry {
  readonly observation: WorldWeatherObservation;
  readonly expiresAt: number;
}

export class OpenMeteoWeatherAdapter implements WeatherPort {
  private readonly baseURL: string;
  private readonly fetchFn: typeof fetch;
  private readonly timeoutMs: number;
  private readonly cacheTtlMs: number;
  private readonly nowFn: () => number;
  private cache: CachedWeatherEntry | null = null;

  constructor(options: OpenMeteoWeatherAdapterOptions = {}) {
    this.baseURL = (options.baseURL ?? 'https://api.open-meteo.com').replace(/\/+$/, '');
    this.fetchFn = options.fetchFn ?? globalThis.fetch.bind(globalThis);
    this.timeoutMs = options.timeoutMs ?? 4000;
    this.cacheTtlMs = options.cacheTtlMs ?? 300_000; // 5 minutes
    this.nowFn = options.nowFn ?? (() => Date.now());
  }

  public async getCurrentWeather(bypassCache: boolean = false): Promise<WorldWeatherObservation> {
    const nowMs = this.nowFn();
    if (!bypassCache && this.cache && nowMs < this.cache.expiresAt) {
      return {
        ...this.cache.observation,
        freshness: 'cached',
      };
    }

    const fetchedAt = new Date(nowMs).toISOString();
    const url =
      `${this.baseURL}/v1/forecast` +
      `?latitude=${CIUDAD_JUAREZ_CENTRO_COORDS.latitude}` +
      `&longitude=${CIUDAD_JUAREZ_CENTRO_COORDS.longitude}` +
      `&current=temperature_2m,relative_humidity_2m,apparent_temperature,is_day,weather_code,wind_speed_10m` +
      `&timezone=${encodeURIComponent(CIUDAD_JUAREZ_CENTRO_COORDS.timeZone)}`;

    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), this.timeoutMs);

    try {
      const response = await this.fetchFn(url, {
        method: 'GET',
        headers: { Accept: 'application/json' },
        signal: controller.signal,
      });

      if (!response.ok) {
        return this.buildUnavailableObservation(fetchedAt, `Upstream HTTP ${response.status}`);
      }

      const payload: unknown = await response.json();
      const parsed = this.parsePayload(payload, fetchedAt);
      if (!parsed) {
        return this.buildUnavailableObservation(fetchedAt, 'Malformed upstream weather payload');
      }

      this.cache = {
        observation: parsed,
        expiresAt: nowMs + this.cacheTtlMs,
      };
      return parsed;
    } catch (err) {
      const detail =
        err instanceof Error && err.name === 'AbortError'
          ? 'Upstream weather request timed out'
          : err instanceof Error
            ? err.message
            : String(err);
      return this.buildUnavailableObservation(fetchedAt, detail);
    } finally {
      clearTimeout(timer);
    }
  }

  private parsePayload(payload: unknown, fetchedAt: string): WorldWeatherObservation | null {
    if (!payload || typeof payload !== 'object') return null;
    const record = payload as Record<string, unknown>;
    const current = record.current;
    if (!current || typeof current !== 'object') return null;

    const c = current as Record<string, unknown>;
    const temp = toFiniteNumber(c.temperature_2m);
    if (temp === null) return null;

    const apparent = toFiniteNumber(c.apparent_temperature);
    const humidity = toFiniteNumber(c.relative_humidity_2m);
    const wind = toFiniteNumber(c.wind_speed_10m);
    const weatherCode = toFiniteNumber(c.weather_code);
    const isDayRaw = toFiniteNumber(c.is_day);
    const isDay = isDayRaw === null ? null : isDayRaw >= 1;
    const observedAt = typeof c.time === 'string' && c.time.trim().length > 0 ? c.time.trim() : null;

    return Object.freeze({
      status: 'available',
      freshness: 'live',
      locationName: CIUDAD_JUAREZ_CENTRO_COORDS.locationName,
      latitude: CIUDAD_JUAREZ_CENTRO_COORDS.latitude,
      longitude: CIUDAD_JUAREZ_CENTRO_COORDS.longitude,
      temperatureCelsius: Math.round(temp * 10) / 10,
      apparentTemperatureCelsius: apparent !== null ? Math.round(apparent * 10) / 10 : null,
      relativeHumidityPercent: humidity !== null ? Math.round(humidity) : null,
      windSpeedKmh: wind !== null ? Math.round(wind * 10) / 10 : null,
      weatherCode: weatherCode !== null ? Math.trunc(weatherCode) : null,
      conditionLabel: mapWmoWeatherCodeToCondition(weatherCode),
      isDay,
      observedAt,
      fetchedAt,
      source: 'open-meteo',
      errorDetail: null,
    });
  }

  private buildUnavailableObservation(fetchedAt: string, errorDetail: string): WorldWeatherObservation {
    return Object.freeze({
      status: 'unavailable',
      freshness: 'unavailable',
      locationName: CIUDAD_JUAREZ_CENTRO_COORDS.locationName,
      latitude: CIUDAD_JUAREZ_CENTRO_COORDS.latitude,
      longitude: CIUDAD_JUAREZ_CENTRO_COORDS.longitude,
      temperatureCelsius: null,
      apparentTemperatureCelsius: null,
      relativeHumidityPercent: null,
      windSpeedKmh: null,
      weatherCode: null,
      conditionLabel: mapWmoWeatherCodeToCondition(null),
      isDay: null,
      observedAt: null,
      fetchedAt,
      source: 'open-meteo',
      errorDetail,
    });
  }
}

function toFiniteNumber(val: unknown): number | null {
  return typeof val === 'number' && Number.isFinite(val) ? val : null;
}
