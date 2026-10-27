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

  // 7. Closure Overrides (optional)
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
    lastEntryCutoffMinutes: cutoff,
    ...(closureOverrides ? { closureOverrides } : {}),
  });
}
