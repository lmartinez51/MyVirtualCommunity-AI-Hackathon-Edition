/**
 * Border Lane Toll Barriers Domain Model (Paso del Norte Bridge Entrance)
 *
 * Models the 4 vehicular toll barrier gates ("plumas de peaje") across
 * Arcos 1, 2, 3, and 4 at the Paso del Norte International Bridge.
 *
 * Provides authoritative state definitions, validation, and immutability
 * for World Core synchronization and external event management.
 */

export type LaneBarrierId = 'lane-1' | 'lane-2' | 'lane-3' | 'lane-4';
export type BarrierPhysicalState = 'open' | 'closed';

export const CANONICAL_LANE_IDS: readonly LaneBarrierId[] = [
  'lane-1',
  'lane-2',
  'lane-3',
  'lane-4',
] as const;

export interface BorderLaneBarrierSnapshot {
  readonly laneId: LaneBarrierId;
  readonly archNumber: number;
  readonly name: string;
  readonly status: BarrierPhysicalState;
  readonly angleDegrees: number;
  readonly reason?: string;
  readonly updatedAt: string;
}

export interface BorderLaneBarriersRoster {
  readonly worldTime: string;
  readonly lanes: Record<LaneBarrierId, BorderLaneBarrierSnapshot>;
}

export const DEFAULT_OPEN_ANGLE = 85;
export const DEFAULT_CLOSED_ANGLE = 0;

export function isValidLaneId(id: unknown): id is LaneBarrierId {
  return typeof id === 'string' && (CANONICAL_LANE_IDS as readonly string[]).includes(id);
}

export function isValidBarrierState(state: unknown): state is BarrierPhysicalState {
  return state === 'open' || state === 'closed';
}

export function createDefaultBorderLaneBarriers(worldTime?: string): BorderLaneBarriersRoster {
  const ts = worldTime ?? new Date().toISOString();
  return {
    worldTime: ts,
    lanes: {
      'lane-1': {
        laneId: 'lane-1',
        archNumber: 1,
        name: 'Arco 1 (Caseta 1)',
        status: 'open',
        angleDegrees: DEFAULT_OPEN_ANGLE,
        reason: 'Flujo vehicular regular',
        updatedAt: ts,
      },
      'lane-2': {
        laneId: 'lane-2',
        archNumber: 2,
        name: 'Arco 2 (Caseta 2)',
        status: 'open',
        angleDegrees: DEFAULT_OPEN_ANGLE,
        reason: 'Flujo vehicular regular',
        updatedAt: ts,
      },
      'lane-3': {
        laneId: 'lane-3',
        archNumber: 3,
        name: 'Arco 3 (Caseta 3)',
        status: 'open',
        angleDegrees: DEFAULT_OPEN_ANGLE,
        reason: 'Flujo vehicular regular',
        updatedAt: ts,
      },
      'lane-4': {
        laneId: 'lane-4',
        archNumber: 4,
        name: 'Arco 4 (Caseta 4 - Acceso Gardenias)',
        status: 'open',
        angleDegrees: DEFAULT_OPEN_ANGLE,
        reason: 'Acceso exclusivo Calle Gardenias',
        updatedAt: ts,
      },
    },
  };
}

export function updateLaneBarrierStatus(
  roster: BorderLaneBarriersRoster,
  laneId: LaneBarrierId,
  newStatus: BarrierPhysicalState,
  reason?: string,
  worldTime?: string
): BorderLaneBarriersRoster {
  const ts = worldTime ?? new Date().toISOString();
  const currentLane = roster.lanes[laneId];

  const updatedLane: BorderLaneBarrierSnapshot = {
    ...currentLane,
    status: newStatus,
    angleDegrees: newStatus === 'open' ? DEFAULT_OPEN_ANGLE : DEFAULT_CLOSED_ANGLE,
    reason: reason ?? (newStatus === 'open' ? 'Paso habilitado' : 'Carril temporalmente cerrado'),
    updatedAt: ts,
  };

  return {
    worldTime: ts,
    lanes: {
      ...roster.lanes,
      [laneId]: updatedLane,
    },
  };
}

