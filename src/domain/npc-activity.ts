/**
 * Deterministic NPC Activity, Daily Routines & Persistent World Presence.
 *
 * Core Architectural Invariants:
 * 1. World Core remains authoritative.
 * 2. Time authority is the World Clock (UTC instant projected into America/Ciudad_Juarez).
 * 3. NPC activity states: 'available' | 'busy' | 'off_duty' | 'unavailable'.
 * 4. State transitions depend strictly on simulation time (deterministic schedules),
 *    never client frame count, wall-clock time, or local client timers.
 * 5. Offline reconstruction: state is deterministically reconstructible for any instant
 *    without persisting redundant derived intermediate states.
 * 6. Explicit distinction between simulated demonstration routines and real-world data.
 */

import { deriveCivilTime, CivilTime } from './civil-time.js';
import { BilingualText, SyntheticWitnessId } from './synthetic-witness.js';
import {
  NpcActorArchetype,
  WitnessClassification,
  canActorArchetypeEnterOffDuty,
  getNpcActorClassification,
} from './npc-actor-archetypes.js';

export type NpcActivityState = 'available' | 'busy' | 'off_duty' | 'unavailable';

export interface NpcTimeWindow {
  /** Start hour in 24h format (0-23) */
  readonly startHour: number;
  /** Start minute (0-59) */
  readonly startMinute: number;
  /** End hour in 24h format (0-23) */
  readonly endHour: number;
  /** End minute (0-59) */
  readonly endMinute: number;
  /** Activity state during this window */
  readonly state: NpcActivityState;
  /** Localized human-readable activity title */
  readonly activityTitle: BilingualText;
  /** Localized human-readable activity description */
  readonly activityDescription: BilingualText;
}

export interface NpcDailySchedule {
  /** Day of week: 0 = Sunday, 1 = Monday, ..., 6 = Saturday */
  readonly dayOfWeek: number;
  /** Sequential, non-overlapping windows for the day */
  readonly windows: readonly NpcTimeWindow[];
  /** State when outside all defined windows */
  readonly defaultState: NpcActivityState;
  readonly defaultActivityTitle: BilingualText;
  readonly defaultActivityDescription: BilingualText;
}

export interface NpcScheduleProfile {
  readonly npcId: string;
  readonly canonicalName: string;
  readonly actorArchetype?: NpcActorArchetype;
  readonly isWitness?: boolean;
  readonly witnessClassification?: WitnessClassification;
  readonly witnessId?: SyntheticWitnessId | null;
  readonly role: BilingualText;
  readonly locationId: string;
  readonly locationName: BilingualText;
  readonly timeZone: string;
  readonly isDemonstrationSchedule: true;
  readonly demonstrationNotice: BilingualText;
  readonly dailySchedules: Readonly<Record<number, NpcDailySchedule>>;
}

export interface NpcActivitySnapshot {
  readonly npcId: string;
  readonly canonicalName: string;
  readonly actorArchetype: NpcActorArchetype;
  readonly hasEmploymentSchedule: boolean;
  readonly canEnterOffDuty: boolean;
  readonly isWitness: boolean;
  readonly witnessClassification: WitnessClassification;
  readonly witnessId: SyntheticWitnessId | null;
  readonly state: NpcActivityState;
  readonly activityTitle: BilingualText;
  readonly activityDescription: BilingualText;
  readonly currentLocation: string;
  readonly locationName: BilingualText;
  /** Whether the NPC is physically receptive to player dialogue */
  readonly canInteract: boolean;
  /** Dialogue demeanor prompt guidance when busy or off_duty */
  readonly interactionNotice: BilingualText | null;
  readonly evaluatedAt: string;
  readonly timeZone: string;
  readonly localCivilTime: string;
  readonly dayOfWeek: number;
  readonly dayName: string;
  readonly nextTransitionTime: string | null;
  readonly isDemonstrationSchedule: true;
  readonly demonstrationNotice: BilingualText;
}

export interface NpcRosterActivitySnapshot {
  readonly evaluatedAt: string;
  readonly timeZone: string;
  readonly localCivilTime: string;
  readonly npcs: Readonly<Record<string, NpcActivitySnapshot>>;
  readonly summary: {
    readonly availableCount: number;
    readonly busyCount: number;
    readonly offDutyCount: number;
    readonly unavailableCount: number;
    readonly totalCount: number;
  };
  readonly isDemonstrationSchedule: true;
  readonly demonstrationNotice: BilingualText;
}

export const SIMULATED_ROUTINE_NOTICE: BilingualText = Object.freeze({
  en: 'Simulated demonstration routine for AI Hackathon Edition. Not an actual person or real-time labor record.',
  es: 'Rutina de demostración simulada para AI Hackathon Edition. No corresponde a una persona real ni registro laboral en tiempo real.',
});

