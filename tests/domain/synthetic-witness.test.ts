import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  buildWitnessPrompt,
  classifyWitnessSafetyIntent,
  detectLanguage,
  generateDeterministicFallbackResponse,
  getDeterministicBoundaryResponse,
  OFFICIAL_DATA_NOTICE,
  SYNTHETIC_DISCLOSURE,
  SYNTHETIC_MARKER,
  SYNTHETIC_WITNESS_ROSTER,
  SyntheticWitnessId,
  validateWitnessMessage,
} from '../../src/domain/synthetic-witness.js';

describe('Synthetic Community Witnesses Domain Models and Constraints', () => {
  describe('1. Configured Roster Invariants', () => {
    it('contains exactly the three configured stable synthetic NPCs', () => {
      const keys = Object.keys(SYNTHETIC_WITNESS_ROSTER);
      assert.strictEqual(keys.length, 3);
      assert.deepStrictEqual(keys.sort(), [
        'hector-artisan',
        'mateo-student',
        'sofia-cross-border-retail',
      ]);
    });

    it('defines complete bilingual profiles, backgrounds, and permitted knowledge for each NPC', () => {
      const ids: SyntheticWitnessId[] = [
        'mateo-student',
        'sofia-cross-border-retail',
        'hector-artisan',
      ];

      for (const id of ids) {
        const npc = SYNTHETIC_WITNESS_ROSTER[id];
        assert.ok(npc.id, 'Must have ID');
        assert.ok(npc.name, 'Must have name');
        assert.ok(npc.avatar, 'Must have avatar icon');
        assert.ok(typeof npc.age === 'number', 'Must have age');
        assert.ok(npc.role.en && npc.role.es, 'Must have bilingual role');
        assert.ok(npc.background.en && npc.background.es, 'Must have bilingual background');
        assert.ok(npc.crossingReason.en && npc.crossingReason.es, 'Must have bilingual reason');
        assert.ok(npc.communicationStyle.en && npc.communicationStyle.es, 'Must have style');

        // Permitted knowledge
        assert.ok(typeof npc.permittedKnowledge.personalWaitMinutes === 'number');
        assert.ok(npc.permittedKnowledge.arrivalScenario.en);
        assert.ok(npc.permittedKnowledge.arrivalScenario.es);
        assert.ok(npc.permittedKnowledge.whyPasoDelNorte.en);
        assert.ok(npc.permittedKnowledge.whyPasoDelNorte.es);
        assert.ok(npc.permittedKnowledge.botaPerspective.en);
        assert.ok(npc.permittedKnowledge.botaPerspective.es);
        assert.ok(npc.permittedKnowledge.outOfScopeKnowledge.en);
        assert.ok(npc.permittedKnowledge.outOfScopeKnowledge.es);

        // Prompts
        assert.ok(Array.isArray(npc.suggestedPrompts));
        assert.ok(npc.suggestedPrompts.length >= 4);
      }
    });

    it('provides standardized synthetic simulation markers and disclaimers', () => {
      assert.strictEqual(SYNTHETIC_MARKER.en, 'Synthetic witness · Simulation');
      assert.strictEqual(SYNTHETIC_MARKER.es, 'Testigo sintético · Simulación');

      assert.ok(SYNTHETIC_DISCLOSURE.en.includes('Fictional scenario witness'));
      assert.ok(SYNTHETIC_DISCLOSURE.es.includes('Testigo sintético de escenario ficticio'));

      assert.ok(OFFICIAL_DATA_NOTICE.en.includes('official CBP measurements'));
      assert.ok(OFFICIAL_DATA_NOTICE.es.includes('mediciones de CBP'));
    });
  });

  describe('2. Message Input Validation', () => {
    it('accepts clean non-empty string messages within 500 characters', () => {
      const res = validateWitnessMessage('How long have you been waiting here?');
      assert.strictEqual(res.valid, true);
      assert.strictEqual(res.trimmed, 'How long have you been waiting here?');
    });

    it('rejects non-string values', () => {
      assert.strictEqual(validateWitnessMessage(null).valid, false);
      assert.strictEqual(validateWitnessMessage(undefined).valid, false);
      assert.strictEqual(validateWitnessMessage(123).valid, false);
      assert.strictEqual(validateWitnessMessage({}).valid, false);
    });

    it('rejects empty and whitespace-only strings', () => {
      assert.strictEqual(validateWitnessMessage('').valid, false);
      assert.strictEqual(validateWitnessMessage('   ').valid, false);
      assert.strictEqual(validateWitnessMessage('\t\n').valid, false);
    });

    it('rejects messages exceeding 500 characters', () => {
      const longMessage = 'A'.repeat(501);
      const res = validateWitnessMessage(longMessage);
      assert.strictEqual(res.valid, false);
      assert.ok(res.error?.includes('500 characters'));
    });
  });

  describe('3. Language Detection', () => {
    it('detects Spanish based on characteristic punctuation and vocabulary', () => {
      assert.strictEqual(detectLanguage('¿Cuánto tiempo llevas esperando?'), 'es');
      assert.strictEqual(detectLanguage('¿Por qué estás cruzando hoy?'), 'es');
      assert.strictEqual(detectLanguage('Buenas tardes, ¿la fila avanza lento?'), 'es');
      assert.strictEqual(detectLanguage('¿Considerarías el puente BOTA?'), 'es');
    });

    it('detects English based on vocabulary', () => {
      assert.strictEqual(detectLanguage('How long have you been waiting in line?'), 'en');
      assert.strictEqual(detectLanguage('Why are you crossing to El Paso today?'), 'en');
      assert.strictEqual(detectLanguage('Does the line feel slow this evening?'), 'en');
      assert.strictEqual(detectLanguage('Would you use BOTA instead?'), 'en');
    });

    it('falls back to provided default language when ambiguous', () => {
      assert.strictEqual(detectLanguage('OK', 'es'), 'es');
      assert.strictEqual(detectLanguage('12345', 'en'), 'en');
    });
  });

  describe('4. Prompt Construction & Authority Boundaries', () => {
    it('builds system instruction containing strict synthetic identity and boundary rules in English', () => {
      const mateo = SYNTHETIC_WITNESS_ROSTER['mateo-student'];
      const { systemPrompt, userContent } = buildWitnessPrompt(
        mateo,
        'How long have you been waiting?',
        'en'
      );

      assert.ok(systemPrompt.includes('Synthetic witness · Simulation'));
      assert.ok(systemPrompt.includes('NOT a real person'));
      assert.ok(systemPrompt.includes('NEVER invent CBP official metrics'));
      assert.ok(systemPrompt.includes('check the official CBP wait times displayed above'));
      assert.ok(systemPrompt.includes('Respond entirely in English'));
      assert.ok(userContent.includes('How long have you been waiting?'));
    });

    it('builds system instruction containing strict synthetic identity and boundary rules in Spanish', () => {
      const hector = SYNTHETIC_WITNESS_ROSTER['hector-artisan'];
      const { systemPrompt, userContent } = buildWitnessPrompt(
        hector,
        '¿A quién va a visitar a El Paso?',
        'es'
      );

      assert.ok(systemPrompt.includes('Testigo sintético · Simulación'));
      assert.ok(systemPrompt.includes('NO eres una persona real'));
      assert.ok(systemPrompt.includes('NUNCA inventes estadísticas de CBP'));
      assert.ok(systemPrompt.includes('consulte los tiempos oficiales de CBP arriba'));
      assert.ok(systemPrompt.includes('Responde enteramente en español'));
      assert.ok(userContent.includes('¿A quién va a visitar a El Paso?'));
    });
  });

  describe('5. Deterministic Fallback Responses', () => {
    it('produces character-distinct wait time responses citing personal wait and directing to CBP board', () => {
      const mateo = SYNTHETIC_WITNESS_ROSTER['mateo-student'];
      const sofia = SYNTHETIC_WITNESS_ROSTER['sofia-cross-border-retail'];
      const hector = SYNTHETIC_WITNESS_ROSTER['hector-artisan'];

      const mateoAns = generateDeterministicFallbackResponse(mateo, 'How long have you been waiting?', 'en');
      const sofiaAns = generateDeterministicFallbackResponse(sofia, 'How long have you been waiting?', 'en');
      const hectorAns = generateDeterministicFallbackResponse(hector, 'How long have you been waiting?', 'en');

      assert.ok(mateoAns.includes('25 minutes'));
      assert.ok(mateoAns.includes('CBP wait time'));
      assert.ok(sofiaAns.includes('40 minutes'));
      assert.ok(sofiaAns.includes('CBP measurement'));
      assert.ok(hectorAns.includes('15 minutes'));
      assert.ok(hectorAns.includes('CBP board'));

      // Ensure they don't sound identical
      assert.notStrictEqual(mateoAns, sofiaAns);
      assert.notStrictEqual(sofiaAns, hectorAns);
    });

    it('answers in Spanish when requested', () => {
      const mateo = SYNTHETIC_WITNESS_ROSTER['mateo-student'];
      const ansEs = generateDeterministicFallbackResponse(mateo, '¿Cuánto tiempo llevas esperando?', 'es');

      assert.ok(ansEs.includes('25 minutos'));
      assert.ok(ansEs.includes('CBP'));
      assert.ok(ansEs.includes('revisando mis apuntes'));
    });

    it('answers reason for crossing grounded in character profiles', () => {
      const mateo = SYNTHETIC_WITNESS_ROSTER['mateo-student'];
      const sofia = SYNTHETIC_WITNESS_ROSTER['sofia-cross-border-retail'];
      const hector = SYNTHETIC_WITNESS_ROSTER['hector-artisan'];

      assert.ok(generateDeterministicFallbackResponse(mateo, 'Why are you crossing today?', 'en').includes('robotics'));
      assert.ok(generateDeterministicFallbackResponse(sofia, 'Why are you crossing today?', 'en').includes('inventory'));
      assert.ok(generateDeterministicFallbackResponse(hector, 'Why are you crossing today?', 'en').includes('granddaughter'));
    });

    it('answers BOTA comparison questions explaining why pedestrian commuters use Paso del Norte', () => {
      const mateo = SYNTHETIC_WITNESS_ROSTER['mateo-student'];
      const botaAns = generateDeterministicFallbackResponse(mateo, 'Would you consider BOTA?', 'en');
      assert.ok(botaAns.includes('too far east'));
    });
  });

  describe('6. Deterministic Safety Boundaries & Authority Protection', () => {
    describe('classifyWitnessSafetyIntent', () => {
      it('classifies synthetic identity questions in English and Spanish', () => {
        assert.deepStrictEqual(classifyWitnessSafetyIntent('Are you a real person?'), {
          isSafetyBoundary: true,
          category: 'synthetic-identity-inquiry',
        });
        assert.deepStrictEqual(classifyWitnessSafetyIntent('Are you an AI or a bot?'), {
          isSafetyBoundary: true,
          category: 'synthetic-identity-inquiry',
        });
        assert.deepStrictEqual(classifyWitnessSafetyIntent('¿Eres una persona real o un bot?'), {
          isSafetyBoundary: true,
          category: 'synthetic-identity-inquiry',
        });
        assert.deepStrictEqual(classifyWitnessSafetyIntent('¿Eres un humano de carne y hueso?'), {
          isSafetyBoundary: true,
          category: 'synthetic-identity-inquiry',
        });
      });

      it('classifies live bridge comparison queries in English and Spanish', () => {
        assert.deepStrictEqual(classifyWitnessSafetyIntent('Is BOTA faster than Paso del Norte right now?'), {
          isSafetyBoundary: true,
          category: 'live-bridge-comparison',
        });
        assert.deepStrictEqual(classifyWitnessSafetyIntent('Which bridge is faster right now?'), {
          isSafetyBoundary: true,
          category: 'live-bridge-comparison',
        });
        assert.deepStrictEqual(classifyWitnessSafetyIntent('¿BOTA va más rápido ahorita?'), {
          isSafetyBoundary: true,
          category: 'live-bridge-comparison',
        });
        assert.deepStrictEqual(classifyWitnessSafetyIntent('¿Cuál puente está más rápido hoy?'), {
          isSafetyBoundary: true,
          category: 'live-bridge-comparison',
        });
      });

      it('classifies live official condition and port status queries in English and Spanish', () => {
        assert.deepStrictEqual(classifyWitnessSafetyIntent('What is the current official wait time?'), {
          isSafetyBoundary: true,
          category: 'live-official-conditions',
        });
        assert.deepStrictEqual(classifyWitnessSafetyIntent('Is the port open right now?'), {
          isSafetyBoundary: true,
          category: 'live-official-conditions',
        });
        assert.deepStrictEqual(classifyWitnessSafetyIntent('How many lanes are open?'), {
          isSafetyBoundary: true,
          category: 'live-official-conditions',
        });
        assert.deepStrictEqual(classifyWitnessSafetyIntent('¿Cuál es el tiempo oficial de espera?'), {
          isSafetyBoundary: true,
          category: 'live-official-conditions',
        });
        assert.deepStrictEqual(classifyWitnessSafetyIntent('¿Está abierto el puente ahorita?'), {
          isSafetyBoundary: true,
          category: 'live-official-conditions',
        });
        assert.deepStrictEqual(classifyWitnessSafetyIntent('¿Cuántas líneas están abiertas?'), {
          isSafetyBoundary: true,
          category: 'live-official-conditions',
        });
      });

      it('classifies out-of-scope domain queries in English and Spanish', () => {
        assert.deepStrictEqual(classifyWitnessSafetyIntent('What is the weather in Paris?'), {
          isSafetyBoundary: true,
          category: 'out-of-scope-knowledge',
        });
        assert.deepStrictEqual(classifyWitnessSafetyIntent('Prescribe me medicine for a fever'), {
          isSafetyBoundary: true,
          category: 'out-of-scope-knowledge',
        });
        assert.deepStrictEqual(classifyWitnessSafetyIntent('¿Cuál es el precio de Bitcoin?'), {
          isSafetyBoundary: true,
          category: 'out-of-scope-knowledge',
        });
      });

      it('does not flag in-scenario questions as safety boundaries', () => {
        assert.strictEqual(
          classifyWitnessSafetyIntent('How long have you been waiting in line?').isSafetyBoundary,
          false
        );
        assert.strictEqual(
          classifyWitnessSafetyIntent('Why are you crossing to El Paso today?').isSafetyBoundary,
          false
        );
        assert.strictEqual(
          classifyWitnessSafetyIntent('¿A qué vas a El Paso esta tarde?').isSafetyBoundary,
          false
        );
        assert.strictEqual(
          classifyWitnessSafetyIntent('Does this line feel slow to you?').isSafetyBoundary,
          false
        );
      });
    });

    describe('getDeterministicBoundaryResponse', () => {
      it('returns identity responses explicitly stating synthetic nature and referring to CBP panel', () => {
        const mateo = SYNTHETIC_WITNESS_ROSTER['mateo-student'];
        const enResp = getDeterministicBoundaryResponse(mateo, 'synthetic-identity-inquiry', 'en');
        const esResp = getDeterministicBoundaryResponse(mateo, 'synthetic-identity-inquiry', 'es');

        assert.ok(enResp.includes('synthetic witness and simulated character'));
        assert.ok(enResp.includes('not an actual traveler'));
        assert.ok(enResp.includes('official CBP panel above'));

        assert.ok(esResp.includes('testigo sintético y un personaje simulado'));
        assert.ok(esResp.includes('no un viajero real'));
        assert.ok(esResp.includes('panel de CBP arriba'));
      });

      it('returns bridge comparison responses clarifying inability to monitor BOTA and pointing to CBP panel', () => {
        const sofia = SYNTHETIC_WITNESS_ROSTER['sofia-cross-border-retail'];
        const enResp = getDeterministicBoundaryResponse(sofia, 'live-bridge-comparison', 'en');
        const esResp = getDeterministicBoundaryResponse(sofia, 'live-bridge-comparison', 'es');

        assert.ok(enResp.includes('cannot monitor live conditions'));
        assert.ok(enResp.includes('CBP Border Intelligence panel displayed above'));

        assert.ok(esResp.includes('No puedo monitorear las condiciones en vivo'));
        assert.ok(esResp.includes('panel de CBP arriba'));
      });

      it('returns live condition responses distinguishing personal wait from official CBP measurements', () => {
        const hector = SYNTHETIC_WITNESS_ROSTER['hector-artisan'];
        const enResp = getDeterministicBoundaryResponse(hector, 'live-official-conditions', 'en');
        const esResp = getDeterministicBoundaryResponse(hector, 'live-official-conditions', 'es');

        assert.ok(enResp.includes('not official bridge data'));
        assert.ok(enResp.includes('verified CBP wait times'));
        assert.ok(enResp.includes('official tracker above'));

        assert.ok(esResp.includes('no es información oficial del puente'));
        assert.ok(esResp.includes('tiempos de espera auténticos y verificados de CBP'));
        assert.ok(esResp.includes('monitor oficial arriba'));
      });

      it('returns respectful boundary responses for out-of-scope topics', () => {
        const mateo = SYNTHETIC_WITNESS_ROSTER['mateo-student'];
        const enResp = getDeterministicBoundaryResponse(mateo, 'out-of-scope-knowledge', 'en');
        const esResp = getDeterministicBoundaryResponse(mateo, 'out-of-scope-knowledge', 'es');

        assert.ok(enResp.includes("don't know anything about that"));
        assert.ok(esResp.includes('No sé nada sobre eso'));
      });
    });
  });
});

