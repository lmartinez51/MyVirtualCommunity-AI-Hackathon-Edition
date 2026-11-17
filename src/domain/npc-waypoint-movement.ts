/**
 * Deterministic NPC Waypoint Movement & Spatial Presence Domain Model.
 *
 * Core Architectural Invariants:
 * 1. World Core remains authoritative for:
 *    - Canonical NPC location.
 *    - Current movement state ('stationary' | 'moving' | 'arrived' | 'waiting').
 *    - Destination waypoint.
 *    - Active route.
 *    - Movement progression (0.0 to 1.0).
 *    - Arrival and departure transitions.
 * 2. Unity remains a presentation-only client that renders the canonical movement state.
 *    Unity transforms never decide canonical world state.
 * 3. Spatial units:
 *    Corridor Cartesian coordinates in meters:
 *    - X: West (-) to East (+) cross-street offset.
 *    - Y: Elevation above street ground level.
 *    - Z: South (-) to North (+) longitudinal axis towards Puente Paso del Norte.
 *    Note: Demonstration corridor Cartesian coordinates; NOT real-world GPS coordinates.
 * 4. Waypoints strictly reference canonical Places (e.g. infrastructure:puente-paso-del-norte,
 *    pharmacy:farmacias-benavides, optical:optica-fashion, desk:patricia-citizen-rep).
 * 5. Time progression & offline reconstruction:
 *    Pure deterministic mathematical function f(instant) of the authoritative World Clock.
 *    Identical inputs produce identical movement state regardless of step size or offline duration.
 */

import { deriveCivilTime, CivilTime, parseLocalCivilTimeToUtc } from './civil-time.js';
import { BilingualText, SyntheticWitnessId } from './synthetic-witness.js';
import { NPC_SCHEDULE_ROSTER, evaluateNpcActivity } from './npc-activity.js';
import {
  NpcActorArchetype,
  WitnessClassification,
  getNpcActorClassification,
} from './npc-actor-archetypes.js';
import {
  BRIDGE_PATROL_PROFILES,
  BUSINESS_AUTONOMY_DOMAINS,
  BUSINESS_LOCAL_AUTONOMY_PROFILES,
  EvaluateAutonomousMovementOptions,
  NpcAutonomousBehaviorState,
  NpcBehaviorProfileType,
  NpcMovementControlMode,
  PassingEncounterType,
  PatrolDirection,
  applyRosterPersonalSpaceSeparation,
  evaluateBridgePatrolMovement,
  evaluateBusinessLocalAutonomyMovement,
  evaluatePassingEncounterPairStatus,
  getNpcBehavioralAutonomyProfile,
  isPointInBridgePatrolDomain,
  isPointInBusinessDomain,
} from './npc-autonomous-locomotion.js';

export type NpcMovementState = 'stationary' | 'moving' | 'arrived' | 'waiting';

export interface Vector3D {
  readonly x: number;
  readonly y: number;
  readonly z: number;
}

export interface Waypoint {
  readonly id: string;
  readonly associatedPlaceId: string;
  readonly position: Vector3D;
  readonly label: BilingualText;
  readonly outgoingWaypointIds: readonly string[];
}

export interface RouteSegment {
  readonly fromWaypointId: string;
  readonly toWaypointId: string;
  readonly distanceMeters: number;
  readonly durationSeconds: number;
}

export interface Route {
  readonly id: string;
  readonly waypointIds: readonly string[];
  readonly segments: readonly RouteSegment[];
  readonly totalDistanceMeters: number;
  readonly totalDurationSeconds: number;
}

export interface NpcMovementTransition {
  readonly id: string;
  /** Days of week when this routine transition applies (0=Sun, 1=Mon, ..., 6=Sat) */
  readonly daysOfWeek: readonly number[];
  /** Outbound departure local civil time */
  readonly departureHour: number;
  readonly departureMinute: number;
  readonly outboundRouteId: string;
  readonly outboundDurationSeconds: number;
  readonly destinationWaypointId: string;
  /** Arrival dwell duration in seconds before entering stationary/waiting state */
  readonly arrivalDwellSeconds: number;
  /** Inbound return departure local civil time */
  readonly returnHour: number;
  readonly returnMinute: number;
  readonly returnRouteId: string;
  readonly returnDurationSeconds: number;
  readonly isPhysicalDestinationVerified?: boolean;
  readonly description: BilingualText;
}

export interface NpcMovementProfile {
  readonly npcId: string;
  readonly canonicalName: string;
  readonly primaryWaypointId: string;
  readonly timeZone: string;
  readonly transitions: readonly NpcMovementTransition[];
}

export interface NpcMovementSnapshot {
  readonly npcId: string;
  readonly canonicalName: string;
  readonly actorArchetype: NpcActorArchetype;
  readonly isWitness: boolean;
  readonly witnessClassification: WitnessClassification;
  readonly witnessId: SyntheticWitnessId | null;
  readonly movementState: NpcMovementState;
  readonly currentWaypointId: string;
  readonly destinationWaypointId: string | null;
  readonly activeRouteId: string | null;
  readonly position: Vector3D;
  readonly forwardHeading: { readonly x: number; readonly z: number };
  readonly progression: number; // 0.0 to 1.0
  readonly movementStartTime: string | null;
  readonly expectedDurationSeconds: number;
  readonly associatedPlaceId: string;
  readonly activityState: string;
  readonly activityTitle: BilingualText;
  readonly evaluatedAt: string;
  readonly timeZone: string;
  readonly localCivilTime: string;
  readonly isDemonstrationMovement: true;
  readonly demonstrationNotice: BilingualText;
  // Increment 5.22.0 Autonomous Locomotion & Bounded Business Activity fields
  readonly behaviorProfileType?: NpcBehaviorProfileType;
  readonly autonomousBehaviorState?: NpcAutonomousBehaviorState;
  readonly controlMode?: NpcMovementControlMode;
  readonly currentSpeedMps?: number;
  readonly patrolDirection?: PatrolDirection;
  readonly patrolCycleIndex?: number;
  readonly isSuspendedByPlayerInteraction?: boolean;
  readonly suspendedByPlayerId?: string | null;
  readonly passingEncounterPartnerNpcId?: string | null;
  readonly passingEncounterSessionId?: string | null;
  readonly passingEncounterType?: PassingEncounterType | null;
  readonly businessDomainId?: string | null;
  readonly withinAuthorizedDomain?: boolean;
  readonly activeSegmentEndPosition?: Vector3D;
}

export interface NpcRosterMovementSnapshot {
  readonly evaluatedAt: string;
  readonly timeZone: string;
  readonly localCivilTime: string;
  readonly npcs: Readonly<Record<string, NpcMovementSnapshot>>;
  readonly npcsList: readonly NpcMovementSnapshot[];
  readonly summary: {
    readonly stationaryCount: number;
    readonly movingCount: number;
    readonly arrivedCount: number;
    readonly waitingCount: number;
    readonly totalCount: number;
  };
  readonly isDemonstrationMovement: true;
  readonly demonstrationNotice: BilingualText;
}

export const SIMULATED_MOVEMENT_NOTICE: BilingualText = Object.freeze({
  en: 'Simulated demonstration movement along Avenida Benito Juárez corridor for AI Hackathon Edition. Coordinates are corridor-relative Cartesian meters, not real-world GPS.',
  es: 'Movimiento de demostración simulado en el corredor de la Avenida Benito Juárez para AI Hackathon Edition. Las coordenadas corresponden a metros cartesianos relativos al corredor, no a GPS real.',
});

// ---------------------------------------------------------------------------
// 1. CANONICAL WAYPOINT GRAPH
// ---------------------------------------------------------------------------

