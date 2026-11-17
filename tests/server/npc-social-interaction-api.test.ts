import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import http from 'node:http';
import { InMemoryWorldCoreAdapter } from '../../src/adapters/in-memory-world-core-adapter.js';
import { RemoteWorldCoreAdapter } from '../../src/adapters/remote-world-core-adapter.js';
import { MockMetaModelAdapter } from '../../src/adapters/mock-meta-model-adapter.js';
import { createAssistantServer } from '../../src/server/index.js';
import {
  INITIATING_DURATION_SECONDS,
  evaluatePairInteractionWindow,
} from '../../src/domain/npc-social-interaction.js';

async function walkPlayerWithinRadius(
  worldCore: InMemoryWorldCoreAdapter,
  target: { x: number; y: number; z: number },
  advanceClockMs: (deltaMs: number) => number,
  seqStart = 1
): Promise<number> {
  let seq = seqStart;
  for (let i = 0; i < 250; i++) {
    const state = await worldCore.getPlayerState('player-1');
    assert.ok(state);
    const dx = target.x - state.position.x;
    const dz = target.z - state.position.z;
    const dist = Math.hypot(dx, dz);
    if (dist <= 1.5) {
      return seq;
    }
    const nowMs = advanceClockMs(400);
    await worldCore.applyPlayerMovement(
      'player-1',
      { direction: { x: dx / dist, z: dz / dist }, movementMode: 'sprint', sequenceNumber: seq++ },
      nowMs
    );
  }
  throw new Error('Failed to walk player within target radius within step budget.');
}

