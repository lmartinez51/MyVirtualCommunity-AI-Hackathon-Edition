import { WorldCorePort } from '../ports/world-core-port.js';
import {
  DEFAULT_PLAYER_ID,
  InvalidPlayerMovementInputError,
  PlayerMovementIntent,
  PlayerMovementStepResult,
  UnauthorizedPlayerIdentityError,
} from '../domain/player-movement.js';
import { WorldCoreCapabilityMissingError } from './validate-npc-interaction.js';

export interface MovePlayerInput {
  readonly playerId: string;
  readonly direction: { readonly x: number; readonly z: number };
  readonly movementMode?: string;
  readonly sequenceNumber?: number;
}

/**
 * Authoritative Application Use Case: Move Player via Intent.
 *
 * Enforces canonical single-player identity protection (cannot spoof another playerId),
 * extracts only normalized movement intent (direction, movementMode, sequenceNumber),
 * and delegates position integration strictly to World Core using server-side timing.
 */
export async function movePlayer(
  worldCore: WorldCorePort,
  input: MovePlayerInput,
  serverTimeMs: number = Date.now()
): Promise<PlayerMovementStepResult> {
  const playerId = typeof input.playerId === 'string' ? input.playerId.trim() : '';
  if (!playerId) {
    throw new InvalidPlayerMovementInputError('invalid-player-id', 'Field "playerId" must be a non-empty string.');
  }

  if (playerId !== DEFAULT_PLAYER_ID) {
    throw new UnauthorizedPlayerIdentityError(playerId);
  }

  if (typeof worldCore.applyPlayerMovement !== 'function') {
    throw new WorldCoreCapabilityMissingError(
      'World Core does not implement authoritative player movement (applyPlayerMovement).'
    );
  }

  const sanitizedIntent: PlayerMovementIntent = {
    direction: input.direction,
    movementMode: input.movementMode,
    sequenceNumber: input.sequenceNumber,
  };

  return worldCore.applyPlayerMovement(playerId, sanitizedIntent, serverTimeMs);
}
