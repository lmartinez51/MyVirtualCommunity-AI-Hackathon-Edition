import { existsSync, mkdirSync, readFileSync, writeFileSync, renameSync } from 'node:fs';
import { dirname } from 'node:path';
import { BusinessId, createBusinessId } from '../domain/business-id.js';
import { BusinessSchedule, validateBusinessSchedule } from '../domain/business-schedule.js';
import {
  BusinessOperationalStatus,
  evaluateBusinessOperationalStatus,
} from '../domain/operational-status.js';
import {
  CitizenReportRecord,
  normalizeCitizenReportRecord,
} from '../domain/citizen-report.js';
import {
  NPC_SCHEDULE_ROSTER,
  evaluateNpcActivity,
  evaluateRosterActivity,
  NpcActivitySnapshot,
  NpcRosterActivitySnapshot,
} from '../domain/npc-activity.js';
import {
  WorldEnvironmentSnapshot,
  evaluateWorldEnvironment,
} from '../domain/time-of-day.js';
import {
  NPC_MOVEMENT_PROFILES,
  evaluateNpcMovement,
  evaluateRosterMovement,
  NpcMovementSnapshot,
  NpcRosterMovementSnapshot,
} from '../domain/npc-waypoint-movement.js';
import {
  EvaluateAutonomousMovementOptions,
  NpcInteractionPauseInterval,
} from '../domain/npc-autonomous-locomotion.js';
import { MAX_INTERACTION_DISTANCE_METERS } from '../domain/npc-interaction-validation.js';
import {
  evaluateNpcInteractionContext,
  evaluateRosterInteractionContext,
  NpcInteractionContext,
  NpcRosterInteractionContext,
} from '../domain/npc-interaction-context.js';
import {
  evaluateRosterSocialAwareness,
  NpcRosterSocialAwarenessSnapshot,
  NpcSocialAwarenessContext,
} from '../domain/npc-social-awareness.js';
import {
  evaluateRosterSocialInteractions,
  NpcRosterSocialInteractionsSnapshot,
  NpcSocialInteractionParticipation,
} from '../domain/npc-social-interaction.js';
import {
  DEFAULT_PLAYER_ID,
  PlayerMovementIntent,
  PlayerMovementStepResult,
  PlayerSpatialState,
  UnauthorizedPlayerIdentityError,
  applyAuthoritativePlayerMovement,
  createInitialPlayerState,
} from '../domain/player-movement.js';
import {
  BorderLaneBarriersRoster,
  BorderLaneBarrierSnapshot,
  LaneBarrierId,
  BarrierPhysicalState,
  createDefaultBorderLaneBarriers,
  updateLaneBarrierStatus,
  isValidLaneId,
  isValidBarrierState,
} from '../domain/border-lane-barriers.js';
import { WorldCorePort, BusinessScheduleSnapshot } from '../ports/world-core-port.js';

