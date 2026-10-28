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
      simStatusConnected: 'World Core Connected (Remote)',
      simStatusFixture: 'World Core Fixture (Standalone)',
      simStatusOffline: 'World Core Offline (Retry)',
      simStatusChecking: 'Checking World Core...',
      simTitleConnected: 'Remote World Core is connected on port 3001. Click to recheck.',
      simTitleFixture: 'In-memory simulation fixture is active (standalone mode). Click to recheck.',
      simTitleOffline: 'Remote World Core server is unreachable. Click to retry.',
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
      txtBtnAskEntity: 'Customize Question',
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

      // Demonstration Flow & Judge Readiness
      txtFlow1: 'Select Landmark',
      txtFlow2: 'Inspect Verified Facts',
      txtFlow3: 'Ask in EN or ES',
      txtFlow4: 'Muse Spark Explains',

      // Entity Snapshot Card
      txtEntitySnapshotTitle: 'Authoritative World Core Snapshot',
      entitySnapshotBadge: 'Deterministic Authority',
      lblEntityStatus: 'Operating Status',
      lblEntityAdmission: 'Admission Eligibility',
      lblEntityHours: 'Official Hours',
      lblEntityCutoff: 'Last Entry Cutoff',
      lblEntityFreshness: 'Snapshot Freshness & Provenance',
      txtEntityInfraDesc: 'International Bridge Infrastructure • 24/7 Transit Corridor (Non-commercial • No retail admission cutoff)',
      consultingSnapshot: 'Consulting World Core...',
      snapshotUnavailable: 'World Core snapshot unavailable',

      // 1-Click Fast Judge Queries
      lblEntityQuickQueries: '1-Click Judge Scenarios:',
      txtQuickOpenNow: 'Check If Open Now',
      txtQuickSchedule: 'Operating Schedule',
      txtQuickAdmission: 'Test 5:50 PM Cutoff',

      // Scenario Explorer
      txtScenarioExplorerTitle: 'Scenario Explorer',
      txtScenarioExplorerBadge: 'Authoritative State & Rules',
      txtScenarioExplorerDesc: 'Evaluate how authoritative World Core state, business rules, world time, and Meta Muse Spark work together for ',
      badgeSc1: 'Authoritative Snapshot',
      txtSc1Title: '⚡ Current Business Status',
      txtSc1Eval: 'Inspect latest point-in-time snapshot from authoritative World Core simulation clock.',
      txtBtnSc1: 'Inspect Status',
      badgeSc2: 'Configured Schedule',
      txtSc2Title: '🕒 Operating Schedule',
      txtSc2Eval: 'Explain official opening/closing hours, operating days, and scheduled closures.',
      txtBtnSc2: 'Explain Schedule',
      badgeSc3: 'Authoritative ?at= Rule',
      txtSc3Title: '🚪 Admission Cutoff Rule',
      txtSc3Eval: 'Examine rule closing entry before close. Evaluated via authoritative World Core ?at= API.',
      txtBtnSc3: 'Evaluate Cutoff',
      badgeSc4: 'Meta Muse Spark',
      txtSc4Title: '✏️ Ask Custom Question',
      txtSc4Eval: 'Ask any natural-language question in English or Spanish, grounded in verified snapshot.',
      txtBtnSc4: 'Customize Question',
      txtInfraScenarioTitle: 'International Bridge Infrastructure Corridor',
      txtInfraScenarioDesc: 'Puente Internacional Paso del Norte is a 24/7 binational border crossing (non-commercial). It operates continuously with no retail schedule or commercial admission cutoff.',
      txtShortcutBenavides: 'Explore Farmacias Benavides',
      txtShortcutOptica: 'Explore Óptica Fashion',
      lblResultAuthority: 'Evaluation Basis:',
      authBasisCurrent: 'Live Simulation Clock (Authoritative World Core Snapshot)',
      authBasisSpecific: 'Authoritative Domain Rule (?at= parameter at specified civil time)',
      txtResponseGrounding: '🛡️ Natural-language explanation strictly grounded in authoritative World Core snapshot. Does not invent, alter, or mutate world state.',

      // Result card query context
      txtTargetBusinessLabel: 'Queried Business:',
      txtTrustBadge: '🛡️ Grounded in World Core Snapshot',
      lblResultQuestion: 'Submitted Question:',
      lblResultRetrievedAt: 'Snapshot Evaluated & Retrieved:',

      // Border Intelligence Translations (Increment 1)
      borderBadge: 'Border Intelligence',
      borderTitle: 'Official Pedestrian Wait Times (Juárez → El Paso)',
      borderDesc: 'Direct comparison of official U.S. Customs & Border Protection (CBP) pedestrian wait estimates for Paso del Norte (Santa Fe) and Bridge of the Americas (Puente Libre).',
      tagDirection: 'Juárez → El Paso (Northbound)',
      tagMode: '🚶 Pedestrian Only',
      tagLane: 'General / Standard Lane',
      laneCatNotice: 'General / Standard Lane (excludes Ready Lane)',
      btnBorderRefresh: 'Refresh Feeds',
      borderStatusLive: 'Official Feeds Live',
      borderStatusLoading: 'Updating Feeds...',
      borderStatusError: 'Feed Outage / Offline',
      lblBorderFetchedAt: 'Fetched at:',
      nameCrossingPdn: 'Paso del Norte (Santa Fe)',
      popCrossingPdn: 'Av. Benito Juárez / Downtown Crossing',
      nameCrossingBota: 'Bridge of the Americas (BOTA)',
      popCrossingBota: 'Puente Libre / Chamizal Crossing',
      freshnessFresh: 'Fresh',
      freshnessStale: 'Stale',
      freshnessPending: 'Pending',
      freshnessUnavailable: 'Unavailable',
      lblObsTime: 'Source Update Time:',
      lblProvider: 'Official Provider:',
      txtSourcePdn: 'View CBP Source (06240202)',
      txtSourceBota: 'View CBP Source (06240201)',
      txtComparisonTitle: 'Deterministic Wait Comparison',
      txtComparisonDisclaimer: 'Official CBP reported wait time is for Standard / General pedestrian queues only (excludes Ready Lane and vehicular traffic) and does not account for travel time between crossings, route conditions, or changing queues.',
      txtCamerasTitle: 'Official Live Bridge Cameras (Paso del Norte)',
      txtCamerasVisualNote: 'Visual Reference Only • Does not compute wait estimates',
      txtCamerasDesc: 'Inspect authentic live camera views of the pedestrian and vehicular approaches to Puente Internacional Paso del Norte, published by binational bridge authorities.',
      txtCameraChihuahua: 'Chihuahua Bridge Authority Cameras (Puentes Fronterizos)',
      txtCameraElPaso: 'City of El Paso Bridge Cameras (PDN Uno)',
      txtShortcutBorder: 'Compare Pedestrian Wait Times',
      unitMinWait: 'min wait',
      lanesOpenPrefix: 'Lanes open:',
      statusNoDelay: 'No Delay',
      statusDelay: 'Delay',
      statusLanesClosed: 'Lanes Closed',
      statusCrossingClosed: 'Crossing Closed',
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
      simStatusConnected: 'World Core Conectado (Remoto)',
      simStatusFixture: 'World Core Ficticio (Sin Conexión)',
      simStatusOffline: 'World Core Desconectado (Reintentar)',
      simStatusChecking: 'Comprobando World Core...',
      simTitleConnected: 'La simulación remota de World Core está conectada en el puerto 3001. Clic para revalidar.',
      simTitleFixture: 'Simulador ficticio en memoria activo (modo independiente). Clic para revalidar.',
      simTitleOffline: 'El servidor remoto World Core no responde. Clic para reintentar.',
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
      txtBtnAskEntity: 'Personalizar Pregunta',
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

      // Demonstration Flow & Judge Readiness
      txtFlow1: 'Seleccionar Lugar',
      txtFlow2: 'Inspeccionar Datos Verificados',
      txtFlow3: 'Preguntar en EN o ES',
      txtFlow4: 'Muse Spark Explica',

      // Entity Snapshot Card
      txtEntitySnapshotTitle: 'Snapshot Autoritativo del World Core',
      entitySnapshotBadge: 'Autoridad Determinista',
      lblEntityStatus: 'Estado Operativo',
      lblEntityAdmission: 'Elegibilidad de Admisión',
      lblEntityHours: 'Horario Oficial',
      lblEntityCutoff: 'Límite de Admisión',
      lblEntityFreshness: 'Frescura y Procedencia del Snapshot',
      txtEntityInfraDesc: 'Infraestructura de Puente Internacional • Corredor de Tránsito 24/7 (No comercial • Sin límite de admisión comercial)',
      consultingSnapshot: 'Consultando World Core...',
      snapshotUnavailable: 'Snapshot de World Core no disponible',

      // 1-Click Fast Judge Queries
      lblEntityQuickQueries: 'Escenarios Rápidos (1 Clic):',
      txtQuickOpenNow: '¿Está abierto ahora?',
      txtQuickSchedule: 'Horario de Atención',
      txtQuickAdmission: 'Probar límite 5:50 PM',

      // Scenario Explorer
      txtScenarioExplorerTitle: 'Explorador de Escenarios',
      txtScenarioExplorerBadge: 'Reglas y Estado Autoritativo',
      txtScenarioExplorerDesc: 'Evalúa cómo interactúan el estado autoritativo de World Core, reglas de negocio, tiempo del mundo y Meta Muse Spark para ',
      badgeSc1: 'Snapshot Autoritativo',
      txtSc1Title: '⚡ Estado Operativo Actual',
      txtSc1Eval: 'Consulta el snapshot en tiempo real del reloj de la simulación de World Core.',
      txtBtnSc1: 'Consultar Estado',
      badgeSc2: 'Horario Configurado',
      txtSc2Title: '🕒 Horario de Atención',
      txtSc2Eval: 'Explica los horarios oficiales de apertura y cierre, días de operación y suspensiones.',
      txtBtnSc2: 'Ver Horarios',
      badgeSc3: 'Regla Autoritativa ?at=',
      txtSc3Title: '🚪 Regla de Límite de Admisión',
      txtSc3Eval: 'Examina la regla que cierra el ingreso antes del cierre. Evaluada mediante la API autoritativa ?at= de World Core.',
      txtBtnSc3: 'Evaluar Límite',
      badgeSc4: 'Meta Muse Spark',
      txtSc4Title: '✏️ Pregunta Personalizada',
      txtSc4Eval: 'Haz cualquier pregunta en inglés o español fundamentada en el snapshot verificado.',
      txtBtnSc4: 'Personalizar Pregunta',
      txtInfraScenarioTitle: 'Corredor de Infraestructura de Puente Internacional',
      txtInfraScenarioDesc: 'El Puente Internacional Paso del Norte es un cruce fronterizo binacional 24/7 (no comercial). Opera continuamente sin horarios comerciales ni límite de admisión.',
      txtShortcutBenavides: 'Explorar Farmacias Benavides',
      txtShortcutOptica: 'Explorar Óptica Fashion',
      lblResultAuthority: 'Base de Evaluación:',
      authBasisCurrent: 'Reloj en Vivo de la Simulación (Snapshot Autoritativo de World Core)',
      authBasisSpecific: 'Regla Autoritativa de Dominio (parámetro ?at= en la hora civil especificada)',
      txtResponseGrounding: '🛡️ Explicación en lenguaje natural fundamentada estrictamente en el snapshot autoritativo de World Core. No inventa, altera ni muta el estado del mundo.',

      // Result card query context
      txtTargetBusinessLabel: 'Negocio Consultado:',
      txtTrustBadge: '🛡️ Fundamentado en Snapshot de World Core',
      lblResultQuestion: 'Pregunta Enviada:',
      lblResultRetrievedAt: 'Snapshot Evaluado y Obtenido:',

      // Border Intelligence Translations (Increment 1)
      borderBadge: 'Inteligencia Fronteriza',
      borderTitle: 'Tiempos Oficiales de Espera Peatonal (Juárez → El Paso)',
      borderDesc: 'Comparación directa de estimaciones oficiales de espera peatonal de CBP para Paso del Norte (Santa Fe) y Puente de las Américas (Puente Libre).',
      tagDirection: 'Juárez → El Paso (Hacia el Norte)',
      tagMode: '🚶 Solo Peatonal',
      tagLane: 'Carril General / Estándar',
      laneCatNotice: 'Carril General / Estándar (excluye Ready Lane)',
      btnBorderRefresh: 'Actualizar Cruces',
      borderStatusLive: 'Feeds Oficiales en Vivo',
      borderStatusLoading: 'Actualizando Cruces...',
      borderStatusError: 'Falla en Fuentes Oficiales',
      lblBorderFetchedAt: 'Consultado a las:',
      nameCrossingPdn: 'Paso del Norte (Santa Fe)',
      popCrossingPdn: 'Av. Benito Juárez / Cruce Centro',
      nameCrossingBota: 'Puente de las Américas (BOTA)',
      popCrossingBota: 'Puente Libre / Cruce Chamizal',
      freshnessFresh: 'Actualizado',
      freshnessStale: 'Desactualizado',
      freshnessPending: 'Pendiente',
      freshnessUnavailable: 'No Disponible',
      lblObsTime: 'Hora de Actualización Oficial:',
      lblProvider: 'Proveedor Oficial:',
      txtSourcePdn: 'Ver Fuente CBP (06240202)',
      txtSourceBota: 'Ver Fuente CBP (06240201)',
      txtComparisonTitle: 'Comparación Determinística de Espera',
      txtComparisonDisclaimer: 'El tiempo oficial de CBP es únicamente para filas peatonales Estándar / General (excluye Ready Lane y tráfico vehicular) y no contempla el tiempo de traslado entre puentes, condiciones viales ni cambios súbitos en la fila.',
      txtCamerasTitle: 'Cámaras Oficiales de Puentes en Vivo (Paso del Norte)',
      txtCamerasVisualNote: 'Solo Referencia Visual • No calcula tiempos de espera',
      txtCamerasDesc: 'Consulte vistas auténticas en vivo de las filas peatonales y vehiculares hacia el Puente Internacional Paso del Norte, publicadas por autoridades binacionales.',
      txtCameraChihuahua: 'Cámaras de Puentes Fronterizos de Chihuahua',
      txtCameraElPaso: 'Cámaras de la Ciudad de El Paso (PDN Uno)',
      txtShortcutBorder: 'Comparar Tiempos Peatonales',
      unitMinWait: 'min de espera',
      lanesOpenPrefix: 'Líneas abiertas:',
      statusNoDelay: 'Sin Demora',
      statusDelay: 'Con Demora',
      statusLanesClosed: 'Líneas Cerradas',
      statusCrossingClosed: 'Cruce Cerrado',
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

  // Simulation Health & Mode Synchronization
  let currentHealthState = null;
  let isHealthCheckInFlight = false;

  function renderSimulationBadge() {
    const badgeEl = document.getElementById('sim-badge');
    const textEl = document.getElementById('txt-sim-status');
    if (!badgeEl || !textEl) return;

    const t = I18N[currentLang];
    badgeEl.classList.remove('connected', 'fixture', 'offline', 'checking');

    if (isHealthCheckInFlight && !currentHealthState) {
      badgeEl.classList.add('checking');
      textEl.textContent = t.simStatusChecking;
      badgeEl.title = t.simStatusChecking;
      return;
    }

    if (!currentHealthState) {
      badgeEl.classList.add('offline');
      textEl.textContent = t.simStatusOffline;
      badgeEl.title = t.simTitleOffline;
      return;
    }

    if (currentHealthState.mode === 'in-memory') {
      badgeEl.classList.add('fixture');
      textEl.textContent = t.simStatusFixture;
      badgeEl.title = t.simTitleFixture;
    } else if (currentHealthState.reachability === 'connected') {
      badgeEl.classList.add('connected');
      textEl.textContent = t.simStatusConnected;
      badgeEl.title = t.simTitleConnected;
    } else {
      badgeEl.classList.add('offline');
      textEl.textContent = t.simStatusOffline;
      badgeEl.title = t.simTitleOffline;
    }
  }

  async function checkSimulationHealth() {
    if (isHealthCheckInFlight) return;
    isHealthCheckInFlight = true;

    const badgeEl = document.getElementById('sim-badge');
    if (badgeEl && !currentHealthState) {
      badgeEl.classList.add('checking');
    }

    try {
      const res = await fetch('/health', {
        headers: { Accept: 'application/json' },
        cache: 'no-store',
      });
      if (!res.ok) {
        throw new Error(`HTTP ${res.status}`);
      }
      const data = await res.json();
      currentHealthState = {
        mode: data.worldCore?.mode || 'in-memory',
        reachability: data.worldCore?.reachability || (data.status === 'ok' ? 'connected' : 'disconnected'),
        connected: Boolean(data.worldCore?.connected),
        metaProvider: data.metaProvider || 'local-mock',
      };
    } catch {
      currentHealthState = {
        mode: 'remote',
        reachability: 'disconnected',
        connected: false,
        metaProvider: 'local-mock',
      };
    } finally {
      isHealthCheckInFlight = false;
      renderSimulationBadge();
    }
  }

  // I18N Updater
  function updateLanguage(lang) {
    currentLang = lang;
    const t = I18N[lang];

    btnLangEn.classList.toggle('active', lang === 'en');
    btnLangEs.classList.toggle('active', lang === 'es');

    document.getElementById('txt-title').textContent = t.title;
    document.getElementById('txt-subtitle').textContent = t.subtitle;
    document.getElementById('txt-regional-info').textContent = t.regionalInfo;
    renderSimulationBadge();

    // Demonstration Flow
    const f1 = document.getElementById('txt-flow-1');
    if (f1) f1.textContent = t.txtFlow1;
    const f2 = document.getElementById('txt-flow-2');
    if (f2) f2.textContent = t.txtFlow2;
    const f3 = document.getElementById('txt-flow-3');
    if (f3) f3.textContent = t.txtFlow3;
    const f4 = document.getElementById('txt-flow-4');
    if (f4) f4.textContent = t.txtFlow4;

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

    // Entity snapshot panel strings
    const snapTitle = document.getElementById('txt-entity-snapshot-title');
    if (snapTitle) snapTitle.textContent = t.txtEntitySnapshotTitle;
    const snapBadge = document.getElementById('entity-snapshot-badge');
    if (snapBadge) snapBadge.textContent = t.entitySnapshotBadge;
    const lblEntStat = document.getElementById('lbl-entity-status');
    if (lblEntStat) lblEntStat.textContent = t.lblEntityStatus;
    const lblEntAdm = document.getElementById('lbl-entity-admission');
    if (lblEntAdm) lblEntAdm.textContent = t.lblEntityAdmission;
    const lblEntHrs = document.getElementById('lbl-entity-hours');
    if (lblEntHrs) lblEntHrs.textContent = t.lblEntityHours;
    const lblEntCut = document.getElementById('lbl-entity-cutoff');
    if (lblEntCut) lblEntCut.textContent = t.lblEntityCutoff;
    const lblEntFresh = document.getElementById('lbl-entity-freshness');
    if (lblEntFresh) lblEntFresh.textContent = t.lblEntityFreshness;
    const txtEntInfra = document.getElementById('txt-entity-infra-desc');
    if (txtEntInfra) txtEntInfra.textContent = t.txtEntityInfraDesc;

    // Quick query buttons strings
    const lblEntQq = document.getElementById('lbl-entity-quick-queries');
    if (lblEntQq) lblEntQq.textContent = t.lblEntityQuickQueries;
    const btnQo = document.getElementById('txt-quick-open-now');
    if (btnQo) btnQo.textContent = t.txtQuickOpenNow;
    const btnQs = document.getElementById('txt-quick-schedule');
    if (btnQs) btnQs.textContent = t.txtQuickSchedule;
    const btnQa = document.getElementById('txt-quick-admission');
    if (btnQa) btnQa.textContent = t.txtQuickAdmission;

    // Scenario Explorer strings
    const scTitle = document.getElementById('txt-scenario-explorer-title');
    if (scTitle) scTitle.textContent = t.txtScenarioExplorerTitle;
    const scBadge = document.getElementById('txt-scenario-explorer-badge');
    if (scBadge) scBadge.textContent = t.txtScenarioExplorerBadge;
    const bSc1 = document.getElementById('badge-sc-1');
    if (bSc1) bSc1.textContent = t.badgeSc1;
    const tSc1 = document.getElementById('txt-sc1-title');
    if (tSc1) tSc1.textContent = t.txtSc1Title;
    const eSc1 = document.getElementById('txt-sc1-eval');
    if (eSc1) eSc1.textContent = t.txtSc1Eval;
    const btnSc1 = document.getElementById('txt-btn-sc1');
    if (btnSc1) btnSc1.textContent = t.txtBtnSc1;

    const bSc2 = document.getElementById('badge-sc-2');
    if (bSc2) bSc2.textContent = t.badgeSc2;
    const tSc2 = document.getElementById('txt-sc2-title');
    if (tSc2) tSc2.textContent = t.txtSc2Title;
    const eSc2 = document.getElementById('txt-sc2-eval');
    if (eSc2) eSc2.textContent = t.txtSc2Eval;
    const btnSc2 = document.getElementById('txt-btn-sc2');
    if (btnSc2) btnSc2.textContent = t.txtBtnSc2;

    const bSc3 = document.getElementById('badge-sc-3');
    if (bSc3) bSc3.textContent = t.badgeSc3;
    const tSc3 = document.getElementById('txt-sc3-title');
    if (tSc3) tSc3.textContent = t.txtSc3Title;
    const eSc3 = document.getElementById('txt-sc3-eval');
    if (eSc3) eSc3.textContent = t.txtSc3Eval;
    const btnSc3 = document.getElementById('txt-btn-sc3');
    if (btnSc3) btnSc3.textContent = t.txtBtnSc3;

    const bSc4 = document.getElementById('badge-sc-4');
    if (bSc4) bSc4.textContent = t.badgeSc4;
    const tSc4 = document.getElementById('txt-sc4-title');
    if (tSc4) tSc4.textContent = t.txtSc4Title;
    const eSc4 = document.getElementById('txt-sc4-eval');
    if (eSc4) eSc4.textContent = t.txtSc4Eval;
    const btnSc4 = document.getElementById('txt-btn-sc4');
    if (btnSc4) btnSc4.textContent = t.txtBtnSc4;

    const tInfraSc = document.getElementById('txt-infra-scenario-title');
    if (tInfraSc) tInfraSc.textContent = t.txtInfraScenarioTitle;
    const dInfraSc = document.getElementById('txt-infra-scenario-desc');
    if (dInfraSc) dInfraSc.textContent = t.txtInfraScenarioDesc;
    const sBen = document.getElementById('txt-shortcut-benavides');
    if (sBen) sBen.textContent = t.txtShortcutBenavides;
    const sOpt = document.getElementById('txt-shortcut-optica');
    if (sOpt) sOpt.textContent = t.txtShortcutOptica;

    const lblResAuth = document.getElementById('lbl-result-authority');
    if (lblResAuth) lblResAuth.textContent = t.lblResultAuthority;
    const txtRespGrd = document.getElementById('txt-response-grounding');
    if (txtRespGrd) txtRespGrd.textContent = t.txtResponseGrounding;

    // Result card query context strings
    const txtTargLbl = document.getElementById('txt-target-business-label');
    if (txtTargLbl) txtTargLbl.textContent = t.txtTargetBusinessLabel;
    const txtTrBadge = document.getElementById('txt-trust-badge');
    if (txtTrBadge) txtTrBadge.textContent = t.txtTrustBadge;
    const lblResQ = document.getElementById('lbl-result-question');
    if (lblResQ) lblResQ.textContent = t.lblResultQuestion;
    const lblResRet = document.getElementById('lbl-result-retrieved-at');
    if (lblResRet) lblResRet.textContent = t.lblResultRetrievedAt;

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

    // Border Intelligence strings
    const bBadge = document.getElementById('txt-border-badge');
    if (bBadge) bBadge.textContent = t.borderBadge;
    const bTitle = document.getElementById('txt-border-title');
    if (bTitle) bTitle.textContent = t.borderTitle;
    const bDesc = document.getElementById('txt-border-desc');
    if (bDesc) bDesc.textContent = t.borderDesc;
    const tagDir = document.getElementById('tag-direction');
    if (tagDir) tagDir.textContent = t.tagDirection;
    const tagMd = document.getElementById('tag-mode');
    if (tagMd) tagMd.textContent = t.tagMode;
    const tagLane = document.getElementById('tag-lane');
    if (tagLane) tagLane.textContent = t.tagLane;

    const lanePdn = document.getElementById('lane-cat-pdn');
    if (lanePdn) lanePdn.textContent = t.laneCatNotice;
    const laneBota = document.getElementById('lane-cat-bota');
    if (laneBota) laneBota.textContent = t.laneCatNotice;

    const btnBRef = document.getElementById('txt-btn-border-refresh');
    if (btnBRef) btnBRef.textContent = t.btnBorderRefresh;
    const lblBFA = document.getElementById('lbl-border-fetched-at');
    if (lblBFA) lblBFA.textContent = t.lblBorderFetchedAt;

    const nPdn = document.getElementById('name-crossing-pdn');
    if (nPdn) nPdn.textContent = t.nameCrossingPdn;
    const pPdn = document.getElementById('pop-crossing-pdn');
    if (pPdn) pPdn.textContent = t.popCrossingPdn;
    const nBota = document.getElementById('name-crossing-bota');
    if (nBota) nBota.textContent = t.nameCrossingBota;
    const pBota = document.getElementById('pop-crossing-bota');
    if (pBota) pBota.textContent = t.popCrossingBota;

    const lPdn = document.getElementById('lbl-obs-time-pdn');
    if (lPdn) lPdn.textContent = t.lblObsTime;
    const lBota = document.getElementById('lbl-obs-time-bota');
    if (lBota) lBota.textContent = t.lblObsTime;
    const prvPdn = document.getElementById('lbl-provider-pdn');
    if (prvPdn) prvPdn.textContent = t.lblProvider;
    const prvBota = document.getElementById('lbl-provider-bota');
    if (prvBota) prvBota.textContent = t.lblProvider;

    const sPdn = document.getElementById('txt-source-pdn');
    if (sPdn) sPdn.textContent = t.txtSourcePdn;
    const sBota = document.getElementById('txt-source-bota');
    if (sBota) sBota.textContent = t.txtSourceBota;

    const compTitle = document.getElementById('txt-comparison-title');
    if (compTitle) compTitle.textContent = t.txtComparisonTitle;
    const compDisc = document.getElementById('txt-comparison-disclaimer');
    if (compDisc) compDisc.textContent = t.txtComparisonDisclaimer;

    const camTitle = document.getElementById('txt-cameras-title');
    if (camTitle) camTitle.textContent = t.txtCamerasTitle;
    const camNote = document.getElementById('txt-cameras-visual-note');
    if (camNote) camNote.textContent = t.txtCamerasVisualNote;
    const camDesc = document.getElementById('txt-cameras-desc');
    if (camDesc) camDesc.textContent = t.txtCamerasDesc;
    const camChih = document.getElementById('txt-camera-chihuahua');
    if (camChih) camChih.textContent = t.txtCameraChihuahua;
    const camEP = document.getElementById('txt-camera-elpaso');
    if (camEP) camEP.textContent = t.txtCameraElPaso;

    const scBrd = document.getElementById('txt-shortcut-border');
    if (scBrd) scBrd.textContent = t.txtShortcutBorder;

    renderBorderIntelligence();
    renderEntityDetail(selectedEntityId);
    renderSuggestions();
    renderScenarioChips();
  }

  // Fetch live authoritative operational status for selected commercial entity
  const statusCache = {};
  async function fetchEntityLiveStatus(businessId) {
    const statusPill = document.getElementById('entity-live-status');
    const admissionPill = document.getElementById('entity-live-admission');
    const hoursEl = document.getElementById('entity-live-hours');
    const cutoffEl = document.getElementById('entity-live-cutoff');
    const freshnessEl = document.getElementById('entity-live-freshness');
    const t = I18N[currentLang];

    if (!statusPill) return;

    statusPill.className = 'snapshot-value status-pill';
    statusPill.textContent = t.consultingSnapshot || 'Consulting World Core...';
    if (admissionPill) {
      admissionPill.className = 'snapshot-value admission-pill';
      admissionPill.textContent = '-';
    }

    try {
      const res = await fetch(`/api/businesses/${encodeURIComponent(businessId)}/status`);
      if (!res.ok) {
        throw new Error(`HTTP ${res.status}`);
      }
      const data = await res.json();
      if (!data || !data.success || !data.status) {
        throw new Error('Invalid status data');
      }

      const st = data.status;
      statusCache[businessId] = st;

      // Render status pill
      statusPill.className = 'snapshot-value status-pill';
      if (st.status === 'OPEN') {
        statusPill.classList.add('open');
        statusPill.textContent = t.statusOpen;
      } else if (st.status === 'CLOSING_SOON') {
        statusPill.classList.add('closing-soon');
        statusPill.textContent = t.statusClosingSoon;
      } else {
        statusPill.classList.add('closed');
        statusPill.textContent = t.statusClosed;
      }

      // Render admission pill
      if (admissionPill) {
        admissionPill.className = 'snapshot-value admission-pill';
        if (st.admissionPermitted) {
          admissionPill.classList.add('permitted');
          admissionPill.textContent = t.admissionPermitted;
        } else {
          admissionPill.classList.add('denied');
          admissionPill.textContent = t.admissionDenied;
        }
      }

      if (hoursEl) {
        hoursEl.textContent = `${st.openTimeCivil} – ${st.closeTimeCivil}`;
      }
      if (cutoffEl) {
        cutoffEl.textContent = st.cutoffTimeCivil || '-';
      }
      if (freshnessEl) {
        const civilIso = st.civilTime?.isoCivilString || '';
        freshnessEl.textContent = `${st.evaluatedAtUtc} (${civilIso} MST/MDT) • ${st.timeZone}`;
      }
      if (currentHealthState && currentHealthState.mode === 'remote' && currentHealthState.reachability !== 'connected') {
        currentHealthState.reachability = 'connected';
        currentHealthState.connected = true;
        renderSimulationBadge();
      }
    } catch (err) {
      console.warn(`Could not retrieve live status for ${businessId}:`, err);
      statusPill.className = 'snapshot-value status-pill closed';
      statusPill.textContent = t.snapshotUnavailable || 'World Core Unavailable';
      if (freshnessEl) {
        freshnessEl.textContent = 'World Core simulation server offline or unreachable';
      }
      if (currentHealthState && currentHealthState.mode === 'remote' && currentHealthState.reachability !== 'disconnected') {
        currentHealthState.reachability = 'disconnected';
        currentHealthState.connected = false;
        renderSimulationBadge();
      }
    }
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

    const snapshotCard = document.getElementById('entity-snapshot-card');
    const snapshotGrid = document.getElementById('entity-snapshot-grid');
    const infraNote = document.getElementById('entity-infrastructure-note');
    const quickQueries = document.getElementById('entity-quick-queries');

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

    const scSelectedName = document.getElementById('scenario-selected-business-name');
    const scCardsGrid = document.getElementById('scenario-cards-grid');
    const scInfraCard = document.getElementById('scenario-infra-card');

    if (scSelectedName) scSelectedName.textContent = entity.name;

    if (entity.businessId) {
      if (actionsEl) actionsEl.style.display = 'block';
      if (snapshotGrid) snapshotGrid.hidden = false;
      if (infraNote) infraNote.hidden = true;
      if (quickQueries) quickQueries.hidden = false;
      if (scCardsGrid) scCardsGrid.hidden = false;
      if (scInfraCard) scInfraCard.hidden = true;
      fetchEntityLiveStatus(entity.businessId);
    } else {
      // Infrastructure is non-commercial
      if (actionsEl) actionsEl.style.display = 'none';
      if (snapshotGrid) snapshotGrid.hidden = true;
      if (infraNote) infraNote.hidden = false;
      if (quickQueries) quickQueries.hidden = true;
      if (scCardsGrid) scCardsGrid.hidden = true;
      if (scInfraCard) scInfraCard.hidden = false;
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

        // "Ask About This Business" / "Customize Question" button in detail panel
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

        // Scenario Explorer Action Runner
        function runScenario(action) {
          const ent = CORRIDOR_ENTITIES[selectedEntityId];
          if (!ent || !ent.businessId) return;

          businessSelect.value = ent.businessId;
          renderSuggestions();

          const name = ent.name;
          if (action === 'status' || action === 'open-now') {
            setMode('current');
            questionInput.value =
              currentLang === 'es'
                ? `¿Está abierto ${name} en este momento y cuál es su estado operativo actual?`
                : `Is ${name} open right now, and what is its current operating status?`;
            updateCharCount();
            handleSubmit();
          } else if (action === 'schedule') {
            setMode('current');
            questionInput.value =
              currentLang === 'es'
                ? `¿Cuáles son los días de operación, horarios de apertura y horas de cierre oficiales de ${name}?`
                : `What are the official operating days, opening hours, and closing times for ${name}?`;
            updateCharCount();
            handleSubmit();
          } else if (action === 'cutoff' || action === 'admission') {
            setMode('specific');
            if (ent.businessId === 'restaurant:wing-spot') {
              evalTimeInput.value = '2026-10-24T17:50';
              questionInput.value =
                currentLang === 'es'
                  ? `¿Todavía me dejarán entrar a Wing Spot Gómez Morín si llego a las 5:50 p. m. o el límite de 15 minutos ya cerró las admisiones?`
                  : `Will I still be admitted to Wing Spot Gómez Morín if I arrive at 5:50 PM, or has the 15-minute last entry cutoff closed new admissions?`;
            } else if (ent.businessId === 'restaurant:pasteleria-tia-lupe') {
              evalTimeInput.value = '2026-10-24T17:50';
              questionInput.value =
                currentLang === 'es'
                  ? `¿Todavía me dejarán entrar a Pastelerías La Tía Lupe si llego a las 5:50 p. m.?`
                  : `Will I still be admitted to Pastelerías La Tía Lupe if I arrive at 5:50 PM?`;
            } else if (ent.businessId === 'pharmacy:farmacias-benavides') {
              evalTimeInput.value = '2026-10-24T21:50';
              questionInput.value =
                currentLang === 'es'
                  ? `¿Farmacias Benavides permite la entrada de clientes a las 9:50 p. m. antes de cerrar a las 10:00 p. m. o aplica un límite de última entrada?`
                  : `Does Farmacias Benavides allow customer entry at 9:50 PM before closing at 10:00 PM, or does it enforce a last entry cutoff?`;
            } else if (ent.businessId === 'optical:optica-fashion') {
              evalTimeInput.value = '2026-10-24T16:50';
              questionInput.value =
                currentLang === 'es'
                  ? `¿Óptica Fashion admite pacientes para consultas a las 4:50 p. m. antes de cerrar a las 5:00 p. m.?`
                  : `Does Óptica Fashion admit patients for optical consultations at 4:50 PM before closing at 5:00 PM?`;
            } else {
              evalTimeInput.value = '2026-10-24T17:50';
              questionInput.value =
                currentLang === 'es'
                  ? `¿Todavía me dejarán entrar si llego a las 5:50 p. m.?`
                  : `Would I still be admitted if I arrive at 5:50 PM?`;
            }
            updateCharCount();
            handleSubmit();
          } else if (action === 'custom') {
            const formEl = document.getElementById('assistant-form');
            if (questionInput) {
              if (!questionInput.value.trim()) {
                questionInput.value =
                  currentLang === 'es'
                    ? `¿Cuáles son las políticas y disponibilidad de ${name}?`
                    : `What are the operating policies and availability for ${name}?`;
                updateCharCount();
              }
              questionInput.focus();
              if (formEl) {
                formEl.scrollIntoView({ behavior: 'smooth', block: 'start' });
              }
            }
          }
        }

        // Bind Scenario Explorer buttons
        const btnScStat = document.getElementById('btn-scenario-status');
        if (btnScStat) btnScStat.addEventListener('click', () => runScenario('status'));
        const btnScSched = document.getElementById('btn-scenario-schedule');
        if (btnScSched) btnScSched.addEventListener('click', () => runScenario('schedule'));
        const btnScCut = document.getElementById('btn-scenario-cutoff');
        if (btnScCut) btnScCut.addEventListener('click', () => runScenario('cutoff'));
        const btnScCust = document.getElementById('btn-scenario-custom');
        if (btnScCust) btnScCust.addEventListener('click', () => runScenario('custom'));

        // Bind backwards-compatible alias buttons
        const btnQOpen = document.getElementById('btn-quick-open-now');
        if (btnQOpen) btnQOpen.addEventListener('click', () => runScenario('status'));
        const btnQSched = document.getElementById('btn-quick-schedule');
        if (btnQSched) btnQSched.addEventListener('click', () => runScenario('schedule'));
        const btnQAdm = document.getElementById('btn-quick-admission');
        if (btnQAdm) btnQAdm.addEventListener('click', () => runScenario('cutoff'));

        // Bind Infrastructure Shortcut Buttons
        const btnShortBen = document.getElementById('btn-shortcut-benavides');
        if (btnShortBen) btnShortBen.addEventListener('click', () => selectEntity('pharmacy:farmacias-benavides'));
        const btnShortOpt = document.getElementById('btn-shortcut-optica');
        if (btnShortOpt) btnShortOpt.addEventListener('click', () => selectEntity('optical:optica-fashion'));

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

  // Helper to prevent duplicate in-flight submissions and manage interactive controls
  function setSubmittingState(isSubmitting) {
    btnSubmit.disabled = isSubmitting;
    btnSpinner.hidden = !isSubmitting;
    txtSubmitBtn.textContent = isSubmitting ? I18N[currentLang].btnSubmitting : I18N[currentLang].btnSubmit;

    businessSelect.disabled = isSubmitting;
    questionInput.disabled = isSubmitting;
    if (evalTimeInput) evalTimeInput.disabled = isSubmitting;
    if (radioModeCurrent) radioModeCurrent.disabled = isSubmitting;
    if (radioModeSpecific) radioModeSpecific.disabled = isSubmitting;

    document
      .querySelectorAll(
        '.suggestion-chip, .scenario-chip, .btn-quick-query, .action-btn-query, .entity-chip, .btn-scenario-action, .btn-infra-shortcut'
      )
      .forEach((el) => {
        el.disabled = isSubmitting;
        if (isSubmitting) {
          el.style.pointerEvents = 'none';
          el.style.opacity = '0.6';
        } else {
          el.style.pointerEvents = '';
          el.style.opacity = '';
        }
      });
  }

  // UI state transition helpers
  function showLoading() {
    stateIdle.hidden = true;
    stateError.hidden = true;
    stateResult.hidden = true;
    stateLoading.hidden = false;

    setSubmittingState(true);
  }

  function showError(message, hint) {
    stateIdle.hidden = true;
    stateLoading.hidden = true;
    stateResult.hidden = true;
    stateError.hidden = false;

    errorMessageText.textContent = message;
    errorHintText.textContent = hint || '';

    setSubmittingState(false);
  }

  function showResult(data) {
    stateIdle.hidden = true;
    stateLoading.hidden = true;
    stateError.hidden = true;
    stateResult.hidden = false;

    setSubmittingState(false);

    const t = I18N[currentLang];

    // Populate Query Context & Provenance Card
    const resBusName = document.getElementById('result-business-name');
    const resQuestionText = document.getElementById('result-question-text');
    const resRetrievedAt = document.getElementById('result-retrieved-at');
    const resAuthBasis = document.getElementById('result-evaluation-basis');
    const resGrounding = document.getElementById('txt-response-grounding');

    if (resBusName) {
      resBusName.textContent =
        data.verifiedStatus?.businessName ||
        data.schedule?.name ||
        businessSelect.options[businessSelect.selectedIndex]?.text ||
        '';
    }
    if (resQuestionText) {
      resQuestionText.textContent = data.question || questionInput.value.trim();
    }
    if (resRetrievedAt) {
      const civilStr = data.evaluatedAtCivil || data.verifiedStatus?.civilTime?.isoCivilString || '';
      resRetrievedAt.textContent = `${data.evaluatedAtUtc || data.verifiedStatus?.evaluatedAtUtc || '-'} (${civilStr} MST/MDT)`;
    }
    if (resAuthBasis) {
      if (data.evaluationMode === 'specific') {
        resAuthBasis.textContent = t.authBasisSpecific;
      } else {
        resAuthBasis.textContent = t.authBasisCurrent;
      }
    }
    if (resGrounding) {
      resGrounding.textContent = t.txtResponseGrounding;
    }

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

    // Smooth scroll to output result card for instant judge visibility
    const outSec = document.getElementById('output-section');
    if (outSec) {
      outSec.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
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

        if (isWorldCoreError && currentHealthState && currentHealthState.mode === 'remote') {
          currentHealthState.reachability = 'disconnected';
          currentHealthState.connected = false;
          renderSimulationBadge();
        }

        showError(errorMsg, isWorldCoreError ? I18N[currentLang].errorWorldCoreDown : '');
        return;
      }

      if (currentHealthState && currentHealthState.mode === 'remote' && currentHealthState.reachability !== 'connected') {
        currentHealthState.reachability = 'connected';
        currentHealthState.connected = true;
        renderSimulationBadge();
      }

      showResult(data);
    } catch (err) {
      if (currentHealthState && currentHealthState.mode === 'remote') {
        currentHealthState.reachability = 'disconnected';
        currentHealthState.connected = false;
        renderSimulationBadge();
      }
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

  // Bind Simulation Badge for manual health recheck
  const simBadgeEl = document.getElementById('sim-badge');
  if (simBadgeEl) {
    simBadgeEl.addEventListener('click', () => {
      checkSimulationHealth();
    });
    simBadgeEl.addEventListener('keydown', (e) => {
      if (e.key === 'Enter' || e.key === ' ') {
        e.preventDefault();
        checkSimulationHealth();
      }
    });
  }

  // ============================================================================
  // Border Intelligence Client State & Rendering (Increment 1)
  // ============================================================================
  let currentBorderComparison = null;
  let isBorderRefreshing = false;

  async function fetchBorderWaitTimes(bypassCache = false) {
    if (isBorderRefreshing) return;
    isBorderRefreshing = true;

    const btnRefresh = document.getElementById('btn-border-refresh');
    const statusDot = document.getElementById('border-status-dot');
    const statusLabel = document.getElementById('txt-border-status-label');
    const t = I18N[currentLang];

    if (btnRefresh) {
      btnRefresh.classList.add('refreshing');
      btnRefresh.disabled = true;
    }
    if (statusDot) {
      statusDot.className = 'status-dot dot-stale';
    }
    if (statusLabel) {
      statusLabel.textContent = t.borderStatusLoading;
    }

    try {
      const url = '/api/border/wait-times' + (bypassCache ? '?refresh=true' : '');
      const res = await fetch(url, { cache: 'no-store' });
      if (!res.ok) {
        throw new Error(`Border API returned status ${res.status}`);
      }
      const data = await res.json();
      if (data && data.success && data.comparison) {
        currentBorderComparison = data.comparison;
        renderBorderIntelligence();
        if (statusDot) statusDot.className = 'status-dot dot-live';
        if (statusLabel) statusLabel.textContent = t.borderStatusLive;
      } else {
        throw new Error('Malformed border data received');
      }
    } catch (err) {
      console.warn('Border wait time refresh failed:', err);
      if (statusDot) statusDot.className = 'status-dot dot-error';
      if (statusLabel) statusLabel.textContent = t.borderStatusError;
    } finally {
      isBorderRefreshing = false;
      if (btnRefresh) {
        btnRefresh.classList.remove('refreshing');
        btnRefresh.disabled = false;
      }
    }
  }

  function renderBorderIntelligence() {
    if (!currentBorderComparison) return;
    const t = I18N[currentLang];
    const { observations, comparison, fetchedAt } = currentBorderComparison;

    // Render Fetch Time
    const valFetchedAt = document.getElementById('val-border-fetched-at');
    if (valFetchedAt && fetchedAt) {
      const d = new Date(fetchedAt);
      valFetchedAt.textContent = isNaN(d.getTime())
        ? fetchedAt
        : d.toLocaleTimeString(currentLang === 'es' ? 'es-MX' : 'en-US', {
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit',
            timeZoneName: 'short',
          });
    }

    // Helper for rendering a single crossing card
    const renderCard = (prefix, obs) => {
      const waitValEl = document.getElementById(`wait-val-${prefix}`);
      const waitUnitEl = document.getElementById(`wait-unit-${prefix}`);
      const freshnessEl = document.getElementById(`freshness-${prefix}`);
      const opStatusEl = document.getElementById(`op-status-${prefix}`);
      const lanesEl = document.getElementById(`lanes-${prefix}`);
      const obsTimeEl = document.getElementById(`val-obs-time-${prefix}`);

      if (obs) {
        // Lane category notice
        const laneCatEl = document.getElementById(`lane-cat-${prefix}`);
        if (laneCatEl) {
          laneCatEl.textContent = t.laneCatNotice;
        }

        // Operational status and wait number
        if (obs.portStatus === 'Closed') {
          if (waitValEl) waitValEl.textContent = '—';
          if (waitUnitEl) waitUnitEl.textContent = t.statusCrossingClosed;
          if (opStatusEl) {
            opStatusEl.textContent = t.statusLanesClosed;
            opStatusEl.className = 'operational-badge closed';
          }
        } else if (typeof obs.delayMinutes === 'number' && !isNaN(obs.delayMinutes)) {
          if (waitValEl) waitValEl.textContent = obs.delayMinutes;
          if (waitUnitEl) waitUnitEl.textContent = t.unitMinWait;
          if (opStatusEl) {
            const rawStatus = (obs.operationalStatus || '').toLowerCase();
            if (rawStatus.includes('no delay')) {
              opStatusEl.textContent = t.statusNoDelay;
              opStatusEl.className = 'operational-badge';
            } else if (rawStatus.includes('delay')) {
              opStatusEl.textContent = t.statusDelay;
              opStatusEl.className = 'operational-badge delay';
            } else {
              opStatusEl.textContent = obs.operationalStatus || '—';
              opStatusEl.className = 'operational-badge';
            }
          }
        } else {
          if (waitValEl) waitValEl.textContent = '—';
          if (waitUnitEl) {
            waitUnitEl.textContent =
              obs.freshness === 'pending'
                ? t.freshnessPending
                : t.freshnessUnavailable;
          }
          if (opStatusEl) {
            opStatusEl.textContent =
              obs.freshness === 'pending'
                ? t.freshnessPending
                : t.freshnessUnavailable;
            opStatusEl.className = 'operational-badge';
          }
        }

        // Freshness pill
        if (freshnessEl) {
          freshnessEl.className = `freshness-pill ${obs.freshness}`;
          if (obs.freshness === 'fresh') freshnessEl.textContent = t.freshnessFresh;
          else if (obs.freshness === 'stale') freshnessEl.textContent = t.freshnessStale;
          else if (obs.freshness === 'pending') freshnessEl.textContent = t.freshnessPending;
          else freshnessEl.textContent = t.freshnessUnavailable;
        }

        // Lanes open
        if (lanesEl) {
          const lanesStr = obs.lanesOpen !== null && obs.lanesOpen !== undefined ? obs.lanesOpen : '—';
          lanesEl.textContent = `${t.lanesOpenPrefix} ${lanesStr}`;
        }

        // Source update time
        if (obsTimeEl) {
          obsTimeEl.textContent = obs.sourceObservationTime || '—';
        }
      }
    };

    if (observations) {
      renderCard('pdn', observations.pasoDelNorte);
      renderCard('bota', observations.bridgeOfTheAmericas);
    }

    // Comparison Statement
    const statementEl = document.getElementById('txt-comparison-statement');
    const compIconEl = document.getElementById('comparison-icon');
    if (statementEl && comparison) {
      if (comparison.canCompare && comparison.statement) {
        statementEl.textContent = comparison.statement[currentLang] || comparison.statement.en;
        if (compIconEl) compIconEl.textContent = '⚖️';
      } else {
        const reason = comparison.reasonUnavailable
          ? ` (${comparison.reasonUnavailable[currentLang] || comparison.reasonUnavailable.en})`
          : '';
        statementEl.textContent =
          (comparison.statement ? comparison.statement[currentLang] || comparison.statement.en : '') + reason;
        if (compIconEl) compIconEl.textContent = '⚠️';
      }
    }
  }

  // Bind Border Intelligence controls
  const btnBorderRefresh = document.getElementById('btn-border-refresh');
  if (btnBorderRefresh) {
    btnBorderRefresh.addEventListener('click', () => fetchBorderWaitTimes(true));
  }

  const btnShortcutBorder = document.getElementById('btn-shortcut-border');
  if (btnShortcutBorder) {
    btnShortcutBorder.addEventListener('click', () => {
      const borderSec = document.getElementById('border-section');
      if (borderSec) {
        borderSec.scrollIntoView({ behavior: 'smooth' });
      }
    });
  }

  // Initialize
  updateLanguage('en');
  updateCharCount();
  setMode('current');
  initMap();
  checkSimulationHealth();
  fetchBorderWaitTimes();
})();
