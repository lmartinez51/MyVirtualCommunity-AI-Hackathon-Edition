import { describe, it } from 'node:test';
import assert from 'node:assert';
import { InMemoryWorldCoreAdapter } from '../../src/adapters/in-memory-world-core-adapter.js';
import { MockMetaModelAdapter } from '../../src/adapters/mock-meta-model-adapter.js';
import { createAssistantServer } from '../../src/server/index.js';

describe('Deterministic NPC Activity & Daily Routines API Integration Tests', () => {
  it('1. GET /api/world/npcs/activity returns 200 with all 7 NPCs and summary counts', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel });

    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      const res = await fetch(`${url}/api/world/npcs/activity`);
      assert.strictEqual(res.status, 200);
      const data = (await res.json()) as any;

      assert.strictEqual(data.success, true);
      assert.strictEqual(data.isDemonstrationSchedule, true);
      assert.ok(data.demonstrationNotice.en);
      assert.ok(data.demonstrationNotice.es);
      assert.strictEqual(data.timeZone, 'America/Ciudad_Juarez');
      assert.ok(data.evaluatedAt);
      assert.ok(data.localCivilTime);

      assert.strictEqual(data.summary.totalCount, 7);
      assert.ok(data.npcs['elena-border-concierge']);
      assert.ok(data.npcs['carlos-farmacia-attendant']);
      assert.ok(data.npcs['valeria-optica-attendant']);
      assert.ok(data.npcs['mateo-student']);
      assert.ok(data.npcs['sofia-cross-border-retail']);
      assert.ok(data.npcs['hector-artisan']);
      assert.ok(data.npcs['patricia-citizen-rep']);

      // Also verify alias /api/npcs/activity
      const aliasRes = await fetch(`${url}/api/npcs/activity`);
      assert.strictEqual(aliasRes.status, 200);
      const aliasData = (await aliasRes.json()) as any;
      assert.strictEqual(aliasData.summary.totalCount, 7);
    } finally {
      await serverInstance.stop();
    }
  });

  it('2. GET /api/world/npcs/activity?at=<timestamp> evaluates deterministically at specified instant', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel });

    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      // Sunday Nov 8, 2026 at 12:00 PM Juárez = 19:00 UTC
      const sundayTime = '2026-11-08T19:00:00.000Z';
      const res = await fetch(`${url}/api/world/npcs/activity?at=${encodeURIComponent(sundayTime)}`);
      assert.strictEqual(res.status, 200);
      const data = (await res.json()) as any;

      assert.strictEqual(data.success, true);
      assert.strictEqual(data.npcs['carlos-farmacia-attendant'].state, 'off_duty');
      assert.strictEqual(data.npcs['valeria-optica-attendant'].state, 'off_duty');
      assert.strictEqual(data.npcs['patricia-citizen-rep'].state, 'off_duty');
      assert.strictEqual(data.npcs['elena-border-concierge'].state, 'available');
    } finally {
      await serverInstance.stop();
    }
  });

  it('3. GET /api/world/npcs/:id/activity returns 200 for known NPC with localized snapshot', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel });

    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      // Wednesday Nov 4, 2026 at 13:30 Juárez = 20:30 UTC (Elena Handover)
      const handoverTime = '2026-11-04T20:30:00.000Z';
      const res = await fetch(
        `${url}/api/world/npcs/elena-border-concierge/activity?at=${encodeURIComponent(handoverTime)}`
      );
      assert.strictEqual(res.status, 200);
      const data = (await res.json()) as any;

      assert.strictEqual(data.success, true);
      assert.strictEqual(data.activity.npcId, 'elena-border-concierge');
      assert.strictEqual(data.activity.canonicalName, 'Elena Ramos');
      assert.strictEqual(data.activity.state, 'busy');
      assert.strictEqual(data.activity.canInteract, true);
      assert.strictEqual(data.activity.activityTitle.es, 'Relevo de turno y conciliación de bitácoras');
      assert.strictEqual(data.activity.currentLocation, 'infrastructure:puente-paso-del-norte');
    } finally {
      await serverInstance.stop();
    }
  });

  it('4. GET /api/world/npcs/:id/activity returns 404 for unknown NPC ID', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel });

    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      const res = await fetch(`${url}/api/world/npcs/unknown-npc-robot/activity`);
      assert.strictEqual(res.status, 404);
      const data = (await res.json()) as any;

      assert.strictEqual(data.success, false);
      assert.strictEqual(data.error.code, 'npc-not-found');
      assert.ok(data.error.message.includes('unknown-npc-robot'));
    } finally {
      await serverInstance.stop();
    }
  });
});

