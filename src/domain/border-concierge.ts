import {
  BorderCrossingComparison,
  BorderCrossingObservation,
  CANONICAL_CROSSINGS,
  ComparisonStatement,
  formatBorderFetchTime,
  formatCbpObservationTime,
  OFFICIAL_CAMERA_METADATA,
  STANDARD_PEDESTRIAN_LANE,
} from './border-crossing.js';

export type ConciergeIntent =
  | 'paso-del-norte-wait'
  | 'bota-wait'
  | 'compare-crossings'
  | 'freshness-and-updates'
  | 'camera-reference'
  | 'traveler-guidance'
  | 'witness-perspectives'
  | 'general-orientation';

export interface ConciergeProfile {
  readonly id: string;
  readonly name: string;
  readonly avatar: string;
  readonly role: ComparisonStatement;
  readonly badge: ComparisonStatement;
  readonly description: ComparisonStatement;
  readonly suggestedPrompts: Array<{
    readonly id: string;
    readonly intent: ConciergeIntent;
    readonly label: ComparisonStatement;
    readonly prompt: ComparisonStatement;
  }>;
}

export const BORDER_CONCIERGE_PROFILE: ConciergeProfile = {
  id: 'elena-border-concierge',
  name: 'Elena Ramos',
  avatar: '🛂',
  role: {
    en: 'Border Pedestrian Concierge',
    es: 'Concierge de Orientación Peatonal',
  },
  badge: {
    en: 'Grounded Assistant · Official CBP Guidance',
    es: 'Asistente Grounded · Orientación Oficial CBP',
  },
  description: {
    en: 'Official orientation assistant stationed at the Paso del Norte pedestrian crossing. Explains verified CBP border observations, bridge comparisons, data freshness, and official camera sources.',
    es: 'Asistente oficial de orientación en el cruce peatonal Paso del Norte. Explica observaciones verificadas de CBP, comparación entre puentes, frescura de datos y fuentes oficiales de cámaras.',
  },
  suggestedPrompts: [
    {
      id: 'pdn-wait',
      intent: 'paso-del-norte-wait',
      label: { en: 'Paso del Norte wait', es: 'Tiempo Paso del Norte' },
      prompt: {
        en: 'What is the reported pedestrian wait time at Paso del Norte right now?',
        es: '¿Cuál es el tiempo de espera peatonal reportado en Paso del Norte en este momento?',
      },
    },
    {
      id: 'bota-wait',
      intent: 'bota-wait',
      label: { en: 'BOTA wait', es: 'Tiempo Puente Libre' },
      prompt: {
        en: 'What is the reported pedestrian wait time at Bridge of the Americas (BOTA)?',
        es: '¿Cuál es el tiempo de espera peatonal reportado en el Puente de las Américas (BOTA)?',
      },
    },
    {
      id: 'compare-bridges',
      intent: 'compare-crossings',
      label: { en: 'Compare crossings', es: 'Comparar ambos puentes' },
      prompt: {
        en: 'Which crossing has the lower reported pedestrian wait time right now?',
        es: '¿Cuál de los dos puentes tiene menor tiempo de espera peatonal reportado ahorita?',
      },
    },
    {
      id: 'freshness',
      intent: 'freshness-and-updates',
      label: { en: 'Data freshness', es: 'Frescura de datos' },
      prompt: {
        en: 'When was each crossing information last updated by CBP, and is it fresh?',
        es: '¿Cuándo fue la última actualización de CBP para cada puente y qué tan frescos son los datos?',
      },
    },
    {
      id: 'cameras',
      intent: 'camera-reference',
      label: { en: 'Official cameras', es: 'Cámaras en vivo' },
      prompt: {
        en: 'Where can I see the official live bridge camera views?',
        es: '¿Dónde puedo ver las cámaras oficiales de video en vivo de los puentes?',
      },
    },
    {
      id: 'guidance',
      intent: 'traveler-guidance',
      label: { en: 'What to consider?', es: '¿Qué considerar?' },
      prompt: {
        en: 'What should I consider when comparing these pedestrian wait times?',
        es: '¿Qué factores debo considerar al comparar los tiempos de espera peatonales?',
      },
    },
    {
      id: 'witness-views',
      intent: 'witness-perspectives',
      label: { en: 'Queue witness perspectives', es: 'Opinión de la fila' },
      prompt: {
        en: 'What do simulated queue witnesses like Mateo or Sofia think about the crossings?',
        es: '¿Qué opinan testigos simulados en la fila como Mateo o Sofía sobre los cruces?',
      },
    },
  ],
};

/**
 * Classifies user inquiry into one of the discrete Concierge intents.
 */
