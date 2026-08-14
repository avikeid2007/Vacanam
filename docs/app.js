/**
 * Vacanam Landing Page — Interactive Engine & Components
 */

// ── 99 Supported Languages Dataset ──────────────────────────────────────────
const WHISPER_LANGUAGES = [
  { code: 'en', name: 'English', native: 'English' },
  { code: 'hi', name: 'Hindi', native: 'हिन्दी' },
  { code: 'es', name: 'Spanish', native: 'Español' },
  { code: 'fr', name: 'French', native: 'Français' },
  { code: 'de', name: 'German', native: 'Deutsch' },
  { code: 'zh', name: 'Chinese', native: '中文' },
  { code: 'ja', name: 'Japanese', native: '日本語' },
  { code: 'ru', name: 'Russian', native: 'Русский' },
  { code: 'pt', name: 'Portuguese', native: 'Português' },
  { code: 'it', name: 'Italian', native: 'Italiano' },
  { code: 'ar', name: 'Arabic', native: 'العربية' },
  { code: 'ko', name: 'Korean', native: '한국어' },
  { code: 'bn', name: 'Bengali', native: 'বাংলা' },
  { code: 'ta', name: 'Tamil', native: 'தமிழ்' },
  { code: 'te', name: 'Telugu', native: 'తెలుగు' },
  { code: 'mr', name: 'Marathi', native: 'मराठी' },
  { code: 'gu', name: 'Gujarati', native: 'ગુજરાતી' },
  { code: 'kn', name: 'Kannada', native: 'ಕನ್ನಡ' },
  { code: 'ml', name: 'Malayalam', native: 'മലയാളം' },
  { code: 'pa', name: 'Punjabi', native: 'ਪੰਜਾਬੀ' },
  { code: 'ur', name: 'Urdu', native: 'اردو' },
  { code: 'nl', name: 'Dutch', native: 'Nederlands' },
  { code: 'pl', name: 'Polish', native: 'Polski' },
  { code: 'tr', name: 'Turkish', native: 'Türkçe' },
  { code: 'vi', name: 'Vietnamese', native: 'Tiếng Việt' },
  { code: 'id', name: 'Indonesian', native: 'Bahasa Indonesia' },
  { code: 'uk', name: 'Ukrainian', native: 'Українська' },
  { code: 'sv', name: 'Swedish', native: 'Svenska' },
  { code: 'el', name: 'Greek', native: 'Ελληνικά' },
  { code: 'cs', name: 'Czech', native: 'Čeština' },
  { code: 'ro', name: 'Romanian', native: 'Română' },
  { code: 'hu', name: 'Hungarian', native: 'Magyar' },
  { code: 'da', name: 'Danish', native: 'Dansk' },
  { code: 'fi', name: 'Finnish', native: 'Suomi' },
  { code: 'no', name: 'Norwegian', native: 'Norsk' },
  { code: 'sk', name: 'Slovak', native: 'Slovenčina' },
  { code: 'bg', name: 'Bulgarian', native: 'Български' },
  { code: 'hr', name: 'Croatian', native: 'Hrvatski' },
  { code: 'sr', name: 'Serbian', native: 'Српски' },
  { code: 'lt', name: 'Lithuanian', native: 'Lietuvių' },
  { code: 'sl', name: 'Slovenian', native: 'Slovenščina' },
  { code: 'lv', name: 'Latvian', native: 'Latviešu' },
  { code: 'et', name: 'Estonian', native: 'Eesti' },
  { code: 'th', name: 'Thai', native: 'ไทย' },
  { code: 'tl', name: 'Tagalog', native: 'Filipino' },
  { code: 'he', name: 'Hebrew', native: 'עברית' },
  { code: 'fa', name: 'Persian', native: 'فارسی' },
  { code: 'ms', name: 'Malay', native: 'Bahasa Melayu' },
  { code: 'af', name: 'Afrikaans', native: 'Afrikaans' },
  { code: 'sq', name: 'Albanian', native: 'Shqip' },
  { code: 'am', name: 'Amharic', native: 'አማርኛ' },
  { code: 'hy', name: 'Armenian', native: 'Հայերեն' },
  { code: 'as', name: 'Assamese', native: 'অসমীয়া' },
  { code: 'az', name: 'Azerbaijani', native: 'Azərbaycan' },
  { code: 'ba', name: 'Bashkir', native: 'Башҡортса' },
  { code: 'eu', name: 'Basque', native: 'Euskara' },
  { code: 'be', name: 'Belarusian', native: 'Беларуская' },
  { code: 'bs', name: 'Bosnian', native: 'Bosanski' },
  { code: 'br', name: 'Breton', native: 'Brezhoneg' },
  { code: 'my', name: 'Burmese', native: 'မြန်မာ' },
  { code: 'yue', name: 'Cantonese', native: '粵語' },
  { code: 'ca', name: 'Catalan', native: 'Català' },
  { code: 'ceb', name: 'Cebuano', native: 'Sinugboanon' },
  { code: 'fo', name: 'Faroese', native: 'Føroyskt' },
  { code: 'gl', name: 'Galician', native: 'Galego' },
  { code: 'ka', name: 'Georgian', native: 'ქართული' },
  { code: 'ht', name: 'Haitian Creole', native: 'Kreyòl ayisyen' },
  { code: 'ha', name: 'Hausa', native: 'Hausa' },
  { code: 'haw', name: 'Hawaiian', native: 'ʻŌlelo Hawaiʻi' },
  { code: 'is', name: 'Icelandic', native: 'Íslenska' },
  { code: 'jw', name: 'Javanese', native: 'Basa Jawa' },
  { code: 'kk', name: 'Kazakh', native: 'Қазақша' },
  { code: 'km', name: 'Khmer', native: 'ភាសាខ្មែរ' },
  { code: 'la', name: 'Latin', native: 'Latina' },
  { code: 'lb', name: 'Luxembourgish', native: 'Lëtzebuergesch' },
  { code: 'mk', name: 'Macedonian', native: 'Македонски' },
  { code: 'mg', name: 'Malagasy', native: 'Malagasy' },
  { code: 'mt', name: 'Maltese', native: 'Malti' },
  { code: 'mi', name: 'Maori', native: 'Te Reo Māori' },
  { code: 'mn', name: 'Mongolian', native: 'Монгол' },
  { code: 'ne', name: 'Nepali', native: 'नेपाली' },
  { code: 'nn', name: 'Nynorsk', native: 'Norsk nynorsk' },
  { code: 'oc', name: 'Occitan', native: 'Occitan' },
  { code: 'ps', name: 'Pashto', native: 'پښتو' },
  { code: 'sa', name: 'Sanskrit', native: 'संस्कृतम्' },
  { code: 'sn', name: 'Shona', native: 'chiShona' },
  { code: 'sd', name: 'Sindhi', native: 'سنڌي' },
  { code: 'si', name: 'Sinhala', native: 'සිංහල' },
  { code: 'so', name: 'Somali', native: 'Soomaali' },
  { code: 'su', name: 'Sundanese', native: 'Basa Sunda' },
  { code: 'sw', name: 'Swahili', native: 'Kiswahili' },
  { code: 'tg', name: 'Tajik', native: 'Тоҷикӣ' },
  { code: 'tt', name: 'Tatar', native: 'Татар' },
  { code: 'bo', name: 'Tibetan', native: 'བོད་སྐད་' },
  { code: 'tk', name: 'Turkmen', native: 'Türkmençe' },
  { code: 'uz', name: 'Uzbek', native: 'Oʻzbekcha' },
  { code: 'cy', name: 'Welsh', native: 'Cymraeg' },
  { code: 'yi', name: 'Yiddish', native: 'ייִדיש' },
  { code: 'yo', name: 'Yoruba', native: 'Èdè Yorùbá' }
];