export const CORRIDOR_WAYPOINTS: Readonly<Record<string, Waypoint>> = Object.freeze({
  // --- East Sidewalk & Bridge Infrastructure ---
  wp_carlos_counter: Object.freeze({
    id: 'wp_carlos_counter',
    associatedPlaceId: 'pharmacy:farmacias-benavides',
    position: Object.freeze({ x: 10.5, y: 0.2, z: 0.0 }),
    label: Object.freeze({
      en: 'Farmacias Benavides Service Counter',
      es: 'Mostrador de Atención de Farmacias Benavides',
    }),
    outgoingWaypointIds: Object.freeze([
      'wp_farmacia_storefront',
      'wp_farmacia_front_threshold',
      'wp_farmacia_display_north',
      'wp_farmacia_window_south',
      'wp_farmacia_consultorio_south',
    ]),
  }),

  wp_farmacia_storefront: Object.freeze({
    id: 'wp_farmacia_storefront',
    associatedPlaceId: 'pharmacy:farmacias-benavides',
    position: Object.freeze({ x: 8.0, y: 0.2, z: 1.0 }),
    label: Object.freeze({
      en: 'Farmacias Benavides Storefront Entrance',
      es: 'Acceso Exterior de Farmacias Benavides',
    }),
    outgoingWaypointIds: Object.freeze([
      'wp_carlos_counter',
      'wp_farmacia_display_north',
      'wp_farmacia_consultorio_south',
      'wp_east_sidewalk_mid',
      'wp_crosswalk_south',
    ]),
  }),

  wp_farmacia_front_threshold: Object.freeze({
    id: 'wp_farmacia_front_threshold',
    associatedPlaceId: 'pharmacy:farmacias-benavides',
    position: Object.freeze({ x: 8.55, y: 0.2, z: 0.4 }),
    label: Object.freeze({
      en: 'Farmacias Benavides Interior Frontage Threshold',
      es: 'Umbral Interior del Frente de Farmacias Benavides',
    }),
    outgoingWaypointIds: Object.freeze([
      'wp_carlos_counter',
      'wp_farmacia_display_north',
      'wp_farmacia_window_south',
    ]),
  }),

  wp_farmacia_display_north: Object.freeze({
    id: 'wp_farmacia_display_north',
    associatedPlaceId: 'pharmacy:farmacias-benavides',
    position: Object.freeze({ x: 10.2, y: 0.2, z: 3.2 }),
    label: Object.freeze({
      en: 'Farmacias Benavides North Display Window',
      es: 'Vitrina Norte de Farmacias Benavides',
    }),
    outgoingWaypointIds: Object.freeze(['wp_carlos_counter', 'wp_farmacia_front_threshold', 'wp_farmacia_storefront']),
  }),

  wp_farmacia_window_south: Object.freeze({
    id: 'wp_farmacia_window_south',
    associatedPlaceId: 'pharmacy:farmacias-benavides',
    position: Object.freeze({ x: 8.8, y: 0.2, z: -1.35 }),
    label: Object.freeze({
      en: 'Farmacias Benavides South Sidewalk Display',
      es: 'Zona Sur de Exhibición de Farmacias Benavides',
    }),
    outgoingWaypointIds: Object.freeze([
      'wp_carlos_counter',
      'wp_farmacia_front_threshold',
      'wp_farmacia_consultorio_south',
    ]),
  }),

  wp_farmacia_consultorio_south: Object.freeze({
    id: 'wp_farmacia_consultorio_south',
    associatedPlaceId: 'pharmacy:farmacias-benavides',
    position: Object.freeze({ x: 10.2, y: 0.2, z: -2.1 }),
    label: Object.freeze({
      en: 'Farmacias Benavides South Consultorio Display',
      es: 'Panel Sur de Consultorio de Farmacias Benavides',
    }),
    outgoingWaypointIds: Object.freeze(['wp_carlos_counter', 'wp_farmacia_window_south', 'wp_farmacia_storefront']),
  }),

  wp_east_sidewalk_mid: Object.freeze({
    id: 'wp_east_sidewalk_mid',
    associatedPlaceId: 'pharmacy:farmacias-benavides',
    position: Object.freeze({ x: 7.5, y: 0.2, z: 15.0 }),
    label: Object.freeze({
      en: 'East Sidewalk Corridor Walkway',
      es: 'Andador de Acera Oriente del Corredor',
    }),
    outgoingWaypointIds: Object.freeze(['wp_farmacia_storefront', 'wp_east_dining_benches']),
  }),

  wp_east_dining_benches: Object.freeze({
    id: 'wp_east_dining_benches',
    associatedPlaceId: 'pharmacy:farmacias-benavides',
    position: Object.freeze({ x: 8.5, y: 0.2, z: 22.0 }),
    label: Object.freeze({
      en: 'East Corridor Seating & Dining Benches',
      es: 'Bancas de Descanso y Alimentos de Acera Oriente',
    }),
    outgoingWaypointIds: Object.freeze(['wp_east_sidewalk_mid', 'wp_bridge_toll_plaza']),
  }),

  wp_bridge_south_approach: Object.freeze({
    id: 'wp_bridge_south_approach',
    associatedPlaceId: 'infrastructure:puente-paso-del-norte',
    position: Object.freeze({ x: 15.2, y: 0.24, z: 36.5 }),
    label: Object.freeze({
      en: 'Puente Paso del Norte South Pedestrian Concourse Entry',
      es: 'Acceso Sur del Andador Peatonal del Puente Paso del Norte',
    }),
    outgoingWaypointIds: Object.freeze(['wp_bridge_toll_plaza']),
  }),

  wp_bridge_toll_plaza: Object.freeze({
    id: 'wp_bridge_toll_plaza',
    associatedPlaceId: 'infrastructure:puente-paso-del-norte',
    position: Object.freeze({ x: 15.2, y: 0.288, z: 39.5 }),
    label: Object.freeze({
      en: 'Puente Paso del Norte Toll Plaza Approach',
      es: 'Aproximación a Casetas del Puente Paso del Norte',
    }),
    outgoingWaypointIds: Object.freeze([
      'wp_bridge_south_approach',
      'wp_east_dining_benches',
      'wp_canopy_waiting',
      'wp_mateo_queue_entry',
    ]),
  }),

  wp_canopy_waiting: Object.freeze({
    id: 'wp_canopy_waiting',
    associatedPlaceId: 'infrastructure:puente-paso-del-norte',
    position: Object.freeze({ x: 15.2, y: 0.304, z: 40.5 }),
    label: Object.freeze({
      en: 'Bridge Plaza Canopy Shelter Waiting Area',
      es: 'Área de Espera Techada de la Plaza del Puente',
    }),
    outgoingWaypointIds: Object.freeze(['wp_bridge_toll_plaza', 'wp_mateo_queue_entry', 'wp_sofia_queue_mid']),
  }),

  wp_mateo_queue_entry: Object.freeze({
    id: 'wp_mateo_queue_entry',
    associatedPlaceId: 'infrastructure:puente-paso-del-norte',
    position: Object.freeze({ x: 15.2, y: 0.328, z: 42.0 }),
    label: Object.freeze({
      en: 'Bridge Pedestrian Queue Entry',
      es: 'Inicio de Fila Peatonal del Puente',
    }),
    outgoingWaypointIds: Object.freeze(['wp_bridge_toll_plaza', 'wp_canopy_waiting', 'wp_sofia_queue_mid']),
  }),

  wp_elena_kiosk: Object.freeze({
    id: 'wp_elena_kiosk',
    associatedPlaceId: 'infrastructure:puente-paso-del-norte',
    position: Object.freeze({ x: 19.2, y: 0.2, z: 39.5 }),
    label: Object.freeze({
      en: 'Border Pedestrian Concierge Kiosk',
      es: 'Quiosco de Orientación Peatonal Fronteriza',
    }),
    outgoingWaypointIds: Object.freeze(['wp_bridge_plaza', 'wp_canopy_waiting']),
  }),

  wp_sofia_queue_mid: Object.freeze({
    id: 'wp_sofia_queue_mid',
    associatedPlaceId: 'infrastructure:puente-paso-del-norte',
    position: Object.freeze({ x: 15.2, y: 0.384, z: 45.5 }),
    label: Object.freeze({
      en: 'Bridge Pedestrian Queue Midpoint',
      es: 'Punto Medio de Fila Peatonal del Puente',
    }),
    outgoingWaypointIds: Object.freeze(['wp_mateo_queue_entry', 'wp_canopy_waiting', 'wp_hector_stand', 'wp_bridge_plaza']),
  }),

  wp_bridge_plaza: Object.freeze({
    id: 'wp_bridge_plaza',
    associatedPlaceId: 'infrastructure:puente-paso-del-norte',
    position: Object.freeze({ x: 17.8, y: 0.2, z: 42.0 }),
    label: Object.freeze({
      en: 'Paso del Norte Bridge Plaza Inspection Point',
      es: 'Punto de Supervisión de la Plaza del Puente',
    }),
    outgoingWaypointIds: Object.freeze(['wp_elena_kiosk', 'wp_sofia_queue_mid', 'wp_hector_bench']),
  }),

  wp_hector_bench: Object.freeze({
    id: 'wp_hector_bench',
    associatedPlaceId: 'infrastructure:puente-paso-del-norte',
    position: Object.freeze({ x: 15.2, y: 0.464, z: 50.5 }),
    label: Object.freeze({
      en: 'Artisan Wood Carving Workbench & Rest Bench',
      es: 'Mesa de Tallado Artesanal y Banca de Trabajo',
    }),
    outgoingWaypointIds: Object.freeze(['wp_hector_stand', 'wp_bridge_plaza', 'wp_bridge_north_overlook']),
  }),

  wp_hector_stand: Object.freeze({
    id: 'wp_hector_stand',
    associatedPlaceId: 'infrastructure:puente-paso-del-norte',
    position: Object.freeze({ x: 15.2, y: 0.44, z: 49.0 }),
    label: Object.freeze({
      en: 'Artisan Woodcraft Exhibition Display',
      es: 'Exhibidor Artesanal de Artesanías de Madera',
    }),
    outgoingWaypointIds: Object.freeze(['wp_sofia_queue_mid', 'wp_hector_bench']),
  }),

  wp_bridge_north_overlook: Object.freeze({
    id: 'wp_bridge_north_overlook',
    associatedPlaceId: 'infrastructure:puente-paso-del-norte',
    position: Object.freeze({ x: 15.5, y: 0.512, z: 53.5 }),
    label: Object.freeze({
      en: 'Puente Paso del Norte North Pedestrian Concourse Overlook',
      es: 'Mirador Norte del Andador Peatonal del Puente Paso del Norte',
    }),
    outgoingWaypointIds: Object.freeze(['wp_hector_bench']),
  }),

  // --- West Sidewalk & Commercial Entities ---
  wp_valeria_storefront: Object.freeze({
    id: 'wp_valeria_storefront',
    associatedPlaceId: 'optical:optica-fashion',
    position: Object.freeze({ x: -10.5, y: 0.2, z: 1.5 }),
    label: Object.freeze({
      en: 'Óptica Fashion Consultation Counter',
      es: 'Mostrador de Consulta de Óptica Fashion',
    }),
    outgoingWaypointIds: Object.freeze([
      'wp_optica_north_window',
      'wp_optica_plaza_threshold',
      'wp_optica_south_window',
      'wp_optica_entrance',
      'wp_optica_display_north',
      'wp_optica_display_south',
      'wp_optica_workshop',
    ]),
  }),

  wp_optica_north_window: Object.freeze({
    id: 'wp_optica_north_window',
    associatedPlaceId: 'optical:optica-fashion',
    position: Object.freeze({ x: -10.55, y: 0.2, z: 2.55 }),
    label: Object.freeze({
      en: 'Óptica Fashion North Eyewear Window',
      es: 'Vitrina Norte de Óptica Fashion',
    }),
    outgoingWaypointIds: Object.freeze([
      'wp_valeria_storefront',
      'wp_optica_plaza_threshold',
    ]),
  }),

  wp_optica_plaza_threshold: Object.freeze({
    id: 'wp_optica_plaza_threshold',
    associatedPlaceId: 'optical:optica-fashion',
    position: Object.freeze({ x: -8.35, y: 0.2, z: 0.85 }),
    label: Object.freeze({
      en: 'Óptica Fashion Interior Awning Promenade',
      es: 'Acceso Interior bajo Toldo de Óptica Fashion',
    }),
    outgoingWaypointIds: Object.freeze([
      'wp_valeria_storefront',
      'wp_optica_north_window',
      'wp_optica_south_promenade',
    ]),
  }),

  wp_optica_south_promenade: Object.freeze({
    id: 'wp_optica_south_promenade',
    associatedPlaceId: 'optical:optica-fashion',
    position: Object.freeze({ x: -8.55, y: 0.2, z: -1.1 }),
    label: Object.freeze({
      en: 'Óptica Fashion South Frontage Promenade',
      es: 'Corredor Sur del Frente de Óptica Fashion',
    }),
    outgoingWaypointIds: Object.freeze([
      'wp_optica_plaza_threshold',
      'wp_optica_south_window',
    ]),
  }),

  wp_optica_south_window: Object.freeze({
    id: 'wp_optica_south_window',
    associatedPlaceId: 'optical:optica-fashion',
    position: Object.freeze({ x: -10.55, y: 0.2, z: -0.55 }),
    label: Object.freeze({
      en: 'Óptica Fashion South Teal Display',
      es: 'Vitrina Sur de Óptica Fashion',
    }),
    outgoingWaypointIds: Object.freeze([
      'wp_valeria_storefront',
      'wp_optica_south_promenade',
    ]),
  }),

  wp_optica_entrance: Object.freeze({
    id: 'wp_optica_entrance',
    associatedPlaceId: 'optical:optica-fashion',
    position: Object.freeze({ x: -7.5, y: 0.2, z: 1.5 }),
    label: Object.freeze({
      en: 'Óptica Fashion West Sidewalk Entrance',
      es: 'Acceso Exterior de Óptica Fashion en Acera Poniente',
    }),
    outgoingWaypointIds: Object.freeze([
      'wp_valeria_storefront',
      'wp_optica_display_north',
      'wp_optica_display_south',
      'wp_optica_workshop',
      'wp_crosswalk_south',
    ]),
  }),

  wp_optica_display_north: Object.freeze({
    id: 'wp_optica_display_north',
    associatedPlaceId: 'optical:optica-fashion',
    position: Object.freeze({ x: -10.2, y: 0.2, z: 5.2 }),
    label: Object.freeze({
      en: 'Óptica Fashion North Eyewear Showcase',
      es: 'Vitrina Norte de Armazones de Óptica Fashion',
    }),
    outgoingWaypointIds: Object.freeze([
      'wp_valeria_storefront',
      'wp_optica_entrance',
      'wp_optica_workshop',
    ]),
  }),

  wp_optica_display_south: Object.freeze({
    id: 'wp_optica_display_south',
    associatedPlaceId: 'optical:optica-fashion',
    position: Object.freeze({ x: -10.2, y: 0.2, z: -2.2 }),
    label: Object.freeze({
      en: 'Óptica Fashion South Frame Display Window',
      es: 'Vitrina Sur de Colección de Óptica Fashion',
    }),
    outgoingWaypointIds: Object.freeze(['wp_valeria_storefront', 'wp_optica_entrance']),
  }),

  wp_optica_workshop: Object.freeze({
    id: 'wp_optica_workshop',
    associatedPlaceId: 'optical:optica-fashion',
    position: Object.freeze({ x: -9.5, y: 0.2, z: 8.0 }),
    label: Object.freeze({
      en: 'Óptica Calibration & Frame Alignment Area',
      es: 'Área de Calibración y Ajuste de Armazones',
    }),
    outgoingWaypointIds: Object.freeze([
      'wp_valeria_storefront',
      'wp_optica_entrance',
      'wp_optica_display_north',
      'wp_patricia_desk',
    ]),
  }),

  wp_patricia_desk: Object.freeze({
    id: 'wp_patricia_desk',
    associatedPlaceId: 'desk:patricia-citizen-rep',
    position: Object.freeze({ x: -10.8, y: 0.0, z: 18.55 }),
    label: Object.freeze({
      en: 'Municipal Citizen Issue Reporting Desk',
      es: 'Módulo Municipal de Atención Ciudadana',
    }),
    outgoingWaypointIds: Object.freeze(['wp_optica_workshop', 'wp_west_inspection']),
  }),

  wp_west_inspection: Object.freeze({
    id: 'wp_west_inspection',
    associatedPlaceId: 'desk:patricia-citizen-rep',
    position: Object.freeze({ x: -7.5, y: 0.0, z: 26.0 }),
    label: Object.freeze({
      en: 'Corridor Infrastructure & Public Works Inspection Point',
      es: 'Punto de Inspección de Obras Públicas y Pavimento',
    }),
    outgoingWaypointIds: Object.freeze(['wp_patricia_desk', 'wp_crosswalk_north']),
  }),

  // --- Pedestrian Crosswalks ---
  wp_crosswalk_south: Object.freeze({
    id: 'wp_crosswalk_south',
    associatedPlaceId: 'pharmacy:farmacias-benavides',
    position: Object.freeze({ x: 0.0, y: 0.0, z: 5.0 }),
    label: Object.freeze({
      en: 'Avenida Juárez South Pedestrian Crosswalk',
      es: 'Paso Peatonal Sur de Avenida Benito Juárez',
    }),
    outgoingWaypointIds: Object.freeze(['wp_farmacia_storefront', 'wp_optica_entrance']),
  }),

  wp_crosswalk_north: Object.freeze({
    id: 'wp_crosswalk_north',
    associatedPlaceId: 'infrastructure:puente-paso-del-norte',
    position: Object.freeze({ x: 0.0, y: 0.0, z: 30.0 }),
    label: Object.freeze({
      en: 'Avenida Juárez North Pedestrian Crosswalk',
      es: 'Paso Peatonal Norte de Avenida Benito Juárez',
    }),
    outgoingWaypointIds: Object.freeze(['wp_west_inspection', 'wp_bridge_toll_plaza']),
  }),
});

