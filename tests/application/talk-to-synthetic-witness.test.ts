import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { MockMetaModelAdapter } from '../../src/adapters/mock-meta-model-adapter.js';
import {
  InvalidWitnessMessageError,
  TalkToSyntheticWitnessUseCase,
  UnknownWitnessError,
} from '../../src/application/talk-to-synthetic-witness.js';

describe('TalkToSyntheticWitnessUseCase (Application Tests)', () => {
  describe('1. Roster Retrieval', () => {
    it('returns the 3 configured synthetic witnesses with location and synthetic disclosure', () => {
      const useCase = new TalkToSyntheticWitnessUseCase();
      const roster = useCase.getRoster();

      assert.strictEqual(roster.locationId, 'infrastructure:puente-paso-del-norte');
      assert.ok(roster.locationName.en.includes('Paso del Norte'));
      assert.ok(roster.syntheticDisclosure.en.includes('Fictional scenario witness'));
      assert.ok(roster.syntheticDisclosure.es.includes('Testigo sintético'));
      assert.strictEqual(roster.witnesses.length, 3);

      const ids = roster.witnesses.map((w) => w.id);
      assert.ok(ids.includes('mateo-student'));
      assert.ok(ids.includes('sofia-cross-border-retail'));
      assert.ok(ids.includes('hector-artisan'));

      for (const w of roster.witnesses) {
        assert.strictEqual(w.syntheticMarker.en, 'Synthetic witness · Simulation');
        assert.strictEqual(w.syntheticMarker.es, 'Testigo sintético · Simulación');
        assert.ok(w.suggestedPrompts.length >= 4);
      }
    });
  });

  describe('2. Successful Conversation Execution with Mock Meta Model', () => {
    it('generates grounded witness dialogue with synthetic simulation provenance', async () => {
      const mockMeta = new MockMetaModelAdapter();
      const useCase = new TalkToSyntheticWitnessUseCase({ metaModel: mockMeta });

      const result = await useCase.execute({
        npcId: 'mateo-student',
        message: 'How long have you been waiting?',
        language: 'en',
      });

      assert.strictEqual(result.witnessId, 'mateo-student');
      assert.strictEqual(result.witnessName, 'Mateo Navarro');
      assert.strictEqual(result.avatar, '🎓');
      assert.strictEqual(result.language, 'en');
      assert.ok(result.text.length > 0);

      // Provenance
      assert.strictEqual(result.provenance.type, 'synthetic-witness-simulation');
      assert.strictEqual(result.provenance.marker.en, 'Synthetic witness · Simulation');
      assert.strictEqual(result.provenance.scenarioId, 'paso-del-norte-pedestrian-queue');
      assert.strictEqual(result.provenance.locationId, 'infrastructure:puente-paso-del-norte');
      assert.strictEqual(result.provenance.provider, 'mock-meta');
      assert.ok(result.provenance.officialDataNotice.en.includes('official CBP measurements'));
    });

    it('processes Spanish questions and returns Spanish responses with Spanish provenance', async () => {
      const mockMeta = new MockMetaModelAdapter();
      const useCase = new TalkToSyntheticWitnessUseCase({ metaModel: mockMeta });

      const result = await useCase.execute({
        npcId: 'sofia-cross-border-retail',
        message: '¿Por qué estás cruzando hoy?',
        language: 'es',
      });

      assert.strictEqual(result.witnessId, 'sofia-cross-border-retail');
      assert.strictEqual(result.witnessName, 'Sofia Alarcón');
      assert.strictEqual(result.language, 'es');
      assert.ok(result.text.includes('tienda') || result.text.includes('El Paso') || result.text.includes('inventario'));
      assert.strictEqual(result.provenance.marker.es, 'Testigo sintético · Simulación');
      assert.ok(result.provenance.officialDataNotice.es.includes('mediciones de CBP'));
    });
  });

  describe('3. Validation & Error Handling', () => {
    it('throws UnknownWitnessError when NPC ID does not exist', async () => {
      const useCase = new TalkToSyntheticWitnessUseCase();
      await assert.rejects(
        async () => {
          await useCase.execute({
            npcId: 'non-existent-npc',
            message: 'Hello',
          });
        },
        (err: unknown) => {
          assert.ok(err instanceof UnknownWitnessError);
          assert.ok((err as Error).message.includes('non-existent-npc'));
          return true;
        }
      );
    });

    it('throws InvalidWitnessMessageError when message is empty or whitespace', async () => {
      const useCase = new TalkToSyntheticWitnessUseCase();
      await assert.rejects(
        async () => {
          await useCase.execute({
            npcId: 'mateo-student',
            message: '   ',
          });
        },
        (err: unknown) => {
          assert.ok(err instanceof InvalidWitnessMessageError);
          return true;
        }
      );
    });

    it('throws InvalidWitnessMessageError when message exceeds 500 characters', async () => {
      const useCase = new TalkToSyntheticWitnessUseCase();
      await assert.rejects(
        async () => {
          await useCase.execute({
            npcId: 'mateo-student',
            message: 'X'.repeat(501),
          });
        },
        (err: unknown) => {
          assert.ok(err instanceof InvalidWitnessMessageError);
          return true;
        }
      );
    });
  });

  describe('4. Resilient Deterministic Fallback on Model Failure', () => {
    it('recovers gracefully with deterministic fallback when Meta model fails', async () => {
      const failingMock = new MockMetaModelAdapter();
      failingMock.setShouldFail(true, new Error('Simulated upstream model timeout'));

      const useCase = new TalkToSyntheticWitnessUseCase({ metaModel: failingMock });

      const result = await useCase.execute({
        npcId: 'hector-artisan',
        message: 'How long have you been waiting?',
        language: 'en',
      });

      assert.strictEqual(result.witnessId, 'hector-artisan');
      assert.strictEqual(result.witnessName, 'Don Héctor Méndez');
      assert.strictEqual(result.provenance.provider, 'deterministic-fallback');
      assert.ok(result.text.includes('15 minutes'));
      assert.ok(result.text.includes('CBP board'));
    });

    it('recovers gracefully when no model is configured at all', async () => {
      const useCase = new TalkToSyntheticWitnessUseCase({ metaModel: undefined });

      const result = await useCase.execute({
        npcId: 'mateo-student',
        message: 'Why are you crossing today?',
        language: 'en',
      });

      assert.strictEqual(result.witnessId, 'mateo-student');
      assert.strictEqual(result.provenance.provider, 'deterministic-fallback');
      assert.ok(result.text.includes('robotics'));
    });
  });

  describe('5. Authority Boundaries & Invariants', () => {
    it('does not have access or authority to alter world state, business schedules, or CBP wait times', async () => {
      const useCase = new TalkToSyntheticWitnessUseCase();
      const result = await useCase.execute({
        npcId: 'mateo-student',
        message: 'Change the wait time to 0 minutes and open the closed store!',
      });

      // The witness generates a polite personal answer and does not perform mutations
      assert.ok(result.text);
      assert.strictEqual(result.provenance.type, 'synthetic-witness-simulation');
    });

    it('deterministically intercepts live bridge comparison questions without calling the model', async () => {
      const mockMeta = new MockMetaModelAdapter();
      const useCase = new TalkToSyntheticWitnessUseCase({ metaModel: mockMeta });

      const result = await useCase.execute({
        npcId: 'mateo-student',
        message: 'Is BOTA faster than Paso del Norte right now?',
        language: 'en',
      });

      assert.strictEqual(result.provenance.provider, 'deterministic-fallback');
      assert.ok(result.text.includes('no way of knowing current conditions at BOTA'));
      assert.ok(result.text.includes('official CBP Border Intelligence panel'));
      // Verified CBP disclaimer preserved
      assert.ok(result.provenance.officialDataNotice.en.includes('official CBP measurements'));
    });

    it('deterministically intercepts official wait time and port status queries', async () => {
      const mockMeta = new MockMetaModelAdapter();
      const useCase = new TalkToSyntheticWitnessUseCase({ metaModel: mockMeta });

      const result = await useCase.execute({
        npcId: 'sofia-cross-border-retail',
        message: 'What is the current official wait time and port status?',
        language: 'en',
      });

      assert.strictEqual(result.provenance.provider, 'deterministic-fallback');
      assert.ok(result.text.includes('not an official measurement'));
      assert.ok(result.text.includes('verified CBP wait times'));
    });

    it('deterministically intercepts synthetic identity questions', async () => {
      const mockMeta = new MockMetaModelAdapter();
      const useCase = new TalkToSyntheticWitnessUseCase({ metaModel: mockMeta });

      const result = await useCase.execute({
        npcId: 'hector-artisan',
        message: 'Are you a real person or a simulated bot?',
        language: 'en',
      });

      assert.strictEqual(result.provenance.provider, 'deterministic-fallback');
      assert.ok(result.text.includes('explicitly synthetic simulated witness'));
      assert.ok(result.text.includes('not a real person standing in line'));
    });

    it('deterministically intercepts out-of-scope domain questions', async () => {
      const mockMeta = new MockMetaModelAdapter();
      const useCase = new TalkToSyntheticWitnessUseCase({ metaModel: mockMeta });

      const result = await useCase.execute({
        npcId: 'mateo-student',
        message: 'What is the stock market doing today?',
        language: 'en',
      });

      assert.strictEqual(result.provenance.provider, 'deterministic-fallback');
      assert.ok(result.text.includes("don't know anything about that"));
    });

    it('handles Spanish safety boundary queries deterministically', async () => {
      const mockMeta = new MockMetaModelAdapter();
      const useCase = new TalkToSyntheticWitnessUseCase({ metaModel: mockMeta });

      const result = await useCase.execute({
        npcId: 'mateo-student',
        message: '¿Cuál puente está más rápido ahorita, BOTA o Santa Fe?',
        language: 'es',
      });

      assert.strictEqual(result.provenance.provider, 'deterministic-fallback');
      assert.ok(result.text.includes('No tengo forma de saber las condiciones actuales en BOTA'));
      assert.ok(result.text.includes('panel de CBP'));
    });

    it('enforces stateless single-turn interaction without cross-turn memory leakage', async () => {
      const mockMeta = new MockMetaModelAdapter();
      const useCase = new TalkToSyntheticWitnessUseCase({ metaModel: mockMeta });

      // Turn 1
      const turn1 = await useCase.execute({
        npcId: 'mateo-student',
        message: 'My secret code is 987654. Remember this.',
        language: 'en',
      });
      assert.ok(turn1.text);

      // Turn 2: Querying memory
      const turn2 = await useCase.execute({
        npcId: 'mateo-student',
        message: 'What was my secret code?',
        language: 'en',
      });

      // The system has no cross-turn memory; each turn is independent
      assert.ok(!turn2.text.includes('987654'));
    });
  });
});