// Sample Dictation Scenarios for Interactive Playground
const SAMPLE_SCENARIOS = [
  {
    app: 'VS Code',
    title: 'editor.ts',
    raw: 'function calculate total items count price return count star price',
    polished: 'function calculateTotal(items: number, price: number): number {\n    return items * price;\n}'
  },
  {
    app: 'Slack',
    title: '#general',
    raw: 'hey team just wanted to let you know the build is green and we are ready for release',
    polished: 'Hey team, just wanted to let you know the build is green and we are ready for release! 🚀'
  },
  {
    app: 'Outlook',
    title: 'Compose Mail',
    raw: 'hi sarah please find attached the quarterly analytics summary for our review call tomorrow morning',
    polished: 'Hi Sarah,\n\nPlease find attached the quarterly analytics summary for our review call tomorrow morning.\n\nBest regards,'
  },
  {
    app: 'Terminal',
    title: 'pwsh',
    raw: 'git checkout dash b feature slash speech recognition pipeline and push to origin',
    polished: 'git checkout -b feature/speech-recognition-pipeline && git push -u origin feature/speech-recognition-pipeline'
  }
];

// ── App Initialization ──────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
  initPlayground();
  initLanguageExplorer();
  initFaqAccordion();
  initWaveformCanvas();
  initCopyButtons();
});

