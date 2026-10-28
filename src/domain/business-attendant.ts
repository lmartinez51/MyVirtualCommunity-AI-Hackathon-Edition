import { BusinessId, createBusinessId } from './business-id.js';
import { BusinessSchedule } from './business-schedule.js';
import { BusinessOperationalStatus } from './operational-status.js';
import { formatBorderFetchTime } from './border-crossing.js';
import { BilingualText } from './synthetic-witness.js';

export type BusinessAttendantId = 'carlos-farmacia-attendant' | 'valeria-optica-attendant';

export type AttendantInquiryTopic =
  | 'hours'
  | 'status'
  | 'location'
  | 'inventory'
  | 'medical'
  | 'pricing'
  | 'appointment'
  | 'general';

export interface BusinessAttendantProfile {
  readonly id: BusinessAttendantId;
  readonly businessId: BusinessId;
  readonly businessCanonicalName: string;
  readonly name: string;
  readonly avatar: string;
  readonly role: BilingualText;
  readonly personality: BilingualText;
  readonly bio: BilingualText;
  readonly virtualMarker: BilingualText;
  readonly virtualDisclosure: BilingualText;
  readonly verifiedAddress: string;
  readonly coordinates: {
    readonly latitude: number;
    readonly longitude: number;
  };
  readonly authorizedTopics: BilingualText;
  readonly boundaryNotice: BilingualText;
  readonly suggestedPrompts: ReadonlyArray<{
    readonly id: string;
    readonly topic: AttendantInquiryTopic;
    readonly label: BilingualText;
    readonly prompt: BilingualText;
  }>;
}

export const VIRTUAL_ATTENDANT_MARKER: BilingualText = Object.freeze({
  en: 'Virtual attendant · AI simulation',
  es: 'Encargado virtual · Simulación de IA',
});

export const VIRTUAL_ATTENDANT_DISCLOSURE: BilingualText = Object.freeze({
  en: 'Fictional virtual character associated with this storefront. Not an actual employee, pharmacist, or eye care specialist. Responses are grounded strictly in verified business schedule data.',
  es: 'Personaje virtual ficticio asociado a este comercio. No es un empleado real, farmacéutico ni optometrista. Las respuestas se basan estrictamente en datos verificados de horarios comerciales.',
});

/**
 * Two official storefront attendants for the simulated Ciudad Juárez corridor.
 * Bound to the existing canonical business records for Farmacias Benavides and Óptica Fashion.
 */
