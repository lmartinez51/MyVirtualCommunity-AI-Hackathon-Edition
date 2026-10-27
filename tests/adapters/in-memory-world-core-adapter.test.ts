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
});

