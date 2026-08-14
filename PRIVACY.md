# Privacy Policy for Vacanam 🔒

*Last Updated: August 14, 2026*

**Vacanam** ("we", "our", or "the application") is a free and open-source, local-first voice typing and AI text refinement application for Windows.

We believe that your voice, keystrokes, and thoughts are private. Vacanam is architected from the ground up on a **Zero-Trust, Zero-Telemetry, 100% Local Processing** principle.

---

## 1. Zero Telemetry & Zero Data Collection

- **No Analytics**: Vacanam contains zero telemetry, tracking SDKs, Google Analytics, telemetry pings, or usage tracking code.
- **No User Profiles**: We do not create user accounts, collect email addresses, names, or device identifiers.
- **No Cloud Transmission**: Your spoken audio, transcribed text, hotkey actions, and application metadata are **never** transmitted to any external server or cloud provider.

---

## 2. Audio Processing & Microphone Handling

- **In-Memory Streaming**: Audio captured via Windows WASAPI is buffered exclusively in volatile memory (RAM) while you hold down the Push-to-Talk hotkey (`Ctrl+Space`).
- **Immediate Discard**: As soon as speech recognition completes, the temporary in-memory audio buffer is immediately overwritten and garbage collected.
- **No Disk Storage**: Raw microphone recordings and audio WAV files are **never written to disk**.

---

## 3. Local-First AI & Speech Inference

- **Whisper Speech-to-Text**: Voice recognition is executed 100% locally on your computer's CPU or GPU using `Whisper.net` and `whisper.cpp`.
- **LLM Text Polish (AI Mode)**: When AI text polish is enabled, grammar refinement is performed locally by `LLamaSharp` executing quantized GGUF models (`Qwen2.5` / `Llama-3.2`) on your local hardware.
- **Zero Third-Party AI APIs**: Vacanam does not send text or prompts to OpenAI, Anthropic, Google, or any third-party cloud AI APIs.

---

## 4. Clipboard & Text Injection Safety

- **Temporary Clipboard Use**: When injecting text via the Clipboard strategy, Vacanam:
  1. Backs up your existing clipboard contents.
  2. Places the transcribed text on the clipboard.
  3. Sends a native `Ctrl+V` paste keystroke to your active window.
  4. Restores your original clipboard contents within ~25 milliseconds.
- **No Clipboard Logging**: Vacanam never inspects, reads, logs, or stores your prior clipboard contents.

---

## 5. Local SQLite Transcript History (Opt-In)

- **Disabled by Default**: Transcript history is **opt-in** and strictly disabled by default.
- **100% Local Storage**: If you explicitly enable history via `Settings > Privacy > Save Transcript History`, records are stored exclusively in a local SQLite database on your machine at:
  ```
  %LOCALAPPDATA%\Vacanam\history.db
  ```
- **Full User Control**: You can search your history in real-time, delete individual records, or click **Clear All History** at any time to permanently erase all stored records.
- **Auto-Pruning**: When history is enabled, older records are automatically pruned according to your configured `MaxHistoryEntries` threshold.

---

## 6. Network Access & External Connections

Vacanam only initiates outbound network requests in two explicit scenarios:
1. **Initial Model Download**: When you first launch the app or click to download a speech/LLM model in Settings, Vacanam downloads the open-source GGUF/GGML model weights directly from public Hugging Face repositories.
2. **Checking for Updates**: When you click to check for app updates, Vacanam queries GitHub Releases for version metadata.

*No personal data, audio, or transcripts are included in these requests.*

---

## 7. Open Source Transparency

Vacanam is open-source software licensed under the **MIT License**. The entire source code is publicly accessible on GitHub:
👉 [https://github.com/avikeid2007/Vacanam](https://github.com/avikeid2007/Vacanam)

You are free and encouraged to inspect, audit, or build the source code yourself to verify our privacy guarantees.

---

## 8. Contact & Questions

If you have questions about this Privacy Policy or Vacanam's security architecture, please open an issue on GitHub:
👉 [https://github.com/avikeid2007/Vacanam/issues](https://github.com/avikeid2007/Vacanam/issues)
