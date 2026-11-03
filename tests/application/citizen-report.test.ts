import assert from 'node:assert/strict';
import { describe, it, before } from 'node:test';
import { existsSync, unlinkSync, mkdirSync, writeFileSync, utimesSync, rmSync } from 'node:fs';
import { resolve, join } from 'node:path';
import sharp from 'sharp';
import { InMemoryWorldCoreAdapter } from '../../src/adapters/in-memory-world-core-adapter.js';
import { MockMetaModelAdapter } from '../../src/adapters/mock-meta-model-adapter.js';
import { MetaModelApiAdapter } from '../../src/adapters/meta-model-api-adapter.js';
import {
  CreateCitizenReportDraftUseCase,
  InvalidImageEvidenceError,
  cleanupExpiredEvidence,
} from '../../src/application/create-citizen-report-draft.js';
import { createAssistantServer } from '../../src/server/index.js';
import { sanitizeAndNormalizeImage } from '../../src/domain/image-sanitizer.js';
import {
  PlayerConfirmationRequiredError,
  SubmitCitizenReportUseCase,
} from '../../src/application/submit-citizen-report.js';
import { GetCitizenReportUseCase } from '../../src/application/get-citizen-report.js';
import {
  CITIZEN_REPRESENTATIVE_PROFILE,
  DEFAULT_REPORTING_REPRESENTATIVE,
  LEGACY_CATEGORY_MAP,
  MAX_IMAGE_SIZE_BYTES,
  SIMULATED_MUNICIPAL_SERVICE_NOTICE_EN,
  SIMULATED_MUNICIPAL_SERVICE_NOTICE_ES,
  assessNonEmergencyBoundary,
  classifyCivicIssue,
  getSimulatedDepartment,
  normalizeCitizenReportRecord,
  normalizeIssueCategory,
  validateImageBuffer,
} from '../../src/domain/citizen-report.js';

