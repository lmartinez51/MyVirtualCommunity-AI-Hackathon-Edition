import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { CbpBorderWaitTimeAdapter } from '../../src/adapters/cbp-border-wait-time-adapter.js';

describe('CbpBorderWaitTimeAdapter (Unit Tests with Mocked HTTP)', () => {
  const MOCK_CBP_FEED = [
    {
      port_number: '240201',
      border: 'Mexican Border',
      port_name: 'El Paso',
      crossing_name: 'Bridge of the Americas (BOTA)',
      hours: '24 hrs/day',
      date: '10/27/2026',
      time: '18:02:07',
      port_status: 'Open',
      pedestrian_lanes: {
        maximum_lanes: '4',
        standard_lanes: {
          update_time: 'At 6:00 pm MDT',
          operational_status: 'no delay',
          delay_minutes: '5',
          lanes_open: '1',
        },
      },
    },
    {
      port_number: '240202',
      border: 'Mexican Border',
      port_name: 'El Paso',
      crossing_name: 'Paso Del Norte (PDN)',
      hours: '24 hrs/day',
      date: '10/27/2026',
      time: '18:02:07',
      port_status: 'Open',
      pedestrian_lanes: {
        maximum_lanes: '14',
        standard_lanes: {
          update_time: 'At 6:00 pm MDT',
          operational_status: 'no delay',
          delay_minutes: '1',
          lanes_open: '2',
        },
      },
    },
  ];

  it('normalizes valid CBP pedestrian wait data for PDN and BOTA', async () => {
    const mockFetch = async () =>
      new Response(JSON.stringify(MOCK_CBP_FEED), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      });

    const adapter = new CbpBorderWaitTimeAdapter({ fetchFn: mockFetch as any });
    const { observations, fetchedAt } = await adapter.getPedestrianCrossings();

    assert.ok(fetchedAt);
    const { pasoDelNorte, bridgeOfTheAmericas } = observations;

    // Paso del Norte checks
    assert.strictEqual(pasoDelNorte.crossingId, 'paso-del-norte');
    assert.strictEqual(pasoDelNorte.portNumber, '240202');
    assert.strictEqual(pasoDelNorte.canonicalName, 'Paso del Norte (Santa Fe)');
    assert.strictEqual(pasoDelNorte.laneCategory, 'standard');
    assert.strictEqual(pasoDelNorte.laneCategoryName.en, 'Standard / General Pedestrian Lane');
    assert.strictEqual(pasoDelNorte.laneCategoryName.es, 'Carril Peatonal Estándar / General');
    assert.strictEqual(pasoDelNorte.delayMinutes, 1);
    assert.strictEqual(pasoDelNorte.lanesOpen, 2);
    assert.strictEqual(pasoDelNorte.maximumLanes, 14);
    assert.strictEqual(pasoDelNorte.operationalStatus, 'no delay');
    assert.strictEqual(pasoDelNorte.sourceObservationTime, 'At 6:00 pm MDT');
    assert.strictEqual(pasoDelNorte.sourceUrl, 'https://bwt.cbp.gov/details/06240202/PED');

    // Bridge of the Americas checks
    assert.strictEqual(bridgeOfTheAmericas.crossingId, 'bridge-of-the-americas');
    assert.strictEqual(bridgeOfTheAmericas.portNumber, '240201');
    assert.strictEqual(bridgeOfTheAmericas.canonicalName, 'Bridge of the Americas (BOTA / Puente Libre)');
    assert.strictEqual(bridgeOfTheAmericas.laneCategory, 'standard');
    assert.strictEqual(bridgeOfTheAmericas.laneCategoryName.en, 'Standard / General Pedestrian Lane');
    assert.strictEqual(bridgeOfTheAmericas.laneCategoryName.es, 'Carril Peatonal Estándar / General');
    assert.strictEqual(bridgeOfTheAmericas.delayMinutes, 5);
    assert.strictEqual(bridgeOfTheAmericas.lanesOpen, 1);
    assert.strictEqual(bridgeOfTheAmericas.maximumLanes, 4);
    assert.strictEqual(bridgeOfTheAmericas.sourceObservationTime, 'At 6:00 pm MDT');
    assert.strictEqual(bridgeOfTheAmericas.sourceUrl, 'https://bwt.cbp.gov/details/06240201/PED');
  });

  it('preserves source timestamps and parses ISO timestamps', async () => {
    const mockFetch = async () =>
      new Response(JSON.stringify(MOCK_CBP_FEED), { status: 200 });

    const adapter = new CbpBorderWaitTimeAdapter({ fetchFn: mockFetch as any });
    const { observations } = await adapter.getPedestrianCrossings();

    assert.strictEqual(observations.pasoDelNorte.sourceObservationTime, 'At 6:00 pm MDT');
    assert.ok(observations.pasoDelNorte.sourceObservationTimestamp);
    // 6:00 pm MDT on 10/27/2026 is 2026-10-28T00:00:00.000Z
    assert.strictEqual(
      observations.pasoDelNorte.sourceObservationTimestamp,
      '2026-10-28T00:00:00.000Z'
    );
  });

  it('handles absent source timestamp gracefully without fabricating current time', async () => {
    const feedWithoutTime = [
      {
        port_number: '240202',
        port_status: 'Open',
        pedestrian_lanes: {
          standard_lanes: {
            operational_status: 'no delay',
            delay_minutes: '5',
            lanes_open: '1',
          },
        },
      },
    ];

    const mockFetch = async () => new Response(JSON.stringify(feedWithoutTime), { status: 200 });
    const adapter = new CbpBorderWaitTimeAdapter({ fetchFn: mockFetch as any });
    const { observations } = await adapter.getPedestrianCrossings();

    assert.strictEqual(observations.pasoDelNorte.sourceObservationTime, null);
    assert.strictEqual(observations.pasoDelNorte.sourceObservationTimestamp, null);
    assert.strictEqual(observations.pasoDelNorte.freshness, 'stale', 'Absent timestamp must be marked stale');
  });

  it('handles pending, unavailable, and non-numeric wait values', async () => {
    const pendingFeed = [
      {
        port_number: '240202',
        port_status: 'Open',
        pedestrian_lanes: {
          standard_lanes: {
            update_time: 'At 6:00 pm MDT',
            operational_status: 'Update Pending',
            delay_minutes: '',
            lanes_open: '',
          },
        },
      },
      {
        port_number: '240201',
        port_status: 'Closed',
        date: '10/27/2026',
        time: '18:02:07',
        pedestrian_lanes: {
          standard_lanes: {
            update_time: 'At 6:00 pm MDT',
            operational_status: 'Lanes Closed',
            delay_minutes: '',
          },
        },
      },
    ];

    const mockFetch = async () => new Response(JSON.stringify(pendingFeed), { status: 200 });
    const adapter = new CbpBorderWaitTimeAdapter({ fetchFn: mockFetch as any });
    const { observations } = await adapter.getPedestrianCrossings();

    assert.strictEqual(observations.pasoDelNorte.freshness, 'pending');
    assert.strictEqual(observations.pasoDelNorte.delayMinutes, null);
    assert.strictEqual(observations.bridgeOfTheAmericas.portStatus, 'Closed');
    assert.strictEqual(observations.bridgeOfTheAmericas.delayMinutes, null);
    // Operational status (Closed) is preserved while data freshness evaluates source reporting freshness
    assert.strictEqual(observations.bridgeOfTheAmericas.freshness, 'fresh');
  });

  it('handles HTTP error without crashing, reporting unavailable observations', async () => {
    const mockFetch = async () => new Response('Internal Server Error', { status: 500, statusText: 'Server Error' });
    const adapter = new CbpBorderWaitTimeAdapter({ fetchFn: mockFetch as any });
    const { observations } = await adapter.getPedestrianCrossings();

    assert.strictEqual(observations.pasoDelNorte.freshness, 'unavailable');
    assert.strictEqual(observations.bridgeOfTheAmericas.freshness, 'unavailable');
    assert.ok(observations.pasoDelNorte.errorMessage?.includes('HTTP 500'));
  });

  it('handles request timeout without throwing, reporting unavailable observations', async () => {
    const mockFetch = async () => {
      const err = new Error('The operation was aborted');
      err.name = 'AbortError';
      throw err;
    };

    const adapter = new CbpBorderWaitTimeAdapter({ fetchFn: mockFetch as any, timeoutMs: 50 });
    const { observations } = await adapter.getPedestrianCrossings();

    assert.strictEqual(observations.pasoDelNorte.freshness, 'unavailable');
    assert.strictEqual(observations.bridgeOfTheAmericas.freshness, 'unavailable');
    assert.ok(observations.pasoDelNorte.errorMessage?.includes('aborted'));
  });

  it('returns valid observation when one crossing is present and the other is missing', async () => {
    const singleFeed = [
      {
        port_number: '240202', // PDN present
        date: '10/27/2026',
        port_status: 'Open',
        pedestrian_lanes: {
          standard_lanes: {
            update_time: 'At 6:00 pm MDT',
            delay_minutes: '3',
            lanes_open: '2',
            operational_status: 'no delay',
          },
        },
      },
      // BOTA missing
    ];

    const mockFetch = async () => new Response(JSON.stringify(singleFeed), { status: 200 });
    const adapter = new CbpBorderWaitTimeAdapter({ fetchFn: mockFetch as any });
    const { observations } = await adapter.getPedestrianCrossings();

    assert.strictEqual(observations.pasoDelNorte.freshness, 'fresh');
    assert.strictEqual(observations.pasoDelNorte.delayMinutes, 3);
    assert.strictEqual(observations.bridgeOfTheAmericas.freshness, 'unavailable');
    assert.ok(observations.bridgeOfTheAmericas.errorMessage?.includes('not found'));
  });

  it('honors bounded in-memory cache and allows bypassCache', async () => {
    let callCount = 0;
    const mockFetch = async () => {
      callCount++;
      return new Response(JSON.stringify(MOCK_CBP_FEED), { status: 200 });
    };

    const adapter = new CbpBorderWaitTimeAdapter({ fetchFn: mockFetch as any, cacheTtlMs: 5000 });

    // First call fetches from source
    await adapter.getPedestrianCrossings();
    assert.strictEqual(callCount, 1);

    // Second call within TTL returns cached result
    await adapter.getPedestrianCrossings();
    assert.strictEqual(callCount, 1);

    // Third call with bypassCache fetches fresh
    await adapter.getPedestrianCrossings(true);
    assert.strictEqual(callCount, 2);
  });
});
