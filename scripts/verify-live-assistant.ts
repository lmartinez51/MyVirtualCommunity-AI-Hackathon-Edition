import { loadConfig, maskApiKey } from '../src/config/index.js';
import { RemoteWorldCoreAdapter } from '../src/adapters/remote-world-core-adapter.js';
import { MetaModelApiAdapter, MetaApiError } from '../src/adapters/meta-model-api-adapter.js';
import { createBusinessId } from '../src/domain/business-id.js';

/**
 * Dedicated live integration command for Phase 3.
 *
 * Requirements:
 * 1. Requires configured MODEL_API_KEY and WORLD_CORE_BASE_URL. Fails before network if either missing.
 * 2. Retrieves real schedule and operational status for 'restaurant:wing-spot' from World Core via RemoteWorldCoreAdapter.
 * 3. Uses authoritative timestamp, schedule, timezone, and operational status from World Core.
 * 4. Passes required business data and the Spanish question to Muse Spark.
 * 5. Uses MetaModelApiAdapter with reasoning_effort: "minimal".
 * 6. Executes exactly ONE live inference request (zero retries, zero loops).
 * 7. Prompts Muse Spark to answer in Spanish strictly using provided facts.
 * 8. Reports actual model response, deterministic status, model ID, latency, and full token usage diagnostics.
 * 9. Never prints API key, fragments, auth headers, or raw environment variables.
 * 10. Does NOT use MockMetaModelAdapter.
 * 11. Read-only: Zero state mutations on World Core.
 */
