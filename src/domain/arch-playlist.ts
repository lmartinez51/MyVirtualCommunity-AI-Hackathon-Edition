/**
 * Authoritative Gateway Arch Digital Billboard Playlist Domain Model.
 *
 * Architectural Invariants:
 * 1. The backend is the single source of truth for active sponsor/event videos,
 *    play order, integrity checksums, and campaign schedule windows.
 * 2. Filenames are strictly validated to prevent path traversal outside the
 *    designated arch media directory.
 */

export interface ArchMediaCampaignWindow {
  readonly startAt: string | null;
  readonly endAt: string | null;
}

export interface ArchPlaylistItem {
  readonly id: string;
  readonly fileName: string;
  readonly title: string;
  readonly sponsor: string | null;
  readonly mediaType: 'video';
  readonly mimeType: string;
  readonly sizeBytes: number;
  readonly sha256: string;
  readonly order: number;
  readonly durationHintSeconds: number | null;
  readonly url: string;
  readonly activeWindow: ArchMediaCampaignWindow | null;
}

export interface ArchPlaylistSnapshot {
  readonly generatedAt: string;
  readonly source: 'local-arch-media';
  readonly pollRecommendedSeconds: number;
  readonly items: readonly ArchPlaylistItem[];
}

export interface ResolvedArchMediaFile {
  readonly fileName: string;
  readonly absolutePath: string;
  readonly mimeType: string;
  readonly sizeBytes: number;
  readonly sha256: string;
  readonly lastModifiedMs: number;
}

const ALLOWED_VIDEO_MIME_TYPES: Readonly<Record<string, string>> = Object.freeze({
  '.mp4': 'video/mp4',
  '.mov': 'video/quicktime',
  '.webm': 'video/webm',
});

export function getAllowedArchVideoMimeType(fileName: string): string | null {
  const dotIdx = fileName.lastIndexOf('.');
  if (dotIdx <= 0) return null;
  const ext = fileName.slice(dotIdx).toLowerCase();
  return ALLOWED_VIDEO_MIME_TYPES[ext] ?? null;
}

/**
 * Validates that a candidate media filename is a flat safe filename (no slashes, "..", or control chars)
 * with an allowed video extension.
 */
export function isSafeArchMediaFileName(fileName: string): boolean {
  if (!fileName || fileName.length > 180) return false;
  if (fileName.includes('/') || fileName.includes('\\') || fileName.includes('..') || fileName.includes('\0')) {
    return false;
  }
  if (!/^[a-zA-Z0-9._-]+$/.test(fileName)) {
    return false;
  }
  return getAllowedArchVideoMimeType(fileName) !== null;
}

/**
 * Determines whether an optional campaign schedule window is active at the authoritative instant.
 */
export function isCampaignWindowActiveAt(
  window: ArchMediaCampaignWindow | null | undefined,
  evaluatedAtMs: number
): boolean {
  if (!window) return true;

  if (window.startAt) {
    const startMs = Date.parse(window.startAt);
    if (!Number.isFinite(startMs) || evaluatedAtMs < startMs) {
      return false;
    }
  }

  if (window.endAt) {
    const endMs = Date.parse(window.endAt);
    if (!Number.isFinite(endMs) || evaluatedAtMs > endMs) {
      return false;
    }
  }

  return true;
}