export function classifyConciergeIntent(message: string): ConciergeIntent {
  const clean = message.trim().toLowerCase();

  // 0. Witness perspectives query (Mateo, Sofia, queue experiences)
  if (
    clean.includes('mateo') ||
    clean.includes('sofia') ||
    clean.includes('sofía') ||
    clean.includes('witness') ||
    clean.includes('testigo') ||
    clean.includes('opinión de la fila') ||
    clean.includes('opinion de la fila') ||
    clean.includes('opinión de los que están en la fila') ||
    clean.includes('gente en la fila') ||
    clean.includes('viajeros en la fila') ||
    clean.includes('estudiante en la fila') ||
    clean.includes('queue witness')
  ) {
    return 'witness-perspectives';
  }

  // 1. Camera queries
  if (
    clean.includes('camera') ||
    clean.includes('cámara') ||
    clean.includes('camara') ||
    clean.includes('video') ||
    clean.includes('live feed') ||
    clean.includes('ver el puente') ||
    clean.includes('view')
  ) {
    return 'camera-reference';
  }

  // 2. Freshness and update timestamp queries
  if (
    clean.includes('fresh') ||
    clean.includes('stale') ||
    clean.includes('updated') ||
    clean.includes('update') ||
    clean.includes('timestamp') ||
    clean.includes('last update') ||
    clean.includes('fresc') ||
    clean.includes('actualiz') ||
    clean.includes('marca de tiempo') ||
    clean.includes('hora del reporte') ||
    clean.includes('vigencia') ||
    clean.includes('desactualiz')
  ) {
    return 'freshness-and-updates';
  }

  // 3. Traveler Guidance / What to consider (checked before compare-crossings because guidance prompts ask 'what to consider when comparing')
  if (
    clean.includes('consider') ||
    clean.includes('advice') ||
    clean.includes('recommend') ||
    clean.includes('tips') ||
    clean.includes('factor') ||
    clean.includes('consejo') ||
    clean.includes('recomendación') ||
    clean.includes('recomendacion') ||
    clean.includes('tomar en cuenta') ||
    clean.includes('en cuenta')
  ) {
    return 'traveler-guidance';
  }

  // 4. Comparison / Which is faster / Lower wait
  if (
    clean.includes('compar') ||
    clean.includes('which is faster') ||
    clean.includes('which crossing') ||
    clean.includes('which bridge') ||
    clean.includes('lower wait') ||
    clean.includes('better option') ||
    clean.includes('faster') ||
    clean.includes('conviene') ||
    clean.includes('de los dos') ||
    clean.includes('dos puentes') ||
    clean.includes('cuál es más rápido') ||
    clean.includes('cual es mas rapido') ||
    clean.includes('más rápido') ||
    clean.includes('mas rapido') ||
    clean.includes('menor espera') ||
    clean.includes('menos espera') ||
    clean.includes('menos fila') ||
    clean.includes('ambos puentes') ||
    clean.includes('both bridges')
  ) {
    return 'compare-crossings';
  }

  // 5. Specific BOTA wait queries
  if (
    clean.includes('bota') ||
    clean.includes('americas') ||
    clean.includes('américas') ||
    clean.includes('puente libre') ||
    clean.includes('córdova') ||
    clean.includes('cordova')
  ) {
    return 'bota-wait';
  }

  // 6. Specific Paso del Norte wait queries
  if (
    clean.includes('paso del norte') ||
    clean.includes('santa fe') ||
    clean.includes('pdn') ||
    clean.includes('downtown bridge') ||
    clean.includes('puente del centro')
  ) {
    return 'paso-del-norte-wait';
  }

  // 7. General wait time query (default to comparison or Paso del Norte depending on phrasing)
  if (
    clean.includes('wait') ||
    clean.includes('delay') ||
    clean.includes('espera') ||
    clean.includes('tardan') ||
    clean.includes('fila')
  ) {
    return 'compare-crossings';
  }

  return 'general-orientation';
}

/**
 * Determines whether an inquiry intent is strictly data-bearing (reporting live CBP measurements,
 * bridge comparisons, or observation freshness).
 * These intents are determined solely by authoritative BorderCrossingComparison records
 * and must never be delegated to language model generation.
 */
export function isFactualBorderIntent(intent: ConciergeIntent): boolean {
  return (
    intent === 'paso-del-norte-wait' ||
    intent === 'bota-wait' ||
    intent === 'compare-crossings' ||
    intent === 'freshness-and-updates'
  );
}

function formatObservationDetail(
  obs: BorderCrossingObservation,
  language: 'en' | 'es'
): string {
  const isEs = language === 'es';
  const cbpTime = formatCbpObservationTime(obs.sourceObservationTime, obs.sourceObservationTimestamp, language);
  const delayStr =
    obs.delayMinutes !== null
      ? `${obs.delayMinutes} ${isEs ? 'minutos' : 'minutes'}`
      : isEs
      ? 'No disponible'
      : 'Unavailable';

  if (obs.portStatus === 'Closed') {
    return isEs
      ? `${obs.canonicalName}: El puerto está reportado como CERRADO por CBP (hora de observación CBP: ${cbpTime}). No se le asigna tiempo de espera de cero minutos.`
      : `${obs.canonicalName}: The crossing is reported as CLOSED by CBP (CBP observation time: ${cbpTime}). It is not assigned a zero-minute wait time.`;
  }

  if (obs.freshness === 'unavailable' || obs.freshness === 'pending') {
    return isEs
      ? `${obs.canonicalName}: Información de espera ${obs.freshness === 'pending' ? 'pendiente de actualización' : 'no disponible'} en el reporte de CBP (hora de observación CBP: ${cbpTime}).`
      : `${obs.canonicalName}: Wait information is ${obs.freshness === 'pending' ? 'pending update' : 'unavailable'} in CBP records (CBP observation time: ${cbpTime}).`;
  }

  return isEs
    ? `${obs.canonicalName}: ${delayStr} reportados para el carril peatonal Estándar/General. Hora de observación CBP: ${cbpTime} (estado de frescura: ${obs.freshness}).`
    : `${obs.canonicalName}: ${delayStr} reported for the Standard / General pedestrian lane. CBP observation time: ${cbpTime} (freshness status: ${obs.freshness}).`;
}

