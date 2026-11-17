/**
 * Deterministic NPC Social Awareness & Environmental Awareness Domain Model (Increment 5.20.0).
 *
 * Core Architectural Invariants:
 * 1. World Core remains the sole authority for:
 *    - Canonical player and NPC corridor coordinates (meters, XZ planar + Y elevation).
 *    - NPC activity schedule, movement state, and interaction availability.
 *    - Proximity-based awareness classification and 1-to-1 deterministic NPC social pairings.
 * 2. Environmental & Visibility Honesty:
 *    - World Core does NOT currently model static occlusion geometry, line-of-sight rays,
 *      weather, ambient audio, traffic feeds, or crowd events.
 *    - Awareness detection in this increment is strictly horizontal + vertical proximity-based
 *      over canonical Player and NPC positions (with Place/Waypoint station metadata preserved).
 * 3. Non-interference:
 *    - Social awareness NEVER modifies NPC routes, schedules, movement progression, or
 *      interaction authorization rules.
 *    - Player awareness (e.g. looking toward nearby player) does NOT grant interaction
 *      authorization; only server-authoritative validation authorizes dialogue.
 * 4. Pure Deterministic Projection:
 *    - Given identical authoritative positions, movement, activity, and clock instant,
 *      evaluation produces identical targets and pairings without mutating world state
 *      or maintaining persistent social memory.
 */

import { BilingualText, SyntheticWitnessId } from './synthetic-witness.js';
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
  NPC_MOVEMENT_PROFILES,
  NpcMovementSnapshot,
  NpcMovementState,
  NpcRosterMovementSnapshot,
  Vector3D,
  evaluateRosterMovement,
} from './npc-waypoint-movement.js';
import { PlayerSpatialState, isValidCorridorVector3 } from './player-movement.js';

export type AwarenessEntityType = 'none' | 'player' | 'npc';

export type NpcSocialAwarenessState =
  | 'unaware'
  | 'player_aware'
  | 'player_attentive'
  | 'npc_aware'
  | 'social_engaged';

export const PLAYER_GENERAL_AWARENESS_RANGE_METERS = 8.0;
export const PLAYER_CLOSE_ATTENTION_RANGE_METERS = 4.0;
export const NPC_AWARENESS_RANGE_METERS = 5.0;
export const NPC_SOCIAL_PROXIMITY_RANGE_METERS = 3.75;
export const MAX_AWARENESS_VERTICAL_SEPARATION_METERS = 3.0;

export interface SocialAwarenessThresholds {
  readonly playerGeneralMeters: number;
  readonly playerCloseMeters: number;
  readonly npcAwarenessMeters: number;
  readonly npcSocialMeters: number;
  readonly maxVerticalSeparationMeters: number;
}

export const SOCIAL_AWARENESS_THRESHOLDS: SocialAwarenessThresholds = Object.freeze({
  playerGeneralMeters: PLAYER_GENERAL_AWARENESS_RANGE_METERS,
  playerCloseMeters: PLAYER_CLOSE_ATTENTION_RANGE_METERS,
  npcAwarenessMeters: NPC_AWARENESS_RANGE_METERS,
  npcSocialMeters: NPC_SOCIAL_PROXIMITY_RANGE_METERS,
  maxVerticalSeparationMeters: MAX_AWARENESS_VERTICAL_SEPARATION_METERS,
});

export const SIMULATED_AWARENESS_NOTICE: BilingualText = Object.freeze({
  en: 'Simulated deterministic social awareness grounded in World Core positions (horizontal & vertical proximity only; no line-of-sight occlusion or fabricated environmental feeds).',
  es: 'Conciencia social determinista simulada basada en posiciones del World Core (solo proximidad horizontal y vertical; sin oclusión de línea de vista ni eventos ambientales inventados).',
});