describe('Autonomous NPC Social Interaction Lifecycle API Integration (Increment 5.21.0)', () => {
  const daytimeBaseIso = '2026-11-04T17:00:00.000Z';

  it('1. GET /api/world/npcs/social-interactions and /:id/social-interaction expose deterministic lifecycle & embed in interaction-context', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel });

    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      const win = evaluatePairInteractionWindow(
        'hector-artisan',
        'sofia-cross-border-retail',
        Date.parse(daytimeBaseIso)
      );
      const engagedIso = new Date(win.startedAtMs + INITIATING_DURATION_SECONDS * 1000 + 1500).toISOString();
      const cooldownIso = new Date(win.expectedEndAtMs + 4000).toISOString();

      // Engaged snapshot
      const engagedRes = await fetch(
        `${url}/api/world/npcs/social-interactions?at=${encodeURIComponent(engagedIso)}`
      );
      assert.strictEqual(engagedRes.status, 200);
      const engagedData = (await engagedRes.json()) as any;
      assert.strictEqual(engagedData.success, true);
      assert.strictEqual(engagedData.summary.activeSessionCount, 1);
      assert.strictEqual(engagedData.sessions[0].phase, 'engaged');
      assert.deepStrictEqual(engagedData.sessions[0].participantNpcIds, [
        'hector-artisan',
        'sofia-cross-border-retail',
      ]);

      // Embedded in interaction-context roster (single-poll aggregation)
      const ctxRes = await fetch(
        `${url}/api/world/npcs/interaction-context?at=${encodeURIComponent(engagedIso)}`
      );
      assert.strictEqual(ctxRes.status, 200);
      const ctxData = (await ctxRes.json()) as any;
      assert.strictEqual(ctxData.success, true);
      assert.strictEqual(ctxData.activeSocialInteractions.length, 1);
      assert.strictEqual(ctxData.npcs['sofia-cross-border-retail'].socialInteraction.phase, 'engaged');
      assert.strictEqual(ctxData.npcs['sofia-cross-border-retail'].socialInteraction.inActiveInteraction, true);

      // Cooldown / empty active sessions check
      const emptyRes = await fetch(
        `${url}/api/world/npcs/social-interactions?at=${encodeURIComponent(cooldownIso)}`
      );
      assert.strictEqual(emptyRes.status, 200);
      const emptyData = (await emptyRes.json()) as any;
      assert.strictEqual(emptyData.success, true);
      assert.strictEqual(emptyData.sessions.length, 0);
      assert.strictEqual(emptyData.summary.activeSessionCount, 0);

      // Individual NPC endpoint
      const singleRes = await fetch(
        `${url}/api/world/npcs/hector-artisan/social-interaction?at=${encodeURIComponent(engagedIso)}`
      );
      assert.strictEqual(singleRes.status, 200);
      const singleData = (await singleRes.json()) as any;
      assert.strictEqual(singleData.success, true);
      assert.strictEqual(singleData.interaction.phase, 'engaged');
      assert.strictEqual(singleData.interaction.isWitness, true);

      // Unknown NPC -> 404
      const unknownRes = await fetch(`${url}/api/world/npcs/unknown-npc/social-interaction`);
      assert.strictEqual(unknownRes.status, 404);

      // Invalid ?at= -> 400 invalid-evaluation-time
      const badAtRes = await fetch(`${url}/api/world/npcs/social-interactions?at=not-a-timestamp`);
      assert.strictEqual(badAtRes.status, 400);
      const badAtData = (await badAtRes.json()) as any;
      assert.strictEqual(badAtData.error.code, 'invalid-evaluation-time');
    } finally {
      await serverInstance.stop();
    }
  });

  it('2. Authorized player interaction succeeds while NPCs socialize, and activePlayerInteractionNpcId supersedes cleanly', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel });

    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      const win = evaluatePairInteractionWindow(
        'hector-artisan',
        'sofia-cross-border-retail',
        Date.parse(daytimeBaseIso)
      );
      const engagedIso = new Date(win.startedAtMs + INITIATING_DURATION_SECONDS * 1000 + 2000).toISOString();

      // Walk player near Sofia
      const initialPlayer = await worldCore.getPlayerState('player-1');
      let clockMs = Math.max(Date.now(), initialPlayer?.lastMovementServerTimeMs ?? 0);
      const movRes = await fetch(`${url}/api/world/npcs/sofia-cross-border-retail/movement?at=${encodeURIComponent(engagedIso)}`);
      const movData = (await movRes.json()) as any;
      await walkPlayerWithinRadius(worldCore, movData.movement.position, (d) => (clockMs += d), 1);

      // Active social interaction NEVER blocks player /interact authorization
      const interactRes = await fetch(
        `${url}/api/world/npcs/sofia-cross-border-retail/interact?at=${encodeURIComponent(engagedIso)}`,
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ playerId: 'player-1', intent: 'talk', requestId: 'priority-test-1' }),
        }
      );
      assert.strictEqual(interactRes.status, 200);
      const interactData = (await interactRes.json()) as any;
      assert.strictEqual(interactData.allowed, true);

      // With activePlayerInteractionNpcId=sofia-cross-border-retail, Sofia yields optional social session
      const yieldedRes = await fetch(
        `${url}/api/world/npcs/social-interactions?at=${encodeURIComponent(engagedIso)}&activePlayerInteractionNpcId=sofia-cross-border-retail`
      );
      assert.strictEqual(yieldedRes.status, 200);
      const yieldedData = (await yieldedRes.json()) as any;
      assert.strictEqual(yieldedData.sessions.length, 0);
      assert.strictEqual(yieldedData.npcs['sofia-cross-border-retail'].supersededByPlayerPriority, true);
      assert.strictEqual(yieldedData.npcs['sofia-cross-border-retail'].inActiveInteraction, false);
    } finally {
      await serverInstance.stop();
    }
  });

  it('3. RemoteWorldCoreAdapter reports explicit 501 not-supported when remote server lacks social-interactions without fabricating sessions', async () => {
    // Minimal remote World Core stub without social-interactions route (returns 404)
    const remoteStub = http.createServer((req, res) => {
      if (req.url === '/api/time') {
        res.writeHead(200, { 'Content-Type': 'application/json' });
        res.end(JSON.stringify({ worldTime: daytimeBaseIso }));
        return;
      }
      res.writeHead(404, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ success: false, error: { code: 'not-found', message: 'Not implemented' } }));
    });
    await new Promise<void>((resolve) => remoteStub.listen(0, '127.0.0.1', resolve));
    const stubAddr = remoteStub.address() as any;

    const remoteWorldCore = new RemoteWorldCoreAdapter({ baseURL: `http://127.0.0.1:${stubAddr.port}` });
    const serverInstance = createAssistantServer({
      worldCore: remoteWorldCore,
      metaModel: new MockMetaModelAdapter(),
    });
    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      const res = await fetch(`${url}/api/world/npcs/social-interactions?at=${encodeURIComponent(daytimeBaseIso)}`);
      assert.strictEqual(res.status, 501);
      const data = (await res.json()) as any;
      assert.strictEqual(data.success, false);
      assert.strictEqual(data.error.code, 'not-supported');

      const singleRes = await fetch(
        `${url}/api/world/npcs/sofia-cross-border-retail/social-interaction?at=${encodeURIComponent(daytimeBaseIso)}`
      );
      assert.strictEqual(singleRes.status, 501);
      const singleData = (await singleRes.json()) as any;
      assert.strictEqual(singleData.error.code, 'not-supported');
    } finally {
      await serverInstance.stop();
      await new Promise<void>((resolve) => remoteStub.close(() => resolve()));
    }
  });
});
