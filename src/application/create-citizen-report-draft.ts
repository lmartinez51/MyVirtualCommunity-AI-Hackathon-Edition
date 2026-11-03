import { existsSync, mkdirSync, writeFileSync, readdirSync, statSync, unlinkSync } from 'node:fs';
import { resolve, join } from 'node:path';
import {
  assessNonEmergencyBoundary,
  classifyCivicIssue,
  DEFAULT_REPORTING_REPRESENTATIVE,
  getDefaultSamTarget,
  getSimulatedDepartment,
  ImageEvidence,
  IncidentLocation,
  IssueCategory,
  MuseSparkInterpretation,
  NonEmergencyAssessment,
  normalizeIssueCategory,
  ReportingRepresentativeRef,
  SAM_MODEL_ID,
  SAM_VISUAL_ANALYSIS_DISCLAIMER_EN,
  SAM_VISUAL_ANALYSIS_DISCLAIMER_ES,
  SIMULATED_MUNICIPAL_SERVICE_NOTICE_EN,
  SIMULATED_MUNICIPAL_SERVICE_NOTICE_ES,
  SamSegmentationResult,
} from '../domain/citizen-report.js';
import { sanitizeAndNormalizeImage, SanitizedImage } from '../domain/image-sanitizer.js';
import { findCorridorEntity, PASO_DEL_NORTE_CORRIDOR } from '../domain/spatial-corridor.js';
import { MetaModelPort } from '../ports/meta-model-port.js';

export class InvalidReportDescriptionError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'InvalidReportDescriptionError';
  }
}

export class InvalidImageEvidenceError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'InvalidImageEvidenceError';
  }
}

export interface CreateReportDraftInput {
  readonly citizenDescription: unknown;
  readonly locationDescription?: unknown;
  readonly corridorEntityId?: unknown;
  readonly language?: unknown;
  readonly category?: unknown;
  readonly representativeId?: unknown;
  readonly externalAiProcessingConsent?: unknown;
  readonly image?: {
    readonly fileName?: unknown;
    readonly mimeType?: unknown;
    readonly base64?: unknown;
  } | null;
}

export interface CitizenReportDraftResult {
  readonly draftId: string;
  readonly citizenDescription: string;
  readonly language: 'en' | 'es';
  readonly category: IssueCategory;
  readonly location: IncidentLocation;
  readonly reportingRepresentative: ReportingRepresentativeRef;
  readonly evidence: ImageEvidence | null;
  readonly externalAiProcessingConsent: boolean;
  readonly visualAnalysis: SamSegmentationResult | null;
  readonly structuredInterpretation: MuseSparkInterpretation;
  readonly simulatedDestination: string;
  readonly simulationNotice: string;
  readonly nonEmergencyAssessment: NonEmergencyAssessment;
  readonly confirmationRequired: true;
}

// In-memory draft cache for multi-step player review & confirmation
const draftStore = new Map<string, CitizenReportDraftResult>();

export function getCachedDraft(draftId: string): CitizenReportDraftResult | null {
  return draftStore.get(draftId) ?? null;
}

export function clearCachedDraft(draftId: string): void {
  draftStore.delete(draftId);
}

/**
 * Cleans up unconfirmed evidence older than maxAgeMs (default 1 hour).
 */
export function cleanupExpiredEvidence(dir: string, maxAgeMs = 60 * 60 * 1000): number {
  if (!existsSync(dir)) return 0;
  let deletedCount = 0;
  const now = Date.now();
  try {
    const files = readdirSync(dir);
    for (const f of files) {
      if (!f.startsWith('ev_')) continue;
      const fullPath = join(dir, f);
      try {
        const stat = statSync(fullPath);
        if (now - stat.mtimeMs > maxAgeMs) {
          unlinkSync(fullPath);
          deletedCount++;
        }
      } catch {}
    }
  } catch {}
  return deletedCount;
}

