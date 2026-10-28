import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { MockMetaModelAdapter } from '../../src/adapters/mock-meta-model-adapter.js';
import {
  AskBorderConciergeUseCase,
  InvalidConciergeMessageError,
} from '../../src/application/ask-border-concierge.js';
import { GetBorderWaitTimesUseCase } from '../../src/application/get-border-wait-times.js';
import {
  BorderCrossingObservation,
  STANDARD_PEDESTRIAN_LANE,
} from '../../src/domain/border-crossing.js';
import { BorderCrossingPort } from '../../src/ports/border-crossing-port.js';

class MockBorderCrossingPort implements BorderCrossingPort {
  private pdnOverrides: Partial<BorderCrossingObservation> = {};
  private botaOverrides: Partial<BorderCrossingObservation> = {};

  public setPdnOverrides(overrides: Partial<BorderCrossingObservation>): void {
    this.pdnOverrides = overrides;
  }

  public setBotaOverrides(overrides: Partial<BorderCrossingObservation>): void {
    this.botaOverrides = overrides;
  }

  public async getPedestrianCrossings(_bypassCache: boolean = false): Promise<{
    observations: {
      pasoDelNorte: BorderCrossingObservation;
      bridgeOfTheAmericas: BorderCrossingObservation;
    };
    fetchedAt: string;
  }> {
    const pasoDelNorte: BorderCrossingObservation = {
      crossingId: 'paso-del-norte',
      portNumber: '240202',
      canonicalName: 'Paso del Norte (Santa Fe)',
      direction: 'juarez-to-el-paso',
      mode: 'pedestrian',
      laneCategory: 'standard',
      laneCategoryName: STANDARD_PEDESTRIAN_LANE.name,
      delayMinutes: 20,
      unit: 'minutes',
      operationalStatus: 'Delay 20 min',
      portStatus: 'Open',
      lanesOpen: 4,
      maximumLanes: 8,
      sourceObservationTime: '10/27/2026 21:40:00',
      sourceObservationTimestamp: '2026-10-27T21:40:00.000Z',
      fetchedAt: '2026-10-27T22:00:00.000Z',
      sourceUrl: 'https://bwt.cbp.gov/details/06240202/PED',
      provider: 'U.S. Customs and Border Protection (CBP)',
      freshness: 'fresh',
      ...this.pdnOverrides,
    };

    const bridgeOfTheAmericas: BorderCrossingObservation = {
      crossingId: 'bridge-of-the-americas',
      portNumber: '240201',
      canonicalName: 'Bridge of the Americas (BOTA / Puente Libre)',
      direction: 'juarez-to-el-paso',
      mode: 'pedestrian',
      laneCategory: 'standard',
      laneCategoryName: STANDARD_PEDESTRIAN_LANE.name,
      delayMinutes: 45,
      unit: 'minutes',
      operationalStatus: 'Delay 45 min',
      portStatus: 'Open',
      lanesOpen: 2,
      maximumLanes: 6,
      sourceObservationTime: '10/27/2026 21:45:00',
      sourceObservationTimestamp: '2026-10-27T21:45:00.000Z',
      fetchedAt: '2026-10-27T22:00:00.000Z',
      sourceUrl: 'https://bwt.cbp.gov/details/06240201/PED',
      provider: 'U.S. Customs and Border Protection (CBP)',
      freshness: 'fresh',
      ...this.botaOverrides,
    };

    return {
      observations: { pasoDelNorte, bridgeOfTheAmericas },
      fetchedAt: '2026-10-27T22:00:00.000Z',
    };
  }
}

