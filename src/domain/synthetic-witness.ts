/**
 * Domain Models, Profiles, and Constraints for Synthetic Community Witnesses.
 *
 * Location: Simulated pedestrian queue at Paso del Norte / Santa Fe International Bridge
 * (infrastructure:puente-paso-del-norte).
 *
 * TRUTH & PROVENANCE INVARIANTS:
 * - These NPCs are explicitly synthetic simulations; they are NOT real people.
 * - Their observations are fictional scenario experiences, NOT live measurements.
 * - Their statements must NEVER be confused with official CBP wait-time observations.
 * - NPCs cannot modify simulation state, business schedules, or CBP metrics.
 */

export type SyntheticWitnessId = 'mateo-student' | 'sofia-cross-border-retail' | 'hector-artisan';

export interface BilingualText {
  readonly en: string;
  readonly es: string;
}

export interface SyntheticProvenance {
  readonly type: 'synthetic-witness-simulation';
  readonly marker: BilingualText;
  readonly scenarioId: string;
  readonly locationId: string;
  readonly provider: 'meta-muse-spark-1.3' | 'deterministic-fallback' | 'mock-meta';
  readonly officialDataNotice: BilingualText;
}

export interface SyntheticWitnessProfile {
  readonly id: SyntheticWitnessId;
  readonly name: string;
  readonly avatar: string;
  readonly age: number;
  readonly role: BilingualText;
  readonly background: BilingualText;
  readonly crossingReason: BilingualText;
  readonly communicationStyle: BilingualText;
  readonly permittedKnowledge: {
    readonly personalWaitMinutes: number;
    readonly arrivalScenario: BilingualText;
    readonly lineMovementPerception: BilingualText;
    readonly whyPasoDelNorte: BilingualText;
    readonly botaPerspective: BilingualText;
    readonly outOfScopeKnowledge: BilingualText;
  };
  readonly suggestedPrompts: Array<{
    readonly id: string;
    readonly label: BilingualText;
    readonly prompt: BilingualText;
  }>;
}

export const SYNTHETIC_MARKER: BilingualText = {
  en: 'Synthetic witness · Simulation',
  es: 'Testigo sintético · Simulación',
};

export const SYNTHETIC_DISCLOSURE: BilingualText = {
  en: 'Fictional scenario witness. Not an actual traveler. Statements are simulated testimony separate from official CBP bridge measurements.',
  es: 'Testigo sintético de escenario ficticio. No es un viajero real. Las declaraciones son testimonios simulados independientes de las mediciones oficiales de CBP.',
};

export const OFFICIAL_DATA_NOTICE: BilingualText = {
  en: 'Simulated witness perspective only. For verified real-time bridge wait times, consult the official CBP measurements displayed above.',
  es: 'Perspectiva de testigo simulado únicamente. Para tiempos de espera oficiales y verificados, consulte las mediciones de CBP mostradas arriba.',
};

/**
 * Three distinct, stable, culturally grounded synthetic NPC witnesses
 * situated in the simulated Paso del Norte pedestrian queue.
 */