// ---------------------------------------------------------------------------
// 2. ROUTE HELPERS & CANONICAL ROUTES
// ---------------------------------------------------------------------------

function calculateDistance(a: Vector3D, b: Vector3D): number {
  const dx = b.x - a.x;
  const dy = b.y - a.y;
  const dz = b.z - a.z;
  return Math.sqrt(dx * dx + dy * dy + dz * dz);
}

function buildRoute(id: string, waypointIds: readonly string[], speedMps: number = 1.0): Route {
  if (waypointIds.length < 2) {
    throw new Error(`Route "${id}" requires at least 2 waypoints, got ${waypointIds.length}`);
  }

  const segments: RouteSegment[] = [];
  let totalDistance = 0;
  let totalDuration = 0;

  for (let i = 0; i < waypointIds.length - 1; i++) {
    const fromId = waypointIds[i]!;
    const toId = waypointIds[i + 1]!;
    const fromWp = CORRIDOR_WAYPOINTS[fromId];
    const toWp = CORRIDOR_WAYPOINTS[toId];

    if (!fromWp) throw new Error(`Route "${id}" references unknown waypoint "${fromId}"`);
    if (!toWp) throw new Error(`Route "${id}" references unknown waypoint "${toId}"`);

    // Verify adjacency connection
    if (!fromWp.outgoingWaypointIds.includes(toId)) {
      throw new Error(`Route "${id}" segment ${fromId} -> ${toId} is not connected in waypoint graph`);
    }

    const dist = calculateDistance(fromWp.position, toWp.position);
    // At least 5 seconds per segment for clear visual presence
    const dur = Math.max(5, Math.round(dist / speedMps));

    segments.push(Object.freeze({
      fromWaypointId: fromId,
      toWaypointId: toId,
      distanceMeters: Math.round(dist * 100) / 100,
      durationSeconds: dur,
    }));

    totalDistance += dist;
    totalDuration += dur;
  }

  return Object.freeze({
    id,
    waypointIds: Object.freeze([...waypointIds]),
    segments: Object.freeze(segments),
    totalDistanceMeters: Math.round(totalDistance * 100) / 100,
    totalDurationSeconds: totalDuration,
  });
}