describe('AskBorderConciergeUseCase (Application Tests)', () => {
  describe('1. Profile and Suggested Inquiries', () => {
    it('returns Elena Ramos concierge profile with 6 structured suggested prompt chips', () => {
      const mockPort = new MockBorderCrossingPort();
      const borderUseCase = new GetBorderWaitTimesUseCase({ borderCrossingPort: mockPort });
      const conciergeUseCase = new AskBorderConciergeUseCase({ getBorderWaitTimesUseCase: borderUseCase });

      const profile = conciergeUseCase.getProfile();
      assert.strictEqual(profile.id, 'elena-border-concierge');
      assert.strictEqual(profile.name, 'Elena Ramos');
      assert.strictEqual(profile.suggestedPrompts.length, 6);
    });
  });

  describe('2. Grounded Inquiries via Existing Border Use Case', () => {
    it('answers question about Paso del Norte wait returning observation facts and timestamps', async () => {
      const mockPort = new MockBorderCrossingPort();
      const borderUseCase = new GetBorderWaitTimesUseCase({ borderCrossingPort: mockPort });
      const mockMeta = new MockMetaModelAdapter();
      const conciergeUseCase = new AskBorderConciergeUseCase({
        getBorderWaitTimesUseCase: borderUseCase,
        metaModel: mockMeta,
      });

      const result = await conciergeUseCase.execute({
        message: 'What is the reported pedestrian wait time at Paso del Norte right now?',
        language: 'en',
      });

      assert.strictEqual(result.intent, 'paso-del-norte-wait');
      assert.strictEqual(result.language, 'en');
      assert.strictEqual(result.concierge.name, 'Elena Ramos');
      assert.ok(result.text.includes('20 minutes reported'));
      assert.ok(result.text.includes('10/27/2026 21:40:00'));

      // Authoritative structured data preserved in result
      assert.strictEqual(result.authoritativeObservations.pasoDelNorte.delayMinutes, 20);
      assert.strictEqual(result.authoritativeObservations.pasoDelNorte.sourceObservationTime, '10/27/2026 21:40:00');
      assert.notStrictEqual(
        result.authoritativeObservations.pasoDelNorte.sourceObservationTime,
        result.authoritativeObservations.pasoDelNorte.fetchedAt
      );
      assert.strictEqual(result.authoritativeObservations.pasoDelNorte.portStatus, 'Open');
      assert.strictEqual(result.authoritativeObservations.pasoDelNorte.freshness, 'fresh');
      assert.strictEqual(result.provider, 'mock-meta');
    });

    it('answers question about BOTA wait returning observation facts and timestamps', async () => {
      const mockPort = new MockBorderCrossingPort();
      const borderUseCase = new GetBorderWaitTimesUseCase({ borderCrossingPort: mockPort });
      const mockMeta = new MockMetaModelAdapter();
      const conciergeUseCase = new AskBorderConciergeUseCase({
        getBorderWaitTimesUseCase: borderUseCase,
        metaModel: mockMeta,
      });

      const result = await conciergeUseCase.execute({
        message: 'What is the wait time at Bridge of the Americas (BOTA)?',
        language: 'en',
      });

      assert.strictEqual(result.intent, 'bota-wait');
      assert.ok(result.text.includes('45 minutes reported'));
      assert.ok(result.text.includes('10/27/2026 21:45:00'));
      assert.strictEqual(result.authoritativeObservations.bridgeOfTheAmericas.delayMinutes, 45);
    });

    it('answers comparison questions using existing eligibility rules and snapshot disclaimer', async () => {
      const mockPort = new MockBorderCrossingPort();
      const borderUseCase = new GetBorderWaitTimesUseCase({ borderCrossingPort: mockPort });
      const mockMeta = new MockMetaModelAdapter();
      const conciergeUseCase = new AskBorderConciergeUseCase({
        getBorderWaitTimesUseCase: borderUseCase,
        metaModel: mockMeta,
      });

      const result = await conciergeUseCase.execute({
        message: 'Which crossing has the lower reported wait time right now?',
        language: 'en',
      });

      assert.strictEqual(result.intent, 'compare-crossings');
      assert.strictEqual(result.comparison.canCompare, true);
      assert.strictEqual(result.comparison.lowerWaitCrossingId, 'paso-del-norte');
      assert.strictEqual(result.comparison.differenceMinutes, 25);
      assert.ok(result.text.includes('Paso del Norte reported wait'));
      assert.ok(result.text.includes('25 minutes lower'));
      assert.ok(result.text.includes('point-in-time snapshots and do not guarantee a faster end-to-end journey'));
    });

    it('refuses comparison when one crossing is stale and explains why', async () => {
      const mockPort = new MockBorderCrossingPort();
      mockPort.setPdnOverrides({ freshness: 'stale' });
      const borderUseCase = new GetBorderWaitTimesUseCase({ borderCrossingPort: mockPort });
      const mockMeta = new MockMetaModelAdapter();
      const conciergeUseCase = new AskBorderConciergeUseCase({
        getBorderWaitTimesUseCase: borderUseCase,
        metaModel: mockMeta,
      });

      const result = await conciergeUseCase.execute({
        message: 'Compare the wait times between the bridges',
        language: 'en',
      });

      assert.strictEqual(result.comparison.canCompare, false);
      assert.strictEqual(result.authoritativeObservations.pasoDelNorte.freshness, 'stale');
      assert.ok(result.text.includes('cannot be computed at this time'));
    });

    it('refuses comparison when BOTA observation is stale and does not upgrade stale status', async () => {
      const mockPort = new MockBorderCrossingPort();
      mockPort.setBotaOverrides({ freshness: 'stale' });
      const borderUseCase = new GetBorderWaitTimesUseCase({ borderCrossingPort: mockPort });
      const mockMeta = new MockMetaModelAdapter();
      const conciergeUseCase = new AskBorderConciergeUseCase({
        getBorderWaitTimesUseCase: borderUseCase,
        metaModel: mockMeta,
      });

      const result = await conciergeUseCase.execute({
        message: 'Which crossing has the lower reported wait?',
        language: 'en',
      });

      assert.strictEqual(result.comparison.canCompare, false);
      assert.strictEqual(result.authoritativeObservations.bridgeOfTheAmericas.freshness, 'stale');
      assert.ok(result.text.includes('cannot be computed at this time'));
      assert.ok(!result.text.includes('lower than'));
    });

    it('refuses comparison when both crossings are stale', async () => {
      const mockPort = new MockBorderCrossingPort();
      mockPort.setPdnOverrides({ freshness: 'stale' });
      mockPort.setBotaOverrides({ freshness: 'stale' });
      const borderUseCase = new GetBorderWaitTimesUseCase({ borderCrossingPort: mockPort });
      const mockMeta = new MockMetaModelAdapter();
      const conciergeUseCase = new AskBorderConciergeUseCase({
        getBorderWaitTimesUseCase: borderUseCase,
        metaModel: mockMeta,
      });

      const result = await conciergeUseCase.execute({
        message: 'Compare Paso del Norte and BOTA',
        language: 'en',
      });

      assert.strictEqual(result.comparison.canCompare, false);
      assert.strictEqual(result.authoritativeObservations.pasoDelNorte.freshness, 'stale');
      assert.strictEqual(result.authoritativeObservations.bridgeOfTheAmericas.freshness, 'stale');
      assert.ok(result.text.includes('cannot be computed at this time'));
    });

    it('refuses comparison when a crossing is closed and explains closure', async () => {
      const mockPort = new MockBorderCrossingPort();
      mockPort.setPdnOverrides({ portStatus: 'Closed', delayMinutes: null, operationalStatus: 'Lanes Closed' });
      const borderUseCase = new GetBorderWaitTimesUseCase({ borderCrossingPort: mockPort });
      const mockMeta = new MockMetaModelAdapter();
      const conciergeUseCase = new AskBorderConciergeUseCase({
        getBorderWaitTimesUseCase: borderUseCase,
        metaModel: mockMeta,
      });

      const result = await conciergeUseCase.execute({
        message: 'Which bridge is faster right now?',
        language: 'en',
      });

      assert.strictEqual(result.comparison.canCompare, false);
      assert.ok(result.text.includes('cannot be computed at this time'));
      assert.ok(!result.text.includes('faster'));
    });

    it('refuses comparison when an observation is pending update', async () => {
      const mockPort = new MockBorderCrossingPort();
      mockPort.setBotaOverrides({ freshness: 'pending', delayMinutes: null, operationalStatus: 'Update Pending' });
      const borderUseCase = new GetBorderWaitTimesUseCase({ borderCrossingPort: mockPort });
      const mockMeta = new MockMetaModelAdapter();
      const conciergeUseCase = new AskBorderConciergeUseCase({
        getBorderWaitTimesUseCase: borderUseCase,
        metaModel: mockMeta,
      });

      const result = await conciergeUseCase.execute({
        message: 'Compare the wait times between the bridges',
        language: 'en',
      });

      assert.strictEqual(result.comparison.canCompare, false);
      assert.strictEqual(result.authoritativeObservations.bridgeOfTheAmericas.freshness, 'pending');
      assert.ok(result.text.includes('cannot be computed at this time'));
    });

    it('refuses comparison when an observation is unavailable due to upstream error', async () => {
      const mockPort = new MockBorderCrossingPort();
      mockPort.setPdnOverrides({ freshness: 'unavailable', delayMinutes: null, operationalStatus: 'Unavailable' });
      const borderUseCase = new GetBorderWaitTimesUseCase({ borderCrossingPort: mockPort });
      const mockMeta = new MockMetaModelAdapter();
      const conciergeUseCase = new AskBorderConciergeUseCase({
        getBorderWaitTimesUseCase: borderUseCase,
        metaModel: mockMeta,
      });

      const result = await conciergeUseCase.execute({
        message: 'Compare the bridges',
        language: 'en',
      });

      assert.strictEqual(result.comparison.canCompare, false);
      assert.strictEqual(result.authoritativeObservations.pasoDelNorte.freshness, 'unavailable');
      assert.ok(result.text.includes('cannot be computed at this time'));
    });

    it('refuses comparison when lane categories mismatch', async () => {
      const mockPort = new MockBorderCrossingPort();
      mockPort.setBotaOverrides({ laneCategory: 'ready' as any });
      const borderUseCase = new GetBorderWaitTimesUseCase({ borderCrossingPort: mockPort });
      const mockMeta = new MockMetaModelAdapter();
      const conciergeUseCase = new AskBorderConciergeUseCase({
        getBorderWaitTimesUseCase: borderUseCase,
        metaModel: mockMeta,
      });

      const result = await conciergeUseCase.execute({
        message: 'Compare the bridges',
        language: 'en',
      });

      assert.strictEqual(result.comparison.canCompare, false);
      assert.ok(result.text.includes('cannot be computed at this time'));
    });

    it('allows comparison when both crossings are fresh within 60 minutes and open', async () => {
      const mockPort = new MockBorderCrossingPort();
      mockPort.setPdnOverrides({ delayMinutes: 10, freshness: 'fresh', portStatus: 'Open' });
      mockPort.setBotaOverrides({ delayMinutes: 35, freshness: 'fresh', portStatus: 'Open' });
      const borderUseCase = new GetBorderWaitTimesUseCase({ borderCrossingPort: mockPort });
      const mockMeta = new MockMetaModelAdapter();
      const conciergeUseCase = new AskBorderConciergeUseCase({
        getBorderWaitTimesUseCase: borderUseCase,
        metaModel: mockMeta,
      });

      const result = await conciergeUseCase.execute({
        message: 'Which crossing has the lower reported wait?',
        language: 'en',
      });

      assert.strictEqual(result.comparison.canCompare, true);
      assert.strictEqual(result.comparison.lowerWaitCrossingId, 'paso-del-norte');
      assert.strictEqual(result.comparison.differenceMinutes, 25);
      assert.strictEqual(result.authoritativeObservations.pasoDelNorte.freshness, 'fresh');
      assert.strictEqual(result.authoritativeObservations.bridgeOfTheAmericas.freshness, 'fresh');
      assert.ok(result.text.includes('25 minutes lower'));
      assert.ok(result.text.includes('snapshots'));
    });

    it('NEVER reports a closed crossing as having a zero-minute wait', async () => {
      const mockPort = new MockBorderCrossingPort();
      mockPort.setPdnOverrides({ portStatus: 'Closed', delayMinutes: null });
      const borderUseCase = new GetBorderWaitTimesUseCase({ borderCrossingPort: mockPort });
      const mockMeta = new MockMetaModelAdapter();
      const conciergeUseCase = new AskBorderConciergeUseCase({
        getBorderWaitTimesUseCase: borderUseCase,
        metaModel: mockMeta,
      });

      const result = await conciergeUseCase.execute({
        message: 'What is the wait time at Paso del Norte right now?',
        language: 'en',
      });

      assert.ok(result.text.includes('CLOSED by CBP'));
      assert.ok(result.text.includes('not assigned a zero-minute wait time'));
      assert.ok(!result.text.includes('0 minutes reported'));
    });
  });

  describe('3. Validation & Safe Error Handling', () => {
    it('throws InvalidConciergeMessageError on empty or whitespace query', async () => {
      const mockPort = new MockBorderCrossingPort();
      const borderUseCase = new GetBorderWaitTimesUseCase({ borderCrossingPort: mockPort });
      const conciergeUseCase = new AskBorderConciergeUseCase({ getBorderWaitTimesUseCase: borderUseCase });

      await assert.rejects(
        async () => {
          await conciergeUseCase.execute({ message: '   ' });
        },
        (err) => {
          assert.ok(err instanceof InvalidConciergeMessageError);
          assert.ok(err.message.includes('cannot be empty'));
          return true;
        }
      );
    });

    it('throws InvalidConciergeMessageError on non-string message', async () => {
      const mockPort = new MockBorderCrossingPort();
      const borderUseCase = new GetBorderWaitTimesUseCase({ borderCrossingPort: mockPort });
      const conciergeUseCase = new AskBorderConciergeUseCase({ getBorderWaitTimesUseCase: borderUseCase });

      await assert.rejects(
        async () => {
          await conciergeUseCase.execute({ message: 12345 as any });
        },
        (err) => {
          assert.ok(err instanceof InvalidConciergeMessageError);
          assert.ok(err.message.includes('must be a string'));
          return true;
        }
      );
    });

    it('throws InvalidConciergeMessageError on message exceeding 500 characters', async () => {
      const mockPort = new MockBorderCrossingPort();
      const borderUseCase = new GetBorderWaitTimesUseCase({ borderCrossingPort: mockPort });
      const conciergeUseCase = new AskBorderConciergeUseCase({ getBorderWaitTimesUseCase: borderUseCase });

      await assert.rejects(
        async () => {
          await conciergeUseCase.execute({ message: 'A'.repeat(501) });
        },
        (err) => {
          assert.ok(err instanceof InvalidConciergeMessageError);
          assert.ok(err.message.includes('500 characters'));
          return true;
        }
      );
    });
  });

  describe('4. Deterministic Fallback & Resilience', () => {
    it('falls back seamlessly to deterministic domain engine when model throws an error', async () => {
      const mockPort = new MockBorderCrossingPort();
      const borderUseCase = new GetBorderWaitTimesUseCase({ borderCrossingPort: mockPort });
      const failingMeta = new MockMetaModelAdapter();
      failingMeta.setShouldFail(true, new Error('Upstream timeout'));
      const conciergeUseCase = new AskBorderConciergeUseCase({
        getBorderWaitTimesUseCase: borderUseCase,
        metaModel: failingMeta,
      });

      const result = await conciergeUseCase.execute({
        message: 'Where can I see the official cameras?',
        language: 'en',
      });

      assert.strictEqual(result.provider, 'deterministic-fallback');
      assert.ok(result.text.includes('https://www.puentesfronterizos.gob.mx/camaras-en-vivo.php'));
      assert.ok(result.text.includes('I do not perform automated video analysis'));
    });

    it('operates deterministically when no Meta model is configured at all', async () => {
      const mockPort = new MockBorderCrossingPort();
      const borderUseCase = new GetBorderWaitTimesUseCase({ borderCrossingPort: mockPort });
      const conciergeUseCase = new AskBorderConciergeUseCase({
        getBorderWaitTimesUseCase: borderUseCase,
        metaModel: undefined,
      });

      const result = await conciergeUseCase.execute({
        message: '¿Cuál es el tiempo de espera en Paso del Norte?',
        language: 'es',
      });

      assert.strictEqual(result.provider, 'deterministic-fallback');
      assert.strictEqual(result.language, 'es');
      assert.ok(result.text.includes('20 minutos reportados'));
      assert.ok(result.text.includes('https://bwt.cbp.gov/details/06240202/PED'));
    });
  });
});
