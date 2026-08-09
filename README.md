# Vacanam

<div align="center">

![Vacanam Banner](https://img.shields.io/badge/Vacanam-Local%20Voice%20Typing-6366F1?style=for-the-badge&logo=windows&logoColor=white)

**Production-quality, local-first voice typing application for Windows.**  
*Hold `Ctrl+Space` anywhere, speak, and watch your words appear instantly.*

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011%20x64-0078D4?style=flat-square&logo=windows)](https://microsoft.com)
[![Framework](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com)
[![STT Engine](https://img.shields.io/badge/STT-Whisper.net%20v1.7.4-10B981?style=flat-square)](https://github.com/samm308/whisper.net)
[![Privacy](https://img.shields.io/badge/Privacy-100%25%20Offline%20%26%20Local-10B981?style=flat-square&logo=lock)](https://github.com)

---

</div>

## 🌟 Features

- 🎙️ **Global Push-to-Talk (`Ctrl+Space`)** — Hold from any application (Notepad, VS Code, Word, Chrome, Terminal, Slack), speak, release.
- 🔇 **Microphone Health & Low Volume Alerts (<30%)** — Real-time detection of muted microphones and low volume levels (<30%), warning you instantly on the floating overlay (`Mic Muted 🔇` / `Mic Volume 20% 🔇`).
- ⚡ **Ultra-Low Latency (<200ms)** — Multi-threaded CPU parallel inference (`OpenMP`/`AVX2`) + automatic VAD silence trimming.
- 🤖 **100% Offline Speech Recognition** — Powered by `whisper.cpp` via `Whisper.net`. Supports `Tiny`, `Small`, `Medium`, and `Large-v3` GGML models.
- 🎯 **Smart 3-Strategy Text Injection**:
  - **Clipboard + Ctrl+V (Primary)**: Backup original clipboard → Paste → Restore original clipboard (<25ms).
  - **SendInput Unicode (Fallback 1)**: Native keystroke simulation for terminal windows (`cmd.exe`, `powershell`, `wt`) without touching clipboard.
  - **UI Automation (Fallback 2)**: `ValuePattern.SetValue` for accessible controls.
- 🎨 **Modern Dark Design System** — Glassmorphism floating overlay with real-time audio visualizer pulse ring + dark settings UI.
- 🔒 **Zero-Trust Local Privacy**:
  - ❌ No cloud APIs or telemetry
  - ❌ Audio is never saved to disk
  - ❌ Clipboard contents are never logged
  - ✅ Transcript history is **opt-in** (disabled by default)

---

## 📸 Overview

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
                              │ <25ms
                              ▼
 ┌─────────────────────────────────────────────────────────┐
 │  Inject Text into Active App (Clipboard / SendInput)    │
 └─────────────────────────────────────────────────────────┘
```

---

## 🚀 Quick Start

### Prerequisites
- **Windows 10 / 11** (x64)
- **.NET 10 SDK** — [Download .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0)

### Build & Run
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
  - **Live Model Badges**: Instantly see which models are `ACTIVE & READY`, `DOWNLOADED`, or `NOT DOWNLOADED`.
  - **One-Click Download**: Download any Whisper model (`Tiny ~75MB`, `Small ~466MB`, `Medium ~1.5GB`, `Large-v3 ~3.1GB`) directly from Settings with live progress tracking.
  - **One-Click Select**: Switch active Whisper models instantly.
- **Audio Tab**: Live **Microphone Volume Slider (0 - 100%)**, **Mute Microphone Toggle**, and Voice Activity Detection (VAD) threshold settings.
- **General Tab**: Startup & notification options.
- **Privacy Tab**: Local-first guarantees & optional transcript history controls.

---

## 📁 Solution Architecture

Vacanam is built with a strictly decoupled modular architecture:

```
Vacanam.slnx
├── src/
│   ├── Vacanam.Core/           Pure domain interfaces, models, and enums (net10.0)
│   ├── Vacanam.Windows/        Win32 P/Invoke (RegisterHotKey, GetForegroundWindow, SendInput)
│   ├── Vacanam.Audio/          WASAPI audio capture, pure C# AudioConverter, VAD
│   ├── Vacanam.Speech/         Whisper.net speech recognition & automatic model downloader
│   ├── Vacanam.Input/          Text injection strategies (Clipboard backup/restore, SendInput, UIA)
│   ├── Vacanam.Infrastructure/ Settings persistence (JSON) & generic host DI registration
│   └── Vacanam.App/            WPF UI (Tray, Floating Overlay, Settings Window, ViewModels)
```

---

## 📂 Local Model & Storage Paths

Models and operational logs are stored in your local application data directory:

```
%LOCALAPPDATA%\Vacanam\
├── Models\
│   ├── Whisper\
│   │   ├── ggml-tiny.bin
│   │   └── ggml-small.bin (Default)
│   └── LLM\
├── Logs\
│   └── vacanam-YYYYMMDD.log
└── settings.json
```

---

## 📜 License

Distributed under the **MIT License**. Free and open-source forever.

---

*Vacanam is an independent project designed for local offline voice typing on Windows.*