export const CORRIDOR_ROUTES: Readonly<Record<string, Route>> = Object.freeze({
  // 1. Elena Ramos: Kiosk <-> Plaza
  route_elena_kiosk_to_plaza: buildRoute('route_elena_kiosk_to_plaza', ['wp_elena_kiosk', 'wp_bridge_plaza'], 0.5),
  route_elena_plaza_to_kiosk: buildRoute('route_elena_plaza_to_kiosk', ['wp_bridge_plaza', 'wp_elena_kiosk'], 0.5),

  // 2. Carlos Méndez: Pharmacy Counter <-> Dining Benches
  route_carlos_to_lunch: buildRoute(
    'route_carlos_to_lunch',
    ['wp_carlos_counter', 'wp_farmacia_storefront', 'wp_east_sidewalk_mid', 'wp_east_dining_benches'],
    0.8
  ),
  route_carlos_to_counter: buildRoute(
    'route_carlos_to_counter',
    ['wp_east_dining_benches', 'wp_east_sidewalk_mid', 'wp_farmacia_storefront', 'wp_carlos_counter'],
    0.8
  ),

  // 3. Valeria Domínguez: Óptica Counter <-> Workshop
  route_valeria_to_workshop: buildRoute(
    'route_valeria_to_workshop',
    ['wp_valeria_storefront', 'wp_optica_entrance', 'wp_optica_workshop'],
    0.6
  ),
  route_valeria_to_counter: buildRoute(
    'route_valeria_to_counter',
    ['wp_optica_workshop', 'wp_optica_entrance', 'wp_valeria_storefront'],
    0.6
  ),

  // 4. Mateo Navarro: Queue Entry <-> Toll Plaza Approach
  route_mateo_advance_queue: buildRoute(
    'route_mateo_advance_queue',
    ['wp_mateo_queue_entry', 'wp_canopy_waiting', 'wp_bridge_toll_plaza'],
    0.7
  ),
  route_mateo_return_queue: buildRoute(
    'route_mateo_return_queue',
    ['wp_bridge_toll_plaza', 'wp_canopy_waiting', 'wp_mateo_queue_entry'],
    0.7
  ),

  // 5. Sofía Alarcón: Queue Mid <-> Canopy Waiting
  route_sofia_to_canopy: buildRoute('route_sofia_to_canopy', ['wp_sofia_queue_mid', 'wp_canopy_waiting'], 0.6),
  route_sofia_to_queue: buildRoute('route_sofia_to_queue', ['wp_canopy_waiting', 'wp_sofia_queue_mid'], 0.6),

  // 6. Don Héctor Méndez: Stand <-> Woodcarving Workbench
  route_hector_to_bench: buildRoute('route_hector_to_bench', ['wp_hector_stand', 'wp_hector_bench'], 0.4),
  route_hector_to_stand: buildRoute('route_hector_to_stand', ['wp_hector_bench', 'wp_hector_stand'], 0.4),

  // 7. Lic. Patricia Morales: Municipal Desk <-> Public Works Inspection
  route_patricia_to_inspection: buildRoute('route_patricia_to_inspection', ['wp_patricia_desk', 'wp_west_inspection'], 0.6),
  route_patricia_to_desk: buildRoute('route_patricia_to_desk', ['wp_west_inspection', 'wp_patricia_desk'], 0.6),
});

// ---------------------------------------------------------------------------
// 3. NPC MOVEMENT PROFILES (ALL 7 NPCS)
// ---------------------------------------------------------------------------

const TZ_JUAREZ = 'America/Ciudad_Juarez';

export const NPC_MOVEMENT_PROFILES: Readonly<Record<string, NpcMovementProfile>> = Object.freeze({
  // 1. Elena Ramos — Shift Handover & Log Inspection
  'elena-border-concierge': Object.freeze({
    npcId: 'elena-border-concierge',
    canonicalName: 'Elena Ramos',
    primaryWaypointId: 'wp_elena_kiosk',
    timeZone: TZ_JUAREZ,
    transitions: Object.freeze([
      Object.freeze({
        id: 'elena_handover_walk',
        daysOfWeek: Object.freeze([0, 1, 2, 3, 4, 5, 6]), // Every day
        departureHour: 13,
        departureMinute: 30,
        outboundRouteId: 'route_elena_kiosk_to_plaza',
        outboundDurationSeconds: CORRIDOR_ROUTES['route_elena_kiosk_to_plaza']!.totalDurationSeconds,
        destinationWaypointId: 'wp_bridge_plaza',
        arrivalDwellSeconds: 15,
        returnHour: 14,
        returnMinute: 29,
        returnRouteId: 'route_elena_plaza_to_kiosk',
        returnDurationSeconds: CORRIDOR_ROUTES['route_elena_plaza_to_kiosk']!.totalDurationSeconds,
        description: Object.freeze({
          en: 'Walking to bridge plaza for shift handover and log reconciliation.',
          es: 'Caminando hacia la plaza del puente para relevo de turno y bitácoras.',
        }),
      }),
    ]),
  }),

  // 2. Carlos Méndez — Lunch & Rest Break
  'carlos-farmacia-attendant': Object.freeze({
    npcId: 'carlos-farmacia-attendant',
    canonicalName: 'Carlos Méndez',
    primaryWaypointId: 'wp_carlos_counter',
    timeZone: TZ_JUAREZ,
    transitions: Object.freeze([
      Object.freeze({
        id: 'carlos_lunch_walk',
        daysOfWeek: Object.freeze([1, 2, 3, 4, 5, 6]), // Mon-Sat
        departureHour: 14,
        departureMinute: 0,
        outboundRouteId: 'route_carlos_to_lunch',
        outboundDurationSeconds: CORRIDOR_ROUTES['route_carlos_to_lunch']!.totalDurationSeconds,
        destinationWaypointId: 'wp_east_dining_benches',
        arrivalDwellSeconds: 20,
        returnHour: 14,
        returnMinute: 59,
        returnRouteId: 'route_carlos_to_counter',
        returnDurationSeconds: CORRIDOR_ROUTES['route_carlos_to_counter']!.totalDurationSeconds,
        description: Object.freeze({
          en: 'Walking along East sidewalk to corridor benches for lunch break.',
          es: 'Caminando por la acera oriente hacia las bancas para tomar su descanso de almuerzo.',
        }),
      }),
    ]),
  }),

  // 3. Valeria Domínguez — Equipment Calibration & Cleaning
  'valeria-optica-attendant': Object.freeze({
    npcId: 'valeria-optica-attendant',
    canonicalName: 'Valeria Domínguez',
    primaryWaypointId: 'wp_valeria_storefront',
    timeZone: TZ_JUAREZ,
    transitions: Object.freeze([
      Object.freeze({
        id: 'valeria_calibration_walk',
        daysOfWeek: Object.freeze([1, 2, 3, 4, 5, 6]), // Mon-Sat
        departureHour: 13,
        departureMinute: 0,
        outboundRouteId: 'route_valeria_to_workshop',
        outboundDurationSeconds: CORRIDOR_ROUTES['route_valeria_to_workshop']!.totalDurationSeconds,
        destinationWaypointId: 'wp_optica_workshop',
        arrivalDwellSeconds: 15,
        returnHour: 13,
        returnMinute: 44,
        returnRouteId: 'route_valeria_to_counter',
        returnDurationSeconds: CORRIDOR_ROUTES['route_valeria_to_counter']!.totalDurationSeconds,
        // wp_optica_workshop (Z=8.0) lies physically in front of Bar La Cucaracha outside Óptica Fashion (Z=-1.80..+3.40),
        // and no physical workshop geometry exists in the scene; retain schedule/busy task at verified storefront station.
        isPhysicalDestinationVerified: false,
        description: Object.freeze({
          en: 'Moving to workshop area for trial lens sanitization and equipment calibration.',
          es: 'Trasladándose al taller para desinfección de lentes y calibración de instrumentos.',
        }),
      }),
    ]),
  }),

  // 4. Mateo Navarro — Queue Advance
  'mateo-student': Object.freeze({
    npcId: 'mateo-student',
    canonicalName: 'Mateo Navarro',
    primaryWaypointId: 'wp_mateo_queue_entry',
    timeZone: TZ_JUAREZ,
    transitions: Object.freeze([
      Object.freeze({
        id: 'mateo_queue_advance',
        daysOfWeek: Object.freeze([1, 2, 3, 4, 5]), // Mon-Fri
        departureHour: 9,
        departureMinute: 30,
        outboundRouteId: 'route_mateo_advance_queue',
        outboundDurationSeconds: CORRIDOR_ROUTES['route_mateo_advance_queue']!.totalDurationSeconds,
        destinationWaypointId: 'wp_bridge_toll_plaza',
        arrivalDwellSeconds: 15,
        returnHour: 9,
        returnMinute: 59,
        returnRouteId: 'route_mateo_return_queue',
        returnDurationSeconds: CORRIDOR_ROUTES['route_mateo_return_queue']!.totalDurationSeconds,
        description: Object.freeze({
          en: 'Advancing in pedestrian line towards toll plaza turnstiles.',
          es: 'Avanzando en la fila peatonal hacia las casetas del puente.',
        }),
      }),
    ]),
  }),

  // 5. Sofía Alarcón — Canopy Coordinator
  'sofia-cross-border-retail': Object.freeze({
    npcId: 'sofia-cross-border-retail',
    canonicalName: 'Sofía Alarcón',
    primaryWaypointId: 'wp_sofia_queue_mid',
    timeZone: TZ_JUAREZ,
    transitions: Object.freeze([
      Object.freeze({
        id: 'sofia_canopy_walk',
        daysOfWeek: Object.freeze([1, 2, 3, 4, 5, 6]), // Mon-Sat
        departureHour: 11,
        departureMinute: 0,
        outboundRouteId: 'route_sofia_to_canopy',
        outboundDurationSeconds: CORRIDOR_ROUTES['route_sofia_to_canopy']!.totalDurationSeconds,
        destinationWaypointId: 'wp_canopy_waiting',
        arrivalDwellSeconds: 15,
        returnHour: 11,
        returnMinute: 29,
        returnRouteId: 'route_sofia_to_queue',
        returnDurationSeconds: CORRIDOR_ROUTES['route_sofia_to_queue']!.totalDurationSeconds,
        description: Object.freeze({
          en: 'Stepping under canopy shelter to coordinate shift with store manager.',
          es: 'Acercándose bajo el techado para coordinar horario con su supervisor.',
        }),
      }),
    ]),
  }),

  // 6. Don Héctor Méndez — Wood Carving Workbench
  'hector-artisan': Object.freeze({
    npcId: 'hector-artisan',
    canonicalName: 'Don Héctor Méndez',
    primaryWaypointId: 'wp_hector_stand',
    timeZone: TZ_JUAREZ,
    transitions: Object.freeze([
      Object.freeze({
        id: 'hector_carving_walk',
        daysOfWeek: Object.freeze([1, 2, 3, 4, 5, 6]), // Mon-Sat
        departureHour: 13,
        departureMinute: 0,
        outboundRouteId: 'route_hector_to_bench',
        outboundDurationSeconds: CORRIDOR_ROUTES['route_hector_to_bench']!.totalDurationSeconds,
        destinationWaypointId: 'wp_hector_bench',
        arrivalDwellSeconds: 15,
        returnHour: 13,
        returnMinute: 59,
        returnRouteId: 'route_hector_to_stand',
        returnDurationSeconds: CORRIDOR_ROUTES['route_hector_to_stand']!.totalDurationSeconds,
        description: Object.freeze({
          en: 'Moving to wooden carving bench to polish and detail handcrafted figures.',
          es: 'Pasando a la mesa de tallado para pulir y detallar figuras de madera.',
        }),
      }),
    ]),
  }),

  // 7. Lic. Patricia Morales — Municipal Infrastructure Inspection
  'patricia-citizen-rep': Object.freeze({
    npcId: 'patricia-citizen-rep',
    canonicalName: 'Lic. Patricia Morales',
    primaryWaypointId: 'wp_patricia_desk',
    timeZone: TZ_JUAREZ,
    transitions: Object.freeze([
      Object.freeze({
        id: 'patricia_inspection_walk',
        daysOfWeek: Object.freeze([1, 2, 3, 4, 5]), // Mon-Fri
        departureHour: 12,
        departureMinute: 0,
        outboundRouteId: 'route_patricia_to_inspection',
        outboundDurationSeconds: CORRIDOR_ROUTES['route_patricia_to_inspection']!.totalDurationSeconds,
        destinationWaypointId: 'wp_west_inspection',
        arrivalDwellSeconds: 20,
        returnHour: 12,
        returnMinute: 59,
        returnRouteId: 'route_patricia_to_desk',
        returnDurationSeconds: CORRIDOR_ROUTES['route_patricia_to_desk']!.totalDurationSeconds,
        description: Object.freeze({
          en: 'Walking along West sidewalk to inspect municipal pavement and public lighting.',
          es: 'Caminando por la acera poniente para inspección de bacheo y luminarias públicas.',
        }),
      }),
    ]),
  }),
});