/**
 * Generates an authoritative, deterministic response strictly grounded in
 * verified BorderCrossingComparison data.
 */
export function formatDeterministicConciergeResponse(
  intent: ConciergeIntent,
  comparison: BorderCrossingComparison,
  language: 'en' | 'es'
): string {
  const isEs = language === 'es';
  const { pasoDelNorte, bridgeOfTheAmericas } = comparison.observations;
  const pdnMeta = CANONICAL_CROSSINGS['paso-del-norte'];
  const botaMeta = CANONICAL_CROSSINGS['bridge-of-the-americas'];

  switch (intent) {
    case 'paso-del-norte-wait': {
      const detail = formatObservationDetail(pasoDelNorte, language);
      const fetchTimeStr = formatBorderFetchTime(pasoDelNorte.fetchedAt, language);
      return isEs
        ? `Información oficial de CBP para Paso del Norte:\n${detail}\nDatos consultados por la aplicación a las ${fetchTimeStr}. Consulte los detalles oficiales de CBP en: ${pdnMeta.sourceDetailUrl}`
        : `Official CBP observation for Paso del Norte:\n${detail}\nFetched by the application at ${fetchTimeStr}. View official CBP details at: ${pdnMeta.sourceDetailUrl}`;
    }

    case 'bota-wait': {
      const detail = formatObservationDetail(bridgeOfTheAmericas, language);
      const fetchTimeStr = formatBorderFetchTime(bridgeOfTheAmericas.fetchedAt, language);
      return isEs
        ? `Información oficial de CBP para Puente de las Américas (BOTA):\n${detail}\nDatos consultados por la aplicación a las ${fetchTimeStr}. Consulte los detalles oficiales de CBP en: ${botaMeta.sourceDetailUrl}`
        : `Official CBP observation for Bridge of the Americas (BOTA):\n${detail}\nFetched by the application at ${fetchTimeStr}. View official CBP details at: ${botaMeta.sourceDetailUrl}`;
    }

    case 'compare-crossings': {
      if (comparison.comparison.canCompare) {
        const stmt = isEs ? comparison.comparison.statement.es : comparison.comparison.statement.en;
        const pdnTime = formatCbpObservationTime(pasoDelNorte.sourceObservationTime, pasoDelNorte.sourceObservationTimestamp, language);
        const botaTime = formatCbpObservationTime(bridgeOfTheAmericas.sourceObservationTime, bridgeOfTheAmericas.sourceObservationTimestamp, language);

        const contextNote = isEs
          ? `Tiempos de observación CBP: Paso del Norte a las ${pdnTime}, BOTA a las ${botaTime}. Esta comparación aplica exclusivamente al carril peatonal Estándar / General. Tenga en cuenta que los tiempos reportados son una instantánea histórica y no garantizan un viaje más rápido ni el tiempo exacto al cruzar; las condiciones de flujo pueden variar.`
          : `CBP observation timestamps: Paso del Norte at ${pdnTime}, BOTA at ${botaTime}. This comparison applies strictly to the Standard / General pedestrian lane. Note: Reported wait times are point-in-time snapshots and do not guarantee a faster end-to-end journey or immediate processing; crossing conditions fluctuate.`;

        return `${stmt}\n\n${contextNote}`;
      } else {
        const reason = comparison.comparison.reasonUnavailable
          ? isEs
            ? comparison.comparison.reasonUnavailable.es
            : comparison.comparison.reasonUnavailable.en
          : isEs
          ? 'Uno o ambos cruces carecen de observaciones numéricas vigentes o se encuentran cerrados.'
          : 'One or both crossings lack fresh numeric observations or are closed.';

        return isEs
          ? `No es posible realizar una comparación válida entre ambos puentes en este momento. Motivo: ${reason}. Por favor revise el estado individual de cada cruce en el tablero superior.`
          : `A valid side-by-side comparison cannot be computed at this time. Reason: ${reason}. Please inspect individual crossing statuses on the panel above.`;
      }
    }

    case 'freshness-and-updates': {
      const pdnTime = formatCbpObservationTime(pasoDelNorte.sourceObservationTime, pasoDelNorte.sourceObservationTimestamp, language);
      const botaTime = formatCbpObservationTime(bridgeOfTheAmericas.sourceObservationTime, bridgeOfTheAmericas.sourceObservationTimestamp, language);
      const fetchTime = formatBorderFetchTime(comparison.fetchedAt, language);

      return isEs
        ? `Estado de frescura de las observaciones de CBP:\n` +
            `• Paso del Norte: Última actualización de CBP: ${pdnTime} | Frescura: ${pasoDelNorte.freshness} (${pasoDelNorte.portStatus}).\n` +
            `• Puente de las Américas: Última actualización de CBP: ${botaTime} | Frescura: ${bridgeOfTheAmericas.freshness} (${bridgeOfTheAmericas.portStatus}).\n` +
            `La aplicación consultó ambos registros a las ${fetchTime}. Los datos se clasifican como frescos si tienen menos de 60 minutos de antigüedad.`
        : `CBP observation freshness status:\n` +
            `• Paso del Norte: Last updated by CBP at ${pdnTime} | Freshness: ${pasoDelNorte.freshness} (${pasoDelNorte.portStatus}).\n` +
            `• Bridge of the Americas: Last updated by CBP at ${botaTime} | Freshness: ${bridgeOfTheAmericas.freshness} (${bridgeOfTheAmericas.portStatus}).\n` +
            `Both observations were fetched by the application at ${fetchTime}. Observations are classified as fresh if under 60 minutes old.`;
    }

    case 'camera-reference': {
      const cameraUrl = OFFICIAL_CAMERA_METADATA.officialAuthorityUrl;
      const desc = isEs ? OFFICIAL_CAMERA_METADATA.description.es : OFFICIAL_CAMERA_METADATA.description.en;

      return isEs
        ? `Puede consultar las transmisiones de video oficiales en el portal del Fideicomiso de Puentes Fronterizos de Chihuahua: ${cameraUrl}\n\n` +
            `Aviso importante: ${desc} Como concierge, le indico dónde encontrar estas transmisiones públicas, pero no realizo inspección visual automatizada de video ni calculo longitud de filas a partir de imágenes de cámara.`
        : `You can view the official live reference cameras on the Chihuahua Bridge Authority portal: ${cameraUrl}\n\n` +
            `Important Notice: ${desc} As a concierge, I guide travelers to these public video streams, but I do not perform automated video analysis, inspect camera feeds, or calculate queue density from camera images.`;
    }

    case 'traveler-guidance': {
      return isEs
        ? `Factores clave para peatones al comparar Paso del Norte y BOTA:\n` +
            `1. Destino final: Paso del Norte desemboca directamente en el centro de El Paso (El Paso St), ideal para peatones. BOTA desemboca en el área de Chamizal/I-110, requiriendo transporte adicional.\n` +
            `2. Instantáneas vs. condiciones en vivo: Los tiempos de CBP son registros de momento; las filas pueden acelerarse o detenerse según el número de casetas de inspección abiertas.\n` +
            `3. Carril analizado: Esta información cubre únicamente el carril peatonal Estándar/General, excluyendo Ready Lane o SENTRI.\n` +
            `4. Verificación visual: Siempre puede revisar las cámaras del Fideicomiso (${OFFICIAL_CAMERA_METADATA.officialAuthorityUrl}) antes de elegir su cruce.`
        : `Key factors for pedestrian travelers comparing Paso del Norte and BOTA:\n` +
            `1. Final Destination: Paso del Norte exits directly into Downtown El Paso (El Paso St), optimal for walking commuters. BOTA exits near Chamizal/I-110, requiring additional bus or rideshare transit.\n` +
            `2. Snapshots vs. Dynamic Flow: Reported CBP times are periodic snapshots; queue movement can shift as inspection booths open or close.\n` +
            `3. Lane Category: This comparison applies exclusively to the Standard / General pedestrian lane, not Ready Lane or SENTRI.\n` +
            `4. Visual Verification: You can always verify approach queues via the official Chihuahua Bridge Authority cameras (${OFFICIAL_CAMERA_METADATA.officialAuthorityUrl}) before departing.`;
    }

    case 'witness-perspectives': {
      const disclaimer = isEs
        ? 'Perspectiva de testigo simulado ficticio — no es una medición oficial.'
        : 'Fictional simulated witness perspective — not an official measurement.';

      return isEs
        ? `[${disclaimer}]\n` +
            `En la fila peatonal contigua se encuentran Mateo Navarro (estudiante de UTEP) y Sofía Alarcón (supervisora comercial):\n` +
            `• Mateo cruza a pie por Paso del Norte porque desemboca directo al centro de El Paso para tomar la Ruta 10 de Sun Metro a UTEP; no caminaría por BOTA sin vehículo.\n` +
            `• Sofía cruza por Paso del Norte debido a la cercanía con su lugar de trabajo, aunque considera BOTA cuando le ofrecen transporte en automóvil.\n\n` +
            `Aviso: Sus testimonios son experiencias subjetivas simuladas y no constituyen mediciones oficiales ni en tiempo real. Para tomar su decisión, básese en los tiempos oficiales de CBP reportados arriba.`
        : `[${disclaimer}]\n` +
            `Stationed in the adjacent pedestrian queue are Mateo Navarro (UTEP student) and Sofia Alarcón (retail supervisor):\n` +
            `• Mateo crosses on foot at Paso del Norte because it connects straight to Downtown El Paso for the Sun Metro Route 10 bus to UTEP; he would not walk BOTA without a vehicle.\n` +
            `• Sofia crosses at Paso del Norte due to proximity to her job, though she considers BOTA when a carpool is available.\n\n` +
            `Notice: Their statements are simulated fictional perspectives, not official or live measurements. For your crossing decision, rely on the official CBP wait times reported above.`;
    }

    case 'general-orientation':
    default: {
      const pdnWait = pasoDelNorte.delayMinutes !== null ? `${pasoDelNorte.delayMinutes} min` : 'N/A';
      const botaWait = bridgeOfTheAmericas.delayMinutes !== null ? `${bridgeOfTheAmericas.delayMinutes} min` : 'N/A';

      return isEs
        ? `¡Hola! Soy Elena Ramos, Concierge de Orientación Peatonal. Estoy aquí para ayudarle a entender los reportes oficiales de CBP para los cruces peatonales hacia El Paso. Actualmente Paso del Norte reporta ${pdnWait} (${pasoDelNorte.freshness}) y BOTA reporta ${botaWait} (${bridgeOfTheAmericas.freshness}). ¿Desea comparar los puentes, revisar la frescura de los datos o consultar los enlaces de cámaras oficiales?`
        : `Hello! I am Elena Ramos, your Border Pedestrian Concierge. I am here to help you navigate and interpret official CBP border observations between Ciudad Juárez and El Paso. Currently, Paso del Norte reports ${pdnWait} (${pasoDelNorte.freshness}) and BOTA reports ${botaWait} (${bridgeOfTheAmericas.freshness}). Would you like to compare the crossings, check observation freshness, or view official live camera links?`;
    }
  }
}

