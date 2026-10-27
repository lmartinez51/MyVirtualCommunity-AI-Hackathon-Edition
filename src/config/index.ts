import { existsSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';

export interface MetaConfig {
  readonly apiKey: string | null;
  readonly baseURL: string;
  readonly model: string;
  readonly isConfigured: boolean;
}

export interface WorldCoreConfig {
  readonly baseURL: string | null;
  readonly isRemote: boolean;
}

export interface ServerConfig {
  readonly port: number;
  readonly host: string;
}

export interface AppConfig {
  readonly meta: MetaConfig;
  readonly worldCore: WorldCoreConfig;
  readonly server: ServerConfig;
}

/**
 * Reports only whether an API key is configured.
 * In accordance with strict credential security requirements:
 * Never outputs the key, fragments, prefixes, or suffixes.
 */
export function maskApiKey(apiKey: string | null | undefined): string {
  if (!apiKey || apiKey.trim() === '') {
    return '[NOT CONFIGURED]';
  }
  return '[CONFIGURED]';
}

/**
 * Minimal zero-dependency .env file parser.
 * Populates process.env with variables if not already defined.
 */
export function loadEnvFile(envPath: string = resolve(process.cwd(), '.env')): void {
  if (!existsSync(envPath)) {
    return;
  }

  try {
    const content = readFileSync(envPath, 'utf-8');
    const lines = content.split(/\r?\n/);
    for (const rawLine of lines) {
      const line = rawLine.trim();
      if (!line || line.startsWith('#')) {
        continue;
      }
      const eqIdx = line.indexOf('=');
      if (eqIdx <= 0) {
        continue;
      }
      const key = line.slice(0, eqIdx).trim();
      let value = line.slice(eqIdx + 1).trim();

      // Strip surrounding quotes if present
      if (
        (value.startsWith('"') && value.endsWith('"')) ||
        (value.startsWith("'") && value.endsWith("'"))
      ) {
        value = value.slice(1, -1);
      }

      if (process.env[key] === undefined) {
        process.env[key] = value;
      }
    }
  } catch {
    // Gracefully ignore reading errors (e.g. permission issues)
  }
}

export interface LoadConfigOptions {
  readonly skipEnvFile?: boolean;
}

/**
 * Loads and returns the validated application configuration.
 * Standardized on MODEL_API_KEY per official Meta Model API specifications.
 */
export function loadConfig(options: LoadConfigOptions = {}): AppConfig {
  if (!options.skipEnvFile) {
    loadEnvFile();
  }

  const apiKeyRaw = process.env.MODEL_API_KEY || process.env.META_API_KEY;
  const apiKey = apiKeyRaw && apiKeyRaw.trim() !== '' ? apiKeyRaw.trim() : null;

  const baseURL =
    process.env.MODEL_API_BASE_URL ||
    process.env.META_API_BASE_URL ||
    'https://api.meta.ai/v1';

  const model = process.env.MODEL_NAME || process.env.META_MODEL || 'muse-spark-1.3';

  const port = parseInt(process.env.PORT || '3000', 10);
  const host = process.env.HOST || '127.0.0.1';

  const worldCoreBaseURLRaw = process.env.WORLD_CORE_BASE_URL;
  const worldCoreBaseURL =
    worldCoreBaseURLRaw && worldCoreBaseURLRaw.trim() !== ''
      ? worldCoreBaseURLRaw.trim().replace(/\/+$/, '')
      : null;

  return Object.freeze({
    meta: Object.freeze({
      apiKey,
      baseURL: baseURL.replace(/\/+$/, ''), // strip trailing slash
      model,
      isConfigured: apiKey !== null,
    }),
    worldCore: Object.freeze({
      baseURL: worldCoreBaseURL,
      isRemote: worldCoreBaseURL !== null,
    }),
    server: Object.freeze({
      port: Number.isFinite(port) && port > 0 ? port : 3000,
      host,
    }),
  });
}
