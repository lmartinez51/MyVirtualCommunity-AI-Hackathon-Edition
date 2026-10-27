import assert from 'node:assert/strict';
import { afterEach, beforeEach, describe, it } from 'node:test';
import { InMemoryWorldCoreAdapter } from '../../src/adapters/in-memory-world-core-adapter.js';
import { MockMetaModelAdapter } from '../../src/adapters/mock-meta-model-adapter.js';
import {
  WorldCoreConnectionError,
  WorldCoreResponseValidationError,
} from '../../src/adapters/remote-world-core-adapter.js';
import type { BusinessId } from '../../src/domain/business-id.js';
import type { WorldCorePort } from '../../src/ports/world-core-port.js';
import { createAssistantServer } from '../../src/server/index.js';

describe('API and Browser Integration (Offline Server Tests)', () => {
  let serverInstance: ReturnType<typeof createAssistantServer>;
  let mockMeta: MockMetaModelAdapter;
  let inMemoryWorldCore: InMemoryWorldCoreAdapter;
  let baseUrl: string;

  beforeEach(async () => {
    mockMeta = new MockMetaModelAdapter();
    inMemoryWorldCore = new InMemoryWorldCoreAdapter();

    serverInstance = createAssistantServer({
      worldCore: inMemoryWorldCore,
      metaModel: mockMeta,
    });

    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address();
    if (addr && typeof addr === 'object') {
      baseUrl = `http://127.0.0.1:${addr.port}`;
    } else {
      throw new Error('Server address unavailable');
    }
  });

  afterEach(async () => {
    if (serverInstance) {
      await serverInstance.stop();
    }
  });

  describe('1. Static Web Client Delivery', () => {
    it('serves index.html with MyVirtualCommunity AI identity and canonical businesses on GET /', async () => {
      const res = await fetch(`${baseUrl}/`);
      assert.strictEqual(res.status, 200);
      assert.match(res.headers.get('content-type') || '', /text\/html/);

      const html = await res.text();
      assert.ok(html.includes('MyVirtualCommunity AI'), 'Must include brand title');
      assert.ok(html.includes('Wing Spot Gómez Morín'), 'Must include verified canonical Wing Spot');
      assert.ok(html.includes('Pastelerías La Tía Lupe'), 'Must include verified canonical Tía Lupe');
      assert.ok(html.includes('data-lang="en"'), 'Must support English');
      assert.ok(html.includes('data-lang="es"'), 'Must support Spanish');
    });

    it('serves style.css with CSS design system on GET /style.css', async () => {
      const res = await fetch(`${baseUrl}/style.css`);
      assert.strictEqual(res.status, 200);
      assert.match(res.headers.get('content-type') || '', /text\/css/);

      const css = await res.text();
      assert.ok(css.includes('--bg-dark'), 'Must include design tokens');
    });

    it('serves app.js with client logic on GET /app.js', async () => {
      const res = await fetch(`${baseUrl}/app.js`);
      assert.strictEqual(res.status, 200);
      assert.match(res.headers.get('content-type') || '', /application\/javascript/);

      const js = await res.text();
      assert.ok(js.includes('I18N'), 'Must include bilingual i18n support');
      assert.ok(js.includes('TILE_PROVIDERS'), 'Must configure tile providers');
      assert.ok(js.includes('tile.openstreetmap.fr'), 'Must use OSM France basemap provider');
      assert.ok(js.includes('btn-retry-map'), 'Must bind retry button');
    });

    it('serves local vendor Leaflet CSS and JS without external CDN dependencies', async () => {
      const cssRes = await fetch(`${baseUrl}/vendor/leaflet/leaflet.css`);
      assert.strictEqual(cssRes.status, 200);
      assert.match(cssRes.headers.get('content-type') || '', /text\/css/);
      const css = await cssRes.text();
      assert.ok(css.includes('leaflet-container'), 'Must serve valid Leaflet CSS');

      const jsRes = await fetch(`${baseUrl}/vendor/leaflet/leaflet.js`);
      assert.strictEqual(jsRes.status, 200);
      assert.match(jsRes.headers.get('content-type') || '', /application\/javascript/);
      const js = await jsRes.text();
      assert.ok(js.includes('Leaflet'), 'Must serve valid Leaflet JS bundle');
    });

    it('serves local vendor Leaflet image assets', async () => {
      const iconRes = await fetch(`${baseUrl}/vendor/leaflet/images/marker-icon.png`);
      assert.strictEqual(iconRes.status, 200);
      assert.match(iconRes.headers.get('content-type') || '', /image\/png/);
      const buf = Buffer.from(await iconRes.arrayBuffer());
      assert.ok(buf.length > 500, 'Must serve valid marker icon binary');
    });

    it('serves index.html with interactive map structure, local vendor assets, and retry button', async () => {
      const res = await fetch(`${baseUrl}/`);
      assert.strictEqual(res.status, 200);
      const html = await res.text();

      assert.ok(html.includes('/vendor/leaflet/leaflet.css'), 'Must reference local Leaflet CSS');
      assert.ok(html.includes('/vendor/leaflet/leaflet.js'), 'Must reference local Leaflet JS');
      assert.ok(html.includes('id="corridor-map"'), 'Must have map container');
      assert.ok(html.includes('id="map-fallback"'), 'Must have map fallback container');
      assert.ok(html.includes('id="btn-retry-map"'), 'Must have map retry button');
    });

    it('serves index.html with demonstration flow strip, entity live snapshot card, and 1-click query buttons', async () => {
      const res = await fetch(`${baseUrl}/`);
      assert.strictEqual(res.status, 200);
      const html = await res.text();

      // Demonstration Flow Strip
      assert.ok(html.includes('demo-flow-strip'), 'Must include demonstration flow strip');
      assert.ok(html.includes('id="txt-flow-1"'), 'Must include flow step 1');
      assert.ok(html.includes('id="txt-flow-2"'), 'Must include flow step 2');
      assert.ok(html.includes('id="txt-flow-3"'), 'Must include flow step 3');
      assert.ok(html.includes('id="txt-flow-4"'), 'Must include flow step 4');

      // Live Authoritative Snapshot in Entity Detail Panel
      assert.ok(html.includes('id="entity-snapshot-card"'), 'Must include entity snapshot card');
      assert.ok(html.includes('id="entity-live-status"'), 'Must include live status pill in panel');
      assert.ok(html.includes('id="entity-live-admission"'), 'Must include live admission pill in panel');
      assert.ok(html.includes('id="entity-live-hours"'), 'Must include live hours in panel');
      assert.ok(html.includes('id="entity-live-cutoff"'), 'Must include live cutoff in panel');
      assert.ok(html.includes('id="entity-live-freshness"'), 'Must include snapshot provenance in panel');

      // 1-Click Fast Judge Queries
      assert.ok(html.includes('id="entity-quick-queries"'), 'Must include 1-click fast queries group');
      assert.ok(html.includes('id="btn-quick-open-now"'), 'Must include check open now quick button');
      assert.ok(html.includes('id="btn-quick-schedule"'), 'Must include operating schedule quick button');
      assert.ok(html.includes('id="btn-quick-admission"'), 'Must include test 5:50 PM cutoff quick button');

      // Query Context Card in Result State
      assert.ok(html.includes('id="query-context-card"'), 'Must include query context card');
      assert.ok(html.includes('id="result-business-name"'), 'Must include queried business element');
      assert.ok(html.includes('id="result-question-text"'), 'Must include submitted question element');
      assert.ok(html.includes('id="result-retrieved-at"'), 'Must include snapshot retrieval timestamp element');
    });

    it('serves app.js with live status fetcher, 1-click fast queries, and duplicate submission guard', async () => {
      const res = await fetch(`${baseUrl}/app.js`);
      assert.strictEqual(res.status, 200);
      const js = await res.text();

      assert.ok(js.includes('fetchEntityLiveStatus'), 'Must define fetchEntityLiveStatus');
      assert.ok(js.includes('setSubmittingState'), 'Must define setSubmittingState to guard against duplicate submissions');
      assert.ok(js.includes('btn-quick-open-now'), 'Must bind btn-quick-open-now');
      assert.ok(js.includes('result-retrieved-at'), 'Must update result-retrieved-at');
    });

    it('returns live operational status and schedule from GET /api/businesses/:id/status', async () => {
      const res = await fetch(`${baseUrl}/api/businesses/pharmacy%3Afarmacias-benavides/status`);
      assert.strictEqual(res.status, 200);
      const data = (await res.json()) as any;

      assert.strictEqual(data.success, true);
      assert.ok(data.status, 'Must return operational status');
      assert.strictEqual(data.status.businessName, 'Farmacias Benavides');
      assert.strictEqual(data.status.businessId, 'pharmacy:farmacias-benavides');
      assert.strictEqual(data.status.timeZone, 'America/Ciudad_Juarez');
      assert.ok(data.status.evaluatedAtUtc, 'Must include UTC evaluated instant');
      assert.ok(data.status.openTimeCivil, 'Must include civil open time');
      assert.ok(data.status.closeTimeCivil, 'Must include civil close time');
      assert.ok(data.schedule, 'Must return schedule definition');
      assert.strictEqual(data.schedule.name, 'Farmacias Benavides');
    });
  });

  describe('2. POST /api/assistant/query Validation & Language Routing', () => {
    it('accepts valid English request and preserves canonical business name', async () => {
      const res = await fetch(`${baseUrl}/api/assistant/query`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          businessId: 'restaurant:wing-spot',
          question: 'Is Wing Spot Gómez Morín open right now, and what time does it close?',
          language: 'en',
        }),
      });

      assert.strictEqual(res.status, 200);
      const data = (await res.json()) as any;
      assert.strictEqual(data.success, true);
      assert.strictEqual(data.language, 'en');

      // Canonical name preserved without alteration
      assert.strictEqual(data.verifiedStatus.businessName, 'Wing Spot Gómez Morín');
      assert.strictEqual(data.schedule.name, 'Wing Spot Gómez Morín');
      assert.strictEqual(data.businessId, 'restaurant:wing-spot');
      assert.strictEqual(data.question, 'Is Wing Spot Gómez Morín open right now, and what time does it close?');

      // Model grounding prompt check
      assert.ok(mockMeta.lastContextReceived);
      assert.ok(
        mockMeta.lastContextReceived.systemPrompt?.includes('American English'),
        'System prompt must instruct American English'
      );
      assert.ok(
        mockMeta.lastContextReceived.systemPrompt?.includes('Wing Spot Gómez Morín'),
        'System prompt must command preserving canonical business name'
      );

      // Distinct authoritative facts from answer
      assert.ok(typeof data.answer === 'string' && data.answer.length > 0);
      assert.ok(typeof data.latencyMs === 'number');
      assert.ok(data.verifiedStatus.status);
      assert.strictEqual(typeof data.verifiedStatus.isOpen, 'boolean');
      assert.strictEqual(typeof data.verifiedStatus.admissionPermitted, 'boolean');
    });

    it('accepts valid Spanish request with Spanish instructions', async () => {
      const res = await fetch(`${baseUrl}/api/assistant/query`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          businessId: 'restaurant:pasteleria-tia-lupe',
          question: '¿A qué hora cierra Pastelerías La Tía Lupe hoy?',
          language: 'es',
        }),
      });

      assert.strictEqual(res.status, 200);
      const data = (await res.json()) as any;
      assert.strictEqual(data.success, true);
      assert.strictEqual(data.language, 'es');

      // Canonical name preserved
      assert.strictEqual(data.verifiedStatus.businessName, 'Pastelerías La Tía Lupe');

      // Prompt in Spanish
      assert.ok(mockMeta.lastContextReceived);
      assert.ok(
        mockMeta.lastContextReceived.systemPrompt?.includes('español'),
        'System prompt must instruct Spanish response'
      );
    });

    it('rejects missing businessId with 400', async () => {
      const res = await fetch(`${baseUrl}/api/assistant/query`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          question: 'Are you open?',
        }),
      });

      assert.strictEqual(res.status, 400);
      const data = (await res.json()) as any;
      assert.strictEqual(data.success, false);
      assert.strictEqual(data.error.code, 'missing-business-id');
    });

    it('rejects missing or empty question with 400', async () => {
      const res = await fetch(`${baseUrl}/api/assistant/query`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          businessId: 'restaurant:wing-spot',
          question: '   ',
        }),
      });

      assert.strictEqual(res.status, 400);
      const data = (await res.json()) as any;
      assert.strictEqual(data.success, false);
      assert.strictEqual(data.error.code, 'missing-question');
    });

    it('rejects question exceeding 500 characters with 400', async () => {
      const longQuery = 'A'.repeat(501);
      const res = await fetch(`${baseUrl}/api/assistant/query`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          businessId: 'restaurant:wing-spot',
          question: longQuery,
        }),
      });

      assert.strictEqual(res.status, 400);
      const data = (await res.json()) as any;
      assert.strictEqual(data.success, false);
      assert.strictEqual(data.error.code, 'question-too-long');
    });

    it('rejects unsupported language with 400', async () => {
      const res = await fetch(`${baseUrl}/api/assistant/query`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          businessId: 'restaurant:wing-spot',
          question: 'Is it open?',
          language: 'de',
        }),
      });

      assert.strictEqual(res.status, 400);
      const data = (await res.json()) as any;
      assert.strictEqual(data.success, false);
      assert.strictEqual(data.error.code, 'invalid-language');
    });

    it('returns 404 for unknown business identifier', async () => {
      const res = await fetch(`${baseUrl}/api/assistant/query`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          businessId: 'restaurant:nonexistent-place',
          question: 'What time do you close?',
        }),
      });

      assert.strictEqual(res.status, 404);
      const data = (await res.json()) as any;
      assert.strictEqual(data.success, false);
      assert.strictEqual(data.error.code, 'business-not-found');
    });
  });

  describe('3. World Core Resiliency & Zero Meta Invocation on Failure', () => {
    it('returns 502 world-core-unavailable and does NOT invoke Meta when World Core connection fails', async () => {
      // Create a failing World Core adapter
      const failingWorldCore: WorldCorePort = {
        async getBusinessSchedule() {
          throw new WorldCoreConnectionError('connect ECONNREFUSED 127.0.0.1:3001');
        },
        async getCurrentWorldTime() {
          return new Date().toISOString();
        },
        async getBusinessOperationalStatus() {
          throw new WorldCoreConnectionError('connect ECONNREFUSED 127.0.0.1:3001');
        },
        async getBusinessScheduleSnapshot() {
          throw new WorldCoreConnectionError('connect ECONNREFUSED 127.0.0.1:3001');
        },
      };

      const failingServer = createAssistantServer({
        worldCore: failingWorldCore,
        metaModel: mockMeta,
      });

      await failingServer.start(0, '127.0.0.1');
      const addr = failingServer.server.address() as any;
      const failingUrl = `http://127.0.0.1:${addr.port}`;

      try {
        const metaInitialCallCount = mockMeta.callCount;

        const res = await fetch(`${failingUrl}/api/assistant/query`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            businessId: 'restaurant:wing-spot',
            question: 'Is Wing Spot open?',
          }),
        });

        assert.strictEqual(res.status, 502);
        const data = (await res.json()) as any;
        assert.strictEqual(data.success, false);
        assert.strictEqual(data.error.code, 'world-core-unavailable');

        // CRITICAL: Meta was NEVER invoked when World Core lookup failed!
        assert.strictEqual(
          mockMeta.callCount,
          metaInitialCallCount,
          'Meta Model API must NOT be called when World Core lookup fails'
        );
      } finally {
        await failingServer.stop();
      }
    });

    it('returns 502 world-core-invalid-response and does NOT invoke Meta when World Core returns malformed data', async () => {
      const malformedWorldCore: WorldCorePort = {
        async getBusinessSchedule() {
          throw new WorldCoreResponseValidationError('World Core result missing hours');
        },
        async getCurrentWorldTime() {
          return new Date().toISOString();
        },
        async getBusinessOperationalStatus() {
          throw new WorldCoreResponseValidationError('World Core result missing hours');
        },
        async getBusinessScheduleSnapshot() {
          throw new WorldCoreResponseValidationError('World Core result missing hours');
        },
      };

      const malformedServer = createAssistantServer({
        worldCore: malformedWorldCore,
        metaModel: mockMeta,
      });

      await malformedServer.start(0, '127.0.0.1');
      const addr = malformedServer.server.address() as any;
      const malformedUrl = `http://127.0.0.1:${addr.port}`;

      try {
        const metaInitialCallCount = mockMeta.callCount;

        const res = await fetch(`${malformedUrl}/api/assistant/query`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            businessId: 'restaurant:wing-spot',
            question: 'Is Wing Spot open?',
          }),
        });

        assert.strictEqual(res.status, 502);
        const data = (await res.json()) as any;
        assert.strictEqual(data.success, false);
        assert.strictEqual(data.error.code, 'world-core-invalid-response');

        // Meta was NEVER invoked
        assert.strictEqual(mockMeta.callCount, metaInitialCallCount);
      } finally {
        await malformedServer.stop();
      }
    });
  });

  describe('4. Exactly One Coherent World Core Snapshot Read', () => {
    it('requests exactly ONE snapshot read per accepted query', async () => {
      let snapshotCalls = 0;
      let scheduleCalls = 0;
      let statusCalls = 0;

      const countingWorldCore: WorldCorePort = {
        async getBusinessSchedule(id: BusinessId) {
          scheduleCalls++;
          return inMemoryWorldCore.getBusinessSchedule(id);
        },
        async getCurrentWorldTime() {
          return inMemoryWorldCore.getCurrentWorldTime();
        },
        async getBusinessOperationalStatus(id: BusinessId, at?: any) {
          statusCalls++;
          return inMemoryWorldCore.getBusinessOperationalStatus(id, at);
        },
        async getBusinessScheduleSnapshot(id: BusinessId, at?: any) {
          snapshotCalls++;
          return inMemoryWorldCore.getBusinessScheduleSnapshot(id, at);
        },
      };

      const countingServer = createAssistantServer({
        worldCore: countingWorldCore,
        metaModel: mockMeta,
      });

      await countingServer.start(0, '127.0.0.1');
      const addr = countingServer.server.address() as any;
      const countingUrl = `http://127.0.0.1:${addr.port}`;

      try {
        const res = await fetch(`${countingUrl}/api/assistant/query`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            businessId: 'restaurant:wing-spot',
            question: 'Is Wing Spot open?',
          }),
        });

        assert.strictEqual(res.status, 200);
        assert.strictEqual(snapshotCalls, 1, 'Must call getBusinessScheduleSnapshot exactly once');
        assert.strictEqual(scheduleCalls, 0, 'Must not make redundant getBusinessSchedule calls');
        assert.strictEqual(statusCalls, 0, 'Must not make redundant getBusinessOperationalStatus calls');
      } finally {
        await countingServer.stop();
      }
    });
  });
});
