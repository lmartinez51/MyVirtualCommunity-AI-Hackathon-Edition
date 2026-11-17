/**
 * Autonomous NPC Social Interaction Lifecycle & Conversation Behavior Domain Model (Increment 5.21.0).
 *
 * Core Architectural Invariants:
 * 1. Explicit Separation from Social Awareness:
 *    - Social awareness (`npc-social-awareness.ts`) models perception (`unaware`, `player_aware`,
 *      `player_attentive`, `npc_aware`, `social_engaged`).
 *    - Social interaction models an actual bounded NPC-to-NPC encounter lifecycle
 *      (`initiating` -> `engaged` -> `closing` -> `completed`) plus cooldown recovery.
 * 2. Pure Authoritative Time-Driven Determinism:
 *    - Zero `Date.now()`, `setTimeout`, `setInterval`, or mutable hidden timers.
 *    - Evaluated purely from authoritative World Clock instant, spatial positions, movement,
 *      activity/interaction context, and Increment 5.20.0.2 actor archetypes.
 *    - Reconstructible identically across process restarts or adapters given equivalent inputs.
 * 3. Actor Archetype & Duty Priority Integrity:
 *    - Consumes explicit `actorArchetype` (`business_attendant`, `public_servant`,
 *      `community_player_proxy`) and `isWitness` metadata directly; never infers from names/labels.
 *    - Business attendants and public servants retain task, busy, and off-duty priority.
 *    - Community player proxies never enter `off_duty`, socialize independently of human player
 *      proximity, and never emit fabricated witness testimony or incident statements.
 * 4. Movement & Spatial Integrity:
 *    - Never mutates canonical coordinates or interrupts authoritative waypoint progression.
 *    - Only spatially compatible, contextually eligible NPCs form 1-to-1 exclusive sessions.
 */

import {
  NpcActorArchetype,
  WitnessClassification,
  canActorArchetypeEnterOffDuty,
  getNpcActorClassification,
} from './npc-actor-archetypes.js';
import {
  CANONICAL_NPC_IDS,
  NpcInteractionAvailability,
  NpcInteractionContext,
  NpcRosterInteractionContext,
  evaluateRosterInteractionContext,
} from './npc-interaction-context.js';
import {
  MAX_AWARENESS_VERTICAL_SEPARATION_METERS,
  NPC_SOCIAL_PROXIMITY_RANGE_METERS,
  evaluateHorizontalDistanceMeters,
  evaluateVerticalSeparationMeters,
  isContextualStationOccupiedForSocial,
} from './npc-social-awareness.js';
import {
  NPC_MOVEMENT_PROFILES,
  NpcMovementSnapshot,
  NpcMovementState,
  NpcRosterMovementSnapshot,
  Vector3D,
  evaluateRosterMovement,
} from './npc-waypoint-movement.js';
import { isValidCorridorVector3 } from './player-movement.js';
import { BilingualText, SyntheticWitnessId } from './synthetic-witness.js';
import { evaluatePassingEncounterPairStatus } from './npc-autonomous-locomotion.js';

export type NpcSocialInteractionPhase = 'initiating' | 'engaged' | 'closing' | 'completed';

export type NpcConversationalRole = 'speaker' | 'listener' | 'mutual' | 'none';

export type NpcSocialInteractionType =
  | 'community_queue_exchange'
  | 'merchant_neighbor_exchange'
  | 'civic_corridor_greeting'
  | 'corridor_social_exchange'
  | 'passing_greeting'
  | 'passing_exchange';

export const SOCIAL_INTERACTION_MAX_RANGE_METERS = NPC_SOCIAL_PROXIMITY_RANGE_METERS;
export const SOCIAL_INTERACTION_MAX_VERTICAL_METERS = MAX_AWARENESS_VERTICAL_SEPARATION_METERS;

export const SOCIAL_INTERACTION_CYCLE_SECONDS = 60;
export const INITIATING_DURATION_SECONDS = 4;
export const MIN_ENGAGED_DURATION_SECONDS = 24;
export const MAX_ENGAGED_DURATION_SECONDS = 36;
export const CLOSING_DURATION_SECONDS = 4;
export const COMPLETED_GRACE_DURATION_SECONDS = 3;
export const CONVERSATION_TURN_DURATION_SECONDS = 5;

export const SIMULATED_SOCIAL_INTERACTION_NOTICE: BilingualText = Object.freeze({
  en: 'Deterministic simulated NPC-to-NPC social interaction lifecycle driven by authoritative World Clock and spatial proximity (no fabricated testimony or external audio).',
  es: 'Ciclo de vida determinista de interacción social simulada entre PNJs guiado por el Reloj del Mundo autoritativo y proximidad espacial (sin testimonios inventados ni audio externo).',
});

export interface NpcSocialInteractionParticipant {
  readonly npcId: string;
  readonly canonicalName: string;
  readonly actorArchetype: NpcActorArchetype;
  readonly isWitness: boolean;
  readonly witnessClassification: WitnessClassification;
  readonly witnessId: SyntheticWitnessId | null;
  readonly conversationalRole: NpcConversationalRole;
  readonly associatedPlaceId: string;
  readonly currentWaypointId: string;
}

