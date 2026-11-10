/**
 * Context-Aware NPC Interactions & Spatial Behavior Domain Model.
 *
 * Core Architectural Invariants:
 * 1. World Core remains authoritative for:
 *    - Canonical world time (World Clock).
 *    - NPC activity state, schedule, and routine.
 *    - Spatial waypoint, route progression, and movement state.
 *    - Interaction context, availability classification, and decision policy.
 * 2. Unity remains a presentation and input client.
 *    Unity submits player interaction intents, but NEVER independently determines
 *    NPC activity, movement, position, or interaction eligibility.
 * 3. Client input validation:
 *    Backend does not accept client assertions of world time, activity state, or position.
 * 4. Deterministic decision policy:
 *    Identical authoritative inputs produce identical interaction decisions and context.
 * 5. Preservation:
 *    Preserves all 7 canonical NPCs, schedules, routes, and existing dialogue endpoints.
 */

import { deriveCivilTime, CivilTime } from './civil-time.js';
import { BilingualText } from './synthetic-witness.js';
import {
  NPC_SCHEDULE_ROSTER,
  NpcActivityState,
  NpcActivitySnapshot,
  evaluateNpcActivity,
} from './npc-activity.js';
import {
  CORRIDOR_WAYPOINTS,
  CORRIDOR_ROUTES,
  NPC_MOVEMENT_PROFILES,
  NpcMovementState,
  NpcMovementSnapshot,
  evaluateNpcMovement,
} from './npc-waypoint-movement.js';

export type NpcInteractionAvailability =
  | 'available_normal'
  | 'available_in_transit'
  | 'available_contextual'
  | 'busy'
  | 'off_duty'
  | 'unavailable';

export type NpcInteractionMode =
  | 'normal_service'
  | 'in_transit'
  | 'break_or_dwell'
  | 'busy_priority'
  | 'off_duty_courtesy'
  | 'closed';

export interface NpcInteractionContext {
  readonly npcId: string;
  readonly canonicalName: string;
  readonly role: BilingualText;
  readonly activityState: NpcActivityState;
  readonly activityTitle: BilingualText;
  readonly activityDescription: BilingualText;
  readonly interactionAvailability: NpcInteractionAvailability;
  readonly interactionMode: NpcInteractionMode;
  readonly canInitiateConversation: boolean;
  readonly isAvailableForNormalService: boolean;
  readonly isMoving: boolean;
  readonly movementState: NpcMovementState;
  readonly currentWaypointId: string;
  readonly currentWaypointLabel: BilingualText;
  readonly associatedPlaceId: string;
  readonly destinationWaypointId: string | null;
  readonly destinationWaypointLabel: BilingualText | null;
  readonly activeRouteId: string | null;
  readonly progression: number;
  readonly decisionReason: BilingualText;
  readonly dialogueGuidance: BilingualText;
  readonly contextualGreeting: BilingualText;
  readonly evaluatedAt: string;
  readonly timeZone: string;
  readonly localCivilTime: string;
  readonly isDemonstrationContext: true;
  readonly demonstrationNotice: BilingualText;
}

export interface NpcRosterInteractionContext {
  readonly evaluatedAt: string;
  readonly timeZone: string;
  readonly localCivilTime: string;
  readonly npcs: Readonly<Record<string, NpcInteractionContext>>;
  readonly npcsList: readonly NpcInteractionContext[];
  readonly summary: {
    readonly availableNormalCount: number;
    readonly inTransitCount: number;
    readonly contextualCount: number;
    readonly busyCount: number;
    readonly offDutyCount: number;
    readonly unavailableCount: number;
    readonly totalCount: number;
  };
  readonly isDemonstrationContext: true;
  readonly demonstrationNotice: BilingualText;
}

export const SIMULATED_CONTEXT_NOTICE: BilingualText = Object.freeze({
  en: 'Simulated demonstration interaction context for AI Hackathon Edition. Grounded in authoritative World Core simulation.',
  es: 'Contexto de interacción de demostración simulado para AI Hackathon Edition. Basado en la simulación autoritativa del World Core.',
});

