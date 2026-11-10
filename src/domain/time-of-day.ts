import { CivilTime, deriveCivilTime } from './civil-time.js';

/**
 * Canonical Time-of-Day presentation phases for Avenida Benito Juárez corridor.
 * Demonstration presentation intervals based on Ciudad Juárez local civil time.
 */
export type TimeOfDayPhase =
  | 'dawn'
  | 'morning'
  | 'midday'
  | 'afternoon'
  | 'sunset'
  | 'evening'
  | 'night';

export interface BilingualLabel {
  readonly en: string;
  readonly es: string;
}

export interface EnvironmentalLightingParameters {
  /** Elevation angle in degrees (-90 to +90). Negative indicates below horizon. */
  readonly sunPitchDegrees: number;
  /** Azimuth angle in degrees (0 = North, 90 = East, 180 = South, 270 = West). */
  readonly sunYawDegrees: number;
  /** Primary directional light intensity (sun or moon). */
  readonly sunIntensity: number;
  /** Directional light color in hex format (#RRGGBB). */
  readonly sunColorHex: string;
  /** Ambient light intensity multiplier. */
  readonly ambientIntensity: number;
  /** Ambient sky hemisphere color in hex format (#RRGGBB). */
  readonly ambientSkyHex: string;
  /** Ambient equator / horizon fill color in hex format (#RRGGBB). */
  readonly ambientEquatorHex: string;
  /** Ambient ground bounce color in hex format (#RRGGBB). */
  readonly ambientGroundHex: string;
  /** Whether streetlights and municipal corridor downlights are active. */
  readonly streetlightsActive: boolean;
  /** Streetlights illumination intensity (0.0 to 1.0). */
  readonly streetlightsIntensity: number;
  /** Procedural skybox exposure multiplier. */
  readonly skyboxExposure: number;
  /** Subtle distance atmospheric fog color in hex format (#RRGGBB). */
  readonly fogColorHex: string;
}

export interface PhaseBoundaryDefinition extends EnvironmentalLightingParameters {
  readonly phase: TimeOfDayPhase;
  readonly title: BilingualLabel;
  /** Civil time start as "HH:mm" in 24-hour format. */
  readonly startLocalTime: string;
  /** Civil time end as "HH:mm" in 24-hour format. */
  readonly endLocalTime: string;
  /** Start in minutes from midnight (0..1439). */
  readonly startMinutes: number;
  /** End in minutes from midnight (0..1439). */
  readonly endMinutes: number;
}

export interface WorldEnvironmentSnapshot {
  readonly evaluatedAt: string;
  readonly timeZone: string;
  readonly localCivilTime: string;
  readonly civilTime: {
    readonly year: number;
    readonly month: number;
    readonly day: number;
    readonly hour: number;
    readonly minute: number;
    readonly second: number;
    readonly dayOfWeek: number;
  };
  readonly environment: {
    readonly phase: TimeOfDayPhase;
    readonly phaseTitle: BilingualLabel;
    readonly phaseProgress: number;
    readonly isNight: boolean;
  } & EnvironmentalLightingParameters;
  readonly isDemonstrationPresentation: boolean;
  readonly demonstrationNotice: BilingualLabel;
}

export const TIME_OF_DAY_DEMONSTRATION_NOTICE: BilingualLabel = Object.freeze({
  en: 'Simulated demonstration time-of-day environmental presentation based on authoritative World Clock.',
  es: 'Presentación ambiental simulada de hora del día basada en el Reloj Mundial autoritativo.',
});

/**
 * Canonical Phase Definitions for Ciudad Juárez civil day.
 *
 * Intervals:
 * - Dawn:      05:30 – 07:00 (90 min)
 * - Morning:   07:00 – 11:30 (270 min)
 * - Midday:    11:30 – 14:00 (150 min)
 * - Afternoon: 14:00 – 17:30 (210 min)
 * - Sunset:    17:30 – 19:00 (90 min)
 * - Evening:   19:00 – 21:00 (120 min)
 * - Night:     21:00 – 05:30 (510 min, wraps midnight)
 * Total: 1440 minutes (24 hours exactly).
 */
