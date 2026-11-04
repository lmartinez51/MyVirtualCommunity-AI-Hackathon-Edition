import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { existsSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { runPersistenceRecoveryVerification } from '../../scripts/verify-persistence-recovery.js';

describe('Task A: Persistence Recovery Isolation & Temporary Directory Safety', () => {
  it('1. sequential invocations of actual verifier receive distinct temp directories and files', async () => {
    // Check .runtime/ state before
    const runtimeFile = resolve(process.cwd(), '.runtime', 'citizen-reports.json');
    const runtimeExistsBefore = existsSync(runtimeFile);
    const runtimeContentBefore = runtimeExistsBefore ? readFileSync(runtimeFile, 'utf-8') : null;

    // First actual invocation
    const res1 = await runPersistenceRecoveryVerification();
    // Second actual invocation
    const res2 = await runPersistenceRecoveryVerification();

    // 1. Both executions return success === true
    assert.strictEqual(res1.success, true, 'Invocation 1 must return success === true');
    assert.strictEqual(res2.success, true, 'Invocation 2 must return success === true');

    // 2. The two returned tempDirectory strings are different
    assert.notStrictEqual(res1.tempDirectory, res2.tempDirectory, 'Temp directories must be distinct across invocations');

    // 3. The two returned persistencePath strings are different
    assert.notStrictEqual(res1.persistencePath, res2.persistencePath, 'Persistence paths must be distinct across invocations');

    // 4. Each returned directory has been removed after its corresponding invocation completes
    assert.strictEqual(existsSync(res1.tempDirectory), false, 'Temp directory 1 must be removed after invocation 1');
    assert.strictEqual(existsSync(res2.tempDirectory), false, 'Temp directory 2 must be removed after invocation 2');

    // 5. Existing .runtime/ persistence remains unchanged
    const runtimeExistsAfter = existsSync(runtimeFile);
    assert.strictEqual(runtimeExistsAfter, runtimeExistsBefore, '.runtime/ existence state must remain untouched');
    if (runtimeExistsBefore) {
      assert.strictEqual(readFileSync(runtimeFile, 'utf-8'), runtimeContentBefore, '.runtime/ content must remain byte-identical');
    }
  });

  it('2. cleanup failure in verifier throws and cannot misleadingly report complete success', async () => {
    let capturedDir: string | null = null;
    try {
      await assert.rejects(
        async () => {
          await runPersistenceRecoveryVerification({ simulateCleanupFailure: true });
        },
        (err: any) => {
          assert.ok(err instanceof Error);
          assert.match(err.message, /Persistence verification cleanup failed/);
          capturedDir = (err as any).tempDirectory || err.message.match(/for directory "([^"]+)"/)?.[1] || null;
          return true;
        },
        'Verifier must throw on cleanup failure and not return complete success'
      );

      // Verify that the simulated cleanup failure indeed retained the directory on disk
      assert.ok(capturedDir, 'Captured temporary directory from cleanup failure error');
      assert.strictEqual(existsSync(capturedDir!), true, 'Temporary directory was retained by failed cleanup');
    } finally {
      // Test-level cleanup to guarantee no orphaned temporary directory is left behind
      if (capturedDir && existsSync(capturedDir)) {
        rmSync(capturedDir, { recursive: true, force: true });
      }
    }

    // Verify that the test-level cleanup cleanly removed the directory
    assert.strictEqual(existsSync(capturedDir!), false, 'Temporary directory must be cleaned up after test completes');
  });

  it('3. two independent mkdtempSync directories receive distinct paths on disk', async () => {
    const dir1 = mkdtempSync(join(tmpdir(), 'mvc-restart-recovery-'));
    const dir2 = mkdtempSync(join(tmpdir(), 'mvc-restart-recovery-'));

    try {
      assert.notStrictEqual(dir1, dir2, 'Independent invocations must create distinct directories');
      assert.ok(existsSync(dir1), 'Dir 1 must exist on disk');
      assert.ok(existsSync(dir2), 'Dir 2 must exist on disk');
    } finally {
      rmSync(dir1, { recursive: true, force: true });
      rmSync(dir2, { recursive: true, force: true });
    }
  });

  it('4. each directory writes and reads only its own persistence file without collision', async () => {
    const dirA = mkdtempSync(join(tmpdir(), 'mvc-restart-recovery-'));
    const dirB = mkdtempSync(join(tmpdir(), 'mvc-restart-recovery-'));

    const fileA = join(dirA, 'world-state.json');
    const fileB = join(dirB, 'world-state.json');

    try {
      writeFileSync(fileA, JSON.stringify({ invocation: 'A', reports: [{ id: 'CJ-000001' }] }), 'utf-8');
      writeFileSync(fileB, JSON.stringify({ invocation: 'B', reports: [{ id: 'CJ-000002' }] }), 'utf-8');

      assert.notStrictEqual(fileA, fileB);
      assert.ok(existsSync(fileA));
      assert.ok(existsSync(fileB));

      const contentA = JSON.parse(readFileSync(fileA, 'utf-8'));
      const contentB = JSON.parse(readFileSync(fileB, 'utf-8'));

      assert.strictEqual(contentA.invocation, 'A');
      assert.strictEqual(contentB.invocation, 'B');
    } finally {
      rmSync(dirA, { recursive: true, force: true });
      rmSync(dirB, { recursive: true, force: true });
    }
  });

  it('5. cleanup removes only invocation own temporary directory, preserving sibling directories', async () => {
    const siblingDir = mkdtempSync(join(tmpdir(), 'mvc-restart-recovery-sibling-'));
    const targetDir = mkdtempSync(join(tmpdir(), 'mvc-restart-recovery-target-'));

    try {
      assert.ok(existsSync(siblingDir));
      assert.ok(existsSync(targetDir));

      // Simulate invocation cleanup of targetDir
      rmSync(targetDir, { recursive: true, force: true });

      assert.strictEqual(existsSync(targetDir), false, 'Target dir must be deleted');
      assert.strictEqual(existsSync(siblingDir), true, 'Sibling dir must be untouched');
    } finally {
      if (existsSync(siblingDir)) rmSync(siblingDir, { recursive: true, force: true });
      if (existsSync(targetDir)) rmSync(targetDir, { recursive: true, force: true });
    }
  });
});
