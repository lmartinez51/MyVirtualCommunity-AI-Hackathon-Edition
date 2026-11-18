/**
 * Local Filesystem Arch Billboard Media Adapter.
 *
 * Manages sponsor & community video files placed inside `media/arch-ads/`
 * (or a configurable directory), optionally ordered and scheduled via `playlist.json`.
 *
 * Key behavior:
 * - Zero Unity recompilation needed: dropping a new `.mp4` into `media/arch-ads/`
 *   makes it immediately visible in `GET /api/world/arch-playlist` and servable at
 *   `GET /api/media/arch/:fileName` with HTTP Range (206 Partial Content) support.
 * - SHA-256 digests are cached by `(fileName, sizeBytes, mtimeMs)` to keep polling cheap.
 */

import { createHash } from 'node:crypto';
import { createReadStream, existsSync, mkdirSync, readdirSync, readFileSync, statSync } from 'node:fs';
import { basename, join, resolve } from 'node:path';
import {
  ArchMediaCampaignWindow,
  ArchPlaylistItem,
  ArchPlaylistSnapshot,
  ResolvedArchMediaFile,
  getAllowedArchVideoMimeType,
  isCampaignWindowActiveAt,
  isSafeArchMediaFileName,
} from '../domain/arch-playlist.js';
import { ArchMediaPort } from '../ports/arch-media-port.js';

export interface LocalArchMediaAdapterOptions {
  readonly mediaDir?: string;
  readonly pollRecommendedSeconds?: number;
}

interface RawPlaylistConfigEntry {
  readonly fileName?: unknown;
  readonly title?: unknown;
  readonly sponsor?: unknown;
  readonly order?: unknown;
  readonly durationHintSeconds?: unknown;
  readonly enabled?: unknown;
  readonly startAt?: unknown;
  readonly endAt?: unknown;
}

interface DigestCacheEntry {
  readonly sizeBytes: number;
  readonly mtimeMs: number;
  readonly sha256: string;
}

export class LocalArchMediaAdapter implements ArchMediaPort {
  private readonly mediaDir: string;
  private readonly pollRecommendedSeconds: number;
  private readonly digestCache = new Map<string, DigestCacheEntry>();

  constructor(options: LocalArchMediaAdapterOptions = {}) {
    this.mediaDir = resolve(options.mediaDir ?? join(process.cwd(), 'media', 'arch-ads'));
    this.pollRecommendedSeconds = options.pollRecommendedSeconds ?? 120;
    mkdirSync(this.mediaDir, { recursive: true });
  }

  public getMediaDir(): string {
    return this.mediaDir;
  }

