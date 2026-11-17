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
      // 13:30:03 CST on Wednesday (20:30:03 UTC): Elena walking to Bridge Plaza
      const movingTime = '2026-11-04T20:30:03.000Z';
      const res = await fetch(`${url}/api/world/npcs/interaction-context?at=${encodeURIComponent(movingTime)}`);
      assert.strictEqual(res.status, 200);
      const data = (await res.json()) as any;

      const elena = data.npcs['elena-border-concierge'];
      assert.strictEqual(elena.movementState, 'moving');
      assert.strictEqual(elena.interactionAvailability, 'available_in_transit');
      assert.strictEqual(elena.interactionMode, 'in_transit');
      assert.strictEqual(elena.canInitiateConversation, true);
      assert.strictEqual(elena.isAvailableForNormalService, false);
      assert.strictEqual(elena.isMoving, true);
      assert.strictEqual(elena.destinationWaypointId, 'wp_bridge_plaza');
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