export const BUSINESS_ATTENDANT_ROSTER: Record<BusinessAttendantId, BusinessAttendantProfile> = Object.freeze({
  'carlos-farmacia-attendant': Object.freeze({
    id: 'carlos-farmacia-attendant',
    businessId: createBusinessId('pharmacy:farmacias-benavides'),
    businessCanonicalName: 'Farmacias Benavides',
    name: 'Carlos Méndez',
    avatar: '💊',
    role: Object.freeze({
      en: 'Virtual Pharmacy Attendant',
      es: 'Encargado Virtual de Farmacia',
    }),
    personality: Object.freeze({
      en: 'Helpful, polite, health-conscious, and attentive to store hours and location, with clear ethical boundaries regarding medical advice and medication inventory.',
      es: 'Servicial, educado, atento a los horarios y ubicación, con claros límites éticos sobre consejos médicos e inventario de medicamentos.',
    }),
    bio: Object.freeze({
      en: 'Simulated storefront attendant at Farmacias Benavides on Avenida Juárez. Assists visitors with verified operating hours, open status, and store location near the international bridge.',
      es: 'Encargado virtual del local de Farmacias Benavides sobre la Avenida Juárez. Orienta a visitantes con horarios verificados, estado de apertura y ubicación junto al puente internacional.',
    }),
    virtualMarker: VIRTUAL_ATTENDANT_MARKER,
    virtualDisclosure: VIRTUAL_ATTENDANT_DISCLOSURE,
    verifiedAddress: 'Av. Benito Juárez 610, Centro (colonia Chamizal unverified), 32000 Juárez, Chih., México',
    coordinates: Object.freeze({
      latitude: 31.745488,
      longitude: -106.486276,
    }),
    authorizedTopics: Object.freeze({
      en: 'Store location, authoritative opening hours, current open/closed status, and general visiting directions.',
      es: 'Ubicación de la sucursal, horarios de atención autorizados, estado de apertura actual e indicaciones generales de llegada.',
    }),
    boundaryNotice: Object.freeze({
      en: 'Cannot verify product inventory, stock levels, medication pricing, or provide medical diagnoses or prescription advice.',
      es: 'No puede verificar inventario de productos, existencias, precios de medicamentos ni brindar diagnósticos médicos o asesoría de recetas.',
    }),
    suggestedPrompts: Object.freeze([
      Object.freeze({
        id: 'hours-query',
        topic: 'hours',
        label: Object.freeze({ en: 'Operating hours', es: 'Horario de atención' }),
        prompt: Object.freeze({
          en: 'What are the operating hours for Farmacias Benavides?',
          es: '¿Cuáles son los horarios de atención de Farmacias Benavides?',
        }),
      }),
      Object.freeze({
        id: 'status-query',
        topic: 'status',
        label: Object.freeze({ en: 'Open right now?', es: '¿Está abierta ahorita?' }),
        prompt: Object.freeze({
          en: 'Is Farmacias Benavides open right now, and what is its current operating status?',
          es: '¿Está abierta Farmacias Benavides en este momento y cuál es su estado de servicio?',
        }),
      }),
      Object.freeze({
        id: 'location-query',
        topic: 'location',
        label: Object.freeze({ en: 'Store location', es: 'Ubicación' }),
        prompt: Object.freeze({
          en: 'Where exactly is Farmacias Benavides located on Avenida Juárez?',
          es: '¿Dónde está ubicada exactamente Farmacias Benavides en la Avenida Juárez?',
        }),
      }),
      Object.freeze({
        id: 'inventory-query',
        topic: 'inventory',
        label: Object.freeze({ en: 'Medication stock?', es: '¿Tienen medicamento?' }),
        prompt: Object.freeze({
          en: 'Do you have antibiotics or pain relievers in stock right now and how much do they cost?',
          es: '¿Tienen antibióticos o analgésicos en existencia ahorita y cuánto cuestan?',
        }),
      }),
    ]),
  }),

  'valeria-optica-attendant': Object.freeze({
    id: 'valeria-optica-attendant',
    businessId: createBusinessId('optical:optica-fashion'),
    businessCanonicalName: 'Óptica Fashion',
    name: 'Valeria Domínguez',
    avatar: '👓',
    role: Object.freeze({
      en: 'Virtual Optical Attendant',
      es: 'Encargada Virtual de Óptica',
    }),
    personality: Object.freeze({
      en: 'Friendly, stylish, welcoming, and concise, eager to share store hours and location across from Farmacias Benavides, while transparent about the lack of live frame inventory and appointment booking.',
      es: 'Amable, moderna, acogedora y concisa, orienta sobre los horarios y ubicación frente a Farmacias Benavides, siendo transparente sobre la ausencia de inventario de armazones o agenda de citas.',
    }),
    bio: Object.freeze({
      en: 'Simulated optical boutique attendant at Óptica Fashion on Avenida Juárez. Helps visitors check confirmed business hours, open status, and store location near the border.',
      es: 'Encargada virtual de la óptica sobre la Avenida Juárez. Ayuda a los visitantes a consultar horarios confirmados, estado de atención y ubicación cerca del cruce fronterizo.',
    }),
    virtualMarker: VIRTUAL_ATTENDANT_MARKER,
    virtualDisclosure: VIRTUAL_ATTENDANT_DISCLOSURE,
    verifiedAddress: 'Av. Benito Juárez 859, Centro, 32000 Juárez, Chih., México',
    coordinates: Object.freeze({
      latitude: 31.745562,
      longitude: -106.486492,
    }),
    authorizedTopics: Object.freeze({
      en: 'Boutique location, authoritative opening hours, current operating status, and directions.',
      es: 'Ubicación de la óptica, horarios autorizados, estado de apertura actual e indicaciones de llegada.',
    }),
    boundaryNotice: Object.freeze({
      en: 'Cannot verify frame or lens inventory, prices, eye-exam appointment availability, or provide clinical optometry recommendations.',
      es: 'No puede verificar inventario de armazones o micas, precios, disponibilidad de citas para examen de la vista ni dar recomendaciones optométricas.',
    }),
    suggestedPrompts: Object.freeze([
      Object.freeze({
        id: 'hours-query',
        topic: 'hours',
        label: Object.freeze({ en: 'Operating hours', es: 'Horario de atención' }),
        prompt: Object.freeze({
          en: 'What are the opening and closing hours for Óptica Fashion?',
          es: '¿Cuáles son los horarios de apertura y cierre de Óptica Fashion?',
        }),
      }),
      Object.freeze({
        id: 'status-query',
        topic: 'status',
        label: Object.freeze({ en: 'Open right now?', es: '¿Está abierta ahorita?' }),
        prompt: Object.freeze({
          en: 'Is Óptica Fashion open right now, and can I visit today?',
          es: '¿Está abierta Óptica Fashion en este momento y puedo ir hoy?',
        }),
      }),
      Object.freeze({
        id: 'location-query',
        topic: 'location',
        label: Object.freeze({ en: 'Store location', es: 'Ubicación' }),
        prompt: Object.freeze({
          en: 'Where is Óptica Fashion located relative to the bridge and other stores?',
          es: '¿Dónde está ubicada Óptica Fashion con respecto al puente y otros comercios?',
        }),
      }),
      Object.freeze({
        id: 'appointment-query',
        topic: 'appointment',
        label: Object.freeze({ en: 'Eye exam appointment?', es: '¿Agendar cita?' }),
        prompt: Object.freeze({
          en: 'Can I schedule an eye exam appointment for tomorrow and check frame prices?',
          es: '¿Puedo agendar una cita para examen de la vista mañana y consultar precios de armazones?',
        }),
      }),
    ]),
  }),
});

