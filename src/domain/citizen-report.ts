import { createHash } from 'node:crypto';
import { GeoCoordinates } from './spatial-corridor.js';

/**
 * Authoritative lifecycle states for citizen issue reports.
 * Version 1 lifecycle is strictly bounded to the local demonstration.
 */
export type ReportLifecycleState = 'RECEIVED' | 'UNDER_REVIEW' | 'SIMULATED_ROUTING';

/**
 * Standard issue categories for municipal issue reporting in Ciudad Juárez.
 * Extended in Increment 5.11.3 for citywide applicability with 9 explicit categories.
 */
export type IssueCategory =
  | 'road-surface-damage'
  | 'sidewalk-obstruction'
  | 'cleaning-and-waste'
  | 'urban-cleanliness' // backward-compatible alias for cleaning-and-waste
  | 'water-and-drainage'
  | 'public-lighting'
  | 'traffic-signage'
  | 'parks-and-public-spaces'
  | 'trees-and-green-areas'
  | 'other';

/**
 * Geographic coverage classification for incident location.
 * Demonstrates citywide applicability while explicitly bounding precise coverage
 * to the verified Avenida Benito Juárez demonstration corridor.
 */
export type GeographicCoverageStatus =
  | 'IN_DEMO_CORRIDOR'
  | 'OUTSIDE_DEMO_COVERAGE'
  | 'UNRESOLVED';

/**
 * Authoritative representation of the incident location.
 * Completely decoupled from the municipal representative through whom the report was submitted.
 */
export interface IncidentLocation {
  readonly description: string;
  readonly corridorEntityId: string | null;
  readonly isApproximate: boolean;
  readonly coverageStatus: GeographicCoverageStatus;
  readonly coordinates: GeoCoordinates | null;
}

/**
 * Municipal representative through whom the report was submitted.
 * The representative is the intake channel, NOT the geographic incident location.
 */
export interface ReportingRepresentativeRef {
  readonly id: string;
  readonly name: string;
  readonly role: string;
  readonly sector: string;
}

export const DEFAULT_REPORTING_REPRESENTATIVE: ReportingRepresentativeRef = Object.freeze({
  id: 'patricia-morales',
  name: 'Lic. Patricia Morales',
  role: 'Enlace de Atención Ciudadana',
  sector: 'puente-paso-del-norte',
});

/**
 * Status of visual analysis performed by SAM 3.1.
 */
export type SamAnalysisStatus = 'completed' | 'inconclusive' | 'unavailable';

/**
 * Prominent disclosure mandated by Section 2:
 * Every relevant UI surface and response must clearly disclose the simulated boundary.
 */
export const SIMULATED_MUNICIPAL_SERVICE_NOTICE_EN =
  'Simulated municipal service — this report is recorded only inside the MyVirtualCommunity demonstration. It is not submitted to a real government authority.';

export const SIMULATED_MUNICIPAL_SERVICE_NOTICE_ES =
  'Servicio municipal simulado — este reporte queda registrado únicamente dentro de la demostración de MyVirtualCommunity. No se envía a ninguna autoridad gubernamental real.';

export const SIMULATED_DESTINATION_DEPARTMENT_EN =
  'Simulated municipal public works / road maintenance department';

export const SIMULATED_DESTINATION_DEPARTMENT_ES =
  'Departamento simulado de obras públicas / bacheo';

export const SAM_MODEL_ID = 'sam-3.1';
export const MUSE_SPARK_MODEL_ID = 'muse-spark-1.3';

export const MAX_IMAGE_SIZE_BYTES = 5 * 1024 * 1024; // 5 Megabytes
export const SUPPORTED_IMAGE_MIME_TYPES = ['image/jpeg', 'image/png'] as const;

export const SAM_VISUAL_ANALYSIS_DISCLAIMER_EN =
  'Visual detection and segmentation only — does not certify structural engineering condition, depth, or road safety severity.';

export const SAM_VISUAL_ANALYSIS_DISCLAIMER_ES =
  'Detección y segmentación visual únicamente — no certifica condiciones de ingeniería estructural, profundidad ni severidad vial.';

/**
 * Assessment for immediate non-emergency civic boundary.
 * If citizen description describes an emergency (e.g. violent crime, active fire, acute medical),
 * clear guidance is returned directing them to real 911 emergency services.
 */
export interface NonEmergencyAssessment {
  readonly isEmergency: boolean;
  readonly emergencyNoticeEs?: string;
  readonly emergencyNoticeEn?: string;
}

/**
 * Normalized 2D bounding box (0.0 to 1.0 coordinates).
 */
