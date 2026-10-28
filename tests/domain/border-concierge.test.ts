import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  BORDER_CONCIERGE_PROFILE,
  buildConciergePrompt,
  classifyConciergeIntent,
  formatDeterministicConciergeResponse,
} from '../../src/domain/border-concierge.js';
import {
  BorderCrossingComparison,
  BorderCrossingObservation,
  OFFICIAL_CAMERA_METADATA,
  STANDARD_PEDESTRIAN_LANE,
} from '../../src/domain/border-crossing.js';

function createMockObservation(overrides: Partial<BorderCrossingObservation> = {}): BorderCrossingObservation {
  return {
    crossingId: 'paso-del-norte',
    portNumber: '240202',
    canonicalName: 'Paso del Norte (Santa Fe)',
    direction: 'juarez-to-el-paso',
    mode: 'pedestrian',
    laneCategory: 'standard',
    laneCategoryName: STANDARD_PEDESTRIAN_LANE.name,
    delayMinutes: 25,
    unit: 'minutes',
    operationalStatus: 'Delay 25 min',
    portStatus: 'Open',
    lanesOpen: 4,
    maximumLanes: 8,
    sourceObservationTime: '10/27/2026 21:45:00',
    sourceObservationTimestamp: '2026-10-27T21:45:00.000Z',
    fetchedAt: '2026-10-27T22:00:00.000Z',
    sourceUrl: 'https://bwt.cbp.gov/details/06240202/PED',
    provider: 'U.S. Customs and Border Protection (CBP)',
    freshness: 'fresh',
    ...overrides,
  };
}

function createMockComparison(
  pdnOverrides: Partial<BorderCrossingObservation> = {},
  botaOverrides: Partial<BorderCrossingObservation> = {}
): BorderCrossingComparison {
  const pdn = createMockObservation({
    crossingId: 'paso-del-norte',
    canonicalName: 'Paso del Norte (Santa Fe)',
    portNumber: '240202',
    delayMinutes: 20,
    sourceObservationTime: '10/27/2026 21:45:00',
    ...pdnOverrides,
  });

  const bota = createMockObservation({
    crossingId: 'bridge-of-the-americas',
    canonicalName: 'Bridge of the Americas (BOTA / Puente Libre)',
    portNumber: '240201',
    delayMinutes: 45,
    sourceObservationTime: '10/27/2026 21:50:00',
    sourceUrl: 'https://bwt.cbp.gov/details/06240201/PED',
    ...botaOverrides,
  });

  const canCompare =
    pdn.portStatus === 'Open' &&
    bota.portStatus === 'Open' &&
    pdn.freshness === 'fresh' &&
    bota.freshness === 'fresh' &&
    typeof pdn.delayMinutes === 'number' &&
    typeof bota.delayMinutes === 'number';

  return {
    direction: 'juarez-to-el-paso',
    mode: 'pedestrian',
    laneCategory: 'standard',
    laneCategoryDisplay: STANDARD_PEDESTRIAN_LANE.name,
    observations: {
      pasoDelNorte: pdn,
      bridgeOfTheAmericas: bota,
    },
    comparison: canCompare
      ? {
          canCompare: true,
          differenceMinutes: Math.abs((pdn.delayMinutes ?? 0) - (bota.delayMinutes ?? 0)),
          lowerWaitCrossingId: (pdn.delayMinutes ?? 0) < (bota.delayMinutes ?? 0) ? 'paso-del-norte' : 'bridge-of-the-americas',
          statement: {
            en: `Paso del Norte reported wait (${pdn.delayMinutes} min) for the Standard / General pedestrian lane is 25 minutes lower than Bridge of the Americas (${bota.delayMinutes} min).`,
            es: `El tiempo reportado en Paso del Norte (${pdn.delayMinutes} min) para el carril peatonal Estándar / General es 25 minutos menor que en Puente de las Américas (${bota.delayMinutes} min).`,
          },
        }
      : {
          canCompare: false,
          differenceMinutes: null,
          lowerWaitCrossingId: null,
          statement: {
            en: 'A side-by-side wait difference cannot be computed because one or both crossings lack fresh numeric wait data.',
            es: 'No es posible calcular la diferencia de tiempo porque uno o ambos cruces carecen de datos numéricos actualizados.',
          },
          reasonUnavailable: {
            en: 'One or both crossings are closed or stale.',
            es: 'Uno o ambos cruces están cerrados o desactualizados.',
          },
        },
    cameras: OFFICIAL_CAMERA_METADATA,
    fetchedAt: '2026-10-27T22:00:00.000Z',
  };
}

