import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  MAX_AWARENESS_VERTICAL_SEPARATION_METERS,
  NPC_AWARENESS_RANGE_METERS,
  NPC_SOCIAL_PROXIMITY_RANGE_METERS,
  PLAYER_CLOSE_ATTENTION_RANGE_METERS,
  PLAYER_GENERAL_AWARENESS_RANGE_METERS,
  evaluateNpcSocialAwareness,
  evaluateRosterSocialAwareness,
} from '../../src/domain/npc-social-awareness.js';
import { evaluateRosterInteractionContext } from '../../src/domain/npc-interaction-context.js';
import {
  NPC_MOVEMENT_PROFILES,
  NpcRosterMovementSnapshot,
  evaluateRosterMovement,
} from '../../src/domain/npc-waypoint-movement.js';
import { PlayerSpatialState } from '../../src/domain/player-movement.js';

// Wednesday 2026-11-04 10:00:00 MST (17:00:00 UTC) — normal daytime open hours; 6 stationary NPCs active at primary waypoints (Sofia & Hector 3.5m apart).
const DAYTIME_NORMAL_INSTANT = '2026-11-04T17:00:00.000Z';
// Wednesday 2026-11-04 14:00:15 MST (21:00:15 UTC) — Carlos walking, Elena away at plaza handover, daytime active.
const CARLOS_WALKING_INSTANT = '2026-11-04T21:00:15.000Z';
// Wednesday 2026-11-04 23:30:00 MST (2026-11-05T06:30:00.000Z) — Nighttime, businesses closed / off duty.
const NIGHT_OFF_DUTY_INSTANT = '2026-11-05T06:30:00.000Z';
// Wednesday 2026-11-04 13:05:00 MST (20:05:00 UTC) — Valeria in optical workshop, Héctor at carving bench.
const CONTEXTUAL_WORKSHOP_INSTANT = '2026-11-04T20:05:00.000Z';

function makePlayer(x: number, y: number, z: number, playerId = 'player-1'): PlayerSpatialState {
  return Object.freeze({
    playerId,
    position: Object.freeze({ x, y, z }),
    lastMovementServerTimeMs: 1000,
    lastSequenceNumber: 1,
    updatedAt: DAYTIME_NORMAL_INSTANT,
  });
}

function buildSyntheticMovementRoster(
  evaluatedAt: string,
  overrides: Record<string, { x: number; y: number; z: number; movementState?: 'stationary' | 'moving' | 'arrived' | 'waiting' }>
): NpcRosterMovementSnapshot {
  const base = evaluateRosterMovement(NPC_MOVEMENT_PROFILES, evaluatedAt);
  const npcs: Record<string, any> = { ...base.npcs };
  for (const [npcId, pos] of Object.entries(overrides)) {
    const existing = npcs[npcId];
    if (!existing) continue;
    npcs[npcId] = Object.freeze({
      ...existing,
      position: Object.freeze({ x: pos.x, y: pos.y, z: pos.z }),
      movementState: pos.movementState ?? existing.movementState,
    });
  }
  return Object.freeze({
    ...base,
    npcs: Object.freeze(npcs),
    npcsList: Object.freeze(Object.values(npcs)),
  });
}

