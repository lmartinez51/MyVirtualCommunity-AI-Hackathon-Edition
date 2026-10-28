import { BorderCrossingObservation } from '../domain/border-crossing.js';

/**
 * Read-only boundary port for obtaining official border crossing observations.
 *
 * Implemented by adapters fetching from official sources (e.g. U.S. Customs and Border Protection).
 * Strictly isolated so other official sources can be substituted or tested offline.
 */
export interface BorderCrossingPort {
  /**
   * Retrieves current pedestrian crossing observations for Paso del Norte and Bridge of the Americas.
   */
  getPedestrianCrossings(): Promise<{
    observations: {
      pasoDelNorte: BorderCrossingObservation;
      bridgeOfTheAmericas: BorderCrossingObservation;
    };
    fetchedAt: string;
  }>;
}
