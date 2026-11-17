import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  BRIDGE_PATROL_DOMAIN,
  BRIDGE_PATROL_PROFILES,
  BUSINESS_AUTONOMY_DOMAINS,
  BUSINESS_LOCAL_AUTONOMY_PROFILES,
  BusinessMovementDomain,
  FARMACIA_BENAVIDES_AUTONOMY_DOMAIN,
  FIXED_ANCHOR_SCHEDULED_PROFILES,
  MIN_PERSONAL_SPACE_METERS,
  NPC_AUTONOMOUS_BEHAVIOR_PROFILES,
  OPTICA_FASHION_AUTONOMY_DOMAIN,
  PASSING_ENCOUNTER_COOLDOWN_SECONDS,
  PASSING_ENCOUNTER_MAX_DURATION_SECONDS,
  PASSING_ENCOUNTER_MIN_DURATION_SECONDS,
  applyRosterPersonalSpaceSeparation,
  evaluateBusinessLocalAutonomyMovement,
  evaluatePassingEncounterPairStatus,
  isConvexPolygonXZ,
  isPointInBridgePatrolDomain,
  isPointInBusinessDomain,
  isPointInSafeBusinessRoamingRegion,
  isSegmentInBusinessDomain,
  isSegmentInPolygonXZ,
  isSegmentInSafeBusinessRoamingRegion,
  resolveAuthoritativeCanonicalName,
  validateBusinessCandidateRoute,
  validateBusinessLocalGraphRoute,
} from '../../src/domain/npc-autonomous-locomotion.js';
import {
  CORRIDOR_WAYPOINTS,
  NPC_MOVEMENT_PROFILES,
  evaluateNpcMovement,
  evaluateRosterMovement,
} from '../../src/domain/npc-waypoint-movement.js';
import {
  BUSINESS_ATTENDANT_IDS,
  COMMUNITY_PLAYER_PROXY_IDS,
  NPC_ACTOR_CLASSIFICATION_ROSTER,
  WITNESS_NPC_IDS,
} from '../../src/domain/npc-actor-archetypes.js';
import { NPC_SCHEDULE_ROSTER } from '../../src/domain/npc-activity.js';
import { evaluateRosterSocialInteractions } from '../../src/domain/npc-social-interaction.js';
import { InMemoryWorldCoreAdapter } from '../../src/adapters/in-memory-world-core-adapter.js';
import { validateNpcInteraction } from '../../src/application/validate-npc-interaction.js';