describe('Border Concierge Domain Models & Logic (Increment 3)', () => {
  describe('1. Profile Invariants & Identity', () => {
    it('defines Elena Ramos with explicit role and grounded CBP guidance badge', () => {
      assert.strictEqual(BORDER_CONCIERGE_PROFILE.id, 'elena-border-concierge');
      assert.strictEqual(BORDER_CONCIERGE_PROFILE.name, 'Elena Ramos');
      assert.strictEqual(BORDER_CONCIERGE_PROFILE.avatar, '🛂');
      assert.strictEqual(BORDER_CONCIERGE_PROFILE.role.en, 'Border Pedestrian Concierge');
      assert.strictEqual(BORDER_CONCIERGE_PROFILE.role.es, 'Concierge de Orientación Peatonal');
      assert.ok(BORDER_CONCIERGE_PROFILE.badge.en.includes('Grounded Assistant'));
      assert.ok(BORDER_CONCIERGE_PROFILE.badge.es.includes('Asistente Grounded'));
      assert.ok(BORDER_CONCIERGE_PROFILE.description.en.includes('Paso del Norte'));
      assert.ok(Array.isArray(BORDER_CONCIERGE_PROFILE.suggestedPrompts));
      assert.strictEqual(BORDER_CONCIERGE_PROFILE.suggestedPrompts.length, 6);
    });
  });

  describe('2. Intent Classification', () => {
    it('classifies Paso del Norte wait inquiries', () => {
      assert.strictEqual(classifyConciergeIntent('What is the wait time at Paso del Norte?'), 'paso-del-norte-wait');
      assert.strictEqual(classifyConciergeIntent('How long is the Santa Fe bridge pedestrian delay?'), 'paso-del-norte-wait');
      assert.strictEqual(classifyConciergeIntent('¿Cuánto tiempo se hace en el puente Santa Fe?'), 'paso-del-norte-wait');
    });

    it('classifies BOTA wait inquiries', () => {
      assert.strictEqual(classifyConciergeIntent('What is the wait time at BOTA?'), 'bota-wait');
      assert.strictEqual(classifyConciergeIntent('How long is Bridge of the Americas?'), 'bota-wait');
      assert.strictEqual(classifyConciergeIntent('¿Cuánto tardan en Puente Libre?'), 'bota-wait');
      assert.strictEqual(classifyConciergeIntent('¿Hay mucha fila en Córdova de las Américas?'), 'bota-wait');
    });

    it('classifies bridge comparison inquiries', () => {
      assert.strictEqual(classifyConciergeIntent('Which crossing has the lower reported wait?'), 'compare-crossings');
      assert.strictEqual(classifyConciergeIntent('Compare Paso del Norte and BOTA'), 'compare-crossings');
      assert.strictEqual(classifyConciergeIntent('Which bridge is faster right now?'), 'compare-crossings');
      assert.strictEqual(classifyConciergeIntent('¿Cuál puente tiene menor espera ahorita?'), 'compare-crossings');
      assert.strictEqual(classifyConciergeIntent('¿Cuál de los dos puentes conviene más?'), 'compare-crossings');
    });

    it('classifies freshness and update timestamp inquiries', () => {
      assert.strictEqual(classifyConciergeIntent('When was this information last updated by CBP?'), 'freshness-and-updates');
      assert.strictEqual(classifyConciergeIntent('Are these observations fresh or stale?'), 'freshness-and-updates');
      assert.strictEqual(classifyConciergeIntent('¿Cuándo se actualizó el reporte de CBP?'), 'freshness-and-updates');
      assert.strictEqual(classifyConciergeIntent('¿Están frescos los datos?'), 'freshness-and-updates');
    });

    it('classifies live camera inquiries', () => {
      assert.strictEqual(classifyConciergeIntent('Where can I see the live camera views?'), 'camera-reference');
      assert.strictEqual(classifyConciergeIntent('Show me the bridge video cameras'), 'camera-reference');
      assert.strictEqual(classifyConciergeIntent('¿Dónde puedo ver las cámaras en vivo del puente?'), 'camera-reference');
    });

    it('classifies traveler guidance inquiries', () => {
      assert.strictEqual(classifyConciergeIntent('What should I consider when comparing wait times?'), 'traveler-guidance');
      assert.strictEqual(classifyConciergeIntent('Any advice or tips for pedestrian commuters?'), 'traveler-guidance');
      assert.strictEqual(classifyConciergeIntent('¿Qué factores debo tomar en cuenta al cruzar?'), 'traveler-guidance');
    });

    it('classifies general inquiries into orientation', () => {
      assert.strictEqual(classifyConciergeIntent('Hello, who are you?'), 'general-orientation');
      assert.strictEqual(classifyConciergeIntent('Hola, ¿qué servicios ofreces?'), 'general-orientation');
    });
  });

  describe('3. Deterministic Responses & Truth Guardrails', () => {
    it('reports Paso del Norte wait with CBP source timestamp, fetch time, and official URL', () => {
      const comp = createMockComparison({ delayMinutes: 30, sourceObservationTime: '10/27/2026 21:30:00' });
      const respEn = formatDeterministicConciergeResponse('paso-del-norte-wait', comp, 'en');

      assert.ok(respEn.includes('30 minutes reported for the Standard / General pedestrian lane'));
      assert.ok(respEn.includes('10/27/2026 21:30:00'));
      assert.ok(respEn.includes('Fetched by the application'));
      assert.ok(respEn.includes('local time (Ciudad Juárez)'));
      assert.ok(respEn.includes('https://bwt.cbp.gov/details/06240202/PED'));

      // Source observation timestamp must NOT equal the fetch time ISO string
      assert.notStrictEqual('10/27/2026 21:30:00', comp.observations.pasoDelNorte.fetchedAt);
    });

    it('reports BOTA wait with CBP source timestamp and official URL', () => {
      const comp = createMockComparison({}, { delayMinutes: 15, sourceObservationTime: '10/27/2026 21:40:00' });
      const respEn = formatDeterministicConciergeResponse('bota-wait', comp, 'en');

      assert.ok(respEn.includes('15 minutes reported for the Standard / General pedestrian lane'));
      assert.ok(respEn.includes('10/27/2026 21:40:00'));
      assert.ok(respEn.includes('local time (Ciudad Juárez)'));
      assert.ok(respEn.includes('https://bwt.cbp.gov/details/06240201/PED'));
    });

    it('NEVER reports a closed crossing as having 0 minutes of wait', () => {
      const comp = createMockComparison({ portStatus: 'Closed', delayMinutes: null });
      const respEn = formatDeterministicConciergeResponse('paso-del-norte-wait', comp, 'en');
      const respEs = formatDeterministicConciergeResponse('paso-del-norte-wait', comp, 'es');

      assert.ok(respEn.includes('reported as CLOSED by CBP'));
      assert.ok(respEn.includes('not assigned a zero-minute wait time'));
      assert.ok(!respEn.includes('0 minutes reported'));

      assert.ok(respEs.includes('CERRADO por CBP'));
      assert.ok(respEs.includes('No se le asigna tiempo de espera de cero minutos'));
      assert.ok(!respEs.includes('0 minutos'));
    });

    it('handles pending and unavailable data plainly without inventing a wait time', () => {
      const comp = createMockComparison({ freshness: 'pending', delayMinutes: null });
      const respEn = formatDeterministicConciergeResponse('paso-del-norte-wait', comp, 'en');
      const respEs = formatDeterministicConciergeResponse('paso-del-norte-wait', comp, 'es');

      assert.ok(respEn.includes('pending update in CBP records'));
      assert.ok(!respEn.includes('minutes reported'));

      assert.ok(respEs.includes('pendiente de actualización'));
      assert.ok(!respEs.includes('minutos reportados'));
    });

    it('formats valid comparison with source timestamps and snapshot disclaimer', () => {
      const comp = createMockComparison(
        { delayMinutes: 15, sourceObservationTime: '10/27/2026 21:40:00' },
        { delayMinutes: 40, sourceObservationTime: '10/27/2026 21:45:00' }
      );
      const respEn = formatDeterministicConciergeResponse('compare-crossings', comp, 'en');
      const respEs = formatDeterministicConciergeResponse('compare-crossings', comp, 'es');

      assert.ok(respEn.includes('Paso del Norte reported wait'));
      assert.ok(respEn.includes('25 minutes lower'));
      assert.ok(respEn.includes('CBP observation timestamps: Paso del Norte at 10/27/2026 21:40:00'));
      assert.ok(respEn.includes('Standard / General pedestrian lane'));
      assert.ok(respEn.includes('point-in-time snapshots and do not guarantee a faster end-to-end journey'));

      assert.ok(respEs.includes('menor que en Puente de las Américas'));
      assert.ok(respEs.includes('instantánea histórica y no garantizan un viaje más rápido'));
    });

    it('refuses comparison when one crossing is stale and explains the reason', () => {
      const comp = createMockComparison(
        { delayMinutes: 15, freshness: 'stale' },
        { delayMinutes: 20, freshness: 'fresh' }
      );
      const respEn = formatDeterministicConciergeResponse('compare-crossings', comp, 'en');

      assert.ok(respEn.includes('cannot be computed at this time'));
      assert.ok(respEn.includes('closed or stale'));
    });

    it('refuses comparison when one crossing is closed and explains the reason', () => {
      const comp = createMockComparison(
        { portStatus: 'Closed', delayMinutes: null },
        { portStatus: 'Open', delayMinutes: 20 }
      );
      const respEn = formatDeterministicConciergeResponse('compare-crossings', comp, 'en');

      assert.ok(respEn.includes('cannot be computed at this time'));
    });

    it('refuses comparison when one crossing is pending or unavailable', () => {
      const pendingComp = createMockComparison(
        { freshness: 'pending', delayMinutes: null },
        { freshness: 'fresh', delayMinutes: 20 }
      );
      const pendingResp = formatDeterministicConciergeResponse('compare-crossings', pendingComp, 'en');
      assert.ok(pendingResp.includes('cannot be computed at this time'));

      const unavailComp = createMockComparison(
        { freshness: 'unavailable', delayMinutes: null },
        { freshness: 'fresh', delayMinutes: 20 }
      );
      const unavailResp = formatDeterministicConciergeResponse('compare-crossings', unavailComp, 'en');
      assert.ok(unavailResp.includes('cannot be computed at this time'));
    });

    it('reports observation freshness with CBP timestamps, application fetch time, and 60-minute threshold', () => {
      const comp = createMockComparison(
        { sourceObservationTime: '10/27/2026 21:30:00', freshness: 'fresh' },
        { sourceObservationTime: '10/27/2026 21:35:00', freshness: 'stale' }
      );
      const respEn = formatDeterministicConciergeResponse('freshness-and-updates', comp, 'en');
      const respEs = formatDeterministicConciergeResponse('freshness-and-updates', comp, 'es');

      assert.ok(respEn.includes('Paso del Norte: Last updated by CBP at 10/27/2026 21:30:00 | Freshness: fresh'));
      assert.ok(respEn.includes('Bridge of the Americas: Last updated by CBP at 10/27/2026 21:35:00 | Freshness: stale'));
      assert.ok(respEn.includes('Both observations were fetched by the application at'));
      assert.ok(respEn.includes('local time (Ciudad Juárez)'));
      assert.ok(respEn.includes('Observations are classified as fresh if under 60 minutes old.'));

      assert.ok(respEs.includes('Paso del Norte: Última actualización de CBP: 10/27/2026 21:30:00 | Frescura: fresh'));
      assert.ok(respEs.includes('Puente de las Américas: Última actualización de CBP: 10/27/2026 21:35:00 | Frescura: stale'));
      assert.ok(respEs.includes('hora local de Ciudad Juárez'));
      assert.ok(respEs.includes('Los datos se clasifican como frescos si tienen menos de 60 minutos de antigüedad.'));
    });

    it('provides official camera links and explicitly disclaims video inspection or queue counting', () => {
      const comp = createMockComparison();
      const respEn = formatDeterministicConciergeResponse('camera-reference', comp, 'en');
      const respEs = formatDeterministicConciergeResponse('camera-reference', comp, 'es');

      assert.ok(respEn.includes('https://www.puentesfronterizos.gob.mx/camaras-en-vivo.php'));
      assert.ok(respEn.includes('I do not perform automated video analysis, inspect camera feeds, or calculate queue density'));

      assert.ok(respEs.includes('https://www.puentesfronterizos.gob.mx/camaras-en-vivo.php'));
      assert.ok(respEs.includes('no realizo inspección visual automatizada de video ni calculo longitud de filas'));
    });

    it('provides traveler guidance explaining destination differences and snapshot nature', () => {
      const comp = createMockComparison();
      const respEn = formatDeterministicConciergeResponse('traveler-guidance', comp, 'en');
      const respEs = formatDeterministicConciergeResponse('traveler-guidance', comp, 'es');

      assert.ok(respEn.includes('Downtown El Paso'));
      assert.ok(respEn.includes('Chamizal/I-110'));
      assert.ok(respEn.includes('Standard / General pedestrian lane'));

      assert.ok(respEs.includes('centro de El Paso'));
      assert.ok(respEs.includes('Chamizal/I-110'));
      assert.ok(respEs.includes('carril peatonal Estándar/General'));
    });
  });

  describe('4. Prompt Builder Grounding & Boundaries', () => {
    it('builds system prompt and grounded user content in English without authority to alter values', () => {
      const comp = createMockComparison({ delayMinutes: 20 }, { delayMinutes: 35 });
      const { systemPrompt, userContent } = buildConciergePrompt(
        'Which bridge has lower wait right now?',
        'compare-crossings',
        comp,
        'en'
      );

      assert.ok(systemPrompt.includes('Elena Ramos'));
      assert.ok(systemPrompt.includes('NEVER alter, invent, or substitute numeric measurements'));
      assert.ok(systemPrompt.includes('If a crossing is closed, NEVER state that it has a 0-minute wait'));
      assert.ok(systemPrompt.includes('DO NOT claim to have inspected camera video'));
      assert.ok(userContent.includes('Paso del Norte (240202): Wait=20 min'));
      assert.ok(userContent.includes('Bridge of the Americas (240201): Wait=35 min'));
      assert.ok(userContent.includes('Comparison Eligible: YES'));
    });

    it('builds system prompt and grounded user content in Spanish', () => {
      const comp = createMockComparison();
      const { systemPrompt, userContent } = buildConciergePrompt(
        '¿Cuál es el tiempo oficial en Paso del Norte?',
        'paso-del-norte-wait',
        comp,
        'es'
      );

      assert.ok(systemPrompt.includes('Elena Ramos'));
      assert.ok(systemPrompt.includes('NUNCA alteres, inventes ni sustituyas'));
      assert.ok(systemPrompt.includes('Si un puerto está cerrado, NUNCA digas que tiene 0 minutos'));
      assert.ok(systemPrompt.includes('Responde enteramente en español'));
      assert.ok(userContent.includes('TRAVELER QUESTION: "¿Cuál es el tiempo oficial en Paso del Norte?"'));
    });
  });
});