export const CANONICAL_PHASE_DEFINITIONS: readonly PhaseBoundaryDefinition[] = Object.freeze([
  {
    phase: 'dawn',
    title: Object.freeze({ en: 'Dawn / Early Morning', es: 'Madrugada / Amanecer' }),
    startLocalTime: '05:30',
    endLocalTime: '07:00',
    startMinutes: 330,
    endMinutes: 420,
    sunPitchDegrees: 12.0,
    sunYawDegrees: 85.0,
    sunIntensity: 0.85,
    sunColorHex: '#FFD6AA',
    ambientIntensity: 0.9,
    ambientSkyHex: '#64708A',
    ambientEquatorHex: '#504855',
    ambientGroundHex: '#282522',
    streetlightsActive: true,
    streetlightsIntensity: 0.25,
    skyboxExposure: 0.85,
    fogColorHex: '#64708A',
  },
  {
    phase: 'morning',
    title: Object.freeze({ en: 'Morning', es: 'Mañana' }),
    startLocalTime: '07:00',
    endLocalTime: '11:30',
    startMinutes: 420,
    endMinutes: 690,
    sunPitchDegrees: 38.0,
    sunYawDegrees: 120.0,
    sunIntensity: 1.2,
    sunColorHex: '#FFF4E0',
    ambientIntensity: 1.0,
    ambientSkyHex: '#8EA7C8',
    ambientEquatorHex: '#636C78',
    ambientGroundHex: '#36332E',
    streetlightsActive: false,
    streetlightsIntensity: 0.0,
    skyboxExposure: 1.0,
    fogColorHex: '#8EA7C8',
  },
  {
    phase: 'midday',
    title: Object.freeze({ en: 'Midday', es: 'Mediodía' }),
    startLocalTime: '11:30',
    endLocalTime: '14:00',
    startMinutes: 690,
    endMinutes: 840,
    sunPitchDegrees: 68.0,
    sunYawDegrees: 175.0,
    sunIntensity: 1.35,
    sunColorHex: '#FFFFFF',
    ambientIntensity: 1.1,
    ambientSkyHex: '#99B8E0',
    ambientEquatorHex: '#707A87',
    ambientGroundHex: '#423E38',
    streetlightsActive: false,
    streetlightsIntensity: 0.0,
    skyboxExposure: 1.1,
    fogColorHex: '#99B8E0',
  },
  {
    phase: 'afternoon',
    title: Object.freeze({ en: 'Afternoon', es: 'Tarde' }),
    startLocalTime: '14:00',
    endLocalTime: '17:30',
    startMinutes: 840,
    endMinutes: 1050,
    sunPitchDegrees: 42.0,
    sunYawDegrees: 230.0,
    sunIntensity: 1.2,
    sunColorHex: '#FFE8C2',
    ambientIntensity: 1.0,
    ambientSkyHex: '#94A5BD',
    ambientEquatorHex: '#6E6D68',
    ambientGroundHex: '#3C3830',
    streetlightsActive: false,
    streetlightsIntensity: 0.0,
    skyboxExposure: 1.0,
    fogColorHex: '#94A5BD',
  },
  {
    phase: 'sunset',
    title: Object.freeze({ en: 'Sunset', es: 'Atardecer' }),
    startLocalTime: '17:30',
    endLocalTime: '19:00',
    startMinutes: 1050,
    endMinutes: 1140,
    sunPitchDegrees: 9.0,
    sunYawDegrees: 265.0,
    sunIntensity: 0.8,
    sunColorHex: '#FF9E59',
    ambientIntensity: 0.85,
    ambientSkyHex: '#885E6E',
    ambientEquatorHex: '#644648',
    ambientGroundHex: '#2D2322',
    streetlightsActive: true,
    streetlightsIntensity: 0.6,
    skyboxExposure: 0.75,
    fogColorHex: '#885E6E',
  },
  {
    phase: 'evening',
    title: Object.freeze({ en: 'Evening', es: 'Noche Temprana' }),
    startLocalTime: '19:00',
    endLocalTime: '21:00',
    startMinutes: 1140,
    endMinutes: 1260,
    sunPitchDegrees: -5.0, // Sun dips just below horizon, twilight
    sunYawDegrees: 290.0,
    sunIntensity: 0.35,
    sunColorHex: '#B8A0D6',
    ambientIntensity: 0.7,
    ambientSkyHex: '#323854',
    ambientEquatorHex: '#26293B',
    ambientGroundHex: '#181922',
    streetlightsActive: true,
    streetlightsIntensity: 1.0,
    skyboxExposure: 0.35,
    fogColorHex: '#323854',
  },
  {
    phase: 'night',
    title: Object.freeze({ en: 'Night', es: 'Noche' }),
    startLocalTime: '21:00',
    endLocalTime: '05:30',
    startMinutes: 1260,
    endMinutes: 330, // Midnight wrap
    sunPitchDegrees: 28.0, // Directional moonlight angle
    sunYawDegrees: 340.0,
    sunIntensity: 0.25,
    sunColorHex: '#8CAAD8', // Cool moonlight
    ambientIntensity: 0.65,
    ambientSkyHex: '#1E2538',
    ambientEquatorHex: '#161B2A',
    ambientGroundHex: '#0E111A',
    streetlightsActive: true,
    streetlightsIntensity: 1.0,
    skyboxExposure: 0.15,
    fogColorHex: '#1E2538',
  },
]);