/**
 * Constructs prompt instructions for Meta Muse Spark dialogue generation.
 * Bounded strictly to authoritative normalized facts with zero authority to invent numbers.
 */
export function buildConciergePrompt(
  message: string,
  intent: ConciergeIntent,
  comparison: BorderCrossingComparison,
  language: 'en' | 'es'
): { systemPrompt: string; userContent: string } {
  const isEs = language === 'es';
  const { pasoDelNorte, bridgeOfTheAmericas } = comparison.observations;

  const systemPrompt = isEs
    ? `Eres Elena Ramos, Concierge de Orientación Peatonal en el cruce de Paso del Norte entre Ciudad Juárez y El Paso.
IDENTIDAD: Eres una asistente interactiva oficial de orientación para viajeros, basada ESTRICTAMENTE en datos oficiales de U.S. Customs and Border Protection (CBP).
REGLAS ESTRICTAS DE AUTORIDAD:
1. Responde de manera concisa (2 a 4 frases), en tono amable, profesional y claro en español.
2. NUNCA alteres, inventes ni sustituyas las mediciones numéricas, marcas de tiempo ni el estado de los puertos proporcionados en los DATOS VERIFICADOS.
3. Si un puerto está cerrado, NUNCA digas que tiene 0 minutos de espera; indica claramente que está CERRADO.
4. Mantén siempre clara la diferencia entre la hora de observación reportada por CBP y la hora en que la aplicación consultó los datos. Comunica la hora de consulta siempre en hora local de Ciudad Juárez (America/Ciudad_Juarez), nunca en UTC sin convertir.
5. Si comparas ambos cruces, recuerda al usuario que los tiempos reportados son instantáneas y no garantizan un viaje más rápido ni el tiempo exacto en caseta.
6. NO afirmes haber mirado cámaras en vivo, medido video ni inspeccionado imágenes de cámaras; solo puedes proporcionar el enlace público de cámaras del Fideicomiso.
7. Responde enteramente en español.`
    : `You are Elena Ramos, the Border Pedestrian Concierge stationed at the Paso del Norte crossing between Ciudad Juárez and El Paso.
IDENTITY: You are an official interactive traveler guidance assistant grounded STRICTLY in verified U.S. Customs and Border Protection (CBP) data.
STRICT AUTHORITY BOUNDARIES:
1. Respond concisely (2 to 4 sentences) in a courteous, professional, and clear tone in English.
2. NEVER alter, invent, or substitute numeric measurements, timestamps, or port operational statuses provided in the VERIFIED DATA.
3. If a crossing is closed, NEVER state that it has a 0-minute wait; state clearly that it is CLOSED.
4. Always preserve the distinction between CBP's observation timestamp and the application fetch time. Always communicate the application fetch time in local Ciudad Juárez time (America/Ciudad_Juarez), never in raw UTC.
5. If comparing crossings, remind the traveler that reported wait times are snapshots and do not guarantee a faster end-to-end trip or processing speed.
6. DO NOT claim to have inspected camera video, analyzed feeds, or measured queues from video; only direct travelers to the official Chihuahua Bridge Authority camera portal.
7. Respond entirely in English.`;

  const verifiedDataSummary = [
    `Lane Category: Standard / General Pedestrian (${STANDARD_PEDESTRIAN_LANE.name.en})`,
    `Paso del Norte (240202): Wait=${pasoDelNorte.delayMinutes !== null ? `${pasoDelNorte.delayMinutes} min` : 'Unavailable'}, Status=${pasoDelNorte.portStatus}, Freshness=${pasoDelNorte.freshness}, CBP Time=${formatCbpObservationTime(pasoDelNorte.sourceObservationTime, pasoDelNorte.sourceObservationTimestamp, 'en')}, Fetched=${formatBorderFetchTime(pasoDelNorte.fetchedAt, 'en')}`,
    `Bridge of the Americas (240201): Wait=${bridgeOfTheAmericas.delayMinutes !== null ? `${bridgeOfTheAmericas.delayMinutes} min` : 'Unavailable'}, Status=${bridgeOfTheAmericas.portStatus}, Freshness=${bridgeOfTheAmericas.freshness}, CBP Time=${formatCbpObservationTime(bridgeOfTheAmericas.sourceObservationTime, bridgeOfTheAmericas.sourceObservationTimestamp, 'en')}, Fetched=${formatBorderFetchTime(bridgeOfTheAmericas.fetchedAt, 'en')}`,
    `Comparison Eligible: ${comparison.comparison.canCompare ? 'YES' : 'NO'} (${comparison.comparison.statement.en})`,
    `Official Cameras URL: ${OFFICIAL_CAMERA_METADATA.officialAuthorityUrl}`,
  ].join('\n');

  const userContent = [
    `TRAVELER QUESTION: "${message}"`,
    `INTENT: ${intent}`,
    '',
    '--- AUTHORITATIVE BORDER OBSERVATIONS (GROUND TRUTH) ---',
    verifiedDataSummary,
    '-------------------------------------------------------',
    '',
    isEs
      ? 'Por favor responde al viajero como Elena Ramos, orientándolo con base exclusiva en los datos oficiales de arriba.'
      : 'Please formulate your response to the traveler as Elena Ramos, grounded exclusively in the official data above.',
  ].join('\n');

  return { systemPrompt, userContent };
}

