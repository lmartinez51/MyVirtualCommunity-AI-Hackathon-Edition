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
