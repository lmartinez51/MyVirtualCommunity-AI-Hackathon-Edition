/**
 * Autonomous NPC Locomotion, Bridge Patrols & Bounded Business Activity Domain Model (Increment 5.22.0).
 *
 * Core Architectural Invariants:
 * 1. Conceptual Separation of Concerns:
 *    - Character Identity (`npcId`, `canonicalName`) is permanent and independent of temporary roles.
 *    - Actor Archetype (`community_player_proxy`, `business_attendant`, `public_servant`) and orthogonal
 *      Witness status (`isWitness`, `witnessClassification`, `witnessId`) come from `npc-actor-archetypes.ts`.
 *    - Behavioral Profile (`bridge_patrol`, `business_local_autonomy`, `fixed_anchor_scheduled`) is
 *      explicitly configured and decoupled from permanent character identity.
 *    - Movement Ownership & Control (`world_core`) remains strictly server-authoritative.
 * 2. Subsystem A — Community Player Proxy Bridge Patrol (`mateo-student`, `sofia-cross-border-retail`, `hector-artisan`):
 *    - Continuous, C^0-continuous bidirectional walking within verified pedestrian bridge geometry
 *      (`X: [14.35, 16.25]`, `Z: [36.0, 53.8]`, `Y: [0.20, 0.55]`).
 *    - Distinct speeds, pause distributions, observation styles, initial directions, and turnaround timings.
 *    - Pure deterministic hash-based variation seeded by `(npcId, civilDate, profileId, cycleIndex)`
 *      with zero `Math.random()`, `Date.now()`, `setTimeout`, or `setInterval`.
 *    - Never enters `off_duty`.
 * 3. Subsystem A2 — Brief Passing Encounters (`passing_greeting`, `passing_exchange`):
 *    - Triggered only on genuine spatial proximity (<= 3.20m horizontal, <= 3.0m vertical) during compatible
 *      patrol states, gated by deterministic selection and a 45-second pair cooldown, lasting 4-6 seconds
 *      without mutating authoritative positions or permanently stopping patrol progression.
 * 4. Subsystem A3 — Player-Initiated Conversation Suspension:
 *    - Mere player proximity NEVER stops patrol (only triggers head/torso awareness).
 *    - Explicit authorized player interactions suspend patrol/local roaming at the exact current position,
 *      preserving route, direction, and progression state; closing the interaction resumes from the exact
 *      paused progression without jumping forward or restarting the route.
 * 5. Subsystem B — Business Attendant Bounded Local Autonomy (`carlos-farmacia-attendant`, `valeria-optica-attendant`):
 *    - Strict polygon + bounds containment within verified storefront domains (`domain:farmacias-benavides`
 *      and `domain:optica-fashion`). Every candidate destination and route segment is validated.
 *    - Respects work priority: active customer interactions, busy tasks, off-duty hours, and explicit
 *      scheduled break/task transitions take priority over optional local roaming.
 * 6. Excluded Fixed Kiosk NPCs (`elena-border-concierge`, `patricia-citizen-rep`):
 *    - Explicitly excluded from autonomous patrol and local business roaming; their fixed anchors and
 *      scheduled transitions remain 100% unchanged.
 */

import { CivilTime } from './civil-time.js';
import { NPC_ACTOR_CLASSIFICATION_ROSTER } from './npc-actor-archetypes.js';
import { NpcActivitySnapshot } from './npc-activity.js';
import { PlayerSpatialState, isValidCorridorVector3 } from './player-movement.js';
import { BilingualText } from './synthetic-witness.js';

interface Vector3D {
  readonly x: number;
  readonly y: number;
  readonly z: number;
}

export interface Point2DXZ {
  readonly x: number;
  readonly z: number;
}

export type NpcBehaviorProfileType =
  | 'bridge_patrol'
  | 'business_local_autonomy'
  | 'fixed_anchor_scheduled';

export type BridgePatrolBehaviorState =
  | 'patrolling'
  | 'observing'
  | 'paused'
  | 'turning_around'
  | 'passing_encounter'
  | 'player_interaction_paused'
  | 'resuming';

export type BusinessLifecycleState =
  | 'business_open'
  | 'closing_preparation'
  | 'returning_inside_for_close'
  | 'closing_pending'
  | 'off_duty_inside'
  | 'opening_transition';

export type BusinessAutonomyBehaviorState =
  | 'working_stationary'
  | 'local_roaming'
  | 'idle_observation'
  | 'customer_awareness'
  | 'returning_to_station'
  | 'returning_inside_for_close'
  | 'off_duty_inside'
  | 'customer_interaction'
  | 'off_duty';

export type FixedAnchorBehaviorState =
  | 'fixed_station'
  | 'scheduled_transit'
  | 'scheduled_station_task'
  | 'off_duty';

export type NpcAutonomousBehaviorState =
  | BridgePatrolBehaviorState
  | BusinessAutonomyBehaviorState
  | FixedAnchorBehaviorState;

export type NpcMovementControlMode =
  | 'autonomous_bridge_patrol'
  | 'autonomous_business_local'
  | 'scheduled_routine_transition'
  | 'player_interaction_suspended'
  | 'fixed_station_anchor';

export type PatrolDirection = 'northbound' | 'southbound' | 'none';

export type PassingEncounterType = 'passing_greeting' | 'passing_exchange';

export const PASSING_ENCOUNTER_MAX_RANGE_METERS = 3.2;
export const PASSING_ENCOUNTER_MAX_VERTICAL_METERS = 3.0;
export const PASSING_ENCOUNTER_MIN_DURATION_SECONDS = 4;
export const PASSING_ENCOUNTER_MAX_DURATION_SECONDS = 6;
export const PASSING_ENCOUNTER_COOLDOWN_SECONDS = 45;
export const PASSING_ENCOUNTER_SLOT_SECONDS = 60;
export const RESUME_TRANSITION_DURATION_SECONDS = 2.0;
export const BUSINESS_CUSTOMER_AWARENESS_RANGE_METERS = 8.0;

/**
 * Personal-space separation constants (Increment 5.22.1):
 * Derived from BobbleheadPrefabBaker.cs CapsuleCollider radius = 0.35m (0.70m body diameter)
 * and BridgeWalkwayRoofAndTrussBuilder.cs clear interior walkway width = 2.01m (±1.005m between trusses).
 * - 1.05m center-to-center leaves 0.35m clear air gap between 0.35m-radius capsules.
 * - ±0.42m lateral lane offset keeps capsule outer edges (0.42 + 0.35 = 0.77m) strictly inside ±1.005m trusses.
 */
export const MIN_PERSONAL_SPACE_METERS = 1.05;
export const COMFORTABLE_FOLLOW_DISTANCE_METERS = 1.35;
export const WALKWAY_SAFE_HALF_WIDTH_METERS = 0.42;
export const PERSONAL_SPACE_MAX_VERTICAL_METERS = 2.0;

/**
 * Pause interval recorded when an authorized player-initiated interaction suspends an NPC's
 * autonomous locomotion. Pure timestamps allow deterministic reconstruction at any instant T.
 */
export interface NpcInteractionPauseInterval {
  readonly npcId: string;
  readonly playerId: string;
  readonly startedAtMs: number;
  readonly endedAtMs: number | null;
  readonly reason: 'authorized_player_interaction';
}

export interface EvaluateAutonomousMovementOptions {
  readonly pauseIntervalsByNpcId?: Readonly<Record<string, readonly NpcInteractionPauseInterval[]>>;
  readonly player?: PlayerSpatialState | null;
  readonly skipPassingEncounters?: boolean;
}

// ---------------------------------------------------------------------------
// 1. VERIFIED SPATIAL DOMAINS & GEOMETRY PROVENANCE
// ---------------------------------------------------------------------------

export interface SpatialDomainBounds3D {
  readonly minX: number;
  readonly maxX: number;
  readonly minY: number;
  readonly maxY: number;
  readonly minZ: number;
  readonly maxZ: number;
}

export interface AuthorizedBridgePatrolDomain {
  readonly domainId: string;
  readonly associatedPlaceId: string;
  readonly bounds: SpatialDomainBounds3D;
  readonly polygonXZ: readonly Point2DXZ[];
  readonly northboundLaneX: number;
  readonly southboundLaneX: number;
  readonly neutralCenterX: number;
  readonly orderedWaypointIds: readonly string[];
  readonly provenance: BilingualText;
}

export interface BridgeLoopNode {
  readonly id: string;
  readonly corridor: 'east_walkway' | 'north_turnaround' | 'west_walkway' | 'south_crosswalk';
  readonly direction: 'northbound' | 'southbound';
  readonly position: Vector3D;
  readonly waypointId: string;
  readonly safeHalfWidthMeters: number;
}

/**
 * Complete built Paso del Norte pedestrian loop derived directly from Unity scene geometry:
 * 1. East Walkway (Ciudad Juárez -> El Paso):
 *    - `Sidewalk_Plaza_South_Main_Z34_54` (Z=36.5..54.0m, X=15.25m, Y=0.240..0.520m)
 *    - `Sidewalk_Ramp_MainConcourse_Z54_58` & `Sidewalk_Plateau_Bridge_Z58_64` & `Sidewalk_Transition_North_Z64_68`
 *      (X=15.75m through Z=54..58m clearing yellow rail at X=15.05m, then X=15.25m)
 *    - `Walkway_Slab_Seg01..28` (Z=68..350m, curving X=15.25..24.77m, cresting Y=7.058m at Seg15 Z=214.07m)
 *    - `Walkway_Slab_Ramp` & `Walkway_Slab_G01` (Z=350..422.5m, X=25.25m, Y=4.139..0.050m)
 * 2. North Turnaround (El Paso End, Z=422.5m, Y=0.050m):
 *    - Connects East walkway (`X=25.25`) across north apron (`Z=422.5`) to West walkway (`X=2.75`).
 * 3. West Walkway (El Paso -> Ciudad Juárez):
 *    - `WestWalkway_Slab_Ramp` (Z=420..350m, X=2.75m, Y=0.050..4.139m)
 *    - `WestWalkway_Slab_Seg28..01` (mirrored from `Bridge_Deck_SegXX` + `deck.right * -11.25f`,
 *      X=2.35..-7.25m, cresting Y=7.058m at Seg15 Z=214.97m)
 *    - `WestWalkway_Slab_SouthExt_Z51_68` (X=-7.25m, Z=68..54.6m, Y=0.486m) exiting via open 1.60m portal (`Z=53.80..55.40m`).
 * 4. South Crosswalk Turnaround (`BridgeRoadwayRebuilder.cs` zebra crosswalk Z=53.70..55.50m):
 *    - Connects West crosswalk portal (`X=-5.90, Z=54.60`) across crosswalk (`Y=0.412m`) into
 *      East accessible curb cut (`Sidewalk_Ramp_BlueExitLanding_Z54_55`, X=14.50, Z=54.60, Y=0.400m)
 *      and back down `Sidewalk_Plaza_South_Main_Z34_54` (`X=14.98`) to `Z=36.50m`.
 */
