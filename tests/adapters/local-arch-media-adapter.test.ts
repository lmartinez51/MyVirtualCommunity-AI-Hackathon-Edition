import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { tmpdir } from 'node:os';
import { LocalArchMediaAdapter } from '../../src/adapters/local-arch-media-adapter.js';
import { isSafeArchMediaFileName } from '../../src/domain/arch-playlist.js';

describe('LocalArchMediaAdapter (Unit Tests)', () => {
  it('validates allowed flat video filenames and rejects path traversal attempts', () => {
    assert.strictEqual(isSafeArchMediaFileName('camila_loop.mp4'), true);
    assert.strictEqual(isSafeArchMediaFileName('sponsor-2026.MOV'), true);
    assert.strictEqual(isSafeArchMediaFileName('../secret.mp4'), false);
    assert.strictEqual(isSafeArchMediaFileName('sub/video.mp4'), false);
    assert.strictEqual(isSafeArchMediaFileName('script.exe'), false);
    assert.strictEqual(isSafeArchMediaFileName('readme.txt'), false);
  });

  it('discovers .mp4 clips dynamically, computes sha256, and applies optional playlist.json order and windows', async () => {
    const dir = mkdtempSync(join(tmpdir(), 'mvc-arch-media-'));
    try {
      writeFileSync(join(dir, 'camila_loop.mp4'), Buffer.from('FAKE_MP4_CAMILA_DATA'));
      writeFileSync(join(dir, 'sponsor_40s.mp4'), Buffer.from('FAKE_MP4_SPONSOR_DATA'));
      writeFileSync(join(dir, 'expired_event.mp4'), Buffer.from('FAKE_MP4_EXPIRED'));
      writeFileSync(join(dir, 'notes.txt'), 'ignore me');

      writeFileSync(
        join(dir, 'playlist.json'),
        JSON.stringify({
          items: [
            {
              fileName: 'sponsor_40s.mp4',
              title: 'Gran Venta Centro',
              sponsor: 'Comercio Juárez',
              order: 1,
              startAt: '2026-11-01T00:00:00Z',
              endAt: '2026-11-30T23:59:59Z',
            },
            {
              fileName: 'camila_loop.mp4',
              title: 'Camila Concierto',
              sponsor: 'Cartelera',
              order: 2,
            },
            {
              fileName: 'expired_event.mp4',
              order: 3,
              startAt: '2026-01-01T00:00:00Z',
              endAt: '2026-02-01T00:00:00Z',
            },
          ],
        })
      );

      const playlist = await new LocalArchMediaAdapter({ mediaDir: dir }).getActivePlaylist('2026-11-17T22:00:00.000Z');
      assert.strictEqual(playlist.source, 'local-arch-media');
      assert.strictEqual(playlist.items.length, 2);
      assert.strictEqual(playlist.items[0]!.fileName, 'sponsor_40s.mp4');
      assert.strictEqual(playlist.items[0]!.sponsor, 'Comercio Juárez');
      assert.strictEqual(playlist.items[0]!.url, '/api/media/arch/sponsor_40s.mp4');
      assert.strictEqual(playlist.items[0]!.sha256.length, 64);
      assert.strictEqual(playlist.items[1]!.fileName, 'camila_loop.mp4');
    } finally {
      rmSync(dir, { recursive: true, force: true });
    }
  });
});
