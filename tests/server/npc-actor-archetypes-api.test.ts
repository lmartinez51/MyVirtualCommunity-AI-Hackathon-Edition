import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { InMemoryWorldCoreAdapter } from '../../src/adapters/in-memory-world-core-adapter.js';
import { MockMetaModelAdapter } from '../../src/adapters/mock-meta-model-adapter.js';
import { createAssistantServer } from '../../src/server/index.js';
import {
  BUSINESS_ATTENDANT_IDS,
  COMMUNITY_PLAYER_PROXY_IDS,
  PUBLIC_SERVANT_IDS,
} from '../../src/domain/npc-actor-archetypes.js';

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

describe('Increment 5.20.0.2: NPC Actor Archetypes & Witness Classification API Integration', () => {
  it('1. Propagates actorArchetype & witness fields across /activity, /interaction-context, /social-awareness, and /movement', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel });

    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const baseUrl = `http://127.0.0.1:${addr.port}`;

    // Sunday 2026-11-08 02:30 MST = 09:30 UTC (late night Sunday: ALL business_attendants and public_servants are off_duty)
    const sundayNightUtc = '2026-11-08T09:30:00.000Z';

    try {
      const [actRes, ctxRes, awrRes, movRes, witRes] = await Promise.all([
        fetch(`${baseUrl}/api/world/npcs/activity?at=${encodeURIComponent(sundayNightUtc)}`),
        fetch(`${baseUrl}/api/world/npcs/interaction-context?at=${encodeURIComponent(sundayNightUtc)}`),
        fetch(`${baseUrl}/api/world/npcs/social-awareness?at=${encodeURIComponent(sundayNightUtc)}`),
        fetch(`${baseUrl}/api/world/npcs/movement?at=${encodeURIComponent(sundayNightUtc)}`),
        fetch(`${baseUrl}/api/border/witnesses`),
      ]);

      assert.strictEqual(actRes.status, 200);
      assert.strictEqual(ctxRes.status, 200);
      assert.strictEqual(awrRes.status, 200);
      assert.strictEqual(movRes.status, 200);
      assert.strictEqual(witRes.status, 200);

      const actData = (await actRes.json()) as any;
      const ctxData = (await ctxRes.json()) as any;
      const awrData = (await awrRes.json()) as any;
      const movData = (await movRes.json()) as any;
      const witData = (await witRes.json()) as any;

      for (const id of COMMUNITY_PLAYER_PROXY_IDS) {
        const act = actData.npcs[id];
        const ctx = ctxData.npcs[id];
        const awr = awrData.npcs[id];
        const mov = movData.npcs[id];

        assert.strictEqual(act.actorArchetype, 'community_player_proxy');
        assert.strictEqual(act.hasEmploymentSchedule, false);
        assert.strictEqual(act.canEnterOffDuty, false);
        assert.strictEqual(act.isWitness, true);
        assert.strictEqual(act.witnessClassification, 'community_queue_witness');
        assert.strictEqual(act.witnessId, id);
        assert.notStrictEqual(act.state, 'off_duty');
        assert.notStrictEqual(act.state, 'unavailable');

        assert.strictEqual(ctx.actorArchetype, 'community_player_proxy');
        assert.strictEqual(ctx.canEnterOffDuty, false);
        assert.strictEqual(ctx.isWitness, true);
        assert.strictEqual(ctx.interactionAvailability, 'available_normal');
        assert.strictEqual(ctx.interactionMode, 'normal_service');
        assert.strictEqual(ctx.socialAwareness.actorArchetype, 'community_player_proxy');
        assert.strictEqual(ctx.socialAwareness.isWitness, true);

        assert.strictEqual(awr.actorArchetype, 'community_player_proxy');
        assert.strictEqual(awr.isWitness, true);
        assert.strictEqual(awr.canOrientTowardTarget, true);

        assert.strictEqual(mov.actorArchetype, 'community_player_proxy');
        assert.strictEqual(mov.isWitness, true);
      }

      // Queue pair active even during late Sunday night
      assert.strictEqual(awrData.npcs['sofia-cross-border-retail'].awarenessState, 'social_engaged');
      assert.strictEqual(awrData.npcs['hector-artisan'].awarenessState, 'social_engaged');

      // Staff remain off_duty during late Sunday night
      for (const id of [...BUSINESS_ATTENDANT_IDS, ...PUBLIC_SERVANT_IDS]) {
        const expectedArchetype = BUSINESS_ATTENDANT_IDS.includes(id)
          ? 'business_attendant'
          : 'public_servant';
        assert.strictEqual(actData.npcs[id].actorArchetype, expectedArchetype);
        assert.strictEqual(actData.npcs[id].hasEmploymentSchedule, true);
        assert.strictEqual(actData.npcs[id].canEnterOffDuty, true);
        assert.strictEqual(actData.npcs[id].isWitness, false);
        assert.strictEqual(actData.npcs[id].state, 'off_duty');
        assert.strictEqual(ctxData.npcs[id].interactionAvailability, 'off_duty');
        assert.strictEqual(awrData.npcs[id].awarenessState, 'unaware');
        assert.strictEqual(awrData.npcs[id].canOrientTowardTarget, false);
      }

      // Witnesses roster also surfaces explicit community_player_proxy classification
      assert.strictEqual(witData.witnesses.length, 3);
      for (const w of witData.witnesses) {
        assert.strictEqual(w.actorArchetype, 'community_player_proxy');
        assert.strictEqual(w.isWitness, true);
        assert.strictEqual(w.witnessClassification, 'community_queue_witness');
      }
    } finally {
      await serverInstance.stop();
    }
  });

  it('2. Interaction validation endpoint authorizes Community Player Proxies at 23:59 while rejecting off_duty staff', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel });

    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const baseUrl = `http://127.0.0.1:${addr.port}`;

    const lateNightUtc = '2026-11-05T06:59:00.000Z'; // Wed 23:59 MST

    try {
      const initialPlayer = await worldCore.getPlayerState('player-1');
      let clockMs = Math.max(Date.now(), initialPlayer?.lastMovementServerTimeMs ?? 0);
      const advanceClock = (deltaMs: number) => {
        clockMs += deltaMs;
        return clockMs;
      };

      const mateoMovRes = await fetch(`${baseUrl}/api/world/npcs/mateo-student/movement?at=${encodeURIComponent(lateNightUtc)}`);
      assert.strictEqual(mateoMovRes.status, 200);
      const mateoMovData = (await mateoMovRes.json()) as any;
      let seq = await walkPlayerWithinRadius(worldCore, mateoMovData.movement.position, advanceClock, 1);

      const mateoRes = await fetch(
        `${baseUrl}/api/world/npcs/mateo-student/interact?at=${encodeURIComponent(lateNightUtc)}`,
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ playerId: 'player-1', intent: 'talk', requestId: 'api-mateo-late' }),
        }
      );
      assert.strictEqual(mateoRes.status, 200);
      const mateoData = (await mateoRes.json()) as any;
      assert.strictEqual(mateoData.allowed, true);
      assert.strictEqual(mateoData.reasonCode, 'authorized_normal_service');
      assert.strictEqual(mateoData.actorArchetype, 'community_player_proxy');
      assert.strictEqual(mateoData.isWitness, true);
      assert.strictEqual(mateoData.witnessId, 'mateo-student');

      // Walk authoritative player right next to Carlos's position
      const carlosMovRes = await fetch(`${baseUrl}/api/world/npcs/carlos-farmacia-attendant/movement?at=${encodeURIComponent(lateNightUtc)}`);
      assert.strictEqual(carlosMovRes.status, 200);
      const carlosMovData = (await carlosMovRes.json()) as any;
      await walkPlayerWithinRadius(worldCore, carlosMovData.movement.position, advanceClock, seq);

      const carlosRes = await fetch(
        `${baseUrl}/api/world/npcs/carlos-farmacia-attendant/interact?at=${encodeURIComponent(lateNightUtc)}`,
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ playerId: 'player-1', intent: 'talk', requestId: 'api-carlos-late' }),
        }
      );
      assert.strictEqual(carlosRes.status, 200);
      const carlosData = (await carlosRes.json()) as any;
      assert.strictEqual(carlosData.allowed, false);
      assert.strictEqual(carlosData.reasonCode, 'npc_off_duty');
      assert.strictEqual(carlosData.actorArchetype, 'business_attendant');
      assert.strictEqual(carlosData.isWitness, false);
    } finally {
      await serverInstance.stop();
    }
  });
});
