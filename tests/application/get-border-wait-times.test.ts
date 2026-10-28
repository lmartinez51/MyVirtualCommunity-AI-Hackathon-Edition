import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { GetBorderWaitTimesUseCase } from '../../src/application/get-border-wait-times.js';
import { BorderCrossingObservation, STANDARD_PEDESTRIAN_LANE } from '../../src/domain/border-crossing.js';
import { BorderCrossingPort } from '../../src/ports/border-crossing-port.js';

describe('GetBorderWaitTimesUseCase (Unit Tests)', () => {
  function createObservation(
    id: 'paso-del-norte' | 'bridge-of-the-americas',
    delayMinutes: number
  ): BorderCrossingObservation {
    return {
      crossingId: id,
      portNumber: id === 'paso-del-norte' ? '240202' : '240201',
      canonicalName: id === 'paso-del-norte' ? 'Paso del Norte (Santa Fe)' : 'Bridge of the Americas (BOTA / Puente Libre)',
      direction: 'juarez-to-el-paso',
      mode: 'pedestrian',
      laneCategory: 'standard',
      laneCategoryName: STANDARD_PEDESTRIAN_LANE.name,
      delayMinutes,
      unit: 'minutes',
      operationalStatus: 'no delay',
      portStatus: 'Open',
      lanesOpen: 2,
      maximumLanes: 10,
      sourceObservationTime: 'At 6:00 pm MDT',
      sourceObservationTimestamp: new Date().toISOString(),
      fetchedAt: new Date().toISOString(),
      sourceUrl: `https://bwt.cbp.gov/details/${id === 'paso-del-norte' ? '06240202' : '06240201'}/PED`,
      provider: 'U.S. Customs and Border Protection (CBP)',
      freshness: 'fresh',
    };
  }

  it('retrieves observations and computes deterministic comparison', async () => {
    let passedBypass = false;
    const mockPort: BorderCrossingPort = {
      async getPedestrianCrossings(bypass?: boolean) {
        passedBypass = Boolean(bypass);
        return {
          observations: {
            pasoDelNorte: createObservation('paso-del-norte', 4),
            bridgeOfTheAmericas: createObservation('bridge-of-the-americas', 12),
          },
          fetchedAt: '2026-10-27T18:05:00.000Z',
        };
      },
    };

    const useCase = new GetBorderWaitTimesUseCase({ borderCrossingPort: mockPort });
    const result = await useCase.execute({ bypassCache: true });

    assert.strictEqual(passedBypass, true);
    assert.strictEqual(result.direction, 'juarez-to-el-paso');
    assert.strictEqual(result.mode, 'pedestrian');
    assert.strictEqual(result.laneCategory, 'standard');
    assert.strictEqual(result.laneCategoryDisplay.en, 'Standard / General Pedestrian Lane');
    assert.strictEqual(result.laneCategoryDisplay.es, 'Carril Peatonal Estándar / General');
    assert.strictEqual(result.comparison.canCompare, true);
    assert.strictEqual(result.comparison.differenceMinutes, 8);
    assert.strictEqual(result.comparison.lowerWaitCrossingId, 'paso-del-norte');
    assert.ok(result.cameras.officialAuthorityUrl.includes('puentesfronterizos'));
    assert.ok(result.cameras.secondaryAuthorityUrl.includes('pdnuno.com'));
  });
});
