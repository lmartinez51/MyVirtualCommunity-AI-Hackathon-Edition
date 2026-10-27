import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  deriveCivilTime,
  InvalidTimestampError,
  InvalidTimezoneError,
  validateIanaTimeZone,
} from '../../src/domain/civil-time.js';

describe('CivilTime Domain Projection', () => {
  it('correctly projects UTC instant to America/Ciudad_Juarez', () => {
    // 2026-10-26T20:30:00.000Z
    // Ciudad Juárez in October: UTC-06:00
    // Expected local civil time: 2026-10-26 14:30:00 (Monday = 1)
    const utcIso = '2026-10-26T20:30:00.000Z';
    const civil = deriveCivilTime(utcIso, 'America/Ciudad_Juarez');

    assert.equal(civil.year, 2026);
    assert.equal(civil.month, 10);
    assert.equal(civil.day, 26);
    assert.equal(civil.hour, 14);
    assert.equal(civil.minute, 30);
    assert.equal(civil.second, 0);
    assert.equal(civil.millisecond, 0);
    assert.equal(civil.dayOfWeek, 1); // Monday
    assert.equal(civil.timeZone, 'America/Ciudad_Juarez');
    assert.equal(civil.utcOffsetMinutes, -360);
    assert.equal(civil.isoCivilString, '2026-10-26T14:30:00.000-06:00');
  });

  it('correctly handles midnight boundary in local timezone', () => {
    // 2026-10-27T05:59:00.000Z in UTC-06:00 is 2026-10-26 23:59:00
    const civil1 = deriveCivilTime('2026-10-27T05:59:00.000Z', 'America/Ciudad_Juarez');
    assert.equal(civil1.day, 26);
    assert.equal(civil1.hour, 23);
    assert.equal(civil1.minute, 59);

    // 2026-10-27T06:00:00.000Z in UTC-06:00 is 2026-10-27 00:00:00 (Tuesday = 2)
    const civil2 = deriveCivilTime('2026-10-27T06:00:00.000Z', 'America/Ciudad_Juarez');
    assert.equal(civil2.day, 27);
    assert.equal(civil2.hour, 0);
    assert.equal(civil2.minute, 0);
    assert.equal(civil2.dayOfWeek, 2);
  });

  it('accepts Date objects and numeric epoch timestamps', () => {
    const epochMs = 1793046600000; // specific timestamp
    const dateObj = new Date(epochMs);

    const fromDate = deriveCivilTime(dateObj, 'America/Ciudad_Juarez');
    const fromNum = deriveCivilTime(epochMs, 'America/Ciudad_Juarez');

    assert.deepEqual(fromDate, fromNum);
  });

  it('throws InvalidTimezoneError for nonexistent or malformed timezones', () => {
    assert.throws(
      () => validateIanaTimeZone('Mars/Olympus_Mons'),
      InvalidTimezoneError
    );
    assert.throws(
      () => deriveCivilTime('2026-10-26T12:00:00Z', 'Invalid/Timezone'),
      InvalidTimezoneError
    );
  });

  it('throws InvalidTimestampError for invalid timestamps', () => {
    assert.throws(
      () => deriveCivilTime('not-a-date', 'America/Ciudad_Juarez'),
      InvalidTimestampError
    );
    assert.throws(
      () => deriveCivilTime('', 'America/Ciudad_Juarez'),
      InvalidTimestampError
    );
    assert.throws(
      () => deriveCivilTime(NaN, 'America/Ciudad_Juarez'),
      InvalidTimestampError
    );
  });
});
