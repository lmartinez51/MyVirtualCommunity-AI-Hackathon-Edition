import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { InMemoryWorldCoreAdapter } from '../../src/adapters/in-memory-world-core-adapter.js';
import { MockMetaModelAdapter } from '../../src/adapters/mock-meta-model-adapter.js';
import { createAssistantServer } from '../../src/server/index.js';

describe('Deterministic NPC Interaction Context API Integration Tests', () => {
  it('1. GET /api/world/npcs/interaction-context returns 200 with all 7 NPCs and summary', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel });

    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      const res = await fetch(`${url}/api/world/npcs/interaction-context`);
      assert.strictEqual(res.status, 200);
      const data = (await res.json()) as any;

      assert.strictEqual(data.success, true);
      assert.strictEqual(data.isDemonstrationContext, true);
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

      // Also verify alias /api/npcs/interaction-context
      const aliasRes = await fetch(`${url}/api/npcs/interaction-context`);
      assert.strictEqual(aliasRes.status, 200);
      const aliasData = (await aliasRes.json()) as any;
      assert.strictEqual(aliasData.summary.totalCount, 7);
    } finally {
      await serverInstance.stop();
    }
  });

  it('2. GET /api/world/npcs/interaction-context?at=<timestamp> evaluates deterministically at specified instant', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel });

    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      // 14:00:15 CST on Wednesday (21:00:15 UTC): Carlos walking to lunch
      const movingTime = '2026-11-04T21:00:15.000Z';
      const res = await fetch(`${url}/api/world/npcs/interaction-context?at=${encodeURIComponent(movingTime)}`);
      assert.strictEqual(res.status, 200);
      const data = (await res.json()) as any;

      const carlos = data.npcs['carlos-farmacia-attendant'];
      assert.strictEqual(carlos.movementState, 'moving');
      assert.strictEqual(carlos.interactionAvailability, 'available_in_transit');
      assert.strictEqual(carlos.interactionMode, 'in_transit');
      assert.strictEqual(carlos.canInitiateConversation, true);
      assert.strictEqual(carlos.isAvailableForNormalService, false);
      assert.strictEqual(carlos.isMoving, true);
      assert.strictEqual(carlos.destinationWaypointId, 'wp_east_dining_benches');
    } finally {
      await serverInstance.stop();
    }
  });

  it('3. GET /api/world/npcs/:id/interaction-context returns 200 for known NPC and 404 for unknown NPC', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel });

    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      // Known NPC
      const resKnown = await fetch(`${url}/api/world/npcs/elena-border-concierge/interaction-context`);
      assert.strictEqual(resKnown.status, 200);
      const dataKnown = (await resKnown.json()) as any;
      assert.strictEqual(dataKnown.success, true);
      assert.strictEqual(dataKnown.context.npcId, 'elena-border-concierge');
      assert.strictEqual(dataKnown.context.canonicalName, 'Elena Ramos');
      assert.ok(dataKnown.context.interactionAvailability);
      assert.ok(dataKnown.context.contextualGreeting.en);

      // Also alias
      const resAlias = await fetch(`${url}/api/npcs/elena-border-concierge/interaction-context`);
      assert.strictEqual(resAlias.status, 200);

      // Unknown NPC returns 404
      const resUnknown = await fetch(`${url}/api/world/npcs/unknown-character-xyz/interaction-context`);
      assert.strictEqual(resUnknown.status, 404);
      const dataUnknown = (await resUnknown.json()) as any;
      assert.strictEqual(dataUnknown.success, false);
      assert.strictEqual(dataUnknown.error.code, 'npc-not-found');
    } finally {
      await serverInstance.stop();
    }
  });
});

