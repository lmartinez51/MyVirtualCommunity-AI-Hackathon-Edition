/**
 * Server Restart Recovery Verification
 * Verifies that citizen report state persists across server restarts
 * and preserves report IDs, lifecycle state, and location metadata.
 * Uses an isolated unique OS temporary directory with guaranteed cleanup in finally.
 */

import { existsSync, mkdtempSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { createAssistantServer } from '../src/server/index.js';
import { InMemoryWorldCoreAdapter } from '../src/adapters/in-memory-world-core-adapter.js';

export interface PersistenceRecoveryOptions {
  throwOnInvariantFailure?: boolean;
  simulateCleanupFailure?: boolean;
}

export async function runPersistenceRecoveryVerification(options?: PersistenceRecoveryOptions) {
  console.log('=== Server Restart Recovery Verification ===\n');

  const tempDirectory = mkdtempSync(join(tmpdir(), 'mvc-restart-recovery-'));
  const persistencePath = join(tempDirectory, 'world-state.json');

  console.log(`[Step 1] Initialized isolated temp directory: ${tempDirectory}`);
  console.log(`         Persistence path: ${persistencePath}`);

  let app1: any = null;
  let app2: any = null;

  try {
    // Phase 1: Launch Server Instance 1
    const worldCore1 = new InMemoryWorldCoreAdapter({ persistenceFilePath: persistencePath });
    app1 = createAssistantServer({ worldCore: worldCore1 });

    await new Promise<void>((res) => app1.server.listen(0, '127.0.0.1', () => res()));
    const port1 = (app1.server.address() as any).port;
    console.log(`[Step 2] Server Instance 1 listening at http://127.0.0.1:${port1}`);

    // Draft citizen report
    const draftPayload = {
      citizenDescription: 'Bache severo en el asfalto frente al cruce peatonal.',
      locationDescription: 'Avenida Benito Juárez',
      category: 'road-surface-damage',
      externalAiProcessingConsent: false,
      language: 'es',
    };

    const draftRes = await fetch(`http://127.0.0.1:${port1}/api/citizen-reports/draft`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(draftPayload),
    });
    const draftJson: any = await draftRes.json();
    console.log(`[Step 3] POST /api/citizen-reports/draft status: ${draftRes.status}`);
    console.log(`         Draft ID: ${draftJson.draft?.draftId}`);
    console.log(`         Location: ${draftJson.draft?.location?.description} (${draftJson.draft?.location?.coverageStatus})`);
    console.log(`         Coordinates: ${JSON.stringify(draftJson.draft?.location?.coordinates)}`);

    const draftId = draftJson.draft?.draftId;
    if (!draftId) throw new Error('Failed to obtain draftId from server 1');

    // Submit citizen report
    const submitPayload = {
      draftId,
      confirmedByPlayer: true,
      confirmedCategory: 'road-surface-damage',
    };

    const submitRes = await fetch(`http://127.0.0.1:${port1}/api/citizen-reports/submit`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(submitPayload),
    });
    const submitJson: any = await submitRes.json();
    console.log(`[Step 4] POST /api/citizen-reports/submit status: ${submitRes.status}`);
    const folioId = submitJson.receipt?.reportId;
    console.log(`         Captured Folio: ${folioId}`);
    console.log(`         Lifecycle State: ${submitJson.receipt?.lifecycleState}`);
    console.log(`         Simulated Destination: ${submitJson.receipt?.simulatedDestination}`);

    if (!folioId) throw new Error('Failed to obtain folioId from submission');

    // Stop Server Instance 1
    await app1.stop();
    app1 = null;
    console.log('[Step 5] Server Instance 1 stopped cleanly.');

    // Verify persistence file exists on disk
    if (!existsSync(persistencePath)) {
      throw new Error(`Persistence file was not created on disk: ${persistencePath}`);
    }
    console.log(`[Step 6] Persistence file confirmed on disk (${persistencePath}).`);

    // Phase 2: Launch Server Instance 2 pointing to the EXACT SAME persistence file
    const worldCore2 = new InMemoryWorldCoreAdapter({ persistenceFilePath: persistencePath });
    app2 = createAssistantServer({ worldCore: worldCore2 });

    await new Promise<void>((res) => app2.server.listen(0, '127.0.0.1', () => res()));
    const port2 = (app2.server.address() as any).port;
    console.log(`[Step 7] Server Instance 2 listening at http://127.0.0.1:${port2}`);

    // Fetch report by folio from Server Instance 2
    const getRes = await fetch(`http://127.0.0.1:${port2}/api/citizen-reports/${folioId}`);
    const getJson: any = await getRes.json();
    console.log(`[Step 8] GET /api/citizen-reports/${folioId} status: ${getRes.status}`);

    // Stop Server Instance 2
    await app2.stop();
    app2 = null;
    console.log('[Step 9] Server Instance 2 stopped cleanly.');

    // Invariant verification assertions
    if (getRes.status !== 200 || !getJson.success || !getJson.report) {
      throw new Error(`Failed to recover report from Server 2: ${JSON.stringify(getJson)}`);
    }

    const rec = getJson.report;
    console.log('\n--- Invariant Verification ---');
    console.log(`✓ Folio Match: ${rec.reportId === folioId} (${rec.reportId})`);
    console.log(`✓ Description Preserved: ${rec.citizenDescription === draftPayload.citizenDescription}`);
    console.log(`✓ Category Match: ${rec.confirmedCategory === 'road-surface-damage'}`);
    console.log(`✓ Location Coverage Status: ${rec.location?.coverageStatus === 'IN_DEMO_CORRIDOR'}`);
    console.log(`✓ Coordinates Null (only street named, Case 2): ${rec.location?.coordinates === null}`);
    console.log(`✓ Representative Preserved: ${rec.reportingRepresentative?.name === 'Lic. Patricia Morales'}`);
    console.log(`✓ Lifecycle State Strictly RECEIVED: ${rec.lifecycleState === 'RECEIVED'}`);
    console.log(`✓ Simulated Notice Present: ${typeof rec.simulationNotice === 'string' && rec.simulationNotice.length > 0}`);

    const allPassed =
      rec.reportId === folioId &&
      rec.citizenDescription === draftPayload.citizenDescription &&
      rec.confirmedCategory === 'road-surface-damage' &&
      rec.location?.coverageStatus === 'IN_DEMO_CORRIDOR' &&
      rec.location?.coordinates === null &&
      rec.reportingRepresentative?.name === 'Lic. Patricia Morales' &&
      rec.lifecycleState === 'RECEIVED';

    if (!allPassed) {
      throw new Error('One or more invariants failed during recovery verification!');
    }

    console.log('\n✅ ALL SERVER RESTART RECOVERY INVARIANTS CONFIRMED PERFECTLY.');
    return { success: true, tempDirectory, persistencePath, folioId };
  } finally {
    if (app1) {
      try { await app1.stop(); } catch {}
    }
    if (app2) {
      try { await app2.stop(); } catch {}
    }
    let cleanupError: Error | null = null;
    if (tempDirectory && existsSync(tempDirectory)) {
      try {
        if (options?.simulateCleanupFailure) {
          throw new Error('Simulated filesystem EPERM during directory cleanup');
        }
        rmSync(tempDirectory, { recursive: true, force: true });
        console.log(`[Step 10] Isolated temporary directory cleanly removed (${tempDirectory}).`);
      } catch (e) {
        cleanupError = e instanceof Error ? e : new Error(String(e));
        console.error(`[Step 10 ERROR] Failed to cleanly remove temporary directory (${tempDirectory}):`, cleanupError);
      }
    }
    if (cleanupError) {
      if (!options?.simulateCleanupFailure) {
        // Best-effort cleanup attempt for real unexpected errors to prevent disk leak
        try { rmSync(tempDirectory, { recursive: true, force: true }); } catch {}
      }
      const err: any = new Error(`Persistence verification cleanup failed for directory "${tempDirectory}": ${cleanupError.message}`);
      err.tempDirectory = tempDirectory;
      throw err;
    }
  }
}

// Auto-run when executed directly
if (process.argv[1]?.includes('verify-persistence-recovery')) {
  runPersistenceRecoveryVerification().catch((err) => {
    console.error('FAILED:', err);
    process.exit(1);
  });
}