export const CANONICAL_NPC_IDS = Object.freeze([
  'elena-border-concierge',
  'carlos-farmacia-attendant',
  'valeria-optica-attendant',
  'mateo-student',
  'sofia-cross-border-retail',
  'hector-artisan',
  'patricia-citizen-rep',
] as const);

export type CanonicalNpcId = typeof CANONICAL_NPC_IDS[number];

/**
 * Evaluates the authoritative interaction context for a canonical NPC.
 * Pure deterministic projection combining activity schedule, waypoint movement, and interaction rules.
 */
export function evaluateNpcInteractionContext(
  npcId: string,
  evaluatedAt: Date | string | number
): NpcInteractionContext | null {
  const scheduleProfile = NPC_SCHEDULE_ROSTER[npcId];
  const movementProfile = NPC_MOVEMENT_PROFILES[npcId];

  if (!scheduleProfile || !movementProfile) {
    return null;
  }

  const instantIso = typeof evaluatedAt === 'string' ? evaluatedAt : new Date(evaluatedAt).toISOString();
  const civil: CivilTime = deriveCivilTime(instantIso, scheduleProfile.timeZone);

  // 1. Authoritative Activity Evaluation
  const activitySnap: NpcActivitySnapshot = evaluateNpcActivity(scheduleProfile, instantIso);

  // 2. Authoritative Waypoint Movement Evaluation
  const movementSnap: NpcMovementSnapshot = evaluateNpcMovement(movementProfile, instantIso);

  // 3. Resolve Waypoint Metadata
  const currentWp = CORRIDOR_WAYPOINTS[movementSnap.currentWaypointId] ?? {
    id: movementSnap.currentWaypointId,
    label: { en: 'Corridor Location', es: 'Ubicación en el Corredor' },
    associatedPlaceId: movementSnap.associatedPlaceId,
  };

  const destWp = movementSnap.destinationWaypointId
    ? CORRIDOR_WAYPOINTS[movementSnap.destinationWaypointId] ?? null
    : null;

  // 4. Deterministic Interaction Policy with Strict Precedence
  // Precedence 1: Off-Duty or Outside Operating Hours
  // Precedence 2: Active In-Transit Movement along Route
  // Precedence 3: Dwell / Break at Secondary Destination
  // Precedence 4: Busy at Primary Station
  // Precedence 5: Available at Primary Station (Normal Service)

  let interactionAvailability: NpcInteractionAvailability = 'available_normal';
  let interactionMode: NpcInteractionMode = 'normal_service';
  let canInitiateConversation = true;
  let isAvailableForNormalService = true;
  let decisionReason: BilingualText;
  let dialogueGuidance: BilingualText;
  let contextualGreeting: BilingualText;

  if (activitySnap.state === 'off_duty') {
    interactionAvailability = 'off_duty';
    interactionMode = 'off_duty_courtesy';
    canInitiateConversation = true;
    isAvailableForNormalService = false;
    decisionReason = Object.freeze({
      en: `${scheduleProfile.canonicalName} is currently off duty according to the corridor daily routine schedule.`,
      es: `${scheduleProfile.canonicalName} se encuentra fuera de turno según el horario diario del corredor.`,
    });
    dialogueGuidance = Object.freeze({
      en: 'NPC is polite but off duty. Acknowledges player, explains they are off shift, and provides scheduled operating hours if asked.',
      es: 'El personaje es cortés pero está fuera de turno. Reconoce al jugador, explica que terminó su turno y ofrece horarios si se solicita.',
    });
    contextualGreeting = getOffDutyGreeting(npcId);

  } else if (activitySnap.state === 'unavailable') {
    interactionAvailability = 'unavailable';
    interactionMode = 'closed';
    canInitiateConversation = true;
    isAvailableForNormalService = false;
    decisionReason = Object.freeze({
      en: `The facility/business is closed today (${civil.dayName}). ${scheduleProfile.canonicalName} is not on scheduled duty.`,
      es: `El establecimiento está cerrado hoy (${civil.dayName}). ${scheduleProfile.canonicalName} no tiene turno programado.`,
    });
    dialogueGuidance = Object.freeze({
      en: 'NPC indicates the location is closed today and refers the player to weekly operating days.',
      es: 'El personaje indica que la sucursal está cerrada hoy y orienta al jugador hacia los días de apertura semanal.',
    });
    contextualGreeting = getClosedGreeting(npcId);

  } else if (movementSnap.movementState === 'moving') {
    interactionAvailability = 'available_in_transit';
    interactionMode = 'in_transit';
    canInitiateConversation = true;
    isAvailableForNormalService = false;
    const destNameEn = destWp?.label.en ?? 'destination';
    const destNameEs = destWp?.label.es ?? 'su destino';

    decisionReason = Object.freeze({
      en: `${scheduleProfile.canonicalName} is currently walking along the corridor toward ${destNameEn}. Desk/counter service is paused during transit.`,
      es: `${scheduleProfile.canonicalName} está caminando por el corredor hacia ${destNameEs}. La atención en mostrador está pausada durante el traslado.`,
    });
    dialogueGuidance = Object.freeze({
      en: 'NPC is actively walking. Player may converse alongside them. Dialogue acknowledges their active transit and destination.',
      es: 'El personaje va caminando. El jugador puede conversar a su paso. El diálogo reconoce su traslado y hacia dónde se dirige.',
    });
    contextualGreeting = getInTransitGreeting(npcId, destWp?.label);

  } else if (movementSnap.movementState === 'arrived' || movementSnap.movementState === 'waiting') {
    interactionAvailability = 'available_contextual';
    interactionMode = 'break_or_dwell';
    canInitiateConversation = true;
    isAvailableForNormalService = false;

    decisionReason = Object.freeze({
      en: `${scheduleProfile.canonicalName} is at ${currentWp.label.en} (${activitySnap.activityTitle.en}). Receptive to contextual conversation.`,
      es: `${scheduleProfile.canonicalName} se encuentra en ${currentWp.label.es} (${activitySnap.activityTitle.es}). Receptivo a conversación contextual.`,
    });
    dialogueGuidance = Object.freeze({
      en: 'NPC is at a break or secondary workstation. Dialogue reflects their current activity rather than standard counter service.',
      es: 'El personaje está en descanso o estación secundaria. El diálogo refleja su actividad actual en lugar del servicio regular de mostrador.',
    });
    contextualGreeting = getContextualDwellGreeting(npcId, currentWp.label, activitySnap.activityTitle);

  } else if (activitySnap.state === 'busy') {
    interactionAvailability = 'busy';
    interactionMode = 'busy_priority';
    canInitiateConversation = true;
    isAvailableForNormalService = false;

    decisionReason = Object.freeze({
      en: `${scheduleProfile.canonicalName} is on duty at ${currentWp.label.en} but currently engaged in: ${activitySnap.activityTitle.en}.`,
      es: `${scheduleProfile.canonicalName} está en turno en ${currentWp.label.es} pero realizando: ${activitySnap.activityTitle.es}.`,
    });
    dialogueGuidance = Object.freeze({
      en: 'NPC is focused on a priority task (e.g. inventory audit, calibration). Receptive to brief queries with a focused, busy demeanor.',
      es: 'El personaje está enfocado en una tarea prioritaria (ej. inventario, calibración). Receptivo a preguntas breves con tono ocupado.',
    });
    contextualGreeting = getBusyGreeting(npcId, activitySnap.activityTitle);

  } else {
    // Normal available service
    interactionAvailability = 'available_normal';
    interactionMode = 'normal_service';
    canInitiateConversation = true;
    isAvailableForNormalService = true;

    decisionReason = Object.freeze({
      en: `${scheduleProfile.canonicalName} is on active duty at ${currentWp.label.en} and fully available for normal service.`,
      es: `${scheduleProfile.canonicalName} está en turno activo en ${currentWp.label.es} y totalmente disponible para atención normal.`,
    });
    dialogueGuidance = Object.freeze({
      en: 'NPC provides full standard assistance according to their professional role in the corridor.',
      es: 'El personaje brinda atención completa de acuerdo con su función profesional en el corredor.',
    });
    contextualGreeting = getStandardGreeting(npcId);
  }

  return Object.freeze({
    npcId: scheduleProfile.npcId,
    canonicalName: scheduleProfile.canonicalName,
    role: scheduleProfile.role,
    activityState: activitySnap.state,
    activityTitle: activitySnap.activityTitle,
    activityDescription: activitySnap.activityDescription,
    interactionAvailability,
    interactionMode,
    canInitiateConversation,
    isAvailableForNormalService,
    isMoving: movementSnap.movementState === 'moving',
    movementState: movementSnap.movementState,
    currentWaypointId: movementSnap.currentWaypointId,
    currentWaypointLabel: currentWp.label,
    associatedPlaceId: movementSnap.associatedPlaceId,
    destinationWaypointId: movementSnap.destinationWaypointId,
    destinationWaypointLabel: destWp ? destWp.label : null,
    activeRouteId: movementSnap.activeRouteId,
    progression: movementSnap.progression,
    decisionReason,
    dialogueGuidance,
    contextualGreeting,
    evaluatedAt: instantIso,
    timeZone: scheduleProfile.timeZone,
    localCivilTime: civil.isoCivilString,
    isDemonstrationContext: true,
    demonstrationNotice: SIMULATED_CONTEXT_NOTICE,
  });
}