/**
 * Validates that an AI-generated model response does not introduce fabricated
 * numeric wait measurements or unsupported comparison conclusions.
 *
 * Enforces:
 * 1. Any numeric minute claim (\d+ min / \d+ minutes) must match either:
 *    - pasoDelNorte.delayMinutes
 *    - bridgeOfTheAmericas.delayMinutes
 *    - comparison.differenceMinutes
 *    - 60 (freshness threshold 60 min)
 * 2. If comparison.canCompare is false, the model must NOT claim that one crossing is faster.
 * 3. If a port is closed, the model must NOT claim 0 minutes wait.
 */
// Written-out duration words mapped to minutes
const WRITTEN_NUMBER_MINUTES: Record<string, number> = {
  one: 1, uno: 1, una: 1,
  two: 2, dos: 2,
  three: 3, tres: 3,
  four: 4, cuatro: 4,
  five: 5, cinco: 5,
  six: 6, seis: 6,
  seven: 7, siete: 7,
  eight: 8, ocho: 8,
  nine: 9, nueve: 9,
  ten: 10, diez: 10,
  eleven: 11, once: 11,
  twelve: 12, doce: 12,
  thirteen: 13, trece: 13,
  fourteen: 14, catorce: 14,
  fifteen: 15, quince: 15,
  twenty: 20, veinte: 20,
  'twenty-five': 25, veinticinco: 25,
  thirty: 30, treinta: 30,
  'thirty-five': 35, 'treinta y cinco': 35,
  forty: 40, cuarenta: 40,
  'forty-five': 45, 'cuarenta y cinco': 45,
  fifty: 50, cincuenta: 50,
  'fifty-five': 55, 'cincuenta y cinco': 55,
  sixty: 60, sesenta: 60,
};

