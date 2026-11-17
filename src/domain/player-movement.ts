/**
 * Authoritative Player Movement Domain Model (Increment 5.18.0.1).
 *
 * Core Architectural Invariants:
 * 1. World Core calculates and owns the player's canonical corridor position.
 * 2. Clients (Unity or HTTP callers) submit normalized planar movement intents ONLY;
 *    never authoritative coordinates, speed, deltaTime, or timestamps.
 * 3. Server real-time clock measures elapsed movement intervals (clamped to
 *    MAX_MOVEMENT_INTEGRATION_SECONDS) independently of the simulated World Clock
 *    used by NPC schedules and time-of-day lighting.
 * 4. Architectural Limitation (documented):
 *    The current corridor domain model defines outer corridor boundaries
 *    (CORRIDOR_POSITION_BOUNDS) and discrete NPC waypoints, but does not yet
 *    include static building/curb collider meshes inside World Core.
 *    World Core therefore enforces corridor boundaries, normalized speed limits,
 *    and monotonic sequence/time bounds without speculative collision geometry.
 */

import { BilingualText } from './synthetic-witness.js';
import { Vector3D } from './npc-waypoint-movement.js';

export type PlayerMovementMode = 'walk' | 'sprint';

export const VALID_PLAYER_MOVEMENT_MODES: readonly PlayerMovementMode[] = Object.freeze([
  'walk',
  'sprint',
]);

export const DEFAULT_PLAYER_ID = 'player-1';

/**
 * Matches FirstPersonPlayer spawn Transform in BorderCorridorScene.unity & CorridorSceneSetup.cs.
 */
export const DEFAULT_PLAYER_SPAWN_POSITION: Vector3D = Object.freeze({
  x: 0.0,
  y: 1.0,
  z: -15.0,
});

/**
 * Matches FirstPersonPlayer.cs serialized walkSpeed (4.5 m/s) and sprintSpeed (7.0 m/s).
 */
export const PLAYER_WALK_SPEED_MPS = 4.5;
export const PLAYER_SPRINT_SPEED_MPS = 7.0;

/**
 * Maximum elapsed integration interval per movement request (seconds).
 * Prevents delayed or stalled network requests from causing large teleports.
 */
export const MAX_MOVEMENT_INTEGRATION_SECONDS = 0.5;

export const CORRIDOR_POSITION_BOUNDS = Object.freeze({
  minX: -250,
  maxX: 250,
  minY: -20,
  maxY: 80,
  minZ: -250,
  maxZ: 500,
});

export interface PlayerSpatialState {
  readonly playerId: string;
  readonly position: Vector3D;
  readonly lastMovementServerTimeMs: number;
  readonly lastSequenceNumber: number;
  readonly updatedAt: string;
}

export interface PlayerPlanarDirection {
  readonly x: number;
  readonly z: number;
}

export interface PlayerMovementIntent {
  readonly direction: PlayerPlanarDirection;
  readonly movementMode?: PlayerMovementMode | string;
  readonly sequenceNumber?: number;
}

export interface PlayerMovementStepResult {
  readonly accepted: boolean;
  readonly outOfOrder: boolean;
  readonly reasonCode: 'movement_applied' | 'stale_sequence_ignored';
  readonly message: BilingualText;
  readonly player: PlayerSpatialState;
  readonly appliedDirection: PlayerPlanarDirection;
  readonly appliedSpeedMps: number;
  readonly elapsedSeconds: number;
  readonly displacementMeters: number;
}

export class InvalidPlayerMovementInputError extends Error {
  constructor(
    public readonly code: string,
    message: string
  ) {
    super(message);
    this.name = 'InvalidPlayerMovementInputError';
  }
}

export class UnauthorizedPlayerIdentityError extends Error {
  public readonly code = 'unauthorized-player-identity';
  constructor(playerId: string) {
    super(`Player "${playerId}" is not authorized or does not exist in authoritative World Core.`);
    this.name = 'UnauthorizedPlayerIdentityError';
  }
}