/**
 * Classifies an incoming customer message into an inquiry topic.
 */
export function classifyAttendantInquiry(message: string): AttendantInquiryTopic {
  const norm = message.toLowerCase();

  // 1. Medical diagnosis, prescription, health complaints
  if (
    /\b(medical|diagnosis|treat|prescription|doctor|illness|pain|sick|headache|dose|dosage|receta|m[eé]dico|diagn[oó]stico|dolor|enfermo|pastillas|tratamiento|s[ií]ntoma)\b/i.test(
      norm
    )
  ) {
    return 'medical';
  }

  // 2. Appointments / reservations
  if (
    /\b(appointment|schedule|book|booking|reservation|exam|optometrist|cita|agendar|reservar|examen de la vista)\b/i.test(
      norm
    )
  ) {
    return 'appointment';
  }

  // 3. Pricing / discounts / insurance
  if (
    /\b(cost|price|pricing|how much|dollar|pesos|discount|promotion|insurance|sale|cu[aá]nto cuesta|precio|costo|descuento|promoci[oó]n|seguro)\b/i.test(
      norm
    )
  ) {
    return 'pricing';
  }

  // 4. Inventory / product stock
  if (
    /\b(in stock|stock|inventory|product|brand|antibiotic|pill|medicine|medication|frames|lenses|glasses|sunglasses|armaz[oó]n|armazones|lentes|micas|medicamento|medicina|existencia|surtido|tienen)\b/i.test(
      norm
    )
  ) {
    return 'inventory';
  }

  // 5. Operating status (open right now, current status)
  if (
    /\b(open now|open right now|are you open|is it open|closed now|abierto ahorita|abierto en este momento|abren ahorita|est[aá] abierto|est[aá] cerrado)\b/i.test(
      norm
    )
  ) {
    return 'status';
  }

  // 6. Operating hours / schedule
  if (
    /\b(hours|schedule|operating days|what time do you open|what time do you close|close today|horario|horarios|a qu[eé] hora abren|a qu[eé] hora cierran|d[ií]as de atenci[oó]n)\b/i.test(
      norm
    )
  ) {
    return 'hours';
  }

  // 7. Location / address / directions
  if (
    /\b(where|location|address|directions|avenida|ju[aá]rez|find you|d[oó]nde est[aá]|ubicaci[oó]n|direcci[oó]n|c[oó]mo llego)\b/i.test(
      norm
    )
  ) {
    return 'location';
  }

  return 'general';
}

