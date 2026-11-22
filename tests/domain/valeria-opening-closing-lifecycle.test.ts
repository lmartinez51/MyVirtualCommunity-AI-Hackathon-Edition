import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  BUSINESS_LOCAL_AUTONOMY_PROFILES,
  OPTICA_EXTERIOR_FRONTAGE_ZONE,
  OPTICA_FASHION_AUTONOMY_DOMAIN,
  OPTICA_FRONT_ENTRANCE_ZONE,
  OPTICA_MAIN_SHOWROOM_ZONE,
  OPTICA_REAR_CALIBRATION_ROOM_ZONE,
  OPTICA_REAR_DOORWAY_ZONE,
  VALERIA_CALIBRATION_STATION_WAYPOINT_ID,
  VALERIA_CALIBRATION_TASK_ID,
  VALERIA_CLOSING_PREP_LEAD_MINUTES,
  VALERIA_OFF_DUTY_STATION_HEADING,
  VALERIA_OFF_DUTY_STATION_POSITION,
  VALERIA_OFF_DUTY_STATION_WAYPOINT_ID,
  VALERIA_OPENING_TRANSITION_SECONDS,
  isPlayerInsideOpticaInterior,
  isPointInSafeBusinessRoamingRegion,
  isValeriaSafelyInsideOptica,
  validateBusinessLocalGraphRoute,
} from '../../src/domain/npc-autonomous-locomotion.js';
import {
  CORRIDOR_WAYPOINTS,
  NPC_MOVEMENT_PROFILES,
  evaluateNpcMovement,
  evaluateRosterMovement,
} from '../../src/domain/npc-waypoint-movement.js';
import { InMemoryWorldCoreAdapter } from '../../src/adapters/in-memory-world-core-adapter.js';
import { validateNpcInteraction } from '../../src/application/validate-npc-interaction.js';
import { createBusinessId } from '../../src/domain/business-id.js';
import { createAssistantServer } from '../../src/server/index.js';