export function isValidCorridorVector3(pos: unknown): pos is Vector3D {
  if (!pos || typeof pos !== 'object') return false;
  const candidate = pos as Record<string, unknown>;
  return (
    typeof candidate.x === 'number' &&
    Number.isFinite(candidate.x) &&
    typeof candidate.y === 'number' &&
    Number.isFinite(candidate.y) &&
    typeof candidate.z === 'number' &&
    Number.isFinite(candidate.z)
  );
}

export function clampToCorridorBounds(pos: Vector3D): Vector3D {
  return Object.freeze({
    x: Number(Math.max(CORRIDOR_POSITION_BOUNDS.minX, Math.min(CORRIDOR_POSITION_BOUNDS.maxX, pos.x)).toFixed(4)),
    y: Number(Math.max(CORRIDOR_POSITION_BOUNDS.minY, Math.min(CORRIDOR_POSITION_BOUNDS.maxY, pos.y)).toFixed(4)),
    z: Number(Math.max(CORRIDOR_POSITION_BOUNDS.minZ, Math.min(CORRIDOR_POSITION_BOUNDS.maxZ, pos.z)).toFixed(4)),
  });
}

export function createInitialPlayerState(
  playerId: string = DEFAULT_PLAYER_ID,
  serverNowMs: number = Date.now()
): PlayerSpatialState {
  const safeNowMs = Number.isFinite(serverNowMs) ? serverNowMs : 0;
  return Object.freeze({
    playerId,
    position: DEFAULT_PLAYER_SPAWN_POSITION,
    lastMovementServerTimeMs: safeNowMs,
    lastSequenceNumber: 0,
    updatedAt: new Date(safeNowMs).toISOString(),
  });
}

/**
 * Pure deterministic server-side player movement integrator.
 *
 * - Normalizes diagonal input so magnitude never exceeds 1.0.
 * - Rejects non-finite or malformed direction / sequence inputs.
 * - Ignores out-of-order requests (sequenceNumber <= lastSequenceNumber).
 * - Measures elapsed time strictly from serverNowMs vs currentState.lastMovementServerTimeMs.
 * - Enforces MAX_MOVEMENT_INTEGRATION_SECONDS and CORRIDOR_POSITION_BOUNDS.
 */
