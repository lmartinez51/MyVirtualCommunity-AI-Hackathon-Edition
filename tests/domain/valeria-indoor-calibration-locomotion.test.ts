import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  BUSINESS_LOCAL_AUTONOMY_PROFILES,
  OPTICA_EXTERIOR_FRONTAGE_ZONE,
  OPTICA_FASHION_AUTONOMY_DOMAIN,
  OPTICA_FASHION_CONNECTED_ZONES,
  OPTICA_FRONT_ENTRANCE_ZONE,
  OPTICA_MAIN_SHOWROOM_ZONE,
  OPTICA_REAR_CALIBRATION_ROOM_ZONE,
  OPTICA_REAR_DOORWAY_ZONE,
  VALERIA_CALIBRATION_STATION_HEADING,
  VALERIA_CALIBRATION_STATION_WAYPOINT_ID,
  VALERIA_CALIBRATION_TASK_ID,
  evaluateValeriaCalibrationLifecycle,
  isConvexPolygonXZ,
  isPointInBusinessDomain,
  isPointInSafeBusinessRoamingRegion,
  isSegmentInBusinessDomain,
  isSegmentInSafeBusinessRoamingRegion,
  resolveAuthorizedBusinessZone,
  validateBusinessLocalGraphRoute,
} from '../../src/domain/npc-autonomous-locomotion.js';
import {
  CORRIDOR_ROUTES,
  CORRIDOR_WAYPOINTS,
  NPC_MOVEMENT_PROFILES,
  evaluateNpcMovement,
  evaluateRosterMovement,
} from '../../src/domain/npc-waypoint-movement.js';
import { evaluateNpcInteractionContext } from '../../src/domain/npc-interaction-context.js';
import { InMemoryWorldCoreAdapter } from '../../src/adapters/in-memory-world-core-adapter.js';
import { validateNpcInteraction } from '../../src/application/validate-npc-interaction.js';

