/**
 * Server-Authoritative NPC Interaction Validation Domain Model (Increment 5.18.0).
 *
 * Core Architectural Invariants:
 * 1. World Core remains the sole authority for canonical world state, NPC positions,
 *    player positions, activity state, movement state, and interaction eligibility.
 * 2. Clients (Unity or HTTP callers) may submit interaction intents, but NEVER supply
 *    authoritative NPC coordinates, player coordinates, availability, movement state,
 *    or distance calculations as trusted facts in an interaction request.
 * 3. Deterministic evaluation using the authoritative World Clock (?at= or current world time).
 * 4. Pure authorization check: evaluating an interaction request does NOT mutate unrelated
 *    world state and does NOT fabricate conversation history.
 */

import { BilingualText, SyntheticWitnessId } from './synthetic-witness.js';
import { NpcActivityState } from './npc-activity.js';
import {
  NpcActorArchetype,
  WitnessClassification,
  getNpcActorClassification,
} from './npc-actor-archetypes.js';
import {
  NpcInteractionAvailability,
  NpcInteractionContext,
  NpcInteractionMode,
} from './npc-interaction-context.js';
import {
  NpcMovementSnapshot,
  NpcMovementState,
  Vector3D,
} from './npc-waypoint-movement.js';
import {
  CORRIDOR_POSITION_BOUNDS,
  DEFAULT_PLAYER_ID,
  DEFAULT_PLAYER_SPAWN_POSITION,
  PlayerSpatialState,
  clampToCorridorBounds,
  isValidCorridorVector3,
} from './player-movement.js';

export {
  CORRIDOR_POSITION_BOUNDS,
  DEFAULT_PLAYER_ID,
  DEFAULT_PLAYER_SPAWN_POSITION,
  clampToCorridorBounds,
  isValidCorridorVector3,
};
export type { PlayerSpatialState };

export type InteractionIntent = 'talk' | 'service' | 'inquiry' | 'greet';

export const VALID_INTERACTION_INTENTS: readonly InteractionIntent[] = Object.freeze([
  'talk',
  'service',
  'inquiry',
  'greet',
]);

export const MAX_INTERACTION_DISTANCE_METERS = 5.0;

export type NpcInteractionDecisionReasonCode =
  | 'authorized_normal_service'
  | 'authorized_in_transit'
  | 'authorized_contextual'
  | 'authorized_while_busy'
  | 'player_not_found'
  | 'npc_not_found'
  | 'out_of_range'
  | 'npc_off_duty'
  | 'npc_unavailable'
  | 'npc_in_transit_service_paused'
  | 'npc_on_break_service_paused'
  | 'npc_busy_service_deferred';

export interface NpcInteractionValidationResult {
  readonly allowed: boolean;
  readonly reasonCode: NpcInteractionDecisionReasonCode;
  readonly message: BilingualText;
  readonly playerId: string;
  readonly npcId: string;
  readonly intent: InteractionIntent;
  readonly requestId: string | null;
  readonly evaluatedAt: string;
  readonly timeZone: string;
  readonly localCivilTime: string;
  readonly distanceMeters: number | null;
  readonly maxDistanceMeters: number;
  readonly playerPosition: Vector3D | null;
  readonly npcPosition: Vector3D | null;
  readonly interactionAvailability: NpcInteractionAvailability | null;
  readonly interactionMode: NpcInteractionMode | null;
  readonly movementState: NpcMovementState | null;
  readonly activityState: NpcActivityState | null;
  readonly actorArchetype: NpcActorArchetype | null;
  readonly isWitness: boolean;
  readonly witnessClassification: WitnessClassification;
  readonly witnessId: SyntheticWitnessId | null;
  readonly interactionContext: NpcInteractionContext | null;
}

export function isValidInteractionIntent(value: unknown): value is InteractionIntent {
  return typeof value === 'string' && (VALID_INTERACTION_INTENTS as readonly string[]).includes(value);
}

export function calculateDistance3D(a: Vector3D, b: Vector3D): number {
  const dx = a.x - b.x;
  const dy = a.y - b.y;
  const dz = a.z - b.z;
  return Math.sqrt(dx * dx + dy * dy + dz * dz);
}

export interface EvaluateAuthoritativeInteractionParams {
  readonly player: PlayerSpatialState | null;
  readonly playerId: string;
  readonly npcId: string;
  readonly intent: InteractionIntent;
  readonly requestId: string | null;
  readonly evaluatedAt: string;
  readonly interactionContext: NpcInteractionContext | null;
  readonly movementSnapshot: NpcMovementSnapshot | null;
  readonly maxDistanceMeters?: number;
}

/**
 * Pure deterministic authorization policy for player-to-NPC interactions.
 * Evaluates identity, authoritative spatial proximity, and NPC availability/movement state.
 */