  public async getActivePlaylist(evaluatedAtIso?: string): Promise<ArchPlaylistSnapshot> {
    const parsedMs = evaluatedAtIso ? Date.parse(evaluatedAtIso) : Date.now();
    const evalMs = Number.isFinite(parsedMs) ? parsedMs : Date.now();
    const generatedAt = new Date(evalMs).toISOString();

    if (!existsSync(this.mediaDir)) {
      return Object.freeze({
        generatedAt,
        source: 'local-arch-media',
        pollRecommendedSeconds: this.pollRecommendedSeconds,
        items: Object.freeze([]),
      });
    }

    const manifestEntries = this.readPlaylistConfig();
    const manifestByFile = new Map<string, RawPlaylistConfigEntry>();
    for (const entry of manifestEntries) {
      if (typeof entry.fileName === 'string' && isSafeArchMediaFileName(entry.fileName)) {
        manifestByFile.set(entry.fileName.toLowerCase(), entry);
      }
    }

    const dirFiles = readdirSync(this.mediaDir)
      .filter((name) => isSafeArchMediaFileName(name))
      .sort((a, b) => a.localeCompare(b, 'en', { sensitivity: 'base' }));

    const resolvedItems: ArchPlaylistItem[] = [];
    let defaultOrder = 1000;

    for (const fileName of dirFiles) {
      const cfg = manifestByFile.get(fileName.toLowerCase());
      if (cfg && cfg.enabled === false) {
        continue;
      }

      const activeWindow: ArchMediaCampaignWindow | null = cfg
        ? {
            startAt: typeof cfg.startAt === 'string' && cfg.startAt.trim() ? cfg.startAt.trim() : null,
            endAt: typeof cfg.endAt === 'string' && cfg.endAt.trim() ? cfg.endAt.trim() : null,
          }
        : null;

      if (!isCampaignWindowActiveAt(activeWindow, evalMs)) {
        continue;
      }

      const fileInfo = await this.resolveMediaFile(fileName);
      if (!fileInfo || fileInfo.sizeBytes <= 0) {
        continue;
      }

      const order =
        cfg && typeof cfg.order === 'number' && Number.isFinite(cfg.order) ? cfg.order : defaultOrder++;
      const title =
        cfg && typeof cfg.title === 'string' && cfg.title.trim().length > 0
          ? cfg.title.trim()
          : stripExtension(fileName);
      const sponsor =
        cfg && typeof cfg.sponsor === 'string' && cfg.sponsor.trim().length > 0 ? cfg.sponsor.trim() : null;
      const durationHintSeconds =
        cfg && typeof cfg.durationHintSeconds === 'number' && Number.isFinite(cfg.durationHintSeconds)
          ? cfg.durationHintSeconds
          : null;

      resolvedItems.push(
        Object.freeze({
          id: `arch-${fileInfo.sha256.slice(0, 12)}`,
          fileName: fileInfo.fileName,
          title,
          sponsor,
          mediaType: 'video',
          mimeType: fileInfo.mimeType,
          sizeBytes: fileInfo.sizeBytes,
          sha256: fileInfo.sha256,
          order,
          durationHintSeconds,
          url: `/api/media/arch/${encodeURIComponent(fileInfo.fileName)}`,
          activeWindow:
            activeWindow && (activeWindow.startAt || activeWindow.endAt)
              ? Object.freeze(activeWindow)
              : null,
        })
      );
    }

    resolvedItems.sort((a, b) => {
      if (a.order !== b.order) return a.order - b.order;
      return a.fileName.localeCompare(b.fileName, 'en', { sensitivity: 'base' });
    });

    return Object.freeze({
      generatedAt,
      source: 'local-arch-media',
      pollRecommendedSeconds: this.pollRecommendedSeconds,
      items: Object.freeze(resolvedItems),
    });
  }

  public async resolveMediaFile(fileName: string): Promise<ResolvedArchMediaFile | null> {
    if (!isSafeArchMediaFileName(fileName)) {
      return null;
    }

    const safeName = basename(fileName);
    const absPath = resolve(this.mediaDir, safeName);
    if (!absPath.startsWith(this.mediaDir)) {
      return null;
    }
    if (!existsSync(absPath)) {
      return null;
    }

    const st = statSync(absPath);
    if (!st.isFile()) {
      return null;
    }

    const mimeType = getAllowedArchVideoMimeType(safeName);
    if (!mimeType) {
      return null;
    }

    const sha256 = await this.computeSha256Cached(absPath, st.size, st.mtimeMs);
    return Object.freeze({
      fileName: safeName,
      absolutePath: absPath,
      mimeType,
      sizeBytes: st.size,
      sha256,
      lastModifiedMs: st.mtimeMs,
    });
  }

  private readPlaylistConfig(): RawPlaylistConfigEntry[] {
    const manifestPath = join(this.mediaDir, 'playlist.json');
    if (!existsSync(manifestPath)) {
      return [];
    }
    try {
      const raw = readFileSync(manifestPath, 'utf8');
      const parsed = JSON.parse(raw) as { items?: unknown };
      return Array.isArray(parsed?.items) ? (parsed.items as RawPlaylistConfigEntry[]) : [];
    } catch {
      return [];
    }
  }

  private async computeSha256Cached(absPath: string, sizeBytes: number, mtimeMs: number): Promise<string> {
    const cached = this.digestCache.get(absPath);
    if (cached && cached.sizeBytes === sizeBytes && cached.mtimeMs === mtimeMs) {
      return cached.sha256;
    }

    const hash = await new Promise<string>((resolveHash, rejectHash) => {
      const h = createHash('sha256');
      const stream = createReadStream(absPath);
      stream.on('data', (chunk) => h.update(chunk));
      stream.on('end', () => resolveHash(h.digest('hex')));
      stream.on('error', rejectHash);
    });

    this.digestCache.set(absPath, { sizeBytes, mtimeMs, sha256: hash });
    return hash;
  }
}

function stripExtension(name: string): string {
  const idx = name.lastIndexOf('.');
  return idx > 0 ? name.slice(0, idx) : name;
}