/**
 * Evaluates the interaction context across all 7 canonical NPCs simultaneously.
 */
export function evaluateRosterInteractionContext(
  evaluatedAt: Date | string | number
): NpcRosterInteractionContext {
  const instantIso = typeof evaluatedAt === 'string' ? evaluatedAt : new Date(evaluatedAt).toISOString();
  const npcs: Record<string, NpcInteractionContext> = {};

  let availableNormalCount = 0;
  let inTransitCount = 0;
  let contextualCount = 0;
  let busyCount = 0;
  let offDutyCount = 0;
  let unavailableCount = 0;

  for (const id of CANONICAL_NPC_IDS) {
    const ctx = evaluateNpcInteractionContext(id, instantIso);
    if (ctx) {
      npcs[id] = ctx;
      switch (ctx.interactionAvailability) {
        case 'available_normal':
          availableNormalCount++;
          break;
        case 'available_in_transit':
          inTransitCount++;
          break;
        case 'available_contextual':
          contextualCount++;
          break;
        case 'busy':
          busyCount++;
          break;
        case 'off_duty':
          offDutyCount++;
          break;
        case 'unavailable':
          unavailableCount++;
          break;
      }
    }
  }

  const sampleTz = 'America/Ciudad_Juarez';
  const civil = deriveCivilTime(instantIso, sampleTz);

  return Object.freeze({
    evaluatedAt: instantIso,
    timeZone: sampleTz,
    localCivilTime: civil.isoCivilString,
    npcs: Object.freeze(npcs),
    npcsList: Object.freeze(Object.values(npcs)),
    summary: Object.freeze({
      availableNormalCount,
      inTransitCount,
      contextualCount,
      busyCount,
      offDutyCount,
      unavailableCount,
      totalCount: Object.keys(npcs).length,
    }),
    isDemonstrationContext: true,
    demonstrationNotice: SIMULATED_CONTEXT_NOTICE,
  });
}

