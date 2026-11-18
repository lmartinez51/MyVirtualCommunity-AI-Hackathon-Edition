import { WorldWeatherObservation } from '../domain/weather.js';

export interface WeatherPort {
  /**
   * Returns current verified meteorological observation for Ciudad Juárez Centro.
   * Upstream errors or timeouts must return an explicit `unavailable` observation
   * without throwing or fabricating synthetic temperature measurements.
   */
  getCurrentWeather(bypassCache?: boolean): Promise<WorldWeatherObservation>;
}