export interface BoundingBox {
  readonly ymin: number;
  readonly xmin: number;
  readonly ymax: number;
  readonly xmax: number;
  readonly label: string;
  readonly confidence?: number;
}

/**
 * Stored metadata for uploaded photographic evidence.
 * Binary bytes are stored in private runtime directory, never committed to git.
 */
export interface ImageEvidence {
  readonly evidenceId: string;
  readonly fileName: string;
  readonly fileSizeBytes: number;
  readonly mimeType: 'image/jpeg' | 'image/png';
  readonly sha256: string;
  readonly storagePath: string;
  readonly uploadedAt: string;
}

/**
 * A detected and segmented object identified by SAM 3.1.
 */
export interface SamDetectedObject {
  readonly objectId: string;
  readonly label: string;
  readonly confidence?: number;
  readonly box: BoundingBox;
  readonly maskBounds?: {
    readonly left: number;
    readonly top: number;
    readonly right: number;
    readonly bottom: number;
  };
  readonly maskDimensions?: { readonly width: number; readonly height: number };
  readonly colorHex?: string;
}

/**
 * SAM 3.1 Visual analysis result.
 */
export interface SamSegmentationResult {
  readonly model: string;
  readonly status: SamAnalysisStatus;
  readonly detectedConcept: string;
  readonly boxes: readonly BoundingBox[];
  readonly maskReference: string | null;
  readonly maskSummary?: string;
  readonly maskDimensions?: { readonly width: number; readonly height: number } | null;
  readonly objectId?: string | null;
  readonly confidence?: number | null;
  readonly imageDimensions?: { readonly width: number; readonly height: number } | null;
  readonly diagnostics?: readonly string[];
  readonly analyzedAt: string;
  readonly provider: string;
  readonly disclaimer: string;
  // Increment 5.11.4: Authentic mask preservation & transparent visual overlay
  readonly objects?: readonly SamDetectedObject[];
  readonly overlayPngBase64?: string | null;
  readonly hasAuthenticMasks?: boolean;
}

/**
 * Muse Spark 1.3 natural language interpretation and structured report summary.
 */
export interface MuseSparkInterpretation {
  readonly model: string;
  readonly summary: string;
  readonly category: IssueCategory;
  readonly locationDescription: string;
  readonly additionalDetails: readonly string[];
  readonly missingInformation: readonly string[];
  readonly suggestedDepartment: string;
  readonly suggestedVisualTarget?: string;
  readonly clarificationQuestion: string | null;
  readonly isEmergency?: boolean;
  readonly interpretedAt: string;
  readonly provider: string;
}

/**
 * Complete, authoritative Citizen Issue Report domain record.
 */
export interface CitizenReportRecord {
  readonly reportId: string;
  readonly citizenDescription: string;
  readonly language: 'en' | 'es';
  readonly confirmedCategory: IssueCategory;
  readonly location: IncidentLocation;
  readonly reportingRepresentative: ReportingRepresentativeRef;
  readonly evidence: ImageEvidence | null;
  readonly externalAiProcessingConsent: boolean;
  readonly visualAnalysis: SamSegmentationResult | null;
  readonly structuredInterpretation: MuseSparkInterpretation | null;
  readonly lifecycleState: ReportLifecycleState;
  readonly simulatedDestination: string;
  readonly simulationNotice: string;
  readonly nonEmergencyAssessment?: NonEmergencyAssessment;
  readonly createdAt: string;
  readonly updatedAt: string;
}

/**
 * Metadata definition for issue reporting categories.
 */
export interface CategoryMetadata {
  readonly id: IssueCategory;
  readonly canonicalId: IssueCategory;
  readonly nameEs: string;
  readonly nameEn: string;
  readonly descriptionEs: string;
  readonly descriptionEn: string;
  readonly simulatedDepartmentEs: string;
  readonly simulatedDepartmentEn: string;
  readonly defaultSamVisualTarget: string;
}

/**
 * Authoritative category catalog mapping each issue category to its
 * simulated municipal responsibility area in Ciudad Juárez.
 */
