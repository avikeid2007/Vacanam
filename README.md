# Vacanam

**Local voice typing for Windows** — a production-quality background application similar to Wispr Flow.  
All AI inference runs 100% locally. No cloud APIs. No data leaves your device.

---

## What is Vacanam?

Vacanam runs silently in your system tray. Press and hold **Ctrl+Space** from any application, speak, and your words are automatically transcribed and typed into whatever you were working in.

### Key Features
- 🎙️ **Global push-to-talk** — works in every Windows application
- 🤖 **Local Whisper STT** — offline speech recognition via whisper.cpp
- ✨ **Optional AI mode** — local LLM grammar correction (Phi-3.5, Llama 3.2, Gemma 2)
- 🔒 **100% private** — no internet connection required, no telemetry
- ⚡ **Fast** — hotkey response <100ms, short transcription <1s
- 🖥️ **Tray app** — runs in the background, near-zero resource use when idle

---

## Development Phases

| Phase | Status | Description |
|-------|--------|-------------|
| 1 — App Shell | ✅ **Complete** | Tray app, DI, logging, settings window |
| 2 — Global Hotkeys | 🔲 Planned | Win32 `RegisterHotKey`, push-to-talk |
| 3 — Audio Capture | 🔲 Planned | NAudio WASAPI, 16 kHz mono PCM |
| 4 — Whisper STT | 🔲 Planned | Whisper.net, CPU + CUDA inference |
| 5 — Text Injection | 🔲 Planned | Clipboard, SendInput, UI Automation |
| 6 — Local LLM | 🔲 Planned | LLamaSharp, grammar correction |
| 7 — Voice Commands | 🔲 Planned | Whitelist-based command processing |
| 8 — App Awareness | 🔲 Planned | Context-aware prompts per application |
| 9 — Selected Text | 🔲 Planned | Rewrite, summarise, translate selection |
| 10 — Optimisation | 🔲 Planned | Performance, memory, polish |

---

## Prerequisites

- **Windows 10/11** (x64)
- **.NET 10 SDK** — [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Visual Studio 2022 v17.14+** or **VS Code** with C# Dev Kit

### Optional (for GPU acceleration)
- NVIDIA GPU with **CUDA 12.x** drivers
- 4 GB+ VRAM for Whisper medium/large models
- 2 GB+ VRAM for LLM models

---

## Building from Source

```powershell
# Clone the repository
git clone https://github.com/your-org/vacanam.git
cd vacanam

# Restore dependencies
dotnet restore Vacanam.slnx

# Build
dotnet build Vacanam.slnx --configuration Release

# Run
dotnet run --project src/Vacanam.App --configuration Release
```

---

## Running Vacanam

After building, run the application. It will appear **only in the system tray** (bottom-right taskbar area).

1. **Right-click** the tray icon to see the menu
2. **Settings** → configure hotkeys, models, audio, privacy
3. **Ctrl+Space** (hold) → speak → release → text appears in your active app

---

## Model Setup (Phase 4+)

Whisper and LLM models are downloaded separately and stored at:

```
%LOCALAPPDATA%\Vacanam\Models\
    Whisper\
        ggml-small.bin          ← Default (466 MB) — download in Settings → Speech
        ggml-tiny.bin           ← Fast option (75 MB)
        ggml-medium.bin         ← High accuracy (1.5 GB)
        ggml-large-v3.bin       ← Best accuracy (3.1 GB)
    LLM\
        phi-3.5-mini-instruct-Q4_K_M.gguf   ← Recommended (2.2 GB)
        llama-3.2-3b-Q4_K_M.gguf
        gemma-2-2b-it-Q4_K_M.gguf
```

Models are never committed to Git. Use **Settings → Speech** and **Settings → AI** to download them.

---

## Solution Structure

```
Vacanam.slnx
src/
    Vacanam.App/           WPF UI, tray, settings, overlays
    Vacanam.Core/          Interfaces, models, enums (no dependencies)
    Vacanam.Infrastructure/ DI wiring, settings, null stubs
    Vacanam.Windows/       Win32 P/Invoke (Phase 2)
    Vacanam.Audio/         NAudio WASAPI capture (Phase 3)
    Vacanam.Speech/        Whisper.net inference (Phase 4)
    Vacanam.Input/         Text injection strategies (Phase 5)
    Vacanam.LLM/           LLamaSharp inference (Phase 6)
    Vacanam.Data/          SQLite history (Phase 7+)
```

---

## Privacy

Vacanam is designed privacy-first:
- ❌ No cloud APIs
- ❌ No audio ever written to disk  
- ❌ No telemetry or analytics
- ❌ No clipboard data in logs
- ✅ Transcript history is **opt-in** (disabled by default)
- ✅ All processing is local

**Log files** are stored at `%LOCALAPPDATA%\Vacanam\Logs\` and contain only operational messages — never audio, transcripts, or clipboard data.

---

## Contributing

This project is in active development. Phase 1 (App Shell) is the current stable baseline.

---

*Vacanam is an independent project. Not affiliated with or derived from Wispr Flow.*
