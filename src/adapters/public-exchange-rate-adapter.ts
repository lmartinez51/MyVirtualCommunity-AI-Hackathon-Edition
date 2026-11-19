/**
 * Public Keyless USD -> MXN Exchange Rate Adapter.
 *
 * Queries free, keyless public FX endpoints with strict timeouts (`AbortController`),
 * automatic secondary provider fallback, bounded in-memory caching (default 15 min),
 * and honest `unavailable` degradation when offline or upstream fails.
 *
 * Primary:   https://open.er-api.com/v6/latest/USD
 * Fallback:  https://api.frankfurter.app/latest?from=USD&to=MXN
 */

import {
  DEFAULT_BORDER_CASH_HALF_SPREAD_MXN,
  ExchangeRateObservation,
  deriveBorderCashRates,
} from '../domain/exchange-rate.js';
import { ExchangeRatePort } from '../ports/exchange-rate-port.js';

export interface PublicExchangeRateAdapterOptions {
  readonly primaryUrl?: string;
  readonly fallbackUrl?: string;
  readonly fetchFn?: typeof fetch;
  readonly timeoutMs?: number;
  readonly cacheTtlMs?: number;
  readonly halfSpreadMxn?: number;
  readonly nowFn?: () => number;
}

interface CachedExchangeRateEntry {
  readonly observation: ExchangeRateObservation;
  readonly expiresAt: number;
}

export class PublicExchangeRateAdapter implements ExchangeRatePort {
  private readonly primaryUrl: string;
  private readonly fallbackUrl: string;
  private readonly fetchFn: typeof fetch;
  private readonly timeoutMs: number;
  private readonly cacheTtlMs: number;
  private readonly halfSpreadMxn: number;
  private readonly nowFn: () => number;
  private cache: CachedExchangeRateEntry | null = null;

  constructor(options: PublicExchangeRateAdapterOptions = {}) {
    this.primaryUrl = options.primaryUrl ?? 'https://open.er-api.com/v6/latest/USD';
    this.fallbackUrl = options.fallbackUrl ?? 'https://api.frankfurter.app/latest?from=USD&to=MXN';
    this.fetchFn = options.fetchFn ?? globalThis.fetch.bind(globalThis);
    this.timeoutMs = options.timeoutMs ?? 4500;
    this.cacheTtlMs = options.cacheTtlMs ?? 900_000; // 15 minutes
    this.halfSpreadMxn = options.halfSpreadMxn ?? DEFAULT_BORDER_CASH_HALF_SPREAD_MXN;
    this.nowFn = options.nowFn ?? (() => Date.now());
  }

  public async getCurrentExchangeRate(bypassCache: boolean = false): Promise<ExchangeRateObservation> {
    const nowMs = this.nowFn();
    if (!bypassCache && this.cache && nowMs < this.cache.expiresAt) {
      return Object.freeze({
        ...this.cache.observation,
        freshness: 'cached',
      });
    }

    const fetchedAt = new Date(nowMs).toISOString();
    const errors: string[] = [];

    const primaryAttempt = await this.fetchAndParse(this.primaryUrl, 'open.er-api.com', fetchedAt);
    if (primaryAttempt.observation) {
      this.cache = { observation: primaryAttempt.observation, expiresAt: nowMs + this.cacheTtlMs };
      return primaryAttempt.observation;
    }
    if (primaryAttempt.error) errors.push(`primary: ${primaryAttempt.error}`);

    const fallbackAttempt = await this.fetchAndParse(this.fallbackUrl, 'api.frankfurter.app', fetchedAt);
    if (fallbackAttempt.observation) {
      this.cache = { observation: fallbackAttempt.observation, expiresAt: nowMs + this.cacheTtlMs };
      return fallbackAttempt.observation;
    }
    if (fallbackAttempt.error) errors.push(`fallback: ${fallbackAttempt.error}`);

    return Object.freeze({
      status: 'unavailable',
      freshness: 'unavailable',
      baseCurrency: 'USD',
      quoteCurrency: 'MXN',
      midMarketRate: null,
      buyRate: null,
      sellRate: null,
      spreadMxn: this.halfSpreadMxn,
      spreadBasis: 'border-cash-derived-from-mid-market',
      observedAt: null,
      fetchedAt,
      source: 'open.er-api.com / api.frankfurter.app',
      errorDetail: errors.join('; ') || 'Exchange rate providers unavailable',
    });
  }

  private async fetchAndParse(
    url: string,
    sourceLabel: string,
    fetchedAt: string
  ): Promise<{ observation: ExchangeRateObservation | null; error?: string }> {
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), this.timeoutMs);

    try {
      const response = await this.fetchFn(url, {
        method: 'GET',
        headers: { Accept: 'application/json' },
        signal: controller.signal,
      });

      if (!response.ok) {
        return { observation: null, error: `HTTP ${response.status}` };
      }

      const payload: unknown = await response.json();
      const parsed = this.parsePayload(payload, sourceLabel, fetchedAt);
      if (!parsed) {
        return { observation: null, error: 'Malformed FX payload (missing finite MXN rate)' };
      }

      return { observation: parsed };
    } catch (err) {
      const detail =
        err instanceof Error && err.name === 'AbortError'
          ? 'Request timed out'
          : err instanceof Error
            ? err.message
            : String(err);
      return { observation: null, error: detail };
    } finally {
      clearTimeout(timer);
    }
  }

  private parsePayload(
    payload: unknown,
    sourceLabel: string,
    fetchedAt: string
  ): ExchangeRateObservation | null {
    if (!payload || typeof payload !== 'object') return null;
    const record = payload as Record<string, unknown>;
    const rates = record.rates;
    if (!rates || typeof rates !== 'object') return null;

    const rawMxn = (rates as Record<string, unknown>).MXN;
    if (typeof rawMxn !== 'number' || !Number.isFinite(rawMxn) || rawMxn <= 0) {
      return null;
    }

    const midMarketRate = Number(rawMxn.toFixed(4));
    const { buyRate, sellRate } = deriveBorderCashRates(midMarketRate, this.halfSpreadMxn);

    let observedAt: string | null = null;
    if (typeof record.time_last_update_utc === 'string' && record.time_last_update_utc.trim().length > 0) {
      const parsedDate = Date.parse(record.time_last_update_utc);
      observedAt = Number.isFinite(parsedDate)
        ? new Date(parsedDate).toISOString()
        : record.time_last_update_utc.trim();
    } else if (typeof record.date === 'string' && record.date.trim().length > 0) {
      observedAt = record.date.trim();
    }

    return Object.freeze({
      status: 'available',
      freshness: 'live',
      baseCurrency: 'USD',
      quoteCurrency: 'MXN',
      midMarketRate,
      buyRate,
      sellRate,
      spreadMxn: this.halfSpreadMxn,
      spreadBasis: 'border-cash-derived-from-mid-market',
      observedAt,
      fetchedAt,
      source: sourceLabel,
    });
  }
}
