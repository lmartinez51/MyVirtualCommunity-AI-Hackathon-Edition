/**
 * Border Intelligence Domain Models and Comparison Logic.
 *
 * Grounded in official U.S. Customs and Border Protection (CBP) Border Wait Times
 * and Chihuahua State Bridge Authority (Fideicomiso de Puentes Fronterizos) records.
 *
 * Strictly read-only; does not alter simulation time, world state, or business schedules.
 */

export type CrossingId = 'paso-del-norte' | 'bridge-of-the-americas';
export type CrossingDirection = 'juarez-to-el-paso';
export type CrossingMode = 'pedestrian';
export type PedestrianLaneCategory = 'standard';
export type FreshnessStatus = 'fresh' | 'stale' | 'pending' | 'unavailable';

export interface ComparisonStatement {
  readonly en: string;
  readonly es: string;
}

export interface LaneCategoryDisplay {
  readonly id: PedestrianLaneCategory;
  readonly name: ComparisonStatement;
  readonly shortName: ComparisonStatement;
  readonly description: ComparisonStatement;
}

/**
 * Explicit definition of the compared pedestrian lane category.
 * Strictly covers the General / Standard lane observations from CBP.
 * Does not cover or imply coverage of the Ready Lane or SENTRI.
 */
export const STANDARD_PEDESTRIAN_LANE: LaneCategoryDisplay = {
  id: 'standard',
  name: {
    en: 'Standard / General Pedestrian Lane',
    es: 'Carril Peatonal Estándar / General',
  },
  shortName: {
    en: 'Standard / General',
    es: 'Estándar / General',
  },
  description: {
    en: 'Standard pedestrian crossing lanes (excludes Ready Lane and SENTRI).',
    es: 'Carriles peatonales estándar/generales (excluye Ready Lane y SENTRI).',
  },
} as const;

export interface CrossingMetadata {
  readonly id: CrossingId;
  readonly portNumber: string;
  readonly canonicalName: string;
  readonly shortName: string;
  readonly popularNameEs: string;
  readonly sourceDetailUrl: string;
  readonly provider: string;
}

export const CANONICAL_CROSSINGS: Record<CrossingId, CrossingMetadata> = {
  'paso-del-norte': {
    id: 'paso-del-norte',
    portNumber: '240202',
    canonicalName: 'Paso del Norte (Santa Fe)',
    shortName: 'Paso del Norte',
    popularNameEs: 'Puente Santa Fe / Paso del Norte',
    sourceDetailUrl: 'https://bwt.cbp.gov/details/06240202/PED',
    provider: 'U.S. Customs and Border Protection (CBP)',
  },
  'bridge-of-the-americas': {
    id: 'bridge-of-the-americas',
    portNumber: '240201',
    canonicalName: 'Bridge of the Americas (BOTA / Puente Libre)',
    shortName: 'Bridge of the Americas',
    popularNameEs: 'Puente de las Américas / Puente Libre',
    sourceDetailUrl: 'https://bwt.cbp.gov/details/06240201/PED',
    provider: 'U.S. Customs and Border Protection (CBP)',
  },
} as const;

export interface BorderCrossingObservation {
  readonly crossingId: CrossingId;
  readonly portNumber: string;
  readonly canonicalName: string;
  readonly direction: CrossingDirection;
  readonly mode: CrossingMode;
  readonly laneCategory: PedestrianLaneCategory;
  readonly laneCategoryName: ComparisonStatement;
  readonly delayMinutes: number | null;
  readonly unit: 'minutes';
  readonly operationalStatus: string;
  readonly portStatus: 'Open' | 'Closed' | 'Unknown';
  readonly lanesOpen: number | null;
  readonly maximumLanes: number | null;
  readonly sourceObservationTime: string | null;
  readonly sourceObservationTimestamp: string | null;
  readonly fetchedAt: string;
  readonly sourceUrl: string;
  readonly provider: string;
  readonly freshness: FreshnessStatus;
  readonly errorMessage?: string;
}

