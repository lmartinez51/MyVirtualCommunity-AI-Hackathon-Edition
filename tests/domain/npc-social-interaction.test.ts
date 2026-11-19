import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  CLOSING_DURATION_SECONDS,
  COMPLETED_GRACE_DURATION_SECONDS,
  CONVERSATION_TURN_DURATION_SECONDS,
  INITIATING_DURATION_SECONDS,
  SOCIAL_INTERACTION_CYCLE_SECONDS,
  evaluateNpcSocialInteraction,
  evaluatePairInteractionWindow,
  evaluateRosterSocialInteractions,
} from '../../src/domain/npc-social-interaction.js';
import {
  evaluateRosterInteractionContext,
  NpcRosterInteractionContext,
} from '../../src/domain/npc-interaction-context.js';
import {
  NPC_MOVEMENT_PROFILES,
  NpcRosterMovementSnapshot,
  evaluateRosterMovement,
} from '../../src/domain/npc-waypoint-movement.js';
import { evaluateRosterSocialAwareness } from '../../src/domain/npc-social-awareness.js';
import { COMMUNITY_PLAYER_PROXY_IDS } from '../../src/domain/npc-actor-archetypes.js';
import { PlayerSpatialState } from '../../src/domain/player-movement.js';

// Wednesday 2026-11-04 10:00:00 MST (17:00:00 UTC): Sofia (z=45.5) and Hector (z=49.0) are 3.5m apart in queue.
const DAYTIME_NORMAL_INSTANT = '2026-11-04T17:00:00.000Z';
// Wednesday 2026-11-04 13:30:03 MST: Elena walking to bridge plaza (in_transit).
const ELENA_WALKING_INSTANT = '2026-11-04T20:30:03.000Z';
// Wednesday 2026-11-04 13:35:00 MST: Elena stationary at wp_bridge_plaza (17.8, 0.2, 42.0), 2.6m from Mateo (15.2, 0.328, 42.0).
const ELENA_PLAZA_INSTANT = '2026-11-04T20:35:00.000Z';
// Sunday 2026-11-08 02:30:00 MST: Late night Sunday — attendants & public servants off_duty, community proxies still active.
const SUNDAY_NIGHT_INSTANT = '2026-11-08T09:30:00.000Z';

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

function buildSyntheticInteractionRoster(
  evaluatedAt: string,
  overrides: Record<string, Partial<NpcRosterInteractionContext['npcs'][string]>>
): NpcRosterInteractionContext {
  const base = evaluateRosterInteractionContext(evaluatedAt);
  const npcs: Record<string, any> = { ...base.npcs };
  for (const [npcId, patch] of Object.entries(overrides)) {
    const existing = npcs[npcId];
    if (!existing) continue;
    npcs[npcId] = Object.freeze({
      ...existing,
      ...patch,
    });
  }
  return Object.freeze({
    ...base,
    npcs: Object.freeze(npcs),
    npcsList: Object.freeze(Object.values(npcs)),
  });
}

