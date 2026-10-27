import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  RemoteWorldCoreAdapter,
  WorldCoreConnectionError,
  WorldCoreTimeoutError,
  WorldCoreHttpError,
  WorldCoreResponseValidationError,
} from '../../src/adapters/remote-world-core-adapter.js';
import { createBusinessId } from '../../src/domain/business-id.js';

describe('RemoteWorldCoreAdapter (Unit Tests with Mocked HTTP)', () => {
  const TEST_BASE_URL = 'http://127.0.0.1:3001';
  const WING_SPOT_ID = createBusinessId('restaurant:wing-spot');

  const VALID_SCHEDULE_RESPONSE = {
    success: true,
    result: {
      businessId: 'restaurant:wing-spot',
      name: 'Wing Spot Gómez Morín',
      timeZone: 'America/Ciudad_Juarez',
      worldTime: '2026-10-24T18:00:00.000Z',
      schedule: {
        operatingDays: [1, 2, 3, 4, 5, 6],
        hours: {
          openHour: 9,
          openMinute: 0,
          closeHour: 18,
          closeMinute: 0,
        },
        lastEntryCutoffMinutes: 15,
        closureOverrides: [],
        version: 1,
      },
      operationalStatus: {
        status: 'open',
        isOpen: true,
        admissionPermitted: true,
        code: 'permitted',
        message: 'Ingreso permitido.',
        openTimeStr: '09:00',
        closingTimeStr: '18:00',
      },
    },
  };

  const VALID_STATUS_RESPONSE = {
    success: true,
    result: {
      ready: true,
      worldId: 'gomez-morin-world',
      schemaVersion: 19,
      worldTime: '2026-10-24T18:00:00.000Z',
      activePlayerId: 'agent:gomez-morin-player',
      status: 'RUNNING',
    },
  };

  function createMockFetch(
    handler: (url: string, options?: any) => Promise<{ status: number; body: string }>
  ): typeof fetch {
    return (async (input: any, init?: any): Promise<Response> => {
      const urlStr = typeof input === 'string' ? input : input.toString();
      const { status, body } = await handler(urlStr, init);
      return new Response(body, {
        status,
        headers: { 'Content-Type': 'application/json' },
      });
    }) as unknown as typeof fetch;
  }

  describe('1. URL Normalization and Read-Only Construction', () => {
    it('normalizes base URL by stripping trailing slashes', () => {
      const adapter = new RemoteWorldCoreAdapter({ baseURL: 'http://localhost:3001///' });
      assert.strictEqual(adapter.getBaseURL(), 'http://localhost:3001');
    });

    it('rejects empty or whitespace baseURL', () => {
      assert.throws(() => new RemoteWorldCoreAdapter({ baseURL: '   ' }), {
        message: /non-empty baseURL/,
      });
    });
  });

  describe('2. getBusinessSchedule', () => {
    it('returns validated domain BusinessSchedule on HTTP 200', async () => {
      let requestedUrl = '';
      let requestedMethod = '';

      const mockFetch = createMockFetch(async (url, init) => {
        requestedUrl = url;
        requestedMethod = init?.method ?? 'GET';
        return { status: 200, body: JSON.stringify(VALID_SCHEDULE_RESPONSE) };
      });

      const adapter = new RemoteWorldCoreAdapter({ baseURL: TEST_BASE_URL, fetchFn: mockFetch });
      const schedule = await adapter.getBusinessSchedule(WING_SPOT_ID);

      assert.ok(schedule, 'Schedule must not be null');
      assert.strictEqual(schedule.businessId, WING_SPOT_ID);
      assert.strictEqual(schedule.name, 'Wing Spot Gómez Morín');
      assert.strictEqual(schedule.timeZone, 'America/Ciudad_Juarez');
      assert.deepStrictEqual(schedule.operatingDays, [1, 2, 3, 4, 5, 6]);
      assert.strictEqual(schedule.hours.openHour, 9);
      assert.strictEqual(schedule.hours.closeHour, 18);
      assert.strictEqual(schedule.lastEntryCutoffMinutes, 15);

      assert.strictEqual(
        requestedUrl,
        `${TEST_BASE_URL}/api/businesses/restaurant%3Awing-spot/schedule`
      );
      assert.strictEqual(requestedMethod, 'GET');
    });

    it('returns null on HTTP 404 with business-not-found error', async () => {
      const mockFetch = createMockFetch(async () => {
        return {
          status: 404,
          body: JSON.stringify({
            success: false,
            error: { code: 'business-not-found', message: 'Business not found' },
          }),
        };
      });

      const adapter = new RemoteWorldCoreAdapter({ baseURL: TEST_BASE_URL, fetchFn: mockFetch });
      const schedule = await adapter.getBusinessSchedule(createBusinessId('restaurant:unknown'));
      assert.strictEqual(schedule, null);
    });

    it('throws WorldCoreHttpError on HTTP 500 server error', async () => {
      const mockFetch = createMockFetch(async () => {
        return {
          status: 500,
          body: JSON.stringify({
            success: false,
            error: { code: 'internal-error', message: 'Database crash' },
          }),
        };
      });

      const adapter = new RemoteWorldCoreAdapter({ baseURL: TEST_BASE_URL, fetchFn: mockFetch });
      await assert.rejects(
        () => adapter.getBusinessSchedule(WING_SPOT_ID),
        (err: unknown) => {
          assert.ok(err instanceof WorldCoreHttpError);
          assert.strictEqual(err.statusCode, 500);
          assert.strictEqual(err.errorCode, 'internal-error');
          assert.match(err.message, /Database crash/);
          return true;
        }
      );
    });

    it('throws WorldCoreResponseValidationError if payload contains invalid schedule schema', async () => {
      const invalidSchedule = {
        success: true,
        result: {
          businessId: 'restaurant:wing-spot',
          name: 'Wing Spot',
          timeZone: 'America/Ciudad_Juarez',
          schedule: {
            operatingDays: [99], // Invalid day
            hours: { openHour: 25, closeHour: 10 }, // Invalid hours
          },
        },
      };

      const mockFetch = createMockFetch(async () => ({
        status: 200,
        body: JSON.stringify(invalidSchedule),
      }));

      const adapter = new RemoteWorldCoreAdapter({ baseURL: TEST_BASE_URL, fetchFn: mockFetch });
      await assert.rejects(
        () => adapter.getBusinessSchedule(WING_SPOT_ID),
        (err: unknown) => {
          assert.ok(err instanceof WorldCoreResponseValidationError);
          return true;
        }
      );
    });

    it('throws WorldCoreResponseValidationError if body is not valid JSON', async () => {
      const mockFetch = createMockFetch(async () => ({
        status: 200,
        body: '<html>502 Bad Gateway</html>',
      }));

      const adapter = new RemoteWorldCoreAdapter({ baseURL: TEST_BASE_URL, fetchFn: mockFetch });
      await assert.rejects(
        () => adapter.getBusinessSchedule(WING_SPOT_ID),
        (err: unknown) => {
          assert.ok(err instanceof WorldCoreResponseValidationError);
          return true;
        }
      );
    });
  });

  describe('3. getCurrentWorldTime', () => {
    it('returns ISO timestamp from GET /api/status', async () => {
      const mockFetch = createMockFetch(async (url) => {
        assert.strictEqual(url, `${TEST_BASE_URL}/api/status`);
        return { status: 200, body: JSON.stringify(VALID_STATUS_RESPONSE) };
      });

      const adapter = new RemoteWorldCoreAdapter({ baseURL: TEST_BASE_URL, fetchFn: mockFetch });
      const time = await adapter.getCurrentWorldTime();
      assert.strictEqual(time, '2026-10-24T18:00:00.000Z');
    });

    it('throws WorldCoreResponseValidationError if status lacks worldTime', async () => {
      const mockFetch = createMockFetch(async () => ({
        status: 200,
        body: JSON.stringify({ success: true, result: { ready: true } }),
      }));

      const adapter = new RemoteWorldCoreAdapter({ baseURL: TEST_BASE_URL, fetchFn: mockFetch });
      await assert.rejects(
        () => adapter.getCurrentWorldTime(),
        (err: unknown) => {
          assert.ok(err instanceof WorldCoreResponseValidationError);
          return true;
        }
      );
    });
  });

  describe('4. getBusinessOperationalStatus', () => {
    it('derives domain BusinessOperationalStatus from remote response', async () => {
      const mockFetch = createMockFetch(async () => ({
        status: 200,
        body: JSON.stringify(VALID_SCHEDULE_RESPONSE),
      }));

      const adapter = new RemoteWorldCoreAdapter({ baseURL: TEST_BASE_URL, fetchFn: mockFetch });
      const status = await adapter.getBusinessOperationalStatus(WING_SPOT_ID);

      assert.ok(status, 'Status must not be null');
      assert.strictEqual(status.businessId, WING_SPOT_ID);
      assert.strictEqual(status.businessName, 'Wing Spot Gómez Morín');
      assert.strictEqual(status.status, 'OPEN');
      assert.strictEqual(status.isOpen, true);
      assert.strictEqual(status.admissionPermitted, true);
      assert.strictEqual(status.openTimeCivil, '09:00');
      assert.strictEqual(status.closeTimeCivil, '18:00');
      assert.strictEqual(status.cutoffTimeCivil, '17:45');
      assert.strictEqual(status.timeZone, 'America/Ciudad_Juarez');
      assert.strictEqual(status.summaryMessage, 'Ingreso permitido.');
    });

    it('maps closing-soon status correctly and includes ?at= query parameter', async () => {
      let requestedUrl = '';
      const closingSoonResponse = structuredClone(VALID_SCHEDULE_RESPONSE);
      closingSoonResponse.result.worldTime = '2026-10-24T23:50:00.000Z';
      closingSoonResponse.result.operationalStatus.status = 'closing-soon';
      closingSoonResponse.result.operationalStatus.isOpen = true;
      closingSoonResponse.result.operationalStatus.admissionPermitted = false;
      closingSoonResponse.result.operationalStatus.message = 'El establecimiento está por cerrar.';

      const mockFetch = createMockFetch(async (url) => {
        requestedUrl = url;
        return { status: 200, body: JSON.stringify(closingSoonResponse) };
      });

      const adapter = new RemoteWorldCoreAdapter({ baseURL: TEST_BASE_URL, fetchFn: mockFetch });
      const status = await adapter.getBusinessOperationalStatus(
        WING_SPOT_ID,
        '2026-10-24T23:50:00.000Z'
      );

      assert.ok(status);
      assert.strictEqual(status.status, 'CLOSING_SOON');
      assert.strictEqual(status.isOpen, true);
      assert.strictEqual(status.admissionPermitted, false);
      assert.match(requestedUrl, /\?at=2026-10-24T23%3A50%3A00\.000Z/);
    });

    it('returns null if business is not found (404)', async () => {
      const mockFetch = createMockFetch(async () => ({
        status: 404,
        body: JSON.stringify({ success: false, error: { code: 'business-not-found' } }),
      }));

      const adapter = new RemoteWorldCoreAdapter({ baseURL: TEST_BASE_URL, fetchFn: mockFetch });
      const status = await adapter.getBusinessOperationalStatus(createBusinessId('restaurant:missing'));
      assert.strictEqual(status, null);
    });
  });

  describe('4b. getBusinessScheduleSnapshot', () => {
    it('retrieves coherent snapshot with both schedule and operationalStatus via single HTTP request', async () => {
      let callCount = 0;
      const mockFetch = createMockFetch(async () => {
        callCount++;
        return {
          status: 200,
          body: JSON.stringify(VALID_SCHEDULE_RESPONSE),
        };
      });

      const adapter = new RemoteWorldCoreAdapter({ baseURL: TEST_BASE_URL, fetchFn: mockFetch });
      const snapshot = await adapter.getBusinessScheduleSnapshot(WING_SPOT_ID);

      assert.strictEqual(callCount, 1, 'Must execute exactly one HTTP request');
      assert.ok(snapshot);
      assert.strictEqual(snapshot.businessId, WING_SPOT_ID);
      assert.strictEqual(snapshot.businessName, 'Wing Spot Gómez Morín');
      assert.strictEqual(snapshot.timeZone, 'America/Ciudad_Juarez');
      assert.strictEqual(snapshot.worldTime, '2026-10-24T18:00:00.000Z');

      // Schedule definition
      assert.strictEqual(snapshot.schedule.name, 'Wing Spot Gómez Morín');
      assert.strictEqual(snapshot.schedule.hours.openHour, 9);
      assert.strictEqual(snapshot.schedule.hours.closeHour, 18);

      // Operational status
      assert.strictEqual(snapshot.operationalStatus.status, 'OPEN');
      assert.strictEqual(snapshot.operationalStatus.isOpen, true);
      assert.strictEqual(snapshot.operationalStatus.admissionPermitted, true);
    });

    it('returns null on 404 business-not-found', async () => {
      const mockFetch = createMockFetch(async () => ({
        status: 404,
        body: JSON.stringify({ success: false, error: { code: 'business-not-found' } }),
      }));

      const adapter = new RemoteWorldCoreAdapter({ baseURL: TEST_BASE_URL, fetchFn: mockFetch });
      const snapshot = await adapter.getBusinessScheduleSnapshot(createBusinessId('restaurant:nonexistent'));
      assert.strictEqual(snapshot, null);
    });
  });

  describe('5. Resilience & Isolation Guarantees', () => {
    it('throws WorldCoreConnectionError on network error and NEVER fabricates fallback mock data', async () => {
      const mockFetch: typeof fetch = (async () => {
        throw new Error('connect ECONNREFUSED 127.0.0.1:3001');
      }) as unknown as typeof fetch;

      const adapter = new RemoteWorldCoreAdapter({ baseURL: TEST_BASE_URL, fetchFn: mockFetch });

      await assert.rejects(
        () => adapter.getBusinessSchedule(WING_SPOT_ID),
        (err: unknown) => {
          assert.ok(err instanceof WorldCoreConnectionError);
          assert.match(err.message, /Failed to connect to World Core/);
          return true;
        }
      );

      await assert.rejects(
        () => adapter.getBusinessOperationalStatus(WING_SPOT_ID),
        (err: unknown) => {
          assert.ok(err instanceof WorldCoreConnectionError);
          return true;
        }
      );
    });

    it('throws WorldCoreTimeoutError when request aborts due to timeout', async () => {
      const mockFetch: typeof fetch = (async () => {
        const error = new Error('The operation was aborted');
        error.name = 'AbortError';
        throw error;
      }) as unknown as typeof fetch;

      const adapter = new RemoteWorldCoreAdapter({
        baseURL: TEST_BASE_URL,
        fetchFn: mockFetch,
        timeoutMs: 50,
      });

      await assert.rejects(
        () => adapter.getBusinessSchedule(WING_SPOT_ID),
        (err: unknown) => {
          assert.ok(err instanceof WorldCoreTimeoutError);
          assert.match(err.message, /timed out after 50ms/);
          return true;
        }
      );
    });
  });

  describe('7. probeHealth (Lightweight Reachability Probe)', () => {
    it('reports remote mode', () => {
      const adapter = new RemoteWorldCoreAdapter({ baseURL: TEST_BASE_URL });
      assert.strictEqual(adapter.mode, 'remote');
    });

    it('returns connected: true when /api/status responds 200 with success: true', async () => {
      let probedUrl = '';
      const mockFetch = createMockFetch(async (url) => {
        probedUrl = url;
        return { status: 200, body: JSON.stringify(VALID_STATUS_RESPONSE) };
      });

      const adapter = new RemoteWorldCoreAdapter({
        baseURL: TEST_BASE_URL,
        fetchFn: mockFetch,
      });

      const result = await adapter.probeHealth(500);
      assert.strictEqual(result.connected, true);
      assert.strictEqual(typeof result.latencyMs, 'number');
      assert.ok(probedUrl.endsWith('/api/status'));
    });

    it('returns connected: false when /api/status responds with HTTP error', async () => {
      const mockFetch = createMockFetch(async () => {
        return { status: 503, body: JSON.stringify({ error: 'Service Unavailable' }) };
      });

      const adapter = new RemoteWorldCoreAdapter({
        baseURL: TEST_BASE_URL,
        fetchFn: mockFetch,
      });

      const result = await adapter.probeHealth(500);
      assert.strictEqual(result.connected, false);
    });

    it('returns connected: false without throwing when connection is refused or aborted', async () => {
      const mockFetch: typeof fetch = (async () => {
        const err = new Error('ECONNREFUSED');
        throw err;
      }) as unknown as typeof fetch;

      const adapter = new RemoteWorldCoreAdapter({
        baseURL: TEST_BASE_URL,
        fetchFn: mockFetch,
      });

      const result = await adapter.probeHealth(500);
      assert.strictEqual(result.connected, false);
    });
  });
});