describe('Increment 5.23.2 — Valeria Business Opening/Closing Lifecycle & Interaction-Safe NPC Suspension', () => {
  const valeriaProfile = NPC_MOVEMENT_PROFILES['valeria-optica-attendant']!;
  const valeriaLocalProfile = BUSINESS_LOCAL_AUTONOMY_PROFILES['valeria-optica-attendant']!;

  const movePlayerNear = async (
    worldCore: InMemoryWorldCoreAdapter,
    targetPos: { x: number; y: number; z: number },
    clockRef: { ms: number },
    seqRef: { seq: number }
  ) => {
    for (let i = 0; i < 90; i++) {
      const p = (await worldCore.getPlayerState('player-1'))!;
      const dx = targetPos.x - p.position.x;
      const dz = targetPos.z - p.position.z;
      const dist = Math.hypot(dx, dz);
      if (dist <= 1.2) break;
      clockRef.ms += 300;
      seqRef.seq++;
      await worldCore.applyPlayerMovement(
        'player-1',
        { direction: { x: dx / dist, z: dz / dist }, movementMode: 'sprint', sequenceNumber: seqRef.seq },
        clockRef.ms
      );
    }
  };

  it('10P1. Priority 1 — Verified interior off-duty station is inside Zone C, distinct from calibration & roaming stops, and reachable from all authorized nodes', () => {
    const offDutyWp = CORRIDOR_WAYPOINTS[VALERIA_OFF_DUTY_STATION_WAYPOINT_ID];
    assert.ok(offDutyWp, 'wp_valeria_off_duty_station must exist in CORRIDOR_WAYPOINTS');
    assert.deepStrictEqual(offDutyWp!.position, VALERIA_OFF_DUTY_STATION_POSITION);
    assert.deepStrictEqual(offDutyWp!.position, { x: -15.85, y: 0.2, z: 1.15 });
    assert.strictEqual(isValeriaSafelyInsideOptica(offDutyWp!.position), true);
    assert.strictEqual(isPointInSafeBusinessRoamingRegion(offDutyWp!.position, OPTICA_FASHION_AUTONOMY_DOMAIN, null), true);

    // Distinct from calibration station, outdoor storefront, and ordinary roaming stops
    assert.notStrictEqual(VALERIA_OFF_DUTY_STATION_WAYPOINT_ID, VALERIA_CALIBRATION_STATION_WAYPOINT_ID);
    assert.notStrictEqual(VALERIA_OFF_DUTY_STATION_WAYPOINT_ID, 'wp_valeria_storefront');
    assert.strictEqual(
      valeriaLocalProfile.secondaryWorkstationIds.includes(VALERIA_OFF_DUTY_STATION_WAYPOINT_ID),
      false,
      'Off-duty station must NOT be an ordinary showroom roaming destination'
    );
    assert.strictEqual(valeriaLocalProfile.localNodes[VALERIA_OFF_DUTY_STATION_WAYPOINT_ID]?.isOffDutyStation, true);

    // Connected edges validate strictly inside Zone C without task-only auth
    for (const neighborId of ['wp_optica_showroom_center', 'wp_optica_showroom_counter_west', 'wp_optica_showroom_north_display']) {
      const fwd = validateBusinessLocalGraphRoute(
        [neighborId, VALERIA_OFF_DUTY_STATION_WAYPOINT_ID],
        OPTICA_FASHION_AUTONOMY_DOMAIN,
        valeriaLocalProfile,
        CORRIDOR_WAYPOINTS,
        null
      );
      const rev = validateBusinessLocalGraphRoute(
        [VALERIA_OFF_DUTY_STATION_WAYPOINT_ID, neighborId],
        OPTICA_FASHION_AUTONOMY_DOMAIN,
        valeriaLocalProfile,
        CORRIDOR_WAYPOINTS,
        null
      );
      assert.strictEqual(fwd.valid, true, `Edge ${neighborId} -> off_duty_station failed: ${fwd.reason}`);
      assert.strictEqual(rev.valid, true, `Edge off_duty_station -> ${neighborId} failed: ${rev.reason}`);
    }

    // Mathematical lead-time verification: worst-case exterior route duration << 3 minutes (180s)
    const longestRoute = [
      'wp_optica_south_promenade',
      'wp_optica_plaza_threshold',
      'wp_valeria_storefront',
      'wp_optica_door_approach',
      'wp_optica_door_threshold',
      'wp_optica_interior_entry',
      'wp_optica_showroom_center',
      VALERIA_OFF_DUTY_STATION_WAYPOINT_ID,
    ];
    let worstDist = 0;
    for (let i = 0; i < longestRoute.length - 1; i++) {
      const a = CORRIDOR_WAYPOINTS[longestRoute[i]!]!.position;
      const b = CORRIDOR_WAYPOINTS[longestRoute[i + 1]!]!.position;
      worstDist += Math.hypot(b.x - a.x, b.y - a.y, b.z - a.z);
    }
    const worstDurationSec = worstDist / valeriaLocalProfile.preferredSpeedMps;
    assert.ok(worstDurationSec < 15, `Expected worst-case return < 15s, got ${worstDurationSec.toFixed(2)}s`);
    assert.ok(
      VALERIA_CLOSING_PREP_LEAD_MINUTES * 60 > worstDurationSec * 10,
      `Lead time (${VALERIA_CLOSING_PREP_LEAD_MINUTES * 60}s) must comfortably exceed worst-case return (${worstDurationSec.toFixed(2)}s)`
    );
  });

  it('10A. Opening: Valeria starts inside at 09:00 America/Ciudad_Juarez, shutter opens first, and she walks continuously through the open doorway into normal autonomy', () => {
    // Wednesday Nov 4, 2026 (MST = UTC-7): 09:00:00 MST = 16:00:00 UTC
    const tBeforeOpen = Date.parse('2026-11-04T15:59:59.000Z'); // 08:59:59 MST
    const tExactOpen = Date.parse('2026-11-04T16:00:00.000Z');  // 09:00:00 MST
    const tMidOpenTrans = Date.parse('2026-11-04T16:00:03.000Z'); // 09:00:03 MST
    const tWalkStart = tExactOpen + VALERIA_OPENING_TRANSITION_SECONDS * 1000; // 09:00:05 MST

    // 1. Just before opening (08:59:59 MST): off_duty_inside, shutter closed
    const snapBefore = evaluateNpcMovement(valeriaProfile, tBeforeOpen);
    assert.strictEqual(snapBefore.businessLifecycleState, 'off_duty_inside');
    assert.strictEqual(snapBefore.shutterClosedPermitted, true);
    assert.strictEqual(snapBefore.currentWaypointId, VALERIA_OFF_DUTY_STATION_WAYPOINT_ID);
    assert.deepStrictEqual(snapBefore.position, VALERIA_OFF_DUTY_STATION_POSITION);
    assert.strictEqual(snapBefore.currentSpeedMps, 0);

    // 2. Exact opening (09:00:00 MST) through opening_transition: shutter lifts, Valeria waits calmly inside
    for (const t of [tExactOpen, tMidOpenTrans]) {
      const snapOpen = evaluateNpcMovement(valeriaProfile, t);
      assert.strictEqual(snapOpen.businessLifecycleState, 'opening_transition');
      assert.strictEqual(snapOpen.shutterClosedPermitted, false);
      assert.strictEqual(snapOpen.activityState, 'available');
      assert.strictEqual(snapOpen.currentWaypointId, VALERIA_OFF_DUTY_STATION_WAYPOINT_ID);
      assert.deepStrictEqual(snapOpen.position, VALERIA_OFF_DUTY_STATION_POSITION);
      assert.strictEqual(snapOpen.currentSpeedMps, 0);
    }

    // 3. After opening transition (09:00:05..09:00:25): continuous walk from wp_valeria_off_duty_station -> Zone C -> Zone B -> Zone A (zero teleport!)
    let prev = evaluateNpcMovement(valeriaProfile, tExactOpen + 4500);
    const zonesTraversed = new Set<string>();
    let reachedExterior = false;
    for (let s = 0; s <= 20; s += 0.5) {
      const cur = evaluateNpcMovement(valeriaProfile, tWalkStart + s * 1000);
      const step = Math.hypot(cur.position.x - prev.position.x, cur.position.z - prev.position.z);
      assert.ok(step <= 0.65, `Discontinuity during opening walk at +${s}s: jumped ${step.toFixed(3)}m`);
      assert.strictEqual(cur.businessLifecycleState, 'business_open');
      assert.strictEqual(cur.shutterClosedPermitted, false);
      assert.strictEqual(cur.withinAuthorizedDomain, true);
      assert.notStrictEqual(cur.currentZoneId, OPTICA_REAR_CALIBRATION_ROOM_ZONE.zoneId);
      if (cur.currentZoneId) zonesTraversed.add(cur.currentZoneId);
      if (cur.currentZoneId === OPTICA_EXTERIOR_FRONTAGE_ZONE.zoneId) reachedExterior = true;
      prev = cur;
    }

    assert.ok(zonesTraversed.has(OPTICA_MAIN_SHOWROOM_ZONE.zoneId), 'Opening walk must start inside main showroom');
    assert.ok(zonesTraversed.has(OPTICA_FRONT_ENTRANCE_ZONE.zoneId), 'Opening walk must traverse open doorway');
    assert.ok(reachedExterior, 'Valeria must subsequently reach exterior frontage through the open doorway');
  });

  it('10B & 10C. Pre-Closing (16:57), Closing (17:00) & Overnight Off-Duty Inside: continuous indoor return without rear lab entry, shutter waits until safely inside', async () => {
    // Wednesday Nov 4, 2026 (MST = UTC-7): 16:57:00 MST = 23:57:00 UTC, 17:00:00 MST = 2026-11-05T00:00:00Z
    const tBeforePreClose = Date.parse('2026-11-04T23:56:59.000Z'); // 16:56:59 MST
    const tPreClose = Date.parse('2026-11-04T23:57:00.000Z');       // 16:57:00 MST
    const tBeforeClose = Date.parse('2026-11-04T23:59:59.000Z');    // 16:59:59 MST
    const tExactClose = Date.parse('2026-11-05T00:00:00.000Z');     // 17:00:00 MST

    const snapBefore = evaluateNpcMovement(valeriaProfile, tBeforePreClose);
    assert.strictEqual(snapBefore.businessLifecycleState, 'business_open');
    assert.strictEqual(snapBefore.shutterClosedPermitted, false);

    // Sweep 16:57:00..16:57:20 at 0.5s steps: C^0 continuous, never enters Zone D, arrives at wp_valeria_off_duty_station
    let prev = snapBefore;
    let sawReturning = false;
    let arrivedInsideBeforeClose = false;
    for (let s = 0; s <= 20; s += 0.5) {
      const cur = evaluateNpcMovement(valeriaProfile, tPreClose + s * 1000);
      const step = Math.hypot(cur.position.x - prev.position.x, cur.position.z - prev.position.z);
      assert.ok(step <= 1.2, `Teleportation during pre-close return at +${s}s: ${step.toFixed(3)}m`);
      assert.strictEqual(cur.withinAuthorizedDomain, true);
      assert.strictEqual(cur.shutterClosedPermitted, false, 'Shutter must remain OPEN prior to 17:00:00 close');
      assert.notStrictEqual(cur.currentZoneId, OPTICA_REAR_DOORWAY_ZONE.zoneId, 'Closing return must never enter rear doorway');
      assert.notStrictEqual(cur.currentZoneId, OPTICA_REAR_CALIBRATION_ROOM_ZONE.zoneId, 'Closing return must never enter rear lab');

      if (cur.businessLifecycleState === 'returning_inside_for_close') {
        sawReturning = true;
        assert.strictEqual(cur.movementState, 'moving');
        assert.strictEqual(cur.currentSpeedMps, 1.1);
        assert.strictEqual(cur.destinationWaypointId, VALERIA_OFF_DUTY_STATION_WAYPOINT_ID);
      }
      if (
        cur.currentWaypointId === VALERIA_OFF_DUTY_STATION_WAYPOINT_ID &&
        cur.movementState === 'stationary' &&
        cur.businessLifecycleState === 'closing_preparation'
      ) {
        arrivedInsideBeforeClose = true;
        assert.strictEqual(cur.currentSpeedMps, 0);
        assert.deepStrictEqual(cur.position, VALERIA_OFF_DUTY_STATION_POSITION);
        assert.deepStrictEqual(cur.forwardHeading, VALERIA_OFF_DUTY_STATION_HEADING);
      }
      prev = cur;
    }

    assert.strictEqual(sawReturning, true, 'Must enter returning_inside_for_close at 16:57');
    assert.strictEqual(arrivedInsideBeforeClose, true, 'Must reach wp_valeria_off_duty_station well before 17:00');

    // 16:59:59 MST: still calmly waiting inside, shutter STILL open until 17:00:00
    const snap165959 = evaluateNpcMovement(valeriaProfile, tBeforeClose);
    assert.strictEqual(snap165959.businessLifecycleState, 'closing_preparation');
    assert.strictEqual(snap165959.shutterClosedPermitted, false);
    assert.deepStrictEqual(snap165959.position, VALERIA_OFF_DUTY_STATION_POSITION);

    // 17:00:00 MST: confirmed safely inside -> off_duty_inside, shutter closes!
    const snap170000 = evaluateNpcMovement(valeriaProfile, tExactClose);
    assert.strictEqual(snap170000.businessLifecycleState, 'off_duty_inside');
    assert.strictEqual(snap170000.shutterClosedPermitted, true);
    assert.strictEqual(snap170000.activityState, 'off_duty');
    assert.strictEqual(snap170000.currentWaypointId, VALERIA_OFF_DUTY_STATION_WAYPOINT_ID);
    assert.deepStrictEqual(snap170000.position, VALERIA_OFF_DUTY_STATION_POSITION);
    assert.strictEqual(snap170000.currentSpeedMps, 0);

    // Throughout closed overnight period -> remains inside (cold startup during closed hours)
    for (const closedIso of [
      '2026-11-05T01:30:00.000Z', // 18:30 MST
      '2026-11-05T05:00:00.000Z', // 22:00 MST
      '2026-11-05T10:00:00.000Z', // 03:00 MST next day
      '2026-11-05T15:59:59.000Z', // 08:59:59 MST next day
    ]) {
      const snap = evaluateNpcMovement(valeriaProfile, closedIso);
      assert.strictEqual(snap.businessLifecycleState, 'off_duty_inside');
      assert.strictEqual(snap.shutterClosedPermitted, true);
      assert.strictEqual(snap.currentWaypointId, VALERIA_OFF_DUTY_STATION_WAYPOINT_ID);
      assert.strictEqual(isValeriaSafelyInsideOptica(snap.position), true);
    }

    // Player non-trap safety: if player is inside Óptica Fashion at 17:00:05, shutter stays OPEN until player exits
    const insidePlayer = {
      playerId: 'player-1',
      position: { x: -14.2, y: 0.2, z: 1.0 },
      lastSequenceNumber: 1,
      lastMovementServerTimeMs: Date.parse('2026-11-05T00:00:05.000Z'),
      updatedAt: '2026-11-05T00:00:05.000Z',
    };
    assert.strictEqual(isPlayerInsideOpticaInterior(insidePlayer), true);
    const snapPlayerInside = evaluateNpcMovement(valeriaProfile, '2026-11-05T00:00:05.000Z', {
      player: insidePlayer,
    });
    assert.strictEqual(snapPlayerInside.businessLifecycleState, 'closing_pending');
    assert.strictEqual(snapPlayerInside.shutterClosedPermitted, false, 'Shutter MUST NOT trap player inside store');
    assert.deepStrictEqual(snapPlayerInside.position, VALERIA_OFF_DUTY_STATION_POSITION);

    const outsidePlayer = {
      ...insidePlayer,
      position: { x: -9.5, y: 0.2, z: 1.2 },
    };
    assert.strictEqual(isPlayerInsideOpticaInterior(outsidePlayer), false);
    const snapPlayerOutside = evaluateNpcMovement(valeriaProfile, '2026-11-05T00:00:10.000Z', {
      player: outsidePlayer,
    });
    assert.strictEqual(snapPlayerOutside.businessLifecycleState, 'off_duty_inside');
    assert.strictEqual(snapPlayerOutside.shutterClosedPermitted, true);

    // Closed-shutter intrusion guard: pushing West from the sidewalk against a closed shutter
    // NEVER advances authoritative player position inside Óptica Fashion or pops the shutter open!
    const tNightMs = Date.parse('2026-11-05T05:00:00.000Z'); // 22:00 MST (closed)
    const nightClock = { ms: tNightMs };
    const nightSeq = { seq: 0 };
    const nightWc = new InMemoryWorldCoreAdapter({ nowFn: () => nightClock.ms });
    nightWc.setSimulatedWorldTime(new Date(tNightMs).toISOString());
    // Walk player to the sidewalk right outside the closed shutter (X ~ -11.5, Z ~ 1.2)
    await movePlayerNear(nightWc, { x: -11.2, y: 0.2, z: 1.2 }, nightClock, nightSeq);
    // Repeatedly push West (-X) directly into the closed shutter
    for (let push = 0; push < 10; push++) {
      nightClock.ms += 300;
      nightSeq.seq++;
      await nightWc.applyPlayerMovement(
        'player-1',
        { direction: { x: -1, z: 0 }, movementMode: 'sprint', sequenceNumber: nightSeq.seq },
        nightClock.ms
      );
    }
    const blockedPlayer = (await nightWc.getPlayerState('player-1'))!;
    assert.strictEqual(isPlayerInsideOpticaInterior(blockedPlayer), false, 'Closed shutter must block exterior player from entering');
    assert.ok(blockedPlayer.position.x >= -11.56, `Player crossed closed shutter plane: X=${blockedPlayer.position.x}`);
    const nightValeriaAfterPush = (await nightWc.getNpcMovement('valeria-optica-attendant', tNightMs))!;
    assert.strictEqual(nightValeriaAfterPush.businessLifecycleState, 'off_duty_inside');
    assert.strictEqual(nightValeriaAfterPush.shutterClosedPermitted, true, 'Approaching/pushing closed shutter must NEVER open it');
  });

  it('10B-2. Pre-closing starting while Valeria is ALREADY inside the showroom routes directly internally without stepping back outside', () => {
    const preCloseMs = Date.parse('2026-11-04T23:57:00.000Z'); // 16:57:00 MST
    let chosenShiftMs = 0;
    for (let shiftSec = 10; shiftSec <= 180; shiftSec += 2) {
      const testSnap = evaluateNpcMovement(valeriaProfile, preCloseMs - 100, {
        pauseIntervalsByNpcId: {
          'valeria-optica-attendant': [
            {
              npcId: 'valeria-optica-attendant',
              playerId: 'player-1',
              startedAtMs: preCloseMs - 240_000,
              endedAtMs: preCloseMs - 240_000 + shiftSec * 1000,
              reason: 'authorized_player_interaction',
            },
          ],
        },
      });
      if (testSnap.currentZoneId === OPTICA_MAIN_SHOWROOM_ZONE.zoneId && testSnap.position.x < -13.5) {
        chosenShiftMs = shiftSec * 1000;
        break;
      }
    }
    assert.ok(chosenShiftMs > 0, 'Must find a showroom-positioned state right before 16:57');

    const opts = {
      pauseIntervalsByNpcId: {
        'valeria-optica-attendant': [
          {
            npcId: 'valeria-optica-attendant',
            playerId: 'player-1',
            startedAtMs: preCloseMs - 240_000,
            endedAtMs: preCloseMs - 240_000 + chosenShiftMs,
            reason: 'authorized_player_interaction' as const,
          },
        ],
      },
    };

    for (let s = 0; s <= 8; s += 0.5) {
      const cur = evaluateNpcMovement(valeriaProfile, preCloseMs + s * 1000, opts);
      assert.strictEqual(
        cur.currentZoneId,
        OPTICA_MAIN_SHOWROOM_ZONE.zoneId,
        'When already inside showroom at 16:57, Valeria must stay inside showroom and route directly to off-duty station'
      );
    }
    const arrived = evaluateNpcMovement(valeriaProfile, preCloseMs + 10_000, opts);
    assert.strictEqual(arrived.currentWaypointId, VALERIA_OFF_DUTY_STATION_WAYPOINT_ID);
    assert.strictEqual(arrived.currentSpeedMps, 0);
  });

  it('10E. Interaction Scenarios A, B, C, D & E — Exact freeze, zero catch-up speed, schedule-boundary reconciliation & closing_pending shutter hold', async () => {
    // --- SCENARIO A: Normal roaming dialogue (12:59:50 -> 13:00:30) crosses calibration start (13:00:00) ---
    {
      const tStart = Date.parse('2026-11-04T19:59:50.000Z'); // 12:59:50 MST
      const tEnd = Date.parse('2026-11-04T20:00:30.000Z');   // 13:00:30 MST
      const clock = { ms: tStart };
      const seq = { seq: 0 };
      const wc = new InMemoryWorldCoreAdapter({ nowFn: () => clock.ms });
      wc.setSimulatedWorldTime(new Date(tStart).toISOString());

      const frozenSnap = (await wc.getNpcMovement('valeria-optica-attendant', tStart))!;
      await movePlayerNear(wc, frozenSnap.position, clock, seq);
      const auth = await validateNpcInteraction(wc, {
        playerId: 'player-1',
        npcId: 'valeria-optica-attendant',
        intent: 'talk',
        evaluatedAt: tStart,
      });
      assert.strictEqual(auth.allowed, true);

      // During dialogue AFTER 13:00:00 (13:00:15): exact same position, speed = 0, suspended
      const midPause = (await wc.getNpcMovement('valeria-optica-attendant', Date.parse('2026-11-04T20:00:15.000Z')))!;
      assert.deepStrictEqual(midPause.position, frozenSnap.position);
      assert.strictEqual(midPause.currentSpeedMps, 0);
      assert.strictEqual(midPause.isSuspendedByPlayerInteraction, true);

      // Release at 13:00:30 -> starts calibration walk from exact frozen position (zero jump!), normal 1.10 m/s
      const released = (await wc.releaseNpcMovementFromInteraction('player-1', 'valeria-optica-attendant', tEnd))!;
      assert.strictEqual(released.isSuspendedByPlayerInteraction, false);
      assert.deepStrictEqual(released.position, frozenSnap.position);
      assert.strictEqual(released.activeTaskId, VALERIA_CALIBRATION_TASK_ID);
      assert.strictEqual(released.destinationWaypointId, VALERIA_CALIBRATION_STATION_WAYPOINT_ID);
      assert.strictEqual(released.currentSpeedMps, 1.1);

      // 1 second after release -> moved ~1.10 m (zero catch-up burst!)
      const plus1s = (await wc.getNpcMovement('valeria-optica-attendant', tEnd + 1000))!;
      const moved1s = Math.hypot(plus1s.position.x - frozenSnap.position.x, plus1s.position.z - frozenSnap.position.z);
      assert.ok(moved1s >= 0.9 && moved1s <= 1.2, `Scenario A catch-up check: moved ${moved1s.toFixed(3)}m in 1s`);
    }

    // --- SCENARIO B: Dialogue crosses calibration end (13:44:30 -> 13:46:00) AND mid-outbound across 13:45 ---
    {
      const tStart = Date.parse('2026-11-04T20:44:30.000Z'); // 13:44:30 MST (at calibration station)
      const tEnd = Date.parse('2026-11-04T20:46:00.000Z');   // 13:46:00 MST (past 13:45 end!)
      const clock = { ms: tStart };
      const seq = { seq: 0 };
      const wc = new InMemoryWorldCoreAdapter({ nowFn: () => clock.ms });
      wc.setSimulatedWorldTime(new Date(tStart).toISOString());

      const beforeB = (await wc.getNpcMovement('valeria-optica-attendant', tStart))!;
      assert.strictEqual(beforeB.currentWaypointId, VALERIA_CALIBRATION_STATION_WAYPOINT_ID);
      await movePlayerNear(wc, beforeB.position, clock, seq);
      const auth = await validateNpcInteraction(wc, {
        playerId: 'player-1',
        npcId: 'valeria-optica-attendant',
        intent: 'talk',
        evaluatedAt: tStart,
      });
      assert.strictEqual(auth.allowed, true);

      // At 13:45:30 (during dialogue past end): stays frozen at calibration station, physicalCalibrationActive = false
      const midB = (await wc.getNpcMovement('valeria-optica-attendant', Date.parse('2026-11-04T20:45:30.000Z')))!;
      assert.deepStrictEqual(midB.position, beforeB.position);
      assert.strictEqual(midB.physicalCalibrationActive, false);
      assert.strictEqual(midB.currentSpeedMps, 0);

      // On release at 13:46:00: does NOT replay calibration; exits continuously at 1.10 m/s with ZERO jump at handoff!
      const relB = (await wc.releaseNpcMovementFromInteraction('player-1', 'valeria-optica-attendant', tEnd))!;
      assert.strictEqual(relB.physicalCalibrationActive, false);
      assert.strictEqual(relB.activeRouteId, 'route_valeria_from_calibration');
      assert.deepStrictEqual(relB.position, beforeB.position);

      let prevB = relB;
      let resumedNormal = false;
      for (let s = 0.5; s <= 25; s += 0.5) {
        const cur = (await wc.getNpcMovement('valeria-optica-attendant', tEnd + s * 1000))!;
        const step = Math.hypot(cur.position.x - prevB.position.x, cur.position.z - prevB.position.z);
        assert.ok(step <= 0.65, `Scenario B return/handoff jump at +${s}s: ${step.toFixed(3)}m`);
        assert.strictEqual(cur.physicalCalibrationActive, false, 'Expired calibration must never reactivate');
        if (cur.controlMode === 'autonomous_business_local' && cur.activeTaskId === null) {
          resumedNormal = true;
        }
        prevB = cur;
      }
      assert.strictEqual(resumedNormal, true, 'Scenario B must cleanly resume normal afternoon autonomy');
    }

    // --- SCENARIO C: Dialogue starts outside before pre-closing (16:56:50) and ends during pre-closing (16:57:30) ---
    {
      const tStart = Date.parse('2026-11-04T23:56:50.000Z'); // 16:56:50 MST
      const tEnd = Date.parse('2026-11-04T23:57:30.000Z');   // 16:57:30 MST
      const clock = { ms: tStart };
      const seq = { seq: 0 };
      const wc = new InMemoryWorldCoreAdapter({ nowFn: () => clock.ms });
      wc.setSimulatedWorldTime(new Date(tStart).toISOString());

      const beforeC = (await wc.getNpcMovement('valeria-optica-attendant', tStart))!;
      await movePlayerNear(wc, beforeC.position, clock, seq);
      const auth = await validateNpcInteraction(wc, {
        playerId: 'player-1',
        npcId: 'valeria-optica-attendant',
        intent: 'talk',
        evaluatedAt: tStart,
      });
      assert.strictEqual(auth.allowed, true);

      // During dialogue at 16:57:15: frozen at exact pre-dialogue position, closing_preparation, shutter open
      const midC = (await wc.getNpcMovement('valeria-optica-attendant', Date.parse('2026-11-04T23:57:15.000Z')))!;
      assert.deepStrictEqual(midC.position, beforeC.position);
      assert.strictEqual(midC.businessLifecycleState, 'closing_preparation');
      assert.strictEqual(midC.shutterClosedPermitted, false);
      assert.strictEqual(midC.currentSpeedMps, 0);

      // On release at 16:57:30: immediately enters returning_inside_for_close from exact frozen position (zero jump!)
      const relC = (await wc.releaseNpcMovementFromInteraction('player-1', 'valeria-optica-attendant', tEnd))!;
      assert.deepStrictEqual(relC.position, beforeC.position);
      assert.strictEqual(relC.businessLifecycleState, 'returning_inside_for_close');
      assert.strictEqual(relC.destinationWaypointId, VALERIA_OFF_DUTY_STATION_WAYPOINT_ID);
      assert.strictEqual(relC.currentSpeedMps, 1.1);
      assert.strictEqual(relC.shutterClosedPermitted, false);

      const arrivedC = (await wc.getNpcMovement('valeria-optica-attendant', tEnd + 15_000))!;
      assert.strictEqual(arrivedC.currentWaypointId, VALERIA_OFF_DUTY_STATION_WAYPOINT_ID);
      assert.strictEqual(arrivedC.businessLifecycleState, 'closing_preparation');
    }

    // --- SCENARIO D: Dialogue holds Valeria OUTSIDE past 17:00:00 (16:56:50 -> 17:01:00) ---
    {
      const tStart = Date.parse('2026-11-04T23:56:50.000Z'); // 16:56:50 MST (outside)
      const tPastClose = Date.parse('2026-11-05T00:00:30.000Z'); // 17:00:30 MST
      const tRelease = Date.parse('2026-11-05T00:01:00.000Z');   // 17:01:00 MST
      const clock = { ms: tStart };
      const seq = { seq: 0 };
      const wc = new InMemoryWorldCoreAdapter({ nowFn: () => clock.ms });
      wc.setSimulatedWorldTime(new Date(tStart).toISOString());

      const beforeD = (await wc.getNpcMovement('valeria-optica-attendant', tStart))!;
      await movePlayerNear(wc, beforeD.position, clock, seq);
      const auth = await validateNpcInteraction(wc, {
        playerId: 'player-1',
        npcId: 'valeria-optica-attendant',
        intent: 'talk',
        evaluatedAt: tStart,
      });
      assert.strictEqual(auth.allowed, true);

      // At 17:00:30 while still conversing outside: closing_pending, shutter MUST remain OPEN, store MUST NOT claim CLOSED
      const pendingMov = (await wc.getNpcMovement('valeria-optica-attendant', tPastClose))!;
      assert.deepStrictEqual(pendingMov.position, beforeD.position);
      assert.strictEqual(pendingMov.businessLifecycleState, 'closing_pending');
      assert.strictEqual(pendingMov.shutterClosedPermitted, false);
      assert.strictEqual(pendingMov.isSuspendedByPlayerInteraction, true);

      const pendingBizStatus = (await wc.getBusinessOperationalStatus(
        createBusinessId('optical:optica-fashion'),
        tPastClose
      ))!;
      assert.strictEqual(pendingBizStatus.businessLifecycleState, 'closing_pending');
      assert.strictEqual(pendingBizStatus.shutterClosedPermitted, false);
      assert.notStrictEqual(pendingBizStatus.status, 'CLOSED', 'Must not falsely claim store is CLOSED while Valeria is outside');

      const pendingCtx = (await wc.getNpcInteractionContext('valeria-optica-attendant', tPastClose))!;
      assert.strictEqual(pendingCtx.businessLifecycleState, 'closing_pending');
      assert.strictEqual(pendingCtx.shutterClosedPermitted, false);
      assert.notStrictEqual(pendingCtx.interactionAvailability, 'off_duty');

      // Release at 17:01:00 -> highest-priority return inside at 1.10 m/s (no jump, no catch-up!), shutter stays open until arrival
      const relD = (await wc.releaseNpcMovementFromInteraction('player-1', 'valeria-optica-attendant', tRelease))!;
      assert.deepStrictEqual(relD.position, beforeD.position);
      assert.strictEqual(relD.businessLifecycleState, 'closing_pending');
      assert.strictEqual(relD.movementState, 'moving');
      assert.strictEqual(relD.currentSpeedMps, 1.1);
      assert.strictEqual(relD.shutterClosedPermitted, false);

      let prevD = relD;
      let closedAfterArrival = false;
      for (let s = 0.5; s <= 15; s += 0.5) {
        const cur = (await wc.getNpcMovement('valeria-optica-attendant', tRelease + s * 1000))!;
        const step = Math.hypot(cur.position.x - prevD.position.x, cur.position.z - prevD.position.z);
        assert.ok(step <= 0.65, `Scenario D return jump at +${s}s: ${step.toFixed(3)}m`);
        if (cur.shutterClosedPermitted) {
          assert.strictEqual(cur.currentWaypointId, VALERIA_OFF_DUTY_STATION_WAYPOINT_ID);
          assert.strictEqual(cur.businessLifecycleState, 'off_duty_inside');
          assert.strictEqual(isValeriaSafelyInsideOptica(cur.position), true);
          closedAfterArrival = true;
        }
        prevD = cur;
      }
      assert.strictEqual(closedAfterArrival, true, 'Closure must complete once Valeria reaches wp_valeria_off_duty_station');

      const finalBizStatus = (await wc.getBusinessOperationalStatus(
        createBusinessId('optical:optica-fashion'),
        tRelease + 15_000
      ))!;
      assert.strictEqual(finalBizStatus.businessLifecycleState, 'off_duty_inside');
      assert.strictEqual(finalBizStatus.shutterClosedPermitted, true);
      assert.strictEqual(finalBizStatus.status, 'CLOSED');
    }

    // --- SCENARIO E: Dialogue during overnight closed period (18:30 -> 18:31) never reactivates roaming ---
    {
      const tClosedStart = Date.parse('2026-11-05T01:30:00.000Z'); // 18:30 MST
      const tClosedEnd = Date.parse('2026-11-05T01:31:00.000Z');   // 18:31 MST
      const wc = new InMemoryWorldCoreAdapter();
      await wc.suspendNpcMovementForInteraction('player-1', 'valeria-optica-attendant', tClosedStart);
      const duringClosed = (await wc.getNpcMovement('valeria-optica-attendant', tClosedStart + 30_000))!;
      assert.strictEqual(duringClosed.businessLifecycleState, 'off_duty_inside');
      assert.deepStrictEqual(duringClosed.position, VALERIA_OFF_DUTY_STATION_POSITION);
      assert.strictEqual(duringClosed.currentSpeedMps, 0);

      const afterClosed = (await wc.releaseNpcMovementFromInteraction('player-1', 'valeria-optica-attendant', tClosedEnd))!;
      assert.strictEqual(afterClosed.businessLifecycleState, 'off_duty_inside');
      assert.strictEqual(afterClosed.movementState, 'stationary');
      assert.strictEqual(afterClosed.currentSpeedMps, 0);
      assert.strictEqual(afterClosed.shutterClosedPermitted, true);
      assert.deepStrictEqual(afterClosed.position, VALERIA_OFF_DUTY_STATION_POSITION);
    }
  });

  it('10F & 10G. Temporal boundaries (Sunday -> Monday reopening, DST MDT vs MST, simulated World Clock API) & 6-NPC regression', async () => {
    // 1. Sunday closed all day -> Monday 09:00 reopening from inside
    for (const sunIso of [
      '2026-11-08T16:00:00.000Z', // Sun 09:00 MST
      '2026-11-08T20:15:00.000Z', // Sun 13:15 MST (no calibration!)
      '2026-11-08T23:58:00.000Z', // Sun 16:58 MST
    ]) {
      const snap = evaluateNpcMovement(valeriaProfile, sunIso);
      assert.strictEqual(snap.businessLifecycleState, 'off_duty_inside');
      assert.strictEqual(snap.shutterClosedPermitted, true);
      assert.strictEqual(snap.currentWaypointId, VALERIA_OFF_DUTY_STATION_WAYPOINT_ID);
      assert.strictEqual(snap.physicalCalibrationActive, false);
    }

    // Monday Nov 9, 2026 09:00:00 MST (16:00:00 UTC) -> opens cleanly from inside!
    const monOpen = evaluateNpcMovement(valeriaProfile, '2026-11-09T16:00:00.000Z');
    assert.strictEqual(monOpen.businessLifecycleState, 'opening_transition');
    assert.strictEqual(monOpen.shutterClosedPermitted, false);
    assert.strictEqual(monOpen.currentWaypointId, VALERIA_OFF_DUTY_STATION_WAYPOINT_ID);

    // 2. Summer DST (MDT = UTC-6, Wednesday July 8, 2026):
    // 09:00 MDT = 15:00 UTC, 16:57 MDT = 22:57 UTC, 17:00 MDT = 23:00 UTC
    const summerOpen = evaluateNpcMovement(valeriaProfile, '2026-07-08T15:00:00.000Z');
    assert.strictEqual(summerOpen.businessLifecycleState, 'opening_transition');
    assert.strictEqual(summerOpen.shutterClosedPermitted, false);

    const summerCalib = evaluateNpcMovement(valeriaProfile, '2026-07-08T19:15:00.000Z'); // 13:15 MDT
    assert.strictEqual(summerCalib.businessLifecycleState, 'business_open');
    assert.strictEqual(summerCalib.shutterClosedPermitted, false);
    assert.strictEqual(summerCalib.physicalCalibrationActive, true);

    const summerPreClose = evaluateNpcMovement(valeriaProfile, '2026-07-08T22:57:02.000Z'); // 16:57:02 MDT
    assert.ok(
      summerPreClose.businessLifecycleState === 'returning_inside_for_close' ||
        summerPreClose.businessLifecycleState === 'closing_preparation'
    );
    assert.strictEqual(summerPreClose.shutterClosedPermitted, false);

    const summerClosed = evaluateNpcMovement(valeriaProfile, '2026-07-08T23:00:00.000Z'); // 17:00:00 MDT
    assert.strictEqual(summerClosed.businessLifecycleState, 'off_duty_inside');
    assert.strictEqual(summerClosed.shutterClosedPermitted, true);
    assert.deepStrictEqual(summerClosed.position, VALERIA_OFF_DUTY_STATION_POSITION);

    // 3. Simulated World Clock API (/api/world/clock) supports deterministic advancing clock without mutating schedules
    let fakeNowMs = Date.parse('2026-11-20T12:00:00.000Z');
    const worldCore = new InMemoryWorldCoreAdapter({ nowFn: () => fakeNowMs });
    const app = createAssistantServer({ worldCore, nowFn: () => fakeNowMs });
    await app.start(0, '127.0.0.1');
    const addr = app.server.address() as { port: number };
    const baseUrl = `http://127.0.0.1:${addr.port}`;
    try {
      const setRes = await fetch(`${baseUrl}/api/world/clock`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ simulatedTime: '2026-11-04T15:59:55.000Z', timeScale: 1 }), // 08:59:55 MST
      });
      assert.strictEqual(setRes.status, 200);
      const setJson = (await setRes.json()) as any;
      assert.strictEqual(setJson.clock.isSimulated, true);

      const closedMovRes = await fetch(`${baseUrl}/api/world/npcs/valeria-optica-attendant/movement`);
      const closedMov = ((await closedMovRes.json()) as any).movement;
      assert.strictEqual(closedMov.businessLifecycleState, 'off_duty_inside');
      assert.strictEqual(closedMov.shutterClosedPermitted, true);

      // Advance real clock by 6 seconds at timeScale=1 -> 09:00:01 MST (opening_transition!)
      fakeNowMs += 6000;
      const openMovRes = await fetch(`${baseUrl}/api/world/npcs/valeria-optica-attendant/movement`);
      const openMov = ((await openMovRes.json()) as any).movement;
      assert.strictEqual(openMov.businessLifecycleState, 'opening_transition');
      assert.strictEqual(openMov.shutterClosedPermitted, false);

      // Reset clock back to live mode
      const resetRes = await fetch(`${baseUrl}/api/world/clock`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ reset: true }),
      });
      assert.strictEqual(resetRes.status, 200);
      assert.strictEqual(((await resetRes.json()) as any).clock.isSimulated, false);
    } finally {
      await app.stop();
    }

    // 4. Regression protection: other 6 canonical NPCs remain completely unaffected across opening/closing/night
    for (const iso of ['2026-11-04T16:00:02.000Z', '2026-11-04T23:57:05.000Z', '2026-11-05T00:05:00.000Z']) {
      const roster = evaluateRosterMovement(NPC_MOVEMENT_PROFILES, iso);
      assert.strictEqual(roster.npcs['carlos-farmacia-attendant']!.controlMode, 'autonomous_business_local');
      assert.strictEqual(roster.npcs['carlos-farmacia-attendant']!.withinAuthorizedDomain, true);
      assert.strictEqual(roster.npcs['elena-border-concierge']!.behaviorProfileType, 'fixed_anchor_scheduled');
      assert.strictEqual(roster.npcs['patricia-citizen-rep']!.behaviorProfileType, 'fixed_anchor_scheduled');
      for (const id of ['mateo-student', 'sofia-cross-border-retail', 'hector-artisan']) {
        assert.strictEqual(roster.npcs[id]!.behaviorProfileType, 'bridge_patrol');
        assert.strictEqual(roster.npcs[id]!.withinAuthorizedDomain, true);
      }
    }
  });

  it('10H. Óptica Fashion rear-wall containment prevents serverPos drift into void & calibration guidance never claims break', async () => {
    const tOpenCalib = Date.parse('2026-11-07T20:38:00.000Z'); // Saturday 13:38 MST (open, calibration window)
    const clock = { ms: tOpenCalib };
    const seq = { seq: 0 };
    const wc = new InMemoryWorldCoreAdapter({ nowFn: () => clock.ms });
    wc.setSimulatedWorldTime(new Date(tOpenCalib).toISOString());

    // 1. Calibration interaction guidance for Valeria must never describe her as on "break"
    const ctxCalib = (await wc.getNpcInteractionContext('valeria-optica-attendant', tOpenCalib))!;
    assert.ok(
      !ctxCalib.dialogueGuidance.en.toLowerCase().includes('break') &&
        !ctxCalib.dialogueGuidance.es.toLowerCase().includes('descanso'),
      `Valeria dialogueGuidance must not mention break/descanso during open hours: ${ctxCalib.dialogueGuidance.en} | ${ctxCalib.dialogueGuidance.es}`
    );

    // 2. Walk player inside Óptica Fashion rear room and push hard West, South, and North against interior walls
    await movePlayerNear(wc, { x: -18.2, y: 0.2, z: 0.2 }, clock, seq);
    for (const dir of [
      { x: -1, z: 0 }, // Push West against DeepBackWall (X=-19.17)
      { x: 0, z: -1 }, // Push South against SouthWall (Z=-1.78)
      { x: 0, z: 1 },  // Push North against NorthWall (Z=+3.03)
    ]) {
      for (let i = 0; i < 6; i++) {
        clock.ms += 400;
        const step = await wc.applyPlayerMovement(
          'player-1',
          { direction: dir, movementMode: 'sprint', sequenceNumber: ++seq.seq },
          clock.ms
        );
        assert.ok(
          step.player.position.x >= -18.80 - 1e-6,
          `Player serverPos escaped West wall into void: x=${step.player.position.x}`
        );
        assert.ok(
          step.player.position.z >= -1.42 - 1e-6 && step.player.position.z <= 2.68 + 1e-6,
          `Player serverPos escaped South/North wall into void: z=${step.player.position.z}`
        );
      }
    }
  });
});
