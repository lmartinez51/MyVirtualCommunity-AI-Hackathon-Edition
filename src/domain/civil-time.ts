/**
 * Pure, deterministic mathematical projection from an authoritative UTC instant
 * and an IANA timezone identifier into a localized civil time representation.
 *
 * Requirements:
 * - Deterministic: identical inputs always produce identical outputs.
 * - Zero external dependencies (uses native Node.js Intl).
 * - Validates timestamp and IANA timezone.
 * - Preserves exact milliseconds.
 */

export interface CivilTime {
  readonly year: number;
  readonly month: number; // 1-12
  readonly day: number; // 1-31
  readonly hour: number; // 0-23
  readonly minute: number; // 0-59
  readonly second: number; // 0-59
  readonly millisecond: number; // 0-999
  readonly dayOfWeek: number; // 0 = Sunday, 1 = Monday, ..., 6 = Saturday
  readonly timeZone: string;
  readonly utcOffsetMinutes: number; // Local wall time minus UTC in minutes
  readonly isoCivilString: string; // e.g. "2026-10-26T14:30:00.000-06:00"
}

export class InvalidTimezoneError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'InvalidTimezoneError';
  }
}

export class InvalidTimestampError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'InvalidTimestampError';
  }
}

/**
 * Validates that an IANA timezone identifier is recognized by the runtime environment.
 */
export function validateIanaTimeZone(timeZone: string): void {
  if (typeof timeZone !== 'string' || timeZone.trim() === '') {
    throw new InvalidTimezoneError('Timezone must be a non-empty string');
  }

  try {
    Intl.DateTimeFormat(undefined, { timeZone });
  } catch {
    throw new InvalidTimezoneError(`Invalid or unrecognized IANA timezone: "${timeZone}"`);
  }
}

/**
 * Projects a UTC timestamp and an IANA timezone into CivilTime.
 *
 * @param instant UTC instant as Date, ISO string, or numeric epoch milliseconds.
 * @param timeZone Canonical IANA timezone string (e.g. "America/Ciudad_Juarez").
 */
export function deriveCivilTime(
  instant: Date | string | number,
  timeZone: string
): CivilTime {
  let epochMs: number;

  if (instant instanceof Date) {
    epochMs = instant.getTime();
  } else if (typeof instant === 'string') {
    if (instant.trim() === '') {
      throw new InvalidTimestampError('Timestamp string cannot be empty');
    }
    epochMs = Date.parse(instant);
  } else if (typeof instant === 'number') {
    epochMs = instant;
  } else {
    throw new InvalidTimestampError(
      `Timestamp must be a Date, string, or number; received ${typeof instant}`
    );
  }

  if (!Number.isFinite(epochMs) || Number.isNaN(epochMs)) {
    throw new InvalidTimestampError(`Invalid timestamp value: "${String(instant)}"`);
  }

  validateIanaTimeZone(timeZone);

  const date = new Date(epochMs);
  const formatter = new Intl.DateTimeFormat('en-US', {
    timeZone,
    year: 'numeric',
    month: 'numeric',
    day: 'numeric',
    hour: 'numeric',
    minute: 'numeric',
    second: 'numeric',
    hourCycle: 'h23',
    fractionalSecondDigits: 3,
  });

  const parts = formatter.formatToParts(date);
  const partMap: Record<string, string> = {};
  for (const part of parts) {
    partMap[part.type] = part.value;
  }

  const year = parseInt(partMap.year ?? '0', 10);
  const month = parseInt(partMap.month ?? '0', 10);
  const day = parseInt(partMap.day ?? '0', 10);
  let hour = parseInt(partMap.hour ?? '0', 10);
  if (hour === 24) {
    hour = 0;
  }
  const minute = parseInt(partMap.minute ?? '0', 10);
  const second = parseInt(partMap.second ?? '0', 10);
  const millisecond = date.getUTCMilliseconds();

  // Day of week for the civil calendar date (0=Sunday, 1=Monday, ..., 6=Saturday)
  const dayOfWeek = new Date(Date.UTC(year, month - 1, day)).getUTCDay();

  // Compute signed UTC offset in minutes: (civil wall time as UTC ms - real UTC ms) / 60000
  const civilAsUtcMs = Date.UTC(year, month - 1, day, hour, minute, second, millisecond);
  const utcOffsetMinutes = Math.round((civilAsUtcMs - epochMs) / 60000);

  const pad2 = (n: number) => n.toString().padStart(2, '0');
  const pad3 = (n: number) => n.toString().padStart(3, '0');
  const pad4 = (n: number) => n.toString().padStart(4, '0');

  const sign = utcOffsetMinutes >= 0 ? '+' : '-';
  const absOffset = Math.abs(utcOffsetMinutes);
  const offsetH = pad2(Math.floor(absOffset / 60));
  const offsetM = pad2(absOffset % 60);
  const offsetStr = `${sign}${offsetH}:${offsetM}`;

  const isoCivilString = `${pad4(year)}-${pad2(month)}-${pad2(day)}T${pad2(hour)}:${pad2(minute)}:${pad2(second)}.${pad3(millisecond)}${offsetStr}`;

  return Object.freeze({
    year,
    month,
    day,
    hour,
    minute,
    second,
    millisecond,
    dayOfWeek,
    timeZone,
    utcOffsetMinutes,
    isoCivilString,
  });
}

