import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { InMemoryWorldCoreAdapter } from '../../src/adapters/in-memory-world-core-adapter.js';
import { RemoteWorldCoreAdapter } from '../../src/adapters/remote-world-core-adapter.js';
import { MockMetaModelAdapter } from '../../src/adapters/mock-meta-model-adapter.js';
import { createAssistantServer } from '../../src/server/index.js';
import {
  DEFAULT_PLAYER_SPAWN_POSITION,
  PLAYER_WALK_SPEED_MPS,
} from '../../src/domain/player-movement.js';

describe('Authoritative Player Movement & Trust Boundary API Tests (Increment 5.18.0.1)', () => {
  const normalDutyUtc = '2026-11-04T17:00:00.000Z';

  it('1. Normal movement updates canonical position strictly via server clock & ignores client timing/coord fields', async () => {
    let clockMs = 1_700_000_000_000;
    const nowFn = () => clockMs;

    const worldCore = new InMemoryWorldCoreAdapter({ nowFn });
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel, nowFn });
    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      // Advance server clock by 200ms while client tries to inject fake deltaTime/speed/position/timestamp
      clockMs += 200;
      const res = await fetch(`${url}/api/world/players/player-1/movement`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          direction: { x: 0, z: 1 },
          movementMode: 'walk',
          sequenceNumber: 1,
          // Forbidden/untrusted client assertions:
          position: { x: 19.2, y: 0.2, z: 39.5 },
          playerPosition: { x: 19.2, y: 0.2, z: 39.5 },
          deltaTime: 100.0,
          speed: 999.0,
          timestamp: '2099-01-01T00:00:00.000Z',
        }),
      });

      assert.strictEqual(res.status, 200);
      const data = (await res.json()) as any;
      assert.strictEqual(data.success, true);
      assert.strictEqual(data.accepted, true);
      assert.strictEqual(data.appliedSpeedMps, PLAYER_WALK_SPEED_MPS);
      assert.strictEqual(data.elapsedSeconds, 0.2);
      assert.strictEqual(data.player.position.x, DEFAULT_PLAYER_SPAWN_POSITION.x);
      assert.strictEqual(
        data.player.position.z,
        Number((DEFAULT_PLAYER_SPAWN_POSITION.z + PLAYER_WALK_SPEED_MPS * 0.2).toFixed(4))
      );

      // Verify alias /api/players/player-1/movement also works
      clockMs += 200;
      const aliasRes = await fetch(`${url}/api/players/player-1/movement`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          direction: { x: 0, z: 1 },
          movementMode: 'walk',
          sequenceNumber: 2,
        }),
      });
      assert.strictEqual(aliasRes.status, 200);
    } finally {
      await serverInstance.stop();
    }
  });

  it('2. Legacy /position route is disabled (403) and cannot overwrite canonical position to bypass 5m range', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel });
    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      // Attempt arbitrary teleport via old /position route
      const teleportRes = await fetch(`${url}/api/world/players/player-1/position`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ x: 19.2, y: 0.2, z: 39.5 }),
      });
      assert.strictEqual(teleportRes.status, 403);
      const teleportData = (await teleportRes.json()) as any;
      assert.strictEqual(teleportData.success, false);
      assert.strictEqual(teleportData.error.code, 'absolute-position-forbidden');

      const legacyAliasRes = await fetch(`${url}/api/players/player-1/position`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ x: 19.2, y: 0.2, z: 39.5 }),
      });
      assert.strictEqual(legacyAliasRes.status, 403);

      // Canonical position remains at spawn, so Elena interaction MUST remain rejected as out_of_range
      const interactRes = await fetch(`${url}/api/world/npcs/elena-border-concierge/interact?at=${encodeURIComponent(normalDutyUtc)}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          playerId: 'player-1',
          intent: 'talk',
          playerPosition: { x: 19.2, y: 0.2, z: 39.5 },
        }),
      });
      assert.strictEqual(interactRes.status, 200);
      const interactData = (await interactRes.json()) as any;
      assert.strictEqual(interactData.allowed, false);
      assert.strictEqual(interactData.reasonCode, 'out_of_range');
    } finally {
      await serverInstance.stop();
    }
  });

  it('3. Rejects unknown/unauthorized player IDs, malformed movement requests, and ignores out-of-order sequences', async () => {
    let clockMs = 1_700_000_000_000;
    const nowFn = () => clockMs;
    const worldCore = new InMemoryWorldCoreAdapter({ nowFn });
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore, metaModel, nowFn });
    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      // Unauthorized player IDs on GET and POST
      const unauthGet = await fetch(`${url}/api/world/players/other-player`);
      assert.strictEqual(unauthGet.status, 403);
      const unauthPost = await fetch(`${url}/api/world/players/other-player/movement`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ direction: { x: 0, z: 1 }, sequenceNumber: 1 }),
      });
      assert.strictEqual(unauthPost.status, 403);
      const unauthData = (await unauthPost.json()) as any;
      assert.strictEqual(unauthData.error.code, 'unauthorized-player-identity');

      // Malformed direction & movementMode
      const badDirRes = await fetch(`${url}/api/world/players/player-1/movement`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ direction: { x: 'north', z: 1 } }),
      });
      assert.strictEqual(badDirRes.status, 400);

      const badModeRes = await fetch(`${url}/api/world/players/player-1/movement`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ direction: { x: 0, z: 1 }, movementMode: 'teleport' }),
      });
      assert.strictEqual(badModeRes.status, 400);

      // Advance cleanly at sequenceNumber 10
      clockMs += 300;
      const seq10Res = await fetch(`${url}/api/world/players/player-1/movement`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ direction: { x: 0, z: 1 }, movementMode: 'walk', sequenceNumber: 10 }),
      });
      assert.strictEqual(seq10Res.status, 200);
      const seq10Data = (await seq10Res.json()) as any;
      assert.strictEqual(seq10Data.accepted, true);

      // Out-of-order sequenceNumber 5 must not corrupt position
      clockMs += 300;
      const seq5Res = await fetch(`${url}/api/world/players/player-1/movement`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ direction: { x: 1, z: 0 }, movementMode: 'sprint', sequenceNumber: 5 }),
      });
      assert.strictEqual(seq5Res.status, 200);
      const seq5Data = (await seq5Res.json()) as any;
      assert.strictEqual(seq5Data.accepted, false);
      assert.strictEqual(seq5Data.outOfOrder, true);
      assert.deepStrictEqual(seq5Data.player.position, seq10Data.player.position);
    } finally {
      await serverInstance.stop();
    }
  });

  it('4. RemoteWorldCoreAdapter does not fabricate player movement/state support when external service lacks it', async () => {
    const remoteAdapter = new RemoteWorldCoreAdapter({ baseURL: 'http://127.0.0.1:3999' });
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({ worldCore: remoteAdapter, metaModel });
    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const url = `http://127.0.0.1:${addr.port}`;

    try {
      const moveRes = await fetch(`${url}/api/world/players/player-1/movement`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ direction: { x: 0, z: 1 }, sequenceNumber: 1 }),
      });
      assert.strictEqual(moveRes.status, 501);

      const interactRes = await fetch(`${url}/api/world/npcs/elena-border-concierge/interact`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ playerId: 'player-1', intent: 'talk' }),
      });
      assert.strictEqual(interactRes.status, 501);
    } finally {
      await serverInstance.stop();
    }
  });
});