export interface FormatAttendantFactOptions {
  readonly language?: 'en' | 'es';
}

/**
 * Formats schedule hours into a human-readable string.
 */
export function formatOperatingHoursDescription(
  schedule: BusinessSchedule,
  language: 'en' | 'es' = 'en'
): string {
  const isEs = language === 'es';
  const pad2 = (n: number) => n.toString().padStart(2, '0');
  const openStr = `${pad2(schedule.hours.openHour)}:${pad2(schedule.hours.openMinute)}`;
  const closeStr = `${pad2(schedule.hours.closeHour)}:${pad2(schedule.hours.closeMinute)}`;

  const daysEn = schedule.operatingDays.includes(0)
    ? 'Every day'
    : 'Monday through Saturday (closed Sunday)';
  const daysEs = schedule.operatingDays.includes(0)
    ? 'Todos los días'
    : 'Lunes a sábado (cerrado los domingos)';

  if (isEs) {
    return `${daysEs}, de ${openStr} a ${closeStr} (${schedule.timeZone}).`;
  }
  return `${daysEn}, from ${openStr} to ${closeStr} (${schedule.timeZone}).`;
}

/**
 * Builds a deterministic, guaranteed-safe response path for an attendant.
 * Used when the model fails, or to guarantee 100% adherence to authoritative facts
 * and knowledge boundaries.
 */