const DAY_NAMES: readonly string[] = Object.freeze([
  'Sunday',
  'Monday',
  'Tuesday',
  'Wednesday',
  'Thursday',
  'Friday',
  'Saturday',
]);

/**
 * Evaluates an NPC's activity state deterministically for a given World Core time instant.
 */
export function evaluateNpcActivity(
  profile: NpcScheduleProfile,
  evaluatedAt: Date | string | number
): NpcActivitySnapshot {
  const civil: CivilTime = deriveCivilTime(evaluatedAt, profile.timeZone);
  const currentTotalMinutes = civil.hour * 60 + civil.minute;
  const daySchedule = profile.dailySchedules[civil.dayOfWeek];

  const classification = getNpcActorClassification(profile.npcId);
  const actorArchetype: NpcActorArchetype =
    profile.actorArchetype ?? classification?.actorArchetype ?? 'business_attendant';
  const allowOffDuty = canActorArchetypeEnterOffDuty(actorArchetype);
  const hasEmploymentSchedule = allowOffDuty;
  const isWitness = profile.isWitness ?? classification?.isWitness ?? false;
  const witnessClassification: WitnessClassification =
    profile.witnessClassification ?? classification?.witnessClassification ?? 'none';
  const witnessId: SyntheticWitnessId | null =
    profile.witnessId !== undefined ? profile.witnessId : (classification?.witnessId ?? null);

  let state: NpcActivityState;
  if (!allowOffDuty) {
    // Community Player Proxies do NOT have employment schedules and NEVER enter off_duty or unavailable.
    state = daySchedule && daySchedule.defaultState === 'busy' ? 'busy' : 'available';
  } else {
    state = daySchedule ? daySchedule.defaultState : 'off_duty';
  }

  let activityTitle: BilingualText = daySchedule ? daySchedule.defaultActivityTitle : !allowOffDuty ? {
    en: 'Present in pedestrian bridge queue',
    es: 'Presente en fila peatonal del puente',
  } : {
    en: 'Off duty',
    es: 'Fuera de servicio',
  };
  let activityDescription: BilingualText = daySchedule ? daySchedule.defaultActivityDescription : !allowOffDuty ? {
    en: 'Simulated community player participating in the Paso del Norte crossing queue.',
    es: 'Jugador comunitario simulado participando en la fila peatonal del cruce Paso del Norte.',
  } : {
    en: 'Not currently scheduled at this location.',
    es: 'Actualmente no tiene turno programado en esta ubicación.',
  };
  let nextTransitionMinutes: number | null = null;

  if (daySchedule && daySchedule.windows.length > 0) {
    let windowMatched = false;
    for (let i = 0; i < daySchedule.windows.length; i++) {
      const win = daySchedule.windows[i];
      if (!win) continue;
      const startMin = win.startHour * 60 + win.startMinute;
      const endMin = win.endHour * 60 + win.endMinute;

      if (currentTotalMinutes >= startMin && currentTotalMinutes < endMin) {
        if (!allowOffDuty && (win.state === 'off_duty' || win.state === 'unavailable')) {
          state = 'available';
        } else {
          state = win.state;
        }
        activityTitle = win.activityTitle;
        activityDescription = win.activityDescription;
        nextTransitionMinutes = endMin;
        windowMatched = true;
        break;
      } else if (currentTotalMinutes < startMin) {
        // Upcoming window today
        if (nextTransitionMinutes === null || startMin < nextTransitionMinutes) {
          nextTransitionMinutes = startMin;
        }
      }
    }

    if (!windowMatched && nextTransitionMinutes === null) {
      // Find earliest window of next day or later today
      const nextDay = (civil.dayOfWeek + 1) % 7;
      const nextDaySchedule = profile.dailySchedules[nextDay];
      if (nextDaySchedule && nextDaySchedule.windows.length > 0) {
        const firstWin = nextDaySchedule.windows[0];
        if (firstWin) {
          nextTransitionMinutes = 24 * 60 + (firstWin.startHour * 60 + firstWin.startMinute);
        }
      }
    }
  }

  // Defensive root invariant guard: Community Player Proxies can never hold off_duty or unavailable
  if (!allowOffDuty && (state === 'off_duty' || state === 'unavailable')) {
    state = 'available';
  }

  // Calculate ISO string for next transition if known
  let nextTransitionIso: string | null = null;
  if (nextTransitionMinutes !== null) {
    const deltaMs = (nextTransitionMinutes - currentTotalMinutes) * 60 * 1000 - civil.second * 1000 - civil.millisecond;
    const currentInstantMs = typeof evaluatedAt === 'number'
      ? evaluatedAt
      : evaluatedAt instanceof Date
      ? evaluatedAt.getTime()
      : Date.parse(evaluatedAt);
    nextTransitionIso = new Date(currentInstantMs + Math.max(0, deltaMs)).toISOString();
  }

  const canInteract = state === 'available' || state === 'busy';
  let interactionNotice: BilingualText | null = null;
  if (state === 'busy') {
    interactionNotice = !allowOffDuty
      ? Object.freeze({
          en: `${profile.canonicalName} is occupied with a brief queue activity (${activityTitle.en}), and can still chat.`,
          es: `${profile.canonicalName} realiza una actividad breve en la fila (${activityTitle.es}), pero puede conversar.`,
        })
      : Object.freeze({
          en: `${profile.canonicalName} is occupied with routine tasks (${activityTitle.en}), but can provide brief responses.`,
          es: `${profile.canonicalName} se encuentra realizando labores de rutina (${activityTitle.es}), pero puede atender consultas breves.`,
        });
  } else if (state === 'off_duty') {
    interactionNotice = Object.freeze({
      en: `${profile.canonicalName} is currently off duty.`,
      es: `${profile.canonicalName} se encuentra fuera de servicio en este momento.`,
    });
  } else if (state === 'unavailable') {
    interactionNotice = Object.freeze({
      en: `${profile.canonicalName} is currently unavailable.`,
      es: `${profile.canonicalName} no está disponible en este momento.`,
    });
  }

  return Object.freeze({
    npcId: profile.npcId,
    canonicalName: profile.canonicalName,
    actorArchetype,
    hasEmploymentSchedule,
    canEnterOffDuty: allowOffDuty,
    isWitness,
    witnessClassification,
    witnessId,
    state,
    activityTitle,
    activityDescription,
    currentLocation: profile.locationId,
    locationName: profile.locationName,
    canInteract,
    interactionNotice,
    evaluatedAt: typeof evaluatedAt === 'string' ? evaluatedAt : new Date(evaluatedAt).toISOString(),
    timeZone: profile.timeZone,
    localCivilTime: civil.isoCivilString,
    dayOfWeek: civil.dayOfWeek,
    dayName: DAY_NAMES[civil.dayOfWeek] ?? 'Unknown',
    nextTransitionTime: nextTransitionIso,
    isDemonstrationSchedule: true,
    demonstrationNotice: SIMULATED_ROUTINE_NOTICE,
  });
}