// ---------------------------------------------------------------------------
// Contextual Greeting Helpers (Concise, Deterministic Bilingual Text)
// ---------------------------------------------------------------------------

function getStandardGreeting(npcId: string): BilingualText {
  switch (npcId) {
    case 'elena-border-concierge':
      return Object.freeze({
        en: 'Welcome to Puente Paso del Norte. How can I help orient your pedestrian crossing today?',
        es: 'Bienvenido al Puente Paso del Norte. ¿En qué puedo orientarle sobre su cruce peatonal el día de hoy?',
      });
    case 'carlos-farmacia-attendant':
      return Object.freeze({
        en: 'Hello, welcome to Farmacias Benavides. How can I assist you with our schedule and services?',
        es: 'Hola, bienvenido a Farmacias Benavides. ¿En qué le puedo apoyar con nuestros horarios y servicios?',
      });
    case 'valeria-optica-attendant':
      return Object.freeze({
        en: 'Good day, welcome to Óptica Fashion. Are you looking for consultation hours or vision services?',
        es: 'Buen día, bienvenido a Óptica Fashion. ¿Busca nuestros horarios de consulta o servicios ópticos?',
      });
    case 'mateo-student':
      return Object.freeze({
        en: 'Hey there. I am waiting in line to cross over to UTEP classes. What is up?',
        es: 'Hola. Estoy haciendo fila para cruzar a mis clases en UTEP. ¿Qué tal?',
      });
    case 'sofia-cross-border-retail':
      return Object.freeze({
        en: 'Hi! Just getting ready for my shift across the border in El Paso. How is your day going?',
        es: '¡Hola! Aquí preparándome para mi turno cruzando al comercio en El Paso. ¿Cómo va tu día?',
      });
    case 'hector-artisan':
      return Object.freeze({
        en: 'Greetings, friend. Take a look at these traditional woodcrafts from Chihuahua. What can I tell you about them?',
        es: 'Saludos, amigo. Pase a ver estas artesanías tradicionales de madera de Chihuahua. ¿En qué le puedo servir?',
      });
    case 'patricia-citizen-rep':
      return Object.freeze({
        en: 'Good day. Municipal Citizen Attention Desk. Are you here to report an urban infrastructure issue?',
        es: 'Buen día. Módulo Municipal de Atención Ciudadana. ¿Desea reportar alguna incidencia de infraestructura urbana?',
      });
    default:
      return Object.freeze({
        en: 'Hello, how can I assist you in the corridor?',
        es: 'Hola, ¿en qué le puedo apoyar en el corredor?',
      });
  }
}

