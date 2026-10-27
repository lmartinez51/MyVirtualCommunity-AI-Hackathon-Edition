import { BusinessId, createBusinessId } from '../domain/business-id.js';
import {
  BusinessSchedule,
  validateBusinessSchedule,
  InvalidScheduleError,
} from '../domain/business-schedule.js';
import { deriveCivilTime } from '../domain/civil-time.js';
import {
  BusinessOperationalStatus,
  BusinessStatus,
} from '../domain/operational-status.js';
import { WorldCorePort, BusinessScheduleSnapshot } from '../ports/world-core-port.js';

export class WorldCoreConnectionError extends Error {
  constructor(message: string, public readonly cause?: unknown) {
    super(message);
    this.name = 'WorldCoreConnectionError';
  }
}

export class WorldCoreTimeoutError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'WorldCoreTimeoutError';
  }
}

export class WorldCoreHttpError extends Error {
  constructor(
    public readonly statusCode: number,
    message: string,
    public readonly errorCode?: string
  ) {
    super(message);
    this.name = 'WorldCoreHttpError';
  }
}

export class WorldCoreResponseValidationError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'WorldCoreResponseValidationError';
  }
}

export interface RemoteWorldCoreOptions {
  readonly baseURL: string;
  readonly fetchFn?: typeof fetch;
  readonly timeoutMs?: number;
}

/**
 * Remote HTTP Adapter for the authoritative World Core.
 *
 * Implements WorldCorePort by issuing read-only HTTP GET requests to the
 * running World Core HTTP server.
 *
 * Key guarantees:
 * - Read-only: Only performs HTTP GET requests. Never issues commands, advances time, or mutates world state.
 * - Runtime validation: Validates all received JSON at runtime against domain rules before instantiation.
 * - Zero mocking / Zero fallback fabrication: If the World Core server is unreachable, times out,
 *   or returns an error, the adapter distinctly surfaces the error and NEVER fabricates fallback records.
 * - Distinct error taxonomy: Network failures, timeouts, HTTP errors, and malformed responses are distinct.
 * - Credential hygiene: No credentials or internal secrets are exposed in logs or errors.
 */
export class RemoteWorldCoreAdapter implements WorldCorePort {
  private readonly baseURL: string;
  private readonly fetchFn: typeof fetch;
  private readonly timeoutMs: number;

  constructor(options: RemoteWorldCoreOptions) {
    if (!options.baseURL || options.baseURL.trim() === '') {
      throw new Error('RemoteWorldCoreAdapter requires a non-empty baseURL');
    }
    this.baseURL = options.baseURL.trim().replace(/\/+$/, '');
    this.fetchFn = options.fetchFn ?? globalThis.fetch;
    this.timeoutMs = options.timeoutMs ?? 10000;
  }

  public getBaseURL(): string {
    return this.baseURL;
  }

  /**
   * Returns known customer-facing business identifiers registered in the Gómez Morín World Core.
   */
  public listBusinessIds(): BusinessId[] {
    return [
      createBusinessId('restaurant:wing-spot'),
      createBusinessId('restaurant:pasteleria-tia-lupe'),
    ];
  }

  /**
   * Internal helper to execute a timeout-bounded HTTP GET request against the World Core.
   */
  private async getJson(endpointPath: string): Promise<{ status: number; data: any }> {
    const url = `${this.baseURL}${endpointPath.startsWith('/') ? endpointPath : `/${endpointPath}`}`;
    const controller = new AbortController();
    const timeoutHandle = setTimeout(() => controller.abort(), this.timeoutMs);

    let res: Response;
    try {
      res = await this.fetchFn(url, {
        method: 'GET',
        headers: {
          Accept: 'application/json',
        },
        signal: controller.signal,
      });
    } catch (err: unknown) {
      if (err instanceof Error && err.name === 'AbortError') {
        throw new WorldCoreTimeoutError(
          `Request to World Core timed out after ${this.timeoutMs}ms for ${endpointPath}`
        );
      }
      throw new WorldCoreConnectionError(
        `Failed to connect to World Core at ${this.baseURL}: ${err instanceof Error ? err.message : String(err)}`,
        err
      );
    } finally {
      clearTimeout(timeoutHandle);
    }

    const text = await res.text();
    let data: any = null;
    if (text.trim().length > 0) {
      try {
        data = JSON.parse(text);
      } catch (err) {
        throw new WorldCoreResponseValidationError(
          `World Core returned invalid non-JSON body (HTTP ${res.status}): ${text.slice(0, 100)}`
        );
      }
    }

    return { status: res.status, data };
  }

