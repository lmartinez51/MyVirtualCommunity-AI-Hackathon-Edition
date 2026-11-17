import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  CORRIDOR_POSITION_BOUNDS,
  DEFAULT_PLAYER_ID,
  DEFAULT_PLAYER_SPAWN_POSITION,
  InvalidPlayerMovementInputError,
  MAX_MOVEMENT_INTEGRATION_SECONDS,
  PLAYER_SPRINT_SPEED_MPS,
  PLAYER_WALK_SPEED_MPS,
  UnauthorizedPlayerIdentityError,
  applyAuthoritativePlayerMovement,
  createInitialPlayerState,
} from '../../src/domain/player-movement.js';
import { InMemoryWorldCoreAdapter } from '../../src/adapters/in-memory-world-core-adapter.js';

describe('Authoritative Player Movement Domain Model (Increment 5.18.0.1)', () => {
  it('1. Initializes canonical spawn from trusted World Core definition', () => {
    const state = createInitialPlayerState(DEFAULT_PLAYER_ID, 1_000_000);
    assert.strictEqual(state.playerId, 'player-1');
    assert.deepStrictEqual(state.position, DEFAULT_PLAYER_SPAWN_POSITION);
    assert.strictEqual(state.lastMovementServerTimeMs, 1_000_000);
    assert.strictEqual(state.lastSequenceNumber, 0);
  });

  it('2. Produces expected authoritative displacement for walk and sprint using server elapsed time', () => {
    const initial = createInitialPlayerState(DEFAULT_PLAYER_ID, 10_000);

    // 200ms walking north (+Z) -> 0.2 * 4.5 = +0.9m
    const walkStep = applyAuthoritativePlayerMovement(
      initial,
      { direction: { x: 0, z: 1 }, movementMode: 'walk', sequenceNumber: 1 },
      10_200
    );
    assert.strictEqual(walkStep.accepted, true);
    assert.strictEqual(walkStep.appliedSpeedMps, PLAYER_WALK_SPEED_MPS);
    assert.strictEqual(walkStep.elapsedSeconds, 0.2);
    assert.strictEqual(walkStep.player.position.x, initial.position.x);
    assert.strictEqual(walkStep.player.position.z, Number((initial.position.z + 0.9).toFixed(4)));

    // 200ms sprinting east (+X) -> 0.2 * 7.0 = +1.4m
    const sprintStep = applyAuthoritativePlayerMovement(
      walkStep.player,
      { direction: { x: 1, z: 0 }, movementMode: 'sprint', sequenceNumber: 2 },
      10_400
    );
    assert.strictEqual(sprintStep.accepted, true);
    assert.strictEqual(sprintStep.appliedSpeedMps, PLAYER_SPRINT_SPEED_MPS);
    assert.strictEqual(sprintStep.player.position.x, Number((walkStep.player.position.x + 1.4).toFixed(4)));
  });

  it('3. Normalizes diagonal and oversized input vectors so no speed advantage occurs', () => {
    const initial = createInitialPlayerState(DEFAULT_PLAYER_ID, 10_000);

    const diagStep = applyAuthoritativePlayerMovement(
      initial,
      { direction: { x: 1, z: 1 }, movementMode: 'walk', sequenceNumber: 1 },
      10_400
    );
    const expectedMag = 1 / Math.SQRT2;
    assert.ok(Math.abs(diagStep.appliedDirection.x - expectedMag) < 1e-5);
    assert.ok(Math.abs(diagStep.appliedDirection.z - expectedMag) < 1e-5);
    assert.ok(Math.abs(diagStep.displacementMeters - PLAYER_WALK_SPEED_MPS * 0.4) < 0.01);

    // Oversized vector { x: 100, z: 0 } is clamped/normalized to unit speed
    const spoofStep = applyAuthoritativePlayerMovement(
      initial,
      { direction: { x: 100, z: 0 }, movementMode: 'walk', sequenceNumber: 1 },
      10_400
    );
    assert.strictEqual(spoofStep.appliedDirection.x, 1);
    assert.strictEqual(spoofStep.appliedDirection.z, 0);
    assert.ok(Math.abs(spoofStep.displacementMeters - PLAYER_WALK_SPEED_MPS * 0.4) < 0.01);
  });

  it('4. Rejects non-finite and malformed direction, mode, and sequence inputs', () => {
    const initial = createInitialPlayerState(DEFAULT_PLAYER_ID, 10_000);

    assert.throws(
      () => applyAuthoritativePlayerMovement(initial, { direction: { x: Number.NaN, z: 0 } }, 10_100),
      InvalidPlayerMovementInputError
    );
    assert.throws(
      () => applyAuthoritativePlayerMovement(initial, { direction: { x: 0, z: Number.POSITIVE_INFINITY } }, 10_100),
      InvalidPlayerMovementInputError
    );
    assert.throws(
      () => applyAuthoritativePlayerMovement(initial, { direction: null as any }, 10_100),
      InvalidPlayerMovementInputError
    );
    assert.throws(
      () =>
        applyAuthoritativePlayerMovement(
          initial,
          { direction: { x: 0, z: 1 }, movementMode: 'fly' as any },
          10_100
        ),
      InvalidPlayerMovementInputError
    );
    assert.throws(
      () =>
        applyAuthoritativePlayerMovement(
          initial,
          { direction: { x: 0, z: 1 }, sequenceNumber: -3 },
          10_100
        ),
      InvalidPlayerMovementInputError
    );
  });

  it('5. Enforces MAX_MOVEMENT_INTEGRATION_SECONDS and handles clock regression safely', () => {
    const initial = createInitialPlayerState(DEFAULT_PLAYER_ID, 10_000);

    // 60 seconds gap must cap at MAX_MOVEMENT_INTEGRATION_SECONDS (0.5s)
    const delayed = applyAuthoritativePlayerMovement(
      initial,
      { direction: { x: 0, z: 1 }, movementMode: 'sprint', sequenceNumber: 1 },
      70_000
    );
    assert.strictEqual(delayed.elapsedSeconds, MAX_MOVEMENT_INTEGRATION_SECONDS);
    assert.strictEqual(
      delayed.displacementMeters,
      Number((PLAYER_SPRINT_SPEED_MPS * MAX_MOVEMENT_INTEGRATION_SECONDS).toFixed(4))
    );

    // Clock regression (< lastMovementServerTimeMs) produces 0 displacement
    const backwardTime = applyAuthoritativePlayerMovement(
      delayed.player,
      { direction: { x: 0, z: 1 }, movementMode: 'sprint', sequenceNumber: 2 },
      65_000
    );
    assert.strictEqual(backwardTime.elapsedSeconds, 0);
    assert.deepStrictEqual(backwardTime.player.position, delayed.player.position);
  });

  it('6. Enforces corridor spatial bounds and deterministic repeatability', () => {
    const edgeState = {
      playerId: DEFAULT_PLAYER_ID,
      position: { x: CORRIDOR_POSITION_BOUNDS.maxX - 0.1, y: 1.0, z: 0 },
      lastMovementServerTimeMs: 10_000,
      lastSequenceNumber: 5,
      updatedAt: new Date(10_000).toISOString(),
    };

    const runA = applyAuthoritativePlayerMovement(
      edgeState,
      { direction: { x: 1, z: 0 }, movementMode: 'sprint', sequenceNumber: 6 },
      10_400
    );
    const runB = applyAuthoritativePlayerMovement(
      edgeState,
      { direction: { x: 1, z: 0 }, movementMode: 'sprint', sequenceNumber: 6 },
      10_400
    );

    assert.strictEqual(runA.player.position.x, CORRIDOR_POSITION_BOUNDS.maxX);
    assert.deepStrictEqual(runA, runB);
  });

  it('7. Ignores out-of-order / duplicate sequence numbers and rejects missing players in adapter', async () => {
    const initial = createInitialPlayerState(DEFAULT_PLAYER_ID, 10_000);
    const step1 = applyAuthoritativePlayerMovement(
      initial,
      { direction: { x: 0, z: 1 }, movementMode: 'walk', sequenceNumber: 5 },
      10_200
    );
    assert.strictEqual(step1.accepted, true);

    const stale = applyAuthoritativePlayerMovement(
      step1.player,
      { direction: { x: 0, z: -1 }, movementMode: 'sprint', sequenceNumber: 4 },
      10_400
    );
    assert.strictEqual(stale.accepted, false);
    assert.strictEqual(stale.outOfOrder, true);
    assert.strictEqual(stale.reasonCode, 'stale_sequence_ignored');
    assert.deepStrictEqual(stale.player, step1.player);

    const adapter = new InMemoryWorldCoreAdapter();
    assert.strictEqual(await adapter.getPlayerState('missing-player'), null);
    await assert.rejects(
      () => adapter.applyPlayerMovement('missing-player', { direction: { x: 0, z: 1 } }, 10_500),
      UnauthorizedPlayerIdentityError
    );
  });
});