export interface NpcAwarenessTargetRelation {
  readonly horizontalDistanceMeters: number;
  readonly verticalSeparationMeters: number;
  readonly directionToTarget: {
    readonly x: number;
    readonly z: number;
  };
}

export interface NpcSocialAwarenessContext {
  readonly npcId: string;
  readonly canonicalName: string;
  readonly actorArchetype: NpcActorArchetype;
  readonly isWitness: boolean;
  readonly witnessClassification: WitnessClassification;
  readonly witnessId: SyntheticWitnessId | null;
  readonly awarenessState: NpcSocialAwarenessState;
  readonly targetEntityType: AwarenessEntityType;
  readonly targetEntityId: string | null;
  readonly spatialRelation: NpcAwarenessTargetRelation | null;
  readonly socialEligible: boolean;
  readonly canOrientTowardTarget: boolean;
  readonly retainsContextualActivity: boolean;
  readonly pairedPartnerNpcId: string | null;
  readonly associatedPlaceId: string;
  readonly currentWaypointId: string;
  readonly interactionAvailability: NpcInteractionAvailability;
  readonly movementState: NpcMovementState;
  readonly nearbyPlayerDistanceMeters: number | null;
  readonly nearbyNpcCount: number;
  readonly detectionBasis: 'proximity_horizontal_vertical';
  readonly supportsLineOfSight: false;
  readonly reason: BilingualText;
  readonly evaluatedAt: string;
  readonly timeZone: string;
  readonly localCivilTime: string;
  readonly isDemonstrationAwareness: true;
  readonly demonstrationNotice: BilingualText;
}

export interface ActiveNpcSocialPair {
  readonly npcIdA: string;
  readonly npcIdB: string;
  readonly distanceMeters: number;
}

export interface NpcRosterSocialAwarenessSnapshot {
  readonly evaluatedAt: string;
  readonly timeZone: string;
  readonly localCivilTime: string;
  readonly playerId: string | null;
  readonly playerPosition: Vector3D | null;
  readonly detectionBasis: 'proximity_horizontal_vertical';
  readonly supportsLineOfSight: false;
  readonly environmentalAwarenessScope: 'player_and_npc_proximity_only';
  readonly thresholds: SocialAwarenessThresholds;
  readonly npcs: Readonly<Record<string, NpcSocialAwarenessContext>>;
  readonly npcsList: readonly NpcSocialAwarenessContext[];
  readonly activeSocialPairs: readonly ActiveNpcSocialPair[];
  readonly summary: {
    readonly unawareCount: number;
    readonly playerAwareCount: number;
    readonly playerAttentiveCount: number;
    readonly npcAwareCount: number;
    readonly socialEngagedCount: number;
    readonly activePairCount: number;
    readonly totalCount: number;
  };
  readonly isDemonstrationAwareness: true;
  readonly demonstrationNotice: BilingualText;
}

export interface EvaluateRosterSocialAwarenessParams {
  readonly evaluatedAt: Date | string | number;
  readonly player?: PlayerSpatialState | null;
  readonly interactionRoster?: NpcRosterInteractionContext;
  readonly movementRoster?: NpcRosterMovementSnapshot;
}

interface ValidNpcEntry {
  readonly npcId: string;
  readonly ctx: NpcInteractionContext;
  readonly mov: NpcMovementSnapshot;
  readonly position: Vector3D;
  readonly socialEligible: boolean;
  readonly canOrientTowardTarget: boolean;
  readonly retainsContextualActivity: boolean;
}

function round3(value: number): number {
  return Math.round(value * 1000) / 1000;
}

export function evaluateHorizontalDistanceMeters(a: Vector3D, b: Vector3D): number {
  const dx = b.x - a.x;
  const dz = b.z - a.z;
  return round3(Math.sqrt(dx * dx + dz * dz));
}

export function evaluateVerticalSeparationMeters(a: Vector3D, b: Vector3D): number {
  return round3(Math.abs(b.y - a.y));
}

