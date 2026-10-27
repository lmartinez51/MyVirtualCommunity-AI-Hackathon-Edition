import { BusinessId } from './business-id.js';
import { BusinessSchedule } from './business-schedule.js';
import { CivilTime, deriveCivilTime } from './civil-time.js';

export type BusinessStatus = 'OPEN' | 'CLOSING_SOON' | 'CLOSED' | 'SPECIAL_CLOSURE';

export interface BusinessOperationalStatus {
  readonly businessId: BusinessId;
  readonly businessName: string;
  readonly status: BusinessStatus;
  readonly isOpen: boolean;
  readonly admissionPermitted: boolean;
  readonly evaluatedAtUtc: string;
  readonly civilTime: CivilTime;
  readonly openTimeCivil: string;
  readonly closeTimeCivil: string;
  readonly cutoffTimeCivil: string;
  readonly timeZone: string;
  readonly summaryMessage: string;
}

const DAY_NAMES = [
  'Sunday',
  'Monday',
  'Tuesday',
  'Wednesday',
  'Thursday',
  'Friday',
  'Saturday',
] as const;

/**
 * Pure, deterministic evaluation of business operational availability and admission status.
 *
 * Grounded in the business's local civil time projection.
 *
 * Invariant:
 * The language model must NEVER invent or determine operational status.
 * Status and admission decisions are authoritatively derived here.
 */
export function evaluateBusinessOperationalStatus(
  schedule: BusinessSchedule,
  evaluatedAt: Date | string | number
): BusinessOperationalStatus {
  if (!schedule) {
    throw new Error('BusinessSchedule is required');
  }

  const civil = deriveCivilTime(evaluatedAt, schedule.timeZone);

  const pad2 = (n: number) => n.toString().padStart(2, '0');
  const openTimeCivil = `${pad2(schedule.hours.openHour)}:${pad2(schedule.hours.openMinute)}`;
  const closeTimeCivil = `${pad2(schedule.hours.closeHour)}:${pad2(schedule.hours.closeMinute)}`;

  const openMinutesOfDay = schedule.hours.openHour * 60 + schedule.hours.openMinute;
  const closeMinutesOfDay = schedule.hours.closeHour * 60 + schedule.hours.closeMinute;
  const cutoffMinutesOfDay = closeMinutesOfDay - schedule.lastEntryCutoffMinutes;

  const cutoffHour = Math.floor(cutoffMinutesOfDay / 60);
  const cutoffMinute = cutoffMinutesOfDay % 60;
  const cutoffTimeCivil = `${pad2(cutoffHour)}:${pad2(cutoffMinute)}`;

  const dayName = DAY_NAMES[civil.dayOfWeek] ?? 'Unknown';
  const civilDateStr = `${civil.year}-${pad2(civil.month)}-${pad2(civil.day)}`;

  // Evaluate instant as UTC ISO string for provenance
  const dateObj =
    evaluatedAt instanceof Date
      ? evaluatedAt
      : typeof evaluatedAt === 'number'
      ? new Date(evaluatedAt)
      : new Date(Date.parse(evaluatedAt));
  const evaluatedAtUtc = dateObj.toISOString();

  // 1. Check special closure overrides (e.g. holiday or full-day closure)
  if (schedule.closureOverrides && schedule.closureOverrides.includes(civilDateStr)) {
    return Object.freeze({
      businessId: schedule.businessId,
      businessName: schedule.name,
      status: 'SPECIAL_CLOSURE',
      isOpen: false,
      admissionPermitted: false,
      evaluatedAtUtc,
      civilTime: civil,
      openTimeCivil,
      closeTimeCivil,
      cutoffTimeCivil,
      timeZone: schedule.timeZone,
      summaryMessage: `${schedule.name} is closed today (${civilDateStr}) for a scheduled closure/holiday.`,
    });
  }

  // 2. Check operating days (e.g. closed on Sunday)
  if (!schedule.operatingDays.includes(civil.dayOfWeek)) {
    return Object.freeze({
      businessId: schedule.businessId,
      businessName: schedule.name,
      status: 'CLOSED',
      isOpen: false,
      admissionPermitted: false,
      evaluatedAtUtc,
      civilTime: civil,
      openTimeCivil,
      closeTimeCivil,
      cutoffTimeCivil,
      timeZone: schedule.timeZone,
      summaryMessage: `${schedule.name} is closed on ${dayName}s.`,
    });
  }

  // 3. Time of day check
  const currentMinutesOfDay = civil.hour * 60 + civil.minute;

  if (currentMinutesOfDay < openMinutesOfDay) {
    return Object.freeze({
      businessId: schedule.businessId,
      businessName: schedule.name,
      status: 'CLOSED',
      isOpen: false,
      admissionPermitted: false,
      evaluatedAtUtc,
      civilTime: civil,
      openTimeCivil,
      closeTimeCivil,
      cutoffTimeCivil,
      timeZone: schedule.timeZone,
      summaryMessage: `${schedule.name} is currently closed. Opens today at ${openTimeCivil} ${schedule.timeZone}.`,
    });
  }

  if (currentMinutesOfDay >= closeMinutesOfDay) {
    return Object.freeze({
      businessId: schedule.businessId,
      businessName: schedule.name,
      status: 'CLOSED',
      isOpen: false,
      admissionPermitted: false,
      evaluatedAtUtc,
      civilTime: civil,
      openTimeCivil,
      closeTimeCivil,
      cutoffTimeCivil,
      timeZone: schedule.timeZone,
      summaryMessage: `${schedule.name} is closed for the day. Closed at ${closeTimeCivil} ${schedule.timeZone}.`,
    });
  }

  if (currentMinutesOfDay >= cutoffMinutesOfDay) {
    return Object.freeze({
      businessId: schedule.businessId,
      businessName: schedule.name,
      status: 'CLOSING_SOON',
      isOpen: true,
      admissionPermitted: false,
      evaluatedAtUtc,
      civilTime: civil,
      openTimeCivil,
      closeTimeCivil,
      cutoffTimeCivil,
      timeZone: schedule.timeZone,
      summaryMessage: `${schedule.name} is closing soon at ${closeTimeCivil}. Last entry was at ${cutoffTimeCivil}; no new customer admissions are permitted.`,
    });
  }

  return Object.freeze({
    businessId: schedule.businessId,
    businessName: schedule.name,
    status: 'OPEN',
    isOpen: true,
    admissionPermitted: true,
    evaluatedAtUtc,
    civilTime: civil,
    openTimeCivil,
    closeTimeCivil,
    cutoffTimeCivil,
    timeZone: schedule.timeZone,
    summaryMessage: `${schedule.name} is open until ${closeTimeCivil} ${schedule.timeZone}. New admissions are permitted until ${cutoffTimeCivil}.`,
  });
}