export const BRIDGE_FULL_LOOP_NODES: readonly BridgeLoopNode[] = Object.freeze([
  // East South Plaza Approach (Northbound)
  Object.freeze({ id: 'east_south_entry', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 15.42, y: 0.24, z: 36.5 }), waypointId: 'wp_bridge_south_approach', safeHalfWidthMeters: 0.90 }),
  Object.freeze({ id: 'east_toll_plaza', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 15.42, y: 0.288, z: 39.5 }), waypointId: 'wp_bridge_toll_plaza', safeHalfWidthMeters: 0.90 }),
  Object.freeze({ id: 'east_mateo_station', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 15.42, y: 0.328, z: 42.0 }), waypointId: 'wp_mateo_queue_entry', safeHalfWidthMeters: 0.90 }),
  Object.freeze({ id: 'east_sofia_station', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 15.42, y: 0.384, z: 45.5 }), waypointId: 'wp_sofia_queue_mid', safeHalfWidthMeters: 0.90 }),
  Object.freeze({ id: 'east_hector_station', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 15.42, y: 0.440, z: 49.0 }), waypointId: 'wp_hector_stand', safeHalfWidthMeters: 0.90 }),
  Object.freeze({ id: 'east_concourse_z54', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 15.75, y: 0.520, z: 54.2 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.65 }),
  Object.freeze({ id: 'east_plateau_z58', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 15.75, y: 0.520, z: 58.0 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.65 }),
  Object.freeze({ id: 'east_trans_z66', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 15.25, y: 0.482, z: 66.25 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.85 }),
  // East Covered Span Segments 01..28 (exact center top surface Y = slabPos.y + 0.075m)
  Object.freeze({ id: 'east_seg01', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 15.25, y: 0.485, z: 74.02 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'east_seg02', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 15.25, y: 0.628, z: 84.046 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'east_seg03', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 15.25, y: 0.905, z: 94.08 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'east_seg04', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 15.25, y: 1.306, z: 104.108 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'east_seg05', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 15.25, y: 1.815, z: 114.14 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'east_seg06', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 15.25, y: 2.404, z: 124.173 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'east_seg07', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 15.25, y: 3.055, z: 134.21 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'east_seg08', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 15.25, y: 3.733, z: 144.242 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'east_seg09', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 15.25, y: 4.415, z: 154.28 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'east_seg10', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 15.25, y: 5.066, z: 164.316 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'east_seg11', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 15.25, y: 5.665, z: 174.35 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'east_seg12', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 15.314, y: 6.172, z: 184.247 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'east_seg13', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 15.51, y: 6.575, z: 194.15 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'east_seg14', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 15.799, y: 6.862, z: 204.098 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'east_seg15_crest', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 16.16, y: 7.015, z: 214.07 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'east_seg16', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 16.587, y: 7.023, z: 224.048 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'east_seg17', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 17.06, y: 6.965, z: 234.03 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'east_seg18', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 17.583, y: 6.913, z: 244.016 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'east_seg19', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 18.15, y: 6.855, z: 254.01 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'east_seg20', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 18.75, y: 6.802, z: 263.999 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'east_seg21', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 19.39, y: 6.745, z: 274.00 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'east_seg22', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 20.065, y: 6.690, z: 283.993 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'east_seg23', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 20.77, y: 6.635, z: 293.99 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'east_seg24', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 21.515, y: 6.579, z: 303.993 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'east_seg25', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 22.29, y: 6.375, z: 314.01 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'east_seg26', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 23.089, y: 5.748, z: 324.030 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'east_seg27', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 23.92, y: 4.865, z: 334.04 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'east_seg28', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 24.77, y: 4.235, z: 344.02 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  // East North Descent Ramp & El Paso End (exact center top surface Y = 2.085m at Z=385m)
  Object.freeze({ id: 'east_ramp_top', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 25.25, y: 3.920, z: 350.00 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'east_ramp_mid', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 25.25, y: 2.085, z: 385.00 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'east_el_paso_end', corridor: 'east_walkway', direction: 'northbound', position: Object.freeze({ x: 25.25, y: 0.050, z: 422.50 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  // North Turnaround (El Paso Apron, Z=422.50)
  Object.freeze({ id: 'north_turn_center', corridor: 'north_turnaround', direction: 'southbound', position: Object.freeze({ x: 14.00, y: 0.050, z: 422.50 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 1.00 }),
  Object.freeze({ id: 'west_el_paso_start', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: 2.75, y: 0.050, z: 422.50 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  // West North Descent Ramp (Southbound El Paso -> Ciudad Juárez)
  Object.freeze({ id: 'west_ramp_mid', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: 2.75, y: 2.085, z: 385.00 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_ramp_top', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: 2.75, y: 3.920, z: 350.00 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  // West Covered Span Segments 28..01 (exact center top surface Y = slabPos.y + 0.075m)
  Object.freeze({ id: 'west_seg28', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: 2.35, y: 4.235, z: 345.94 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_seg27', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: 1.50, y: 4.865, z: 335.86 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_seg26', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: 0.664, y: 5.747, z: 325.864 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_seg25', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: -0.15, y: 6.375, z: 315.75 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_seg24', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: -0.921, y: 6.579, z: 305.690 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_seg23', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: -1.67, y: 6.635, z: 295.61 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_seg22', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: -2.381, y: 6.690, z: 285.548 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_seg21', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: -3.07, y: 6.745, z: 275.46 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_seg20', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: -3.707, y: 6.802, z: 265.398 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_seg19', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: -4.31, y: 6.855, z: 255.31 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_seg18', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: -4.884, y: 6.913, z: 245.238 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_seg17', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: -5.40, y: 6.965, z: 235.15 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_seg16', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: -5.890, y: 7.023, z: 225.064 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_seg15_crest', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: -6.32, y: 7.015, z: 214.97 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_seg14', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: -6.689, y: 6.862, z: 204.848 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_seg13', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: -6.99, y: 6.575, z: 194.75 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_seg12', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: -7.184, y: 6.173, z: 184.537 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_seg11', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: -7.25, y: 5.665, z: 174.41 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_seg10', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: -7.250, y: 5.066, z: 164.311 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_seg09', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: -7.25, y: 4.415, z: 154.32 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_seg08', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: -7.250, y: 3.733, z: 144.237 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_seg07', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: -7.25, y: 3.055, z: 134.25 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_seg06', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: -7.250, y: 2.403, z: 124.168 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_seg05', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: -7.25, y: 1.815, z: 114.18 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_seg04', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: -7.250, y: 1.306, z: 104.104 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_seg03', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: -7.25, y: 0.905, z: 94.10 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_seg02', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: -7.250, y: 0.628, z: 84.044 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_seg01', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: -7.25, y: 0.485, z: 74.02 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_south_ext_north', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: -7.25, y: 0.486, z: 68.00 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.95 }),
  Object.freeze({ id: 'west_crosswalk_portal', corridor: 'west_walkway', direction: 'southbound', position: Object.freeze({ x: -7.25, y: 0.486, z: 54.60 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.70 }),
  // South Zebra Crosswalk Turnaround (Z=54.60) -> East Curb Cut -> South Plaza Return
  Object.freeze({ id: 'south_crosswalk_west_lip', corridor: 'south_crosswalk', direction: 'southbound', position: Object.freeze({ x: -5.90, y: 0.440, z: 54.60 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.70 }),
  Object.freeze({ id: 'south_crosswalk_center', corridor: 'south_crosswalk', direction: 'southbound', position: Object.freeze({ x: 4.00, y: 0.412, z: 54.60 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.70 }),
  Object.freeze({ id: 'south_crosswalk_east_curb', corridor: 'south_crosswalk', direction: 'southbound', position: Object.freeze({ x: 14.50, y: 0.400, z: 54.60 }), waypointId: 'wp_bridge_north_overlook', safeHalfWidthMeters: 0.50 }),
  Object.freeze({ id: 'east_plaza_return_z52', corridor: 'east_walkway', direction: 'southbound', position: Object.freeze({ x: 14.98, y: 0.488, z: 52.00 }), waypointId: 'wp_hector_bench', safeHalfWidthMeters: 0.85 }),
  Object.freeze({ id: 'east_plaza_return_z42', corridor: 'east_walkway', direction: 'southbound', position: Object.freeze({ x: 14.98, y: 0.328, z: 42.00 }), waypointId: 'wp_mateo_queue_entry', safeHalfWidthMeters: 0.85 }),
  Object.freeze({ id: 'east_plaza_return_z36', corridor: 'east_walkway', direction: 'southbound', position: Object.freeze({ x: 14.98, y: 0.240, z: 36.50 }), waypointId: 'wp_bridge_south_approach', safeHalfWidthMeters: 0.85 }),
]);

/**
 * Verified full-bridge Paso del Norte pedestrian corridor domain derived from:
 * - `EastWalkwayPhase1Builder.cs`: `Sidewalk_Plaza_South_Main_Z34_54`, `Sidewalk_Ramp_MainConcourse_Z54_58`,
 *   `Walkway_Slab_Seg01..28`, `Walkway_Slab_Ramp`, `Walkway_Slab_G01` (to Z=435m).
 * - `WestWalkwayPhase1Builder.cs`: `WestWalkway_Slab_SouthExt_Z51_68`, `WestWalkway_Slab_Seg01..28`,
 *   `WestWalkway_Slab_Ramp`, and open crosswalk portal at Z=53.80..55.40m.
 * - `BridgeRoadwayRebuilder.cs`: Zebra crosswalk at Z=53.70..55.50m (Y=0.412m).
 * - Elena's fixed kiosk (19.20, 0.20, 39.50) and mid-bridge vehicle lanes remain strictly excluded.
 */
export const BRIDGE_PATROL_DOMAIN: AuthorizedBridgePatrolDomain = Object.freeze({
  domainId: 'domain:puente-paso-del-norte-pedestrian-concourse',
  associatedPlaceId: 'infrastructure:puente-paso-del-norte',
  bounds: Object.freeze({
    minX: -8.50,
    maxX: 26.50,
    minY: 0.00,
    maxY: 7.30,
    minZ: 34.0,
    maxZ: 435.0,
  }),
  polygonXZ: Object.freeze([
    Object.freeze({ x: 14.35, z: 36.0 }),
    Object.freeze({ x: 16.25, z: 36.0 }),
    Object.freeze({ x: 16.25, z: 53.8 }),
    Object.freeze({ x: 14.35, z: 53.8 }),
  ]),
  northboundLaneX: 15.42,
  southboundLaneX: 14.98,
  neutralCenterX: 15.20,
  orderedWaypointIds: Object.freeze([
    'wp_bridge_south_approach',
    'wp_bridge_toll_plaza',
    'wp_canopy_waiting',
    'wp_mateo_queue_entry',
    'wp_sofia_queue_mid',
    'wp_hector_stand',
    'wp_hector_bench',
    'wp_bridge_north_overlook',
  ]),
  provenance: Object.freeze({
    en: 'Verified full-bridge pedestrian loop: East Walkway (Z=36.5..422.5m, X=15.25..25.25m, crest Y=7.058m), North El Paso apron turnaround (Z=422.5m), West Walkway (Z=422.5..54.6m, X=2.75..-7.25m), and South Zebra Crosswalk (Z=54.6m). Excludes vehicle roadway lanes and Elena kiosk.',
    es: 'Circuito peatonal completo verificado del Puente Paso del Norte: Andador Oriente (Z=36.5..422.5m, X=15.25..25.25m, cresta Y=7.058m), retorno Norte en El Paso (Z=422.5m), Andador Poniente (Z=422.5..54.6m, X=2.75..-7.25m) y Cruce Peatonal Sur (Z=54.6m). Excluye carriles vehiculares y quiosco de Elena.',
  }),
});

export type BusinessSpatialZoneKind =
  | 'exterior_frontage'
  | 'entrance_threshold'
  | 'main_showroom'
  | 'task_only_room';

export interface BusinessSpatialZone {
  readonly zoneId: string;
  readonly kind: BusinessSpatialZoneKind;
  readonly taskOnly: boolean;
  readonly authorizedTaskIds?: readonly string[];
  readonly bounds: SpatialDomainBounds3D;
  readonly polygonXZ: readonly Point2DXZ[];
  readonly toleranceMeters: number;
  readonly waypointIds: readonly string[];
  readonly provenance: BilingualText;
}

export interface BusinessMovementDomain {
  readonly domainId: string;
  readonly npcId: string;
  readonly associatedPlaceId: string;
  readonly primaryStationWaypointId: string;
  readonly bounds: SpatialDomainBounds3D;
  readonly polygonXZ: readonly Point2DXZ[];
  readonly navigationToleranceMeters: number;
  readonly safeInteriorMarginMeters: number;
  readonly safeRoamingBounds: SpatialDomainBounds3D;
  readonly safeRoamingPolygonXZ: readonly Point2DXZ[];
  readonly safeRoamingToleranceMeters: number;
  readonly authorizedWaypointIds: readonly string[];
  readonly authorizedExternalTaskTransitionIds: readonly string[];
  readonly connectedZones?: readonly BusinessSpatialZone[];
  readonly provenance: BilingualText;
}

/**
 * Verified Farmacias Benavides business movement domain (`carlos-farmacia-attendant`).
 * Derived from `EastSidewalkBlockBuilder.cs` (Module 3 `Benavides_MainBuilding` Z = -3.8m..+5.2m,
 * `Benavides_Awning` Z = -3.45m..+4.85m, facade at X = 12.0m) and `CorridorSceneSetup.cs`
 * (`Sidewalk_East` X = 7.0m..12.0m, curb at X = 6.9m, `ParkingMeter_Dual_Center` max X = 7.39m,
 * `Streetlight_East_BenavidesCenter` max X = 7.415m).
 * Safe interior roaming region insets >= 0.55m (0.35m capsule + 0.20m clearance) from domain edges.
 */
export const FARMACIA_BENAVIDES_AUTONOMY_DOMAIN: BusinessMovementDomain = Object.freeze({
  domainId: 'domain:farmacias-benavides',
  npcId: 'carlos-farmacia-attendant',
  associatedPlaceId: 'pharmacy:farmacias-benavides',
  primaryStationWaypointId: 'wp_carlos_counter',
  bounds: Object.freeze({
    minX: 7.80,
    maxX: 11.40,
    minY: 0.15,
    maxY: 1.05,
    minZ: -3.20,
    maxZ: 4.40,
  }),
  polygonXZ: Object.freeze([
    Object.freeze({ x: 7.80, z: -3.20 }),
    Object.freeze({ x: 11.40, z: -3.20 }),
    Object.freeze({ x: 11.40, z: 4.40 }),
    Object.freeze({ x: 7.80, z: 4.40 }),
  ]),
  navigationToleranceMeters: 0.05,
  safeInteriorMarginMeters: 0.55,
  safeRoamingBounds: Object.freeze({
    minX: 8.40,
    maxX: 10.85,
    minY: 0.15,
    maxY: 1.05,
    minZ: -2.65,
    maxZ: 3.85,
  }),
  safeRoamingPolygonXZ: Object.freeze([
    Object.freeze({ x: 8.40, z: -2.65 }),
    Object.freeze({ x: 10.85, z: -2.65 }),
    Object.freeze({ x: 10.85, z: 3.85 }),
    Object.freeze({ x: 8.40, z: 3.85 }),
  ]),
  safeRoamingToleranceMeters: 0.01,
  authorizedWaypointIds: Object.freeze([
    'wp_carlos_counter',
    'wp_farmacia_front_threshold',
    'wp_farmacia_display_north',
    'wp_farmacia_window_south',
    'wp_farmacia_consultorio_south',
    'wp_farmacia_storefront',
  ]),
  authorizedExternalTaskTransitionIds: Object.freeze([]),
  provenance: Object.freeze({
    en: 'Verified against EastSidewalkBlockBuilder.cs (Benavides_MainBuilding Z=-3.8..+5.2m, facade X=12.0m) and Sidewalk_East (X=7.0..12.0m, curb X=6.9m, ParkingMeter/Pole max X=7.415m). Safe roaming inset [8.40, 10.85] x [-2.65, 3.85] maintains >=0.55m clearance from all domain edges.',
    es: 'Verificado contra EastSidewalkBlockBuilder.cs (Benavides_MainBuilding Z=-3.8..+5.2m, fachada X=12.0m) y Sidewalk_East (X=7.0..12.0m, guarnición X=6.9m, parquímetro/poste máx X=7.415m). Zona interior segura [8.40, 10.85] x [-2.65, 3.85] mantiene >=0.55m de holgura respecto a todo el perímetro.',
  }),
});

/**
 * Verified Óptica Fashion multi-zone spatial topology (`valeria-optica-attendant`, Increment 5.23.1).
 * Derived directly from Increment 5.23.0 `OpticaFashionInteriorBuilder.cs` and `WestSidewalkBlockBuilder.cs`:
 * - Zone A (`zone:optica-exterior-frontage`): Preserved Bay 4 exterior frontage on Sidewalk_West
 *   (`X = -11.30..-8.00m, Z = -1.65..+2.90m`; legacy exterior safeRoamingBounds `[-10.85, -8.00] x [-1.65, 2.90]`).
 * - Zone B (`zone:optica-front-entrance`): Narrow verified passage centered on the 1.087m open inward-swinging
 *   north storefront door (`X = -13.35..-11.15m, Z = +1.02..+1.38m`, >=0.35m capsule clearance from south mullion
 *   at Z=+0.660m and open north door leaf at Z=+1.747m).
 * - Zone C (`zone:optica-main-showroom`): Strictly convex 5-vertex clear showroom circulation polygon
 *   (`X = -15.95..-13.20m, Z = +0.30..+1.92m` chamfered around the open door leaf swing), maintaining >=0.38m
 *   clearance from the long south glass display counter (Z=-0.21m), north waiting chairs (Z=+2.31m),
 *   north eyewear shelving (Z=+2.81m), and rear burgundy cabinet (X=-16.34m).
 * - Zone D1 (`zone:optica-rear-doorway`, task-only): Verified passage through the 1.22m open rear doorway
 *   (`X = -17.95..-15.70m, Z = +1.72..+2.28m`, authorized ONLY for `valeria_calibration_walk`).
 * - Zone D2 (`zone:optica-rear-calibration-room`, task-only): Rear consultation/calibration standing corridor
 *   (`X = -18.02..-17.35m, Z = -1.25..+2.30m`, clear of workbench at X=-18.42m and rear partition at X=-16.96m,
 *   containing verified `Optica_CalibrationStation` at (-17.85, 0.20, -0.50), authorized ONLY for `valeria_calibration_walk`).
 */
export const OPTICA_EXTERIOR_FRONTAGE_ZONE: BusinessSpatialZone = Object.freeze({
  zoneId: 'zone:optica-exterior-frontage',
  kind: 'exterior_frontage',
  taskOnly: false,
  bounds: Object.freeze({
    minX: -11.30,
    maxX: -8.00,
    minY: 0.15,
    maxY: 1.05,
    minZ: -1.65,
    maxZ: 2.90,
  }),
  polygonXZ: Object.freeze([
    Object.freeze({ x: -11.30, z: -1.65 }),
    Object.freeze({ x: -8.00, z: -1.65 }),
    Object.freeze({ x: -8.00, z: 2.90 }),
    Object.freeze({ x: -11.30, z: 2.90 }),
  ]),
  toleranceMeters: 0.02,
  waypointIds: Object.freeze([
    'wp_valeria_storefront',
    'wp_optica_north_window',
    'wp_optica_plaza_threshold',
    'wp_optica_south_promenade',
    'wp_optica_south_window',
    'wp_optica_door_approach',
  ]),
  provenance: Object.freeze({
    en: 'Zone A: Verified Óptica Fashion Bay 4 exterior sidewalk frontage (X=-11.30..-8.00m, Z=-1.65..+2.90m).',
    es: 'Zona A: Frente exterior verificado de Óptica Fashion en acera poniente (X=-11.30..-8.00m, Z=-1.65..+2.90m).',
  }),
});

export const OPTICA_FRONT_ENTRANCE_ZONE: BusinessSpatialZone = Object.freeze({
  zoneId: 'zone:optica-front-entrance',
  kind: 'entrance_threshold',
  taskOnly: false,
  bounds: Object.freeze({
    minX: -13.35,
    maxX: -11.15,
    minY: 0.15,
    maxY: 1.05,
    minZ: 1.02,
    maxZ: 1.38,
  }),
  polygonXZ: Object.freeze([
    Object.freeze({ x: -13.35, z: 1.02 }),
    Object.freeze({ x: -11.15, z: 1.02 }),
    Object.freeze({ x: -11.15, z: 1.38 }),
    Object.freeze({ x: -13.35, z: 1.38 }),
  ]),
  toleranceMeters: 0.02,
  waypointIds: Object.freeze([
    'wp_optica_door_approach',
    'wp_optica_door_threshold',
    'wp_optica_interior_entry',
  ]),
  provenance: Object.freeze({
    en: 'Zone B: Open inward-swinging north storefront doorway passage (1.087m clear Z=+0.660..+1.747m, centered Z=+1.20m).',
    es: 'Zona B: Acceso por puerta norte abatible hacia adentro (claro libre 1.087m Z=+0.660..+1.747m, centrado en Z=+1.20m).',
  }),
});

export const OPTICA_MAIN_SHOWROOM_ZONE: BusinessSpatialZone = Object.freeze({
  zoneId: 'zone:optica-main-showroom',
  kind: 'main_showroom',
  taskOnly: false,
  bounds: Object.freeze({
    minX: -15.95,
    maxX: -13.20,
    minY: 0.15,
    maxY: 1.05,
    minZ: 0.30,
    maxZ: 1.92,
  }),
  polygonXZ: Object.freeze([
    Object.freeze({ x: -15.95, z: 0.30 }),
    Object.freeze({ x: -13.20, z: 0.30 }),
    Object.freeze({ x: -13.20, z: 1.38 }),
    Object.freeze({ x: -13.70, z: 1.92 }),
    Object.freeze({ x: -15.95, z: 1.92 }),
  ]),
  toleranceMeters: 0.02,
  waypointIds: Object.freeze([
    'wp_optica_interior_entry',
    'wp_optica_showroom_counter_east',
    'wp_optica_showroom_center',
    'wp_optica_showroom_counter_west',
    'wp_optica_showroom_waiting',
    'wp_optica_showroom_north_display',
    'wp_valeria_off_duty_station',
  ]),
  provenance: Object.freeze({
    en: 'Zone C: Convex main showroom circulation area clear of south glass counter (Z=-0.21m), north waiting chairs (Z=+2.31m), north shelving, rear burgundy cabinet (X=-16.34m), and open door leaf.',
    es: 'Zona C: Área convexa de circulación del showroom principal libre del mostrador sur (Z=-0.21m), sillas de espera norte (Z=+2.31m), estantería norte, gabinete borgoña trasero (X=-16.34m) y hoja abierta.',
  }),
});

export const OPTICA_REAR_DOORWAY_ZONE: BusinessSpatialZone = Object.freeze({
  zoneId: 'zone:optica-rear-doorway',
  kind: 'task_only_room',
  taskOnly: true,
  authorizedTaskIds: Object.freeze(['valeria_calibration_walk']),
  bounds: Object.freeze({
    minX: -17.95,
    maxX: -15.70,
    minY: 0.15,
    maxY: 1.05,
    minZ: 1.72,
    maxZ: 2.28,
  }),
  polygonXZ: Object.freeze([
    Object.freeze({ x: -17.95, z: 1.72 }),
    Object.freeze({ x: -15.70, z: 1.72 }),
    Object.freeze({ x: -15.70, z: 2.28 }),
    Object.freeze({ x: -17.95, z: 2.28 }),
  ]),
  toleranceMeters: 0.02,
  waypointIds: Object.freeze([
    'wp_optica_showroom_north_display',
    'wp_optica_rear_door_threshold',
    'wp_optica_rear_room_north',
  ]),
  provenance: Object.freeze({
    en: 'Zone D1 (Task-Only): Open rear consultation doorway passage (1.22m clear Z=+1.35..+2.65m, centered Z=+2.00m).',
    es: 'Zona D1 (Solo tarea): Paso de puerta abierta al laboratorio trasero (claro libre 1.22m Z=+1.35..+2.65m, centrado en Z=+2.00m).',
  }),
});

export const OPTICA_REAR_CALIBRATION_ROOM_ZONE: BusinessSpatialZone = Object.freeze({
  zoneId: 'zone:optica-rear-calibration-room',
  kind: 'task_only_room',
  taskOnly: true,
  authorizedTaskIds: Object.freeze(['valeria_calibration_walk']),
  bounds: Object.freeze({
    minX: -18.02,
    maxX: -17.35,
    minY: 0.15,
    maxY: 1.05,
    minZ: -1.25,
    maxZ: 2.30,
  }),
  polygonXZ: Object.freeze([
    Object.freeze({ x: -18.02, z: -1.25 }),
    Object.freeze({ x: -17.35, z: -1.25 }),
    Object.freeze({ x: -17.35, z: 2.30 }),
    Object.freeze({ x: -18.02, z: 2.30 }),
  ]),
  toleranceMeters: 0.02,
  waypointIds: Object.freeze([
    'wp_optica_rear_room_north',
    'wp_optica_calibration_station',
  ]),
  provenance: Object.freeze({
    en: 'Zone D2 (Task-Only): Rear calibration room corridor clear of workbench (X=-18.42m) and rear partition (X=-16.96m), containing Optica_CalibrationStation (-17.85, 0.20, -0.50).',
    es: 'Zona D2 (Solo tarea): Corredor del cuarto trasero de calibración libre de mesa de trabajo (X=-18.42m) y muro divisorio (X=-16.96m), con Optica_CalibrationStation (-17.85, 0.20, -0.50).',
  }),
});

export const OPTICA_FASHION_CONNECTED_ZONES: readonly BusinessSpatialZone[] = Object.freeze([
  OPTICA_EXTERIOR_FRONTAGE_ZONE,
  OPTICA_FRONT_ENTRANCE_ZONE,
  OPTICA_MAIN_SHOWROOM_ZONE,
  OPTICA_REAR_DOORWAY_ZONE,
  OPTICA_REAR_CALIBRATION_ROOM_ZONE,
]);

/**
 * Verified Óptica Fashion business movement domain (`valeria-optica-attendant`).
 * Preserves exact 5.22.3 exterior bounds/safeRoamingBounds (Zone A) and adds explicit connected
 * interior zones (`connectedZones`) for the entrance threshold (Zone B), main showroom (Zone C),
 * and task-only rear calibration laboratory (Zones D1/D2).
 */
export const OPTICA_FASHION_AUTONOMY_DOMAIN: BusinessMovementDomain = Object.freeze({
  domainId: 'domain:optica-fashion',
  npcId: 'valeria-optica-attendant',
  associatedPlaceId: 'optical:optica-fashion',
  primaryStationWaypointId: 'wp_valeria_storefront',
  bounds: Object.freeze({
    minX: -11.40,
    maxX: -7.40,
    minY: 0.15,
    maxY: 1.05,
    minZ: -4.50,
    maxZ: 8.20,
  }),
  polygonXZ: Object.freeze([
    Object.freeze({ x: -11.40, z: -4.50 }),
    Object.freeze({ x: -7.40, z: -4.50 }),
    Object.freeze({ x: -7.40, z: 8.20 }),
    Object.freeze({ x: -11.40, z: 8.20 }),
  ]),
  navigationToleranceMeters: 0.05,
  safeInteriorMarginMeters: 0.55,
  safeRoamingBounds: Object.freeze({
    minX: -10.85,
    maxX: -8.00,
    minY: 0.15,
    maxY: 1.05,
    minZ: -1.65,
    maxZ: 2.90,
  }),
  safeRoamingPolygonXZ: Object.freeze([
    Object.freeze({ x: -10.85, z: -1.65 }),
    Object.freeze({ x: -8.00, z: -1.65 }),
    Object.freeze({ x: -8.00, z: 2.90 }),
    Object.freeze({ x: -10.85, z: 2.90 }),
  ]),
  safeRoamingToleranceMeters: 0.01,
  authorizedWaypointIds: Object.freeze([
    'wp_valeria_storefront',
    'wp_optica_north_window',
    'wp_optica_plaza_threshold',
    'wp_optica_south_promenade',
    'wp_optica_south_window',
    'wp_optica_door_approach',
    'wp_optica_door_threshold',
    'wp_optica_interior_entry',
    'wp_optica_showroom_counter_east',
    'wp_optica_showroom_center',
    'wp_optica_showroom_counter_west',
    'wp_optica_showroom_waiting',
    'wp_optica_showroom_north_display',
    'wp_valeria_off_duty_station',
  ]),
  authorizedExternalTaskTransitionIds: Object.freeze(['valeria_calibration_walk']),
  connectedZones: OPTICA_FASHION_CONNECTED_ZONES,
  provenance: Object.freeze({
    en: 'Verified against WestSidewalkBlockBuilder.cs and OpticaFashionInteriorBuilder.cs (Increment 5.23.0): Zone A exterior frontage [-11.30,-8.00]x[-1.65,2.90], Zone B open north door threshold [-13.35,-11.15]x[1.02,1.38], Zone C main showroom [-15.95,-13.20]x[0.30,1.92] chamfered clear of door swing/furniture, and task-only Zones D1/D2 rear doorway & calibration room (-17.85, 0.20, -0.50).',
    es: 'Verificado contra WestSidewalkBlockBuilder.cs y OpticaFashionInteriorBuilder.cs (Incremento 5.23.0): Zona A frente exterior [-11.30,-8.00]x[-1.65,2.90], Zona B umbral puerta norte abierta [-13.35,-11.15]x[1.02,1.38], Zona C showroom principal [-15.95,-13.20]x[0.30,1.92] libre de mobiliario/hoja, y Zonas D1/D2 de solo tarea hacia estación de calibración (-17.85, 0.20, -0.50).',
  }),
});

export const BUSINESS_AUTONOMY_DOMAINS: Readonly<Record<string, BusinessMovementDomain>> = Object.freeze({
  'carlos-farmacia-attendant': FARMACIA_BENAVIDES_AUTONOMY_DOMAIN,
  'valeria-optica-attendant': OPTICA_FASHION_AUTONOMY_DOMAIN,
});

// ---------------------------------------------------------------------------
// 2. INDEPENDENT CHARACTER BEHAVIORAL PROFILES (SECTION 3, 4.2, 7.2, 11)
// ---------------------------------------------------------------------------

export interface BridgePatrolBehaviorProfile {
  readonly profileId: string;
  readonly npcId: string;
  readonly canonicalName: string;
  readonly behaviorProfileType: 'bridge_patrol';
  readonly primaryWaypointId: string;
  readonly preferredSpeedMps: number;
  readonly minPauseSeconds: number;
  readonly maxPauseSeconds: number;
  readonly turnaroundPauseSeconds: { readonly min: number; readonly max: number };
  readonly initialDirection: 'northbound' | 'southbound';
  readonly initialLoopPhase: number;
  readonly lateralLaneOffsetMeters: number;
  readonly nominalCycleDurationSeconds: number;
  readonly observationFocus: BilingualText;
  readonly encounterEligible: boolean;
}

export interface BusinessLocalNodeSpec {
  readonly waypointId: string;
  readonly connectedWaypointIds: readonly string[];
  readonly observationHeading: { readonly x: number; readonly z: number };
  readonly isPrimaryStation?: boolean;
  readonly isTransitOnly?: boolean;
  readonly isOffDutyStation?: boolean;
  readonly zoneId?: string;
  readonly taskOnly?: boolean;
}

export interface BusinessLocalAutonomyProfile {
  readonly profileId: string;
  readonly npcId: string;
  readonly canonicalName: string;
  readonly behaviorProfileType: 'business_local_autonomy';
  readonly domainId: string;
  readonly primaryWaypointId: string;
  readonly preferredSpeedMps: number;
  readonly workStationDwellSeconds: { readonly min: number; readonly max: number };
  readonly localInspectionDwellSeconds: { readonly min: number; readonly max: number };
  readonly nominalCycleDurationSeconds: number;
  readonly secondaryWorkstationIds: readonly string[];
  readonly localNodes: Readonly<Record<string, BusinessLocalNodeSpec>>;
}

export interface FixedAnchorScheduledProfile {
  readonly profileId: string;
  readonly npcId: string;
  readonly canonicalName: string;
  readonly behaviorProfileType: 'fixed_anchor_scheduled';
  readonly primaryWaypointId: string;
  readonly exclusionReason: BilingualText;
}

export type NpcBehavioralAutonomyProfile =
  | BridgePatrolBehaviorProfile
  | BusinessLocalAutonomyProfile
  | FixedAnchorScheduledProfile;

export function resolveAuthoritativeCanonicalName(npcId: string): string {
  const entry = NPC_ACTOR_CLASSIFICATION_ROSTER[npcId];
  if (!entry) {
    throw new Error(`Unknown canonical NPC ID "${npcId}" in NPC_ACTOR_CLASSIFICATION_ROSTER.`);
  }
  return entry.canonicalName;
}

export const BRIDGE_PATROL_PROFILES: Readonly<Record<string, BridgePatrolBehaviorProfile>> = Object.freeze({
  'mateo-student': Object.freeze({
    profileId: 'patrol:mateo-student-bridge',
    npcId: 'mateo-student',
    canonicalName: resolveAuthoritativeCanonicalName('mateo-student'),
    behaviorProfileType: 'bridge_patrol',
    primaryWaypointId: 'wp_mateo_queue_entry',
    preferredSpeedMps: 3.45, // Steady ordinary pedestrian pace (~76.7% of player 4.5 m/s walkSpeed)
    minPauseSeconds: 3,
    maxPauseSeconds: 6,
    turnaroundPauseSeconds: Object.freeze({ min: 2, max: 4 }),
    initialDirection: 'northbound',
    initialLoopPhase: 0.02, // Starts on East South Approach (northbound Ciudad Juárez -> El Paso)
    lateralLaneOffsetMeters: 0.30,
    nominalCycleDurationSeconds: 290,
    observationFocus: Object.freeze({
      en: 'Traversing the Paso del Norte pedestrian bridge and observing crossing flow',
      es: 'Recorriendo el andador peatonal del Puente Paso del Norte y observando el flujo',
    }),
    encounterEligible: true,
  }),

  'sofia-cross-border-retail': Object.freeze({
    profileId: 'patrol:sofia-retail-bridge',
    npcId: 'sofia-cross-border-retail',
    canonicalName: resolveAuthoritativeCanonicalName('sofia-cross-border-retail'),
    behaviorProfileType: 'bridge_patrol',
    primaryWaypointId: 'wp_sofia_queue_mid',
    preferredSpeedMps: 3.85, // Brisk, purposeful commuter pace (~85.6% of player 4.5 m/s walkSpeed)
    minPauseSeconds: 2,
    maxPauseSeconds: 5,
    turnaroundPauseSeconds: Object.freeze({ min: 2, max: 3 }),
    initialDirection: 'southbound',
    initialLoopPhase: 0.62, // Starts on West Walkway (southbound El Paso -> Ciudad Juárez)
    lateralLaneOffsetMeters: -0.32,
    nominalCycleDurationSeconds: 255,
    observationFocus: Object.freeze({
      en: 'Crossing the Paso del Norte pedestrian walkway at a brisk commuter pace',
      es: 'Cruzando el andador peatonal del Puente Paso del Norte a paso ágil',
    }),
    encounterEligible: true,
  }),

  'hector-artisan': Object.freeze({
    profileId: 'patrol:hector-artisan-bridge',
    npcId: 'hector-artisan',
    canonicalName: resolveAuthoritativeCanonicalName('hector-artisan'),
    behaviorProfileType: 'bridge_patrol',
    primaryWaypointId: 'wp_hector_stand',
    preferredSpeedMps: 2.85, // Relaxed, credible walking pace (~63.3% of player 4.5 m/s walkSpeed)
    minPauseSeconds: 4,
    maxPauseSeconds: 8,
    turnaroundPauseSeconds: Object.freeze({ min: 3, max: 5 }),
    initialDirection: 'northbound',
    initialLoopPhase: 0.28, // Starts on East Walkway mid-span (northbound toward crest)
    lateralLaneOffsetMeters: -0.22,
    nominalCycleDurationSeconds: 350,
    observationFocus: Object.freeze({
      en: 'Walking at a relaxed artisan pace across the Paso del Norte pedestrian bridge',
      es: 'Caminando a paso tranquilo de artesano por todo el andador del Puente Paso del Norte',
    }),
    encounterEligible: true,
  }),
});

export const BUSINESS_LOCAL_AUTONOMY_PROFILES: Readonly<Record<string, BusinessLocalAutonomyProfile>> = Object.freeze({
  'carlos-farmacia-attendant': Object.freeze({
    profileId: 'autonomy:carlos-farmacias-benavides',
    npcId: 'carlos-farmacia-attendant',
    canonicalName: resolveAuthoritativeCanonicalName('carlos-farmacia-attendant'),
    behaviorProfileType: 'business_local_autonomy',
    domainId: 'domain:farmacias-benavides',
    primaryWaypointId: 'wp_carlos_counter',
    preferredSpeedMps: 1.25,
    workStationDwellSeconds: Object.freeze({ min: 10, max: 18 }),
    localInspectionDwellSeconds: Object.freeze({ min: 5, max: 8 }),
    nominalCycleDurationSeconds: 70,
    secondaryWorkstationIds: Object.freeze([
      'wp_farmacia_display_north',
      'wp_farmacia_front_threshold',
      'wp_farmacia_window_south',
      'wp_farmacia_consultorio_south',
    ]),
    localNodes: Object.freeze({
      wp_carlos_counter: Object.freeze({
        waypointId: 'wp_carlos_counter',
        connectedWaypointIds: Object.freeze([
          'wp_farmacia_display_north',
          'wp_farmacia_front_threshold',
          'wp_farmacia_window_south',
          'wp_farmacia_consultorio_south',
        ]),
        observationHeading: Object.freeze({ x: -1, z: 0 }),
        isPrimaryStation: true,
      }),
      wp_farmacia_display_north: Object.freeze({
        waypointId: 'wp_farmacia_display_north',
        connectedWaypointIds: Object.freeze([
          'wp_carlos_counter',
          'wp_farmacia_front_threshold',
        ]),
        observationHeading: Object.freeze({ x: 1, z: 0 }),
      }),
      wp_farmacia_front_threshold: Object.freeze({
        waypointId: 'wp_farmacia_front_threshold',
        connectedWaypointIds: Object.freeze([
          'wp_carlos_counter',
          'wp_farmacia_display_north',
          'wp_farmacia_window_south',
        ]),
        observationHeading: Object.freeze({ x: 0.707, z: 0.707 }),
      }),
      wp_farmacia_window_south: Object.freeze({
        waypointId: 'wp_farmacia_window_south',
        connectedWaypointIds: Object.freeze([
          'wp_carlos_counter',
          'wp_farmacia_front_threshold',
          'wp_farmacia_consultorio_south',
        ]),
        observationHeading: Object.freeze({ x: 0.894, z: 0.447 }),
      }),
      wp_farmacia_consultorio_south: Object.freeze({
        waypointId: 'wp_farmacia_consultorio_south',
        connectedWaypointIds: Object.freeze([
          'wp_carlos_counter',
          'wp_farmacia_window_south',
        ]),
        observationHeading: Object.freeze({ x: 1, z: 0 }),
      }),
    }),
  }),

  'valeria-optica-attendant': Object.freeze({
    profileId: 'autonomy:valeria-optica-fashion',
    npcId: 'valeria-optica-attendant',
    canonicalName: resolveAuthoritativeCanonicalName('valeria-optica-attendant'),
    behaviorProfileType: 'business_local_autonomy',
    domainId: 'domain:optica-fashion',
    primaryWaypointId: 'wp_valeria_storefront',
    preferredSpeedMps: 1.10,
    workStationDwellSeconds: Object.freeze({ min: 11, max: 19 }),
    localInspectionDwellSeconds: Object.freeze({ min: 5, max: 8 }),
    nominalCycleDurationSeconds: 85,
    secondaryWorkstationIds: Object.freeze([
      // Zone A — Exterior business frontage
      'wp_optica_north_window',
      'wp_optica_plaza_threshold',
      'wp_optica_south_promenade',
      'wp_optica_south_window',
      // Zone C — Main showroom interior (reached through Zone B open north door)
      'wp_optica_showroom_counter_east',
      'wp_optica_showroom_center',
      'wp_optica_showroom_counter_west',
      'wp_optica_showroom_waiting',
      'wp_optica_showroom_north_display',
    ]),
    localNodes: Object.freeze({
      // Zone A — Exterior business frontage
      wp_valeria_storefront: Object.freeze({
        waypointId: 'wp_valeria_storefront',
        connectedWaypointIds: Object.freeze([
          'wp_optica_north_window',
          'wp_optica_plaza_threshold',
          'wp_optica_south_window',
          'wp_optica_door_approach',
        ]),
        observationHeading: Object.freeze({ x: 1, z: 0 }),
        isPrimaryStation: true,
        zoneId: 'zone:optica-exterior-frontage',
      }),
      wp_optica_north_window: Object.freeze({
        waypointId: 'wp_optica_north_window',
        connectedWaypointIds: Object.freeze([
          'wp_valeria_storefront',
          'wp_optica_plaza_threshold',
        ]),
        observationHeading: Object.freeze({ x: -1, z: 0 }),
        zoneId: 'zone:optica-exterior-frontage',
      }),
      wp_optica_plaza_threshold: Object.freeze({
        waypointId: 'wp_optica_plaza_threshold',
        connectedWaypointIds: Object.freeze([
          'wp_valeria_storefront',
          'wp_optica_north_window',
          'wp_optica_south_promenade',
        ]),
        observationHeading: Object.freeze({ x: -1, z: 0 }),
        zoneId: 'zone:optica-exterior-frontage',
      }),
      wp_optica_south_promenade: Object.freeze({
        waypointId: 'wp_optica_south_promenade',
        connectedWaypointIds: Object.freeze([
          'wp_optica_plaza_threshold',
          'wp_optica_south_window',
        ]),
        observationHeading: Object.freeze({ x: -0.894, z: 0.447 }),
        zoneId: 'zone:optica-exterior-frontage',
      }),
      wp_optica_south_window: Object.freeze({
        waypointId: 'wp_optica_south_window',
        connectedWaypointIds: Object.freeze([
          'wp_valeria_storefront',
          'wp_optica_south_promenade',
        ]),
        observationHeading: Object.freeze({ x: -1, z: 0 }),
        zoneId: 'zone:optica-exterior-frontage',
      }),

      // Zone B — Front entrance & threshold (transit passage through open 1.087m north door)
      wp_optica_door_approach: Object.freeze({
        waypointId: 'wp_optica_door_approach',
        connectedWaypointIds: Object.freeze([
          'wp_valeria_storefront',
          'wp_optica_door_threshold',
        ]),
        observationHeading: Object.freeze({ x: -1, z: 0 }),
        isTransitOnly: true,
        zoneId: 'zone:optica-front-entrance',
      }),
      wp_optica_door_threshold: Object.freeze({
        waypointId: 'wp_optica_door_threshold',
        connectedWaypointIds: Object.freeze([
          'wp_optica_door_approach',
          'wp_optica_interior_entry',
        ]),
        observationHeading: Object.freeze({ x: -1, z: 0 }),
        isTransitOnly: true,
        zoneId: 'zone:optica-front-entrance',
      }),
      wp_optica_interior_entry: Object.freeze({
        waypointId: 'wp_optica_interior_entry',
        connectedWaypointIds: Object.freeze([
          'wp_optica_door_threshold',
          'wp_optica_showroom_counter_east',
          'wp_optica_showroom_center',
          'wp_optica_showroom_waiting',
        ]),
        observationHeading: Object.freeze({ x: -1, z: 0 }),
        isTransitOnly: true,
        zoneId: 'zone:optica-main-showroom',
      }),

      // Zone C — Main showroom circulation nodes
      wp_optica_showroom_counter_east: Object.freeze({
        waypointId: 'wp_optica_showroom_counter_east',
        connectedWaypointIds: Object.freeze([
          'wp_optica_interior_entry',
          'wp_optica_showroom_center',
          'wp_optica_showroom_counter_west',
        ]),
        observationHeading: Object.freeze({ x: 0, z: -1 }),
        zoneId: 'zone:optica-main-showroom',
      }),
      wp_optica_showroom_center: Object.freeze({
        waypointId: 'wp_optica_showroom_center',
        connectedWaypointIds: Object.freeze([
          'wp_optica_interior_entry',
          'wp_optica_showroom_counter_east',
          'wp_optica_showroom_counter_west',
          'wp_optica_showroom_waiting',
          'wp_optica_showroom_north_display',
          'wp_valeria_off_duty_station',
        ]),
        observationHeading: Object.freeze({ x: 0, z: -1 }),
        zoneId: 'zone:optica-main-showroom',
      }),
      wp_optica_showroom_counter_west: Object.freeze({
        waypointId: 'wp_optica_showroom_counter_west',
        connectedWaypointIds: Object.freeze([
          'wp_optica_showroom_counter_east',
          'wp_optica_showroom_center',
          'wp_optica_showroom_north_display',
          'wp_valeria_off_duty_station',
        ]),
        observationHeading: Object.freeze({ x: -0.707, z: -0.707 }),
        zoneId: 'zone:optica-main-showroom',
      }),
      wp_optica_showroom_waiting: Object.freeze({
        waypointId: 'wp_optica_showroom_waiting',
        connectedWaypointIds: Object.freeze([
          'wp_optica_interior_entry',
          'wp_optica_showroom_center',
          'wp_optica_showroom_north_display',
        ]),
        observationHeading: Object.freeze({ x: 0, z: 1 }),
        zoneId: 'zone:optica-main-showroom',
      }),
      wp_optica_showroom_north_display: Object.freeze({
        waypointId: 'wp_optica_showroom_north_display',
        connectedWaypointIds: Object.freeze([
          'wp_optica_showroom_center',
          'wp_optica_showroom_counter_west',
          'wp_optica_showroom_waiting',
          'wp_valeria_off_duty_station',
        ]),
        observationHeading: Object.freeze({ x: 0, z: 1 }),
        zoneId: 'zone:optica-main-showroom',
      }),
      // Increment 5.23.2 — Dedicated verified interior off-duty / overnight station
      // Strictly inside Zone C showroom (-15.85, 0.20, 1.15), distinct from calibration station & ordinary roaming stops
      wp_valeria_off_duty_station: Object.freeze({
        waypointId: 'wp_valeria_off_duty_station',
        connectedWaypointIds: Object.freeze([
          'wp_optica_showroom_center',
          'wp_optica_showroom_counter_west',
          'wp_optica_showroom_north_display',
        ]),
        observationHeading: Object.freeze({ x: 1, z: 0 }),
        isOffDutyStation: true,
        zoneId: 'zone:optica-main-showroom',
      }),
    }),
  }),
});

export const FIXED_ANCHOR_SCHEDULED_PROFILES: Readonly<Record<string, FixedAnchorScheduledProfile>> = Object.freeze({
  'elena-border-concierge': Object.freeze({
    profileId: 'fixed:elena-border-kiosk',
    npcId: 'elena-border-concierge',
    canonicalName: resolveAuthoritativeCanonicalName('elena-border-concierge'),
    behaviorProfileType: 'fixed_anchor_scheduled',
    primaryWaypointId: 'wp_elena_kiosk',
    exclusionReason: Object.freeze({
      en: 'Fixed border information kiosk concierge; autonomous roaming explicitly excluded per Increment 5.22.0.',
      es: 'Concierge de quiosco fijo de orientación fronteriza; desplazamiento autónomo excluido explícitamente en Incremento 5.22.0.',
    }),
  }),

  'patricia-citizen-rep': Object.freeze({
    profileId: 'fixed:patricia-citizen-desk',
    npcId: 'patricia-citizen-rep',
    canonicalName: resolveAuthoritativeCanonicalName('patricia-citizen-rep'),
    behaviorProfileType: 'fixed_anchor_scheduled',
    primaryWaypointId: 'wp_patricia_desk',
    exclusionReason: Object.freeze({
      en: 'Fixed municipal citizen service desk representative; autonomous roaming explicitly excluded per Increment 5.22.0.',
      es: 'Representante de módulo fijo de atención ciudadana; desplazamiento autónomo excluido explícitamente en Incremento 5.22.0.',
    }),
  }),
});

export const NPC_AUTONOMOUS_BEHAVIOR_PROFILES: Readonly<Record<string, NpcBehavioralAutonomyProfile>> = Object.freeze({
  ...BRIDGE_PATROL_PROFILES,
  ...BUSINESS_LOCAL_AUTONOMY_PROFILES,
  ...FIXED_ANCHOR_SCHEDULED_PROFILES,
});

export function getNpcBehavioralAutonomyProfile(npcId: string): NpcBehavioralAutonomyProfile | null {
  if (typeof npcId !== 'string' || npcId.trim() === '') return null;
  return NPC_AUTONOMOUS_BEHAVIOR_PROFILES[npcId.trim()] ?? null;
}

// ---------------------------------------------------------------------------
// 3. DETERMINISTIC PSEUDO-RANDOM HASHING (ZERO Math.random / Date.now)
// ---------------------------------------------------------------------------

/**
 * Pure 32-bit FNV-1a + Murmur3 finalizer hash for deterministic behavioral variation.
 */
export function computeDeterministicHash32(seedInput: string): number {
  let h = 0x811c9dc5;
  for (let i = 0; i < seedInput.length; i++) {
    h ^= seedInput.charCodeAt(i);
    h = Math.imul(h, 0x01000193);
  }
  h ^= h >>> 16;
  h = Math.imul(h, 0x85ebca6b);
  h ^= h >>> 13;
  h = Math.imul(h, 0xc2b2ae35);
  h ^= h >>> 16;
  return h >>> 0;
}

export function deterministicRangeInt(hash: number, shift: number, minInclusive: number, maxInclusive: number): number {
  if (maxInclusive <= minInclusive) return minInclusive;
  const span = maxInclusive - minInclusive + 1;
  const sub = ((hash >>> shift) ^ (hash >>> (shift + 7))) >>> 0;
  return minInclusive + (sub % span);
}

export function deterministicUnitFloat(hash: number, shift: number): number {
  const bits = ((hash >>> shift) & 0xffff) >>> 0;
  return bits / 65535;
}

// ---------------------------------------------------------------------------
// 4. SPATIAL CONTAINMENT & ROUTE SEGMENT VALIDATION (SECTION 4.1, 7.4, 12.E)
// ---------------------------------------------------------------------------

/**
 * Verifies whether a 2D polygon in the XZ plane is strictly convex and simple
 * (non-self-intersecting, single loop with total exterior turning angle +/- 2*pi).
 */
export function isConvexPolygonXZ(polygon: readonly Point2DXZ[]): boolean {
  if (!Array.isArray(polygon) || polygon.length < 3) {
    return false;
  }

  const n = polygon.length;
  for (let i = 0; i < n; i++) {
    const v = polygon[i]!;
    if (!Number.isFinite(v.x) || !Number.isFinite(v.z)) {
      return false;
    }
  }

  let expectedSign = 0;
  let totalTurnAngle = 0;

  for (let i = 0; i < n; i++) {
    const p0 = polygon[i]!;
    const p1 = polygon[(i + 1) % n]!;
    const p2 = polygon[(i + 2) % n]!;

    const e1x = p1.x - p0.x;
    const e1z = p1.z - p0.z;
    const e2x = p2.x - p1.x;
    const e2z = p2.z - p1.z;

    const len1 = Math.hypot(e1x, e1z);
    const len2 = Math.hypot(e2x, e2z);
    if (len1 <= 1e-9 || len2 <= 1e-9) {
      return false;
    }

    const cross = e1x * e2z - e1z * e2x;
    const dot = e1x * e2x + e1z * e2z;
    if (Math.abs(cross) <= 1e-9) {
      // Collinear or 180-degree foldback edge is not strictly convex
      return false;
    }

    const sign = cross > 0 ? 1 : -1;
    if (expectedSign === 0) {
      expectedSign = sign;
    } else if (sign !== expectedSign) {
      return false;
    }

    totalTurnAngle += Math.atan2(cross, dot);
  }

  return Math.abs(Math.abs(totalTurnAngle) - 2 * Math.PI) <= 1e-5;
}

export function isPointInPolygonXZ(pt: Point2DXZ, polygon: readonly Point2DXZ[], tolerance = 0.05): boolean {
  if (!Number.isFinite(pt.x) || !Number.isFinite(pt.z) || polygon.length < 3) {
    return false;
  }

  // Bounding check with tolerance for edges
  let minX = Infinity;
  let maxX = -Infinity;
  let minZ = Infinity;
  let maxZ = -Infinity;
  for (const v of polygon) {
    if (!Number.isFinite(v.x) || !Number.isFinite(v.z)) return false;
    if (v.x < minX) minX = v.x;
    if (v.x > maxX) maxX = v.x;
    if (v.z < minZ) minZ = v.z;
    if (v.z > maxZ) maxZ = v.z;
  }
  if (
    pt.x < minX - tolerance ||
    pt.x > maxX + tolerance ||
    pt.z < minZ - tolerance ||
    pt.z > maxZ + tolerance
  ) {
    return false;
  }

  // Ray-casting point-in-polygon with edge tolerance
  let inside = false;
  for (let i = 0, j = polygon.length - 1; i < polygon.length; j = i++) {
    const xi = polygon[i]!.x;
    const zi = polygon[i]!.z;
    const xj = polygon[j]!.x;
    const zj = polygon[j]!.z;

    // Distance to segment (xi, zi)-(xj, zj)
    const dx = xj - xi;
    const dz = zj - zi;
    const lenSq = dx * dx + dz * dz;
    const t = lenSq > 0 ? Math.max(0, Math.min(1, ((pt.x - xi) * dx + (pt.z - zi) * dz) / lenSq)) : 0;
    const projX = xi + t * dx;
    const projZ = zi + t * dz;
    if (Math.hypot(pt.x - projX, pt.z - projZ) <= tolerance) {
      return true;
    }

    const intersect = zi > pt.z !== zj > pt.z && pt.x < ((xj - xi) * (pt.z - zi)) / (zj - zi) + xi;
    if (intersect) inside = !inside;
  }

  return inside;
}

/**
 * Geometrically exact 2D line-segment containment check in a polygon (with optional edge tolerance).
 *
 * - Convex case (`isConvexPolygonXZ(polygon) === true`):
 *   A convex polygon (and its Minkowski sum with a closed disk of radius `tolerance >= 0`) is a
 *   convex set in R^2. Therefore, if both endpoints `from` and `to` belong to the set, every point
 *   `P(t) = (1 - t)*from + t*to` for `t in [0, 1]` is guaranteed to belong to the set.
 * - Non-convex case (`isConvexPolygonXZ(polygon) === false`):
 *   Computes all exact parametric intersections `t in [0, 1]` between segment `from->to` and every
 *   polygon edge, plus vertex projections and tolerance-band crossings, partitioning `[0, 1]` into
 *   elementary intervals `[t_k, t_{k+1}]` free of interior boundary crossings, and verifies every
 *   split point and interval midpoint against `isPointInPolygonXZ`.
 */
export function isSegmentInPolygonXZ(
  from: Point2DXZ,
  to: Point2DXZ,
  polygon: readonly Point2DXZ[],
  tolerance = 0.05
): boolean {
  if (!isPointInPolygonXZ(from, polygon, tolerance) || !isPointInPolygonXZ(to, polygon, tolerance)) {
    return false;
  }

  if (isConvexPolygonXZ(polygon)) {
    return true;
  }

  const segDx = to.x - from.x;
  const segDz = to.z - from.z;
  const segLenSq = segDx * segDx + segDz * segDz;
  if (segLenSq <= 1e-12) {
    return true;
  }
  const segLen = Math.sqrt(segLenSq);
  const deltaT = Math.min(0.05, Math.max(1e-5, (tolerance + 1e-3) / segLen));

  const candidateParams: number[] = [0, 1];
  const pushParam = (t: number): void => {
    if (Number.isFinite(t) && t > 0 && t < 1) {
      candidateParams.push(t);
    }
  };

  for (let i = 0, j = polygon.length - 1; i < polygon.length; j = i++) {
    const p = polygon[j]!;
    const q = polygon[i]!;
    const edgeDx = q.x - p.x;
    const edgeDz = q.z - p.z;

    // 1. Exact segment-edge intersection parameter t in [0, 1]
    const denom = segDx * edgeDz - segDz * edgeDx;
    const qpX = p.x - from.x;
    const qpZ = p.z - from.z;
    if (Math.abs(denom) > 1e-12) {
      const t = (qpX * edgeDz - qpZ * edgeDx) / denom;
      const u = (qpX * segDz - qpZ * segDx) / denom;
      if (t >= -1e-9 && t <= 1 + 1e-9 && u >= -1e-9 && u <= 1 + 1e-9) {
        const clampedT = Math.max(0, Math.min(1, t));
        pushParam(clampedT);
        pushParam(clampedT - deltaT);
        pushParam(clampedT + deltaT);
      }
    }

    // 2. Closest-approach projection of polygon vertex `p` onto segment `from->to`
    const tProj = ((p.x - from.x) * segDx + (p.z - from.z) * segDz) / segLenSq;
    const clampedProj = Math.max(0, Math.min(1, tProj));
    pushParam(clampedProj);
    pushParam(clampedProj - deltaT);
    pushParam(clampedProj + deltaT);
  }

  candidateParams.sort((a, b) => a - b);
  const uniqueParams: number[] = [candidateParams[0]!];
  for (let i = 1; i < candidateParams.length; i++) {
    const cur = candidateParams[i]!;
    if (cur - uniqueParams[uniqueParams.length - 1]! > 1e-7) {
      uniqueParams.push(cur);
    }
  }

  for (let i = 0; i < uniqueParams.length; i++) {
    const t = uniqueParams[i]!;
    const ptAtT: Point2DXZ = {
      x: from.x + segDx * t,
      z: from.z + segDz * t,
    };
    if (!isPointInPolygonXZ(ptAtT, polygon, tolerance)) {
      return false;
    }
    if (i + 1 < uniqueParams.length) {
      const tMid = 0.5 * (t + uniqueParams[i + 1]!);
      const midPt: Point2DXZ = {
        x: from.x + segDx * tMid,
        z: from.z + segDz * tMid,
      };
      if (!isPointInPolygonXZ(midPt, polygon, tolerance)) {
        return false;
      }
    }
  }

  return true;
}

function distancePointToSegmentXZAndYError(
  pt: Vector3D,
  a: Vector3D,
  b: Vector3D
): { readonly distXZ: number; readonly dy: number } {
  const dx = b.x - a.x;
  const dz = b.z - a.z;
  const lenSq = dx * dx + dz * dz;
  const t = lenSq > 1e-9 ? Math.max(0, Math.min(1, ((pt.x - a.x) * dx + (pt.z - a.z) * dz) / lenSq)) : 0;
  const projX = a.x + t * dx;
  const projY = a.y + t * (b.y - a.y);
  const projZ = a.z + t * dz;
  return {
    distXZ: Math.hypot(pt.x - projX, pt.z - projZ),
    dy: Math.abs(pt.y - projY),
  };
}

export function isPointInBridgePatrolDomain(point: Vector3D, tolerance = 0.05): boolean {
  if (!isValidCorridorVector3(point)) return false;
  const b = BRIDGE_PATROL_DOMAIN.bounds;
  if (
    point.x < b.minX - tolerance ||
    point.x > b.maxX + tolerance ||
    point.y < b.minY - 0.5 - tolerance ||
    point.y > b.maxY + 0.5 + tolerance ||
    point.z < b.minZ - tolerance ||
    point.z > b.maxZ + tolerance
  ) {
    return false;
  }

  // Must lie within the safe lateral half-width of at least one verified bridge walkway / crosswalk segment
  const n = BRIDGE_FULL_LOOP_NODES.length;
  for (let i = 0; i < n; i++) {
    const nodeA = BRIDGE_FULL_LOOP_NODES[i]!;
    const nodeB = BRIDGE_FULL_LOOP_NODES[(i + 1) % n]!;
    const maxHalfWidth = Math.max(nodeA.safeHalfWidthMeters, nodeB.safeHalfWidthMeters) + tolerance;
    const { distXZ, dy } = distancePointToSegmentXZAndYError(point, nodeA.position, nodeB.position);
    if (distXZ <= maxHalfWidth && dy <= 0.65 + tolerance) {
      return true;
    }
  }
  return false;
}

function isZoneAuthorized(
  zone: BusinessSpatialZone,
  activeTaskId?: string | null
): boolean {
  if (!zone.taskOnly) return true;
  if (!activeTaskId) return false;
  return Boolean(zone.authorizedTaskIds && zone.authorizedTaskIds.includes(activeTaskId));
}

function isPointInSpatialZone(
  point: Vector3D,
  zone: BusinessSpatialZone
): boolean {
  if (!isValidCorridorVector3(point)) return false;
  const tol = zone.toleranceMeters;
  const b = zone.bounds;
  if (
    point.x < b.minX - tol ||
    point.x > b.maxX + tol ||
    point.y < b.minY - tol ||
    point.y > b.maxY + tol ||
    point.z < b.minZ - tol ||
    point.z > b.maxZ + tol
  ) {
    return false;
  }
  if (Math.abs(point.x) < 6.9) {
    return false;
  }
  return isPointInPolygonXZ({ x: point.x, z: point.z }, zone.polygonXZ, tol);
}

/**
 * Resolves the authorized spatial zone containing `point` for a multi-zone business domain.
 * Returns null if `point` does not belong to any authorized zone for `activeTaskId`.
 */
export function resolveAuthorizedBusinessZone(
  point: Vector3D,
  domain: BusinessMovementDomain | null | undefined,
  activeTaskId?: string | null
): BusinessSpatialZone | null {
  if (!domain || !domain.connectedZones || domain.connectedZones.length === 0) {
    return null;
  }
  // Prefer most specific zone (task-only room / doorway / showroom / entrance / exterior)
  for (let i = domain.connectedZones.length - 1; i >= 0; i--) {
    const zone = domain.connectedZones[i]!;
    if (!isZoneAuthorized(zone, activeTaskId)) continue;
    if (isPointInSpatialZone(point, zone)) {
      return zone;
    }
  }
  return null;
}

export function isPointInBusinessDomain(
  point: Vector3D,
  domain: BusinessMovementDomain | null | undefined,
  activeTaskId?: string | null
): boolean {
  if (!domain || !isValidCorridorVector3(point)) return false;
  // Never enter Avenida Juárez roadway / vehicle lanes (|x| < 6.9m)
  if (Math.abs(point.x) < 6.9) {
    return false;
  }
  if (domain.connectedZones && domain.connectedZones.length > 0) {
    if (resolveAuthorizedBusinessZone(point, domain, activeTaskId) !== null) {
      return true;
    }
  }
  const tol = domain.navigationToleranceMeters;
  const b = domain.bounds;
  if (
    point.x < b.minX - tol ||
    point.x > b.maxX + tol ||
    point.y < b.minY - tol ||
    point.y > b.maxY + tol ||
    point.z < b.minZ - tol ||
    point.z > b.maxZ + tol
  ) {
    return false;
  }
  return isPointInPolygonXZ({ x: point.x, z: point.z }, domain.polygonXZ, tol);
}

/**
 * Validates that the entire continuous 3D line segment `from -> to` remains inside `domain`.
 * For multi-zone domains (`domain.connectedZones`), both endpoints and the entire segment
 * must lie inside the SAME authorized spatial zone (preventing illegal shortcuts through
 * solid storefront glass, interior walls, or furniture).
 */
export function isSegmentInBusinessDomain(
  from: Vector3D,
  to: Vector3D,
  domain: BusinessMovementDomain | null | undefined,
  activeTaskId?: string | null
): boolean {
  if (
    !domain ||
    !isPointInBusinessDomain(from, domain, activeTaskId) ||
    !isPointInBusinessDomain(to, domain, activeTaskId)
  ) {
    return false;
  }
  // Both endpoints must lie on the same side of the Avenida Juárez roadway (never crossing |x| < 6.9m)
  if (from.x * to.x <= 0) {
    return false;
  }
  if (domain.connectedZones && domain.connectedZones.length > 0) {
    for (const zone of domain.connectedZones) {
      if (!isZoneAuthorized(zone, activeTaskId)) continue;
      if (
        isPointInSpatialZone(from, zone) &&
        isPointInSpatialZone(to, zone) &&
        isSegmentInPolygonXZ({ x: from.x, z: from.z }, { x: to.x, z: to.z }, zone.polygonXZ, zone.toleranceMeters)
      ) {
        return true;
      }
    }
  }
  // Fallback to exterior domain polygon ONLY if both endpoints are inside the exterior bounding box
  const tol = domain.navigationToleranceMeters;
  const b = domain.bounds;
  const bothInExteriorBox =
    from.x >= b.minX - tol &&
    from.x <= b.maxX + tol &&
    from.z >= b.minZ - tol &&
    from.z <= b.maxZ + tol &&
    to.x >= b.minX - tol &&
    to.x <= b.maxX + tol &&
    to.z >= b.minZ - tol &&
    to.z <= b.maxZ + tol;
  if (!bothInExteriorBox) {
    return false;
  }
  return isSegmentInPolygonXZ(
    { x: from.x, z: from.z },
    { x: to.x, z: to.z },
    domain.polygonXZ,
    domain.navigationToleranceMeters
  );
}

export function validateBusinessCandidateRoute(
  waypointIds: readonly string[],
  domain: BusinessMovementDomain | null | undefined,
  waypointsRegistry: Readonly<Record<string, { readonly position: Vector3D; readonly outgoingWaypointIds: readonly string[] }>>,
  activeTaskId?: string | null
): { readonly valid: boolean; readonly reason: string } {
  if (!domain) {
    return { valid: false, reason: 'missing_business_domain' };
  }
  if (!Array.isArray(waypointIds) || waypointIds.length < 2) {
    return { valid: false, reason: 'insufficient_waypoints' };
  }

  const authorizedZoneWaypoints = new Set<string>(domain.authorizedWaypointIds);
  if (domain.connectedZones) {
    for (const zone of domain.connectedZones) {
      if (!isZoneAuthorized(zone, activeTaskId)) continue;
      for (const id of zone.waypointIds) {
        authorizedZoneWaypoints.add(id);
      }
    }
  }

  for (let i = 0; i < waypointIds.length; i++) {
    const wpId = waypointIds[i]!;
    if (!authorizedZoneWaypoints.has(wpId)) {
      return { valid: false, reason: `unauthorized_waypoint:${wpId}` };
    }
    const wp = waypointsRegistry[wpId];
    if (!wp || !isPointInBusinessDomain(wp.position, domain, activeTaskId)) {
      return { valid: false, reason: `waypoint_outside_domain:${wpId}` };
    }
    if (i > 0) {
      const prevId = waypointIds[i - 1]!;
      const prevWp = waypointsRegistry[prevId]!;
      if (!prevWp.outgoingWaypointIds.includes(wpId) && prevId !== wpId) {
        return { valid: false, reason: `disconnected_segment:${prevId}->${wpId}` };
      }
      if (!isSegmentInBusinessDomain(prevWp.position, wp.position, domain, activeTaskId)) {
        return { valid: false, reason: `segment_outside_domain:${prevId}->${wpId}` };
      }
    }
  }

  return { valid: true, reason: 'ok' };
}

/**
 * Validates that a 3D point falls within the safe roaming region for `domain`
 * (or within an authorized connected zone for multi-zone indoor/outdoor domains).
 * Task-only rooms (e.g. Zone D rear calibration lab) are strictly excluded unless `activeTaskId` authorizes them.
 */
export function isPointInSafeBusinessRoamingRegion(
  point: Vector3D,
  domain: BusinessMovementDomain | null | undefined,
  activeTaskId?: string | null
): boolean {
  if (!domain || !isPointInBusinessDomain(point, domain, activeTaskId)) {
    return false;
  }
  if (domain.connectedZones && domain.connectedZones.length > 0) {
    return resolveAuthorizedBusinessZone(point, domain, activeTaskId) !== null;
  }
  const tol = domain.safeRoamingToleranceMeters;
  const sb = domain.safeRoamingBounds;
  if (
    point.x < sb.minX - tol ||
    point.x > sb.maxX + tol ||
    point.y < sb.minY - tol ||
    point.y > sb.maxY + tol ||
    point.z < sb.minZ - tol ||
    point.z > sb.maxZ + tol
  ) {
    return false;
  }
  return isPointInPolygonXZ({ x: point.x, z: point.z }, domain.safeRoamingPolygonXZ, tol);
}

/**
 * Validates that the entire continuous 3D line segment `from -> to` lies inside BOTH
 * the safe roaming region (same authorized convex zone for multi-zone domains) and the absolute business domain.
 */
export function isSegmentInSafeBusinessRoamingRegion(
  from: Vector3D,
  to: Vector3D,
  domain: BusinessMovementDomain | null | undefined,
  activeTaskId?: string | null
): boolean {
  if (
    !domain ||
    !isPointInSafeBusinessRoamingRegion(from, domain, activeTaskId) ||
    !isPointInSafeBusinessRoamingRegion(to, domain, activeTaskId) ||
    !isSegmentInBusinessDomain(from, to, domain, activeTaskId)
  ) {
    return false;
  }
  if (domain.connectedZones && domain.connectedZones.length > 0) {
    for (const zone of domain.connectedZones) {
      if (!isZoneAuthorized(zone, activeTaskId)) continue;
      if (
        isPointInSpatialZone(from, zone) &&
        isPointInSpatialZone(to, zone) &&
        isSegmentInPolygonXZ({ x: from.x, z: from.z }, { x: to.x, z: to.z }, zone.polygonXZ, zone.toleranceMeters)
      ) {
        return true;
      }
    }
    return false;
  }
  return isSegmentInPolygonXZ(
    { x: from.x, z: from.z },
    { x: to.x, z: to.z },
    domain.safeRoamingPolygonXZ,
    domain.safeRoamingToleranceMeters
  );
}

/**
 * Validates a multi-node sequence in an establishment's connected local roaming graph
 * against both `safeRoamingPolygonXZ` (or authorized `connectedZones`) and the absolute business domain.
 */
export function validateBusinessLocalGraphRoute(
  waypointIds: readonly string[],
  domain: BusinessMovementDomain | null | undefined,
  profile: BusinessLocalAutonomyProfile,
  waypointsRegistry: Readonly<Record<string, { readonly position: Vector3D; readonly outgoingWaypointIds: readonly string[] }>>,
  activeTaskId?: string | null
): { readonly valid: boolean; readonly reason: string } {
  const baseCheck = validateBusinessCandidateRoute(waypointIds, domain, waypointsRegistry, activeTaskId);
  if (!baseCheck.valid) {
    return baseCheck;
  }

  for (let i = 0; i < waypointIds.length; i++) {
    const wpId = waypointIds[i]!;
    const node = profile.localNodes[wpId];
    if (!node && !activeTaskId) {
      return { valid: false, reason: `unregistered_local_node:${wpId}` };
    }
    const wp = waypointsRegistry[wpId]!;
    if (!isPointInSafeBusinessRoamingRegion(wp.position, domain, activeTaskId)) {
      return { valid: false, reason: `waypoint_outside_safe_roaming:${wpId}` };
    }
    if (i > 0) {
      const prevId = waypointIds[i - 1]!;
      const prevNode = profile.localNodes[prevId];
      const prevWp = waypointsRegistry[prevId]!;
      if (!activeTaskId && (!prevNode || !prevNode.connectedWaypointIds.includes(wpId))) {
        return { valid: false, reason: `disconnected_local_graph_edge:${prevId}->${wpId}` };
      }
      if (!isSegmentInSafeBusinessRoamingRegion(prevWp.position, wp.position, domain, activeTaskId)) {
        return { valid: false, reason: `segment_outside_safe_roaming:${prevId}->${wpId}` };
      }
    }
  }

  return { valid: true, reason: 'ok' };
}

// ---------------------------------------------------------------------------
// 5. PLAYER INTERACTION PAUSE / RESUME TIME EVALUATION (SUBSYSTEM A3)
// ---------------------------------------------------------------------------

export interface EffectiveAutonomousClockEvaluation {
  readonly effectiveTimeMs: number;
  readonly isSuspended: boolean;
  readonly suspendedByPlayerId: string | null;
  readonly isResuming: boolean;
  readonly totalPausedDurationMs: number;
}

/**
 * Evaluates the effective simulation timestamp for an NPC's autonomous locomotion
 * given its history of authorized player-interaction pause intervals.
 *
 * Mathematical properties:
 * - While a pause interval `[startedAtMs, null]` or `[startedAtMs, endedAtMs)` is active at `rawTimeMs`,
 *   `effectiveTimeMs` remains frozen at its exact value at `startedAtMs`.
 * - When the pause ends at `endedAtMs`, `effectiveTimeMs` resumes smoothly from that exact instant
 *   (`effectiveTimeMs(endedAtMs) === effectiveTimeMs(startedAtMs)`), preventing any forward jump or reset.
 */
export function evaluateEffectiveAutonomousTime(
  rawTimeMs: number,
  pauseIntervals: readonly NpcInteractionPauseInterval[] | undefined,
  epochStartUtcMs: number
): EffectiveAutonomousClockEvaluation {
  if (!pauseIntervals || pauseIntervals.length === 0) {
    return {
      effectiveTimeMs: rawTimeMs,
      isSuspended: false,
      suspendedByPlayerId: null,
      isResuming: false,
      totalPausedDurationMs: 0,
    };
  }

  let totalPausedMs = 0;
  let isSuspended = false;
  let suspendedByPlayerId: string | null = null;
  let mostRecentEndedAtMs: number | null = null;

  for (const interval of pauseIntervals) {
    const startMs = Math.max(epochStartUtcMs, interval.startedAtMs);
    if (rawTimeMs < startMs) continue;

    if (interval.endedAtMs === null || rawTimeMs < interval.endedAtMs) {
      isSuspended = true;
      suspendedByPlayerId = interval.playerId;
      totalPausedMs += Math.max(0, rawTimeMs - startMs);
    } else if (interval.endedAtMs >= startMs) {
      totalPausedMs += interval.endedAtMs - startMs;
      if (mostRecentEndedAtMs === null || interval.endedAtMs > mostRecentEndedAtMs) {
        mostRecentEndedAtMs = interval.endedAtMs;
      }
    }
  }

  const effectiveTimeMs = Math.max(epochStartUtcMs, rawTimeMs - totalPausedMs);
  const isResuming =
    !isSuspended &&
    mostRecentEndedAtMs !== null &&
    rawTimeMs >= mostRecentEndedAtMs &&
    rawTimeMs - mostRecentEndedAtMs <= RESUME_TRANSITION_DURATION_SECONDS * 1000;

  return {
    effectiveTimeMs,
    isSuspended,
    suspendedByPlayerId,
    isResuming,
    totalPausedDurationMs: totalPausedMs,
  };
}

// ---------------------------------------------------------------------------
// 6. SUBSYSTEM A — COMMUNITY PLAYER PROXY BRIDGE PATROL PLANNER
// ---------------------------------------------------------------------------

interface TimeWindowSec {
  readonly startSec: number;
  readonly endSec: number;
}

function mergeSortedWindows(windows: readonly TimeWindowSec[]): TimeWindowSec[] {
  if (windows.length === 0) return [];
  const sorted = [...windows].sort((a, b) => a.startSec - b.startSec);
  const merged: TimeWindowSec[] = [sorted[0]!];
  for (let i = 1; i < sorted.length; i++) {
    const cur = sorted[i]!;
    const last = merged[merged.length - 1]!;
    if (cur.startSec <= last.endSec) {
      merged[merged.length - 1] = {
        startSec: last.startSec,
        endSec: Math.max(last.endSec, cur.endSec),
      };
    } else {
      merged.push(cur);
    }
  }
  return merged;
}

function getBridgePatrolAnchorWindows(npcId: string, dayOfWeek: number): TimeWindowSec[] {
  const windows: TimeWindowSec[] = [
    { startSec: 0, endSec: 60 },
    { startSec: 2 * 3600 + 29 * 60, endSec: 2 * 3600 + 32 * 60 },
    { startSec: 8 * 3600 + 29 * 60, endSec: 8 * 3600 + 31 * 60 },
    { startSec: 9 * 3600 + 59 * 60, endSec: 10 * 3600 + 2 * 60 },
    { startSec: 23 * 3600 + 29 * 60, endSec: 23 * 3600 + 31 * 60 },
    { startSec: 23 * 3600 + 58 * 60 + 45, endSec: 24 * 3600 },
  ];

  if (npcId === 'mateo-student') {
    windows.push({ startSec: 13 * 3600 + 34 * 60 + 45, endSec: 13 * 3600 + 36 * 60 + 15 });
    if (dayOfWeek >= 1 && dayOfWeek <= 5) {
      windows.push({ startSec: 9 * 3600 + 28 * 60 + 45, endSec: 10 * 3600 + 5 * 60 + 30 });
    }
  } else if (npcId === 'sofia-cross-border-retail') {
    if (dayOfWeek >= 1 && dayOfWeek <= 6) {
      windows.push({ startSec: 10 * 3600 + 58 * 60 + 45, endSec: 11 * 3600 + 35 * 60 + 30 });
    }
  } else if (npcId === 'hector-artisan') {
    if (dayOfWeek >= 1 && dayOfWeek <= 6) {
      windows.push({ startSec: 12 * 3600 + 58 * 60 + 45, endSec: 14 * 3600 + 5 * 60 + 30 });
    }
  }

  return mergeSortedWindows(windows);
}

function buildComplementEpochs(
  dayStartSec: number,
  dayEndSec: number,
  anchors: readonly TimeWindowSec[]
): TimeWindowSec[] {
  const epochs: TimeWindowSec[] = [];
  let cursor = dayStartSec;
  for (const a of anchors) {
    const aStart = Math.max(dayStartSec, a.startSec);
    const aEnd = Math.min(dayEndSec, a.endSec);
    if (aStart > cursor + 30) {
      epochs.push({ startSec: cursor, endSec: aStart });
    }
    cursor = Math.max(cursor, aEnd);
  }
  if (dayEndSec > cursor + 30) {
    epochs.push({ startSec: cursor, endSec: dayEndSec });
  }
  return epochs;
}

export interface AutonomousMovementEvaluationResult {
  readonly movementState: 'stationary' | 'moving' | 'arrived' | 'waiting';
  readonly currentWaypointId: string;
  readonly destinationWaypointId: string | null;
  readonly activeRouteId: string | null;
  readonly position: Vector3D;
  readonly forwardHeading: { readonly x: number; readonly z: number };
  readonly progression: number;
  readonly movementStartTime: string | null;
  readonly expectedDurationSeconds: number;
  readonly associatedPlaceId: string;
  readonly behaviorProfileType: NpcBehaviorProfileType;
  readonly autonomousBehaviorState: NpcAutonomousBehaviorState;
  readonly controlMode: NpcMovementControlMode;
  readonly patrolDirection: PatrolDirection;
  readonly patrolCycleIndex: number;
  readonly isSuspendedByPlayerInteraction: boolean;
  readonly suspendedByPlayerId: string | null;
  readonly passingEncounterPartnerNpcId: string | null;
  readonly passingEncounterSessionId: string | null;
  readonly passingEncounterType: PassingEncounterType | null;
  readonly businessDomainId: string | null;
  readonly withinAuthorizedDomain: boolean;
  readonly actualSpeedMps?: number;
  readonly activeSegmentEndPosition?: Vector3D;
  readonly currentZoneId?: string | null;
  readonly activeTaskId?: string | null;
  readonly taskDestinationReachable?: boolean;
  readonly physicalCalibrationActive?: boolean;
  // Increment 5.23.2 Business Opening/Closing Lifecycle & Shutter Synchronization
  readonly businessLifecycleState?: BusinessLifecycleState;
  readonly shutterClosedPermitted?: boolean;
}

interface BridgeLoopMetrics {
  readonly segLengths: readonly number[];
  readonly cumulativeDist: readonly number[];
  readonly totalLength: number;
}

const BRIDGE_LOOP_METRICS: BridgeLoopMetrics = (() => {
  const n = BRIDGE_FULL_LOOP_NODES.length;
  const segLengths: number[] = [];
  const cumulativeDist: number[] = [0];
  let total = 0;
  for (let i = 0; i < n; i++) {
    const a = BRIDGE_FULL_LOOP_NODES[i]!.position;
    const b = BRIDGE_FULL_LOOP_NODES[(i + 1) % n]!.position;
    const d = Math.hypot(b.x - a.x, b.y - a.y, b.z - a.z);
    segLengths.push(d);
    total += d;
    cumulativeDist.push(total);
  }
  return { segLengths, cumulativeDist, totalLength: total };
})();

export const BRIDGE_FULL_LOOP_TOTAL_METERS = BRIDGE_LOOP_METRICS.totalLength;

function evaluatePointOnBridgeLoop(
  loopFraction: number,
  lateralOffsetMeters: number
): {
  readonly position: Vector3D;
  readonly activeSegmentEndPosition: Vector3D;
  readonly heading: { readonly x: number; readonly z: number };
  readonly currentWaypointId: string;
  readonly destinationWaypointId: string;
  readonly direction: 'northbound' | 'southbound';
  readonly corridor: BridgeLoopNode['corridor'];
} {
  const n = BRIDGE_FULL_LOOP_NODES.length;
  const total = BRIDGE_LOOP_METRICS.totalLength;
  const wrappedFrac = ((loopFraction % 1) + 1) % 1;
  const targetDist = wrappedFrac * total;

  for (let i = 0; i < n; i++) {
    const startDist = BRIDGE_LOOP_METRICS.cumulativeDist[i]!;
    const segLen = BRIDGE_LOOP_METRICS.segLengths[i]!;
    if (targetDist <= startDist + segLen || i === n - 1) {
      const r = segLen > 1e-6 ? Math.max(0, Math.min(1, (targetDist - startDist) / segLen)) : 0;
      const nodeA = BRIDGE_FULL_LOOP_NODES[i]!;
      const nodeB = BRIDGE_FULL_LOOP_NODES[(i + 1) % n]!;

      const dx = nodeB.position.x - nodeA.position.x;
      const dy = nodeB.position.y - nodeA.position.y;
      const dz = nodeB.position.z - nodeA.position.z;
      const horizLen = Math.hypot(dx, dz) || 1;
      const hx = dx / horizLen;
      const hz = dz / horizLen;

      // Right-hand perpendicular in XZ plane: (hz, -hx)
      // Clamp lateral offset strictly inside the segment's safe corridor half-width
      const maxLat = Math.min(nodeA.safeHalfWidthMeters, nodeB.safeHalfWidthMeters, WALKWAY_SAFE_HALF_WIDTH_METERS);
      const clampedLat = Math.max(-maxLat, Math.min(maxLat, lateralOffsetMeters));
      // Taper lateral offset gently near turnaround segments so transitions remain centered
      const latWeight = nodeA.corridor === 'east_walkway' || nodeA.corridor === 'west_walkway' ? 1.0 : 0.35;

      const x = nodeA.position.x + dx * r + hz * clampedLat * latWeight;
      const y = nodeA.position.y + dy * r;
      const z = nodeA.position.z + dz * r - hx * clampedLat * latWeight;
      const endX = nodeB.position.x + hz * clampedLat * latWeight;
      const endZ = nodeB.position.z - hx * clampedLat * latWeight;

      return {
        position: {
          x: Math.round(x * 1000) / 1000,
          y: Math.round(y * 1000) / 1000,
          z: Math.round(z * 1000) / 1000,
        },
        activeSegmentEndPosition: {
          x: Math.round(endX * 1000) / 1000,
          y: Math.round(nodeB.position.y * 1000) / 1000,
          z: Math.round(endZ * 1000) / 1000,
        },
        heading: {
          x: Math.round(hx * 1000) / 1000,
          z: Math.round(hz * 1000) / 1000,
        },
        currentWaypointId: r < 0.5 ? nodeA.waypointId : nodeB.waypointId,
        destinationWaypointId: nodeB.waypointId,
        direction: nodeA.direction,
        corridor: nodeA.corridor,
      };
    }
  }

  const first = BRIDGE_FULL_LOOP_NODES[0]!;
  return {
    position: first.position,
    activeSegmentEndPosition: BRIDGE_FULL_LOOP_NODES[1]!.position,
    heading: { x: 0, z: 1 },
    currentWaypointId: first.waypointId,
    destinationWaypointId: BRIDGE_FULL_LOOP_NODES[1]!.waypointId,
    direction: first.direction,
    corridor: first.corridor,
  };
}

export function evaluateBridgePatrolMovement(params: {
  readonly profile: BridgePatrolBehaviorProfile;
  readonly rawEvaluatedAtMs: number;
  readonly civil: CivilTime;
  readonly waypointsRegistry: Readonly<
    Record<string, { readonly position: Vector3D; readonly associatedPlaceId: string; readonly outgoingWaypointIds: readonly string[] }>
  >;
  readonly pauseIntervals?: readonly NpcInteractionPauseInterval[];
}): AutonomousMovementEvaluationResult {
  const { profile, rawEvaluatedAtMs, civil, waypointsRegistry, pauseIntervals } = params;
  const homeWp = waypointsRegistry[profile.primaryWaypointId]!;

  const rawSecInDay =
    civil.hour * 3600 + civil.minute * 60 + civil.second + civil.millisecond / 1000;
  const dayStartUtcMs = rawEvaluatedAtMs - rawSecInDay * 1000;

  const anchors = getBridgePatrolAnchorWindows(profile.npcId, civil.dayOfWeek);
  const epochs = buildComplementEpochs(0, 86400, anchors);

  // Locate active patrol epoch
  let activeEpoch: TimeWindowSec | null = null;
  let epochIndex = 0;
  for (let i = 0; i < epochs.length; i++) {
    const ep = epochs[i]!;
    if (rawSecInDay >= ep.startSec && rawSecInDay < ep.endSec) {
      activeEpoch = ep;
      epochIndex = i;
      break;
    }
  }

  // If inside a brief station anchor window, remain calmly observing at primary waypoint
  if (!activeEpoch) {
    const pauseEval = evaluateEffectiveAutonomousTime(rawEvaluatedAtMs, pauseIntervals, dayStartUtcMs);
    const dirVec = profile.initialDirection === 'northbound' ? 1 : -1;
    return {
      movementState: 'stationary',
      currentWaypointId: profile.primaryWaypointId,
      destinationWaypointId: null,
      activeRouteId: null,
      position: homeWp.position,
      forwardHeading: { x: 0, z: dirVec },
      progression: 0,
      movementStartTime: null,
      expectedDurationSeconds: 0,
      associatedPlaceId: homeWp.associatedPlaceId,
      behaviorProfileType: 'bridge_patrol',
      autonomousBehaviorState: pauseEval.isSuspended ? 'player_interaction_paused' : 'observing',
      controlMode: pauseEval.isSuspended ? 'player_interaction_suspended' : 'autonomous_bridge_patrol',
      patrolDirection: profile.initialDirection,
      patrolCycleIndex: 0,
      isSuspendedByPlayerInteraction: pauseEval.isSuspended,
      suspendedByPlayerId: pauseEval.suspendedByPlayerId,
      passingEncounterPartnerNpcId: null,
      passingEncounterSessionId: null,
      passingEncounterType: null,
      businessDomainId: BRIDGE_PATROL_DOMAIN.domainId,
      withinAuthorizedDomain: isPointInBridgePatrolDomain(homeWp.position),
    };
  }

  const epochStartUtcMs = dayStartUtcMs + activeEpoch.startSec * 1000;
  const pauseEval = evaluateEffectiveAutonomousTime(rawEvaluatedAtMs, pauseIntervals, epochStartUtcMs);
  const effSecInDay = Math.max(
    activeEpoch.startSec,
    Math.min(activeEpoch.endSec - 0.001, (pauseEval.effectiveTimeMs - dayStartUtcMs) / 1000)
  );

  // Compute exact lap duration from full-bridge loop length and profile's natural walking speed
  const walkLapSec = BRIDGE_FULL_LOOP_TOTAL_METERS / Math.max(0.5, profile.preferredSpeedMps);
  const dateKey = `${civil.year}-${String(civil.month).padStart(2, '0')}-${String(civil.day).padStart(2, '0')}`;
  const daySeed = computeDeterministicHash32(`${profile.npcId}|${dateKey}|${profile.profileId}|ep${epochIndex}`);

  const rawPauseNorth = deterministicRangeInt(daySeed, 3, profile.minPauseSeconds, profile.maxPauseSeconds);
  const rawTurnNorth = deterministicRangeInt(
    daySeed,
    8,
    profile.turnaroundPauseSeconds.min,
    profile.turnaroundPauseSeconds.max
  );
  const rawPauseSouth = deterministicRangeInt(daySeed, 13, profile.minPauseSeconds, profile.maxPauseSeconds);
  const rawTurnSouth = deterministicRangeInt(
    daySeed,
    18,
    profile.turnaroundPauseSeconds.min,
    profile.turnaroundPauseSeconds.max
  );

  const totalPauseSec = rawPauseNorth + rawTurnNorth + rawPauseSouth + rawTurnSouth;
  const lapDurSec = walkLapSec + totalPauseSec;

  // Deterministic calendar-day phase offset + initialLoopPhase keeps the 3 proxies naturally distributed
  const dailyPhaseJitter = deterministicUnitFloat(daySeed, 20) * 0.08;
  const phaseSec = (profile.initialLoopPhase + dailyPhaseJitter) * lapDurSec;
  const elapsedInEpoch = effSecInDay - activeEpoch.startSec + phaseSec;
  const cycleIndex = Math.floor(elapsedInEpoch / lapDurSec);
  const secInCycle = elapsedInEpoch - cycleIndex * lapDurSec;

  // 6 stages around the full loop:
  // 1. Walk East Walkway (Northbound Juárez -> El Paso): loop [0.00 .. 0.45]
  // 2. Turnaround pause at El Paso North End: loop 0.45
  // 3. Walk North Turnaround + West Walkway south to Crest: loop [0.45 .. 0.68]
  // 4. Brief scenic observation on West Crest: loop 0.68
  // 5. Walk West Walkway south + South Crosswalk back to Plaza: loop [0.68 .. 0.96]
  // 6. Brief turnaround / observation at South Plaza Approach: loop [0.96 .. 1.00]
  const w1 = walkLapSec * 0.45;
  const w2 = walkLapSec * 0.23;
  const w3 = walkLapSec * 0.28;
  const w4 = walkLapSec * 0.04;

  const t1 = w1;
  const t2 = t1 + rawTurnNorth;
  const t3 = t2 + w2;
  const t4 = t3 + rawPauseSouth;
  const t5 = t4 + w3;
  const t6 = t5 + rawPauseNorth;
  const t7 = t6 + w4;

  let loopFrac = 0;
  let stageKind: 'walk' | 'turnaround' | 'observe' = 'walk';
  let stageRatio = 0;
  let stageStartSec = 0;
  let stageDur = 1;

  if (secInCycle < t1) {
    stageKind = 'walk';
    stageStartSec = 0;
    stageDur = w1;
    stageRatio = secInCycle / Math.max(0.001, w1);
    loopFrac = 0.0 + 0.45 * stageRatio;
  } else if (secInCycle < t2) {
    stageKind = 'turnaround';
    stageStartSec = t1;
    stageDur = rawTurnNorth;
    stageRatio = (secInCycle - t1) / Math.max(0.001, rawTurnNorth);
    loopFrac = 0.45;
  } else if (secInCycle < t3) {
    stageKind = 'walk';
    stageStartSec = t2;
    stageDur = w2;
    stageRatio = (secInCycle - t2) / Math.max(0.001, w2);
    loopFrac = 0.45 + 0.23 * stageRatio;
  } else if (secInCycle < t4) {
    stageKind = 'observe';
    stageStartSec = t3;
    stageDur = rawPauseSouth;
    stageRatio = (secInCycle - t3) / Math.max(0.001, rawPauseSouth);
    loopFrac = 0.68;
  } else if (secInCycle < t5) {
    stageKind = 'walk';
    stageStartSec = t4;
    stageDur = w3;
    stageRatio = (secInCycle - t4) / Math.max(0.001, w3);
    loopFrac = 0.68 + 0.28 * stageRatio;
  } else if (secInCycle < t6) {
    stageKind = 'observe';
    stageStartSec = t5;
    stageDur = rawPauseNorth;
    stageRatio = (secInCycle - t5) / Math.max(0.001, rawPauseNorth);
    loopFrac = 0.96;
  } else if (secInCycle < t7) {
    stageKind = 'walk';
    stageStartSec = t6;
    stageDur = w4;
    stageRatio = (secInCycle - t6) / Math.max(0.001, w4);
    loopFrac = 0.96 + 0.04 * stageRatio;
  } else {
    stageKind = 'turnaround';
    stageStartSec = t7;
    stageDur = rawTurnSouth;
    stageRatio = (secInCycle - t7) / Math.max(0.001, rawTurnSouth);
    loopFrac = 0.0;
  }

  const interp = evaluatePointOnBridgeLoop(loopFrac, profile.lateralLaneOffsetMeters);

  let movementState: 'stationary' | 'moving' | 'arrived' | 'waiting' = 'stationary';
  let behaviorState: BridgePatrolBehaviorState = 'observing';
  let destinationWaypointId: string | null = null;
  let activeRouteId: string | null = null;
  let progression = 0;
  let heading = interp.heading;
  let movementStartTime: string | null = null;
  let expectedDurationSeconds = 0;

  if (stageKind === 'walk') {
    movementState = 'moving';
    behaviorState = 'patrolling';
    destinationWaypointId = interp.destinationWaypointId;
    activeRouteId = `patrol_${profile.npcId}_${interp.corridor}_${interp.direction}`;
    progression = Math.round(stageRatio * 1000) / 1000;
    expectedDurationSeconds = Math.round(stageDur);
    const stageStartUtcMs =
      epochStartUtcMs + (cycleIndex * lapDurSec - phaseSec + stageStartSec) * 1000;
    movementStartTime = new Date(stageStartUtcMs).toISOString();
  } else if (stageKind === 'turnaround') {
    movementState = 'stationary';
    behaviorState = 'turning_around';
    const angle = stageRatio * Math.PI;
    const turnX = Math.cos(angle) * interp.heading.x - Math.sin(angle) * interp.heading.z;
    const turnZ = Math.sin(angle) * interp.heading.x + Math.cos(angle) * interp.heading.z;
    const len = Math.hypot(turnX, turnZ) || 1;
    heading = {
      x: Math.round((turnX / len) * 1000) / 1000,
      z: Math.round((turnZ / len) * 1000) / 1000,
    };
  } else {
    movementState = 'stationary';
    behaviorState = (daySeed & 1) === 0 ? 'observing' : 'paused';
  }

  let controlMode: NpcMovementControlMode = 'autonomous_bridge_patrol';
  if (pauseEval.isSuspended) {
    movementState = 'stationary';
    behaviorState = 'player_interaction_paused';
    controlMode = 'player_interaction_suspended';
  } else if (pauseEval.isResuming) {
    behaviorState = 'resuming';
  }

  return {
    movementState,
    currentWaypointId: interp.currentWaypointId,
    destinationWaypointId,
    activeRouteId,
    position: interp.position,
    forwardHeading: heading,
    progression,
    movementStartTime,
    expectedDurationSeconds,
    associatedPlaceId: BRIDGE_PATROL_DOMAIN.associatedPlaceId,
    behaviorProfileType: 'bridge_patrol',
    autonomousBehaviorState: behaviorState,
    controlMode,
    patrolDirection: interp.direction,
    patrolCycleIndex: cycleIndex,
    isSuspendedByPlayerInteraction: pauseEval.isSuspended,
    suspendedByPlayerId: pauseEval.suspendedByPlayerId,
    passingEncounterPartnerNpcId: null,
    passingEncounterSessionId: null,
    passingEncounterType: null,
    businessDomainId: BRIDGE_PATROL_DOMAIN.domainId,
    withinAuthorizedDomain: isPointInBridgePatrolDomain(interp.position),
    activeSegmentEndPosition: movementState === 'moving' ? interp.activeSegmentEndPosition : interp.position,
  };
}

// ---------------------------------------------------------------------------
// 7. SUBSYSTEM B — BUSINESS ATTENDANT BOUNDED LOCAL AUTONOMY PLANNER
// ---------------------------------------------------------------------------

export const VALERIA_CALIBRATION_TASK_ID = 'valeria_calibration_walk' as const;
export const VALERIA_CALIBRATION_STATION_WAYPOINT_ID = 'wp_optica_calibration_station' as const;
export const VALERIA_CALIBRATION_STATION_HEADING: Readonly<{ x: number; z: number }> = Object.freeze({ x: -1, z: 0 });
export const VALERIA_CALIBRATION_DAYS_OF_WEEK: readonly number[] = Object.freeze([1, 2, 3, 4, 5, 6]);
export const VALERIA_CALIBRATION_START_HOUR = 13;
export const VALERIA_CALIBRATION_START_MINUTE = 0;
export const VALERIA_CALIBRATION_END_HOUR = 13;
export const VALERIA_CALIBRATION_END_MINUTE = 45;
export const VALERIA_CALIBRATION_ARRIVAL_DWELL_SECONDS = 15;
export const VALERIA_CALIBRATION_RETURN_WINDOW_PADDING_SECONDS = 15;

// Increment 5.23.2 — Valeria Opening/Closing Business Lifecycle Constants
// Reuses the single authoritative Óptica Fashion schedule (Mon-Sat 09:00-17:00 America/Ciudad_Juarez; Sun closed)
export const VALERIA_OFF_DUTY_STATION_WAYPOINT_ID = 'wp_valeria_off_duty_station' as const;
export const VALERIA_OFF_DUTY_STATION_POSITION: Readonly<Vector3D> = Object.freeze({ x: -15.85, y: 0.2, z: 1.15 });
export const VALERIA_OFF_DUTY_STATION_HEADING: Readonly<{ x: number; z: number }> = Object.freeze({ x: 1, z: 0 });
export const VALERIA_BUSINESS_OPERATING_DAYS: readonly number[] = Object.freeze([1, 2, 3, 4, 5, 6]);
export const VALERIA_BUSINESS_OPEN_HOUR = 9;
export const VALERIA_BUSINESS_OPEN_MINUTE = 0;
export const VALERIA_BUSINESS_CLOSE_HOUR = 17;
export const VALERIA_BUSINESS_CLOSE_MINUTE = 0;
/**
 * Configurable pre-closing lead time before the established 17:00 close boundary.
 * Target: 3 minutes (16:57 local America/Ciudad_Juarez).
 * Longest validated return route (wp_optica_south_promenade -> wp_valeria_off_duty_station) is
 * 9.68 m = 8.80 s at Valeria's actual walking speed (1.10 m/s), giving > 20x safety margin.
 */
export const VALERIA_CLOSING_PREP_LEAD_MINUTES = 3;
/** Seconds Valeria remains inside while the roller shutter raises at 09:00:00 before stepping out. */
export const VALERIA_OPENING_TRANSITION_SECONDS = 5;
/** Padding before morning roaming epoch starts at 09:00:12 (5s shutter open + 5.43s indoor-to-frontage walk). */
export const VALERIA_OPENING_WALK_PADDING_SECONDS = 12;

/**
 * Returns true if the player's authoritative position lies inside Óptica Fashion's interior
 * footprint (west of the X = -11.85m shutter plane) so the shutter never traps the player inside.
 */
export function isPlayerInsideOpticaInterior(player: PlayerSpatialState | null | undefined): boolean {
  if (!player || !isValidCorridorVector3(player.position)) return false;
  const p = player.position;
  return p.x < -11.85 && p.x > -19.5 && p.z >= -1.85 && p.z <= 3.1;
}

/**
 * Returns true if Valeria's authoritative position is safely inside the approved Óptica Fashion
 * interior and clear of the front doorway threshold (X = -12.02m) and door-swing volume.
 */
export function isValeriaSafelyInsideOptica(position: Vector3D): boolean {
  if (!isValidCorridorVector3(position)) return false;
  return position.x <= -13.1 && position.x >= -19.2 && position.z >= -1.7 && position.z <= 2.95;
}

function getBusinessStationAnchorWindows(npcId: string, dayOfWeek: number): TimeWindowSec[] {
  if (dayOfWeek === 0) {
    return [{ startSec: 0, endSec: 86400 }];
  }

  const windows: TimeWindowSec[] = [];

  if (npcId === 'carlos-farmacia-attendant') {
    // Carlos Méndez: preserved continuous zero-break operating hours (07:00-22:00 Mon-Sat)
    windows.push({ startSec: 0, endSec: 7 * 3600 });
    windows.push({ startSec: 22 * 3600, endSec: 24 * 3600 });
  } else if (npcId === 'valeria-optica-attendant') {
    // Valeria Domínguez: operating hours 09:00-17:00 Mon-Sat with:
    // - 09:00:00..09:00:12 indoor opening transition & walk out from wp_valeria_off_duty_station
    // - 13:00:00..13:45:15 scheduled rear-room optical calibration (+ smooth return handoff)
    // - 16:57:00..17:00:00 pre-closing return to wp_valeria_off_duty_station & overnight closed period
    const openSec = VALERIA_BUSINESS_OPEN_HOUR * 3600 + VALERIA_BUSINESS_OPEN_MINUTE * 60;
    const closeSec = VALERIA_BUSINESS_CLOSE_HOUR * 3600 + VALERIA_BUSINESS_CLOSE_MINUTE * 60;
    const preCloseSec = closeSec - VALERIA_CLOSING_PREP_LEAD_MINUTES * 60;
    const calStartSec = VALERIA_CALIBRATION_START_HOUR * 3600 + VALERIA_CALIBRATION_START_MINUTE * 60;
    const calEndSec =
      VALERIA_CALIBRATION_END_HOUR * 3600 +
      VALERIA_CALIBRATION_END_MINUTE * 60 +
      VALERIA_CALIBRATION_RETURN_WINDOW_PADDING_SECONDS;
    windows.push({ startSec: 0, endSec: openSec + VALERIA_OPENING_WALK_PADDING_SECONDS });
    windows.push({ startSec: calStartSec, endSec: calEndSec });
    windows.push({ startSec: preCloseSec, endSec: 24 * 3600 });
  }

  return mergeSortedWindows(windows);
}

function isPlayerWithinBusinessAwareness(
  npcPos: Vector3D,
  player: PlayerSpatialState | null | undefined
): boolean {
  if (!player || !isValidCorridorVector3(player.position)) return false;
  const dx = player.position.x - npcPos.x;
  const dz = player.position.z - npcPos.z;
  const dy = Math.abs(player.position.y - npcPos.y);
  return Math.hypot(dx, dz) <= BUSINESS_CUSTOMER_AWARENESS_RANGE_METERS && dy <= 3.0;
}

function buildDeterministicPermutation(seedInput: string, length: number): number[] {
  const arr: number[] = [];
  for (let i = 0; i < length; i++) arr.push(i);
  for (let i = length - 1; i > 0; i--) {
    const h = computeDeterministicHash32(`${seedInput}|p${i}`);
    const j = h % (i + 1);
    const tmp = arr[i]!;
    arr[i] = arr[j]!;
    arr[j] = tmp;
  }
  return arr;
}

function findShortestValidLocalPath(
  startId: string,
  goalId: string,
  domain: BusinessMovementDomain,
  profile: BusinessLocalAutonomyProfile,
  waypointsRegistry: Readonly<
    Record<string, { readonly position: Vector3D; readonly associatedPlaceId: string; readonly outgoingWaypointIds: readonly string[] }>
  >,
  activeTaskId?: string | null
): string[] | null {
  if (startId === goalId) {
    const startWp = waypointsRegistry[startId];
    return startWp && isPointInSafeBusinessRoamingRegion(startWp.position, domain, activeTaskId)
      ? [startId]
      : null;
  }
  const visited = new Set<string>([startId]);
  const queue: string[][] = [[startId]];

  while (queue.length > 0) {
    const path = queue.shift()!;
    const current = path[path.length - 1]!;
    const nodeSpec = profile.localNodes[current];
    const currentWp = waypointsRegistry[current];
    const candidateNeighbors = activeTaskId
      ? currentWp?.outgoingWaypointIds ?? []
      : nodeSpec?.connectedWaypointIds ?? [];

    for (const nextId of candidateNeighbors) {
      if (visited.has(nextId)) continue;
      const edgeCheck = validateBusinessLocalGraphRoute(
        [current, nextId],
        domain,
        profile,
        waypointsRegistry,
        activeTaskId
      );
      if (!edgeCheck.valid) continue;
      const nextPath = [...path, nextId];
      if (nextId === goalId) {
        return nextPath;
      }
      visited.add(nextId);
      queue.push(nextPath);
    }
  }
  return null;
}

interface PlannedLocalTour {
  readonly tourWaypoints: readonly string[];
  readonly stopIndices: ReadonlySet<number>;
}

function selectDeterministicLocalRoamingTour(params: {
  readonly profile: BusinessLocalAutonomyProfile;
  readonly domain: BusinessMovementDomain;
  readonly waypointsRegistry: Readonly<
    Record<string, { readonly position: Vector3D; readonly associatedPlaceId: string; readonly outgoingWaypointIds: readonly string[] }>
  >;
  readonly dateKey: string;
  readonly epochIndex: number;
  readonly cycleIndex: number;
  readonly cycleSeed: number;
}): PlannedLocalTour | null {
  const { profile, domain, waypointsRegistry, dateKey, epochIndex, cycleIndex, cycleSeed } = params;
  const primaryId = profile.primaryWaypointId;
  const primaryNode = profile.localNodes[primaryId];
  if (!primaryNode) return null;

  const hasMultiZoneTransitNodes = Boolean(
    domain.connectedZones &&
    domain.connectedZones.length > 0 &&
    Object.values(profile.localNodes).some((n) => n.isTransitOnly)
  );

  // Multi-zone connected indoor/outdoor tour selection (Valeria):
  // Routes smoothly through transit doorway nodes without stopping inside thresholds,
  // naturally alternating across both exterior frontage and main showroom stops while
  // strictly excluding task-only rear laboratory nodes.
  if (hasMultiZoneTransitNodes) {
    const validFirstDestinations: Array<{
      readonly stopId: string;
      readonly pathFromPrimary: string[];
      readonly returnPath: string[];
    }> = [];

    for (const candidateId of profile.secondaryWorkstationIds) {
      if (candidateId === primaryId) continue;
      const spec = profile.localNodes[candidateId];
      if (!spec || spec.isTransitOnly || spec.taskOnly || spec.isOffDutyStation) continue;
      const outPath = findShortestValidLocalPath(primaryId, candidateId, domain, profile, waypointsRegistry);
      if (!outPath || outPath.length < 2) continue;
      const retPath = findShortestValidLocalPath(candidateId, primaryId, domain, profile, waypointsRegistry);
      if (!retPath || retPath.length < 2) continue;
      validFirstDestinations.push({ stopId: candidateId, pathFromPrimary: outPath, returnPath: retPath });
    }

    if (validFirstDestinations.length === 0) {
      return null;
    }

    // Partition into exterior-first vs interior-doorway-first pools so Valeria naturally alternates
    // between frontage activity and showroom circulation without repeating the doorway edge on consecutive cycles
    const exteriorPool = validFirstDestinations.filter(
      (d) => !profile.localNodes[d.pathFromPrimary[1]!]?.isTransitOnly
    );
    const interiorPool = validFirstDestinations.filter(
      (d) => Boolean(profile.localNodes[d.pathFromPrimary[1]!]?.isTransitOnly)
    );

    let activePool = validFirstDestinations;
    let poolCycleIndex = cycleIndex;
    let poolTag = 'all';
    if (exteriorPool.length > 0 && interiorPool.length > 0) {
      if (cycleIndex % 2 === 0) {
        activePool = exteriorPool;
        poolCycleIndex = Math.floor(cycleIndex / 2);
        poolTag = 'ext';
      } else {
        activePool = interiorPool;
        poolCycleIndex = Math.floor(cycleIndex / 2);
        poolTag = 'int';
      }
    }

    const k = activePool.length;
    let chosenIdx = 0;
    if (k >= 2) {
      const superBlock = Math.floor(poolCycleIndex / k);
      const slot = poolCycleIndex % k;
      const blkPrefix = `${profile.npcId}|${dateKey}|${profile.profileId}|ep${epochIndex}|${poolTag}|blk`;
      const perm = buildDeterministicPermutation(`${blkPrefix}${superBlock}`, k);
      if (superBlock > 0) {
        const prevPerm = buildDeterministicPermutation(`${blkPrefix}${superBlock - 1}`, k);
        if (perm[0] === prevPerm[k - 1]) {
          const tmp = perm[0]!;
          perm[0] = perm[1]!;
          perm[1] = tmp;
        }
      }
      chosenIdx = perm[slot]!;
    }

    const firstChoice = activePool[chosenIdx]!;
    const routeNodes: string[] = [...firstChoice.pathFromPrimary];
    const stopIndices = new Set<number>([routeNodes.length - 1]);
    const visitedMeaningful = new Set<string>([firstChoice.stopId]);
    const desiredStops = (cycleSeed & 1) === 0 ? 2 : 3;
    let returnTail: string[] = firstChoice.returnPath.slice(1);

    for (let step = 1; step < desiredStops; step++) {
      const curId = routeNodes[routeNodes.length - 1]!;
      const curSpec = profile.localNodes[curId];
      if (!curSpec) break;

      // Prefer direct connected non-transit neighbors in the same zone for natural retail circulation
      const nextCandidates: Array<{ readonly wpId: string; readonly stepPath: string[]; readonly retPath: string[] }> = [];
      for (const neighborId of curSpec.connectedWaypointIds) {
        if (neighborId === primaryId || visitedMeaningful.has(neighborId)) continue;
        const nSpec = profile.localNodes[neighborId];
        if (!nSpec || nSpec.isTransitOnly || nSpec.taskOnly || nSpec.isOffDutyStation) continue;
        const edgeOk = validateBusinessLocalGraphRoute([curId, neighborId], domain, profile, waypointsRegistry);
        if (!edgeOk.valid) continue;
        const retPath = findShortestValidLocalPath(neighborId, primaryId, domain, profile, waypointsRegistry);
        if (retPath && retPath.length >= 2) {
          nextCandidates.push({ wpId: neighborId, stepPath: [curId, neighborId], retPath });
        }
      }

      if (nextCandidates.length === 0) break;
      const pickIdx = deterministicRangeInt(cycleSeed, 3 + step * 5, 0, nextCandidates.length - 1);
      const picked = nextCandidates[pickIdx]!;
      for (let p = 1; p < picked.stepPath.length; p++) {
        routeNodes.push(picked.stepPath[p]!);
      }
      stopIndices.add(routeNodes.length - 1);
      visitedMeaningful.add(picked.wpId);
      returnTail = picked.retPath.slice(1);
    }

    const fullTour = [...routeNodes, ...returnTail];
    const fullCheck = validateBusinessLocalGraphRoute(fullTour, domain, profile, waypointsRegistry);
    if (fullCheck.valid) {
      return { tourWaypoints: fullTour, stopIndices };
    }

    const simpleTour = [...firstChoice.pathFromPrimary, ...firstChoice.returnPath.slice(1)];
    const simpleCheck = validateBusinessLocalGraphRoute(simpleTour, domain, profile, waypointsRegistry);
    if (simpleCheck.valid) {
      return { tourWaypoints: simpleTour, stopIndices: new Set<number>([firstChoice.pathFromPrimary.length - 1]) };
    }

    return null;
  }

  // Preserved single-zone local roaming planner (Carlos Méndez — 100% unchanged from Increment 5.22.3)
  const validFirstNeighbors: Array<{ readonly stopId: string; readonly returnPath: string[] }> = [];
  for (const neighborId of primaryNode.connectedWaypointIds) {
    if (neighborId === primaryId) continue;
    const outEdge = validateBusinessLocalGraphRoute([primaryId, neighborId], domain, profile, waypointsRegistry);
    if (!outEdge.valid) continue;
    const retPath = findShortestValidLocalPath(neighborId, primaryId, domain, profile, waypointsRegistry);
    if (retPath && retPath.length >= 2) {
      validFirstNeighbors.push({ stopId: neighborId, returnPath: retPath });
    }
  }

  if (validFirstNeighbors.length === 0) {
    return null;
  }

  // O(1) deterministic anti-repetition via superblock permutations over validFirstNeighbors
  const k = validFirstNeighbors.length;
  let chosenIdx = 0;
  if (k >= 2) {
    const superBlock = Math.floor(cycleIndex / k);
    const slot = cycleIndex % k;
    const blkPrefix = `${profile.npcId}|${dateKey}|${profile.profileId}|ep${epochIndex}|blk`;
    const perm = buildDeterministicPermutation(`${blkPrefix}${superBlock}`, k);
    if (superBlock > 0) {
      const prevPerm = buildDeterministicPermutation(`${blkPrefix}${superBlock - 1}`, k);
      if (perm[0] === prevPerm[k - 1]) {
        const tmp = perm[0]!;
        perm[0] = perm[1]!;
        perm[1] = tmp;
      }
    }
    chosenIdx = perm[slot]!;
  }

  const firstChoice = validFirstNeighbors[chosenIdx]!;
  const routeNodes: string[] = [primaryId, firstChoice.stopId];
  const stopIndices = new Set<number>([1]);
  const visitedInterior = new Set<string>([firstChoice.stopId]);

  // Extend into 2-stop or 3-stop interior circuit along valid connected edges
  const desiredInteriorStops = (cycleSeed & 1) === 0 ? 2 : 3;
  let returnTail: string[] = firstChoice.returnPath.slice(1);

  for (let step = 1; step < desiredInteriorStops; step++) {
    const curId = routeNodes[routeNodes.length - 1]!;
    const curSpec = profile.localNodes[curId];
    if (!curSpec) break;

    const nextCandidates: Array<{ readonly wpId: string; readonly retPath: string[] }> = [];
    for (const candidateId of curSpec.connectedWaypointIds) {
      if (candidateId === primaryId || visitedInterior.has(candidateId)) continue;
      const edgeOk = validateBusinessLocalGraphRoute([curId, candidateId], domain, profile, waypointsRegistry);
      if (!edgeOk.valid) continue;
      const retPath = findShortestValidLocalPath(candidateId, primaryId, domain, profile, waypointsRegistry);
      if (retPath && retPath.length >= 2) {
        nextCandidates.push({ wpId: candidateId, retPath });
      }
    }

    if (nextCandidates.length === 0) break;
    const pickIdx = deterministicRangeInt(cycleSeed, 3 + step * 5, 0, nextCandidates.length - 1);
    const picked = nextCandidates[pickIdx]!;
    const newIndex = routeNodes.length;
    routeNodes.push(picked.wpId);
    stopIndices.add(newIndex);
    visitedInterior.add(picked.wpId);
    returnTail = picked.retPath.slice(1);
  }

  const fullTour = [...routeNodes, ...returnTail];
  const fullCheck = validateBusinessLocalGraphRoute(fullTour, domain, profile, waypointsRegistry);
  if (fullCheck.valid) {
    return { tourWaypoints: fullTour, stopIndices };
  }

  // Fallback to single validated loop if extended tail ever failed
  const simpleTour = [primaryId, firstChoice.stopId, ...firstChoice.returnPath.slice(1)];
  const simpleCheck = validateBusinessLocalGraphRoute(simpleTour, domain, profile, waypointsRegistry);
  if (simpleCheck.valid) {
    return { tourWaypoints: simpleTour, stopIndices: new Set<number>([1]) };
  }

  return null;
}

export function evaluateBusinessLocalAutonomyMovement(params: {
  readonly profile: BusinessLocalAutonomyProfile;
  readonly rawEvaluatedAtMs: number;
  readonly civil: CivilTime;
  readonly activitySnapshot: NpcActivitySnapshot | null;
  readonly waypointsRegistry: Readonly<
    Record<string, { readonly position: Vector3D; readonly associatedPlaceId: string; readonly outgoingWaypointIds: readonly string[] }>
  >;
  readonly pauseIntervals?: readonly NpcInteractionPauseInterval[];
  readonly player?: PlayerSpatialState | null;
}): AutonomousMovementEvaluationResult {
  const {
    profile,
    rawEvaluatedAtMs,
    civil,
    activitySnapshot,
    waypointsRegistry,
    pauseIntervals,
    player,
  } = params;
  const domain = BUSINESS_AUTONOMY_DOMAINS[profile.npcId]!;
  const primaryWp = waypointsRegistry[profile.primaryWaypointId]!;
  const defaultHeading =
    profile.localNodes[profile.primaryWaypointId]?.observationHeading ??
    (profile.npcId === 'carlos-farmacia-attendant' ? { x: -1, z: 0 } : { x: 1, z: 0 });

  const actState = activitySnapshot?.state ?? 'available';

  const primaryZone = resolveAuthorizedBusinessZone(primaryWp.position, domain, null);

  // 1. Off-duty or closed store guard: never roam, never represent active customer service
  if (actState === 'off_duty' || actState === 'unavailable') {
    return {
      movementState: 'stationary',
      currentWaypointId: profile.primaryWaypointId,
      destinationWaypointId: null,
      activeRouteId: null,
      position: primaryWp.position,
      forwardHeading: defaultHeading,
      progression: 0,
      movementStartTime: null,
      expectedDurationSeconds: 0,
      associatedPlaceId: primaryWp.associatedPlaceId,
      behaviorProfileType: 'business_local_autonomy',
      autonomousBehaviorState: 'off_duty',
      controlMode: 'fixed_station_anchor',
      patrolDirection: 'none',
      patrolCycleIndex: 0,
      isSuspendedByPlayerInteraction: false,
      suspendedByPlayerId: null,
      passingEncounterPartnerNpcId: null,
      passingEncounterSessionId: null,
      passingEncounterType: null,
      businessDomainId: domain.domainId,
      withinAuthorizedDomain: isPointInBusinessDomain(primaryWp.position, domain),
      actualSpeedMps: 0,
      activeSegmentEndPosition: primaryWp.position,
      ...(primaryZone ? { currentZoneId: primaryZone.zoneId } : {}),
      activeTaskId: null,
      taskDestinationReachable: true,
      physicalCalibrationActive: false,
    };
  }

  // 2. Active busy service task at station prevents optional idle roaming
  if (actState === 'busy') {
    return {
      movementState: 'stationary',
      currentWaypointId: profile.primaryWaypointId,
      destinationWaypointId: null,
      activeRouteId: null,
      position: primaryWp.position,
      forwardHeading: defaultHeading,
      progression: 0,
      movementStartTime: null,
      expectedDurationSeconds: 0,
      associatedPlaceId: primaryWp.associatedPlaceId,
      behaviorProfileType: 'business_local_autonomy',
      autonomousBehaviorState: 'working_stationary',
      controlMode: 'autonomous_business_local',
      patrolDirection: 'none',
      patrolCycleIndex: 0,
      isSuspendedByPlayerInteraction: false,
      suspendedByPlayerId: null,
      passingEncounterPartnerNpcId: null,
      passingEncounterSessionId: null,
      passingEncounterType: null,
      businessDomainId: domain.domainId,
      withinAuthorizedDomain: isPointInBusinessDomain(primaryWp.position, domain),
      actualSpeedMps: 0,
      activeSegmentEndPosition: primaryWp.position,
      ...(primaryZone ? { currentZoneId: primaryZone.zoneId } : {}),
      activeTaskId: null,
      taskDestinationReachable: true,
      physicalCalibrationActive: false,
    };
  }

  const rawSecInDay =
    civil.hour * 3600 + civil.minute * 60 + civil.second + civil.millisecond / 1000;
  const dayStartUtcMs = rawEvaluatedAtMs - rawSecInDay * 1000;

  const anchors = getBusinessStationAnchorWindows(profile.npcId, civil.dayOfWeek);
  const epochs = buildComplementEpochs(0, 86400, anchors);

  let activeEpoch: TimeWindowSec | null = null;
  let epochIndex = 0;
  for (let i = 0; i < epochs.length; i++) {
    const ep = epochs[i]!;
    if (rawSecInDay >= ep.startSec && rawSecInDay < ep.endSec) {
      activeEpoch = ep;
      epochIndex = i;
      break;
    }
  }

  if (!activeEpoch) {
    const pauseEval = evaluateEffectiveAutonomousTime(rawEvaluatedAtMs, pauseIntervals, dayStartUtcMs);
    const playerNearby = isPlayerWithinBusinessAwareness(primaryWp.position, player);
    const behaviorState: BusinessAutonomyBehaviorState = pauseEval.isSuspended
      ? 'customer_interaction'
      : playerNearby
        ? 'customer_awareness'
        : 'working_stationary';
    return {
      movementState: 'stationary',
      currentWaypointId: profile.primaryWaypointId,
      destinationWaypointId: null,
      activeRouteId: null,
      position: primaryWp.position,
      forwardHeading: defaultHeading,
      progression: 0,
      movementStartTime: null,
      expectedDurationSeconds: 0,
      associatedPlaceId: primaryWp.associatedPlaceId,
      behaviorProfileType: 'business_local_autonomy',
      autonomousBehaviorState: behaviorState,
      controlMode: pauseEval.isSuspended ? 'player_interaction_suspended' : 'autonomous_business_local',
      patrolDirection: 'none',
      patrolCycleIndex: 0,
      isSuspendedByPlayerInteraction: pauseEval.isSuspended,
      suspendedByPlayerId: pauseEval.suspendedByPlayerId,
      passingEncounterPartnerNpcId: null,
      passingEncounterSessionId: null,
      passingEncounterType: null,
      businessDomainId: domain.domainId,
      withinAuthorizedDomain: isPointInBusinessDomain(primaryWp.position, domain),
      actualSpeedMps: 0,
      activeSegmentEndPosition: primaryWp.position,
    };
  }

  const epochStartUtcMs = dayStartUtcMs + activeEpoch.startSec * 1000;
  const pauseEval = evaluateEffectiveAutonomousTime(rawEvaluatedAtMs, pauseIntervals, epochStartUtcMs);
  const effSecInDay = Math.max(
    activeEpoch.startSec,
    Math.min(activeEpoch.endSec - 0.001, (pauseEval.effectiveTimeMs - dayStartUtcMs) / 1000)
  );

  const epochLenSec = activeEpoch.endSec - activeEpoch.startSec;
  const numCycles = Math.max(1, Math.round(epochLenSec / profile.nominalCycleDurationSeconds));
  const cycleDurSec = epochLenSec / numCycles;
  const elapsedInEpoch = effSecInDay - activeEpoch.startSec;
  const cycleIndex = Math.min(numCycles - 1, Math.floor(elapsedInEpoch / cycleDurSec));
  const secInCycle = elapsedInEpoch - cycleIndex * cycleDurSec;

  const dateKey = `${civil.year}-${String(civil.month).padStart(2, '0')}-${String(civil.day).padStart(2, '0')}`;
  const seed = computeDeterministicHash32(
    `${profile.npcId}|${dateKey}|${profile.profileId}|ep${epochIndex}|cy${cycleIndex}`
  );

  const tourPlan = selectDeterministicLocalRoamingTour({
    profile,
    domain,
    waypointsRegistry,
    dateKey,
    epochIndex,
    cycleIndex,
    cycleSeed: seed,
  });

  // If no valid local graph tour exists, remain safely at primary workstation
  if (!tourPlan) {
    return {
      movementState: 'stationary',
      currentWaypointId: profile.primaryWaypointId,
      destinationWaypointId: null,
      activeRouteId: null,
      position: primaryWp.position,
      forwardHeading: defaultHeading,
      progression: 0,
      movementStartTime: null,
      expectedDurationSeconds: 0,
      associatedPlaceId: primaryWp.associatedPlaceId,
      behaviorProfileType: 'business_local_autonomy',
      autonomousBehaviorState: pauseEval.isSuspended ? 'customer_interaction' : 'working_stationary',
      controlMode: pauseEval.isSuspended ? 'player_interaction_suspended' : 'autonomous_business_local',
      patrolDirection: 'none',
      patrolCycleIndex: cycleIndex,
      isSuspendedByPlayerInteraction: pauseEval.isSuspended,
      suspendedByPlayerId: pauseEval.suspendedByPlayerId,
      passingEncounterPartnerNpcId: null,
      passingEncounterSessionId: null,
      passingEncounterType: null,
      businessDomainId: domain.domainId,
      withinAuthorizedDomain: true,
      actualSpeedMps: 0,
      activeSegmentEndPosition: primaryWp.position,
    };
  }

  // Compute exact geometric travel durations from segment distance / profile.preferredSpeedMps
  // (never stretch short segments with minimum-5s floors; extra cycle time stays in calm dwell)
  interface TourSegment {
    readonly fromId: string;
    readonly toId: string;
    readonly fromPos: Vector3D;
    readonly toPos: Vector3D;
    readonly dist: number;
    readonly rawWalkDur: number;
    readonly inspectDwellAfter: number;
    readonly isReturnFinal: boolean;
  }

  const segments: TourSegment[] = [];
  let totalRawWalk = 0;
  let totalInspectDwell = 0;

  for (let i = 0; i < tourPlan.tourWaypoints.length - 1; i++) {
    const fromId = tourPlan.tourWaypoints[i]!;
    const toId = tourPlan.tourWaypoints[i + 1]!;
    const fromPos = waypointsRegistry[fromId]!.position;
    const toPos = waypointsRegistry[toId]!.position;
    const dist = Math.hypot(toPos.x - fromPos.x, toPos.y - fromPos.y, toPos.z - fromPos.z);
    const rawWalkDur = dist > 1e-4 ? dist / profile.preferredSpeedMps : 0;
    const isStopNode = tourPlan.stopIndices.has(i + 1);
    const inspectDwell = isStopNode
      ? deterministicRangeInt(
        seed,
        (i * 5 + 11) % 24,
        profile.localInspectionDwellSeconds.min,
        profile.localInspectionDwellSeconds.max
      )
      : 0;

    totalRawWalk += rawWalkDur;
    totalInspectDwell += inspectDwell;
    segments.push({
      fromId,
      toId,
      fromPos,
      toPos,
      dist,
      rawWalkDur,
      inspectDwellAfter: inspectDwell,
      isReturnFinal: i === tourPlan.tourWaypoints.length - 2 && toId === profile.primaryWaypointId,
    });
  }

  const rawWorkDwell = deterministicRangeInt(
    seed,
    5,
    profile.workStationDwellSeconds.min,
    profile.workStationDwellSeconds.max
  );

  // Preserve exact walking speed (walkScale = 1) whenever totalRawWalk fits comfortably in cycleDurSec
  const availableDwellBudget = Math.max(0, cycleDurSec - totalRawWalk);
  const desiredTotalDwell = rawWorkDwell + totalInspectDwell;
  const dwellScale = desiredTotalDwell > availableDwellBudget && desiredTotalDwell > 0
    ? availableDwellBudget / desiredTotalDwell
    : 1;
  const walkScale = totalRawWalk > cycleDurSec && totalRawWalk > 0
    ? cycleDurSec / totalRawWalk
    : 1;

  const startWorkDur = rawWorkDwell * dwellScale;

  let pos: Vector3D = primaryWp.position;
  let heading = defaultHeading;
  let movementState: 'stationary' | 'moving' | 'arrived' | 'waiting' = 'stationary';
  let behaviorState: BusinessAutonomyBehaviorState = 'working_stationary';
  let currentWaypointId = profile.primaryWaypointId;
  let destinationWaypointId: string | null = null;
  let activeRouteId: string | null = null;
  let progression = 0;
  let movementStartTime: string | null = null;
  let expectedDurationSeconds = 0;
  let actualSpeedMps = 0;
  let activeSegmentEndPosition: Vector3D = primaryWp.position;

  if (secInCycle < startWorkDur) {
    pos = primaryWp.position;
    heading = defaultHeading;
    movementState = 'stationary';
    behaviorState = 'working_stationary';
    currentWaypointId = profile.primaryWaypointId;
    activeSegmentEndPosition = primaryWp.position;
  } else {
    let cursorSec = startWorkDur;
    let resolved = false;

    for (let i = 0; i < segments.length; i++) {
      const seg = segments[i]!;
      const walkDur = seg.rawWalkDur * walkScale;
      const walkEnd = cursorSec + walkDur;

      if (secInCycle < walkEnd && walkDur > 1e-4) {
        const r = Math.max(0, Math.min(1, (secInCycle - cursorSec) / walkDur));
        pos = {
          x: Math.round((seg.fromPos.x + (seg.toPos.x - seg.fromPos.x) * r) * 1000) / 1000,
          y: Math.round((seg.fromPos.y + (seg.toPos.y - seg.fromPos.y) * r) * 1000) / 1000,
          z: Math.round((seg.fromPos.z + (seg.toPos.z - seg.fromPos.z) * r) * 1000) / 1000,
        };
        const dx = seg.toPos.x - seg.fromPos.x;
        const dz = seg.toPos.z - seg.fromPos.z;
        const len = Math.hypot(dx, dz) || 1;
        heading = {
          x: Math.round((dx / len) * 1000) / 1000,
          z: Math.round((dz / len) * 1000) / 1000,
        };
        movementState = 'moving';
        behaviorState = seg.isReturnFinal ? 'returning_to_station' : 'local_roaming';
        currentWaypointId = r < 0.5 ? seg.fromId : seg.toId;
        destinationWaypointId = seg.toId;
        activeRouteId = `local_${profile.npcId}_${seg.fromId}_to_${seg.toId}`;
        progression = Math.round(r * 1000) / 1000;
        expectedDurationSeconds = Math.round(walkDur * 1000) / 1000;
        actualSpeedMps = Math.round((seg.dist / walkDur) * 1000) / 1000;
        activeSegmentEndPosition = seg.toPos;
        movementStartTime = new Date(
          epochStartUtcMs + (cycleIndex * cycleDurSec + cursorSec) * 1000
        ).toISOString();
        resolved = true;
        break;
      }

      cursorSec = walkEnd;
      const inspectDur = seg.inspectDwellAfter * dwellScale;
      const inspectEnd = cursorSec + inspectDur;

      if (inspectDur > 0 && secInCycle < inspectEnd) {
        pos = seg.toPos;
        heading = profile.localNodes[seg.toId]?.observationHeading ?? defaultHeading;
        movementState = 'stationary';
        behaviorState = 'idle_observation';
        currentWaypointId = seg.toId;
        destinationWaypointId = null;
        activeRouteId = null;
        progression = 0;
        actualSpeedMps = 0;
        activeSegmentEndPosition = seg.toPos;
        resolved = true;
        break;
      }

      cursorSec = inspectEnd;
    }

    if (!resolved) {
      pos = primaryWp.position;
      heading = defaultHeading;
      movementState = 'stationary';
      behaviorState = 'working_stationary';
      currentWaypointId = profile.primaryWaypointId;
      actualSpeedMps = 0;
      activeSegmentEndPosition = primaryWp.position;
    }
  }

  // Defensive containment clamp: enforce safe roaming region and absolute domain
  const insideSafe = isPointInSafeBusinessRoamingRegion(pos, domain);
  if (!insideSafe) {
    pos = primaryWp.position;
    heading = defaultHeading;
    movementState = 'stationary';
    behaviorState = 'working_stationary';
    currentWaypointId = profile.primaryWaypointId;
    destinationWaypointId = null;
    activeRouteId = null;
    progression = 0;
    actualSpeedMps = 0;
    activeSegmentEndPosition = primaryWp.position;
  }

  // Upgrade stationary state to customer_awareness if player is within range
  if (movementState === 'stationary' && isPlayerWithinBusinessAwareness(pos, player)) {
    behaviorState = 'customer_awareness';
  }

  let controlMode: NpcMovementControlMode = 'autonomous_business_local';
  if (pauseEval.isSuspended) {
    movementState = 'stationary';
    behaviorState = 'customer_interaction';
    controlMode = 'player_interaction_suspended';
    actualSpeedMps = 0;
    activeSegmentEndPosition = pos;
  }

  const resolvedZone = resolveAuthorizedBusinessZone(pos, domain, null);

  return {
    movementState,
    currentWaypointId,
    destinationWaypointId,
    activeRouteId,
    position: pos,
    forwardHeading: heading,
    progression,
    movementStartTime,
    expectedDurationSeconds,
    associatedPlaceId: domain.associatedPlaceId,
    behaviorProfileType: 'business_local_autonomy',
    autonomousBehaviorState: behaviorState,
    controlMode,
    patrolDirection: 'none',
    patrolCycleIndex: cycleIndex,
    isSuspendedByPlayerInteraction: pauseEval.isSuspended,
    suspendedByPlayerId: pauseEval.suspendedByPlayerId,
    passingEncounterPartnerNpcId: null,
    passingEncounterSessionId: null,
    passingEncounterType: null,
    businessDomainId: domain.domainId,
    withinAuthorizedDomain: isPointInBusinessDomain(pos, domain, null),
    actualSpeedMps,
    activeSegmentEndPosition,
    ...(resolvedZone ? { currentZoneId: resolvedZone.zoneId } : {}),
    activeTaskId: null,
    taskDestinationReachable: true,
    physicalCalibrationActive: false,
    ...(profile.npcId === 'valeria-optica-attendant'
      ? { businessLifecycleState: 'business_open' as const, shutterClosedPermitted: false }
      : {}),
  };
}

// ---------------------------------------------------------------------------
// 7B. VALERIA SCHEDULED CALIBRATION LIFECYCLE (INCREMENT 5.23.1)
// ---------------------------------------------------------------------------

interface PolylineLeg {
  readonly fromId: string;
  readonly toId: string;
  readonly fromPos: Vector3D;
  readonly toPos: Vector3D;
  readonly dist: number;
  readonly durationSec: number;
}

interface PlannedPolyline {
  readonly legs: readonly PolylineLeg[];
  readonly totalDistance: number;
  readonly totalDurationSec: number;
  readonly finalWaypointId: string;
  readonly finalPosition: Vector3D;
}

function buildValidatedPolylineFromPosition(params: {
  readonly startPos: Vector3D;
  readonly startWaypointId: string;
  readonly startSegmentDestId?: string | null;
  readonly goalWaypointId: string;
  readonly domain: BusinessMovementDomain;
  readonly profile: BusinessLocalAutonomyProfile;
  readonly waypointsRegistry: Readonly<
    Record<string, { readonly position: Vector3D; readonly associatedPlaceId: string; readonly outgoingWaypointIds: readonly string[] }>
  >;
  readonly speedMps: number;
  readonly activeTaskId: string | null;
}): PlannedPolyline | null {
  const {
    startPos,
    startWaypointId,
    startSegmentDestId,
    goalWaypointId,
    domain,
    profile,
    waypointsRegistry,
    speedMps,
    activeTaskId,
  } = params;

  const goalWp = waypointsRegistry[goalWaypointId];
  if (!goalWp || !isPointInSafeBusinessRoamingRegion(goalWp.position, domain, activeTaskId)) {
    return null;
  }

  // Candidate anchor nodes directly reachable from startPos inside the same authorized convex zone
  const candidateFirstNodes: string[] = [];
  if (startSegmentDestId && waypointsRegistry[startSegmentDestId]) {
    candidateFirstNodes.push(startSegmentDestId);
  }
  if (waypointsRegistry[startWaypointId] && !candidateFirstNodes.includes(startWaypointId)) {
    candidateFirstNodes.push(startWaypointId);
  }
  // Also consider neighbors of startWaypointId in case startPos sits on a connecting edge
  const startWpObj = waypointsRegistry[startWaypointId];
  if (startWpObj) {
    for (const nId of startWpObj.outgoingWaypointIds) {
      if (!candidateFirstNodes.includes(nId) && waypointsRegistry[nId]) {
        candidateFirstNodes.push(nId);
      }
    }
  }

  let bestPolyline: PlannedPolyline | null = null;

  for (const anchorId of candidateFirstNodes) {
    const anchorWp = waypointsRegistry[anchorId];
    if (!anchorWp) continue;
    const distToAnchor = Math.hypot(
      anchorWp.position.x - startPos.x,
      anchorWp.position.y - startPos.y,
      anchorWp.position.z - startPos.z
    );
    if (
      distToAnchor > 1e-4 &&
      !isSegmentInSafeBusinessRoamingRegion(startPos, anchorWp.position, domain, activeTaskId)
    ) {
      continue;
    }

    const graphPath = findShortestValidLocalPath(
      anchorId,
      goalWaypointId,
      domain,
      profile,
      waypointsRegistry,
      activeTaskId
    );
    if (!graphPath || graphPath.length === 0) continue;

    // Validate complete graph path
    if (graphPath.length >= 2) {
      const routeCheck = validateBusinessLocalGraphRoute(
        graphPath,
        domain,
        profile,
        waypointsRegistry,
        activeTaskId
      );
      if (!routeCheck.valid) continue;
    }

    const legs: PolylineLeg[] = [];
    let totalDist = 0;
    let totalDur = 0;

    if (distToAnchor > 1e-4) {
      const dur = distToAnchor / speedMps;
      legs.push({
        fromId: startWaypointId,
        toId: anchorId,
        fromPos: startPos,
        toPos: anchorWp.position,
        dist: distToAnchor,
        durationSec: dur,
      });
      totalDist += distToAnchor;
      totalDur += dur;
    }

    for (let i = 0; i < graphPath.length - 1; i++) {
      const fId = graphPath[i]!;
      const tId = graphPath[i + 1]!;
      const fPos = waypointsRegistry[fId]!.position;
      const tPos = waypointsRegistry[tId]!.position;
      const d = Math.hypot(tPos.x - fPos.x, tPos.y - fPos.y, tPos.z - fPos.z);
      if (d <= 1e-4) continue;
      const dur = d / speedMps;
      legs.push({
        fromId: fId,
        toId: tId,
        fromPos: fPos,
        toPos: tPos,
        dist: d,
        durationSec: dur,
      });
      totalDist += d;
      totalDur += dur;
    }

    const candidate: PlannedPolyline = {
      legs,
      totalDistance: totalDist,
      totalDurationSec: totalDur,
      finalWaypointId: goalWaypointId,
      finalPosition: goalWp.position,
    };

    if (!bestPolyline || candidate.totalDistance < bestPolyline.totalDistance - 1e-6) {
      bestPolyline = candidate;
    }
  }

  return bestPolyline;
}

function samplePolylineAtElapsed(
  polyline: PlannedPolyline,
  elapsedSec: number
): {
  readonly position: Vector3D;
  readonly heading: { readonly x: number; readonly z: number };
  readonly currentWaypointId: string;
  readonly activeSegmentEndPosition: Vector3D;
  readonly progression: number;
} {
  if (polyline.legs.length === 0 || polyline.totalDurationSec <= 1e-6) {
    return {
      position: polyline.finalPosition,
      heading: VALERIA_CALIBRATION_STATION_HEADING,
      currentWaypointId: polyline.finalWaypointId,
      activeSegmentEndPosition: polyline.finalPosition,
      progression: 1,
    };
  }

  const clamped = Math.max(0, Math.min(polyline.totalDurationSec, elapsedSec));
  let cursor = 0;
  for (let i = 0; i < polyline.legs.length; i++) {
    const leg = polyline.legs[i]!;
    const legEnd = cursor + leg.durationSec;
    if (clamped <= legEnd || i === polyline.legs.length - 1) {
      const r = leg.durationSec > 1e-6 ? Math.max(0, Math.min(1, (clamped - cursor) / leg.durationSec)) : 1;
      const pos: Vector3D = {
        x: Math.round((leg.fromPos.x + (leg.toPos.x - leg.fromPos.x) * r) * 1000) / 1000,
        y: Math.round((leg.fromPos.y + (leg.toPos.y - leg.fromPos.y) * r) * 1000) / 1000,
        z: Math.round((leg.fromPos.z + (leg.toPos.z - leg.fromPos.z) * r) * 1000) / 1000,
      };
      const dx = leg.toPos.x - leg.fromPos.x;
      const dz = leg.toPos.z - leg.fromPos.z;
      const len = Math.hypot(dx, dz) || 1;
      return {
        position: pos,
        heading: {
          x: Math.round((dx / len) * 1000) / 1000,
          z: Math.round((dz / len) * 1000) / 1000,
        },
        currentWaypointId: r < 0.5 ? leg.fromId : leg.toId,
        activeSegmentEndPosition: leg.toPos,
        progression: Math.round((clamped / polyline.totalDurationSec) * 1000) / 1000,
      };
    }
    cursor = legEnd;
  }

  return {
    position: polyline.finalPosition,
    heading: VALERIA_CALIBRATION_STATION_HEADING,
    currentWaypointId: polyline.finalWaypointId,
    activeSegmentEndPosition: polyline.finalPosition,
    progression: 1,
  };
}

/**
 * Evaluates Valeria Domínguez's continuous, C^0-continuous scheduled calibration lifecycle
 * (`valeria_calibration_walk`, Mon-Sat 13:00-13:45 America/Ciudad_Juarez + smooth return walk).
 *
 * Guarantees:
 * - Zero teleportation: plans the shortest validated multi-zone path from Valeria's exact
 *   position at 13:00:00 (whether outside or already inside the showroom).
 * - Strict Zone D task-only authorization: rear laboratory access unlocks ONLY for this task
 *   and releases as soon as Valeria returns into the main showroom after 13:45:00.
 * - Full player-interaction suspension & resumption during outbound transit, calibration dwell,
 *   and return walk without restarting or duplicating the task.
 * - Safe unreachable-destination fallback: if the station or route is invalid, stays safely in
 *   authorized space and reports `taskDestinationReachable: false`, `physicalCalibrationActive: false`.
 *
 * Returns `null` when outside the active calibration + return lifecycle window so normal
 * connected indoor/outdoor autonomy runs uninterrupted.
 */
export function evaluateValeriaCalibrationLifecycle(params: {
  readonly rawEvaluatedAtMs: number;
  readonly civil: CivilTime;
  readonly waypointsRegistry: Readonly<
    Record<string, { readonly position: Vector3D; readonly associatedPlaceId: string; readonly outgoingWaypointIds: readonly string[] }>
  >;
  readonly pauseIntervals?: readonly NpcInteractionPauseInterval[];
  readonly player?: PlayerSpatialState | null;
}): AutonomousMovementEvaluationResult | null {
  const { rawEvaluatedAtMs, civil, waypointsRegistry, pauseIntervals } = params;

  if (!VALERIA_CALIBRATION_DAYS_OF_WEEK.includes(civil.dayOfWeek)) {
    return null;
  }

  const calStartSec = VALERIA_CALIBRATION_START_HOUR * 3600 + VALERIA_CALIBRATION_START_MINUTE * 60;
  const calEndSec = VALERIA_CALIBRATION_END_HOUR * 3600 + VALERIA_CALIBRATION_END_MINUTE * 60;
  const preCloseSec =
    VALERIA_BUSINESS_CLOSE_HOUR * 3600 +
    VALERIA_BUSINESS_CLOSE_MINUTE * 60 -
    VALERIA_CLOSING_PREP_LEAD_MINUTES * 60;
  const rawSecInDay =
    civil.hour * 3600 + civil.minute * 60 + civil.second + civil.millisecond / 1000;

  if (rawSecInDay < calStartSec || rawSecInDay >= preCloseSec) {
    return null;
  }

  const profile = BUSINESS_LOCAL_AUTONOMY_PROFILES['valeria-optica-attendant']!;
  const domain = OPTICA_FASHION_AUTONOMY_DOMAIN;
  const primaryWp = waypointsRegistry[profile.primaryWaypointId]!;
  const defaultHeading = profile.localNodes[profile.primaryWaypointId]?.observationHeading ?? { x: 1, z: 0 };

  const dayStartUtcMs = rawEvaluatedAtMs - rawSecInDay * 1000;
  const calStartUtcMs = dayStartUtcMs + calStartSec * 1000;
  const calEndUtcMs = dayStartUtcMs + calEndSec * 1000;

  // Determine Valeria's exact position at the instant calibration starts (12:59:59.999)
  const preTaskCivil: CivilTime = {
    ...civil,
    hour: 12,
    minute: 59,
    second: 59,
    millisecond: 999,
  };
  const preTaskPauses = pauseIntervals
    ?.filter((iv) => iv.startedAtMs < calStartUtcMs)
    .map((iv) => ({
      ...iv,
      endedAtMs: iv.endedAtMs === null ? calStartUtcMs - 1 : Math.min(iv.endedAtMs, calStartUtcMs - 1),
    }));

  const preTaskState = evaluateBusinessLocalAutonomyMovement({
    profile,
    rawEvaluatedAtMs: calStartUtcMs - 1,
    civil: preTaskCivil,
    activitySnapshot: null,
    waypointsRegistry,
    pauseIntervals: preTaskPauses,
    player: null,
  });

  const calibWp = waypointsRegistry[VALERIA_CALIBRATION_STATION_WAYPOINT_ID];
  const calibInsideRearRoom = Boolean(
    calibWp && isPointInSpatialZone(calibWp.position, OPTICA_REAR_CALIBRATION_ROOM_ZONE)
  );

  const outboundPolyline = calibInsideRearRoom
    ? buildValidatedPolylineFromPosition({
      startPos: preTaskState.position,
      startWaypointId: preTaskState.currentWaypointId,
      startSegmentDestId: preTaskState.destinationWaypointId,
      goalWaypointId: VALERIA_CALIBRATION_STATION_WAYPOINT_ID,
      domain,
      profile,
      waypointsRegistry,
      speedMps: profile.preferredSpeedMps,
      activeTaskId: VALERIA_CALIBRATION_TASK_ID,
    })
    : null;

  // Phase 1 & 2: During scheduled calibration window [13:00:00, 13:45:00)
  if (rawEvaluatedAtMs < calEndUtcMs) {
    const pauseEval = evaluateEffectiveAutonomousTime(rawEvaluatedAtMs, pauseIntervals, calStartUtcMs);

    // Safe fallback if destination or route is invalid: never retry invalid edges or claim calibration
    if (!outboundPolyline || !calibWp) {
      const safeFallbackPos = isPointInSafeBusinessRoamingRegion(preTaskState.position, domain, null)
        ? preTaskState.position
        : primaryWp.position;
      const safeFallbackWpId = isPointInSafeBusinessRoamingRegion(preTaskState.position, domain, null)
        ? preTaskState.currentWaypointId
        : profile.primaryWaypointId;
      const fallbackZone = resolveAuthorizedBusinessZone(safeFallbackPos, domain, null);
      return {
        movementState: 'stationary',
        currentWaypointId: safeFallbackWpId,
        destinationWaypointId: null,
        activeRouteId: null,
        position: safeFallbackPos,
        forwardHeading: defaultHeading,
        progression: 0,
        movementStartTime: null,
        expectedDurationSeconds: 0,
        associatedPlaceId: domain.associatedPlaceId,
        behaviorProfileType: 'business_local_autonomy',
        autonomousBehaviorState: pauseEval.isSuspended ? 'customer_interaction' : 'working_stationary',
        controlMode: pauseEval.isSuspended ? 'player_interaction_suspended' : 'scheduled_routine_transition',
        patrolDirection: 'none',
        patrolCycleIndex: 0,
        isSuspendedByPlayerInteraction: pauseEval.isSuspended,
        suspendedByPlayerId: pauseEval.suspendedByPlayerId,
        passingEncounterPartnerNpcId: null,
        passingEncounterSessionId: null,
        passingEncounterType: null,
        businessDomainId: domain.domainId,
        withinAuthorizedDomain: isPointInBusinessDomain(safeFallbackPos, domain, null),
        actualSpeedMps: 0,
        activeSegmentEndPosition: safeFallbackPos,
        currentZoneId: fallbackZone?.zoneId ?? null,
        activeTaskId: VALERIA_CALIBRATION_TASK_ID,
        taskDestinationReachable: false,
        physicalCalibrationActive: false,
        businessLifecycleState: 'business_open',
        shutterClosedPermitted: false,
      };
    }

    const effElapsedSec = Math.max(0, (pauseEval.effectiveTimeMs - calStartUtcMs) / 1000);

    // Outbound walk from exact current location to Optica_CalibrationStation
    if (effElapsedSec < outboundPolyline.totalDurationSec) {
      const sample = samplePolylineAtElapsed(outboundPolyline, effElapsedSec);
      const zone = resolveAuthorizedBusinessZone(sample.position, domain, VALERIA_CALIBRATION_TASK_ID);
      const isSuspended = pauseEval.isSuspended;

      return {
        movementState: isSuspended ? 'stationary' : 'moving',
        currentWaypointId: sample.currentWaypointId,
        destinationWaypointId: VALERIA_CALIBRATION_STATION_WAYPOINT_ID,
        activeRouteId: 'route_valeria_to_calibration',
        position: sample.position,
        forwardHeading: sample.heading,
        progression: sample.progression,
        movementStartTime: new Date(calStartUtcMs).toISOString(),
        expectedDurationSeconds: Math.round(outboundPolyline.totalDurationSec * 1000) / 1000,
        associatedPlaceId: domain.associatedPlaceId,
        behaviorProfileType: 'business_local_autonomy',
        autonomousBehaviorState: isSuspended ? 'customer_interaction' : 'scheduled_transit',
        controlMode: isSuspended ? 'player_interaction_suspended' : 'scheduled_routine_transition',
        patrolDirection: 'none',
        patrolCycleIndex: 0,
        isSuspendedByPlayerInteraction: isSuspended,
        suspendedByPlayerId: pauseEval.suspendedByPlayerId,
        passingEncounterPartnerNpcId: null,
        passingEncounterSessionId: null,
        passingEncounterType: null,
        businessDomainId: domain.domainId,
        withinAuthorizedDomain: isPointInBusinessDomain(sample.position, domain, VALERIA_CALIBRATION_TASK_ID),
        actualSpeedMps: isSuspended ? 0 : profile.preferredSpeedMps,
        activeSegmentEndPosition: isSuspended ? sample.position : sample.activeSegmentEndPosition,
        currentZoneId: zone?.zoneId ?? null,
        activeTaskId: VALERIA_CALIBRATION_TASK_ID,
        taskDestinationReachable: true,
        physicalCalibrationActive: false,
        businessLifecycleState: 'business_open',
        shutterClosedPermitted: false,
      };
    }

    // Arrived & performing calibration at Optica_CalibrationStation (-17.85, 0.20, -0.50), facing West (-1, 0)
    const isArrivingDwell =
      effElapsedSec < outboundPolyline.totalDurationSec + VALERIA_CALIBRATION_ARRIVAL_DWELL_SECONDS;
    const isSuspended = pauseEval.isSuspended;
    const stationZone = resolveAuthorizedBusinessZone(
      calibWp.position,
      domain,
      VALERIA_CALIBRATION_TASK_ID
    );

    return {
      movementState: isSuspended ? 'stationary' : isArrivingDwell ? 'arrived' : 'waiting',
      currentWaypointId: VALERIA_CALIBRATION_STATION_WAYPOINT_ID,
      destinationWaypointId: !isSuspended && isArrivingDwell ? VALERIA_CALIBRATION_STATION_WAYPOINT_ID : null,
      activeRouteId: null,
      position: calibWp.position,
      forwardHeading: VALERIA_CALIBRATION_STATION_HEADING,
      progression: !isSuspended && isArrivingDwell ? 1.0 : 0.0,
      movementStartTime: null,
      expectedDurationSeconds: isArrivingDwell
        ? Math.round(outboundPolyline.totalDurationSec * 1000) / 1000
        : 0,
      associatedPlaceId: domain.associatedPlaceId,
      behaviorProfileType: 'business_local_autonomy',
      autonomousBehaviorState: isSuspended ? 'customer_interaction' : 'scheduled_station_task',
      controlMode: isSuspended ? 'player_interaction_suspended' : 'scheduled_routine_transition',
      patrolDirection: 'none',
      patrolCycleIndex: 0,
      isSuspendedByPlayerInteraction: isSuspended,
      suspendedByPlayerId: pauseEval.suspendedByPlayerId,
      passingEncounterPartnerNpcId: null,
      passingEncounterSessionId: null,
      passingEncounterType: null,
      businessDomainId: domain.domainId,
      withinAuthorizedDomain: isPointInBusinessDomain(calibWp.position, domain, VALERIA_CALIBRATION_TASK_ID),
      actualSpeedMps: 0,
      activeSegmentEndPosition: calibWp.position,
      currentZoneId: stationZone?.zoneId ?? OPTICA_REAR_CALIBRATION_ROOM_ZONE.zoneId,
      activeTaskId: VALERIA_CALIBRATION_TASK_ID,
      taskDestinationReachable: true,
      physicalCalibrationActive: !isSuspended,
      businessLifecycleState: 'business_open',
      shutterClosedPermitted: false,
    };
  }

  // Phase 3: Task finished (13:45:00+) — continuous return from actual end-of-task position back to showroom/exterior
  if (!outboundPolyline || !calibWp) {
    // Task never entered rear room; immediately hand back to normal afternoon autonomy
    return null;
  }

  // Compute exact position at 13:44:59.999 (normally Optica_CalibrationStation; handles rare prolonged pauses)
  const endTaskPauseEval = evaluateEffectiveAutonomousTime(calEndUtcMs - 1, pauseIntervals, calStartUtcMs);
  const effAtEndSec = Math.max(0, (endTaskPauseEval.effectiveTimeMs - calStartUtcMs) / 1000);
  let returnStartPos: Vector3D = calibWp.position;
  let returnStartWpId: string = VALERIA_CALIBRATION_STATION_WAYPOINT_ID;
  if (effAtEndSec < outboundPolyline.totalDurationSec) {
    const partialSample = samplePolylineAtElapsed(outboundPolyline, effAtEndSec);
    returnStartPos = partialSample.position;
    returnStartWpId = partialSample.currentWaypointId;
  }

  // Only authorize Zone D rear-room exit if Valeria is physically inside Zone D at 13:45:00;
  // never re-enter rear lab if an extended dialogue held her outside/in showroom past 13:45:00 (Scenario B).
  const startedReturnInPublicZone = resolveAuthorizedBusinessZone(returnStartPos, domain, null) !== null;
  const returnAuthTaskId = startedReturnInPublicZone ? null : VALERIA_CALIBRATION_TASK_ID;

  const returnPolyline = buildValidatedPolylineFromPosition({
    startPos: returnStartPos,
    startWaypointId: returnStartWpId,
    startSegmentDestId: null,
    goalWaypointId: profile.primaryWaypointId,
    domain,
    profile,
    waypointsRegistry,
    speedMps: profile.preferredSpeedMps,
    activeTaskId: returnAuthTaskId,
  });

  if (!returnPolyline) {
    return null;
  }

  const returnPauseEval = evaluateEffectiveAutonomousTime(rawEvaluatedAtMs, pauseIntervals, calEndUtcMs);
  const effReturnSec = Math.max(0, (returnPauseEval.effectiveTimeMs - calEndUtcMs) / 1000);

  if (effReturnSec < returnPolyline.totalDurationSec) {
    const sample = samplePolylineAtElapsed(returnPolyline, effReturnSec);
    // Release task-only rear laboratory authorization as soon as Valeria crosses into public showroom/frontage
    const publicZone = resolveAuthorizedBusinessZone(sample.position, domain, null);
    const activeExitTaskId = publicZone ? null : VALERIA_CALIBRATION_TASK_ID;
    const currentZone = publicZone ?? resolveAuthorizedBusinessZone(sample.position, domain, VALERIA_CALIBRATION_TASK_ID);
    const isSuspended = returnPauseEval.isSuspended;

    return {
      movementState: isSuspended ? 'stationary' : 'moving',
      currentWaypointId: sample.currentWaypointId,
      destinationWaypointId: profile.primaryWaypointId,
      activeRouteId: 'route_valeria_from_calibration',
      position: sample.position,
      forwardHeading: sample.heading,
      progression: sample.progression,
      movementStartTime: new Date(calEndUtcMs).toISOString(),
      expectedDurationSeconds: Math.round(returnPolyline.totalDurationSec * 1000) / 1000,
      associatedPlaceId: domain.associatedPlaceId,
      behaviorProfileType: 'business_local_autonomy',
      autonomousBehaviorState: isSuspended ? 'customer_interaction' : 'returning_to_station',
      controlMode: isSuspended ? 'player_interaction_suspended' : 'scheduled_routine_transition',
      patrolDirection: 'none',
      patrolCycleIndex: 0,
      isSuspendedByPlayerInteraction: isSuspended,
      suspendedByPlayerId: returnPauseEval.suspendedByPlayerId,
      passingEncounterPartnerNpcId: null,
      passingEncounterSessionId: null,
      passingEncounterType: null,
      businessDomainId: domain.domainId,
      withinAuthorizedDomain: isPointInBusinessDomain(sample.position, domain, activeExitTaskId),
      actualSpeedMps: isSuspended ? 0 : profile.preferredSpeedMps,
      activeSegmentEndPosition: isSuspended ? sample.position : sample.activeSegmentEndPosition,
      currentZoneId: currentZone?.zoneId ?? null,
      activeTaskId: activeExitTaskId,
      taskDestinationReachable: true,
      physicalCalibrationActive: false,
      businessLifecycleState: 'business_open',
      shutterClosedPermitted: false,
    };
  }

  // Brief arrival settle at primary station before afternoon roaming epoch starts at 13:45:15
  if (rawSecInDay < calEndSec + VALERIA_CALIBRATION_RETURN_WINDOW_PADDING_SECONDS) {
    const isSuspended = returnPauseEval.isSuspended;
    const primaryZone = resolveAuthorizedBusinessZone(primaryWp.position, domain, null);
    return {
      movementState: 'stationary',
      currentWaypointId: profile.primaryWaypointId,
      destinationWaypointId: null,
      activeRouteId: null,
      position: primaryWp.position,
      forwardHeading: defaultHeading,
      progression: 0,
      movementStartTime: null,
      expectedDurationSeconds: 0,
      associatedPlaceId: domain.associatedPlaceId,
      behaviorProfileType: 'business_local_autonomy',
      autonomousBehaviorState: isSuspended ? 'customer_interaction' : 'working_stationary',
      controlMode: isSuspended ? 'player_interaction_suspended' : 'autonomous_business_local',
      patrolDirection: 'none',
      patrolCycleIndex: 0,
      isSuspendedByPlayerInteraction: isSuspended,
      suspendedByPlayerId: returnPauseEval.suspendedByPlayerId,
      passingEncounterPartnerNpcId: null,
      passingEncounterSessionId: null,
      passingEncounterType: null,
      businessDomainId: domain.domainId,
      withinAuthorizedDomain: isPointInBusinessDomain(primaryWp.position, domain, null),
      actualSpeedMps: 0,
      activeSegmentEndPosition: primaryWp.position,
      currentZoneId: primaryZone?.zoneId ?? OPTICA_EXTERIOR_FRONTAGE_ZONE.zoneId,
      activeTaskId: null,
      taskDestinationReachable: true,
      physicalCalibrationActive: false,
      businessLifecycleState: 'business_open',
      shutterClosedPermitted: false,
    };
  }

  // Return complete: hand off seamlessly to afternoon connected showroom/frontage autonomy
  return null;
}

// ---------------------------------------------------------------------------
// 7C. VALERIA DAILY BUSINESS OPENING / CLOSING LIFECYCLE (INCREMENT 5.23.2)
// ---------------------------------------------------------------------------

/**
 * Evaluates Valeria Domínguez's complete deterministic daily business lifecycle
 * (Óptica Fashion, Mon-Sat 09:00-17:00 America/Ciudad_Juarez; Sunday closed):
 *
 * 1. Closed Overnight & Sunday (`off_duty_inside`):
 *    Valeria remains stationary at the verified interior off-duty station
 *    `wp_valeria_off_duty_station (-15.85, 0.20, 1.15)` facing East (+1, 0),
 *    with `shutterClosedPermitted: !playerInsideStore`.
 * 2. Daily Opening (`09:00:00..09:00:05`, `opening_transition`):
 *    Shutter opens (`shutterClosedPermitted: false`) while Valeria waits inside;
 *    at `09:00:05`, she walks continuously out from `wp_valeria_off_duty_station`
 *    through Zone C -> Zone B open doorway -> `wp_valeria_storefront` into normal autonomy.
 * 3. Midday Calibration (`13:00:00..13:45:00+`):
 *    Delegates to `evaluateValeriaCalibrationLifecycle` (`businessLifecycleState: 'business_open'`,
 *    `shutterClosedPermitted: false`).
 * 4. Pre-Closing Return (`16:57:00..17:00:00`, configurable lead time):
 *    Suspends ordinary roaming from exact current position, routes through open doorway
 *    (if outside) or showroom (if inside) to `wp_valeria_off_duty_station` at 1.10 m/s
 *    without entering Zone D, and waits inside (`closing_preparation`) once arrived.
 * 5. Closing Boundary & `closing_pending` (`>= 17:00:00`):
 *    If Valeria is safely inside at `wp_valeria_off_duty_station` and player is not trapped
 *    inside, enters `off_duty_inside` with `shutterClosedPermitted: true`.
 *    If held outside or mid-return by an active player dialogue past 17:00:00, enters
 *    `closing_pending` (`shutterClosedPermitted: false`), preserves the dialogue, gives
 *    return-to-interior highest priority immediately on release at normal 1.10 m/s speed,
 *    and completes closure only after physical arrival inside.
 */
export function evaluateValeriaBusinessLifecycle(params: {
  readonly rawEvaluatedAtMs: number;
  readonly civil: CivilTime;
  readonly waypointsRegistry: Readonly<
    Record<string, { readonly position: Vector3D; readonly associatedPlaceId: string; readonly outgoingWaypointIds: readonly string[] }>
  >;
  readonly pauseIntervals?: readonly NpcInteractionPauseInterval[];
  readonly player?: PlayerSpatialState | null;
  readonly closingPrepLeadMinutes?: number;
}): AutonomousMovementEvaluationResult | null {
  const {
    rawEvaluatedAtMs,
    civil,
    waypointsRegistry,
    pauseIntervals,
    player,
    closingPrepLeadMinutes = VALERIA_CLOSING_PREP_LEAD_MINUTES,
  } = params;

  const profile = BUSINESS_LOCAL_AUTONOMY_PROFILES['valeria-optica-attendant']!;
  const domain = OPTICA_FASHION_AUTONOMY_DOMAIN;
  const primaryWp = waypointsRegistry[profile.primaryWaypointId]!;
  const defaultHeading = profile.localNodes[profile.primaryWaypointId]?.observationHeading ?? { x: 1, z: 0 };
  const offDutyWp = waypointsRegistry[VALERIA_OFF_DUTY_STATION_WAYPOINT_ID] ?? {
    position: VALERIA_OFF_DUTY_STATION_POSITION,
    associatedPlaceId: domain.associatedPlaceId,
    outgoingWaypointIds: [],
  };
  const offDutyZone =
    resolveAuthorizedBusinessZone(offDutyWp.position, domain, null) ?? OPTICA_MAIN_SHOWROOM_ZONE;
  const playerInsideStore = isPlayerInsideOpticaInterior(player);

  const rawSecInDay =
    civil.hour * 3600 + civil.minute * 60 + civil.second + civil.millisecond / 1000;
  const dayStartUtcMs = rawEvaluatedAtMs - rawSecInDay * 1000;

  const isOperatingDay = VALERIA_BUSINESS_OPERATING_DAYS.includes(civil.dayOfWeek);
  const openSec = VALERIA_BUSINESS_OPEN_HOUR * 3600 + VALERIA_BUSINESS_OPEN_MINUTE * 60;
  const closeSec = VALERIA_BUSINESS_CLOSE_HOUR * 3600 + VALERIA_BUSINESS_CLOSE_MINUTE * 60;
  const safeLeadMinutes = Math.max(1, Math.min(30, closingPrepLeadMinutes));
  const preCloseSec = closeSec - safeLeadMinutes * 60;
  const calStartSec = VALERIA_CALIBRATION_START_HOUR * 3600 + VALERIA_CALIBRATION_START_MINUTE * 60;

  // Helper to construct deterministic interior off-duty waiting snapshot
  const buildOffDutyInsideSnapshot = (opts: {
    readonly lifecycleState: BusinessLifecycleState;
    readonly behaviorState: BusinessAutonomyBehaviorState;
    readonly controlMode: NpcMovementControlMode;
    readonly shutterClosedPermitted: boolean;
    readonly isSuspended?: boolean;
    readonly suspendedByPlayerId?: string | null;
  }): AutonomousMovementEvaluationResult => ({
    movementState: 'stationary',
    currentWaypointId: VALERIA_OFF_DUTY_STATION_WAYPOINT_ID,
    destinationWaypointId: null,
    activeRouteId: null,
    position: offDutyWp.position,
    forwardHeading: VALERIA_OFF_DUTY_STATION_HEADING,
    progression: 0,
    movementStartTime: null,
    expectedDurationSeconds: 0,
    associatedPlaceId: domain.associatedPlaceId,
    behaviorProfileType: 'business_local_autonomy',
    autonomousBehaviorState: opts.behaviorState,
    controlMode: opts.controlMode,
    patrolDirection: 'none',
    patrolCycleIndex: 0,
    isSuspendedByPlayerInteraction: opts.isSuspended ?? false,
    suspendedByPlayerId: opts.suspendedByPlayerId ?? null,
    passingEncounterPartnerNpcId: null,
    passingEncounterSessionId: null,
    passingEncounterType: null,
    businessDomainId: domain.domainId,
    withinAuthorizedDomain: isPointInBusinessDomain(offDutyWp.position, domain, null),
    actualSpeedMps: 0,
    activeSegmentEndPosition: offDutyWp.position,
    currentZoneId: offDutyZone.zoneId,
    activeTaskId: null,
    taskDestinationReachable: true,
    physicalCalibrationActive: false,
    businessLifecycleState: opts.lifecycleState,
    shutterClosedPermitted: opts.shutterClosedPermitted,
  });

  // 1. Closed day (Sunday) OR before business opening (< 09:00:00)
  // Always remains safely inside at wp_valeria_off_duty_station (Scenario E safe: pause/resume never starts roaming)
  if (!isOperatingDay || rawSecInDay < openSec) {
    const pauseEval = evaluateEffectiveAutonomousTime(rawEvaluatedAtMs, pauseIntervals, dayStartUtcMs);
    const safelyInside = isValeriaSafelyInsideOptica(offDutyWp.position);
    return buildOffDutyInsideSnapshot({
      lifecycleState: playerInsideStore ? 'closing_pending' : 'off_duty_inside',
      behaviorState: 'off_duty',
      controlMode: 'fixed_station_anchor',
      shutterClosedPermitted: safelyInside && !playerInsideStore,
      isSuspended: pauseEval.isSuspended,
      suspendedByPlayerId: pauseEval.suspendedByPlayerId,
    });
  }

  // 2. Daily Opening Transition & Indoor Start of Shift (09:00:00 .. morning autonomy handoff)
  if (rawSecInDay >= openSec && rawSecInDay < calStartSec) {
    const openUtcMs = dayStartUtcMs + openSec * 1000;
    const openWalkStartUtcMs = openUtcMs + VALERIA_OPENING_TRANSITION_SECONDS * 1000;

    // 2a. [09:00:00, 09:00:05): Shutter opens while Valeria waits safely inside at wp_valeria_off_duty_station
    if (rawEvaluatedAtMs < openWalkStartUtcMs) {
      const pauseEval = evaluateEffectiveAutonomousTime(rawEvaluatedAtMs, pauseIntervals, openUtcMs);
      return buildOffDutyInsideSnapshot({
        lifecycleState: 'opening_transition',
        behaviorState: pauseEval.isSuspended ? 'customer_interaction' : 'working_stationary',
        controlMode: pauseEval.isSuspended ? 'player_interaction_suspended' : 'autonomous_business_local',
        shutterClosedPermitted: false,
        isSuspended: pauseEval.isSuspended,
        suspendedByPlayerId: pauseEval.suspendedByPlayerId,
      });
    }

    // 2b. [09:00:05+): Walk continuously from wp_valeria_off_duty_station -> open doorway -> wp_valeria_storefront
    const openingPolyline = buildValidatedPolylineFromPosition({
      startPos: offDutyWp.position,
      startWaypointId: VALERIA_OFF_DUTY_STATION_WAYPOINT_ID,
      startSegmentDestId: null,
      goalWaypointId: profile.primaryWaypointId,
      domain,
      profile,
      waypointsRegistry,
      speedMps: profile.preferredSpeedMps,
      activeTaskId: null,
    });

    if (openingPolyline) {
      const openWalkPauseEval = evaluateEffectiveAutonomousTime(
        rawEvaluatedAtMs,
        pauseIntervals,
        openWalkStartUtcMs
      );
      const effOpenWalkSec = Math.max(0, (openWalkPauseEval.effectiveTimeMs - openWalkStartUtcMs) / 1000);

      if (effOpenWalkSec < openingPolyline.totalDurationSec) {
        const sample = samplePolylineAtElapsed(openingPolyline, effOpenWalkSec);
        const zone = resolveAuthorizedBusinessZone(sample.position, domain, null);
        const isSuspended = openWalkPauseEval.isSuspended;
        return {
          movementState: isSuspended ? 'stationary' : 'moving',
          currentWaypointId: sample.currentWaypointId,
          destinationWaypointId: profile.primaryWaypointId,
          activeRouteId: 'route_valeria_opening_shift',
          position: sample.position,
          forwardHeading: sample.heading,
          progression: sample.progression,
          movementStartTime: new Date(openWalkStartUtcMs).toISOString(),
          expectedDurationSeconds: Math.round(openingPolyline.totalDurationSec * 1000) / 1000,
          associatedPlaceId: domain.associatedPlaceId,
          behaviorProfileType: 'business_local_autonomy',
          autonomousBehaviorState: isSuspended ? 'customer_interaction' : 'local_roaming',
          controlMode: isSuspended ? 'player_interaction_suspended' : 'autonomous_business_local',
          patrolDirection: 'none',
          patrolCycleIndex: 0,
          isSuspendedByPlayerInteraction: isSuspended,
          suspendedByPlayerId: openWalkPauseEval.suspendedByPlayerId,
          passingEncounterPartnerNpcId: null,
          passingEncounterSessionId: null,
          passingEncounterType: null,
          businessDomainId: domain.domainId,
          withinAuthorizedDomain: isPointInBusinessDomain(sample.position, domain, null),
          actualSpeedMps: isSuspended ? 0 : profile.preferredSpeedMps,
          activeSegmentEndPosition: isSuspended ? sample.position : sample.activeSegmentEndPosition,
          currentZoneId: zone?.zoneId ?? null,
          activeTaskId: null,
          taskDestinationReachable: true,
          physicalCalibrationActive: false,
          businessLifecycleState: 'business_open',
          shutterClosedPermitted: false,
        };
      }

      // Brief calm arrival settle at wp_valeria_storefront until morning roaming epoch opens at 09:00:12
      if (rawSecInDay < openSec + VALERIA_OPENING_WALK_PADDING_SECONDS) {
        const isSuspended = openWalkPauseEval.isSuspended;
        const primaryZone = resolveAuthorizedBusinessZone(primaryWp.position, domain, null);
        return {
          movementState: 'stationary',
          currentWaypointId: profile.primaryWaypointId,
          destinationWaypointId: null,
          activeRouteId: null,
          position: primaryWp.position,
          forwardHeading: defaultHeading,
          progression: 0,
          movementStartTime: null,
          expectedDurationSeconds: 0,
          associatedPlaceId: domain.associatedPlaceId,
          behaviorProfileType: 'business_local_autonomy',
          autonomousBehaviorState: isSuspended ? 'customer_interaction' : 'working_stationary',
          controlMode: isSuspended ? 'player_interaction_suspended' : 'autonomous_business_local',
          patrolDirection: 'none',
          patrolCycleIndex: 0,
          isSuspendedByPlayerInteraction: isSuspended,
          suspendedByPlayerId: openWalkPauseEval.suspendedByPlayerId,
          passingEncounterPartnerNpcId: null,
          passingEncounterSessionId: null,
          passingEncounterType: null,
          businessDomainId: domain.domainId,
          withinAuthorizedDomain: isPointInBusinessDomain(primaryWp.position, domain, null),
          actualSpeedMps: 0,
          activeSegmentEndPosition: primaryWp.position,
          currentZoneId: primaryZone?.zoneId ?? OPTICA_EXTERIOR_FRONTAGE_ZONE.zoneId,
          activeTaskId: null,
          taskDestinationReachable: true,
          physicalCalibrationActive: false,
          businessLifecycleState: 'business_open',
          shutterClosedPermitted: false,
        };
      }
    }

    // Morning normal indoor/outdoor autonomy
    return null;
  }

  // 3. Midday Calibration & Afternoon Normal Autonomy [13:00:00, preCloseSec)
  if (rawSecInDay >= calStartSec && rawSecInDay < preCloseSec) {
    return evaluateValeriaCalibrationLifecycle({
      rawEvaluatedAtMs,
      civil,
      waypointsRegistry,
      pauseIntervals,
      player,
    });
  }

  // 4. Pre-Closing Preparation, Return Inside, Closing Pending & Overnight Off-Duty (rawSecInDay >= preCloseSec)
  const preCloseUtcMs = dayStartUtcMs + preCloseSec * 1000;
  const closeUtcMs = dayStartUtcMs + closeSec * 1000;
  const isPastClosingBoundary = rawEvaluatedAtMs >= closeUtcMs;

  // Determine Valeria's exact position at the instant pre-closing triggers (preCloseSec - 1ms)
  const preClosePrevSec = Math.max(0, preCloseSec - 1);
  const preCloseCivil: CivilTime = {
    ...civil,
    hour: Math.floor(preClosePrevSec / 3600),
    minute: Math.floor((preClosePrevSec % 3600) / 60),
    second: preClosePrevSec % 60,
    millisecond: 999,
  };
  const preClosePauses = pauseIntervals
    ?.filter((iv) => iv.startedAtMs < preCloseUtcMs)
    .map((iv) => ({
      ...iv,
      endedAtMs: iv.endedAtMs === null ? preCloseUtcMs - 1 : Math.min(iv.endedAtMs, preCloseUtcMs - 1),
    }));

  // Respect any calibration return still active at preCloseUtcMs - 1 (e.g., prolonged pause from calibration past 16:57)
  const preCloseCalibState = evaluateValeriaCalibrationLifecycle({
    rawEvaluatedAtMs: preCloseUtcMs - 1,
    civil: preCloseCivil,
    waypointsRegistry,
    pauseIntervals: preClosePauses,
    player: null,
  });

  const preCloseState =
    preCloseCalibState ??
    evaluateBusinessLocalAutonomyMovement({
      profile,
      rawEvaluatedAtMs: preCloseUtcMs - 1,
      civil: preCloseCivil,
      activitySnapshot: null,
      waypointsRegistry,
      pauseIntervals: preClosePauses,
      player: null,
    });

  // Never authorize Zone D entry during closing; ONLY allow Zone D exit if Valeria was already inside Zone D
  const startedInPublicZone = resolveAuthorizedBusinessZone(preCloseState.position, domain, null) !== null;
  const closingRouteAuthTaskId = startedInPublicZone ? null : VALERIA_CALIBRATION_TASK_ID;

  const closingPolyline = buildValidatedPolylineFromPosition({
    startPos: preCloseState.position,
    startWaypointId: preCloseState.currentWaypointId,
    startSegmentDestId: preCloseState.destinationWaypointId,
    goalWaypointId: VALERIA_OFF_DUTY_STATION_WAYPOINT_ID,
    domain,
    profile,
    waypointsRegistry,
    speedMps: profile.preferredSpeedMps,
    activeTaskId: closingRouteAuthTaskId,
  });

  const closePauseEval = evaluateEffectiveAutonomousTime(rawEvaluatedAtMs, pauseIntervals, preCloseUtcMs);
  const effCloseSec = Math.max(0, (closePauseEval.effectiveTimeMs - preCloseUtcMs) / 1000);

  // 4a. Valeria is still walking inside (or held outside/mid-return by an active player dialogue)
  if (closingPolyline && effCloseSec < closingPolyline.totalDurationSec) {
    const sample = samplePolylineAtElapsed(closingPolyline, effCloseSec);
    const publicZone = resolveAuthorizedBusinessZone(sample.position, domain, null);
    const activeExitTaskId = publicZone ? null : closingRouteAuthTaskId;
    const currentZone =
      publicZone ?? resolveAuthorizedBusinessZone(sample.position, domain, activeExitTaskId);
    const isSuspended = closePauseEval.isSuspended;

    const lifecycleState: BusinessLifecycleState = isPastClosingBoundary
      ? 'closing_pending'
      : isSuspended
        ? 'closing_preparation'
        : 'returning_inside_for_close';

    return {
      movementState: isSuspended ? 'stationary' : 'moving',
      currentWaypointId: sample.currentWaypointId,
      destinationWaypointId: VALERIA_OFF_DUTY_STATION_WAYPOINT_ID,
      activeRouteId: 'route_valeria_return_for_close',
      position: sample.position,
      forwardHeading: sample.heading,
      progression: sample.progression,
      movementStartTime: new Date(preCloseUtcMs).toISOString(),
      expectedDurationSeconds: Math.round(closingPolyline.totalDurationSec * 1000) / 1000,
      associatedPlaceId: domain.associatedPlaceId,
      behaviorProfileType: 'business_local_autonomy',
      autonomousBehaviorState: isSuspended ? 'customer_interaction' : 'returning_inside_for_close',
      controlMode: isSuspended ? 'player_interaction_suspended' : 'scheduled_routine_transition',
      patrolDirection: 'none',
      patrolCycleIndex: 0,
      isSuspendedByPlayerInteraction: isSuspended,
      suspendedByPlayerId: closePauseEval.suspendedByPlayerId,
      passingEncounterPartnerNpcId: null,
      passingEncounterSessionId: null,
      passingEncounterType: null,
      businessDomainId: domain.domainId,
      withinAuthorizedDomain: isPointInBusinessDomain(sample.position, domain, activeExitTaskId),
      actualSpeedMps: isSuspended ? 0 : profile.preferredSpeedMps,
      activeSegmentEndPosition: isSuspended ? sample.position : sample.activeSegmentEndPosition,
      currentZoneId: currentZone?.zoneId ?? null,
      activeTaskId: activeExitTaskId,
      taskDestinationReachable: true,
      physicalCalibrationActive: false,
      businessLifecycleState: lifecycleState,
      // Shutter MUST stay open while Valeria is outside, in threshold, or still returning inside
      shutterClosedPermitted: false,
    };
  }

  // 4b. Valeria has physically reached wp_valeria_off_duty_station (-15.85, 0.20, 1.15)
  const safelyInside = isValeriaSafelyInsideOptica(offDutyWp.position);

  if (!isPastClosingBoundary) {
    // Arrived inside during [16:57, 17:00): calm indoor resting/waiting until exact 17:00:00 shutter close
    return buildOffDutyInsideSnapshot({
      lifecycleState: 'closing_preparation',
      behaviorState: closePauseEval.isSuspended ? 'customer_interaction' : 'off_duty_inside',
      controlMode: closePauseEval.isSuspended ? 'player_interaction_suspended' : 'scheduled_routine_transition',
      shutterClosedPermitted: false,
      isSuspended: closePauseEval.isSuspended,
      suspendedByPlayerId: closePauseEval.suspendedByPlayerId,
    });
  }

  // Past 17:00:00 closing boundary and Valeria is confirmed safely inside:
  // Only permit shutter closure if the player is NOT inside Óptica Fashion (never trap player!).
  if (playerInsideStore) {
    return buildOffDutyInsideSnapshot({
      lifecycleState: 'closing_pending',
      behaviorState: 'off_duty_inside',
      controlMode: 'fixed_station_anchor',
      shutterClosedPermitted: false,
      isSuspended: closePauseEval.isSuspended,
      suspendedByPlayerId: closePauseEval.suspendedByPlayerId,
    });
  }

  return buildOffDutyInsideSnapshot({
    lifecycleState: 'off_duty_inside',
    behaviorState: 'off_duty',
    controlMode: 'fixed_station_anchor',
    shutterClosedPermitted: safelyInside,
    isSuspended: closePauseEval.isSuspended,
    suspendedByPlayerId: closePauseEval.suspendedByPlayerId,
  });
}

// ---------------------------------------------------------------------------
// 8. SUBSYSTEM A2 — BRIEF PASSING ENCOUNTERS RESOLVER
// ---------------------------------------------------------------------------

export interface ActivePassingEncounterPair {
  readonly sessionId: string;
  readonly npcIdA: string;
  readonly npcIdB: string;
  readonly encounterType: PassingEncounterType;
  readonly startedAtMs: number;
  readonly expectedEndAtMs: number;
  readonly cooldownUntilMs: number;
  readonly distanceMeters: number;
  readonly phase: 'initiating' | 'engaged' | 'closing';
  readonly activeSpeakerNpcId: string;
  readonly activeListenerNpcId: string;
}

export function evaluatePassingEncounterPairStatus(params: {
  readonly npcA: {
    readonly npcId: string;
    readonly position: Vector3D;
    readonly behaviorProfileType?: NpcBehaviorProfileType;
    readonly autonomousBehaviorState?: NpcAutonomousBehaviorState;
    readonly isSuspendedByPlayerInteraction?: boolean;
    readonly activityState: string;
  };
  readonly npcB: {
    readonly npcId: string;
    readonly position: Vector3D;
    readonly behaviorProfileType?: NpcBehaviorProfileType;
    readonly autonomousBehaviorState?: NpcAutonomousBehaviorState;
    readonly isSuspendedByPlayerInteraction?: boolean;
    readonly activityState: string;
  };
  readonly evaluatedAtMs: number;
  readonly wasCloseAtFn?: (testTimeMs: number) => boolean;
}): {
  readonly activeEncounter: ActivePassingEncounterPair | null;
  readonly inCooldown: boolean;
  readonly cooldownUntilMs: number | null;
} {
  const { npcA, npcB, evaluatedAtMs, wasCloseAtFn } = params;

  // Passing encounters apply strictly to eligible Bridge Patrol proxies not in busy tasks or player interactions
  if (
    npcA.behaviorProfileType !== 'bridge_patrol' ||
    npcB.behaviorProfileType !== 'bridge_patrol' ||
    npcA.isSuspendedByPlayerInteraction ||
    npcB.isSuspendedByPlayerInteraction ||
    npcA.activityState === 'busy' ||
    npcB.activityState === 'busy'
  ) {
    return { activeEncounter: null, inCooldown: false, cooldownUntilMs: null };
  }

  // Must not conflict with stationary sustained social exchange windows (where both are observing at home station)
  const bothStationaryObserving =
    npcA.autonomousBehaviorState === 'observing' &&
    npcB.autonomousBehaviorState === 'observing';
  if (bothStationaryObserving) {
    return { activeEncounter: null, inCooldown: false, cooldownUntilMs: null };
  }

  const dx = npcB.position.x - npcA.position.x;
  const dz = npcB.position.z - npcA.position.z;
  const dy = Math.abs(npcB.position.y - npcA.position.y);
  const horizDist = Math.hypot(dx, dz);

  if (horizDist > PASSING_ENCOUNTER_MAX_RANGE_METERS || dy > PASSING_ENCOUNTER_MAX_VERTICAL_METERS) {
    return { activeEncounter: null, inCooldown: false, cooldownUntilMs: null };
  }

  const [minId, maxId] = npcA.npcId < npcB.npcId ? [npcA.npcId, npcB.npcId] : [npcB.npcId, npcA.npcId];
  const microSlotMs = 10_000;
  const currentSlotIdx = Math.floor(evaluatedAtMs / microSlotMs);

  const isSlotSelected = (slotIdx: number): { selected: boolean; hash: number; durationSec: number } => {
    const h = computeDeterministicHash32(`passing|${minId}|${maxId}|slot${slotIdx}`);
    // 75% of proximity micro-slots engage; at least 3 out of every 4 slots are guaranteed selected
    const selected = (h % 100) < 75 || (slotIdx % 2 === 0);
    const durationSec = deterministicRangeInt(
      h,
      4,
      PASSING_ENCOUNTER_MIN_DURATION_SECONDS,
      PASSING_ENCOUNTER_MAX_DURATION_SECONDS
    );
    return { selected, hash: h, durationSec };
  };

  // Check if an earlier micro-slot within the 45-second cooldown window already triggered an encounter
  const cooldownSlots = Math.ceil((PASSING_ENCOUNTER_COOLDOWN_SECONDS * 1000) / microSlotMs);
  for (let back = cooldownSlots; back >= 1; back--) {
    const prevIdx = currentSlotIdx - back;
    const prevStartMs = prevIdx * microSlotMs;
    if (evaluatedAtMs - prevStartMs >= PASSING_ENCOUNTER_COOLDOWN_SECONDS * 1000) {
      continue;
    }
    const prevMeta = isSlotSelected(prevIdx);
    if (!prevMeta.selected) continue;
    const wasClose = wasCloseAtFn ? wasCloseAtFn(prevStartMs + 500) : false;
    if (wasClose) {
      const cooldownUntilMs = prevStartMs + PASSING_ENCOUNTER_COOLDOWN_SECONDS * 1000;
      return {
        activeEncounter: null,
        inCooldown: evaluatedAtMs < cooldownUntilMs,
        cooldownUntilMs,
      };
    }
  }

  const curMeta = isSlotSelected(currentSlotIdx);
  if (!curMeta.selected) {
    return { activeEncounter: null, inCooldown: false, cooldownUntilMs: null };
  }

  const startedAtMs = currentSlotIdx * microSlotMs;
  const durationMs = curMeta.durationSec * 1000;
  const expectedEndAtMs = startedAtMs + durationMs;
  const cooldownUntilMs = startedAtMs + PASSING_ENCOUNTER_COOLDOWN_SECONDS * 1000;

  if (evaluatedAtMs >= expectedEndAtMs) {
    return {
      activeEncounter: null,
      inCooldown: evaluatedAtMs < cooldownUntilMs,
      cooldownUntilMs,
    };
  }

  const encounterType: PassingEncounterType =
    (curMeta.hash & 0x100) === 0 ? 'passing_greeting' : 'passing_exchange';

  const elapsedMs = evaluatedAtMs - startedAtMs;
  let phase: 'initiating' | 'engaged' | 'closing' = 'engaged';
  if (elapsedMs < 1000) {
    phase = 'initiating';
  } else if (elapsedMs >= durationMs - 1000) {
    phase = 'closing';
  }

  const turnIdx = Math.floor(elapsedMs / 2000);
  const speakerFirst = ((curMeta.hash >>> 10) & 1) === 0;
  const minIsSpeaker = turnIdx % 2 === 0 ? speakerFirst : !speakerFirst;

  return {
    activeEncounter: {
      sessionId: `pass_${minId}_${maxId}_${Math.floor(startedAtMs / 1000)}`,
      npcIdA: minId,
      npcIdB: maxId,
      encounterType,
      startedAtMs,
      expectedEndAtMs,
      cooldownUntilMs,
      distanceMeters: Math.round(horizDist * 1000) / 1000,
      phase,
      activeSpeakerNpcId: minIsSpeaker ? minId : maxId,
      activeListenerNpcId: minIsSpeaker ? maxId : minId,
    },
    inCooldown: false,
    cooldownUntilMs,
  };
}

// ---------------------------------------------------------------------------
// 9. PERSONAL-SPACE SEPARATION & COLLISION-AWARE YIELDING (INCREMENT 5.22.1)
// ---------------------------------------------------------------------------

export interface PersonalSpaceSnapLike {
  readonly npcId: string;
  readonly position: Vector3D;
  readonly forwardHeading: { readonly x: number; readonly z: number };
  readonly movementState: 'stationary' | 'moving' | 'arrived' | 'waiting';
  readonly behaviorProfileType?: NpcBehaviorProfileType;
  readonly autonomousBehaviorState?: NpcAutonomousBehaviorState;
  readonly isSuspendedByPlayerInteraction?: boolean;
  readonly patrolCycleIndex?: number;
  readonly withinAuthorizedDomain?: boolean;
}

function isPositionAuthorizedForNpc(snap: PersonalSpaceSnapLike, candidate: Vector3D): boolean {
  if (snap.behaviorProfileType === 'bridge_patrol') {
    return isPointInBridgePatrolDomain(candidate, 0.02);
  }
  if (snap.behaviorProfileType === 'business_local_autonomy') {
    const domain = BUSINESS_AUTONOMY_DOMAINS[snap.npcId];
    return domain ? isPointInSafeBusinessRoamingRegion(candidate, domain) : false;
  }
  return false;
}

function computePriorityScore(snap: PersonalSpaceSnapLike): number {
  if (snap.isSuspendedByPlayerInteraction) return 4;
  if (snap.behaviorProfileType === 'fixed_anchor_scheduled') return 3;
  if (snap.movementState !== 'moving') return 2;
  return 1;
}

function selectYieldingNpcPair<T extends PersonalSpaceSnapLike>(
  a: T,
  b: T
): { readonly keeper: T; readonly yielder: T } {
  const scoreA = computePriorityScore(a);
  const scoreB = computePriorityScore(b);
  if (scoreA !== scoreB) {
    return scoreA > scoreB ? { keeper: a, yielder: b } : { keeper: b, yielder: a };
  }
  const cycA = a.patrolCycleIndex ?? 0;
  const cycB = b.patrolCycleIndex ?? 0;
  if (cycA !== cycB) {
    return cycA > cycB ? { keeper: a, yielder: b } : { keeper: b, yielder: a };
  }
  return a.npcId < b.npcId ? { keeper: a, yielder: b } : { keeper: b, yielder: a };
}

/**
 * Deterministic World Core personal-space enforcement & collision-aware yielding.
 * Ensures no two active NPCs overlap closer than `MIN_PERSONAL_SPACE_METERS` (1.05m),
 * preferring corridor-validated lateral lane offset inside the 2.01m walkway before
 * falling back to longitudinal yielding (`movementState: 'waiting'`).
 */
export function applyRosterPersonalSpaceSeparation<T extends PersonalSpaceSnapLike>(
  npcs: Record<string, T>,
  minSeparationMeters = MIN_PERSONAL_SPACE_METERS
): void {
  const activeIds = Object.keys(npcs)
    .filter((id) => {
      const s = npcs[id];
      return (
        s &&
        s.autonomousBehaviorState !== 'off_duty' &&
        (s.behaviorProfileType === 'bridge_patrol' ||
          s.behaviorProfileType === 'business_local_autonomy')
      );
    })
    .sort();

  if (activeIds.length < 2) return;

  for (let pass = 0; pass < 2; pass++) {
    for (let i = 0; i < activeIds.length; i++) {
      for (let j = i + 1; j < activeIds.length; j++) {
        const snapA = npcs[activeIds[i]!]!;
        const snapB = npcs[activeIds[j]!]!;

        const dy = Math.abs(snapB.position.y - snapA.position.y);
        if (dy > PERSONAL_SPACE_MAX_VERTICAL_METERS) continue;

        const horizDist = Math.hypot(
          snapB.position.x - snapA.position.x,
          snapB.position.z - snapA.position.z
        );
        if (horizDist >= minSeparationMeters) continue;

        const { keeper, yielder } = selectYieldingNpcPair(snapA, snapB);
        if (yielder.isSuspendedByPlayerInteraction) continue;

        const hLen = Math.hypot(yielder.forwardHeading.x, yielder.forwardHeading.z) || 1;
        const fx = yielder.forwardHeading.x / hLen;
        const fz = yielder.forwardHeading.z / hLen;
        const rx = fz;
        const rz = -fx;

        const toYielderX = yielder.position.x - keeper.position.x;
        const toYielderZ = yielder.position.z - keeper.position.z;
        const sideDot = toYielderX * rx + toYielderZ * rz;
        const latDir = sideDot >= 0 ? 1 : -1;

        const deficit = minSeparationMeters - horizDist + 0.08;
        const latShift = Math.min(WALKWAY_SAFE_HALF_WIDTH_METERS, deficit);
        const lateralCandidate: Vector3D = {
          x: Math.round((yielder.position.x + rx * latDir * latShift) * 1000) / 1000,
          y: yielder.position.y,
          z: Math.round((yielder.position.z + rz * latDir * latShift) * 1000) / 1000,
        };
        const latDist = Math.hypot(
          lateralCandidate.x - keeper.position.x,
          lateralCandidate.z - keeper.position.z
        );

        if (latDist >= minSeparationMeters && isPositionAuthorizedForNpc(yielder, lateralCandidate)) {
          npcs[yielder.npcId] = Object.freeze({
            ...yielder,
            position: lateralCandidate,
            withinAuthorizedDomain: true,
          });
          continue;
        }

        // Insufficient lateral passing room -> yield longitudinally behind keeper along -forward
        const backDist = minSeparationMeters - horizDist + 0.10;
        const longitudinalCandidate: Vector3D = {
          x: Math.round((yielder.position.x - fx * backDist) * 1000) / 1000,
          y: yielder.position.y,
          z: Math.round((yielder.position.z - fz * backDist) * 1000) / 1000,
        };
        const longDist = Math.hypot(
          longitudinalCandidate.x - keeper.position.x,
          longitudinalCandidate.z - keeper.position.z
        );

        if (longDist >= minSeparationMeters && isPositionAuthorizedForNpc(yielder, longitudinalCandidate)) {
          npcs[yielder.npcId] = Object.freeze({
            ...yielder,
            position: longitudinalCandidate,
            movementState: 'waiting',
            currentSpeedMps: 0,
            autonomousBehaviorState:
              yielder.behaviorProfileType === 'bridge_patrol' ? 'paused' : 'idle_observation',
            withinAuthorizedDomain: true,
          });
        } else {
          // Fallback push strictly away along vector from keeper to yielder (or -forward) if authorized
          const awayLen = Math.hypot(toYielderX, toYielderZ);
          const ax = awayLen > 1e-4 ? toYielderX / awayLen : -fx;
          const az = awayLen > 1e-4 ? toYielderZ / awayLen : -fz;
          const awayCandidate: Vector3D = {
            x: Math.round((keeper.position.x + ax * (minSeparationMeters + 0.08)) * 1000) / 1000,
            y: yielder.position.y,
            z: Math.round((keeper.position.z + az * (minSeparationMeters + 0.08)) * 1000) / 1000,
          };
          const resolvedPos = isPositionAuthorizedForNpc(yielder, awayCandidate)
            ? awayCandidate
            : yielder.position;
          npcs[yielder.npcId] = Object.freeze({
            ...yielder,
            position: resolvedPos,
            movementState: 'waiting',
            currentSpeedMps: 0,
            autonomousBehaviorState:
              yielder.behaviorProfileType === 'bridge_patrol' ? 'paused' : 'idle_observation',
            withinAuthorizedDomain: isPositionAuthorizedForNpc(yielder, resolvedPos),
          });
        }
      }
    }
  }
}