export function getDeterministicAttendantResponse(
  profile: BusinessAttendantProfile,
  status: BusinessOperationalStatus,
  schedule: BusinessSchedule,
  topic: AttendantInquiryTopic,
  _userMessage?: string,
  language: 'en' | 'es' = 'en'
): string {
  const isEs = language === 'es';
  const businessName = profile.businessCanonicalName;
  const attendantName = profile.name;
  const address = profile.verifiedAddress;
  const hoursDesc = formatOperatingHoursDescription(schedule, language);
  const evalLocalTime = formatBorderFetchTime(status.evaluatedAtUtc, language);

  switch (topic) {
    case 'status': {
      if (status.status === 'OPEN') {
        return isEs
          ? `¡Hola! Soy ${attendantName}, encargado virtual de ${businessName}. En este momento (${evalLocalTime}), el establecimiento se encuentra ABIERTO y recibiendo clientes. Hoy atendemos hasta las ${status.closeTimeCivil} (${status.timeZone}).`
          : `Hello! I'm ${attendantName}, virtual attendant for ${businessName}. As of our current verification (${evalLocalTime}), the business is OPEN and admitting customers. Today's service runs until ${status.closeTimeCivil} (${status.timeZone}).`;
      }
      if (status.status === 'CLOSING_SOON') {
        return isEs
          ? `Atención: Soy ${attendantName} de ${businessName}. El local se encuentra POR CERRAR. Cierra a las ${status.closeTimeCivil} (${status.timeZone}) y ya no se admiten nuevos ingresos.`
          : `Notice: I'm ${attendantName} from ${businessName}. The business is currently CLOSING SOON. Doors close at ${status.closeTimeCivil} (${status.timeZone}), and new customer admissions are closed.`;
      }
      return isEs
        ? `Hola, soy ${attendantName}, encargado virtual de ${businessName}. En este momento (${evalLocalTime}), el establecimiento se encuentra CERRADO. ${status.summaryMessage} Nuestro horario oficial es: ${hoursDesc}`
        : `Hello, I'm ${attendantName}, virtual attendant for ${businessName}. As of ${evalLocalTime}, the business is currently CLOSED. ${status.summaryMessage} Regular operating hours are: ${hoursDesc}`;
    }

    case 'hours': {
      return isEs
        ? `Con gusto le informo: el horario oficial registrado para ${businessName} es ${hoursDesc}`
        : `Happy to help! The official confirmed schedule for ${businessName} is ${hoursDesc}`;
    }

    case 'location': {
      const spatialNote =
        profile.id === 'carlos-farmacia-attendant'
          ? isEs
            ? 'Nos ubicamos sobre la acera este de la Avenida Juárez, a unos 250 metros al sur del Puente Internacional Paso del Norte, justo frente a Óptica Fashion.'
            : 'We are located on the east side of Avenida Juárez, approximately 250 meters south of the Paso del Norte International Bridge, right across from Óptica Fashion.'
          : isEs
          ? 'Nos ubicamos sobre la acera oeste de la Avenida Juárez, frente a Farmacias Benavides y a pocos pasos del acceso peatonal al puente internacional.'
          : 'We are located on the west side of Avenida Juárez, directly across from Farmacias Benavides and just steps south of the international bridge entrance.';

      return isEs
        ? `La dirección oficial de ${businessName} es: ${address}. ${spatialNote}`
        : `The official address for ${businessName} is: ${address}. ${spatialNote}`;
    }

    case 'inventory': {
      if (profile.id === 'carlos-farmacia-attendant') {
        return isEs
          ? `Como encargado virtual de ${businessName}, solo tengo acceso al registro verificado de horarios y ubicación; no cuento con acceso al inventario de medicamentos en vivo ni a la lista de precios. Le sugiero visitar la sucursal directamente en ${address} durante nuestro horario de servicio para consultar existencias.`
          : `As a virtual attendant for ${businessName}, I only have access to verified schedule and location data; I do not have access to live medication inventory or pricing records. Please visit the pharmacy in person at ${address} during operating hours to check product availability.`;
      }
      return isEs
        ? `Como encargada virtual de ${businessName}, no tengo acceso al catálogo de armazones en existencia ni a precios en los datos verificados del sistema. Le invitamos a acudir a nuestra boutique en ${address} durante el horario habitual para ver modelos y presupuestos.`
        : `As a virtual attendant for ${businessName}, I do not have access to live frame inventory or price lists in our verified records. Please stop by our boutique at ${address} during regular hours to explore available styles.`;
    }

    case 'medical': {
      return isEs
        ? `Por razones de seguridad y salud, como asistente virtual no estoy autorizado para ofrecer asesoría médica, interpretar recetas ni recomendar tratamientos. Para cualquier consulta sobre medicamentos o salud, por favor consulte a un médico certificado o al farmacéutico en turno en ${businessName} (${address}).`
        : `For health and safety reasons, as a virtual assistant I am not authorized to provide medical advice, evaluate prescriptions, or suggest treatments. For any medication or healthcare question, please consult a certified doctor or the licensed pharmacist on duty at ${businessName} (${address}).`;
    }

    case 'pricing': {
      return isEs
        ? `No dispongo de información de precios, promociones ni descuentos en los registros verificados del sistema para ${businessName}. Le sugerimos consultar directamente en tienda en ${address}.`
        : `I do not have access to verified pricing, discounts, or promotions for ${businessName}. Please check directly with the staff at ${address}.`;
    }

    case 'appointment': {
      return isEs
        ? `En los registros actuales de ${businessName} no contamos con un sistema de agenda o reserva de citas para exámenes optométricos. Le sugerimos acudir personalmente a ${address} durante nuestro horario (${hoursDesc}) para solicitar atención.`
        : `Our current verified records for ${businessName} do not include an appointment scheduling system for eye examinations. Please visit us in person at ${address} during regular hours (${hoursDesc}) for assistance.`;
    }

    case 'general':
    default: {
      return isEs
        ? `¡Hola! Soy ${attendantName}, ${profile.role.es} de ${businessName}. Estoy aquí para orientarle sobre nuestros horarios de atención (${hoursDesc}) y nuestra ubicación en ${address}. ¿En qué le puedo colaborar hoy?`
        : `Hello! I'm ${attendantName}, ${profile.role.en} for ${businessName}. I'm here to assist you with our confirmed business hours (${hoursDesc}) and store location at ${address}. How may I help you today?`;
    }
  }
}

/**
 * Builds the Meta Muse Spark prompt bounded strictly by the attendant profile,
 * authoritative business schedule, and operational status.
 */