/**
 * Evaluates activity for all NPCs in a roster at a given World Core time instant.
 */
export function evaluateRosterActivity(
  roster: Readonly<Record<string, NpcScheduleProfile>>,
  evaluatedAt: Date | string | number
): NpcRosterActivitySnapshot {
  const instantIso = typeof evaluatedAt === 'string' ? evaluatedAt : new Date(evaluatedAt).toISOString();
  const npcs: Record<string, NpcActivitySnapshot> = {};

  let availableCount = 0;
  let busyCount = 0;
  let offDutyCount = 0;
  let unavailableCount = 0;

  for (const [id, profile] of Object.entries(roster)) {
    const snap = evaluateNpcActivity(profile, instantIso);
    npcs[id] = snap;
    if (snap.state === 'available') availableCount++;
    else if (snap.state === 'busy') busyCount++;
    else if (snap.state === 'off_duty') offDutyCount++;
    else unavailableCount++;
  }

  const sampleTz = Object.values(roster)[0]?.timeZone ?? 'America/Ciudad_Juarez';
  const civil = deriveCivilTime(instantIso, sampleTz);

  return Object.freeze({
    evaluatedAt: instantIso,
    timeZone: sampleTz,
    localCivilTime: civil.isoCivilString,
    npcs: Object.freeze(npcs),
    summary: Object.freeze({
      availableCount,
      busyCount,
      offDutyCount,
      unavailableCount,
      totalCount: Object.keys(roster).length,
    }),
    isDemonstrationSchedule: true,
    demonstrationNotice: SIMULATED_ROUTINE_NOTICE,
  });
}

// ---------------------------------------------------------------------------
// CANONICAL ROSTER SCHEDULE PROFILES (ALL 7 NPCS)
// ---------------------------------------------------------------------------

const TZ_JUAREZ = 'America/Ciudad_Juarez';

function createOffDutyDay(
  dayOfWeek: number,
  titleEn: string,
  titleEs: string,
  descEn: string,
  descEs: string
): NpcDailySchedule {
  return {
    dayOfWeek,
    defaultState: 'off_duty',
    defaultActivityTitle: { en: titleEn, es: titleEs },
    defaultActivityDescription: { en: descEn, es: descEs },
    windows: [],
  };
}