export interface BorderCrossingComparison {
  readonly direction: CrossingDirection;
  readonly mode: CrossingMode;
  readonly laneCategory: PedestrianLaneCategory;
  readonly laneCategoryDisplay: ComparisonStatement;
  readonly observations: {
    readonly pasoDelNorte: BorderCrossingObservation;
    readonly bridgeOfTheAmericas: BorderCrossingObservation;
  };
  readonly comparison: {
    readonly canCompare: boolean;
    readonly differenceMinutes: number | null;
    readonly lowerWaitCrossingId: CrossingId | 'equal' | null;
    readonly statement: ComparisonStatement;
    readonly reasonUnavailable?: ComparisonStatement;
  };
  readonly cameras: {
    readonly officialAuthorityUrl: string;
    readonly secondaryAuthorityUrl: string;
    readonly provider: string;
    readonly description: ComparisonStatement;
  };
  readonly fetchedAt: string;
}

export const OFFICIAL_CAMERA_METADATA = {
  officialAuthorityUrl: 'https://www.puentesfronterizos.gob.mx/camaras-en-vivo.php',
  secondaryAuthorityUrl: 'https://pdnuno.com/border-crossings/paso-del-norte',
  provider: 'Fideicomiso de Puentes Fronterizos de Chihuahua & City of El Paso',
  description: {
    en: 'Official live video reference cameras provided by the Chihuahua Bridge Authority and City of El Paso. Visual reference only; does not calculate wait estimates.',
    es: 'Cámaras oficiales de video en vivo provistas por el Fideicomiso de Puentes Fronterizos de Chihuahua y la Ciudad de El Paso. Referencia visual únicamente; no calcula estimaciones de espera.',
  },
} as const;

/**
 * Freshness policy threshold: observations older than 60 minutes are classified as stale.
 */
export const FRESHNESS_THRESHOLD_MS = 60 * 60 * 1000;

/**
 * Evaluates observation freshness based on source reported time and application fetch time.
 *
 * Operational distinction:
 * - A closed crossing is NOT described as an unavailable data source.
 * - If the source successfully reported the port status (Open or Closed), data availability
 *   is evaluated independently of port operational state.
 * - A source timestamp in the future (relative to fetch time) is never classified as fresh.
 */
export function evaluateFreshness(
  sourceTimestampIso: string | null,
  fetchedAtIso: string,
  operationalStatus: string,
  _portStatus: 'Open' | 'Closed' | 'Unknown',
  errorMessage?: string
): FreshnessStatus {
  if (errorMessage) {
    return 'unavailable';
  }

  const lowerOp = (operationalStatus || '').toLowerCase();
  if (
    lowerOp.includes('pending') ||
    lowerOp.includes('update pending') ||
    lowerOp === 'n/a'
  ) {
    return 'pending';
  }

  if (!sourceTimestampIso) {
    // When source timestamp is unavailable, cannot verify freshness
    return 'stale';
  }

  const sourceTime = new Date(sourceTimestampIso).getTime();
  const fetchTime = new Date(fetchedAtIso).getTime();

  if (isNaN(sourceTime) || isNaN(fetchTime)) {
    return 'stale';
  }

  const ageMs = fetchTime - sourceTime;

  // A source timestamp reported in the future is invalid/untrustworthy and must not be classified as fresh.
  if (ageMs < 0) {
    return 'stale';
  }

  return ageMs <= FRESHNESS_THRESHOLD_MS ? 'fresh' : 'stale';
}

/**
 * Computes deterministic comparison between two border observations.
 * Compares ONLY when both observations provide compatible numeric wait values, are fresh,
 * represent the exact same lane category, and are officially open.
 */