  /**
   * Retrieves an authoritative, point-in-time snapshot containing both the schedule
   * definition and the operational status from a single coherent World Core read.
   * Returns null if the business is not registered.
   */
  public async getBusinessScheduleSnapshot(
    businessId: BusinessId,
    evaluatedAt?: Date | string | number
  ): Promise<BusinessScheduleSnapshot | null> {
    const encodedId = encodeURIComponent(businessId);
    let path = `/api/businesses/${encodedId}/schedule`;

    if (evaluatedAt !== undefined && evaluatedAt !== null) {
      const parsedDate = new Date(evaluatedAt);
      if (Number.isNaN(parsedDate.getTime())) {
        throw new Error(`Invalid evaluatedAt timestamp: ${String(evaluatedAt)}`);
      }
      path += `?at=${encodeURIComponent(parsedDate.toISOString())}`;
    }

    const { status, data } = await this.getJson(path);

    if (status === 404) {
      if (data?.error?.code === 'business-not-found' || data?.success === false) {
        return null;
      }
      throw new WorldCoreHttpError(404, `World Core resource not found for business "${businessId}"`);
    }

    if (status !== 200) {
      const errorMsg = data?.error?.message ?? `World Core responded with HTTP ${status}`;
      const errorCode = data?.error?.code;
      throw new WorldCoreHttpError(status, errorMsg, errorCode);
    }

    if (!data || typeof data !== 'object' || data.success !== true || !data.result) {
      throw new WorldCoreResponseValidationError(
        `World Core schedule response missing success or result payload`
      );
    }

    const result = data.result;
    if (typeof result !== 'object') {
      throw new WorldCoreResponseValidationError('World Core result payload must be an object');
    }

    // 1. Runtime validate the received schedule against domain invariants
    let schedule: BusinessSchedule;
    try {
      schedule = validateBusinessSchedule({
        businessId: result.businessId,
        name: result.name,
        timeZone: result.timeZone,
        operatingDays: result.schedule?.operatingDays,
        hours: result.schedule?.hours,
        lastEntryCutoffMinutes: result.schedule?.lastEntryCutoffMinutes,
        closureOverrides: result.schedule?.closureOverrides,
      });
    } catch (err) {
      if (err instanceof InvalidScheduleError) {
        throw new WorldCoreResponseValidationError(
          `World Core returned invalid schedule schema: ${err.message}`
        );
      }
      throw err;
    }

    // 2. Validate operational status and world time fields
    if (
      !result.operationalStatus ||
      typeof result.operationalStatus !== 'object' ||
      !result.worldTime ||
      typeof result.worldTime !== 'string'
    ) {
      throw new WorldCoreResponseValidationError(
        'World Core response missing operationalStatus or worldTime'
      );
    }

    const rawStatus = String(result.operationalStatus.status).toLowerCase();
    const statusMap: Record<string, BusinessStatus> = {
      open: 'OPEN',
      'closing-soon': 'CLOSING_SOON',
      closed: 'CLOSED',
    };
    const domainStatus: BusinessStatus = statusMap[rawStatus] ?? 'CLOSED';

    const civilTime = deriveCivilTime(result.worldTime, result.timeZone);

    const pad2 = (n: number) => n.toString().padStart(2, '0');
    const openTimeCivil =
      result.operationalStatus.openTimeStr ??
      (result.schedule?.hours
        ? `${pad2(result.schedule.hours.openHour)}:${pad2(result.schedule.hours.openMinute)}`
        : '09:00');

    const closeTimeCivil =
      result.operationalStatus.closingTimeStr ??
      (result.schedule?.hours
        ? `${pad2(result.schedule.hours.closeHour)}:${pad2(result.schedule.hours.closeMinute)}`
        : '18:00');

    let cutoffTimeCivil = closeTimeCivil;
    if (result.schedule?.hours && typeof result.schedule.lastEntryCutoffMinutes === 'number') {
      const closeMinutes = result.schedule.hours.closeHour * 60 + result.schedule.hours.closeMinute;
      const cutoffTotal = closeMinutes - result.schedule.lastEntryCutoffMinutes;
      cutoffTimeCivil = `${pad2(Math.floor(cutoffTotal / 60))}:${pad2(cutoffTotal % 60)}`;
    }

    const operationalStatus: BusinessOperationalStatus = Object.freeze({
      businessId,
      businessName: typeof result.name === 'string' ? result.name : String(businessId),
      status: domainStatus,
      isOpen: Boolean(result.operationalStatus.isOpen),
      admissionPermitted: Boolean(result.operationalStatus.admissionPermitted),
      evaluatedAtUtc: result.worldTime,
      civilTime,
      openTimeCivil,
      closeTimeCivil,
      cutoffTimeCivil,
      timeZone: typeof result.timeZone === 'string' ? result.timeZone : 'America/Ciudad_Juarez',
      summaryMessage:
        typeof result.operationalStatus.message === 'string'
          ? result.operationalStatus.message
          : `Status is ${domainStatus}`,
    });

    return Object.freeze({
      businessId,
      businessName: operationalStatus.businessName,
      timeZone: operationalStatus.timeZone,
      worldTime: result.worldTime,
      schedule,
      operationalStatus,
    });
  }

