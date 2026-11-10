import { describe, it, before, after } from 'node:test';
import assert from 'node:assert/strict';
import { InMemoryWorldCoreAdapter } from '../../src/adapters/in-memory-world-core-adapter.js';
import { MockMetaModelAdapter } from '../../src/adapters/mock-meta-model-adapter.js';
import { createAssistantServer } from '../../src/server/index.js';

describe('Time-of-Day Environmental Presentation API', () => {
  let serverInstance: any;
  let baseUrl: string;
  let inMemoryCore: InMemoryWorldCoreAdapter;

  before(async () => {
    inMemoryCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    serverInstance = createAssistantServer({
      worldCore: inMemoryCore,
      metaModel,
    });
    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    baseUrl = `http://127.0.0.1:${addr.port}`;
  });

  after(async () => {
    if (serverInstance && typeof serverInstance.stop === 'function') {
      await serverInstance.stop();
    }
  });

  it('1. GET /api/world/environment returns 200 and authoritative snapshot', async () => {
    const res = await fetch(`${baseUrl}/api/world/environment`);
    assert.equal(res.status, 200);
    const body = await res.json() as any;
    assert.equal(body.success, true);
    assert.ok(body.evaluatedAt);
    assert.equal(body.timeZone, 'America/Ciudad_Juarez');
    assert.ok(body.localCivilTime);
    assert.ok(body.civilTime);
    assert.ok(body.environment);
    assert.ok(body.environment.phase);
    assert.ok(typeof body.environment.sunPitchDegrees === 'number');
    assert.ok(typeof body.environment.sunIntensity === 'number');
    assert.ok(body.environment.sunColorHex);
    assert.ok(typeof body.environment.ambientIntensity === 'number');
    assert.ok(body.environment.ambientSkyHex);
    assert.ok(typeof body.environment.streetlightsActive === 'boolean');
    assert.ok(typeof body.environment.skyboxExposure === 'number');
    assert.equal(body.isDemonstrationPresentation, true);
  });

  it('2. GET /api/world/environment?at= evaluates specified instant deterministically', async () => {
    // 2026-11-04T19:00:00Z -> 12:00 MST (Midday)
    const res = await fetch(`${baseUrl}/api/world/environment?at=2026-11-04T19:00:00.000Z`);
    assert.equal(res.status, 200);
    const body = await res.json() as any;
    assert.equal(body.success, true);
    assert.equal(body.civilTime.hour, 12);
    assert.equal(body.environment.phase, 'midday');
    assert.equal(body.environment.streetlightsActive, false);
    assert.ok(body.environment.sunIntensity > 1.2);
  });

  it('3. GET /api/world/environment?at= evaluates night instant with streetlights active', async () => {
    // 2026-11-05T06:00:00Z -> 23:00 MST (Night)
    const res = await fetch(`${baseUrl}/api/world/environment?at=2026-11-05T06:00:00.000Z`);
    assert.equal(res.status, 200);
    const body = await res.json() as any;
    assert.equal(body.success, true);
    assert.equal(body.civilTime.hour, 23);
    assert.equal(body.environment.phase, 'night');
    assert.equal(body.environment.isNight, true);
    assert.equal(body.environment.streetlightsActive, true);
    assert.equal(body.environment.streetlightsIntensity, 1.0);
  });

  it('4. GET /api/world/time alias responds identically', async () => {
    const res = await fetch(`${baseUrl}/api/world/time?at=2026-11-05T00:30:00.000Z`);
    assert.equal(res.status, 200);
    const body = await res.json() as any;
    assert.equal(body.success, true);
    assert.equal(body.environment.phase, 'sunset');
    assert.equal(body.environment.streetlightsActive, true);
  });
});