export const ISSUE_CATEGORY_CATALOG: readonly CategoryMetadata[] = Object.freeze([
  {
    id: 'road-surface-damage',
    canonicalId: 'road-surface-damage',
    nameEs: 'Vialidad y Pavimentación',
    nameEn: 'Roads & Pavement Infrastructure',
    descriptionEs: 'Baches, pavimento agrietado, hundimientos de calzada y desniveles de asfalto.',
    descriptionEn: 'Potholes, cracked pavement, road depressions, and asphalt irregularities.',
    simulatedDepartmentEs: 'Dirección General de Obras Públicas — Mantenimiento Vial',
    simulatedDepartmentEn: 'General Directorate of Public Works — Road Maintenance',
    defaultSamVisualTarget: 'road pothole or damaged asphalt surface',
  },
  {
    id: 'sidewalk-obstruction',
    canonicalId: 'sidewalk-obstruction',
    nameEs: 'Banquetas y Accesibilidad Peatonal',
    nameEn: 'Sidewalks & Pedestrian Accessibility',
    descriptionEs: 'Banquetas fracturadas, rampas bloqueadas u obstáculos para el tránsito peatonal.',
    descriptionEn: 'Broken sidewalks, blocked pedestrian ramps, or accessibility obstacles.',
    simulatedDepartmentEs: 'Dirección General de Obras Públicas — Accesibilidad y Banquetas',
    simulatedDepartmentEn: 'General Directorate of Public Works — Accessibility & Sidewalks',
    defaultSamVisualTarget: 'broken sidewalk or pedestrian obstruction',
  },
  {
    id: 'cleaning-and-waste',
    canonicalId: 'cleaning-and-waste',
    nameEs: 'Limpia y Residuos Urbanos',
    nameEn: 'Cleaning & Waste Management',
    descriptionEs: 'Bolsas de basura abandonadas, escombros o tiraderos clandestinos en vía pública.',
    descriptionEn: 'Discarded garbage bags, debris, or illegal dumping in public areas.',
    simulatedDepartmentEs: 'Dirección de Limpia — Gestión de Residuos Urbanos',
    simulatedDepartmentEn: 'Sanitation Directorate — Urban Waste Management',
    defaultSamVisualTarget: 'discarded garbage bags or accumulated waste',
  },
  {
    id: 'urban-cleanliness', // Backward-compatible alias
    canonicalId: 'cleaning-and-waste',
    nameEs: 'Limpia y Residuos Urbanos',
    nameEn: 'Cleaning & Waste Management',
    descriptionEs: 'Bolsas de basura abandonadas, escombros o tiraderos clandestinos en vía pública.',
    descriptionEn: 'Discarded garbage bags, debris, or illegal dumping in public areas.',
    simulatedDepartmentEs: 'Dirección de Limpia — Gestión de Residuos Urbanos',
    simulatedDepartmentEn: 'Sanitation Directorate — Urban Waste Management',
    defaultSamVisualTarget: 'discarded garbage bags or accumulated waste',
  },
  {
    id: 'water-and-drainage',
    canonicalId: 'water-and-drainage',
    nameEs: 'Agua y Drenaje Pluvial',
    nameEn: 'Water & Storm Drainage',
    descriptionEs: 'Encharcamientos graves, alcantarillado desbordado, fugas de agua o anegamientos.',
    descriptionEn: 'Severe standing water, overflowing storm drains, water leaks, or localized flooding.',
    simulatedDepartmentEs: 'Junta Municipal de Agua y Saneamiento (JMAS) — Drenaje Pluvial',
    simulatedDepartmentEn: 'Municipal Water & Sanitation Board (JMAS) — Storm Drainage',
    defaultSamVisualTarget: 'standing water or drainage accumulation',
  },
  {
    id: 'public-lighting',
    canonicalId: 'public-lighting',
    nameEs: 'Alumbrado Público',
    nameEn: 'Public Lighting',
    descriptionEs: 'Luminarias apagadas, farolas dañadas o sectores peatonales a oscuras.',
    descriptionEn: 'Non-working streetlights, damaged lamp posts, or dark pedestrian corridors.',
    simulatedDepartmentEs: 'Dirección de Servicios Públicos — Alumbrado Público',
    simulatedDepartmentEn: 'Public Services Directorate — Public Lighting',
    defaultSamVisualTarget: 'damaged or non-working streetlight',
  },
  {
    id: 'traffic-signage',
    canonicalId: 'traffic-signage',
    nameEs: 'Señalización y Semáforos',
    nameEn: 'Traffic Signage & Signals',
    descriptionEs: 'Señales viales caídas o faltantes, discos vandalizados o semáforos descompuestos.',
    descriptionEn: 'Missing or damaged traffic signs, fallen stop signs, or malfunctioning signals.',
    simulatedDepartmentEs: 'Coordinación General de Seguridad Vial — Infraestructura Vial',
    simulatedDepartmentEn: 'General Road Safety Coordination — Road Infrastructure',
    defaultSamVisualTarget: 'damaged or missing traffic sign',
  },
  {
    id: 'parks-and-public-spaces',
    canonicalId: 'parks-and-public-spaces',
    nameEs: 'Parques y Espacios Públicos',
    nameEn: 'Parks & Public Spaces',
    descriptionEs: 'Mobiliario urbano destruido, bancas rotas, juegos infantiles o plazas deterioradas.',
    descriptionEn: 'Damaged urban furniture, broken park benches, play structures, or neglected plazas.',
    simulatedDepartmentEs: 'Dirección de Parques y Jardines — Equipamiento Urbano',
    simulatedDepartmentEn: 'Parks & Gardens Directorate — Urban Equipment',
    defaultSamVisualTarget: 'damaged public park equipment or urban furniture',
  },
  {
    id: 'trees-and-green-areas',
    canonicalId: 'trees-and-green-areas',
    nameEs: 'Arbolado y Áreas Verdes',
    nameEn: 'Trees & Green Areas',
    descriptionEs: 'Árboles caídos, ramas en riesgo de desprenderse o áreas verdes públicas dañadas.',
    descriptionEn: 'Fallen trees, dangerous hanging branches, or damaged public green spaces.',
    simulatedDepartmentEs: 'Dirección de Ecología — Arbolado Urbano',
    simulatedDepartmentEn: 'Ecology Directorate — Urban Trees',
    defaultSamVisualTarget: 'fallen tree branch or damaged public tree',
  },
  {
    id: 'other',
    canonicalId: 'other',
    nameEs: 'Otros Asuntos Cívicos / Revisión Manual',
    nameEn: 'Other Civic Issues / Manual Review',
    descriptionEs: 'Incidencias cívicas o solicitudes de servicio municipal válidas no contempladas en las categorías anteriores.',
    descriptionEn: 'Valid non-emergency civic matters or municipal service requests not covered by specific categories.',
    simulatedDepartmentEs: 'Dirección de Atención Ciudadana — Revisión y Canalización',
    simulatedDepartmentEn: 'Citizen Attention Directorate — Review & Routing',
    defaultSamVisualTarget: 'urban public space issue',
  },
]);