/**
 * Resolves the incident location independently of the municipal intake representative.
 * Explicitly distinguishes verified in-corridor entities from out-of-coverage citywide locations.
 * Invariant: Never invents coordinates or substitutes representative location.
 */
export function resolveIncidentLocation(
  corridorEntityId?: unknown,
  rawLocationDescription?: unknown,
  language: 'en' | 'es' = 'es'
): IncidentLocation {
  const isEs = language === 'es';

  // 1. Direct corridor entity match by ID
  if (typeof corridorEntityId === 'string' && corridorEntityId.trim().length > 0) {
    const cleanId = corridorEntityId.trim();
    const entity = findCorridorEntity(cleanId);
    if (entity) {
      return {
        description: `${entity.canonicalName} (${entity.address})`,
        corridorEntityId: entity.id,
        isApproximate: false,
        coverageStatus: 'IN_DEMO_CORRIDOR',
        coordinates: {
          latitude: entity.coordinates.latitude,
          longitude: entity.coordinates.longitude,
        },
      };
    }

    // Entity ID provided but not found in corridor catalog: treat as external unresolved identifier without fabricated coords
    return {
      description: cleanId,
      corridorEntityId: null,
      isApproximate: true,
      coverageStatus: 'UNRESOLVED',
      coordinates: null,
    };
  }

  // 2. Free text location description
  if (typeof rawLocationDescription === 'string' && rawLocationDescription.trim().length > 0) {
    const cleanDesc = rawLocationDescription.trim();
    const lower = cleanDesc.toLowerCase();

    // Check if free text explicitly references a known corridor landmark or POI entity
    for (const entity of PASO_DEL_NORTE_CORRIDOR.entities) {
      const canonicalMatch = lower.includes(entity.canonicalName.toLowerCase());
      const popularMatch = entity.popularName ? lower.includes(entity.popularName.toLowerCase()) : false;
      const alternateMatch = entity.alternateNames
        ? entity.alternateNames.some((alt) => lower.includes(alt.toLowerCase()))
        : false;
      const idMatch = lower.includes(entity.id.toLowerCase());

      if (canonicalMatch || popularMatch || alternateMatch || idMatch) {
        return {
          description: cleanDesc,
          corridorEntityId: entity.id,
          isApproximate: false,
          coverageStatus: 'IN_DEMO_CORRIDOR',
          coordinates: {
            latitude: entity.coordinates.latitude,
            longitude: entity.coordinates.longitude,
          },
        };
      }
    }

    // Check if free text references known corridor toll plaza or specific landmark reference
    if (lower.includes('plaza de cobro') || lower.includes('caseta de cobro') || lower.includes('c-51')) {
      const bridgeEntity = PASO_DEL_NORTE_CORRIDOR.entities.find((e) => e.entityType === 'infrastructure');
      if (bridgeEntity) {
        return {
          description: cleanDesc,
          corridorEntityId: bridgeEntity.id,
          isApproximate: false,
          coverageStatus: 'IN_DEMO_CORRIDOR',
          coordinates: {
            latitude: bridgeEntity.coordinates.latitude,
            longitude: bridgeEntity.coordinates.longitude,
          },
        };
      }
    }

    // Check if location is generally inside the corridor street without specific POI coordinates
    if (
      lower.includes('avenida benito juárez') ||
      lower.includes('av. benito juárez') ||
      lower.includes('av benito juárez') ||
      lower.includes('av. juárez') ||
      lower.includes('av juárez') ||
      lower.includes('avenida juarez') ||
      lower.includes('calle juárez') ||
      lower.includes('calle juarez')
    ) {
      return {
        description: cleanDesc,
        corridorEntityId: null,
        isApproximate: true,
        coverageStatus: 'IN_DEMO_CORRIDOR',
        coordinates: null, // General corridor reference: does NOT receive landmark coordinates
      };
    }

    // Check for ambiguous or non-specific descriptions (e.g., "en una esquina", "por ahí", "zona centro")
    const ambiguousPhrases = [
      'en una esquina',
      'alguna esquina',
      'por ahí',
      'por ahi',
      'en algún lugar',
      'en algun lugar',
      'no sé',
      'no se',
      'no recuerdo',
      'desconocida',
      'sin dirección',
      'sin direccion',
      'ambigua',
      'indefinida',
      'en la calle',
      'por el centro',
      'zona centro',
      'en la ciudad',
      'algún punto',
      'algun punto',
      'somewhere',
      'unspecified',
      'unknown',
      'downtown',
      'corner',
    ];

    const isAmbiguous =
      ambiguousPhrases.some((phrase) => lower === phrase || lower.includes(phrase)) &&
      !lower.includes('colonia') &&
      !lower.includes('calle ') &&
      !lower.includes('av.') &&
      !lower.includes('avenida') &&
      !lower.includes('blvd');

    if (isAmbiguous) {
      return {
        description: cleanDesc,
        corridorEntityId: null,
        isApproximate: true,
        coverageStatus: 'UNRESOLVED',
        coordinates: null,
      };
    }

    // Explicitly outside demonstration corridor (e.g. Melchor Ocampo, Gómez Morín, Torres, etc.)
    return {
      description: cleanDesc,
      corridorEntityId: null,
      isApproximate: true,
      coverageStatus: 'OUTSIDE_DEMO_COVERAGE',
      coordinates: null,
    };
  }

  // 3. Fallback when location is not specified: strictly marked UNRESOLVED without inventing an address or sector
  return {
    description: isEs ? 'Ubicación no especificada' : 'Unspecified location',
    corridorEntityId: null,
    isApproximate: true,
    coverageStatus: 'UNRESOLVED',
    coordinates: null,
  };
}

