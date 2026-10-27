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

describe('Milestone 2: Time-Specific Business Admission Integration Tests', () => {
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

  describe('1. Default vs Specific Time Query Behavior', () => {
    it('retains default current-time behavior when evaluationTime is omitted', async () => {
      const res = await fetch(`${baseUrl}/api/assistant/query`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          businessId: 'restaurant:wing-spot',
          question: 'Is Wing Spot Gómez Morín open right now?',
          language: 'en',
        }),
      });

      assert.equal(res.status, 200);
      const data = (await res.json()) as any;
      assert.equal(data.success, true);
      assert.equal(data.evaluationMode, 'current');
      assert.ok(data.evaluatedAtUtc);
      assert.ok(data.evaluatedAtCivil);
      assert.equal(data.verifiedStatus.businessName, 'Wing Spot Gómez Morín');
      assert.equal(mockMeta.callCount, 1);
    });

    it('engages specific evaluation mode and reports local civil time when evaluationTime is supplied', async () => {
      const res = await fetch(`${baseUrl}/api/assistant/query`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          businessId: 'restaurant:wing-spot',
          question: 'Will this business be open at 2:00 PM?',
          evaluationTime: '2026-10-24T14:00',
          language: 'en',
        }),
      });

      assert.equal(res.status, 200);
      const data = (await res.json()) as any;
      assert.equal(data.success, true);
      assert.equal(data.evaluationMode, 'specific');
      assert.equal(data.evaluatedAtUtc, '2026-10-24T20:00:00.000Z');
      assert.match(data.evaluatedAtCivil, /2026-10-24T14:00:00\.000-06:00/);
      assert.equal(data.verifiedStatus.isOpen, true);
      assert.equal(data.verifiedStatus.admissionPermitted, true);
    });
  });

  describe('2. Critical Business Rules & Admission Status Scenarios', () => {
    it('Scenario A: Admission permitted before cutoff (Saturday 17:40 local)', async () => {
      // 17:40 is 5 minutes before the 17:45 cutoff
      const res = await fetch(`${baseUrl}/api/assistant/query`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          businessId: 'restaurant:wing-spot',
          question: 'Would I still be admitted if I arrive at 5:40 PM?',
          evaluationTime: '2026-10-24T17:40',
          language: 'en',
        }),
      });

      assert.equal(res.status, 200);
      const data = (await res.json()) as any;
      assert.equal(data.success, true);
      assert.equal(data.verifiedStatus.status, 'OPEN');
      assert.equal(data.verifiedStatus.isOpen, true);
      assert.equal(data.verifiedStatus.admissionPermitted, true);
      assert.equal(data.verifiedStatus.cutoffTimeCivil, '17:45');
      assert.equal(data.verifiedStatus.closeTimeCivil, '18:00');

      // Model explanation is grounded in authoritative facts
      assert.match(data.answer, /Wing Spot Gómez Morín/);
      assert.match(data.answer, /OPEN/);
      assert.match(data.answer, /17:45/);
    });

    it('Scenario B: Business open but admission denied after cutoff (Saturday 17:50 local)', async () => {
      // 17:50 is past the 17:45 cutoff, but before the 18:00 close
      const res = await fetch(`${baseUrl}/api/assistant/query`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          businessId: 'restaurant:wing-spot',
          question: 'Would I still be admitted if I arrive at 5:50 PM?',
          evaluationTime: '2026-10-24T17:50',
          language: 'en',
        }),
      });

      assert.equal(res.status, 200);
      const data = (await res.json()) as any;
      assert.equal(data.success, true);
      assert.equal(data.verifiedStatus.status, 'CLOSING_SOON');
      assert.equal(data.verifiedStatus.isOpen, true);
      assert.equal(data.verifiedStatus.admissionPermitted, false);

      // Distinguishes open status from admission eligibility
      assert.match(data.answer, /Wing Spot Gómez Morín/);
      assert.match(data.answer, /CLOSING SOON|closing soon/i);
      assert.match(data.answer, /no longer permitted/i);
    });

    it('Scenario C: Business closed after scheduled closing time (Saturday 18:05 local)', async () => {
      // 18:05 is past the 18:00 closing hour
      const res = await fetch(`${baseUrl}/api/assistant/query`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          businessId: 'restaurant:wing-spot',
          question: 'Will this business be open at 6:05 PM?',
          evaluationTime: '2026-10-24T18:05',
          language: 'en',
        }),
      });

      assert.equal(res.status, 200);
      const data = (await res.json()) as any;
      assert.equal(data.success, true);
      assert.equal(data.verifiedStatus.status, 'CLOSED');
      assert.equal(data.verifiedStatus.isOpen, false);
      assert.equal(data.verifiedStatus.admissionPermitted, false);

      assert.match(data.answer, /Wing Spot Gómez Morín/);
      assert.match(data.answer, /CLOSED/);
    });

    it('Scenario D: Non-operating day (Sunday 12:00 local)', async () => {
      // Wing Spot is closed on Sundays (operatingDays = [1, 2, 3, 4, 5, 6])
      const res = await fetch(`${baseUrl}/api/assistant/query`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          businessId: 'restaurant:wing-spot',
          question: 'Is Wing Spot open on Sunday at 12:00 PM?',
          evaluationTime: '2026-10-25T12:00',
          language: 'en',
        }),
      });

      assert.equal(res.status, 200);
      const data = (await res.json()) as any;
      assert.equal(data.success, true);
      assert.equal(data.verifiedStatus.status, 'CLOSED');
      assert.equal(data.verifiedStatus.isOpen, false);
      assert.equal(data.verifiedStatus.admissionPermitted, false);
      assert.match(data.verifiedStatus.summaryMessage, /closed on Sunday/i);
    });

    it('Scenario E: Bilingual Spanish admission query (¿Todavía me dejarán entrar a las 5:50 p. m.?)', async () => {
      const res = await fetch(`${baseUrl}/api/assistant/query`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          businessId: 'restaurant:wing-spot',
          question: '¿Todavía me dejarán entrar si llego a las 5:50 p. m.?',
          evaluationTime: '2026-10-24T17:50',
          language: 'es',
        }),
      });

      assert.equal(res.status, 200);
      const data = (await res.json()) as any;
      assert.equal(data.success, true);
      assert.equal(data.language, 'es');
      assert.equal(data.verifiedStatus.businessName, 'Wing Spot Gómez Morín');
      assert.equal(data.verifiedStatus.status, 'CLOSING_SOON');
      assert.equal(data.verifiedStatus.admissionPermitted, false);

      // Model instructions and response in Spanish, preserving exact canonical name
      assert.match(data.answer, /Wing Spot Gómez Morín/);
      assert.match(data.answer, /no se permite el ingreso/i);
      assert.equal(mockMeta.lastContextReceived?.systemPrompt?.includes('Responde en español'), true);
      assert.equal(mockMeta.lastContextReceived?.systemPrompt?.includes('Conserva los nombres comerciales canónicos'), true);
    });
  });

  describe('3. Validation, Malformed Requests, and DST Handling', () => {
    it('returns 400 invalid-evaluation-time for malformed date string', async () => {
      const res = await fetch(`${baseUrl}/api/assistant/query`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          businessId: 'restaurant:wing-spot',
          question: 'Is it open?',
          evaluationTime: 'invalid-date-format',
        }),
      });

      assert.equal(res.status, 400);
      const data = (await res.json()) as any;
      assert.equal(data.success, false);
      assert.equal(data.error.code, 'invalid-evaluation-time');
    });

    it('returns 400 invalid-evaluation-time for impossible calendar dates (e.g. Feb 30)', async () => {
      const res = await fetch(`${baseUrl}/api/assistant/query`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          businessId: 'restaurant:wing-spot',
          question: 'Is it open on Feb 30?',
          evaluationTime: '2026-02-30T12:00',
        }),
      });

      assert.equal(res.status, 400);
      const data = (await res.json()) as any;
      assert.equal(data.success, false);
      assert.equal(data.error.code, 'invalid-evaluation-time');
      assert.match(data.error.message, /Calendar date does not exist/);
    });

    it('returns 400 invalid-evaluation-time for non-existent time during DST spring forward gap', async () => {
      // 02:30 on March 8, 2026 does not exist in America/Ciudad_Juarez due to 1-hour jump
      const res = await fetch(`${baseUrl}/api/assistant/query`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          businessId: 'restaurant:wing-spot',
          question: 'Is it open during DST transition?',
          evaluationTime: '2026-03-08T02:30',
        }),
      });

      assert.equal(res.status, 400);
      const data = (await res.json()) as any;
      assert.equal(data.success, false);
      assert.equal(data.error.code, 'invalid-evaluation-time');
      assert.match(data.error.message, /spring-forward/i);
    });

    it('flags isAmbiguousEvaluationTime and returns clear disambiguation notice during DST fall-back overlap', async () => {
      // 01:30 on November 1, 2026 occurs twice in America/Ciudad_Juarez: 07:30 UTC (-06:00) and 08:30 UTC (-07:00)
      const res = await fetch(`${baseUrl}/api/assistant/query`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          businessId: 'restaurant:wing-spot',
          question: 'Is it open during autumn DST transition?',
          evaluationTime: '2026-11-01T01:30',
        }),
      });

      assert.equal(res.status, 200);
      const data = (await res.json()) as any;
      assert.equal(data.success, true);
      assert.equal(data.isAmbiguousEvaluationTime, true);
      assert.ok(data.disambiguationNotice);
      assert.match(data.disambiguationNotice, /earlier occurrence/i);
      assert.equal(data.evaluatedAtUtc, '2026-11-01T07:30:00.000Z');
      assert.equal(data.evaluatedAtCivil, '2026-11-01T01:30:00.000-06:00');
    });

    it('correctly maps seasonal local times to UTC before and after DST transition', async () => {
      // Oct 31, 2026 at 17:50 local (MDT UTC-6) -> 23:50 UTC
      const resBefore = await fetch(`${baseUrl}/api/assistant/query`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          businessId: 'restaurant:wing-spot',
          question: 'Status before DST change',
          evaluationTime: '2026-10-31T17:50',
        }),
      });
      const dataBefore = (await resBefore.json()) as any;
      assert.equal(dataBefore.evaluatedAtUtc, '2026-10-31T23:50:00.000Z');
      assert.equal(dataBefore.evaluatedAtCivil, '2026-10-31T17:50:00.000-06:00');

      // Nov 1, 2026 at 12:00 local (MST UTC-7) -> 19:00 UTC
      const resAfter = await fetch(`${baseUrl}/api/assistant/query`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          businessId: 'restaurant:wing-spot',
          question: 'Status after DST change',
          evaluationTime: '2026-11-01T12:00',
        }),
      });
      const dataAfter = (await resAfter.json()) as any;
      assert.equal(dataAfter.evaluatedAtUtc, '2026-11-01T19:00:00.000Z');
      assert.equal(dataAfter.evaluatedAtCivil, '2026-11-01T12:00:00.000-07:00');
    });
  });

  describe('4. Architecture, Invariants, and Failure Modes', () => {
    it('requests exactly ONE coherent snapshot read per accepted query', async () => {
      let snapshotReadCount = 0;
      const trackingWorldCore: WorldCorePort = {
        async getBusinessSchedule(id: BusinessId) {
          return inMemoryWorldCore.getBusinessSchedule(id);
        },
        async getCurrentWorldTime() {
          return inMemoryWorldCore.getCurrentWorldTime();
        },
        async getBusinessOperationalStatus(id: BusinessId, evaluatedAt?: Date | string | number) {
          return inMemoryWorldCore.getBusinessOperationalStatus(id, evaluatedAt);
        },
        async getBusinessScheduleSnapshot(id: BusinessId, evaluatedAt?: Date | string | number) {
          snapshotReadCount++;
          return inMemoryWorldCore.getBusinessScheduleSnapshot(id, evaluatedAt);
        },
      };

      const serverWithTracking = createAssistantServer({
        worldCore: trackingWorldCore,
        metaModel: mockMeta,
      });

      await serverWithTracking.start(0, '127.0.0.1');
      const trackingAddr = serverWithTracking.server.address();
      const trackingPort = trackingAddr && typeof trackingAddr === 'object' ? trackingAddr.port : 0;
      try {
        const res = await fetch(`http://127.0.0.1:${trackingPort}/api/assistant/query`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            businessId: 'restaurant:wing-spot',
            question: 'Would I still be admitted at 5:50 PM?',
            evaluationTime: '2026-10-24T17:50',
          }),
        });

        assert.equal(res.status, 200);
        assert.equal(snapshotReadCount, 1, 'Must execute exactly ONE snapshot read per accepted query');
        assert.equal(mockMeta.callCount, 1, 'Must execute exactly ONE Meta inference per accepted query');
      } finally {
        await serverWithTracking.stop();
      }
    });

    it('returns 502 and does NOT invoke Meta when World Core connection fails', async () => {
      const failingWorldCore: WorldCorePort = {
        async getBusinessSchedule() { throw new WorldCoreConnectionError('Simulation server connection refused'); },
        async getCurrentWorldTime() { throw new WorldCoreConnectionError('Simulation server connection refused'); },
        async getBusinessOperationalStatus() { throw new WorldCoreConnectionError('Simulation server connection refused'); },
        async getBusinessScheduleSnapshot() { throw new WorldCoreConnectionError('Simulation server connection refused'); },
      };

      const serverWithFailure = createAssistantServer({
        worldCore: failingWorldCore,
        metaModel: mockMeta,
      });

      await serverWithFailure.start(0, '127.0.0.1');
      const failureAddr = serverWithFailure.server.address();
      const failurePort = failureAddr && typeof failureAddr === 'object' ? failureAddr.port : 0;
      try {
        const res = await fetch(`http://127.0.0.1:${failurePort}/api/assistant/query`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            businessId: 'restaurant:wing-spot',
            question: 'Would I still be admitted at 5:50 PM?',
            evaluationTime: '2026-10-24T17:50',
          }),
        });

        assert.equal(res.status, 502);
        const data = (await res.json()) as any;
        assert.equal(data.success, false);
        assert.equal(data.error.code, 'world-core-unavailable');
        assert.equal(mockMeta.callCount, 0, 'Must NOT invoke Meta inference when World Core fails');
      } finally {
        await serverWithFailure.stop();
      }
    });

    it('returns 502 and does NOT invoke Meta when World Core returns malformed response', async () => {
      const malformedWorldCore: WorldCorePort = {
        async getBusinessSchedule() { throw new WorldCoreResponseValidationError('Malformed JSON from World Core'); },
        async getCurrentWorldTime() { throw new WorldCoreResponseValidationError('Malformed JSON from World Core'); },
        async getBusinessOperationalStatus() { throw new WorldCoreResponseValidationError('Malformed JSON from World Core'); },
        async getBusinessScheduleSnapshot() { throw new WorldCoreResponseValidationError('Malformed JSON from World Core'); },
      };

      const serverWithMalformed = createAssistantServer({
        worldCore: malformedWorldCore,
        metaModel: mockMeta,
      });

      await serverWithMalformed.start(0, '127.0.0.1');
      const malformedAddr = serverWithMalformed.server.address();
      const malformedPort = malformedAddr && typeof malformedAddr === 'object' ? malformedAddr.port : 0;
      try {
        const res = await fetch(`http://127.0.0.1:${malformedPort}/api/assistant/query`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            businessId: 'restaurant:wing-spot',
            question: 'Would I still be admitted at 5:50 PM?',
            evaluationTime: '2026-10-24T17:50',
          }),
        });

        assert.equal(res.status, 502);
        const data = (await res.json()) as any;
        assert.equal(data.success, false);
        assert.equal(data.error.code, 'world-core-invalid-response');
        assert.equal(mockMeta.callCount, 0, 'Must NOT invoke Meta inference on malformed World Core data');
      } finally {
        await serverWithMalformed.stop();
      }
    });
  });
});