export function comparePedestrianCrossings(
  pasoDelNorte: BorderCrossingObservation,
  bridgeOfTheAmericas: BorderCrossingObservation
): BorderCrossingComparison['comparison'] {
  const pdnIsOpen = pasoDelNorte.portStatus === 'Open';
  const botaIsOpen = bridgeOfTheAmericas.portStatus === 'Open';

  const pdnValid =
    pdnIsOpen &&
    pasoDelNorte.freshness === 'fresh' &&
    typeof pasoDelNorte.delayMinutes === 'number' &&
    !isNaN(pasoDelNorte.delayMinutes);

  const botaValid =
    botaIsOpen &&
    bridgeOfTheAmericas.freshness === 'fresh' &&
    typeof bridgeOfTheAmericas.delayMinutes === 'number' &&
    !isNaN(bridgeOfTheAmericas.delayMinutes);

  const sameCategory = pasoDelNorte.laneCategory === bridgeOfTheAmericas.laneCategory;

  if (!pdnValid || !botaValid || !sameCategory) {
    const reasonsEn: string[] = [];
    const reasonsEs: string[] = [];

    if (!sameCategory) {
      reasonsEn.push('Crossings are being compared under incompatible lane categories');
      reasonsEs.push('Los cruces se están comparando bajo categorías de carril incompatibles');
    }

    if (!pdnValid) {
      if (pasoDelNorte.portStatus === 'Closed') {
        reasonsEn.push('Paso del Norte crossing is reported as closed by CBP');
        reasonsEs.push('El cruce de Paso del Norte se reporta cerrado por CBP');
      } else if (pasoDelNorte.freshness === 'unavailable') {
        reasonsEn.push('Paso del Norte data is currently unavailable');
        reasonsEs.push('La información de Paso del Norte no está disponible');
      } else if (pasoDelNorte.freshness === 'pending') {
        reasonsEn.push('Paso del Norte has an update pending from CBP');
        reasonsEs.push('Paso del Norte tiene una actualización pendiente de CBP');
      } else if (pasoDelNorte.freshness === 'stale') {
        reasonsEn.push('Paso del Norte data is stale or lacks an official update timestamp');
        reasonsEs.push('La información de Paso del Norte no está actualizada o carece de marca de tiempo oficial');
      } else {
        reasonsEn.push('Paso del Norte reported wait time is non-numeric');
        reasonsEs.push('El tiempo reportado en Paso del Norte no es numérico');
      }
    }

    if (!botaValid) {
      if (bridgeOfTheAmericas.portStatus === 'Closed') {
        reasonsEn.push('Bridge of the Americas crossing is reported as closed by CBP');
        reasonsEs.push('El cruce de Puente de las Américas se reporta cerrado por CBP');
      } else if (bridgeOfTheAmericas.freshness === 'unavailable') {
        reasonsEn.push('Bridge of the Americas data is currently unavailable');
        reasonsEs.push('La información de Puente de las Américas no está disponible');
      } else if (bridgeOfTheAmericas.freshness === 'pending') {
        reasonsEn.push('Bridge of the Americas has an update pending from CBP');
        reasonsEs.push('Puente de las Américas tiene una actualización pendiente de CBP');
      } else if (bridgeOfTheAmericas.freshness === 'stale') {
        reasonsEn.push('Bridge of the Americas data is stale or lacks an official update timestamp');
        reasonsEs.push('La información de Puente de las Américas no está actualizada o carece de marca de tiempo oficial');
      } else {
        reasonsEn.push('Bridge of the Americas reported wait time is non-numeric');
        reasonsEs.push('El tiempo reportado en Puente de las Américas no es numérico');
      }
    }

    return {
      canCompare: false,
      differenceMinutes: null,
      lowerWaitCrossingId: null,
      statement: {
        en: 'A side-by-side wait difference cannot be computed because one or both crossings lack fresh numeric wait data for the Standard / General pedestrian lane or are closed.',
        es: 'No es posible calcular la diferencia de tiempo porque uno o ambos cruces carecen de datos numéricos actualizados para el carril peatonal Estándar / General o se encuentran cerrados.',
      },
      reasonUnavailable: {
        en: reasonsEn.join('; '),
        es: reasonsEs.join('; '),
      },
    };
  }

  const pdnWait = pasoDelNorte.delayMinutes as number;
  const botaWait = bridgeOfTheAmericas.delayMinutes as number;
  const diff = Math.abs(pdnWait - botaWait);

  if (pdnWait < botaWait) {
    return {
      canCompare: true,
      differenceMinutes: diff,
      lowerWaitCrossingId: 'paso-del-norte',
      statement: {
        en: `Paso del Norte reported wait (${pdnWait} min) for the Standard / General pedestrian lane is ${diff} minute${diff === 1 ? '' : 's'} lower than Bridge of the Americas (${botaWait} min).`,
        es: `El tiempo reportado en Paso del Norte (${pdnWait} min) para el carril peatonal Estándar / General es ${diff} minuto${diff === 1 ? '' : 's'} menor que en Puente de las Américas (${botaWait} min).`,
      },
    };
  } else if (botaWait < pdnWait) {
    return {
      canCompare: true,
      differenceMinutes: diff,
      lowerWaitCrossingId: 'bridge-of-the-americas',
      statement: {
        en: `Bridge of the Americas reported wait (${botaWait} min) for the Standard / General pedestrian lane is ${diff} minute${diff === 1 ? '' : 's'} lower than Paso del Norte (${pdnWait} min).`,
        es: `El tiempo reportado en Puente de las Américas (${botaWait} min) para el carril peatonal Estándar / General es ${diff} minuto${diff === 1 ? '' : 's'} menor que en Paso del Norte (${pdnWait} min).`,
      },
    };
  } else {
    return {
      canCompare: true,
      differenceMinutes: 0,
      lowerWaitCrossingId: 'equal',
      statement: {
        en: `Both crossings currently report an identical wait estimate of ${pdnWait} minute${pdnWait === 1 ? '' : 's'} for the Standard / General pedestrian lane.`,
        es: `Ambos cruces reportan actualmente un tiempo estimado idéntico de ${pdnWait} minuto${pdnWait === 1 ? '' : 's'} para el carril peatonal Estándar / General.`,
      },
    };
  }
}

