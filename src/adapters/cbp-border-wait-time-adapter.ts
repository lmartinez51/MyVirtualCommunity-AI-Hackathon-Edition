/**
 * Official U.S. Customs and Border Protection (CBP) Border Wait Times Adapter.
 *
 * Source Origin & Discovery:
 * - CBP historically maintained public XML feeds (e.g. /xml/bwt.xml), but that endpoint has been
 *   retired (returning HTTP 404) in favor of their modernized web SPA at https://bwt.cbp.gov/.
 * - Inspection of the client-side SPA asset configuration revealed the backing JSON data feed
 *   at https://bwt.cbp.gov/api/bwtpublicmod.
 * - This endpoint delivers live observation records across 85 U.S. land ports of entry,
 *   including Paso del Norte (port 240202) and Bridge of the Americas (port 240201).
 *
 * Architectural & Stability Caveat:
 * - While this REST endpoint is publicly accessible over HTTPS without API keys or credentials,
 *   it has NOT been established as a formally documented, stable, versioned public API contract with SLA.
 * - To safeguard application integrity:
 *   1. This adapter is strictly isolated behind the read-only BorderCrossingPort interface.
 *   2. Server-side requests are bounded by timeouts (default 4000ms) with AbortController.
 *   3. Responses are validated defensively; any unexpected payload structure, HTTP error, or timeout
 *      is treated explicitly as a source/parsing failure (returning unavailable observations)
 *      rather than silently fabricating synthetic wait estimates.
 *   4. Data freshness and crossing operational status (Open/Closed) are modeled distinctly so an upstream
 *      outage is never conflated with a closed crossing.
 *   5. Observations strictly capture the General / Standard pedestrian lane category, distinguished from
 *      the Ready Lane or SENTRI.
 */
import {
  BorderCrossingObservation,
  CANONICAL_CROSSINGS,
  CrossingId,
  evaluateFreshness,
  FreshnessStatus,
  STANDARD_PEDESTRIAN_LANE,
} from '../domain/border-crossing.js';
import { BorderCrossingPort } from '../ports/border-crossing-port.js';

export interface CbpBorderWaitTimeAdapterOptions {
  readonly baseURL?: string;
  readonly fetchFn?: typeof fetch;
  readonly timeoutMs?: number;
  readonly cacheTtlMs?: number;
}

interface CachedResult {
  readonly observations: {
    readonly pasoDelNorte: BorderCrossingObservation;
    readonly bridgeOfTheAmericas: BorderCrossingObservation;
  };
  readonly fetchedAt: string;
  readonly expiresAt: number;
}

export class CbpBorderWaitTimeAdapter implements BorderCrossingPort {
  private readonly baseURL: string;
  private readonly fetchFn: typeof fetch;
  private readonly timeoutMs: number;
  private readonly cacheTtlMs: number;
  private cache: CachedResult | null = null;

  constructor(options: CbpBorderWaitTimeAdapterOptions = {}) {
    this.baseURL = (options.baseURL ?? 'https://bwt.cbp.gov').replace(/\/+$/, '');
    this.fetchFn = options.fetchFn ?? globalThis.fetch.bind(globalThis);
    this.timeoutMs = options.timeoutMs ?? 4000;
    this.cacheTtlMs = options.cacheTtlMs ?? 30000; // 30 second bounded cache
  }

  /**
   * Retrieves current pedestrian crossing observations for Paso del Norte and BOTA.
   */
  public async getPedestrianCrossings(bypassCache: boolean = false): Promise<{
    observations: {
      pasoDelNorte: BorderCrossingObservation;
      bridgeOfTheAmericas: BorderCrossingObservation;
    };
    fetchedAt: string;
  }> {
    const now = Date.now();
    if (!bypassCache && this.cache && now < this.cache.expiresAt) {
      return {
        observations: this.cache.observations,
        fetchedAt: this.cache.fetchedAt,
      };
    }

    const fetchedAt = new Date().toISOString();
    let upstreamData: unknown = null;
    let fetchError: Error | null = null;

    try {
      upstreamData = await this.fetchUpstreamData();
    } catch (err) {
      fetchError = err instanceof Error ? err : new Error(String(err));
    }

    const pasoDelNorte = this.normalizeCrossing(
      'paso-del-norte',
      upstreamData,
      fetchedAt,
      fetchError?.message
    );

    const bridgeOfTheAmericas = this.normalizeCrossing(
      'bridge-of-the-americas',
      upstreamData,
      fetchedAt,
      fetchError?.message
    );

    const result = {
      observations: {
        pasoDelNorte,
        bridgeOfTheAmericas,
      },
      fetchedAt,
    };

    // Only cache if at least one crossing had a successful read
    if (!fetchError && (pasoDelNorte.freshness !== 'unavailable' || bridgeOfTheAmericas.freshness !== 'unavailable')) {
      this.cache = {
        observations: result.observations,
        fetchedAt,
        expiresAt: now + this.cacheTtlMs,
      };
    }

    return result;
  }