export interface NpcSocialInteractionSession {
  readonly sessionId: string;
  readonly participantNpcIds: readonly [string, string];
  readonly participants: readonly [NpcSocialInteractionParticipant, NpcSocialInteractionParticipant];
  readonly interactionType: NpcSocialInteractionType;
  readonly phase: NpcSocialInteractionPhase;
  readonly isActive: boolean;
  readonly startedAt: string;
  readonly expectedEndAt: string;
  readonly cooldownEndsAt: string;
  readonly turnIndex: number;
  readonly activeSpeakerNpcId: string | null;
  readonly activeListenerNpcId: string | null;
  readonly distanceMeters: number;
  readonly evaluatedAt: string;
  readonly timeZone: string;
  readonly localCivilTime: string;
  readonly reason: BilingualText;
  readonly isDemonstrationInteraction: true;
  readonly demonstrationNotice: BilingualText;
}

export interface NpcSocialInteractionParticipation {
  readonly npcId: string;
  readonly canonicalName: string;
  readonly actorArchetype: NpcActorArchetype;
  readonly hasEmploymentSchedule: boolean;
  readonly canEnterOffDuty: boolean;
  readonly isWitness: boolean;
  readonly witnessClassification: WitnessClassification;
  readonly witnessId: SyntheticWitnessId | null;
  readonly inActiveInteraction: boolean;
  readonly sessionId: string | null;
  readonly phase: NpcSocialInteractionPhase | null;
  readonly interactionType: NpcSocialInteractionType | null;
  readonly partnerNpcId: string | null;
  readonly conversationalRole: NpcConversationalRole;
  readonly turnIndex: number;
  readonly sessionStartedAt: string | null;
  readonly sessionExpectedEndAt: string | null;
  readonly cooldownUntil: string | null;
  readonly inCooldown: boolean;
  readonly eligibleForSocialInteraction: boolean;
  readonly supersededByPlayerPriority: boolean;
  readonly interactionAvailability: NpcInteractionAvailability;
  readonly movementState: NpcMovementState;
  readonly evaluatedAt: string;
  readonly timeZone: string;
  readonly localCivilTime: string;
  readonly reason: BilingualText;
  readonly isDemonstrationInteraction: true;
  readonly demonstrationNotice: BilingualText;
}

export interface NpcRosterSocialInteractionsSnapshot {
  readonly evaluatedAt: string;
  readonly timeZone: string;
  readonly localCivilTime: string;
  readonly activePlayerInteractionNpcId: string | null;
  readonly maxHorizontalRangeMeters: number;
  readonly maxVerticalSeparationMeters: number;
  readonly sessions: readonly NpcSocialInteractionSession[];
  readonly npcs: Readonly<Record<string, NpcSocialInteractionParticipation>>;
  readonly npcsList: readonly NpcSocialInteractionParticipation[];
  readonly summary: {
    readonly activeSessionCount: number;
    readonly initiatingCount: number;
    readonly engagedCount: number;
    readonly closingCount: number;
    readonly completedCount: number;
    readonly participatingNpcCount: number;
    readonly cooldownNpcCount: number;
    readonly totalCount: number;
  };
  readonly isDemonstrationInteraction: true;
  readonly demonstrationNotice: BilingualText;
}

export interface EvaluateRosterSocialInteractionsParams {
  readonly evaluatedAt: Date | string | number;
  readonly interactionRoster?: NpcRosterInteractionContext;
  readonly movementRoster?: NpcRosterMovementSnapshot;
  readonly activePlayerInteractionNpcId?: string | null;
}

export interface PairInteractionWindowEvaluation {
  readonly pairKey: string;
  readonly sessionId: string;
  readonly startedAtMs: number;
  readonly expectedEndAtMs: number;
  readonly completedEndAtMs: number;
  readonly cooldownEndsAtMs: number;
  readonly startedAt: string;
  readonly expectedEndAt: string;
  readonly cooldownEndsAt: string;
  readonly phase: NpcSocialInteractionPhase | null;
  readonly isActive: boolean;
  readonly inCooldown: boolean;
  readonly turnIndex: number;
  readonly activeSpeakerNpcId: string | null;
  readonly activeListenerNpcId: string | null;
}

interface EligibleSocialEntry {
  readonly npcId: string;
  readonly ctx: NpcInteractionContext;
  readonly mov: NpcMovementSnapshot;
  readonly position: Vector3D;
  readonly eligible: boolean;
  readonly supersededByPlayerPriority: boolean;
}

function stableHash32(input: string): number {
  let hash = 2166136261;
  for (let i = 0; i < input.length; i++) {
    hash ^= input.charCodeAt(i);
    hash = Math.imul(hash, 16777619);
  }
  return hash >>> 0;
}

/**
 * Explicit archetype-based eligibility guard for autonomous NPC-to-NPC social interactions.
 * Never infers eligibility from display names, labels, or prefabs.
 */
export function isNpcEligibleForSocialInteraction(
  ctx: NpcInteractionContext,
  mov: NpcMovementSnapshot
): boolean {
  if (mov.isSuspendedByPlayerInteraction) {
    return false;
  }

  if (mov.movementState === 'moving' || ctx.interactionAvailability === 'available_in_transit') {
    return false;
  }

  if (ctx.interactionAvailability === 'busy') {
    return false;
  }

  if (canActorArchetypeEnterOffDuty(ctx.actorArchetype)) {
    if (
      ctx.interactionAvailability === 'off_duty' ||
      ctx.interactionAvailability === 'unavailable'
    ) {
      return false;
    }
  }

  if (isContextualStationOccupiedForSocial(ctx.npcId, ctx)) {
    return false;
  }

  return true;
}

