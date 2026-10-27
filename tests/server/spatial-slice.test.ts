import assert from 'node:assert/strict';
import { afterEach, beforeEach, describe, it } from 'node:test';
import { InMemoryWorldCoreAdapter } from '../../src/adapters/in-memory-world-core-adapter.js';
import { MockMetaModelAdapter } from '../../src/adapters/mock-meta-model-adapter.js';
import {
  WorldCoreConnectionError
} from '../../src/adapters/remote-world-core-adapter.js';
import { createBusinessId } from '../../src/domain/business-id.js';
import type { WorldCorePort } from '../../src/ports/world-core-port.js';
import { createAssistantServer } from '../../src/server/index.js';

describe('Milestone 3: Spatial Slice v0.1 & Business Registry Tests', () => {
  let serverInstance: ReturnType<typeof createAssistantServer>;
  let mockMeta: MockMetaModelAdapter;
  let inMemoryWorldCore: InMemoryWorldCoreAdapter;
  let baseUrl: string;

  beforeEach(async () => {
    mockMeta = new MockMetaModelAdapter();
    inMemoryWorldCore = new InMemoryWorldCoreAdapter();

    serverInstance = createAssistantServer({
      worldCore: inMemoryWorldCore,
      metaModel: mockMeta,
    });

    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address();
    if (addr && typeof addr === 'object') {
      baseUrl = `http://127.0.0.1:${addr.port}`;
    } else {
      throw new Error('Server address unavailable');
    }
  });

  afterEach(async () => {
    if (serverInstance) {
      await serverInstance.stop();
    }
  });

  describe('1. Spatial Corridor Endpoint (GET /api/spatial/corridor)', () => {
    it('returns HTTP 200 with corridor definition and valid geographic coordinates', async () => {
      const res = await fetch(`${baseUrl}/api/spatial/corridor`);
      assert.strictEqual(res.status, 200);
      assert.match(res.headers.get('content-type') || '', /application\/json/);

      const data = (await res.json()) as any;
      assert.strictEqual(data.success, true);
      assert.ok(data.corridor);
      assert.strictEqual(data.corridor.id, 'corridor:juarez-paso-del-norte');
      assert.strictEqual(data.corridor.city, 'Ciudad Juárez');

      // Finite geographic coordinates
      assert.ok(Number.isFinite(data.corridor.center.latitude));
      assert.ok(Number.isFinite(data.corridor.center.longitude));
      assert.ok(data.corridor.center.latitude >= 31.74 && data.corridor.center.latitude <= 31.75);
      assert.ok(data.corridor.center.longitude >= -106.495 && data.corridor.center.longitude <= -106.48);

      // Exactly three designated landmarks
      assert.strictEqual(data.corridor.entities.length, 3);
      const bridge = data.corridor.entities.find((e: any) => e.id === 'infrastructure:puente-paso-del-norte');
      const pharmacy = data.corridor.entities.find((e: any) => e.id === 'pharmacy:farmacias-benavides');
      const optica = data.corridor.entities.find((e: any) => e.id === 'optical:optica-fashion');

      assert.ok(bridge, 'Bridge must be present in corridor entities');
      assert.strictEqual(bridge.entityType, 'infrastructure');
      assert.strictEqual(bridge.businessId, undefined);
      assert.strictEqual(bridge.canonicalName, 'Puente Internacional Paso del Norte');
      assert.strictEqual(bridge.popularName, 'Puente Santa Fe');

      assert.ok(pharmacy, 'Farmacias Benavides must be present');
      assert.strictEqual(pharmacy.entityType, 'pharmacy');
      assert.strictEqual(pharmacy.businessId, 'pharmacy:farmacias-benavides');
      assert.strictEqual(pharmacy.canonicalName, 'Farmacias Benavides');
      assert.strictEqual(pharmacy.coordinates.latitude, 31.745488);
      assert.strictEqual(pharmacy.coordinates.longitude, -106.486276);

      assert.ok(optica, 'Óptica Fashion must be present');
      assert.strictEqual(optica.entityType, 'optical');
      assert.strictEqual(optica.businessId, 'optical:optica-fashion');
      assert.strictEqual(optica.canonicalName, 'Óptica Fashion');
      assert.strictEqual(optica.coordinates.latitude, 31.745562);
      assert.strictEqual(optica.coordinates.longitude, -106.486492);
    });
  });

  describe('2. Authoritative Schedule Retrieval & Business Rule Verification', () => {
    it('retrieves Farmacias Benavides schedule with Mon-Sat 07:00-22:00, Sun closed, and 0 cutoff', async () => {
      const schedule = await inMemoryWorldCore.getBusinessSchedule(createBusinessId('pharmacy:farmacias-benavides'));
      assert.ok(schedule);
      assert.strictEqual(schedule?.name, 'Farmacias Benavides');
      assert.strictEqual(schedule?.timeZone, 'America/Ciudad_Juarez');
      assert.strictEqual(schedule?.lastEntryCutoffMinutes, 0, 'Generic pharmacy has 0 min cutoff, not restaurant 15 min');

      // Mon-Sat (days 1..6), Sunday (0) closed
      assert.deepStrictEqual(schedule?.operatingDays, [1, 2, 3, 4, 5, 6]);
      assert.strictEqual(schedule?.hours.openHour, 7);
      assert.strictEqual(schedule?.hours.openMinute, 0);
      assert.strictEqual(schedule?.hours.closeHour, 22);
      assert.strictEqual(schedule?.hours.closeMinute, 0);
    });

    it('evaluates Farmacias Benavides status: open & admitting before 22:00 on Saturday (no 15-min cutoff penalty)', async () => {
      // 2026-10-24 is Saturday. 21:50 local is 10 minutes before 22:00 close.
      // With 0 cutoff, admission is PERMITTED until 22:00.
      const status = await inMemoryWorldCore.getBusinessOperationalStatus(
        createBusinessId('pharmacy:farmacias-benavides'),
        '2026-10-25T03:50:00.000Z' // 21:50 MDT (UTC-6)
      );

      assert.ok(status);
      assert.strictEqual(status?.status, 'OPEN');
      assert.strictEqual(status?.admissionPermitted, true, 'Admission should be permitted at 21:50 when cutoff is 0');
      assert.strictEqual(status?.openTimeCivil, '07:00');
      assert.strictEqual(status?.closeTimeCivil, '22:00');
      assert.strictEqual(status?.cutoffTimeCivil, '22:00');
    });

    it('evaluates Farmacias Benavides status: closed on Sunday', async () => {
      // 2026-10-25 is Sunday. 12:00 local.
      const status = await inMemoryWorldCore.getBusinessOperationalStatus(
        createBusinessId('pharmacy:farmacias-benavides'),
        '2026-10-25T18:00:00.000Z' // 12:00 MDT
      );

      assert.ok(status);
      assert.strictEqual(status?.status, 'CLOSED');
      assert.strictEqual(status?.admissionPermitted, false);
    });

    it('retrieves Óptica Fashion schedule with Mon-Sat 09:00-17:00, Sun closed, and 0 cutoff', async () => {
      const schedule = await inMemoryWorldCore.getBusinessSchedule(createBusinessId('optical:optica-fashion'));
      assert.ok(schedule);
      assert.strictEqual(schedule?.name, 'Óptica Fashion');
      assert.strictEqual(schedule?.timeZone, 'America/Ciudad_Juarez');
      assert.strictEqual(schedule?.lastEntryCutoffMinutes, 0, 'Generic optical shop has 0 min cutoff, not restaurant 15 min');

      // Mon-Sat (days 1..6), Sunday (0) closed
      assert.deepStrictEqual(schedule?.operatingDays, [1, 2, 3, 4, 5, 6]);
      assert.strictEqual(schedule?.hours.openHour, 9);
      assert.strictEqual(schedule?.hours.openMinute, 0);
      assert.strictEqual(schedule?.hours.closeHour, 17);
      assert.strictEqual(schedule?.hours.closeMinute, 0);
    });

    it('evaluates Óptica Fashion status: open & admitting before 17:00 on Saturday (no 15-min cutoff penalty)', async () => {
      // 2026-10-24 is Saturday. 16:50 local is 10 minutes before 17:00 close.
      // With 0 cutoff, admission is PERMITTED until 17:00.
      const status = await inMemoryWorldCore.getBusinessOperationalStatus(
        createBusinessId('optical:optica-fashion'),
        '2026-10-24T22:50:00.000Z' // 16:50 MDT (UTC-6)
      );

      assert.ok(status);
      assert.strictEqual(status?.status, 'OPEN');
      assert.strictEqual(status?.admissionPermitted, true, 'Admission should be permitted at 16:50 when cutoff is 0');
      assert.strictEqual(status?.openTimeCivil, '09:00');
      assert.strictEqual(status?.closeTimeCivil, '17:00');
      assert.strictEqual(status?.cutoffTimeCivil, '17:00');
    });

    it('evaluates Óptica Fashion status: closed on Sunday', async () => {
      // 2026-10-25 is Sunday. 12:00 local.
      const status = await inMemoryWorldCore.getBusinessOperationalStatus(
        createBusinessId('optical:optica-fashion'),
        '2026-10-25T18:00:00.000Z'
      );

      assert.ok(status);
      assert.strictEqual(status?.status, 'CLOSED');
      assert.strictEqual(status?.admissionPermitted, false);
    });

    it('preserves existing Wing Spot Gómez Morín and Pastelerías La Tía Lupe records and 15-min cutoffs', async () => {
      const wingSpot = await inMemoryWorldCore.getBusinessSchedule(createBusinessId('restaurant:wing-spot'));
      assert.ok(wingSpot);
      assert.strictEqual(wingSpot?.name, 'Wing Spot Gómez Morín');
      assert.strictEqual(wingSpot?.lastEntryCutoffMinutes, 15);

      const tiaLupe = await inMemoryWorldCore.getBusinessSchedule(createBusinessId('restaurant:pasteleria-tia-lupe'));
      assert.ok(tiaLupe);
      assert.strictEqual(tiaLupe?.name, 'Pastelerías La Tía Lupe');
      assert.strictEqual(tiaLupe?.lastEntryCutoffMinutes, 15);
    });
  });

  describe('3. Assistant API Integration for New Corridor Businesses', () => {
    it('answers queries for Farmacias Benavides using authoritative World Core facts', async () => {
      const res = await fetch(`${baseUrl}/api/assistant/query`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          businessId: 'pharmacy:farmacias-benavides',
          question: 'What are the operating hours for Farmacias Benavides?',
          language: 'en',
        }),
      });

      assert.strictEqual(res.status, 200);
      const data = (await res.json()) as any;
      assert.strictEqual(data.success, true);
      assert.strictEqual(data.verifiedStatus.businessName, 'Farmacias Benavides');
      assert.strictEqual(data.schedule.name, 'Farmacias Benavides');
      assert.ok(data.modelUsed.includes('muse-spark-1.3'));
      assert.ok(data.answer.includes('Farmacias Benavides'));

      // Verify mock adapter received prompt grounded with Farmacias Benavides facts
      const ctx = mockMeta.lastContextReceived;
      assert.ok(ctx);
      assert.strictEqual(ctx?.verifiedData.businessName, 'Farmacias Benavides');
      assert.strictEqual(ctx?.schedule.name, 'Farmacias Benavides');
      assert.strictEqual(ctx?.schedule.hours.openHour, 7);
      assert.strictEqual(ctx?.schedule.hours.closeHour, 22);
    });

    it('answers queries for Óptica Fashion in Spanish preserving canonical name and accents', async () => {
      const res = await fetch(`${baseUrl}/api/assistant/query`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          businessId: 'optical:optica-fashion',
          question: '¿A qué hora cierra Óptica Fashion?',
          language: 'es',
        }),
      });

      assert.strictEqual(res.status, 200);
      const data = (await res.json()) as any;
      assert.strictEqual(data.success, true);
      assert.strictEqual(data.verifiedStatus.businessName, 'Óptica Fashion');
      assert.strictEqual(data.schedule.name, 'Óptica Fashion');
      assert.ok(data.answer.includes('Óptica Fashion'));

      // Verify Spanish instructions were sent to Muse Spark
      const ctx = mockMeta.lastContextReceived;
      assert.ok(ctx);
      assert.strictEqual(ctx?.verifiedData.businessName, 'Óptica Fashion');
      assert.strictEqual(ctx?.schedule.name, 'Óptica Fashion');
      assert.ok(ctx?.systemPrompt?.includes('español'));
    });

    it('evaluates time-specific admission for Farmacias Benavides at Saturday 21:50 local (open & admitting)', async () => {
      const res = await fetch(`${baseUrl}/api/assistant/query`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          businessId: 'pharmacy:farmacias-benavides',
          question: 'Would I still be admitted if I arrive at 9:50 PM on Saturday?',
          evaluationTime: '2026-10-24T21:50',
          language: 'en',
        }),
      });

      assert.strictEqual(res.status, 200);
      const data = (await res.json()) as any;
      assert.strictEqual(data.success, true);
      assert.strictEqual(data.evaluationMode, 'specific');
      assert.strictEqual(data.verifiedStatus.status, 'OPEN');
      assert.strictEqual(data.verifiedStatus.admissionPermitted, true);
      assert.strictEqual(data.verifiedStatus.cutoffTimeCivil, '22:00');
    });

    it('rejects query for infrastructure bridge as a business (non-commercial)', async () => {
      const res = await fetch(`${baseUrl}/api/assistant/query`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          businessId: 'infrastructure:puente-paso-del-norte',
          question: 'What are the operating hours of the bridge?',
          language: 'en',
        }),
      });

      // The bridge is not a commercial business in World Core
      assert.ok(res.status === 400 || res.status === 404);
      const data = (await res.json()) as any;
      assert.strictEqual(data.success, false);
      assert.strictEqual(mockMeta.callCount, 0, 'Must NOT invoke Meta for non-business entity');
    });

    it('handles World Core server failure gracefully without calling Meta API', async () => {
      const failingWorldCore: WorldCorePort = {
        async getBusinessSchedule() {
          throw new WorldCoreConnectionError('Connection refused to World Core');
        },
        async getCurrentWorldTime() {
          return new Date().toISOString();
        },
        async getBusinessOperationalStatus() {
          throw new WorldCoreConnectionError('Connection refused to World Core');
        },
        async getBusinessScheduleSnapshot() {
          throw new WorldCoreConnectionError('Connection refused to World Core');
        },
      };

      const failingServer = createAssistantServer({
        worldCore: failingWorldCore,
        metaModel: mockMeta,
      });

      await failingServer.start(0, '127.0.0.1');
      const addr = failingServer.server.address() as any;
      const failingBaseUrl = `http://127.0.0.1:${addr.port}`;

      try {
        const res = await fetch(`${failingBaseUrl}/api/assistant/query`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            businessId: 'pharmacy:farmacias-benavides',
            question: 'Is it open?',
            language: 'en',
          }),
        });

        assert.strictEqual(res.status, 502);
        const data = (await res.json()) as any;
        assert.strictEqual(data.success, false);
        assert.strictEqual(data.error.code, 'world-core-unavailable');
        assert.strictEqual(mockMeta.callCount, 0, 'Must NOT call Meta API when World Core fails');
      } finally {
        await failingServer.stop();
      }
    });
  });
});