// ---------------------------------------------------------------------------
// 4. ROUTE INTERPOLATION & HEADING MATHEMATICS
// ---------------------------------------------------------------------------

function interpolateAlongRoute(
  route: Route,
  progressRatio: number
): {
  position: Vector3D;
  heading: { x: number; z: number };
  currentWaypointId: string;
  segmentEndPosition: Vector3D;
  segmentSpeedMps: number;
} {
  const clampedProgress = Math.max(0, Math.min(1, progressRatio));

  if (clampedProgress <= 0) {
    const firstSeg = route.segments[0]!;
    const firstWpId = firstSeg.fromWaypointId;
    const firstWp = CORRIDOR_WAYPOINTS[firstWpId]!;
    const nextWp = CORRIDOR_WAYPOINTS[firstSeg.toWaypointId]!;
    const dx = nextWp.position.x - firstWp.position.x;
    const dz = nextWp.position.z - firstWp.position.z;
    const len = Math.sqrt(dx * dx + dz * dz) || 1;
    return {
      position: firstWp.position,
      heading: { x: dx / len, z: dz / len },
      currentWaypointId: firstWpId,
      segmentEndPosition: nextWp.position,
      segmentSpeedMps: Number((firstSeg.distanceMeters / Math.max(0.001, firstSeg.durationSeconds)).toFixed(3)),
    };
  }

  if (clampedProgress >= 1) {
    const lastSeg = route.segments[route.segments.length - 1]!;
    const lastWp = CORRIDOR_WAYPOINTS[lastSeg.toWaypointId]!;
    const prevWp = CORRIDOR_WAYPOINTS[lastSeg.fromWaypointId]!;
    const dx = lastWp.position.x - prevWp.position.x;
    const dz = lastWp.position.z - prevWp.position.z;
    const len = Math.sqrt(dx * dx + dz * dz) || 1;
    return {
      position: lastWp.position,
      heading: { x: dx / len, z: dz / len },
      currentWaypointId: lastSeg.toWaypointId,
      segmentEndPosition: lastWp.position,
      segmentSpeedMps: 0,
    };
  }

  const targetElapsedSeconds = clampedProgress * route.totalDurationSeconds;
  let accumulatedSeconds = 0;

  for (let i = 0; i < route.segments.length; i++) {
    const seg = route.segments[i]!;
    const segEnd = accumulatedSeconds + seg.durationSeconds;

    if (targetElapsedSeconds <= segEnd || i === route.segments.length - 1) {
      const segElapsed = targetElapsedSeconds - accumulatedSeconds;
      const segRatio = Math.max(0, Math.min(1, segElapsed / seg.durationSeconds));

      const fromWp = CORRIDOR_WAYPOINTS[seg.fromWaypointId]!;
      const toWp = CORRIDOR_WAYPOINTS[seg.toWaypointId]!;

      // Linear segment interpolation
      const x = fromWp.position.x + (toWp.position.x - fromWp.position.x) * segRatio;
      const y = fromWp.position.y + (toWp.position.y - fromWp.position.y) * segRatio;
      const z = fromWp.position.z + (toWp.position.z - fromWp.position.z) * segRatio;

      const dx = toWp.position.x - fromWp.position.x;
      const dz = toWp.position.z - fromWp.position.z;
      const len = Math.sqrt(dx * dx + dz * dz) || 1;

      const activeWaypointId = segRatio < 0.5 ? seg.fromWaypointId : seg.toWaypointId;

      return {
        position: {
          x: Math.round(x * 1000) / 1000,
          y: Math.round(y * 1000) / 1000,
          z: Math.round(z * 1000) / 1000,
        },
        heading: {
          x: Math.round((dx / len) * 1000) / 1000,
          z: Math.round((dz / len) * 1000) / 1000,
        },
        currentWaypointId: activeWaypointId,
        segmentEndPosition: toWp.position,
        segmentSpeedMps: Number((seg.distanceMeters / Math.max(0.001, seg.durationSeconds)).toFixed(3)),
      };
    }

    accumulatedSeconds = segEnd;
  }

  // Fallback to end
  const finalWpId = route.segments[route.segments.length - 1]!.toWaypointId;
  const finalPos = CORRIDOR_WAYPOINTS[finalWpId]!.position;
  return {
    position: finalPos,
    heading: { x: 0, z: 1 },
    currentWaypointId: finalWpId,
    segmentEndPosition: finalPos,
    segmentSpeedMps: 0,
  };
}

// ---------------------------------------------------------------------------
// 5. DETERMINISTIC EVALUATION
// ---------------------------------------------------------------------------

/**
 * Evaluates the base movement state of a single NPC (prior to multi-NPC passing-encounter pairing).
 */