/** 1. Elena Ramos — Border Concierge Kiosk */
export const ELENA_SCHEDULE: NpcScheduleProfile = Object.freeze({
  npcId: 'elena-border-concierge',
  canonicalName: 'Elena Ramos',
  actorArchetype: 'public_servant',
  isWitness: false,
  witnessClassification: 'none',
  witnessId: null,
  role: Object.freeze({
    en: 'Border Pedestrian Concierge',
    es: 'Concierge de Orientación Peatonal',
  }),
  locationId: 'infrastructure:puente-paso-del-norte',
  locationName: Object.freeze({
    en: 'Paso del Norte International Bridge Pedestrian Kiosk',
    es: 'Quiosco Peatonal del Puente Internacional Paso del Norte',
  }),
  timeZone: TZ_JUAREZ,
  isDemonstrationSchedule: true,
  demonstrationNotice: SIMULATED_ROUTINE_NOTICE,
  dailySchedules: Object.freeze(
    // Elena is active every day 06:00 to 22:00, with a shift handover 13:30 - 14:30
    [0, 1, 2, 3, 4, 5, 6].reduce<Record<number, NpcDailySchedule>>((acc, day) => {
      acc[day] = {
        dayOfWeek: day,
        defaultState: 'off_duty',
        defaultActivityTitle: { en: 'Off duty', es: 'Fuera de servicio' },
        defaultActivityDescription: {
          en: 'Kiosk closed for the night. Reopens daily at 06:00.',
          es: 'Quiosco cerrado durante la noche. Reanuda atención diariamente a las 06:00.',
        },
        windows: [
          {
            startHour: 6,
            startMinute: 0,
            endHour: 13,
            endMinute: 30,
            state: 'available',
            activityTitle: { en: 'Guiding pedestrians', es: 'Orientando peatones' },
            activityDescription: {
              en: 'Actively orienting cross-border travelers and explaining CBP wait times.',
              es: 'Orientando activamente a viajeros transfronterizos y explicando tiempos CBP.',
            },
          },
          {
            startHour: 13,
            startMinute: 30,
            endHour: 14,
            endMinute: 30,
            state: 'busy',
            activityTitle: {
              en: 'Shift handover & log review',
              es: 'Relevo de turno y conciliación de bitácoras',
            },
            activityDescription: {
              en: 'Reviewing pedestrian flow metrics and synchronizing bridge log entries.',
              es: 'Revisando métricas de flujo peatonal y sincronizando bitácoras de cruce.',
            },
          },
          {
            startHour: 14,
            startMinute: 30,
            endHour: 22,
            endMinute: 0,
            state: 'available',
            activityTitle: { en: 'Guiding pedestrians', es: 'Orientando peatones' },
            activityDescription: {
              en: 'Evening pedestrian guidance and border wait-time orientation.',
              es: 'Orientación vespertina de peatones y tiempos de espera en puente.',
            },
          },
        ],
      };
      return acc;
    }, {})
  ),
});

/** 2. Carlos Méndez — Farmacias Benavides Attendant */
export const CARLOS_SCHEDULE: NpcScheduleProfile = Object.freeze({
  npcId: 'carlos-farmacia-attendant',
  canonicalName: 'Carlos Méndez',
  actorArchetype: 'business_attendant',
  isWitness: false,
  witnessClassification: 'none',
  witnessId: null,
  role: Object.freeze({
    en: 'Virtual Pharmacy Attendant',
    es: 'Encargado Virtual de Farmacia',
  }),
  locationId: 'pharmacy:farmacias-benavides',
  locationName: Object.freeze({
    en: 'Farmacias Benavides (Av. Benito Juárez 610)',
    es: 'Farmacias Benavides (Av. Benito Juárez 610)',
  }),
  timeZone: TZ_JUAREZ,
  isDemonstrationSchedule: true,
  demonstrationNotice: SIMULATED_ROUTINE_NOTICE,
  dailySchedules: Object.freeze<Record<number, NpcDailySchedule>>({
    0: createOffDutyDay(
      0,
      'Store closed on Sunday',
      'Sucursal cerrada en domingo',
      'Farmacias Benavides is closed on Sundays. Reopens Monday at 07:00.',
      'Farmacias Benavides permanece cerrada los domingos. Reabre el lunes a las 07:00.'
    ),
    ...[1, 2, 3, 4, 5, 6].reduce<Record<number, NpcDailySchedule>>((acc, day) => {
      acc[day] = {
        dayOfWeek: day,
        defaultState: 'off_duty',
        defaultActivityTitle: { en: 'Store closed', es: 'Sucursal cerrada' },
        defaultActivityDescription: {
          en: 'Outside operating hours (07:00 - 22:00 Mon-Sat).',
          es: 'Fuera de horario de atención (07:00 - 22:00 Lun-Sáb).',
        },
        windows: [
          {
            startHour: 7,
            startMinute: 0,
            endHour: 22,
            endMinute: 0,
            state: 'available',
            activityTitle: { en: 'Staffing pharmacy counter', es: 'Atendiendo mostrador de farmacia' },
            activityDescription: {
              en: 'Continuously staffing Farmacias Benavides throughout operating hours (07:00 - 22:00 Mon-Sat) with zero breaks.',
              es: 'Atendiendo Farmacias Benavides de forma continua durante todo el horario comercial (07:00 - 22:00 Lun-Sáb) sin descansos.',
            },
          },
        ],
      };
      return acc;
    }, {}),
  }),
});