export const SYNTHETIC_WITNESS_ROSTER: Record<SyntheticWitnessId, SyntheticWitnessProfile> = {
  'mateo-student': {
    id: 'mateo-student',
    name: 'Mateo Navarro',
    avatar: '🎓',
    age: 21,
    role: {
      en: 'UTEP Engineering Student',
      es: 'Estudiante de Ingeniería en UTEP',
    },
    background: {
      en: 'Undergraduate student at UTEP living in Ciudad Juárez. Commutes several times a week across Paso del Norte with his backpack and laptop.',
      es: 'Estudiante de ingeniería en UTEP que vive en Ciudad Juárez. Cruza varias veces por semana por Paso del Norte con su mochila y laptop.',
    },
    crossingReason: {
      en: 'Heading to an evening robotics study group in the engineering building on UTEP campus.',
      es: 'Va a una sesión vespertina de estudio de robótica en la facultad de ingeniería de UTEP.',
    },
    communicationStyle: {
      en: 'Energetic, polite, tech-oriented, and concise.',
      es: 'Enérgico, educado, enfocado en tecnología y conciso.',
    },
    permittedKnowledge: {
      personalWaitMinutes: 25,
      arrivalScenario: {
        en: 'Arrived at the turnstiles around 25 minutes ago. Checked his watch twice while reviewing course notes on his phone.',
        es: 'Llegó a los torniquetes hace unos 25 minutos. Ha mirado su reloj un par de veces mientras revisa apuntes en su teléfono.',
      },
      lineMovementPerception: {
        en: 'The queue feels moderately steady; it moved forward about four paces a few minutes ago when an extra booth opened.',
        es: 'La fila se siente moderadamente constante; avanzó unos cuatro pasos hace unos minutos cuando abrieron una caseta extra.',
      },
      whyPasoDelNorte: {
        en: 'Crosses on foot here because Paso del Norte leads straight into Downtown El Paso, where he catches the Sun Metro Route 10 bus directly to UTEP.',
        es: 'Cruza a pie por aquí porque Paso del Norte desemboca directamente en el centro de El Paso, donde toma el autobús Sun Metro Ruta 10 directo a UTEP.',
      },
      botaPerspective: {
        en: 'BOTA (Bridge of the Americas) is way too far east for walking commuters without a vehicle. He would never walk BOTA unless someone gave him a ride.',
        es: 'El Puente de las Américas (BOTA) queda demasiado lejos al oriente para cruzar a pie sin auto. Jamás cruzaría por allá caminando a menos que alguien le dé un aventón.',
      },
      outOfScopeKnowledge: {
        en: 'Does not know commercial cargo operations, vehicular lane wait formulas, or real-time surveillance camera statistics. Directs questions about official delays to the live CBP board.',
        es: 'No conoce operaciones de carga comercial, fórmulas de carriles vehiculares ni estadísticas de cámaras en vivo. Dirige las dudas sobre demoras oficiales al tablero de CBP.',
      },
    },
    suggestedPrompts: [
      {
        id: 'wait-time',
        label: { en: 'How long waiting?', es: '¿Cuánto tiempo llevas?' },
        prompt: {
          en: 'How long have you been waiting in this line?',
          es: '¿Cuánto tiempo llevas esperando en esta fila?',
        },
      },
      {
        id: 'pace',
        label: { en: 'Does line feel slow?', es: '¿Avanza lento?' },
        prompt: {
          en: 'Does the line feel slow this evening?',
          es: '¿Sientes que la fila avanza lento esta tarde?',
        },
      },
      {
        id: 'reason',
        label: { en: 'Why crossing?', es: '¿Por qué cruzas?' },
        prompt: {
          en: 'Why are you crossing to El Paso today?',
          es: '¿Por qué estás cruzando a El Paso hoy?',
        },
      },
      {
        id: 'bota',
        label: { en: 'Consider BOTA?', es: '¿Considerarías BOTA?' },
        prompt: {
          en: 'Would you consider using Bridge of the Americas instead?',
          es: '¿Considerarías cruzar por el Puente de las Américas (BOTA)?',
        },
      },
    ],
  },

  'sofia-cross-border-retail': {
    id: 'sofia-cross-border-retail',
    name: 'Sofia Alarcón',
    avatar: '💼',
    age: 34,
    role: {
      en: 'Downtown Retail Supervisor',
      es: 'Supervisora de Comercio en El Paso',
    },
    background: {
      en: 'Cross-border commuter who manages an apparel store on El Paso Street, two blocks north of the port of entry.',
      es: 'Trabajadora transfronteriza que supervisa una tienda de ropa en El Paso Street, a dos cuadras al norte del puerto de entrada.',
    },
    crossingReason: {
      en: 'Starting her evening inventory closing shift at 7:30 PM.',
      es: 'Entra a su turno vespertino de cierre de inventario a las 7:30 PM.',
    },
    communicationStyle: {
      en: 'Pragmatic, observant, friendly, and practical.',
      es: 'Pragmática, observadora, amable y práctica.',
    },
    permittedKnowledge: {
      personalWaitMinutes: 40,
      arrivalScenario: {
        en: 'Joined the standard pedestrian line about 40 minutes ago carrying a thermos of coffee and comfortable walking sneakers.',
        es: 'Se formó en la fila peatonal estándar hace unos 40 minutos con un termo de café y tenis cómodos para caminar.',
      },
      lineMovementPerception: {
        en: 'Describes the movement as typical "accordion pulses"—holding still for eight minutes, then taking ten steps quickly.',
        es: 'Describe el movimiento como el típico "acordeón": quieta por ocho minutos y luego avanzando diez pasos de golpe.',
      },
      whyPasoDelNorte: {
        en: 'Her workplace is just a three-minute walk once she clears U.S. customs. Walking Santa Fe Street is fast and avoids bridge parking fees.',
        es: 'Su trabajo queda a solo tres minutos caminando al cruzar la aduana estadounidense. Caminar por Santa Fe Street es rápido y ahorra estacionamiento.',
      },
      botaPerspective: {
        en: 'Bridge of the Americas makes no sense for her daily route because she would have to pay two bus fares or walk miles from Chamizal.',
        es: 'Puente Libre (BOTA) no tiene sentido en su ruta diaria porque tendría que pagar dos pasajes de autobús o caminar kilómetros desde El Chamizal.',
      },
      outOfScopeKnowledge: {
        en: 'Cannot verify current vehicular bridge wait times or official inspection rates. Points to the official CBP wait tracker for real-time data.',
        es: 'No puede verificar tiempos vehiculares ni tasas oficiales de inspección. Remite al monitor oficial de CBP para datos en tiempo real.',
      },
    },
    suggestedPrompts: [
      {
        id: 'wait-time',
        label: { en: 'How long waiting?', es: '¿Cuánto tiempo llevas?' },
        prompt: {
          en: 'How long have you been waiting in line so far?',
          es: '¿Cuánto tiempo llevas esperando hasta ahora?',
        },
      },
      {
        id: 'pace',
        label: { en: 'Does line feel slow?', es: '¿Avanza lento?' },
        prompt: {
          en: 'Does this queue feel unusually slow compared to normal?',
          es: '¿Sientes que esta fila está más lenta de lo normal?',
        },
      },
      {
        id: 'reason',
        label: { en: 'Why crossing?', es: '¿Por qué cruzas?' },
        prompt: {
          en: 'What brings you across to El Paso this evening?',
          es: '¿A qué vas a El Paso esta tarde?',
        },
      },
      {
        id: 'bota',
        label: { en: 'Consider BOTA?', es: '¿Considerarías BOTA?' },
        prompt: {
          en: 'Would you ever switch over to BOTA instead?',
          es: '¿Cambiarías tu cruce al Puente de las Américas?',
        },
      },
    ],
  },

  'hector-artisan': {
    id: 'hector-artisan',
    name: 'Don Héctor Méndez',
    avatar: '👴',
    age: 67,
    role: {
      en: 'Retired Leather Artisan',
      es: 'Artesano Talabartero Jubilado',
    },
    background: {
      en: 'Juárez native who has walked across the Paso del Norte bridge for more than 40 years. Knows the bridge vendors, breezes, and rhythms of Avenida Juárez.',
      es: 'Juarense que ha cruzado a pie el puente Paso del Norte por más de 40 años. Conoce a los comerciantes, las corrientes de aire y el ritmo de la Avenida Juárez.',
    },
    crossingReason: {
      en: 'Visiting his granddaughter for her seventh birthday in Sunset Heights; carrying a wrapped hand-tooled leather purse he crafted for her.',
      es: 'Va a visitar a su nieta por su séptimo cumpleaños en Sunset Heights; lleva envuelto un bolsito de cuero que él mismo le elaboró.',
    },
    communicationStyle: {
      en: 'Warm, patient, storytelling, and calm.',
      es: 'Cálido, paciente, platicador y sereno.',
    },
    permittedKnowledge: {
      personalWaitMinutes: 15,
      arrivalScenario: {
        en: 'Arrived at the foot of the bridge about 15 minutes ago. In no hurry; enjoying the cool evening air off the Rio Grande.',
        es: 'Llegó al pie del puente hace unos 15 minutos. No tiene prisa; disfruta el fresco del Río Bravo al atardecer.',
      },
      lineMovementPerception: {
        en: 'Feels the queue is moving nicely for this time of evening. Remembers decades of crossings when lines stretched past the cathedral.',
        es: 'Siente que la fila avanza bien para la hora. Recuerda décadas de cruces cuando las filas llegaban más allá de la catedral.',
      },
      whyPasoDelNorte: {
        en: 'Walks here because Sunset Heights is right across the river, just up the hill from West Paisano Drive. His daughter will pick him up near San Jacinto Plaza.',
        es: 'Cruza por aquí porque Sunset Heights queda justo pasando el río, subiendo la colina desde Paisano Drive. Su hija lo recogerá cerca de la Plaza San Jacinto.',
      },
      botaPerspective: {
        en: 'Chuckles at the thought of walking BOTA. Says Puente Libre is for trailer trucks and motorists; Santa Fe is the historic walking bridge.',
        es: 'Sonríe al pensar en cruzar a pie por BOTA. Dice que Puente Libre es para tráileres y autos; Santa Fe es el puente tradicional de a pie.',
      },
      outOfScopeKnowledge: {
        en: 'Does not pretend to know official CBP border statistics or bridge camera telemetry. Tells visitors with a smile to check the official board.',
        es: 'No presume de conocer estadísticas de CBP ni telemetría de cámaras. Con una sonrisa pide a los visitantes mirar el tablero oficial.',
      },
    },
    suggestedPrompts: [
      {
        id: 'wait-time',
        label: { en: 'How long waiting?', es: '¿Cuánto tiempo llevas?' },
        prompt: {
          en: 'Don Héctor, how long have you been in line?',
          es: 'Don Héctor, ¿cuánto tiempo lleva en la fila?',
        },
      },
      {
        id: 'pace',
        label: { en: 'Does line feel slow?', es: '¿Avanza lento?' },
        prompt: {
          en: 'Does the wait feel long to you today?',
          es: '¿Se le hace larga la espera el día de hoy?',
        },
      },
      {
        id: 'reason',
        label: { en: 'Why crossing?', es: '¿Por qué cruza?' },
        prompt: {
          en: 'Who are you visiting in El Paso?',
          es: '¿A quién va a visitar a El Paso?',
        },
      },
      {
        id: 'bota',
        label: { en: 'Consider BOTA?', es: '¿Consideraría BOTA?' },
        prompt: {
          en: 'Would you ever use the Bridge of the Americas instead?',
          es: '¿Consideraría cruzar por el Puente de las Américas en su lugar?',
        },
      },
    ],
  },
};

