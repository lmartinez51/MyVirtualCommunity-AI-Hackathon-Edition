import { BusinessId } from '../domain/business-id.js';
import { BusinessSchedule } from '../domain/business-schedule.js';
import { BusinessOperationalStatus } from '../domain/operational-status.js';

/**
 * A coherent point-in-time snapshot of business operational data from the World Core.
 * Combines the static schedule configuration and the dynamic operational status
 * evaluated at an authoritative world time instant in a single atomic response.
 */
export interface BusinessScheduleSnapshot {
  readonly businessId: BusinessId;
  readonly businessName: string;
  readonly timeZone: string;
  readonly worldTime: string;
  readonly schedule: BusinessSchedule;
  readonly operationalStatus: BusinessOperationalStatus;
}

/**
 * Read-only boundary port to the World Core.
 *
 * In accordance with the World Core architectural invariant:
 * External services, AI assistants, and clients read authoritative state
 * through dedicated query boundaries and never mutate world state directly.
 */
export interface WorldCorePort {
  /**
   * Retrieves the authoritative business schedule for a given business identifier,
   * or null if the business is not registered.
   */
  getBusinessSchedule(businessId: BusinessId): Promise<BusinessSchedule | null>;

  /**
   * Returns the current authoritative World Time as an ISO-8601 UTC string.
   */
  getCurrentWorldTime(): Promise<string>;

  /**
   * Derives the authoritative operational status for a business at a specified
   * world time instant (or current world time if omitted).
   * Returns null if the business schedule does not exist.
   */
  getBusinessOperationalStatus(
    businessId: BusinessId,
    evaluatedAt?: Date | string | number
  ): Promise<BusinessOperationalStatus | null>;

  /**
   * Retrieves an authoritative, point-in-time snapshot containing both the schedule
   * definition and the operational status from a single coherent World Core read.
   * Returns null if the business schedule does not exist.
   */
  getBusinessScheduleSnapshot(
    businessId: BusinessId,
    evaluatedAt?: Date | string | number
  ): Promise<BusinessScheduleSnapshot | null>;
}
