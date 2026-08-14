# Vacanam 🎙️

<div align="center">

![Vacanam Banner](https://img.shields.io/badge/Vacanam-Voice%20In.%20Words%20Out.-6366F1?style=for-the-badge&logo=windows&logoColor=white)

### *Voice In. Words Out.*

**Production-quality, local-first voice typing and AI text refinement for Windows.**  
*Hold `Ctrl+Space` anywhere, speak naturally, and watch your polished words appear instantly.*

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011%20x64-0078D4?style=flat-square&logo=windows)](https://microsoft.com)
[![Framework](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com)
[![Website](https://img.shields.io/badge/Website-avikeid2007.github.io%2FVacanam-6366F1?style=flat-square&logo=googlechrome&logoColor=white)](https://avikeid2007.github.io/Vacanam/)
[![STT Engine](https://img.shields.io/badge/STT-Whisper.net%20v1.7.4-10B981?style=flat-square)](https://github.com/samm308/whisper.net)
[![LLM Engine](https://img.shields.io/badge/LLM-LLamaSharp%20v0.27.0-837AF9?style=flat-square)](https://github.com/SciSharp/LLamaSharp)
[![Database](https://img.shields.io/badge/Database-SQLite%20(WAL)-003B57?style=flat-square&logo=sqlite)](https://sqlite.org)
[![Privacy](https://img.shields.io/badge/Privacy-100%25%20Offline%20%26%20Local-10B981?style=flat-square&logo=lock)](https://github.com)

---

</div>

## 🌟 Key Features

- 🎙️ **Global Push-to-Talk (`Ctrl+Space`)** — Hold from any application (Notepad, VS Code, Word, Chrome, Terminal, Slack, Outlook), speak, release.
- 🚀 **Zero-Configuration Auto-Setup** — Launches with a sleek animated splash banner. If no speech model is downloaded on launch, Vacanam automatically downloads and selects the **Ultra Fast (`tiny` ~75 MB)** model.
- 🗣️ **User-Centric Speech Engine Profiles**:
  - **`⚡ Ultra Fast`** (`tiny` ~75 MB) — Instant response for short phrases & low-end PCs.
  - **`⭐ Balanced (Recommended)`** (`small` ~466 MB) — Optimal balance of speed and high accuracy.
  - **`🎯 High Precision`** (`medium` ~1.5 GB) — Exceptional accuracy for technical & complex terms.
  - **`👑 Maximum Accuracy`** (`large-v3` ~3.1 GB) — Maximum precision for multi-language speech & accents.
- 🤖 **Local LLM Text Refinement (AI Mode)** — Sub-second CPU grammar polish using local GGUF models (`Qwen2.5-0.5B-Instruct` & `Llama-3.2-1B-Instruct`) via `LLamaSharp`. Custom System Prompts editable directly in Settings.
- 📜 **Local SQLite Transcript History & Search** — Opt-in local SQLite database (`%LOCALAPPDATA%\Vacanam\history.db` in WAL mode) saving timestamps, raw speech, polished text, and target app badges (`devenv`, `notepad`, `chrome`). Features instant real-time search, one-click **Copy**, and single-entry deletion.
- 🔇 **Microphone Health & Low Volume Alerts (<30%)** — Real-time detection of muted microphones and low volume levels (<30%), warning you instantly on the floating overlay (`Mic Muted 🔇` / `Mic Volume 20% 🔇`).
- 🎨 **Windows 11 Fluent Dark UI** — Animated Launch Banner, glassmorphic floating overlay with audio visualizer pulse ring, and dark Settings UI.
- 🎯 **Smart 3-Strategy Text Injection**:
  - **Clipboard + Ctrl+V (Primary)**: Backup original clipboard → Paste → Restore original clipboard (<25ms).
  - **SendInput Unicode (Fallback 1)**: Native keystroke simulation for terminal windows (`cmd.exe`, `powershell`, `wt`) without touching clipboard.
  - **UI Automation (Fallback 2)**: `ValuePattern.SetValue` for accessible controls.
- 🔒 **Zero-Trust Local Privacy**:
  - ❌ No cloud APIs, no telemetry, no analytics
  - ❌ Audio is never saved to disk
  - ❌ Clipboard contents are never logged
  - ✅ Transcript history is **opt-in** (disabled by default) and stored 100% locally.
  - 📜 Read our full [Privacy Policy](file:///d:/Vacanam/PRIVACY.md) or online at [avikeid2007.github.io/Vacanam/privacy.html](https://avikeid2007.github.io/Vacanam/privacy.html).

---

## 📸 Pipeline Overview

```
 ┌─────────────────────────────────────────────────────────┐
 │                   System Tray (Idle)                    │
 └────────────────────────────┬────────────────────────────┘
                              │ Hold Ctrl+Space
                              ▼
 ┌─────────────────────────────────────────────────────────┐
 │  WASAPI 16kHz PCM Capture + Active Window Context HWND  │
 └────────────────────────────┬────────────────────────────┘
                              │ Release Ctrl+Space
                              ▼
 ┌─────────────────────────────────────────────────────────┐
 │   VAD Silence Trimming → Multi-Threaded Whisper.net     │
 └────────────────────────────┬────────────────────────────┘
                              │ Transcribed Text
                              ▼
 ┌─────────────────────────────────────────────────────────┐
 │  (Optional) LLamaSharp GGUF Local LLM Text Polish       │
 └────────────────────────────┬────────────────────────────┘
                              │ Final Text
                              ▼
 ┌─────────────────────────────────────────────────────────┐
 │  Inject Text into Active App (Clipboard / SendInput)    │
 └────────────────────────────┬────────────────────────────┘
                              │ (Opt-in)
                              ▼
 ┌─────────────────────────────────────────────────────────┐
 │   Save Transcript Record to SQLite (%LOCALAPPDATA%)     │
 └─────────────────────────────────────────────────────────┘
```

---

## 🚀 Quick Start & Installation

### 📥 Download Installer
1. Download the latest installer `VacanamSetup-1.x.x.exe` from [GitHub Releases](https://github.com/avikeid2007/Vacanam/releases/latest).
2. Run `VacanamSetup-1.x.x.exe` (no administrative privileges required).
3. Vacanam will launch automatically in your System Tray — hold `Ctrl+Space` anywhere to dictate.

> [!NOTE]
> **Code Signing**: Free code signing provided by [SignPath.io](https://signpath.io), certificate by [SignPath Foundation](https://signpath.org).

### Prerequisites for Building from Source
- **Windows 10 / 11** (x64)
- **.NET 10 SDK** — [Download .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0)

### Build & Run from Source
```powershell
# Clone the repository
git clone https://github.com/your-org/vacanam.git
cd Vacanam

# Restore dependencies
dotnet restore Vacanam.slnx

# Build Release binary
dotnet build Vacanam.slnx --configuration Release

# Run Vacanam (runs in System Tray)
dotnet run --project src/Vacanam.App --configuration Release
```

---

## ⚙️ Settings & Model Management

Right-click the **Vacanam** tray icon and click **Settings**:

- **Speech Tab**:
  - **User-Centric Profiles**: Select from `Ultra Fast`, `Balanced`, `High Precision`, or `Maximum Accuracy`.
  - **Live Model Badges**: See which models are `ACTIVE & READY`, `DOWNLOADED`, or `NOT DOWNLOADED`.
  - **One-Click Download & Select**: Download models directly from Settings with live progress tracking.
- **Audio Tab**: Live **Microphone Volume Slider (0 - 100%)**, **Mute Microphone Toggle**, and Voice Activity Detection (VAD) threshold settings.
- **AI Tab**: Enable/disable AI text polish, customize **System Prompt / Grammar Rules**, and manage local GGUF models (`Qwen 2.5 0.5B` & `Llama 3.2 1B`).
- **History Tab**: **Local SQLite Transcript Search**, instant real-time filtering, target application badges, one-click **Copy**, single-record deletion, and **Clear All History**.
- **General Tab**: Startup & notification options.
- **Privacy Tab**: Local-first guarantees & opt-in transcript history toggles.

---

## 📁 Solution Architecture

Vacanam is built with a strictly decoupled modular architecture across 8 projects:

```
Vacanam.slnx
├── src/
│   ├── Vacanam.Core/           Pure domain interfaces, models, and enums (net10.0)
│   ├── Vacanam.Windows/        Win32 P/Invoke (RegisterHotKey, GetForegroundWindow, SendInput)
│   ├── Vacanam.Audio/          WASAPI audio capture, pure C# AudioConverter, VAD
│   ├── Vacanam.Speech/         Whisper.net speech recognition & automatic model downloader
│   ├── Vacanam.LLM/            LLamaSharp v0.27.0 CPU local LLM text refinement engine
│   ├── Vacanam.Input/          Text injection strategies (Clipboard backup/restore, SendInput, UIA)
│   ├── Vacanam.Infrastructure/ Settings persistence (JSON), SQLite history, DI registration
│   └── Vacanam.App/            WPF UI (Tray, Floating Overlay, Launch Banner, Settings Window)
```

---

## 📂 Local Model & Storage Paths

Models, settings, databases, and operational logs are stored in your local application data directory:

```
%LOCALAPPDATA%\Vacanam\
├── Models\
│   ├── Whisper\
│   │   ├── ggml-tiny.bin (Ultra Fast ~75 MB)
│   │   └── ggml-small.bin (Balanced ~466 MB, Recommended)
│   └── LLM\
│       ├── Qwen2.5-0.5B-Instruct-Q4_K_M.gguf (~398 MB)
│       └── Llama-3.2-1B-Instruct-Q4_K_M.gguf (~808 MB)
├── history.db (SQLite database - opt-in history)
├── Logs\
│   └── vacanam-YYYYMMDD.log
└── settings.json
```

---

## 📜 License

Distributed under the **MIT License**. Free and open-source forever.

---

*Vacanam is an independent project designed for local offline voice typing on Windows.*
