import { BusinessId, createBusinessId } from './business-id.js';
import { validateIanaTimeZone } from './civil-time.js';

export interface BusinessOperatingHours {
  readonly openHour: number; // 0-23
  readonly openMinute: number; // 0-59
  readonly closeHour: number; // 0-23
  readonly closeMinute: number; // 0-59
}

export interface BusinessSchedule {
  readonly businessId: BusinessId;
  readonly name: string;
  readonly timeZone: string;
  readonly operatingDays: readonly number[]; // 0 = Sunday, 1 = Monday, ..., 6 = Saturday
  readonly hours: BusinessOperatingHours;
  readonly hoursByDay?: Readonly<Record<number, BusinessOperatingHours>>; // Optional per-day hours override (0=Sun..6=Sat)
  readonly is24Hours?: boolean; // Optional 24/7 continuous operation flag
  readonly lastEntryCutoffMinutes: number; // e.g. 15 minutes before close
  readonly closureOverrides?: readonly string[]; // ISO date strings "YYYY-MM-DD"
}

export class InvalidScheduleError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'InvalidScheduleError';
  }
}

/**
 * Validates and freezes a raw schedule object into a type-safe BusinessSchedule.
 * Throws InvalidScheduleError on any validation violation.
 */
export function validateBusinessSchedule(raw: unknown): BusinessSchedule {
  if (!raw || typeof raw !== 'object') {
    throw new InvalidScheduleError('Schedule must be a non-null object');
  }

  const candidate = raw as Record<string, unknown>;

  // 1. Business ID
  if (!candidate.businessId) {
    throw new InvalidScheduleError('Schedule requires a "businessId"');
  }
  let businessId: BusinessId;
  try {
    businessId = createBusinessId(String(candidate.businessId));
  } catch (err) {
    throw new InvalidScheduleError(
      `Invalid businessId in schedule: ${err instanceof Error ? err.message : String(err)}`
    );
  }

  // 2. Name
  if (typeof candidate.name !== 'string' || candidate.name.trim() === '') {
    throw new InvalidScheduleError('Schedule requires a non-empty string "name"');
  }
  const name = candidate.name.trim();

  // 3. Timezone
  if (typeof candidate.timeZone !== 'string' || candidate.timeZone.trim() === '') {
    throw new InvalidScheduleError('Schedule requires a non-empty string "timeZone"');
  }
  try {
    validateIanaTimeZone(candidate.timeZone);
  } catch (err) {
    throw new InvalidScheduleError(
      `Invalid timeZone in schedule: ${err instanceof Error ? err.message : String(err)}`
    );
  }
  const timeZone = candidate.timeZone.trim();

  // 4. Operating Days
  if (!Array.isArray(candidate.operatingDays) || candidate.operatingDays.length === 0) {
    throw new InvalidScheduleError(
      'Schedule requires a non-empty array of "operatingDays" (0=Sunday ... 6=Saturday)'
    );
  }
  for (const day of candidate.operatingDays) {
    if (typeof day !== 'number' || !Number.isInteger(day) || day < 0 || day > 6) {
      throw new InvalidScheduleError(
        `Invalid operating day "${String(day)}": must be an integer between 0 (Sunday) and 6 (Saturday)`
      );
    }
  }
  const operatingDays = Object.freeze([...new Set(candidate.operatingDays)].sort());

  // 5. Operating Hours
  if (!candidate.hours || typeof candidate.hours !== 'object') {
    throw new InvalidScheduleError('Schedule requires an "hours" object');
  }
  const hoursObj = candidate.hours as Record<string, unknown>;

  const openHour = hoursObj.openHour;
  const openMinute = hoursObj.openMinute;
  const closeHour = hoursObj.closeHour;
  const closeMinute = hoursObj.closeMinute;

  if (
    typeof openHour !== 'number' ||
    !Number.isInteger(openHour) ||
    openHour < 0 ||
    openHour > 23
  ) {
    throw new InvalidScheduleError(
      `Invalid openHour "${String(openHour)}": must be integer 0-23`
    );
  }

  if (
    typeof openMinute !== 'number' ||
    !Number.isInteger(openMinute) ||
    openMinute < 0 ||
    openMinute > 59
  ) {
    throw new InvalidScheduleError(
      `Invalid openMinute "${String(openMinute)}": must be integer 0-59`
    );
  }

  if (
    typeof closeHour !== 'number' ||
    !Number.isInteger(closeHour) ||
    closeHour < 0 ||
    closeHour > 23
  ) {
    throw new InvalidScheduleError(
      `Invalid closeHour "${String(closeHour)}": must be integer 0-23`
    );
  }

  if (
    typeof closeMinute !== 'number' ||
    !Number.isInteger(closeMinute) ||
    closeMinute < 0 ||
    closeMinute > 59
  ) {
    throw new InvalidScheduleError(
      `Invalid closeMinute "${String(closeMinute)}": must be integer 0-59`
    );
  }

  const openMinutesOfDay = openHour * 60 + openMinute;
  const closeMinutesOfDay = closeHour * 60 + closeMinute;

  if (closeMinutesOfDay <= openMinutesOfDay) {
    throw new InvalidScheduleError(
      `Closing time (${closeHour}:${closeMinute.toString().padStart(2, '0')}) must be strictly after opening time (${openHour}:${openMinute.toString().padStart(2, '0')}) for same-day schedule`
    );
  }

  const operatingDurationMinutes = closeMinutesOfDay - openMinutesOfDay;

  // 6. Last Entry Cutoff
  const cutoff = candidate.lastEntryCutoffMinutes;
  if (
    typeof cutoff !== 'number' ||
    !Number.isInteger(cutoff) ||
    cutoff < 0 ||
    cutoff >= operatingDurationMinutes
  ) {
    throw new InvalidScheduleError(
      `lastEntryCutoffMinutes (${String(cutoff)}) must be non-negative integer and strictly less than total operating duration (${operatingDurationMinutes} minutes)`
    );
  }

  // 7. Optional per-day operating hours overrides (hoursByDay)
  let hoursByDay: Readonly<Record<number, BusinessOperatingHours>> | undefined = undefined;
  if (candidate.hoursByDay !== undefined) {
    if (!candidate.hoursByDay || typeof candidate.hoursByDay !== 'object' || Array.isArray(candidate.hoursByDay)) {
      throw new InvalidScheduleError('hoursByDay must be a map of day-of-week (0-6) to operating hours');
    }
    const rawMap = candidate.hoursByDay as Record<string, unknown>;
    const normalizedMap: Record<number, BusinessOperatingHours> = {};
    for (const [dayKey, dayRawHours] of Object.entries(rawMap)) {
      const dayNum = Number(dayKey);
      if (!Number.isInteger(dayNum) || dayNum < 0 || dayNum > 6) {
        throw new InvalidScheduleError(
          `Invalid hoursByDay key "${dayKey}": must be an integer 0 (Sunday) through 6 (Saturday)`
        );
      }
      if (!dayRawHours || typeof dayRawHours !== 'object') {
        throw new InvalidScheduleError(`Invalid hoursByDay entry for day ${dayNum}: must be an object`);
      }
      const dObj = dayRawHours as Record<string, unknown>;
      const dOpenH = dObj.openHour;
      const dOpenM = dObj.openMinute;
      const dCloseH = dObj.closeHour;
      const dCloseM = dObj.closeMinute;
      if (
        typeof dOpenH !== 'number' || !Number.isInteger(dOpenH) || dOpenH < 0 || dOpenH > 23 ||
        typeof dOpenM !== 'number' || !Number.isInteger(dOpenM) || dOpenM < 0 || dOpenM > 59 ||
        typeof dCloseH !== 'number' || !Number.isInteger(dCloseH) || dCloseH < 0 || dCloseH > 23 ||
        typeof dCloseM !== 'number' || !Number.isInteger(dCloseM) || dCloseM < 0 || dCloseM > 59
      ) {
        throw new InvalidScheduleError(`Invalid operating hours in hoursByDay for day ${dayNum}`);
      }
      const dOpenTotal = dOpenH * 60 + dOpenM;
      const dCloseTotal = dCloseH * 60 + dCloseM;
      if (dCloseTotal <= dOpenTotal) {
        throw new InvalidScheduleError(
          `Closing time must be strictly after opening time in hoursByDay for day ${dayNum}`
        );
      }
      if (cutoff >= dCloseTotal - dOpenTotal) {
        throw new InvalidScheduleError(
          `lastEntryCutoffMinutes (${cutoff}) must be strictly less than operating duration for day ${dayNum}`
        );
      }
      normalizedMap[dayNum] = Object.freeze({
        openHour: dOpenH,
        openMinute: dOpenM,
        closeHour: dCloseH,
        closeMinute: dCloseM,
      });
    }
    if (Object.keys(normalizedMap).length > 0) {
      hoursByDay = Object.freeze(normalizedMap);
    }
  }

  // 8. Optional 24/7 flag
  let is24Hours: boolean | undefined = undefined;
  if (candidate.is24Hours !== undefined) {
    if (typeof candidate.is24Hours !== 'boolean') {
      throw new InvalidScheduleError('is24Hours must be a boolean when provided');
    }
    is24Hours = candidate.is24Hours;
  }

  // 9. Closure Overrides (optional)
  let closureOverrides: readonly string[] | undefined = undefined;
  if (candidate.closureOverrides !== undefined) {
    if (!Array.isArray(candidate.closureOverrides)) {
      throw new InvalidScheduleError('closureOverrides must be an array of date strings');
    }
    const dateRegex = /^\d{4}-\d{2}-\d{2}$/;
    for (const d of candidate.closureOverrides) {
      if (typeof d !== 'string' || !dateRegex.test(d)) {
        throw new InvalidScheduleError(
          `Invalid closureOverride date "${String(d)}", expected YYYY-MM-DD`
        );
      }
    }
    closureOverrides = Object.freeze([...candidate.closureOverrides]);
  }

  return Object.freeze({
    businessId,
    name,
    timeZone,
    operatingDays,
    hours: Object.freeze({
      openHour,
      openMinute,
      closeHour,
      closeMinute,
    }),
    ...(hoursByDay ? { hoursByDay } : {}),
    ...(is24Hours ? { is24Hours } : {}),
    lastEntryCutoffMinutes: cutoff,
    ...(closureOverrides ? { closureOverrides } : {}),
  });
}
