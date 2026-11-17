import { BusinessId } from '../domain/business-id.js';
import { BusinessSchedule } from '../domain/business-schedule.js';
import { BusinessOperationalStatus } from '../domain/operational-status.js';
import { CitizenReportRecord } from '../domain/citizen-report.js';
import { NpcActivitySnapshot, NpcRosterActivitySnapshot } from '../domain/npc-activity.js';
import { WorldEnvironmentSnapshot } from '../domain/time-of-day.js';
import { NpcMovementSnapshot, NpcRosterMovementSnapshot } from '../domain/npc-waypoint-movement.js';
import { NpcInteractionPauseInterval } from '../domain/npc-autonomous-locomotion.js';
import { NpcInteractionContext, NpcRosterInteractionContext } from '../domain/npc-interaction-context.js';
import {
  NpcRosterSocialAwarenessSnapshot,
  NpcSocialAwarenessContext,
} from '../domain/npc-social-awareness.js';
import {
  NpcRosterSocialInteractionsSnapshot,
  NpcSocialInteractionParticipation,
} from '../domain/npc-social-interaction.js';
import {
  PlayerMovementIntent,
  PlayerMovementStepResult,
  PlayerSpatialState,
} from '../domain/player-movement.js';
import {
  BorderLaneBarriersRoster,
  BorderLaneBarrierSnapshot,
  LaneBarrierId,
  BarrierPhysicalState,
} from '../domain/border-lane-barriers.js';

/**
 * A coherent point-in-time snapshot of business operational data from the World Core.
 * Combines the static schedule configuration and the dynamic operational status
 * evaluated at an authoritative world time instant in a single atomic response.
 */
export interface BusinessScheduleSnapshot {
  readonly businessId: BusinessId;
  readonly businessName: string;
  readonly timeZone: string;
  readonly worldTime: string;
  readonly schedule: BusinessSchedule;
  readonly operationalStatus: BusinessOperationalStatus;
}

/**
 * Read-only boundary port to the World Core.
 *
 * In accordance with the World Core architectural invariant:
 * External services, AI assistants, and clients read authoritative state
 * through dedicated query boundaries and never mutate world state directly.
 */
export interface WorldCorePort {
  /**
   * Retrieves the authoritative business schedule for a given business identifier,
   * or null if the business is not registered.
   */
  getBusinessSchedule(businessId: BusinessId): Promise<BusinessSchedule | null>;

  /**
   * Returns the current authoritative World Time as an ISO-8601 UTC string.
   */
  getCurrentWorldTime(): Promise<string>;

  /**
   * Derives the authoritative operational status for a business at a specified
   * world time instant (or current world time if omitted).
   * Returns null if the business schedule does not exist.
   */
  getBusinessOperationalStatus(
    businessId: BusinessId,
    evaluatedAt?: Date | string | number
  ): Promise<BusinessOperationalStatus | null>;

  /**
   * Retrieves an authoritative, point-in-time snapshot containing both the schedule
   * definition and the operational status from a single coherent World Core read.
   * Returns null if the business schedule does not exist.
   */
  getBusinessScheduleSnapshot(
    businessId: BusinessId,
    evaluatedAt?: Date | string | number
  ): Promise<BusinessScheduleSnapshot | null>;

  /**
   * Authoritatively registers a confirmed citizen issue report in the World Core.
   */
  registerCitizenReport?(report: CitizenReportRecord): Promise<CitizenReportRecord>;

  /**
   * Retrieves a registered citizen issue report by its canonical identifier.
   */
  getCitizenReport?(reportId: string): Promise<CitizenReportRecord | null>;

  /**
   * Lists all registered citizen issue reports.
   */
  listCitizenReports?(filter?: { limit?: number; offset?: number }): Promise<readonly CitizenReportRecord[]>;

  /**
   * Evaluates the authoritative activity state of an NPC at a specified world time.
   * Returns null if the NPC is not recognized.
   */
  getNpcActivity?(
    npcId: string,
    evaluatedAt?: Date | string | number
  ): Promise<NpcActivitySnapshot | null>;

  /**
   * Evaluates the authoritative activity roster of all corridor NPCs at a specified world time.
   */
  getNpcRosterActivity?(
    evaluatedAt?: Date | string | number
  ): Promise<NpcRosterActivitySnapshot>;

  /**
   * Evaluates the authoritative environmental lighting and time-of-day presentation state.
   */
  getWorldEnvironment?(
    evaluatedAt?: Date | string | number
  ): Promise<WorldEnvironmentSnapshot>;