export function buildBusinessAttendantPrompt(
  profile: BusinessAttendantProfile,
  status: BusinessOperationalStatus,
  schedule: BusinessSchedule,
  userMessage: string,
  language: 'en' | 'es'
): { systemPrompt: string; userContent: string } {
  const isEs = language === 'es';
  const hoursDesc = formatOperatingHoursDescription(schedule, language);
  const evalLocalTime = formatBorderFetchTime(status.evaluatedAtUtc, language);

  const systemPrompt = isEs
    ? `Eres ${profile.name}, ${profile.role.es} de "${profile.businessCanonicalName}" en la comunidad virtual de Ciudad Juárez.
PERSONALIDAD: ${profile.personality.es}
DIVULGACIÓN: Eres un personaje virtual simulado por IA (${profile.virtualMarker.es}). No eres un empleado de carne y hueso ni un profesional médico u optometrista.
REGLAS CRÍTICAS DE VERACIDAD:
1. Nombre comercial: Mantén estrictamente "${profile.businessCanonicalName}". Nunca lo traduzcas, transliteres ni modifiques.
2. Estado y horarios: Básate EXCLUSIVAMENTE en los DATOS AUTORITATIVOS VERIFICADOS proporcionados abajo. Si el negocio está ${status.status}, dilo claramente sin inventar.
3. Límites estrictos de conocimiento:
   - NUNCA inventes existencias de medicamentos, inventario de productos, armazones ni disponibilidad en vivo.
   - NUNCA inventes precios, descuentos, costos en dólares o pesos ni promociones.
   - NUNCA des diagnósticos médicos, interpretaciones de recetas ni consejos clínicos de salud.
   - NUNCA inventes que se pueden agendar citas si no está en los datos verificados.
4. Cuando el usuario pregunte por inventario, precios, recetas o salud: Sé cortés, explica amablemente que como encargado virtual solo cuentas con información de horarios y ubicación, y sugiérele acudir a la sucursal física en ${profile.verifiedAddress} o consultar a un profesional de la salud.
5. Responde de forma concisa, cálida y natural en español.`
    : `You are ${profile.name}, the ${profile.role.en} for "${profile.businessCanonicalName}" in the Ciudad Juárez virtual community.
PERSONALITY: ${profile.personality.en}
DISCLOSURE: You are a fictional virtual character simulated by AI (${profile.virtualMarker.en}). You are NOT a real human employee, pharmacist, or eye care specialist.
CRITICAL TRUTH INVARIANTS:
1. Business name: Keep "${profile.businessCanonicalName}" strictly unaltered. Do not translate or modify it.
2. Operational state: Base your answer EXCLUSIVELY on the AUTHORITATIVE VERIFIED DATA provided below. If the business is currently ${status.status}, state it accurately.
3. Strict knowledge boundaries:
   - NEVER invent medication inventory, product stock levels, frame styles, or live availability.
   - NEVER invent prices, discounts, dollar/peso costs, or promotions.
   - NEVER offer medical diagnoses, prescription analysis, or individualized clinical advice.
   - NEVER pretend to book appointments or exams if not supported by verified data.
4. When asked about inventory, pricing, prescriptions, or clinical questions: Be polite and explain that as a virtual attendant you only have verified operating hours and location data, and invite them to visit the physical store at ${profile.verifiedAddress} or consult a licensed professional.
5. Respond concisely, courteously, and naturally in American English.`;

  const userContent = [
    `CUSTOMER QUERY: "${userMessage}"`,
    '',
    '--- AUTHORITATIVE STOREFRONT FACTS (WORLD CORE SNAPSHOT) ---',
    `Establishment: ${profile.businessCanonicalName} (ID: ${profile.businessId})`,
    `Attendant: ${profile.name} (${profile.role[language]})`,
    `Virtual Disclosure: ${profile.virtualMarker[language]}`,
    `Physical Address: ${profile.verifiedAddress}`,
    `Evaluated Time: ${evalLocalTime}`,
    `Current Operating Status: ${status.status} (isOpen: ${status.isOpen ? 'YES' : 'NO'}, admissionPermitted: ${status.admissionPermitted ? 'YES' : 'NO'})`,
    `Today's Schedule: Open ${status.openTimeCivil} to ${status.closeTimeCivil} (${status.timeZone})`,
    `Last Entry Cutoff: ${status.cutoffTimeCivil}`,
    `Weekly Operating Days: ${hoursDesc}`,
    `Official Status Summary: ${status.summaryMessage}`,
    '-----------------------------------------------------------',
    'Please answer the customer inquiry grounded strictly in these facts and observe all knowledge boundaries.',
  ].join('\n');

  return { systemPrompt, userContent };
}