describe('Autonomous NPC Locomotion, Bridge Patrols & Bounded Business Activity (Increment 5.22.0)', () => {
  it('12A. Community Player Proxy identity, Witness designation, never-off-duty & profile separation', () => {
    const bridgePatrolIds = Object.keys(BRIDGE_PATROL_PROFILES).sort();
    assert.deepStrictEqual(bridgePatrolIds, [
      'hector-artisan',
      'mateo-student',
      'sofia-cross-border-retail',
    ]);
    assert.deepStrictEqual([...COMMUNITY_PLAYER_PROXY_IDS].sort(), bridgePatrolIds);
    assert.deepStrictEqual([...WITNESS_NPC_IDS].sort(), bridgePatrolIds);

    for (const npcId of bridgePatrolIds) {
      const classification = NPC_ACTOR_CLASSIFICATION_ROSTER[npcId]!;
      const profile = BRIDGE_PATROL_PROFILES[npcId]!;
      assert.strictEqual(classification.actorArchetype, 'community_player_proxy');
      assert.strictEqual(classification.isWitness, true);
      assert.strictEqual(classification.witnessClassification, 'community_queue_witness');
      assert.strictEqual(classification.witnessId, npcId);
      assert.strictEqual(classification.canEnterOffDuty, false);
      assert.strictEqual(classification.hasEmploymentSchedule, false);
      assert.strictEqual(profile.behaviorProfileType, 'bridge_patrol');
      assert.strictEqual(profile.canonicalName, classification.canonicalName);
    }

    // No unrelated actor is assigned bridge_patrol
    for (const [npcId, prof] of Object.entries(NPC_AUTONOMOUS_BEHAVIOR_PROFILES)) {
      if (!bridgePatrolIds.includes(npcId)) {
        assert.notStrictEqual(
          prof.behaviorProfileType,
          'bridge_patrol',
          `Unrelated NPC ${npcId} must not receive bridge_patrol profile`
        );
      }
    }

    // Business local autonomy is assigned strictly to Carlos and Valeria
    const bizIds = Object.keys(BUSINESS_LOCAL_AUTONOMY_PROFILES).sort();
    assert.deepStrictEqual(bizIds, ['carlos-farmacia-attendant', 'valeria-optica-attendant']);
    assert.deepStrictEqual([...BUSINESS_ATTENDANT_IDS].sort(), bizIds);

    // Elena and Patricia are explicitly fixed_anchor_scheduled
    assert.deepStrictEqual(Object.keys(FIXED_ANCHOR_SCHEDULED_PROFILES).sort(), [
      'elena-border-concierge',
      'patricia-citizen-rep',
    ]);

    // Authoritative canonical identity mapping across all 7 NPCs and all domain registries
    const expectedCanonicalIdentities: Readonly<Record<string, string>> = {
      'elena-border-concierge': 'Elena Ramos',
      'carlos-farmacia-attendant': 'Carlos Méndez',
      'valeria-optica-attendant': 'Valeria Domínguez',
      'mateo-student': 'Mateo Navarro',
      'sofia-cross-border-retail': 'Sofía Alarcón',
      'hector-artisan': 'Don Héctor Méndez',
      'patricia-citizen-rep': 'Lic. Patricia Morales',
    };
    const forbiddenConflictingNames = new Set([
      'Elena Morales',
      'Carlos Mendoza',
      'Valeria Solís',
      'Héctor Ríos',
      'Patricia Vega',
    ]);

    const rosterMovementSnap = evaluateRosterMovement(
      NPC_MOVEMENT_PROFILES,
      '2026-11-04T17:15:00.000Z'
    );
    assert.deepStrictEqual(
      Object.keys(NPC_AUTONOMOUS_BEHAVIOR_PROFILES).sort(),
      Object.keys(expectedCanonicalIdentities).sort()
    );

    for (const [npcId, expectedName] of Object.entries(expectedCanonicalIdentities)) {
      assert.strictEqual(resolveAuthoritativeCanonicalName(npcId), expectedName);
      assert.strictEqual(NPC_ACTOR_CLASSIFICATION_ROSTER[npcId]?.canonicalName, expectedName);
      assert.strictEqual(NPC_SCHEDULE_ROSTER[npcId]?.canonicalName, expectedName);
      assert.strictEqual(NPC_MOVEMENT_PROFILES[npcId]?.canonicalName, expectedName);
      assert.strictEqual(NPC_AUTONOMOUS_BEHAVIOR_PROFILES[npcId]?.canonicalName, expectedName);
      assert.strictEqual(rosterMovementSnap.npcs[npcId]?.canonicalName, expectedName);
      assert.strictEqual(
        forbiddenConflictingNames.has(NPC_AUTONOMOUS_BEHAVIOR_PROFILES[npcId]!.canonicalName),
        false
      );
    }
  });

  it('12B. Bridge Patrol movement: independent profiles, speeds, pauses, bidirectional traversal, turnaround, C^0 continuity & deterministic reconstruction', () => {
    const mateoProf = BRIDGE_PATROL_PROFILES['mateo-student']!;
    const sofiaProf = BRIDGE_PATROL_PROFILES['sofia-cross-border-retail']!;
    const hectorProf = BRIDGE_PATROL_PROFILES['hector-artisan']!;

    // Distinct speeds, pause distributions, initial directions, and cycle lengths
    assert.notStrictEqual(mateoProf.preferredSpeedMps, sofiaProf.preferredSpeedMps);
    assert.notStrictEqual(sofiaProf.preferredSpeedMps, hectorProf.preferredSpeedMps);
    assert.notStrictEqual(mateoProf.preferredSpeedMps, hectorProf.preferredSpeedMps);
    assert.ok(sofiaProf.preferredSpeedMps > mateoProf.preferredSpeedMps);
    assert.ok(mateoProf.preferredSpeedMps > hectorProf.preferredSpeedMps);
    assert.notStrictEqual(mateoProf.initialDirection, sofiaProf.initialDirection);
    assert.notStrictEqual(mateoProf.primaryWaypointId, sofiaProf.primaryWaypointId);
    assert.notStrictEqual(sofiaProf.primaryWaypointId, hectorProf.primaryWaypointId);

    // Sweep a 6-minute patrol window on Wednesday 2026-11-04 10:10:00..10:16:00 MST (17:10:00..17:16:00 UTC) at 1s steps
    const startMs = Date.parse('2026-11-04T17:10:00.000Z');
    const seenStatesByNpc: Record<string, Set<string>> = {
      'mateo-student': new Set(),
      'sofia-cross-border-retail': new Set(),
      'hector-artisan': new Set(),
    };
    const seenDirectionsByNpc: Record<string, Set<string>> = {
      'mateo-student': new Set(),
      'sofia-cross-border-retail': new Set(),
      'hector-artisan': new Set(),
    };
    const minZByNpc: Record<string, number> = {
      'mateo-student': Infinity,
      'sofia-cross-border-retail': Infinity,
      'hector-artisan': Infinity,
    };
    const maxZByNpc: Record<string, number> = {
      'mateo-student': -Infinity,
      'sofia-cross-border-retail': -Infinity,
      'hector-artisan': -Infinity,
    };

    let prevPositions: Record<string, { x: number; y: number; z: number }> | null = null;

    for (let sec = 0; sec <= 360; sec++) {
      const tMs = startMs + sec * 1000;
      const snap = evaluateRosterMovement(NPC_MOVEMENT_PROFILES, tMs, {
        skipPassingEncounters: true,
      });

      const currentPositions: Record<string, { x: number; y: number; z: number }> = {};
      for (const npcId of ['mateo-student', 'sofia-cross-border-retail', 'hector-artisan']) {
        const m = snap.npcs[npcId]!;
        assert.strictEqual(m.behaviorProfileType, 'bridge_patrol');
        assert.strictEqual(m.withinAuthorizedDomain, true);
        assert.ok(
          isPointInBridgePatrolDomain(m.position),
          `${npcId} exited bridge patrol domain at sec=${sec}: ${JSON.stringify(m.position)}`
        );
        assert.notStrictEqual(m.activityState, 'off_duty');

        seenStatesByNpc[npcId]!.add(m.autonomousBehaviorState!);
        if (m.patrolDirection === 'northbound' || m.patrolDirection === 'southbound') {
          seenDirectionsByNpc[npcId]!.add(m.patrolDirection);
        }
        minZByNpc[npcId] = Math.min(minZByNpc[npcId]!, m.position.z);
        maxZByNpc[npcId] = Math.max(maxZByNpc[npcId]!, m.position.z);
        currentPositions[npcId] = m.position;

        // C^0 continuity check: step displacement over 1s must never jump (> max speed 3.85m/s + tolerance)
        if (prevPositions && prevPositions[npcId]) {
          const prev = prevPositions[npcId]!;
          const stepDist = Math.hypot(
            m.position.x - prev.x,
            m.position.y - prev.y,
            m.position.z - prev.z
          );
          assert.ok(
            stepDist <= 4.35,
            `Positional discontinuity for ${npcId} at sec=${sec}: jumped ${stepDist.toFixed(3)}m`
          );
        }
      }
      prevPositions = currentPositions;
    }

    // All three traverse the full built pedestrian bridge (Z <= 42m south to Z >= 415m north, both East and West walkways)
    for (const npcId of ['mateo-student', 'sofia-cross-border-retail', 'hector-artisan']) {
      assert.ok(seenDirectionsByNpc[npcId]!.has('northbound'), `${npcId} must patrol northbound`);
      assert.ok(seenDirectionsByNpc[npcId]!.has('southbound'), `${npcId} must patrol southbound`);
      assert.ok(seenStatesByNpc[npcId]!.has('patrolling'), `${npcId} must enter patrolling state`);
      assert.ok(seenStatesByNpc[npcId]!.has('turning_around'), `${npcId} must enter turning_around state`);
      assert.ok(
        seenStatesByNpc[npcId]!.has('observing') || seenStatesByNpc[npcId]!.has('paused'),
        `${npcId} must enter observing/paused state`
      );
      assert.ok(
        minZByNpc[npcId]! <= 42.0,
        `${npcId} must reach south Ciudad Juárez approach (minZ=${minZByNpc[npcId]})`
      );
      assert.ok(
        maxZByNpc[npcId]! >= 415.0,
        `${npcId} must reach north El Paso end (maxZ=${maxZByNpc[npcId]})`
      );
    }

    // Mid-bridge vehicle roadway must remain strictly excluded from Bridge Patrol domain
    assert.strictEqual(
      isPointInBridgePatrolDomain({ x: 4.0, y: 3.5, z: 150.0 }),
      false,
      'Mid-bridge vehicle roadway must never be inside authorized bridge patrol domain'
    );

    // Midnight date boundary & DST transition continuity (no positional discontinuity across day change)
    const midnightMinus1 = Date.parse('2026-11-05T06:59:59.000Z'); // 23:59:59 MST
    const midnightExact = Date.parse('2026-11-05T07:00:00.000Z');  // 00:00:00 MST next day
    const midnightPlus1 = Date.parse('2026-11-05T07:00:01.000Z');  // 00:00:01 MST next day
    const snapM1 = evaluateRosterMovement(NPC_MOVEMENT_PROFILES, midnightMinus1);
    const snapM0 = evaluateRosterMovement(NPC_MOVEMENT_PROFILES, midnightExact);
    const snapP1 = evaluateRosterMovement(NPC_MOVEMENT_PROFILES, midnightPlus1);

    for (const npcId of ['mateo-student', 'sofia-cross-border-retail', 'hector-artisan']) {
      const d1 = Math.hypot(
        snapM0.npcs[npcId]!.position.x - snapM1.npcs[npcId]!.position.x,
        snapM0.npcs[npcId]!.position.z - snapM1.npcs[npcId]!.position.z
      );
      const d2 = Math.hypot(
        snapP1.npcs[npcId]!.position.x - snapM0.npcs[npcId]!.position.x,
        snapP1.npcs[npcId]!.position.z - snapM0.npcs[npcId]!.position.z
      );
      assert.ok(d1 <= 1.5 && d2 <= 1.5, `Midnight day change caused discontinuity for ${npcId}`);
    }

    // Deterministic variation across different calendar days (same local time, different day -> varied trajectory)
    const dayATime = '2026-11-04T17:18:25.000Z';
    const dayBTime = '2026-11-05T17:18:25.000Z';
    const dayASnap = evaluateRosterMovement(NPC_MOVEMENT_PROFILES, dayATime);
    const dayBSnap = evaluateRosterMovement(NPC_MOVEMENT_PROFILES, dayBTime);
    const anyDiffAcrossDays = ['mateo-student', 'sofia-cross-border-retail', 'hector-artisan'].some(
      (id) =>
        Math.abs(dayASnap.npcs[id]!.position.z - dayBSnap.npcs[id]!.position.z) > 0.05 ||
        dayASnap.npcs[id]!.patrolDirection !== dayBSnap.npcs[id]!.patrolDirection
    );
    assert.ok(anyDiffAcrossDays, 'Patrol trajectories should vary deterministically across calendar dates');

    // Repeated evaluation & skipped interval invariance
    const rep1 = evaluateRosterMovement(NPC_MOVEMENT_PROFILES, dayATime);
    const rep2 = evaluateRosterMovement(NPC_MOVEMENT_PROFILES, dayATime);
    assert.deepStrictEqual(rep1, rep2);
  });

  it('12C. Brief passing encounters: spatial/contextual gates, 4-6s bounded duration, 45s pair cooldown & coexistence with 5.21.0', () => {
    const baseSlotStartMs = Math.floor(Date.parse('2026-11-04T17:15:00.000Z') / 10_000) * 10_000;

    // 1. Compatible approaching bridge patrol proxies within 3.2m trigger passing_greeting / passing_exchange
    const npcA = {
      npcId: 'mateo-student',
      position: { x: 15.42, y: 0.35, z: 44.0 },
      behaviorProfileType: 'bridge_patrol' as const,
      autonomousBehaviorState: 'patrolling' as const,
      isSuspendedByPlayerInteraction: false,
      activityState: 'available',
    };
    const npcB = {
      npcId: 'sofia-cross-border-retail',
      position: { x: 14.98, y: 0.36, z: 45.8 }, // ~1.85m apart
      behaviorProfileType: 'bridge_patrol' as const,
      autonomousBehaviorState: 'patrolling' as const,
      isSuspendedByPlayerInteraction: false,
      activityState: 'available',
    };

    const activeEval = evaluatePassingEncounterPairStatus({
      npcA,
      npcB,
      evaluatedAtMs: baseSlotStartMs + 1500,
    });
    assert.ok(activeEval.activeEncounter, 'Eligible close patrol proxies should trigger passing encounter');
    const enc = activeEval.activeEncounter!;
    const durationSec = (enc.expectedEndAtMs - enc.startedAtMs) / 1000;
    assert.ok(
      durationSec >= PASSING_ENCOUNTER_MIN_DURATION_SECONDS &&
      durationSec <= PASSING_ENCOUNTER_MAX_DURATION_SECONDS,
      `Encounter duration ${durationSec}s must be bounded between 4s and 6s`
    );
    assert.ok(
      enc.encounterType === 'passing_greeting' || enc.encounterType === 'passing_exchange'
    );

    // 2. After expectedEndAtMs (e.g. +7s into slot), encounter completes and enters cooldown
    const postEval = evaluatePassingEncounterPairStatus({
      npcA,
      npcB,
      evaluatedAtMs: baseSlotStartMs + 7000,
    });
    assert.strictEqual(postEval.activeEncounter, null);
    assert.strictEqual(postEval.inCooldown, true);

    // 3. Cooldown prevents immediate repeated encounters across subsequent 10s slots while still close
    const duringCooldownEval = evaluatePassingEncounterPairStatus({
      npcA,
      npcB,
      evaluatedAtMs: baseSlotStartMs + 21_500,
      wasCloseAtFn: (tMs) => tMs >= baseSlotStartMs && tMs < baseSlotStartMs + 10_000,
    });
    assert.strictEqual(duringCooldownEval.activeEncounter, null);
    assert.strictEqual(duringCooldownEval.inCooldown, true);
    assert.strictEqual(
      duringCooldownEval.cooldownUntilMs,
      baseSlotStartMs + PASSING_ENCOUNTER_COOLDOWN_SECONDS * 1000
    );

    // 4. Natural patrol sweep verifies real passing encounters occur and resume patrol afterward
    const sweepStartMs = Date.parse('2026-11-04T17:10:00.000Z');
    let observedNaturalPassing = false;
    let passedPartnerId: string | null = null;
    let passedAtMs = 0;

    for (let sec = 0; sec <= 1800; sec += 2) {
      const tMs = sweepStartMs + sec * 1000;
      const roster = evaluateRosterMovement(NPC_MOVEMENT_PROFILES, tMs);
      const activePasser = ['mateo-student', 'sofia-cross-border-retail', 'hector-artisan']
        .map((id) => roster.npcs[id]!)
        .find((n) => n.autonomousBehaviorState === 'passing_encounter');
      if (activePasser) {
        observedNaturalPassing = true;
        passedPartnerId = activePasser.passingEncounterPartnerNpcId!;
        passedAtMs = tMs;

        // Verify social-interactions endpoint surfaces passing_greeting / passing_exchange
        const socialSnap = evaluateRosterSocialInteractions({
          evaluatedAt: new Date(tMs).toISOString(),
          movementRoster: roster,
        });
        const passSess = socialSnap.sessions.find((s) =>
          s.participantNpcIds.includes(activePasser.npcId)
        );
        assert.ok(passSess, 'Social interactions roster must surface active passing encounter session');
        assert.ok(
          passSess!.interactionType === 'passing_greeting' ||
          passSess!.interactionType === 'passing_exchange'
        );

        // Verify patrol resumes cleanly 8 seconds after passing encounter starts
        const resumedRoster = evaluateRosterMovement(NPC_MOVEMENT_PROFILES, passedAtMs + 8000);
        assert.notStrictEqual(
          resumedRoster.npcs[activePasser.npcId]!.autonomousBehaviorState,
          'passing_encounter',
          'Passing greeting must not permanently stop patrol progression'
        );
        break;
      }
    }
    assert.ok(
      observedNaturalPassing && passedPartnerId,
      'Bridge patrol proxies must naturally trigger passing encounters during patrol'
    );
  });

  it('12D. Player interaction suspension: mere proximity does not stop patrol; authorized interaction suspends and resumes without jumping', async () => {
    const baseMs = Date.parse('2026-11-04T17:12:00.000Z');
    let tPatrolStartMs = baseMs;
    for (let sec = 0; sec <= 360; sec++) {
      const candMs = baseMs + sec * 1000;
      const r = evaluateRosterMovement(NPC_MOVEMENT_PROFILES, candMs);
      const m = r.npcs['mateo-student']!;
      const s = r.npcs['sofia-cross-border-retail']!;
      const h = r.npcs['hector-artisan']!;
      const dMS = Math.hypot(m.position.x - s.position.x, m.position.z - s.position.z);
      const dMH = Math.hypot(m.position.x - h.position.x, m.position.z - h.position.z);
      if (
        m.movementState === 'moving' &&
        m.autonomousBehaviorState === 'patrolling' &&
        m.position.y <= 3.5 &&
        dMS > 4.0 &&
        dMH > 4.0
      ) {
        tPatrolStartMs = candMs;
        break;
      }
    }
    const tPatrolStart = new Date(tPatrolStartMs).toISOString();
    let clockMs = tPatrolStartMs;
    const worldCore = new InMemoryWorldCoreAdapter({ nowFn: () => clockMs });
    worldCore.setSimulatedWorldTime(tPatrolStart);

    // 1. Mateo is actively patrolling at tPatrolStart
    const mateoInitial = await worldCore.getNpcMovement('mateo-student', tPatrolStart);
    assert.ok(mateoInitial);
    assert.strictEqual(mateoInitial!.movementState, 'moving');
    assert.strictEqual(mateoInitial!.autonomousBehaviorState, 'patrolling');
    assert.strictEqual(mateoInitial!.isSuspendedByPlayerInteraction, false);

    // 2. Walk player close to Mateo's position: mere proximity does NOT suspend patrol
    for (let seq = 1; seq <= 200; seq++) {
      const pState = await worldCore.getPlayerState('player-1');
      assert.ok(pState);
      const dx = mateoInitial!.position.x - pState.position.x;
      const dz = mateoInitial!.position.z - pState.position.z;
      const dist = Math.hypot(dx, dz);
      if (dist <= 0.6) break;
      const stepSec = Math.min(0.4, dist / 7.0);
      clockMs += Math.max(50, Math.round(stepSec * 1000));
      await worldCore.applyPlayerMovement(
        'player-1',
        { direction: { x: dx / dist, z: dz / dist }, movementMode: 'sprint', sequenceNumber: seq },
        clockMs
      );
    }

    const mateoWithNearbyPlayer = await worldCore.getNpcMovement('mateo-student', tPatrolStart);
    assert.strictEqual(
      mateoWithNearbyPlayer!.movementState,
      'moving',
      'Mere player proximity must not stop patrol'
    );
    assert.strictEqual(mateoWithNearbyPlayer!.isSuspendedByPlayerInteraction, false);

    // Awareness notices the close player while Mateo continues walking
    const awrWithNearbyPlayer = await worldCore.getNpcSocialAwareness(
      'mateo-student',
      'player-1',
      tPatrolStart
    );
    assert.strictEqual(awrWithNearbyPlayer?.awarenessState, 'player_attentive');

    // 3. Player explicitly initiates an authorized interaction -> patrol is suspended at current position
    const authResult = await validateNpcInteraction(worldCore, {
      playerId: 'player-1',
      npcId: 'mateo-student',
      intent: 'talk',
      evaluatedAt: tPatrolStart,
    });
    assert.strictEqual(authResult.allowed, true);

    const mateoPaused0 = await worldCore.getNpcMovement('mateo-student', tPatrolStart);
    assert.strictEqual(mateoPaused0!.movementState, 'stationary');
    assert.strictEqual(mateoPaused0!.autonomousBehaviorState, 'player_interaction_paused');
    assert.strictEqual(mateoPaused0!.isSuspendedByPlayerInteraction, true);
    assert.strictEqual(mateoPaused0!.suspendedByPlayerId, 'player-1');
    assert.deepStrictEqual(mateoPaused0!.position, mateoInitial!.position);

    // 4. Advance simulation clock by 90 seconds during prolonged conversation: Mateo remains stationary at exact same position
    const tDuringChat = new Date(tPatrolStartMs + 90_000).toISOString();
    const mateoPaused90 = await worldCore.getNpcMovement('mateo-student', tDuringChat);
    assert.strictEqual(mateoPaused90!.movementState, 'stationary');
    assert.strictEqual(mateoPaused90!.autonomousBehaviorState, 'player_interaction_paused');
    assert.strictEqual(mateoPaused90!.isSuspendedByPlayerInteraction, true);
    assert.deepStrictEqual(mateoPaused90!.position, mateoInitial!.position);
    assert.strictEqual(mateoPaused90!.patrolDirection, mateoInitial!.patrolDirection);

    // 5. Close conversation at +90s -> releases suspension, enters resuming state at the exact preserved position (zero jump!)
    const mateoResumed = await worldCore.releaseNpcMovementFromInteraction(
      'player-1',
      'mateo-student',
      tDuringChat
    );
    assert.ok(mateoResumed);
    assert.strictEqual(mateoResumed!.isSuspendedByPlayerInteraction, false);
    assert.strictEqual(mateoResumed!.autonomousBehaviorState, 'resuming');
    assert.deepStrictEqual(
      mateoResumed!.position,
      mateoInitial!.position,
      'Closing a prolonged conversation must not jump the NPC forward'
    );
    assert.strictEqual(mateoResumed!.patrolDirection, mateoInitial!.patrolDirection);

    // 1 second after closing (+91s), Mateo has moved smoothly by ~1 second of walking from the paused position
    const tAfter1s = new Date(tPatrolStartMs + 91_000).toISOString();
    const mateoAfter1s = await worldCore.getNpcMovement('mateo-student', tAfter1s);
    const stepAfterResume = Math.hypot(
      mateoAfter1s!.position.x - mateoInitial!.position.x,
      mateoAfter1s!.position.z - mateoInitial!.position.z
    );
    assert.ok(
      stepAfterResume > 2.0 && stepAfterResume < 3.9,
      `Expected smooth 1s step (~3.45m/s) after resume, got ${stepAfterResume.toFixed(3)}m`
    );

    // 3 seconds after closing (+93s), resuming transitions back to normal patrolling
    const tAfter3s = new Date(tPatrolStartMs + 93_000).toISOString();
    const mateoAfter3s = await worldCore.getNpcMovement('mateo-student', tAfter3s);
    assert.strictEqual(mateoAfter3s!.autonomousBehaviorState, 'patrolling');

    // 6. Repeated open/close cycle (+93s -> +115s) preserves state without corruption
    await worldCore.suspendNpcMovementForInteraction('player-1', 'mateo-student', tAfter3s);
    const tSecondClose = new Date(tPatrolStartMs + 115_000).toISOString();
    const mateoPausedAgain = await worldCore.getNpcMovement('mateo-student', tSecondClose);
    assert.strictEqual(mateoPausedAgain!.isSuspendedByPlayerInteraction, true);
    assert.deepStrictEqual(mateoPausedAgain!.position, mateoAfter3s!.position);

    const mateoResumedAgain = await worldCore.releaseNpcMovementFromInteraction(
      'player-1',
      'mateo-student',
      tSecondClose
    );
    assert.strictEqual(mateoResumedAgain!.isSuspendedByPlayerInteraction, false);
    assert.deepStrictEqual(mateoResumedAgain!.position, mateoAfter3s!.position);

    // 7. Player interaction safely interrupts an active passing encounter
    const sweepStartMs = Date.parse('2026-11-04T17:10:00.000Z');
    let passingInstantIso: string | null = null;
    let passingNpcId = 'mateo-student';
    for (let sec = 0; sec <= 1800; sec += 2) {
      const candidateIso = new Date(sweepStartMs + sec * 1000).toISOString();
      const r = evaluateRosterMovement(NPC_MOVEMENT_PROFILES, candidateIso);
      const found = ['mateo-student', 'sofia-cross-border-retail', 'hector-artisan']
        .map((id) => r.npcs[id]!)
        .find((n) => n.autonomousBehaviorState === 'passing_encounter');
      if (found) {
        passingInstantIso = candidateIso;
        passingNpcId = found.npcId;
        break;
      }
    }
    assert.ok(passingInstantIso, 'Must locate a passing encounter instant');
    const cleanWorldCore = new InMemoryWorldCoreAdapter();
    const beforeInterrupt = await cleanWorldCore.getNpcRosterMovement(passingInstantIso!);
    const partnerId = beforeInterrupt.npcs[passingNpcId]!.passingEncounterPartnerNpcId!;
    assert.ok(partnerId);
    assert.strictEqual(beforeInterrupt.npcs[partnerId]!.autonomousBehaviorState, 'passing_encounter');

    await cleanWorldCore.suspendNpcMovementForInteraction('player-1', passingNpcId, passingInstantIso!);
    const afterInterrupt = await cleanWorldCore.getNpcRosterMovement(passingInstantIso!);
    assert.strictEqual(
      afterInterrupt.npcs[passingNpcId]!.autonomousBehaviorState,
      'player_interaction_paused'
    );
    assert.strictEqual(afterInterrupt.npcs[passingNpcId]!.passingEncounterPartnerNpcId, null);
    assert.notStrictEqual(
      afterInterrupt.npcs[partnerId]!.autonomousBehaviorState,
      'passing_encounter',
      'Partner must safely exit passing encounter when player interrupts'
    );
  });

  it('12E & 12F. Business domain containment & task priority for Carlos and Valeria', async () => {
    // 0. Establish and verify convexity invariant for all authorized patrol and business polygons
    assert.strictEqual(
      isConvexPolygonXZ(FARMACIA_BENAVIDES_AUTONOMY_DOMAIN.polygonXZ),
      true,
      'Farmacias Benavides authorized polygonXZ must be strictly convex'
    );
    assert.strictEqual(
      isConvexPolygonXZ(OPTICA_FASHION_AUTONOMY_DOMAIN.polygonXZ),
      true,
      'Optica Fashion authorized polygonXZ must be strictly convex'
    );
    assert.strictEqual(
      isConvexPolygonXZ(BRIDGE_PATROL_DOMAIN.polygonXZ),
      true,
      'Bridge Patrol concourse polygonXZ must be strictly convex'
    );

    // Non-convex (notched / L-shaped) polygon must be identified as non-convex and its exterior notch crossings rejected
    // even when a thin exterior notch (width 0.10m over a 6.0m segment, < 1/16th of segment length) would evade 16-step sampling
    const notchedConcavePolygon = [
      { x: 8.0, z: -3.0 },
      { x: 11.2, z: -3.0 },
      { x: 11.2, z: 3.0 },
      { x: 8.0, z: 3.0 },
      { x: 8.0, z: 0.23 },
      { x: 10.5, z: 0.23 },
      { x: 10.5, z: 0.13 },
      { x: 8.0, z: 0.13 },
    ];
    assert.strictEqual(isConvexPolygonXZ(notchedConcavePolygon), false);
    assert.strictEqual(
      isSegmentInPolygonXZ(
        { x: 9.0, z: -2.5 },
        { x: 9.0, z: 2.5 },
        notchedConcavePolygon,
        0.01
      ),
      false,
      'Segment crossing a narrow exterior notch must be rejected via exact edge-intersection and interval-midpoint check'
    );
    assert.strictEqual(
      isSegmentInPolygonXZ(
        { x: 10.8, z: -2.5 },
        { x: 10.8, z: 2.5 },
        notchedConcavePolygon,
        0.01
      ),
      true,
      'Segment staying inside the solid spine of the non-convex polygon must be accepted'
    );

    const notchedCustomDomain: BusinessMovementDomain = {
      ...FARMACIA_BENAVIDES_AUTONOMY_DOMAIN,
      polygonXZ: notchedConcavePolygon,
      navigationToleranceMeters: 0.01,
    };
    assert.strictEqual(
      isSegmentInBusinessDomain(
        { x: 9.0, y: 0.2, z: -2.5 },
        { x: 9.0, y: 0.2, z: 2.5 },
        notchedCustomDomain
      ),
      false,
      'isSegmentInBusinessDomain must reject segments crossing a non-convex exterior notch'
    );

    // 1. Validate all configured business local graph nodes and connected edges against BOTH safe roaming & absolute domain
    for (const [npcId, domain] of Object.entries(BUSINESS_AUTONOMY_DOMAINS)) {
      const prof = BUSINESS_LOCAL_AUTONOMY_PROFILES[npcId]!;
      assert.strictEqual(isConvexPolygonXZ(domain.safeRoamingPolygonXZ), true, `${npcId} safeRoamingPolygonXZ must be convex`);
      for (const [nodeId, nodeSpec] of Object.entries(prof.localNodes)) {
        for (const targetId of nodeSpec.connectedWaypointIds) {
          const graphCheck = validateBusinessLocalGraphRoute([nodeId, targetId], domain, prof, CORRIDOR_WAYPOINTS);
          assert.strictEqual(graphCheck.valid, true, `${npcId} local edge ${nodeId}->${targetId}: ${graphCheck.reason}`);
        }
      }
    }

    // 2. Reject invalid / out-of-domain / street-lane candidate destinations
    assert.strictEqual(
      isPointInBusinessDomain({ x: 0.0, y: 0.2, z: 1.0 }, FARMACIA_BENAVIDES_AUTONOMY_DOMAIN),
      false,
      'Street centerline must be rejected'
    );
    assert.strictEqual(
      isPointInBusinessDomain({ x: 10.5, y: 0.2, z: -8.0 }, FARMACIA_BENAVIDES_AUTONOMY_DOMAIN),
      false,
      'Adjacent business (La Michoacana Z=-8.0) must be rejected'
    );
    assert.strictEqual(
      isPointInBusinessDomain({ x: -10.5, y: 0.2, z: 18.55 }, OPTICA_FASHION_AUTONOMY_DOMAIN),
      false,
      'Patricia Citizen Desk (Z=18.55) must be rejected from Optica domain'
    );
    assert.strictEqual(
      validateBusinessCandidateRoute(
        ['wp_carlos_counter', 'wp_east_dining_benches'],
        FARMACIA_BENAVIDES_AUTONOMY_DOMAIN,
        CORRIDOR_WAYPOINTS
      ).valid,
      false,
      'External lunch benches must not be treated as local store roaming'
    );
    assert.strictEqual(
      isSegmentInBusinessDomain(
        { x: 10.5, y: 0.2, z: 0.0 },
        { x: 5.0, y: 0.2, z: 0.0 },
        FARMACIA_BENAVIDES_AUTONOMY_DOMAIN
      ),
      false
    );

    // 3. Sweep 4 minutes of daytime operating hours (10:10..10:14 MST): verify Carlos & Valeria roam strictly inside their domains
    const startMs = Date.parse('2026-11-04T17:10:00.000Z');
    const carlosStates = new Set<string>();
    const valeriaStates = new Set<string>();

    for (let sec = 0; sec <= 240; sec += 2) {
      const tMs = startMs + sec * 1000;
      const snap = evaluateRosterMovement(NPC_MOVEMENT_PROFILES, tMs);
      const carlos = snap.npcs['carlos-farmacia-attendant']!;
      const valeria = snap.npcs['valeria-optica-attendant']!;

      assert.strictEqual(carlos.withinAuthorizedDomain, true);
      assert.ok(isPointInBusinessDomain(carlos.position, FARMACIA_BENAVIDES_AUTONOMY_DOMAIN));
      carlosStates.add(carlos.autonomousBehaviorState!);

      assert.strictEqual(valeria.withinAuthorizedDomain, true);
      assert.ok(isPointInBusinessDomain(valeria.position, OPTICA_FASHION_AUTONOMY_DOMAIN));
      valeriaStates.add(valeria.autonomousBehaviorState!);
    }

    assert.ok(carlosStates.has('working_stationary'));
    assert.ok(carlosStates.has('local_roaming'));
    assert.ok(carlosStates.has('idle_observation'));
    assert.ok(carlosStates.has('returning_to_station'));

    assert.ok(valeriaStates.has('working_stationary'));
    assert.ok(valeriaStates.has('local_roaming'));
    assert.ok(valeriaStates.has('idle_observation'));
    assert.ok(valeriaStates.has('returning_to_station'));

    // 4. Busy service task (Carlos 21:15 MST register closing) prevents optional roaming
    const busySnap = evaluateNpcMovement(
      NPC_MOVEMENT_PROFILES['carlos-farmacia-attendant']!,
      '2026-11-05T04:15:00.000Z'
    );
    assert.strictEqual(busySnap.movementState, 'stationary');
    assert.strictEqual(busySnap.autonomousBehaviorState, 'working_stationary');
    assert.strictEqual(busySnap.currentWaypointId, 'wp_carlos_counter');

    // 5. Off-duty (Sunday or night) prevents roaming and sets off_duty state
    const offDutySnap = evaluateNpcMovement(
      NPC_MOVEMENT_PROFILES['valeria-optica-attendant']!,
      '2026-11-08T18:00:00.000Z'
    );
    assert.strictEqual(offDutySnap.movementState, 'stationary');
    assert.strictEqual(offDutySnap.autonomousBehaviorState, 'off_duty');
    assert.strictEqual(offDutySnap.activityState, 'off_duty');

    // 6. Customer interaction suspends Carlos's local roaming
    const tRoam = '2026-11-04T17:10:30.000Z';
    let clockMs = Date.parse(tRoam);
    const worldCore = new InMemoryWorldCoreAdapter({ nowFn: () => clockMs });
    worldCore.setSimulatedWorldTime(tRoam);
    const carlosBefore = await worldCore.getNpcMovement('carlos-farmacia-attendant', tRoam);
    assert.ok(carlosBefore);

    // Move player to Carlos
    for (let seq = 1; seq <= 25; seq++) {
      const p = (await worldCore.getPlayerState('player-1'))!;
      const dx = carlosBefore!.position.x - p.position.x;
      const dz = carlosBefore!.position.z - p.position.z;
      const d = Math.hypot(dx, dz);
      if (d <= 1.8) break;
      clockMs += 400;
      await worldCore.applyPlayerMovement(
        'player-1',
        { direction: { x: dx / d, z: dz / d }, movementMode: 'sprint', sequenceNumber: seq },
        clockMs
      );
    }

    const serviceAuth = await validateNpcInteraction(worldCore, {
      playerId: 'player-1',
      npcId: 'carlos-farmacia-attendant',
      intent: 'service',
      evaluatedAt: tRoam,
    });
    assert.strictEqual(serviceAuth.allowed, true);

    const carlosServing = await worldCore.getNpcMovement('carlos-farmacia-attendant', tRoam);
    assert.strictEqual(carlosServing!.movementState, 'stationary');
    assert.strictEqual(carlosServing!.autonomousBehaviorState, 'customer_interaction');
    assert.strictEqual(carlosServing!.isSuspendedByPlayerInteraction, true);
  });

  it('12G. Excluded fixed kiosk NPCs (Elena & Patricia) retain exact fixed anchors and scheduled transitions', () => {
    const testTimes = [
      '2026-11-04T15:15:00.000Z', // 08:15 MST
      '2026-11-04T17:22:00.000Z', // 10:22 MST
      '2026-11-04T22:40:00.000Z', // 15:40 MST
      '2026-11-05T05:10:00.000Z', // 22:10 MST (off duty)
    ];

    for (const iso of testTimes) {
      const snap = evaluateRosterMovement(NPC_MOVEMENT_PROFILES, iso);
      const elena = snap.npcs['elena-border-concierge']!;
      const patricia = snap.npcs['patricia-citizen-rep']!;

      assert.strictEqual(elena.behaviorProfileType, 'fixed_anchor_scheduled');
      assert.strictEqual(elena.controlMode, 'fixed_station_anchor');
      assert.strictEqual(elena.movementState, 'stationary');
      assert.strictEqual(elena.currentWaypointId, 'wp_elena_kiosk');
      assert.deepStrictEqual(elena.position, { x: 19.2, y: 0.2, z: 39.5 });

      assert.strictEqual(patricia.behaviorProfileType, 'fixed_anchor_scheduled');
      assert.strictEqual(patricia.controlMode, 'fixed_station_anchor');
      assert.strictEqual(patricia.movementState, 'stationary');
      assert.strictEqual(patricia.currentWaypointId, 'wp_patricia_desk');
      assert.deepStrictEqual(patricia.position, { x: -10.8, y: 0.0, z: 18.55 });
    }

    // Elena's scheduled 13:30 handover walk and Patricia's scheduled 12:00 inspection walk still work identically
    const elenaHandover = evaluateNpcMovement(
      NPC_MOVEMENT_PROFILES['elena-border-concierge']!,
      '2026-11-04T20:30:05.000Z'
    );
    assert.strictEqual(elenaHandover.movementState, 'moving');
    assert.strictEqual(elenaHandover.controlMode, 'scheduled_routine_transition');

    const patriciaInspection = evaluateNpcMovement(
      NPC_MOVEMENT_PROFILES['patricia-citizen-rep']!,
      '2026-11-04T19:00:05.000Z'
    );
    assert.strictEqual(patriciaInspection.movementState, 'moving');
    assert.strictEqual(patriciaInspection.controlMode, 'scheduled_routine_transition');
  });

  it('12H. Personal-space separation, collision-aware yielding, walking speeds & corridor safety (Increment 5.22.1)', () => {
    // 1. Tuned walking speeds relative to player walkSpeed (4.5 m/s)
    assert.strictEqual(BRIDGE_PATROL_PROFILES['mateo-student']!.preferredSpeedMps, 3.45);
    assert.strictEqual(BRIDGE_PATROL_PROFILES['sofia-cross-border-retail']!.preferredSpeedMps, 3.85);
    assert.strictEqual(BRIDGE_PATROL_PROFILES['hector-artisan']!.preferredSpeedMps, 2.85);
    assert.strictEqual(BUSINESS_LOCAL_AUTONOMY_PROFILES['carlos-farmacia-attendant']!.preferredSpeedMps, 1.25);
    assert.strictEqual(BUSINESS_LOCAL_AUTONOMY_PROFILES['valeria-optica-attendant']!.preferredSpeedMps, 1.10);

    // 2. Same-direction overlap -> deterministic lateral / longitudinal separation >= MIN_PERSONAL_SPACE_METERS
    const closeSameDir = {
      'mateo-student': {
        npcId: 'mateo-student',
        position: { x: 15.25, y: 1.58, z: 104.11 },
        forwardHeading: { x: 0, z: 1 },
        movementState: 'moving' as const,
        behaviorProfileType: 'bridge_patrol' as const,
        autonomousBehaviorState: 'patrolling' as const,
        isSuspendedByPlayerInteraction: false,
        patrolCycleIndex: 2,
        withinAuthorizedDomain: true,
      },
      'sofia-cross-border-retail': {
        npcId: 'sofia-cross-border-retail',
        position: { x: 15.28, y: 1.58, z: 104.35 }, // 0.24m apart!
        forwardHeading: { x: 0, z: 1 },
        movementState: 'moving' as const,
        behaviorProfileType: 'bridge_patrol' as const,
        autonomousBehaviorState: 'patrolling' as const,
        isSuspendedByPlayerInteraction: false,
        patrolCycleIndex: 1,
        withinAuthorizedDomain: true,
      },
    };
    applyRosterPersonalSpaceSeparation(closeSameDir);
    const sepSame = Math.hypot(
      closeSameDir['sofia-cross-border-retail']!.position.x - closeSameDir['mateo-student']!.position.x,
      closeSameDir['sofia-cross-border-retail']!.position.z - closeSameDir['mateo-student']!.position.z
    );
    assert.ok(
      sepSame >= MIN_PERSONAL_SPACE_METERS - 1e-3,
      `Expected >= ${MIN_PERSONAL_SPACE_METERS}m personal space, got ${sepSame.toFixed(3)}m`
    );
    assert.ok(isPointInBridgePatrolDomain(closeSameDir['mateo-student']!.position));
    assert.ok(isPointInBridgePatrolDomain(closeSameDir['sofia-cross-border-retail']!.position));

    // 3. Narrow space longitudinal yielding (when near edge with insufficient lateral room)
    const narrowYield = {
      'hector-artisan': {
        npcId: 'hector-artisan',
        position: { x: 14.38, y: 0.529, z: 74.02 },
        forwardHeading: { x: 0, z: 1 },
        movementState: 'stationary' as const,
        behaviorProfileType: 'bridge_patrol' as const,
        autonomousBehaviorState: 'observing' as const,
        isSuspendedByPlayerInteraction: false,
        patrolCycleIndex: 1,
        withinAuthorizedDomain: true,
      },
      'mateo-student': {
        npcId: 'mateo-student',
        position: { x: 14.38, y: 0.529, z: 73.70 }, // 0.32m behind Hector near west edge
        forwardHeading: { x: 0, z: 1 },
        movementState: 'moving' as const,
        behaviorProfileType: 'bridge_patrol' as const,
        autonomousBehaviorState: 'patrolling' as const,
        isSuspendedByPlayerInteraction: false,
        patrolCycleIndex: 1,
        withinAuthorizedDomain: true,
      },
    };
    applyRosterPersonalSpaceSeparation(narrowYield);
    const sepNarrow = Math.hypot(
      narrowYield['mateo-student']!.position.x - narrowYield['hector-artisan']!.position.x,
      narrowYield['mateo-student']!.position.z - narrowYield['hector-artisan']!.position.z
    );
    assert.ok(sepNarrow >= MIN_PERSONAL_SPACE_METERS - 1e-3);
    assert.ok(isPointInBridgePatrolDomain(narrowYield['mateo-student']!.position));

    // 4. Roster sweep across 180 seconds verifies all bridge proxies maintain >= MIN_PERSONAL_SPACE_METERS at all times
    const startMs = Date.parse('2026-11-04T17:15:00.000Z');
    for (let sec = 0; sec <= 180; sec += 2) {
      const snap = evaluateRosterMovement(NPC_MOVEMENT_PROFILES, startMs + sec * 1000);
      const ids = ['mateo-student', 'sofia-cross-border-retail', 'hector-artisan'];
      for (let i = 0; i < ids.length; i++) {
        for (let j = i + 1; j < ids.length; j++) {
          const a = snap.npcs[ids[i]!]!;
          const b = snap.npcs[ids[j]!]!;
          const dy = Math.abs(a.position.y - b.position.y);
          if (dy <= 2.0) {
            const d = Math.hypot(a.position.x - b.position.x, a.position.z - b.position.z);
            assert.ok(
              d >= MIN_PERSONAL_SPACE_METERS - 1e-3,
              `Personal space violation between ${ids[i]} and ${ids[j]} at sec=${sec}: ${d.toFixed(3)}m`
            );
          }
        }
      }
    }
  });

  it('12I. Increment 5.22.3: Business speed consistency, safe interior roaming, local graph anti-repetition & Valeria calibration preservation', () => {
    // A. Speed consistency: actual displacement delta/dt matches currentSpeedMps; dwell/stationary reports 0; segmentEndPosition provided
    const tSweepStart = Date.parse('2026-11-04T17:10:00.000Z'); // 10:10 MST open hours
    const dtStepSec = 0.25;

    for (const npcId of ['carlos-farmacia-attendant', 'valeria-optica-attendant'] as const) {
      const prof = NPC_MOVEMENT_PROFILES[npcId]!;
      const domain = BUSINESS_AUTONOMY_DOMAINS[npcId]!;
      const localProf = BUSINESS_LOCAL_AUTONOMY_PROFILES[npcId]!;

      let sawMoving = false;
      let sawStationary = false;
      const visitedWaypoints = new Set<string>();
      let prevSnap = evaluateNpcMovement(prof, tSweepStart);

      // Sweep 600s (~8 full cycles) at 0.25s resolution for sub-second speed, dwell, and multi-stop coverage checks
      for (let t = dtStepSec; t <= 600; t += dtStepSec) {
        const curSnap = evaluateNpcMovement(prof, tSweepStart + t * 1000);
        visitedWaypoints.add(curSnap.currentWaypointId);

        // B. Interior containment: EVERY position (both moving and stationary) stays strictly inside safeRoamingBounds & polygon
        assert.strictEqual(curSnap.withinAuthorizedDomain, true);
        assert.ok(
          isPointInSafeBusinessRoamingRegion(curSnap.position, domain),
          `${npcId} exited safe roaming region at +${t}s: (${curSnap.position.x}, ${curSnap.position.z})`
        );

        // Verify clearance >= 0.55m from outer domain edges where applicable
        const marginMinX = curSnap.position.x - domain.bounds.minX;
        const marginMaxX = domain.bounds.maxX - curSnap.position.x;
        assert.ok(
          marginMinX >= 0.54 && marginMaxX >= 0.54,
          `${npcId} X=${curSnap.position.x} violated 0.55m clearance from X bounds [${domain.bounds.minX}, ${domain.bounds.maxX}]`
        );

        if (npcId === 'valeria-optica-attendant') {
          // Valeria never leaves Bay 4 Optica Fashion frontage Z = [-1.80, +3.40] during ordinary roaming
          assert.ok(
            curSnap.position.z >= -1.66 && curSnap.position.z <= 2.91,
            `Valeria Z=${curSnap.position.z} wandered outside Optica Bay 4 frontage`
          );
        } else {
          // Carlos stays strictly inside Farmacias Benavides frontage
          assert.ok(
            curSnap.position.z >= -2.66 && curSnap.position.z <= 3.86,
            `Carlos Z=${curSnap.position.z} exited Farmacias Benavides frontage`
          );
        }

        if (curSnap.movementState === 'moving') {
          sawMoving = true;
          assert.ok(curSnap.currentSpeedMps && curSnap.currentSpeedMps > 0.8 && curSnap.currentSpeedMps <= 1.4,
            `${npcId} currentSpeedMps=${curSnap.currentSpeedMps} out of controlled interior range`);
          assert.strictEqual(curSnap.currentSpeedMps, localProf.preferredSpeedMps);
          assert.ok(curSnap.activeSegmentEndPosition, `${npcId} missing activeSegmentEndPosition while moving`);
          assert.ok(isPointInSafeBusinessRoamingRegion(curSnap.activeSegmentEndPosition!, domain));

          // Check instantaneous velocity consistency when continuing on same route segment without midpoint jump
          if (
            prevSnap.movementState === 'moving' &&
            prevSnap.activeRouteId === curSnap.activeRouteId
          ) {
            const actualDist = Math.hypot(
              curSnap.position.x - prevSnap.position.x,
              curSnap.position.y - prevSnap.position.y,
              curSnap.position.z - prevSnap.position.z
            );
            const empiricalSpeed = actualDist / dtStepSec;
            assert.ok(
              Math.abs(empiricalSpeed - curSnap.currentSpeedMps!) < 0.08,
              `${npcId} empirical speed ${empiricalSpeed.toFixed(3)} != currentSpeedMps ${curSnap.currentSpeedMps} on ${curSnap.activeRouteId}`
            );
          }
        } else {
          sawStationary = true;
          assert.strictEqual(curSnap.currentSpeedMps, 0, `${npcId} stationary state must report currentSpeedMps=0`);
        }

        prevSnap = curSnap;
      }

      assert.ok(sawMoving, `${npcId} must walk during open business hours`);
      assert.ok(sawStationary, `${npcId} must pause during open business hours`);
      // C. Local graph visits multiple distinct local destinations across cycles
      assert.ok(
        visitedWaypoints.size >= 4,
        `${npcId} should visit at least 4 distinct local nodes; visited=${[...visitedWaypoints].join(',')}`
      );
    }

    // C2. Deterministic anti-repetition: consecutive cycles pick distinct first local destinations when alternatives exist
    const baseCycleCheckMs = Date.parse('2026-11-04T17:10:00.000Z');
    for (const npcId of ['carlos-farmacia-attendant', 'valeria-optica-attendant'] as const) {
      const prof = NPC_MOVEMENT_PROFILES[npcId]!;
      const cycleFirstDestinations = new Map<number, string>();
      for (let s = 0; s < 420; s += 1) {
        const snap = evaluateNpcMovement(prof, baseCycleCheckMs + s * 1000);
        if (snap.autonomousBehaviorState === 'local_roaming' && snap.patrolCycleIndex !== undefined && snap.destinationWaypointId) {
          if (!cycleFirstDestinations.has(snap.patrolCycleIndex)) {
            cycleFirstDestinations.set(snap.patrolCycleIndex, snap.destinationWaypointId);
          }
        }
      }
      const orderedCycles = [...cycleFirstDestinations.keys()].sort((a, b) => a - b);
      assert.ok(orderedCycles.length >= 4, `${npcId} should produce >=4 cycles in 7 minutes`);
      for (let i = 1; i < orderedCycles.length; i++) {
        const prevCy = orderedCycles[i - 1]!;
        const curCy = orderedCycles[i]!;
        if (curCy === prevCy + 1) {
          assert.notStrictEqual(
            cycleFirstDestinations.get(curCy),
            cycleFirstDestinations.get(prevCy),
            `${npcId} repeated consecutive cycle first destination ${cycleFirstDestinations.get(curCy)} at cycle ${prevCy}->${curCy}`
          );
        }
      }
    }

    // C3. Invalid node/edge fallback stays safe without crashing or retrying invalid edges
    const brokenRegistry = {
      ...CORRIDOR_WAYPOINTS,
      wp_farmacia_display_north: {
        ...CORRIDOR_WAYPOINTS['wp_farmacia_display_north']!,
        position: { x: 5.0, y: 0.2, z: 3.2 }, // In roadway!
      },
      wp_farmacia_front_threshold: {
        ...CORRIDOR_WAYPOINTS['wp_farmacia_front_threshold']!,
        position: { x: 5.0, y: 0.2, z: 0.4 },
      },
      wp_farmacia_window_south: {
        ...CORRIDOR_WAYPOINTS['wp_farmacia_window_south']!,
        position: { x: 5.0, y: 0.2, z: -1.35 },
      },
      wp_farmacia_consultorio_south: {
        ...CORRIDOR_WAYPOINTS['wp_farmacia_consultorio_south']!,
        position: { x: 5.0, y: 0.2, z: -2.1 },
      },
    };
    assert.strictEqual(
      isSegmentInSafeBusinessRoamingRegion(
        CORRIDOR_WAYPOINTS['wp_carlos_counter']!.position,
        CORRIDOR_WAYPOINTS['wp_farmacia_front_threshold']!.position,
        FARMACIA_BENAVIDES_AUTONOMY_DOMAIN
      ),
      true
    );
    assert.strictEqual(
      isSegmentInSafeBusinessRoamingRegion(
        CORRIDOR_WAYPOINTS['wp_carlos_counter']!.position,
        { x: 7.9, y: 0.2, z: 0.0 }, // inside absolute domain but violates safe interior margin
        FARMACIA_BENAVIDES_AUTONOMY_DOMAIN
      ),
      false
    );
    const fallbackRes = evaluateBusinessLocalAutonomyMovement({
      profile: BUSINESS_LOCAL_AUTONOMY_PROFILES['carlos-farmacia-attendant']!,
      rawEvaluatedAtMs: Date.parse('2026-11-04T17:10:30.000Z'),
      civil: { timeZone: 'America/Ciudad_Juarez', year: 2026, month: 11, day: 4, hour: 10, minute: 10, second: 30, millisecond: 0, dayOfWeek: 3, utcOffsetMinutes: -420, isoCivilString: '2026-11-04T10:10:30-07:00' },
      activitySnapshot: null,
      waypointsRegistry: brokenRegistry,
    });
    assert.strictEqual(fallbackRes.movementState, 'stationary');
    assert.strictEqual(fallbackRes.currentWaypointId, 'wp_carlos_counter');
    assert.strictEqual(fallbackRes.actualSpeedMps, 0);

    // D. Valeria elevation & scheduled workshop calibration preservation
    assert.strictEqual(CORRIDOR_WAYPOINTS['wp_valeria_storefront']!.position.y, 0.2);
    const valeriaCalib = evaluateNpcMovement(
      NPC_MOVEMENT_PROFILES['valeria-optica-attendant']!,
      '2026-11-04T20:10:00.000Z' // Wed 13:10 MST inside valeria_calibration_walk (13:00-13:45)
    );
    assert.strictEqual(valeriaCalib.activityState, 'busy');
    assert.strictEqual(valeriaCalib.movementState, 'stationary');
    assert.strictEqual(valeriaCalib.autonomousBehaviorState, 'scheduled_station_task');
    assert.strictEqual(valeriaCalib.currentWaypointId, 'wp_valeria_storefront');
    assert.strictEqual(valeriaCalib.currentSpeedMps, 0);
    assert.ok(valeriaCalib.position.z >= -1.80 && valeriaCalib.position.z <= 3.40, 'Valeria calibration must not walk into Bar La Cucaracha Z=8.0');

    // E. Carlos scheduled lunch walk preserved
    const carlosLunch = evaluateNpcMovement(
      NPC_MOVEMENT_PROFILES['carlos-farmacia-attendant']!,
      '2026-11-04T21:00:05.000Z' // Wed 14:00:05 MST
    );
    assert.strictEqual(carlosLunch.movementState, 'moving');
    assert.strictEqual(carlosLunch.activeRouteId, 'route_carlos_to_lunch');
    assert.strictEqual(carlosLunch.destinationWaypointId, 'wp_east_dining_benches');
  });
});