describe('Deterministic NPC Social Awareness Domain Model (Increment 5.20.0)', () => {
  it('1. Player awareness outside general range (> 8.0m) leaves counter NPCs unaware', () => {
    // Carlos at wp_carlos_counter (10.5, 1.0, 0.0). Place player at x = 18.6 -> 8.1m away, zero nearby NPCs.
    const player = makePlayer(18.6, 1.0, 0.0);
    const snapshot = evaluateRosterSocialAwareness({
      evaluatedAt: DAYTIME_NORMAL_INSTANT,
      player,
    });

    const carlos = snapshot.npcs['carlos-farmacia-attendant']!;
    assert.strictEqual(carlos.awarenessState, 'unaware');
    assert.strictEqual(carlos.targetEntityType, 'none');
    assert.strictEqual(carlos.targetEntityId, null);
    assert.strictEqual(carlos.spatialRelation, null);
    assert.ok(carlos.nearbyPlayerDistanceMeters !== null && carlos.nearbyPlayerDistanceMeters > PLAYER_GENERAL_AWARENESS_RANGE_METERS);
  });

  it('2. Player awareness exactly at general threshold (8.000m) yields player_aware and switches to player_attentive at 4.000m', () => {
    // Carlos at (10.5, 1.0, 0.0)
    const exactGeneral = makePlayer(10.5 + 8.0, 1.0, 0.0);
    const generalSnap = evaluateRosterSocialAwareness({
      evaluatedAt: DAYTIME_NORMAL_INSTANT,
      player: exactGeneral,
    });
    const carlosGeneral = generalSnap.npcs['carlos-farmacia-attendant']!;
    assert.strictEqual(carlosGeneral.awarenessState, 'player_aware');
    assert.strictEqual(carlosGeneral.targetEntityType, 'player');
    assert.strictEqual(carlosGeneral.targetEntityId, 'player-1');
    assert.strictEqual(carlosGeneral.spatialRelation?.horizontalDistanceMeters, 8.0);

    // Just outside close threshold (4.001m) stays player_aware
    const justOutsideClose = makePlayer(10.5 + 4.001, 1.0, 0.0);
    const outsideCloseSnap = evaluateRosterSocialAwareness({
      evaluatedAt: DAYTIME_NORMAL_INSTANT,
      player: justOutsideClose,
    });
    assert.strictEqual(outsideCloseSnap.npcs['carlos-farmacia-attendant']!.awarenessState, 'player_aware');

    // Exactly at close threshold (4.000m) becomes player_attentive
    const exactClose = makePlayer(10.5 + 4.0, 1.0, 0.0);
    const closeSnap = evaluateRosterSocialAwareness({
      evaluatedAt: DAYTIME_NORMAL_INSTANT,
      player: exactClose,
    });
    const carlosClose = closeSnap.npcs['carlos-farmacia-attendant']!;
    assert.strictEqual(carlosClose.awarenessState, 'player_attentive');
    assert.strictEqual(carlosClose.spatialRelation?.horizontalDistanceMeters, PLAYER_CLOSE_ATTENTION_RANGE_METERS);
  });

  it('3. NPC detection inside (<= 3.75m) establishes mutual social_engaged pairing vs awareness band (3.75m < d <= 5.0m -> npc_aware, > 5.0m -> unaware)', () => {
    // At primary waypoints (DAYTIME_NORMAL_INSTANT, 10:00 MST): Sofia (15.2, 0.384, 45.5) and Hector (15.2, 0.44, 49.0) are 3.5m apart
    // -> within calibrated NPC_SOCIAL_PROXIMITY_RANGE_METERS (3.75m), establishing a genuine mutual social_engaged queue pair.
    const primarySnap = evaluateRosterSocialAwareness({
      evaluatedAt: DAYTIME_NORMAL_INSTANT,
      player: null,
    });
    const sofiaPrimary = primarySnap.npcs['sofia-cross-border-retail']!;
    const hectorPrimary = primarySnap.npcs['hector-artisan']!;
    assert.strictEqual(sofiaPrimary.awarenessState, 'social_engaged');
    assert.strictEqual(sofiaPrimary.targetEntityType, 'npc');
    assert.strictEqual(sofiaPrimary.targetEntityId, 'hector-artisan');
    assert.strictEqual(sofiaPrimary.pairedPartnerNpcId, 'hector-artisan');
    assert.strictEqual(hectorPrimary.awarenessState, 'social_engaged');
    assert.strictEqual(hectorPrimary.pairedPartnerNpcId, 'sofia-cross-border-retail');
    assert.strictEqual(primarySnap.activeSocialPairs.length, 1);

    // Beyond 5.0m -> unaware (Carlos & Valeria are > 20m from others)
    assert.strictEqual(primarySnap.npcs['carlos-farmacia-attendant']!.awarenessState, 'unaware');

    // When Sofia & Hector stand 4.25m apart (between 3.75m and 5.0m) with Mateo moved away from the bench, they remain mutually npc_aware without locking a social pair.
    const awarenessOnlyMov = buildSyntheticMovementRoster(DAYTIME_NORMAL_INSTANT, {
      'sofia-cross-border-retail': { x: 15.2, y: 0.38, z: 44.75 }, // 4.25m from Hector (49.0)
      'mateo-student': { x: 15.2, y: 0.38, z: 30.0 },
    });
    const awarenessOnlySnap = evaluateRosterSocialAwareness({
      evaluatedAt: DAYTIME_NORMAL_INSTANT,
      movementRoster: awarenessOnlyMov,
      player: null,
    });
    assert.strictEqual(awarenessOnlySnap.activeSocialPairs.length, 0);
    assert.strictEqual(awarenessOnlySnap.npcs['sofia-cross-border-retail']!.awarenessState, 'npc_aware');
    assert.strictEqual(awarenessOnlySnap.npcs['sofia-cross-border-retail']!.targetEntityId, 'hector-artisan');
    assert.strictEqual(awarenessOnlySnap.npcs['hector-artisan']!.awarenessState, 'npc_aware');
    assert.strictEqual(awarenessOnlySnap.npcs['hector-artisan']!.targetEntityId, 'sofia-cross-border-retail');
  });

  it('4. Vertical separation guard excludes targets more than 3.0m higher/lower', () => {
    const carlosPos = evaluateRosterMovement(NPC_MOVEMENT_PROFILES, DAYTIME_NORMAL_INSTANT).npcs['carlos-farmacia-attendant']!.position;
    const elevatedPlayer = makePlayer(carlosPos.x, carlosPos.y + MAX_AWARENESS_VERTICAL_SEPARATION_METERS + 0.5, carlosPos.z);
    const excludedSnap = evaluateRosterSocialAwareness({
      evaluatedAt: DAYTIME_NORMAL_INSTANT,
      player: elevatedPlayer,
    });
    assert.strictEqual(excludedSnap.npcs['carlos-farmacia-attendant']!.awarenessState, 'unaware');

    const exactVerticalPlayer = makePlayer(carlosPos.x, carlosPos.y + MAX_AWARENESS_VERTICAL_SEPARATION_METERS, carlosPos.z);
    const includedSnap = evaluateRosterSocialAwareness({
      evaluatedAt: DAYTIME_NORMAL_INSTANT,
      player: exactVerticalPlayer,
    });
    assert.strictEqual(includedSnap.npcs['carlos-farmacia-attendant']!.awarenessState, 'player_attentive');
  });

  it('5. Nearest-target selection, deterministic tie-breaking, and 1-to-1 non-duplicate social pairing across clusters', () => {
    // Cluster 3 available_normal NPCs closely together at 10:00 MST (keep Mateo away from Elena so only Elena/Sofia/Hector cluster):
    // Elena (z=42.0), Sofia (z=44.0, dist=2.0), Hector (z=44.5, dist to Sofia=0.5, to Elena=2.5).
    // Sorted pairs: (hector, sofia)=0.5m first, leaving Elena paired with NO duplicate partner!
    const clusterMov = buildSyntheticMovementRoster(DAYTIME_NORMAL_INSTANT, {
      'elena-border-concierge': { x: 15.2, y: 0.35, z: 42.0 },
      'sofia-cross-border-retail': { x: 15.2, y: 0.35, z: 44.0 },
      'hector-artisan': { x: 15.2, y: 0.35, z: 44.5 },
      'mateo-student': { x: 15.2, y: 0.35, z: 30.0 },
    });
    const clusterSnap = evaluateRosterSocialAwareness({
      evaluatedAt: DAYTIME_NORMAL_INSTANT,
      movementRoster: clusterMov,
      player: null,
    });

    assert.strictEqual(clusterSnap.activeSocialPairs.length, 1);
    const pair = clusterSnap.activeSocialPairs[0]!;
    assert.deepStrictEqual(
      [pair.npcIdA, pair.npcIdB].sort(),
      ['hector-artisan', 'sofia-cross-border-retail']
    );
    assert.strictEqual(clusterSnap.npcs['sofia-cross-border-retail']!.pairedPartnerNpcId, 'hector-artisan');
    assert.strictEqual(clusterSnap.npcs['hector-artisan']!.pairedPartnerNpcId, 'sofia-cross-border-retail');
    // Elena notices nearby Sofia (2.0m <= NPC_AWARENESS_RANGE_METERS), NOT duplicated into a second social pair.
    assert.strictEqual(clusterSnap.npcs['elena-border-concierge']!.awarenessState, 'npc_aware');
    assert.strictEqual(clusterSnap.npcs['elena-border-concierge']!.pairedPartnerNpcId, null);
    assert.strictEqual(clusterSnap.npcs['elena-border-concierge']!.targetEntityId, 'sofia-cross-border-retail');

    // Identical tie-distance test: repeat evaluation produces exact same selection
    const snapA = evaluateRosterSocialAwareness({ evaluatedAt: DAYTIME_NORMAL_INSTANT, movementRoster: clusterMov, player: null });
    const snapB = evaluateRosterSocialAwareness({ evaluatedAt: DAYTIME_NORMAL_INSTANT, movementRoster: clusterMov, player: null });
    assert.deepStrictEqual(snapA, snapB);
  });

  it('6. In-transit, busy, off-duty, unavailable, and focused contextual activities respect social eligibility rules', () => {
    // Moving NPC (Carlos at 14:00:15) is ineligible for social pairing even if near another NPC, yet can notice nearby player.
    const movingInteractionRoster = evaluateRosterInteractionContext(CARLOS_WALKING_INSTANT);
    const movingBaseMov = evaluateRosterMovement(NPC_MOVEMENT_PROFILES, CARLOS_WALKING_INSTANT);
    const carlosPos = movingBaseMov.npcs['carlos-farmacia-attendant']!.position;
    const movingMovNearNpc = {
      ...movingBaseMov,
      npcs: Object.freeze({
        ...movingBaseMov.npcs,
        'mateo-student': Object.freeze({
          ...movingBaseMov.npcs['mateo-student']!,
          position: Object.freeze({ x: carlosPos.x + 1.5, y: carlosPos.y, z: carlosPos.z }),
        }),
      }),
    };
    const movingSnap = evaluateRosterSocialAwareness({
      evaluatedAt: CARLOS_WALKING_INSTANT,
      interactionRoster: movingInteractionRoster,
      movementRoster: movingMovNearNpc,
      player: makePlayer(carlosPos.x, carlosPos.y, carlosPos.z + 3.0),
    });
    const carlosMoving = movingSnap.npcs['carlos-farmacia-attendant']!;
    assert.strictEqual(carlosMoving.interactionAvailability, 'available_in_transit');
    assert.strictEqual(carlosMoving.socialEligible, false);
    assert.strictEqual(carlosMoving.retainsContextualActivity, true);
    assert.strictEqual(carlosMoving.pairedPartnerNpcId, null);

    // Off-duty evening: all NPCs off_duty/unavailable remain strictly unaware and cannot orient
    const nightSnap = evaluateRosterSocialAwareness({
      evaluatedAt: NIGHT_OFF_DUTY_INSTANT,
      player: makePlayer(10.5, 1.0, 1.0), // right next to Carlos counter at night
    });
    assert.strictEqual(nightSnap.npcs['carlos-farmacia-attendant']!.awarenessState, 'unaware');
    assert.strictEqual(nightSnap.npcs['carlos-farmacia-attendant']!.canOrientTowardTarget, false);
    assert.strictEqual(nightSnap.npcs['carlos-farmacia-attendant']!.socialEligible, false);

    // Contextual station work (13:05:00 CST): Valeria at optical workshop, Don Héctor at wood carving workbench retain contextual focus
    const contextualSnap = evaluateRosterSocialAwareness({
      evaluatedAt: CONTEXTUAL_WORKSHOP_INSTANT,
      player: null,
    });
    assert.strictEqual(contextualSnap.npcs['valeria-optica-attendant']!.socialEligible, false);
    assert.strictEqual(contextualSnap.npcs['valeria-optica-attendant']!.retainsContextualActivity, true);
    assert.strictEqual(contextualSnap.npcs['hector-artisan']!.socialEligible, false);
    assert.strictEqual(contextualSnap.npcs['hector-artisan']!.retainsContextualActivity, true);
  });

  it('7. Missing or malformed spatial information falls back safely without mutating world state', () => {
    const interactionBefore = evaluateRosterInteractionContext(DAYTIME_NORMAL_INSTANT);
    const movementBefore = evaluateRosterMovement(NPC_MOVEMENT_PROFILES, DAYTIME_NORMAL_INSTANT);
    const movementSnapshotCopy = structuredClone(movementBefore);

    // Malformed player vector (NaN) -> ignored safely, no exception, no fabricated player awareness
    const malformedPlayer: PlayerSpatialState = {
      playerId: 'player-1',
      position: { x: Number.NaN, y: 1.0, z: 0.0 },
      lastMovementServerTimeMs: 100,
      lastSequenceNumber: 1,
      updatedAt: DAYTIME_NORMAL_INSTANT,
    };

    const snap = evaluateRosterSocialAwareness({
      evaluatedAt: DAYTIME_NORMAL_INSTANT,
      player: malformedPlayer,
      interactionRoster: interactionBefore,
      movementRoster: movementBefore,
    });

    assert.strictEqual(snap.playerPosition, null);
    assert.strictEqual(snap.npcs['carlos-farmacia-attendant']!.awarenessState, 'unaware');
    assert.strictEqual(snap.npcs['carlos-farmacia-attendant']!.nearbyPlayerDistanceMeters, null);
    assert.strictEqual(snap.supportsLineOfSight, false);
    assert.strictEqual(snap.environmentalAwarenessScope, 'player_and_npc_proximity_only');
    assert.ok(snap.thresholds.npcAwarenessMeters === NPC_AWARENESS_RANGE_METERS);
    assert.ok(snap.thresholds.npcSocialMeters === NPC_SOCIAL_PROXIMITY_RANGE_METERS);

    // Verify no canonical world state mutation
    assert.deepStrictEqual(movementBefore, movementSnapshotCopy);

    // Different positions produce expected change
    const carlosAware = evaluateNpcSocialAwareness('carlos-farmacia-attendant', {
      evaluatedAt: DAYTIME_NORMAL_INSTANT,
      player: makePlayer(10.5, 1.0, 2.0), // 2.0m from Carlos
    });
    assert.strictEqual(carlosAware?.awarenessState, 'player_attentive');
    assert.strictEqual(evaluateNpcSocialAwareness('unknown-npc', { evaluatedAt: DAYTIME_NORMAL_INSTANT }), null);
  });
});