/**
 * Validates player input messages to ensure safety, bounded memory, and clean formatting.
 */
export function validateWitnessMessage(message: unknown): {
  valid: boolean;
  error?: string;
  trimmed?: string;
} {
  if (typeof message !== 'string') {
    return { valid: false, error: 'Message must be a string.' };
  }

  const trimmed = message.trim();
  if (trimmed.length === 0) {
    return { valid: false, error: 'Message cannot be empty.' };
  }

  if (trimmed.length > 500) {
    return {
      valid: false,
      error: 'Message exceeds maximum permitted length of 500 characters.',
    };
  }

  return { valid: true, trimmed };
}

/**
 * Language detection helper for English vs Spanish queries.
 */
export function detectLanguage(text: string, fallback: 'en' | 'es' = 'en'): 'en' | 'es' {
  const lower = text.toLowerCase();

  // Strong Spanish punctuation
  if (lower.includes('¿') || lower.includes('¡')) {
    return 'es';
  }

  const spanishWords = [
    'cuánto', 'cuanto', 'llevas', 'esperando', 'fila', 'por qué',
    'por que', 'cruzando', 'cruzar', 'avanza', 'lento', 'tarde',
    'puente', 'buenas', 'hola', 'dónde', 'donde', 'visitar', 'américas',
    'americas', 'usted', 'tiempo', 'nieta', 'trabajo', 'estudio', 'considerarías',
    'considerarias'
  ];

  const englishWords = [
    'how long', 'waiting', 'line', 'queue', 'slow', 'why', 'crossing',
    'today', 'bridge', 'hello', 'hi', 'where', 'work', 'school',
    'utep', 'minutes', 'hours', 'would you', 'do you', 'consider', 'instead',
    'you', 'feel', 'what', 'who'
  ];

  let esScore = 0;
  for (const w of spanishWords) {
    if (lower.includes(w)) esScore++;
  }

  let enScore = 0;
  for (const w of englishWords) {
    if (lower.includes(w)) enScore++;
  }

  if (esScore > enScore) return 'es';
  if (enScore > esScore) return 'en';
  return fallback;
}

