import assert from 'node:assert/strict';
import { createServer as createHttpServer } from 'node:http';
import { afterEach, beforeEach, describe, it } from 'node:test';
import { InMemoryWorldCoreAdapter } from '../../src/adapters/in-memory-world-core-adapter.js';
import { MetaModelApiAdapter } from '../../src/adapters/meta-model-api-adapter.js';
import { MockMetaModelAdapter } from '../../src/adapters/mock-meta-model-adapter.js';
import {
  RemoteWorldCoreAdapter,
  WorldCoreConnectionError,
  WorldCoreResponseValidationError,
} from '../../src/adapters/remote-world-core-adapter.js';
import { loadConfig } from '../../src/config/index.js';
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

  describe('5. Scenario Explorer Contract & Semantic Invariants', () => {
    it('serves index.html with Scenario Explorer container, 4 scenario cards, and authority badges', async () => {
      const res = await fetch(`${baseUrl}/`);
      assert.strictEqual(res.status, 200);
      const html = await res.text();

      // Main Scenario Explorer elements
      assert.ok(html.includes('id="scenario-explorer"'), 'Must include scenario explorer container');
      assert.ok(html.includes('id="txt-scenario-explorer-title"'), 'Must include scenario explorer title');
      assert.ok(html.includes('id="txt-scenario-explorer-badge"'), 'Must include scenario explorer badge');
      assert.ok(html.includes('id="scenario-selected-business-name"'), 'Must include selected business name target');
      assert.ok(html.includes('id="scenario-cards-grid"'), 'Must include 4 scenario cards grid');

      // 4 Scenarios
      assert.ok(html.includes('id="scenario-card-status"'), 'Must include Scenario 1 (Status)');
      assert.ok(html.includes('id="btn-scenario-status"'), 'Must include Scenario 1 action button');
      assert.ok(html.includes('id="scenario-card-schedule"'), 'Must include Scenario 2 (Schedule)');
      assert.ok(html.includes('id="btn-scenario-schedule"'), 'Must include Scenario 2 action button');
      assert.ok(html.includes('id="scenario-card-cutoff"'), 'Must include Scenario 3 (Cutoff)');
      assert.ok(html.includes('id="btn-scenario-cutoff"'), 'Must include Scenario 3 action button');
      assert.ok(html.includes('id="scenario-card-custom"'), 'Must include Scenario 4 (Custom)');
      assert.ok(html.includes('id="btn-scenario-custom"'), 'Must include Scenario 4 action button');

      // Infrastructure non-commercial alternate panel
      assert.ok(html.includes('id="scenario-infra-card"'), 'Must include scenario infrastructure card');
      assert.ok(html.includes('id="btn-shortcut-benavides"'), 'Must include shortcut to Farmacias Benavides');
      assert.ok(html.includes('id="btn-shortcut-optica"'), 'Must include shortcut to Óptica Fashion');

      // Result semantic distinction elements
      assert.ok(html.includes('id="result-evaluation-basis"'), 'Must include evaluation basis element in result');
      assert.ok(html.includes('id="txt-response-grounding"'), 'Must include response grounding note in result');
    });

    it('serves app.js with bilingual Scenario Explorer translations, action runner, and duplicate submission locks', async () => {
      const res = await fetch(`${baseUrl}/app.js`);
      assert.strictEqual(res.status, 200);
      const js = await res.text();

      // Logic functions
      assert.ok(js.includes('runScenario'), 'Must define runScenario');
      assert.ok(js.includes('btn-scenario-status'), 'Must bind btn-scenario-status');
      assert.ok(js.includes('btn-scenario-schedule'), 'Must bind btn-scenario-schedule');
      assert.ok(js.includes('btn-scenario-cutoff'), 'Must bind btn-scenario-cutoff');
      assert.ok(js.includes('btn-scenario-custom'), 'Must bind btn-scenario-custom');

      // Bilingual labels
      assert.ok(js.includes('Scenario Explorer'), 'Must include English title');
      assert.ok(js.includes('Explorador de Escenarios'), 'Must include Spanish title');
      assert.ok(js.includes('Authoritative ?at= Rule'), 'Must include English rule badge');
      assert.ok(js.includes('Regla Autoritativa ?at='), 'Must include Spanish rule badge');

      // Duplicate submission lock
      assert.ok(js.includes('.btn-scenario-action'), 'Must disable scenario action buttons in setSubmittingState');
      assert.ok(js.includes('.btn-infra-shortcut'), 'Must disable infra shortcut buttons in setSubmittingState');
    });

    it('Scenario 1 (Current Status): evaluates authoritative current-time snapshot without changing clock', async () => {
      const res = await fetch(`${baseUrl}/api/assistant/query`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          businessId: 'pharmacy:farmacias-benavides',
          question: 'Is Farmacias Benavides open right now, and what is its current operating status?',
          language: 'en',
        }),
      });

      assert.strictEqual(res.status, 200);
      const data = (await res.json()) as any;
      assert.strictEqual(data.success, true);
      assert.strictEqual(data.evaluationMode, 'current');
      assert.strictEqual(data.businessId, 'pharmacy:farmacias-benavides');
      assert.strictEqual(data.verifiedStatus.businessName, 'Farmacias Benavides');
      assert.ok(data.verifiedStatus.openTimeCivil);
      assert.ok(data.verifiedStatus.closeTimeCivil);
      assert.ok(data.evaluatedAtUtc);
    });

    it('Scenario 2 (Operating Schedule): explains official configured hours and operating days', async () => {
      const res = await fetch(`${baseUrl}/api/assistant/query`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          businessId: 'optical:optica-fashion',
          question: 'What are the official operating days, opening hours, and closing times for Óptica Fashion?',
          language: 'es',
        }),
      });

      assert.strictEqual(res.status, 200);
      const data = (await res.json()) as any;
      assert.strictEqual(data.success, true);
      assert.strictEqual(data.language, 'es');
      assert.strictEqual(data.verifiedStatus.businessName, 'Óptica Fashion');
      assert.strictEqual(data.schedule.hours.openHour, 9);
      assert.strictEqual(data.schedule.hours.closeHour, 17);
      assert.deepStrictEqual(data.schedule.operatingDays, [1, 2, 3, 4, 5, 6]);
    });

    it('Scenario 3 (Admission Cutoff Rule): evaluates authoritative ?at= rule without mutating world clock', async () => {
      // Wing Spot closes at 18:00 with 15-minute last entry cutoff (cutoff at 17:45).
      // Evaluating at 17:50: open for dining guests, but admission is denied!
      inMemoryWorldCore.setSimulatedWorldTime('2026-10-27T18:00:00.000Z');
      const initialWorldTime = await inMemoryWorldCore.getCurrentWorldTime();

      const res = await fetch(`${baseUrl}/api/assistant/query`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          businessId: 'restaurant:wing-spot',
          question: 'Will I still be admitted to Wing Spot Gómez Morín if I arrive at 5:50 PM, or has the 15-minute last entry cutoff closed new admissions?',
          language: 'en',
          evaluationTime: '2026-10-24T17:50',
        }),
      });

      assert.strictEqual(res.status, 200);
      const data = (await res.json()) as any;
      assert.strictEqual(data.success, true);
      assert.strictEqual(data.evaluationMode, 'specific');
      assert.strictEqual(data.verifiedStatus.status, 'CLOSING_SOON');
      assert.strictEqual(data.verifiedStatus.isOpen, true);
      assert.strictEqual(data.verifiedStatus.admissionPermitted, false);
      assert.ok(data.verifiedStatus.summaryMessage.length > 0);
      assert.strictEqual(data.schedule.lastEntryCutoffMinutes, 15);

      // Verify that evaluating an explicit hypothetical time did NOT mutate live world clock
      const currentWorldTime = await inMemoryWorldCore.getCurrentWorldTime();
      assert.strictEqual(currentWorldTime, initialWorldTime, 'World Core clock must remain unmutated by explicit-time evaluation');
      assert.strictEqual(currentWorldTime, '2026-10-27T18:00:00.000Z');
    });

    it('clearly distinguishes current authoritative facts, domain rule results, and AI interpretation', async () => {
      const res = await fetch(`${baseUrl}/api/assistant/query`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          businessId: 'pharmacy:farmacias-benavides',
          question: 'Does Farmacias Benavides allow customer entry at 9:50 PM before closing at 10:00 PM?',
          language: 'en',
          evaluationTime: '2026-10-24T21:50',
        }),
      });

      assert.strictEqual(res.status, 200);
      const data = (await res.json()) as any;

      // 1. Current Authoritative Facts (static schedule definitions)
      assert.ok(data.schedule, 'Must include authoritative schedule');
      assert.strictEqual(data.schedule.name, 'Farmacias Benavides');
      assert.strictEqual(data.schedule.hours.openHour, 7);
      assert.strictEqual(data.schedule.hours.closeHour, 22);
      assert.strictEqual(data.schedule.lastEntryCutoffMinutes, 0);

      // 2. Authoritative Domain Rule Evaluation (deterministic calculation at ?at= instant)
      assert.ok(data.verifiedStatus, 'Must include verified status from domain rule');
      assert.strictEqual(data.verifiedStatus.status, 'OPEN');
      assert.strictEqual(data.verifiedStatus.admissionPermitted, true);
      assert.ok(data.verifiedStatus.summaryMessage.length > 0);
      assert.strictEqual(data.evaluationMode, 'specific');
      assert.ok(data.evaluatedAtUtc);
      assert.ok(data.evaluatedAtCivil);

      // 3. AI Interpretation (Muse Spark natural language formulation)
      assert.ok(data.answer, 'Must include AI answer');
      assert.strictEqual(data.modelUsed, 'muse-spark-1.3-mock');
      assert.strictEqual(mockMeta.lastContextReceived?.schedule?.name, 'Farmacias Benavides');
      assert.strictEqual(mockMeta.lastContextReceived?.verifiedData?.businessName, 'Farmacias Benavides');
    });

    it('properly handles missing or unavailable World Core data without fabricating answers', async () => {
      const failingWorldCore: WorldCorePort = {
        async getBusinessSchedule() {
          throw new WorldCoreConnectionError('World Core offline');
        },
        async getCurrentWorldTime() {
          throw new WorldCoreConnectionError('World Core offline');
        },
        async getBusinessOperationalStatus() {
          throw new WorldCoreConnectionError('World Core offline');
        },
        async getBusinessScheduleSnapshot() {
          throw new WorldCoreConnectionError('World Core offline');
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
        const metaCallsBefore = mockMeta.callCount;

        const res = await fetch(`${failingUrl}/api/assistant/query`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            businessId: 'pharmacy:farmacias-benavides',
            question: 'Is it open?',
          }),
        });

        assert.strictEqual(res.status, 502);
        const data = (await res.json()) as any;
        assert.strictEqual(data.success, false);
        assert.strictEqual(data.error.code, 'world-core-unavailable');

        // Meta Muse Spark was NOT invoked
        assert.strictEqual(mockMeta.callCount, metaCallsBefore, 'Meta model must not be called when World Core fails');
      } finally {
        await failingServer.stop();
      }
    });
  });

  describe('6. Simulation Health Probe & Provider Mode Synchronization', () => {
    // 1. Remote World Core responds successfully
    it('reports connected remote World Core when remote endpoint responds 200', async () => {
      const mockWcServer = createHttpServer((req, res) => {
        if (req.url === '/api/status') {
          res.writeHead(200, { 'Content-Type': 'application/json' });
          res.end(
            JSON.stringify({
              success: true,
              result: {
                worldId: 'gomez-morin-world',
                worldTime: '2026-10-27T18:00:00.000Z',
                status: 'RUNNING',
              },
            })
          );
          return;
        }
        res.writeHead(404).end();
      });

      await new Promise<void>((resolve) => mockWcServer.listen(0, '127.0.0.1', () => resolve()));
      const wcPort = (mockWcServer.address() as any).port;

      const remoteAdapter = new RemoteWorldCoreAdapter({
        baseURL: `http://127.0.0.1:${wcPort}`,
      });

      const server = createAssistantServer({
        worldCore: remoteAdapter,
        metaModel: mockMeta,
      });

      await server.start(0, '127.0.0.1');
      const addr = server.server.address() as any;
      const url = `http://127.0.0.1:${addr.port}`;

      try {
        const res = await fetch(`${url}/health`);
        assert.strictEqual(res.status, 200);
        const data = (await res.json()) as any;

        assert.strictEqual(data.status, 'ok');
        assert.strictEqual(data.worldCore.mode, 'remote');
        assert.strictEqual(data.worldCore.reachability, 'connected');
        assert.strictEqual(data.worldCore.connected, true);
        assert.strictEqual(typeof data.worldCore.latencyMs, 'number');
        assert.strictEqual(data.metaProvider, 'local-mock');
      } finally {
        await server.stop();
        await new Promise<void>((resolve) => mockWcServer.close(() => resolve()));
      }
    });

    // 2. Remote World Core refuses the connection
    it('reports disconnected remote World Core when connection is refused without crashing', async () => {
      const remoteAdapter = new RemoteWorldCoreAdapter({
        baseURL: 'http://127.0.0.1:59999',
      });

      const server = createAssistantServer({
        worldCore: remoteAdapter,
        metaModel: mockMeta,
      });

      await server.start(0, '127.0.0.1');
      const addr = server.server.address() as any;
      const url = `http://127.0.0.1:${addr.port}`;

      try {
        const res = await fetch(`${url}/health`);
        assert.strictEqual(res.status, 200);
        const data = (await res.json()) as any;

        assert.strictEqual(data.status, 'ok');
        assert.strictEqual(data.worldCore.mode, 'remote');
        assert.strictEqual(data.worldCore.reachability, 'disconnected');
        assert.strictEqual(data.worldCore.connected, false);
      } finally {
        await server.stop();
      }
    });

    // 3. Remote World Core times out within configured bound (<= 500 ms)
    it('times out and reports disconnected within strict bound when World Core hangs', async () => {
      const hangingWcServer = createHttpServer((_req, res) => {
        // Deliberately delay response for 1200ms to test timeout cancellation
        setTimeout(() => {
          if (!res.writableEnded) {
            res.writeHead(200, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({ success: true, result: {} }));
          }
        }, 1200);
      });

      await new Promise<void>((resolve) => hangingWcServer.listen(0, '127.0.0.1', () => resolve()));
      const wcPort = (hangingWcServer.address() as any).port;

      const remoteAdapter = new RemoteWorldCoreAdapter({
        baseURL: `http://127.0.0.1:${wcPort}`,
      });

      const server = createAssistantServer({
        worldCore: remoteAdapter,
        metaModel: mockMeta,
      });

      await server.start(0, '127.0.0.1');
      const addr = server.server.address() as any;
      const url = `http://127.0.0.1:${addr.port}`;

      try {
        const start = performance.now();
        const res = await fetch(`${url}/health`);
        const elapsed = performance.now() - start;

        assert.strictEqual(res.status, 200);
        const data = (await res.json()) as any;

        assert.strictEqual(data.worldCore.mode, 'remote');
        assert.strictEqual(data.worldCore.reachability, 'disconnected');
        assert.strictEqual(data.worldCore.connected, false);

        // Strict bound: 500ms timeout should resolve around ~500-750ms, well under the 1200ms server delay
        assert.ok(elapsed < 900, `Probe must cancel within bound; took ${elapsed}ms`);
      } finally {
        await server.stop();
        await new Promise<void>((resolve) => hangingWcServer.close(() => resolve()));
      }
    });

    // 4. In-memory mode reports a fixture, not a live connection
    it('reports in-memory fixture and not-applicable reachability in default in-memory mode', async () => {
      const res = await fetch(`${baseUrl}/health`);
      assert.strictEqual(res.status, 200);
      const data = (await res.json()) as any;

      assert.strictEqual(data.status, 'ok');
      assert.strictEqual(data.worldCore.mode, 'in-memory');
      assert.strictEqual(data.worldCore.reachability, 'not-applicable');
      assert.strictEqual(data.worldCore.connected, false, 'Must not claim a live connection in fixture mode');
    });

    // 5. Provider mode accurately distinguishes live Meta API and local mock
    it('accurately distinguishes live Meta API and local mock in provider mode', async () => {
      // A. Mock Meta Model
      const mockRes = await fetch(`${baseUrl}/health`);
      const mockData = (await mockRes.json()) as any;
      assert.strictEqual(mockData.metaProvider, 'local-mock');

      // B. Live Meta API Adapter (mocked without actual remote calls)
      let networkCalled = false;
      const dummyLiveAdapter = new MetaModelApiAdapter({
        config: {
          apiKey: 'mock-meta-key-for-test',
          baseURL: 'https://api.meta.test/v1',
          model: 'muse-spark-1.3',
          isConfigured: true,
        },
        fetchFn: (async () => {
          networkCalled = true;
          return new Response('{}');
        }) as any,
      });

      const liveMetaServer = createAssistantServer({
        worldCore: inMemoryWorldCore,
        metaModel: dummyLiveAdapter,
        config: {
          ...loadConfig(),
          meta: {
            apiKey: 'mock-meta-key-for-test',
            baseURL: 'https://api.meta.test/v1',
            model: 'muse-spark-1.3',
            isConfigured: true,
          },
        },
      });

      await liveMetaServer.start(0, '127.0.0.1');
      const addr = liveMetaServer.server.address() as any;
      const liveUrl = `http://127.0.0.1:${addr.port}`;

      try {
        const res = await fetch(`${liveUrl}/health`);
        const data = (await res.json()) as any;

        assert.strictEqual(data.metaProvider, 'live-api');
        assert.strictEqual(data.metaModelConfigured, true);
        assert.strictEqual(data.model, 'muse-spark-1.3');
        assert.strictEqual(networkCalled, false, 'Health check must never trigger paid/live Meta API calls');
      } finally {
        await liveMetaServer.stop();
      }
    });

    // 6. Browser badge displays the correct state for each health response
    it('delivers accessible browser badge markup, styles, and state-mapping logic', async () => {
      const htmlRes = await fetch(`${baseUrl}/`);
      const html = await htmlRes.text();
      assert.ok(html.includes('id="sim-badge"'), 'HTML must have id="sim-badge"');
      assert.ok(html.includes('role="button"'), 'HTML badge must have role="button"');
      assert.ok(html.includes('tabindex="0"'), 'HTML badge must be keyboard focusable');

      const cssRes = await fetch(`${baseUrl}/style.css`);
      const css = await cssRes.text();
      assert.ok(css.includes('.sim-badge.connected'), 'CSS must define connected badge style');
      assert.ok(css.includes('.sim-badge.fixture'), 'CSS must define fixture badge style');
      assert.ok(css.includes('.sim-badge.offline'), 'CSS must define offline badge style');
      assert.ok(css.includes('.sim-badge:focus-visible'), 'CSS must define accessible focus ring');

      const jsRes = await fetch(`${baseUrl}/app.js`);
      const js = await jsRes.text();
      assert.ok(js.includes('renderSimulationBadge'), 'app.js must implement renderSimulationBadge');
      assert.ok(js.includes('checkSimulationHealth'), 'app.js must implement checkSimulationHealth');
    });

    // 7. English/Spanish changes update all badge states
    it('provides complete bilingual localization for all badge states in app.js', async () => {
      const jsRes = await fetch(`${baseUrl}/app.js`);
      const js = await jsRes.text();

      // Check English localization
      assert.ok(js.includes("simStatusConnected: 'World Core Connected (Remote)'"), 'Must have EN live status');
      assert.ok(js.includes("simStatusFixture: 'World Core Fixture (Standalone)'"), 'Must have EN fixture status');
      assert.ok(js.includes("simStatusOffline: 'World Core Offline (Retry)'"), 'Must have EN offline status');

      // Check Spanish localization
      assert.ok(js.includes("simStatusConnected: 'World Core Conectado (Remoto)'"), 'Must have ES live status');
      assert.ok(js.includes("simStatusFixture: 'World Core Ficticio (Sin Conexión)'"), 'Must have ES fixture status');
      assert.ok(js.includes("simStatusOffline: 'World Core Desconectado (Reintentar)'"), 'Must have ES offline status');

      // Check that updateLanguage updates badge
      assert.ok(js.includes('renderSimulationBadge()'), 'updateLanguage must re-render simulation badge');
    });

    // 8. A health-probe failure does not crash the assistant server or expose secrets
    it('does not expose secrets or stack traces on failed probe and keeps server running', async () => {
      const secretKey = 'super-secret-token-do-not-leak-9988';
      const failingAdapter = new RemoteWorldCoreAdapter({
        baseURL: 'http://127.0.0.1:59998',
      });

      const server = createAssistantServer({
        worldCore: failingAdapter,
        metaModel: mockMeta,
        config: {
          ...loadConfig(),
          meta: {
            apiKey: secretKey,
            baseURL: 'https://api.meta.test',
            model: 'muse-spark-1.3',
            isConfigured: true,
          },
        },
      });

      await server.start(0, '127.0.0.1');
      const addr = server.server.address() as any;
      const url = `http://127.0.0.1:${addr.port}`;

      try {
        const res = await fetch(`${url}/health`);
        assert.strictEqual(res.status, 200);
        const text = await res.text();

        assert.ok(!text.includes(secretKey), 'Must never leak API key in health response');
        assert.ok(!text.includes('ECONNREFUSED'), 'Must not leak raw internal error names');
        assert.ok(!text.includes('at '), 'Must not leak stack traces in response');

        // Verify server is still operational for subsequent requests
        const corridorRes = await fetch(`${url}/api/spatial/corridor`);
        assert.strictEqual(corridorRes.status, 200);
      } finally {
        await server.stop();
      }
    });

    // 9. Existing query behavior still suppresses AI generation when authoritative World Core data is unavailable
    it('suppresses AI generation when authoritative World Core data is unavailable during assistant query', async () => {
      const failingWorldCore: WorldCorePort = {
        async getBusinessSchedule() {
          throw new WorldCoreConnectionError('World Core connection refused');
        },
        async getCurrentWorldTime() {
          throw new WorldCoreConnectionError('World Core connection refused');
        },
        async getBusinessOperationalStatus() {
          throw new WorldCoreConnectionError('World Core connection refused');
        },
        async getBusinessScheduleSnapshot() {
          throw new WorldCoreConnectionError('World Core connection refused');
        },
      };

      const failingServer = createAssistantServer({
        worldCore: failingWorldCore,
        metaModel: mockMeta,
      });

      await failingServer.start(0, '127.0.0.1');
      const addr = failingServer.server.address() as any;
      const url = `http://127.0.0.1:${addr.port}`;

      try {
        const callsBefore = mockMeta.callCount;
        const res = await fetch(`${url}/api/assistant/query`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            businessId: 'restaurant:wing-spot',
            question: 'What are your hours today?',
          }),
        });

        assert.strictEqual(res.status, 502);
        const data = (await res.json()) as any;
        assert.strictEqual(data.success, false);
        assert.strictEqual(data.error.code, 'world-core-unavailable');
        assert.strictEqual(
          mockMeta.callCount,
          callsBefore,
          'AI generation must be strictly suppressed when World Core is unavailable'
        );
      } finally {
        await failingServer.stop();
      }
    });
  });
});

