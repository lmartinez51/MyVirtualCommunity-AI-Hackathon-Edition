import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  BorderCrossingObservation,
  CANONICAL_CROSSINGS,
  comparePedestrianCrossings,
  evaluateFreshness,
  FRESHNESS_THRESHOLD_MS,
  OFFICIAL_CAMERA_METADATA,
  STANDARD_PEDESTRIAN_LANE,
} from '../../src/domain/border-crossing.js';

describe('Border Intelligence Domain Models and Comparison Rules', () => {
  const baseFetchedAt = '2026-10-27T18:05:00.000Z';

  function createMockObservation(overrides: Partial<BorderCrossingObservation> = {}): BorderCrossingObservation {
    return {
      crossingId: 'paso-del-norte',
      portNumber: '240202',
      canonicalName: 'Paso del Norte (Santa Fe)',
      direction: 'juarez-to-el-paso',
      mode: 'pedestrian',
      laneCategory: 'standard',
      laneCategoryName: STANDARD_PEDESTRIAN_LANE.name,
      delayMinutes: 5,
      unit: 'minutes',
      operationalStatus: 'no delay',
      portStatus: 'Open',
      lanesOpen: 2,
      maximumLanes: 14,
      sourceObservationTime: 'At 6:00 pm MDT',
      sourceObservationTimestamp: '2026-10-27T18:00:00.000Z',
      fetchedAt: baseFetchedAt,
      sourceUrl: 'https://bwt.cbp.gov/details/06240202/PED',
      provider: 'U.S. Customs and Border Protection (CBP)',
      freshness: 'fresh',
      ...overrides,
    };
  }

  describe('1. Canonical Crossings Mapping', () => {
    it('maps Paso del Norte to CBP port 240202 and PED detail URL', () => {
      const pdn = CANONICAL_CROSSINGS['paso-del-norte'];
      assert.strictEqual(pdn.portNumber, '240202');
      assert.strictEqual(pdn.canonicalName, 'Paso del Norte (Santa Fe)');
      assert.strictEqual(pdn.sourceDetailUrl, 'https://bwt.cbp.gov/details/06240202/PED');
    });

    it('maps Bridge of the Americas to CBP port 240201 and PED detail URL', () => {
      const bota = CANONICAL_CROSSINGS['bridge-of-the-americas'];
      assert.strictEqual(bota.portNumber, '240201');
      assert.strictEqual(bota.canonicalName, 'Bridge of the Americas (BOTA / Puente Libre)');
      assert.strictEqual(bota.sourceDetailUrl, 'https://bwt.cbp.gov/details/06240201/PED');
    });
  });

  describe('2. Freshness Policy Evaluation', () => {
    it('classifies observations within 60 minutes as fresh', () => {
      const sourceTime = new Date(Date.now() - 15 * 60 * 1000).toISOString();
      const fetchTime = new Date().toISOString();
      const status = evaluateFreshness(sourceTime, fetchTime, 'no delay', 'Open');
      assert.strictEqual(status, 'fresh');
    });

    it('classifies observations older than 60 minutes as stale', () => {
      assert.strictEqual(FRESHNESS_THRESHOLD_MS, 60 * 60 * 1000);
      const sourceTime = new Date(Date.now() - (FRESHNESS_THRESHOLD_MS + 5000)).toISOString();
      const fetchTime = new Date().toISOString();
      const status = evaluateFreshness(sourceTime, fetchTime, 'no delay', 'Open');
      assert.strictEqual(status, 'stale');
    });

    it('classifies observations as stale when source timestamp is missing', () => {
      const fetchTime = new Date().toISOString();
      const status = evaluateFreshness(null, fetchTime, 'no delay', 'Open');
      assert.strictEqual(status, 'stale', 'Missing source timestamp cannot guarantee freshness');
    });

    it('classifies source timestamp in the future as stale', () => {
      const futureTime = new Date(Date.now() + 15 * 60 * 1000).toISOString();
      const fetchTime = new Date().toISOString();
      const status = evaluateFreshness(futureTime, fetchTime, 'no delay', 'Open');
      assert.strictEqual(status, 'stale', 'Future source timestamp must not be classified as fresh');
    });

    it('classifies status as pending when operational status indicates update pending', () => {
      const fetchTime = new Date().toISOString();
      const status = evaluateFreshness(fetchTime, fetchTime, 'Update Pending', 'Open');
      assert.strictEqual(status, 'pending');
    });

    it('evaluates freshness for closed crossing independently of operational state', () => {
      const recentTime = new Date(Date.now() - 5 * 60 * 1000).toISOString();
      const fetchTime = new Date().toISOString();
      // Fresh source report that the port is closed
      assert.strictEqual(evaluateFreshness(recentTime, fetchTime, 'Lanes Closed', 'Closed'), 'fresh');
      // Unavailable only when error is present
      assert.strictEqual(evaluateFreshness(recentTime, fetchTime, 'no delay', 'Open', 'Connection refused'), 'unavailable');
    });
  });

  describe('3. Deterministic Comparison Rules', () => {
    it('preserves lane category standard and includes category in comparison statements', () => {
      const pdn = createMockObservation({ delayMinutes: 4 });
      const bota = createMockObservation({
        crossingId: 'bridge-of-the-americas',
        portNumber: '240201',
        canonicalName: 'Bridge of the Americas (BOTA / Puente Libre)',
        delayMinutes: 10,
      });

      const comp = comparePedestrianCrossings(pdn, bota);
      assert.strictEqual(pdn.laneCategory, 'standard');
      assert.strictEqual(bota.laneCategory, 'standard');
      assert.strictEqual(pdn.laneCategoryName.en, 'Standard / General Pedestrian Lane');
      assert.strictEqual(pdn.laneCategoryName.es, 'Carril Peatonal Estándar / General');
      assert.ok(comp.statement.en.includes('Standard / General pedestrian lane'));
      assert.ok(comp.statement.es.includes('carril peatonal Estándar / General'));
    });

    it('determines lower wait when Paso del Norte has lower delay', () => {
      const pdn = createMockObservation({ delayMinutes: 4 });
      const bota = createMockObservation({
        crossingId: 'bridge-of-the-americas',
        portNumber: '240201',
        canonicalName: 'Bridge of the Americas (BOTA / Puente Libre)',
        delayMinutes: 10,
      });

      const comp = comparePedestrianCrossings(pdn, bota);
      assert.strictEqual(comp.canCompare, true);
      assert.strictEqual(comp.differenceMinutes, 6);
      assert.strictEqual(comp.lowerWaitCrossingId, 'paso-del-norte');
      assert.ok(comp.statement.en.includes('Paso del Norte reported wait (4 min)'));
      assert.ok(comp.statement.en.includes('6 minutes lower'));
      assert.ok(comp.statement.es.includes('Paso del Norte (4 min)'));
      assert.ok(comp.statement.es.includes('6 minutos menor'));
    });

    it('determines lower wait when Bridge of the Americas has lower delay', () => {
      const pdn = createMockObservation({ delayMinutes: 25 });
      const bota = createMockObservation({
        crossingId: 'bridge-of-the-americas',
        portNumber: '240201',
        delayMinutes: 15,
      });

      const comp = comparePedestrianCrossings(pdn, bota);
      assert.strictEqual(comp.canCompare, true);
      assert.strictEqual(comp.differenceMinutes, 10);
      assert.strictEqual(comp.lowerWaitCrossingId, 'bridge-of-the-americas');
      assert.ok(comp.statement.en.includes('Bridge of the Americas reported wait (15 min)'));
      assert.ok(comp.statement.en.includes('10 minutes lower'));
      assert.ok(comp.statement.es.includes('Puente de las Américas (15 min)'));
      assert.ok(comp.statement.es.includes('10 minutos menor'));
    });

    it('reports identical wait when delays are equal', () => {
      const pdn = createMockObservation({ delayMinutes: 5 });
      const bota = createMockObservation({
        crossingId: 'bridge-of-the-americas',
        delayMinutes: 5,
      });

      const comp = comparePedestrianCrossings(pdn, bota);
      assert.strictEqual(comp.canCompare, true);
      assert.strictEqual(comp.differenceMinutes, 0);
      assert.strictEqual(comp.lowerWaitCrossingId, 'equal');
      assert.ok(comp.statement.en.includes('identical wait estimate of 5 minutes'));
    });

    it('refuses to compare when one observation is stale', () => {
      const pdn = createMockObservation({ delayMinutes: 5, freshness: 'stale' });
      const bota = createMockObservation({ delayMinutes: 5, freshness: 'fresh' });

      const comp = comparePedestrianCrossings(pdn, bota);
      assert.strictEqual(comp.canCompare, false);
      assert.strictEqual(comp.differenceMinutes, null);
      assert.strictEqual(comp.lowerWaitCrossingId, null);
      assert.ok(comp.reasonUnavailable?.en.includes('Paso del Norte data is stale'));
    });

    it('refuses to compare when delayMinutes is null or non-numeric', () => {
      const pdn = createMockObservation({ delayMinutes: null, freshness: 'pending' });
      const bota = createMockObservation({ delayMinutes: 5, freshness: 'fresh' });

      const comp = comparePedestrianCrossings(pdn, bota);
      assert.strictEqual(comp.canCompare, false);
      assert.strictEqual(comp.differenceMinutes, null);
      assert.ok(comp.reasonUnavailable?.en.includes('Paso del Norte has an update pending'));
    });

    it('refuses to compare when a crossing is closed, reporting closure rather than data unavailability', () => {
      const pdn = createMockObservation({
        portStatus: 'Closed',
        delayMinutes: null,
        operationalStatus: 'Lanes Closed',
        freshness: 'fresh',
      });
      const bota = createMockObservation({
        crossingId: 'bridge-of-the-americas',
        portNumber: '240201',
        canonicalName: 'Bridge of the Americas (BOTA / Puente Libre)',
        delayMinutes: 5,
        portStatus: 'Open',
        freshness: 'fresh',
      });

      const comp = comparePedestrianCrossings(pdn, bota);
      assert.strictEqual(comp.canCompare, false);
      assert.strictEqual(comp.differenceMinutes, null);
      assert.strictEqual(comp.lowerWaitCrossingId, null);
      assert.ok(comp.reasonUnavailable?.en.includes('Paso del Norte crossing is reported as closed by CBP'));
      assert.ok(comp.reasonUnavailable?.es.includes('El cruce de Paso del Norte se reporta cerrado por CBP'));
    });

    it('refuses to compare if lane categories mismatch', () => {
      const pdn = createMockObservation({ delayMinutes: 4, laneCategory: 'standard' });
      const bota = createMockObservation({
        crossingId: 'bridge-of-the-americas',
        delayMinutes: 10,
        laneCategory: 'ready' as any,
      });

      const comp = comparePedestrianCrossings(pdn, bota);
      assert.strictEqual(comp.canCompare, false);
      assert.ok(comp.reasonUnavailable?.en.includes('incompatible lane categories'));
    });
  });

  describe('4. Official Camera Metadata', () => {
    it('provides verified primary and secondary camera authority URLs', () => {
      assert.strictEqual(
        OFFICIAL_CAMERA_METADATA.officialAuthorityUrl,
        'https://www.puentesfronterizos.gob.mx/camaras-en-vivo.php'
      );
      assert.strictEqual(
        OFFICIAL_CAMERA_METADATA.secondaryAuthorityUrl,
        'https://pdnuno.com/border-crossings/paso-del-norte'
      );
      assert.ok(OFFICIAL_CAMERA_METADATA.description.en.includes('Visual reference only'));
      assert.ok(OFFICIAL_CAMERA_METADATA.description.es.includes('Referencia visual únicamente'));
    });
  });
});