/**
 * Classifies an interaction pair strictly from authoritative actor archetypes and places.
 */
export function resolveSocialInteractionType(
  archetypeA: NpcActorArchetype,
  archetypeB: NpcActorArchetype,
  placeIdA: string,
  placeIdB: string
): NpcSocialInteractionType {
  if (
    archetypeA === 'community_player_proxy' &&
    archetypeB === 'community_player_proxy'
  ) {
    return 'community_queue_exchange';
  }

  if (archetypeA === 'public_servant' || archetypeB === 'public_servant') {
    return 'civic_corridor_greeting';
  }

  if (archetypeA === 'business_attendant' || archetypeB === 'business_attendant') {
    if (placeIdA === placeIdB && placeIdA.length > 0) {
      return 'merchant_neighbor_exchange';
    }
    return 'merchant_neighbor_exchange';
  }

  return 'corridor_social_exchange';
}

/**
 * Evaluates the deterministic encounter window, lifecycle phase, conversational turn speaker,
 * and cooldown boundaries for a canonical sorted NPC pair at `evaluatedAtMs`.
 * Pure math over simulation time — zero wall-clock or random state.
 */
export function evaluatePairInteractionWindow(
  minNpcId: string,
  maxNpcId: string,
  evaluatedAtMs: number
): PairInteractionWindowEvaluation {
  const ordered = minNpcId <= maxNpcId ? [minNpcId, maxNpcId] : [maxNpcId, minNpcId];
  const idA = ordered[0]!;
  const idB = ordered[1]!;
  const pairKey = `${idA}<->${idB}`;

  const cycleMs = SOCIAL_INTERACTION_CYCLE_SECONDS * 1000;
  const seed = stableHash32(pairKey);
  const offsetSeconds = seed % 20;
  const offsetMs = offsetSeconds * 1000;

  const shiftedMs = evaluatedAtMs - offsetMs;
  const cycleIndex = Math.floor(shiftedMs / cycleMs);
  const startedAtMs = cycleIndex * cycleMs + offsetMs;

  const cycleSeed = stableHash32(`${pairKey}|${cycleIndex}`);
  const engagedRange = MAX_ENGAGED_DURATION_SECONDS - MIN_ENGAGED_DURATION_SECONDS + 1;
  const engagedDurationSeconds = MIN_ENGAGED_DURATION_SECONDS + (cycleSeed % engagedRange);

  const initEndMs = startedAtMs + INITIATING_DURATION_SECONDS * 1000;
  const engagedEndMs = initEndMs + engagedDurationSeconds * 1000;
  const expectedEndAtMs = engagedEndMs + CLOSING_DURATION_SECONDS * 1000;
  const completedEndAtMs = expectedEndAtMs + COMPLETED_GRACE_DURATION_SECONDS * 1000;
  const cooldownEndsAtMs = startedAtMs + cycleMs;

  const startedAt = new Date(startedAtMs).toISOString();
  const expectedEndAt = new Date(expectedEndAtMs).toISOString();
  const cooldownEndsAt = new Date(cooldownEndsAtMs).toISOString();
  const startEpochSec = Math.floor(startedAtMs / 1000);
  const sessionId = `soc_${idA}_${idB}_${startEpochSec}`;

  let phase: NpcSocialInteractionPhase | null = null;
  let isActive = false;
  let inCooldown = false;
  let turnIndex = 0;
  let activeSpeakerNpcId: string | null = null;
  let activeListenerNpcId: string | null = null;

  const firstSpeaker = (cycleSeed & 1) === 0 ? idA : idB;
  const secondSpeaker = firstSpeaker === idA ? idB : idA;

  if (evaluatedAtMs >= startedAtMs && evaluatedAtMs < initEndMs) {
    phase = 'initiating';
    isActive = true;
    turnIndex = 0;
    activeSpeakerNpcId = firstSpeaker;
    activeListenerNpcId = secondSpeaker;
  } else if (evaluatedAtMs >= initEndMs && evaluatedAtMs < engagedEndMs) {
    phase = 'engaged';
    isActive = true;
    const elapsedEngagedSec = Math.floor((evaluatedAtMs - initEndMs) / 1000);
    turnIndex = Math.floor(elapsedEngagedSec / CONVERSATION_TURN_DURATION_SECONDS);
    const firstSpeaks = turnIndex % 2 === 0;
    activeSpeakerNpcId = firstSpeaks ? firstSpeaker : secondSpeaker;
    activeListenerNpcId = firstSpeaks ? secondSpeaker : firstSpeaker;
  } else if (evaluatedAtMs >= engagedEndMs && evaluatedAtMs < expectedEndAtMs) {
    phase = 'closing';
    isActive = true;
    turnIndex = Math.max(0, Math.floor((engagedEndMs - initEndMs) / (CONVERSATION_TURN_DURATION_SECONDS * 1000)) - 1);
    activeSpeakerNpcId = null;
    activeListenerNpcId = null;
  } else if (evaluatedAtMs >= expectedEndAtMs && evaluatedAtMs < completedEndAtMs) {
    phase = 'completed';
    isActive = false;
    inCooldown = true;
    turnIndex = 0;
  } else {
    phase = null;
    isActive = false;
    inCooldown = true;
    turnIndex = 0;
  }

  return Object.freeze({
    pairKey,
    sessionId,
    startedAtMs,
    expectedEndAtMs,
    completedEndAtMs,
    cooldownEndsAtMs,
    startedAt,
    expectedEndAt,
    cooldownEndsAt,
    phase,
    isActive,
    inCooldown,
    turnIndex,
    activeSpeakerNpcId,
    activeListenerNpcId,
  });
}