/** 3. Valeria Domínguez — Óptica Fashion Attendant */
export const VALERIA_SCHEDULE: NpcScheduleProfile = Object.freeze({
  npcId: 'valeria-optica-attendant',
  canonicalName: 'Valeria Domínguez',
  actorArchetype: 'business_attendant',
  isWitness: false,
  witnessClassification: 'none',
  witnessId: null,
  role: Object.freeze({
    en: 'Virtual Optical Attendant',
    es: 'Encargada Virtual de Óptica',
  }),
  locationId: 'optical:optica-fashion',
  locationName: Object.freeze({
    en: 'Óptica Fashion (Av. Benito Juárez 506)',
    es: 'Óptica Fashion (Av. Benito Juárez 506)',
  }),
  timeZone: TZ_JUAREZ,
  isDemonstrationSchedule: true,
  demonstrationNotice: SIMULATED_ROUTINE_NOTICE,
  dailySchedules: Object.freeze<Record<number, NpcDailySchedule>>({
    0: createOffDutyDay(
      0,
      'Store closed on Sunday',
      'Óptica cerrada en domingo',
      'Óptica Fashion is closed on Sundays. Reopens Monday at 09:00.',
      'Óptica Fashion permanece cerrada los domingos. Reabre el lunes a las 09:00.'
    ),
    ...[1, 2, 3, 4, 5, 6].reduce<Record<number, NpcDailySchedule>>((acc, day) => {
      acc[day] = {
        dayOfWeek: day,
        defaultState: 'off_duty',
        defaultActivityTitle: { en: 'Store closed', es: 'Óptica cerrada' },
        defaultActivityDescription: {
          en: 'Outside operating hours (09:00 - 17:00 Mon-Sat).',
          es: 'Fuera de horario de atención (09:00 - 17:00 Lun-Sáb).',
        },
        windows: [
          {
            startHour: 9,
            startMinute: 0,
            endHour: 13,
            endMinute: 0,
            state: 'available',
            activityTitle: { en: 'Assisting optical customers', es: 'Atendiendo clientes de óptica' },
            activityDescription: {
              en: 'Providing schedule, eyewear fitting, and optical consultation inside and outside Óptica Fashion.',
              es: 'Brindando atención en horarios, exhibición de armazones y consulta óptica dentro y fuera de Óptica Fashion.',
            },
          },
          {
            startHour: 13,
            startMinute: 0,
            endHour: 13,
            endMinute: 45,
            state: 'busy',
            activityTitle: {
              en: 'Equipment calibration & frame alignment',
              es: 'Calibración de equipo y ajuste de armazones',
            },
            activityDescription: {
              en: 'Sanitizing optical trial lenses and calibrating the rear-room lensometer.',
              es: 'Desinfectando lentes de prueba y calibrando el lensómetro en el laboratorio trasero.',
            },
          },
          {
            startHour: 13,
            startMinute: 45,
            endHour: 17,
            endMinute: 0,
            state: 'available',
            activityTitle: { en: 'Assisting optical customers', es: 'Atendiendo clientes de óptica' },
            activityDescription: {
              en: 'Resuming afternoon optical consultations and eyewear showroom assistance (09:00 - 17:00 Mon-Sat).',
              es: 'Reanudando consultas ópticas vespertinas y atención en sala de exhibición (09:00 - 17:00 Lun-Sáb).',
            },
          },
        ],
      };
      return acc;
    }, {}),
  }),
});

