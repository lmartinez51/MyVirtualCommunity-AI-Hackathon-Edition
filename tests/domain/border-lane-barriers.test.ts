import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  CANONICAL_LANE_IDS,
  DEFAULT_CLOSED_ANGLE,
  DEFAULT_OPEN_ANGLE,
  createDefaultBorderLaneBarriers,
  isValidBarrierState,
  isValidLaneId,
  updateLaneBarrierStatus,
} from '../../src/domain/border-lane-barriers.js';

describe('Border Lane Toll Barriers Domain Model', () => {
  it('validates canonical lane IDs correctly', () => {
    assert.equal(isValidLaneId('lane-1'), true);
    assert.equal(isValidLaneId('lane-2'), true);
    assert.equal(isValidLaneId('lane-3'), true);
    assert.equal(isValidLaneId('lane-4'), true);
    assert.equal(isValidLaneId('lane-5'), false);
    assert.equal(isValidLaneId(''), false);
    assert.equal(isValidLaneId(null), false);
  });

  it('validates barrier states correctly', () => {
    assert.equal(isValidBarrierState('open'), true);
    assert.equal(isValidBarrierState('closed'), true);
    assert.equal(isValidBarrierState('pending'), false);
    assert.equal(isValidBarrierState(123), false);
  });

  it('creates default roster with all 4 lanes in open state', () => {
    const fixedTime = '2026-11-10T12:00:00.000Z';
    const roster = createDefaultBorderLaneBarriers(fixedTime);

    assert.equal(roster.worldTime, fixedTime);
    assert.equal(Object.keys(roster.lanes).length, 4);

    for (const laneId of CANONICAL_LANE_IDS) {
      const lane = roster.lanes[laneId];
      assert.equal(lane.laneId, laneId);
      assert.equal(lane.status, 'open');
      assert.equal(lane.angleDegrees, DEFAULT_OPEN_ANGLE);
      assert.equal(lane.updatedAt, fixedTime);
    }
  });

  it('updates lane barrier status immutably when closed and re-opened', () => {
    const initial = createDefaultBorderLaneBarriers('2026-11-10T10:00:00.000Z');

    // Close Lane 1
    const closedLane1 = updateLaneBarrierStatus(
      initial,
      'lane-1',
      'closed',
      'Operativo de inspección',
      '2026-11-10T10:15:00.000Z'
    );

    assert.equal(closedLane1.lanes['lane-1'].status, 'closed');
    assert.equal(closedLane1.lanes['lane-1'].angleDegrees, DEFAULT_CLOSED_ANGLE);
    assert.equal(closedLane1.lanes['lane-1'].reason, 'Operativo de inspección');
    assert.equal(closedLane1.lanes['lane-1'].updatedAt, '2026-11-10T10:15:00.000Z');

    // Other lanes remain unaffected
    assert.equal(closedLane1.lanes['lane-2'].status, 'open');
    assert.equal(closedLane1.lanes['lane-3'].status, 'open');
    assert.equal(closedLane1.lanes['lane-4'].status, 'open');

    // Re-open Lane 1
    const reopenedLane1 = updateLaneBarrierStatus(
      closedLane1,
      'lane-1',
      'open',
      'Inspección finalizada',
      '2026-11-10T10:30:00.000Z'
    );

    assert.equal(reopenedLane1.lanes['lane-1'].status, 'open');
    assert.equal(reopenedLane1.lanes['lane-1'].angleDegrees, DEFAULT_OPEN_ANGLE);
    assert.equal(reopenedLane1.lanes['lane-1'].reason, 'Inspección finalizada');
  });
});