export function applyAuthoritativePlayerMovement(
  currentState: PlayerSpatialState,
  intent: PlayerMovementIntent,
  serverNowMs: number
): PlayerMovementStepResult {
  if (!Number.isFinite(serverNowMs)) {
    throw new InvalidPlayerMovementInputError('invalid-server-time', 'Server timestamp must be a finite number.');
  }

  if (!intent || typeof intent !== 'object' || !intent.direction || typeof intent.direction !== 'object') {
    throw new InvalidPlayerMovementInputError(
      'invalid-movement-direction',
      'Movement intent must include a "direction" object with finite numeric x and z.'
    );
  }

  const rawX = intent.direction.x;
  const rawZ = intent.direction.z;
  if (typeof rawX !== 'number' || !Number.isFinite(rawX) || typeof rawZ !== 'number' || !Number.isFinite(rawZ)) {
    throw new InvalidPlayerMovementInputError(
      'invalid-movement-direction',
      'Movement direction components x and z must be finite numbers.'
    );
  }

  let movementMode: PlayerMovementMode = 'walk';
  if (intent.movementMode !== undefined && intent.movementMode !== null && intent.movementMode !== '') {
    if (intent.movementMode !== 'walk' && intent.movementMode !== 'sprint') {
      throw new InvalidPlayerMovementInputError(
        'invalid-movement-mode',
        `Invalid movementMode "${String(intent.movementMode)}". Allowed values: walk, sprint.`
      );
    }
    movementMode = intent.movementMode;
  }

  let sequenceNumber: number | undefined;
  if (intent.sequenceNumber !== undefined && intent.sequenceNumber !== null) {
    if (
      typeof intent.sequenceNumber !== 'number' ||
      !Number.isFinite(intent.sequenceNumber) ||
      !Number.isInteger(intent.sequenceNumber) ||
      intent.sequenceNumber < 0
    ) {
      throw new InvalidPlayerMovementInputError(
        'invalid-sequence-number',
        'Field "sequenceNumber" must be a non-negative finite integer.'
      );
    }
    sequenceNumber = intent.sequenceNumber;
  }

  // Out-of-order / duplicate protection: never regress canonical player state
  if (sequenceNumber !== undefined && sequenceNumber <= currentState.lastSequenceNumber) {
    return Object.freeze({
      accepted: false,
      outOfOrder: true,
      reasonCode: 'stale_sequence_ignored',
      message: Object.freeze({
        en: `Ignored out-of-order movement intent (seq ${sequenceNumber} <= ${currentState.lastSequenceNumber}).`,
        es: `Intención de movimiento fuera de orden ignorada (secuencia ${sequenceNumber} <= ${currentState.lastSequenceNumber}).`,
      }),
      player: currentState,
      appliedDirection: Object.freeze({ x: 0, z: 0 }),
      appliedSpeedMps: 0,
      elapsedSeconds: 0,
      displacementMeters: 0,
    });
  }

  // Normalize diagonal / oversized planar input so magnitude <= 1.0
  const mag = Math.hypot(rawX, rawZ);
  let normX = 0;
  let normZ = 0;
  if (mag > 1e-6) {
    if (mag > 1.0) {
      normX = rawX / mag;
      normZ = rawZ / mag;
    } else {
      normX = rawX;
      normZ = rawZ;
    }
  }

  // Compute server-measured elapsed time (clamped to [0, MAX_MOVEMENT_INTEGRATION_SECONDS])
  const rawElapsedSeconds = (serverNowMs - currentState.lastMovementServerTimeMs) / 1000;
  const elapsedSeconds = Number(
    Math.min(MAX_MOVEMENT_INTEGRATION_SECONDS, Math.max(0, rawElapsedSeconds)).toFixed(6)
  );

  const appliedSpeedMps = movementMode === 'sprint' ? PLAYER_SPRINT_SPEED_MPS : PLAYER_WALK_SPEED_MPS;
  const deltaX = normX * appliedSpeedMps * elapsedSeconds;
  const deltaZ = normZ * appliedSpeedMps * elapsedSeconds;

  const unclampedNext: Vector3D = {
    x: currentState.position.x + deltaX,
    y: currentState.position.y,
    z: currentState.position.z + deltaZ,
  };
  const nextPosition = clampToCorridorBounds(unclampedNext);

  const actualDisplacement = Number(
    Math.hypot(nextPosition.x - currentState.position.x, nextPosition.z - currentState.position.z).toFixed(4)
  );

  const nextServerTimeMs = Math.max(currentState.lastMovementServerTimeMs, serverNowMs);
  const nextSequence = sequenceNumber !== undefined ? sequenceNumber : currentState.lastSequenceNumber + 1;

  const nextState: PlayerSpatialState = Object.freeze({
    playerId: currentState.playerId,
    position: nextPosition,
    lastMovementServerTimeMs: nextServerTimeMs,
    lastSequenceNumber: nextSequence,
    updatedAt: new Date(nextServerTimeMs).toISOString(),
  });

  return Object.freeze({
    accepted: true,
    outOfOrder: false,
    reasonCode: 'movement_applied',
    message: Object.freeze({
      en: 'Authoritative player movement integrated by World Core.',
      es: 'Movimiento autoritativo del jugador integrado por el World Core.',
    }),
    player: nextState,
    appliedDirection: Object.freeze({
      x: Number(normX.toFixed(6)),
      z: Number(normZ.toFixed(6)),
    }),
    appliedSpeedMps,
    elapsedSeconds,
    displacementMeters: actualDisplacement,
  });
}