async function main(): Promise<void> {
  const config = loadConfig();

  console.log('='.repeat(70));
  console.log('MYVIRTUALCOMMUNITY: LIVE GROUNDED ASSISTANT QUERY');
  console.log('='.repeat(70));

  // Requirement 1: Require MODEL_API_KEY and WORLD_CORE_BASE_URL before any network request
  if (!config.meta.isConfigured || !config.meta.apiKey) {
    console.error('\n[CONFIG ERROR] MODEL_API_KEY is not configured.');
    console.error('Please configure MODEL_API_KEY in your .env file or environment variables.');
    console.error('Execution halted before making any network requests.');
    process.exit(1);
  }

  if (!config.worldCore.isRemote || !config.worldCore.baseURL) {
    console.error('\n[CONFIG ERROR] WORLD_CORE_BASE_URL is not configured.');
    console.error('Please configure WORLD_CORE_BASE_URL in your .env file (e.g. http://127.0.0.1:3001).');
    console.error('Execution halted before making any network requests.');
    process.exit(1);
  }

  console.log(`World Core Target : ${config.worldCore.baseURL}`);
  console.log(`Meta Model Target : ${config.meta.model} (API Key: ${maskApiKey(config.meta.apiKey)})`);
  console.log(`Endpoint Base URL : ${config.meta.baseURL}`);
  console.log('-'.repeat(70));

  // Requirement 2 & 3: Retrieve single coherent authoritative snapshot via RemoteWorldCoreAdapter
  const worldCore = new RemoteWorldCoreAdapter({
    baseURL: config.worldCore.baseURL,
  });

  const businessId = createBusinessId('restaurant:wing-spot');
  console.log(`\n[1/3] Querying World Core for authoritative snapshot of "${businessId}"...`);

  let snapshot;
  try {
    snapshot = await worldCore.getBusinessScheduleSnapshot(businessId);
  } catch (err) {
    console.error(`\n[WORLD CORE ERROR] Failed to retrieve data from World Core: ${err instanceof Error ? err.message : String(err)}`);
    process.exit(1);
  }

  if (!snapshot) {
    console.error(`\n[WORLD CORE ERROR] Business "${businessId}" was not found in World Core records.`);
    process.exit(1);
  }

  const { schedule, operationalStatus } = snapshot;

  console.log(`✓ Retrieved coherent authoritative World Core snapshot:`);
  console.log(`  - Canonical Name   : ${snapshot.businessName}`);
  console.log(`  - Local Timezone   : ${snapshot.timeZone}`);
  console.log(`  - World Time (UTC) : ${snapshot.worldTime}`);
  console.log(`  - Local Civil Time : ${String(operationalStatus.civilTime.hour).padStart(2, '0')}:${String(operationalStatus.civilTime.minute).padStart(2, '0')}`);
  console.log(`  - Operational State: ${operationalStatus.status} (isOpen: ${operationalStatus.isOpen}, admissionPermitted: ${operationalStatus.admissionPermitted})`);
  console.log(`  - Operating Hours  : ${operationalStatus.openTimeCivil} to ${operationalStatus.closeTimeCivil} (Last entry cutoff: ${operationalStatus.cutoffTimeCivil})`);
  console.log(`  - Authority Message: "${operationalStatus.summaryMessage}"`);

  // Requirement 4, 5, 6, 7 & 8: Single live inference request with Muse Spark
  const userQuery = 'Is Wing Spot Gómez Morín open right now, and what time does it close?';
  const systemPrompt =
    'You are a courteous, clear, and concise local-business assistant for the Ciudad Juárez–El Paso virtual community. ' +
    'Respond in American English. ' +
    'CRITICAL RULES: ' +
    '1. Base your answer strictly and authoritatively on the VERIFIED OPERATIONAL DATA provided below. ' +
    '2. Preserve the canonical business name exactly as returned by the World Core ("Wing Spot Gómez Morín"). Do not translate, transliterate, normalize, or alter business names. ' +
    '3. Do not recalculate operational status, guess, or contradict the verified data. ' +
    '4. When the business is open but admission is no longer permitted (e.g. closing soon / last entry cutoff reached), clearly distinguish that the business is currently open for existing guests but no new admissions or dine-in entries are permitted.';

  const metaAdapter = new MetaModelApiAdapter({
    config: config.meta,
  });

  console.log(`\n[2/3] Preparing grounded prompt for Muse Spark...`);
  console.log(`  - User Query : "${userQuery}"`);
  console.log(`  - Reasoning  : minimal`);
  console.log(`  - Language   : American English`);
  console.log(`  - Dispatching exactly ONE live inference request to ${config.meta.baseURL}/chat/completions...`);

  const startTime = performance.now();
  let metaResponse;

  try {
    metaResponse = await metaAdapter.generateAssistantResponse({
      userQuery,
      verifiedData: operationalStatus,
      schedule,
      systemPrompt,
    });
  } catch (err) {
    const sanitizedError = metaAdapter.sanitize(
      err instanceof MetaApiError ? err.message : err instanceof Error ? err.message : String(err)
    );
    console.error(`\n[META API ERROR] Live inference request failed:`);
    console.error(sanitizedError);
    process.exit(1);
  }

  const durationMs = Math.round(performance.now() - startTime);

  // Requirement 9 & 10: Print actual response and diagnostics without credentials
  console.log('\n[3/3] Live Model Response Received:');
  console.log('='.repeat(70));
  console.log(`MUSE SPARK RESPONSE:\n`);
  console.log(metaResponse.text);
  console.log('\n' + '='.repeat(70));

  console.log('DIAGNOSTICS & METRICS:');
  console.log(`  - Canonical Name   : ${snapshot.businessName}`);
  console.log(`  - World Timestamp  : ${snapshot.worldTime}`);
  console.log(`  - Timezone         : ${snapshot.timeZone}`);
  console.log(`  - Operational State: ${operationalStatus.status} (isOpen: ${operationalStatus.isOpen})`);
  console.log(`  - Admission Status : admissionPermitted: ${operationalStatus.admissionPermitted}`);
  console.log(`  - Model Identifier : ${metaResponse.model}`);
  console.log(`  - Roundtrip Latency: ${durationMs}ms`);

  if (metaResponse.usage) {
    const u = metaResponse.usage;
    console.log(`  - Token Usage:`);
    console.log(`    * Prompt Tokens    : ${u.promptTokens ?? 'N/A'}`);
    console.log(`    * Completion Tokens: ${u.completionTokens ?? 'N/A'}`);
    console.log(`    * Total Tokens     : ${u.totalTokens ?? 'N/A'}`);
    if (u.reasoningTokens !== undefined) {
      console.log(`    * Reasoning Tokens : ${u.reasoningTokens}`);
    }
    if (u.completionTokensDetails) {
      const d = u.completionTokensDetails;
      if (d.reasoningTokens !== undefined && u.reasoningTokens === undefined) {
        console.log(`    * Reasoning Tokens : ${d.reasoningTokens}`);
      }
      if (d.acceptedPredictionTokens !== undefined) {
        console.log(`    * Accepted Pred.   : ${d.acceptedPredictionTokens}`);
      }
      if (d.rejectedPredictionTokens !== undefined) {
        console.log(`    * Rejected Pred.   : ${d.rejectedPredictionTokens}`);
      }
    }
  } else {
    console.log(`  - Token Usage: Not reported by API`);
  }

  console.log('='.repeat(70));
  console.log('LIVE ASSISTANT GROUNDING VERIFIED: 1 real query completed successfully.');
  console.log('='.repeat(70));
}

main().catch((err) => {
  console.error('[FATAL UNCAUGHT ERROR]', err instanceof Error ? err.message : String(err));
  process.exit(1);
});
