/**
 * Exchange Rate Port (Hexagonal Boundary).
 *
 * Decouples HTTP routes and application consumers from upstream FX providers.
 */

import { ExchangeRateObservation } from '../domain/exchange-rate.js';

export interface ExchangeRatePort {
  getCurrentExchangeRate(bypassCache?: boolean): Promise<ExchangeRateObservation>;
}