  private async fetchUpstreamData(): Promise<unknown> {
    const controller = new AbortController();
    const timeoutHandle = setTimeout(() => controller.abort(), this.timeoutMs);

    try {
      const url = `${this.baseURL}/api/bwtpublicmod`;
      const res = await this.fetchFn(url, {
        method: 'GET',
        headers: {
          Accept: 'application/json, text/plain, */*',
        },
        signal: controller.signal,
      });

      if (!res.ok) {
        throw new Error(`CBP upstream returned HTTP ${res.status} ${res.statusText}`);
      }

      const text = await res.text();
      return JSON.parse(text);
    } finally {
      clearTimeout(timeoutHandle);
    }
  }

  private normalizeCrossing(
    id: CrossingId,
    upstream: unknown,
    fetchedAt: string,
    generalError?: string
  ): BorderCrossingObservation {
    const meta = CANONICAL_CROSSINGS[id];

    if (generalError || !Array.isArray(upstream)) {
      return this.createUnavailableObservation(
        id,
        fetchedAt,
        generalError ?? 'Invalid or non-array CBP response'
      );
    }

    const portItem = upstream.find(
      (item) => item && typeof item === 'object' && item.port_number === meta.portNumber
    );

    if (!portItem) {
      return this.createUnavailableObservation(
        id,
        fetchedAt,
        `Port ${meta.portNumber} (${meta.shortName}) not found in CBP feed`
      );
    }

    const portStatus: 'Open' | 'Closed' | 'Unknown' =
      portItem.port_status === 'Open'
        ? 'Open'
        : portItem.port_status === 'Closed'
        ? 'Closed'
        : 'Unknown';

    const pedLanes = portItem.pedestrian_lanes;
    const stdLanes = pedLanes?.standard_lanes;

    const operationalStatus =
      typeof stdLanes?.operational_status === 'string' && stdLanes.operational_status.trim()
        ? stdLanes.operational_status.trim()
        : portStatus === 'Closed'
        ? 'Lanes Closed'
        : 'Unknown';

    // Parse numeric delay minutes
    let delayMinutes: number | null = null;
    const rawDelay = stdLanes?.delay_minutes;
    if (typeof rawDelay === 'number' && !isNaN(rawDelay)) {
      delayMinutes = rawDelay;
    } else if (typeof rawDelay === 'string' && rawDelay.trim() !== '') {
      const parsed = parseInt(rawDelay.trim(), 10);
      if (!isNaN(parsed)) {
        delayMinutes = parsed;
      }
    }

    // Parse lanes open
    let lanesOpen: number | null = null;
    const rawLanes = stdLanes?.lanes_open;
    if (typeof rawLanes === 'number' && !isNaN(rawLanes)) {
      lanesOpen = rawLanes;
    } else if (typeof rawLanes === 'string' && rawLanes.trim() !== '') {
      const parsed = parseInt(rawLanes.trim(), 10);
      if (!isNaN(parsed)) {
        lanesOpen = parsed;
      }
    }

    // Parse maximum lanes
    let maximumLanes: number | null = null;
    const rawMax = pedLanes?.maximum_lanes;
    if (typeof rawMax === 'number' && !isNaN(rawMax)) {
      maximumLanes = rawMax;
    } else if (typeof rawMax === 'string' && rawMax.trim() !== '') {
      const parsed = parseInt(rawMax.trim(), 10);
      if (!isNaN(parsed)) {
        maximumLanes = parsed;
      }
    }

    // Closed ports must never report a numeric delay
    if (portStatus === 'Closed') {
      delayMinutes = null;
    }

    // Parse source timestamps
    const rawUpdateTime = typeof stdLanes?.update_time === 'string' && stdLanes.update_time.trim()
      ? stdLanes.update_time.trim()
      : null;

    const sourceObservationTimestamp = this.parseSourceTimestamp(
      portItem.date,
      rawUpdateTime,
      portItem.time
    );

    const freshness: FreshnessStatus = evaluateFreshness(
      sourceObservationTimestamp,
      fetchedAt,
      operationalStatus,
      portStatus
    );

    return {
      crossingId: id,
      portNumber: meta.portNumber,
      canonicalName: meta.canonicalName,
      direction: 'juarez-to-el-paso',
      mode: 'pedestrian',
      laneCategory: 'standard',
      laneCategoryName: STANDARD_PEDESTRIAN_LANE.name,
      delayMinutes,
      unit: 'minutes',
      operationalStatus,
      portStatus,
      lanesOpen,
      maximumLanes,
      sourceObservationTime: rawUpdateTime,
      sourceObservationTimestamp,
      fetchedAt,
      sourceUrl: meta.sourceDetailUrl,
      provider: meta.provider,
      freshness,
    };
  }

