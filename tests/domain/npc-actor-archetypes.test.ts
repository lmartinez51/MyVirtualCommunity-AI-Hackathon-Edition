import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  ACTOR_ARCHETYPE_LABELS,
  BUSINESS_ATTENDANT_IDS,
  COMMUNITY_PLAYER_PROXY_IDS,
  NPC_ACTOR_CLASSIFICATION_ROSTER,
  PUBLIC_SERVANT_IDS,
  WITNESS_NPC_IDS,
  canActorArchetypeEnterOffDuty,
  getNpcActorClassification,
  isCommunityPlayerProxy,
  isNpcWitness,
} from '../../src/domain/npc-actor-archetypes.js';
import {
  NPC_SCHEDULE_ROSTER,
  NpcScheduleProfile,
  evaluateNpcActivity,
  evaluateRosterActivity,
} from '../../src/domain/npc-activity.js';
import {
  CANONICAL_NPC_IDS,
  evaluateNpcInteractionContext,
  evaluateRosterInteractionContext,
} from '../../src/domain/npc-interaction-context.js';
import { evaluateRosterSocialAwareness } from '../../src/domain/npc-social-awareness.js';
import { evaluateAuthoritativeInteractionDecision } from '../../src/domain/npc-interaction-validation.js';
import { NPC_MOVEMENT_PROFILES, evaluateRosterMovement } from '../../src/domain/npc-waypoint-movement.js';
import { SYNTHETIC_WITNESS_ROSTER } from '../../src/domain/synthetic-witness.js';
import { parseLocalCivilTimeToUtc } from '../../src/domain/civil-time.js';
import { PlayerSpatialState } from '../../src/domain/player-movement.js';

const TZ_JUAREZ = 'America/Ciudad_Juarez';

const SWEEP_LOCAL_TIMES = [
  '00:00:00',
  '05:30:00',
  '07:00:00',
  '09:00:00',
  '12:00:00',
  '14:00:00',
  '17:30:00',
  '19:00:00',
  '21:00:00',
  '23:59:00',
] as const;

const SWEEP_DATES = [
  '2026-11-04', // Wednesday (Standard MST)
  '2026-11-07', // Saturday (Weekend)
  '2026-11-08', // Sunday (Weekend closed businesses)
  '2026-12-31', // Thursday New Year's Eve (month/year boundary)
  '2027-01-01', // Friday New Year's Day (month/year boundary)
  '2026-03-08', // Sunday DST spring-forward in America/Ciudad_Juarez (02:00 -> 03:00)
  '2026-11-01', // Sunday DST fall-back in America/Ciudad_Juarez (02:00 -> 01:00)
] as const;

