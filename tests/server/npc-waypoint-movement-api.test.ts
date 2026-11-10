import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { InMemoryWorldCoreAdapter } from '../../src/adapters/in-memory-world-core-adapter.js';
import { MockMetaModelAdapter } from '../../src/adapters/mock-meta-model-adapter.js';
import { createAssistantServer } from '../../src/server/index.js';

describe('Deterministic NPC Waypoint Movement API Integration Tests', () => {
  it('1. GET /api/world/npcs/movement returns 200 with all 7 NPCs and valid schemas', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel });

    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      const res = await fetch(`${url}/api/world/npcs/movement`);
      assert.strictEqual(res.status, 200);
      const data = (await res.json()) as any;

      assert.strictEqual(data.success, true);
      assert.strictEqual(data.isDemonstrationMovement, true);
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

      // Also verify alias /api/npcs/movement
      const aliasRes = await fetch(`${url}/api/npcs/movement`);
      assert.strictEqual(aliasRes.status, 200);
      const aliasData = (await aliasRes.json()) as any;
      assert.strictEqual(aliasData.summary.totalCount, 7);
    } finally {
      await serverInstance.stop();
    }
  });

  it('2. GET /api/world/npcs/movement?at=<timestamp> evaluates deterministically at specified instant', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel });

    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      // 14:00:15 CST on Wednesday (21:00:15 UTC): Carlos is moving towards lunch
      const movingTime = '2026-11-04T21:00:15.000Z';
      const res = await fetch(`${url}/api/world/npcs/movement?at=${encodeURIComponent(movingTime)}`);
      assert.strictEqual(res.status, 200);
      const data = (await res.json()) as any;

      const carlos = data.npcs['carlos-farmacia-attendant'];
      assert.strictEqual(carlos.movementState, 'moving');
      assert.strictEqual(carlos.destinationWaypointId, 'wp_east_dining_benches');
      assert.ok(carlos.progression > 0 && carlos.progression < 1);
      assert.ok(carlos.position.z > 0 && carlos.position.z < 22);
    } finally {
      await serverInstance.stop();
    }
  });

  it('3. GET /api/world/npcs/:id/movement returns 200 for known NPC and 404 for unknown NPC', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel });

    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      // Known NPC
      const resKnown = await fetch(`${url}/api/world/npcs/elena-border-concierge/movement`);
      assert.strictEqual(resKnown.status, 200);
      const dataKnown = (await resKnown.json()) as any;
      assert.strictEqual(dataKnown.success, true);
      assert.strictEqual(dataKnown.movement.npcId, 'elena-border-concierge');
      assert.strictEqual(dataKnown.movement.canonicalName, 'Elena Ramos');
      assert.ok(dataKnown.movement.position);

      // Also alias
      const resAlias = await fetch(`${url}/api/npcs/elena-border-concierge/movement`);
      assert.strictEqual(resAlias.status, 200);

      // Unknown NPC returns 404
      const resUnknown = await fetch(`${url}/api/world/npcs/unknown-character-xyz/movement`);
      assert.strictEqual(resUnknown.status, 404);
      const dataUnknown = (await resUnknown.json()) as any;
      assert.strictEqual(dataUnknown.success, false);
      assert.strictEqual(dataUnknown.error.code, 'npc-not-found');
    } finally {
      await serverInstance.stop();
    }
  });

  it('4. Movement state and activity state remain strictly consistent across endpoints', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel });

    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      const atTime = '2026-11-04T18:00:00.000Z'; // 11:00 CST
      const [actRes, movRes] = await Promise.all([
        fetch(`${url}/api/world/npcs/activity?at=${encodeURIComponent(atTime)}`),
        fetch(`${url}/api/world/npcs/movement?at=${encodeURIComponent(atTime)}`),
      ]);

      assert.strictEqual(actRes.status, 200);
      assert.strictEqual(movRes.status, 200);

      const actData = (await actRes.json()) as any;
      const movData = (await movRes.json()) as any;

      for (const id of Object.keys(actData.npcs)) {
        assert.strictEqual(
          movData.npcs[id].activityState,
          actData.npcs[id].state,
          `Activity state mismatch for ${id}`
        );
      }
    } finally {
      await serverInstance.stop();
    }
  });
});