/**
 * Comprehensive mapping for legacy category identifiers used across past increments
 * (e.g., BACHES_Y_VIALIDAD, BANQUETAS_Y_ACCESIBILIDAD, ALUMBRADO_PUBLICO, etc.).
 */
export const LEGACY_CATEGORY_MAP: Readonly<Record<string, IssueCategory>> = Object.freeze({
  'baches_y_vialidad': 'road-surface-damage',
  'baches-y-vialidad': 'road-surface-damage',
  'bacheo': 'road-surface-damage',
  'vialidad_y_pavimentacion': 'road-surface-damage',
  'vialidad-y-pavimentacion': 'road-surface-damage',
  'banquetas_y_accesibilidad': 'sidewalk-obstruction',
  'banquetas-y-accesibilidad': 'sidewalk-obstruction',
  'limpieza_y_residuos': 'cleaning-and-waste',
  'limpieza-y-residuos': 'cleaning-and-waste',
  'limpia_y_residuos': 'cleaning-and-waste',
  'limpia-y-residuos': 'cleaning-and-waste',
  'urban-cleanliness': 'cleaning-and-waste',
  'agua_y_drenaje': 'water-and-drainage',
  'agua-y-drenaje': 'water-and-drainage',
  'fugas_de_agua': 'water-and-drainage',
  'alumbrado_publico': 'public-lighting',
  'alumbrado-publico': 'public-lighting',
  'senalizacion_vial': 'traffic-signage',
  'senalizacion-vial': 'traffic-signage',
  'señalizacion_vial': 'traffic-signage',
  'señalizacion-vial': 'traffic-signage',
  'parques_y_espacios_publicos': 'parks-and-public-spaces',
  'parques-y-espacios-publicos': 'parks-and-public-spaces',
  'arbolado_y_areas_verdes': 'trees-and-green-areas',
  'arbolado-y-areas-verdes': 'trees-and-green-areas',
  'otro': 'other',
  'otros': 'other',
  'otros_asuntos': 'other',
});

/**
 * Normalizes an arbitrary string or legacy category ID to a canonical IssueCategory.
 * Accepts legacy screaming-snake-case, kebab-case, or localized aliases.
 * Defaults to 'other' if unrecognized.
 */
export function normalizeIssueCategory(rawCategory: unknown): IssueCategory {
  if (typeof rawCategory !== 'string' || rawCategory.trim() === '') {
    return 'other';
  }
  const clean = rawCategory.trim().toLowerCase();

  // 1. Direct legacy alias lookup
  if (clean in LEGACY_CATEGORY_MAP) {
    return LEGACY_CATEGORY_MAP[clean]!;
  }

  // 2. Catalog canonical match
  const entry = ISSUE_CATEGORY_CATALOG.find(
    (c) => c.id.toLowerCase() === clean || c.canonicalId.toLowerCase() === clean
  );
  if (entry) {
    return entry.canonicalId;
  }
  return 'other';
}

