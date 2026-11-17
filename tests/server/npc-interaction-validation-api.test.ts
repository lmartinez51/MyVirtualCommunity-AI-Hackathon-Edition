import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { InMemoryWorldCoreAdapter } from '../../src/adapters/in-memory-world-core-adapter.js';
import { MockMetaModelAdapter } from '../../src/adapters/mock-meta-model-adapter.js';
import { createAssistantServer } from '../../src/server/index.js';
import { Vector3D } from '../../src/domain/npc-waypoint-movement.js';

/**
 * Helper to move the canonical player legitimately through World Core movement integration
 * using a deterministic test server clock (never setting arbitrary absolute coordinates).
 */
async function walkPlayerToTarget(
  worldCore: InMemoryWorldCoreAdapter,
  target: Vector3D,
  advanceClockMs: (deltaMs: number) => number,
  seqStart = 1
): Promise<number> {
  let seq = seqStart;
  for (let i = 0; i < 60; i++) {
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

describe('Server-Authoritative NPC Interaction Validation API Integration Tests', () => {
  const normalDutyUtc = '2026-11-04T17:00:00.000Z'; // Wed 10:00 CST
  const inTransitUtc = '2026-11-04T20:30:03.000Z';  // Wed 13:30:03 CST (Elena walking to bridge plaza)
  const offDutyUtc = '2026-11-05T06:00:00.000Z';    // Wed 23:00 CST
  const sundayClosedUtc = '2026-11-08T18:00:00.000Z'; // Sun 11:00 CST (Valeria off duty)

  it('1. Rejects out-of-range interaction even if client spoofs distance/coordinates/availability', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel });
    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      // Default player-1 spawns at (0, 1, -15), far from Elena at (19.2, 0.2, 39.5)
      const res = await fetch(`${url}/api/world/npcs/elena-border-concierge/interact?at=${encodeURIComponent(normalDutyUtc)}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          playerId: 'player-1',
          intent: 'talk',
          requestId: 'spoof-attempt-1',
          playerPosition: { x: 19.2, y: 0.2, z: 39.5 },
          npcPosition: { x: 0, y: 1, z: -15 },
          distanceMeters: 0.1,
          interactionAvailability: 'available_normal',
          canInteract: true,
        }),
      });

      assert.strictEqual(res.status, 200);
      const data = (await res.json()) as any;
      assert.strictEqual(data.success, true);
      assert.strictEqual(data.allowed, false);
      assert.strictEqual(data.reasonCode, 'out_of_range');
      assert.strictEqual(data.requestId, 'spoof-attempt-1');
      assert.ok(data.distanceMeters > 5.0);
    } finally {
      await serverInstance.stop();
    }
  });

  it('2. Accepts interaction after authoritative movement brings player within range of Elena', async () => {
    let clockMs = 1_700_000_000_000;
    const nowFn = () => clockMs;

    const worldCore = new InMemoryWorldCoreAdapter({ nowFn });
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel, nowFn });
    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      // Walk player via HTTP movement intents toward Elena's kiosk (19.2, 0.2, 39.5)
      let seq = 1;
      for (let i = 0; i < 30; i++) {
        const stateRes = await fetch(`${url}/api/world/players/player-1`);
        const stateJson = (await stateRes.json()) as any;
        const pos = stateJson.player.position;
        const dx = 19.2 - pos.x;
        const dz = 39.5 - pos.z;
        const dist = Math.hypot(dx, dz);
        if (dist <= 2.5) break;

        clockMs += 400;
        const moveRes = await fetch(`${url}/api/world/players/player-1/movement`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            direction: { x: dx / dist, z: dz / dist },
            movementMode: 'sprint',
            sequenceNumber: seq++,
          }),
        });
        assert.strictEqual(moveRes.status, 200);
      }

      const res = await fetch(`${url}/api/world/npcs/elena-border-concierge/interact?at=${encodeURIComponent(normalDutyUtc)}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          playerId: 'player-1',
          intent: 'talk',
          requestId: 'valid-1',
        }),
      });

      assert.strictEqual(res.status, 200);
      const data = (await res.json()) as any;
      assert.strictEqual(data.success, true);
      assert.strictEqual(data.allowed, true);
      assert.strictEqual(data.reasonCode, 'authorized_normal_service');
      assert.strictEqual(data.interactionAvailability, 'available_normal');
      assert.ok(data.distanceMeters <= 5.0);
      assert.ok(data.interactionContext);
      assert.strictEqual(data.interactionContext.npcId, 'elena-border-concierge');

      const validateRes = await fetch(`${url}/api/world/interactions/validate?at=${encodeURIComponent(normalDutyUtc)}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          playerId: 'player-1',
          npcId: 'elena-border-concierge',
          intent: 'service',
        }),
      });
      assert.strictEqual(validateRes.status, 200);
      const validateData = (await validateRes.json()) as any;
      assert.strictEqual(validateData.allowed, true);
    } finally {
      await serverInstance.stop();
    }
  });

  it('3. Rejects off_duty NPCs deterministically without mutating unrelated state', async () => {
    let clockMs = 1_700_000_000_000;
    const nowFn = () => clockMs;
    const advanceClockMs = (delta: number) => {
      clockMs += delta;
      return clockMs;
    };

    const worldCore = new InMemoryWorldCoreAdapter({ nowFn });
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel, nowFn });
    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      // Walk player near Carlos (10.5, 1.0, 0.0)
      let seq = await walkPlayerToTarget(worldCore, { x: 10.5, y: 1.0, z: 0.0 }, advanceClockMs);

      const barriersBefore = await worldCore.getBorderLaneBarriers();

      const offDutyRes = await fetch(`${url}/api/world/npcs/carlos-farmacia-attendant/interact?at=${encodeURIComponent(offDutyUtc)}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ playerId: 'player-1', intent: 'talk' }),
      });
      assert.strictEqual(offDutyRes.status, 200);
      const offDutyData = (await offDutyRes.json()) as any;
      assert.strictEqual(offDutyData.allowed, false);
      assert.strictEqual(offDutyData.reasonCode, 'npc_off_duty');

      // Walk player near Valeria (-10.5, 1.0, 1.5)
      await walkPlayerToTarget(worldCore, { x: -10.5, y: 1.0, z: 1.5 }, advanceClockMs, seq);

      const closedRes = await fetch(`${url}/api/world/npcs/valeria-optica-attendant/interact?at=${encodeURIComponent(sundayClosedUtc)}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ playerId: 'player-1', intent: 'talk' }),
      });
      assert.strictEqual(closedRes.status, 200);
      const closedData = (await closedRes.json()) as any;
      assert.strictEqual(closedData.allowed, false);
      assert.strictEqual(closedData.reasonCode, 'npc_off_duty');

      const barriersAfter = await worldCore.getBorderLaneBarriers();
      assert.deepStrictEqual(barriersAfter.lanes, barriersBefore.lanes);
    } finally {
      await serverInstance.stop();
    }
  });

  it('4. Handles in-transit NPC: allows talk when within range of moving position, rejects formal service', async () => {
    let clockMs = 1_700_000_000_000;
    const nowFn = () => clockMs;
    const advanceClockMs = (delta: number) => {
      clockMs += delta;
      return clockMs;
    };

    const worldCore = new InMemoryWorldCoreAdapter({ nowFn });
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel, nowFn });
    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      const mov = await worldCore.getNpcMovement('elena-border-concierge', inTransitUtc);
      assert.ok(mov);
      assert.strictEqual(mov.movementState, 'moving');

      await walkPlayerToTarget(worldCore, mov.position, advanceClockMs);

      const talkRes = await fetch(`${url}/api/npcs/elena-border-concierge/interact?at=${encodeURIComponent(inTransitUtc)}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ playerId: 'player-1', intent: 'talk' }),
      });
      assert.strictEqual(talkRes.status, 200);
      const talkData = (await talkRes.json()) as any;
      assert.strictEqual(talkData.allowed, true);
      assert.strictEqual(talkData.reasonCode, 'authorized_in_transit');

      const serviceRes = await fetch(`${url}/api/npcs/elena-border-concierge/interact?at=${encodeURIComponent(inTransitUtc)}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ playerId: 'player-1', intent: 'service' }),
      });
      assert.strictEqual(serviceRes.status, 200);
      const serviceData = (await serviceRes.json()) as any;
      assert.strictEqual(serviceData.allowed, false);
      assert.strictEqual(serviceData.reasonCode, 'npc_in_transit_service_paused');
    } finally {
      await serverInstance.stop();
    }
  });

  it('5. Returns 404 for unknown NPC/player and 400 for invalid intent or invalid ?at= timestamp', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel });
    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      const unknownNpcRes = await fetch(`${url}/api/world/npcs/ghost-npc/interact`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ playerId: 'player-1', intent: 'talk' }),
      });
      assert.strictEqual(unknownNpcRes.status, 404);
      const unknownNpcData = (await unknownNpcRes.json()) as any;
      assert.strictEqual(unknownNpcData.allowed, false);
      assert.strictEqual(unknownNpcData.reasonCode, 'npc_not_found');

      const unknownPlayerRes = await fetch(`${url}/api/world/npcs/elena-border-concierge/interact`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ playerId: 'non-existent-player', intent: 'talk' }),
      });
      assert.strictEqual(unknownPlayerRes.status, 404);
      const unknownPlayerData = (await unknownPlayerRes.json()) as any;
      assert.strictEqual(unknownPlayerData.allowed, false);
      assert.strictEqual(unknownPlayerData.reasonCode, 'player_not_found');

      const invalidIntentRes = await fetch(`${url}/api/world/npcs/elena-border-concierge/interact`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ playerId: 'player-1', intent: 'hack-the-planet' }),
      });
      assert.strictEqual(invalidIntentRes.status, 400);

      const invalidAtRes = await fetch(`${url}/api/world/npcs/elena-border-concierge/interact?at=not-a-date`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ playerId: 'player-1', intent: 'talk' }),
      });
      assert.strictEqual(invalidAtRes.status, 400);
    } finally {
      await serverInstance.stop();
    }
  });
});
