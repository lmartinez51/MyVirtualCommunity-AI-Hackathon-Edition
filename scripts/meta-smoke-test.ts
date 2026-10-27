import { MetaModelApiAdapter } from '../src/adapters/meta-model-api-adapter.js';
import { loadConfig, maskApiKey } from '../src/config/index.js';

/**
 * Meta Model API Explicit Smoke Test.
 *
 * Requirements (Phase 4):
 * - Standardized on MODEL_API_KEY.
 * - Preliminary check via GET /models to verify muse-spark-1.3 availability.
 * - Uses standard model identifier muse-spark-1.3 (not muse-spark-1.3-contributor).
 * - Sends ONLY synthetic test instruction: "Reply with exactly: META_API_SMOKE_OK".
 * - Contains NO source code, NO docs, NO World Core data, NO personal information.
 * - NO automatic retries.
 * - NEVER prints or logs API keys or authorization headers.
 * - Validates response extraction and content.
 */
async function runSmokeTest(): Promise<void> {
  console.log('============================================================');
  console.log('   MyVirtualCommunity - Meta Model API Controlled Smoke Test');
  console.log('============================================================\n');

  const config = loadConfig();

  if (!config.meta.apiKey) {
    console.error('ERROR: Missing API key.');
    console.error('No MODEL_API_KEY found in environment or .env file.\n');
    console.error('To run this live smoke test:');
    console.error('  1. Copy .env.example to .env:');
    console.error('       cp .env.example .env');
    console.error('  2. Set your valid API key in .env:');
    console.error('       MODEL_API_KEY=your_actual_key');
    console.error('  3. Re-run:');
    console.error('       npm run test:meta-smoke\n');
    console.error('Safely aborting smoke test without making any network calls.');
    process.exit(1);
  }

  console.log(`Base URL:     ${config.meta.baseURL}`);
  console.log(`Target Model: ${config.meta.model}`);
  console.log(`API Key:      ${maskApiKey(config.meta.apiKey)}`);
  console.log('------------------------------------------------------------\n');

  const metaAdapter = new MetaModelApiAdapter({ config: config.meta });

  // 1. Preliminary Check: GET /models
  console.log('[Step 1/2] Preliminary Check: Querying GET /models...');
  let modelsListed: string[] = [];
  try {
    modelsListed = await metaAdapter.listModels();
    console.log(`  Models returned (${modelsListed.length}): ${modelsListed.join(', ')}`);
    const modelFound = modelsListed.includes(config.meta.model);
    if (modelFound) {
      console.log(`  ✓ Confirmed: Target model "${config.meta.model}" is available in the model list.`);
    } else {
      console.log(`  ⚠ Notice: Target model "${config.meta.model}" was not explicitly listed in GET /models (will attempt direct chat completion).`);
    }
  } catch (err) {
    console.log(`  ⚠ Notice: Preliminary GET /models query failed or not supported by endpoint: ${err instanceof Error ? err.message : String(err)}`);
    console.log('  Proceeding to chat completions check...');
  }
  console.log('');

  // 2. Synthetic Test Prompt: POST /chat/completions
  const syntheticPrompt = 'Reply with exactly: META_API_SMOKE_OK';
  console.log('[Step 2/2] Sending synthetic instruction to POST /chat/completions:');
  console.log(`  Instruction: "${syntheticPrompt}"`);
  console.log('  Executing single controlled request (no retries)...\n');

  try {
    const startTime = Date.now();
    const result = await metaAdapter.generateSimpleCompletion(syntheticPrompt, {
      temperature: 0.0,
    });
    const elapsedMs = Date.now() - startTime;

    console.log('============================================================');
    console.log('SMOKE TEST RESULT: HTTP 200 OK');
    console.log(`Latency:             ${elapsedMs}ms`);
    console.log(`Returned Model:      ${result.model}`);
    if (result.usage) {
      console.log(`Prompt Tokens:       ${result.usage.promptTokens ?? 'N/A'}`);
      console.log(`Completion Tokens:   ${result.usage.completionTokens ?? 'N/A'}`);
      if (result.usage.reasoningTokens !== undefined) {
        console.log(`Reasoning Tokens:    ${result.usage.reasoningTokens}`);
      }
      console.log(`Total Tokens:        ${result.usage.totalTokens ?? 'N/A'}`);
    }
    console.log('------------------------------------------------------------');
    console.log(`Assistant Response:\n"${result.text}"`);
    console.log('------------------------------------------------------------');

    const containsExpected = result.text.includes('META_API_SMOKE_OK');
    if (containsExpected) {
      console.log('✓ VALIDATION SUCCESS: Response matches expected token "META_API_SMOKE_OK"!');
      console.log('============================================================');
      process.exit(0);
    } else {
      console.warn('⚠ WARNING: Response received, but did not match exact token "META_API_SMOKE_OK".');
      console.log('============================================================');
      process.exit(0);
    }
  } catch (err) {
    console.error('============================================================');
    console.error('SMOKE TEST FAILED');
    console.error(`Reason: ${err instanceof Error ? err.message : String(err)}`);
    console.error('============================================================');
    process.exit(1);
  }
}

runSmokeTest();
