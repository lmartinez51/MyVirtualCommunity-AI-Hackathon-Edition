/**
 * MyVirtualCommunity AI Hackathon Edition
 * Bilingual Interactive Web Demo Logic
 */

(function () {
  'use strict';

  // Supported languages & translations
  const I18N = {
    en: {
      title: 'MyVirtualCommunity AI',
      subtitle: 'Authoritative Local Business Assistant Grounded in World Simulation',
      regionalInfo: 'Ciudad Juárez (America/Ciudad_Juarez, UTC-6) • Authoritative Simulation Data',
      simStatusConnected: 'World Core Connected',
      simStatusOffline: 'World Core Offline',
      lblBusiness: 'Select Business',
      hintBusiness: 'Verified customer-facing business registered in World Core.',
      lblSuggestions: 'Suggested Questions',
      lblQuestion: 'Your Question',
      hintQuestion: 'Press Ask or Ctrl+Enter to submit.',
      btnSubmit: 'Ask Assistant',
      btnSubmitting: 'Consulting...',
      idleTitle: 'Ready for Your Query',
      idleDesc: 'Choose a business and question above to get an authoritative, AI-grounded response.',
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
      lblFactTime: 'World Time (Evaluated)',
      lblFactTz: 'Regional Timezone',
      lblFactSummary: 'World Core Directive',
      guaranteeNote: 'Factual status and admission rules are derived deterministically from the World Core simulation and cannot be fabricated or altered by the language model.',
      diagnosticsSummary: 'Technical Diagnostics & Usage',
      statusOpen: 'OPEN',
      statusClosingSoon: 'CLOSING SOON',
      statusClosed: 'CLOSED',
      admissionPermitted: 'Permitted',
      admissionDenied: 'Not Permitted (Cutoff)',
      footerNote: 'MyVirtualCommunity AI Hackathon Edition • Powered by Meta Muse Spark 1.3 & Ciudad Juárez World Simulation',
      suggestions: {
        'restaurant:wing-spot': [
          'Is Wing Spot Gómez Morín open right now, and what time does it close?',
          'Can I enter to dine in right now or is it past the last-entry cutoff?',
          'What are the operating hours and days for Wing Spot Gómez Morín?'
        ],
        'restaurant:pasteleria-tia-lupe': [
          'Is Pastelerías La Tía Lupe open right now, and what time does it close?',
          'Can I purchase pastries right now or is admission closed?',
          'What are the operating days and hours for Pastelerías La Tía Lupe?'
        ]
      }
    },
    es: {
      title: 'MyVirtualCommunity AI',
      subtitle: 'Asistente de Negocios Locales Fundamentado en la Simulación del Mundo',
      regionalInfo: 'Ciudad Juárez (America/Ciudad_Juarez, UTC-6) • Datos Autoritativos de Simulación',
      simStatusConnected: 'World Core Conectado',
      simStatusOffline: 'World Core Desconectado',
      lblBusiness: 'Seleccionar Negocio',
      hintBusiness: 'Negocio verificado de atención al público registrado en el World Core.',
      lblSuggestions: 'Preguntas Sugeridas',
      lblQuestion: 'Tu Pregunta',
      hintQuestion: 'Presiona Consultar o Ctrl+Enter para enviar.',
      btnSubmit: 'Consultar Asistente',
      btnSubmitting: 'Consultando...',
      idleTitle: 'Listo para tu Consulta',
      idleDesc: 'Selecciona un negocio y formula tu pregunta para obtener una respuesta autoritativa y fundamentada.',
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
      lblFactTime: 'Tiempo del Mundo (UTC)',
      lblFactTz: 'Zona Horaria Regional',
      lblFactSummary: 'Directiva del World Core',
      guaranteeNote: 'El estado operativo y las reglas de admisión se derivan de forma determinista desde la simulación del World Core y no pueden ser alteradas ni inventadas por el modelo de lenguaje.',
      diagnosticsSummary: 'Diagnósticos Técnicos y Uso',
      statusOpen: 'ABIERTO',
      statusClosingSoon: 'POR CERRAR',
      statusClosed: 'CERRADO',
      admissionPermitted: 'Permitido',
      admissionDenied: 'No Permitido (Límite Superado)',
      footerNote: 'MyVirtualCommunity AI Hackathon Edition • Impulsado por Meta Muse Spark 1.3 y la Simulación de Ciudad Juárez',
      suggestions: {
        'restaurant:wing-spot': [
          '¿Está abierto Wing Spot Gómez Morín y a qué hora cierra?',
          '¿Todavía puedo entrar a cenar o ya pasó el límite de admisión?',
          '¿Cuáles son los días y horarios de atención de Wing Spot Gómez Morín?'
        ],
        'restaurant:pasteleria-tia-lupe': [
          '¿Está abierta Pastelerías La Tía Lupe y a qué hora cierra?',
          '¿Puedo comprar pasteles ahora mismo o ya no se permite el ingreso?',
          '¿Cuáles son los días y horarios de Pastelerías La Tía Lupe?'
        ]
      }
    }
  };

  // State
  let currentLang = 'en';

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
  const factWorldTime = document.getElementById('fact-world-time');
  const factTimezone = document.getElementById('fact-timezone');
  const factSummary = document.getElementById('fact-summary');

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
    document.getElementById('lbl-fact-time').textContent = t.lblFactTime;
    document.getElementById('lbl-fact-tz').textContent = t.lblFactTz;
    document.getElementById('lbl-fact-summary').textContent = t.lblFactSummary;
    document.getElementById('txt-guarantee-note').textContent = t.guaranteeNote;
    document.getElementById('txt-diagnostics-summary').textContent = t.diagnosticsSummary;
    document.getElementById('txt-footer-note').textContent = t.footerNote;

    renderSuggestions();
  }

  // Render question chips for current business and language
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
    factWorldTime.textContent = data.evaluatedAtUtc || vs.evaluatedAtUtc || '-';
    factTimezone.textContent = vs.timeZone || 'America/Ciudad_Juarez';
    factSummary.textContent = vs.summaryMessage || '-';

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

    showLoading();

    try {
      const response = await fetch('/api/assistant/query', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json',
        },
        body: JSON.stringify({
          businessId,
          question,
          language: currentLang,
        }),
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
})();
