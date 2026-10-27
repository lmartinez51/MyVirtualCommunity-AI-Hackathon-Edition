import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  deriveCivilTime,
  InvalidTimestampError,
  InvalidTimezoneError,
  parseLocalCivilTimeToUtc,
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

describe('parseLocalCivilTimeToUtc Projection & Validation', () => {
  it('correctly converts valid civil date-time in America/Ciudad_Juarez to UTC instant', () => {
    // Saturday 17:40 local in Ciudad Juárez (UTC-6) -> 23:40 UTC
    const r1 = parseLocalCivilTimeToUtc('2026-10-24T17:40', 'America/Ciudad_Juarez');
    assert.equal(r1.success, true);
    assert.equal(r1.utcInstant?.toISOString(), '2026-10-24T23:40:00.000Z');

    // Saturday 17:50 local (cutoff) -> 23:50 UTC
    const r2 = parseLocalCivilTimeToUtc('2026-10-24T17:50', 'America/Ciudad_Juarez');
    assert.equal(r2.success, true);
    assert.equal(r2.utcInstant?.toISOString(), '2026-10-24T23:50:00.000Z');

    // Saturday 18:05 local (closed) -> 2026-10-25T00:05:00.000Z
    const r3 = parseLocalCivilTimeToUtc('2026-10-24T18:05', 'America/Ciudad_Juarez');
    assert.equal(r3.success, true);
    assert.equal(r3.utcInstant?.toISOString(), '2026-10-25T00:05:00.000Z');

    // Sunday 12:00 local -> 18:00 UTC
    const r4 = parseLocalCivilTimeToUtc('2026-10-25T12:00', 'America/Ciudad_Juarez');
    assert.equal(r4.success, true);
    assert.equal(r4.utcInstant?.toISOString(), '2026-10-25T18:00:00.000Z');
  });

  it('correctly handles ISO strings with explicit offsets or UTC Z', () => {
    const r1 = parseLocalCivilTimeToUtc('2026-10-24T23:50:00.000Z');
    assert.equal(r1.success, true);
    assert.equal(r1.utcInstant?.toISOString(), '2026-10-24T23:50:00.000Z');

    const r2 = parseLocalCivilTimeToUtc('2026-10-24T17:50:00-06:00');
    assert.equal(r2.success, true);
    assert.equal(r2.utcInstant?.toISOString(), '2026-10-24T23:50:00.000Z');
  });

  it('rejects invalid calendar dates (non-leap Feb 29, April 31, etc.)', () => {
    // 2026 is not a leap year
    const leapFail = parseLocalCivilTimeToUtc('2026-02-29T12:00', 'America/Ciudad_Juarez');
    assert.equal(leapFail.success, false);
    assert.match(leapFail.error ?? '', /Calendar date does not exist/);

    // 2028 is a leap year
    const leapPass = parseLocalCivilTimeToUtc('2028-02-29T12:00', 'America/Ciudad_Juarez');
    assert.equal(leapPass.success, true);

    // April 31 does not exist
    const april31 = parseLocalCivilTimeToUtc('2026-04-31T12:00', 'America/Ciudad_Juarez');
    assert.equal(april31.success, false);
    assert.match(april31.error ?? '', /Calendar date does not exist/);
  });

  it('rejects malformed date-time formats and out-of-range hours/minutes', () => {
    assert.equal(parseLocalCivilTimeToUtc('not-a-date').success, false);
    assert.equal(parseLocalCivilTimeToUtc('').success, false);
    assert.equal(parseLocalCivilTimeToUtc('2026-10-24T25:00').success, false);
    assert.equal(parseLocalCivilTimeToUtc('2026-10-24T12:65').success, false);
    assert.equal(parseLocalCivilTimeToUtc('2026-13-10T12:00').success, false);
    assert.equal(parseLocalCivilTimeToUtc('2026-10-35T12:00').success, false);
  });

  it('detects spring-forward gap (non-existent local wall time)', () => {
    // On March 8, 2026, clocks spring forward from 02:00 to 03:00 in America/Ciudad_Juarez
    // 02:30 does not exist
    const gap = parseLocalCivilTimeToUtc('2026-03-08T02:30', 'America/Ciudad_Juarez');
    assert.equal(gap.success, false);
    assert.equal(gap.isInvalid, true);
    assert.match(gap.error ?? '', /does not exist.*spring-forward/i);
  });

  it('detects fall-back overlap (ambiguous local wall time occurring twice) and communicates resolution', () => {
    // On November 1, 2026, clocks fall back from 02:00 to 01:00 in America/Ciudad_Juarez
    // 01:30 occurs twice: first at 07:30 UTC (MDT, UTC-6), then at 08:30 UTC (MST, UTC-7)
    const overlap = parseLocalCivilTimeToUtc('2026-11-01T01:30', 'America/Ciudad_Juarez');
    assert.equal(overlap.success, true);
    assert.equal(overlap.isAmbiguous, true);
    assert.equal(overlap.utcInstant?.toISOString(), '2026-11-01T07:30:00.000Z');
    assert.ok(overlap.disambiguationNotice);
    assert.match(overlap.disambiguationNotice, /earlier occurrence/i);
    assert.match(overlap.disambiguationNotice, /2026-11-01T07:30:00.000Z/);
  });

  it('verifies exact Ciudad Juárez 2026 seasonal and transition UTC instants', () => {
    // 1. October 31, 2026 at 17:50 local (before DST end, MDT UTC-6) -> 2026-10-31T23:50:00.000Z
    const oct31 = parseLocalCivilTimeToUtc('2026-10-31T17:50', 'America/Ciudad_Juarez');
    assert.equal(oct31.success, true);
    assert.equal(oct31.utcInstant?.toISOString(), '2026-10-31T23:50:00.000Z');

    // 2. November 1, 2026 at 12:00 local (after DST end, MST UTC-7) -> 2026-11-01T19:00:00.000Z
    const nov1 = parseLocalCivilTimeToUtc('2026-11-01T12:00', 'America/Ciudad_Juarez');
    assert.equal(nov1.success, true);
    assert.equal(nov1.utcInstant?.toISOString(), '2026-11-01T19:00:00.000Z');

    // 3. November 1, 2026 at 01:30 local (ambiguous overlap) -> 2026-11-01T07:30:00.000Z (earlier MDT occurrence)
    const nov1Ambiguous = parseLocalCivilTimeToUtc('2026-11-01T01:30', 'America/Ciudad_Juarez');
    assert.equal(nov1Ambiguous.success, true);
    assert.equal(nov1Ambiguous.isAmbiguous, true);
    assert.equal(nov1Ambiguous.utcInstant?.toISOString(), '2026-11-01T07:30:00.000Z');

    // 4. March 8, 2026 at 02:30 local (nonexistent spring-forward gap) -> rejected
    const mar8Gap = parseLocalCivilTimeToUtc('2026-03-08T02:30', 'America/Ciudad_Juarez');
    assert.equal(mar8Gap.success, false);
    assert.equal(mar8Gap.isInvalid, true);
  });
});