function evaluateSingleNpcMovementBase(
  profile: NpcMovementProfile,
  evaluatedAt: Date | string | number,
  options?: EvaluateAutonomousMovementOptions
): NpcMovementSnapshot {
  const evaluatedAtIso =
    typeof evaluatedAt === 'string' ? evaluatedAt : new Date(evaluatedAt).toISOString();
  const rawEvaluatedAtMs =
    typeof evaluatedAt === 'number'
      ? evaluatedAt
      : evaluatedAt instanceof Date
      ? evaluatedAt.getTime()
      : Date.parse(evaluatedAt);

  const civil: CivilTime = deriveCivilTime(evaluatedAt, profile.timeZone);
  const totalSecondsInDay = civil.hour * 3600 + civil.minute * 60 + civil.second;

  const primaryWp = CORRIDOR_WAYPOINTS[profile.primaryWaypointId]!;

  // Retrieve current actor classification & activity state for seamless cross-domain coherence
  const classification = getNpcActorClassification(profile.npcId);
  const activityProfile = NPC_SCHEDULE_ROSTER[profile.npcId];
  const activitySnapshot = activityProfile ? evaluateNpcActivity(activityProfile, evaluatedAt) : null;
  const actorArchetype: NpcActorArchetype =
    classification?.actorArchetype ?? activitySnapshot?.actorArchetype ?? 'business_attendant';
  const isWitness = classification?.isWitness ?? activitySnapshot?.isWitness ?? false;
  const witnessClassification: WitnessClassification =
    classification?.witnessClassification ?? activitySnapshot?.witnessClassification ?? 'none';
  const witnessId: SyntheticWitnessId | null =
    classification?.witnessId !== undefined ? classification.witnessId : (activitySnapshot?.witnessId ?? null);
  const activityState = activitySnapshot?.state ?? 'available';
  const activityTitle = activitySnapshot?.activityTitle ?? { en: 'On duty', es: 'En turno' };

  const autonomyProfile = getNpcBehavioralAutonomyProfile(profile.npcId);
  const behaviorProfileType: NpcBehaviorProfileType =
    autonomyProfile?.behaviorProfileType ?? 'fixed_anchor_scheduled';
  const pauseIntervals = options?.pauseIntervalsByNpcId?.[profile.npcId];

  // Check matching scheduled transition for today
  let activeTransition: NpcMovementTransition | null = null;
  for (const trans of profile.transitions) {
    if (trans.daysOfWeek.includes(civil.dayOfWeek)) {
      activeTransition = trans;
      break;
    }
  }

  if (activeTransition) {
    const outboundRoute = CORRIDOR_ROUTES[activeTransition.outboundRouteId]!;
    const returnRoute = CORRIDOR_ROUTES[activeTransition.returnRouteId]!;
    const destWp = CORRIDOR_WAYPOINTS[activeTransition.destinationWaypointId]!;

    const outboundStartSec = activeTransition.departureHour * 3600 + activeTransition.departureMinute * 60;
    const outboundEndSec = outboundStartSec + outboundRoute.totalDurationSeconds;
    const outboundDwellEndSec = outboundEndSec + activeTransition.arrivalDwellSeconds;

    const returnStartSec = activeTransition.returnHour * 3600 + activeTransition.returnMinute * 60;
    const returnEndSec = returnStartSec + returnRoute.totalDurationSeconds;
    const returnDwellEndSec = returnEndSec + activeTransition.arrivalDwellSeconds;

    const inScheduledWindow =
      totalSecondsInDay >= outboundStartSec && totalSecondsInDay < returnDwellEndSec;

    if (inScheduledWindow) {
      if (activeTransition.isPhysicalDestinationVerified === false) {
        const businessDomain = BUSINESS_AUTONOMY_DOMAINS[profile.npcId] ?? null;
        const stationHeading = profile.npcId === 'carlos-farmacia-attendant' ? { x: -1, z: 0 } : { x: 1, z: 0 };
        return Object.freeze({
          npcId: profile.npcId,
          canonicalName: profile.canonicalName,
          actorArchetype,
          isWitness,
          witnessClassification,
          witnessId,
          movementState: 'stationary',
          currentWaypointId: profile.primaryWaypointId,
          destinationWaypointId: null,
          activeRouteId: null,
          position: Object.freeze(primaryWp.position),
          forwardHeading: Object.freeze(stationHeading),
          progression: 0,
          movementStartTime: null,
          expectedDurationSeconds: 0,
          associatedPlaceId: primaryWp.associatedPlaceId,
          activityState,
          activityTitle,
          evaluatedAt: evaluatedAtIso,
          timeZone: profile.timeZone,
          localCivilTime: civil.isoCivilString,
          isDemonstrationMovement: true,
          demonstrationNotice: SIMULATED_MOVEMENT_NOTICE,
          behaviorProfileType,
          autonomousBehaviorState: 'scheduled_station_task',
          controlMode: 'scheduled_routine_transition',
          currentSpeedMps: 0,
          patrolDirection: 'none',
          patrolCycleIndex: 0,
          isSuspendedByPlayerInteraction: false,
          suspendedByPlayerId: null,
          passingEncounterPartnerNpcId: null,
          passingEncounterSessionId: null,
          passingEncounterType: null,
          businessDomainId: businessDomain?.domainId ?? null,
          withinAuthorizedDomain: businessDomain ? isPointInBusinessDomain(primaryWp.position, businessDomain) : true,
          activeSegmentEndPosition: Object.freeze(primaryWp.position),
        });
      }

      let movementState: NpcMovementState = 'stationary';
      let currentWaypointId = profile.primaryWaypointId;
      let destinationWaypointId: string | null = null;
      let activeRouteId: string | null = null;
      let position: Vector3D = primaryWp.position;
      let forwardHeading: { x: number; z: number } = { x: 0, z: 1 };
      let progression = 0;
      let movementStartTime: string | null = null;
      let expectedDurationSeconds = 0;
      let associatedPlaceId = primaryWp.associatedPlaceId;
      let activeSegmentSpeedMps = 0;
      let activeSegmentEndPosition: Vector3D = primaryWp.position;

      if (totalSecondsInDay >= outboundStartSec && totalSecondsInDay < outboundEndSec) {
        movementState = 'moving';
        activeRouteId = outboundRoute.id;
        destinationWaypointId = activeTransition.destinationWaypointId;
        expectedDurationSeconds = outboundRoute.totalDurationSeconds;
        const elapsed = totalSecondsInDay - outboundStartSec;
        progression = Math.round((elapsed / outboundRoute.totalDurationSeconds) * 1000) / 1000;

        const interpolated = interpolateAlongRoute(outboundRoute, progression);
        position = interpolated.position;
        forwardHeading = interpolated.heading;
        currentWaypointId = interpolated.currentWaypointId;
        activeSegmentSpeedMps = interpolated.segmentSpeedMps;
        activeSegmentEndPosition = interpolated.segmentEndPosition;

        const currentWp = CORRIDOR_WAYPOINTS[currentWaypointId]!;
        associatedPlaceId = currentWp.associatedPlaceId;

        movementStartTime = buildIsoDepartureTime(
          civil,
          activeTransition.departureHour,
          activeTransition.departureMinute,
          profile.timeZone
        );
      } else if (totalSecondsInDay >= outboundEndSec && totalSecondsInDay < outboundDwellEndSec) {
        movementState = 'arrived';
        currentWaypointId = activeTransition.destinationWaypointId;
        destinationWaypointId = activeTransition.destinationWaypointId;
        position = destWp.position;
        activeSegmentEndPosition = destWp.position;
        associatedPlaceId = destWp.associatedPlaceId;
        progression = 1.0;
        expectedDurationSeconds = outboundRoute.totalDurationSeconds;
      } else if (totalSecondsInDay >= outboundDwellEndSec && totalSecondsInDay < returnStartSec) {
        movementState = 'waiting';
        currentWaypointId = activeTransition.destinationWaypointId;
        destinationWaypointId = null;
        position = destWp.position;
        activeSegmentEndPosition = destWp.position;
        associatedPlaceId = destWp.associatedPlaceId;
        progression = 0.0;
      } else if (totalSecondsInDay >= returnStartSec && totalSecondsInDay < returnEndSec) {
        movementState = 'moving';
        activeRouteId = returnRoute.id;
        destinationWaypointId = profile.primaryWaypointId;
        expectedDurationSeconds = returnRoute.totalDurationSeconds;
        const elapsed = totalSecondsInDay - returnStartSec;
        progression = Math.round((elapsed / returnRoute.totalDurationSeconds) * 1000) / 1000;

        const interpolated = interpolateAlongRoute(returnRoute, progression);
        position = interpolated.position;
        forwardHeading = interpolated.heading;
        currentWaypointId = interpolated.currentWaypointId;
        activeSegmentSpeedMps = interpolated.segmentSpeedMps;
        activeSegmentEndPosition = interpolated.segmentEndPosition;

        const currentWp = CORRIDOR_WAYPOINTS[currentWaypointId]!;
        associatedPlaceId = currentWp.associatedPlaceId;

        movementStartTime = buildIsoDepartureTime(
          civil,
          activeTransition.returnHour,
          activeTransition.returnMinute,
          profile.timeZone
        );
      } else if (totalSecondsInDay >= returnEndSec && totalSecondsInDay < returnDwellEndSec) {
        movementState = 'arrived';
        currentWaypointId = profile.primaryWaypointId;
        destinationWaypointId = profile.primaryWaypointId;
        position = primaryWp.position;
        activeSegmentEndPosition = primaryWp.position;
        associatedPlaceId = primaryWp.associatedPlaceId;
        progression = 1.0;
        expectedDurationSeconds = returnRoute.totalDurationSeconds;
      }

      const scheduledBehaviorState: NpcAutonomousBehaviorState =
        movementState === 'moving'
          ? behaviorProfileType === 'bridge_patrol'
            ? 'patrolling'
            : 'scheduled_transit'
          : behaviorProfileType === 'bridge_patrol'
          ? 'observing'
          : 'scheduled_station_task';

      const businessDomain = BUSINESS_AUTONOMY_DOMAINS[profile.npcId] ?? null;

      return Object.freeze({
        npcId: profile.npcId,
        canonicalName: profile.canonicalName,
        actorArchetype,
        isWitness,
        witnessClassification,
        witnessId,
        movementState,
        currentWaypointId,
        destinationWaypointId,
        activeRouteId,
        position: Object.freeze(position),
        forwardHeading: Object.freeze(forwardHeading),
        progression,
        movementStartTime,
        expectedDurationSeconds,
        associatedPlaceId,
        activityState,
        activityTitle,
        evaluatedAt: evaluatedAtIso,
        timeZone: profile.timeZone,
        localCivilTime: civil.isoCivilString,
        isDemonstrationMovement: true,
        demonstrationNotice: SIMULATED_MOVEMENT_NOTICE,
        behaviorProfileType,
        autonomousBehaviorState: scheduledBehaviorState,
        controlMode: 'scheduled_routine_transition',
        currentSpeedMps: movementState === 'moving' ? activeSegmentSpeedMps : 0,
        patrolDirection:
          behaviorProfileType === 'bridge_patrol'
            ? forwardHeading.z >= 0
              ? 'northbound'
              : 'southbound'
            : 'none',
        patrolCycleIndex: 0,
        isSuspendedByPlayerInteraction: false,
        suspendedByPlayerId: null,
        passingEncounterPartnerNpcId: null,
        passingEncounterSessionId: null,
        passingEncounterType: null,
        businessDomainId:
          behaviorProfileType === 'bridge_patrol'
            ? 'domain:puente-paso-del-norte-pedestrian-concourse'
            : businessDomain?.domainId ?? null,
        withinAuthorizedDomain:
          behaviorProfileType === 'bridge_patrol'
            ? isPointInBridgePatrolDomain(position)
            : businessDomain
            ? isPointInBusinessDomain(position, businessDomain) ||
              businessDomain.authorizedExternalTaskTransitionIds.includes(activeTransition.id)
            : true,
        activeSegmentEndPosition: Object.freeze(activeSegmentEndPosition),
      });
    }
  }

  // Subsystem A: Community Player Proxy Bridge Patrol
  const bridgeProfile = BRIDGE_PATROL_PROFILES[profile.npcId];
  if (bridgeProfile) {
    const patrolRes = evaluateBridgePatrolMovement({
      profile: bridgeProfile,
      rawEvaluatedAtMs,
      civil,
      waypointsRegistry: CORRIDOR_WAYPOINTS,
      pauseIntervals,
    });

    return Object.freeze({
      npcId: profile.npcId,
      canonicalName: profile.canonicalName,
      actorArchetype,
      isWitness,
      witnessClassification,
      witnessId,
      movementState: patrolRes.movementState,
      currentWaypointId: patrolRes.currentWaypointId,
      destinationWaypointId: patrolRes.destinationWaypointId,
      activeRouteId: patrolRes.activeRouteId,
      position: Object.freeze(patrolRes.position),
      forwardHeading: Object.freeze(patrolRes.forwardHeading),
      progression: patrolRes.progression,
      movementStartTime: patrolRes.movementStartTime,
      expectedDurationSeconds: patrolRes.expectedDurationSeconds,
      associatedPlaceId: patrolRes.associatedPlaceId,
      activityState,
      activityTitle,
      evaluatedAt: evaluatedAtIso,
      timeZone: profile.timeZone,
      localCivilTime: civil.isoCivilString,
      isDemonstrationMovement: true,
      demonstrationNotice: SIMULATED_MOVEMENT_NOTICE,
      behaviorProfileType: patrolRes.behaviorProfileType,
      autonomousBehaviorState: patrolRes.autonomousBehaviorState,
      controlMode: patrolRes.controlMode,
      currentSpeedMps: patrolRes.movementState === 'moving' ? bridgeProfile.preferredSpeedMps : 0,
      patrolDirection: patrolRes.patrolDirection,
      patrolCycleIndex: patrolRes.patrolCycleIndex,
      isSuspendedByPlayerInteraction: patrolRes.isSuspendedByPlayerInteraction,
      suspendedByPlayerId: patrolRes.suspendedByPlayerId,
      passingEncounterPartnerNpcId: patrolRes.passingEncounterPartnerNpcId,
      passingEncounterSessionId: patrolRes.passingEncounterSessionId,
      passingEncounterType: patrolRes.passingEncounterType,
      businessDomainId: patrolRes.businessDomainId,
      withinAuthorizedDomain: patrolRes.withinAuthorizedDomain,
    });
  }

  // Subsystem B: Business Attendant Bounded Local Autonomy
  const businessProfile = BUSINESS_LOCAL_AUTONOMY_PROFILES[profile.npcId];
  if (businessProfile) {
    const bizRes = evaluateBusinessLocalAutonomyMovement({
      profile: businessProfile,
      rawEvaluatedAtMs,
      civil,
      activitySnapshot,
      waypointsRegistry: CORRIDOR_WAYPOINTS,
      pauseIntervals,
      player: options?.player,
    });

    return Object.freeze({
      npcId: profile.npcId,
      canonicalName: profile.canonicalName,
      actorArchetype,
      isWitness,
      witnessClassification,
      witnessId,
      movementState: bizRes.movementState,
      currentWaypointId: bizRes.currentWaypointId,
      destinationWaypointId: bizRes.destinationWaypointId,
      activeRouteId: bizRes.activeRouteId,
      position: Object.freeze(bizRes.position),
      forwardHeading: Object.freeze(bizRes.forwardHeading),
      progression: bizRes.progression,
      movementStartTime: bizRes.movementStartTime,
      expectedDurationSeconds: bizRes.expectedDurationSeconds,
      associatedPlaceId: bizRes.associatedPlaceId,
      activityState,
      activityTitle,
      evaluatedAt: evaluatedAtIso,
      timeZone: profile.timeZone,
      localCivilTime: civil.isoCivilString,
      isDemonstrationMovement: true,
      demonstrationNotice: SIMULATED_MOVEMENT_NOTICE,
      behaviorProfileType: bizRes.behaviorProfileType,
      autonomousBehaviorState: bizRes.autonomousBehaviorState,
      controlMode: bizRes.controlMode,
      currentSpeedMps:
        bizRes.movementState === 'moving'
          ? (bizRes.actualSpeedMps ?? businessProfile.preferredSpeedMps)
          : 0,
      patrolDirection: bizRes.patrolDirection,
      patrolCycleIndex: bizRes.patrolCycleIndex,
      isSuspendedByPlayerInteraction: bizRes.isSuspendedByPlayerInteraction,
      suspendedByPlayerId: bizRes.suspendedByPlayerId,
      passingEncounterPartnerNpcId: bizRes.passingEncounterPartnerNpcId,
      passingEncounterSessionId: bizRes.passingEncounterSessionId,
      passingEncounterType: bizRes.passingEncounterType,
      businessDomainId: bizRes.businessDomainId,
      withinAuthorizedDomain: bizRes.withinAuthorizedDomain,
      ...(bizRes.activeSegmentEndPosition
        ? { activeSegmentEndPosition: Object.freeze(bizRes.activeSegmentEndPosition) }
        : {}),
    });
  }

  // Excluded Fixed Kiosk NPCs (Elena Ramos & Lic. Patricia Morales) — 100% preserved
  const fixedState: NpcAutonomousBehaviorState =
    activityState === 'off_duty' || activityState === 'unavailable' ? 'off_duty' : 'fixed_station';

  return Object.freeze({
    npcId: profile.npcId,
    canonicalName: profile.canonicalName,
    actorArchetype,
    isWitness,
    witnessClassification,
    witnessId,
    movementState: 'stationary',
    currentWaypointId: profile.primaryWaypointId,
    destinationWaypointId: null,
    activeRouteId: null,
    position: Object.freeze(primaryWp.position),
    forwardHeading: Object.freeze({ x: 0, z: 1 }),
    progression: 0,
    movementStartTime: null,
    expectedDurationSeconds: 0,
    associatedPlaceId: primaryWp.associatedPlaceId,
    activityState,
    activityTitle,
    evaluatedAt: evaluatedAtIso,
    timeZone: profile.timeZone,
    localCivilTime: civil.isoCivilString,
    isDemonstrationMovement: true,
    demonstrationNotice: SIMULATED_MOVEMENT_NOTICE,
    behaviorProfileType: 'fixed_anchor_scheduled',
    autonomousBehaviorState: fixedState,
    controlMode: 'fixed_station_anchor',
    currentSpeedMps: 0,
    patrolDirection: 'none',
    patrolCycleIndex: 0,
    isSuspendedByPlayerInteraction: false,
    suspendedByPlayerId: null,
    passingEncounterPartnerNpcId: null,
    passingEncounterSessionId: null,
    passingEncounterType: null,
    businessDomainId: null,
    withinAuthorizedDomain: true,
  });
}