export class InvalidEvaluationTimeError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'InvalidEvaluationTimeError';
  }
}

export interface CivilToUtcResult {
  readonly success: boolean;
  readonly utcInstant?: Date;
  readonly isAmbiguous?: boolean; // Occurs during fall-back (overlap)
  readonly isInvalid?: boolean;   // Occurs during spring-forward (gap)
  readonly disambiguationNotice?: string;
  readonly error?: string;
}

/**
 * Parses a civil local date-time string (e.g. from browser datetime-local or API)
 * and resolves it to an authoritative UTC Date instant in the given IANA timezone.
 *
 * Requirements:
 * - Deterministic, pure Node.js (zero third-party dependencies).
 * - Validates format (YYYY-MM-DDTHH:mm or YYYY-MM-DDTHH:mm:ss, optional seconds).
 * - Rejects non-existent calendar dates (e.g. Feb 30, April 31, non-leap Feb 29).
 * - Accurately models Daylight Saving Time for Northern Mexico border / Ciudad Juárez (America/Ciudad_Juarez).
 * - Detects spring-forward gap: returns isInvalid=true when local wall time does not exist.
 * - Detects fall-back overlap: returns isAmbiguous=true when local wall time occurs twice.
 * - Also safely parses ISO strings that already have explicit timezone offsets or 'Z'.
 */
