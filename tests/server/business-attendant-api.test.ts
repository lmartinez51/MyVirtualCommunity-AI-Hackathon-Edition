import { describe, it } from 'node:test';
import assert from 'node:assert';
import { InMemoryWorldCoreAdapter } from '../../src/adapters/in-memory-world-core-adapter.js';
import { MockMetaModelAdapter } from '../../src/adapters/mock-meta-model-adapter.js';
import { createAssistantServer } from '../../src/server/index.js';
import { WorldCorePort } from '../../src/ports/world-core-port.js';

describe('Storefront Business Attendants API Integration Tests', () => {
  it('1. GET /api/businesses/attendants returns 200 with both attendants and canonical business metadata', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel });

    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      const res = await fetch(`${url}/api/businesses/attendants`);
      assert.strictEqual(res.status, 200);
      const data = (await res.json()) as any;

      assert.strictEqual(data.success, true);
      assert.strictEqual(data.total, 2);
      assert.strictEqual(data.attendants.length, 2);

      const carlos = data.attendants.find((a: any) => a.id === 'carlos-farmacia-attendant');
      const valeria = data.attendants.find((a: any) => a.id === 'valeria-optica-attendant');

      assert.ok(carlos, 'Carlos must exist in roster');
      assert.strictEqual(carlos.businessId, 'pharmacy:farmacias-benavides');
      assert.strictEqual(carlos.businessCanonicalName, 'Farmacias Benavides');
      assert.strictEqual(carlos.avatar, '💊');
      assert.notStrictEqual(carlos.avatar, '👨‍⚕️');
      assert.ok(carlos.verifiedAddress.includes('colonia Chamizal unverified'));
      assert.strictEqual(carlos.coordinates.latitude, 31.745488);
      assert.strictEqual(carlos.coordinates.longitude, -106.486276);
      assert.strictEqual(carlos.virtualMarker.en, 'Virtual attendant · AI simulation');
      assert.strictEqual(carlos.virtualMarker.es, 'Encargado virtual · Simulación de IA');
      assert.ok(carlos.suggestedPrompts.length >= 3);

      assert.ok(valeria, 'Valeria must exist in roster');
      assert.strictEqual(valeria.businessId, 'optical:optica-fashion');
      assert.strictEqual(valeria.businessCanonicalName, 'Óptica Fashion');

      // Also test alias endpoint /api/business-attendants
      const aliasRes = await fetch(`${url}/api/business-attendants`);
      assert.strictEqual(aliasRes.status, 200);
      const aliasData = (await aliasRes.json()) as any;
      assert.strictEqual(aliasData.total, 2);
    } finally {
      await serverInstance.stop();
    }
  });

  it('2. POST /api/businesses/attendants/chat accepts English question and grounds in World Core facts', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel });

    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      const res = await fetch(`${url}/api/businesses/attendants/chat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          attendantId: 'carlos-farmacia-attendant',
          message: 'What are the operating hours for Farmacias Benavides?',
          language: 'en',
        }),
      });

      assert.strictEqual(res.status, 200);
      const data = (await res.json()) as any;
      assert.strictEqual(data.success, true);
      assert.strictEqual(data.response.attendantId, 'carlos-farmacia-attendant');
      assert.strictEqual(data.response.businessCanonicalName, 'Farmacias Benavides');
      assert.strictEqual(data.response.language, 'en');
      assert.strictEqual(data.response.virtualDisclosure, 'Virtual attendant · AI simulation');
      assert.ok(data.response.text.includes('07:00 to 22:00'));

      const facts = data.response.authoritativeFacts;
      assert.strictEqual(facts.businessName, 'Farmacias Benavides');
      assert.strictEqual(facts.openTimeCivil, '07:00');
      assert.strictEqual(facts.closeTimeCivil, '22:00');
      assert.strictEqual(facts.timeZone, 'America/Ciudad_Juarez');
      assert.ok(facts.evaluatedAtCivil.includes('local time (Ciudad Juárez)'));
    } finally {
      await serverInstance.stop();
    }
  });

  it('3. POST /api/businesses/attendants/chat accepts Spanish question and responds in Spanish', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel });

    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      const res = await fetch(`${url}/api/businesses/attendants/chat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          attendantId: 'valeria-optica-attendant',
          message: '¿Cuáles son los horarios de atención de Óptica Fashion?',
          language: 'es',
        }),
      });

      assert.strictEqual(res.status, 200);
      const data = (await res.json()) as any;
      assert.strictEqual(data.success, true);
      assert.strictEqual(data.response.attendantId, 'valeria-optica-attendant');
      assert.strictEqual(data.response.businessCanonicalName, 'Óptica Fashion');
      assert.strictEqual(data.response.language, 'es');
      assert.strictEqual(data.response.virtualDisclosure, 'Encargado virtual · Simulación de IA');
      assert.ok(data.response.text.includes('09:00 a 17:00'));
      assert.ok(data.response.authoritativeFacts.evaluatedAtCivil.includes('hora local de Ciudad Juárez'));
    } finally {
      await serverInstance.stop();
    }
  });

  it('4. Rejects unknown attendant ID with 404', async () => {
    const serverInstance = createAssistantServer();
    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      const res = await fetch(`${url}/api/businesses/attendants/chat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          attendantId: 'unknown-npc',
          message: 'Hello',
        }),
      });

      assert.strictEqual(res.status, 404);
      const data = (await res.json()) as any;
      assert.strictEqual(data.success, false);
      assert.strictEqual(data.error.code, 'attendant-not-found');
    } finally {
      await serverInstance.stop();
    }
  });

  it('5. Rejects missing attendantId with 400', async () => {
    const serverInstance = createAssistantServer();
    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      const res = await fetch(`${url}/api/businesses/attendants/chat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          message: 'Hello',
        }),
      });

      assert.strictEqual(res.status, 400);
      const data = (await res.json()) as any;
      assert.strictEqual(data.error.code, 'missing-attendant-id');
    } finally {
      await serverInstance.stop();
    }
  });

  it('6. Rejects empty message with 400', async () => {
    const serverInstance = createAssistantServer();
    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      const res = await fetch(`${url}/api/businesses/attendants/chat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          attendantId: 'carlos-farmacia-attendant',
          message: '   ',
        }),
      });

      assert.strictEqual(res.status, 400);
      const data = (await res.json()) as any;
      assert.strictEqual(data.error.code, 'invalid-message');
    } finally {
      await serverInstance.stop();
    }
  });

  it('7. Rejects message exceeding 500 characters with 400', async () => {
    const serverInstance = createAssistantServer();
    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      const res = await fetch(`${url}/api/businesses/attendants/chat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          attendantId: 'carlos-farmacia-attendant',
          message: 'Z'.repeat(501),
        }),
      });

      assert.strictEqual(res.status, 400);
      const data = (await res.json()) as any;
      assert.strictEqual(data.error.code, 'message-too-long');
    } finally {
      await serverInstance.stop();
    }
  });

  it('8. Rejects invalid language with 400', async () => {
    const serverInstance = createAssistantServer();
    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      const res = await fetch(`${url}/api/businesses/attendants/chat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          attendantId: 'carlos-farmacia-attendant',
          message: 'Hello',
          language: 'de',
        }),
      });

      assert.strictEqual(res.status, 400);
      const data = (await res.json()) as any;
      assert.strictEqual(data.error.code, 'invalid-language');
    } finally {
      await serverInstance.stop();
    }
  });

  it('9. Enforces boundary protections against inventory, medical, pricing, and appointments', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel });

    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      // Carlos - inventory
      const res1 = await fetch(`${url}/api/businesses/attendants/chat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          attendantId: 'carlos-farmacia-attendant',
          message: 'Do you have antibiotics in stock right now?',
          language: 'en',
        }),
      });
      const data1 = (await res1.json()) as any;
      assert.ok(data1.response.text.includes('do not have access to live medication inventory or pricing'));
      assert.ok(data1.response.authoritativeFacts.unavailableFactsNotice.includes('Live product inventory'));

      // Carlos - medical diagnosis
      const res2 = await fetch(`${url}/api/businesses/attendants/chat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          attendantId: 'carlos-farmacia-attendant',
          message: 'I have stomach pain and need a medical prescription',
          language: 'en',
        }),
      });
      const data2 = (await res2.json()) as any;
      assert.ok(data2.response.text.includes('not authorized to provide medical advice'));

      // Valeria - appointments
      const res3 = await fetch(`${url}/api/businesses/attendants/chat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          attendantId: 'valeria-optica-attendant',
          message: 'Can I book an eye exam appointment for tomorrow?',
          language: 'en',
        }),
      });
      const data3 = (await res3.json()) as any;
      assert.ok(data3.response.text.includes('do not include an appointment scheduling system'));
    } finally {
      await serverInstance.stop();
    }
  });

  it('10. Ignores client-supplied spoofed business profiles, schedules, or facts', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel });

    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      const spoofedPayload = {
        attendantId: 'carlos-farmacia-attendant',
        message: 'Where are you located?',
        businessId: 'restaurant:wing-spot', // Spoofed business ID
        businessCanonicalName: 'Spoofed Pharmacy', // Spoofed name
        verifiedAddress: '123 Fake Street', // Spoofed address
      };

      const res = await fetch(`${url}/api/businesses/attendants/chat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(spoofedPayload),
      });

      assert.strictEqual(res.status, 200);
      const data = (await res.json()) as any;

      // Invariant: Server strictly used authoritative data for Farmacias Benavides
      assert.strictEqual(data.response.businessId, 'pharmacy:farmacias-benavides');
      assert.strictEqual(data.response.businessCanonicalName, 'Farmacias Benavides');
      assert.strictEqual(
        data.response.authoritativeFacts.address,
        'Av. Benito Juárez 610, Centro (colonia Chamizal unverified), 32000 Juárez, Chih., México'
      );
      assert.ok(!data.response.text.includes('123 Fake Street'));
    } finally {
      await serverInstance.stop();
    }
  });

  it('11. Returns 502 when World Core business data is unavailable and does not fabricate confirmation', async () => {
    const brokenWorldCore: WorldCorePort = {
      async getBusinessSchedule() {
        return null;
      },
      async getCurrentWorldTime() {
        return '2026-10-28T18:00:00Z';
      },
      async getBusinessOperationalStatus() {
        return null;
      },
      async getBusinessScheduleSnapshot() {
        return null;
      },
    };
    const serverInstance = createAssistantServer({ worldCore: brokenWorldCore });

    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      const res = await fetch(`${url}/api/businesses/attendants/chat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          attendantId: 'carlos-farmacia-attendant',
          message: 'Are you open right now?',
        }),
      });

      assert.strictEqual(res.status, 502);
      const data = (await res.json()) as any;
      assert.strictEqual(data.success, false);
      assert.strictEqual(data.error.code, 'business-source-unavailable');
    } finally {
      await serverInstance.stop();
    }
  });
});