describe('Autonomous NPC Social Interaction Lifecycle Domain (Increment 5.21.0)', () => {
  it('11A. Transitions deterministically through initiating -> engaged -> closing -> completed -> cooldown with replay & reconstruction invariants', () => {
    const baseWindow = evaluatePairInteractionWindow(
      'hector-artisan',
      'sofia-cross-border-retail',
      Date.parse(DAYTIME_NORMAL_INSTANT)
    );
    const startMs = baseWindow.startedAtMs;
    const expectedEndMs = baseWindow.expectedEndAtMs;

    // Step 1: Initiating phase (startMs + 1s)
    const initIso = new Date(startMs + 1000).toISOString();
    const initSnap = evaluateRosterSocialInteractions({ evaluatedAt: initIso });
    assert.strictEqual(initSnap.sessions.length, 1);
    const initSession = initSnap.sessions[0]!;
    assert.strictEqual(initSession.phase, 'initiating');
    assert.strictEqual(initSession.isActive, true);
    assert.deepStrictEqual(initSession.participantNpcIds, ['hector-artisan', 'sofia-cross-border-retail']);
    assert.strictEqual(initSession.sessionId, baseWindow.sessionId);
    assert.strictEqual(initSession.startedAt, baseWindow.startedAt);
    assert.strictEqual(initSession.expectedEndAt, baseWindow.expectedEndAt);
    assert.strictEqual(initSession.interactionType, 'community_queue_exchange');

    // Step 2: Engaged phase + alternating speaker/listener conversational turn-taking
    const turn0Iso = new Date(startMs + INITIATING_DURATION_SECONDS * 1000 + 1000).toISOString();
    const turn1Iso = new Date(
      startMs + INITIATING_DURATION_SECONDS * 1000 + CONVERSATION_TURN_DURATION_SECONDS * 1000 + 1000
    ).toISOString();
    const turn0Snap = evaluateRosterSocialInteractions({ evaluatedAt: turn0Iso });
    const turn1Snap = evaluateRosterSocialInteractions({ evaluatedAt: turn1Iso });
    assert.strictEqual(turn0Snap.sessions[0]!.phase, 'engaged');
    assert.strictEqual(turn1Snap.sessions[0]!.phase, 'engaged');
    assert.strictEqual(turn0Snap.sessions[0]!.turnIndex, 0);
    assert.strictEqual(turn1Snap.sessions[0]!.turnIndex, 1);
    assert.notStrictEqual(
      turn0Snap.sessions[0]!.activeSpeakerNpcId,
      turn1Snap.sessions[0]!.activeSpeakerNpcId,
      'Speaker role must alternate between participants across conversational turns'
    );
    assert.strictEqual(
      turn0Snap.npcs[turn0Snap.sessions[0]!.activeSpeakerNpcId!]!.conversationalRole,
      'speaker'
    );
    assert.strictEqual(
      turn0Snap.npcs[turn0Snap.sessions[0]!.activeListenerNpcId!]!.conversationalRole,
      'listener'
    );

    // Step 3: Closing phase
    const closingIso = new Date(expectedEndMs - Math.floor((CLOSING_DURATION_SECONDS * 1000) / 2)).toISOString();
    const closingSnap = evaluateRosterSocialInteractions({ evaluatedAt: closingIso });
    assert.strictEqual(closingSnap.sessions[0]!.phase, 'closing');
    assert.strictEqual(closingSnap.sessions[0]!.isActive, true);
    assert.strictEqual(closingSnap.summary.closingCount, 1);

    // Step 4: Completed grace phase -> no active sessions, participants enter cooldown
    const completedIso = new Date(expectedEndMs + 1000).toISOString();
    const completedSnap = evaluateRosterSocialInteractions({ evaluatedAt: completedIso });
    assert.strictEqual(completedSnap.sessions.length, 0);
    assert.strictEqual(completedSnap.npcs['hector-artisan']!.phase, 'completed');
    assert.strictEqual(completedSnap.npcs['hector-artisan']!.inActiveInteraction, false);
    assert.strictEqual(completedSnap.npcs['hector-artisan']!.inCooldown, true);
    assert.strictEqual(completedSnap.npcs['sofia-cross-border-retail']!.phase, 'completed');
    assert.strictEqual(completedSnap.npcs['sofia-cross-border-retail']!.inCooldown, true);

    // Step 5: Mid-cooldown window -> neither active nor completed, inCooldown stays true until cycle boundary
    const midCooldownIso = new Date(
      expectedEndMs + COMPLETED_GRACE_DURATION_SECONDS * 1000 + 2000
    ).toISOString();
    const midCooldownSnap = evaluateRosterSocialInteractions({ evaluatedAt: midCooldownIso });
    assert.strictEqual(midCooldownSnap.sessions.length, 0);
    assert.strictEqual(midCooldownSnap.npcs['hector-artisan']!.phase, null);
    assert.strictEqual(midCooldownSnap.npcs['hector-artisan']!.inCooldown, true);

    // Step 6: Next cycle boundary -> deterministically starts a fresh session ID
    const nextStartIso = new Date(startMs + SOCIAL_INTERACTION_CYCLE_SECONDS * 1000 + 1000).toISOString();
    const nextCycleSnap = evaluateRosterSocialInteractions({ evaluatedAt: nextStartIso });
    assert.strictEqual(nextCycleSnap.sessions.length, 1);
    assert.strictEqual(nextCycleSnap.sessions[0]!.phase, 'initiating');
    assert.notStrictEqual(nextCycleSnap.sessions[0]!.sessionId, initSession.sessionId);

    // Step 7: Deterministic replay / reconstruction invariance
    const replayA = evaluateRosterSocialInteractions({ evaluatedAt: turn0Iso });
    const replayB = evaluateRosterSocialInteractions({ evaluatedAt: turn0Iso });
    assert.deepStrictEqual(replayA, replayB);
  });

  it('11A-2. Excludes spatially incompatible pairs and prevents simultaneous exclusive sessions across a 3-NPC cluster', () => {
    const win = evaluatePairInteractionWindow(
      'elena-border-concierge',
      'sofia-cross-border-retail',
      Date.parse(DAYTIME_NORMAL_INSTANT)
    );
    const activeIso = new Date(win.startedAtMs + 6000).toISOString();

    // Cluster Elena, Sofia, and Hector within 2.5m; only 1 mutually exclusive pair can form at once (max 2 active NPCs in that cluster).
    const clusterMov = buildSyntheticMovementRoster(activeIso, {
      'elena-border-concierge': { x: 15.2, y: 0.35, z: 42.0 },
      'sofia-cross-border-retail': { x: 15.2, y: 0.35, z: 44.0 },
      'hector-artisan': { x: 15.2, y: 0.35, z: 44.5 },
      'mateo-student': { x: 15.2, y: 0.35, z: 30.0 },
    });

    const clusterSnap = evaluateRosterSocialInteractions({
      evaluatedAt: activeIso,
      movementRoster: clusterMov,
    });

    const seenNpcs = new Set<string>();
    for (const s of clusterSnap.sessions) {
      assert.ok(!seenNpcs.has(s.participantNpcIds[0]), 'NPC cannot hold simultaneous social interactions');
      assert.ok(!seenNpcs.has(s.participantNpcIds[1]), 'NPC cannot hold simultaneous social interactions');
      seenNpcs.add(s.participantNpcIds[0]);
      seenNpcs.add(s.participantNpcIds[1]);
    }
    assert.strictEqual(clusterSnap.sessions.length, 1);
    assert.strictEqual(clusterSnap.npcs['elena-border-concierge']!.inActiveInteraction, false);

    // Spatially distant or vertically separated NPCs never form a session
    const distantMov = buildSyntheticMovementRoster(activeIso, {
      'hector-artisan': { x: 15.2, y: 0.35, z: 80.0 },
      'mateo-student': { x: 15.2, y: 0.35, z: 10.0 },
      'elena-border-concierge': { x: 19.2, y: 6.0, z: 44.0 }, // vertical separation > 3.0m
    });
    const distantSnap = evaluateRosterSocialInteractions({
      evaluatedAt: activeIso,
      movementRoster: distantMov,
    });
    assert.strictEqual(distantSnap.sessions.length, 0);
  });

  it('11B. Respects Business Attendant, Public Servant, and Community Player Proxy archetype rules & Witness boundary', () => {
    const pairWin = evaluatePairInteractionWindow(
      'carlos-farmacia-attendant',
      'elena-border-concierge',
      Date.parse(DAYTIME_NORMAL_INSTANT)
    );
    const activeIso = new Date(pairWin.startedAtMs + 6000).toISOString();

    // Place Carlos (business_attendant) and Elena (public_servant) 2.0m apart
    const nearMov = buildSyntheticMovementRoster(activeIso, {
      'carlos-farmacia-attendant': { x: 10.5, y: 0.2, z: 0.0 },
      'elena-border-concierge': { x: 12.5, y: 0.2, z: 0.0 },
    });

    // When both are available_normal, mixed encounter may start
    const normalCtx = buildSyntheticInteractionRoster(activeIso, {
      'carlos-farmacia-attendant': { interactionAvailability: 'available_normal' },
      'elena-border-concierge': { interactionAvailability: 'available_normal' },
    });
    const allowedSnap = evaluateRosterSocialInteractions({
      evaluatedAt: activeIso,
      movementRoster: nearMov,
      interactionRoster: normalCtx,
    });
    const carlosElenaAllowed = allowedSnap.sessions.find(
      (s) =>
        s.participantNpcIds.includes('carlos-farmacia-attendant') &&
        s.participantNpcIds.includes('elena-border-concierge')
    );
    assert.ok(carlosElenaAllowed, 'Eligible business_attendant and public_servant may socialize when available_normal');
    assert.strictEqual(carlosElenaAllowed!.interactionType, 'civic_corridor_greeting');

    // Busy Business Attendant cannot abandon active customer service task
    const carlosBusyCtx = buildSyntheticInteractionRoster(activeIso, {
      'carlos-farmacia-attendant': { interactionAvailability: 'busy' },
      'elena-border-concierge': { interactionAvailability: 'available_normal' },
    });
    const carlosBusySnap = evaluateRosterSocialInteractions({
      evaluatedAt: activeIso,
      movementRoster: nearMov,
      interactionRoster: carlosBusyCtx,
    });
    assert.strictEqual(carlosBusySnap.npcs['carlos-farmacia-attendant']!.inActiveInteraction, false);
    assert.strictEqual(carlosBusySnap.npcs['carlos-farmacia-attendant']!.eligibleForSocialInteraction, false);

    // Busy Public Servant preserves institutional priority
    const elenaBusyCtx = buildSyntheticInteractionRoster(activeIso, {
      'carlos-farmacia-attendant': { interactionAvailability: 'available_normal' },
      'elena-border-concierge': { interactionAvailability: 'busy' },
    });
    const elenaBusySnap = evaluateRosterSocialInteractions({
      evaluatedAt: activeIso,
      movementRoster: nearMov,
      interactionRoster: elenaBusyCtx,
    });
    assert.strictEqual(elenaBusySnap.npcs['elena-border-concierge']!.inActiveInteraction, false);
    assert.strictEqual(elenaBusySnap.npcs['elena-border-concierge']!.eligibleForSocialInteraction, false);

    // Sunday 02:30 MST: ALL 4 attendants/public servants remain off_duty while ALL 3 proxies never enter off_duty and Sofia/Hector socialize
    const nightPair = evaluatePairInteractionWindow(
      'hector-artisan',
      'sofia-cross-border-retail',
      Date.parse(SUNDAY_NIGHT_INSTANT)
    );
    const nightEngagedIso = new Date(nightPair.startedAtMs + 8000).toISOString();
    const nightSnap = evaluateRosterSocialInteractions({ evaluatedAt: nightEngagedIso });

    assert.strictEqual(nightSnap.npcs['carlos-farmacia-attendant']!.interactionAvailability, 'off_duty');
    assert.strictEqual(nightSnap.npcs['carlos-farmacia-attendant']!.inActiveInteraction, false);
    assert.strictEqual(nightSnap.npcs['valeria-optica-attendant']!.interactionAvailability, 'off_duty');
    assert.strictEqual(nightSnap.npcs['elena-border-concierge']!.interactionAvailability, 'off_duty');
    assert.strictEqual(nightSnap.npcs['patricia-citizen-rep']!.interactionAvailability, 'off_duty');

    for (const proxyId of COMMUNITY_PLAYER_PROXY_IDS) {
      const p = nightSnap.npcs[proxyId]!;
      assert.strictEqual(p.actorArchetype, 'community_player_proxy');
      assert.strictEqual(p.canEnterOffDuty, false);
      assert.strictEqual(p.isWitness, true);
      assert.notStrictEqual(p.interactionAvailability, 'off_duty');
      assert.notStrictEqual(p.interactionAvailability, 'unavailable');
      assert.doesNotMatch(p.reason.en.toLowerCase(), /\b(accident|witnessed|testimony|evidence|crash|pothole)\b/);
    }
    assert.strictEqual(nightSnap.npcs['sofia-cross-border-retail']!.inActiveInteraction, true);
    assert.strictEqual(nightSnap.npcs['hector-artisan']!.inActiveInteraction, true);
  });

  it('11C. Works independently of player presence/distance and yields only to active authorized player interaction', () => {
    const win = evaluatePairInteractionWindow(
      'hector-artisan',
      'sofia-cross-border-retail',
      Date.parse(DAYTIME_NORMAL_INSTANT)
    );
    const engagedIso = new Date(win.startedAtMs + 10_000).toISOString();

    // 1. No human player present
    const noPlayerSocial = evaluateRosterSocialInteractions({ evaluatedAt: engagedIso });
    const noPlayerAwareness = evaluateRosterSocialAwareness({ evaluatedAt: engagedIso, player: null });
    assert.strictEqual(noPlayerSocial.sessions.length, 1);
    assert.strictEqual(noPlayerSocial.sessions[0]!.phase, 'engaged');
    assert.strictEqual(noPlayerAwareness.activeSocialPairs.length, 1);

    // 2. Human player far outside interaction area (> 45m away)
    const farPlayer: PlayerSpatialState = {
      playerId: 'player-1',
      position: { x: 10.5, y: 1.0, z: 0.0 },
      lastMovementServerTimeMs: 1000,
      lastSequenceNumber: 1,
      updatedAt: engagedIso,
    };
    const farAwareness = evaluateRosterSocialAwareness({ evaluatedAt: engagedIso, player: farPlayer });
    const farSocial = evaluateRosterSocialInteractions({ evaluatedAt: engagedIso });
    assert.deepStrictEqual(farSocial.sessions, noPlayerSocial.sessions);
    assert.strictEqual(farAwareness.activeSocialPairs.length, 1);

    // 3. Human player nearby (4.5m away) but NOT interacting
    const nearbyPlayer: PlayerSpatialState = {
      playerId: 'player-1',
      position: { x: 19.7, y: 0.38, z: 47.25 },
      lastMovementServerTimeMs: 2000,
      lastSequenceNumber: 2,
      updatedAt: engagedIso,
    };
    const nearbyAwareness = evaluateRosterSocialAwareness({ evaluatedAt: engagedIso, player: nearbyPlayer });
    const nearbySocial = evaluateRosterSocialInteractions({ evaluatedAt: engagedIso });
    assert.deepStrictEqual(nearbySocial.sessions, noPlayerSocial.sessions);
    assert.strictEqual(nearbyAwareness.activeSocialPairs.length, 1);

    // 4. Active authorized player interaction with Sofia supersedes her optional NPC-to-NPC session
    const playerPrioritySnap = evaluateRosterSocialInteractions({
      evaluatedAt: engagedIso,
      activePlayerInteractionNpcId: 'sofia-cross-border-retail',
    });
    assert.strictEqual(playerPrioritySnap.npcs['sofia-cross-border-retail']!.supersededByPlayerPriority, true);
    assert.strictEqual(playerPrioritySnap.npcs['sofia-cross-border-retail']!.inActiveInteraction, false);
    assert.strictEqual(playerPrioritySnap.sessions.length, 0);
  });

  it('11D. Preserves deterministic movement routes and fixed service coordinates during social interactions', () => {
    const movBefore = evaluateRosterMovement(NPC_MOVEMENT_PROFILES, ELENA_PLAZA_INSTANT);
    const win = evaluatePairInteractionWindow(
      'elena-border-concierge',
      'mateo-student',
      Date.parse(ELENA_PLAZA_INSTANT)
    );
    const duringPlazaIso = new Date(
      Math.max(Date.parse(ELENA_PLAZA_INSTANT), win.startedAtMs + 6000)
    ).toISOString();

    // Evaluate movement alongside social interaction and confirm zero position mutation
    const movSnapshotBefore = structuredClone(movBefore);
    const socialSnap = evaluateRosterSocialInteractions({ evaluatedAt: duringPlazaIso });
    const movSnapshotAfter = evaluateRosterMovement(NPC_MOVEMENT_PROFILES, ELENA_PLAZA_INSTANT);
    assert.deepStrictEqual(movSnapshotAfter, movSnapshotBefore);

    // In-transit Elena (13:30:03) remains ineligible for social interaction and completes movement cleanly
    const inTransitMov = evaluateRosterMovement(NPC_MOVEMENT_PROFILES, ELENA_WALKING_INSTANT);
    assert.strictEqual(inTransitMov.npcs['elena-border-concierge']!.movementState, 'moving');
    const inTransitSocial = evaluateNpcSocialInteraction('elena-border-concierge', {
      evaluatedAt: ELENA_WALKING_INSTANT,
    });
    assert.strictEqual(inTransitSocial?.inActiveInteraction, false);
    assert.strictEqual(inTransitSocial?.eligibleForSocialInteraction, false);

    // Fixed anchor coordinates remain intact
    const primaryMov = evaluateRosterMovement(NPC_MOVEMENT_PROFILES, DAYTIME_NORMAL_INSTANT);
    assert.deepStrictEqual(primaryMov.npcs['elena-border-concierge']!.position, { x: 19.2, y: 0.2, z: 39.5 });
    assert.deepStrictEqual(primaryMov.npcs['patricia-citizen-rep']!.position, { x: -7.6, y: 0.0, z: 18.8 });
    assert.ok(socialSnap.evaluatedAt === duringPlazaIso);
  });
});
