# Vacanam 🎙️

<div align="center">

![Vacanam Banner](https://img.shields.io/badge/Vacanam-Voice%20In.%20Words%20Out.-6366F1?style=for-the-badge&logo=windows&logoColor=white)

### *Voice In. Words Out.*

**Production-quality, local-first voice typing and on-device AI assistant for Windows.**  
*Hold `Ctrl+Space` to dictate speech. Hold `Shift+Space` to transform selected text or ask AI.*

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

### 🎙️ Dual Global Push-to-Talk Hotkeys
- **`Ctrl + Space` (Voice Typing / Dictation)**: Hold from any application (Notepad, VS Code, Word, Chrome, Windows Terminal, Slack, Outlook), speak naturally, and release to inject transcribed text directly at your cursor.
- **`Shift + Space` ("Ask AI" & Voice Transform Mode)**:
  - **Selection Transformation (Text Selected)**: Highlight text in any app (an email, code snippet, draft message) and hold `Shift+Space` to command (*"Reply politely to this email"*, *"Refactor to LINQ"*, *"Translate to Spanish"*, *"Summarize in 3 bullet points"*). The local LLM processes the selection and injects the result.
  - **Direct Generation (No Text Selected)**: Hold `Shift+Space` at an empty prompt to generate text on the fly (*"Draft an apology for being 5 minutes late"*, *"Write a python regex for email"*).
  - **Read-Only Context Auto-Persistence**: When text is highlighted in read-only panes (e.g. Outlook reading view, PDF, browser), Vacanam copies the generated response to your clipboard and notifies you with `📋 Copied to Clipboard (Ctrl+V to paste)` so you can click reply and paste immediately.

---

### 🧠 App-Specific Context Profiles
Vacanam automatically detects the foreground application process and tailors AI responses accordingly:
- **Coding & Terminal (`code`, `devenv`, `idea64`, `windowsterminal`, `powershell`)**: Preserves camelCase, snake_case, variable/function syntax, CLI flags, and avoids turning code into prose.
- **Chat & Messaging (`slack`, `teams`, `discord`, `telegram`, `whatsapp`)**: Natural, punchy, conversational tone suitable for direct messaging.
- **Email & Documents (`outlook`, `olk`, `winword`, `thunderbird`)**: Professional, well-structured business phrasing.

---

### 🔊 Audio Earcons & Feedback
- Subtle, high-polish audio cues confirm voice state changes (`start.wav` on key press, `success.wav` on injection, `error.wav` on failure) generated natively without external audio dependencies. Can be toggled on/off in Settings.

---

### 🚀 First-Run Onboarding & Quick Start Experience
- **No Auto-Dismiss Panic**: The launch window remains open until the user chooses to start or visit settings.
- **Interactive Test Scratchpad**: Built-in test box inviting new users to click, hold `Ctrl+Space`, and test their microphone on the spot.
- **System Tray Guidance**: Visual callout explaining that Vacanam runs quietly in the system tray near the Windows clock (🎙).
- **Direct Settings Navigation**: One-click **Go to Settings** button on the welcome window.
- **Re-open Anytime**: Right-click the system tray icon anytime and select **`💡 Quick Start & Shortcuts`**.

---

### 🗣️ User-Centric Speech Engine Profiles
- **`⚡ Ultra Fast`** (`tiny` ~75 MB) — Instant response for short phrases and low-end PCs. (Auto-downloaded on initial setup if no model is present).
- **`⭐ Balanced (Recommended)`** (`small` ~466 MB) — Optimal sweet spot of speed and high recognition accuracy.
- **`🎯 High Precision`** (`medium` ~1.5 GB) — Exceptional accuracy for technical & complex terms.
- **`👑 Maximum Accuracy`** (`large-v3` ~3.1 GB) — Maximum precision for multi-language speech & accents.

---

### ⚡ Standalone Voice Commands & Expansion Macros
- **Voice Action Shortcuts**: Speak commands to trigger keyboard actions hands-free:
  - `"select all"` (`Ctrl+A`), `"undo that"` (`Ctrl+Z`), `"redo that"` (`Ctrl+Y`), `"copy that"`, `"paste that"`, `"save file"` (`Ctrl+S`), `"press enter"`, `"press tab"`, `"press escape"`, `"delete line"`, `"delete word"`, `"switch window"` (`Alt+Tab`), `"close window"` (`Alt+F4`), `"lock computer"`.
- **Smart Verbal Punctuation**: Dictate spoken punctuation (*"comma"*, *"period"*, *"question mark"*, *"new line"*, *"new paragraph"*, *"open paren"*, *"smiley face"*, *"thumbs up"*, *"fire emoji"*) formatted automatically with capitalization rules.
- **Custom Snippets & Macros**: Define trigger phrases (e.g. `"insert signature"`, `"insert date"`, `"my meeting link"`) with dynamic `{DATE}`, `{TIME}`, and `{DATETIME}` tags.

---

### 🤖 Local GGUF LLM Text Refinement
- Sub-second local CPU grammar polish and instruction following using GGUF models (`Qwen2.5-0.5B-Instruct` & `Llama-3.2-1B-Instruct`) via `LLamaSharp`.
- Anti-echo retry protection and prompt conditioning prevent the LLM from repeating input text.
- Full control over System Prompts (Refinement, Transform, Ask AI) in Settings.

---

### 📜 Local SQLite Transcript History & Search
- Opt-in local SQLite database (`%LOCALAPPDATA%\Vacanam\history.db` in WAL mode).
- Logs timestamps, speech text, transformed output, and target application badges (`olk`, `code`, `slack`, `notepad`).
- Instant real-time search, one-click **Copy**, and single-entry deletion.

---

### 🔇 Microphone Health & Low Volume Alerts (<30%)
- Real-time detection of muted microphones and low volume levels (<30%), alerting you instantly on the floating overlay (`Mic Muted 🔇` / `Mic Volume 20% 🔇`).

---

### 🔒 Zero-Trust Local Privacy
- ❌ **No cloud APIs, no telemetry, no analytics.**
- ❌ **Audio is never recorded to disk.**
- ❌ **Clipboard contents are backed up and restored (<25ms) without logging.**
- ✅ **100% offline.** Works without an internet connection once models are downloaded.
- 📜 Read our full [Privacy Policy](PRIVACY.md) or online at [avikeid2007.github.io/Vacanam/privacy.html](https://avikeid2007.github.io/Vacanam/privacy.html).

---

## 📸 Pipeline Overview

```
 ┌─────────────────────────────────────────────────────────────┐
 │                     System Tray (Idle)                      │
 └──────────────┬───────────────────────────────┬──────────────┘
                │ Hold Ctrl+Space               │ Hold Shift+Space
                ▼ (Voice Typing)                ▼ (Voice Transform / Ask AI)
 ┌──────────────────────────────┐ ┌────────────────────────────┐
 │  WASAPI 16kHz PCM Capture    │ │  Capture Selected Text     │
 │  + Active Window HWND        │ │  + WASAPI Audio + Context  │
 └──────────────┬───────────────┘ └─────────────┬──────────────┘
                ▼                               ▼
 ┌──────────────────────────────┐ ┌────────────────────────────┐
 │   VAD Silence Trimming       │ │   VAD Silence Trimming     │
 │   → Whisper.net STT          │ │   → Whisper.net STT        │
 └──────────────┬───────────────┘ └─────────────┬──────────────┘
                ▼ Transcribed Speech            ▼ Voice Instruction
 ┌──────────────────────────────┐ ┌────────────────────────────┐
 │  (Optional) Local LLM Polish │ │  Local LLM Transform       │
 │  for punctuation & grammar   │ │  (Prompt + Context Profile)│
 └──────────────┬───────────────┘ └─────────────┬──────────────┘
                ▼ Final Output                  ▼ Transformed / Answer
 ┌─────────────────────────────────────────────────────────────┐
 │       Inject Text into Active Application (Ctrl+V)          │
 │       (Or auto-copy to clipboard if in read-only view)      │
 └──────────────────────────────┬──────────────────────────────┘
                                │ (If History Opt-in Enabled)
                                ▼
 ┌─────────────────────────────────────────────────────────────┐
 │       Save Transcript Record to Local SQLite History        │
 └─────────────────────────────────────────────────────────────┘
```

---

## 🚀 Quick Start & Installation

### 📥 Download Installer
1. Download `VacanamSetup-1.x.x.exe` from [GitHub Releases](https://github.com/avikeid2007/Vacanam/releases/latest).
2. Run the installer (no admin rights required).
3. The **Quick Start & Onboarding Window** will guide you through your first dictation test.
4. Vacanam minimizes quietly to your system tray — hold `Ctrl+Space` anytime to dictate.

> [!NOTE]
> **Code Signing**: Free code signing provided by [SignPath.io](https://signpath.io), certificate by [SignPath Foundation](https://signpath.org).

### Prerequisites for Building from Source
- **Windows 10 / 11** (x64)
- **.NET 10 SDK** — [Download .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0)

### Build & Run from Source
```powershell
# Clone the repository
git clone https://github.com/avikeid2007/Vacanam.git
cd Vacanam

# Restore dependencies
dotnet restore Vacanam.slnx

# Build Release binary
dotnet build Vacanam.slnx --configuration Release

# Run Vacanam
dotnet run --project src/Vacanam.App --configuration Release
```

---

## ⚙️ Settings & Configuration

Right-click the **Vacanam** tray icon (🎙) and choose **Settings**:

| Tab | Capabilities |
| :--- | :--- |
| **Hotkeys** | Configure primary dictation key (`Ctrl+Space`), toggle Push-to-Talk, and enable/disable `Shift+Space` AI Transform. |
| **Speech** | Select speech models (`Ultra Fast`, `Balanced`, `High Precision`, `Maximum Accuracy`) with live download progress. |
| **AI** | Enable/disable local LLM refinement, manage GGUF models (`Qwen 2.5 0.5B`, `Llama 3.2 1B`), and edit System Prompts (Refinement, Transform, Ask AI). |
| **Audio** | Choose input microphone, test live volume slider (0–100%), toggle mute, adjust VAD threshold, and enable/disable sound effects. |
| **Commands** | Enable standalone voice shortcuts (`"undo that"`, `"copy that"`), smart punctuation, and custom expansion snippets (`{DATE}`, `{TIME}`). |
| **History** | Search local SQLite transcripts in real time, filter by application, copy to clipboard, or delete individual entries. |
| **General** | Toggle Start with Windows, tray notifications, and quick start guide behavior. |
| **Privacy** | View offline guarantees and opt-in/out of transcript storage. |

---

## 📁 Solution Architecture

Vacanam is built with a strictly decoupled modular architecture across 8 projects:

```
Vacanam.slnx
├── src/
│   ├── Vacanam.Core/           Pure domain interfaces, models, enums (net10.0)
│   ├── Vacanam.Windows/        Win32 P/Invoke (RegisterHotKey, GetForegroundWindow, SendInput, KeySimulator)
│   ├── Vacanam.Audio/          WASAPI audio capture, pure C# AudioConverter, VAD silence trimmer
│   ├── Vacanam.Speech/         Whisper.net speech recognition & automatic model downloader
│   ├── Vacanam.LLM/            LLamaSharp v0.27.0 CPU local LLM text refinement & transform engine
│   ├── Vacanam.Input/          Text injection strategies (Clipboard backup/restore, SendInput, UIA)
│   ├── Vacanam.Infrastructure/ Settings persistence (JSON), SQLite history, DI registration
│   └── Vacanam.App/            WPF UI (Tray, Floating Overlay, Onboarding Window, Settings Window)
└── tests/
    └── Vacanam.Tests/          xUnit unit tests across core domain, audio feedback, transform, and hotkeys
```

---

## 📂 Local Storage Paths

All models, settings, databases, and operational logs are stored locally:

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

*Vacanam is an independent project designed for local offline voice typing and desktop AI assistance on Windows.*