/**
 * Retrieves the deterministic category metadata.
 */
export function getCategoryMetadata(category: IssueCategory): CategoryMetadata {
  const normalized = normalizeIssueCategory(category);
  return (
    ISSUE_CATEGORY_CATALOG.find((c) => c.canonicalId === normalized) ??
    ISSUE_CATEGORY_CATALOG[ISSUE_CATEGORY_CATALOG.length - 1]!
  );
}

/**
 * Derives the deterministic simulated department for an issue category.
 */
export function getSimulatedDepartment(category: IssueCategory, language: 'en' | 'es' = 'es'): string {
  const meta = getCategoryMetadata(category);
  return language === 'en' ? meta.simulatedDepartmentEn : meta.simulatedDepartmentEs;
}

/**
 * Derives the default concrete visual target for SAM 3.1 based on issue category.
 */
export function getDefaultSamTarget(category: IssueCategory): string {
  const meta = getCategoryMetadata(category);
  return meta.defaultSamVisualTarget;
}

/**
 * Deterministic classifier: parses a citizen's natural language description
 * and suggests the most appropriate category among the 9 municipal areas.
 * Avoids forcing uncertain reports into unrelated categories.
 */
export function classifyCivicIssue(description: string): {
  category: IssueCategory;
  isAmbiguous: boolean;
  confidenceScore: number;
} {
  if (!description || typeof description !== 'string' || description.trim().length === 0) {
    return { category: 'other', isAmbiguous: true, confidenceScore: 0.1 };
  }

  const lower = description.toLowerCase();

  // Pattern detection rules
  const roadMatches =
    lower.includes('bache') ||
    lower.includes('asfalto') ||
    lower.includes('pavimento') ||
    lower.includes('calzada') ||
    lower.includes('hoyo en la calle') ||
    lower.includes('zanja') ||
    lower.includes('pothole') ||
    lower.includes('road surface') ||
    lower.includes('damaged road');

  const sidewalkMatches =
    lower.includes('banqueta') ||
    lower.includes('acera') ||
    lower.includes('rampa peatonal') ||
    lower.includes('paso peatonal') ||
    lower.includes('sidewalk') ||
    lower.includes('curb ramp') ||
    lower.includes('pedestrian path');

  const cleaningMatches =
    lower.includes('basura') ||
    lower.includes('bolsas de basura') ||
    lower.includes('desperdicios') ||
    lower.includes('escombros') ||
    lower.includes('tiradero') ||
    lower.includes('limpia') ||
    lower.includes('garbage') ||
    lower.includes('trash') ||
    lower.includes('litter') ||
    lower.includes('waste');

  const waterMatches =
    lower.includes('encharcamiento') ||
    lower.includes('charco grande') ||
    lower.includes('inundaci') ||
    lower.includes('drenaje') ||
    lower.includes('alcantarilla') ||
    lower.includes('fuga de agua') ||
    lower.includes('aguas negras') ||
    lower.includes('standing water') ||
    lower.includes('flooding') ||
    lower.includes('storm drain') ||
    lower.includes('sewer overflow');

  const lightingMatches =
    lower.includes('luminaria') ||
    lower.includes('alumbrado') ||
    lower.includes('farola') ||
    lower.includes('poste de luz') ||
    lower.includes('foco fundido') ||
    lower.includes('a oscuras') ||
    lower.includes('streetlight') ||
    lower.includes('street light') ||
    lower.includes('lamp post') ||
    lower.includes('broken light');

  const signageMatches =
    lower.includes('señal de alto') ||
    lower.includes('señalización') ||
    lower.includes('señalamiento') ||
    lower.includes('semáforo') ||
    lower.includes('disco de alto') ||
    lower.includes('nomenclatura de calle') ||
    lower.includes('traffic sign') ||
    lower.includes('stop sign') ||
    lower.includes('traffic light');

  const parksMatches =
    lower.includes('parque') ||
    lower.includes('plaza pública') ||
    lower.includes('banca rota') ||
    lower.includes('juegos infantiles') ||
    lower.includes('mobiliario urbano') ||
    lower.includes('kiosko dañado') ||
    lower.includes('public park') ||
    lower.includes('park bench') ||
    lower.includes('playground');

  const treesMatches =
    lower.includes('árbol') ||
    lower.includes('arbol') ||
    lower.includes('rama caída') ||
    lower.includes('rama rota') ||
    lower.includes('área verde') ||
    lower.includes('arbusto') ||
    lower.includes('tree') ||
    lower.includes('fallen branch') ||
    lower.includes('green area');

  // Count active category matches
  const matches: Array<{ category: IssueCategory; weight: number }> = [];
  if (cleaningMatches) matches.push({ category: 'cleaning-and-waste', weight: 1.0 });
  if (waterMatches) matches.push({ category: 'water-and-drainage', weight: 1.0 });
  if (lightingMatches) matches.push({ category: 'public-lighting', weight: 1.0 });
  if (sidewalkMatches) matches.push({ category: 'sidewalk-obstruction', weight: 1.0 });
  if (signageMatches) matches.push({ category: 'traffic-signage', weight: 1.0 });
  if (parksMatches) matches.push({ category: 'parks-and-public-spaces', weight: 1.0 });
  if (treesMatches) matches.push({ category: 'trees-and-green-areas', weight: 1.0 });
  if (roadMatches) matches.push({ category: 'road-surface-damage', weight: 1.0 });

  if (matches.length === 1) {
    return { category: matches[0]!.category, isAmbiguous: false, confidenceScore: 0.85 };
  }

  if (matches.length > 1) {
    // If ambiguous description matches multiple distinct categories, do not fabricate certainty
    return { category: 'other', isAmbiguous: true, confidenceScore: 0.5 };
  }

  // Valid non-emergency civic issue not matching existing explicit keywords
  return { category: 'other', isAmbiguous: false, confidenceScore: 0.6 };
}