  private createUnavailableObservation(
    id: CrossingId,
    fetchedAt: string,
    errorMessage: string
  ): BorderCrossingObservation {
    const meta = CANONICAL_CROSSINGS[id];
    return {
      crossingId: id,
      portNumber: meta.portNumber,
      canonicalName: meta.canonicalName,
      direction: 'juarez-to-el-paso',
      mode: 'pedestrian',
      laneCategory: 'standard',
      laneCategoryName: STANDARD_PEDESTRIAN_LANE.name,
      delayMinutes: null,
      unit: 'minutes',
      operationalStatus: 'Unavailable',
      portStatus: 'Unknown',
      lanesOpen: null,
      maximumLanes: null,
      sourceObservationTime: null,
      sourceObservationTimestamp: null,
      fetchedAt,
      sourceUrl: meta.sourceDetailUrl,
      provider: meta.provider,
      freshness: 'unavailable',
      errorMessage,
    };
  }

  /**
   * Parses CBP source date ("MM/DD/YYYY") and update_time ("At 6:00 pm MDT") into ISO UTC.
   */
  private parseSourceTimestamp(
    rawDate: unknown,
    rawUpdateTime: string | null,
    rawFallbackTime: unknown
  ): string | null {
    if (typeof rawDate !== 'string' || !rawDate.includes('/')) {
      return null;
    }

    const dateParts = rawDate.split('/');
    const [rawMonth, rawDay, rawYear] = dateParts;
    if (!rawMonth || !rawDay || !rawYear) {
      return null;
    }

    const month = parseInt(rawMonth, 10);
    const day = parseInt(rawDay, 10);
    const year = parseInt(rawYear, 10);

    if (isNaN(month) || isNaN(day) || isNaN(year)) {
      return null;
    }

    // Try parsing rawUpdateTime: e.g. "At 6:00 pm MDT" or "At 11:30 am MST"
    if (rawUpdateTime) {
      const match = rawUpdateTime.match(/At\s+(\d{1,2}):(\d{2})\s*(am|pm)\s*([A-Z]{3})/i);
      if (match && match[1] && match[2] && match[3] && match[4]) {
        let hour = parseInt(match[1], 10);
        const minute = parseInt(match[2], 10);
        const meridiem = match[3].toLowerCase();
        const tzAbbr = match[4].toUpperCase();

        if (meridiem === 'pm' && hour < 12) hour += 12;
        if (meridiem === 'am' && hour === 12) hour = 0;

        // MDT = UTC-6, MST = UTC-7, EDT = UTC-4, EST = UTC-5, CDT = UTC-5, CST = UTC-6, PDT = UTC-7, PST = UTC-8
        let utcOffsetHours = 6; // Default to Mountain Daylight Time (UTC-6)
        if (tzAbbr === 'MST') utcOffsetHours = 7;
        else if (tzAbbr === 'MDT') utcOffsetHours = 6;
        else if (tzAbbr === 'CST') utcOffsetHours = 6;
        else if (tzAbbr === 'CDT') utcOffsetHours = 5;
        else if (tzAbbr === 'EST') utcOffsetHours = 5;
        else if (tzAbbr === 'EDT') utcOffsetHours = 4;
        else if (tzAbbr === 'PST') utcOffsetHours = 8;
        else if (tzAbbr === 'PDT') utcOffsetHours = 7;

        const date = new Date(Date.UTC(year, month - 1, day, hour + utcOffsetHours, minute, 0));
        if (!isNaN(date.getTime())) {
          return date.toISOString();
        }
      }
    }

    // Fallback to top-level "time": "18:02:07" (assumed Mountain Local, UTC-6 / UTC-7)
    if (typeof rawFallbackTime === 'string') {
      const timeParts = rawFallbackTime.split(':');
      const [rawHour, rawMin, rawSec] = timeParts;
      if (rawHour && rawMin) {
        const hour = parseInt(rawHour, 10);
        const minute = parseInt(rawMin, 10);
        const second = rawSec ? parseInt(rawSec, 10) : 0;
        if (!isNaN(hour) && !isNaN(minute)) {
          // Assume Mountain Time (UTC-6 in summer/autumn, UTC-7 in winter)
          const date = new Date(Date.UTC(year, month - 1, day, hour + 6, minute, second));
          if (!isNaN(date.getTime())) {
            return date.toISOString();
          }
        }
      }
    }

    return null;
  }
}
