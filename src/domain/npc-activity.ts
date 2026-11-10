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
import { BilingualText } from './synthetic-witness.js';

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

  let state: NpcActivityState = daySchedule ? daySchedule.defaultState : 'off_duty';
  let activityTitle: BilingualText = daySchedule ? daySchedule.defaultActivityTitle : {
    en: 'Off duty',
    es: 'Fuera de servicio',
  };
  let activityDescription: BilingualText = daySchedule ? daySchedule.defaultActivityDescription : {
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
        state = win.state;
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
    interactionNotice = Object.freeze({
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
            endHour: 14,
            endMinute: 0,
            state: 'available',
            activityTitle: { en: 'Staffing pharmacy counter', es: 'Atendiendo mostrador de farmacia' },
            activityDescription: {
              en: 'Assisting storefront visitors with opening hours, location, and corridor orientation.',
              es: 'Atendiendo a clientes en mostrador sobre horarios, ubicación y orientación del corredor.',
            },
          },
          {
            startHour: 14,
            startMinute: 0,
            endHour: 15,
            endMinute: 0,
            state: 'busy',
            activityTitle: {
              en: 'Lunch & stock verification',
              es: 'Almuerzo y verificación de anaqueles',
            },
            activityDescription: {
              en: 'Restocking retail supplies and taking midday meal break.',
              es: 'Reabasteciendo anaqueles y tomando descanso de almuerzo.',
            },
          },
          {
            startHour: 15,
            startMinute: 0,
            endHour: 21,
            endMinute: 0,
            state: 'available',
            activityTitle: { en: 'Staffing pharmacy counter', es: 'Atendiendo mostrador de farmacia' },
            activityDescription: {
              en: 'Assisting afternoon customers with schedule inquiries.',
              es: 'Atendiendo consultas de visitantes por la tarde.',
            },
          },
          {
            startHour: 21,
            startMinute: 0,
            endHour: 22,
            endMinute: 0,
            state: 'busy',
            activityTitle: {
              en: 'Register closing & night preparation',
              es: 'Cierre de caja y preparación de turno nocturno',
            },
            activityDescription: {
              en: 'Balancing register and securing storefront for closing.',
              es: 'Realizando corte de caja y preparando el cierre del local.',
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
              en: 'Providing schedule and eyewear consultation orientation.',
              es: 'Brindando orientación sobre horarios y servicios de armazones.',
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
              en: 'Sanitizing optical trial lenses and calibrating pupillometer.',
              es: 'Desinfectando lentes de prueba y calibrando equipo de medición pupilar.',
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
              en: 'Assisting afternoon visitors with store hours and appointment scheduling.',
              es: 'Atendiendo visitantes vespertinos con horarios y citas de consulta.',
            },
          },
        ],
      };
      return acc;
    }, {}),
  }),
});

/** 4. Mateo Navarro — Student Pedestrian Witness */
export const MATEO_SCHEDULE: NpcScheduleProfile = Object.freeze({
  npcId: 'mateo-student',
  canonicalName: 'Mateo Navarro',
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
  dailySchedules: Object.freeze<Record<number, NpcDailySchedule>>({
    0: createOffDutyDay(
      0,
      'Weekend rest',
      'Fin de semana en casa',
      'Not commuting today. Working on computer science coursework at home.',
      'No cruza el puente hoy. Realiza tareas de programación y descanso en casa.'
    ),
    6: createOffDutyDay(
      6,
      'Weekend study group',
      'Grupo de estudio de fin de semana',
      'Studying off-corridor with classmates.',
      'Estudiando fuera del corredor con compañeros de carrera.'
    ),
    ...[1, 2, 3, 4, 5].reduce<Record<number, NpcDailySchedule>>((acc, day) => {
      acc[day] = {
        dayOfWeek: day,
        defaultState: 'off_duty',
        defaultActivityTitle: { en: 'Attending classes in El Paso', es: 'En clases en El Paso' },
        defaultActivityDescription: {
          en: 'Currently on campus at UTEP in El Paso, Texas.',
          es: 'Actualmente en el campus universitario de UTEP en El Paso, Texas.',
        },
        windows: [
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
            activityTitle: { en: 'Returning to Juárez after class', es: 'Regresando a Juárez tras clases' },
            activityDescription: {
              en: 'Heading southbound across the bridge back to Ciudad Juárez.',
              es: 'Cruzando en dirección sur de regreso a Ciudad Juárez.',
            },
          },
        ],
      };
      return acc;
    }, {}),
  }),
});

