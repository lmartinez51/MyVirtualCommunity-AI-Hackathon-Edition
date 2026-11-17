import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  DEFAULT_PLAYER_ID,
  MAX_INTERACTION_DISTANCE_METERS,
  calculateDistance3D,
  evaluateAuthoritativeInteractionDecision,
} from '../../src/domain/npc-interaction-validation.js';
import { PlayerSpatialState } from '../../src/domain/player-movement.js';
import { evaluateNpcInteractionContext } from '../../src/domain/npc-interaction-context.js';
import { NPC_MOVEMENT_PROFILES, Vector3D, evaluateNpcMovement } from '../../src/domain/npc-waypoint-movement.js';

function makePlayerState(position: Vector3D, updatedAt: string): PlayerSpatialState {
  return {
    playerId: DEFAULT_PLAYER_ID,
    position,
    lastMovementServerTimeMs: 1_000_000,
    lastSequenceNumber: 1,
    updatedAt,
  };
}

describe('Server-Authoritative NPC Interaction Validation Domain Model', () => {
  // Wednesday 2026-11-04 10:00:00 CST (17:00:00 UTC) -> Elena & Carlos on normal duty
  const normalDutyUtc = '2026-11-04T17:00:00.000Z';
  // Wednesday 2026-11-04 14:00:15 CST (21:00:15 UTC) -> Carlos walking to lunch (in transit)
  const inTransitUtc = '2026-11-04T21:00:15.000Z';
  // Wednesday 2026-11-04 14:02:00 CST (21:02:00 UTC) -> Carlos at lunch benches (contextual)
  const contextualUtc = '2026-11-04T21:02:00.000Z';
  // Wednesday 2026-11-04 23:00:00 CST (2026-11-05T06:00:00.000Z) -> Off duty
  const offDutyUtc = '2026-11-05T06:00:00.000Z';
  // Sunday 2026-11-08 11:00:00 CST (18:00:00 UTC) -> Óptica Fashion closed (unavailable)
  const sundayClosedUtc = '2026-11-08T18:00:00.000Z';
  // Wednesday 2026-11-04 21:15:00 CST (2026-11-05T04:15:00.000Z) -> Carlos register closing (busy)
  const busyUtc = '2026-11-05T04:15:00.000Z';

  it('1. Rejects when player does not exist in World Core', () => {
    const npcId = 'elena-border-concierge';
    const ctx = evaluateNpcInteractionContext(npcId, normalDutyUtc);
    const mov = evaluateNpcMovement(NPC_MOVEMENT_PROFILES[npcId]!, normalDutyUtc);

    const decision = evaluateAuthoritativeInteractionDecision({
      player: null,
      playerId: 'unknown-player',
      npcId,
      intent: 'talk',
      requestId: 'req-1',
      evaluatedAt: normalDutyUtc,
      interactionContext: ctx,
      movementSnapshot: mov,
    });

    assert.strictEqual(decision.allowed, false);
    assert.strictEqual(decision.reasonCode, 'player_not_found');
    assert.strictEqual(decision.requestId, 'req-1');
  });

  it('2. Rejects when NPC does not exist in World Core', () => {
    const decision = evaluateAuthoritativeInteractionDecision({
      player: makePlayerState({ x: 19.2, y: 0.2, z: 39.5 }, normalDutyUtc),
      playerId: DEFAULT_PLAYER_ID,
      npcId: 'non-existent-npc',
      intent: 'talk',
      requestId: null,
      evaluatedAt: normalDutyUtc,
      interactionContext: null,
      movementSnapshot: null,
    });

    assert.strictEqual(decision.allowed, false);
    assert.strictEqual(decision.reasonCode, 'npc_not_found');
  });

  it('3. Enforces authoritative spatial proximity (accepts <= 5.0m, rejects > 5.0m)', () => {
    const npcId = 'elena-border-concierge';
    const ctx = evaluateNpcInteractionContext(npcId, normalDutyUtc);
    const mov = evaluateNpcMovement(NPC_MOVEMENT_PROFILES[npcId]!, normalDutyUtc);
    assert.ok(ctx && mov);

    // Exactly at 5.0m boundary
    const atBoundary = evaluateAuthoritativeInteractionDecision({
      player: makePlayerState(
        { x: mov.position.x + MAX_INTERACTION_DISTANCE_METERS, y: mov.position.y, z: mov.position.z },
        normalDutyUtc
      ),
      playerId: DEFAULT_PLAYER_ID,
      npcId,
      intent: 'talk',
      requestId: 'boundary-ok',
      evaluatedAt: normalDutyUtc,
      interactionContext: ctx,
      movementSnapshot: mov,
    });
    assert.strictEqual(atBoundary.allowed, true);
    assert.strictEqual(atBoundary.reasonCode, 'authorized_normal_service');
    assert.strictEqual(atBoundary.distanceMeters, 5);

    // Outside range (5.5m)
    const outOfRange = evaluateAuthoritativeInteractionDecision({
      player: makePlayerState(
        { x: mov.position.x + 5.5, y: mov.position.y, z: mov.position.z },
        normalDutyUtc
      ),
      playerId: DEFAULT_PLAYER_ID,
      npcId,
      intent: 'talk',
      requestId: 'too-far',
      evaluatedAt: normalDutyUtc,
      interactionContext: ctx,
      movementSnapshot: mov,
    });
    assert.strictEqual(outOfRange.allowed, false);
    assert.strictEqual(outOfRange.reasonCode, 'out_of_range');
    assert.ok(outOfRange.distanceMeters !== null && outOfRange.distanceMeters > 5.0);
    assert.ok(outOfRange.message.en.includes('Too far'));
    assert.ok(outOfRange.message.es.includes('demasiado lejos'));
  });

  it('4. Evaluates all 6 interactionAvailability states deterministically across talk and service intents', () => {
    // A. available_in_transit (Carlos walking to lunch)
    const carlosTransitCtx = evaluateNpcInteractionContext('carlos-farmacia-attendant', inTransitUtc);
    const carlosTransitMov = evaluateNpcMovement(NPC_MOVEMENT_PROFILES['carlos-farmacia-attendant']!, inTransitUtc);
    assert.ok(carlosTransitCtx && carlosTransitMov);
    assert.strictEqual(carlosTransitCtx.interactionAvailability, 'available_in_transit');

    const transitTalk = evaluateAuthoritativeInteractionDecision({
      player: makePlayerState(carlosTransitMov.position, inTransitUtc),
      playerId: DEFAULT_PLAYER_ID,
      npcId: 'carlos-farmacia-attendant',
      intent: 'talk',
      requestId: null,
      evaluatedAt: inTransitUtc,
      interactionContext: carlosTransitCtx,
      movementSnapshot: carlosTransitMov,
    });
    assert.strictEqual(transitTalk.allowed, true);
    assert.strictEqual(transitTalk.reasonCode, 'authorized_in_transit');

    const transitService = evaluateAuthoritativeInteractionDecision({
      player: makePlayerState(carlosTransitMov.position, inTransitUtc),
      playerId: DEFAULT_PLAYER_ID,
      npcId: 'carlos-farmacia-attendant',
      intent: 'service',
      requestId: null,
      evaluatedAt: inTransitUtc,
      interactionContext: carlosTransitCtx,
      movementSnapshot: carlosTransitMov,
    });
    assert.strictEqual(transitService.allowed, false);
    assert.strictEqual(transitService.reasonCode, 'npc_in_transit_service_paused');

    // B. available_contextual (Carlos at lunch benches)
    const carlosCtx = evaluateNpcInteractionContext('carlos-farmacia-attendant', contextualUtc);
    const carlosMov = evaluateNpcMovement(NPC_MOVEMENT_PROFILES['carlos-farmacia-attendant']!, contextualUtc);
    assert.ok(carlosCtx && carlosMov);
    assert.strictEqual(carlosCtx.interactionAvailability, 'available_contextual');

    const contextualTalk = evaluateAuthoritativeInteractionDecision({
      player: makePlayerState(carlosMov.position, contextualUtc),
      playerId: DEFAULT_PLAYER_ID,
      npcId: 'carlos-farmacia-attendant',
      intent: 'talk',
      requestId: null,
      evaluatedAt: contextualUtc,
      interactionContext: carlosCtx,
      movementSnapshot: carlosMov,
    });
    assert.strictEqual(contextualTalk.allowed, true);
    assert.strictEqual(contextualTalk.reasonCode, 'authorized_contextual');

    // C. busy (Carlos inventory audit)
    const carlosBusyCtx = evaluateNpcInteractionContext('carlos-farmacia-attendant', busyUtc);
    const carlosBusyMov = evaluateNpcMovement(NPC_MOVEMENT_PROFILES['carlos-farmacia-attendant']!, busyUtc);
    assert.ok(carlosBusyCtx && carlosBusyMov);
    assert.strictEqual(carlosBusyCtx.interactionAvailability, 'busy');

    const busyTalk = evaluateAuthoritativeInteractionDecision({
      player: makePlayerState(carlosBusyMov.position, busyUtc),
      playerId: DEFAULT_PLAYER_ID,
      npcId: 'carlos-farmacia-attendant',
      intent: 'talk',
      requestId: null,
      evaluatedAt: busyUtc,
      interactionContext: carlosBusyCtx,
      movementSnapshot: carlosBusyMov,
    });
    assert.strictEqual(busyTalk.allowed, true);
    assert.strictEqual(busyTalk.reasonCode, 'authorized_while_busy');

    const busyService = evaluateAuthoritativeInteractionDecision({
      player: makePlayerState(carlosBusyMov.position, busyUtc),
      playerId: DEFAULT_PLAYER_ID,
      npcId: 'carlos-farmacia-attendant',
      intent: 'service',
      requestId: null,
      evaluatedAt: busyUtc,
      interactionContext: carlosBusyCtx,
      movementSnapshot: carlosBusyMov,
    });
    assert.strictEqual(busyService.allowed, false);
    assert.strictEqual(busyService.reasonCode, 'npc_busy_service_deferred');

    // D. off_duty
    const offDutyCtx = evaluateNpcInteractionContext('carlos-farmacia-attendant', offDutyUtc);
    const offDutyMov = evaluateNpcMovement(NPC_MOVEMENT_PROFILES['carlos-farmacia-attendant']!, offDutyUtc);
    assert.ok(offDutyCtx && offDutyMov);
    assert.strictEqual(offDutyCtx.interactionAvailability, 'off_duty');

    const offDutyDecision = evaluateAuthoritativeInteractionDecision({
      player: makePlayerState(offDutyMov.position, offDutyUtc),
      playerId: DEFAULT_PLAYER_ID,
      npcId: 'carlos-farmacia-attendant',
      intent: 'talk',
      requestId: null,
      evaluatedAt: offDutyUtc,
      interactionContext: offDutyCtx,
      movementSnapshot: offDutyMov,
    });
    assert.strictEqual(offDutyDecision.allowed, false);
    assert.strictEqual(offDutyDecision.reasonCode, 'npc_off_duty');

    // E. unavailable (when facility/business is explicitly marked unavailable/closed)
    const valeriaCtx = evaluateNpcInteractionContext('valeria-optica-attendant', sundayClosedUtc);
    const valeriaMov = evaluateNpcMovement(NPC_MOVEMENT_PROFILES['valeria-optica-attendant']!, sundayClosedUtc);
    assert.ok(valeriaCtx && valeriaMov);
    assert.strictEqual(valeriaCtx.interactionAvailability, 'off_duty');

    const unavailableDecision = evaluateAuthoritativeInteractionDecision({
      player: makePlayerState(valeriaMov.position, sundayClosedUtc),
      playerId: DEFAULT_PLAYER_ID,
      npcId: 'valeria-optica-attendant',
      intent: 'talk',
      requestId: null,
      evaluatedAt: sundayClosedUtc,
      interactionContext: { ...valeriaCtx, interactionAvailability: 'unavailable', interactionMode: 'closed' },
      movementSnapshot: valeriaMov,
    });
    assert.strictEqual(unavailableDecision.allowed, false);
    assert.strictEqual(unavailableDecision.reasonCode, 'npc_unavailable');
  });

  it('5. calculateDistance3D computes exact Euclidean distance', () => {
    const dist = calculateDistance3D({ x: 0, y: 0, z: 0 }, { x: 3, y: 4, z: 0 });
    assert.strictEqual(dist, 5);
  });
});