/**
 * Resolves the intake representative through whom the report was channeled.
 */
export function resolveReportingRepresentative(repId?: unknown): ReportingRepresentativeRef {
  if (typeof repId === 'string' && repId.trim().length > 0) {
    const cleanId = repId.trim().toLowerCase();
    if (cleanId === 'patricia-morales' || cleanId === 'patricia-citizen-rep') {
      return DEFAULT_REPORTING_REPRESENTATIVE;
    }
    if (cleanId === 'elena-ramos') {
      return {
        id: 'elena-ramos',
        name: 'Elena Ramos',
        role: 'Virtual Border Concierge',
        sector: 'kiosko-puente-santa-fe',
      };
    }
    if (cleanId === 'carlos-mendez') {
      return {
        id: 'carlos-mendez',
        name: 'Carlos Méndez',
        role: 'Encargado de Farmacia',
        sector: 'farmacia-benavides-juarez',
      };
    }
    return {
      id: cleanId,
      name: repId.trim(),
      role: 'Representante Municipal',
      sector: 'modulo-general',
    };
  }
  return DEFAULT_REPORTING_REPRESENTATIVE;
}

export interface CreateCitizenReportDraftOptions {
  readonly metaModel?: MetaModelPort;
  readonly evidenceDir?: string;
}

export class CreateCitizenReportDraftUseCase {
  private readonly metaModel?: MetaModelPort;
  private readonly evidenceDir: string;

  constructor(options: CreateCitizenReportDraftOptions = {}) {
    this.metaModel = options.metaModel;
    this.evidenceDir = options.evidenceDir ?? resolve(process.cwd(), '.runtime', 'evidence');
  }

