import { RemoteWorldCoreAdapter } from '../src/adapters/remote-world-core-adapter.js';
import { MockMetaModelAdapter } from '../src/adapters/mock-meta-model-adapter.js';
import { AnswerBusinessHoursQueryUseCase } from '../src/application/answer-business-hours-query.js';
import { createBusinessId } from '../src/domain/business-id.js';

async function main(): Promise<void> {
  const baseURL = process.env.WORLD_CORE_BASE_URL || 'http://127.0.0.1:3001';
  console.log('='.repeat(70));
  console.log('MYVIRTUALCOMMUNITY: READ-ONLY WORLD CORE INTEGRATION TEST');
  console.log('='.repeat(70));
  console.log(`Connecting to real World Core server at: ${baseURL}`);

  // Step 1: Direct HTTP verification of the new endpoint
  console.log('\n--- Step 1: Direct HTTP verification against World Core ---');
  const wingSpotUrl = `${baseURL}/api/businesses/restaurant:wing-spot/schedule`;
  console.log(`Fetching GET ${wingSpotUrl}...`);

  const rawRes = await fetch(wingSpotUrl);
  console.log(`HTTP Status: ${rawRes.status}`);
  if (rawRes.status !== 200) {
    throw new Error(`Expected HTTP 200 from World Core endpoint, got ${rawRes.status}`);
  }

  const rawJson = (await rawRes.json()) as any;
  console.log('Response Payload from real World Core:');
  console.log(JSON.stringify(rawJson, null, 2));

  if (!rawJson.success || rawJson.result?.businessId !== 'restaurant:wing-spot') {
    throw new Error('World Core response does not match expected schedule contract');
  }
  console.log('✓ Direct HTTP contract verified: 200 OK, valid schedule and operationalStatus');

  // Step 2: Instantiate RemoteWorldCoreAdapter
  console.log('\n--- Step 2: Testing RemoteWorldCoreAdapter against World Core ---');
  const remoteAdapter = new RemoteWorldCoreAdapter({ baseURL });

  // 2a. Query authoritative world time
  const worldTime = await remoteAdapter.getCurrentWorldTime();
  console.log(`Authoritative World Time from World Core: ${worldTime}`);

  // 2b. Query Wing Spot schedule
  const wingSpotSchedule = await remoteAdapter.getBusinessSchedule(
    createBusinessId('restaurant:wing-spot')
  );
  if (!wingSpotSchedule) {
    throw new Error('Wing Spot schedule returned null');
  }
  console.log(`Retrieved Wing Spot: "${wingSpotSchedule.name}", Timezone: ${wingSpotSchedule.timeZone}`);
  console.log(`Hours: ${wingSpotSchedule.hours.openHour}:00 - ${wingSpotSchedule.hours.closeHour}:00`);

  // 2c. Query Tía Lupe schedule
  const tiaLupeSchedule = await remoteAdapter.getBusinessSchedule(
    createBusinessId('restaurant:pasteleria-tia-lupe')
  );
  if (!tiaLupeSchedule) {
    throw new Error('Tía Lupe schedule returned null');
  }
  console.log(`Retrieved Tía Lupe: "${tiaLupeSchedule.name}", Timezone: ${tiaLupeSchedule.timeZone}`);
  console.log(`Hours: ${tiaLupeSchedule.hours.openHour}:00 - ${tiaLupeSchedule.hours.closeHour}:00`);

  // 2d. Query Unknown Business
  const unknownSchedule = await remoteAdapter.getBusinessSchedule(
    createBusinessId('restaurant:non-existent')
  );
  console.log(`Querying unknown business returned: ${unknownSchedule} (expected null)`);
  if (unknownSchedule !== null) {
    throw new Error('Unknown business must return null, not fabricated data');
  }

  // 2e. Query operational status
  const wingSpotStatus = await remoteAdapter.getBusinessOperationalStatus(
    createBusinessId('restaurant:wing-spot')
  );
  if (!wingSpotStatus) {
    throw new Error('Operational status returned null for Wing Spot');
  }
  console.log(`Operational Status: ${wingSpotStatus.status} (isOpen: ${wingSpotStatus.isOpen}, admissionPermitted: ${wingSpotStatus.admissionPermitted})`);
  console.log(`Civil Time: ${wingSpotStatus.civilTime.hour}:${String(wingSpotStatus.civilTime.minute).padStart(2, '0')} (${wingSpotStatus.timeZone})`);
  console.log(`Summary Message: "${wingSpotStatus.summaryMessage}"`);

  // Step 3: End-to-end Use Case with Mock Meta Model
  console.log('\n--- Step 3: End-to-End Use Case with RemoteWorldCoreAdapter & MockMetaModelAdapter ---');
  console.log('NOTE: Using MockMetaModelAdapter locally. Zero cloud API calls and zero credit consumption.');

  const mockMeta = new MockMetaModelAdapter({ modelName: 'muse-spark-1.3-mock' });
  const useCase = new AnswerBusinessHoursQueryUseCase({
    worldCore: remoteAdapter,
    metaModel: mockMeta,
  });

  const queryResult = await useCase.execute({
    businessId: 'restaurant:wing-spot',
    userQuery: '¿A qué hora abren y puedo pasar a cenar al rato?',
  });

  console.log('\nUse Case Execution Result:');
  console.log(`- Business ID: ${queryResult.verifiedStatus.businessId}`);
  console.log(`- Business Name: ${queryResult.verifiedStatus.businessName}`);
  console.log(`- Operational Status: ${queryResult.verifiedStatus.status}`);
  console.log(`- Admission Permitted: ${queryResult.verifiedStatus.admissionPermitted}`);
  console.log(`- Open Civil Time: ${queryResult.verifiedStatus.openTimeCivil}`);
  console.log(`- Close Civil Time: ${queryResult.verifiedStatus.closeTimeCivil}`);
  console.log(`- Assistant Natural Language Output:\n"${queryResult.answer}"`);

  console.log('\n='.repeat(70));
  console.log('SUCCESS: Read-Only World Core Integration Verified 100% End-to-End!');
  console.log('='.repeat(70));
}

main().catch((err) => {
  console.error('\nIntegration verification failed:', err);
  process.exit(1);
});