describe('Increment 5.23.1 — Valeria Indoor/Outdoor Autonomous Locomotion & Scheduled Calibration Lifecycle', () => {
  const valeriaProfile = NPC_MOVEMENT_PROFILES['valeria-optica-attendant']!;
  const valeriaLocalProfile = BUSINESS_LOCAL_AUTONOMY_PROFILES['valeria-optica-attendant']!;

  it('9A. Spatial topology & authorization: separate convex zones, door-only transitions, obstacle/neighbor exclusion & task-only rear laboratory', () => {
    // 1. Exterior safe roaming region remains unchanged from Increment 5.22.3
    assert.deepStrictEqual(OPTICA_FASHION_AUTONOMY_DOMAIN.safeRoamingBounds, {
      minX: -10.85,
      maxX: -8.0,
      minY: 0.15,
      maxY: 1.05,
      minZ: -1.65,
      maxZ: 2.9,
    });
    assert.strictEqual(isConvexPolygonXZ(OPTICA_FASHION_AUTONOMY_DOMAIN.polygonXZ), true);
    assert.strictEqual(isConvexPolygonXZ(OPTICA_FASHION_AUTONOMY_DOMAIN.safeRoamingPolygonXZ), true);

    // 2. All 5 connected zones are strictly convex; Zone D1/D2 are strictly taskOnly
    assert.strictEqual(OPTICA_FASHION_CONNECTED_ZONES.length, 5);
    for (const z of OPTICA_FASHION_CONNECTED_ZONES) {
      assert.strictEqual(isConvexPolygonXZ(z.polygonXZ), true, `Zone ${z.zoneId} must be strictly convex`);
    }
    assert.strictEqual(OPTICA_EXTERIOR_FRONTAGE_ZONE.taskOnly, false);
    assert.strictEqual(OPTICA_FRONT_ENTRANCE_ZONE.taskOnly, false);
    assert.strictEqual(OPTICA_MAIN_SHOWROOM_ZONE.taskOnly, false);
    assert.strictEqual(OPTICA_REAR_DOORWAY_ZONE.taskOnly, true);
    assert.strictEqual(OPTICA_REAR_CALIBRATION_ROOM_ZONE.taskOnly, true);

    // 3. Every normal roaming graph edge validates cleanly with activeTaskId = null
    for (const [nodeId, spec] of Object.entries(valeriaLocalProfile.localNodes)) {
      for (const nextId of spec.connectedWaypointIds) {
        const res = validateBusinessLocalGraphRoute(
          [nodeId, nextId],
          OPTICA_FASHION_AUTONOMY_DOMAIN,
          valeriaLocalProfile,
          CORRIDOR_WAYPOINTS,
          null
        );
        assert.strictEqual(res.valid, true, `Normal graph edge ${nodeId}->${nextId} failed: ${res.reason}`);
      }
    }

    // 4. Task-only calibration route validates ONLY when VALERIA_CALIBRATION_TASK_ID is active
    const calRouteIds = CORRIDOR_ROUTES['route_valeria_to_calibration']!.waypointIds;
    const withoutTask = validateBusinessLocalGraphRoute(
      calRouteIds,
      OPTICA_FASHION_AUTONOMY_DOMAIN,
      valeriaLocalProfile,
      CORRIDOR_WAYPOINTS,
      null
    );
    assert.strictEqual(withoutTask.valid, false, 'Normal roaming must reject rear calibration route');

    const withTask = validateBusinessLocalGraphRoute(
      calRouteIds,
      OPTICA_FASHION_AUTONOMY_DOMAIN,
      valeriaLocalProfile,
      CORRIDOR_WAYPOINTS,
      VALERIA_CALIBRATION_TASK_ID
    );
    assert.strictEqual(withTask.valid, true, `Calibration route must validate with task auth: ${withTask.reason}`);

    // 5. Reject illegal obstacles, fixed glass, furniture, neighbors, and direct wall-bypassing shortcuts
    const forbiddenPoints = [
      { label: 'Closed south glass door leaf', pt: { x: -12.02, y: 0.2, z: 0.0 } },
      { label: 'Fixed south sidelight glass', pt: { x: -12.02, y: 0.2, z: -1.22 } },
      { label: 'Fixed north sidelight glass', pt: { x: -12.02, y: 0.2, z: 2.46 } },
      { label: 'Open north door leaf collider', pt: { x: -12.6, y: 0.2, z: 1.78 } },
      { label: 'Long south glass display counter', pt: { x: -14.7, y: 0.2, z: -0.6 } },
      { label: 'North waiting chair 0', pt: { x: -13.66, y: 0.2, z: 2.58 } },
      { label: 'North waiting glass table', pt: { x: -14.26, y: 0.2, z: 2.66 } },
      { label: 'North waiting chair 1', pt: { x: -14.86, y: 0.2, z: 2.58 } },
      { label: 'Deep north eyewear display rack', pt: { x: -16.0, y: 0.2, z: 2.9 } },
      { label: 'Rear burgundy cabinet', pt: { x: -16.56, y: 0.2, z: 0.18 } },
      { label: 'Rear partition solid wall south of doorway', pt: { x: -16.88, y: 0.2, z: 0.2 } },
      { label: 'Calibration workbench solid body', pt: { x: -18.77, y: 0.2, z: -0.5 } },
      { label: 'Neighboring Pharmacy (South Z<-1.80)', pt: { x: -10.5, y: 0.2, z: -2.6 } },
      { label: 'Neighboring Cambios Santa Fe (North Z>3.05)', pt: { x: -10.5, y: 0.2, z: 4.1 } },
      { label: 'Roadway lane (|x|<6.9)', pt: { x: -5.0, y: 0.2, z: 1.2 } },
    ];
    for (const item of forbiddenPoints) {
      assert.strictEqual(
        isPointInSafeBusinessRoamingRegion(item.pt, OPTICA_FASHION_AUTONOMY_DOMAIN, VALERIA_CALIBRATION_TASK_ID),
        false,
        `Forbidden obstacle "${item.label}" must be rejected even with task auth`
      );
    }

    // Rear calibration station is strictly forbidden during normal roaming (activeTaskId = null)
    const calPos = CORRIDOR_WAYPOINTS[VALERIA_CALIBRATION_STATION_WAYPOINT_ID]!.position;
    assert.deepStrictEqual(calPos, { x: -17.85, y: 0.2, z: -0.5 });
    assert.strictEqual(resolveAuthorizedBusinessZone(calPos, OPTICA_FASHION_AUTONOMY_DOMAIN, null), null);
    assert.strictEqual(
      resolveAuthorizedBusinessZone(calPos, OPTICA_FASHION_AUTONOMY_DOMAIN, VALERIA_CALIBRATION_TASK_ID)?.zoneId,
      OPTICA_REAR_CALIBRATION_ROOM_ZONE.zoneId
    );
    assert.strictEqual(isPointInBusinessDomain(calPos, OPTICA_FASHION_AUTONOMY_DOMAIN, null), false);
    assert.strictEqual(isPointInSafeBusinessRoamingRegion(calPos, OPTICA_FASHION_AUTONOMY_DOMAIN, null), false);
    assert.strictEqual(isPointInSafeBusinessRoamingRegion(calPos, OPTICA_FASHION_AUTONOMY_DOMAIN, VALERIA_CALIBRATION_TASK_ID), true);

    // Direct wall/glass-bypassing shortcuts must be rejected
    assert.strictEqual(
      isSegmentInBusinessDomain(
        CORRIDOR_WAYPOINTS['wp_valeria_storefront']!.position,
        CORRIDOR_WAYPOINTS['wp_optica_showroom_center']!.position,
        OPTICA_FASHION_AUTONOMY_DOMAIN,
        VALERIA_CALIBRATION_TASK_ID
      ),
      false,
      'Direct exterior -> showroom segment bypassing front doorway must be rejected'
    );
    assert.strictEqual(
      isSegmentInSafeBusinessRoamingRegion(
        CORRIDOR_WAYPOINTS['wp_optica_showroom_counter_west']!.position,
        calPos,
        OPTICA_FASHION_AUTONOMY_DOMAIN,
        VALERIA_CALIBRATION_TASK_ID
      ),
      false,
      'Direct showroom -> calibration station segment crossing rear partition wall must be rejected'
    );
  });

  it('9B & 9C. Normal indoor/outdoor autonomous roaming: traverses open doorway in both directions, visits showroom & frontage, never enters rear lab, C^0 continuous', () => {
    // Wednesday 2026-11-04 10:00:00..10:25:00 MST (17:00..17:25 UTC) at 0.5s steps
    const startMs = Date.parse('2026-11-04T17:00:00.000Z');
    const dtSec = 0.5;
    const seenZones = new Set<string>();
    const seenWaypoints = new Set<string>();
    let enteredShowroomCount = 0;
    let exitedToExteriorCount = 0;
    let prevZone: string | null = null;
    let prevSnap = evaluateNpcMovement(valeriaProfile, startMs);

    for (let s = dtSec; s <= 1500; s += dtSec) {
      const snap = evaluateNpcMovement(valeriaProfile, startMs + s * 1000);
      assert.strictEqual(snap.withinAuthorizedDomain, true);
      assert.strictEqual(snap.controlMode, 'autonomous_business_local');
      assert.strictEqual(snap.activityState, 'available');
      assert.strictEqual(snap.physicalCalibrationActive, false);

      // Must NEVER enter task-only rear laboratory during normal roaming
      assert.notStrictEqual(snap.currentZoneId, OPTICA_REAR_DOORWAY_ZONE.zoneId);
      assert.notStrictEqual(snap.currentZoneId, OPTICA_REAR_CALIBRATION_ROOM_ZONE.zoneId);
      assert.ok(snap.position.x >= -15.96, `Valeria entered rear room X=${snap.position.x} during normal roaming`);
      assert.ok(
        isPointInSafeBusinessRoamingRegion(snap.position, OPTICA_FASHION_AUTONOMY_DOMAIN, null),
        `Valeria exited normal safe zones at +${s}s: ${JSON.stringify(snap.position)}`
      );

      // Every time she crosses the storefront plane X = -12.02, she must be centered inside the open north door (Z in [1.02, 1.38])
      if (snap.position.x <= -11.35 && snap.position.x >= -13.15) {
        assert.ok(
          snap.position.z >= 1.02 && snap.position.z <= 1.38,
          `Doorway crossing Z=${snap.position.z} outside open north doorway clearance [1.02, 1.38]`
        );
      }

      // C^0 continuity check (speed 1.10 m/s -> <= 0.65m per 0.5s step)
      const stepDist = Math.hypot(
        snap.position.x - prevSnap.position.x,
        snap.position.y - prevSnap.position.y,
        snap.position.z - prevSnap.position.z
      );
      assert.ok(stepDist <= 0.65, `Discontinuity detected at +${s}s: jumped ${stepDist.toFixed(3)}m`);

      if (snap.movementState === 'moving') {
        assert.strictEqual(snap.currentSpeedMps, 1.1);
        assert.ok(snap.activeSegmentEndPosition, 'Must expose activeSegmentEndPosition while moving');
      } else {
        assert.strictEqual(snap.currentSpeedMps, 0);
        // Never awkwardly dwell stationary inside the front doorway threshold
        assert.notStrictEqual(
          snap.currentWaypointId,
          'wp_optica_door_threshold',
          'Valeria must not stop and dwell inside doorway threshold'
        );
      }

      if (snap.currentZoneId) {
        seenZones.add(snap.currentZoneId);
        if (prevZone === OPTICA_FRONT_ENTRANCE_ZONE.zoneId && snap.currentZoneId === OPTICA_MAIN_SHOWROOM_ZONE.zoneId) {
          enteredShowroomCount++;
        }
        if (prevZone === OPTICA_FRONT_ENTRANCE_ZONE.zoneId && snap.currentZoneId === OPTICA_EXTERIOR_FRONTAGE_ZONE.zoneId) {
          exitedToExteriorCount++;
        }
        prevZone = snap.currentZoneId;
      }
      seenWaypoints.add(snap.currentWaypointId);
      prevSnap = snap;
    }

    assert.ok(seenZones.has(OPTICA_EXTERIOR_FRONTAGE_ZONE.zoneId), 'Must use Zone A exterior frontage');
    assert.ok(seenZones.has(OPTICA_FRONT_ENTRANCE_ZONE.zoneId), 'Must traverse Zone B open front entrance');
    assert.ok(seenZones.has(OPTICA_MAIN_SHOWROOM_ZONE.zoneId), 'Must circulate inside Zone C main showroom');
    assert.ok(enteredShowroomCount >= 2, `Expected >=2 showroom entries, got ${enteredShowroomCount}`);
    assert.ok(exitedToExteriorCount >= 2, `Expected >=2 showroom exits back outside, got ${exitedToExteriorCount}`);

    const showroomNodesVisited = [
      'wp_optica_showroom_counter_east',
      'wp_optica_showroom_center',
      'wp_optica_showroom_counter_west',
      'wp_optica_showroom_waiting',
      'wp_optica_showroom_north_display',
    ].filter((id) => seenWaypoints.has(id));
    assert.ok(
      showroomNodesVisited.length >= 4,
      `Expected >=4 distinct showroom nodes visited, got ${showroomNodesVisited.join(', ')}`
    );
  });

  it('9D & 9E. Scheduled calibration lifecycle (13:00-13:45 Mon-Sat America/Ciudad_Juarez): continuous entry, station arrival, busy posture, continuous exit & DST/Sunday boundaries', () => {
    // Wednesday Nov 4, 2026 (MST = UTC-7): 13:00 MST = 20:00 UTC, 13:45 MST = 20:45 UTC
    const tBeforeStart = Date.parse('2026-11-04T19:59:59.000Z'); // 12:59:59 MST
    const tExactStart = Date.parse('2026-11-04T20:00:00.000Z');  // 13:00:00 MST
    const tMidTask = Date.parse('2026-11-04T20:20:00.000Z');     // 13:20:00 MST
    const tBeforeEnd = Date.parse('2026-11-04T20:44:59.000Z');   // 13:44:59 MST
    const tExactEnd = Date.parse('2026-11-04T20:45:00.000Z');    // 13:45:00 MST

    // 1. Just before start (12:59:59 MST)
    const snapBefore = evaluateNpcMovement(valeriaProfile, tBeforeStart);
    assert.strictEqual(snapBefore.activityState, 'available');
    assert.strictEqual(snapBefore.controlMode, 'autonomous_business_local');
    assert.strictEqual(snapBefore.physicalCalibrationActive, false);
    assert.notStrictEqual(snapBefore.currentZoneId, OPTICA_REAR_CALIBRATION_ROOM_ZONE.zoneId);

    // 2. Exact start (13:00:00 MST) -> zero teleportation from 12:59:59!
    const snapStart = evaluateNpcMovement(valeriaProfile, tExactStart);
    assert.strictEqual(snapStart.activityState, 'busy');
    assert.strictEqual(snapStart.controlMode, 'scheduled_routine_transition');
    assert.strictEqual(snapStart.activeTaskId, VALERIA_CALIBRATION_TASK_ID);
    assert.strictEqual(snapStart.destinationWaypointId, VALERIA_CALIBRATION_STATION_WAYPOINT_ID);
    assert.strictEqual(snapStart.activeRouteId, 'route_valeria_to_calibration');
    const jumpAtStart = Math.hypot(
      snapStart.position.x - snapBefore.position.x,
      snapStart.position.z - snapBefore.position.z
    );
    assert.ok(jumpAtStart <= 1.2, `Teleportation detected at 13:00:00 start: ${jumpAtStart.toFixed(3)}m`);

    // 3. Sweep outbound walk every 0.5s from 13:00:00 to 13:00:30: continuous, passes doorway + showroom + rear doorway -> arrives at Optica_CalibrationStation
    const outboundZones = new Set<string>();
    let prevOut = snapStart;
    let arrivedAtStation = false;
    for (let s = 0.5; s <= 30; s += 0.5) {
      const cur = evaluateNpcMovement(valeriaProfile, tExactStart + s * 1000);
      const step = Math.hypot(cur.position.x - prevOut.position.x, cur.position.z - prevOut.position.z);
      assert.ok(step <= 0.65, `Outbound jump at +${s}s: ${step.toFixed(3)}m`);
      assert.strictEqual(cur.withinAuthorizedDomain, true);
      if (cur.currentZoneId) outboundZones.add(cur.currentZoneId);
      if (cur.movementState === 'arrived' && cur.currentWaypointId === VALERIA_CALIBRATION_STATION_WAYPOINT_ID) {
        arrivedAtStation = true;
        assert.deepStrictEqual(cur.position, { x: -17.85, y: 0.2, z: -0.5 });
        assert.deepStrictEqual(cur.forwardHeading, VALERIA_CALIBRATION_STATION_HEADING);
        assert.strictEqual(cur.physicalCalibrationActive, true);
      }
      prevOut = cur;
    }
    assert.ok(arrivedAtStation, 'Valeria must continuously walk to and arrive at Optica_CalibrationStation');
    assert.ok(outboundZones.has(OPTICA_MAIN_SHOWROOM_ZONE.zoneId), 'Outbound walk must traverse main showroom');
    assert.ok(outboundZones.has(OPTICA_REAR_DOORWAY_ZONE.zoneId), 'Outbound walk must cross open rear doorway');
    assert.ok(outboundZones.has(OPTICA_REAR_CALIBRATION_ROOM_ZONE.zoneId), 'Outbound walk must enter rear calibration room');

    // 4. Mid-task (13:20:00 MST): remains at workstation, busy/contextual interaction semantics
    const snapMid = evaluateNpcMovement(valeriaProfile, tMidTask);
    assert.strictEqual(snapMid.movementState, 'waiting');
    assert.strictEqual(snapMid.currentWaypointId, VALERIA_CALIBRATION_STATION_WAYPOINT_ID);
    assert.deepStrictEqual(snapMid.position, { x: -17.85, y: 0.2, z: -0.5 });
    assert.deepStrictEqual(snapMid.forwardHeading, { x: -1, z: 0 });
    assert.strictEqual(snapMid.physicalCalibrationActive, true);
    assert.strictEqual(snapMid.autonomousBehaviorState, 'scheduled_station_task');

    const ctxMid = evaluateNpcInteractionContext('valeria-optica-attendant', tMidTask)!;
    assert.strictEqual(ctxMid.activityState, 'busy');
    assert.strictEqual(ctxMid.interactionAvailability, 'available_contextual');
    assert.strictEqual(ctxMid.canInitiateConversation, true);
    assert.strictEqual(ctxMid.isAvailableForNormalService, false);

    // 5. Just before task end (13:44:59 MST) -> still at workstation
    const snapBeforeEnd = evaluateNpcMovement(valeriaProfile, tBeforeEnd);
    assert.deepStrictEqual(snapBeforeEnd.position, { x: -17.85, y: 0.2, z: -0.5 });
    assert.strictEqual(snapBeforeEnd.physicalCalibrationActive, true);

    // 6. Exact task end (13:45:00 MST) -> starts continuous return walk from (-17.85, 0.20, -0.50), zero jump!
    const snapEnd = evaluateNpcMovement(valeriaProfile, tExactEnd);
    assert.strictEqual(snapEnd.movementState, 'moving');
    assert.strictEqual(snapEnd.activeRouteId, 'route_valeria_from_calibration');
    assert.strictEqual(snapEnd.physicalCalibrationActive, false);
    assert.deepStrictEqual(snapEnd.position, { x: -17.85, y: 0.2, z: -0.5 });

    // Sweep return walk every 0.5s through 13:45:25 -> releases lab auth upon entering showroom & resumes afternoon autonomy smoothly!
    const returnZones = new Set<string>();
    let releasedLabAuthInShowroom = false;
    let resumedNormalAutonomy = false;
    let prevRet = snapEnd;
    for (let s = 0.5; s <= 25; s += 0.5) {
      const cur = evaluateNpcMovement(valeriaProfile, tExactEnd + s * 1000);
      const step = Math.hypot(cur.position.x - prevRet.position.x, cur.position.z - prevRet.position.z);
      assert.ok(step <= 0.65, `Return walk jump at +${s}s: ${step.toFixed(3)}m`);
      assert.strictEqual(cur.withinAuthorizedDomain, true);
      if (cur.currentZoneId) returnZones.add(cur.currentZoneId);
      if (cur.currentZoneId === OPTICA_MAIN_SHOWROOM_ZONE.zoneId && cur.activeTaskId === null) {
        releasedLabAuthInShowroom = true;
      }
      if (cur.controlMode === 'autonomous_business_local' && cur.activeTaskId === null) {
        resumedNormalAutonomy = true;
      }
      prevRet = cur;
    }
    assert.ok(returnZones.has(OPTICA_REAR_DOORWAY_ZONE.zoneId), 'Return walk must cross rear doorway');
    assert.ok(returnZones.has(OPTICA_MAIN_SHOWROOM_ZONE.zoneId), 'Return walk must cross showroom');
    assert.ok(returnZones.has(OPTICA_FRONT_ENTRANCE_ZONE.zoneId), 'Return walk must cross front doorway');
    assert.ok(returnZones.has(OPTICA_EXTERIOR_FRONTAGE_ZONE.zoneId), 'Return walk must reach exterior frontage');
    assert.strictEqual(releasedLabAuthInShowroom, true, 'Lab authorization must be released once Valeria exits into showroom');
    assert.strictEqual(resumedNormalAutonomy, true, 'Must seamlessly resume afternoon business autonomy');

    // 7. Repeatability & timezone boundaries (DST July MDT & closed Sunday)
    assert.deepStrictEqual(
      evaluateNpcMovement(valeriaProfile, tMidTask),
      evaluateNpcMovement(valeriaProfile, tMidTask)
    );
    // Wednesday July 8, 2026 13:15 MDT (UTC-6 = 19:15 UTC)
    const snapSummerMdt = evaluateNpcMovement(valeriaProfile, '2026-07-08T19:15:00.000Z');
    assert.strictEqual(snapSummerMdt.activityState, 'busy');
    assert.strictEqual(snapSummerMdt.currentWaypointId, VALERIA_CALIBRATION_STATION_WAYPOINT_ID);
    assert.strictEqual(snapSummerMdt.physicalCalibrationActive, true);

    // Sunday Nov 8, 2026 13:15 MST (UTC-7 = 20:15 UTC) -> closed on Sunday, NO calibration, stays safely inside at wp_valeria_off_duty_station
    const snapSunday = evaluateNpcMovement(valeriaProfile, '2026-11-08T20:15:00.000Z');
    assert.strictEqual(snapSunday.activityState, 'off_duty');
    assert.strictEqual(snapSunday.currentWaypointId, 'wp_valeria_off_duty_station');
    assert.strictEqual(snapSunday.businessLifecycleState, 'off_duty_inside');
    assert.strictEqual(snapSunday.shutterClosedPermitted, true);
    assert.strictEqual(snapSunday.physicalCalibrationActive, false);
  });

  it('9D-2. Starting calibration while ALREADY inside the showroom routes directly through interior without walking back outside', () => {
    // Find a morning timestamp near 12:59:59 shifted by a pause interval across 2 cycles (~170s) so Valeria is inside Zone C at 12:59:59.999
    const calStartMs = Date.parse('2026-11-04T20:00:00.000Z'); // 13:00:00 MST
    let chosenPauseMs = 0;
    for (let shiftSec = 10; shiftSec <= 180; shiftSec += 2) {
      const testSnap = evaluateNpcMovement(valeriaProfile, calStartMs - 100, {
        pauseIntervalsByNpcId: {
          'valeria-optica-attendant': [
            {
              npcId: 'valeria-optica-attendant',
              playerId: 'player-1',
              startedAtMs: calStartMs - 240_000,
              endedAtMs: calStartMs - 240_000 + shiftSec * 1000,
              reason: 'authorized_player_interaction',
            },
          ],
        },
      });
      if (testSnap.currentZoneId === OPTICA_MAIN_SHOWROOM_ZONE.zoneId && testSnap.position.x < -13.6) {
        chosenPauseMs = shiftSec * 1000;
        break;
      }
    }
    assert.ok(chosenPauseMs > 0, 'Must locate a showroom-positioned pre-task state');

    const opts = {
      pauseIntervalsByNpcId: {
        'valeria-optica-attendant': [
          {
            npcId: 'valeria-optica-attendant',
            playerId: 'player-1',
            startedAtMs: calStartMs - 240_000,
            endedAtMs: calStartMs - 240_000 + chosenPauseMs,
            reason: 'authorized_player_interaction' as const,
          },
        ],
      },
    };

    const beforeStart = evaluateNpcMovement(valeriaProfile, calStartMs - 1000, opts);
    assert.strictEqual(beforeStart.currentZoneId, OPTICA_MAIN_SHOWROOM_ZONE.zoneId);

    const atStart = evaluateNpcMovement(valeriaProfile, calStartMs, opts);
    const stepAtBoundary = Math.hypot(
      atStart.position.x - beforeStart.position.x,
      atStart.position.z - beforeStart.position.z
    );
    assert.ok(stepAtBoundary <= 1.2, `Zero-teleport handoff from showroom: jumped ${stepAtBoundary.toFixed(3)}m`);

    // Verify she NEVER walks back out to exterior Zone A during outbound calibration transit
    for (let s = 0; s <= 12; s += 0.5) {
      const cur = evaluateNpcMovement(valeriaProfile, calStartMs + s * 1000, opts);
      assert.notStrictEqual(
        cur.currentZoneId,
        OPTICA_EXTERIOR_FRONTAGE_ZONE.zoneId,
        'When already inside showroom at 13:00, Valeria must NOT exit to exterior before heading to rear room'
      );
      assert.notStrictEqual(
        cur.currentZoneId,
        OPTICA_FRONT_ENTRANCE_ZONE.zoneId,
        'When already inside showroom at 13:00, Valeria must take direct interior route'
      );
    }

    const arrived = evaluateNpcMovement(valeriaProfile, calStartMs + 12_000, opts);
    assert.strictEqual(arrived.currentWaypointId, VALERIA_CALIBRATION_STATION_WAYPOINT_ID);
    assert.strictEqual(arrived.physicalCalibrationActive, true);
  });

  it('9E-2. Player interaction suspends & resumes Valeria cleanly during outbound entry, active calibration, and return walk without teleport or restart', async () => {
    const movePlayerNear = async (
      worldCore: InMemoryWorldCoreAdapter,
      targetPos: { x: number; y: number; z: number },
      clockRef: { ms: number },
      seqRef: { seq: number }
    ) => {
      for (let i = 0; i < 80; i++) {
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

    const tOutbound4s = Date.parse('2026-11-04T20:00:04.000Z'); // 13:00:04 MST (walking through showroom/entrance)
    const clock = { ms: tOutbound4s };
    const seq = { seq: 0 };
    const worldCore = new InMemoryWorldCoreAdapter({ nowFn: () => clock.ms });
    worldCore.setSimulatedWorldTime(new Date(tOutbound4s).toISOString());

    // A. Interrupt during outbound walk at 13:00:04
    const beforeOut = (await worldCore.getNpcMovement('valeria-optica-attendant', tOutbound4s))!;
    assert.strictEqual(beforeOut.movementState, 'moving');
    await movePlayerNear(worldCore, beforeOut.position, clock, seq);

    const outInteract = await validateNpcInteraction(worldCore, {
      playerId: 'player-1',
      npcId: 'valeria-optica-attendant',
      intent: 'talk',
      evaluatedAt: tOutbound4s,
    });
    assert.strictEqual(outInteract.allowed, true);

    const pausedOut = (await worldCore.getNpcMovement('valeria-optica-attendant', tOutbound4s + 15_000))!;
    assert.strictEqual(pausedOut.movementState, 'stationary');
    assert.strictEqual(pausedOut.isSuspendedByPlayerInteraction, true);
    assert.strictEqual(pausedOut.controlMode, 'player_interaction_suspended');
    assert.deepStrictEqual(pausedOut.position, beforeOut.position);

    // Resume at 13:00:19 -> zero jump, completes walk and arrives at station by 13:00:35
    const resumedOut = (await worldCore.releaseNpcMovementFromInteraction(
      'player-1',
      'valeria-optica-attendant',
      tOutbound4s + 15_000
    ))!;
    assert.strictEqual(resumedOut.isSuspendedByPlayerInteraction, false);
    assert.deepStrictEqual(resumedOut.position, beforeOut.position);

    const arrivedAfterPause = (await worldCore.getNpcMovement('valeria-optica-attendant', tOutbound4s + 35_000))!;
    assert.strictEqual(arrivedAfterPause.currentWaypointId, VALERIA_CALIBRATION_STATION_WAYPOINT_ID);
    assert.strictEqual(arrivedAfterPause.physicalCalibrationActive, true);

    // B. Interrupt during active calibration at 13:20:00
    const tCalib20 = Date.parse('2026-11-04T20:20:00.000Z');
    await movePlayerNear(worldCore, arrivedAfterPause.position, clock, seq);
    const calInteract = await validateNpcInteraction(worldCore, {
      playerId: 'player-1',
      npcId: 'valeria-optica-attendant',
      intent: 'talk',
      evaluatedAt: tCalib20,
    });
    assert.strictEqual(calInteract.allowed, true);

    const pausedCal = (await worldCore.getNpcMovement('valeria-optica-attendant', tCalib20 + 20_000))!;
    assert.strictEqual(pausedCal.isSuspendedByPlayerInteraction, true);
    assert.strictEqual(pausedCal.physicalCalibrationActive, false);
    assert.deepStrictEqual(pausedCal.position, { x: -17.85, y: 0.2, z: -0.5 });

    const resumedCal = (await worldCore.releaseNpcMovementFromInteraction(
      'player-1',
      'valeria-optica-attendant',
      tCalib20 + 20_000
    ))!;
    assert.strictEqual(resumedCal.isSuspendedByPlayerInteraction, false);
    assert.strictEqual(resumedCal.physicalCalibrationActive, true);
    assert.strictEqual(resumedCal.currentWaypointId, VALERIA_CALIBRATION_STATION_WAYPOINT_ID);

    // C. Interrupt during return walk at 13:45:04
    const tRet4s = Date.parse('2026-11-04T20:45:04.000Z');
    const beforeRet = (await worldCore.getNpcMovement('valeria-optica-attendant', tRet4s))!;
    assert.strictEqual(beforeRet.movementState, 'moving');
    assert.strictEqual(beforeRet.activeRouteId, 'route_valeria_from_calibration');

    await movePlayerNear(worldCore, beforeRet.position, clock, seq);
    const retInteract = await validateNpcInteraction(worldCore, {
      playerId: 'player-1',
      npcId: 'valeria-optica-attendant',
      intent: 'talk',
      evaluatedAt: tRet4s,
    });
    assert.strictEqual(retInteract.allowed, true);

    const pausedRet = (await worldCore.getNpcMovement('valeria-optica-attendant', tRet4s + 20_000))!;
    assert.strictEqual(pausedRet.movementState, 'stationary');
    assert.strictEqual(pausedRet.isSuspendedByPlayerInteraction, true);
    assert.deepStrictEqual(pausedRet.position, beforeRet.position);

    const resumedRet = (await worldCore.releaseNpcMovementFromInteraction(
      'player-1',
      'valeria-optica-attendant',
      tRet4s + 20_000
    ))!;
    assert.strictEqual(resumedRet.isSuspendedByPlayerInteraction, false);
    assert.deepStrictEqual(resumedRet.position, beforeRet.position);

    const backOutside = (await worldCore.getNpcMovement('valeria-optica-attendant', tRet4s + 30_000))!;
    assert.strictEqual(backOutside.controlMode, 'autonomous_business_local');
    assert.strictEqual(backOutside.activeTaskId, null);
    assert.strictEqual(backOutside.withinAuthorizedDomain, true);
  });

  it('9D-3. Invalid calibration destination/route triggers safe fallback without fabricating calibration', () => {
    const brokenRegistry = {
      ...CORRIDOR_WAYPOINTS,
      wp_optica_calibration_station: {
        ...CORRIDOR_WAYPOINTS['wp_optica_calibration_station']!,
        position: { x: -25.0, y: 0.2, z: -0.5 }, // Outside hollow interior!
      },
    };
    const res = evaluateValeriaCalibrationLifecycle({
      rawEvaluatedAtMs: Date.parse('2026-11-04T20:15:00.000Z'),
      civil: {
        timeZone: 'America/Ciudad_Juarez',
        year: 2026,
        month: 11,
        day: 4,
        hour: 13,
        minute: 15,
        second: 0,
        millisecond: 0,
        dayOfWeek: 3,
        utcOffsetMinutes: -420,
        isoCivilString: '2026-11-04T13:15:00-07:00',
      },
      waypointsRegistry: brokenRegistry,
    });
    assert.ok(res);
    assert.strictEqual(res!.taskDestinationReachable, false);
    assert.strictEqual(res!.physicalCalibrationActive, false);
    assert.strictEqual(res!.movementState, 'stationary');
    assert.strictEqual(res!.actualSpeedMps, 0);
    assert.strictEqual(res!.withinAuthorizedDomain, true);
    assert.notStrictEqual(res!.currentWaypointId, VALERIA_CALIBRATION_STATION_WAYPOINT_ID);
  });

  it('9F. Regression protection: other 6 canonical NPCs remain strictly unaffected', () => {
    const rosterMidday = evaluateRosterMovement(NPC_MOVEMENT_PROFILES, '2026-11-04T20:15:00.000Z'); // 13:15 MST
    // Carlos stays inside Farmacias Benavides
    assert.strictEqual(rosterMidday.npcs['carlos-farmacia-attendant']!.controlMode, 'autonomous_business_local');
    assert.strictEqual(rosterMidday.npcs['carlos-farmacia-attendant']!.withinAuthorizedDomain, true);
    // Elena & Patricia stay anchored
    assert.strictEqual(rosterMidday.npcs['elena-border-concierge']!.behaviorProfileType, 'fixed_anchor_scheduled');
    assert.deepStrictEqual(rosterMidday.npcs['elena-border-concierge']!.position, { x: 19.2, y: 0.2, z: 39.5 });
    assert.strictEqual(rosterMidday.npcs['patricia-citizen-rep']!.behaviorProfileType, 'fixed_anchor_scheduled');
    assert.deepStrictEqual(rosterMidday.npcs['patricia-citizen-rep']!.position, { x: -7.6, y: 0.0, z: 18.8 });
    // Bridge patrol trio stays on bridge
    for (const id of ['mateo-student', 'sofia-cross-border-retail', 'hector-artisan']) {
      assert.strictEqual(rosterMidday.npcs[id]!.behaviorProfileType, 'bridge_patrol');
      assert.strictEqual(rosterMidday.npcs[id]!.withinAuthorizedDomain, true);
    }
  });
});