  public async execute(input: CreateReportDraftInput): Promise<CitizenReportDraftResult> {
    if (!input || typeof input !== 'object') {
      throw new InvalidReportDescriptionError('Request body must be an object.');
    }

    if (
      typeof input.citizenDescription !== 'string' ||
      input.citizenDescription.trim().length === 0
    ) {
      throw new InvalidReportDescriptionError(
        'citizenDescription is required and must be a non-empty string.'
      );
    }

    const trimmedDescription = input.citizenDescription.trim();
    if (trimmedDescription.length > 1000) {
      throw new InvalidReportDescriptionError(
        'citizenDescription exceeds maximum length of 1000 characters.'
      );
    }

    const rawLang = String(input.language ?? 'es').toLowerCase();
    const language: 'en' | 'es' = rawLang === 'en' ? 'en' : 'es';
    const isEs = language === 'es';

    // Step 1: Assess non-emergency boundary
    const nonEmergencyAssessment = assessNonEmergencyBoundary(trimmedDescription);

    // Step 2: Separate Representative from Incident Location
    const reportingRepresentative = resolveReportingRepresentative(input.representativeId);
    const location = resolveIncidentLocation(input.corridorEntityId, input.locationDescription, language);

    // Step 3: Classify civic issue (deterministic base)
    const deterministicClassification = classifyCivicIssue(trimmedDescription);
    let preliminaryCategory: IssueCategory = deterministicClassification.category;

    if (input.category && typeof input.category === 'string' && input.category.trim().length > 0) {
      preliminaryCategory = normalizeIssueCategory(input.category);
    }

    // Step 4: Photographic Evidence & External AI Processing Consent Boundary
    let evidence: ImageEvidence | null = null;
    let visualAnalysis: SamSegmentationResult | null = null;
    const externalAiProcessingConsent = input.externalAiProcessingConsent === true;

    // Proactively clean up expired unconfirmed evidence older than 1 hour
    cleanupExpiredEvidence(this.evidenceDir);

    // Determine if photographic evidence was supplied (Case A: optional evidence flow)
    let rawBase64Candidate: string | null = null;
    let imageBufferCandidate: Buffer | null = null;
    let declaredMime: string | undefined = undefined;
    let declaredName: string = 'evidence.jpg';

    if (input.image && typeof input.image === 'object') {
      const { base64, mimeType, fileName } = input.image;
      if (typeof base64 === 'string' && base64.trim().length > 0) {
        rawBase64Candidate = base64.trim();
      }
      if (typeof mimeType === 'string' && mimeType.trim().length > 0) {
        declaredMime = mimeType.trim();
      }
      if (typeof fileName === 'string' && fileName.trim().length > 0) {
        declaredName = fileName.trim();
      }
    } else if (typeof (input as any).imageBase64 === 'string' && (input as any).imageBase64.trim().length > 0) {
      rawBase64Candidate = (input as any).imageBase64.trim();
    }

    if (Buffer.isBuffer((input as any).imageBuffer) && (input as any).imageBuffer.length > 0) {
      imageBufferCandidate = (input as any).imageBuffer;
    }

    const hasAttachedImage = rawBase64Candidate !== null || imageBufferCandidate !== null;

    if (hasAttachedImage) {
      let imageBuffer: Buffer;
      if (imageBufferCandidate) {
        imageBuffer = imageBufferCandidate;
      } else {
        let rawBase64 = rawBase64Candidate!;
        // Strip optional data URL prefix if provided
        if (rawBase64.startsWith('data:')) {
          const commaIdx = rawBase64.indexOf(',');
          if (commaIdx >= 0) {
            rawBase64 = rawBase64.slice(commaIdx + 1).trim();
          }
        }

        if (rawBase64.length === 0) {
          throw new InvalidImageEvidenceError('Image base64 data is missing or empty.');
        }

        const cleanBase64 = rawBase64.replace(/\s+/g, '');
        // Validate base64 structure (reject invalid characters outside base64 charset)
        const isBase64Valid =
          /^(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?$/.test(cleanBase64) ||
          /^(?:[A-Za-z0-9_-]{4})*(?:[A-Za-z0-9_-]{2}==|[A-Za-z0-9_-]{3}=)?$/.test(cleanBase64);
        if (!isBase64Valid) {
          throw new InvalidImageEvidenceError('Failed to decode base64 image data: invalid characters in base64 payload.');
        }

        try {
          imageBuffer = Buffer.from(cleanBase64, 'base64');
        } catch {
          throw new InvalidImageEvidenceError('Failed to decode base64 image data.');
        }
      }

      if (!imageBuffer || imageBuffer.length === 0) {
        throw new InvalidImageEvidenceError('Image file is empty (0 bytes).');
      }

      let sanitized: SanitizedImage;
      try {
        sanitized = await sanitizeAndNormalizeImage(imageBuffer, declaredMime, declaredName);
      } catch (err) {
        throw new InvalidImageEvidenceError(err instanceof Error ? err.message : String(err));
      }

      const evidenceId = `ev_${Date.now()}_${sanitized.sha256.slice(0, 8)}`;
      const safeFileName = `${evidenceId}.${sanitized.format === 'png' ? 'png' : 'jpg'}`;
      const storagePath = resolve(this.evidenceDir, safeFileName);

      // Save sanitized bytes to private bounded runtime directory
      try {
        if (!existsSync(this.evidenceDir)) {
          mkdirSync(this.evidenceDir, { recursive: true });
        }
        writeFileSync(storagePath, sanitized.buffer);
      } catch {
        // Continue even if disk write fails in restrictive sandboxes
      }

      evidence = {
        evidenceId,
        fileName: declaredName,
        fileSizeBytes: sanitized.sizeBytes,
        mimeType: sanitized.mimeType,
        sha256: sanitized.sha256,
        storagePath,
        uploadedAt: new Date().toISOString(),
      };

      const dynamicSamTarget = getDefaultSamTarget(preliminaryCategory);

      // Enforcement of Section 4.2 External Image-Processing Consent:
      if (!externalAiProcessingConsent) {
        // Image is preserved locally as evidence, but MUST NOT be sent to external AI provider
        visualAnalysis = {
          model: SAM_MODEL_ID,
          status: 'unavailable',
          detectedConcept: dynamicSamTarget,
          boxes: [],
          maskReference: null,
          maskSummary: isEs
            ? 'Análisis visual externo no autorizado por el usuario (procesamiento omitido por privacidad).'
            : 'External visual analysis not authorized by user (processing omitted for privacy).',
          analyzedAt: new Date().toISOString(),
          provider: 'not-authorized',
          disclaimer: isEs
            ? SAM_VISUAL_ANALYSIS_DISCLAIMER_ES
            : SAM_VISUAL_ANALYSIS_DISCLAIMER_EN,
          objects: [],
          overlayPngBase64: null,
          hasAuthenticMasks: false,
        };
      } else if (this.metaModel && typeof this.metaModel.segmentImage === 'function') {
        // External consent granted: dispatch sanitized image to SAM 3.1
        try {
          visualAnalysis = await this.metaModel.segmentImage({
            imageBase64: sanitized.base64,
            mimeType: sanitized.mimeType,
            promptText: dynamicSamTarget,
            imageDimensions: { width: sanitized.width, height: sanitized.height },
          });
        } catch (samErr) {
          visualAnalysis = {
            model: SAM_MODEL_ID,
            status: 'unavailable',
            detectedConcept: dynamicSamTarget,
            boxes: [],
            maskReference: null,
            maskSummary: `Visual segmentation service unavailable: ${
              samErr instanceof Error ? samErr.message : String(samErr)
            }`,
            analyzedAt: new Date().toISOString(),
            provider: 'sam-3.1-unavailable',
            disclaimer: isEs
              ? SAM_VISUAL_ANALYSIS_DISCLAIMER_ES
              : SAM_VISUAL_ANALYSIS_DISCLAIMER_EN,
            objects: [],
            overlayPngBase64: null,
            hasAuthenticMasks: false,
          };
        }
      } else {
        visualAnalysis = {
          model: SAM_MODEL_ID,
          status: 'unavailable',
          detectedConcept: dynamicSamTarget,
          boxes: [],
          maskReference: null,
          maskSummary: 'No visual segmentation provider configured',
          analyzedAt: new Date().toISOString(),
          provider: 'none',
          disclaimer: isEs
            ? SAM_VISUAL_ANALYSIS_DISCLAIMER_ES
            : SAM_VISUAL_ANALYSIS_DISCLAIMER_EN,
          objects: [],
          overlayPngBase64: null,
          hasAuthenticMasks: false,
        };
      }
    }

    // Step 5: Muse Spark 1.3 Interpretation
    let structuredInterpretation: MuseSparkInterpretation;
    if (this.metaModel && typeof this.metaModel.interpretCitizenReport === 'function') {
      try {
        structuredInterpretation = await this.metaModel.interpretCitizenReport({
          citizenDescription: trimmedDescription,
          locationDescription: location.description,
          language,
          visualAnalysis: visualAnalysis ?? undefined,
        });
      } catch {
        structuredInterpretation = this.buildDeterministicInterpretation(
          trimmedDescription,
          location.description,
          language,
          preliminaryCategory,
          visualAnalysis
        );
      }
    } else {
      structuredInterpretation = this.buildDeterministicInterpretation(
        trimmedDescription,
        location.description,
        language,
        preliminaryCategory,
        visualAnalysis
      );
    }

    // Confirmed Category & Deterministic Simulated Destination
    let finalCategory = preliminaryCategory;
    if (structuredInterpretation?.category) {
      finalCategory = normalizeIssueCategory(structuredInterpretation.category);
    }

    const destination = getSimulatedDepartment(finalCategory, language);
    const notice = isEs
      ? SIMULATED_MUNICIPAL_SERVICE_NOTICE_ES
      : SIMULATED_MUNICIPAL_SERVICE_NOTICE_EN;

    const draftId = `draft_${Date.now()}_${Math.random().toString(36).substring(2, 8)}`;

    const draftResult: CitizenReportDraftResult = {
      draftId,
      citizenDescription: trimmedDescription,
      language,
      category: finalCategory,
      location,
      reportingRepresentative,
      evidence,
      externalAiProcessingConsent,
      visualAnalysis,
      structuredInterpretation,
      simulatedDestination: destination,
      simulationNotice: notice,
      nonEmergencyAssessment,
      confirmationRequired: true,
    };

    draftStore.set(draftId, draftResult);
    return draftResult;
  }

  private buildDeterministicInterpretation(
    description: string,
    location: string,
    language: 'en' | 'es',
    category: IssueCategory,
    visualAnalysis: SamSegmentationResult | null
  ): MuseSparkInterpretation {
    const isEs = language === 'es';
    const dept = getSimulatedDepartment(category, language);
    const visualTarget = getDefaultSamTarget(category);

    return {
      model: 'muse-spark-1.3',
      summary: isEs
        ? `Reporte cívico (${category}): ${description}`
        : `Civic issue report (${category}): ${description}`,
      category,
      locationDescription: location,
      additionalDetails: [
        isEs
          ? `Incidencia ciudadana registrada bajo categoría '${category}'`
          : `Citizen issue logged under category '${category}'`,
        visualAnalysis && visualAnalysis.status === 'completed'
          ? isEs
            ? 'Evidencia fotográfica analizada con segmentación visual coincidente'
            : 'Photographic evidence analyzed with matching visual segmentation'
          : visualAnalysis && visualAnalysis.provider === 'not-authorized'
          ? isEs
            ? 'Evidencia fotográfica preservada localmente sin análisis externo por consentimiento'
            : 'Photographic evidence preserved locally without external analysis per user consent'
          : visualAnalysis
          ? isEs
            ? 'Evidencia fotográfica adjunta'
            : 'Photographic evidence attached'
          : isEs
          ? 'Reporte registrado sin evidencia fotográfica'
          : 'Report registered without photographic evidence',
      ],
      missingInformation: [],
      suggestedDepartment: dept,
      suggestedVisualTarget: visualTarget,
      clarificationQuestion: null,
      interpretedAt: new Date().toISOString(),
      provider: 'deterministic-fallback',
    };
  }
}