export interface InMemoryWorldCoreOptions {
  readonly initialSchedules?: readonly BusinessSchedule[];
  readonly persistenceFilePath?: string | null;
  readonly persistencePath?: string | null;
  readonly nowFn?: () => number;
  readonly initialPauseIntervals?: Readonly<Record<string, readonly NpcInteractionPauseInterval[]>>;
}

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
  private citizenReports: Map<string, CitizenReportRecord> = new Map();
  private persistenceFilePath: string | null = null;
  private laneBarriers: BorderLaneBarriersRoster = createDefaultBorderLaneBarriers();
  private players: Map<string, PlayerSpatialState> = new Map();
  private interactionPauseIntervals: Map<string, NpcInteractionPauseInterval[]> = new Map();
  private readonly nowFn: () => number;

  constructor(optionsOrSchedules?: readonly BusinessSchedule[] | InMemoryWorldCoreOptions) {
    let schedules: readonly BusinessSchedule[] | undefined;
    let customNowFn: (() => number) | undefined;
    let initialPauses: Readonly<Record<string, readonly NpcInteractionPauseInterval[]>> | undefined;
    if (Array.isArray(optionsOrSchedules)) {
      schedules = optionsOrSchedules;
    } else if (optionsOrSchedules && typeof optionsOrSchedules === 'object') {
      const opts = optionsOrSchedules as InMemoryWorldCoreOptions;
      schedules = opts.initialSchedules;
      this.persistenceFilePath = opts.persistenceFilePath ?? opts.persistencePath ?? null;
      customNowFn = opts.nowFn;
      initialPauses = opts.initialPauseIntervals;
    }
    this.nowFn = customNowFn ?? (() => Date.now());

    if (schedules && schedules.length > 0) {
      for (const s of schedules) {
        this.schedules.set(s.businessId, s);
      }
      this.players.set(DEFAULT_PLAYER_ID, createInitialPlayerState(DEFAULT_PLAYER_ID, this.nowFn()));
    } else {
      this.resetToDefaults();
    }

    if (initialPauses) {
      for (const [npcId, list] of Object.entries(initialPauses)) {
        if (Array.isArray(list)) {
          this.interactionPauseIntervals.set(npcId, list.map((item) => Object.freeze({ ...item })));
        }
      }
    }

    this.loadReportsFromDisk();
  }

  private loadReportsFromDisk(): void {
    if (!this.persistenceFilePath) return;
    try {
      if (existsSync(this.persistenceFilePath)) {
        const raw = readFileSync(this.persistenceFilePath, 'utf-8');
        const list = JSON.parse(raw);
        if (Array.isArray(list)) {
          for (const item of list) {
            if (item && typeof item.reportId === 'string') {
              const normalized = normalizeCitizenReportRecord(item);
              this.citizenReports.set(normalized.reportId, normalized);
            }
          }
        }
      }
    } catch {
      // Graceful fallback to memory on read error
    }
  }

  private saveReportsToDisk(): void {
    if (!this.persistenceFilePath) return;
    try {
      const dir = dirname(this.persistenceFilePath);
      if (!existsSync(dir)) {
        mkdirSync(dir, { recursive: true });
      }
      const data = JSON.stringify(Array.from(this.citizenReports.values()), null, 2);
      const tempPath = `${this.persistenceFilePath}.tmp.${Date.now()}`;
      writeFileSync(tempPath, data, 'utf-8');
      renameSync(tempPath, this.persistenceFilePath);
    } catch {
      // Gracefully ignore write errors
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

    const farmaciaJuarez = validateBusinessSchedule({
      businessId: 'pharmacy:farmacia-juarez',
      name: 'Farmacia Juárez',
      timeZone: 'America/Ciudad_Juarez',
      operatingDays: [0, 1, 2, 3, 4, 5], // Mon-Fri & Sunday (closed Saturday)
      hours: {
        openHour: 8,
        openMinute: 0,
        closeHour: 20,
        closeMinute: 0,
      },
      hoursByDay: {
        0: { openHour: 8, openMinute: 0, closeHour: 16, closeMinute: 0 }, // Sunday 8AM-4PM
      },
      lastEntryCutoffMinutes: 0,
    });

    const dentalMexico = validateBusinessSchedule({
      businessId: 'dental:dental-mexico',
      name: 'Dental México',
      timeZone: 'America/Ciudad_Juarez',
      operatingDays: [1, 2, 3, 4, 5, 6], // Mon-Sat (closed Sunday)
      hours: {
        openHour: 9,
        openMinute: 0,
        closeHour: 17,
        closeMinute: 0,
      },
      hoursByDay: {
        6: { openHour: 9, openMinute: 0, closeHour: 15, closeMinute: 0 }, // Saturday 9AM-3PM
      },
      lastEntryCutoffMinutes: 0,
    });

    const casaCambioJuarezSur = validateBusinessSchedule({
      businessId: 'exchange:casa-cambio-juarez-sur',
      name: 'Casa de Cambio (Av. Juárez)',
      timeZone: 'America/Ciudad_Juarez',
      operatingDays: [0, 1, 2, 3, 4, 5, 6], // 24/7
      hours: {
        openHour: 0,
        openMinute: 0,
        closeHour: 23,
        closeMinute: 59,
      },
      is24Hours: true,
      lastEntryCutoffMinutes: 0,
    });

    this.schedules.set(wingSpot.businessId, wingSpot);
    this.schedules.set(tiaLupe.businessId, tiaLupe);
    this.schedules.set(farmaciasBenavides.businessId, farmaciasBenavides);
    this.schedules.set(opticaFashion.businessId, opticaFashion);
    this.schedules.set(farmaciaJuarez.businessId, farmaciaJuarez);
    this.schedules.set(dentalMexico.businessId, dentalMexico);
    this.schedules.set(casaCambioJuarezSur.businessId, casaCambioJuarezSur);
    this.simulatedWorldTime = null;
    this.players.clear();
    this.players.set(DEFAULT_PLAYER_ID, createInitialPlayerState(DEFAULT_PLAYER_ID, this.nowFn()));
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

    let effectiveTime: string;
    if (evaluatedAt !== undefined && evaluatedAt !== null && String(evaluatedAt).trim() !== '') {
      const parsedDate = new Date(evaluatedAt);
      if (!Number.isNaN(parsedDate.getTime())) {
        effectiveTime = parsedDate.toISOString();
      } else {
        effectiveTime = await this.getCurrentWorldTime();
      }
    } else {
      effectiveTime = await this.getCurrentWorldTime();
    }

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

  public async registerCitizenReport(report: CitizenReportRecord): Promise<CitizenReportRecord> {
    const normalized = normalizeCitizenReportRecord(report);
    this.citizenReports.set(normalized.reportId, normalized);
    this.saveReportsToDisk();
    return normalized;
  }

  public async getCitizenReport(reportId: string): Promise<CitizenReportRecord | null> {
    return this.citizenReports.get(reportId) ?? null;
  }

  public async listCitizenReports(filter?: { limit?: number; offset?: number }): Promise<readonly CitizenReportRecord[]> {
    const all = Array.from(this.citizenReports.values()).sort(
      (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    );
    const offset = filter?.offset ?? 0;
    const limit = filter?.limit ?? 50;
    return all.slice(offset, offset + limit);
  }

  public async getNpcActivity(
    npcId: string,
    evaluatedAt?: Date | string | number
  ): Promise<NpcActivitySnapshot | null> {
    const profile = NPC_SCHEDULE_ROSTER[npcId];
    if (!profile) return null;
    const effectiveTime =
      evaluatedAt !== undefined
        ? typeof evaluatedAt === 'string'
          ? evaluatedAt
          : new Date(evaluatedAt).toISOString()
        : await this.getCurrentWorldTime();
    return evaluateNpcActivity(profile, effectiveTime);
  }

  public async getNpcRosterActivity(
    evaluatedAt?: Date | string | number
  ): Promise<NpcRosterActivitySnapshot> {
    const effectiveTime =
      evaluatedAt !== undefined
        ? typeof evaluatedAt === 'string'
          ? evaluatedAt
          : new Date(evaluatedAt).toISOString()
        : await this.getCurrentWorldTime();
    return evaluateRosterActivity(NPC_SCHEDULE_ROSTER, effectiveTime);
  }

  public async getWorldEnvironment(
    evaluatedAt?: Date | string | number
  ): Promise<WorldEnvironmentSnapshot> {
    const effectiveTime =
      evaluatedAt !== undefined
        ? typeof evaluatedAt === 'string'
          ? evaluatedAt
          : new Date(evaluatedAt).toISOString()
        : await this.getCurrentWorldTime();
    return evaluateWorldEnvironment(effectiveTime);
  }

  private buildMovementOptions(playerId: string = DEFAULT_PLAYER_ID): EvaluateAutonomousMovementOptions {
    const normalizedPlayerId =
      typeof playerId === 'string' && playerId.trim() !== '' ? playerId.trim() : DEFAULT_PLAYER_ID;
    const player = this.players.get(normalizedPlayerId) ?? null;
    return {
      pauseIntervalsByNpcId: this.getNpcInteractionPauseIntervals(),
      player,
    };
  }

  public getNpcInteractionPauseIntervals(
    npcId?: string
  ): Readonly<Record<string, readonly NpcInteractionPauseInterval[]>> {
    if (typeof npcId === 'string' && npcId.trim() !== '') {
      const key = npcId.trim();
      const list = this.interactionPauseIntervals.get(key) ?? [];
      return Object.freeze({ [key]: Object.freeze([...list]) });
    }
    const out: Record<string, readonly NpcInteractionPauseInterval[]> = {};
    for (const [k, list] of this.interactionPauseIntervals.entries()) {
      out[k] = Object.freeze([...list]);
    }
    return Object.freeze(out);
  }

  public async suspendNpcMovementForInteraction(
    playerId: string,
    npcId: string,
    suspendedAt?: Date | string | number
  ): Promise<NpcMovementSnapshot | null> {
    const cleanNpcId = typeof npcId === 'string' ? npcId.trim() : '';
    const cleanPlayerId =
      typeof playerId === 'string' && playerId.trim() !== '' ? playerId.trim() : DEFAULT_PLAYER_ID;
    const profile = NPC_MOVEMENT_PROFILES[cleanNpcId];
    if (!profile) return null;

    const effectiveTime =
      suspendedAt !== undefined
        ? typeof suspendedAt === 'string'
          ? suspendedAt
          : new Date(suspendedAt).toISOString()
        : await this.getCurrentWorldTime();
    const suspendedAtMs = Date.parse(effectiveTime);

    const currentSnap = evaluateNpcMovement(
      profile,
      effectiveTime,
      this.buildMovementOptions(cleanPlayerId)
    );

    // Release any previous active suspension held by this player on other NPCs
    for (const [otherNpcId, intervals] of this.interactionPauseIntervals.entries()) {
      if (otherNpcId === cleanNpcId) continue;
      const updated = intervals.map((iv) =>
        iv.playerId === cleanPlayerId && iv.endedAtMs === null
          ? Object.freeze({ ...iv, endedAtMs: Math.max(iv.startedAtMs, suspendedAtMs) })
          : iv
      );
      this.interactionPauseIntervals.set(otherNpcId, updated);
    }

    // Only autonomous bridge_patrol and autonomous_business_local modes suspend movement on player interaction
    // (scheduled routine transitions like Carlos's lunch walk remain scheduled_routine_transition).
    if (
      currentSnap.controlMode === 'autonomous_bridge_patrol' ||
      currentSnap.controlMode === 'autonomous_business_local' ||
      currentSnap.controlMode === 'player_interaction_suspended'
    ) {
      const list = [...(this.interactionPauseIntervals.get(cleanNpcId) ?? [])];
      const alreadyActive = list.some(
        (iv) =>
          iv.playerId === cleanPlayerId &&
          iv.startedAtMs <= suspendedAtMs &&
          (iv.endedAtMs === null || suspendedAtMs < iv.endedAtMs)
      );
      if (!alreadyActive) {
        list.push(
          Object.freeze({
            npcId: cleanNpcId,
            playerId: cleanPlayerId,
            startedAtMs: suspendedAtMs,
            endedAtMs: null,
            reason: 'authorized_player_interaction',
          })
        );
        this.interactionPauseIntervals.set(cleanNpcId, list);
      }
    }

    return evaluateNpcMovement(profile, effectiveTime, this.buildMovementOptions(cleanPlayerId));
  }

  public async releaseNpcMovementFromInteraction(
    playerId: string,
    npcId?: string,
    releasedAt?: Date | string | number
  ): Promise<NpcMovementSnapshot | null> {
    const cleanPlayerId =
      typeof playerId === 'string' && playerId.trim() !== '' ? playerId.trim() : DEFAULT_PLAYER_ID;
    const cleanNpcId = typeof npcId === 'string' && npcId.trim() !== '' ? npcId.trim() : null;

    const effectiveTime =
      releasedAt !== undefined
        ? typeof releasedAt === 'string'
          ? releasedAt
          : new Date(releasedAt).toISOString()
        : await this.getCurrentWorldTime();
    const releasedAtMs = Date.parse(effectiveTime);

    let targetNpcId: string | null = cleanNpcId;
    for (const [id, intervals] of this.interactionPauseIntervals.entries()) {
      if (cleanNpcId && id !== cleanNpcId) continue;
      let changed = false;
      const updated = intervals.map((iv) => {
        if (iv.playerId === cleanPlayerId && (iv.endedAtMs === null || iv.endedAtMs > releasedAtMs)) {
          changed = true;
          if (!targetNpcId) targetNpcId = id;
          return Object.freeze({
            ...iv,
            endedAtMs: Math.max(iv.startedAtMs, releasedAtMs),
          });
        }
        return iv;
      });
      if (changed) {
        this.interactionPauseIntervals.set(id, updated);
      }
    }

    if (targetNpcId && NPC_MOVEMENT_PROFILES[targetNpcId]) {
      return evaluateNpcMovement(
        NPC_MOVEMENT_PROFILES[targetNpcId]!,
        effectiveTime,
        this.buildMovementOptions(cleanPlayerId)
      );
    }
    return null;
  }

  public async getNpcMovement(
    npcId: string,
    evaluatedAt?: Date | string | number,
    playerId: string = DEFAULT_PLAYER_ID
  ): Promise<NpcMovementSnapshot | null> {
    const profile = NPC_MOVEMENT_PROFILES[npcId];
    if (!profile) return null;
    const effectiveTime =
      evaluatedAt !== undefined
        ? typeof evaluatedAt === 'string'
          ? evaluatedAt
          : new Date(evaluatedAt).toISOString()
        : await this.getCurrentWorldTime();
    return evaluateNpcMovement(profile, effectiveTime, this.buildMovementOptions(playerId));
  }

  public async getNpcRosterMovement(
    evaluatedAt?: Date | string | number,
    playerId: string = DEFAULT_PLAYER_ID
  ): Promise<NpcRosterMovementSnapshot> {
    const effectiveTime =
      evaluatedAt !== undefined
        ? typeof evaluatedAt === 'string'
          ? evaluatedAt
          : new Date(evaluatedAt).toISOString()
        : await this.getCurrentWorldTime();
    return evaluateRosterMovement(
      NPC_MOVEMENT_PROFILES,
      effectiveTime,
      this.buildMovementOptions(playerId)
    );
  }

  public async getNpcInteractionContext(
    npcId: string,
    evaluatedAt?: Date | string | number,
    playerId: string = DEFAULT_PLAYER_ID,
    activePlayerInteractionNpcId?: string | null
  ): Promise<NpcInteractionContext | null> {
    const effectiveTime =
      evaluatedAt !== undefined
        ? typeof evaluatedAt === 'string'
          ? evaluatedAt
          : new Date(evaluatedAt).toISOString()
        : await this.getCurrentWorldTime();
    const movOpts = this.buildMovementOptions(playerId);
    const baseContext = evaluateNpcInteractionContext(npcId, effectiveTime, movOpts);
    if (!baseContext) return null;

    const awarenessRoster = await this.getNpcRosterSocialAwareness(playerId, effectiveTime);
    const interactionsRoster = await this.getNpcRosterSocialInteractions(
      playerId,
      effectiveTime,
      activePlayerInteractionNpcId
    );
    const awareness = awarenessRoster.npcs[npcId];
    const socialInteraction = interactionsRoster.npcs[npcId];
    return Object.freeze({
      ...baseContext,
      ...(awareness ? { socialAwareness: awareness } : {}),
      ...(socialInteraction ? { socialInteraction } : {}),
    });
  }

  public async getNpcRosterInteractionContext(
    evaluatedAt?: Date | string | number,
    playerId: string = DEFAULT_PLAYER_ID,
    activePlayerInteractionNpcId?: string | null
  ): Promise<NpcRosterInteractionContext> {
    const effectiveTime =
      evaluatedAt !== undefined
        ? typeof evaluatedAt === 'string'
          ? evaluatedAt
          : new Date(evaluatedAt).toISOString()
        : await this.getCurrentWorldTime();
    const normalizedPlayerId = typeof playerId === 'string' && playerId.trim() !== '' ? playerId.trim() : DEFAULT_PLAYER_ID;
    const player = this.players.get(normalizedPlayerId) ?? null;
    const movOpts = this.buildMovementOptions(normalizedPlayerId);
    const interactionRoster = evaluateRosterInteractionContext(effectiveTime, movOpts);
    const movementRoster = evaluateRosterMovement(NPC_MOVEMENT_PROFILES, effectiveTime, movOpts);

    const awarenessRoster = evaluateRosterSocialAwareness({
      evaluatedAt: effectiveTime,
      player,
      interactionRoster,
      movementRoster,
    });
    const interactionsRoster = evaluateRosterSocialInteractions({
      evaluatedAt: effectiveTime,
      interactionRoster,
      movementRoster,
      activePlayerInteractionNpcId,
    });

    const enrichedNpcs: Record<string, NpcInteractionContext> = {};
    const enrichedList: NpcInteractionContext[] = [];
    for (const ctx of interactionRoster.npcsList) {
      const awareness = awarenessRoster.npcs[ctx.npcId];
      const socialInteraction = interactionsRoster.npcs[ctx.npcId];
      const enriched = Object.freeze({
        ...ctx,
        ...(awareness ? { socialAwareness: awareness } : {}),
        ...(socialInteraction ? { socialInteraction } : {}),
      });
      enrichedNpcs[ctx.npcId] = enriched;
      enrichedList.push(enriched);
    }

    return Object.freeze({
      ...interactionRoster,
      npcs: Object.freeze(enrichedNpcs),
      npcsList: Object.freeze(enrichedList),
      socialAwarenessSummary: awarenessRoster.summary,
      activeSocialPairs: awarenessRoster.activeSocialPairs,
      activeSocialInteractions: interactionsRoster.sessions,
      socialInteractionsSummary: interactionsRoster.summary,
      playerPosition: awarenessRoster.playerPosition,
    });
  }

  public async getNpcSocialAwareness(
    npcId: string,
    playerId: string = DEFAULT_PLAYER_ID,
    evaluatedAt?: Date | string | number
  ): Promise<NpcSocialAwarenessContext | null> {
    if (typeof npcId !== 'string' || npcId.trim() === '') {
      return null;
    }
    const roster = await this.getNpcRosterSocialAwareness(playerId, evaluatedAt);
    return roster.npcs[npcId.trim()] ?? null;
  }

  public async getNpcRosterSocialAwareness(
    playerId: string = DEFAULT_PLAYER_ID,
    evaluatedAt?: Date | string | number
  ): Promise<NpcRosterSocialAwarenessSnapshot> {
    const effectiveTime =
      evaluatedAt !== undefined
        ? typeof evaluatedAt === 'string'
          ? evaluatedAt
          : new Date(evaluatedAt).toISOString()
        : await this.getCurrentWorldTime();
    const normalizedPlayerId =
      typeof playerId === 'string' && playerId.trim() !== '' ? playerId.trim() : DEFAULT_PLAYER_ID;
    const player = this.players.get(normalizedPlayerId) ?? null;
    const movOpts = this.buildMovementOptions(normalizedPlayerId);

    return evaluateRosterSocialAwareness({
      evaluatedAt: effectiveTime,
      player,
      interactionRoster: evaluateRosterInteractionContext(effectiveTime, movOpts),
      movementRoster: evaluateRosterMovement(NPC_MOVEMENT_PROFILES, effectiveTime, movOpts),
    });
  }

  public async getNpcSocialInteraction(
    npcId: string,
    playerId: string = DEFAULT_PLAYER_ID,
    evaluatedAt?: Date | string | number,
    activePlayerInteractionNpcId?: string | null
  ): Promise<NpcSocialInteractionParticipation | null> {
    if (typeof npcId !== 'string' || npcId.trim() === '') {
      return null;
    }
    const roster = await this.getNpcRosterSocialInteractions(
      playerId,
      evaluatedAt,
      activePlayerInteractionNpcId
    );
    return roster.npcs[npcId.trim()] ?? null;
  }

  public async getNpcRosterSocialInteractions(
    playerId: string = DEFAULT_PLAYER_ID,
    evaluatedAt?: Date | string | number,
    activePlayerInteractionNpcId?: string | null
  ): Promise<NpcRosterSocialInteractionsSnapshot> {
    const effectiveTime =
      evaluatedAt !== undefined
        ? typeof evaluatedAt === 'string'
          ? evaluatedAt
          : new Date(evaluatedAt).toISOString()
        : await this.getCurrentWorldTime();
    const movOpts = this.buildMovementOptions(playerId);

    return evaluateRosterSocialInteractions({
      evaluatedAt: effectiveTime,
      interactionRoster: evaluateRosterInteractionContext(effectiveTime, movOpts),
      movementRoster: evaluateRosterMovement(NPC_MOVEMENT_PROFILES, effectiveTime, movOpts),
      activePlayerInteractionNpcId,
    });
  }

  public async getBorderLaneBarriers(
    evaluatedAt?: Date | string | number
  ): Promise<BorderLaneBarriersRoster> {
    const effectiveTime =
      evaluatedAt !== undefined
        ? typeof evaluatedAt === 'string'
          ? evaluatedAt
          : new Date(evaluatedAt).toISOString()
        : await this.getCurrentWorldTime();

    return {
      ...this.laneBarriers,
      worldTime: effectiveTime,
    };
  }

  public async setBorderLaneBarrierStatus(
    laneId: LaneBarrierId,
    status: BarrierPhysicalState,
    reason?: string
  ): Promise<BorderLaneBarrierSnapshot> {
    if (!isValidLaneId(laneId)) {
      throw new Error(`Invalid lane identifier: ${String(laneId)}`);
    }
    if (!isValidBarrierState(status)) {
      throw new Error(`Invalid barrier status: ${String(status)}`);
    }
    const worldTime = await this.getCurrentWorldTime();
    this.laneBarriers = updateLaneBarrierStatus(this.laneBarriers, laneId, status, reason, worldTime);
    return this.laneBarriers.lanes[laneId];
  }

  public async getPlayerState(playerId: string): Promise<PlayerSpatialState | null> {
    if (!playerId || typeof playerId !== 'string') return null;
    return this.players.get(playerId.trim()) ?? null;
  }

  public async applyPlayerMovement(
    playerId: string,
    movementIntent: PlayerMovementIntent,
    serverTimeMs?: number
  ): Promise<PlayerMovementStepResult> {
    const normalizedId = typeof playerId === 'string' ? playerId.trim() : '';
    const existing = normalizedId ? this.players.get(normalizedId) ?? null : null;
    if (!existing) {
      throw new UnauthorizedPlayerIdentityError(normalizedId || String(playerId));
    }

    const effectiveNowMs =
      typeof serverTimeMs === 'number' && Number.isFinite(serverTimeMs) ? serverTimeMs : this.nowFn();

    const stepResult = applyAuthoritativePlayerMovement(existing, movementIntent, effectiveNowMs);
    if (stepResult.accepted) {
      this.players.set(normalizedId, stepResult.player);
      const currentWorldIso = await this.getCurrentWorldTime();
      for (const [npcId, intervals] of this.interactionPauseIntervals.entries()) {
        const hasOpen = intervals.some((iv) => iv.playerId === normalizedId && iv.endedAtMs === null);
        if (!hasOpen) continue;
        const prof = NPC_MOVEMENT_PROFILES[npcId];
        if (!prof) continue;
        const npcSnap = evaluateNpcMovement(prof, currentWorldIso, this.buildMovementOptions(normalizedId));
        const dist = Math.hypot(
          stepResult.player.position.x - npcSnap.position.x,
          stepResult.player.position.y - npcSnap.position.y,
          stepResult.player.position.z - npcSnap.position.z
        );
        if (dist > MAX_INTERACTION_DISTANCE_METERS + 1.5) {
          await this.releaseNpcMovementFromInteraction(normalizedId, npcId, currentWorldIso);
        }
      }
    }
    return stepResult;
  }
}
