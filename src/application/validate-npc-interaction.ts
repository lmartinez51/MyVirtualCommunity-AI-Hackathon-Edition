import { WorldCorePort } from '../ports/world-core-port.js';
import {
  InteractionIntent,
  NpcInteractionValidationResult,
  evaluateAuthoritativeInteractionDecision,
  isValidInteractionIntent,
} from '../domain/npc-interaction-validation.js';

export interface ValidateNpcInteractionInput {
  readonly playerId: string;
  readonly npcId: string;
  readonly intent?: InteractionIntent | string;
  readonly requestId?: string | null;
  readonly evaluatedAt?: Date | string | number;
  readonly maxDistanceMeters?: number;
}

export class InvalidInteractionRequestError extends Error {
  constructor(
    public readonly code: string,
    message: string
  ) {
    super(message);
    this.name = 'InvalidInteractionRequestError';
  }
}

export class WorldCoreCapabilityMissingError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'WorldCoreCapabilityMissingError';
  }
}

/**
 * Authoritative Application Use Case: Validate Player-to-NPC Interaction.
 *
 * Strictly reads canonical player spatial state, NPC movement/position,
 * NPC interaction context, and World Clock from WorldCorePort.
 * Never accepts client-supplied coordinates, availability, movement state,
 * or distance calculations as authoritative facts.
 */
export async function validateNpcInteraction(
  worldCore: WorldCorePort,
  input: ValidateNpcInteractionInput
): Promise<NpcInteractionValidationResult> {
  const playerId = typeof input.playerId === 'string' ? input.playerId.trim() : '';
  if (!playerId) {
    throw new InvalidInteractionRequestError('invalid-player-id', 'Field "playerId" must be a non-empty string.');
  }

  const npcId = typeof input.npcId === 'string' ? input.npcId.trim() : '';
  if (!npcId) {
    throw new InvalidInteractionRequestError('invalid-npc-id', 'Field "npcId" must be a non-empty string.');
  }

  let intent: InteractionIntent = 'talk';
  if (input.intent !== undefined && input.intent !== null && input.intent !== '') {
    if (!isValidInteractionIntent(input.intent)) {
      throw new InvalidInteractionRequestError(
        'invalid-interaction-intent',
        `Invalid interaction intent "${String(input.intent)}". Allowed values: talk, service, inquiry, greet.`
      );
    }
    intent = input.intent;
  }

  const requestId =
    typeof input.requestId === 'string' && input.requestId.trim() !== '' ? input.requestId.trim() : null;

  if (
    typeof worldCore.getPlayerState !== 'function' ||
    typeof worldCore.getNpcInteractionContext !== 'function' ||
    typeof worldCore.getNpcMovement !== 'function'
  ) {
    throw new WorldCoreCapabilityMissingError(
      'World Core does not implement required authoritative interaction validation queries (getPlayerState, getNpcInteractionContext, getNpcMovement).'
    );
  }

  const player = await worldCore.getPlayerState(playerId);

  const evaluatedAtIso =
    input.evaluatedAt !== undefined
      ? typeof input.evaluatedAt === 'string'
        ? new Date(input.evaluatedAt).toISOString()
        : new Date(input.evaluatedAt).toISOString()
      : await worldCore.getCurrentWorldTime();

  const [interactionContext, movementSnapshot] = await Promise.all([
    worldCore.getNpcInteractionContext(npcId, evaluatedAtIso),
    worldCore.getNpcMovement(npcId, evaluatedAtIso),
  ]);

  const decision = evaluateAuthoritativeInteractionDecision({
    player,
    playerId,
    npcId,
    intent,
    requestId,
    evaluatedAt: evaluatedAtIso,
    interactionContext,
    movementSnapshot,
    maxDistanceMeters: input.maxDistanceMeters,
  });

  if (decision.allowed && typeof worldCore.suspendNpcMovementForInteraction === 'function') {
    const suspendedMov = await worldCore.suspendNpcMovementForInteraction(
      playerId,
      npcId,
      evaluatedAtIso
    );
    if (suspendedMov) {
      return Object.freeze({
        ...decision,
        movementState: suspendedMov.movementState,
        npcPosition: suspendedMov.position,
      });
    }
  }

  return decision;
}