export function computeUnitPlanarDirection(
  from: Vector3D,
  to: Vector3D
): { readonly x: number; readonly z: number } {
  const dx = to.x - from.x;
  const dz = to.z - from.z;
  const len = Math.sqrt(dx * dx + dz * dz);
  if (len < 0.0001) {
    return Object.freeze({ x: 0, z: 0 });
  }
  return Object.freeze({
    x: round3(dx / len),
    z: round3(dz / len),
  });
}

function buildSpatialRelation(from: Vector3D, to: Vector3D): NpcAwarenessTargetRelation {
  return Object.freeze({
    horizontalDistanceMeters: evaluateHorizontalDistanceMeters(from, to),
    verticalSeparationMeters: evaluateVerticalSeparationMeters(from, to),
    directionToTarget: computeUnitPlanarDirection(from, to),
  });
}

/**
 * Station-specific contextual tasks (calibration inspection, woodcraft bench carving,
 * municipal pavement inspection) take priority over stationary social pairing so NPCs
 * do not abandon focused contextual animations for social chatter.
 */
export function isContextualStationOccupiedForSocial(
  npcId: string,
  ctx: NpcInteractionContext
): boolean {
  if (ctx.interactionAvailability !== 'available_contextual') {
    return false;
  }

  switch (npcId) {
    case 'valeria-optica-attendant':
      return ctx.currentWaypointId === 'wp_optica_workshop';
    case 'hector-artisan':
      return ctx.currentWaypointId === 'wp_hector_bench';
    case 'patricia-citizen-rep':
      return ctx.currentWaypointId === 'wp_west_inspection';
    default:
      return false;
  }
}

export function canNpcOrientTowardTarget(ctx: NpcInteractionContext): boolean {
  if (!canActorArchetypeEnterOffDuty(ctx.actorArchetype)) {
    return true;
  }
  return ctx.interactionAvailability !== 'off_duty' && ctx.interactionAvailability !== 'unavailable';
}

export function isNpcSociallyEligibleForPairing(
  ctx: NpcInteractionContext,
  mov: NpcMovementSnapshot
): boolean {
  if (!canNpcOrientTowardTarget(ctx)) {
    return false;
  }
  if (mov.movementState === 'moving' || ctx.interactionAvailability === 'available_in_transit') {
    return false;
  }
  if (ctx.interactionAvailability === 'busy') {
    return false;
  }
  if (isContextualStationOccupiedForSocial(ctx.npcId, ctx)) {
    return false;
  }
  return true;
}

/**
 * Spatial grid bucket index over planar XZ corridor coordinates.
 * Keeps pairwise neighbor lookup bounded and reusable as the NPC roster scales.
 */
function buildPlanarGridIndex(
  entries: readonly ValidNpcEntry[],
  cellSizeMeters: number
): Map<string, ValidNpcEntry[]> {
  const index = new Map<string, ValidNpcEntry[]>();
  const safeCell = Math.max(0.5, cellSizeMeters);

  for (const entry of entries) {
    const cx = Math.floor(entry.position.x / safeCell);
    const cz = Math.floor(entry.position.z / safeCell);
    const key = `${cx}:${cz}`;
    const bucket = index.get(key);
    if (bucket) {
      bucket.push(entry);
    } else {
      index.set(key, [entry]);
    }
  }
  return index;
}

function queryPlanarNeighbors(
  entry: ValidNpcEntry,
  grid: Map<string, ValidNpcEntry[]>,
  cellSizeMeters: number
): ValidNpcEntry[] {
  const safeCell = Math.max(0.5, cellSizeMeters);
  const cx = Math.floor(entry.position.x / safeCell);
  const cz = Math.floor(entry.position.z / safeCell);
  const neighbors: ValidNpcEntry[] = [];

  for (let dx = -1; dx <= 1; dx++) {
    for (let dz = -1; dz <= 1; dz++) {
      const bucket = grid.get(`${cx + dx}:${cz + dz}`);
      if (!bucket) continue;
      for (const candidate of bucket) {
        if (candidate.npcId !== entry.npcId) {
          neighbors.push(candidate);
        }
      }
    }
  }

  return neighbors;
}

