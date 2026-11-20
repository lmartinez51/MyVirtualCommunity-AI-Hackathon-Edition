import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { InMemoryWorldCoreAdapter } from '../../src/adapters/in-memory-world-core-adapter.js';
import { createBusinessId } from '../../src/domain/business-id.js';

describe('InMemoryWorldCoreAdapter Fixture', () => {
  it('initializes with canonical Ciudad Juárez business schedules', async () => {
    const adapter = new InMemoryWorldCoreAdapter();
    const ids = adapter.listBusinessIds();

    assert.ok(ids.includes(createBusinessId('restaurant:wing-spot')));
    assert.ok(ids.includes(createBusinessId('restaurant:pasteleria-tia-lupe')));

    const wingSpot = await adapter.getBusinessSchedule(createBusinessId('restaurant:wing-spot'));
    assert.ok(wingSpot);
    assert.equal(wingSpot.name, 'Wing Spot Gómez Morín');
    assert.equal(wingSpot.timeZone, 'America/Ciudad_Juarez');
    assert.equal(wingSpot.hours.openHour, 9);
    assert.equal(wingSpot.hours.closeHour, 18);
  });

  it('returns null for non-registered business ID', async () => {
    const adapter = new InMemoryWorldCoreAdapter();
    const schedule = await adapter.getBusinessSchedule(
      createBusinessId('restaurant:unknown-place')
    );
    assert.equal(schedule, null);
  });

  it('supports simulated world time override', async () => {
    const adapter = new InMemoryWorldCoreAdapter();
    const fixedTime = '2026-10-26T20:00:00.000Z';

    adapter.setSimulatedWorldTime(fixedTime);
    assert.equal(await adapter.getCurrentWorldTime(), fixedTime);

    // Operational status should use this simulated time when not explicitly passed
    const status = await adapter.getBusinessOperationalStatus(
      createBusinessId('restaurant:wing-spot')
    );
    assert.ok(status);
    assert.equal(status.status, 'OPEN');
    assert.equal(status.evaluatedAtUtc, fixedTime);

    // Resetting simulated time returns to real time
    adapter.setSimulatedWorldTime(null);
    const realTime = await adapter.getCurrentWorldTime();
    assert.notEqual(realTime, fixedTime);
  });

  it('exposes in-memory mode and fast health probe', async () => {
    const adapter = new InMemoryWorldCoreAdapter();
    assert.strictEqual(adapter.mode, 'in-memory');
    const health = await adapter.probeHealth();
    assert.strictEqual(health.connected, true);
    assert.strictEqual(health.latencyMs, 0);
  });

  it('evaluates per-day schedules for Farmacia Juárez, Dental México, and 24/7 Casa de Cambio', async () => {
    const adapter = new InMemoryWorldCoreAdapter();
    const fjId = createBusinessId('pharmacy:farmacia-juarez');
    const dmId = createBusinessId('dental:dental-mexico');
    const ccId = createBusinessId('exchange:casa-cambio-juarez-sur');

    // Note: November 2026 in America/Ciudad_Juarez is MST (UTC-7):
    // 2026-11-20 is Friday, 2026-11-21 is Saturday, 2026-11-22 is Sunday.
    // Friday 19:30 local (2026-11-21T02:30:00Z): Farmacia Juárez OPEN (until 20:00), Dental México CLOSED (closed at 17:00), Casa de Cambio OPEN
    const friEvening = '2026-11-21T02:30:00.000Z';
    assert.equal((await adapter.getBusinessOperationalStatus(fjId, friEvening))?.isOpen, true);
    assert.equal((await adapter.getBusinessOperationalStatus(dmId, friEvening))?.isOpen, false);
    assert.equal((await adapter.getBusinessOperationalStatus(ccId, friEvening))?.isOpen, true);

    // Friday 20:05 local (2026-11-21T03:05:00Z): Farmacia Juárez CLOSED, Dental México CLOSED, Casa de Cambio OPEN
    const friNight = '2026-11-21T03:05:00.000Z';
    assert.equal((await adapter.getBusinessOperationalStatus(fjId, friNight))?.isOpen, false);
    assert.equal((await adapter.getBusinessOperationalStatus(dmId, friNight))?.isOpen, false);
    assert.equal((await adapter.getBusinessOperationalStatus(ccId, friNight))?.isOpen, true);

    // Saturday 11:00 local (2026-11-21T18:00:00Z): Farmacia Juárez CLOSED all Saturday, Dental México OPEN (9-15), Casa de Cambio OPEN
    const satMorning = '2026-11-21T18:00:00.000Z';
    assert.equal((await adapter.getBusinessOperationalStatus(fjId, satMorning))?.isOpen, false);
    assert.equal((await adapter.getBusinessOperationalStatus(dmId, satMorning))?.isOpen, true);
    assert.equal((await adapter.getBusinessOperationalStatus(ccId, satMorning))?.isOpen, true);

    // Saturday 15:15 local (2026-11-21T22:15:00Z): Dental México CLOSED after 3PM Saturday
    const satAfternoon = '2026-11-21T22:15:00.000Z';
    assert.equal((await adapter.getBusinessOperationalStatus(dmId, satAfternoon))?.isOpen, false);

    // Sunday 10:00 local (2026-11-22T17:00:00Z): Farmacia Juárez OPEN (8-16), Dental México CLOSED all Sunday, Casa de Cambio OPEN
    const sunMorning = '2026-11-22T17:00:00.000Z';
    assert.equal((await adapter.getBusinessOperationalStatus(fjId, sunMorning))?.isOpen, true);
    assert.equal((await adapter.getBusinessOperationalStatus(dmId, sunMorning))?.isOpen, false);
    assert.equal((await adapter.getBusinessOperationalStatus(ccId, sunMorning))?.isOpen, true);

    // Sunday 16:05 local (2026-11-22T23:05:00Z): Farmacia Juárez CLOSED after 4PM Sunday, Casa de Cambio OPEN 24/7
    const sunLate = '2026-11-22T23:05:00.000Z';
    assert.equal((await adapter.getBusinessOperationalStatus(fjId, sunLate))?.isOpen, false);
    assert.equal((await adapter.getBusinessOperationalStatus(ccId, sunLate))?.isOpen, true);
  });
});

