/**
 * MyVirtualCommunity AI Hackathon Edition
 * Bilingual Interactive Web Demo Logic (Milestone 2)
 */

(function () {
  'use strict';

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

    renderSuggestions();
    renderScenarioChips();
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
        // Default to Saturday 17:50 scenario to showcase admission cutoff
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
      language: currentLang,
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
          'Accept': 'application/json',
        },
        body: JSON.stringify(payload),
      });

      const data = await response.json().catch(() => null);

      if (!response.ok || !data || data.success !== true) {
        const errorMsg = data?.error?.message || data?.error || `HTTP ${response.status}`;
        const isWorldCoreError = data?.error?.code === 'world-core-unavailable' || response.status === 502 || response.status === 503;

        showError(
          errorMsg,
          isWorldCoreError ? I18N[currentLang].errorWorldCoreDown : ''
        );
        return;
      }

      showResult(data);
    } catch (err) {
      showError(
        err instanceof Error ? err.message : String(err),
        I18N[currentLang].errorWorldCoreDown
      );
    }
  }

  // Event Listeners
  btnLangEn.addEventListener('click', () => updateLanguage('en'));
  btnLangEs.addEventListener('click', () => updateLanguage('es'));
  businessSelect.addEventListener('change', () => renderSuggestions());
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
})();