// ---------------------------------------------------------------------------
// COLOR & MATH HELPERS
// ---------------------------------------------------------------------------

export interface RgbColor {
  readonly r: number;
  readonly g: number;
  readonly b: number;
}

export function hexToRgb(hex: string): RgbColor {
  const clean = hex.replace('#', '').trim();
  const num = parseInt(clean, 16);
  return {
    r: ((num >> 16) & 255) / 255,
    g: ((num >> 8) & 255) / 255,
    b: (num & 255) / 255,
  };
}

export function rgbToHex(rgb: RgbColor): string {
  const clampByte = (v: number) => Math.max(0, Math.min(255, Math.round(v * 255)));
  const rHex = clampByte(rgb.r).toString(16).padStart(2, '0');
  const gHex = clampByte(rgb.g).toString(16).padStart(2, '0');
  const bHex = clampByte(rgb.b).toString(16).padStart(2, '0');
  return `#${rHex}${gHex}${bHex}`.toUpperCase();
}

export function lerpRgb(a: RgbColor, b: RgbColor, t: number): RgbColor {
  const clampT = Math.max(0, Math.min(1, t));
  return {
    r: a.r + (b.r - a.r) * clampT,
    g: a.g + (b.g - a.g) * clampT,
    b: a.b + (b.b - a.b) * clampT,
  };
}

export function lerpHex(hexA: string, hexB: string, t: number): string {
  return rgbToHex(lerpRgb(hexToRgb(hexA), hexToRgb(hexB), t));
}

export function lerpFloat(a: number, b: number, t: number): number {
  const clampT = Math.max(0, Math.min(1, t));
  return Math.round((a + (b - a) * clampT) * 1000) / 1000;
}

export function lerpAngle(a: number, b: number, t: number): number {
  const clampT = Math.max(0, Math.min(1, t));
  let diff = (b - a) % 360;
  if (diff < -180) diff += 360;
  if (diff > 180) diff -= 360;
  const result = (a + diff * clampT) % 360;
  return Math.round((result < 0 ? result + 360 : result) * 10) / 10;
}

// ---------------------------------------------------------------------------
// CANONICAL PHASE RESOLUTION & INTERPOLATION
// ---------------------------------------------------------------------------

/**
 * Resolves the active phase and progress within the phase for a given civil time.
 * Handles the midnight wrap for 'night' cleanly.
 */
export function resolvePhaseAndProgress(civil: CivilTime): {
  phase: TimeOfDayPhase;
  progress: number;
  currentDef: PhaseBoundaryDefinition;
  nextDef: PhaseBoundaryDefinition;
  blendWeight: number;
} {
  const civilMinutes = civil.hour * 60 + civil.minute + civil.second / 60;

  // Find phase index
  let phaseIndex = -1;
  const defs = CANONICAL_PHASE_DEFINITIONS;

  for (let i = 0; i < defs.length; i++) {
    const def = defs[i]!;
    if (def.phase === 'night') {
      // Night wraps midnight: startMinutes (1260 = 21:00) to endMinutes (330 = 05:30)
      if (civilMinutes >= def.startMinutes || civilMinutes < def.endMinutes) {
        phaseIndex = i;
        break;
      }
    } else {
      if (civilMinutes >= def.startMinutes && civilMinutes < def.endMinutes) {
        phaseIndex = i;
        break;
      }
    }
  }

  if (phaseIndex === -1) {
    // Safeguard
    phaseIndex = 6; // Night
  }

  const currentDef = defs[phaseIndex]!;
  const nextDef = defs[(phaseIndex + 1) % defs.length]!;

  let progress = 0;
  let totalDurationMinutes = 0;
  let minutesElapsed = 0;

  if (currentDef.phase === 'night') {
    // Total night duration = (1440 - 1260) + 330 = 180 + 330 = 510 minutes
    totalDurationMinutes = (1440 - currentDef.startMinutes) + currentDef.endMinutes;
    if (civilMinutes >= currentDef.startMinutes) {
      minutesElapsed = civilMinutes - currentDef.startMinutes;
    } else {
      minutesElapsed = (1440 - currentDef.startMinutes) + civilMinutes;
    }
  } else {
    totalDurationMinutes = currentDef.endMinutes - currentDef.startMinutes;
    minutesElapsed = civilMinutes - currentDef.startMinutes;
  }

  progress = Math.max(0, Math.min(0.9999, minutesElapsed / totalDurationMinutes));

  // Transition window: smooth blending into the next phase during the final
  // min(45, duration * 0.4) minutes of each phase
  const transitionWindowMinutes = Math.min(45, totalDurationMinutes * 0.4);
  const minutesUntilEnd = totalDurationMinutes - minutesElapsed;

  let blendWeight = 0;
  if (minutesUntilEnd <= transitionWindowMinutes && transitionWindowMinutes > 0) {
    const rawT = 1.0 - minutesUntilEnd / transitionWindowMinutes;
    // Smoothstep Hermite curve: 3*t^2 - 2*t^3
    blendWeight = rawT * rawT * (3 - 2 * rawT);
    blendWeight = Math.max(0, Math.min(1.0, blendWeight));
  }

  return {
    phase: currentDef.phase,
    progress: Math.round(progress * 10000) / 10000,
    currentDef,
    nextDef,
    blendWeight: Math.round(blendWeight * 1000) / 1000,
  };
}