/** 5. Sofía Alarcón — Cross-Border Retail Commuter */
export const SOFIA_SCHEDULE: NpcScheduleProfile = Object.freeze({
  npcId: 'sofia-cross-border-retail',
  canonicalName: 'Sofía Alarcón',
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
  dailySchedules: Object.freeze<Record<number, NpcDailySchedule>>({
    0: createOffDutyDay(
      0,
      'Sunday rest',
      'Descanso dominical',
      'Spending Sunday with family in Ciudad Juárez.',
      'Pasando el domingo en familia en Ciudad Juárez.'
    ),
    ...[1, 2, 3, 4, 5, 6].reduce<Record<number, NpcDailySchedule>>((acc, day) => {
      acc[day] = {
        dayOfWeek: day,
        defaultState: 'off_duty',
        defaultActivityTitle: { en: 'Working retail shift in El Paso', es: 'Laborando en turno comercial en El Paso' },
        defaultActivityDescription: {
          en: 'Working floor shift at retail store in downtown El Paso.',
          es: 'Cumpliendo turno de trabajo en comercio del centro de El Paso.',
        },
        windows: [
          {
            startHour: 8,
            startMinute: 0,
            endHour: 11,
            endMinute: 0,
            state: 'available',
            activityTitle: { en: 'Commuting to retail shift', es: 'Cruzando hacia turno de trabajo' },
            activityDescription: {
              en: 'Walking in pedestrian queue with cross-border work permit.',
              es: 'Avanzando en la fila peatonal con permiso laboral transfronterizo.',
            },
          },
          {
            startHour: 11,
            startMinute: 0,
            endHour: 11,
            endMinute: 30,
            state: 'busy',
            activityTitle: {
              en: 'Coordinating shift with supervisor',
              es: 'Coordinando horario con supervisor',
            },
            activityDescription: {
              en: 'Sending message to store supervisor regarding bridge crossing progress.',
              es: 'Enviando mensaje al encargado de tienda sobre el avance en el puente.',
            },
          },
          {
            startHour: 17,
            startMinute: 30,
            endHour: 20,
            endMinute: 30,
            state: 'available',
            activityTitle: { en: 'Returning home from work', es: 'Regresando a casa tras jornada' },
            activityDescription: {
              en: 'Walking southbound back across the bridge to Avenida Juárez.',
              es: 'Caminando de regreso en dirección sur hacia Avenida Juárez.',
            },
          },
        ],
      };
      return acc;
    }, {}),
  }),
});

/** 6. Don Héctor Méndez — Artisan Woodworker */
export const HECTOR_SCHEDULE: NpcScheduleProfile = Object.freeze({
  npcId: 'hector-artisan',
  canonicalName: 'Don Héctor Méndez',
  role: Object.freeze({
    en: 'Traditional Woodcraft Artisan',
    es: 'Artesano de Madera Tradicional',
  }),
  locationId: 'infrastructure:puente-paso-del-norte',
  locationName: Object.freeze({
    en: 'Corridor Pedestrian Walkway near Bridge Plaza',
    es: 'Andador Peatonal cerca de Plaza del Puente',
  }),
  timeZone: TZ_JUAREZ,
  isDemonstrationSchedule: true,
  demonstrationNotice: SIMULATED_ROUTINE_NOTICE,
  dailySchedules: Object.freeze<Record<number, NpcDailySchedule>>({
    0: createOffDutyDay(
      0,
      'Sunday rest',
      'Descanso dominical',
      'At home workshop preparing wood materials for the upcoming week.',
      'En taller preparando maderas y barnices para la semana.'
    ),
    ...[1, 2, 3, 4, 5, 6].reduce<Record<number, NpcDailySchedule>>((acc, day) => {
      acc[day] = {
        dayOfWeek: day,
        defaultState: 'off_duty',
        defaultActivityTitle: { en: 'Artisan workshop', es: 'Taller de artesanías' },
        defaultActivityDescription: {
          en: 'Outside outdoor exhibition hours (10:00 - 17:00 Mon-Sat).',
          es: 'Fuera de horario de exhibición en corredor (10:00 - 17:00 Lun-Sáb).',
        },
        windows: [
          {
            startHour: 10,
            startMinute: 0,
            endHour: 13,
            endMinute: 0,
            state: 'available',
            activityTitle: { en: 'Exhibiting woodcrafts', es: 'Mostrando artesanías de madera' },
            activityDescription: {
              en: 'Conversing with travelers and exhibiting handcrafted ironwood and pine figures.',
              es: 'Conversando con transeúntes y exhibiendo figuras de palo fierro y pino.',
            },
          },
          {
            startHour: 13,
            startMinute: 0,
            endHour: 14,
            endMinute: 0,
            state: 'busy',
            activityTitle: {
              en: 'Carving and polishing pieces',
              es: 'Tallando y puliendo piezas artesanales',
            },
            activityDescription: {
              en: 'Carefully carving relief details on a wooden plaque at his workstation.',
              es: 'Tallando con gubia detalles en relieve en una tabla de mezquite.',
            },
          },
          {
            startHour: 14,
            startMinute: 0,
            endHour: 17,
            endMinute: 0,
            state: 'available',
            activityTitle: { en: 'Exhibiting woodcrafts', es: 'Mostrando artesanías de madera' },
            activityDescription: {
              en: 'Sharing stories of Avenida Juárez and showing handmade souvenirs.',
              es: 'Compartiendo anécdotas de la Avenida Juárez y mostrando recuerdos artesanales.',
            },
          },
        ],
      };
      return acc;
    }, {}),
  }),
});

/** 7. Lic. Patricia Morales — Municipal Citizen Service Desk */
export const PATRICIA_SCHEDULE: NpcScheduleProfile = Object.freeze({
  npcId: 'patricia-citizen-rep',
  canonicalName: 'Lic. Patricia Morales',
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
