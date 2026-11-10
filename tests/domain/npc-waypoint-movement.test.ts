import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  CORRIDOR_WAYPOINTS,
  CORRIDOR_ROUTES,
  NPC_MOVEMENT_PROFILES,
  evaluateNpcMovement,
  evaluateRosterMovement,
} from '../../src/domain/npc-waypoint-movement.js';
import { parseLocalCivilTimeToUtc } from '../../src/domain/civil-time.js';

describe('Deterministic NPC Waypoint Movement & Spatial Presence (Domain Tests)', () => {
  describe('1. Waypoint Identity and Canonical Place Invariants', () => {
    it('all waypoints have unique stable IDs, valid coordinates, and reference canonical places', () => {
      const canonicalPlaces = new Set([
        'infrastructure:puente-paso-del-norte',
        'pharmacy:farmacias-benavides',
        'optical:optica-fashion',
        'desk:patricia-citizen-rep',
      ]);

      const waypointKeys = Object.keys(CORRIDOR_WAYPOINTS);
      assert.ok(waypointKeys.length >= 15, `Expected >= 15 corridor waypoints, got ${waypointKeys.length}`);

      for (const [id, wp] of Object.entries(CORRIDOR_WAYPOINTS)) {
        assert.strictEqual(wp.id, id);
        assert.ok(canonicalPlaces.has(wp.associatedPlaceId), `Waypoint ${id} references unknown place: ${wp.associatedPlaceId}`);
        assert.ok(typeof wp.position.x === 'number' && Number.isFinite(wp.position.x));
        assert.ok(typeof wp.position.y === 'number' && Number.isFinite(wp.position.y));
        assert.ok(typeof wp.position.z === 'number' && Number.isFinite(wp.position.z));
        assert.ok(wp.label.en && wp.label.en.length > 0);
        assert.ok(wp.label.es && wp.label.es.length > 0);
        assert.ok(Array.isArray(wp.outgoingWaypointIds));
      }
    });

    it('each outgoing waypoint connection exists in the master waypoint registry', () => {
      for (const [id, wp] of Object.entries(CORRIDOR_WAYPOINTS)) {
        for (const outId of wp.outgoingWaypointIds) {
          assert.ok(CORRIDOR_WAYPOINTS[outId], `Waypoint ${id} has invalid outgoing target: ${outId}`);
        }
      }
    });
  });

  describe('2. Route Validation & Graph Connectivity', () => {
    it('all canonical routes have at least 2 waypoints, positive distance, and valid segments', () => {
      const routeKeys = Object.keys(CORRIDOR_ROUTES);
      assert.ok(routeKeys.length >= 7, `Expected at least 7 canonical routes, got ${routeKeys.length}`);

      for (const [id, route] of Object.entries(CORRIDOR_ROUTES)) {
        assert.strictEqual(route.id, id);
        assert.ok(route.waypointIds.length >= 2);
        assert.ok(route.totalDistanceMeters > 0, `Route ${id} has non-positive distance`);
        assert.ok(route.totalDurationSeconds > 0, `Route ${id} has non-positive duration`);
        assert.strictEqual(route.segments.length, route.waypointIds.length - 1);

        for (const seg of route.segments) {
          assert.ok(CORRIDOR_WAYPOINTS[seg.fromWaypointId]);
          assert.ok(CORRIDOR_WAYPOINTS[seg.toWaypointId]);
          assert.ok(seg.distanceMeters > 0);
          assert.ok(seg.durationSeconds >= 5);
        }
      }
    });
  });

  describe('3. Multi-Segment Route Traversal & Piecewise Interpolation', () => {
    it('progresses smoothly across segments for multi-segment route (Carlos Farmacia to Lunch Benches)', () => {
      const profile = NPC_MOVEMENT_PROFILES['carlos-farmacia-attendant']!;
      assert.ok(profile);

      // On a Wednesday (day 3), Carlos departs at 14:00:00 CST
      // Departure instant: 2026-11-04T14:00:00-07:00 (which is 21:00:00 UTC)
      const t0 = new Date('2026-11-04T21:00:00Z');
      const snap0 = evaluateNpcMovement(profile, t0);
      assert.strictEqual(snap0.movementState, 'moving');
      assert.strictEqual(snap0.progression, 0.0);
      assert.strictEqual(snap0.currentWaypointId, 'wp_carlos_counter');
      assert.strictEqual(snap0.destinationWaypointId, 'wp_east_dining_benches');
      assert.strictEqual(snap0.position.x, 10.5);
      assert.strictEqual(snap0.position.z, 0.0);

      // Halfway through Carlos's route
      const routeDur = CORRIDOR_ROUTES['route_carlos_to_lunch']!.totalDurationSeconds;
      const tMid = new Date(t0.getTime() + Math.floor(routeDur / 2) * 1000);
      const snapMid = evaluateNpcMovement(profile, tMid);
      assert.strictEqual(snapMid.movementState, 'moving');
      assert.ok(snapMid.progression > 0.4 && snapMid.progression < 0.6);
      assert.ok(snapMid.position.z > 0 && snapMid.position.z < 22);

      // At route arrival
      const tArrive = new Date(t0.getTime() + routeDur * 1000);
      const snapArrive = evaluateNpcMovement(profile, tArrive);
      assert.strictEqual(snapArrive.movementState, 'arrived');
      assert.strictEqual(snapArrive.progression, 1.0);
      assert.strictEqual(snapArrive.currentWaypointId, 'wp_east_dining_benches');
      assert.strictEqual(snapArrive.position.x, 8.5);
      assert.strictEqual(snapArrive.position.z, 22.0);

      // Waiting at destination (5 minutes into lunch)
      const tWaiting = new Date(t0.getTime() + 300 * 1000);
      const snapWaiting = evaluateNpcMovement(profile, tWaiting);
      assert.strictEqual(snapWaiting.movementState, 'waiting');
      assert.strictEqual(snapWaiting.currentWaypointId, 'wp_east_dining_benches');
      assert.strictEqual(snapWaiting.position.x, 8.5);
      assert.strictEqual(snapWaiting.position.z, 22.0);
    });
  });

  describe('4. Return Route Progression & Arrival Back at Origin', () => {
    it('executes return journey and arrives back at primary counter waypoint', () => {
      const profile = NPC_MOVEMENT_PROFILES['carlos-farmacia-attendant']!;

      // Return departure is at 14:59:00 CST (21:59:00 UTC)
      const tReturn0 = new Date('2026-11-04T21:59:00Z');
      const snapReturn0 = evaluateNpcMovement(profile, tReturn0);
      assert.strictEqual(snapReturn0.movementState, 'moving');
      assert.strictEqual(snapReturn0.destinationWaypointId, 'wp_carlos_counter');
      assert.strictEqual(snapReturn0.activeRouteId, 'route_carlos_to_counter');

      // Halfway back
      const returnDur = CORRIDOR_ROUTES['route_carlos_to_counter']!.totalDurationSeconds;
      const tReturnMid = new Date(tReturn0.getTime() + Math.floor(returnDur / 2) * 1000);
      const snapReturnMid = evaluateNpcMovement(profile, tReturnMid);
      assert.strictEqual(snapReturnMid.movementState, 'moving');
      assert.ok(snapReturnMid.progression > 0.4 && snapReturnMid.progression < 0.6);

      // Arrival back at counter
      const tBack = new Date(tReturn0.getTime() + returnDur * 1000);
      const snapBack = evaluateNpcMovement(profile, tBack);
      assert.strictEqual(snapBack.movementState, 'arrived');
      assert.strictEqual(snapBack.currentWaypointId, 'wp_carlos_counter');

      // 15:05:00 CST (past return dwell) -> stationary at primary counter
      const tDuty = new Date('2026-11-04T22:05:00Z');
      const snapDuty = evaluateNpcMovement(profile, tDuty);
      assert.strictEqual(snapDuty.movementState, 'stationary');
      assert.strictEqual(snapDuty.currentWaypointId, 'wp_carlos_counter');
      assert.strictEqual(snapDuty.position.x, 10.5);
      assert.strictEqual(snapDuty.position.z, 0.0);
    });
  });

  describe('5. Elena Ramos Shift Handover & Plaza Inspection', () => {
    it('moves Elena from Kiosk to Bridge Plaza at 13:30 CST and returns at 14:29 CST', () => {
      const profile = NPC_MOVEMENT_PROFILES['elena-border-concierge']!;

      // 10:00:00 CST -> stationary at Kiosk
      const tMorning = new Date('2026-11-04T17:00:00Z'); // 10:00 CST
      const snapMorning = evaluateNpcMovement(profile, tMorning);
      assert.strictEqual(snapMorning.movementState, 'stationary');
      assert.strictEqual(snapMorning.currentWaypointId, 'wp_elena_kiosk');
      assert.strictEqual(snapMorning.position.x, 19.2);
      assert.strictEqual(snapMorning.position.z, 39.5);

      // 13:30:05 CST -> moving towards Plaza
      const tHandover = new Date('2026-11-04T20:30:05Z');
      const snapHandover = evaluateNpcMovement(profile, tHandover);
      assert.strictEqual(snapHandover.movementState, 'moving');
      assert.strictEqual(snapHandover.destinationWaypointId, 'wp_bridge_plaza');
      assert.strictEqual(snapHandover.activeRouteId, 'route_elena_kiosk_to_plaza');

      // 13:35:00 CST -> waiting at Plaza
      const tPlaza = new Date('2026-11-04T20:35:00Z');
      const snapPlaza = evaluateNpcMovement(profile, tPlaza);
      assert.strictEqual(snapPlaza.movementState, 'waiting');
      assert.strictEqual(snapPlaza.currentWaypointId, 'wp_bridge_plaza');
      assert.strictEqual(snapPlaza.position.x, 17.8);
      assert.strictEqual(snapPlaza.position.z, 42.0);
    });
  });

  describe('6. Equivalence of Large Time Advancements vs Sequential Steps', () => {
    it('produces identical state when evaluating directly after a large time jump as sequential steps', () => {
      const profile = NPC_MOVEMENT_PROFILES['valeria-optica-attendant']!;
      const baseTime = new Date('2026-11-04T19:00:00Z').getTime(); // 12:00 CST

      // Sequential evaluation every 10 minutes up to 13:10 CST (70 minutes total)
      let sequentialSnap = null;
      for (let sec = 0; sec <= 70 * 60; sec += 600) {
        sequentialSnap = evaluateNpcMovement(profile, new Date(baseTime + sec * 1000));
      }

      // Direct single large jump evaluation at exactly 70 minutes (13:10 CST)
      const directSnap = evaluateNpcMovement(profile, new Date(baseTime + 70 * 60 * 1000));

      assert.deepStrictEqual(directSnap, sequentialSnap);
      assert.strictEqual(directSnap.movementState, 'waiting');
      assert.strictEqual(directSnap.currentWaypointId, 'wp_optica_workshop');
    });
  });

  describe('7. Offline Reconstruction & Deterministic Invariance', () => {
    it('accurately reconstructs NPC position across offline periods without drift', () => {
      const profile = NPC_MOVEMENT_PROFILES['hector-artisan']!;
      // Offline jump of 3 days
      const tDay1 = new Date('2026-11-04T20:10:00Z'); // Wed 13:10 CST
      const tDay4 = new Date(tDay1.getTime() + 3 * 24 * 3600 * 1000); // Sat 13:10 CST

      const snapDay1 = evaluateNpcMovement(profile, tDay1);
      const snapDay4 = evaluateNpcMovement(profile, tDay4);

      assert.strictEqual(snapDay1.movementState, 'waiting');
      assert.strictEqual(snapDay4.movementState, 'waiting');
      assert.strictEqual(snapDay1.currentWaypointId, 'wp_hector_bench');
      assert.strictEqual(snapDay4.currentWaypointId, 'wp_hector_bench');
      assert.strictEqual(snapDay1.position.x, snapDay4.position.x);
      assert.strictEqual(snapDay1.position.z, snapDay4.position.z);
    });
  });

  describe('8. Roster Movement Snapshot Verification (All 7 NPCs)', () => {
    it('evaluates all 7 NPCs coherently with summary counters', () => {
      // 10:00 CST (17:00 UTC) on Wednesday
      const t = new Date('2026-11-04T17:00:00Z');
      const rosterSnap = evaluateRosterMovement(NPC_MOVEMENT_PROFILES, t);

      assert.strictEqual(rosterSnap.isDemonstrationMovement, true);
      assert.strictEqual(rosterSnap.timeZone, 'America/Ciudad_Juarez');
      assert.strictEqual(rosterSnap.summary.totalCount, 7);

      const expectedIds = [
        'elena-border-concierge',
        'carlos-farmacia-attendant',
        'valeria-optica-attendant',
        'mateo-student',
        'sofia-cross-border-retail',
        'hector-artisan',
        'patricia-citizen-rep',
      ];

      for (const id of expectedIds) {
        const npc = rosterSnap.npcs[id];
        assert.ok(npc, `Missing NPC ${id} in roster snapshot`);
        assert.ok(npc.position);
        assert.ok(npc.forwardHeading);
        assert.ok(npc.associatedPlaceId);
        assert.ok(npc.activityState);
        assert.ok(npc.activityTitle.en);
      }

      // Sum of summary counts must equal total
      const { stationaryCount, movingCount, arrivedCount, waitingCount, totalCount } = rosterSnap.summary;
      assert.strictEqual(stationaryCount + movingCount + arrivedCount + waitingCount, totalCount);
    });
  });

  describe('9. Authoritative movementStartTime UTC Contract (Increment 5.17.0 Task A)', () => {
    it('evaluates movementStartTime as true UTC instant under Standard Time (Nov 4, 2026, 14:00 CST -> 21:00 UTC)', () => {
      const profile = NPC_MOVEMENT_PROFILES['carlos-farmacia-attendant']!;
      // Nov 4, 2026, 14:00:10 local Ciudad Juarez time (UTC-7)
      const tStandard = new Date('2026-11-04T21:00:10.000Z');
      const snap = evaluateNpcMovement(profile, tStandard);

      assert.strictEqual(snap.movementState, 'moving');
      assert.strictEqual(snap.movementStartTime, '2026-11-04T21:00:00.000Z');
    });

    it('evaluates movementStartTime as true UTC instant under Daylight Time (Jul 4, 2026, 14:00 CDT -> 20:00 UTC)', () => {
      const profile = NPC_MOVEMENT_PROFILES['carlos-farmacia-attendant']!;
      // Jul 4, 2026 is a Saturday (day 6), Carlos works Mon-Sat, departs at 14:00 local time (UTC-6)
      const tDaylight = new Date('2026-07-04T20:00:10.000Z');
      const snap = evaluateNpcMovement(profile, tDaylight);

      assert.strictEqual(snap.movementState, 'moving');
      assert.strictEqual(snap.movementStartTime, '2026-07-04T20:00:00.000Z');
    });

    it('correctly handles local calendar date differing from UTC calendar date', () => {
      const profile = NPC_MOVEMENT_PROFILES['elena-border-concierge']!;
      // Elena has shift handover transition daily at 13:30 local time.
      // Standard time Nov 4 13:30 local time is Nov 4 20:30 UTC. Elena moves for 9 seconds.
      const t = new Date('2026-11-04T20:30:05.000Z');
      const snap = evaluateNpcMovement(profile, t);

      assert.strictEqual(snap.movementState, 'moving');
      assert.strictEqual(snap.movementStartTime, '2026-11-04T20:30:00.000Z');

      // Test local date vs UTC date boundary: 18:00 local time on Nov 4 = 01:00 UTC on Nov 5
      const result = parseLocalCivilTimeToUtc('2026-11-04T18:00:00', 'America/Ciudad_Juarez');
      assert.strictEqual(result.utcInstant?.toISOString(), '2026-11-05T01:00:00.000Z');
    });

    it('produces identical movementStartTime on repeated evaluations with identical input', () => {
      const profile = NPC_MOVEMENT_PROFILES['carlos-farmacia-attendant']!;
      const t = new Date('2026-11-04T21:00:15.000Z');
      const snap1 = evaluateNpcMovement(profile, t);
      const snap2 = evaluateNpcMovement(profile, t);

      assert.strictEqual(snap1.movementStartTime, snap2.movementStartTime);
      assert.strictEqual(snap1.movementStartTime, '2026-11-04T21:00:00.000Z');
    });
  });
});