/**
 * Evaluates whether a citizen report describes an acute emergency
 * (threat to life, violence, building on fire, active robbery).
 */
export function assessNonEmergencyBoundary(description: string): NonEmergencyAssessment {
  if (!description || typeof description !== 'string') {
    return { isEmergency: false };
  }
  const lower = description.toLowerCase();
  const emergencyKeywords = [
    'balacera',
    'disparos',
    'secuestro',
    'incendio activo',
    'fuego en casa',
    'arma de fuego',
    'herido de bala',
    'herido grave',
    'infarto',
    'paro cardiaco',
    'amenaza de bomba',
    'asalto a mano armada',
    'active shooter',
    'gunfire',
    'gunshots',
    'armed robbery',
    'building on fire',
    'hostage',
    'medical emergency',
    'cardiac arrest',
  ];

  const matched = emergencyKeywords.some((kw) => lower.includes(kw));
  if (matched) {
    return {
      isEmergency: true,
      emergencyNoticeEs:
        'EMERGENCIA DETECTADA: Este sistema es exclusivamente para servicios municipales y mantenimiento urbano no urgente. Si se encuentra ante una emergencia médica, situación de violencia, delito en progreso o peligro inminente a la vida, comuníquese de inmediato al 911 o a los servicios de emergencia reales correspondientes. Este reporte simulado no envía cuerpos de rescate ni unidades policiacas.',
      emergencyNoticeEn:
        'EMERGENCY DETECTED: This system is strictly for non-urgent municipal services, public works, and civic maintenance. If you are facing a medical emergency, active violence, crime in progress, or imminent threat to life, call 911 or real emergency services immediately. This simulated report does not dispatch emergency response units.',
    };
  }

  return { isEmergency: false };
}

/**
 * Profile of the fictional municipal citizen-service representative.
 */
export interface CitizenRepresentativeProfile {
  readonly id: string;
  readonly name: string;
  readonly role: string;
  readonly roleEn: string;
  readonly avatar: string;
  readonly destination: string;
  readonly simulationNotice: string;
  readonly suggestedPromptsEn: readonly string[];
  readonly suggestedPromptsEs: readonly string[];
}

export const CITIZEN_REPRESENTATIVE_PROFILE: CitizenRepresentativeProfile = Object.freeze({
  id: 'patricia-citizen-rep',
  name: 'Lic. Patricia Morales',
  role: 'Enlace de Atención Ciudadana',
  roleEn: 'Citizen Service Representative',
  avatar: '📋',
  destination: 'Dirección General de Obras Públicas — Mantenimiento Vial',
  simulationNotice: SIMULATED_MUNICIPAL_SERVICE_NOTICE_ES,
  suggestedPromptsEn: Object.freeze([
    'I found a pothole on Avenida Juárez near the crossing.',
    'There are abandoned garbage bags blocking the pedestrian walkway.',
    'A streetlight is broken and the block is dark.',
    'There is heavy standing water and drain overflow.',
    'How does simulated municipal citizen issue reporting work?',
  ]),
  suggestedPromptsEs: Object.freeze([
    'Encontré un bache en Avenida Juárez cerca del cruce.',
    'Hay bolsas de basura abandonadas en la acera.',
    'Una luminaria está apagada y la calle está oscura.',
    'Hay un encharcamiento grave por alcantarilla tapada.',
    '¿Cómo funciona el reporte ciudadano simulado?',
  ]),
});