  /**
   * Retrieves the authoritative business schedule for a given business identifier,
   * or null if the business is not registered in the World Core.
   */
  public async getBusinessSchedule(businessId: BusinessId): Promise<BusinessSchedule | null> {
    const snapshot = await this.getBusinessScheduleSnapshot(businessId);
    return snapshot ? snapshot.schedule : null;
  }

  /**
   * Returns the current authoritative World Time as an ISO-8601 UTC string.
   */
  public async getCurrentWorldTime(): Promise<string> {
    const { status, data } = await this.getJson('/api/status');

    if (status !== 200) {
      const errorMsg = data?.error?.message ?? `World Core responded with HTTP ${status}`;
      throw new WorldCoreHttpError(status, errorMsg);
    }

    if (!data || typeof data !== 'object' || data.success !== true || !data.result) {
      throw new WorldCoreResponseValidationError(
        'World Core status response missing success or result payload'
      );
    }

    const worldTime = data.result.worldTime;
    if (typeof worldTime !== 'string' || worldTime.trim() === '') {
      throw new WorldCoreResponseValidationError(
        'World Core status response missing string worldTime'
      );
    }

    const parsedDate = new Date(worldTime);
    if (Number.isNaN(parsedDate.getTime())) {
      throw new WorldCoreResponseValidationError(
        `World Core status returned non-ISO timestamp: "${worldTime}"`
      );
    }

    return parsedDate.toISOString();
  }

  /**
   * Derives the authoritative operational status for a business at a specified
   * world time instant (or current world time if omitted).
   * Returns null if the business schedule does not exist.
   */
  public async getBusinessOperationalStatus(
    businessId: BusinessId,
    evaluatedAt?: Date | string | number
  ): Promise<BusinessOperationalStatus | null> {
    const snapshot = await this.getBusinessScheduleSnapshot(businessId, evaluatedAt);
    return snapshot ? snapshot.operationalStatus : null;
  }
}