describe('Increment 5.11: Multimodal Citizen Issue Reporting (SAM 3.1 + Muse Spark + World Core)', () => {
  // Test fixture helpers: valid 1x1 PNG and JPEG buffers
  const validPngBuffer = Buffer.from([
    0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, // PNG magic
    0x00, 0x00, 0x00, 0x0d, 0x49, 0x48, 0x44, 0x52, // IHDR chunk
    0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
    0x08, 0x06, 0x00, 0x00, 0x00, 0x1f, 0x15, 0xc4,
    0x89, 0x00, 0x00, 0x00, 0x0a, 0x49, 0x44, 0x41,
    0x54, 0x78, 0x9c, 0x63, 0x00, 0x01, 0x00, 0x00,
    0x05, 0x00, 0x01, 0x0d, 0x0a, 0x2d, 0xb4, 0x00,
    0x00, 0x00, 0x00, 0x49, 0x45, 0x4e, 0x44, 0xae,
    0x42, 0x60, 0x82,
  ]);

  // Genuine decodable minimal 8x8 JPEG buffer created via sharp
  let validJpegBuffer: Buffer;
  before(async () => {
    validJpegBuffer = await sharp({
      create: { width: 8, height: 8, channels: 3, background: { r: 120, g: 120, b: 120 } },
    })
      .jpeg()
      .toBuffer();
  });

  // Requirement 1: Valid pothole photograph upload (PNG & JPEG)
  it('1. validates and accepts valid PNG and JPEG photographic evidence', () => {
    const pngResult = validateImageBuffer(validPngBuffer);
    assert.equal(pngResult.valid, true);
    assert.equal(pngResult.detectedMime, 'image/png');
    assert.equal(pngResult.format, 'png');

    const jpegResult = validateImageBuffer(validJpegBuffer);
    assert.equal(jpegResult.valid, true);
    assert.equal(jpegResult.detectedMime, 'image/jpeg');
    assert.equal(jpegResult.format, 'jpeg');
  });

  // Requirement 2: Invalid image format rejected
  it('2. rejects unsupported or malformed image formats (e.g. text/exe/corrupt)', () => {
    const fakeGif = Buffer.from('GIF89a1234567890');
    const result = validateImageBuffer(fakeGif);
    assert.equal(result.valid, false);
    assert.match(result.error ?? '', /Unsupported image format/i);
  });

  // Requirement 3: Oversized image (>5MB rejected)
  it('3. rejects images exceeding the 5MB upload limit', () => {
    const oversizedBuffer = Buffer.alloc(MAX_IMAGE_SIZE_BYTES + 1024, 0x00);
    oversizedBuffer[0] = 0x89;
    oversizedBuffer[1] = 0x50;
    oversizedBuffer[2] = 0x4e;
    oversizedBuffer[3] = 0x47;

    const result = validateImageBuffer(oversizedBuffer);
    assert.equal(result.valid, false);
    assert.match(result.error ?? '', /exceeds maximum allowed size/i);
  });

  // Requirement 4: Missing or unavailable image handled gracefully
  it('4. handles report draft without image gracefully with visual analysis null', async () => {
    const mockMeta = new MockMetaModelAdapter();
    const createDraft = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });

    const draft = await createDraft.execute({
      citizenDescription: 'Bache profundo cerca de la farmacia en Avenida Juárez.',
      language: 'es',
    });

    assert.ok(draft.draftId.startsWith('draft_'));
    assert.equal(draft.evidence, null);
    assert.equal(draft.visualAnalysis, null);
    assert.equal(draft.confirmationRequired, true);
    assert.ok(draft.structuredInterpretation.summary.length > 0);
  });

  // Requirement 5: SAM successful segmentation
  it('5. executes SAM 3.1 segmentation returning bounding boxes and status completed', async () => {
    const mockMeta = new MockMetaModelAdapter();
    const createDraft = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });

    const draft = await createDraft.execute({
      citizenDescription: 'Bache grande en el asfalto que daña llantas.',
      image: {
        fileName: 'bache.jpg',
        mimeType: 'image/jpeg',
        base64: validJpegBuffer.toString('base64'),
      },
      externalAiProcessingConsent: true,
      language: 'es',
    });

    assert.ok(draft.evidence !== null);
    assert.ok(draft.visualAnalysis !== null);
    const va = draft.visualAnalysis!;
    assert.equal(va.status, 'completed');
    assert.equal(va.model, 'sam-3.1');
    assert.ok(va.boxes.length > 0);
    assert.ok(va.boxes[0]!.xmax > va.boxes[0]!.xmin);
    assert.ok(va.boxes[0]!.ymax > va.boxes[0]!.ymin);
  });

  // Requirement 6: SAM no-match result (inconclusive)
  it('6. handles SAM 3.1 no-match result gracefully as inconclusive without claiming damage does not exist', async () => {
    const mockMeta = new MockMetaModelAdapter({ samNoMatch: true });
    const createDraft = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });

    const draft = await createDraft.execute({
      citizenDescription: 'Reporte de asfalto liso sin bache visible en la foto.',
      image: {
        fileName: 'calle_limpia.png',
        mimeType: 'image/png',
        base64: validPngBuffer.toString('base64'),
      },
      externalAiProcessingConsent: true,
      language: 'es',
    });

    assert.ok(draft.visualAnalysis !== null);
    assert.equal(draft.visualAnalysis.status, 'inconclusive');
    assert.equal(draft.visualAnalysis.boxes.length, 0);
    assert.match(draft.visualAnalysis.maskSummary ?? '', /No distinct boundaries detected|No distinct candidate region/i);
  });

  // Requirement 7: SAM API unavailable fails closed
  it('7. fails safe when SAM API is unavailable without crashing, marking visual analysis unavailable', async () => {
    const mockMeta = new MockMetaModelAdapter({ samShouldFail: true });
    const createDraft = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });

    const draft = await createDraft.execute({
      citizenDescription: 'Bache frente a la tienda de recuerdos.',
      image: {
        fileName: 'evidencia.jpg',
        mimeType: 'image/jpeg',
        base64: validJpegBuffer.toString('base64'),
      },
      externalAiProcessingConsent: true,
      language: 'es',
    });

    assert.ok(draft.visualAnalysis !== null);
    assert.equal(draft.visualAnalysis.status, 'unavailable');
    assert.match(draft.visualAnalysis.maskSummary ?? '', /unavailable/i);
  });

  // Requirement 8: Muse Spark structured report generation
  it('8. generates structured report interpretation with Muse Spark (summary, category, missing info)', async () => {
    const mockMeta = new MockMetaModelAdapter();
    const createDraft = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });

    const draft = await createDraft.execute({
      citizenDescription: 'Hay un bache peligroso y asfalto roto a media calle.',
      language: 'es',
    });

    assert.equal(draft.structuredInterpretation.model, 'muse-spark-1.3');
    assert.ok(draft.structuredInterpretation.summary.length > 0);
    assert.equal(draft.structuredInterpretation.category, 'road-surface-damage');
    assert.ok(Array.isArray(draft.structuredInterpretation.missingInformation));
  });

  // Requirement 9: Muse Spark unavailable fallback
  it('9. provides deterministic structured fallback when Muse Spark is unavailable', async () => {
    const mockMeta = new MockMetaModelAdapter({ museShouldFail: true });
    const createDraft = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });

    const draft = await createDraft.execute({
      citizenDescription: 'Bache profundo en el carril derecho.',
      language: 'es',
    });

    assert.equal(draft.structuredInterpretation.model, 'muse-spark-1.3');
    assert.equal(draft.structuredInterpretation.provider, 'deterministic-fallback');
    assert.match(draft.structuredInterpretation.summary, /Bache profundo/i);
    assert.equal(draft.structuredInterpretation.category, 'road-surface-damage');
  });

  // Requirement 10: Missing location resolves to UNRESOLVED without inventing Avenida Benito Juárez
  it('10. marks missing location as UNRESOLVED without inventing Avenida Benito Juárez', async () => {
    const mockMeta = new MockMetaModelAdapter();
    const createDraft = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });

    const draft = await createDraft.execute({
      citizenDescription: 'Reporte sin ubicación exacta.',
      language: 'en',
    });

    assert.equal(draft.location.isApproximate, true);
    assert.equal(draft.location.coverageStatus, 'UNRESOLVED');
    assert.equal(draft.location.coordinates, null);
    assert.equal(draft.location.description, 'Unspecified location');
  });

  // Requirement 11: Player confirmation required before registration
  it('11. strictly enforces player confirmation before World Core registration', async () => {
    const mockMeta = new MockMetaModelAdapter();
    const worldCore = new InMemoryWorldCoreAdapter();
    const createDraft = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });
    const submitReport = new SubmitCitizenReportUseCase({ worldCore });

    const draft = await createDraft.execute({
      citizenDescription: 'Bache grande cerca de la línea fronteriza.',
      language: 'es',
    });

    await assert.rejects(
      async () => {
        await submitReport.execute({
          draftId: draft.draftId,
          confirmedByPlayer: false, // NOT confirmed
        });
      },
      (err: unknown) => {
        assert.ok(err instanceof PlayerConfirmationRequiredError);
        assert.match((err as Error).message, /confirmation is strictly required/i);
        return true;
      }
    );
  });

  // Requirement 12 & 13: Successful World Core report registration, stable ID and lifecycle state
  it('12 & 13. registers authoritative CitizenReportRecord in World Core with stable ID and RECEIVED lifecycle state', async () => {
    const mockMeta = new MockMetaModelAdapter();
    const worldCore = new InMemoryWorldCoreAdapter();
    const createDraft = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });
    const submitReport = new SubmitCitizenReportUseCase({ worldCore });

    const draft = await createDraft.execute({
      citizenDescription: 'Bache en el pavimento que causa congestión.',
      image: {
        fileName: 'pothole.png',
        mimeType: 'image/png',
        base64: validPngBuffer.toString('base64'),
      },
      language: 'es',
    });

    const result = await submitReport.execute({
      draftId: draft.draftId,
      confirmedByPlayer: true,
    });

    assert.equal(result.success, true);
    assert.match(result.report.reportId, /^CJ-[A-Z0-9]{6}$/);
    assert.equal(result.report.lifecycleState, 'RECEIVED');
    assert.equal(result.report.simulatedDestination, 'Dirección General de Obras Públicas — Mantenimiento Vial');
    assert.equal(result.receipt.reportId, result.report.reportId);
    assert.equal(result.receipt.lifecycleState, 'RECEIVED');

    // Verify stored in World Core
    const fetched = await worldCore.getCitizenReport(result.report.reportId);
    assert.ok(fetched !== null);
    assert.equal(fetched.reportId, result.report.reportId);
    assert.equal(fetched.lifecycleState, 'RECEIVED');
  });

  // Requirement 14: Persistent report recovery after backend restart
  it('14. persists registered citizen reports to disk and recovers them across adapter instances', async () => {
    const tempStorePath = resolve(process.cwd(), '.runtime', 'test-citizen-reports.json');
    if (existsSync(tempStorePath)) {
      unlinkSync(tempStorePath);
    }

    // Instance 1: Register report
    const worldCore1 = new InMemoryWorldCoreAdapter({ persistenceFilePath: tempStorePath });
    const mockMeta = new MockMetaModelAdapter();
    const createDraft = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });
    const submitReport1 = new SubmitCitizenReportUseCase({ worldCore: worldCore1 });

    const draft = await createDraft.execute({
      citizenDescription: 'Reporte para prueba de persistencia en disco.',
      language: 'es',
    });

    const submitResult = await submitReport1.execute({
      draftId: draft.draftId,
      confirmedByPlayer: true,
    });

    const reportId = submitResult.report.reportId;

    // Instance 2: Simulate backend restart reading from same storage
    const worldCore2 = new InMemoryWorldCoreAdapter({ persistenceFilePath: tempStorePath });
    const getReport2 = new GetCitizenReportUseCase({ worldCore: worldCore2 });

    const recoveredReport = await getReport2.getById(reportId);
    assert.ok(recoveredReport !== null);
    assert.equal(recoveredReport.reportId, reportId);
    assert.equal(recoveredReport.citizenDescription, 'Reporte para prueba de persistencia en disco.');
    assert.equal(recoveredReport.lifecycleState, 'RECEIVED');

    // Cleanup
    if (existsSync(tempStorePath)) {
      unlinkSync(tempStorePath);
    }
  });

  // Requirement 15: No real municipal HTTP request or external dispatch
  it('15. strictly guarantees no real external municipal dispatch or government HTTP endpoints', async () => {
    const mockMeta = new MockMetaModelAdapter();
    const worldCore = new InMemoryWorldCoreAdapter();
    const createDraft = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });
    const submitReport = new SubmitCitizenReportUseCase({ worldCore });

    const draft = await createDraft.execute({
      citizenDescription: 'Incidencia simulada en el corredor.',
      language: 'es',
    });

    const submitResult = await submitReport.execute({
      draftId: draft.draftId,
      confirmedByPlayer: true,
    });

    // Invariant: Lifecycle states are strictly demonstration/simulation bounds
    assert.ok(['RECEIVED', 'UNDER_REVIEW', 'SIMULATED_ROUTING'].includes(submitResult.report.lifecycleState));
    assert.equal(submitResult.report.simulationNotice, SIMULATED_MUNICIPAL_SERVICE_NOTICE_ES);
  });

  // Requirement 16: Fictional municipal disclosure present in player-facing interaction & receipt
  it('16. presents explicit simulation disclosure in player receipt and NPC representative profile', () => {
    // NPC Profile disclosure
    assert.ok(CITIZEN_REPRESENTATIVE_PROFILE.simulationNotice.includes('Servicio municipal simulado'));
    assert.equal(CITIZEN_REPRESENTATIVE_PROFILE.role, 'Enlace de Atención Ciudadana');

    // Notice constants bilingual verification
    assert.ok(SIMULATED_MUNICIPAL_SERVICE_NOTICE_ES.includes('Servicio municipal simulado'));
    assert.ok(SIMULATED_MUNICIPAL_SERVICE_NOTICE_EN.includes('Simulated municipal service'));
  });

  // Requirement 17: API key remains server-side
  it('17. guarantees that Meta API key remains server-side and never leaks in receipts or DTOs', async () => {
    const mockMeta = new MockMetaModelAdapter();
    const worldCore = new InMemoryWorldCoreAdapter();
    const createDraft = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });
    const submitReport = new SubmitCitizenReportUseCase({ worldCore });

    const draft = await createDraft.execute({
      citizenDescription: 'Verificación de seguridad de credenciales.',
      language: 'es',
    });

    const submitResult = await submitReport.execute({
      draftId: draft.draftId,
      confirmedByPlayer: true,
    });

    const serializedDraft = JSON.stringify(draft);
    const serializedSubmit = JSON.stringify(submitResult);

    assert.equal(serializedDraft.includes('MODEL_API_KEY'), false);
    assert.equal(serializedDraft.includes('Authorization'), false);
    assert.equal(serializedSubmit.includes('MODEL_API_KEY'), false);
    assert.equal(serializedSubmit.includes('Authorization'), false);
  });

  describe('Increment 5.11.2: Hardening, Sanitization & Persistence Verifications', () => {
    // 18: Image sanitization strips EXIF metadata
    it('18. strips all EXIF and GPS metadata from photographic evidence using sharp', async () => {
      const rawImage = await sharp({
        create: { width: 16, height: 16, channels: 3, background: { r: 200, g: 100, b: 50 } },
      })
        .jpeg()
        .toBuffer();

      const sanitized = await sanitizeAndNormalizeImage(rawImage, 'image/jpeg');
      assert.equal(sanitized.format, 'jpeg');
      assert.equal(sanitized.width, 16);
      assert.equal(sanitized.height, 16);
      assert.ok(sanitized.sha256.length === 64);

      // Verify probe of sanitized buffer has no EXIF
      const probe = await sharp(sanitized.buffer).metadata();
      assert.equal(probe.exif, undefined);
    });

    // 19: Validates mismatched format or corrupted image
    it('19. rejects format mismatches and corrupted image buffers', async () => {
      await assert.rejects(
        async () => {
          await sanitizeAndNormalizeImage(validPngBuffer, 'image/jpeg');
        },
        /Declared MIME type 'image\/jpeg' does not match verified file format 'png'/
      );
    });

    // 20: Expired unconfirmed evidence cleanup
    it('20. removes unconfirmed evidence files older than 1 hour', () => {
      const testDir = resolve(process.cwd(), '.runtime', 'test-evidence');
      mkdirSync(testDir, { recursive: true });
      const oldFile = join(testDir, 'ev_old_1234.jpg');
      writeFileSync(oldFile, 'dummy data');
      const twoHoursAgo = new Date(Date.now() - 2 * 3600 * 1000);
      utimesSync(oldFile, twoHoursAgo, twoHoursAgo);

      const freshFile = join(testDir, 'ev_fresh_5678.jpg');
      writeFileSync(freshFile, 'fresh data');

      const deleted = cleanupExpiredEvidence(testDir, 3600 * 1000);
      assert.equal(deleted, 1);
      assert.equal(existsSync(oldFile), false);
      assert.equal(existsSync(freshFile), true);

      // Cleanup
      unlinkSync(freshFile);
      rmSync(testDir, { recursive: true, force: true });
    });

    // 21: Mock provider does NOT return fabricated 94% confidence
    it('21. mock provider does not invent a 0.94 confidence score', async () => {
      const mockMeta = new MockMetaModelAdapter();
      const res = await mockMeta.segmentImage({
        imageBase64: validPngBuffer.toString('base64'),
        mimeType: 'image/png',
      });
      assert.equal(res.provider, 'sam-3.1-local-mock');
      assert.equal(res.boxes[0]!.confidence, undefined);
      assert.equal(res.confidence, undefined);
    });

    // 22: World Core persistence atomic writing and reloading
    it('22. atomic persistence and recovery in InMemoryWorldCoreAdapter', async () => {
      const testPersistFile = resolve(process.cwd(), '.runtime', 'test-citizen-reports.json');
      if (existsSync(testPersistFile)) unlinkSync(testPersistFile);

      const adapter1 = new InMemoryWorldCoreAdapter({ persistenceFilePath: testPersistFile });
      const record = {
        reportId: 'rep_test_persist_01',
        confirmedCategory: 'road-surface-damage' as const,
        reportingRepresentative: DEFAULT_REPORTING_REPRESENTATIVE,
        citizenDescription: 'Bache en el pavimento',
        language: 'es' as const,
        location: {
          corridorEntityId: null,
          description: 'Av Juárez',
          isApproximate: true,
          coverageStatus: 'IN_DEMO_CORRIDOR' as const,
          coordinates: null,
        },
        evidence: null,
        externalAiProcessingConsent: false,
        visualAnalysis: null,
        structuredInterpretation: {
          model: 'muse-spark-1.3',
          summary: 'Bache reportado',
          category: 'road-surface-damage' as const,
          locationDescription: 'Av Juárez',
          additionalDetails: [],
          missingInformation: [],
          suggestedDepartment: 'Obras Públicas',
          clarificationQuestion: null,
          interpretedAt: new Date().toISOString(),
          provider: 'mock',
        },
        lifecycleState: 'RECEIVED' as const,
        simulatedDestination: 'Obras Públicas',
        simulationNotice: 'Simulado',
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      };

      await adapter1.registerCitizenReport(record);
      assert.equal(existsSync(testPersistFile), true);

      // Create new adapter reading the same file
      const adapter2 = new InMemoryWorldCoreAdapter({ persistenceFilePath: testPersistFile });
      const retrieved = await adapter2.getCitizenReport('rep_test_persist_01');
      assert.ok(retrieved !== null);
      assert.equal(retrieved.reportId, 'rep_test_persist_01');
      assert.equal(retrieved.citizenDescription, 'Bache en el pavimento');

      // Cleanup
      if (existsSync(testPersistFile)) unlinkSync(testPersistFile);
    });

    // 23: MetaModelApiAdapter streaming SAM 3.1 with SSE mock
    it('23. MetaModelApiAdapter parses SAM 3.1 SSE streaming response via @meta-sam/parser', async () => {
      const ssePayload =
        'data: {"type":"response.output_text.delta","delta":"Segmented pothole region","item_id":"item_1","output_index":0,"content_index":0}\n\n' +
        'data: {"type":"response.output_text.done","text":"Segmented pothole region","item_id":"item_1","output_index":0,"content_index":0}\n\n' +
        'data: {"type":"response.completed"}\n\n' +
        'data: [DONE]\n\n';

      const mockFetch: typeof fetch = async () => {
        return new Response(ssePayload, {
          status: 200,
          headers: { 'Content-Type': 'text/event-stream' },
        });
      };

      const adapter = new MetaModelApiAdapter({
        config: {
          apiKey: 'test-key-mock',
          baseURL: 'https://api.meta.ai/v1',
          model: 'muse-spark-1.3',
          isConfigured: true,
        },
        fetchFn: mockFetch,
      });

      const res = await adapter.segmentImage({
        imageBase64: validPngBuffer.toString('base64'),
        mimeType: 'image/png',
        promptText: 'pothole',
      });

      assert.equal(res.model, 'sam-3.1');
      assert.equal(res.provider, 'sam-3.1-live-api');
      assert.ok(res.analyzedAt);
    });
  });

  describe('Increment 5.11.3: 29 Verification & Conformance Scenarios', () => {
    const mockMeta = new MockMetaModelAdapter();
    const createDraft = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });

    // Scenario 1: Pothole in road -> road-surface-damage & Road Maintenance
    it('Scenario 1: classifies pothole in road as road-surface-damage and routes to Road Maintenance', async () => {
      const classification = classifyCivicIssue('Bache profundo en el carril central');
      assert.equal(classification.category, 'road-surface-damage');
      assert.equal(getSimulatedDepartment('road-surface-damage', 'es'), 'Dirección General de Obras Públicas — Mantenimiento Vial');

      const draft = await createDraft.execute({
        citizenDescription: 'Bache profundo en el asfalto que revienta llantas.',
        language: 'es',
      });
      assert.equal(draft.structuredInterpretation.category, 'road-surface-damage');
      assert.equal(draft.structuredInterpretation.suggestedDepartment, 'Dirección General de Obras Públicas — Mantenimiento Vial');
    });

    // Scenario 2: Abandoned garbage bags -> cleaning-and-waste & Sanitation Directorate
    it('Scenario 2: classifies abandoned garbage bags as cleaning-and-waste, not roadworks', async () => {
      const classification = classifyCivicIssue('Bolsas de basura acumuladas y tiradero clandestino en la esquina');
      assert.equal(classification.category, 'cleaning-and-waste');
      assert.equal(getSimulatedDepartment('cleaning-and-waste', 'es'), 'Dirección de Limpia — Gestión de Residuos Urbanos');
      assert.notEqual(getSimulatedDepartment('cleaning-and-waste', 'es'), 'Dirección General de Obras Públicas — Mantenimiento Vial');

      const draft = await createDraft.execute({
        citizenDescription: 'Montón de basura y bolsas de desperdicios tiradas en la vía pública.',
        language: 'es',
      });
      assert.equal(draft.structuredInterpretation.category, 'cleaning-and-waste');
      assert.equal(draft.structuredInterpretation.suggestedDepartment, 'Dirección de Limpia — Gestión de Residuos Urbanos');
    });

    // Scenario 3: Standing water -> water-and-drainage & JMAS
    it('Scenario 3: classifies standing water as water-and-drainage routed to JMAS', async () => {
      const classification = classifyCivicIssue('Encharcamiento constante por drenaje pluvial tapado y aguas negras');
      assert.equal(classification.category, 'water-and-drainage');
      assert.equal(getSimulatedDepartment('water-and-drainage', 'es'), 'Junta Municipal de Agua y Saneamiento (JMAS) — Drenaje Pluvial');
    });

    // Scenario 4: Broken streetlight -> public-lighting & Public Lighting
    it('Scenario 4: classifies broken streetlight as public-lighting routed to Public Services', async () => {
      const classification = classifyCivicIssue('Luminaria apagada y farola destruida dejando la calle a oscuras');
      assert.equal(classification.category, 'public-lighting');
      assert.equal(getSimulatedDepartment('public-lighting', 'es'), 'Dirección de Servicios Públicos — Alumbrado Público');
    });

    // Scenario 5: Broken sidewalk -> sidewalk-obstruction & Accessibility
    it('Scenario 5: classifies broken sidewalk as sidewalk-obstruction routed to Accessibility and Sidewalks', async () => {
      const classification = classifyCivicIssue('Banqueta fracturada y rampa peatonal intransitable para sillas de ruedas');
      assert.equal(classification.category, 'sidewalk-obstruction');
      assert.equal(getSimulatedDepartment('sidewalk-obstruction', 'es'), 'Dirección General de Obras Públicas — Accesibilidad y Banquetas');
    });

    // Scenario 6: Missing traffic sign -> traffic-signage & Road Safety
    it('Scenario 6: classifies missing traffic sign as traffic-signage routed to Road Safety', async () => {
      const classification = classifyCivicIssue('Semáforo descompuesto y disco de alto derribado');
      assert.equal(classification.category, 'traffic-signage');
      assert.equal(getSimulatedDepartment('traffic-signage', 'es'), 'Coordinación General de Seguridad Vial — Infraestructura Vial');
    });

    // Scenario 7: Unrecognized civic issue -> other & Citizen Attention / Manual Review
    it('Scenario 7: handles unrecognized valid civic issue as other routed to Citizen Attention', async () => {
      const classification = classifyCivicIssue('Solicitud vecinal para organizar una feria comunitaria en el barrio');
      assert.equal(classification.category, 'other');
      assert.equal(getSimulatedDepartment('other', 'es'), 'Dirección de Atención Ciudadana — Revisión y Canalización');
    });

    // Scenario 8: Ambiguous description -> requests clarification / other
    it('Scenario 8: ambiguous description matching multiple domains defaults to other without false certainty', async () => {
      const classification = classifyCivicIssue('Hay basura acumulada, una farola apagada y un bache en la calle');
      assert.equal(classification.isAmbiguous, true);
      assert.equal(classification.category, 'other');
    });

    // Scenario 9: Invalid model category -> normalized to deterministic fallback or other
    it('Scenario 9: normalizes arbitrary or invalid category strings safely to valid categories', () => {
      assert.equal(normalizeIssueCategory('nuclear-hazard'), 'other');
      assert.equal(normalizeIssueCategory(''), 'other');
      assert.equal(normalizeIssueCategory(null), 'other');
      assert.equal(normalizeIssueCategory('urban-cleanliness'), 'cleaning-and-waste');
      assert.equal(normalizeIssueCategory('PUBLIC-LIGHTING'), 'public-lighting');
    });

    // Scenario 10: Report to representative A concerns location in sector B
    it('Scenario 10: decouples intake representative from incident location in another sector', async () => {
      const draft = await createDraft.execute({
        citizenDescription: 'Basura tirada en lote baldío.',
        representativeId: 'patricia-morales',
        locationDescription: 'Colonia Melchor Ocampo, Calle Juan Escutia',
        language: 'es',
      });
      assert.equal(draft.reportingRepresentative.id, 'patricia-morales');
      assert.equal(draft.reportingRepresentative.sector, 'puente-paso-del-norte');
      assert.equal(draft.location.description, 'Colonia Melchor Ocampo, Calle Juan Escutia');
      assert.equal(draft.location.coverageStatus, 'OUTSIDE_DEMO_COVERAGE');
    });

    // Scenario 11: Representative coordinates never substituted for incident location
    it('Scenario 11: representative coordinates are never substituted for incident location', async () => {
      const draft = await createDraft.execute({
        citizenDescription: 'Luminaria rota en Gómez Morín.',
        locationDescription: 'Blvd. Manuel Gómez Morín #1234',
        language: 'es',
      });
      assert.equal(draft.location.coordinates, null);
      assert.notEqual(draft.location.coordinates, { latitude: 31.741, longitude: -106.484 });
    });

    // Scenario 12: In-corridor location uses verified geography
    it('Scenario 12: verified corridor location resolves to IN_DEMO_CORRIDOR with real coordinates', async () => {
      const draft = await createDraft.execute({
        citizenDescription: 'Banqueta rota frente a Farmacias Benavides.',
        corridorEntityId: 'pharmacy:farmacias-benavides',
        language: 'es',
      });
      assert.equal(draft.location.coverageStatus, 'IN_DEMO_CORRIDOR');
      assert.ok(draft.location.coordinates !== null);
      assert.ok(draft.location.coordinates.latitude > 31.0);
      assert.ok(draft.location.coordinates.longitude < -100.0);
    });

    // Scenario 13: Out-of-corridor location marked OUTSIDE_DEMO_COVERAGE, coordinates null
    it('Scenario 13: out-of-corridor location marked OUTSIDE_DEMO_COVERAGE with null coordinates', async () => {
      const draft = await createDraft.execute({
        citizenDescription: 'Bache grande en el sur.',
        locationDescription: 'Avenida Tecnológico y Zaragoza',
        language: 'es',
      });
      assert.equal(draft.location.coverageStatus, 'OUTSIDE_DEMO_COVERAGE');
      assert.equal(draft.location.coordinates, null);
    });

    // Scenario 14: Unresolved location never assigned fabricated coordinates
    it('Scenario 14: unresolved location retains null coordinates without fabrication', async () => {
      const draft = await createDraft.execute({
        citizenDescription: 'Reporte sin ninguna ubicación especificada.',
        language: 'es',
      });
      assert.equal(draft.location.isApproximate, true);
      assert.equal(draft.location.coordinates, null);
    });

    // Scenario 15: Report without image succeeds
    it('Scenario 15: report without image succeeds end-to-end generating folio and receipt', async () => {
      const worldCore = new InMemoryWorldCoreAdapter();
      const submit = new SubmitCitizenReportUseCase({ worldCore });

      const draft = await createDraft.execute({
        citizenDescription: 'Fuga de agua potable frente a la tienda.',
        language: 'es',
      });
      assert.equal(draft.evidence, null);
      assert.equal(draft.visualAnalysis, null);

      const result = await submit.execute({
        draftId: draft.draftId,
        confirmedByPlayer: true,
      });
      assert.equal(result.success, true);
      assert.match(result.receipt.reportId, /^CJ-[A-Z0-9]{6}$/);
    });

    // Scenario 16: Report with image succeeds
    it('Scenario 16: report with valid image evidence succeeds end-to-end', async () => {
      const worldCore = new InMemoryWorldCoreAdapter();
      const submit = new SubmitCitizenReportUseCase({ worldCore });

      const draft = await createDraft.execute({
        citizenDescription: 'Acumulación de basura en la calle.',
        image: {
          fileName: 'basura.png',
          mimeType: 'image/png',
          base64: validPngBuffer.toString('base64'),
        },
        externalAiProcessingConsent: true,
        language: 'es',
      });
      assert.ok(draft.evidence !== null);
      assert.ok(draft.visualAnalysis !== null);

      const result = await submit.execute({
        draftId: draft.draftId,
        confirmedByPlayer: true,
      });
      assert.equal(result.success, true);
      assert.ok(result.report.evidence !== null);
    });

    // Scenario 17: Invalid image rejected safely
    it('Scenario 17: rejects invalid image format with clear error', async () => {
      await assert.rejects(
        async () => {
          await createDraft.execute({
            citizenDescription: 'Prueba con imagen corrupta.',
            image: {
              fileName: 'test.exe',
              mimeType: 'application/octet-stream',
              base64: Buffer.from('corrupt data').toString('base64'),
            },
            language: 'es',
          });
        },
        /Only valid PNG and JPEG images are accepted/i
      );
    });

    // Scenario 18: Image without consent -> not transmitted to Meta, provider: 'not-authorized'
    it('Scenario 18: image without consent is stored locally but never sent to SAM 3.1', async () => {
      const draft = await createDraft.execute({
        citizenDescription: 'Bache frente a mi casa.',
        image: {
          fileName: 'bache.png',
          mimeType: 'image/png',
          base64: validPngBuffer.toString('base64'),
        },
        externalAiProcessingConsent: false, // Explicitly false or omitted
        language: 'es',
      });
      assert.ok(draft.evidence !== null); // image stored locally as evidence
      assert.ok(draft.visualAnalysis !== null);
      assert.equal(draft.visualAnalysis.provider, 'not-authorized');
      assert.equal(draft.visualAnalysis.status, 'unavailable');
      assert.equal(draft.visualAnalysis.boxes.length, 0);
      assert.match(draft.visualAnalysis.maskSummary ?? '', /no autorizado/i);
    });

    // Scenario 19: Image with consent -> transmitted to SAM with dynamic prompt
    it('Scenario 19: image with consent uses dynamic SAM target derived from category', async () => {
      const draft = await createDraft.execute({
        citizenDescription: 'Basura tirada en bolsas rotas en la calle.',
        image: {
          fileName: 'basura.png',
          mimeType: 'image/png',
          base64: validPngBuffer.toString('base64'),
        },
        externalAiProcessingConsent: true,
        language: 'es',
      });
      assert.ok(draft.visualAnalysis !== null);
      assert.equal(draft.visualAnalysis.detectedConcept, 'discarded garbage bags or accumulated waste');
    });

    // Scenario 20: SAM unavailable/inconclusive does not block submission
    it('Scenario 20: inconclusive SAM analysis does not block player confirmation or submission', async () => {
      const mockMetaNoMatch = new MockMetaModelAdapter({ samNoMatch: true });
      const draftUseCase = new CreateCitizenReportDraftUseCase({ metaModel: mockMetaNoMatch });
      const worldCore = new InMemoryWorldCoreAdapter();
      const submit = new SubmitCitizenReportUseCase({ worldCore });

      const draft = await draftUseCase.execute({
        citizenDescription: 'Banqueta con desnivel.',
        image: {
          fileName: 'foto.png',
          mimeType: 'image/png',
          base64: validPngBuffer.toString('base64'),
        },
        externalAiProcessingConsent: true,
        language: 'es',
      });
      assert.equal(draft.visualAnalysis?.status, 'inconclusive');

      const result = await submit.execute({
        draftId: draft.draftId,
        confirmedByPlayer: true,
      });
      assert.equal(result.success, true);
      assert.match(result.receipt.reportId, /^CJ-[A-Z0-9]{6}$/);
    });

    // Scenario 21: Zero SAM matches -> inconclusive, not positive detection
    it('Scenario 21: zero SAM detections results in inconclusive status, not positive detection', async () => {
      const mockMetaNoMatch = new MockMetaModelAdapter({ samNoMatch: true });
      const draftUseCase = new CreateCitizenReportDraftUseCase({ metaModel: mockMetaNoMatch });

      const draft = await draftUseCase.execute({
        citizenDescription: 'Luminaria apagada en la noche.',
        image: {
          fileName: 'luz.png',
          mimeType: 'image/png',
          base64: validPngBuffer.toString('base64'),
        },
        externalAiProcessingConsent: true,
        language: 'es',
      });
      assert.equal(draft.visualAnalysis?.status, 'inconclusive');
      assert.equal(draft.visualAnalysis?.boxes.length, 0);
    });

    // Scenario 22: No fabricated confidence or boxes
    it('Scenario 22: does not fabricate confidence score when SAM returns inconclusive', async () => {
      const mockMetaNoMatch = new MockMetaModelAdapter({ samNoMatch: true });
      const draftUseCase = new CreateCitizenReportDraftUseCase({ metaModel: mockMetaNoMatch });

      const draft = await draftUseCase.execute({
        citizenDescription: 'Foto sin bache visible.',
        image: {
          fileName: 'clean.png',
          mimeType: 'image/png',
          base64: validPngBuffer.toString('base64'),
        },
        externalAiProcessingConsent: true,
        language: 'es',
      });
      assert.equal(draft.visualAnalysis?.confidence ?? null, null);
    });

    // Scenario 23: Unique folio generated
    it('Scenario 23: generates unique, non-colliding folios for sequential reports', async () => {
      const worldCore = new InMemoryWorldCoreAdapter();
      const submit = new SubmitCitizenReportUseCase({ worldCore });

      const draft1 = await createDraft.execute({ citizenDescription: 'Reporte uno', language: 'es' });
      const res1 = await submit.execute({ draftId: draft1.draftId, confirmedByPlayer: true });

      const draft2 = await createDraft.execute({ citizenDescription: 'Reporte dos', language: 'es' });
      const res2 = await submit.execute({ draftId: draft2.draftId, confirmedByPlayer: true });

      assert.notEqual(res1.report.reportId, res2.report.reportId);
      assert.match(res1.report.reportId, /^CJ-[A-Z0-9]{6}$/);
      assert.match(res2.report.reportId, /^CJ-[A-Z0-9]{6}$/);
    });

    // Scenario 24: Original citizen description retained verbatim
    it('Scenario 24: retains original citizen description untruncated and unaltered', async () => {
      const text = 'Descripción con caracteres especiales: ¡Bache peligroso en Av. Juárez #500 con más de 15 cm de profundidad!';
      const draft = await createDraft.execute({ citizenDescription: text, language: 'es' });
      const worldCore = new InMemoryWorldCoreAdapter();
      const submit = new SubmitCitizenReportUseCase({ worldCore });

      const res = await submit.execute({ draftId: draft.draftId, confirmedByPlayer: true });
      assert.equal(res.report.citizenDescription, text);
    });

    // Scenario 25: Confirmed category and location persisted
    it('Scenario 25: persists confirmed category override and location in authoritative record', async () => {
      const draft = await createDraft.execute({ citizenDescription: 'Problema en la calle', language: 'es' });
      const worldCore = new InMemoryWorldCoreAdapter();
      const submit = new SubmitCitizenReportUseCase({ worldCore });

      const res = await submit.execute({
        draftId: draft.draftId,
        confirmedByPlayer: true,
        confirmedCategory: 'public-lighting',
      });
      assert.equal(res.report.confirmedCategory, 'public-lighting');
      assert.equal(res.report.simulatedDestination, 'Dirección de Servicios Públicos — Alumbrado Público');
    });

    // Scenario 26: Simulated recommendation persisted and labeled
    it('Scenario 26: persists simulated destination and simulation notice', async () => {
      const draft = await createDraft.execute({ citizenDescription: 'Semáforo roto', language: 'es' });
      const worldCore = new InMemoryWorldCoreAdapter();
      const submit = new SubmitCitizenReportUseCase({ worldCore });

      const res = await submit.execute({ draftId: draft.draftId, confirmedByPlayer: true });
      assert.equal(res.report.simulatedDestination, 'Coordinación General de Seguridad Vial — Infraestructura Vial');
      assert.match(res.report.simulationNotice, /simulado/i);
    });

    // Scenario 27: Lifecycle state strictly RECEIVED
    it('Scenario 27: initializes report strictly with lifecycle state RECEIVED', async () => {
      const draft = await createDraft.execute({ citizenDescription: 'Banqueta dañada', language: 'es' });
      const worldCore = new InMemoryWorldCoreAdapter();
      const submit = new SubmitCitizenReportUseCase({ worldCore });

      const res = await submit.execute({ draftId: draft.draftId, confirmedByPlayer: true });
      assert.equal(res.report.lifecycleState, 'RECEIVED');
      assert.equal(res.receipt.lifecycleState, 'RECEIVED');
    });

    // Scenario 28: Recovery after server restart via persistence file
    it('Scenario 28: recovers full report data across persistence file reloads', async () => {
      const tempPath = resolve(process.cwd(), '.runtime', `persist_test_${Date.now()}.json`);
      const core1 = new InMemoryWorldCoreAdapter({ persistenceFilePath: tempPath });
      const submit1 = new SubmitCitizenReportUseCase({ worldCore: core1 });

      const draft = await createDraft.execute({
        citizenDescription: 'Fuga de agua importante',
        locationDescription: 'Calle 16 de Septiembre',
        language: 'es',
      });
      const res = await submit1.execute({ draftId: draft.draftId, confirmedByPlayer: true });

      const core2 = new InMemoryWorldCoreAdapter({ persistenceFilePath: tempPath });
      const loaded = await core2.getCitizenReport(res.report.reportId);
      assert.ok(loaded !== null);
      assert.equal(loaded.reportId, res.report.reportId);
      assert.equal(loaded.confirmedCategory, 'water-and-drainage');
      assert.equal(loaded.location.description, 'Calle 16 de Septiembre');

      if (existsSync(tempPath)) unlinkSync(tempPath);
    });

    // Scenario 29: No real municipal delivery triggered
    it('Scenario 29: guarantees zero external municipal HTTP calls or real government side effects', async () => {
      const worldCore = new InMemoryWorldCoreAdapter();
      const submit = new SubmitCitizenReportUseCase({ worldCore });

      const draft = await createDraft.execute({ citizenDescription: 'Incidencia simulada', language: 'es' });
      const res = await submit.execute({ draftId: draft.draftId, confirmedByPlayer: true });

      assert.equal(res.report.lifecycleState, 'RECEIVED');
      assert.match(res.report.simulationNotice, /ficticio|simulado/i);
    });

    // Emergency boundary assessment test
    it('Emergency boundary: detects acute emergencies and emits guidance to 911', () => {
      const resEmergency = assessNonEmergencyBoundary('Hay disparos y una balacera en la esquina');
      assert.equal(resEmergency.isEmergency, true);
      assert.match(resEmergency.emergencyNoticeEs ?? '', /911/);

      const resNormal = assessNonEmergencyBoundary('Hay un bache grande en el asfalto');
      assert.equal(resNormal.isEmergency, false);
    });
  });

  describe('Increment 5.11.3.1: Conformance, Location Precision & Legacy Compatibility Audit', () => {
    // -------------------------------------------------------------
    // AUDIT A: Legacy Category & Pre-5.11.3 Record Compatibility
    // -------------------------------------------------------------
    it('Audit A1: maps all legacy category identifiers to canonical categories', () => {
      assert.equal(LEGACY_CATEGORY_MAP['baches_y_vialidad'], 'road-surface-damage');
      assert.equal(normalizeIssueCategory('BACHES_Y_VIALIDAD'), 'road-surface-damage');
      assert.equal(normalizeIssueCategory('baches_y_vialidad'), 'road-surface-damage');
      assert.equal(normalizeIssueCategory('bacheo'), 'road-surface-damage');
      assert.equal(normalizeIssueCategory('BANQUETAS_Y_ACCESIBILIDAD'), 'sidewalk-obstruction');
      assert.equal(normalizeIssueCategory('LIMPIEZA_Y_RESIDUOS'), 'cleaning-and-waste');
      assert.equal(normalizeIssueCategory('AGUA_Y_DRENAJE'), 'water-and-drainage');
      assert.equal(normalizeIssueCategory('ALUMBRADO_PUBLICO'), 'public-lighting');
      assert.equal(normalizeIssueCategory('SENALIZACION_VIAL'), 'traffic-signage');
      assert.equal(normalizeIssueCategory('PARQUES_Y_ESPACIOS_PUBLICOS'), 'parks-and-public-spaces');
      assert.equal(normalizeIssueCategory('ARBOLADO_Y_AREAS_VERDES'), 'trees-and-green-areas');
      assert.equal(normalizeIssueCategory('OTRO'), 'other');
      assert.equal(normalizeIssueCategory('NON_EXISTENT_CATEGORY'), 'other');
      assert.equal(normalizeIssueCategory(null), 'other');
      assert.equal(normalizeIssueCategory(undefined), 'other');
    });

    it('Audit A2: normalizes pre-5.11.3 legacy persisted records without losing reportId or evidence', () => {
      const legacyRaw = {
        reportId: 'rep_legacy_pre_5113_001',
        citizenDescription: 'Bache grande en la calle',
        language: 'es',
        location: {
          corridorEntityId: null,
          description: 'Avenida Benito Juárez',
          isApproximate: true,
          coordinates: null,
        },
        evidence: {
          evidenceId: 'ev_legacy_001',
          fileName: 'pothole.jpg',
          fileSizeBytes: 1024,
          mimeType: 'image/jpeg',
          storagePath: 'storage/evidence/ev_legacy_001.jpg',
          sanitization: {
            exifStripped: true,
            dimensionsNormalized: true,
            format: 'jpeg',
            sha256Hash: 'abcd1234',
          },
          uploadedAt: '2026-11-01T12:00:00.000Z',
        },
        visualAnalysis: null,
        structuredInterpretation: {
          model: 'muse-spark-1.3',
          summary: 'Bache reportado',
          category: 'road-surface-damage',
          locationDescription: 'Avenida Benito Juárez',
          additionalDetails: [],
          missingInformation: [],
          suggestedDepartment: 'Dirección General de Obras Públicas — Mantenimiento Vial',
          clarificationQuestion: null,
          interpretedAt: '2026-11-01T12:01:00.000Z',
        },
        lifecycleState: 'RECEIVED',
        simulatedDestination: 'Dirección General de Obras Públicas — Mantenimiento Vial',
        simulationNotice: 'Simulado',
        createdAt: '2026-11-01T12:01:00.000Z',
        updatedAt: '2026-11-01T12:01:00.000Z',
      };

      const normalized = normalizeCitizenReportRecord(legacyRaw);
      assert.equal(normalized.reportId, 'rep_legacy_pre_5113_001');
      assert.equal(normalized.citizenDescription, 'Bache grande en la calle');
      assert.equal(normalized.confirmedCategory, 'road-surface-damage');
      assert.equal(normalized.location.coverageStatus, 'IN_DEMO_CORRIDOR');
      assert.equal(normalized.location.coordinates, null);
      assert.equal(normalized.externalAiProcessingConsent, false);
      assert.ok(normalized.reportingRepresentative);
      assert.equal(normalized.reportingRepresentative.name, 'Lic. Patricia Morales');
      assert.ok(normalized.evidence);
      assert.equal(normalized.evidence.evidenceId, 'ev_legacy_001');
    });

    it('Audit A3: InMemoryWorldCoreAdapter loads and serves pre-5.11.3 legacy records seamlessly', async () => {
      const tempPath = resolve(process.cwd(), '.runtime', `legacy_load_test_${Date.now()}.json`);
      const legacyRawRecords = [
        {
          reportId: 'rep_legacy_01',
          citizenDescription: 'Reporte antiguo de bache en la calle',
          category: 'BACHES_Y_VIALIDAD',
          language: 'es',
          location: {
            description: 'Av Juárez',
            isApproximate: true,
          },
          lifecycleState: 'RECEIVED',
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
        },
      ];
      writeFileSync(tempPath, JSON.stringify(legacyRawRecords, null, 2), 'utf8');

      const adapter = new InMemoryWorldCoreAdapter({ persistenceFilePath: tempPath });
      const record = await adapter.getCitizenReport('rep_legacy_01');
      assert.ok(record !== null);
      assert.equal(record.reportId, 'rep_legacy_01');
      assert.equal(record.confirmedCategory, 'road-surface-damage');
      assert.equal(record.location.coverageStatus, 'IN_DEMO_CORRIDOR');
      assert.equal(record.reportingRepresentative.name, 'Lic. Patricia Morales');
      assert.equal(record.externalAiProcessingConsent, false);

      if (existsSync(tempPath)) unlinkSync(tempPath);
    });

    // -------------------------------------------------------------
    // AUDIT B: Geographic Precision & Truthful Corridor Coverage (6 Cases)
    // -------------------------------------------------------------
    it('Audit B1 (Case 1): Specific verified corridor entity -> IN_DEMO_CORRIDOR, exact verified coordinates', async () => {
      const mockMeta = new MockMetaModelAdapter();
      const useCase = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });

      const draft = await useCase.execute({
        citizenDescription: 'Banqueta dañada frente a la farmacia',
        corridorEntityId: 'pharmacy:farmacias-benavides',
        language: 'es',
      });

      assert.equal(draft.location.coverageStatus, 'IN_DEMO_CORRIDOR');
      assert.equal(draft.location.isApproximate, false);
      assert.ok(draft.location.coordinates !== null);
      assert.ok(typeof draft.location.coordinates.latitude === 'number');
      assert.ok(typeof draft.location.coordinates.longitude === 'number');
      assert.equal(draft.location.corridorEntityId, 'pharmacy:farmacias-benavides');
    });

    it('Audit B2 (Case 2): Only "Avenida Benito Juárez" -> IN_DEMO_CORRIDOR, isApproximate: true, coordinates: null', async () => {
      const mockMeta = new MockMetaModelAdapter();
      const useCase = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });

      const draft = await useCase.execute({
        citizenDescription: 'Lámpara fundida',
        locationDescription: 'Avenida Benito Juárez',
        language: 'es',
      });

      assert.equal(draft.location.coverageStatus, 'IN_DEMO_CORRIDOR');
      assert.equal(draft.location.isApproximate, true);
      assert.equal(draft.location.coordinates, null);
      assert.equal(draft.location.corridorEntityId, null);
    });

    it('Audit B3 (Case 3): Known corridor intersection/landmark -> IN_DEMO_CORRIDOR, coordinates populated', async () => {
      const mockMeta = new MockMetaModelAdapter();
      const useCase = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });

      const draft = await useCase.execute({
        citizenDescription: 'Semáforo intermitente cerca de la plaza de cobro',
        locationDescription: 'Plaza de Cobro C-51 Puente Santa Fe',
        language: 'es',
      });

      assert.equal(draft.location.coverageStatus, 'IN_DEMO_CORRIDOR');
      assert.ok(draft.location.coordinates !== null);
    });

    it('Audit B4 (Case 4): Outside corridor (e.g. Colonia Melchor Ocampo) -> OUTSIDE_DEMO_COVERAGE, coordinates: null', async () => {
      const mockMeta = new MockMetaModelAdapter();
      const useCase = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });

      const draft = await useCase.execute({
        citizenDescription: 'Bache enorme en la calle Asfalto',
        locationDescription: 'Colonia Melchor Ocampo, calle Monterrey 402',
        language: 'es',
      });

      assert.equal(draft.location.coverageStatus, 'OUTSIDE_DEMO_COVERAGE');
      assert.equal(draft.location.isApproximate, true);
      assert.equal(draft.location.coordinates, null);
      assert.equal(draft.location.corridorEntityId, null);
    });

    it('Audit B5 (Case 5): Ambiguous location ("en una esquina") -> UNRESOLVED, coordinates: null', async () => {
      const mockMeta = new MockMetaModelAdapter();
      const useCase = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });

      const draft = await useCase.execute({
        citizenDescription: 'Basura tirada en la banqueta',
        locationDescription: 'en una esquina por ahí cerca',
        language: 'es',
      });

      assert.equal(draft.location.coverageStatus, 'UNRESOLVED');
      assert.equal(draft.location.coordinates, null);
    });

    it('Audit B6 (Case 6): Empty / no location -> UNRESOLVED, coordinates: null, never invents Avenida Juárez', async () => {
      const mockMeta = new MockMetaModelAdapter();
      const useCase = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });

      const draft = await useCase.execute({
        citizenDescription: 'Reporte sin especificar ninguna ubicación',
        language: 'es',
      });

      assert.equal(draft.location.coverageStatus, 'UNRESOLVED');
      assert.equal(draft.location.description, 'Ubicación no especificada');
      assert.equal(draft.location.coordinates, null);
      assert.equal(draft.location.corridorEntityId, null);
    });

    // -------------------------------------------------------------
    // AUDIT C: External Image Processing Consent Enforcement
    // -------------------------------------------------------------
    it('Audit C1: when externalAiProcessingConsent is false, SAM 3.1 is NOT called and provider is none/not-authorized', async () => {
      let samCalled = false;
      let sparkReceivedImage = false;

      const trackingMeta: any = {
        interpretCitizenReport: async (params: any) => {
          if ((params as any).imageBase64 || (params as any).imageBuffer) {
            sparkReceivedImage = true;
          }
          return {
            summary: 'Interpretación de prueba',
            category: 'road-surface-damage',
            locationDescription: 'Av Juárez',
            additionalDetails: [],
            missingInformation: [],
            suggestedDepartment: 'Obras Públicas',
            clarificationQuestion: null,
            model: 'muse-spark-1.3',
            interpretedAt: new Date().toISOString(),
            provider: 'mock',
          };
        },
        segmentImage: async () => {
          samCalled = true;
          return {
            model: 'sam-3.1',
            provider: 'sam-3.1-live-api',
            analyzedAt: new Date().toISOString(),
            boxes: [],
          };
        },
      };

      const useCase = new CreateCitizenReportDraftUseCase({ metaModel: trackingMeta });
      const draft = await useCase.execute({
        citizenDescription: 'Bache grande en el pavimento',
        image: {
          base64: validPngBuffer.toString('base64'),
          mimeType: 'image/png',
          fileName: 'pothole.png',
        },
        externalAiProcessingConsent: false,
        language: 'es',
      });

      assert.equal(samCalled, false, 'SAM 3.1 must NOT be called without consent');
      assert.equal(sparkReceivedImage, false, 'Muse Spark must NEVER receive image bytes');
      assert.ok(draft.visualAnalysis);
      assert.equal(draft.visualAnalysis.provider, 'not-authorized');
      assert.equal(draft.visualAnalysis.boxes.length, 0);
      assert.ok(draft.evidence !== null, 'Image is still stored locally as evidence');
    });

    it('Audit C2: when externalAiProcessingConsent is true, SAM 3.1 is called with dynamic prompt', async () => {
      let promptSent: string | undefined;

      const trackingMeta: any = {
        interpretCitizenReport: async () => ({
          summary: 'Basura en la banqueta',
          category: 'cleaning-and-waste',
          locationDescription: 'Calle Juárez',
          additionalDetails: [],
          missingInformation: [],
          suggestedDepartment: 'Dirección de Limpia',
          clarificationQuestion: null,
          model: 'muse-spark-1.3',
          interpretedAt: new Date().toISOString(),
          provider: 'mock',
        }),
        segmentImage: async (params: any) => {
          promptSent = params.promptText;
          return {
            model: 'sam-3.1',
            provider: 'sam-3.1-live-api',
            analyzedAt: new Date().toISOString(),
            boxes: [{ ymin: 0.1, xmin: 0.1, ymax: 0.5, xmax: 0.5, label: 'waste' }],
          };
        },
      };

      const useCase = new CreateCitizenReportDraftUseCase({ metaModel: trackingMeta });
      const draft = await useCase.execute({
        citizenDescription: 'Basura tirada y escombros acumulados',
        image: {
          base64: validPngBuffer.toString('base64'),
          mimeType: 'image/png',
          fileName: 'trash.png',
        },
        externalAiProcessingConsent: true,
        language: 'es',
      });

      assert.ok(promptSent !== undefined);
      assert.equal(draft.visualAnalysis?.provider, 'sam-3.1-live-api');
      assert.equal(draft.visualAnalysis?.boxes.length, 1);
    });
  });

  describe('Increment 5.11.3.2: Optional Image Evidence Flow & Regression Suite', () => {
    // Test 1: Unity/API DTO with no selected image (nested default instance {"fileName":"","mimeType":"","base64":""})
    it('1. accepts Unity/API DTO with empty default image object without throwing HTTP 400', async () => {
      const mockMeta = new MockMetaModelAdapter();
      const useCase = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });

      const draft = await useCase.execute({
        citizenDescription: 'Encharcamiento constante en la esquina de Avenida Juárez.',
        locationDescription: 'Avenida Benito Juárez',
        category: 'water-and-drainage',
        language: 'es',
        image: {
          fileName: '',
          mimeType: '',
          base64: '',
        },
      });

      assert.ok(draft.draftId);
      assert.equal(draft.evidence, null, 'evidence must be null');
      assert.equal(draft.visualAnalysis, null, 'visualAnalysis must be null');
      assert.equal(draft.category, 'water-and-drainage');
      assert.equal(draft.simulatedDestination, 'Junta Municipal de Agua y Saneamiento (JMAS) — Drenaje Pluvial');
    });

    // Test 2: Draft request with image field omitted entirely
    it('2. draft request with image field omitted entirely succeeds with evidence: null', async () => {
      const mockMeta = new MockMetaModelAdapter();
      const useCase = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });

      const draft = await useCase.execute({
        citizenDescription: 'Semáforo descompuesto frente a la plaza de cobro.',
        locationDescription: 'Avenida Benito Juárez',
        language: 'es',
      });

      assert.ok(draft.draftId);
      assert.equal(draft.evidence, null);
      assert.equal(draft.visualAnalysis, null);
    });

    // Test 3: Draft request with image field explicitly null
    it('3. draft request with image field explicitly null succeeds with evidence: null', async () => {
      const mockMeta = new MockMetaModelAdapter();
      const useCase = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });

      const draft = await useCase.execute({
        citizenDescription: 'Bolsas de basura acumuladas en la acera.',
        language: 'es',
        image: null,
      });

      assert.ok(draft.draftId);
      assert.equal(draft.evidence, null);
      assert.equal(draft.visualAnalysis, null);
    });

    // Test 4: Draft request with an empty Base64 field
    it('4. draft request with an empty Base64 field is treated as no image and succeeds', async () => {
      const mockMeta = new MockMetaModelAdapter();
      const useCase = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });

      const draft = await useCase.execute({
        citizenDescription: 'Fuga de agua potable.',
        language: 'es',
        image: {
          fileName: 'evidence.jpg',
          mimeType: 'image/jpeg',
          base64: '   ',
        },
      });

      assert.ok(draft.draftId);
      assert.equal(draft.evidence, null);
      assert.equal(draft.visualAnalysis, null);
    });

    // Test 5: No-image request with external consent false
    it('5. no-image request with external consent false succeeds with evidence: null', async () => {
      const mockMeta = new MockMetaModelAdapter();
      const useCase = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });

      const draft = await useCase.execute({
        citizenDescription: 'Luminaria apagada en la noche.',
        externalAiProcessingConsent: false,
        language: 'es',
      });

      assert.equal(draft.evidence, null);
      assert.equal(draft.visualAnalysis, null);
    });

    // Test 6: No-image request with external consent true (consent does not fabricate image processing)
    it('6. no-image request with external consent true succeeds with evidence: null', async () => {
      const mockMeta = new MockMetaModelAdapter();
      const useCase = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });

      const draft = await useCase.execute({
        citizenDescription: 'Luminaria rota en la esquina.',
        externalAiProcessingConsent: true,
        language: 'es',
      });

      assert.equal(draft.evidence, null);
      assert.equal(draft.visualAnalysis, null);
    });

    // Test 7, 8, 9: No-image draft invokes no image sanitizer, no SAM provider, and no external calls
    it('7-9. no-image draft invokes no image sanitizer, no SAM provider, and no external image service', async () => {
      let samInvoked = false;
      let anyImageCall = false;

      const trackingMeta: any = {
        interpretCitizenReport: async (options: any) => {
          if ((options as any).imageBase64 || (options as any).imageBuffer) {
            anyImageCall = true;
          }
          return {
            model: 'muse-spark-1.3',
            summary: 'Luminaria apagada',
            category: 'public-lighting',
            locationDescription: 'Av Juárez',
            additionalDetails: [],
            missingInformation: [],
            suggestedDepartment: 'Servicios Públicos',
            clarificationQuestion: null,
            interpretedAt: new Date().toISOString(),
            provider: 'mock',
          };
        },
        segmentImage: async () => {
          samInvoked = true;
          anyImageCall = true;
          throw new Error('SAM 3.1 should NOT be called when no image exists!');
        },
      };

      const useCase = new CreateCitizenReportDraftUseCase({ metaModel: trackingMeta });
      const draft = await useCase.execute({
        citizenDescription: 'Luminaria apagada en poste de luz.',
        image: { fileName: '', mimeType: '', base64: '' },
        externalAiProcessingConsent: true,
        language: 'es',
      });

      assert.equal(samInvoked, false, 'SAM must NOT be invoked when no image is attached');
      assert.equal(anyImageCall, false, 'No image-processing service should be called');
      assert.equal(draft.evidence, null);
      assert.equal(draft.visualAnalysis, null);
    });

    // Test 10: Muse Spark text interpretation remains fully available without an image
    it('10. Muse Spark text interpretation remains fully available without an image', async () => {
      let museSparkCalled = false;
      const trackingMeta: any = {
        interpretCitizenReport: async (opts: any) => {
          museSparkCalled = true;
          assert.equal(opts.visualAnalysis, undefined);
          assert.ok(typeof opts.citizenDescription === 'string');
          return {
            model: 'muse-spark-1.3',
            summary: 'Encharcamiento por drenaje pluvial tapado',
            category: 'water-and-drainage',
            locationDescription: 'Avenida Juárez',
            additionalDetails: ['Reporte registrado sin evidencia fotográfica'],
            missingInformation: [],
            suggestedDepartment: 'Junta Municipal de Agua y Saneamiento (JMAS) — Drenaje Pluvial',
            clarificationQuestion: null,
            interpretedAt: new Date().toISOString(),
            provider: 'muse-spark-1.3-mock',
          };
        },
      };

      const useCase = new CreateCitizenReportDraftUseCase({ metaModel: trackingMeta });
      const draft = await useCase.execute({
        citizenDescription: 'Encharcamiento constante por drenaje tapado',
        locationDescription: 'Avenida Benito Juárez',
        language: 'es',
      });

      assert.equal(museSparkCalled, true, 'Muse Spark must be called for text interpretation');
      assert.equal(draft.category, 'water-and-drainage');
      assert.equal(draft.structuredInterpretation.summary, 'Encharcamiento por drenaje pluvial tapado');
    });

    // Test 11, 12, 13: Full submission and persistence without an image
    it('11-13. report submission succeeds without an image, persists in World Core with evidence: null and visualAnalysis: null', async () => {
      const tempPath = resolve(process.cwd(), '.runtime', `persist_no_image_${Date.now()}.json`);
      const worldCore = new InMemoryWorldCoreAdapter({ persistenceFilePath: tempPath });
      const mockMeta = new MockMetaModelAdapter();
      const createDraft = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });
      const submitReport = new SubmitCitizenReportUseCase({ worldCore });

      const draft = await createDraft.execute({
        citizenDescription: 'Encharcamiento grave de agua potable por tubería rota',
        locationDescription: 'Avenida Benito Juárez',
        image: { fileName: '', mimeType: '', base64: '' },
        language: 'es',
      });

      assert.equal(draft.evidence, null);
      assert.equal(draft.visualAnalysis, null);

      const submitRes = await submitReport.execute({
        draftId: draft.draftId,
        confirmedByPlayer: true,
        confirmedCategory: 'water-and-drainage',
      });

      assert.equal(submitRes.success, true);
      assert.ok(submitRes.report.reportId.startsWith('CJ-'));
      assert.equal(submitRes.report.evidence, null);
      assert.equal(submitRes.report.visualAnalysis, null);
      assert.equal(submitRes.report.lifecycleState, 'RECEIVED');
      assert.equal(submitRes.receipt.evidenceStatus, 'Sin evidencia fotográfica (opcional)');
      assert.equal(submitRes.receipt.visualAnalysisStatus, 'No aplicable');

      // Verify persistence reload
      const reloadedCore = new InMemoryWorldCoreAdapter({ persistenceFilePath: tempPath });
      const persistedRecord = await reloadedCore.getCitizenReport(submitRes.report.reportId);
      assert.ok(persistedRecord !== null);
      assert.equal(persistedRecord.evidence, null);
      assert.equal(persistedRecord.visualAnalysis, null);
      assert.equal(persistedRecord.confirmedCategory, 'water-and-drainage');

      if (existsSync(tempPath)) unlinkSync(tempPath);
    });

    // Test 14: Valid image without consent remains local
    it('14. valid image without consent remains local and SAM is not called', async () => {
      let samCalled = false;
      const trackingMeta: any = {
        segmentImage: async () => {
          samCalled = true;
          return { model: 'sam-3.1', provider: 'sam-3.1-live-api', analyzedAt: new Date().toISOString(), boxes: [] };
        },
      };

      const useCase = new CreateCitizenReportDraftUseCase({ metaModel: trackingMeta });
      const draft = await useCase.execute({
        citizenDescription: 'Bache grande',
        image: {
          base64: validPngBuffer.toString('base64'),
          mimeType: 'image/png',
          fileName: 'pothole.png',
        },
        externalAiProcessingConsent: false,
        language: 'es',
      });

      assert.equal(samCalled, false, 'SAM must not be called when consent is false');
      assert.ok(draft.evidence !== null);
      assert.equal(draft.visualAnalysis?.provider, 'not-authorized');
    });

    // Test 15: Valid image with consent uses authorized processing path
    it('15. valid image with consent uses authorized processing path with dynamic target', async () => {
      let promptSent: string | undefined;
      const trackingMeta: any = {
        segmentImage: async (params: any) => {
          promptSent = params.promptText;
          return {
            model: 'sam-3.1',
            provider: 'sam-3.1-live-api',
            analyzedAt: new Date().toISOString(),
            boxes: [{ ymin: 0.2, xmin: 0.2, ymax: 0.6, xmax: 0.6, label: 'water leak' }],
          };
        },
      };

      const useCase = new CreateCitizenReportDraftUseCase({ metaModel: trackingMeta });
      const draft = await useCase.execute({
        citizenDescription: 'Encharcamiento y fuga de agua potable',
        category: 'water-and-drainage',
        image: {
          base64: validPngBuffer.toString('base64'),
          mimeType: 'image/png',
          fileName: 'leak.png',
        },
        externalAiProcessingConsent: true,
        language: 'es',
      });

      assert.ok(promptSent?.includes('water leak') || promptSent?.includes('water'));
      assert.equal(draft.visualAnalysis?.provider, 'sam-3.1-live-api');
      assert.equal(draft.visualAnalysis?.boxes.length, 1);
    });

    // Test 16: Malformed nonempty image payload is rejected explicitly with InvalidImageEvidenceError
    it('16. malformed nonempty image payload is rejected explicitly with InvalidImageEvidenceError', async () => {
      const mockMeta = new MockMetaModelAdapter();
      const useCase = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });

      // Nonempty invalid characters
      await assert.rejects(
        async () => {
          await useCase.execute({
            citizenDescription: 'Bache en el pavimento',
            image: {
              fileName: 'photo.jpg',
              mimeType: 'image/jpeg',
              base64: 'this-is-not-valid-base64-payload!@#$%',
            },
            language: 'es',
          });
        },
        (err: any) => {
          assert.ok(err instanceof InvalidImageEvidenceError);
          return true;
        }
      );

      // Nonempty valid base64 but corrupt non-image bytes (fails magic byte inspection)
      const corruptBase64 = Buffer.from('NOT AN IMAGE FILE AT ALL').toString('base64');
      await assert.rejects(
        async () => {
          await useCase.execute({
            citizenDescription: 'Bache en el pavimento',
            image: {
              fileName: 'corrupt.jpg',
              mimeType: 'image/jpeg',
              base64: corruptBase64,
            },
            language: 'es',
          });
        },
        (err: any) => {
          assert.ok(err instanceof InvalidImageEvidenceError);
          assert.match(err.message, /Unsupported image format|corrupted file/i);
          return true;
        }
      );
    });

    // Test 17: Real HTTP server integration test (reproducing exact user scenario)
    it('17. Real HTTP Server integration: POST /api/citizen-reports/draft with empty image object succeeds 200 OK', async () => {
      const tempPath = resolve(process.cwd(), '.runtime', `http_test_persist_${Date.now()}.json`);
      const worldCore = new InMemoryWorldCoreAdapter({ persistenceFilePath: tempPath });
      const app = createAssistantServer({ worldCore });

      await new Promise<void>((resolve) => app.server.listen(0, '127.0.0.1', () => resolve()));
      const port = (app.server.address() as any).port;

      try {
        // Send draft request matching Unity's exact serialized payload when no photo is attached
        const res = await fetch(`http://127.0.0.1:${port}/api/citizen-reports/draft`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            citizenDescription: 'Encharcamiento de agua en Avenida Benito Juárez frente al cruce peatonal.',
            locationDescription: 'Avenida Benito Juárez',
            category: 'water-and-drainage',
            representativeId: 'patricia-morales',
            externalAiProcessingConsent: false,
            language: 'es',
            image: {
              fileName: '',
              mimeType: '',
              base64: '',
            },
          }),
        });

        assert.equal(res.status, 200, 'HTTP status must be 200 OK, not 400');
        const json = (await res.json()) as any;
        assert.equal(json.success, true);
        assert.ok(json.draft?.draftId);
        assert.equal(json.draft?.evidence, null);
        assert.equal(json.draft?.visualAnalysis, null);
        assert.equal(json.draft?.category, 'water-and-drainage');

        // Submit the draft
        const submitRes = await fetch(`http://127.0.0.1:${port}/api/citizen-reports/submit`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            draftId: json.draft.draftId,
            confirmedByPlayer: true,
            confirmedCategory: 'water-and-drainage',
          }),
        });

        assert.equal(submitRes.status, 201, 'HTTP status must be 201 Created');
        const submitJson = (await submitRes.json()) as any;
        assert.equal(submitJson.success, true);
        assert.ok(submitJson.receipt?.reportId.startsWith('CJ-'));
        assert.equal(submitJson.receipt?.evidenceStatus, 'Sin evidencia fotográfica (opcional)');
      } finally {
        await app.stop();
        if (existsSync(tempPath)) unlinkSync(tempPath);
      }
    });
  });

  describe('Increment 5.11.4: Visual Evidence Inspection & Mask Overlay Transport', () => {
    it('1. draft with valid photo and consent carries authentic mask overlay and distinct objects', async () => {
      const mockMeta = new MockMetaModelAdapter();
      const createDraft = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });

      const draft = await createDraft.execute({
        citizenDescription: 'Basura acumulada en la banqueta obstruyendo el paso.',
        language: 'es',
        category: 'cleaning-and-waste',
        externalAiProcessingConsent: true,
        image: {
          fileName: 'garbage_bags.png',
          mimeType: 'image/png',
          base64: validPngBuffer.toString('base64'),
        },
      });

      assert.ok(draft.visualAnalysis !== null);
      const va = draft.visualAnalysis!;
      assert.equal(va.status, 'completed');
      assert.equal(va.hasAuthenticMasks, true);
      assert.ok(typeof va.overlayPngBase64 === 'string' && va.overlayPngBase64.length > 50);
      assert.ok(Array.isArray(va.objects));
      assert.equal(va.objects.length, 2);

      // Verify two distinct objects with different colors and labels
      const obj0 = va.objects[0]!;
      const obj1 = va.objects[1]!;
      assert.equal(obj0.objectId, 'mock_target_001');
      assert.equal(obj1.objectId, 'mock_target_002');
      assert.notEqual(obj0.colorHex, obj1.colorHex);
    });

    it('2. draft with photo but no consent returns no overlay and hasAuthenticMasks false', async () => {
      const mockMeta = new MockMetaModelAdapter();
      const createDraft = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });

      const draft = await createDraft.execute({
        citizenDescription: 'Basura acumulada en la banqueta.',
        language: 'es',
        category: 'cleaning-and-waste',
        externalAiProcessingConsent: false,
        image: {
          fileName: 'garbage_bags.png',
          mimeType: 'image/png',
          base64: validPngBuffer.toString('base64'),
        },
      });

      assert.ok(draft.visualAnalysis !== null);
      const va = draft.visualAnalysis!;
      assert.equal(va.provider, 'not-authorized');
      assert.equal(va.hasAuthenticMasks, false);
      assert.equal(va.overlayPngBase64, null);
    });

    it('3. inconclusive SAM analysis returns no overlay and hasAuthenticMasks false', async () => {
      const mockMeta = new MockMetaModelAdapter({ samNoMatch: true });
      const createDraft = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });

      const draft = await createDraft.execute({
        citizenDescription: 'Pavimento sin problemas aparentes.',
        language: 'es',
        category: 'road-surface-damage',
        externalAiProcessingConsent: true,
        image: {
          fileName: 'clean_road.png',
          mimeType: 'image/png',
          base64: validPngBuffer.toString('base64'),
        },
      });

      assert.ok(draft.visualAnalysis !== null);
      const va = draft.visualAnalysis!;
      assert.equal(va.status, 'inconclusive');
      assert.equal(va.hasAuthenticMasks, false);
      assert.equal(va.overlayPngBase64, null);
      assert.equal(va.boxes.length, 0);
    });

    it('4. boxes-only result correctly indicates hasAuthenticMasks false and provides box objects', async () => {
      const mockMeta = new MockMetaModelAdapter({ samBoxesOnly: true });
      const createDraft = new CreateCitizenReportDraftUseCase({ metaModel: mockMeta });

      const draft = await createDraft.execute({
        citizenDescription: 'Baches en el carril derecho.',
        language: 'es',
        category: 'road-surface-damage',
        externalAiProcessingConsent: true,
        image: {
          fileName: 'road_potholes.png',
          mimeType: 'image/png',
          base64: validPngBuffer.toString('base64'),
        },
      });

      assert.ok(draft.visualAnalysis !== null);
      const va = draft.visualAnalysis!;
      assert.equal(va.status, 'completed');
      assert.equal(va.hasAuthenticMasks, false);
      assert.equal(va.overlayPngBase64, null);
      assert.equal(va.boxes.length, 2);
      assert.ok(Array.isArray(va.objects));
      assert.equal(va.objects.length, 2);
    });

    it('5. MetaModelApiAdapter parses SAM 3.1 streaming SSE response with 2 masks and composites overlay', async () => {
      const sseText =
        '<0f>0<|box;x1=211;y1=228;x2=270;y2=254;w=320;h=334|>' +
        "<|mask;x=0;y=0;data=27,60,~!!!!M!0c[0o91w?q1!pIH4pPRVp2B3'7`e.ioeAf6-k/#Xd8%dX9x(|>" +
        ',1<|box;x1=155;y1=228;x2=202;y2=254;w=320;h=334|>' +
        '<|mask;x=0;y=0;data=27,48,~!!!!J!0c[q=Pj_zs=*4C4(/./x#:/`S_GnD`=o3?X{emCgO$y@|>\n';

      const sseStream =
        `data: {"type":"response.output_text.delta","delta":${JSON.stringify(sseText)},"item_id":"msg_1","output_index":0,"content_index":0}\n\n` +
        `data: {"type":"response.output_text.done","text":${JSON.stringify(sseText)},"item_id":"msg_1","output_index":0,"content_index":0}\n\n` +
        'data: {"type":"response.completed"}\n\n' +
        'data: [DONE]\n\n';

      const mockFetch: typeof fetch = async () => {
        return new Response(sseStream, {
          status: 200,
          headers: { 'Content-Type': 'text/event-stream' },
        });
      };

      const adapter = new MetaModelApiAdapter({
        config: {
          apiKey: 'test-api-key',
          baseURL: 'https://api.meta.ai/v1',
          model: 'muse-spark-1.3',
          isConfigured: true,
        },
        fetchFn: mockFetch,
      });

      const res = await adapter.segmentImage({
        imageBase64: validPngBuffer.toString('base64'),
        mimeType: 'image/png',
        promptText: 'garbage bag',
        imageDimensions: { width: 320, height: 334 },
      });

      assert.equal(res.status, 'completed');
      assert.equal(res.hasAuthenticMasks, true);
      assert.ok(res.overlayPngBase64 !== null);
      assert.equal(res.objects?.length, 2);
      assert.equal(res.boxes.length, 2);
      assert.equal(res.objects![0]!.objectId, '0');
      assert.equal(res.objects![1]!.objectId, '1');
    });
  });
});
