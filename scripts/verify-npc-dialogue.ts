/**
 * NPC Live Communication Verification Suite
 * Verifies live/mock dialogue communication across all corridor NPCs:
 * - Carlos Méndez (Farmacias Benavides Attendant)
 * - Valeria Domínguez (Óptica Fashion Attendant)
 * - Mateo Navarro (UTEP Student Commuter)
 * - Sofía Alarcón (Cross-Border Retail Worker)
 * - Don Héctor Méndez (Retired Leather Artisan)
 * - Elena Ramos (Official Border Concierge)
 */

import { createAssistantServer } from '../src/server/index.js';
import { MockMetaModelAdapter } from '../src/adapters/mock-meta-model-adapter.js';

async function main() {
  console.log('--- Starting NPC Live Communication Verification Suite ---');

  const mockMeta = new MockMetaModelAdapter({ modelName: 'llama-3.3-70b-instruct-mock' });
  const app = createAssistantServer({
    metaModel: mockMeta,
    config: {
      meta: { apiKey: null, baseURL: 'http://mock', model: 'llama-3.3-70b-instruct-mock', isConfigured: false },
      worldCore: { baseURL: null, isRemote: false },
      server: { port: 3099, host: '127.0.0.1' }
    }
  });

  await app.start(3099, '127.0.0.1');
  const baseUrl = 'http://127.0.0.1:3099';

  const results: any[] = [];

  // Helper for POST
  async function postJson(endpoint: string, body: any) {
    const res = await fetch(`${baseUrl}${endpoint}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body)
    });
    const json = await res.json();
    return { status: res.status, ok: res.ok, body: json };
  }

  // 1. CARLOS MENDEZ
  console.log('\n[1/6] Testing Carlos Méndez (Farmacias Benavides Attendant)...');
  const resCarlos = await postJson('/api/business-attendants/chat', {
    attendantId: 'carlos-farmacia-attendant',
    message: 'Buenas tardes Carlos, ¿tienen analgésicos y protectores solares disponibles?',
    language: 'es'
  });
  console.log(`Status: ${resCarlos.status}`);
  console.log(`Response: ${JSON.stringify(resCarlos.body, null, 2)}`);
  results.push({ npc: 'Carlos Méndez', id: 'carlos-farmacia-attendant', ok: resCarlos.ok, status: resCarlos.status, data: resCarlos.body });

  // 2. VALERIA DOMINGUEZ
  console.log('\n[2/6] Testing Valeria Domínguez (Óptica Fashion Attendant)...');
  const resValeria = await postJson('/api/business-attendants/chat', {
    attendantId: 'valeria-optica-attendant',
    message: 'Hello Valeria! Can you tell me about prescription eyewear adjustments and sunglasses?',
    language: 'en'
  });
  console.log(`Status: ${resValeria.status}`);
  console.log(`Response: ${JSON.stringify(resValeria.body, null, 2)}`);
  results.push({ npc: 'Valeria Domínguez', id: 'valeria-optica-attendant', ok: resValeria.ok, status: resValeria.status, data: resValeria.body });

  // 3. MATEO NAVARRO
  console.log('\n[3/6] Testing Mateo Navarro (UTEP Student Commuter)...');
  const resMateo = await postJson('/api/border/witnesses/chat', {
    npcId: 'mateo-student',
    message: 'Hey Mateo, how bad is the pedestrian wait at Santa Fe bridge today?',
    language: 'en'
  });
  console.log(`Status: ${resMateo.status}`);
  console.log(`Response: ${JSON.stringify(resMateo.body, null, 2)}`);
  results.push({ npc: 'Mateo Navarro', id: 'mateo-student', ok: resMateo.ok, status: resMateo.status, data: resMateo.body });

  // 4. SOFÍA ALARCÓN
  console.log('\n[4/6] Testing Sofía Alarcón (Cross-Border Retail Worker)...');
  const resSofia = await postJson('/api/border/witnesses/chat', {
    npcId: 'sofia-cross-border-retail',
    message: 'Hola Sofía, ¿a qué hora suele avanzar más rápido la fila peatonal por las tardes?',
    language: 'es'
  });
  console.log(`Status: ${resSofia.status}`);
  console.log(`Response: ${JSON.stringify(resSofia.body, null, 2)}`);
  results.push({ npc: 'Sofía Alarcón', id: 'sofia-cross-border-retail', ok: resSofia.ok, status: resSofia.status, data: resSofia.body });

  // 5. DON HÉCTOR MÉNDEZ
  console.log('\n[5/6] Testing Don Héctor Méndez (Retired Leather Artisan)...');
  const resHector = await postJson('/api/border/witnesses/chat', {
    npcId: 'hector-artisan',
    message: 'Don Héctor, ¿cuántos años lleva cruzando por esta frontera de Ciudad Juárez a El Paso?',
    language: 'es'
  });
  console.log(`Status: ${resHector.status}`);
  console.log(`Response: ${JSON.stringify(resHector.body, null, 2)}`);
  results.push({ npc: 'Don Héctor Méndez', id: 'hector-artisan', ok: resHector.ok, status: resHector.status, data: resHector.body });

  // 6. ELENA RAMOS (BORDER CONCIERGE)
  console.log('\n[6/6] Testing Elena Ramos (Official Border Concierge)...');
  const resElena = await postJson('/api/border/concierge/chat', {
    message: 'Elena, what is the recommended crossing lane for pedestrians heading to downtown El Paso?',
    language: 'en'
  });
  console.log(`Status: ${resElena.status}`);
  console.log(`Response: ${JSON.stringify(resElena.body, null, 2)}`);
  results.push({ npc: 'Elena Ramos', id: 'elena-border-concierge', ok: resElena.ok, status: resElena.status, data: resElena.body });

  await app.stop();

  console.log('\n========================================');
  console.log('SUMMARY OF NPC COMMUNICATION AUDIT:');
  let allPass = true;
  for (const r of results) {
    const passed = r.ok && r.status === 200 && r.data && r.data.success !== false;
    if (!passed) allPass = false;
    console.log(`- ${r.npc} (${r.id}): ${passed ? 'PASS (200 OK)' : 'FAIL'}`);
  }
  console.log('========================================');

  if (!allPass) {
    console.error('Some NPC dialogue endpoints failed.');
    process.exit(1);
  } else {
    console.log('ALL 6 NPC DIALOGUE ENDPOINTS VERIFIED AND PASSING 100%!');
  }
}

main().catch((err) => {
  console.error('Fatal test error:', err);
  process.exit(1);
});