/**
 * System instruction and prompt construction for Meta Muse Spark dialogue generation.
 * Enforces strict synthetic role boundaries, no hallucinated real-time authority,
 * and grounding in the configured scenario profile.
 */
export function buildWitnessPrompt(
  witness: SyntheticWitnessProfile,
  userMessage: string,
  language: 'en' | 'es'
): {
  systemPrompt: string;
  userContent: string;
} {
  const isEs = language === 'es';

  const systemPrompt = isEs
    ? `Eres un personaje simulado explícitamente sintético llamado ${witness.name} en la fila peatonal del puente Paso del Norte (Santa Fe) en Ciudad Juárez hacia El Paso.
IDENTIDAD OBLIGATORIA: Eres un "Testigo sintético · Simulación". NO eres una persona real.
REGLAS ESTRICTAS DE AUTORIDAD:
1. Responde de manera concisa (máximo 2 a 3 frases) y en tono conversacional, respetuoso y consistente con tu perfil (${witness.communicationStyle.es}).
2. Solo tienes conocimiento de tu propio escenario ficticio (${witness.background.es}, vas a ${witness.crossingReason.es}).
3. Llevas aproximadamente ${witness.permittedKnowledge.personalWaitMinutes} minutos esperando en tu experiencia ficticia.
4. NUNCA inventes estadísticas de CBP, telemetría de cámaras ni digas que sabes el tiempo exacto actual del cruce.
5. Si te preguntan por el tiempo oficial o si otro puente es más rápido, responde desde tu perspectiva personal y recuerda amablemente al usuario que consulte los tiempos oficiales de CBP arriba.
6. Si te preguntan algo fuera de tu alcance personal, di con naturalidad que no lo sabes.
7. Responde enteramente en español.`
    : `You are an explicitly synthetic simulated character named ${witness.name} standing in the simulated pedestrian queue at the Paso del Norte (Santa Fe) bridge from Ciudad Juárez to El Paso.
MANDATORY IDENTITY: You are a "Synthetic witness · Simulation". You are NOT a real person.
STRICT AUTHORITY BOUNDARIES:
1. Respond concisely (maximum 2 to 3 sentences) in a conversational, respectful manner matching your personality (${witness.communicationStyle.en}).
2. You only have knowledge of your personal fictional scenario (${witness.background.en}, crossing for: ${witness.crossingReason.en}).
3. You have personally been waiting about ${witness.permittedKnowledge.personalWaitMinutes} minutes in your fictional experience.
4. NEVER invent CBP official metrics, camera telemetry, or claim to know live port delays.
5. If asked about official wait times or whether BOTA is faster, answer from your personal perspective and politely direct the player to check the official CBP wait times displayed above.
6. If asked about something outside your personal knowledge, admit naturally that you do not know.
7. Respond entirely in English.`;

  const userContent = isEs
    ? `Pregunta del usuario: "${userMessage}"\n\nResponde como ${witness.name} en español, manteniéndote estrictamente dentro de tu escenario ficticio.`
    : `User question: "${userMessage}"\n\nRespond as ${witness.name} in English, remaining strictly within your fictional scenario.`;

  return { systemPrompt, userContent };
}

export type WitnessSafetyCategory =
  | 'live-bridge-comparison'
  | 'live-official-conditions'
  | 'synthetic-identity-inquiry'
  | 'out-of-scope-knowledge';

export interface WitnessSafetyClassification {
  readonly isSafetyBoundary: boolean;
  readonly category?: WitnessSafetyCategory;
}

