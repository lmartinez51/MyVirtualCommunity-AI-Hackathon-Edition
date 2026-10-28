import {
  BorderCrossingComparison,
  comparePedestrianCrossings,
  OFFICIAL_CAMERA_METADATA,
  STANDARD_PEDESTRIAN_LANE,
} from '../domain/border-crossing.js';
import { BorderCrossingPort } from '../ports/border-crossing-port.js';

export interface GetBorderWaitTimesDependencies {
  readonly borderCrossingPort: BorderCrossingPort;
}

export interface GetBorderWaitTimesOptions {
  readonly bypassCache?: boolean;
}

/**
 * Use case: Retrieve and compare current official pedestrian border wait times
 * for Ciudad Juárez to El Paso (Paso del Norte vs Bridge of the Americas).
 *
 * Deterministic and grounded in authoritative U.S. Customs and Border Protection records.
 */
export class GetBorderWaitTimesUseCase {
  private readonly borderCrossingPort: BorderCrossingPort;

  constructor(dependencies: GetBorderWaitTimesDependencies) {
    this.borderCrossingPort = dependencies.borderCrossingPort;
  }

  public async execute(options: GetBorderWaitTimesOptions = {}): Promise<BorderCrossingComparison> {
    const { observations, fetchedAt } = await (this.borderCrossingPort as any).getPedestrianCrossings(
      options.bypassCache ?? false
    );

    const comparison = comparePedestrianCrossings(
      observations.pasoDelNorte,
      observations.bridgeOfTheAmericas
    );

    return {
      direction: 'juarez-to-el-paso',
      mode: 'pedestrian',
      laneCategory: 'standard',
      laneCategoryDisplay: STANDARD_PEDESTRIAN_LANE.name,
      observations,
      comparison,
      cameras: OFFICIAL_CAMERA_METADATA,
      fetchedAt,
    };
  }
}
