import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { tmpdir } from 'node:os';
import { InMemoryWorldCoreAdapter } from '../../src/adapters/in-memory-world-core-adapter.js';
import { MockMetaModelAdapter } from '../../src/adapters/mock-meta-model-adapter.js';
import { createAssistantServer } from '../../src/server/index.js';

describe('Arch Billboard Playlist & Media Streaming API Integration Tests', () => {
  it('serves active playlist and streams MP4 bytes with HTTP Range 206 + traversal protection', async () => {
    const dir = mkdtempSync(join(tmpdir(), 'mvc-arch-api-'));
    const sampleBytes = Buffer.from('0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_MP4_BODY');
    writeFileSync(join(dir, 'camila_loop.mp4'), sampleBytes);

    const worldCore = new InMemoryWorldCoreAdapter();
    const metaModel = new MockMetaModelAdapter();
    const serverInstance = createAssistantServer({
      worldCore,
      metaModel,
      archMediaDir: dir,
    });
    await serverInstance.start(0, '127.0.0.1');
    const addr = serverInstance.server.address() as any;
    const baseUrl = `http://127.0.0.1:${addr.port}`;

    try {
      // 1. Playlist listing
      const listRes = await fetch(`${baseUrl}/api/world/arch-playlist`);
      assert.strictEqual(listRes.status, 200);
      const listJson = (await listRes.json()) as any;
      assert.strictEqual(listJson.success, true);
      assert.strictEqual(listJson.items.length, 1);
      assert.strictEqual(listJson.items[0].fileName, 'camila_loop.mp4');
      assert.strictEqual(listJson.items[0].sizeBytes, sampleBytes.length);

      // 2. Full download (200 OK)
      const fullRes = await fetch(`${baseUrl}${listJson.items[0].url}`);
      assert.strictEqual(fullRes.status, 200);
      assert.strictEqual(fullRes.headers.get('accept-ranges'), 'bytes');
      const fullBuf = Buffer.from(await fullRes.arrayBuffer());
      assert.deepStrictEqual(fullBuf, sampleBytes);

      // 3. Partial Range request (206 Partial Content)
      const rangeRes = await fetch(`${baseUrl}${listJson.items[0].url}`, {
        headers: { Range: 'bytes=10-19' },
      });
      assert.strictEqual(rangeRes.status, 206);
      assert.strictEqual(rangeRes.headers.get('content-range'), `bytes 10-19/${sampleBytes.length}`);
      const sliceText = await rangeRes.text();
      assert.strictEqual(sliceText, 'ABCDEFGHIJ');

      // 4. Rejects missing & traversal attempts safely with 404
      const missingRes = await fetch(`${baseUrl}/api/media/arch/nonexistent.mp4`);
      assert.strictEqual(missingRes.status, 404);
      const traversalRes = await fetch(`${baseUrl}/api/media/arch/..%2Fpackage.json`);
      assert.strictEqual(traversalRes.status, 404);
    } finally {
      await serverInstance.stop();
      rmSync(dir, { recursive: true, force: true });
    }
  });
});
