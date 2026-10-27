import { BusinessId, createBusinessId } from '../domain/business-id.js';
import { BusinessSchedule, validateBusinessSchedule } from '../domain/business-schedule.js';
import {
  BusinessOperationalStatus,
  evaluateBusinessOperationalStatus,
} from '../domain/operational-status.js';
import { WorldCorePort, BusinessScheduleSnapshot } from '../ports/world-core-port.js';

/**
 * Clearly labeled Development Fixture & In-Memory Adapter for the World Core.
 *
 * Current World Core status:
 * The pre-existing repository does not expose an active, callable HTTP or RPC interface
 * for business operating hours and operational status queries.
 * This adapter serves as a documented development fixture fulfilling the WorldCorePort
 * contract using verified domain fixtures grounded in the Ciudad Juárez regional setting.
 *
 * Connecting a real World Core will simply require writing an HTTP or RPC adapter
 * implementing the same WorldCorePort boundary once the World Core exposes the endpoint.
 */
export class InMemoryWorldCoreAdapter implements WorldCorePort {
  public readonly mode = 'in-memory' as const;
  private schedules: Map<string, BusinessSchedule> = new Map();
  private simulatedWorldTime: string | null = null;

  constructor(initialSchedules?: readonly BusinessSchedule[]) {
    if (initialSchedules && initialSchedules.length > 0) {
      for (const s of initialSchedules) {
        this.schedules.set(s.businessId, s);
      }
    } else {
      this.resetToDefaults();
    }
  }

  /**
   * Resets schedules to the canonical Ciudad Juárez development fixtures.
   */
  public resetToDefaults(): void {
    this.schedules.clear();

    const wingSpot = validateBusinessSchedule({
      businessId: 'restaurant:wing-spot',
      name: 'Wing Spot Gómez Morín',
      timeZone: 'America/Ciudad_Juarez',
      operatingDays: [1, 2, 3, 4, 5, 6], // Mon-Sat (closed Sunday)
      hours: {
        openHour: 9,
        openMinute: 0,
        closeHour: 18,
        closeMinute: 0,
      },
      lastEntryCutoffMinutes: 15,
    });

    const tiaLupe = validateBusinessSchedule({
      businessId: 'restaurant:pasteleria-tia-lupe',
      name: 'Pastelerías La Tía Lupe',
      timeZone: 'America/Ciudad_Juarez',
      operatingDays: [0, 1, 2, 3, 4, 5, 6], // Everyday
      hours: {
        openHour: 8,
        openMinute: 30,
        closeHour: 20,
        closeMinute: 0,
      },
      lastEntryCutoffMinutes: 15,
    });

    const farmaciasBenavides = validateBusinessSchedule({
      businessId: 'pharmacy:farmacias-benavides',
      name: 'Farmacias Benavides',
      timeZone: 'America/Ciudad_Juarez',
      operatingDays: [1, 2, 3, 4, 5, 6], // Mon-Sat (closed Sunday)
      hours: {
        openHour: 7,
        openMinute: 0,
        closeHour: 22,
        closeMinute: 0,
      },
      lastEntryCutoffMinutes: 0,
    });

    const opticaFashion = validateBusinessSchedule({
      businessId: 'optical:optica-fashion',
      name: 'Óptica Fashion',
      timeZone: 'America/Ciudad_Juarez',
      operatingDays: [1, 2, 3, 4, 5, 6], // Mon-Sat (closed Sunday)
      hours: {
        openHour: 9,
        openMinute: 0,
        closeHour: 17,
        closeMinute: 0,
      },
      lastEntryCutoffMinutes: 0,
    });

    this.schedules.set(wingSpot.businessId, wingSpot);
    this.schedules.set(tiaLupe.businessId, tiaLupe);
    this.schedules.set(farmaciasBenavides.businessId, farmaciasBenavides);
    this.schedules.set(opticaFashion.businessId, opticaFashion);
    this.simulatedWorldTime = null;
  }

  /**
   * Allows setting a simulated authoritative world time for testing or demo purposes.
   */
  public setSimulatedWorldTime(isoString: string | null): void {
    this.simulatedWorldTime = isoString;
  }

  /**
   * Registers or updates a schedule fixture in memory.
   */
  public registerSchedule(schedule: BusinessSchedule): void {
    this.schedules.set(schedule.businessId, schedule);
  }

  /**
   * Lists all currently registered business IDs.
   */
  public listBusinessIds(): BusinessId[] {
    return Array.from(this.schedules.keys()).map((id) => createBusinessId(id));
  }

  public async getBusinessSchedule(businessId: BusinessId): Promise<BusinessSchedule | null> {
    const found = this.schedules.get(businessId);
    return found ?? null;
  }

  public async getCurrentWorldTime(): Promise<string> {
    if (this.simulatedWorldTime) {
      return this.simulatedWorldTime;
    }
    return new Date().toISOString();
  }

  /**
   * Health probe for in-memory development fixture.
   * Reports fixture status with zero network latency.
   */
  public async probeHealth(): Promise<{ connected: boolean; latencyMs: number }> {
    return { connected: true, latencyMs: 0 };
  }

  public async getBusinessOperationalStatus(
    businessId: BusinessId,
    evaluatedAt?: Date | string | number
  ): Promise<BusinessOperationalStatus | null> {
    const snapshot = await this.getBusinessScheduleSnapshot(businessId, evaluatedAt);
    return snapshot ? snapshot.operationalStatus : null;
  }

  public async getBusinessScheduleSnapshot(
    businessId: BusinessId,
    evaluatedAt?: Date | string | number
  ): Promise<BusinessScheduleSnapshot | null> {
    const schedule = await this.getBusinessSchedule(businessId);
    if (!schedule) {
      return null;
    }

    const effectiveTime =
      evaluatedAt !== undefined
        ? new Date(evaluatedAt).toISOString()
        : await this.getCurrentWorldTime();

    const operationalStatus = evaluateBusinessOperationalStatus(schedule, effectiveTime);

    return Object.freeze({
      businessId,
      businessName: schedule.name,
      timeZone: schedule.timeZone,
      worldTime: effectiveTime,
      schedule,
      operationalStatus,
    });
  }
}
