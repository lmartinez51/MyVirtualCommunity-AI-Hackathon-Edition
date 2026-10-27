import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { InMemoryWorldCoreAdapter } from '../../src/adapters/in-memory-world-core-adapter.js';
import { MockMetaModelAdapter } from '../../src/adapters/mock-meta-model-adapter.js';
import {
  AnswerBusinessHoursQueryUseCase,
  BusinessNotFoundError,
  InvalidQueryError,
} from '../../src/application/answer-business-hours-query.js';
import { InvalidBusinessIdError } from '../../src/domain/business-id.js';

describe('AnswerBusinessHoursQueryUseCase', () => {
  it('successfully executes end-to-end flow with verified operational grounding', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const mockMeta = new MockMetaModelAdapter();
    const useCase = new AnswerBusinessHoursQueryUseCase({
      worldCore,
      metaModel: mockMeta,
    });

    // 2026-10-26 (Monday) 14:00 local Ciudad Juárez (20:00 UTC) -> Wing Spot is OPEN
    const result = await useCase.execute({
      businessId: 'restaurant:wing-spot',
      userQuery: '¿A qué hora cierran hoy?',
      evaluatedAtOverride: '2026-10-26T20:00:00.000Z',
    });

    assert.equal(result.success, true);
    assert.equal(result.verifiedStatus.businessId, 'restaurant:wing-spot');
    assert.equal(result.verifiedStatus.status, 'OPEN');
    assert.equal(result.verifiedStatus.isOpen, true);
    assert.equal(result.verifiedStatus.admissionPermitted, true);
    assert.match(result.answer, /ABIERTO/);

    // Verify what was sent to Meta Model: must contain the verified operational facts!
    assert.ok(mockMeta.lastContextReceived);
    assert.equal(mockMeta.lastContextReceived.verifiedData.status, 'OPEN');
    assert.equal(mockMeta.lastContextReceived.userQuery, '¿A qué hora cierran hoy?');
    assert.equal(mockMeta.lastContextReceived.schedule.hours.closeHour, 18);
  });

  it('correctly handles CLOSING_SOON status when evaluated during cutoff period', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const mockMeta = new MockMetaModelAdapter();
    const useCase = new AnswerBusinessHoursQueryUseCase({
      worldCore,
      metaModel: mockMeta,
    });

    // 17:50 local (23:50 UTC) -> Cutoff was 17:45, closes 18:00
    const result = await useCase.execute({
      businessId: 'restaurant:wing-spot',
      userQuery: '¿Todavía puedo entrar a cenar?',
      evaluatedAtOverride: '2026-10-26T23:50:00.000Z',
    });

    assert.equal(result.verifiedStatus.status, 'CLOSING_SOON');
    assert.equal(result.verifiedStatus.isOpen, true);
    assert.equal(result.verifiedStatus.admissionPermitted, false);
    assert.match(result.answer, /por cerrar/);
  });

  it('correctly handles CLOSED status on non-operating day', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const mockMeta = new MockMetaModelAdapter();
    const useCase = new AnswerBusinessHoursQueryUseCase({
      worldCore,
      metaModel: mockMeta,
    });

    // 2026-10-25 is Sunday
    const result = await useCase.execute({
      businessId: 'restaurant:wing-spot',
      userQuery: '¿Están abiertos hoy domingo?',
      evaluatedAtOverride: '2026-10-25T18:00:00.000Z',
    });

    assert.equal(result.verifiedStatus.status, 'CLOSED');
    assert.equal(result.verifiedStatus.isOpen, false);
    assert.match(result.answer, /CERRADO/);
  });

  it('throws BusinessNotFoundError when requested business is not in World Core', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const mockMeta = new MockMetaModelAdapter();
    const useCase = new AnswerBusinessHoursQueryUseCase({
      worldCore,
      metaModel: mockMeta,
    });

    await assert.rejects(
      () =>
        useCase.execute({
          businessId: 'restaurant:non-existent-cafe',
          userQuery: '¿Están abiertos?',
        }),
      BusinessNotFoundError
    );
  });

  it('throws InvalidBusinessIdError for malformed business IDs', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const mockMeta = new MockMetaModelAdapter();
    const useCase = new AnswerBusinessHoursQueryUseCase({
      worldCore,
      metaModel: mockMeta,
    });

    await assert.rejects(
      () =>
        useCase.execute({
          businessId: 'invalid_no_colon',
          userQuery: 'Horarios?',
        }),
      InvalidBusinessIdError
    );
  });

  it('throws InvalidQueryError for empty queries', async () => {
    const worldCore = new InMemoryWorldCoreAdapter();
    const mockMeta = new MockMetaModelAdapter();
    const useCase = new AnswerBusinessHoursQueryUseCase({
      worldCore,
      metaModel: mockMeta,
    });

    await assert.rejects(
      () =>
        useCase.execute({
          businessId: 'restaurant:wing-spot',
          userQuery: '   ',
        }),
      InvalidQueryError
    );
  });
});