function buildPhaseReason(
  phase: NpcSocialInteractionPhase,
  nameA: string,
  nameB: string,
  speakerName: string | null,
  interactionType: NpcSocialInteractionType
): BilingualText {
  const typeEn =
    interactionType === 'community_queue_exchange'
      ? 'queue community exchange'
      : interactionType === 'civic_corridor_greeting'
        ? 'corridor civic greeting'
        : interactionType === 'merchant_neighbor_exchange'
          ? 'merchant neighbor exchange'
          : 'corridor exchange';
  const typeEs =
    interactionType === 'community_queue_exchange'
      ? 'intercambio comunitario en fila'
      : interactionType === 'civic_corridor_greeting'
        ? 'saludo cívico del corredor'
        : interactionType === 'merchant_neighbor_exchange'
          ? 'intercambio vecinal comercial'
          : 'intercambio en el corredor';

  switch (phase) {
    case 'initiating':
      return Object.freeze({
        en: `${nameA} and ${nameB} are initiating a brief ${typeEn}.`,
        es: `${nameA} y ${nameB} están iniciando un breve ${typeEs}.`,
      });
    case 'engaged':
      return Object.freeze({
        en: speakerName
          ? `${nameA} and ${nameB} are engaged in a ${typeEn} (${speakerName} taking turn).`
          : `${nameA} and ${nameB} are engaged in a ${typeEn}.`,
        es: speakerName
          ? `${nameA} y ${nameB} conversan en un ${typeEs} (${speakerName} con la palabra).`
          : `${nameA} y ${nameB} conversan en un ${typeEs}.`,
      });
    case 'closing':
      return Object.freeze({
        en: `${nameA} and ${nameB} are wrapping up their ${typeEn} and returning to normal posture.`,
        es: `${nameA} y ${nameB} están concluyendo su ${typeEs} para retomar su postura habitual.`,
      });
    case 'completed':
      return Object.freeze({
        en: `${nameA} and ${nameB} completed their ${typeEn} and entered re-engagement cooldown.`,
        es: `${nameA} y ${nameB} concluyeron su ${typeEs} y entraron en pausa de enfriamiento.`,
      });
  }
}

/**
 * Pure deterministic evaluator for autonomous NPC-to-NPC social interaction sessions across the roster.
 */