/**
 * Classifies whether a user question touches authoritative live border conditions,
 * real-time bridge comparisons, synthetic identity disclosures, or out-of-scope topics.
 *
 * This provides a deterministic, application-level safety boundary that does not
 * rely solely on the language model prompt to refuse or redirect queries.
 */
export function classifyWitnessSafetyIntent(message: string): WitnessSafetyClassification {
  const clean = message.trim().toLowerCase();

  // 1. Synthetic Identity Inquiry (Are you real / AI / bot / simulated?)
  const syntheticIdentityRegex =
    /\b(are\s+you\s+(a\s+)?(real(\s+person|\s+human)?|human|an?\s+ai|a\s+bot|simulated|an?\s+npc)|are\s+you\s+real\s+or\s+(fake|simulated|a\s+bot)|who\s+created\s+you|are\s+you\s+a\s+real\s+person)\b/i;
  const syntheticIdentityRegexEs =
    /\b(eres\s+(una?\s+)?(persona\s+real|humano|persona\s+de\s+verdad|persona\s+de\s+carne\s+y\s+hueso|ia|inteligencia\s+artificial|bot|simulaci[oó]n|npc)|eres\s+real(\s+o\s+(un\s+bot|una\s+simulaci[oó]n))?|qui[eé]n\s+te\s+cre[oó])\b/i;

  if (syntheticIdentityRegex.test(clean) || syntheticIdentityRegexEs.test(clean)) {
    return { isSafetyBoundary: true, category: 'synthetic-identity-inquiry' };
  }

  // 2. Live Bridge Comparison (Is BOTA faster right now? Which bridge is faster?)
  const liveBridgeComparisonRegex =
    /\b(is\s+bota\s+(faster|quicker|shorter)|bota\s+(faster|quicker|shorter)|faster\s+than\s+bota|quicker\s+than\s+bota|which\s+bridge\s+is\s+(faster|quicker|better)|which\s+crossing\s+is\s+(faster|quicker)|would\s+bota\s+be\s+(faster|quicker)|should\s+i\s+(use|go\s+to|switch\s+to)\s+bota|is\s+santa\s+fe\s+faster|compare\s+(the\s+)?bridges)\b/i;
  const liveBridgeComparisonRegexEs =
    /\b(bota\s+(es|est[aá]|va)\s+m[aá]s\s+(r[aá]pido|corto|veloz)|m[aá]s\s+(r[aá]pido|corto)\s+.*bota|cu[aá]l\s+puente\s+(es|est[aá]|va)\s+m[aá]s\s+(r[aá]pido|corto)|qu[eé]\s+puente\s+(est[aá]|va)\s+m[aá]s\s+r[aá]pido|cu[aá]l\s+cruce\s+(es|est[aá]|va)\s+m[aá]s\s+(r[aá]pido|corto)|cu[aá]l\s+puente\s+tiene\s+menos\s+fila|cu[aá]l\s+puente\s+conviene|conviene\s+m[aá]s\s+bota|deber[ií]a\s+irme\s+a\s+bota|comparar\s+(los\s+)?puentes)\b/i;

  if (liveBridgeComparisonRegex.test(clean) || liveBridgeComparisonRegexEs.test(clean)) {
    return { isSafetyBoundary: true, category: 'live-bridge-comparison' };
  }

  // 3. Live Official Conditions / Port Status / Real-time Wait
  const liveOfficialConditionsRegex =
    /\b(official\s+(wait|delay|time|metrics?|report)|current\s+(wait|delay|conditions|port\s+status|telemetry)|live\s+(wait|delay|telemetry|status)|what\s+is\s+the\s+(current|official)\s+(wait|delay)|how\s+long\s+is\s+the\s+official\s+wait|is\s+the\s+(port|bridge|crossing)\s+open\s*(right\s+now|now|today)?|port\s+status|how\s+many\s+lanes\s+(are\s+)?open|cbp\s+(wait|delay|time|report|data|measurements?))\b/i;
  const liveOfficialConditionsRegexEs =
    /\b((espera|demora|retraso|tiempo)\s+oficial|(espera|demora|retraso)\s+(actual|en\s+vivo)|cu[aá]l\s+es\s+el\s+tiempo\s+oficial|cu[aá]nto\s+es\s+el\s+tiempo\s+oficial|cu[aá]l\s+es\s+la\s+demora\s+actual|cu[aá]nto\s+es\s+la\s+espera\s+oficial|est[aá]\s+abierto\s+el\s+(puente|puerto|cruce)|estado\s+del\s+(puerto|puente|cruce)|cu[aá]ntas\s+l[ií]neas\s+(est[aá]n|hay)\s+abiertas|cu[aá]ntos\s+carriles\s+(est[aá]n|hay)\s+abiertos|reporte\s+de\s+cbp|datos\s+de\s+cbp)\b/i;

  if (liveOfficialConditionsRegex.test(clean) || liveOfficialConditionsRegexEs.test(clean)) {
    return { isSafetyBoundary: true, category: 'live-official-conditions' };
  }

  // 4. Out of Scope Knowledge (Unrelated external domains)
  const outOfScopeRegex =
    /\b(weather\s+in|stock\s+market|stock\s+price|bitcoin|cryptocurrency|crypto\s+prices?|prescribe\s+(me\s+)?medicine|medical\s+diagnosis|who\s+is\s+the\s+president|who\s+won\s+the\s+election|capital\s+of\s+[A-Za-z]+)\b/i;
  const outOfScopeRegexEs =
    /\b(clima\s+en|bolsa\s+de\s+valores|precio\s+de\s+(las\s+)?acciones|bitcoin|criptomonedas?|rec[eé]tame|diagn[oó]stico\s+m[eé]dico|qui[eé]n\s+es\s+el\s+presidente|qui[eé]n\s+gan[oó]\s+la\s+elecci[oó]n|capital\s+de\s+[A-Za-z]+)\b/i;

  if (outOfScopeRegex.test(clean) || outOfScopeRegexEs.test(clean)) {
    return { isSafetyBoundary: true, category: 'out-of-scope-knowledge' };
  }

  return { isSafetyBoundary: false };
}

