import { readdirSync } from 'node:fs';
import { join } from 'node:path';
import { spawnSync } from 'node:child_process';

/**
 * Cross-platform test runner script utilizing Node.js native test runner with tsx.
 * Recursively discovers all *.test.ts files in tests/ and spawns node --import tsx --test.
 */
const testFiles = readdirSync('tests', { recursive: true })
  .filter((f) => typeof f === 'string' && f.endsWith('.test.ts'))
  .map((f) => join('tests', f));

if (testFiles.length === 0) {
  console.log('No test files found in tests/');
  process.exit(0);
}

const result = spawnSync(process.execPath, ['--import', 'tsx', '--test', ...testFiles], {
  stdio: 'inherit',
});

process.exit(result.status ?? 1);
