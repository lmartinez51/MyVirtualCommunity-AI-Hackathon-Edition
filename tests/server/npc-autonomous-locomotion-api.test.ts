import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { InMemoryWorldCoreAdapter } from '../../src/adapters/in-memory-world-core-adapter.js';
import { RemoteWorldCoreAdapter, WorldCoreHttpError } from '../../src/adapters/remote-world-core-adapter.js';
import { MockMetaModelAdapter } from '../../src/adapters/mock-meta-model-adapter.js';
import { createAssistantServer } from '../../src/server/index.js';
import {
  FARMACIA_BENAVIDES_AUTONOMY_DOMAIN,
  OPTICA_FASHION_AUTONOMY_DOMAIN,
  isPointInBridgePatrolDomain,
  isPointInBusinessDomain,
} from '../../src/domain/npc-autonomous-locomotion.js';

describe('Autonomous NPC Locomotion, Bridge Patrols & Bounded Business Activity API Tests (Increment 5.22.0)', () => {
  it('1. GET /api/world/npcs/movement exposes autonomous behavior states, patrol directions, and business domain metadata', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel });
    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      const patrolIso = '2026-11-04T17:12:30.000Z'; // 10:12:30 MST
      const res = await fetch(`${url}/api/world/npcs/movement?at=${encodeURIComponent(patrolIso)}`);
      assert.strictEqual(res.status, 200);
      const data = (await res.json()) as any;
      assert.strictEqual(data.success, true);

      for (const proxyId of ['mateo-student', 'sofia-cross-border-retail', 'hector-artisan']) {
        const m = data.npcs[proxyId];
        assert.strictEqual(m.behaviorProfileType, 'bridge_patrol');
        assert.strictEqual(m.actorArchetype, 'community_player_proxy');
        assert.strictEqual(m.isWitness, true);
        assert.strictEqual(m.withinAuthorizedDomain, true);
        assert.ok(isPointInBridgePatrolDomain(m.position));
        assert.ok(m.patrolDirection === 'northbound' || m.patrolDirection === 'southbound');
      }

      const carlos = data.npcs['carlos-farmacia-attendant'];
      assert.strictEqual(carlos.behaviorProfileType, 'business_local_autonomy');
      assert.strictEqual(carlos.businessDomainId, 'domain:farmacias-benavides');
      assert.strictEqual(carlos.withinAuthorizedDomain, true);
      assert.ok(isPointInBusinessDomain(carlos.position, FARMACIA_BENAVIDES_AUTONOMY_DOMAIN));

      const valeria = data.npcs['valeria-optica-attendant'];
      assert.strictEqual(valeria.behaviorProfileType, 'business_local_autonomy');
      assert.strictEqual(valeria.businessDomainId, 'domain:optica-fashion');
      assert.strictEqual(valeria.withinAuthorizedDomain, true);
      assert.ok(isPointInBusinessDomain(valeria.position, OPTICA_FASHION_AUTONOMY_DOMAIN));

      const elena = data.npcs['elena-border-concierge'];
      assert.strictEqual(elena.behaviorProfileType, 'fixed_anchor_scheduled');
      assert.strictEqual(elena.autonomousBehaviorState, 'fixed_station');
      assert.deepStrictEqual(elena.position, { x: 19.2, y: 0.2, z: 39.5 });
    } finally {
      await serverInstance.stop();
    }
  });

  it('2. POST /api/world/interactions/validate suspends patrol & POST /api/world/interactions/end resumes without position jump + adapter state reconstruction', async () => {
    const startIso = '2026-11-04T17:13:55.000Z';
    const startMs = Date.parse(startIso);
    let clockMs = startMs;
    const worldCore = new InMemoryWorldCoreAdapter({ nowFn: () => clockMs });
    worldCore.setSimulatedWorldTime(startIso);
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel, nowFn: () => clockMs });
    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      const sofiaBeforeRes = await fetch(
        `${url}/api/world/npcs/sofia-cross-border-retail/movement?at=${encodeURIComponent(startIso)}`
      );
      const sofiaBefore = ((await sofiaBeforeRes.json()) as any).movement;
      assert.strictEqual(sofiaBefore.movementState, 'moving');

      // Walk player to Sofia
      for (let seq = 1; seq <= 200; seq++) {
        const p = (await worldCore.getPlayerState('player-1'))!;
        const dx = sofiaBefore.position.x - p.position.x;
        const dz = sofiaBefore.position.z - p.position.z;
        const d = Math.hypot(dx, dz);
        if (d <= 1.8) break;
        clockMs += 400;
        await worldCore.applyPlayerMovement(
          'player-1',
          { direction: { x: dx / d, z: dz / d }, movementMode: 'sprint', sequenceNumber: seq },
          clockMs
        );
      }

      // Validate interaction -> suspends Sofia's bridge patrol
      const valRes = await fetch(
        `${url}/api/world/interactions/validate?at=${encodeURIComponent(startIso)}`,
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            playerId: 'player-1',
            npcId: 'sofia-cross-border-retail',
            intent: 'talk',
          }),
        }
      );
      assert.strictEqual(valRes.status, 200);
      const valData = (await valRes.json()) as any;
      assert.strictEqual(valData.allowed, true);

      // 45 seconds into conversation: Sofia remains paused at exact same position
      const midChatIso = new Date(startMs + 45_000).toISOString();
      const midMovRes = await fetch(
        `${url}/api/world/npcs/sofia-cross-border-retail/movement?at=${encodeURIComponent(midChatIso)}`
      );
      const midMov = ((await midMovRes.json()) as any).movement;
      assert.strictEqual(midMov.movementState, 'stationary');
      assert.strictEqual(midMov.autonomousBehaviorState, 'player_interaction_paused');
      assert.strictEqual(midMov.isSuspendedByPlayerInteraction, true);
      assert.deepStrictEqual(midMov.position, sofiaBefore.position);

      // Reconstruct a new adapter from exported pause intervals and verify identical state
      const exportedPauses = worldCore.getNpcInteractionPauseIntervals();
      const reconstructedAdapter = new InMemoryWorldCoreAdapter({
        initialPauseIntervals: exportedPauses,
      });
      const reconstructedMov = await reconstructedAdapter.getNpcMovement(
        'sofia-cross-border-retail',
        midChatIso
      );
      assert.deepStrictEqual(reconstructedMov, midMov);

      // End interaction at +45s via POST /api/world/interactions/end
      const endRes = await fetch(
        `${url}/api/world/interactions/end?at=${encodeURIComponent(midChatIso)}`,
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            playerId: 'player-1',
            npcId: 'sofia-cross-border-retail',
          }),
        }
      );
      assert.strictEqual(endRes.status, 200);
      const endData = (await endRes.json()) as any;
      assert.strictEqual(endData.success, true);
      assert.strictEqual(endData.released, true);
      assert.strictEqual(endData.movement.isSuspendedByPlayerInteraction, false);
      assert.strictEqual(endData.movement.autonomousBehaviorState, 'resuming');
      assert.deepStrictEqual(endData.movement.position, sofiaBefore.position);
    } finally {
      await serverInstance.stop();
    }
  });

  it('3. RemoteWorldCoreAdapter explicitly reports 501 not-supported for unsupported movement suspension/release', async () => {
    const remote = new RemoteWorldCoreAdapter({ baseURL: 'http://127.0.0.1:59999' });
    await assert.rejects(
      () => remote.suspendNpcMovementForInteraction('player-1', 'mateo-student'),
      (err: unknown) => err instanceof WorldCoreHttpError && err.statusCode === 501 && err.errorCode === 'not-supported'
    );
    await assert.rejects(
      () => remote.releaseNpcMovementFromInteraction('player-1', 'mateo-student'),
      (err: unknown) => err instanceof WorldCoreHttpError && err.statusCode === 501 && err.errorCode === 'not-supported'
    );
  });
});
