import { describe, it } from 'node:test';
import assert from 'node:assert';
import {
  BUSINESS_ATTENDANT_ROSTER,
  BusinessAttendantId,
  classifyAttendantInquiry,
  formatOperatingHoursDescription,
  getDeterministicAttendantResponse,
  VIRTUAL_ATTENDANT_DISCLOSURE,
  VIRTUAL_ATTENDANT_MARKER,
} from '../../src/domain/business-attendant.js';
import { validateBusinessSchedule } from '../../src/domain/business-schedule.js';
import { evaluateBusinessOperationalStatus } from '../../src/domain/operational-status.js';

describe('Storefront Business Attendants Domain Models and Constraints', () => {
  const farmaciasSchedule = validateBusinessSchedule({
    businessId: 'pharmacy:farmacias-benavides',
    name: 'Farmacias Benavides',
    timeZone: 'America/Ciudad_Juarez',
    operatingDays: [1, 2, 3, 4, 5, 6],
    hours: {
      openHour: 7,
      openMinute: 0,
      closeHour: 22,
      closeMinute: 0,
    },
    lastEntryCutoffMinutes: 0,
  });

  const opticaSchedule = validateBusinessSchedule({
    businessId: 'optical:optica-fashion',
    name: 'Óptica Fashion',
    timeZone: 'America/Ciudad_Juarez',
    operatingDays: [1, 2, 3, 4, 5, 6],
    hours: {
      openHour: 9,
      openMinute: 0,
      closeHour: 17,
      closeMinute: 0,
    },
    lastEntryCutoffMinutes: 0,
  });

  it('1. Roster contains exactly the two configured business attendants', () => {
    const keys = Object.keys(BUSINESS_ATTENDANT_ROSTER) as BusinessAttendantId[];
    assert.strictEqual(keys.length, 2);
    assert.ok(keys.includes('carlos-farmacia-attendant'));
    assert.ok(keys.includes('valeria-optica-attendant'));
  });

  it('2. Each attendant references the correct existing business ID', () => {
    const carlos = BUSINESS_ATTENDANT_ROSTER['carlos-farmacia-attendant'];
    const valeria = BUSINESS_ATTENDANT_ROSTER['valeria-optica-attendant'];

    assert.strictEqual(carlos.businessId, 'pharmacy:farmacias-benavides');
    assert.strictEqual(valeria.businessId, 'optical:optica-fashion');
  });

  it('3. Canonical business names remain unchanged in both languages', () => {
    const carlos = BUSINESS_ATTENDANT_ROSTER['carlos-farmacia-attendant'];
    const valeria = BUSINESS_ATTENDANT_ROSTER['valeria-optica-attendant'];

    assert.strictEqual(carlos.businessCanonicalName, 'Farmacias Benavides');
    assert.strictEqual(valeria.businessCanonicalName, 'Óptica Fashion');
  });

  it('4. Virtual attendant identity marker and disclosure are explicitly preserved', () => {
    assert.strictEqual(VIRTUAL_ATTENDANT_MARKER.en, 'Virtual attendant · AI simulation');
    assert.strictEqual(VIRTUAL_ATTENDANT_MARKER.es, 'Encargado virtual · Simulación de IA');
    assert.ok(VIRTUAL_ATTENDANT_DISCLOSURE.en.includes('Not an actual employee'));
    assert.ok(VIRTUAL_ATTENDANT_DISCLOSURE.es.includes('No es un empleado real'));

    const carlos = BUSINESS_ATTENDANT_ROSTER['carlos-farmacia-attendant'];
    assert.strictEqual(carlos.virtualMarker.en, 'Virtual attendant · AI simulation');
    assert.strictEqual(carlos.virtualMarker.es, 'Encargado virtual · Simulación de IA');
  });

  it('5. Correctly classifies customer inquiry topics', () => {
    assert.strictEqual(classifyAttendantInquiry('What are your hours today?'), 'hours');
    assert.strictEqual(classifyAttendantInquiry('¿A qué hora abren y cierran?'), 'hours');
    assert.strictEqual(classifyAttendantInquiry('Are you open right now?'), 'status');
    assert.strictEqual(classifyAttendantInquiry('¿Está abierto en este momento?'), 'status');
    assert.strictEqual(classifyAttendantInquiry('Where is the store located on Avenida Juárez?'), 'location');
    assert.strictEqual(classifyAttendantInquiry('¿Cuál es la dirección del local?'), 'location');
    assert.strictEqual(classifyAttendantInquiry('Do you have amoxicillin pills in stock?'), 'inventory');
    assert.strictEqual(classifyAttendantInquiry('¿Tienen armazones de diseñador en existencia?'), 'inventory');
    assert.strictEqual(classifyAttendantInquiry('I have a severe headache, what prescription do you recommend?'), 'medical');
    assert.strictEqual(classifyAttendantInquiry('Tengo un dolor muy fuerte, ¿qué medicamento tomo?'), 'medical');
    assert.strictEqual(classifyAttendantInquiry('How much do lenses cost and do you have discounts?'), 'pricing');
    assert.strictEqual(classifyAttendantInquiry('¿Cuánto cuesta la consulta y los lentes?'), 'pricing');
    assert.strictEqual(classifyAttendantInquiry('Can I book an eye exam appointment for tomorrow?'), 'appointment');
    assert.strictEqual(classifyAttendantInquiry('¿Puedo agendar una cita para examen de la vista?'), 'appointment');
    assert.strictEqual(classifyAttendantInquiry('Hello there, who are you?'), 'general');
  });

  it('6. Operating hours description formatting is bilingual and contains canonical schedule', () => {
    const hoursEn = formatOperatingHoursDescription(farmaciasSchedule, 'en');
    const hoursEs = formatOperatingHoursDescription(farmaciasSchedule, 'es');

    assert.ok(hoursEn.includes('Monday through Saturday (closed Sunday)'));
    assert.ok(hoursEn.includes('07:00 to 22:00'));
    assert.ok(hoursEs.includes('Lunes a sábado (cerrado los domingos)'));
    assert.ok(hoursEs.includes('07:00 a 22:00'));
  });

  it('7. Farmacias Benavides attendant: deterministic status response grounds open/closed status', () => {
    const carlos = BUSINESS_ATTENDANT_ROSTER['carlos-farmacia-attendant'];
    // Saturday 14:00 (Open)
    const openStatus = evaluateBusinessOperationalStatus(
      farmaciasSchedule,
      '2026-10-24T14:00:00-06:00'
    );
    const respEn = getDeterministicAttendantResponse(
      carlos,
      openStatus,
      farmaciasSchedule,
      'status',
      'Are you open right now?',
      'en'
    );
    assert.ok(respEn.includes('Farmacias Benavides'));
    assert.ok(respEn.includes('Carlos Méndez'));
    assert.ok(respEn.includes('OPEN'));
    assert.ok(respEn.includes('local time (Ciudad Juárez)'));

    // Sunday 14:00 (Closed)
    const closedStatus = evaluateBusinessOperationalStatus(
      farmaciasSchedule,
      '2026-10-25T14:00:00-06:00'
    );
    const respEs = getDeterministicAttendantResponse(
      carlos,
      closedStatus,
      farmaciasSchedule,
      'status',
      '¿Está abierta ahorita?',
      'es'
    );
    assert.ok(respEs.includes('Farmacias Benavides'));
    assert.ok(respEs.includes('CERRADO'));
    assert.ok(respEs.includes('hora local de Ciudad Juárez'));
  });

  it('8. Farmacias Benavides attendant: strictly refuses medical diagnoses and prescriptions', () => {
    const carlos = BUSINESS_ATTENDANT_ROSTER['carlos-farmacia-attendant'];
    const status = evaluateBusinessOperationalStatus(farmaciasSchedule, '2026-10-24T14:00:00-06:00');

    const medRespEn = getDeterministicAttendantResponse(
      carlos,
      status,
      farmaciasSchedule,
      'medical',
      'What antibiotic should I take for an infection?',
      'en'
    );
    assert.ok(medRespEn.includes('not authorized to provide medical advice'));
    assert.ok(medRespEn.includes('consult a certified doctor or the licensed pharmacist'));

    const medRespEs = getDeterministicAttendantResponse(
      carlos,
      status,
      farmaciasSchedule,
      'medical',
      '¿Qué medicamento me receta para una infección?',
      'es'
    );
    assert.ok(medRespEs.includes('no estoy autorizado para ofrecer asesoría médica'));
    assert.ok(medRespEs.includes('médico certificado o al farmacéutico en turno'));
  });

  it('9. Farmacias Benavides attendant: strictly refuses to fabricate medication inventory or pricing', () => {
    const carlos = BUSINESS_ATTENDANT_ROSTER['carlos-farmacia-attendant'];
    const status = evaluateBusinessOperationalStatus(farmaciasSchedule, '2026-10-24T14:00:00-06:00');

    const invRespEn = getDeterministicAttendantResponse(
      carlos,
      status,
      farmaciasSchedule,
      'inventory',
      'Do you have ibuprofen in stock and what is the price?',
      'en'
    );
    assert.ok(invRespEn.includes('do not have access to live medication inventory or pricing'));
    assert.ok(invRespEn.includes('Av. Benito Juárez 610'));

    const priceRespEs = getDeterministicAttendantResponse(
      carlos,
      status,
      farmaciasSchedule,
      'pricing',
      '¿Cuánto cuesta la caja de aspirinas?',
      'es'
    );
    assert.ok(priceRespEs.includes('No dispongo de información de precios'));
    assert.ok(priceRespEs.includes('Av. Benito Juárez 610'));
  });

  it('10. Óptica Fashion attendant: strictly refuses to fabricate frame inventory or appointments', () => {
    const valeria = BUSINESS_ATTENDANT_ROSTER['valeria-optica-attendant'];
    const status = evaluateBusinessOperationalStatus(opticaSchedule, '2026-10-24T14:00:00-06:00');

    const apptRespEn = getDeterministicAttendantResponse(
      valeria,
      status,
      opticaSchedule,
      'appointment',
      'Can you book me an eye exam appointment for tomorrow 10am?',
      'en'
    );
    assert.ok(apptRespEn.includes('do not include an appointment scheduling system'));
    assert.ok(apptRespEn.includes('Av. Benito Juárez 859'));

    const apptRespEs = getDeterministicAttendantResponse(
      valeria,
      status,
      opticaSchedule,
      'appointment',
      '¿Puedo agendar una cita para examen de la vista?',
      'es'
    );
    assert.ok(apptRespEs.includes('no contamos con un sistema de agenda o reserva de citas'));
    assert.ok(apptRespEs.includes('Av. Benito Juárez 859'));

    const invRespEs = getDeterministicAttendantResponse(
      valeria,
      status,
      opticaSchedule,
      'inventory',
      '¿Tienen armazones Ray-Ban en existencia?',
      'es'
    );
    assert.ok(invRespEs.includes('no tengo acceso al catálogo de armazones'));
  });

  it('11. Location queries return authoritative address and spatial context along corridor', () => {
    const carlos = BUSINESS_ATTENDANT_ROSTER['carlos-farmacia-attendant'];
    const valeria = BUSINESS_ATTENDANT_ROSTER['valeria-optica-attendant'];
    const status = evaluateBusinessOperationalStatus(farmaciasSchedule, '2026-10-24T14:00:00-06:00');

    const locCarlos = getDeterministicAttendantResponse(
      carlos,
      status,
      farmaciasSchedule,
      'location',
      'Where is your store?',
      'en'
    );
    assert.ok(locCarlos.includes('Av. Benito Juárez 610'));
    assert.ok(locCarlos.includes('Paso del Norte International Bridge'));

    const locValeria = getDeterministicAttendantResponse(
      valeria,
      status,
      opticaSchedule,
      'location',
      '¿Dónde se ubica la óptica?',
      'es'
    );
    assert.ok(locValeria.includes('Av. Benito Juárez 859'));
    assert.ok(locValeria.includes('Farmacias Benavides'));
  });

  it('12. Carlos Méndez uses a neutral pharmacy avatar (💊) and not a healthcare professional avatar', () => {
    const carlos = BUSINESS_ATTENDANT_ROSTER['carlos-farmacia-attendant'];
    assert.strictEqual(carlos.avatar, '💊');
    assert.notStrictEqual(carlos.avatar, '👨‍⚕️');

    const valeria = BUSINESS_ATTENDANT_ROSTER['valeria-optica-attendant'];
    assert.strictEqual(valeria.avatar, '👓');
  });

  it('13. Farmacias Benavides preserves canonical ID, coordinates, and explicitly marks Chamizal neighborhood as unverified', () => {
    const carlos = BUSINESS_ATTENDANT_ROSTER['carlos-farmacia-attendant'];
    assert.strictEqual(carlos.businessId, 'pharmacy:farmacias-benavides');
    assert.strictEqual(carlos.coordinates.latitude, 31.745488);
    assert.strictEqual(carlos.coordinates.longitude, -106.486276);
    assert.ok(carlos.verifiedAddress.startsWith('Av. Benito Juárez 610'));
    assert.ok(carlos.verifiedAddress.includes('colonia Chamizal unverified'));
  });
});