/** 4. Mateo Navarro — Community Player Proxy & Bridge Pedestrian Queue Witness */
export const MATEO_SCHEDULE: NpcScheduleProfile = Object.freeze({
  npcId: 'mateo-student',
  canonicalName: 'Mateo Navarro',
  actorArchetype: 'community_player_proxy',
  isWitness: true,
  witnessClassification: 'community_queue_witness',
  witnessId: 'mateo-student',
  role: Object.freeze({
    en: 'UTEP Computer Science Student',
    es: 'Estudiante de Ciencias Computacionales en UTEP',
  }),
  locationId: 'infrastructure:puente-paso-del-norte',
  locationName: Object.freeze({
    en: 'Paso del Norte Pedestrian Queue',
    es: 'Fila Peatonal del Puente Paso del Norte',
  }),
  timeZone: TZ_JUAREZ,
  isDemonstrationSchedule: true,
  demonstrationNotice: SIMULATED_ROUTINE_NOTICE,
  dailySchedules: Object.freeze<Record<number, NpcDailySchedule>>(
    [0, 1, 2, 3, 4, 5, 6].reduce<Record<number, NpcDailySchedule>>((acc, day) => {
      const isWeekend = day === 0 || day === 6;
      acc[day] = {
        dayOfWeek: day,
        defaultState: 'available',
        defaultActivityTitle: isWeekend
          ? { en: 'Waiting in weekend pedestrian queue', es: 'Esperando en fila peatonal de fin de semana' }
          : { en: 'Waiting in pedestrian crossing queue', es: 'Esperando en fila peatonal del cruce' },
        defaultActivityDescription: isWeekend
          ? {
              en: 'Simulated community player participating in weekend pedestrian foot traffic toward El Paso.',
              es: 'Jugador comunitario simulado participando en el cruce peatonal de fin de semana hacia El Paso.',
            }
          : {
              en: 'Simulated community player participating in the Paso del Norte crossing queue and sharing observations.',
              es: 'Jugador comunitario simulado en la fila peatonal del Puente Paso del Norte disponible para conversar.',
            },
        windows: isWeekend
          ? []
          : [
              {
                startHour: 7,
                startMinute: 0,
                endHour: 9,
                endMinute: 30,
                state: 'available',
                activityTitle: { en: 'Commuting to morning classes', es: 'Cruzando hacia clases matutinas' },
                activityDescription: {
                  en: 'Waiting in pedestrian queue to cross for morning lectures.',
                  es: 'Esperando en la fila peatonal para cruzar a sus clases matutinas.',
                },
              },
              {
                startHour: 9,
                startMinute: 30,
                endHour: 10,
                endMinute: 0,
                state: 'busy',
                activityTitle: {
                  en: 'Reviewing exam notes in queue',
                  es: 'Repasando apuntes de examen en la fila',
                },
                activityDescription: {
                  en: 'Focused on digital study flashcards on smartphone while queue moves slowly.',
                  es: 'Concentrado en notas de estudio en su teléfono mientras avanza la fila.',
                },
              },
              {
                startHour: 16,
                startMinute: 30,
                endHour: 19,
                endMinute: 30,
                state: 'available',
                activityTitle: { en: 'Returning through bridge corridor', es: 'Cruzando por el corredor del puente' },
                activityDescription: {
                  en: 'Walking along the bridge corridor between Ciudad Juárez and El Paso.',
                  es: 'Recorriendo el corredor peatonal del puente entre Ciudad Juárez y El Paso.',
                },
              },
            ],
      };
      return acc;
    }, {})
  ),
});

/** 5. Sofía Alarcón — Community Player Proxy & Bridge Pedestrian Queue Witness */
export const SOFIA_SCHEDULE: NpcScheduleProfile = Object.freeze({
  npcId: 'sofia-cross-border-retail',
  canonicalName: 'Sofía Alarcón',
  actorArchetype: 'community_player_proxy',
  isWitness: true,
  witnessClassification: 'community_queue_witness',
  witnessId: 'sofia-cross-border-retail',
  role: Object.freeze({
    en: 'Cross-Border Retail Employee',
    es: 'Trabajadora Comercial Transfronteriza',
  }),
  locationId: 'infrastructure:puente-paso-del-norte',
  locationName: Object.freeze({
    en: 'Paso del Norte Pedestrian Queue',
    es: 'Fila Peatonal del Puente Paso del Norte',
  }),
  timeZone: TZ_JUAREZ,
  isDemonstrationSchedule: true,
  demonstrationNotice: SIMULATED_ROUTINE_NOTICE,
  dailySchedules: Object.freeze<Record<number, NpcDailySchedule>>(
    [0, 1, 2, 3, 4, 5, 6].reduce<Record<number, NpcDailySchedule>>((acc, day) => {
      const isSunday = day === 0;
      acc[day] = {
        dayOfWeek: day,
        defaultState: 'available',
        defaultActivityTitle: isSunday
          ? { en: 'Crossing on Sunday errands', es: 'En fila peatonal dominical' }
          : { en: 'Waiting in pedestrian crossing queue', es: 'Esperando en fila peatonal del puente' },
        defaultActivityDescription: isSunday
          ? {
              en: 'Simulated community player participating in Sunday pedestrian queue activity.',
              es: 'Jugadora comunitaria simulada participando en el flujo peatonal dominical del puente.',
            }
          : {
              en: 'Simulated community player participating in the Paso del Norte pedestrian queue.',
              es: 'Jugadora comunitaria simulada presente en la fila peatonal del Puente Paso del Norte.',
            },
        windows: isSunday
          ? []
          : [
              {
                startHour: 8,
                startMinute: 0,
                endHour: 11,
                endMinute: 0,
                state: 'available',
                activityTitle: { en: 'Commuting in bridge queue', es: 'Avanzando en fila del puente' },
                activityDescription: {
                  en: 'Waiting and moving smoothly within the pedestrian crossing queue.',
                  es: 'Esperando y avanzando con calma dentro de la fila peatonal del cruce.',
                },
              },
              {
                startHour: 11,
                startMinute: 0,
                endHour: 11,
                endMinute: 30,
                state: 'busy',
                activityTitle: {
                  en: 'Checking crossing pace under canopy',
                  es: 'Revisando avance bajo el techado de fila',
                },
                activityDescription: {
                  en: 'Stepping briefly under the shaded canopy shelter to message family and check line progress.',
                  es: 'Haciendo una breve pausa bajo la sombra del techado para enviar un mensaje y observar el avance.',
                },
              },
              {
                startHour: 17,
                startMinute: 30,
                endHour: 20,
                endMinute: 30,
                state: 'available',
                activityTitle: { en: 'Passing through bridge plaza', es: 'Recorriendo la plaza del puente' },
                activityDescription: {
                  en: 'Active along the Avenida Juárez pedestrian approach toward the bridge plaza.',
                  es: 'Presente en el acceso peatonal de Avenida Juárez junto a la plaza del puente.',
                },
              },
            ],
      };
      return acc;
    }, {})
  ),
});