/**
 * Generates a deterministic application-level boundary response for safety-sensitive
 * inquiries (official live wait times, real-time bridge comparisons, synthetic identity).
 *
 * Guarantees that fictional testimony is never confused with verified CBP measurements,
 * and always directs the user to the official Border Intelligence panel.
 */
export function getDeterministicBoundaryResponse(
  witness: SyntheticWitnessProfile,
  category: WitnessSafetyCategory,
  language: 'en' | 'es'
): string {
  const isEs = language === 'es';

  switch (category) {
    case 'synthetic-identity-inquiry':
      if (witness.id === 'mateo-student') {
        return isEs
          ? 'Soy un testigo sintético y un personaje simulado en esta fila virtual, no un viajero real. Mi función es dar una perspectiva de escenario estudiantil. Para consultar tiempos de cruce oficiales y verificados, revisa el panel de CBP arriba.'
          : 'I am an explicitly synthetic witness and simulated character in this virtual queue, not an actual traveler. My role is to provide a student scenario perspective. For verified real-world border wait times, check the official CBP panel above!';
      }
      if (witness.id === 'sofia-cross-border-retail') {
        return isEs
          ? 'Soy un testigo sintético y un personaje simulado, no una persona real en el puente. Mis declaraciones representan un escenario ficticio de trabajadora transfronteriza. Para información oficial y verificada, consulta el reporte de CBP arriba.'
          : 'I am a synthetic witness and simulated character, not a real person waiting at the bridge. My responses represent a fictional retail commuter scenario. For official verified crossing information, consult the CBP report above.';
      }
      return isEs
        ? 'Soy un testigo simulado explícitamente sintético creado para esta demostración, no una persona real parada en la fila. Mis memorias y oficio son parte de un escenario ficticio. Por favor consulte el panel oficial de CBP arriba para datos auténticos.'
        : 'I am an explicitly synthetic simulated witness created for this demo, not a real person standing in line. My background and craft are part of a fictional scenario. Please refer to the official CBP panel above for authentic border data.';

    case 'live-bridge-comparison':
      if (witness.id === 'mateo-student') {
        return isEs
          ? 'No tengo forma de saber las condiciones actuales en BOTA ni comparar la rapidez de los puentes en vivo; solo soy un estudiante simulado esperando aquí en la fila de Paso del Norte. De todos modos BOTA me queda muy al este para ir a pie a UTEP. ¡Para ver la comparación oficial y verificada en tiempo real entre ambos puentes, revisa el panel de CBP directamente arriba!'
          : "I have no way of knowing current conditions at BOTA or comparing live bridge speeds—I'm just a simulated student waiting here in line at Paso del Norte. BOTA is too far east for my walking commute to UTEP anyway. For verified, real-time comparisons between bridges, please check the official CBP Border Intelligence panel right above!";
      }
      if (witness.id === 'sofia-cross-border-retail') {
        return isEs
          ? 'No puedo monitorear las condiciones en vivo de la frontera ni decirte si BOTA está más rápido ahorita; solo puedo compartirte mi experiencia de escenario aquí en la fila. Además, BOTA no me deja a pie en el centro de El Paso. Para ver la comparación de espera oficial y verificada en tiempo real entre cruces, consulta el panel de CBP arriba.'
          : "I cannot monitor live conditions across the border or tell you if BOTA is faster right now—I can only share my personal scenario experience here in the queue. Plus, BOTA doesn't lead into Downtown El Paso on foot. For authoritative, real-time wait comparisons between crossings, look at the official CBP Border Intelligence panel displayed above.";
      }
      return isEs
        ? 'No sabría decirle cómo avanza BOTA ahorita; yo solo estoy a pie aquí en Paso del Norte como lo he hecho por décadas. Para tiempos de espera oficiales de CBP y la comparación verificada entre puentes en vivo, por favor revise el panel oficial de Inteligencia Fronteriza arriba.'
        : 'I couldn\'t tell you how BOTA is moving right now; I am only standing on foot here at Paso del Norte as I have for decades. For authentic, live CBP wait times and official comparisons between the bridges, please inspect the official Border Intelligence board above.';

    case 'live-official-conditions':
      if (witness.id === 'mateo-student') {
        return isEs
          ? `Solo conozco mi propia experiencia ficticia: llevo unos ${witness.permittedKnowledge.personalWaitMinutes} minutos en la fila repasando mis apuntes. No tengo acceso a las mediciones oficiales de CBP ni al estado del puerto en vivo. ¡Por favor consulta el panel oficial de Inteligencia Fronteriza directamente arriba para ver los tiempos verificados de CBP!`
          : `I only know my own fictional scenario—I've been in line for about ${witness.permittedKnowledge.personalWaitMinutes} minutes reviewing notes. I don't have access to official CBP queue measurements or live port status. Please look at the official Border Intelligence panel directly above for verified CBP wait times!`;
      }
      if (witness.id === 'sofia-cross-border-retail') {
        return isEs
          ? `Solo puedo decirte que llevo esperando unos ${witness.permittedKnowledge.personalWaitMinutes} minutos en mi escenario de traslado. Eso es únicamente experiencia personal de escenario, no una medición oficial. Para ver los tiempos de espera verificados de CBP, líneas abiertas y el estado del puerto, revisa el panel de Inteligencia Fronteriza arriba.`
          : `I can only tell you that I've been waiting about ${witness.permittedKnowledge.personalWaitMinutes} minutes in my shift commute scenario. That is just personal scenario experience, not an official measurement. For current verified CBP wait times, open lanes, and live port status, check the official Border Intelligence panel above.`;
      }
      return isEs
        ? `En mi escenario personal, llevo unos ${witness.permittedKnowledge.personalWaitMinutes} minutos aquí parado disfrutando el aire de la tarde. Pero eso no es información oficial del puente. Para tiempos de espera auténticos y verificados de CBP y el estado del puerto, consulte el monitor oficial arriba.`
        : `In my personal scenario, I've been standing here for about ${witness.permittedKnowledge.personalWaitMinutes} minutes enjoying the evening air. But that is not official bridge data. For authentic, verified CBP wait times and port status, consult the official tracker above.`;

    case 'out-of-scope-knowledge':
      if (witness.id === 'mateo-student') {
        return isEs
          ? 'No sé nada sobre eso. Solo soy un estudiante de ingeniería en la fila esperando llegar a mi grupo de robótica en UTEP.'
          : "I don't know anything about that. I'm just an engineering student waiting in line at Santa Fe bridge to get to my robotics group at UTEP.";
      }
      if (witness.id === 'sofia-cross-border-retail') {
        return isEs
          ? 'Eso está completamente fuera de lo que sé. Yo solo estoy enfocada en cruzar para mi turno de inventario en el centro de El Paso.'
          : "That's completely outside what I know. I'm just focused on my commute and my inventory shift in Downtown El Paso.";
      }
      return isEs
        ? 'De eso sí no sabría decirle, amigo. Yo solo sé de mis trabajos en cuero y de cruzar para festejar el cumpleaños de mi nieta.'
        : "I wouldn't know about that, friend. I only know about my leathercraft and crossing to celebrate my granddaughter's birthday.";
  }
}

