import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  InvalidScheduleError,
  validateBusinessSchedule,
} from '../../src/domain/business-schedule.js';

describe('BusinessSchedule Validation and Contract', () => {
  const validPayload = {
    businessId: 'restaurant:wing-spot',
    name: 'Wing Spot Gómez Morín',
    timeZone: 'America/Ciudad_Juarez',
    operatingDays: [1, 2, 3, 4, 5, 6],
    hours: {
      openHour: 9,
      openMinute: 0,
      closeHour: 18,
      closeMinute: 0,
    },
    lastEntryCutoffMinutes: 15,
  };

  it('successfully validates a well-formed schedule', () => {
    const schedule = validateBusinessSchedule(validPayload);
    assert.equal(schedule.businessId, 'restaurant:wing-spot');
    assert.equal(schedule.name, 'Wing Spot Gómez Morín');
    assert.equal(schedule.timeZone, 'America/Ciudad_Juarez');
    assert.deepEqual(schedule.operatingDays, [1, 2, 3, 4, 5, 6]);
    assert.equal(schedule.hours.openHour, 9);
    assert.equal(schedule.hours.closeHour, 18);
    assert.equal(schedule.lastEntryCutoffMinutes, 15);
  });

  it('rejects null, non-objects, or empty payloads', () => {
    assert.throws(() => validateBusinessSchedule(null), InvalidScheduleError);
    assert.throws(() => validateBusinessSchedule(undefined), InvalidScheduleError);
    assert.throws(() => validateBusinessSchedule('string'), InvalidScheduleError);
    assert.throws(() => validateBusinessSchedule({}), InvalidScheduleError);
  });

  it('rejects missing or malformed businessId', () => {
    assert.throws(
      () => validateBusinessSchedule({ ...validPayload, businessId: '' }),
      InvalidScheduleError
    );
    assert.throws(
      () => validateBusinessSchedule({ ...validPayload, businessId: 'invalid-no-namespace' }),
      InvalidScheduleError
    );
  });

  it('rejects missing or invalid timezone', () => {
    assert.throws(
      () => validateBusinessSchedule({ ...validPayload, timeZone: '' }),
      InvalidScheduleError
    );
    assert.throws(
      () => validateBusinessSchedule({ ...validPayload, timeZone: 'Invalid/City_Region' }),
      InvalidScheduleError
    );
  });

  it('rejects empty operatingDays or days out of range [0-6]', () => {
    assert.throws(
      () => validateBusinessSchedule({ ...validPayload, operatingDays: [] }),
      InvalidScheduleError
    );
    assert.throws(
      () => validateBusinessSchedule({ ...validPayload, operatingDays: [1, 2, 7] }),
      InvalidScheduleError
    );
    assert.throws(
      () => validateBusinessSchedule({ ...validPayload, operatingDays: [-1, 2, 3] }),
      InvalidScheduleError
    );
  });

  it('rejects invalid or backwards operating hours', () => {
    // Open >= Close
    assert.throws(
      () =>
        validateBusinessSchedule({
          ...validPayload,
          hours: { openHour: 18, openMinute: 0, closeHour: 9, closeMinute: 0 },
        }),
      InvalidScheduleError
    );

    // Open == Close
    assert.throws(
      () =>
        validateBusinessSchedule({
          ...validPayload,
          hours: { openHour: 10, openMinute: 0, closeHour: 10, closeMinute: 0 },
        }),
      InvalidScheduleError
    );

    // Out-of-bounds hour or minute
    assert.throws(
      () =>
        validateBusinessSchedule({
          ...validPayload,
          hours: { openHour: 25, openMinute: 0, closeHour: 26, closeMinute: 0 },
        }),
      InvalidScheduleError
    );
    assert.throws(
      () =>
        validateBusinessSchedule({
          ...validPayload,
          hours: { openHour: 9, openMinute: 65, closeHour: 18, closeMinute: 0 },
        }),
      InvalidScheduleError
    );
  });

  it('rejects cutoff minutes greater than or equal to total operating duration', () => {
    // 9:00 to 18:00 is 9 hours = 540 minutes.
    // Cutoff = 540 minutes is invalid (must be strictly less than duration)
    assert.throws(
      () =>
        validateBusinessSchedule({
          ...validPayload,
          lastEntryCutoffMinutes: 540,
        }),
      InvalidScheduleError
    );

    // Negative cutoff
    assert.throws(
      () =>
        validateBusinessSchedule({
          ...validPayload,
          lastEntryCutoffMinutes: -5,
        }),
      InvalidScheduleError
    );
  });

  it('validates closureOverrides date format (YYYY-MM-DD)', () => {
    const withValidOverrides = validateBusinessSchedule({
      ...validPayload,
      closureOverrides: ['2026-12-25', '2026-01-01'],
    });
    assert.deepEqual(withValidOverrides.closureOverrides, ['2026-12-25', '2026-01-01']);

    assert.throws(
      () =>
        validateBusinessSchedule({
          ...validPayload,
          closureOverrides: ['25-12-2026'], // wrong format
        }),
      InvalidScheduleError
    );
  });
});