export function evaluateRosterSocialInteractions(
  params: EvaluateRosterSocialInteractionsParams
): NpcRosterSocialInteractionsSnapshot {
  const evaluatedDate = new Date(params.evaluatedAt);
  const evaluatedAtIso = evaluatedDate.toISOString();
  const evaluatedAtMs = evaluatedDate.getTime();

  const movementRoster =
    params.movementRoster ?? evaluateRosterMovement(NPC_MOVEMENT_PROFILES, evaluatedAtIso);
  const interactionRoster =
    params.interactionRoster ?? evaluateRosterInteractionContext(evaluatedAtIso);
  const timeZone = interactionRoster.timeZone;
  const localCivilTime = interactionRoster.localCivilTime;

  const activePlayerNpcId =
    typeof params.activePlayerInteractionNpcId === 'string' &&
    params.activePlayerInteractionNpcId.trim() !== ''
      ? params.activePlayerInteractionNpcId.trim()
      : null;

  const idSet = new Set<string>(CANONICAL_NPC_IDS);
  for (const id of Object.keys(interactionRoster.npcs)) idSet.add(id);
  for (const id of Object.keys(movementRoster.npcs)) idSet.add(id);
  const orderedIds = Array.from(idSet).sort((a, b) => {
    const idxA = (CANONICAL_NPC_IDS as readonly string[]).indexOf(a);
    const idxB = (CANONICAL_NPC_IDS as readonly string[]).indexOf(b);
    if (idxA !== -1 && idxB !== -1) return idxA - idxB;
    if (idxA !== -1) return -1;
    if (idxB !== -1) return 1;
    return a.localeCompare(b);
  });

  const entriesById = new Map<string, EligibleSocialEntry>();
  const eligibleEntries: EligibleSocialEntry[] = [];

  for (const npcId of orderedIds) {
    const ctx = interactionRoster.npcs[npcId];
    const mov = movementRoster.npcs[npcId];
    if (!ctx || !mov || !isValidCorridorVector3(mov.position)) {
      continue;
    }
    const superseded = activePlayerNpcId === npcId || Boolean(mov.isSuspendedByPlayerInteraction);
    const eligible = !superseded && isNpcEligibleForSocialInteraction(ctx, mov);
    const entry: EligibleSocialEntry = {
      npcId,
      ctx,
      mov,
      position: mov.position,
      eligible,
      supersededByPlayerPriority: superseded,
    };
    entriesById.set(npcId, entry);
    if (eligible) {
      eligibleEntries.push(entry);
    }
  }

  interface CandidatePair {
    readonly a: EligibleSocialEntry;
    readonly b: EligibleSocialEntry;
    readonly distanceMeters: number;
    readonly minId: string;
    readonly maxId: string;
    readonly window: PairInteractionWindowEvaluation;
    readonly explicitInteractionType?: NpcSocialInteractionType;
  }

  const candidatePairs: CandidatePair[] = [];
  for (let i = 0; i < eligibleEntries.length; i++) {
    const a = eligibleEntries[i]!;
    for (let j = i + 1; j < eligibleEntries.length; j++) {
      const b = eligibleEntries[j]!;
      const vSep = evaluateVerticalSeparationMeters(a.position, b.position);
      if (vSep > SOCIAL_INTERACTION_MAX_VERTICAL_METERS) continue;

      const hDist = evaluateHorizontalDistanceMeters(a.position, b.position);
      if (hDist > SOCIAL_INTERACTION_MAX_RANGE_METERS) continue;

      const minId = a.npcId <= b.npcId ? a.npcId : b.npcId;
      const maxId = a.npcId <= b.npcId ? b.npcId : a.npcId;
      const entryMin = a.npcId <= b.npcId ? a : b;
      const entryMax = a.npcId <= b.npcId ? b : a;

      // Priority 3: Check if this pair is in a brief passing encounter (Increment 5.22.0 Subsystem A2)
      const isPassingPair =
        entryMin.mov.autonomousBehaviorState === 'passing_encounter' &&
        entryMax.mov.autonomousBehaviorState === 'passing_encounter' &&
        entryMin.mov.passingEncounterPartnerNpcId === entryMax.npcId;

      if (isPassingPair) {
        const passingEval = evaluatePassingEncounterPairStatus({
          npcA: entryMin.mov,
          npcB: entryMax.mov,
          evaluatedAtMs,
        });
        if (passingEval.activeEncounter) {
          const pe = passingEval.activeEncounter;
          const window: PairInteractionWindowEvaluation = Object.freeze({
            pairKey: `${minId}<->${maxId}`,
            sessionId: pe.sessionId,
            startedAtMs: pe.startedAtMs,
            expectedEndAtMs: pe.expectedEndAtMs,
            completedEndAtMs: pe.expectedEndAtMs + 2000,
            cooldownEndsAtMs: pe.cooldownUntilMs,
            startedAt: new Date(pe.startedAtMs).toISOString(),
            expectedEndAt: new Date(pe.expectedEndAtMs).toISOString(),
            cooldownEndsAt: new Date(pe.cooldownUntilMs).toISOString(),
            phase: pe.phase,
            isActive: true,
            inCooldown: false,
            turnIndex: 0,
            activeSpeakerNpcId: pe.activeSpeakerNpcId,
            activeListenerNpcId: pe.activeListenerNpcId,
          });
          candidatePairs.push({
            a: entryMin,
            b: entryMax,
            distanceMeters: hDist,
            minId,
            maxId,
            window,
            explicitInteractionType: pe.encounterType,
          });
          continue;
        }
      }

      const window = evaluatePairInteractionWindow(minId, maxId, evaluatedAtMs);
      candidatePairs.push({
        a: entryMin,
        b: entryMax,
        distanceMeters: hDist,
        minId,
        maxId,
        window,
      });
    }
  }

  // Deterministic ordering: active sessions first (nearest distance, then canonical IDs), then completed/cooldown pairs.
  candidatePairs.sort((p1, p2) => {
    if (p1.window.isActive !== p2.window.isActive) {
      return p1.window.isActive ? -1 : 1;
    }
    if (p1.distanceMeters !== p2.distanceMeters) {
      return p1.distanceMeters - p2.distanceMeters;
    }
    const minCmp = p1.minId.localeCompare(p2.minId);
    if (minCmp !== 0) return minCmp;
    return p1.maxId.localeCompare(p2.maxId);
  });

  const sessions: NpcSocialInteractionSession[] = [];
  const participationSessionByNpc = new Map<string, NpcSocialInteractionSession>();
  const completedSessionByNpc = new Map<string, NpcSocialInteractionSession>();
  const cooldownWindowByNpc = new Map<string, PairInteractionWindowEvaluation>();

  const claimedActiveNpcs = new Set<string>();

  for (const cand of candidatePairs) {
    if (claimedActiveNpcs.has(cand.a.npcId) || claimedActiveNpcs.has(cand.b.npcId)) {
      continue;
    }

    const interactionType =
      cand.explicitInteractionType ??
      resolveSocialInteractionType(
        cand.a.ctx.actorArchetype,
        cand.b.ctx.actorArchetype,
        cand.a.ctx.associatedPlaceId,
        cand.b.ctx.associatedPlaceId
      );

    if (cand.window.phase === 'initiating' || cand.window.phase === 'engaged' || cand.window.phase === 'closing') {
      claimedActiveNpcs.add(cand.a.npcId);
      claimedActiveNpcs.add(cand.b.npcId);

      const speakerEntry =
        cand.window.activeSpeakerNpcId === cand.a.npcId
          ? cand.a
          : cand.window.activeSpeakerNpcId === cand.b.npcId
            ? cand.b
            : null;

      const roleFor = (id: string): NpcConversationalRole => {
        if (cand.window.phase === 'closing') return 'mutual';
        if (cand.window.activeSpeakerNpcId === id) return 'speaker';
        if (cand.window.activeListenerNpcId === id) return 'listener';
        return 'mutual';
      };

      const pA: NpcSocialInteractionParticipant = Object.freeze({
        npcId: cand.a.npcId,
        canonicalName: cand.a.ctx.canonicalName,
        actorArchetype: cand.a.ctx.actorArchetype,
        isWitness: cand.a.ctx.isWitness,
        witnessClassification: cand.a.ctx.witnessClassification,
        witnessId: cand.a.ctx.witnessId,
        conversationalRole: roleFor(cand.a.npcId),
        associatedPlaceId: cand.a.ctx.associatedPlaceId,
        currentWaypointId: cand.a.ctx.currentWaypointId,
      });

      const pB: NpcSocialInteractionParticipant = Object.freeze({
        npcId: cand.b.npcId,
        canonicalName: cand.b.ctx.canonicalName,
        actorArchetype: cand.b.ctx.actorArchetype,
        isWitness: cand.b.ctx.isWitness,
        witnessClassification: cand.b.ctx.witnessClassification,
        witnessId: cand.b.ctx.witnessId,
        conversationalRole: roleFor(cand.b.npcId),
        associatedPlaceId: cand.b.ctx.associatedPlaceId,
        currentWaypointId: cand.b.ctx.currentWaypointId,
      });

      const session: NpcSocialInteractionSession = Object.freeze({
        sessionId: cand.window.sessionId,
        participantNpcIds: Object.freeze([cand.a.npcId, cand.b.npcId]) as readonly [string, string],
        participants: Object.freeze([pA, pB]) as readonly [
          NpcSocialInteractionParticipant,
          NpcSocialInteractionParticipant,
        ],
        interactionType,
        phase: cand.window.phase,
        isActive: true,
        startedAt: cand.window.startedAt,
        expectedEndAt: cand.window.expectedEndAt,
        cooldownEndsAt: cand.window.cooldownEndsAt,
        turnIndex: cand.window.turnIndex,
        activeSpeakerNpcId: cand.window.activeSpeakerNpcId,
        activeListenerNpcId: cand.window.activeListenerNpcId,
        distanceMeters: cand.distanceMeters,
        evaluatedAt: evaluatedAtIso,
        timeZone,
        localCivilTime,
        reason: buildPhaseReason(
          cand.window.phase,
          cand.a.ctx.canonicalName,
          cand.b.ctx.canonicalName,
          speakerEntry ? speakerEntry.ctx.canonicalName : null,
          interactionType
        ),
        isDemonstrationInteraction: true,
        demonstrationNotice: SIMULATED_SOCIAL_INTERACTION_NOTICE,
      });

      sessions.push(session);
      participationSessionByNpc.set(cand.a.npcId, session);
      participationSessionByNpc.set(cand.b.npcId, session);
    } else if (cand.window.phase === 'completed') {
      if (!completedSessionByNpc.has(cand.a.npcId) && !completedSessionByNpc.has(cand.b.npcId)) {
        const pA: NpcSocialInteractionParticipant = Object.freeze({
          npcId: cand.a.npcId,
          canonicalName: cand.a.ctx.canonicalName,
          actorArchetype: cand.a.ctx.actorArchetype,
          isWitness: cand.a.ctx.isWitness,
          witnessClassification: cand.a.ctx.witnessClassification,
          witnessId: cand.a.ctx.witnessId,
          conversationalRole: 'none',
          associatedPlaceId: cand.a.ctx.associatedPlaceId,
          currentWaypointId: cand.a.ctx.currentWaypointId,
        });
        const pB: NpcSocialInteractionParticipant = Object.freeze({
          npcId: cand.b.npcId,
          canonicalName: cand.b.ctx.canonicalName,
          actorArchetype: cand.b.ctx.actorArchetype,
          isWitness: cand.b.ctx.isWitness,
          witnessClassification: cand.b.ctx.witnessClassification,
          witnessId: cand.b.ctx.witnessId,
          conversationalRole: 'none',
          associatedPlaceId: cand.b.ctx.associatedPlaceId,
          currentWaypointId: cand.b.ctx.currentWaypointId,
        });
        const completedSession: NpcSocialInteractionSession = Object.freeze({
          sessionId: cand.window.sessionId,
          participantNpcIds: Object.freeze([cand.a.npcId, cand.b.npcId]) as readonly [string, string],
          participants: Object.freeze([pA, pB]) as readonly [
            NpcSocialInteractionParticipant,
            NpcSocialInteractionParticipant,
          ],
          interactionType,
          phase: 'completed',
          isActive: false,
          startedAt: cand.window.startedAt,
          expectedEndAt: cand.window.expectedEndAt,
          cooldownEndsAt: cand.window.cooldownEndsAt,
          turnIndex: 0,
          activeSpeakerNpcId: null,
          activeListenerNpcId: null,
          distanceMeters: cand.distanceMeters,
          evaluatedAt: evaluatedAtIso,
          timeZone,
          localCivilTime,
          reason: buildPhaseReason(
            'completed',
            cand.a.ctx.canonicalName,
            cand.b.ctx.canonicalName,
            null,
            interactionType
          ),
          isDemonstrationInteraction: true,
          demonstrationNotice: SIMULATED_SOCIAL_INTERACTION_NOTICE,
        });
        completedSessionByNpc.set(cand.a.npcId, completedSession);
        completedSessionByNpc.set(cand.b.npcId, completedSession);
      }
      if (!cooldownWindowByNpc.has(cand.a.npcId)) cooldownWindowByNpc.set(cand.a.npcId, cand.window);
      if (!cooldownWindowByNpc.has(cand.b.npcId)) cooldownWindowByNpc.set(cand.b.npcId, cand.window);
    } else if (cand.window.inCooldown) {
      if (!cooldownWindowByNpc.has(cand.a.npcId)) cooldownWindowByNpc.set(cand.a.npcId, cand.window);
      if (!cooldownWindowByNpc.has(cand.b.npcId)) cooldownWindowByNpc.set(cand.b.npcId, cand.window);
    }
  }

  const npcsMap: Record<string, NpcSocialInteractionParticipation> = {};
  const npcsList: NpcSocialInteractionParticipation[] = [];

  let initiatingCount = 0;
  let engagedCount = 0;
  let closingCount = 0;
  let completedCount = 0;
  let participatingNpcCount = 0;
  let cooldownNpcCount = 0;

  for (const s of sessions) {
    if (s.phase === 'initiating') initiatingCount++;
    else if (s.phase === 'engaged') engagedCount++;
    else if (s.phase === 'closing') closingCount++;
  }

  for (const npcId of orderedIds) {
    const entry = entriesById.get(npcId);
    const fallbackCtx = interactionRoster.npcs[npcId];
    const fallbackMov = movementRoster.npcs[npcId];
    const fallbackClass = getNpcActorClassification(npcId);

    const canonicalName = entry?.ctx.canonicalName ?? fallbackCtx?.canonicalName ?? npcId;
    const actorArchetype =
      entry?.ctx.actorArchetype ?? fallbackCtx?.actorArchetype ?? fallbackClass?.actorArchetype ?? 'business_attendant';
    const hasEmploymentSchedule =
      entry?.ctx.hasEmploymentSchedule ??
      fallbackCtx?.hasEmploymentSchedule ??
      canActorArchetypeEnterOffDuty(actorArchetype);
    const canEnterOffDuty =
      entry?.ctx.canEnterOffDuty ??
      fallbackCtx?.canEnterOffDuty ??
      canActorArchetypeEnterOffDuty(actorArchetype);
    const isWitness = entry?.ctx.isWitness ?? fallbackCtx?.isWitness ?? fallbackClass?.isWitness ?? false;
    const witnessClassification =
      entry?.ctx.witnessClassification ??
      fallbackCtx?.witnessClassification ??
      fallbackClass?.witnessClassification ??
      'none';
    const witnessId =
      entry?.ctx.witnessId !== undefined
        ? entry.ctx.witnessId
        : (fallbackCtx?.witnessId ?? fallbackClass?.witnessId ?? null);
    const availability = entry?.ctx.interactionAvailability ?? fallbackCtx?.interactionAvailability ?? 'unavailable';
    const movementState = entry?.mov.movementState ?? fallbackMov?.movementState ?? 'stationary';

    const activeSession = participationSessionByNpc.get(npcId) ?? null;
    const completedSession = !activeSession ? completedSessionByNpc.get(npcId) ?? null : null;
    const cooldownWin = !activeSession ? cooldownWindowByNpc.get(npcId) ?? null : null;

    let participation: NpcSocialInteractionParticipation;

    if (activeSession) {
      participatingNpcCount++;
      const participant = activeSession.participants.find((p) => p.npcId === npcId)!;
      const partnerId = activeSession.participantNpcIds[0] === npcId
        ? activeSession.participantNpcIds[1]
        : activeSession.participantNpcIds[0];

      participation = Object.freeze({
        npcId,
        canonicalName,
        actorArchetype,
        hasEmploymentSchedule,
        canEnterOffDuty,
        isWitness,
        witnessClassification,
        witnessId,
        inActiveInteraction: true,
        sessionId: activeSession.sessionId,
        phase: activeSession.phase,
        interactionType: activeSession.interactionType,
        partnerNpcId: partnerId,
        conversationalRole: participant.conversationalRole,
        turnIndex: activeSession.turnIndex,
        sessionStartedAt: activeSession.startedAt,
        sessionExpectedEndAt: activeSession.expectedEndAt,
        cooldownUntil: activeSession.cooldownEndsAt,
        inCooldown: false,
        eligibleForSocialInteraction: true,
        supersededByPlayerPriority: false,
        interactionAvailability: availability,
        movementState,
        evaluatedAt: evaluatedAtIso,
        timeZone,
        localCivilTime,
        reason: activeSession.reason,
        isDemonstrationInteraction: true,
        demonstrationNotice: SIMULATED_SOCIAL_INTERACTION_NOTICE,
      });
    } else if (completedSession) {
      completedCount++;
      cooldownNpcCount++;
      const partnerId = completedSession.participantNpcIds[0] === npcId
        ? completedSession.participantNpcIds[1]
        : completedSession.participantNpcIds[0];

      participation = Object.freeze({
        npcId,
        canonicalName,
        actorArchetype,
        hasEmploymentSchedule,
        canEnterOffDuty,
        isWitness,
        witnessClassification,
        witnessId,
        inActiveInteraction: false,
        sessionId: completedSession.sessionId,
        phase: 'completed',
        interactionType: completedSession.interactionType,
        partnerNpcId: partnerId,
        conversationalRole: 'none',
        turnIndex: 0,
        sessionStartedAt: completedSession.startedAt,
        sessionExpectedEndAt: completedSession.expectedEndAt,
        cooldownUntil: completedSession.cooldownEndsAt,
        inCooldown: true,
        eligibleForSocialInteraction: true,
        supersededByPlayerPriority: false,
        interactionAvailability: availability,
        movementState,
        evaluatedAt: evaluatedAtIso,
        timeZone,
        localCivilTime,
        reason: completedSession.reason,
        isDemonstrationInteraction: true,
        demonstrationNotice: SIMULATED_SOCIAL_INTERACTION_NOTICE,
      });
    } else {
      const inCooldown = Boolean(entry?.eligible && cooldownWin);
      if (inCooldown) cooldownNpcCount++;

      let reason: BilingualText;
      if (entry?.supersededByPlayerPriority) {
        reason = Object.freeze({
          en: `${canonicalName} is prioritizing an active human player interaction over optional NPC conversation.`,
          es: `${canonicalName} prioriza la interacción activa con el jugador sobre conversaciones opcionales entre PNJs.`,
        });
      } else if (!entry?.eligible) {
        reason = Object.freeze({
          en: `${canonicalName} is currently ${availability.replace('_', ' ')} and retains priority activity without social interaction.`,
          es: `${canonicalName} se encuentra ${availability === 'off_duty' ? 'fuera de turno' : 'en actividad prioritaria'} y no participa en interacción social.`,
        });
      } else if (inCooldown) {
        reason = Object.freeze({
          en: `${canonicalName} is in a calm post-conversation cooldown window until the next encounter cycle.`,
          es: `${canonicalName} se encuentra en pausa tranquila posterior a la conversación hasta el siguiente ciclo.`,
        });
      } else {
        reason = Object.freeze({
          en: `${canonicalName} has no spatially compatible eligible partner for a social interaction right now.`,
          es: `${canonicalName} no tiene un interlocutor elegible cercano para iniciar una interacción social en este momento.`,
        });
      }

      participation = Object.freeze({
        npcId,
        canonicalName,
        actorArchetype,
        hasEmploymentSchedule,
        canEnterOffDuty,
        isWitness,
        witnessClassification,
        witnessId,
        inActiveInteraction: false,
        sessionId: null,
        phase: null,
        interactionType: null,
        partnerNpcId: null,
        conversationalRole: 'none',
        turnIndex: 0,
        sessionStartedAt: null,
        sessionExpectedEndAt: null,
        cooldownUntil: inCooldown && cooldownWin ? cooldownWin.cooldownEndsAt : null,
        inCooldown,
        eligibleForSocialInteraction: Boolean(entry?.eligible),
        supersededByPlayerPriority: Boolean(entry?.supersededByPlayerPriority),
        interactionAvailability: availability,
        movementState,
        evaluatedAt: evaluatedAtIso,
        timeZone,
        localCivilTime,
        reason,
        isDemonstrationInteraction: true,
        demonstrationNotice: SIMULATED_SOCIAL_INTERACTION_NOTICE,
      });
    }

    npcsMap[npcId] = participation;
    npcsList.push(participation);
  }

  return Object.freeze({
    evaluatedAt: evaluatedAtIso,
    timeZone,
    localCivilTime,
    activePlayerInteractionNpcId: activePlayerNpcId,
    maxHorizontalRangeMeters: SOCIAL_INTERACTION_MAX_RANGE_METERS,
    maxVerticalSeparationMeters: SOCIAL_INTERACTION_MAX_VERTICAL_METERS,
    sessions: Object.freeze(sessions),
    npcs: Object.freeze(npcsMap),
    npcsList: Object.freeze(npcsList),
    summary: Object.freeze({
      activeSessionCount: sessions.length,
      initiatingCount,
      engagedCount,
      closingCount,
      completedCount: Math.floor(completedCount / 2),
      participatingNpcCount,
      cooldownNpcCount,
      totalCount: npcsList.length,
    }),
    isDemonstrationInteraction: true,
    demonstrationNotice: SIMULATED_SOCIAL_INTERACTION_NOTICE,
  });
}

/**
 * Evaluates autonomous social interaction participation for a single canonical NPC.
 * Returns null when the NPC ID is unknown.
 */
export function evaluateNpcSocialInteraction(
  npcId: string,
  params: EvaluateRosterSocialInteractionsParams
): NpcSocialInteractionParticipation | null {
  if (typeof npcId !== 'string' || npcId.trim() === '') {
    return null;
  }
  const snapshot = evaluateRosterSocialInteractions(params);
  return snapshot.npcs[npcId.trim()] ?? null;
}