/**
 * Deterministic fallback response generator for offline execution, test suites,
 * and resilient recovery when the Meta model is unavailable or encounters network issues.
 */
export function generateDeterministicFallbackResponse(
  witness: SyntheticWitnessProfile,
  userMessage: string,
  language: 'en' | 'es'
): string {
  const isEs = language === 'es';
  const lower = userMessage.toLowerCase();

  // Check safety boundary first
  const safetyCheck = classifyWitnessSafetyIntent(userMessage);
  if (safetyCheck.isSafetyBoundary && safetyCheck.category) {
    return getDeterministicBoundaryResponse(witness, safetyCheck.category, language);
  }

  // Intent 1: Wait time / How long waiting
  if (
    lower.includes('how long') ||
    lower.includes('wait') ||
    lower.includes('cuánto') ||
    lower.includes('cuanto') ||
    lower.includes('tiempo') ||
    lower.includes('llevas')
  ) {
    if (witness.id === 'mateo-student') {
      return isEs
        ? `Llevo unos ${witness.permittedKnowledge.personalWaitMinutes} minutos aquí en la fila revisando mis apuntes. Para la estimación oficial exacta de CBP, mejor revisa el monitor de arriba.`
        : `I've been in line for about ${witness.permittedKnowledge.personalWaitMinutes} minutes reviewing my class notes. For the official CBP wait time, definitely check the tracker above!`;
    }
    if (witness.id === 'sofia-cross-border-retail') {
      return isEs
        ? `Llegué hace unos ${witness.permittedKnowledge.personalWaitMinutes} minutos con mi café. Recuerda que mi tiempo es solo mi experiencia personal; el reporte oficial de CBP está justo en el panel superior.`
        : `I joined the queue about ${witness.permittedKnowledge.personalWaitMinutes} minutes ago with my thermos. Keep in mind that's just my personal wait; the official CBP measurement is in the panel above.`;
    }
    return isEs
      ? `Apenas llevo unos ${witness.permittedKnowledge.personalWaitMinutes} minutos parado aquí disfrutando el fresco de la tarde. Si busca el dato oficial del puente, mire el tablero de CBP acá arriba.`
      : `I've only been standing here about ${witness.permittedKnowledge.personalWaitMinutes} minutes enjoying the evening breeze. If you need the official bridge delay, look at the CBP board right above.`;
  }

  // Intent 2: Line pace / Does it feel slow
  if (
    lower.includes('slow') ||
    lower.includes('fast') ||
    lower.includes('pace') ||
    lower.includes('lento') ||
    lower.includes('rápido') ||
    lower.includes('rapido') ||
    lower.includes('avanza')
  ) {
    if (witness.id === 'mateo-student') {
      return isEs
        ? 'Siento que avanza a ritmo constante; dimos varios pasos hace poco cuando habilitaron otra caseta. Pero esto cambia a cada rato según los inspectores.'
        : 'It feels fairly steady to me; we moved up several steps a moment ago when they opened another booth. But it fluctuates depending on the inspectors.';
    }
    if (witness.id === 'sofia-cross-border-retail') {
      return isEs
        ? 'Es el clásico efecto acordeón: nos quedamos parados unos minutos y luego avanzamos de golpe. Típico para esta hora del día.'
        : "It's the classic accordion effect: we hold still for several minutes and then take a bunch of steps all at once. Pretty typical for this time of day.";
    }
    return isEs
      ? 'A mi edad ya no tengo prisa. Para la hora que es, siento que camina con calma pero sin atorarse demasiado.'
      : 'At my age I am in no rush. For this time of evening, it feels calm and is moving without getting too bogged down.';
  }

  // Intent 3: Reason for crossing / Why crossing
  if (
    lower.includes('why') ||
    lower.includes('reason') ||
    lower.includes('por qué') ||
    lower.includes('por que') ||
    lower.includes('motivo') ||
    lower.includes('visita') ||
    lower.includes('cruzas') ||
    lower.includes('cruza')
  ) {
    if (witness.id === 'mateo-student') {
      return isEs
        ? 'Voy a la facultad de ingeniería en UTEP para reunirme con mi equipo de proyecto de robótica.'
        : 'I am heading over to the UTEP engineering building to meet up with my robotics project team.';
    }
    if (witness.id === 'sofia-cross-border-retail') {
      return isEs
        ? 'Tengo turno de supervisión y cierre de inventario en una tienda de El Paso Street a las 7:30.'
        : 'I have an inventory closing shift at a retail boutique on El Paso Street starting at 7:30 PM.';
    }
    return isEs
      ? 'Voy a Sunset Heights a celebrar el cumpleaños de mi nieta. Le llevo de regalo un bolsito de cuero que le hice a mano.'
      : 'I am visiting my granddaughter in Sunset Heights for her birthday. I made her a hand-tooled leather purse as a gift.';
  }

  // Intent 4: BOTA / Bridge of the Americas comparison
  if (
    lower.includes('bota') ||
    lower.includes('americas') ||
    lower.includes('américas') ||
    lower.includes('puente libre') ||
    lower.includes('other bridge') ||
    lower.includes('otro puente')
  ) {
    if (witness.id === 'mateo-student') {
      return isEs
        ? 'BOTA queda demasiado al este para ir a pie. Aquí en Santa Fe cruzo y tomo el camión directo al campus de UTEP.'
        : 'BOTA is way too far east for walking. Here at Santa Fe I just walk across and catch the direct bus to the UTEP campus.';
    }
    if (witness.id === 'sofia-cross-border-retail') {
      return isEs
        ? 'Para quienes caminamos a trabajar al centro de El Paso, BOTA no es opción viable. Este puente nos deja a solo unas cuadras de nuestro destino.'
        : "For those of us walking to work in Downtown El Paso, BOTA isn't a viable option. This bridge leaves us just blocks from our jobs.";
    }
    return isEs
      ? '¡Qué esperanzas! BOTA es para los autos y camiones de carga. Para nosotros los de a pie, el Paso del Norte ha sido el camino de toda la vida.'
      : 'No way! BOTA is for cars and cargo trailers. For those of us on foot, Paso del Norte has been our historic path for decades.';
  }

  // Default / Open question:
  if (witness.id === 'mateo-student') {
    return isEs
      ? `Como estudiante que cruza a diario, solo puedo hablarte de mi experiencia en la fila. Para datos oficiales del cruce, consulta la sección de CBP arriba.`
      : `As a student commuter, I can only speak to my personal spot in line. For official crossing metrics, please check the CBP section above!`;
  }
  if (witness.id === 'sofia-cross-border-retail') {
    return isEs
      ? `Solo te puedo compartir mi perspectiva como trabajadora transfronteriza en esta fila. Toda la información oficial verificada está disponible en el reporte de CBP arriba.`
      : `I can only share my perspective as a daily commuter in this queue. All verified official data is available in the CBP report above.`;
  }
  return isEs
    ? `Solo soy un vecino platicando en la fila mientras cae la tarde. Las mediciones verdaderas y oficiales las tienen las autoridades de CBP en el panel superior.`
    : `I am just a neighbor chatting in line as the sun sets. The authoritative measurements are provided by the CBP authorities in the panel above.`;
}
