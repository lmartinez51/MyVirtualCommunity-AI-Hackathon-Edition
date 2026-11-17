import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { InMemoryWorldCoreAdapter } from '../../src/adapters/in-memory-world-core-adapter.js';
import { MockMetaModelAdapter } from '../../src/adapters/mock-meta-model-adapter.js';
import { WorldCoreConnectionError, WorldCoreHttpError, WorldCoreTimeoutError } from '../../src/adapters/remote-world-core-adapter.js';
import { createAssistantServer } from '../../src/server/index.js';

describe('Contextual Social Awareness API Integration Tests (Increment 5.20.0)', () => {
  it('1. GET /api/world/npcs/social-awareness & /api/npcs/social-awareness return stable aggregate snapshot', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel });

    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      const res = await fetch(`${url}/api/world/npcs/social-awareness?at=2026-11-04T17:00:00.000Z`);
      assert.strictEqual(res.status, 200);
      const data = (await res.json()) as any;

      assert.strictEqual(data.success, true);
      assert.strictEqual(data.isDemonstrationAwareness, true);
      assert.strictEqual(data.detectionBasis, 'proximity_horizontal_vertical');
      assert.strictEqual(data.supportsLineOfSight, false);
      assert.strictEqual(data.environmentalAwarenessScope, 'player_and_npc_proximity_only');
      assert.strictEqual(data.summary.totalCount, 7);
      assert.strictEqual(data.npcsList.length, 7);
      assert.ok(data.npcs['elena-border-concierge']);
      assert.ok(data.npcs['carlos-farmacia-attendant']);

      const aliasRes = await fetch(`${url}/api/npcs/social-awareness?at=2026-11-04T17:00:00.000Z`);
      assert.strictEqual(aliasRes.status, 200);
      const aliasData = (await aliasRes.json()) as any;
      assert.strictEqual(aliasData.summary.totalCount, 7);
    } finally {
      await serverInstance.stop();
    }
  });

  it('2. Authoritative player movement updates social awareness state on both social-awareness and interaction-context endpoints', async () => {
    let fakeNowMs = 1_000_000;
    const fakeNow = () => fakeNowMs;
    const worldCore = new InMemoryWorldCoreAdapter({ nowFn: fakeNow });
    worldCore.setSimulatedWorldTime('2026-11-04T17:00:00.000Z'); // Daytime normal
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel, nowFn: fakeNow });

    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      // Initially player is at spawn (0, 1, -15), which is 18.3m from Carlos (10.5, 1, 0) -> unaware.
      const initRes = await fetch(`${url}/api/world/npcs/carlos-farmacia-attendant/social-awareness`);
      assert.strictEqual(initRes.status, 200);
      const initData = (await initRes.json()) as any;
      assert.strictEqual(initData.awareness.awarenessState, 'unaware');

      // Move authoritatively northeast across multiple valid server-clock ticks toward Carlos counter.
      for (let seq = 1; seq <= 8; seq++) {
        fakeNowMs += 500; // 4.5 m/s * 0.5s = 2.25m per step toward (10.5, 0) from (0, -15) => ~18.3m total
        const moveRes = await fetch(`${url}/api/world/players/player-1/movement`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            direction: { x: 10.5, z: 15.0 },
            movementMode: 'walk',
            sequenceNumber: seq,
          }),
        });
        assert.strictEqual(moveRes.status, 200);
      }

      // Now verify Carlos awareness changed via BOTH /social-awareness and existing /interaction-context endpoint!
      const closeRes = await fetch(`${url}/api/world/npcs/carlos-farmacia-attendant/social-awareness`);
      assert.strictEqual(closeRes.status, 200);
      const closeData = (await closeRes.json()) as any;
      assert.strictEqual(closeData.awareness.targetEntityType, 'player');
      assert.strictEqual(closeData.awareness.targetEntityId, 'player-1');
      assert.strictEqual(closeData.awareness.awarenessState, 'player_attentive');

      const ctxRes = await fetch(`${url}/api/world/npcs/interaction-context`);
      assert.strictEqual(ctxRes.status, 200);
      const ctxData = (await ctxRes.json()) as any;
      assert.strictEqual(ctxData.success, true);
      assert.strictEqual(ctxData.npcs['carlos-farmacia-attendant'].socialAwareness.awarenessState, 'player_attentive');
      assert.ok(ctxData.socialAwarenessSummary);
    } finally {
      await serverInstance.stop();
    }
  });

  it('3. Temporal ?at= evaluation reflects off-duty silence and deterministic plaza handover pairing', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel });

    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      // At 10:00 MST (17:00 UTC): Sofia (z=45.5) and Hector (z=49.0) are active and 3.5m apart -> mutual social_engaged queue pair.
      const resDay = await fetch(`${url}/api/world/npcs/social-awareness?at=2026-11-04T17:00:00.000Z`);
      assert.strictEqual(resDay.status, 200);
      const dataDay = (await resDay.json()) as any;
      assert.strictEqual(dataDay.npcs['sofia-cross-border-retail'].awarenessState, 'social_engaged');
      assert.strictEqual(dataDay.npcs['sofia-cross-border-retail'].targetEntityId, 'hector-artisan');
      assert.strictEqual(dataDay.npcs['sofia-cross-border-retail'].pairedPartnerNpcId, 'hector-artisan');
      assert.strictEqual(dataDay.npcs['hector-artisan'].awarenessState, 'social_engaged');
      assert.strictEqual(dataDay.npcs['hector-artisan'].targetEntityId, 'sofia-cross-border-retail');
      assert.strictEqual(dataDay.summary.activePairCount, 1);

      // At 08:30 MST (15:30 UTC): all 3 queue proxies (Mateo z=42.0, Sofia z=45.5, Hector z=49.0) are active.
      // (hector-artisan, sofia-cross-border-retail) ties (mateo-student, sofia-cross-border-retail) at 3.5m and wins tie-break ('h' < 'm');
      // Mateo becomes npc_aware of Sofia (3.5m) and Elena becomes npc_aware of Mateo (4.72m).
      const resMorning = await fetch(`${url}/api/world/npcs/social-awareness?at=2026-11-04T15:30:00.000Z`);
      assert.strictEqual(resMorning.status, 200);
      const dataMorning = (await resMorning.json()) as any;
      assert.strictEqual(dataMorning.npcs['hector-artisan'].awarenessState, 'social_engaged');
      assert.strictEqual(dataMorning.npcs['hector-artisan'].pairedPartnerNpcId, 'sofia-cross-border-retail');
      assert.strictEqual(dataMorning.npcs['sofia-cross-border-retail'].awarenessState, 'social_engaged');
      assert.strictEqual(dataMorning.npcs['sofia-cross-border-retail'].pairedPartnerNpcId, 'hector-artisan');
      assert.strictEqual(dataMorning.npcs['mateo-student'].awarenessState, 'npc_aware');
      assert.strictEqual(dataMorning.npcs['mateo-student'].targetEntityId, 'sofia-cross-border-retail');
      assert.strictEqual(dataMorning.npcs['elena-border-concierge'].awarenessState, 'npc_aware');
      assert.strictEqual(dataMorning.npcs['elena-border-concierge'].targetEntityId, 'mateo-student');

      // At 13:35 MST (20:35 UTC): Elena is stationary/arrived at wp_bridge_plaza (17.8, 0.2, 42.0) and retains contextual status cleanly
      const resPlaza = await fetch(`${url}/api/world/npcs/elena-border-concierge/social-awareness?at=2026-11-04T20:35:00.000Z`);
      assert.strictEqual(resPlaza.status, 200);
      const dataPlaza = (await resPlaza.json()) as any;
      assert.strictEqual(dataPlaza.awareness.currentWaypointId, 'wp_bridge_plaza');
      assert.strictEqual(dataPlaza.awareness.socialEligible, true);

      // At 23:30 MST (2026-11-05T06:30:00.000Z): attendants/public servants (4 NPCs) are off_duty/closed -> unaware,
      // while the 3 community player proxies remain active in the queue (Hector <-> Sofia pair + Mateo npc_aware).
      const resNight = await fetch(`${url}/api/world/npcs/social-awareness?at=2026-11-05T06:30:00.000Z`);
      assert.strictEqual(resNight.status, 200);
      const dataNight = (await resNight.json()) as any;
      assert.strictEqual(dataNight.summary.activePairCount, 1);
      assert.strictEqual(dataNight.summary.unawareCount, 4);
      assert.strictEqual(dataNight.npcs['carlos-farmacia-attendant'].awarenessState, 'unaware');
      assert.strictEqual(dataNight.npcs['elena-border-concierge'].awarenessState, 'unaware');
      assert.strictEqual(dataNight.npcs['mateo-student'].socialEligible, true);

      // Invalid ?at= returns 400 invalid-evaluation-time
      const resBadTime = await fetch(`${url}/api/world/npcs/social-awareness?at=not-a-date`);
      assert.strictEqual(resBadTime.status, 400);
      const badData = (await resBadTime.json()) as any;
      assert.strictEqual(badData.error.code, 'invalid-evaluation-time');
    } finally {
      await serverInstance.stop();
    }
  });

  it('4. Handles missing/unknown entities, missing playerId safely, and surfaces World Core adapter errors cleanly', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel });

    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      const unknownNpc = await fetch(`${url}/api/world/npcs/does-not-exist/social-awareness`);
      assert.strictEqual(unknownNpc.status, 404);
      const unknownBody = (await unknownNpc.json()) as any;
      assert.strictEqual(unknownBody.error.code, 'npc-not-found');

      const missingPlayer = await fetch(`${url}/api/world/npcs/social-awareness?playerId=unknown-player&at=2026-11-04T17:00:00.000Z`);
      assert.strictEqual(missingPlayer.status, 200);
      const missingBody = (await missingPlayer.json()) as any;
      assert.strictEqual(missingBody.playerPosition, null);
      assert.strictEqual(missingBody.npcs['carlos-farmacia-attendant'].nearbyPlayerDistanceMeters, null);
    } finally {
      await serverInstance.stop();
    }

    // Adapter connection/timeout error -> 502 world-core-unavailable
    const failingAdapter = new InMemoryWorldCoreAdapter();
    failingAdapter.getNpcRosterSocialAwareness = async () => {
      throw new WorldCoreTimeoutError('Timed out reading World Core spatial state');
    };
    const errorServer = createAssistantServer({ worldCore: failingAdapter, metaModel });
    await errorServer.start(0, '127.0.0.1');
    const errAddr = errorServer.server.address() as any;
    try {
      const errRes = await fetch(`http://127.0.0.1:${errAddr.port}/api/world/npcs/social-awareness`);
      assert.strictEqual(errRes.status, 502);
      const errData = (await errRes.json()) as any;
      assert.strictEqual(errData.success, false);
      assert.strictEqual(errData.error.code, 'world-core-unavailable');

      failingAdapter.getNpcRosterSocialAwareness = async () => {
        throw new WorldCoreConnectionError('ECONNREFUSED');
      };
      const connRes = await fetch(`http://127.0.0.1:${errAddr.port}/api/world/npcs/social-awareness`);
      assert.strictEqual(connRes.status, 502);

      failingAdapter.getNpcRosterSocialAwareness = async () => {
        throw new WorldCoreHttpError(503, 'World Core maintenance', 'service-unavailable');
      };
      const httpErrRes = await fetch(`http://127.0.0.1:${errAddr.port}/api/world/npcs/social-awareness`);
      assert.strictEqual(httpErrRes.status, 502);
      const httpErrData = (await httpErrRes.json()) as any;
      assert.strictEqual(httpErrData.error.code, 'service-unavailable');
    } finally {
      await errorServer.stop();
    }
  });

  it('5. Scheduled movement windows execute deterministically across all 7 canonical NPCs including Elena kiosk-to-plaza route', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel });

    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    // Weekday Wednesday 2026-11-11 in America/Ciudad_Juarez (MST = UTC-7)
    const mstIso = (hh: number, mm: number, ss = 0) => {
      const utcHour = hh + 7;
      const pad = (n: number) => String(n).padStart(2, '0');
      return `2026-11-11T${pad(utcHour)}:${pad(mm)}:${pad(ss)}.000Z`;
    };

    const schedules = [
      { npcId: 'mateo-student', homeWp: 'wp_mateo_queue_entry', awayWp: 'wp_bridge_toll_plaza', before: mstIso(9, 29), midOut: mstIso(9, 30, 5), arrived: mstIso(9, 35), midRet: mstIso(9, 59, 5), after: mstIso(10, 5) },
      { npcId: 'sofia-cross-border-retail', homeWp: 'wp_sofia_queue_mid', awayWp: 'wp_canopy_waiting', before: mstIso(10, 59), midOut: mstIso(11, 0, 5), arrived: mstIso(11, 5), midRet: mstIso(11, 29, 5), after: mstIso(11, 35) },
      { npcId: 'patricia-citizen-rep', homeWp: 'wp_patricia_desk', awayWp: 'wp_west_inspection', before: mstIso(11, 59), midOut: mstIso(12, 0, 5), arrived: mstIso(12, 5), midRet: mstIso(12, 59, 5), after: mstIso(13, 5) },
      { npcId: 'hector-artisan', homeWp: 'wp_hector_stand', awayWp: 'wp_hector_bench', before: mstIso(12, 59), midOut: mstIso(13, 0, 2), arrived: mstIso(13, 5), midRet: mstIso(13, 59, 2), after: mstIso(14, 5) },
      { npcId: 'elena-border-concierge', homeWp: 'wp_elena_kiosk', awayWp: 'wp_bridge_plaza', before: mstIso(13, 29), midOut: mstIso(13, 30, 3), arrived: mstIso(13, 35), midRet: mstIso(14, 29, 3), after: mstIso(14, 35) },
    ];

    try {
      for (const s of schedules) {
        const readSnap = async (iso: string) => {
          const res = await fetch(`${url}/api/world/npcs/movement?at=${encodeURIComponent(iso)}`);
          assert.strictEqual(res.status, 200);
          const body = (await res.json()) as any;
          return body.npcs[s.npcId];
        };

        const before = await readSnap(s.before);
        assert.strictEqual(before.movementState, 'stationary');
        assert.strictEqual(before.currentWaypointId, s.homeWp);

        const midOut = await readSnap(s.midOut);
        assert.strictEqual(midOut.movementState, 'moving');

        const arrived = await readSnap(s.arrived);
        assert.ok(arrived.movementState === 'waiting' || arrived.movementState === 'arrived');
        assert.strictEqual(arrived.currentWaypointId, s.awayWp);

        const midRet = await readSnap(s.midRet);
        assert.strictEqual(midRet.movementState, 'moving');

        const after = await readSnap(s.after);
        assert.strictEqual(after.movementState, 'stationary');
        assert.strictEqual(after.currentWaypointId, s.homeWp);
      }

      // Carlos (14:05 CST) & Valeria (13:05 CST) have zero breaks during open storefront hours and remain bounded inside their shops
      const carlosMiddayRes = await fetch(`${url}/api/world/npcs/movement?at=${encodeURIComponent(mstIso(14, 5))}`);
      assert.strictEqual(carlosMiddayRes.status, 200);
      const carlosMidday = ((await carlosMiddayRes.json()) as any).npcs['carlos-farmacia-attendant'];
      assert.strictEqual(carlosMidday.activityState, 'available');
      assert.strictEqual(carlosMidday.controlMode, 'autonomous_business_local');
      assert.strictEqual(carlosMidday.withinAuthorizedDomain, true);

      const valeriaCalibRes = await fetch(`${url}/api/world/npcs/movement?at=${encodeURIComponent(mstIso(13, 5))}`);
      assert.strictEqual(valeriaCalibRes.status, 200);
      const valeriaCalibBody = (await valeriaCalibRes.json()) as any;
      const valeriaCalib = valeriaCalibBody.npcs['valeria-optica-attendant'];
      assert.strictEqual(valeriaCalib.activityState, 'available');
      assert.strictEqual(valeriaCalib.controlMode, 'autonomous_business_local');
      assert.strictEqual(valeriaCalib.withinAuthorizedDomain, true);
    } finally {
      await serverInstance.stop();
    }
  });
});
