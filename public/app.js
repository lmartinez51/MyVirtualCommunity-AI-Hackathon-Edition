/**
 * MyVirtualCommunity AI Hackathon Edition
 * Bilingual Interactive Web Demo Logic (Milestone 3: Spatial Slice v0.1)
 */

(function () {
  'use strict';

  // Spatial corridor entities definition (synchronized with /api/spatial/corridor)
  const CORRIDOR_ENTITIES = {
    'infrastructure:puente-paso-del-norte': {
      id: 'infrastructure:puente-paso-del-norte',
      businessId: null,
      name: 'Puente Internacional Paso del Norte',
      popularName: 'Puente Santa Fe',
      entityType: 'infrastructure',
      coordinates: { latitude: 31.74765, longitude: -106.48698 },
      zoom: 17,
      address: 'Av. Benito Juárez s/n, Centro, 32000 Juárez, Chih., México',
      sources: 'Fideicomiso de Puentes Fronterizos de Chihuahua • OpenStreetMap (way/1531523734 / way/436474488)',
      description: {
        en: 'Historic international border bridge connecting Ciudad Juárez and El Paso, Texas across the Rio Grande. Marker represents the bridge structure span (31.74765° N, 106.48698° W), located 203 m north of the Mexican toll plaza (Plaza de Cobro C-51) and 237–246 m north of the commercial establishments. Live border wait times and traffic are not simulated.',
        es: 'Puente internacional que conecta Ciudad Juárez y El Paso, Texas sobre el Río Bravo. El marcador representa la estructura del puente (31.74765° N, 106.48698° W), a 203 m al norte de la caseta de cobro mexicana (Plaza de Cobro C-51) y a 237–246 m de los comercios. Los tiempos de espera y el tráfico no están simulados.'
      }
    },
    'pharmacy:farmacias-benavides': {
      id: 'pharmacy:farmacias-benavides',
      businessId: 'pharmacy:farmacias-benavides',
      name: 'Farmacias Benavides',
      popularName: 'Av. Juárez 610 / 852',
      entityType: 'pharmacy',
      coordinates: { latitude: 31.745488, longitude: -106.486276 },
      zoom: 18,
      address: 'Av. Benito Juárez 610, Chamizal, 32000 Juárez, Chih., México',
      sources: 'INEGI DENUE (IDEE: 6231329 - SUC M100 CHAMIZAL) • Google Maps Place • World Core Registry',
      description: {
        en: 'Independently verified retail pharmacy (INEGI DENUE IDEE 6231329: SUC M100 CHAMIZAL) offering prescription medicines, personal care, and convenience goods along Avenida Benito Juárez. Located 22.02 m across the avenue from Óptica Fashion and 46.06 m south of the bridge entrance toll plaza.',
        es: 'Farmacia de autoservicio verificada independientemente (INEGI DENUE IDEE 6231329: SUC M100 CHAMIZAL) con medicamentos con receta y artículos de conveniencia sobre Avenida Benito Juárez. Ubicada a 22.02 m frente a Óptica Fashion y a 46.06 m al sur de la caseta de cobro del puente.'
      }
    },
    'optical:optica-fashion': {
      id: 'optical:optica-fashion',
      businessId: 'optical:optica-fashion',
      name: 'Óptica Fashion',
      popularName: 'Av. Juárez 859',
      entityType: 'optical',
      coordinates: { latitude: 31.745562, longitude: -106.486492 },
      zoom: 18,
      address: 'Av. Benito Juárez 859, Centro, 32000 Juárez, Chih., México',
      sources: 'Google Maps Place (Provisional — Unresolved in INEGI/OSM) • World Core Registry',
      description: {
        en: 'Prescription eyewear, optometry consultations, and frame styling boutique along Avenida Benito Juárez (provisional user-supplied reference; unresolved in INEGI/OSM open data). Located 22.02 m across the avenue from Farmacias Benavides and 37.00 m south of the bridge entrance toll plaza.',
        es: 'Lentes graduados, consultas optométricas y armazones en Avenida Benito Juárez (referencia provisional provista por el usuario; no registrada en INEGI/OSM). Ubicada a 22.02 m frente a Farmacias Benavides y a 37.00 m al sur de la caseta de cobro del puente.'
      }
    }
  };

  // Supported languages & translations
  const I18N = {
    en: {
      title: 'MyVirtualCommunity AI',
      subtitle: 'Authoritative Local Business Assistant Grounded in World Simulation',
      regionalInfo: 'Ciudad Juárez (America/Ciudad_Juarez, MST/MDT) • Authoritative Simulation Data',
      simStatusConnected: 'World Core Connected',
      simStatusOffline: 'World Core Offline',
      lblBusiness: 'Select Business',
      hintBusiness: 'Verified customer-facing business registered in World Core.',
      lblEvalMode: 'Evaluation Time',
      modeCurrentTitle: 'Current World Clock',
      modeCurrentDesc: 'Evaluate at live simulation time',
      modeSpecificTitle: 'Specific Date & Time',
      modeSpecificDesc: 'Evaluate admission at a chosen instant',
      lblEvalTime: 'Local Date & Time (Ciudad Juárez)',
      hintEvalTime: 'Interpreted strictly in America/Ciudad_Juarez (not your browser\'s local timezone).',
      btnResetTime: 'Reset to Current',
      lblQuickScenarios: 'Quick Test Scenarios:',
      lblSuggestions: 'Suggested Questions',
      lblQuestion: 'Your Question',
      hintQuestion: 'Press Ask or Ctrl+Enter to submit.',
      btnSubmit: 'Ask Assistant',
      btnSubmitting: 'Consulting...',
      idleTitle: 'Ready for Your Query',
      idleDesc: 'Choose a business, evaluation time, and question above to get an authoritative response.',
      loadingTitle: 'Retrieving Authoritative Snapshot...',
      loadingDesc: 'Consulting World Core simulation and formulating response with Muse Spark.',
      errorTitle: 'Service Error',
      errorWorldCoreDown: 'The authoritative World Core server is currently unreachable. Please ensure the local simulation server is running.',
      factsTitle: 'Authoritative World Core Facts',
      sourceTag: 'Deterministic Source',
      lblFactName: 'Canonical Name',
      lblFactStatus: 'Operational Status',
      lblFactAdmission: 'Admission Status',
      lblFactHours: 'Scheduled Hours',
      lblFactCutoff: 'Last Entry Cutoff',
      lblFactMode: 'Evaluation Mode',
      modeCurrentPill: 'Live Simulation Clock',
      modeSpecificPill: 'Specific Evaluation Time',
      lblFactCivilTime: 'Local Evaluated Time',
      lblFactTime: 'World Time (Evaluated UTC)',
      lblFactTz: 'Regional Timezone',
      lblFactSummary: 'World Core Directive',
      dstWarning: 'Ambiguous local time: This wall clock time occurs twice due to Daylight Saving Time fall-back. Evaluated using the earlier occurrence (01:30 MDT, UTC-6 / 07:30 UTC).',
      guaranteeNote: 'Factual status and admission rules are derived deterministically from the World Core simulation and cannot be fabricated or altered by the language model.',
      diagnosticsSummary: 'Technical Diagnostics & Usage',
      statusOpen: 'OPEN',
      statusClosingSoon: 'CLOSING SOON',
      statusClosed: 'CLOSED',
      admissionPermitted: 'Permitted',
      admissionDenied: 'Not Permitted (Cutoff)',
      footerNote: 'MyVirtualCommunity AI Hackathon Edition • Powered by Meta Muse Spark 1.3 & Ciudad Juárez World Simulation',
      
      // Spatial Slice Translations
      spatialBadge: 'Spatial Slice v0.1',
      spatialTitle: 'Avenida Benito Juárez & Puente Internacional Paso del Norte',
      spatialDesc: 'Explore the authentic international border corridor, inspect verified landmarks, and query real business operating status.',
      lblCorridorEntities: 'Select Landmark:',
      txtChipBridge: 'Puente Santa Fe',
      txtChipBenavides: 'Farmacias Benavides',
      txtChipOptica: 'Óptica Fashion',
      txtBtnAskEntity: 'Ask About This Business',
      lblEntityAddress: 'Verified Address',
      lblEntityCoords: 'Geographic Coordinates',
      lblEntitySources: 'Authoritative Sources',
      mapFallbackTitle: 'Map Unavailable',
      mapFallbackDesc: 'Interactive map tiles could not be loaded. You can still inspect verified landmark details and query business status below.',
      btnRetryMap: 'Retry Map',
      mapLibraryUnavailableTitle: 'Map Library Unavailable',
      mapLibraryUnavailableDesc: 'The interactive mapping library (Leaflet) could not be loaded. Check your connection or assets and retry.',
      mapTilesUnavailableTitle: 'Map Tiles Unavailable',
      mapTilesUnavailableDesc: 'Interactive street map tiles could not be loaded from online providers. You can still inspect verified landmark details and query business status below.',
      badges: {
        infrastructure: 'Border Infrastructure',
        pharmacy: 'Pharmacy',
        optical: 'Optical Shop'
      },

      quickScenarios: [
        {
          date: '2026-10-24T17:40',
          label: 'Sat 5:40 PM (Open & Admitting)',
          question: 'Would I still be admitted if I arrive at 5:40 PM?'
        },
        {
          date: '2026-10-24T17:50',
          label: 'Sat 5:50 PM (Closing Soon / Cutoff)',
          question: 'Would I still be admitted if I arrive at 5:50 PM?'
        },
        {
          date: '2026-10-24T18:05',
          label: 'Sat 6:05 PM (Closed)',
          question: 'Will this business be open at 6:05 PM?'
        },
        {
          date: '2026-10-25T12:00',
          label: 'Sun 12:00 PM (Closed / Sunday)',
          question: 'Is this business open on Sunday at 12:00 PM?'
        }
      ],
      suggestions: {
        'pharmacy:farmacias-benavides': [
          'Is Farmacias Benavides open right now, and what time does it close?',
          'What are the operating days and hours for Farmacias Benavides?',
          'Will Farmacias Benavides be open on Saturday at 9:00 PM?',
          'Is Farmacias Benavides open on Sunday at 12:00 PM?'
        ],
        'optical:optica-fashion': [
          'Is Óptica Fashion open right now, and what time does it close?',
          'What are the operating days and hours for Óptica Fashion?',
          'Will Óptica Fashion be open at 4:30 PM?',
          'Is Óptica Fashion open on Sunday?'
        ],
        'restaurant:wing-spot': [
          'Would I still be admitted if I arrive at 5:50 PM?',
          'Will this business be open at 5:30 PM?',
          'Is Wing Spot Gómez Morín open right now, and what time does it close?',
          'What are the operating hours and days for Wing Spot Gómez Morín?'
        ],
        'restaurant:pasteleria-tia-lupe': [
          'Would I still be admitted if I arrive at 5:50 PM?',
          'Will Pastelerías La Tía Lupe be open at 5:30 PM?',
          'Is Pastelerías La Tía Lupe open right now, and what time does it close?',
          'What are the operating days and hours for Pastelerías La Tía Lupe?'
        ]
      }
    },
    es: {
      title: 'MyVirtualCommunity AI',
      subtitle: 'Asistente de Negocios Locales Fundamentado en la Simulación del Mundo',
      regionalInfo: 'Ciudad Juárez (America/Ciudad_Juarez, MST/MDT) • Datos Autoritativos de Simulación',
      simStatusConnected: 'World Core Conectado',
      simStatusOffline: 'World Core Desconectado',
      lblBusiness: 'Seleccionar Negocio',
      hintBusiness: 'Negocio verificado de atención al público registrado en el World Core.',
      lblEvalMode: 'Hora de Evaluación',
      modeCurrentTitle: 'Reloj Actual de Simulación',
      modeCurrentDesc: 'Evaluar al tiempo en vivo de la simulación',
      modeSpecificTitle: 'Fecha y Hora Específica',
      modeSpecificDesc: 'Evaluar admisión en un instante elegido',
      lblEvalTime: 'Fecha y Hora Local (Ciudad Juárez)',
      hintEvalTime: 'Interpretada estrictamente en America/Ciudad_Juarez (no la zona de tu navegador).',
      btnResetTime: 'Volver al Actual',
      lblQuickScenarios: 'Escenarios Rápidos:',
      lblSuggestions: 'Preguntas Sugeridas',
      lblQuestion: 'Tu Pregunta',
      hintQuestion: 'Presiona Consultar o Ctrl+Enter para enviar.',
      btnSubmit: 'Consultar Asistente',
      btnSubmitting: 'Consultando...',
      idleTitle: 'Listo para tu Consulta',
      idleDesc: 'Selecciona un negocio, hora de evaluación y pregunta para obtener una respuesta autoritativa.',
      loadingTitle: 'Obteniendo Snapshot Autoritativo...',
      loadingDesc: 'Consultando la simulación del World Core y formulando la respuesta con Muse Spark.',
      errorTitle: 'Error del Servicio',
      errorWorldCoreDown: 'El servidor autoritativo World Core no está disponible. Asegúrate de que el servidor local de simulación esté en ejecución.',
      factsTitle: 'Datos Autoritativos del World Core',
      sourceTag: 'Fuente Determinista',
      lblFactName: 'Nombre Canónico',
      lblFactStatus: 'Estado Operativo',
      lblFactAdmission: 'Estado de Admisión',
      lblFactHours: 'Horario Programado',
      lblFactCutoff: 'Límite de Admisión',
      lblFactMode: 'Modo de Evaluación',
      modeCurrentPill: 'Reloj en Vivo de Simulación',
      modeSpecificPill: 'Hora Específica Evaluada',
      lblFactCivilTime: 'Hora Local Evaluada',
      lblFactTime: 'Tiempo del Mundo (UTC Evaluado)',
      lblFactTz: 'Zona Horaria Regional',
      lblFactSummary: 'Directiva del World Core',
      dstWarning: 'Hora local ambigua: Esta hora ocurre dos veces por el cambio de horario de verano. Evaluada usando la primera ocurrencia (01:30 MDT, UTC-6 / 07:30 UTC).',
      guaranteeNote: 'El estado operativo y las reglas de admisión se derivan de forma determinista desde la simulación del World Core y no pueden ser alteradas ni inventadas por el modelo de lenguaje.',
      diagnosticsSummary: 'Diagnósticos Técnicos y Uso',
      statusOpen: 'ABIERTO',
      statusClosingSoon: 'POR CERRAR',
      statusClosed: 'CERRADO',
      admissionPermitted: 'Permitido',
      admissionDenied: 'No Permitido (Límite Superado)',
      footerNote: 'MyVirtualCommunity AI Hackathon Edition • Impulsado por Meta Muse Spark 1.3 y la Simulación de Ciudad Juárez',
      
      // Spatial Slice Translations
      spatialBadge: 'Spatial Slice v0.1',
      spatialTitle: 'Avenida Benito Juárez y Puente Internacional Paso del Norte',
      spatialDesc: 'Explora el auténtico corredor fronterizo internacional, inspecciona puntos de interés verificados y consulta el estado operativo real de los negocios.',
      lblCorridorEntities: 'Seleccionar Lugar:',
      txtChipBridge: 'Puente Santa Fe',
      txtChipBenavides: 'Farmacias Benavides',
      txtChipOptica: 'Óptica Fashion',
      txtBtnAskEntity: 'Consultar Sobre Este Negocio',
      lblEntityAddress: 'Dirección Verificada',
      lblEntityCoords: 'Coordenadas Geográficas',
      lblEntitySources: 'Fuentes Autoritativas',
      mapFallbackTitle: 'Mapa No Disponible',
      mapFallbackDesc: 'No se pudieron cargar los mapas interactivos. Aún puedes inspeccionar los detalles de los lugares verificados y consultar el estado del negocio abajo.',
      btnRetryMap: 'Reintentar mapa',
      mapLibraryUnavailableTitle: 'Biblioteca de Mapa No Disponible',
      mapLibraryUnavailableDesc: 'No se pudo cargar la biblioteca de mapas interactivos (Leaflet). Revisa tu conexión o recursos y reintenta.',
      mapTilesUnavailableTitle: 'Capa de Mapa No Disponible',
      mapTilesUnavailableDesc: 'No se pudieron cargar los mosaicos de calles interactivos de los proveedores en línea. Aún puedes inspeccionar los lugares verificados y consultar el estado del negocio abajo.',
      badges: {
        infrastructure: 'Infraestructura Fronteriza',
        pharmacy: 'Farmacia',
        optical: 'Óptica'
      },

      quickScenarios: [
        {
          date: '2026-10-24T17:40',
          label: 'Sáb 5:40 PM (Abierto y Admisión)',
          question: '¿Todavía me dejarán entrar si llego a las 5:40 p. m.?'
        },
        {
          date: '2026-10-24T17:50',
          label: 'Sáb 5:50 PM (Por Cerrar / Límite)',
          question: '¿Todavía me dejarán entrar si llego a las 5:50 p. m.?'
        },
        {
          date: '2026-10-24T18:05',
          label: 'Sáb 6:05 PM (Cerrado)',
          question: '¿Estará abierto a las 6:05 p. m.?'
        },
        {
          date: '2026-10-25T12:00',
          label: 'Dom 12:00 PM (Cerrado / Domingo)',
          question: '¿Abre este negocio el domingo a las 12:00 p. m.?'
        }
      ],
      suggestions: {
        'pharmacy:farmacias-benavides': [
          '¿Está abierta Farmacias Benavides en este momento y a qué hora cierra?',
          '¿Cuáles son los días y horarios de atención de Farmacias Benavides?',
          '¿Estará abierta Farmacias Benavides el sábado a las 9:00 p. m.?',
          '¿Abre Farmacias Benavides los domingos a las 12:00 p. m.?'
        ],
        'optical:optica-fashion': [
          '¿Está abierta Óptica Fashion en este momento y a qué hora cierra?',
          '¿Cuáles son los días y horarios de atención de Óptica Fashion?',
          '¿Estará abierta Óptica Fashion a las 4:30 p. m.?',
          '¿Abre Óptica Fashion los domingos?'
        ],
        'restaurant:wing-spot': [
          '¿Todavía me dejarán entrar si llego a las 5:50 p. m.?',
          '¿Estará abierto a las 5:30 p. m.?',
          '¿Está abierto Wing Spot Gómez Morín y a qué hora cierra?',
          '¿Cuáles son los días y horarios de atención de Wing Spot Gómez Morín?'
        ],
        'restaurant:pasteleria-tia-lupe': [
          '¿Todavía me dejarán entrar si llego a las 5:50 p. m.?',
          '¿Estará abierta Pastelerías La Tía Lupe a las 5:30 p. m.?',
          '¿Está abierta Pastelerías La Tía Lupe y a qué hora cierra?',
          '¿Cuáles son los días y horarios de Pastelerías La Tía Lupe?'
        ]
      }
    }
  };

  // State
  let currentLang = 'en';
  let evalMode = 'current';
  let selectedEntityId = 'pharmacy:farmacias-benavides';
  let leafletMap = null;
  const mapMarkers = {};

  // DOM Elements
  const btnLangEn = document.getElementById('btn-lang-en');
  const btnLangEs = document.getElementById('btn-lang-es');
  const form = document.getElementById('assistant-form');
  const businessSelect = document.getElementById('business-select');
  const questionInput = document.getElementById('question-input');
  const charCounter = document.getElementById('char-counter');
  const btnSubmit = document.getElementById('btn-submit');
  const btnSpinner = document.getElementById('btn-spinner');
  const txtSubmitBtn = document.getElementById('txt-submit-btn');
  const suggestionsContainer = document.getElementById('suggestions-container');

  // Milestone 2 DOM Elements
  const radioModeCurrent = document.getElementById('radio-mode-current');
  const radioModeSpecific = document.getElementById('radio-mode-specific');
  const lblModeCurrent = document.getElementById('lbl-mode-current');
  const lblModeSpecific = document.getElementById('lbl-mode-specific');
  const evalTimeContainer = document.getElementById('eval-time-container');
  const evalTimeInput = document.getElementById('eval-time-input');
  const btnResetTime = document.getElementById('btn-reset-time');
  const scenarioChipsContainer = document.getElementById('scenario-chips-container');

  const stateIdle = document.getElementById('state-idle');
  const stateLoading = document.getElementById('state-loading');
  const stateError = document.getElementById('state-error');
  const stateResult = document.getElementById('state-result');

  const errorMessageText = document.getElementById('error-message-text');
  const errorHintText = document.getElementById('error-hint-text');

  const assistantAnswerText = document.getElementById('assistant-answer-text');
  const lblLatency = document.getElementById('lbl-latency');

  const factCanonicalName = document.getElementById('fact-canonical-name');
  const factStatus = document.getElementById('fact-status');
  const factAdmission = document.getElementById('fact-admission');
  const factHours = document.getElementById('fact-hours');
  const factCutoff = document.getElementById('fact-cutoff');
  const factEvalMode = document.getElementById('fact-eval-mode');
  const factCivilTime = document.getElementById('fact-civil-time');
  const factWorldTime = document.getElementById('fact-world-time');
  const factTimezone = document.getElementById('fact-timezone');
  const factSummary = document.getElementById('fact-summary');
  const dstAmbiguityWarning = document.getElementById('dst-ambiguity-warning');

  const diagModel = document.getElementById('diag-model');
  const diagLatency = document.getElementById('diag-latency');
  const diagLang = document.getElementById('diag-lang');
  const diagPromptTokens = document.getElementById('diag-prompt-tokens');
  const diagCompletionTokens = document.getElementById('diag-completion-tokens');
  const diagReasoningTokens = document.getElementById('diag-reasoning-tokens');
  const diagTotalTokens = document.getElementById('diag-total-tokens');

  // I18N Updater
  function updateLanguage(lang) {
    currentLang = lang;
    const t = I18N[lang];

    btnLangEn.classList.toggle('active', lang === 'en');
    btnLangEs.classList.toggle('active', lang === 'es');

    document.getElementById('txt-title').textContent = t.title;
    document.getElementById('txt-subtitle').textContent = t.subtitle;
    document.getElementById('txt-regional-info').textContent = t.regionalInfo;
    document.getElementById('txt-sim-status').textContent = t.simStatusConnected;

    // Spatial corridor strings
    const badgeEl = document.getElementById('txt-spatial-badge');
    if (badgeEl) badgeEl.textContent = t.spatialBadge;
    const sTitle = document.getElementById('txt-spatial-title');
    if (sTitle) sTitle.textContent = t.spatialTitle;
    const sDesc = document.getElementById('txt-spatial-desc');
    if (sDesc) sDesc.textContent = t.spatialDesc;
    const lblCE = document.getElementById('lbl-corridor-entities');
    if (lblCE) lblCE.textContent = t.lblCorridorEntities;
    const cBridge = document.getElementById('txt-chip-bridge');
    if (cBridge) cBridge.textContent = t.txtChipBridge;
    const cBen = document.getElementById('txt-chip-benavides');
    if (cBen) cBen.textContent = t.txtChipBenavides;
    const cOpt = document.getElementById('txt-chip-optica');
    if (cOpt) cOpt.textContent = t.txtChipOptica;
    const btnAsk = document.getElementById('txt-btn-ask-entity');
    if (btnAsk) btnAsk.textContent = t.txtBtnAskEntity;
    const lblAddr = document.getElementById('lbl-entity-address');
    if (lblAddr) lblAddr.textContent = t.lblEntityAddress;
    const lblCoords = document.getElementById('lbl-entity-coords');
    if (lblCoords) lblCoords.textContent = t.lblEntityCoords;
    const lblSrc = document.getElementById('lbl-entity-sources');
    if (lblSrc) lblSrc.textContent = t.lblEntitySources;
    const mapFbTitle = document.getElementById('txt-map-fallback-title');
    if (mapFbTitle) mapFbTitle.textContent = t.mapFallbackTitle;
    const mapFbDesc = document.getElementById('txt-map-fallback-desc');
    if (mapFbDesc) mapFbDesc.textContent = t.mapFallbackDesc;
    const btnRetry = document.getElementById('btn-retry-map');
    if (btnRetry) btnRetry.textContent = t.btnRetryMap || 'Retry Map';

    document.getElementById('lbl-business').textContent = t.lblBusiness;
    document.getElementById('hint-business').textContent = t.hintBusiness;

    document.getElementById('lbl-eval-mode').textContent = t.lblEvalMode;
    document.getElementById('txt-mode-current-title').textContent = t.modeCurrentTitle;
    document.getElementById('txt-mode-current-desc').textContent = t.modeCurrentDesc;
    document.getElementById('txt-mode-specific-title').textContent = t.modeSpecificTitle;
    document.getElementById('txt-mode-specific-desc').textContent = t.modeSpecificDesc;
    document.getElementById('lbl-eval-time').textContent = t.lblEvalTime;
    document.getElementById('hint-eval-time').textContent = t.hintEvalTime;
    btnResetTime.textContent = t.btnResetTime;
    document.getElementById('lbl-quick-scenarios').textContent = t.lblQuickScenarios;

    document.getElementById('lbl-suggestions').textContent = t.lblSuggestions;
    document.getElementById('lbl-question').textContent = t.lblQuestion;
    document.getElementById('hint-question').textContent = t.hintQuestion;
    txtSubmitBtn.textContent = t.btnSubmit;

    document.getElementById('txt-idle-title').textContent = t.idleTitle;
    document.getElementById('txt-idle-desc').textContent = t.idleDesc;
    document.getElementById('txt-loading-title').textContent = t.loadingTitle;
    document.getElementById('txt-loading-desc').textContent = t.loadingDesc;
    document.getElementById('txt-error-title').textContent = t.errorTitle;

    document.getElementById('txt-facts-title').textContent = t.factsTitle;
    document.getElementById('txt-source-tag').textContent = t.sourceTag;
    document.getElementById('lbl-fact-name').textContent = t.lblFactName;
    document.getElementById('lbl-fact-status').textContent = t.lblFactStatus;
    document.getElementById('lbl-fact-admission').textContent = t.lblFactAdmission;
    document.getElementById('lbl-fact-hours').textContent = t.lblFactHours;
    document.getElementById('lbl-fact-cutoff').textContent = t.lblFactCutoff;
    document.getElementById('lbl-fact-mode').textContent = t.lblFactMode;
    document.getElementById('lbl-fact-civil-time').textContent = t.lblFactCivilTime;
    document.getElementById('lbl-fact-time').textContent = t.lblFactTime;
    document.getElementById('lbl-fact-tz').textContent = t.lblFactTz;
    document.getElementById('lbl-fact-summary').textContent = t.lblFactSummary;
    document.getElementById('txt-dst-warning').textContent = t.dstWarning;
    document.getElementById('txt-guarantee-note').textContent = t.guaranteeNote;
    document.getElementById('txt-diagnostics-summary').textContent = t.diagnosticsSummary;
    document.getElementById('txt-footer-note').textContent = t.footerNote;

    renderEntityDetail(selectedEntityId);
    renderSuggestions();
    renderScenarioChips();
  }

  // Render entity detail panel
  function renderEntityDetail(entityId) {
    const entity = CORRIDOR_ENTITIES[entityId];
    if (!entity) return;

    const t = I18N[currentLang];
    const badgeEl = document.getElementById('entity-type-badge');
    const titleEl = document.getElementById('entity-title');
    const popularEl = document.getElementById('entity-popular-name');
    const descEl = document.getElementById('entity-description');
    const addrEl = document.getElementById('entity-address');
    const coordsEl = document.getElementById('entity-coords');
    const sourcesEl = document.getElementById('entity-sources');
    const actionsEl = document.getElementById('entity-actions');

    if (badgeEl) {
      badgeEl.className = 'entity-type-badge ' + entity.entityType;
      badgeEl.textContent = t.badges[entity.entityType] || entity.entityType.toUpperCase();
    }
    if (titleEl) titleEl.textContent = entity.name;
    if (popularEl) popularEl.textContent = `(${entity.popularName})`;
    if (descEl) descEl.textContent = entity.description[currentLang] || entity.description.en;
    if (addrEl) addrEl.textContent = entity.address;
    if (coordsEl) {
      coordsEl.textContent = `${entity.coordinates.latitude.toFixed(6)}° N, ${Math.abs(entity.coordinates.longitude).toFixed(6)}° W`;
    }
    if (sourcesEl) sourcesEl.textContent = entity.sources;

    if (actionsEl) {
      if (entity.businessId) {
        actionsEl.style.display = 'block';
      } else {
        // Infrastructure is non-commercial
        actionsEl.style.display = 'none';
      }
    }
  }

  // Select a spatial corridor entity
  function selectEntity(entityId) {
    if (!CORRIDOR_ENTITIES[entityId]) return;
    selectedEntityId = entityId;
    const entity = CORRIDOR_ENTITIES[entityId];

    // Update chips active state
    document.querySelectorAll('.entity-chip').forEach((btn) => {
      btn.classList.toggle('active', btn.getAttribute('data-entity-id') === entityId);
    });

    // Update custom marker pin active styling
    document.querySelectorAll('.custom-map-pin').forEach((pin) => {
      pin.classList.remove('active');
    });
    const pinEl = document.getElementById(`marker-${entityId}`);
    if (pinEl) {
      pinEl.classList.add('active');
    }

    // Pan map smoothly if loaded
    if (leafletMap) {
      leafletMap.panTo([entity.coordinates.latitude, entity.coordinates.longitude], {
        animate: true,
        duration: 0.5
      });
      if (mapMarkers[entityId]) {
        mapMarkers[entityId].openPopup();
      }
    }

    // Update detail panel
    renderEntityDetail(entityId);

    // Synchronize business select if entity is a commercial establishment
    if (entity.businessId) {
      if (businessSelect.value !== entity.businessId) {
        businessSelect.value = entity.businessId;
        renderSuggestions();
      }
    }
  }

  // Selected tile providers: OpenStreetMap France (primary) providing authentic, high-resolution OpenStreetMap data
  // without localhost blocking or watermarks, backed by Esri World Street Map as high-reliability secondary.
  const TILE_PROVIDERS = [
    {
      name: 'OpenStreetMap France',
      url: 'https://{s}.tile.openstreetmap.fr/osmfr/{z}/{x}/{y}.png',
      subdomains: 'abc',
      maxZoom: 20,
      attribution: '&copy; OpenStreetMap France | &copy; <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener">OpenStreetMap</a> contributors'
    },
    {
      name: 'Esri World Street Map',
      url: 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Street_Map/MapServer/tile/{z}/{y}/{x}',
      subdomains: '',
      maxZoom: 19,
      attribution: 'Tiles &copy; Esri &mdash; Source: Esri, DeLorme, NAVTEQ, USGS, Intermap, iPC, NRCAN, Esri Japan, METI, Esri China (Hong Kong), Esri (Thailand), TomTom'
    }
  ];

  let spatialListenersAttached = false;

  // Initialize interactive Leaflet map
  function initMap() {
    const fallbackContainer = document.getElementById('map-fallback');
    const fallbackTitle = document.getElementById('txt-map-fallback-title');
    const fallbackDesc = document.getElementById('txt-map-fallback-desc');
    const btnRetry = document.getElementById('btn-retry-map');

    function showMapFallback(title, desc) {
      if (fallbackTitle && title) fallbackTitle.textContent = title;
      if (fallbackDesc && desc) fallbackDesc.textContent = desc;
      if (fallbackContainer) {
        fallbackContainer.hidden = false;
        fallbackContainer.style.display = 'flex';
      }
    }

    function hideMapFallback() {
      if (fallbackContainer) {
        fallbackContainer.hidden = true;
        fallbackContainer.style.display = 'none';
      }
    }

    if (btnRetry) {
      btnRetry.onclick = () => {
        initMap();
      };
    }

    if (typeof L === 'undefined') {
      const t = I18N[currentLang];
      showMapFallback(
        t.mapLibraryUnavailableTitle || t.mapFallbackTitle,
        t.mapLibraryUnavailableDesc || t.mapFallbackDesc
      );
      return;
    }

    try {
      // Clean up previous instance if retrying
      if (leafletMap) {
        try {
          leafletMap.remove();
        } catch (e) {
          console.warn('Map cleanup notice:', e);
        }
        leafletMap = null;
        Object.keys(mapMarkers).forEach((k) => delete mapMarkers[k]);
      }

      // Hide fallback initially
      hideMapFallback();

      leafletMap = L.map('corridor-map', {
        center: [31.7462, -106.4865],
        zoom: 17,
        zoomControl: true,
        scrollWheelZoom: true
      });

      // Provider management with automatic fallback
      let currentProviderIndex = 0;
      let tilesLoaded = 0;
      let tilesFailed = 0;
      let activeTileLayer = null;

      function attachTileLayer(providerIndex) {
        if (activeTileLayer && leafletMap.hasLayer(activeTileLayer)) {
          leafletMap.removeLayer(activeTileLayer);
        }
        const provider = TILE_PROVIDERS[providerIndex];
        currentProviderIndex = providerIndex;
        tilesLoaded = 0;
        tilesFailed = 0;

        activeTileLayer = L.tileLayer(provider.url, {
          subdomains: provider.subdomains || 'abc',
          maxZoom: provider.maxZoom || 19,
          attribution: provider.attribution
        });

        activeTileLayer.on('tileload', function () {
          tilesLoaded++;
          // Any successful tile means map layer is working
          hideMapFallback();
        });

        activeTileLayer.on('tileerror', function (err) {
          tilesFailed++;
          console.warn(`Tile load event on ${provider.name}:`, err);

          // An isolated tile failure must NOT declare the entire map unavailable.
          // Only switch provider or declare failure if NO tiles ever loaded and multiple consecutive tiles failed:
          if (tilesLoaded === 0 && tilesFailed >= 6) {
            if (providerIndex + 1 < TILE_PROVIDERS.length) {
              console.warn(`Provider ${provider.name} unavailable. Trying fallback: ${TILE_PROVIDERS[providerIndex + 1].name}`);
              attachTileLayer(providerIndex + 1);
            } else {
              console.error('All configured map tile providers failed to deliver tiles.');
              const t = I18N[currentLang];
              showMapFallback(
                t.mapTilesUnavailableTitle || t.mapFallbackTitle,
                t.mapTilesUnavailableDesc || t.mapFallbackDesc
              );
            }
          }
        });

        activeTileLayer.addTo(leafletMap);
      }

      attachTileLayer(0);

      // Create distinct markers for corridor entities
      Object.keys(CORRIDOR_ENTITIES).forEach((id) => {
        const ent = CORRIDOR_ENTITIES[id];
        let iconClass = 'pin-pharmacy';
        let iconEmoji = '💊';
        if (ent.entityType === 'infrastructure') {
          iconClass = 'pin-bridge';
          iconEmoji = '🌉';
        } else if (ent.entityType === 'optical') {
          iconClass = 'pin-optical';
          iconEmoji = '👓';
        }

        const customIcon = L.divIcon({
          className: 'custom-map-pin-container',
          html: `<div class="custom-map-pin ${iconClass}${id === selectedEntityId ? ' active' : ''}" id="marker-${id}"><span class="pin-content">${iconEmoji}</span></div>`,
          iconSize: [38, 38],
          iconAnchor: [19, 38],
          popupAnchor: [0, -36]
        });

        const marker = L.marker([ent.coordinates.latitude, ent.coordinates.longitude], {
          icon: customIcon,
          title: ent.name,
          keyboard: true,
          riseOnHover: true
        }).addTo(leafletMap);

        const popupWrapper = document.createElement('div');
        popupWrapper.innerHTML = `
          <div class="map-popup-title">${ent.name}</div>
          <span class="map-popup-badge ${ent.entityType}">${ent.entityType.toUpperCase()}</span>
          <div class="map-popup-desc">${ent.address}</div>
          <button type="button" class="map-popup-btn" id="popup-btn-${id}">Inspect</button>
        `;

        marker.bindPopup(popupWrapper);

        marker.on('click', () => {
          selectEntity(id);
        });

        marker.on('popupopen', () => {
          const btn = document.getElementById(`popup-btn-${id}`);
          if (btn) {
            btn.onclick = () => {
              selectEntity(id);
              marker.closePopup();
            };
          }
        });

        mapMarkers[id] = marker;
      });

      // Quick select chips click listeners (attach once)
      if (!spatialListenersAttached) {
        document.querySelectorAll('.entity-chip').forEach((btn) => {
          btn.addEventListener('click', () => {
            const entId = btn.getAttribute('data-entity-id');
            if (entId) selectEntity(entId);
          });
        });

        // "Ask About This Business" button in detail panel
        const btnAskEntity = document.getElementById('btn-entity-ask-assistant');
        if (btnAskEntity) {
          btnAskEntity.addEventListener('click', () => {
            const ent = CORRIDOR_ENTITIES[selectedEntityId];
            if (ent && ent.businessId) {
              businessSelect.value = ent.businessId;
              renderSuggestions();
              const suggestions = I18N[currentLang].suggestions[ent.businessId];
              if (suggestions && suggestions.length > 0 && !questionInput.value.trim()) {
                questionInput.value = suggestions[0];
                updateCharCount();
              }
              questionInput.focus();
              form.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }
          });
        }

        spatialListenersAttached = true;
      }

      // Ensure proper Leaflet viewport dimensions
      setTimeout(() => {
        if (leafletMap) {
          leafletMap.invalidateSize();
        }
      }, 150);
    } catch (err) {
      console.error('Failed to initialize interactive map:', err);
      const t = I18N[currentLang];
      if (fallbackTitle) fallbackTitle.textContent = t.mapFallbackTitle;
      if (fallbackDesc) fallbackDesc.textContent = t.mapFallbackDesc;
      if (fallbackContainer) fallbackContainer.hidden = false;
    }
  }

  // Render quick scenario chips
  function renderScenarioChips() {
    scenarioChipsContainer.innerHTML = '';
    const t = I18N[currentLang];
    const scenarios = t.quickScenarios || [];

    scenarios.forEach((sc) => {
      const chip = document.createElement('button');
      chip.type = 'button';
      chip.className = 'scenario-chip';
      chip.textContent = sc.label;
      chip.addEventListener('click', () => {
        setMode('specific');
        evalTimeInput.value = sc.date;
        questionInput.value = sc.question;
        updateCharCount();
        evalTimeInput.focus();
      });
      scenarioChipsContainer.appendChild(chip);
    });
  }

  // Render suggested queries for current business and language
  function renderSuggestions() {
    suggestionsContainer.innerHTML = '';
    const t = I18N[currentLang];
    const bId = businessSelect.value;
    const list = t.suggestions[bId] || [];

    list.forEach((query) => {
      const chip = document.createElement('button');
      chip.type = 'button';
      chip.className = 'suggestion-chip';
      chip.textContent = query;
      chip.addEventListener('click', () => {
        questionInput.value = query;
        updateCharCount();
        questionInput.focus();
      });
      suggestionsContainer.appendChild(chip);
    });

    if (list.length > 0 && !questionInput.value.trim()) {
      questionInput.placeholder = list[0];
    }
  }

  // Mode switcher handler
  function setMode(mode) {
    evalMode = mode;
    if (mode === 'current') {
      radioModeCurrent.checked = true;
      lblModeCurrent.classList.add('active');
      lblModeSpecific.classList.remove('active');
      evalTimeContainer.hidden = true;
      evalTimeInput.value = '';
    } else {
      radioModeSpecific.checked = true;
      lblModeSpecific.classList.add('active');
      lblModeCurrent.classList.remove('active');
      evalTimeContainer.hidden = false;
      if (!evalTimeInput.value) {
        evalTimeInput.value = '2026-10-24T17:50';
      }
    }
  }

  // Character counter
  function updateCharCount() {
    const len = questionInput.value.length;
    charCounter.textContent = `${len} / 500`;
  }

  // UI state transition helpers
  function showLoading() {
    stateIdle.hidden = true;
    stateError.hidden = true;
    stateResult.hidden = true;
    stateLoading.hidden = false;

    btnSubmit.disabled = true;
    btnSpinner.hidden = false;
    txtSubmitBtn.textContent = I18N[currentLang].btnSubmitting;
  }

  function showError(message, hint) {
    stateIdle.hidden = true;
    stateLoading.hidden = true;
    stateResult.hidden = true;
    stateError.hidden = false;

    errorMessageText.textContent = message;
    errorHintText.textContent = hint || '';

    btnSubmit.disabled = false;
    btnSpinner.hidden = true;
    txtSubmitBtn.textContent = I18N[currentLang].btnSubmit;
  }

  function showResult(data) {
    stateIdle.hidden = true;
    stateLoading.hidden = true;
    stateError.hidden = true;
    stateResult.hidden = false;

    btnSubmit.disabled = false;
    btnSpinner.hidden = true;
    txtSubmitBtn.textContent = I18N[currentLang].btnSubmit;

    const t = I18N[currentLang];

    // Assistant response
    assistantAnswerText.textContent = data.answer || '';
    lblLatency.textContent = `${data.latencyMs ?? 0}ms`;

    // Authoritative facts
    const vs = data.verifiedStatus;
    const sched = data.schedule;

    // Preserving exact canonical name as returned by World Core
    factCanonicalName.textContent = vs.businessName || sched.name;

    // Status pill
    factStatus.className = 'fact-value status-pill';
    if (vs.status === 'OPEN') {
      factStatus.classList.add('open');
      factStatus.textContent = t.statusOpen;
    } else if (vs.status === 'CLOSING_SOON') {
      factStatus.classList.add('closing-soon');
      factStatus.textContent = t.statusClosingSoon;
    } else {
      factStatus.classList.add('closed');
      factStatus.textContent = t.statusClosed;
    }

    // Admission pill
    factAdmission.className = 'fact-value admission-pill';
    if (vs.admissionPermitted) {
      factAdmission.classList.add('permitted');
      factAdmission.textContent = t.admissionPermitted;
    } else {
      factAdmission.classList.add('denied');
      factAdmission.textContent = t.admissionDenied;
    }

    factHours.textContent = `${vs.openTimeCivil} – ${vs.closeTimeCivil}`;
    factCutoff.textContent = vs.cutoffTimeCivil;

    // Evaluation mode pill
    factEvalMode.className = 'fact-value mode-pill';
    if (data.evaluationMode === 'specific') {
      factEvalMode.classList.add('specific');
      factEvalMode.textContent = t.modeSpecificPill;
    } else {
      factEvalMode.classList.add('current');
      factEvalMode.textContent = t.modeCurrentPill;
    }

    // Distinguish local evaluated civil time from UTC world time
    factCivilTime.textContent = data.evaluatedAtCivil || vs.civilTime?.isoCivilString || '-';
    factWorldTime.textContent = data.evaluatedAtUtc || vs.evaluatedAtUtc || '-';
    factTimezone.textContent = vs.timeZone || 'America/Ciudad_Juarez';
    factSummary.textContent = vs.summaryMessage || '-';

    // DST Ambiguity warning
    if (dstAmbiguityWarning) {
      dstAmbiguityWarning.hidden = !data.isAmbiguousEvaluationTime;
      if (data.isAmbiguousEvaluationTime) {
        const warningEl = document.getElementById('txt-dst-warning');
        if (warningEl) {
          if (currentLang === 'es') {
            warningEl.textContent =
              'Hora local ambigua: Esta hora ocurre dos veces por el cambio de horario de verano. Evaluada usando la primera ocurrencia (01:30 MDT, UTC-6 / 07:30 UTC).';
          } else {
            warningEl.textContent =
              data.disambiguationNotice ||
              'Ambiguous local time: This wall clock time occurs twice due to Daylight Saving Time fall-back. Evaluated using the earlier occurrence (01:30 MDT, UTC-6 / 07:30 UTC).';
          }
        }
      }
    }

    // Diagnostics
    diagModel.textContent = data.modelUsed || 'muse-spark-1.3';
    diagLatency.textContent = `${data.latencyMs ?? 0}ms`;
    diagLang.textContent = data.language ? data.language.toUpperCase() : currentLang.toUpperCase();

    if (data.usage) {
      diagPromptTokens.textContent = data.usage.promptTokens ?? 'N/A';
      diagCompletionTokens.textContent = data.usage.completionTokens ?? 'N/A';
      diagReasoningTokens.textContent =
        data.usage.reasoningTokens ??
        data.usage.completionTokensDetails?.reasoningTokens ??
        '0';
      diagTotalTokens.textContent = data.usage.totalTokens ?? 'N/A';
    } else {
      diagPromptTokens.textContent = 'N/A';
      diagCompletionTokens.textContent = 'N/A';
      diagReasoningTokens.textContent = 'N/A';
      diagTotalTokens.textContent = 'N/A';
    }
  }

  // Form submission
  async function handleSubmit(e) {
    if (e) e.preventDefault();

    const businessId = businessSelect.value.trim();
    const question = questionInput.value.trim();

    if (!question) {
      questionInput.focus();
      return;
    }

    if (question.length > 500) {
      showError(
        currentLang === 'es' ? 'La pregunta no debe exceder 500 caracteres.' : 'Question must not exceed 500 characters.'
      );
      return;
    }

    const payload = {
      businessId,
      question,
      language: currentLang
    };

    if (evalMode === 'specific') {
      const customTime = evalTimeInput.value.trim();
      if (!customTime) {
        showError(
          currentLang === 'es'
            ? 'Por favor ingresa una fecha y hora de evaluación o selecciona el reloj en vivo.'
            : 'Please enter an evaluation date and time or switch back to the live clock.'
        );
        evalTimeInput.focus();
        return;
      }
      payload.evaluationTime = customTime;
    }

    showLoading();

    try {
      const response = await fetch('/api/assistant/query', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Accept: 'application/json'
        },
        body: JSON.stringify(payload)
      });

      const data = await response.json().catch(() => null);

      if (!response.ok || !data || data.success !== true) {
        const errorMsg = data?.error?.message || data?.error || `HTTP ${response.status}`;
        const isWorldCoreError =
          data?.error?.code === 'world-core-unavailable' || response.status === 502 || response.status === 503;

        showError(errorMsg, isWorldCoreError ? I18N[currentLang].errorWorldCoreDown : '');
        return;
      }

      showResult(data);
    } catch (err) {
      showError(err instanceof Error ? err.message : String(err), I18N[currentLang].errorWorldCoreDown);
    }
  }

  // Event Listeners
  btnLangEn.addEventListener('click', () => updateLanguage('en'));
  btnLangEs.addEventListener('click', () => updateLanguage('es'));
  businessSelect.addEventListener('change', () => {
    renderSuggestions();
    const bId = businessSelect.value;
    if (CORRIDOR_ENTITIES[bId]) {
      selectEntity(bId);
    }
  });
  questionInput.addEventListener('input', () => updateCharCount());

  radioModeCurrent.addEventListener('change', () => setMode('current'));
  radioModeSpecific.addEventListener('change', () => setMode('specific'));
  lblModeCurrent.addEventListener('click', () => setMode('current'));
  lblModeSpecific.addEventListener('click', () => setMode('specific'));
  btnResetTime.addEventListener('click', () => setMode('current'));

  // Submit on Ctrl+Enter (IME safe)
  questionInput.addEventListener('keydown', (e) => {
    if (e.isComposing) return;
    if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
      e.preventDefault();
      handleSubmit();
    }
  });

  form.addEventListener('submit', handleSubmit);

  // Initialize
  updateLanguage('en');
  updateCharCount();
  setMode('current');
  initMap();
})();
