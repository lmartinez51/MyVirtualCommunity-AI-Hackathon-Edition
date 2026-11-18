/**
 * Authoritative World Weather Domain Model (Ciudad Juárez Centro Histórico).
 *
 * Architectural Invariants:
 * 1. The backend is the single source of truth for external environmental observations.
 * 2. Weather readings never fabricate synthetic temperatures when upstream feeds fail;
 *    outages or timeouts degrade explicitly to `status: 'unavailable'` with null metrics.
 */

export interface WeatherBilingualText {
  readonly en: string;
  readonly es: string;
}

export type WeatherObservationStatus = 'available' | 'unavailable';
export type WeatherFreshnessStatus = 'live' | 'cached' | 'unavailable';

export interface WorldWeatherObservation {
  readonly status: WeatherObservationStatus;
  readonly freshness: WeatherFreshnessStatus;
  readonly locationName: string;
  readonly latitude: number;
  readonly longitude: number;
  readonly temperatureCelsius: number | null;
  readonly apparentTemperatureCelsius: number | null;
  readonly relativeHumidityPercent: number | null;
  readonly windSpeedKmh: number | null;
  readonly weatherCode: number | null;
  readonly conditionLabel: WeatherBilingualText;
  readonly isDay: boolean | null;
  readonly observedAt: string | null;
  readonly fetchedAt: string;
  readonly source: 'open-meteo';
  readonly errorDetail: string | null;
}

export const CIUDAD_JUAREZ_CENTRO_COORDS = Object.freeze({
  locationName: 'Ciudad Juárez, Chih.',
  latitude: 31.7455,
  longitude: -106.485,
  timeZone: 'America/Ciudad_Juarez',
});

/**
 * Maps standard WMO weather interpretation codes (0-99) to concise bilingual condition labels.
 */
export function mapWmoWeatherCodeToCondition(code: number | null): WeatherBilingualText {
  if (code === null || !Number.isFinite(code)) {
    return Object.freeze({ en: 'Unavailable', es: 'No disponible' });
  }

  if (code === 0) return Object.freeze({ en: 'Clear sky', es: 'Despejado' });
  if (code === 1) return Object.freeze({ en: 'Mainly clear', es: 'Mayormente despejado' });
  if (code === 2) return Object.freeze({ en: 'Partly cloudy', es: 'Parcialmente nublado' });
  if (code === 3) return Object.freeze({ en: 'Overcast', es: 'Nublado' });
  if (code === 45 || code === 48) return Object.freeze({ en: 'Foggy', es: 'Neblina' });
  if (code >= 51 && code <= 57) return Object.freeze({ en: 'Drizzle', es: 'Llovizna' });
  if ((code >= 61 && code <= 67) || (code >= 80 && code <= 82)) {
    return Object.freeze({ en: 'Rain', es: 'Lluvia' });
  }
  if ((code >= 71 && code <= 77) || code === 85 || code === 86) {
    return Object.freeze({ en: 'Snow', es: 'Nieve' });
  }
  if (code >= 95 && code <= 99) {
    return Object.freeze({ en: 'Thunderstorm', es: 'Tormenta eléctrica' });
  }

  return Object.freeze({ en: `Weather code ${Math.trunc(code)}`, es: `Condición ${Math.trunc(code)}` });
}