/** 6. Don Héctor Méndez — Community Player Proxy & Bridge Pedestrian Queue Witness */
export const HECTOR_SCHEDULE: NpcScheduleProfile = Object.freeze({
  npcId: 'hector-artisan',
  canonicalName: 'Don Héctor Méndez',
  actorArchetype: 'community_player_proxy',
  isWitness: true,
  witnessClassification: 'community_queue_witness',
  witnessId: 'hector-artisan',
  role: Object.freeze({
    en: 'Retired Leather Artisan & Regular Traveler',
    es: 'Artesano Retirado y Viajero Frecuente',
  }),
  locationId: 'infrastructure:puente-paso-del-norte',
  locationName: Object.freeze({
    en: 'Paso del Norte Pedestrian Queue',
    es: 'Fila Peatonal del Puente Paso del Norte',
  }),
  timeZone: TZ_JUAREZ,
  isDemonstrationSchedule: true,
  demonstrationNotice: SIMULATED_ROUTINE_NOTICE,
  dailySchedules: Object.freeze<Record<number, NpcDailySchedule>>(
    [0, 1, 2, 3, 4, 5, 6].reduce<Record<number, NpcDailySchedule>>((acc, day) => {
      const isSunday = day === 0;
      acc[day] = {
        dayOfWeek: day,
        defaultState: 'available',
        defaultActivityTitle: isSunday
          ? { en: 'Visiting family across the bridge', es: 'En fila para visita familiar dominical' }
          : { en: 'Waiting in bridge pedestrian queue', es: 'Esperando en fila peatonal del puente' },
        defaultActivityDescription: isSunday
          ? {
              en: 'Simulated community player waiting comfortably in the Sunday crossing queue.',
              es: 'Jugador comunitario simulado esperando tranquilamente en la fila dominical del puente.',
            }
          : {
              en: 'Simulated community player waiting in the Paso del Norte crossing line and observing queue conditions.',
              es: 'Jugador comunitario simulado esperando en la fila del Puente Paso del Norte y observando el ritmo de cruce.',
            },
        windows: isSunday
          ? []
          : [
              {
                startHour: 10,
                startMinute: 0,
                endHour: 13,
                endMinute: 0,
                state: 'available',
                activityTitle: { en: 'Chatting in pedestrian queue', es: 'Conversando en fila peatonal' },
                activityDescription: {
                  en: 'Conversing calmly with fellow pedestrians along the bridge plaza approach.',
                  es: 'Conversando con tranquilidad con otros peatones en el andador del puente.',
                },
              },
              {
                startHour: 13,
                startMinute: 0,
                endHour: 14,
                endMinute: 0,
                state: 'busy',
                activityTitle: {
                  en: 'Resting and carving woodkeepsake at bench',
                  es: 'Descansando y tallando figura de madera en banca',
                },
                activityDescription: {
                  en: 'Seated briefly on the wooden bench whittling a small keepsake while waiting in the queue area.',
                  es: 'Sentado brevemente en la banca de madera tallando un detalle artesanal mientras espera en la zona del puente.',
                },
              },
              {
                startHour: 14,
                startMinute: 0,
                endHour: 17,
                endMinute: 0,
                state: 'available',
                activityTitle: { en: 'Waiting near bridge plaza', es: 'Esperando junto a la plaza del puente' },
                activityDescription: {
                  en: 'Sharing anecdotes of Avenida Juárez while waiting to cross.',
                  es: 'Compartiendo anécdotas de la Avenida Juárez mientras aguarda su turno de cruce.',
                },
              },
            ],
      };
      return acc;
    }, {})
  ),
});