function getInTransitGreeting(npcId: string, destLabel?: BilingualText | null): BilingualText {
  const destEn = destLabel?.en ?? 'my destination';
  const destEs = destLabel?.es ?? 'mi destino';
  switch (npcId) {
    case 'carlos-farmacia-attendant':
      return Object.freeze({
        en: `Hello! I am currently walking to ${destEn} for my lunch break, but walk with me—what can I quickly answer?`,
        es: `¡Hola! Voy caminando hacia ${destEs} para mi descanso de almuerzo, pero acompáñame, ¿en qué te puedo orientar rápidamente?`,
      });
    case 'elena-border-concierge':
      return Object.freeze({
        en: `Good day. I am walking to ${destEn} for shift handover, but I can answer brief crossing questions as we walk.`,
        es: `Buen día. Voy caminando hacia ${destEs} para el relevo de turno, pero con gusto le respondo dudas breves del cruce sobre la marcha.`,
      });
    case 'valeria-optica-attendant':
      return Object.freeze({
        en: `Hi! I am heading over to ${destEn} for lens sanitization, but feel free to ask a quick question while I walk.`,
        es: `¡Hola! Me dirijo a ${destEs} para desinfección de armazones, pero con gusto dime tu consulta breve mientras camino.`,
      });
    case 'mateo-student':
      return Object.freeze({
        en: `Hey! The bridge line is advancing toward ${destEn}. Let's chat as we move forward.`,
        es: `¡Hola! La fila del puente va avanzando hacia ${destEs}. Platiquemos mientras avanzamos.`,
      });
    case 'sofia-cross-border-retail':
      return Object.freeze({
        en: `Hi there! Stepping over to ${destEn} to check the canopy board. What's on your mind?`,
        es: `¡Hola! Me estoy moviendo hacia ${destEs} para ver el techado. ¿Qué tal?`,
      });
    case 'hector-artisan':
      return Object.freeze({
        en: `Salud, amigo! Walking to ${destEn} to pick up my carving tools. Tell me as we walk.`,
        es: `¡Salud, amigo! Voy caminando a ${destEs} para recoger mis gubias de tallar. Dígame mientras caminamos.`,
      });
    case 'patricia-citizen-rep':
      return Object.freeze({
        en: `Hello. Conducting an on-foot corridor inspection toward ${destEn}. How can I assist you on the move?`,
        es: `Hola. Realizando una inspección peatonal del corredor hacia ${destEs}. ¿En qué le puedo orientar sobre la marcha?`,
      });
    default:
      return Object.freeze({
        en: `Hello, I am walking toward ${destEn}. What can I help you with?`,
        es: `Hola, voy caminando hacia ${destEs}. ¿En qué le puedo apoyar?`,
      });
  }
}