/**
 * Canonical IANA timezone identifier for Ciudad Juárez and the regional border crossing corridor.
 */
export const BORDER_LOCAL_TIMEZONE = 'America/Ciudad_Juarez';

export interface FormatBorderTimeOptions {
  readonly timeZone?: string;
  readonly includeDate?: boolean;
}

/**
 * Formats an application fetch timestamp into a localized, user-friendly
 * representation in the local time zone of Ciudad Juárez (America/Ciudad_Juarez).
 *
 * Requirements:
 * - Deterministic conversion using native Intl.DateTimeFormat (zero external dependencies).
 * - Explicitly targets America/Ciudad_Juarez.
 * - Handles Daylight Saving Time transitions (MDT UTC-6 vs MST UTC-7) dynamically via IANA DB.
 * - Never returns raw ISO strings or raw UTC to end-users.
 * - Localizes for English ("local time (Ciudad Juárez)") and Spanish ("hora local de Ciudad Juárez").
 * - Missing or invalid timestamps return 'Unavailable' / 'No disponible' without throwing.
 */
export function formatBorderFetchTime(
  timestamp: string | Date | null | undefined,
  language: 'en' | 'es' = 'en',
  options: FormatBorderTimeOptions = {}
): string {
  const isEs = language === 'es';
  const unavailableStr = isEs ? 'No disponible' : 'Unavailable';

  if (!timestamp) {
    return unavailableStr;
  }

  const date = timestamp instanceof Date ? timestamp : new Date(timestamp);
  if (isNaN(date.getTime())) {
    return unavailableStr;
  }

  const timeZone = options.timeZone || BORDER_LOCAL_TIMEZONE;
  const locale = isEs ? 'es-MX' : 'en-US';

  const timeFormatter = new Intl.DateTimeFormat(locale, {
    timeZone,
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  });

  const formattedTime = timeFormatter.format(date);
  const tzSuffix = isEs ? '(hora local de Ciudad Juárez)' : 'local time (Ciudad Juárez)';

  if (options.includeDate) {
    const dateFormatter = new Intl.DateTimeFormat(locale, {
      timeZone,
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
    const formattedDate = dateFormatter.format(date);
    return `${formattedDate}, ${formattedTime} ${tzSuffix}`;
  }

  return `${formattedTime} ${tzSuffix}`;
}

/**
 * Formats a CBP observation timestamp for user-facing display in local Ciudad Juárez time
 * while preserving source data and distinct provenance.
 *
 * Requirements:
 * - When CBP observation time is missing or unavailable, returns 'Unavailable' / 'No disponible'.
 * - NEVER substitutes application fetch time for a missing CBP observation timestamp.
 * - If an ISO sourceObservationTimestamp is provided, converts it to America/Ciudad_Juarez.
 * - If a raw formatted sourceObservationTime string exists (e.g. "At 6:00 pm MDT"), preserves it.
 */
export function formatCbpObservationTime(
  sourceTime: string | null | undefined,
  sourceTimestampIso: string | null | undefined,
  language: 'en' | 'es' = 'en'
): string {
  const isEs = language === 'es';
  const unavailableStr = isEs ? 'No disponible' : 'Unavailable';

  if (!sourceTime && !sourceTimestampIso) {
    return unavailableStr;
  }

  if (sourceTime && sourceTime.trim() !== '') {
    return sourceTime.trim();
  }

  if (sourceTimestampIso) {
    const date = new Date(sourceTimestampIso);
    if (!isNaN(date.getTime())) {
      return formatBorderFetchTime(date, language);
    }
  }

  return unavailableStr;
}