/**
 * Validates an image buffer: verifies size limit and checks authentic magic bytes.
 */
export function validateImageBuffer(
  buffer: Buffer,
  declaredMimeType?: string,
  _fileName?: string
): { valid: boolean; error?: string; format?: 'png' | 'jpeg'; detectedMime?: 'image/png' | 'image/jpeg'; size: number } {
  if (!buffer || buffer.length === 0) {
    return { valid: false, error: 'Image file is empty (0 bytes).', size: 0 };
  }

  if (buffer.length > MAX_IMAGE_SIZE_BYTES) {
    return {
      valid: false,
      error: `Image exceeds maximum allowed size of 5 MB (received ${(buffer.length / (1024 * 1024)).toFixed(2)} MB).`,
      size: buffer.length,
    };
  }

  // Check PNG magic bytes: 89 50 4E 47 0D 0A 1A 0A
  const isPng =
    buffer.length >= 8 &&
    buffer[0] === 0x89 &&
    buffer[1] === 0x50 &&
    buffer[2] === 0x4e &&
    buffer[3] === 0x47 &&
    buffer[4] === 0x0d &&
    buffer[5] === 0x0a &&
    buffer[6] === 0x1a &&
    buffer[7] === 0x0a;

  // Check JPEG magic bytes: FF D8 FF
  const isJpeg =
    buffer.length >= 3 &&
    buffer[0] === 0xff &&
    buffer[1] === 0xd8 &&
    buffer[2] === 0xff;

  if (!isPng && !isJpeg) {
    return {
      valid: false,
      error: 'Unsupported image format or corrupted file. Only valid PNG and JPEG images are accepted.',
      size: buffer.length,
    };
  }

  const detectedFormat = isPng ? 'png' : 'jpeg';
  const expectedMime = isPng ? 'image/png' : 'image/jpeg';

  if (declaredMimeType && declaredMimeType.toLowerCase() !== expectedMime) {
    return {
      valid: false,
      error: `Declared MIME type '${declaredMimeType}' does not match verified file format '${detectedFormat}'.`,
      size: buffer.length,
    };
  }

  return { valid: true, format: detectedFormat, detectedMime: expectedMime, size: buffer.length };
}

/**
 * Computes SHA-256 digest of binary buffer.
 */
export function computeSha256(buffer: Buffer): string {
  return createHash('sha256').update(buffer).digest('hex');
}

let reportCounter = 184;

/**
 * Generates a stable, human-readable report identifier (e.g. CJ-000184).
 */
export function generateReportId(): string {
  const num = reportCounter++;
  const padded = String(num).padStart(6, '0');
  return `CJ-${padded}`;
}

/**
 * Resets report counter (primarily for deterministic unit testing).
 */
export function resetReportCounter(initialVal: number = 184): void {
  reportCounter = initialVal;
}

/**
 * Normalizes an arbitrary or legacy persisted CitizenReportRecord to ensure
 * total backward compatibility with pre-5.11.3 stored records.
 * Preserves original reportId, citizenDescription, evidence metadata, and visual analysis.
 */