/**
 * Pure deterministic evaluator for social and spatial awareness across the canonical NPC roster.
 */
export function evaluateRosterSocialAwareness(
  params: EvaluateRosterSocialAwarenessParams
): NpcRosterSocialAwarenessSnapshot {
  const evaluatedAtIso =
    typeof params.evaluatedAt === 'string'
      ? new Date(params.evaluatedAt).toISOString()
      : new Date(params.evaluatedAt).toISOString();

  const movementRoster =
    params.movementRoster ?? evaluateRosterMovement(NPC_MOVEMENT_PROFILES, evaluatedAtIso);
  const interactionRoster =
    params.interactionRoster ?? evaluateRosterInteractionContext(evaluatedAtIso);

  const validPlayer =
    params.player && isValidCorridorVector3(params.player.position) ? params.player : null;

  // Deterministic ordered roster: canonical 7 first, then any extra sorted IDs if extended later.
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

  const validEntries: ValidNpcEntry[] = [];
  const validEntryById = new Map<string, ValidNpcEntry>();

  for (const npcId of orderedIds) {
    const ctx = interactionRoster.npcs[npcId];
    const mov = movementRoster.npcs[npcId];
    if (!ctx || !mov || !isValidCorridorVector3(mov.position)) {
      continue;
    }
    const socialEligible = isNpcSociallyEligibleForPairing(ctx, mov);
    const canOrient = canNpcOrientTowardTarget(ctx);
    const retainsContextual =
      !canOrient ||
      mov.movementState === 'moving' ||
      ctx.interactionAvailability === 'busy' ||
      isContextualStationOccupiedForSocial(npcId, ctx);

    const entry: ValidNpcEntry = {
      npcId,
      ctx,
      mov,
      position: mov.position,
      socialEligible,
      canOrientTowardTarget: canOrient,
      retainsContextualActivity: retainsContextual,
    };
    validEntries.push(entry);
    validEntryById.set(npcId, entry);
  }

  // Compute spatial grid for bounded neighbor queries (covering max NPC range 4.5m).
  const grid = buildPlanarGridIndex(validEntries, NPC_AWARENESS_RANGE_METERS);

  // Precompute valid player proximity per NPC.
  const playerRelations = new Map<string, NpcAwarenessTargetRelation>();
  if (validPlayer) {
    for (const entry of validEntries) {
      const relation = buildSpatialRelation(entry.position, validPlayer.position);
      playerRelations.set(entry.npcId, relation);
    }
  }

  // Identify NPCs excluded from NPC-to-NPC pairing because an approaching close player is closer
  // than NPC social proximity (prevents asymmetrical one-sided pairing when player walks right up).
  const prefersClosePlayer = new Set<string>();
  if (validPlayer) {
    for (const entry of validEntries) {
      const rel = playerRelations.get(entry.npcId);
      if (
        rel &&
        rel.verticalSeparationMeters <= MAX_AWARENESS_VERTICAL_SEPARATION_METERS &&
        rel.horizontalDistanceMeters <= PLAYER_CLOSE_ATTENTION_RANGE_METERS
      ) {
        // Check whether any eligible NPC partner is strictly closer than the player.
        const neighbors = queryPlanarNeighbors(entry, grid, NPC_AWARENESS_RANGE_METERS);
        let hasStrictlyCloserEligibleNpc = false;
        for (const other of neighbors) {
          if (!other.socialEligible) continue;
          const vSep = evaluateVerticalSeparationMeters(entry.position, other.position);
          if (vSep > MAX_AWARENESS_VERTICAL_SEPARATION_METERS) continue;
          const hDist = evaluateHorizontalDistanceMeters(entry.position, other.position);
          if (hDist <= NPC_SOCIAL_PROXIMITY_RANGE_METERS && hDist < rel.horizontalDistanceMeters) {
            hasStrictlyCloserEligibleNpc = true;
            break;
          }
        }
        if (!hasStrictlyCloserEligibleNpc) {
          prefersClosePlayer.add(entry.npcId);
        }
      }
    }
  }

  // Collect all unordered eligible NPC-to-NPC candidate pairs inside NPC_SOCIAL_PROXIMITY_RANGE_METERS.
  interface CandidateSocialPair {
    readonly a: ValidNpcEntry;
    readonly b: ValidNpcEntry;
    readonly distanceMeters: number;
    readonly minId: string;
    readonly maxId: string;
  }

  const candidatePairs: CandidateSocialPair[] = [];
  for (const entry of validEntries) {
    if (!entry.socialEligible || prefersClosePlayer.has(entry.npcId)) continue;
    const neighbors = queryPlanarNeighbors(entry, grid, NPC_AWARENESS_RANGE_METERS);
    for (const other of neighbors) {
      if (entry.npcId >= other.npcId) continue; // Undirected edge deduplication
      if (!other.socialEligible || prefersClosePlayer.has(other.npcId)) continue;

      const vSep = evaluateVerticalSeparationMeters(entry.position, other.position);
      if (vSep > MAX_AWARENESS_VERTICAL_SEPARATION_METERS) continue;

      const hDist = evaluateHorizontalDistanceMeters(entry.position, other.position);
      if (hDist <= NPC_SOCIAL_PROXIMITY_RANGE_METERS) {
        candidatePairs.push({
          a: entry,
          b: other,
          distanceMeters: hDist,
          minId: entry.npcId,
          maxId: other.npcId,
        });
      }
    }
  }

  // Total deterministic ordering: nearest pair distance first, then canonical NPC IDs.
  candidatePairs.sort((p1, p2) => {
    if (p1.distanceMeters !== p2.distanceMeters) {
      return p1.distanceMeters - p2.distanceMeters;
    }
    const minCmp = p1.minId.localeCompare(p2.minId);
    if (minCmp !== 0) return minCmp;
    return p1.maxId.localeCompare(p2.maxId);
  });

  // Greedy 1-to-1 mutual matching guarantees at most one active social partner per NPC.
  const pairedPartnerByNpcId = new Map<string, ValidNpcEntry>();
  const activeSocialPairs: ActiveNpcSocialPair[] = [];

  for (const pair of candidatePairs) {
    if (pairedPartnerByNpcId.has(pair.a.npcId) || pairedPartnerByNpcId.has(pair.b.npcId)) {
      continue;
    }
    pairedPartnerByNpcId.set(pair.a.npcId, pair.b);
    pairedPartnerByNpcId.set(pair.b.npcId, pair.a);
    activeSocialPairs.push(
      Object.freeze({
        npcIdA: pair.a.npcId,
        npcIdB: pair.b.npcId,
        distanceMeters: pair.distanceMeters,
      })
    );
  }

  // Evaluate each NPC's final authoritative awareness context.
  const npcsMap: Record<string, NpcSocialAwarenessContext> = {};
  const npcsList: NpcSocialAwarenessContext[] = [];

  let unawareCount = 0;
  let playerAwareCount = 0;
  let playerAttentiveCount = 0;
  let npcAwareCount = 0;
  let socialEngagedCount = 0;

  for (const npcId of orderedIds) {
    const entry = validEntryById.get(npcId);
    if (!entry) {
      // Honest safe fallback when authoritative spatial data is unavailable for this ID.
      const fallbackCtx = interactionRoster.npcs[npcId];
      const fallbackMov = movementRoster.npcs[npcId];
      const fallbackClass = getNpcActorClassification(npcId);
      const missingContext = Object.freeze<NpcSocialAwarenessContext>({
        npcId,
        canonicalName: fallbackCtx?.canonicalName ?? npcId,
        actorArchetype: fallbackCtx?.actorArchetype ?? fallbackClass?.actorArchetype ?? 'business_attendant',
        isWitness: fallbackCtx?.isWitness ?? fallbackClass?.isWitness ?? false,
        witnessClassification: fallbackCtx?.witnessClassification ?? fallbackClass?.witnessClassification ?? 'none',
        witnessId: fallbackCtx?.witnessId !== undefined ? fallbackCtx.witnessId : (fallbackClass?.witnessId ?? null),
        awarenessState: 'unaware',
        targetEntityType: 'none',
        targetEntityId: null,
        spatialRelation: null,
        socialEligible: false,
        canOrientTowardTarget: false,
        retainsContextualActivity: true,
        pairedPartnerNpcId: null,
        associatedPlaceId: fallbackCtx?.associatedPlaceId ?? '',
        currentWaypointId: fallbackCtx?.currentWaypointId ?? '',
        interactionAvailability: fallbackCtx?.interactionAvailability ?? 'unavailable',
        movementState: fallbackMov?.movementState ?? 'stationary',
        nearbyPlayerDistanceMeters: null,
        nearbyNpcCount: 0,
        detectionBasis: 'proximity_horizontal_vertical',
        supportsLineOfSight: false,
        reason: Object.freeze({
          en: 'Authoritative spatial coordinates are unavailable; remaining in unaware fallback.',
          es: 'Las coordenadas espaciales autoritativas no están disponibles; permanece sin detección.',
        }),
        evaluatedAt: interactionRoster.evaluatedAt,
        timeZone: interactionRoster.timeZone,
        localCivilTime: interactionRoster.localCivilTime,
        isDemonstrationAwareness: true,
        demonstrationNotice: SIMULATED_AWARENESS_NOTICE,
      });
      npcsMap[npcId] = missingContext;
      npcsList.push(missingContext);
      unawareCount++;
      continue;
    }

    const neighbors = queryPlanarNeighbors(entry, grid, NPC_AWARENESS_RANGE_METERS);
    const nearbyNpcsInRange: Array<{ entry: ValidNpcEntry; relation: NpcAwarenessTargetRelation }> = [];

    for (const other of neighbors) {
      if (!other.canOrientTowardTarget) continue;
      const relation = buildSpatialRelation(entry.position, other.position);
      if (
        relation.verticalSeparationMeters <= MAX_AWARENESS_VERTICAL_SEPARATION_METERS &&
        relation.horizontalDistanceMeters <= NPC_AWARENESS_RANGE_METERS
      ) {
        nearbyNpcsInRange.push({ entry: other, relation });
      }
    }

    nearbyNpcsInRange.sort((a, b) => {
      if (a.relation.horizontalDistanceMeters !== b.relation.horizontalDistanceMeters) {
        return a.relation.horizontalDistanceMeters - b.relation.horizontalDistanceMeters;
      }
      return a.entry.npcId.localeCompare(b.entry.npcId);
    });

    const playerRel = validPlayer ? playerRelations.get(npcId) ?? null : null;
    const playerInVerticalBounds =
      playerRel !== null && playerRel.verticalSeparationMeters <= MAX_AWARENESS_VERTICAL_SEPARATION_METERS;
    const nearbyPlayerDistanceMeters = playerInVerticalBounds && playerRel ? playerRel.horizontalDistanceMeters : null;

    let awarenessState: NpcSocialAwarenessState = 'unaware';
    let targetEntityType: AwarenessEntityType = 'none';
    let targetEntityId: string | null = null;
    let spatialRelation: NpcAwarenessTargetRelation | null = null;
    let pairedPartnerNpcId: string | null = null;
    let reason: BilingualText;

    if (!entry.canOrientTowardTarget) {
      awarenessState = 'unaware';
      reason = Object.freeze({
        en: `${entry.ctx.canonicalName} is currently ${entry.ctx.interactionAvailability.replace('_', ' ')} and does not initiate visual awareness.`,
        es: `${entry.ctx.canonicalName} se encuentra ${entry.ctx.interactionAvailability === 'off_duty' ? 'fuera de turno' : 'no disponible'} y no inicia atención visual.`,
      });
    } else if (pairedPartnerByNpcId.has(npcId)) {
      const partner = pairedPartnerByNpcId.get(npcId)!;
      awarenessState = 'social_engaged';
      targetEntityType = 'npc';
      targetEntityId = partner.npcId;
      pairedPartnerNpcId = partner.npcId;
      spatialRelation = buildSpatialRelation(entry.position, partner.position);
      reason = Object.freeze({
        en: `${entry.ctx.canonicalName} has established calm visual social awareness with nearby ${partner.ctx.canonicalName} (${spatialRelation.horizontalDistanceMeters.toFixed(1)} m).`,
        es: `${entry.ctx.canonicalName} mantiene atención social visual tranquila con ${partner.ctx.canonicalName} a ${spatialRelation.horizontalDistanceMeters.toFixed(1)} m.`,
      });
    } else {
      // Compare eligible candidates: player within 8.0m vs nearest NPC within 4.5m.
      const playerEligible =
        validPlayer !== null &&
        playerRel !== null &&
        playerInVerticalBounds &&
        playerRel.horizontalDistanceMeters <= PLAYER_GENERAL_AWARENESS_RANGE_METERS;

      const nearestNpc = nearbyNpcsInRange.length > 0 ? nearbyNpcsInRange[0]! : null;

      let choosePlayer = false;
      let chooseNpc = false;

      if (playerEligible && nearestNpc) {
        const pDist = playerRel!.horizontalDistanceMeters;
        const nDist = nearestNpc.relation.horizontalDistanceMeters;
        if (pDist < nDist) {
          choosePlayer = true;
        } else if (nDist < pDist) {
          chooseNpc = true;
        } else {
          // Deterministic tie-breaker when horizontal distances are identical: player precedes NPC.
          choosePlayer = true;
        }
      } else if (playerEligible) {
        choosePlayer = true;
      } else if (nearestNpc) {
        chooseNpc = true;
      }

      if (choosePlayer && validPlayer && playerRel) {
        const isClose = playerRel.horizontalDistanceMeters <= PLAYER_CLOSE_ATTENTION_RANGE_METERS;
        awarenessState = isClose ? 'player_attentive' : 'player_aware';
        targetEntityType = 'player';
        targetEntityId = validPlayer.playerId;
        spatialRelation = playerRel;
        reason = isClose
          ? Object.freeze({
              en: `${entry.ctx.canonicalName} notices the approaching player at close range (${playerRel.horizontalDistanceMeters.toFixed(1)} m).`,
              es: `${entry.ctx.canonicalName} nota al jugador acercándose a corta distancia (${playerRel.horizontalDistanceMeters.toFixed(1)} m).`,
            })
          : Object.freeze({
              en: `${entry.ctx.canonicalName} detects the player within corridor awareness range (${playerRel.horizontalDistanceMeters.toFixed(1)} m).`,
              es: `${entry.ctx.canonicalName} detecta al jugador dentro del radio de atención (${playerRel.horizontalDistanceMeters.toFixed(1)} m).`,
            });
      } else if (chooseNpc && nearestNpc) {
        awarenessState = 'npc_aware';
        targetEntityType = 'npc';
        targetEntityId = nearestNpc.entry.npcId;
        spatialRelation = nearestNpc.relation;
        reason = Object.freeze({
          en: `${entry.ctx.canonicalName} notices nearby ${nearestNpc.entry.ctx.canonicalName} (${nearestNpc.relation.horizontalDistanceMeters.toFixed(1)} m) without interrupting current duties.`,
          es: `${entry.ctx.canonicalName} nota la presencia de ${nearestNpc.entry.ctx.canonicalName} (${nearestNpc.relation.horizontalDistanceMeters.toFixed(1)} m) sin interrumpir su actividad actual.`,
        });
      } else {
        awarenessState = 'unaware';
        reason = Object.freeze({
          en: `No nearby player or canonical NPC within awareness range of ${entry.ctx.canonicalName}.`,
          es: `No hay jugador ni personaje canónico cercano dentro del radio de atención de ${entry.ctx.canonicalName}.`,
        });
      }
    }

    switch (awarenessState) {
      case 'player_aware':
        playerAwareCount++;
        break;
      case 'player_attentive':
        playerAttentiveCount++;
        break;
      case 'npc_aware':
        npcAwareCount++;
        break;
      case 'social_engaged':
        socialEngagedCount++;
        break;
      case 'unaware':
      default:
        unawareCount++;
        break;
    }

    const result: NpcSocialAwarenessContext = Object.freeze({
      npcId,
      canonicalName: entry.ctx.canonicalName,
      actorArchetype: entry.ctx.actorArchetype,
      isWitness: entry.ctx.isWitness,
      witnessClassification: entry.ctx.witnessClassification,
      witnessId: entry.ctx.witnessId,
      awarenessState,
      targetEntityType,
      targetEntityId,
      spatialRelation,
      socialEligible: entry.socialEligible,
      canOrientTowardTarget: entry.canOrientTowardTarget,
      retainsContextualActivity: entry.retainsContextualActivity,
      pairedPartnerNpcId,
      associatedPlaceId: entry.ctx.associatedPlaceId,
      currentWaypointId: entry.ctx.currentWaypointId,
      interactionAvailability: entry.ctx.interactionAvailability,
      movementState: entry.mov.movementState,
      nearbyPlayerDistanceMeters,
      nearbyNpcCount: nearbyNpcsInRange.length,
      detectionBasis: 'proximity_horizontal_vertical',
      supportsLineOfSight: false,
      reason,
      evaluatedAt: interactionRoster.evaluatedAt,
      timeZone: interactionRoster.timeZone,
      localCivilTime: interactionRoster.localCivilTime,
      isDemonstrationAwareness: true,
      demonstrationNotice: SIMULATED_AWARENESS_NOTICE,
    });

    npcsMap[npcId] = result;
    npcsList.push(result);
  }

  return Object.freeze({
    evaluatedAt: interactionRoster.evaluatedAt,
    timeZone: interactionRoster.timeZone,
    localCivilTime: interactionRoster.localCivilTime,
    playerId: validPlayer ? validPlayer.playerId : null,
    playerPosition: validPlayer ? validPlayer.position : null,
    detectionBasis: 'proximity_horizontal_vertical',
    supportsLineOfSight: false,
    environmentalAwarenessScope: 'player_and_npc_proximity_only',
    thresholds: SOCIAL_AWARENESS_THRESHOLDS,
    npcs: Object.freeze(npcsMap),
    npcsList: Object.freeze(npcsList),
    activeSocialPairs: Object.freeze(activeSocialPairs),
    summary: Object.freeze({
      unawareCount,
      playerAwareCount,
      playerAttentiveCount,
      npcAwareCount,
      socialEngagedCount,
      activePairCount: activeSocialPairs.length,
      totalCount: npcsList.length,
    }),
    isDemonstrationAwareness: true,
    demonstrationNotice: SIMULATED_AWARENESS_NOTICE,
  });
}

export function evaluateNpcSocialAwareness(
  npcId: string,
  params: EvaluateRosterSocialAwarenessParams
): NpcSocialAwarenessContext | null {
  if (typeof npcId !== 'string' || npcId.trim() === '') {
    return null;
  }
  const roster = evaluateRosterSocialAwareness(params);
  return roster.npcs[npcId.trim()] ?? null;
}
