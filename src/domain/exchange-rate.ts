/**
 * Authoritative USD -> MXN Exchange Rate Domain Model
 * for Ciudad Juárez Centro Histórico currency exchange displays (e.g. Cambios Santa Fe).
 *
 * Strictly preserves factual provenance:
 * - Upstream APIs provide live mid-market USD/MXN reference rates.
 * - Border cash `buyRate` (COMPRA) and `sellRate` (VENTA) are deterministically
 *   derived from the authoritative mid-market rate using a documented cash spread
 *   and rounded to 0.05 MXN steps (matching downtown Juárez border casa-de-cambio boards).
 */

export type ExchangeRateStatus = 'available' | 'unavailable';
export type ExchangeRateFreshness = 'live' | 'cached' | 'unavailable';

export const DEFAULT_BORDER_CASH_HALF_SPREAD_MXN = 0.5;

export interface ExchangeRateObservation {
  readonly status: ExchangeRateStatus;
  readonly freshness: ExchangeRateFreshness;
  readonly baseCurrency: 'USD';
  readonly quoteCurrency: 'MXN';
  readonly midMarketRate: number | null;
  readonly buyRate: number | null;
  readonly sellRate: number | null;
  readonly spreadMxn: number;
  readonly spreadBasis: 'border-cash-derived-from-mid-market';
  readonly observedAt: string | null;
  readonly fetchedAt: string;
  readonly source: string;
  readonly errorDetail?: string;
}

export interface DerivedBorderCashRates {
  readonly buyRate: number;
  readonly sellRate: number;
}

/**
 * Rounds a positive MXN value to the nearest 0.05 MXN increment (e.g. 18.23 -> 18.25).
 */
export function roundToFiveCents(value: number): number {
  return Math.round(value * 20) / 20;
}

/**
 * Deterministically derives realistic Ciudad Juárez border casa-de-cambio
 * COMPRA (buy) and VENTA (sell) quotes from an authoritative live mid-market USD/MXN rate.
 */
export function deriveBorderCashRates(
  midMarketRate: number,
  halfSpreadMxn: number = DEFAULT_BORDER_CASH_HALF_SPREAD_MXN
): DerivedBorderCashRates {
  const safeSpread = Number.isFinite(halfSpreadMxn) && halfSpreadMxn >= 0 ? halfSpreadMxn : DEFAULT_BORDER_CASH_HALF_SPREAD_MXN;
  const rawBuy = Math.max(0.05, midMarketRate - safeSpread);
  const rawSell = Math.max(rawBuy + 0.05, midMarketRate + safeSpread);
  const buyRate = Number(roundToFiveCents(rawBuy).toFixed(2));
  const sellRate = Number(Math.max(buyRate + 0.05, roundToFiveCents(rawSell)).toFixed(2));
  return Object.freeze({ buyRate, sellRate });
}