export function normalizeCitizenReportRecord(raw: any): CitizenReportRecord {
  if (!raw || typeof raw !== 'object') {
    throw new Error('Invalid citizen report record structure: record must be an object.');
  }

  let rawCat = raw.confirmedCategory ?? raw.category ?? raw.structuredInterpretation?.category;
  if (!rawCat && typeof raw.citizenDescription === 'string' && raw.citizenDescription.trim().length > 0) {
    rawCat = classifyCivicIssue(raw.citizenDescription).category;
  }
  const confirmedCategory = normalizeIssueCategory(rawCat ?? 'other');

  // Normalize location
  let location: IncidentLocation;
  if (raw.location && typeof raw.location === 'object') {
    const rawLoc = raw.location;
    const desc =
      typeof rawLoc.description === 'string' && rawLoc.description.trim().length > 0
        ? rawLoc.description.trim()
        : 'Ubicación no especificada';
    const isApprox = typeof rawLoc.isApproximate === 'boolean' ? rawLoc.isApproximate : true;
    let coverageStatus: GeographicCoverageStatus = 'UNRESOLVED';
    if (
      rawLoc.coverageStatus === 'IN_DEMO_CORRIDOR' ||
      rawLoc.coverageStatus === 'OUTSIDE_DEMO_COVERAGE' ||
      rawLoc.coverageStatus === 'UNRESOLVED'
    ) {
      coverageStatus = rawLoc.coverageStatus;
    } else if (rawLoc.corridorEntityId) {
      coverageStatus = 'IN_DEMO_CORRIDOR';
    } else if (
      desc.toLowerCase().includes('benito juárez') ||
      desc.toLowerCase().includes('benito juarez') ||
      desc.toLowerCase().includes('juarez') ||
      desc.toLowerCase().includes('juárez') ||
      desc.toLowerCase().includes('paso del norte')
    ) {
      coverageStatus = 'IN_DEMO_CORRIDOR';
    } else {
      coverageStatus = 'UNRESOLVED';
    }

    const coords =
      rawLoc.coordinates &&
      typeof rawLoc.coordinates.latitude === 'number' &&
      typeof rawLoc.coordinates.longitude === 'number'
        ? { latitude: rawLoc.coordinates.latitude, longitude: rawLoc.coordinates.longitude }
        : null;

    location = {
      description: desc,
      corridorEntityId: typeof rawLoc.corridorEntityId === 'string' ? rawLoc.corridorEntityId : null,
      isApproximate: isApprox,
      coverageStatus,
      coordinates: coords,
    };
  } else {
    location = {
      description: 'Ubicación no especificada',
      corridorEntityId: null,
      isApproximate: true,
      coverageStatus: 'UNRESOLVED',
      coordinates: null,
    };
  }

  // Normalize representative
  const representative: ReportingRepresentativeRef =
    raw.reportingRepresentative && typeof raw.reportingRepresentative === 'object'
      ? {
          id: String(raw.reportingRepresentative.id ?? DEFAULT_REPORTING_REPRESENTATIVE.id),
          name: String(raw.reportingRepresentative.name ?? DEFAULT_REPORTING_REPRESENTATIVE.name),
          role: String(raw.reportingRepresentative.role ?? DEFAULT_REPORTING_REPRESENTATIVE.role),
          sector: String(raw.reportingRepresentative.sector ?? DEFAULT_REPORTING_REPRESENTATIVE.sector),
        }
      : DEFAULT_REPORTING_REPRESENTATIVE;

  // Normalize structured interpretation
  let structuredInterpretation: MuseSparkInterpretation | null = null;
  if (raw.structuredInterpretation && typeof raw.structuredInterpretation === 'object') {
    const si = raw.structuredInterpretation;
    structuredInterpretation = {
      model: String(si.model ?? 'muse-spark-1.3'),
      summary: String(si.summary ?? raw.citizenDescription ?? ''),
      category: normalizeIssueCategory(si.category ?? confirmedCategory),
      locationDescription: String(si.locationDescription ?? location.description),
      additionalDetails: Array.isArray(si.additionalDetails) ? si.additionalDetails.map(String) : [],
      missingInformation: Array.isArray(si.missingInformation) ? si.missingInformation.map(String) : [],
      suggestedDepartment: String(si.suggestedDepartment ?? getSimulatedDepartment(confirmedCategory)),
      clarificationQuestion: typeof si.clarificationQuestion === 'string' ? si.clarificationQuestion : null,
      isEmergency: typeof si.isEmergency === 'boolean' ? si.isEmergency : undefined,
      interpretedAt: String(si.interpretedAt ?? raw.createdAt ?? new Date().toISOString()),
      provider: String(si.provider ?? 'legacy'),
    };
  }

  const simulatedDestination = String(
    raw.simulatedDestination ??
      structuredInterpretation?.suggestedDepartment ??
      getSimulatedDepartment(confirmedCategory)
  );

  return {
    reportId: String(raw.reportId),
    citizenDescription: String(raw.citizenDescription ?? ''),
    language: raw.language === 'en' ? 'en' : 'es',
    confirmedCategory,
    location,
    reportingRepresentative: representative,
    evidence: raw.evidence ?? null,
    externalAiProcessingConsent: raw.externalAiProcessingConsent === true,
    visualAnalysis: raw.visualAnalysis ?? null,
    structuredInterpretation,
    lifecycleState:
      raw.lifecycleState === 'UNDER_REVIEW' || raw.lifecycleState === 'SIMULATED_ROUTING'
        ? raw.lifecycleState
        : 'RECEIVED',
    simulatedDestination,
    simulationNotice: String(raw.simulationNotice ?? SIMULATED_MUNICIPAL_SERVICE_NOTICE_ES),
    nonEmergencyAssessment: raw.nonEmergencyAssessment ?? undefined,
    createdAt: String(raw.createdAt ?? new Date().toISOString()),
    updatedAt: String(raw.updatedAt ?? raw.createdAt ?? new Date().toISOString()),
  };
}