/**
 * Pure deterministic evaluation of the World Environment snapshot
 * for any given UTC instant and timezone.
 *
 * @param evaluatedAt Authoritative instant as Date, ISO string, or epoch ms.
 * @param timeZone Canonical IANA timezone string (default: "America/Ciudad_Juarez").
 */
export function evaluateWorldEnvironment(
  evaluatedAt: Date | string | number,
  timeZone: string = 'America/Ciudad_Juarez'
): WorldEnvironmentSnapshot {
  const instantIso = typeof evaluatedAt === 'string'
    ? evaluatedAt
    : new Date(evaluatedAt).toISOString();

  const civil = deriveCivilTime(instantIso, timeZone);
  const { phase, progress, currentDef, nextDef, blendWeight } = resolvePhaseAndProgress(civil);

  // Smoothly blend parameters from current phase definition towards next phase definition
  const sunPitchDegrees = lerpFloat(currentDef.sunPitchDegrees, nextDef.sunPitchDegrees, blendWeight);
  const sunYawDegrees = lerpAngle(currentDef.sunYawDegrees, nextDef.sunYawDegrees, blendWeight);
  const sunIntensity = lerpFloat(currentDef.sunIntensity, nextDef.sunIntensity, blendWeight);
  const sunColorHex = lerpHex(currentDef.sunColorHex, nextDef.sunColorHex, blendWeight);

  const ambientIntensity = lerpFloat(currentDef.ambientIntensity, nextDef.ambientIntensity, blendWeight);
  const ambientSkyHex = lerpHex(currentDef.ambientSkyHex, nextDef.ambientSkyHex, blendWeight);
  const ambientEquatorHex = lerpHex(currentDef.ambientEquatorHex, nextDef.ambientEquatorHex, blendWeight);
  const ambientGroundHex = lerpHex(currentDef.ambientGroundHex, nextDef.ambientGroundHex, blendWeight);

  const streetlightsIntensity = lerpFloat(currentDef.streetlightsIntensity, nextDef.streetlightsIntensity, blendWeight);
  const streetlightsActive = streetlightsIntensity > 0.05;

  const skyboxExposure = lerpFloat(currentDef.skyboxExposure, nextDef.skyboxExposure, blendWeight);
  const fogColorHex = lerpHex(currentDef.fogColorHex, nextDef.fogColorHex, blendWeight);

  return Object.freeze({
    evaluatedAt: instantIso,
    timeZone,
    localCivilTime: civil.isoCivilString,
    civilTime: Object.freeze({
      year: civil.year,
      month: civil.month,
      day: civil.day,
      hour: civil.hour,
      minute: civil.minute,
      second: civil.second,
      dayOfWeek: civil.dayOfWeek,
    }),
    environment: Object.freeze({
      phase,
      phaseTitle: currentDef.title,
      phaseProgress: progress,
      isNight: phase === 'night' || phase === 'evening',
      sunPitchDegrees,
      sunYawDegrees,
      sunIntensity,
      sunColorHex,
      ambientIntensity,
      ambientSkyHex,
      ambientEquatorHex,
      ambientGroundHex,
      streetlightsActive,
      streetlightsIntensity,
      skyboxExposure,
      fogColorHex,
    }),
    isDemonstrationPresentation: true,
    demonstrationNotice: TIME_OF_DAY_DEMONSTRATION_NOTICE,
  });
}