// ── Interactive Playground Simulation ───────────────────────────────────────
let isRecording = false;
let currentScenarioIdx = 0;
let animationFrameId = null;

function initPlayground() {
  const pttBtn = document.getElementById('pttBtn');
  const typingOutput = document.getElementById('typingOutput');
  const targetAppTitle = document.getElementById('targetAppTitle');
  const appChips = document.querySelectorAll('.app-chip');

  if (!pttBtn || !typingOutput) return;

  // App Selector Chips
  appChips.forEach((chip, idx) => {
    chip.addEventListener('click', () => {
      appChips.forEach(c => c.classList.remove('active'));
      chip.classList.add('active');
      currentScenarioIdx = idx;
      if (targetAppTitle) {
        targetAppTitle.textContent = `${SAMPLE_SCENARIOS[idx].app} — ${SAMPLE_SCENARIOS[idx].title}`;
      }
      typingOutput.innerHTML = `<span style="color: var(--text-muted);">// Hold Ctrl+Space or the button below to dictate into ${SAMPLE_SCENARIOS[idx].app}...</span>`;
    });
  });

  // Press & Hold Handlers
  const startRecording = (e) => {
    if (e) e.preventDefault();
    if (isRecording) return;
    isRecording = true;
    pttBtn.classList.add('recording');
    pttBtn.innerHTML = `<span>🔴</span> Recording... Speak now (Release to inject)`;
    typingOutput.innerHTML = `<span style="color: #F59E0B;">🎙️ Listening to microphone stream (WASAPI 16kHz PCM)...</span>`;
  };

  const stopRecording = (e) => {
    if (e) e.preventDefault();
    if (!isRecording) return;
    isRecording = false;
    pttBtn.classList.remove('recording');
    pttBtn.innerHTML = `<span>🎙️</span> <span>Hold <kbd class="key-badge">Ctrl</kbd> + <kbd class="key-badge">Space</kbd> or <strong>Hold to Dictate</strong></span>`;

    const scenario = SAMPLE_SCENARIOS[currentScenarioIdx];
    typingOutput.innerHTML = `<span style="color: var(--text-accent);">⚡ Whisper Transcribing & AI Polishing...</span>`;

    setTimeout(() => {
      typeWriterEffect(typingOutput, scenario.polished);
    }, 350);
  };

  // Mouse & Touch events on simulator button
  pttBtn.addEventListener('mousedown', startRecording);
  window.addEventListener('mouseup', stopRecording);
  pttBtn.addEventListener('touchstart', startRecording, { passive: false });
  window.addEventListener('touchend', stopRecording);

  // Global Keyboard shortcut listener for Ctrl+Space
  let ctrlPressed = false;
  window.addEventListener('keydown', (e) => {
    if (e.key === 'Control') ctrlPressed = true;
    if (ctrlPressed && e.code === 'Space' && !isRecording) {
      e.preventDefault();
      startRecording();
    }
  });

  window.addEventListener('keyup', (e) => {
    if (e.key === 'Control') ctrlPressed = false;
    if (e.code === 'Space' && isRecording) {
      e.preventDefault();
      stopRecording();
    }
  });
}