  /**
   * Evaluates the authoritative spatial waypoint movement state of an NPC at a specified world time.
   * Returns null if the NPC is not recognized.
   */
  getNpcMovement?(
    npcId: string,
    evaluatedAt?: Date | string | number
  ): Promise<NpcMovementSnapshot | null>;

  /**
   * Evaluates the authoritative spatial movement state for all corridor NPCs in a single coherent snapshot.
   */
  getNpcRosterMovement?(
    evaluatedAt?: Date | string | number
  ): Promise<NpcRosterMovementSnapshot>;

  /**
   * Evaluates the authoritative interaction context for a canonical NPC at a specified world time.
   * Returns null if the NPC is not recognized.
   */
  getNpcInteractionContext?(
    npcId: string,
    evaluatedAt?: Date | string | number,
    playerId?: string
  ): Promise<NpcInteractionContext | null>;

  /**
   * Evaluates the authoritative interaction context for all corridor NPCs in a single coherent snapshot.
   */
  getNpcRosterInteractionContext?(
    evaluatedAt?: Date | string | number,
    playerId?: string
  ): Promise<NpcRosterInteractionContext>;

  /**
   * Evaluates deterministic social awareness for a canonical NPC grounded in World Core positions.
   * Returns null if the NPC is not recognized.
   */
  getNpcSocialAwareness?(
    npcId: string,
    playerId?: string,
    evaluatedAt?: Date | string | number
  ): Promise<NpcSocialAwarenessContext | null>;

  /**
   * Evaluates deterministic social awareness for all corridor NPCs in a single coherent snapshot.
   */
  getNpcRosterSocialAwareness?(
    playerId?: string,
    evaluatedAt?: Date | string | number
  ): Promise<NpcRosterSocialAwarenessSnapshot>;

  /**
   * Evaluates deterministic autonomous NPC-to-NPC social interaction participation for a single NPC.
   * Returns null if the NPC is not recognized.
   */
  getNpcSocialInteraction?(
    npcId: string,
    playerId?: string,
    evaluatedAt?: Date | string | number,
    activePlayerInteractionNpcId?: string | null
  ): Promise<NpcSocialInteractionParticipation | null>;

  /**
   * Evaluates deterministic autonomous NPC-to-NPC social interaction sessions across the roster.
   */
  getNpcRosterSocialInteractions?(
    playerId?: string,
    evaluatedAt?: Date | string | number,
    activePlayerInteractionNpcId?: string | null
  ): Promise<NpcRosterSocialInteractionsSnapshot>;

  /**
   * Retrieves the authoritative status of all 4 vehicular toll barrier gates at the Paso del Norte bridge entrance.
   */
  getBorderLaneBarriers?(
    evaluatedAt?: Date | string | number
  ): Promise<BorderLaneBarriersRoster>;

  /**
   * Updates the authoritative physical barrier status (open/closed) of a specific vehicular lane.
   */
  setBorderLaneBarrierStatus?(
    laneId: LaneBarrierId,
    status: BarrierPhysicalState,
    reason?: string
  ): Promise<BorderLaneBarrierSnapshot>;

  /**
   * Retrieves the authoritative spatial state of a player in the corridor,
   * or null if the player identifier is not registered.
   */
  getPlayerState?(playerId: string): Promise<PlayerSpatialState | null>;

  /**
   * Applies a normalized movement intent in World Core using server-measured elapsed time,
   * returning the resulting canonical player position and integration metadata.
   */
  applyPlayerMovement?(
    playerId: string,
    movementIntent: PlayerMovementIntent,
    serverTimeMs?: number
  ): Promise<PlayerMovementStepResult>;

  /**
   * Authoritatively suspends an NPC's autonomous patrol or business local roaming
   * when an authorized player-initiated interaction begins (Increment 5.22.0 Subsystem A3).
   */
  suspendNpcMovementForInteraction?(
    playerId: string,
    npcId: string,
    suspendedAt?: Date | string | number
  ): Promise<NpcMovementSnapshot | null>;

  /**
   * Releases an active player-interaction movement suspension and resumes autonomous
   * locomotion from the preserved progression state (Increment 5.22.0 Subsystem A3).
   */
  releaseNpcMovementFromInteraction?(
    playerId: string,
    npcId?: string,
    releasedAt?: Date | string | number
  ): Promise<NpcMovementSnapshot | null>;

  /**
   * Returns recorded interaction pause intervals for deterministic state reconstruction.
   */
  getNpcInteractionPauseIntervals?(
    npcId?: string
  ): Readonly<Record<string, readonly NpcInteractionPauseInterval[]>>;
}