function applyRosterPassingEncounters(
  baseNpcs: Record<string, NpcMovementSnapshot>,
  roster: Readonly<Record<string, NpcMovementProfile>>,
  evaluatedAtMs: number,
  options?: EvaluateAutonomousMovementOptions
): void {
  const patrolIds = Object.keys(baseNpcs)
    .filter((id) => baseNpcs[id]?.behaviorProfileType === 'bridge_patrol')
    .sort();
  if (patrolIds.length < 2) return;

  interface CandidateEncounter {
    readonly npcIdA: string;
    readonly npcIdB: string;
    readonly distanceMeters: number;
    readonly sessionId: string;
    readonly encounterType: PassingEncounterType;
  }

  const candidates: CandidateEncounter[] = [];
  for (let i = 0; i < patrolIds.length; i++) {
    for (let j = i + 1; j < patrolIds.length; j++) {
      const idA = patrolIds[i]!;
      const idB = patrolIds[j]!;
      const snapA = baseNpcs[idA]!;
      const snapB = baseNpcs[idB]!;
      const profA = roster[idA];
      const profB = roster[idB];

      const wasCloseAtFn =
        profA && profB
          ? (testTimeMs: number) => {
              const pastA = evaluateSingleNpcMovementBase(profA, testTimeMs, {
                ...options,
                skipPassingEncounters: true,
              });
              const pastB = evaluateSingleNpcMovementBase(profB, testTimeMs, {
                ...options,
                skipPassingEncounters: true,
              });
              const d = Math.hypot(
                pastB.position.x - pastA.position.x,
                pastB.position.z - pastA.position.z
              );
              return d <= 3.2;
            }
          : undefined;

      const evalPair = evaluatePassingEncounterPairStatus({
        npcA: snapA,
        npcB: snapB,
        evaluatedAtMs,
        wasCloseAtFn,
      });

      if (evalPair.activeEncounter) {
        candidates.push({
          npcIdA: idA,
          npcIdB: idB,
          distanceMeters: evalPair.activeEncounter.distanceMeters,
          sessionId: evalPair.activeEncounter.sessionId,
          encounterType: evalPair.activeEncounter.encounterType,
        });
      }
    }
  }

  candidates.sort((a, b) => {
    if (Math.abs(a.distanceMeters - b.distanceMeters) > 1e-6) {
      return a.distanceMeters - b.distanceMeters;
    }
    if (a.npcIdA !== b.npcIdA) return a.npcIdA < b.npcIdA ? -1 : 1;
    return a.npcIdB < b.npcIdB ? -1 : 1;
  });

  const paired = new Set<string>();
  for (const c of candidates) {
    if (paired.has(c.npcIdA) || paired.has(c.npcIdB)) continue;
    paired.add(c.npcIdA);
    paired.add(c.npcIdB);

    const snapA = baseNpcs[c.npcIdA]!;
    const snapB = baseNpcs[c.npcIdB]!;
    const dx = snapB.position.x - snapA.position.x;
    const dz = snapB.position.z - snapA.position.z;
    const len = Math.hypot(dx, dz) || 1;
    const headAToB = Object.freeze({
      x: Math.round((dx / len) * 1000) / 1000,
      z: Math.round((dz / len) * 1000) / 1000,
    });
    const headBToA = Object.freeze({
      x: Math.round((-dx / len) * 1000) / 1000,
      z: Math.round((-dz / len) * 1000) / 1000,
    });

    baseNpcs[c.npcIdA] = Object.freeze({
      ...snapA,
      movementState: 'stationary',
      currentSpeedMps: 0,
      forwardHeading: headAToB,
      autonomousBehaviorState: 'passing_encounter',
      passingEncounterPartnerNpcId: c.npcIdB,
      passingEncounterSessionId: c.sessionId,
      passingEncounterType: c.encounterType,
    });
    baseNpcs[c.npcIdB] = Object.freeze({
      ...snapB,
      movementState: 'stationary',
      currentSpeedMps: 0,
      forwardHeading: headBToA,
      autonomousBehaviorState: 'passing_encounter',
      passingEncounterPartnerNpcId: c.npcIdA,
      passingEncounterSessionId: c.sessionId,
      passingEncounterType: c.encounterType,
    });
  }
}

