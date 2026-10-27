import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { validateBusinessSchedule } from '../../src/domain/business-schedule.js';
import { evaluateBusinessOperationalStatus } from '../../src/domain/operational-status.js';

describe('Deterministic Operational Status Evaluation', () => {
  // Wing Spot: 09:00 - 18:00, Mon-Sat (1-6), Cutoff 15 mins (17:45), Closed Sun (0)
  // Timezone: America/Ciudad_Juarez (UTC-06:00 in Oct 2026)
  const wingSpotSchedule = validateBusinessSchedule({
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
    closureOverrides: ['2026-12-25'],
  });

  it('evaluates status as OPEN during normal operating hours (e.g. 14:30)', () => {
    // 2026-10-26 is Monday.
    // 14:30 local Ciudad Juárez = 20:30 UTC
    const evalInstant = '2026-10-26T20:30:00.000Z';
    const status = evaluateBusinessOperationalStatus(wingSpotSchedule, evalInstant);

    assert.equal(status.status, 'OPEN');
    assert.equal(status.isOpen, true);
    assert.equal(status.admissionPermitted, true);
    assert.equal(status.openTimeCivil, '09:00');
    assert.equal(status.closeTimeCivil, '18:00');
    assert.equal(status.cutoffTimeCivil, '17:45');
    assert.match(status.summaryMessage, /is open until 18:00/);
  });

  it('evaluates status at exact boundary: 09:00 local is OPEN', () => {
    // 09:00 local = 15:00 UTC
    const status = evaluateBusinessOperationalStatus(wingSpotSchedule, '2026-10-26T15:00:00.000Z');
    assert.equal(status.status, 'OPEN');
    assert.equal(status.isOpen, true);
    assert.equal(status.admissionPermitted, true);
  });

  it('evaluates status 1 minute before opening: 08:59 local is CLOSED', () => {
    // 08:59 local = 14:59 UTC
    const status = evaluateBusinessOperationalStatus(wingSpotSchedule, '2026-10-26T14:59:00.000Z');
    assert.equal(status.status, 'CLOSED');
    assert.equal(status.isOpen, false);
    assert.equal(status.admissionPermitted, false);
    assert.match(status.summaryMessage, /Opens today at 09:00/);
  });

  it('evaluates status at exact cutoff: 17:45 local is CLOSING_SOON (no new admissions)', () => {
    // 17:45 local = 23:45 UTC
    const status = evaluateBusinessOperationalStatus(wingSpotSchedule, '2026-10-26T23:45:00.000Z');
    assert.equal(status.status, 'CLOSING_SOON');
    assert.equal(status.isOpen, true);
    assert.equal(status.admissionPermitted, false);
    assert.match(status.summaryMessage, /closing soon at 18:00/);
    assert.match(status.summaryMessage, /no new customer admissions are permitted/);
  });

  it('evaluates status 1 minute before cutoff: 17:44 local is still OPEN', () => {
    // 17:44 local = 23:44 UTC
    const status = evaluateBusinessOperationalStatus(wingSpotSchedule, '2026-10-26T23:44:00.000Z');
    assert.equal(status.status, 'OPEN');
    assert.equal(status.isOpen, true);
    assert.equal(status.admissionPermitted, true);
  });

  it('evaluates status 1 minute before close: 17:59 local is CLOSING_SOON', () => {
    // 17:59 local = 23:59 UTC
    const status = evaluateBusinessOperationalStatus(wingSpotSchedule, '2026-10-26T23:59:00.000Z');
    assert.equal(status.status, 'CLOSING_SOON');
    assert.equal(status.isOpen, true);
    assert.equal(status.admissionPermitted, false);
  });

  it('evaluates status at exact close: 18:00 local is CLOSED', () => {
    // 18:00 local = 2026-10-27T00:00:00.000Z
    const status = evaluateBusinessOperationalStatus(wingSpotSchedule, '2026-10-27T00:00:00.000Z');
    assert.equal(status.status, 'CLOSED');
    assert.equal(status.isOpen, false);
    assert.equal(status.admissionPermitted, false);
    assert.match(status.summaryMessage, /closed for the day/);
  });

  it('evaluates status on closed operating day (Sunday 2026-10-25)', () => {
    // 2026-10-25 is Sunday.
    // 14:00 local = 20:00 UTC
    const status = evaluateBusinessOperationalStatus(wingSpotSchedule, '2026-10-25T20:00:00.000Z');
    assert.equal(status.status, 'CLOSED');
    assert.equal(status.isOpen, false);
    assert.equal(status.admissionPermitted, false);
    assert.match(status.summaryMessage, /closed on Sundays/);
  });

  it('evaluates status on scheduled holiday / closure override', () => {
    // 2026-12-25 is in closureOverrides
    // 12:00 local Ciudad Juárez (in Dec: UTC-06:00 = 18:00 UTC)
    const status = evaluateBusinessOperationalStatus(wingSpotSchedule, '2026-12-25T18:00:00.000Z');
    assert.equal(status.status, 'SPECIAL_CLOSURE');
    assert.equal(status.isOpen, false);
    assert.equal(status.admissionPermitted, false);
    assert.match(status.summaryMessage, /scheduled closure\/holiday/);
  });
});