/** 7. Lic. Patricia Morales — Municipal Citizen Service Desk */
export const PATRICIA_SCHEDULE: NpcScheduleProfile = Object.freeze({
  npcId: 'patricia-citizen-rep',
  canonicalName: 'Lic. Patricia Morales',
  actorArchetype: 'public_servant',
  isWitness: false,
  witnessClassification: 'none',
  witnessId: null,
  role: Object.freeze({
    en: 'Citizen Service Representative',
    es: 'Enlace de Atención Ciudadana',
  }),
  locationId: 'desk:patricia-citizen-rep',
  locationName: Object.freeze({
    en: 'Municipal Citizen Issue Reporting Desk',
    es: 'Módulo Municipal de Atención Ciudadana',
  }),
  timeZone: TZ_JUAREZ,
  isDemonstrationSchedule: true,
  demonstrationNotice: SIMULATED_ROUTINE_NOTICE,
  dailySchedules: Object.freeze<Record<number, NpcDailySchedule>>({
    0: createOffDutyDay(
      0,
      'Desk closed on Sunday',
      'Módulo cerrado en domingo',
      'Municipal citizen desk is closed on weekends. Reopens Monday at 08:00.',
      'El módulo municipal de atención ciudadana permanece cerrado los fines de semana. Reabre el lunes a las 08:00.'
    ),
    6: createOffDutyDay(
      6,
      'Desk closed on Saturday',
      'Módulo cerrado en sábado',
      'Municipal citizen desk is closed on weekends.',
      'El módulo municipal de atención ciudadana permanece cerrado los fines de semana.'
    ),
    ...[1, 2, 3, 4, 5].reduce<Record<number, NpcDailySchedule>>((acc, day) => {
      acc[day] = {
        dayOfWeek: day,
        defaultState: 'off_duty',
        defaultActivityTitle: { en: 'Municipal desk closed', es: 'Módulo municipal cerrado' },
        defaultActivityDescription: {
          en: 'Desk hours are 08:00 - 16:00 Monday to Friday.',
          es: 'El horario de atención es de 08:00 a 16:00 de lunes a viernes.',
        },
        windows: [
          {
            startHour: 8,
            startMinute: 0,
            endHour: 12,
            endMinute: 0,
            state: 'available',
            activityTitle: { en: 'Receiving citizen reports', es: 'Recibiendo reportes ciudadanos' },
            activityDescription: {
              en: 'Assisting citizens with municipal infrastructure and public works reports.',
              es: 'Orientando a ciudadanos para levantar reportes de baches, luminarias y servicios públicos.',
            },
          },
          {
            startHour: 12,
            startMinute: 0,
            endHour: 13,
            endMinute: 0,
            state: 'busy',
            activityTitle: {
              en: 'Routing reports to Public Works',
              es: 'Canalización técnica de reportes con Obras Públicas',
            },
            activityDescription: {
              en: 'Reviewing AI-assisted report drafts and transmitting batches to municipal dispatch.',
              es: 'Revisando borradores asistidos por IA y canalizando expedientes a cuadrillas de mantenimiento.',
            },
          },
          {
            startHour: 13,
            startMinute: 0,
            endHour: 16,
            endMinute: 0,
            state: 'available',
            activityTitle: { en: 'Receiving citizen reports', es: 'Recibiendo reportes ciudadanos' },
            activityDescription: {
              en: 'Assisting afternoon citizens with local public works issues.',
              es: 'Atendiendo reportes de infraestructura y orientación comunitaria vespertina.',
            },
          },
        ],
      };
      return acc;
    }, {}),
  }),
});

/**
 * Authoritative master roster of all 7 NPC schedules.
 */
export const NPC_SCHEDULE_ROSTER: Readonly<Record<string, NpcScheduleProfile>> = Object.freeze({
  'elena-border-concierge': ELENA_SCHEDULE,
  'carlos-farmacia-attendant': CARLOS_SCHEDULE,
  'valeria-optica-attendant': VALERIA_SCHEDULE,
  'mateo-student': MATEO_SCHEDULE,
  'sofia-cross-border-retail': SOFIA_SCHEDULE,
  'hector-artisan': HECTOR_SCHEDULE,
  'patricia-citizen-rep': PATRICIA_SCHEDULE,
});