function typeWriterEffect(element, text) {
  element.textContent = '';
  let i = 0;
  const speed = 12;

  function type() {
    if (i < text.length) {
      element.textContent += text.charAt(i);
      i++;
      setTimeout(type, speed);
    }
  }
  type();
}

// ── Audio Waveform Visualizer Simulation ────────────────────────────────────
function initWaveformCanvas() {
  const canvas = document.getElementById('waveformCanvas');
  if (!canvas) return;
  const ctx = canvas.getContext('2d');
  let phase = 0;

  function resizeCanvas() {
    canvas.width = canvas.parentElement.clientWidth;
    canvas.height = 64;
  }
  resizeCanvas();
  window.addEventListener('resize', resizeCanvas);

  function draw() {
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    const width = canvas.width;
    const height = canvas.height;
    const centerY = height / 2;

    ctx.beginPath();
    ctx.lineWidth = isRecording ? 2.5 : 1.2;
    ctx.strokeStyle = isRecording ? '#EC4899' : 'rgba(99, 102, 241, 0.4)';

    const amplitude = isRecording ? 20 : 3;
    const frequency = isRecording ? 0.05 : 0.02;

    for (let x = 0; x < width; x++) {
      const y = centerY + Math.sin(x * frequency + phase) * amplitude * Math.sin(x / width * Math.PI);
      if (x === 0) ctx.moveTo(x, y);
      else ctx.lineTo(x, y);
    }
    ctx.stroke();

    phase += isRecording ? 0.15 : 0.03;
    requestAnimationFrame(draw);
  }
  draw();
}

// ── Supported Languages Search & Explorer ───────────────────────────────────
function initLanguageExplorer() {
  const container = document.getElementById('languagesGrid');
  const searchInput = document.getElementById('langSearchInput');
  const countBadge = document.getElementById('langCountBadge');

  if (!container) return;

  function renderLanguages(list) {
    container.innerHTML = '';
    list.forEach(lang => {
      const card = document.createElement('div');
      card.className = 'lang-card';
      card.innerHTML = `
        <div>
          <strong>${lang.name}</strong>
          <span style="color: var(--text-muted); font-size: 0.8rem; display: block;">${lang.native}</span>
        </div>
        <span class="lang-code">${lang.code}</span>
      `;
      container.appendChild(card);
    });

    if (countBadge) {
      countBadge.textContent = `${list.length} Languages`;
    }
  }

  renderLanguages(WHISPER_LANGUAGES);

  if (searchInput) {
    searchInput.addEventListener('input', (e) => {
      const query = e.target.value.toLowerCase().trim();
      const filtered = WHISPER_LANGUAGES.filter(l => 
        l.name.toLowerCase().includes(query) ||
        l.code.toLowerCase().includes(query) ||
        l.native.toLowerCase().includes(query)
      );
      renderLanguages(filtered);
    });
  }
}

// ── FAQ Accordion ───────────────────────────────────────────────────────────
function initFaqAccordion() {
  const items = document.querySelectorAll('.faq-item');
  items.forEach(item => {
    const q = item.querySelector('.faq-question');
    q?.addEventListener('click', () => {
      const isActive = item.classList.contains('active');
      items.forEach(i => i.classList.remove('active'));
      if (!isActive) {
        item.classList.add('active');
      }
    });
  });
}

// ── Copy Snippet Button ─────────────────────────────────────────────────────
function initCopyButtons() {
  const copyBtns = document.querySelectorAll('.btn-copy');
  copyBtns.forEach(btn => {
    btn.addEventListener('click', () => {
      const targetId = btn.getAttribute('data-target');
      const targetEl = document.getElementById(targetId);
      if (targetEl) {
        navigator.clipboard.writeText(targetEl.textContent.trim());
        const originalText = btn.innerHTML;
        btn.innerHTML = '✓ Copied';
        setTimeout(() => {
          btn.innerHTML = originalText;
        }, 2000);
      }
    });
  });
}
