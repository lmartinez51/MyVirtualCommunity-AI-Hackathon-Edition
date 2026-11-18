import { ArchPlaylistSnapshot, ResolvedArchMediaFile } from '../domain/arch-playlist.js';

export interface ArchMediaPort {
  /**
   * Returns the authoritative active billboard video playlist evaluated at `evaluatedAtIso`
   * (or current server time if omitted).
   */
  getActivePlaylist(evaluatedAtIso?: string): Promise<ArchPlaylistSnapshot>;

  /**
   * Resolves a validated, safe video asset on disk for streaming/download.
   * Returns null if missing, unsafe, or non-video.
   */
  resolveMediaFile(fileName: string): Promise<ResolvedArchMediaFile | null>;
}
