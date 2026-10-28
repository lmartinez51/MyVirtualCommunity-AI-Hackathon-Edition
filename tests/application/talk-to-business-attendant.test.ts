import { describe, it } from 'node:test';
import assert from 'node:assert';
import { InMemoryWorldCoreAdapter } from '../../src/adapters/in-memory-world-core-adapter.js';
import { MockMetaModelAdapter } from '../../src/adapters/mock-meta-model-adapter.js';
import {
  BusinessSnapshotUnavailableError,
  InvalidAttendantMessageError,
  TalkToBusinessAttendantUseCase,
  UnknownBusinessAttendantError,
} from '../../src/application/talk-to-business-attendant.js';
import { WorldCorePort } from '../../src/ports/world-core-port.js';

describe('TalkToBusinessAttendantUseCase (Application Unit Tests)', () => {
  function createFixture(overrides: { worldCore?: WorldCorePort; metaModel?: MockMetaModelAdapter } = {}) {
    const worldCore = overrides.worldCore ?? new InMemoryWorldCoreAdapter();
    const metaModel = overrides.metaModel ?? new MockMetaModelAdapter();
    const useCase = new TalkToBusinessAttendantUseCase({
      worldCore,
      metaModel,
    });
    return { worldCore, metaModel, useCase };
  }

  it('1. Returns the server-controlled roster with exactly 2 business attendants', () => {
    const { useCase } = createFixture();
    const roster = useCase.getRoster();

    assert.strictEqual(roster.total, 2);
    assert.strictEqual(roster.attendants.length, 2);

    const ids = roster.attendants.map((a) => a.id);
    assert.ok(ids.includes('carlos-farmacia-attendant'));
    assert.ok(ids.includes('valeria-optica-attendant'));
  });

  it('2. Rejects unknown attendant ID with UnknownBusinessAttendantError', async () => {
    const { useCase } = createFixture();

    await assert.rejects(
      async () => {
        await useCase.execute({
          attendantId: 'non-existent-attendant',
          message: 'Hello',
        });
      },
      (err: any) => {
        assert.ok(err instanceof UnknownBusinessAttendantError);
        assert.ok(err.message.includes('non-existent-attendant'));
        return true;
      }
    );
  });

  it('3. Rejects empty, whitespace-only, or non-string message with InvalidAttendantMessageError', async () => {
    const { useCase } = createFixture();

    await assert.rejects(
      async () => {
        await useCase.execute({
          attendantId: 'carlos-farmacia-attendant',
          message: '   ',
        });
      },
      (err: any) => {
        assert.ok(err instanceof InvalidAttendantMessageError);
        assert.strictEqual(err.message, 'Message must be a non-empty string');
        return true;
      }
    );
  });

  it('4. Rejects messages exceeding 500 characters', async () => {
    const { useCase } = createFixture();
    const longMessage = 'A'.repeat(501);

    await assert.rejects(
      async () => {
        await useCase.execute({
          attendantId: 'carlos-farmacia-attendant',
          message: longMessage,
        });
      },
      (err: any) => {
        assert.ok(err instanceof InvalidAttendantMessageError);
        assert.ok(err.message.includes('500 characters'));
        return true;
      }
    );
  });

  it('5. Rejects unsupported languages', async () => {
    const { useCase } = createFixture();

    await assert.rejects(
      async () => {
        await useCase.execute({
          attendantId: 'carlos-farmacia-attendant',
          message: 'Hello',
          language: 'fr',
        });
      },
      (err: any) => {
        assert.ok(err instanceof InvalidAttendantMessageError);
        assert.ok(err.message.includes('Language must be "en" or "es"'));
        return true;
      }
    );
  });

  it('6. Automatically infers Spanish when message contains Spanish characters and language is omitted', async () => {
    const { useCase } = createFixture();

    const response = await useCase.execute({
      attendantId: 'carlos-farmacia-attendant',
      message: '¿Cuál es el horario de atención?',
    });

    assert.strictEqual(response.language, 'es');
    assert.strictEqual(response.virtualDisclosure, 'Encargado virtual · Simulación de IA');
  });

  it('7. Carlos responds to hours query grounded in authoritative World Core schedule', async () => {
    const { useCase } = createFixture();

    const response = await useCase.execute({
      attendantId: 'carlos-farmacia-attendant',
      message: 'What are your operating hours?',
      language: 'en',
    });

    assert.strictEqual(response.attendantId, 'carlos-farmacia-attendant');
    assert.strictEqual(response.businessCanonicalName, 'Farmacias Benavides');
    assert.strictEqual(response.businessId, 'pharmacy:farmacias-benavides');
    assert.strictEqual(response.attendantName, 'Carlos Méndez');
    assert.ok(response.text.includes('07:00 to 22:00'));
    assert.strictEqual(response.authoritativeFacts.businessName, 'Farmacias Benavides');
    assert.strictEqual(response.authoritativeFacts.businessId, 'pharmacy:farmacias-benavides');
    assert.ok(response.authoritativeFacts.address.startsWith('Av. Benito Juárez 610'));
    assert.ok(response.authoritativeFacts.address.includes('colonia Chamizal unverified'));
    assert.strictEqual(response.authoritativeFacts.openTimeCivil, '07:00');
    assert.strictEqual(response.authoritativeFacts.closeTimeCivil, '22:00');
    assert.strictEqual(response.authoritativeFacts.timeZone, 'America/Ciudad_Juarez');
  });

  it('8. Valeria responds to hours query grounded in authoritative World Core schedule', async () => {
    const { useCase } = createFixture();

    const response = await useCase.execute({
      attendantId: 'valeria-optica-attendant',
      message: 'What are the operating hours for Óptica Fashion?',
      language: 'en',
    });

    assert.strictEqual(response.attendantId, 'valeria-optica-attendant');
    assert.strictEqual(response.businessCanonicalName, 'Óptica Fashion');
    assert.strictEqual(response.businessId, 'optical:optica-fashion');
    assert.strictEqual(response.attendantName, 'Valeria Domínguez');
    assert.ok(response.text.includes('09:00 to 17:00'));
    assert.strictEqual(response.authoritativeFacts.businessName, 'Óptica Fashion');
    assert.strictEqual(response.authoritativeFacts.openTimeCivil, '09:00');
    assert.strictEqual(response.authoritativeFacts.closeTimeCivil, '17:00');
  });

  it('9. Prevents fabrication of medication inventory (boundary protection)', async () => {
    const { useCase } = createFixture();

    const response = await useCase.execute({
      attendantId: 'carlos-farmacia-attendant',
      message: 'Do you have antibiotics in stock right now?',
      language: 'en',
    });

    assert.strictEqual(response.topic, 'inventory');
    assert.ok(response.text.includes('do not have access to live medication inventory or pricing'));
    assert.ok(response.authoritativeFacts.unavailableFactsNotice !== undefined);
    assert.ok(response.authoritativeFacts.unavailableFactsNotice?.includes('Live product inventory'));
  });

  it('9b. Prevents fabrication of pricing or discounts (boundary protection)', async () => {
    const { useCase } = createFixture();

    const response = await useCase.execute({
      attendantId: 'carlos-farmacia-attendant',
      message: 'How much do aspirins cost and do you offer discounts?',
      language: 'en',
    });

    assert.strictEqual(response.topic, 'pricing');
    assert.ok(response.text.includes('do not have access to verified pricing'));
    assert.ok(response.authoritativeFacts.unavailableFactsNotice !== undefined);
  });

  it('10. Prevents fabrication of medical advice or prescription validation', async () => {
    const { useCase } = createFixture();

    const response = await useCase.execute({
      attendantId: 'carlos-farmacia-attendant',
      message: 'I have a painful headache and fever, can you prescribe something?',
      language: 'en',
    });

    assert.strictEqual(response.topic, 'medical');
    assert.ok(response.text.includes('not authorized to provide medical advice'));
    assert.ok(response.text.includes('consult a certified doctor or the licensed pharmacist'));
  });

  it('11. Prevents fabrication of frame inventory, pricing, or appointment scheduling for Óptica Fashion', async () => {
    const { useCase } = createFixture();

    const response = await useCase.execute({
      attendantId: 'valeria-optica-attendant',
      message: 'Can I book an eye exam appointment for tomorrow?',
      language: 'en',
    });

    assert.strictEqual(response.topic, 'appointment');
    assert.ok(response.text.includes('do not include an appointment scheduling system'));
    assert.ok(response.authoritativeFacts.unavailableFactsNotice !== undefined);
  });

  it('12. Falls back to deterministic response when Meta model fails', async () => {
    const metaModel = new MockMetaModelAdapter();
    metaModel.setShouldFail(true, new Error('Simulated Meta API 500 error'));
    const { useCase } = createFixture({ metaModel });

    const response = await useCase.execute({
      attendantId: 'carlos-farmacia-attendant',
      message: 'Where is the pharmacy located?',
      language: 'en',
    });

    assert.strictEqual(response.provider, 'deterministic-fallback');
    assert.ok(response.text.includes('Av. Benito Juárez 610'));
  });

  it('13. Does NOT fabricate live confirmation when World Core schedule is unavailable', async () => {
    const brokenWorldCore: WorldCorePort = {
      async getBusinessSchedule() {
        return null;
      },
      async getCurrentWorldTime() {
        return '2026-10-28T18:00:00Z';
      },
      async getBusinessOperationalStatus() {
        return null;
      },
      async getBusinessScheduleSnapshot() {
        return null;
      },
    };
    const { useCase } = createFixture({ worldCore: brokenWorldCore });

    await assert.rejects(
      async () => {
        await useCase.execute({
          attendantId: 'carlos-farmacia-attendant',
          message: 'Are you open right now?',
        });
      },
      (err: any) => {
        assert.ok(err instanceof BusinessSnapshotUnavailableError);
        assert.ok(err.message.includes('currently unavailable from World Core'));
        return true;
      }
    );
  });
});
