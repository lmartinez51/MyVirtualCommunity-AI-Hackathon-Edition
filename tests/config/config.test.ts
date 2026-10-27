import { describe, it, beforeEach, afterEach } from 'node:test';
import assert from 'node:assert/strict';
import { loadConfig, maskApiKey } from '../../src/config/index.js';

describe('Configuration & Security Diagnostics (Unit Tests)', () => {
  const originalEnv = { ...process.env };

  beforeEach(() => {
    // Clean environment variables for each test
    delete process.env.MODEL_API_KEY;
    delete process.env.META_API_KEY;
    delete process.env.MODEL_API_BASE_URL;
    delete process.env.META_API_BASE_URL;
    delete process.env.MODEL_NAME;
    delete process.env.META_MODEL;
    delete process.env.WORLD_CORE_BASE_URL;
    delete process.env.PORT;
    delete process.env.HOST;
  });

  afterEach(() => {
    // Restore original environment
    process.env = { ...originalEnv };
  });

  describe('1. maskApiKey credential protection', () => {
    it('returns [NOT CONFIGURED] for null, undefined, or empty string', () => {
      assert.strictEqual(maskApiKey(null), '[NOT CONFIGURED]');
      assert.strictEqual(maskApiKey(undefined), '[NOT CONFIGURED]');
      assert.strictEqual(maskApiKey(''), '[NOT CONFIGURED]');
      assert.strictEqual(maskApiKey('   '), '[NOT CONFIGURED]');
    });

    it('returns [CONFIGURED] for any populated key without leaking prefixes or suffixes', () => {
      const secret = 'sk-live-secret-meta-key-1234567890';
      const masked = maskApiKey(secret);
      assert.strictEqual(masked, '[CONFIGURED]');
      assert.strictEqual(masked.includes('sk-live'), false);
      assert.strictEqual(masked.includes('7890'), false);
    });
  });

  describe('2. loadConfig World Core URL parsing', () => {
    it('defaults to isRemote: false and baseURL: null when WORLD_CORE_BASE_URL is not set', () => {
      const config = loadConfig({ skipEnvFile: true });
      assert.strictEqual(config.worldCore.baseURL, null);
      assert.strictEqual(config.worldCore.isRemote, false);
    });

    it('normalizes WORLD_CORE_BASE_URL by trimming and stripping trailing slashes', () => {
      process.env.WORLD_CORE_BASE_URL = 'http://127.0.0.1:3001///';
      const config = loadConfig({ skipEnvFile: true });
      assert.strictEqual(config.worldCore.baseURL, 'http://127.0.0.1:3001');
      assert.strictEqual(config.worldCore.isRemote, true);
    });

    it('treats whitespace-only WORLD_CORE_BASE_URL as unconfigured', () => {
      process.env.WORLD_CORE_BASE_URL = '   ';
      const config = loadConfig({ skipEnvFile: true });
      assert.strictEqual(config.worldCore.baseURL, null);
      assert.strictEqual(config.worldCore.isRemote, false);
    });
  });

  describe('3. loadConfig Meta Model settings', () => {
    it('detects unconfigured API key safely', () => {
      const config = loadConfig({ skipEnvFile: true });
      assert.strictEqual(config.meta.isConfigured, false);
      assert.strictEqual(config.meta.apiKey, null);
      assert.strictEqual(config.meta.baseURL, 'https://api.meta.ai/v1');
      assert.strictEqual(config.meta.model, 'muse-spark-1.3');
    });

    it('loads MODEL_API_KEY and custom baseURL cleanly', () => {
      process.env.MODEL_API_KEY = 'test-meta-key';
      process.env.MODEL_API_BASE_URL = 'https://custom.api.meta.ai/v1/';
      process.env.MODEL_NAME = 'muse-spark-custom';

      const config = loadConfig({ skipEnvFile: true });
      assert.strictEqual(config.meta.isConfigured, true);
      assert.strictEqual(config.meta.apiKey, 'test-meta-key');
      assert.strictEqual(config.meta.baseURL, 'https://custom.api.meta.ai/v1');
      assert.strictEqual(config.meta.model, 'muse-spark-custom');
    });
  });
});