/**
 * Factual Integrity Guard for Border Concierge.
 *
 * Verifies that candidate natural language output strictly respects:
 * 1. Eligibility constraints: If comparison.canCompare is false, no comparative speed,
 *    wait, congestion, or preference claims may be made.
 * 2. Directional correctness: If comparison.canCompare is true, candidate cannot contradict
 *    the authoritative lowerWaitCrossingId.
 * 3. Data Association: Values for Paso del Norte and BOTA cannot be cross-swapped.
 * 4. Grounded numerical metrics: Numeric wait values, unitless durations in wait contexts,
 *    hour-based assertions, written-out numbers, and ranges must strictly match
 *    the authoritative values (pasoDelNorte.delayMinutes, bridgeOfTheAmericas.delayMinutes,
 *    comparison.differenceMinutes, or 60m freshness policy).
 * 5. Closed port semantics: A closed port can NEVER be claimed as 0 minutes wait.
 * 6. Synthetic witness boundaries: Model cannot introduce non-canonical simulated witnesses.
 */
export function validateConciergeFactualIntegrity(
  text: string,
  comparison: BorderCrossingComparison,
  intent?: ConciergeIntent
): boolean {
  if (!text || typeof text !== 'string') return false;

  const { pasoDelNorte, bridgeOfTheAmericas } = comparison.observations;
  const comp = comparison.comparison;

  // 1. Ineligible Comparison Checks (canCompare === false)
  if (!comp.canCompare) {
    const lower = text.toLowerCase();
    const forbiddenClaims = [
      // Speed & wait comparisons
      'es más rápido',
      'es mas rapido',
      'is faster',
      'faster crossing',
      'has a shorter wait',
      'shorter wait',
      'tiene menor espera',
      'menor espera',
      'menos espera',
      'menor tiempo de espera',
      'menos tiempo',
      'tarda menos',
      'tardarás menos',
      'tardaras menos',
      'is slower',
      'es más lento',
      'es mas lento',
      'más lento',
      'mas lento',
      'más tardado',
      'mas tardado',
      'longer wait',
      'more wait',
      'mayor espera',
      // Congestion comparisons
      'less congested',
      'more congested',
      'menos congestionado',
      'más congestionado',
      'mas congestionado',
      'más despejado',
      'mas despejado',
      'menos fila',
      'más fila',
      'mas fila',
      // Preference & recommendation
      'better option',
      'mejor opción',
      'mejor opcion',
      'preferable',
      'preferible',
      'conviene más',
      'conviene mas',
      'conviene cruzar',
      'recomiendo cruzar',
      'recommend crossing',
      'better to cross',
    ];
    for (const claim of forbiddenClaims) {
      if (lower.includes(claim)) {
        return false;
      }
    }
  }

  // 2. Contradictory Comparative Claim when canCompare === true
  if (comp.canCompare && comp.lowerWaitCrossingId) {
    const lower = text.toLowerCase();
    if (comp.lowerWaitCrossingId === 'paso-del-norte') {
      // PDN is faster/lower; BOTA cannot be claimed faster/lower
      const botaClaims = [
        'bota es más rápido',
        'bota es mas rapido',
        'bota is faster',
        'puente libre es más rápido',
        'puente de las américas es más rápido',
        'bridge of the americas is faster',
        'bota has a shorter wait',
        'bota tiene menor espera',
        'conviene más cruzar por bota',
        'recomiendo bota',
      ];
      for (const claim of botaClaims) {
        if (lower.includes(claim)) return false;
      }
    } else if (comp.lowerWaitCrossingId === 'bridge-of-the-americas') {
      // BOTA is faster/lower; PDN cannot be claimed faster/lower
      const pdnClaims = [
        'paso del norte es más rápido',
        'paso del norte es mas rapido',
        'paso del norte is faster',
        'santa fe es más rápido',
        'santa fe is faster',
        'paso del norte has a shorter wait',
        'paso del norte tiene menor espera',
        'conviene más cruzar por paso del norte',
        'recomiendo paso del norte',
      ];
      for (const claim of pdnClaims) {
        if (lower.includes(claim)) return false;
      }
    }
  }

  // 3. Check 0-min Claim for Closed Ports or Missing Observations
  if (pasoDelNorte.portStatus === 'Closed' || bridgeOfTheAmericas.portStatus === 'Closed') {
    const lower = text.toLowerCase();
    if (
      lower.includes('0 min') ||
      lower.includes('cero min') ||
      lower.includes('zero min') ||
      lower.includes('0 minutos') ||
      lower.includes('zero minutes') ||
      lower.includes('cero minutos')
    ) {
      return false;
    }
    if (/\b(?:wait(?:\s+time)?|delay|demora|espera)\s+(?:is|of|de|es)?\s*(?:0|zero|cero)\b/i.test(text)) {
      return false;
    }
  }

  // 4. Data Association: Association between Crossing and Observed Delay
  if (
    pasoDelNorte.delayMinutes !== null &&
    bridgeOfTheAmericas.delayMinutes !== null &&
    pasoDelNorte.delayMinutes !== bridgeOfTheAmericas.delayMinutes
  ) {
    const pdnDelay = pasoDelNorte.delayMinutes;
    const botaDelay = bridgeOfTheAmericas.delayMinutes;

    // Split text into clauses / sub-sentences to isolate subject-predicate associations
    const clauses = text.split(/(?:[.!?;:\n]|(?:,\s*)?\b(?:while|mientras\s+que|mientras|pero|but|whereas)\b)/i);
    for (const rawClause of clauses) {
      const clause = rawClause.trim();
      if (!clause) continue;

      const mentionsPdn = /\b(?:paso\s+del\s+norte|santa\s+fe|pdn)\b/i.test(clause);
      const mentionsBota = /\b(?:bridge\s+of\s+the\s+americas|bota|puente\s+libre|c[oó]rdova|cordova)\b/i.test(clause);

      // Clause specifically discusses PDN without BOTA
      if (mentionsPdn && !mentionsBota) {
        const assertsBotaDelay =
          new RegExp(`\\b${botaDelay}\\s*(?:minutos?|minutes?|mins?|min)?\\b`, 'i').test(clause) &&
          /\b(?:wait|delay|espera|tarda|taking|takes|report|demora|is|es|de)\b/i.test(clause);
        const assertsPdnDelay =
          new RegExp(`\\b${pdnDelay}\\s*(?:minutos?|minutes?|mins?|min)?\\b`, 'i').test(clause);

        if (assertsBotaDelay && !assertsPdnDelay) {
          // Erroneously attributed BOTA's delay to Paso del Norte!
          return false;
        }
      }

      // Clause specifically discusses BOTA without PDN
      if (mentionsBota && !mentionsPdn) {
        const assertsPdnDelay =
          new RegExp(`\\b${pdnDelay}\\s*(?:minutos?|minutes?|mins?|min)?\\b`, 'i').test(clause) &&
          /\b(?:wait|delay|espera|tarda|taking|takes|report|demora|is|es|de)\b/i.test(clause);
        const assertsBotaDelay =
          new RegExp(`\\b${botaDelay}\\s*(?:minutos?|minutes?|mins?|min)?\\b`, 'i').test(clause);

        if (assertsPdnDelay && !assertsBotaDelay) {
          // Erroneously attributed Paso del Norte's delay to BOTA!
          return false;
        }
      }
    }
  }

  // 5. Numerical and Duration Verification
  const validMinutes = new Set<number>();
  if (pasoDelNorte.delayMinutes !== null) validMinutes.add(pasoDelNorte.delayMinutes);
  if (bridgeOfTheAmericas.delayMinutes !== null) validMinutes.add(bridgeOfTheAmericas.delayMinutes);
  if (comp.differenceMinutes !== null) validMinutes.add(comp.differenceMinutes);
  validMinutes.add(60); // Permitted freshness policy window ("< 60 minutes")

  // 5a. Ranges or approximate durations (CBP reports exact snapshots, never ranges)
  if (
    /\b\d+\s*(?:-|to|a)\s*\d+\s*(?:minutos?|minutes?|mins?|min)\b/i.test(text) ||
    /\b(?:between|entre)\s+\d+\s+(?:and|y)\s+\d+\s*(?:minutos?|minutes?|mins?|min)\b/i.test(text)
  ) {
    return false;
  }

  // 5b. Hour-based duration assertions in wait/crossing contexts
  const hourWaitPattern = /\b(?:wait(?:\s+time)?|delay|demora|espera|tarda(?:r[aá])?|taking|takes)\b[^\n.]{0,35}\b(\d+(?:\.\d+)?)\s*(?:hours?|horas?|hrs?)\b/i;
  const waitHourPattern = /\b(\d+(?:\.\d+)?)\s*(?:hours?|horas?|hrs?)\s*(?:of\s+wait|de\s+espera|delay|demora)\b/i;
  if (hourWaitPattern.test(text) || waitHourPattern.test(text)) {
    return false;
  }

  // 5c. Written-out duration words
  const writtenPattern = /\b(one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve|thirteen|fourteen|fifteen|twenty(?:-five)?|thirty(?:-five)?|forty(?:-five)?|fifty(?:-five)?|sixty|uno|dos|tres|cuatro|cinco|seis|siete|ocho|nueve|diez|once|doce|trece|catorce|quince|veinte|veinticinco|treinta(?: y cinco)?|cuarenta(?: y cinco)?|cincuenta(?: y cinco)?|sesenta)\s+(minutes?|minutos?|hours?|horas?)\b/gi;
  let writtenMatch: RegExpExecArray | null;
  while ((writtenMatch = writtenPattern.exec(text)) !== null) {
    if (writtenMatch[1] && writtenMatch[2]) {
      const word = writtenMatch[1].toLowerCase();
      const unit = writtenMatch[2].toLowerCase();
      if (unit.startsWith('hour') || unit.startsWith('hora')) {
        return false;
      }
      const mapped = WRITTEN_NUMBER_MINUTES[word];
      if (mapped !== undefined && !validMinutes.has(mapped)) {
        return false;
      }
    }
  }

  // 5d. Explicit numeric minute assertions
  const minutePattern = /\b(\d+)\s*(?:minutos?|minutes?|mins?|min)\b/gi;
  let match: RegExpExecArray | null;
  while ((match = minutePattern.exec(text)) !== null) {
    if (match[1]) {
      const num = parseInt(match[1], 10);
      if (!validMinutes.has(num)) {
        return false;
      }
    }
  }

  // 5e. Unitless numbers directly following wait/delay assertions
  const unitlessWaitPattern = /\b(?:wait(?:\s+time)?|delay|queue|demora|espera|tarda(?:r[aá])?|taking|takes|report(?:s|a)?)\s+(?:of|is|at|de|es(?:\s+de)?|alrededor\s+de|aproximadamente)?\s*(\d+)\b/gi;
  let unitlessMatch: RegExpExecArray | null;
  while ((unitlessMatch = unitlessWaitPattern.exec(text)) !== null) {
    if (unitlessMatch[1]) {
      const num = parseInt(unitlessMatch[1], 10);
      if (!validMinutes.has(num)) {
        return false;
      }
    }
  }

  // 6. Synthetic Witness Canonical Boundaries
  if (intent === 'witness-perspectives' || /\b(?:testigo|witness)\b/i.test(text)) {
    // Detect fabricated witness names introduced as queue witnesses
    const uncanonicalWitnessPattern = /\b(?:testigo|witness|viajero simulado|simulated traveler)\s+([A-ZÁÉÍÓÚ][a-záéíóú]+)\b/g;
    let witnessMatch: RegExpExecArray | null;
    while ((witnessMatch = uncanonicalWitnessPattern.exec(text)) !== null) {
      const name = witnessMatch[1];
      const canonicalNames = ['Mateo', 'Sofia', 'Sofía', 'Héctor', 'Hector', 'Don'];
      if (name && !canonicalNames.includes(name)) {
        return false;
      }
    }
  }

  return true;
}
