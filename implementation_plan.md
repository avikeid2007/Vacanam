# Phase 6 — Local LLM Integration & Ultra-Lightweight CPU Model Selection

> Integrate local LLM text refinement (grammar correction, formatting, tone adjustment) into Vacanam using **LLamaSharp v0.27.0** and ultra-lightweight GGUF models strictly **under 150 MB / 400 MB in size** for fast, low-RAM CPU execution.

---

## 1. Ultra-Lightweight Sub-400 MB & Sub-100 MB Model Catalog

The catalog features models that fit under 400 MB, including ultra-compact sub-100 MB models:

| Model Name | Quantization | GGUF Size | RAM Footprint | CPU Speed (Approx) | Recommended For | HuggingFace Repo |
|---|---|---|---|---|---|---|
| **Gemma 4 E2B Assistant** *(Ultra-Small)* | `Q4_K_M` | **~76.7 MB** | ~180 MB | ~120–160 tok/s | Lowest disk & RAM usage (<80MB file size) | `AtomicChat/gemma-4-E2B-it-assistant-GGUF` |
| **Gemma 4 E2B Assistant** *(High Quality)* | `Q8_0` | **~97.8 MB** | ~220 MB | ~110–140 tok/s | 8-bit precision under 100MB file size | `AtomicChat/gemma-4-E2B-it-assistant-GGUF` |
| **SmolLM2-360M-Instruct** | `Q4_K_M` | **~230 MB** | ~350 MB | ~90–130 tok/s | Smallest 360M general instruction SLM | `bartowski/SmolLM2-360M-Instruct-GGUF` |
| **Qwen2.5-0.5B-Instruct** *(Default)* | `Q4_K_S` | **~390 MB** | ~600 MB | ~60–90 tok/s | Top grammar accuracy for 0.5B models | `bartowski/Qwen2.5-0.5B-Instruct-GGUF` |

> [!TIP]
> **Sub-100 MB Highlight**: **`gemma-4-E2B-it-assistant.Q4_K_M.gguf`** is only **76.7 MB** on disk and takes **<200 MB RAM**, making it lightning fast for CPU dictation cleanup!

---

## 2. Solution & Project Architecture — Phase 6

```
src/
├── Vacanam.LLM/                       ← [NEW] Local LLM execution project
│   ├── DependencyInjection/
│   │   └── LlmServiceCollectionExtensions.cs
│   ├── Model/
│   │   ├── LlmModelManager.cs          ← Manages ultra-lightweight GGUF downloads
│   │   └── LlmModelDescriptor.cs       ← Catalog of sub-400MB models
│   ├── Processing/
│   │   └── LlmTextProcessor.cs         ← LLamaSharp CPU inference & streaming
│   └── Prompts/
│       └── SystemPrompts.cs            ← Presets (Grammar Fix, Professional, Concise, Code)
```

### NuGet Packages — `Vacanam.LLM`

| Package | Version | Purpose |
|---|---|---|
| `LLamaSharp` | `0.27.0` | C# bindings for llama.cpp (v0.27.0) |
| `LLamaSharp.Backend.Cpu` | `0.27.0` | Native CPU AVX2/AVX512 binaries (v0.27.0) |

---

## 3. Key Technical Specifications

### System Prompt (`SystemPrompts.cs`)

```csharp
public static class SystemPrompts
{
    public const string DefaultGrammarFix = """
        You are a silent, ultra-fast text polish engine. Your job is to clean up transcribed speech.
        RULES:
        1. Fix capitalization, punctuation, and obvious grammar errors.
        2. Remove filler words (uh, um, like, you know).
        3. DO NOT change facts, numbers, names, code, or intentional word choices.
        4. Return ONLY the cleaned text. Do NOT add notes, explanations, or quotes around the output.
        """;
}
```

### Model Storage & Lifecycle

- Model Directory: `%LOCALAPPDATA%\Vacanam\Models\LLM\`
- Catalog: Filtered exclusively to models with `FileSizeBytes <= 400 MB` (including ~76.7 MB Gemma E2B Assistant).
- Memory Management: Lazy loading on demand; automatically disposes native context after 3 minutes of idle time to free RAM (< 50 MB resident memory when idle).

---

## 4. Proposed File Changes

### Solution Root
#### [MODIFY] [Vacanam.slnx](file:///d:/Vacanam/Vacanam.slnx) — Add `Vacanam.LLM.csproj` project.

---

### [NEW] `Vacanam.LLM` Project
#### [NEW] [Vacanam.LLM.csproj](file:///d:/Vacanam/src/Vacanam.LLM/Vacanam.LLM.csproj)
#### [NEW] `src/Vacanam.LLM/DependencyInjection/LlmServiceCollectionExtensions.cs`
#### [NEW] `src/Vacanam.LLM/Model/LlmModelDescriptor.cs`
#### [NEW] `src/Vacanam.LLM/Model/LlmModelManager.cs`
#### [NEW] `src/Vacanam.LLM/Processing/LlmTextProcessor.cs`
#### [NEW] `src/Vacanam.LLM/Prompts/SystemPrompts.cs`

---

### Integration Updates
#### [MODIFY] [ServiceCollectionExtensions.cs](file:///d:/Vacanam/src/Vacanam.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs)
Register `Vacanam.LLM` services.

#### [MODIFY] [SettingsViewModel.cs](file:///d:/Vacanam/src/Vacanam.App/ViewModels/SettingsViewModel.cs)
Connect LLM model download commands, progress tracking, and model selection.

#### [MODIFY] [MainViewModel.cs](file:///d:/Vacanam/src/Vacanam.App/ViewModels/MainViewModel.cs)
Pass transcribed text through `ITextProcessor` when AI mode is enabled.

---

## 5. Verification Plan

### Automated Build Verification
```powershell
dotnet build Vacanam.slnx --configuration Debug
```

### Manual & Performance Verification
1. **File Size Check**: Confirm `gemma-4-E2B-it-assistant.Q4_K_M.gguf` downloads cleanly at **~76.7 MB**.
2. **CPU Inference Latency**: Verify transcription cleanup response is sub-second (< 200ms) on CPU using LLamaSharp 0.27.0.
3. **RAM Overhead**: Verify peak RAM during inference stays under 200 MB.