describe('Increment 5.20.0.2: NPC Actor Archetypes, Community Player Proxies & Witness Classification', () => {
  it('8A.1 Exact classification of all 7 canonical NPCs across the 3 archetypes and witness registry', () => {
    assert.strictEqual(Object.keys(NPC_ACTOR_CLASSIFICATION_ROSTER).length, 7);
    assert.strictEqual(CANONICAL_NPC_IDS.length, 7);

    assert.deepStrictEqual([...COMMUNITY_PLAYER_PROXY_IDS].sort(), [
      'hector-artisan',
      'mateo-student',
      'sofia-cross-border-retail',
    ]);
    assert.deepStrictEqual([...BUSINESS_ATTENDANT_IDS].sort(), [
      'carlos-farmacia-attendant',
      'valeria-optica-attendant',
    ]);
    assert.deepStrictEqual([...PUBLIC_SERVANT_IDS].sort(), [
      'elena-border-concierge',
      'patricia-citizen-rep',
    ]);
    assert.deepStrictEqual([...WITNESS_NPC_IDS].sort(), [
      'hector-artisan',
      'mateo-student',
      'sofia-cross-border-retail',
    ]);

    for (const id of COMMUNITY_PLAYER_PROXY_IDS) {
      const c = getNpcActorClassification(id)!;
      assert.ok(c);
      assert.strictEqual(c.actorArchetype, 'community_player_proxy');
      assert.strictEqual(c.hasEmploymentSchedule, false);
      assert.strictEqual(c.canEnterOffDuty, false);
      assert.strictEqual(c.isWitness, true);
      assert.strictEqual(c.witnessClassification, 'community_queue_witness');
      assert.strictEqual(c.witnessId, id);
      assert.ok(id in SYNTHETIC_WITNESS_ROSTER);
      assert.strictEqual(isCommunityPlayerProxy(id), true);
      assert.strictEqual(isNpcWitness(id), true);
    }

    for (const id of BUSINESS_ATTENDANT_IDS) {
      const c = getNpcActorClassification(id)!;
      assert.ok(c);
      assert.strictEqual(c.actorArchetype, 'business_attendant');
      assert.strictEqual(c.hasEmploymentSchedule, true);
      assert.strictEqual(c.canEnterOffDuty, true);
      assert.strictEqual(c.isWitness, false);
      assert.strictEqual(c.witnessClassification, 'none');
      assert.strictEqual(c.witnessId, null);
      assert.strictEqual(isCommunityPlayerProxy(id), false);
      assert.strictEqual(isNpcWitness(id), false);
    }

    for (const id of PUBLIC_SERVANT_IDS) {
      const c = getNpcActorClassification(id)!;
      assert.ok(c);
      assert.strictEqual(c.actorArchetype, 'public_servant');
      assert.strictEqual(c.hasEmploymentSchedule, true);
      assert.strictEqual(c.canEnterOffDuty, true);
      assert.strictEqual(c.isWitness, false);
      assert.strictEqual(c.witnessClassification, 'none');
      assert.strictEqual(c.witnessId, null);
      assert.strictEqual(isCommunityPlayerProxy(id), false);
      assert.strictEqual(isNpcWitness(id), false);
    }

    assert.strictEqual(canActorArchetypeEnterOffDuty('community_player_proxy'), false);
    assert.strictEqual(canActorArchetypeEnterOffDuty('business_attendant'), true);
    assert.strictEqual(canActorArchetypeEnterOffDuty('public_servant'), true);
    assert.ok(ACTOR_ARCHETYPE_LABELS.community_player_proxy.en.length > 0);
    assert.ok(ACTOR_ARCHETYPE_LABELS.community_player_proxy.es.length > 0);
  });

  it('8A.2 Witness designation is orthogonal to operational state, movement, and never fabricates incident testimony', () => {
    // Compare contexts during weekday busy/contextual time and weekend time
    const weekdayInstant = '2026-11-04T16:45:00.000Z'; // Wed 09:45 MST (Mateo reviewing exam notes -> busy)
    const sundayInstant = '2026-11-08T19:00:00.000Z';  // Sun 12:00 MST

    for (const proxyId of COMMUNITY_PLAYER_PROXY_IDS) {
      const ctxWeek = evaluateNpcInteractionContext(proxyId, weekdayInstant)!;
      const ctxSun = evaluateNpcInteractionContext(proxyId, sundayInstant)!;
      assert.strictEqual(ctxWeek.actorArchetype, 'community_player_proxy');
      assert.strictEqual(ctxSun.actorArchetype, 'community_player_proxy');
      assert.strictEqual(ctxWeek.isWitness, true);
      assert.strictEqual(ctxSun.isWitness, true);
      assert.strictEqual(ctxWeek.witnessId, proxyId);
      assert.strictEqual(ctxSun.witnessId, proxyId);
      // Verify no fabricated incident evidence fields exist on interaction context
      assert.strictEqual('incidentEvidence' in ctxWeek, false);
      assert.strictEqual('testimonyStatement' in ctxWeek, false);
    }
  });

  it('8B. Never-Off-Duty Time-Sweep across all 10 daily times, weekends, month/year boundaries, and Juárez DST transitions', () => {
    let evaluatedPoints = 0;

    for (const dateStr of SWEEP_DATES) {
      for (const timeStr of SWEEP_LOCAL_TIMES) {
        const parsed = parseLocalCivilTimeToUtc(`${dateStr}T${timeStr}`, TZ_JUAREZ);
        assert.ok(parsed.utcInstant, `Failed to parse ${dateStr}T${timeStr} in ${TZ_JUAREZ}`);
        const instantIso = parsed.utcInstant.toISOString();
        evaluatedPoints++;

        const activityRoster = evaluateRosterActivity(NPC_SCHEDULE_ROSTER, instantIso);
        const contextRoster = evaluateRosterInteractionContext(instantIso);
        const movementRoster = evaluateRosterMovement(NPC_MOVEMENT_PROFILES, instantIso);
        const awarenessRoster = evaluateRosterSocialAwareness({
          evaluatedAt: instantIso,
          player: null,
          interactionRoster: contextRoster,
          movementRoster,
        });

        for (const proxyId of COMMUNITY_PLAYER_PROXY_IDS) {
          const act = activityRoster.npcs[proxyId]!;
          const ctx = contextRoster.npcs[proxyId]!;
          const mov = movementRoster.npcs[proxyId]!;
          const awr = awarenessRoster.npcs[proxyId]!;

          assert.strictEqual(act.actorArchetype, 'community_player_proxy');
          assert.strictEqual(act.hasEmploymentSchedule, false);
          assert.strictEqual(act.canEnterOffDuty, false);
          assert.strictEqual(act.isWitness, true);
          assert.notStrictEqual(act.state, 'off_duty', `${proxyId} entered off_duty at ${dateStr} ${timeStr}`);
          assert.notStrictEqual(act.state, 'unavailable', `${proxyId} entered unavailable at ${dateStr} ${timeStr}`);
          assert.strictEqual(act.canInteract, true);

          assert.strictEqual(ctx.actorArchetype, 'community_player_proxy');
          assert.strictEqual(ctx.canEnterOffDuty, false);
          assert.strictEqual(ctx.isWitness, true);
          assert.notStrictEqual(ctx.activityState, 'off_duty');
          assert.notStrictEqual(ctx.activityState, 'unavailable');
          assert.notStrictEqual(ctx.interactionAvailability, 'off_duty');
          assert.notStrictEqual(ctx.interactionAvailability, 'unavailable');
          assert.notStrictEqual(ctx.interactionMode, 'off_duty_courtesy');
          assert.notStrictEqual(ctx.interactionMode, 'closed');
          assert.strictEqual(ctx.canInitiateConversation, true);

          assert.strictEqual(mov.actorArchetype, 'community_player_proxy');
          assert.strictEqual(mov.isWitness, true);
          assert.notStrictEqual(mov.activityState, 'off_duty');

          assert.strictEqual(awr.actorArchetype, 'community_player_proxy');
          assert.strictEqual(awr.isWitness, true);
          assert.strictEqual(awr.canOrientTowardTarget, true);
        }
      }
    }

    assert.strictEqual(evaluatedPoints, SWEEP_DATES.length * SWEEP_LOCAL_TIMES.length);
  });

  it('8B.2 Domain root guard refuses off_duty even for a malformed community_player_proxy profile', () => {
    const legacyPoisonedProfile: NpcScheduleProfile = {
      ...NPC_SCHEDULE_ROSTER['mateo-student']!,
      actorArchetype: 'community_player_proxy',
      dailySchedules: {
        3: {
          dayOfWeek: 3,
          defaultState: 'off_duty',
          defaultActivityTitle: { en: 'Forbidden off duty', es: 'Prohibido' },
          defaultActivityDescription: { en: 'Should never surface', es: 'Nunca debe salir' },
          windows: [
            {
              startHour: 8,
              startMinute: 0,
              endHour: 10,
              endMinute: 0,
              state: 'off_duty',
              activityTitle: { en: 'Poisoned window', es: 'Ventana envenenada' },
              activityDescription: { en: 'Must be coerced away from off_duty', es: 'Debe ser neutralizada' },
            },
          ],
        },
      },
    };

    const snapInWindow = evaluateNpcActivity(legacyPoisonedProfile, '2026-11-04T16:00:00.000Z'); // Wed 09:00 MST
    const snapOutside = evaluateNpcActivity(legacyPoisonedProfile, '2026-11-05T06:00:00.000Z'); // Wed 23:00 MST
    const snapMissingDay = evaluateNpcActivity(legacyPoisonedProfile, '2026-11-08T19:00:00.000Z'); // Sun 12:00 MST

    assert.strictEqual(snapInWindow.state, 'available');
    assert.strictEqual(snapOutside.state, 'available');
    assert.strictEqual(snapMissingDay.state, 'available');
  });

  it('8C. Preserves authentic off_duty and weekend closure behavior for Business Attendants and Public Servants', () => {
    // Late night Wednesday 23:59 MST -> Next day 06:59 UTC
    const lateNight = '2026-11-05T06:59:00.000Z';
    // Sunday midday 12:00 MST -> 19:00 UTC
    const sundayNoon = '2026-11-08T19:00:00.000Z';
    // Saturday midday 12:00 MST -> 19:00 UTC
    const saturdayNoon = '2026-11-07T19:00:00.000Z';

    for (const id of [...BUSINESS_ATTENDANT_IDS, ...PUBLIC_SERVANT_IDS]) {
      const ctxNight = evaluateNpcInteractionContext(id, lateNight)!;
      assert.strictEqual(ctxNight.activityState, 'off_duty', `${id} must be off_duty at 23:59`);
      assert.strictEqual(ctxNight.interactionAvailability, 'off_duty');
      assert.strictEqual(ctxNight.interactionMode, 'off_duty_courtesy');
      assert.strictEqual(ctxNight.hasEmploymentSchedule, true);
      assert.strictEqual(ctxNight.canEnterOffDuty, true);
      assert.strictEqual(ctxNight.isWitness, false);
    }

    // Closed storefront attendants & municipal clerk on Sunday
    for (const id of ['carlos-farmacia-attendant', 'valeria-optica-attendant', 'patricia-citizen-rep']) {
      const ctxSun = evaluateNpcInteractionContext(id, sundayNoon)!;
      assert.strictEqual(ctxSun.interactionAvailability, 'off_duty', `${id} must be off_duty on Sunday`);
    }

    // Patricia closed on Saturday; Carlos & Valeria open Saturday
    assert.strictEqual(
      evaluateNpcInteractionContext('patricia-citizen-rep', saturdayNoon)!.interactionAvailability,
      'off_duty'
    );
    assert.strictEqual(
      evaluateNpcInteractionContext('carlos-farmacia-attendant', saturdayNoon)!.interactionAvailability,
      'available_normal'
    );
    assert.strictEqual(
      evaluateNpcInteractionContext('valeria-optica-attendant', saturdayNoon)!.interactionAvailability,
      'available_normal'
    );

    // Elena is public_servant operating kiosk 06:00-22:00 daily, including Sunday
    assert.strictEqual(
      evaluateNpcInteractionContext('elena-border-concierge', sundayNoon)!.interactionAvailability,
      'available_normal'
    );
  });

  it('8D. Preserves queue social engagement, player awareness, interaction validation & deterministic reconstruction at off-hours', () => {
    // Late night 23:59 MST (2026-11-05T06:59:00.000Z): all 4 staff off_duty; 3 queue proxies present & active
    const lateNight = '2026-11-05T06:59:00.000Z';
    const nearbyMateoPlayer: PlayerSpatialState = {
      playerId: 'player-1',
      position: { x: 15.2, y: 0.328, z: 41.5 }, // 0.5m from Mateo (z=42.0), >4m from Sofia (z=45.5)
      lastMovementServerTimeMs: 1000,
      lastSequenceNumber: 5,
      updatedAt: lateNight,
    };

    const awarenessLate = evaluateRosterSocialAwareness({
      evaluatedAt: lateNight,
      player: nearbyMateoPlayer,
    });

    // Carlos off_duty -> unaware & cannot orient
    assert.strictEqual(awarenessLate.npcs['carlos-farmacia-attendant']!.awarenessState, 'unaware');
    assert.strictEqual(awarenessLate.npcs['carlos-farmacia-attendant']!.canOrientTowardTarget, false);
    // Sofia <-> Hector (3.5m apart in queue) are mutually social_engaged even at 23:59!
    assert.strictEqual(awarenessLate.npcs['sofia-cross-border-retail']!.awarenessState, 'social_engaged');
    assert.strictEqual(awarenessLate.npcs['sofia-cross-border-retail']!.pairedPartnerNpcId, 'hector-artisan');
    assert.strictEqual(awarenessLate.npcs['hector-artisan']!.awarenessState, 'social_engaged');
    assert.strictEqual(awarenessLate.npcs['hector-artisan']!.pairedPartnerNpcId, 'sofia-cross-border-retail');
    // Mateo notices close player at 0.5m even at 23:59!
    assert.strictEqual(awarenessLate.npcs['mateo-student']!.awarenessState, 'player_attentive');
    assert.strictEqual(awarenessLate.npcs['mateo-student']!.targetEntityType, 'player');

    // Authoritative interaction validation: talking to Mateo at 23:59 succeeds; talking to Carlos at 23:59 is rejected (off_duty)
    const movementLate = evaluateRosterMovement(NPC_MOVEMENT_PROFILES, lateNight);
    const contextLate = evaluateRosterInteractionContext(lateNight);

    const mateoDecision = evaluateAuthoritativeInteractionDecision({
      player: nearbyMateoPlayer,
      playerId: 'player-1',
      npcId: 'mateo-student',
      intent: 'talk',
      requestId: 'req-late-mateo',
      evaluatedAt: lateNight,
      interactionContext: contextLate.npcs['mateo-student']!,
      movementSnapshot: movementLate.npcs['mateo-student']!,
    });
    assert.strictEqual(mateoDecision.allowed, true);
    assert.strictEqual(mateoDecision.reasonCode, 'authorized_normal_service');
    assert.strictEqual(mateoDecision.actorArchetype, 'community_player_proxy');
    assert.strictEqual(mateoDecision.isWitness, true);
    assert.strictEqual(mateoDecision.witnessId, 'mateo-student');

    const carlosPos = movementLate.npcs['carlos-farmacia-attendant']!.position;
    const carlosDecision = evaluateAuthoritativeInteractionDecision({
      player: { ...nearbyMateoPlayer, position: { ...carlosPos, z: carlosPos.z + 1.0 } },
      playerId: 'player-1',
      npcId: 'carlos-farmacia-attendant',
      intent: 'talk',
      requestId: 'req-late-carlos',
      evaluatedAt: lateNight,
      interactionContext: contextLate.npcs['carlos-farmacia-attendant']!,
      movementSnapshot: movementLate.npcs['carlos-farmacia-attendant']!,
    });
    assert.strictEqual(carlosDecision.allowed, false);
    assert.strictEqual(carlosDecision.reasonCode, 'npc_off_duty');
    assert.strictEqual(carlosDecision.actorArchetype, 'business_attendant');
    assert.strictEqual(carlosDecision.isWitness, false);

    // Offline reconstruction determinism across far past & future
    const farFuture = '2028-07-16T05:30:00.000Z';
    const ctx1 = evaluateNpcInteractionContext('hector-artisan', farFuture);
    const ctx2 = evaluateNpcInteractionContext('hector-artisan', farFuture);
    assert.deepStrictEqual(ctx1, ctx2);
    assert.strictEqual(ctx1!.actorArchetype, 'community_player_proxy');
    assert.notStrictEqual(ctx1!.interactionAvailability, 'off_duty');
  });
});