function getContextualDwellGreeting(npcId: string, locationLabel: BilingualText, activityTitle: BilingualText): BilingualText {
  switch (npcId) {
    case 'carlos-farmacia-attendant':
      return Object.freeze({
        en: `Hello! I am taking my scheduled lunch break here at the ${locationLabel.en}. The pharmacy counter resumes shortly.`,
        es: `¡Hola! Estoy tomando mi descanso de comida aquí en las ${locationLabel.es}. La atención en mostrador se reanuda en unos momentos.`,
      });
    case 'elena-border-concierge':
      return Object.freeze({
        en: `Good day. I am currently reconciling bridge logs here at the ${locationLabel.en}. How can I assist you?`,
        es: `Buen día. Me encuentro revisando bitácoras del puente aquí en la ${locationLabel.es}. ¿En qué le puedo orientar?`,
      });
    case 'valeria-optica-attendant':
      return Object.freeze({
        en: `Hello. I am at the ${locationLabel.en} completing lens calibration. Feel free to ask about our hours or services.`,
        es: `Hola. Me encuentro en el ${locationLabel.es} realizando calibración de lentes. Con gusto le informo sobre horarios o servicios.`,
      });
    case 'hector-artisan':
      return Object.freeze({
        en: `Welcome! I am sitting at the ${locationLabel.en} detailing these woodcarvings. Feel free to watch or ask about the craft.`,
        es: `¡Bienvenido! Estoy sentado en la ${locationLabel.es} detallando estas figuras de madera. Con gusto le platico sobre el tallado.`,
      });
    case 'patricia-citizen-rep':
      return Object.freeze({
        en: `Good day. I am stationed at ${locationLabel.en} inspecting public works conditions. Do you have a report to share?`,
        es: `Buen día. Me encuentro en el ${locationLabel.es} verificando las condiciones de la obra pública. ¿Desea ingresar algún reporte?`,
      });
    default:
      return Object.freeze({
        en: `Hello from ${locationLabel.en}. I am currently ${activityTitle.en}. How can I help?`,
        es: `Hola desde ${locationLabel.es}. Estoy actualmente en ${activityTitle.es}. ¿En qué le puedo apoyar?`,
      });
  }
}

function getBusyGreeting(npcId: string, activityTitle: BilingualText): BilingualText {
  return Object.freeze({
    en: `Hello. I am in the middle of ${activityTitle.en} right now, but please ask your question and I will assist you.`,
    es: `Hola. Me encuentro en medio de ${activityTitle.es} en este momento, pero con gusto dígame su consulta y le oriento.`,
  });
}

function getOffDutyGreeting(npcId: string): BilingualText {
  switch (npcId) {
    case 'carlos-farmacia-attendant':
      return Object.freeze({
        en: 'Good evening. Farmacias Benavides is closed or my shift has concluded. Our regular counter hours resume tomorrow at 07:00.',
        es: 'Buenas noches. Farmacias Benavides está cerrada o mi turno concluyó. Nuestro mostrador reanuda servicio mañana a las 07:00.',
      });
    case 'valeria-optica-attendant':
      return Object.freeze({
        en: 'Good evening. Óptica Fashion has finished consultations for today. We reopen Monday through Saturday at 09:00.',
        es: 'Buenas noches. Óptica Fashion concluyó consultas por hoy. Reabrimos de lunes a sábado a las 09:00.',
      });
    case 'elena-border-concierge':
      return Object.freeze({
        en: 'Good evening. My border concierge shift has ended for the day. Bridge information kiosks resume early tomorrow morning.',
        es: 'Buenas noches. Mi turno de orientación peatonal ha concluido por hoy. Los quioscos reanudan servicio temprano mañana.',
      });
    default:
      return Object.freeze({
        en: 'Good evening. My scheduled shift has concluded for today. See you during our normal hours tomorrow.',
        es: 'Buenas noches. Mi turno programado ha terminado por hoy. Le esperamos durante nuestro horario regular mañana.',
      });
  }
}

function getClosedGreeting(npcId: string): BilingualText {
  return Object.freeze({
    en: 'Good day. We are closed today according to the weekly schedule. Please visit during our normal operating days.',
    es: 'Buen día. Nos encontramos cerrados hoy según el horario semanal. Le invitamos a visitarnos en nuestros días de apertura.',
  });
}