/**
 * Evaluates the authoritative movement state of an NPC at any given instant.
 * Closed-form pure function of the World Clock timestamp and configured route data.
 */
export function evaluateNpcMovement(
  profile: NpcMovementProfile,
  evaluatedAt: Date | string | number,
  options?: EvaluateAutonomousMovementOptions
): NpcMovementSnapshot {
  const baseSnap = evaluateSingleNpcMovementBase(profile, evaluatedAt, options);
  if (
    options?.skipPassingEncounters ||
    baseSnap.behaviorProfileType !== 'bridge_patrol' ||
    !NPC_MOVEMENT_PROFILES[profile.npcId]
  ) {
    return baseSnap;
  }

  // Evaluate bridge patrol peers so single-NPC and roster movement queries agree on passing encounters
  const evaluatedAtMs =
    typeof evaluatedAt === 'number'
      ? evaluatedAt
      : evaluatedAt instanceof Date
      ? evaluatedAt.getTime()
      : Date.parse(evaluatedAt);
  const peerSnaps: Record<string, NpcMovementSnapshot> = { [profile.npcId]: baseSnap };
  for (const peerId of Object.keys(BRIDGE_PATROL_PROFILES)) {
    if (peerId === profile.npcId) continue;
    const peerProf = NPC_MOVEMENT_PROFILES[peerId];
    if (peerProf) {
      peerSnaps[peerId] = evaluateSingleNpcMovementBase(peerProf, evaluatedAt, {
        ...options,
        skipPassingEncounters: true,
      });
    }
  }
  applyRosterPassingEncounters(peerSnaps, NPC_MOVEMENT_PROFILES, evaluatedAtMs, options);
  applyRosterPersonalSpaceSeparation(peerSnaps);
  return peerSnaps[profile.npcId]!;
}

/**
 * Deterministically converts a scheduled local civil departure time in the given timezone
 * into the corresponding authoritative UTC ISO instant.
 */
function buildIsoDepartureTime(
  civil: CivilTime,
  hour: number,
  minute: number,
  timeZone: string
): string {
  const pad2 = (n: number) => n.toString().padStart(2, '0');
  const pad4 = (n: number) => n.toString().padStart(4, '0');
  const localIso = `${pad4(civil.year)}-${pad2(civil.month)}-${pad2(civil.day)}T${pad2(hour)}:${pad2(minute)}:00`;
  const result = parseLocalCivilTimeToUtc(localIso, timeZone);
  if (result.success && result.utcInstant) {
    return result.utcInstant.toISOString();
  }
  // Fallback for DST gap: format local civil time with the day's standard offset
  const sign = civil.utcOffsetMinutes >= 0 ? '+' : '-';
  const absOffset = Math.abs(civil.utcOffsetMinutes);
  const offsetH = pad2(Math.floor(absOffset / 60));
  const offsetM = pad2(absOffset % 60);
  return `${localIso}${sign}${offsetH}:${offsetM}`;
}

/**
 * Evaluates movement state for all 7 corridor NPCs simultaneously in a single coherent snapshot.
 */
export function evaluateRosterMovement(
  roster: Readonly<Record<string, NpcMovementProfile>>,
  evaluatedAt: Date | string | number,
  options?: EvaluateAutonomousMovementOptions
): NpcRosterMovementSnapshot {
  const instantIso = typeof evaluatedAt === 'string' ? evaluatedAt : new Date(evaluatedAt).toISOString();
  const evaluatedAtMs = Date.parse(instantIso);
  const npcs: Record<string, NpcMovementSnapshot> = {};

  for (const [id, profile] of Object.entries(roster)) {
    npcs[id] = evaluateSingleNpcMovementBase(profile, instantIso, {
      ...options,
      skipPassingEncounters: true,
    });
  }

  if (!options?.skipPassingEncounters) {
    applyRosterPassingEncounters(npcs, roster, evaluatedAtMs, options);
  }
  applyRosterPersonalSpaceSeparation(npcs);

  let stationaryCount = 0;
  let movingCount = 0;
  let arrivedCount = 0;
  let waitingCount = 0;

  for (const snap of Object.values(npcs)) {
    if (snap.movementState === 'stationary') stationaryCount++;
    else if (snap.movementState === 'moving') movingCount++;
    else if (snap.movementState === 'arrived') arrivedCount++;
    else if (snap.movementState === 'waiting') waitingCount++;
  }

  const sampleTz = Object.values(roster)[0]?.timeZone ?? TZ_JUAREZ;
  const civil = deriveCivilTime(instantIso, sampleTz);

  return Object.freeze({
    evaluatedAt: instantIso,
    timeZone: sampleTz,
    localCivilTime: civil.isoCivilString,
    npcs: Object.freeze(npcs),
    npcsList: Object.freeze(Object.values(npcs)),
    summary: Object.freeze({
      stationaryCount,
      movingCount,
      arrivedCount,
      waitingCount,
      totalCount: Object.keys(roster).length,
    }),
    isDemonstrationMovement: true,
    demonstrationNotice: SIMULATED_MOVEMENT_NOTICE,
  });
}