export function parseLocalCivilTimeToUtc(
  input: string | Date,
  timeZone: string = 'America/Ciudad_Juarez'
): CivilToUtcResult {
  if (!input) {
    return { success: false, error: 'Evaluation time must be a non-empty string or Date' };
  }

  if (input instanceof Date) {
    if (Number.isNaN(input.getTime())) {
      return { success: false, error: 'Provided Date object is invalid (NaN)' };
    }
    return { success: true, utcInstant: input };
  }

  if (typeof input !== 'string') {
    return { success: false, error: `Expected evaluation time as string, received ${typeof input}` };
  }

  const str = input.trim();
  if (str === '') {
    return { success: false, error: 'Evaluation time string cannot be empty' };
  }

  validateIanaTimeZone(timeZone);

  // If the string contains an explicit timezone designator ('Z' or [+-]HH:MM)
  if (/[zZ]$|[+-]\d{2}(?::?\d{2})?$/.test(str)) {
    const parsedEpoch = Date.parse(str);
    if (Number.isNaN(parsedEpoch)) {
      return { success: false, error: `Invalid ISO-8601 timestamp with timezone: "${str}"` };
    }
    return { success: true, utcInstant: new Date(parsedEpoch) };
  }

  // Parse local date-time format: YYYY-MM-DDTHH:mm[:ss[.sss]]
  const match = /^(\d{4})-(\d{2})-(\d{2})[T ](\d{2}):(\d{2})(?::(\d{2})(?:\.(\d{1,3}))?)?$/.exec(str);
  if (!match) {
    return {
      success: false,
      error: `Invalid local date-time format: "${str}". Expected YYYY-MM-DDTHH:mm[:ss]`,
    };
  }

  const year = parseInt(match[1]!, 10);
  const month = parseInt(match[2]!, 10);
  const day = parseInt(match[3]!, 10);
  const hour = parseInt(match[4]!, 10);
  const minute = parseInt(match[5]!, 10);
  const second = match[6] ? parseInt(match[6], 10) : 0;
  const millisecond = match[7] ? parseInt(match[7].padEnd(3, '0'), 10) : 0;

  if (month < 1 || month > 12) {
    return { success: false, error: `Invalid month: ${month}. Must be between 1 and 12.` };
  }
  if (day < 1 || day > 31) {
    return { success: false, error: `Invalid day: ${day}. Must be between 1 and 31.` };
  }
  if (hour < 0 || hour > 23) {
    return { success: false, error: `Invalid hour: ${hour}. Must be between 0 and 23.` };
  }
  if (minute < 0 || minute > 59) {
    return { success: false, error: `Invalid minute: ${minute}. Must be between 0 and 59.` };
  }
  if (second < 0 || second > 59) {
    return { success: false, error: `Invalid second: ${second}. Must be between 0 and 59.` };
  }

  // Calendar validity check (e.g. Feb 30, Nov 31, non-leap Feb 29)
  const calCheck = new Date(Date.UTC(year, month - 1, day));
  if (calCheck.getUTCMonth() !== month - 1 || calCheck.getUTCDate() !== day) {
    return { success: false, error: `Calendar date does not exist: ${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}` };
  }

  const wallAsUtc = Date.UTC(year, month - 1, day, hour, minute, second, millisecond);

  // Probe the timezone offset at wallAsUtc, and 24h before/after to capture DST shifts
  const offsetsToTry = new Set<number>();
  offsetsToTry.add(deriveCivilTime(wallAsUtc, timeZone).utcOffsetMinutes);
  offsetsToTry.add(deriveCivilTime(wallAsUtc - 24 * 3600 * 1000, timeZone).utcOffsetMinutes);
  offsetsToTry.add(deriveCivilTime(wallAsUtc + 24 * 3600 * 1000, timeZone).utcOffsetMinutes);

  const validCandidates: Date[] = [];

  for (const offsetMinutes of offsetsToTry) {
    const candidateMs = wallAsUtc - offsetMinutes * 60000;
    const verifiedCivil = deriveCivilTime(candidateMs, timeZone);
    if (
      verifiedCivil.year === year &&
      verifiedCivil.month === month &&
      verifiedCivil.day === day &&
      verifiedCivil.hour === hour &&
      verifiedCivil.minute === minute &&
      verifiedCivil.second === second
    ) {
      if (!validCandidates.some((d) => d.getTime() === candidateMs)) {
        validCandidates.push(new Date(candidateMs));
      }
    }
  }

  if (validCandidates.length === 0) {
    return {
      success: false,
      isInvalid: true,
      error: `Local time "${str}" does not exist in timezone "${timeZone}" due to daylight saving spring-forward transition.`,
    };
  }

  if (validCandidates.length > 1) {
    // Ambiguous local time (fall-back overlap: occurs twice)
    // Sort chronologically so validCandidates[0] is the earlier occurrence (MDT, UTC-6)
    validCandidates.sort((a, b) => a.getTime() - b.getTime());
    const selected = validCandidates[0]!;
    const civilFirst = deriveCivilTime(selected, timeZone);
    const civilSecond = deriveCivilTime(validCandidates[1]!, timeZone);

    const disambiguationNotice =
      `Ambiguous local time: "${str}" occurs twice in ${timeZone} due to daylight saving fall-back transition ` +
      `(${civilFirst.isoCivilString} and ${civilSecond.isoCivilString}). ` +
      `Evaluated using the earlier occurrence (${selected.toISOString()} / ${civilFirst.isoCivilString}).`;

    return {
      success: true,
      isAmbiguous: true,
      utcInstant: selected,
      disambiguationNotice,
    };
  }

  return {
    success: true,
    utcInstant: validCandidates[0],
  };
}