export function evaluateAuthoritativeInteractionDecision(
  params: EvaluateAuthoritativeInteractionParams
): NpcInteractionValidationResult {
  const maxDistanceMeters = params.maxDistanceMeters ?? MAX_INTERACTION_DISTANCE_METERS;
  const { player, playerId, npcId, intent, requestId, evaluatedAt, interactionContext, movementSnapshot } = params;

  const baseTimeZone = interactionContext?.timeZone ?? 'America/Ciudad_Juarez';
  const baseCivilTime = interactionContext?.localCivilTime ?? evaluatedAt;
  const fallbackClassification = getNpcActorClassification(npcId);
  const actorArchetype = interactionContext?.actorArchetype ?? fallbackClassification?.actorArchetype ?? null;
  const isWitness = interactionContext?.isWitness ?? fallbackClassification?.isWitness ?? false;
  const witnessClassification =
    interactionContext?.witnessClassification ?? fallbackClassification?.witnessClassification ?? 'none';
  const witnessId =
    interactionContext !== null && interactionContext !== undefined
      ? interactionContext.witnessId
      : (fallbackClassification?.witnessId ?? null);

  if (!player) {
    return Object.freeze({
      allowed: false,
      reasonCode: 'player_not_found',
      message: Object.freeze({
        en: `Player "${playerId}" was not found in authoritative World Core state.`,
        es: `El jugador "${playerId}" no fue encontrado en el estado autoritativo del World Core.`,
      }),
      playerId,
      npcId,
      intent,
      requestId,
      evaluatedAt,
      timeZone: baseTimeZone,
      localCivilTime: baseCivilTime,
      distanceMeters: null,
      maxDistanceMeters,
      playerPosition: null,
      npcPosition: movementSnapshot?.position ?? null,
      interactionAvailability: interactionContext?.interactionAvailability ?? null,
      interactionMode: interactionContext?.interactionMode ?? null,
      movementState: movementSnapshot?.movementState ?? null,
      activityState: interactionContext?.activityState ?? null,
      actorArchetype,
      isWitness,
      witnessClassification,
      witnessId,
      interactionContext: interactionContext ?? null,
    });
  }

  if (!interactionContext || !movementSnapshot) {
    return Object.freeze({
      allowed: false,
      reasonCode: 'npc_not_found',
      message: Object.freeze({
        en: `NPC "${npcId}" was not found in authoritative World Core roster.`,
        es: `El personaje "${npcId}" no fue encontrado en el registro autoritativo del World Core.`,
      }),
      playerId,
      npcId,
      intent,
      requestId,
      evaluatedAt,
      timeZone: baseTimeZone,
      localCivilTime: baseCivilTime,
      distanceMeters: null,
      maxDistanceMeters,
      playerPosition: player.position,
      npcPosition: null,
      interactionAvailability: null,
      interactionMode: null,
      movementState: null,
      activityState: null,
      actorArchetype: fallbackClassification?.actorArchetype ?? null,
      isWitness: fallbackClassification?.isWitness ?? false,
      witnessClassification: fallbackClassification?.witnessClassification ?? 'none',
      witnessId: fallbackClassification?.witnessId ?? null,
      interactionContext: null,
    });
  }

  const rawDistance = calculateDistance3D(player.position, movementSnapshot.position);
  const distanceMeters = Number(rawDistance.toFixed(3));

  const buildResult = (
    allowed: boolean,
    reasonCode: NpcInteractionDecisionReasonCode,
    message: BilingualText
  ): NpcInteractionValidationResult =>
    Object.freeze({
      allowed,
      reasonCode,
      message,
      playerId,
      npcId,
      intent,
      requestId,
      evaluatedAt: interactionContext.evaluatedAt,
      timeZone: interactionContext.timeZone,
      localCivilTime: interactionContext.localCivilTime,
      distanceMeters,
      maxDistanceMeters,
      playerPosition: player.position,
      npcPosition: movementSnapshot.position,
      interactionAvailability: interactionContext.interactionAvailability,
      interactionMode: interactionContext.interactionMode,
      movementState: movementSnapshot.movementState,
      activityState: interactionContext.activityState,
      actorArchetype: interactionContext.actorArchetype,
      isWitness: interactionContext.isWitness,
      witnessClassification: interactionContext.witnessClassification,
      witnessId: interactionContext.witnessId,
      interactionContext,
    });

  if (distanceMeters > maxDistanceMeters) {
    return buildResult(
      false,
      'out_of_range',
      Object.freeze({
        en: `Too far from ${interactionContext.canonicalName} (${distanceMeters.toFixed(1)} m away; max ${maxDistanceMeters.toFixed(1)} m). Move closer to interact.`,
        es: `Estás demasiado lejos de ${interactionContext.canonicalName} (a ${distanceMeters.toFixed(1)} m; máximo ${maxDistanceMeters.toFixed(1)} m). Acércate para interactuar.`,
      })
    );
  }

  const availability = interactionContext.interactionAvailability;

  if (availability === 'unavailable') {
    return buildResult(false, 'npc_unavailable', interactionContext.decisionReason);
  }

  if (availability === 'off_duty') {
    return buildResult(false, 'npc_off_duty', interactionContext.decisionReason);
  }

  if (availability === 'available_in_transit') {
    if (intent === 'service') {
      return buildResult(false, 'npc_in_transit_service_paused', interactionContext.decisionReason);
    }
    return buildResult(true, 'authorized_in_transit', interactionContext.decisionReason);
  }

  if (availability === 'available_contextual') {
    if (intent === 'service') {
      return buildResult(false, 'npc_on_break_service_paused', interactionContext.decisionReason);
    }
    return buildResult(true, 'authorized_contextual', interactionContext.decisionReason);
  }

  if (availability === 'busy') {
    if (intent === 'service') {
      return buildResult(false, 'npc_busy_service_deferred', interactionContext.decisionReason);
    }
    return buildResult(true, 'authorized_while_busy', interactionContext.decisionReason);
  }

  // available_normal
  return buildResult(true, 'authorized_normal_service', interactionContext.decisionReason);
}
