using System.Text;
using LLama;
using LLama.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vacanam.Core.Interfaces;
using Vacanam.Core.Models;
using Vacanam.LLM.Model;
using Vacanam.LLM.Prompts;

namespace Vacanam.LLM.Processing;

public sealed class LlmTextProcessor : ITextProcessor
{
    private readonly IOptions<AppSettings> _settings;
    private readonly LlmModelManager _modelManager;
    private readonly ILogger<LlmTextProcessor> _logger;

    private LLamaWeights? _weights;
    private ModelParams? _modelParams;
    private string? _loadedModelFile;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;

    public bool IsEnabled => _weights is not null;
    public event EventHandler<TokenEventArgs>? TokenGenerated;

    public LlmTextProcessor(
        IOptions<AppSettings> settings,
        LlmModelManager modelManager,
        ILogger<LlmTextProcessor> logger)
    {
        _settings = settings;
        _modelManager = modelManager;
        _logger = logger;
    }

    public async Task LoadModelAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        string modelFile = _settings.Value.Ai.ModelFile;
        if (string.IsNullOrWhiteSpace(modelFile))
        {
            modelFile = "gemma-4-E2B-it-assistant.Q4_K_M.gguf";
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_weights is not null && string.Equals(_loadedModelFile, modelFile, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!_modelManager.LlmModelExists(modelFile))
            {
                _logger.LogInformation("LLM model '{File}' not found locally. Auto downloading...", modelFile);
                await _modelManager.EnsureLlmModelDownloadedAsync(modelFile, cancellationToken: cancellationToken);
            }

            string modelPath = _modelManager.GetLlmModelPath(modelFile);
            _logger.LogInformation("Loading LLamaSharp weights from '{Path}'...", modelPath);

            _weights?.Dispose();
            _weights = null;

            int cpuThreads = Math.Max(2, Environment.ProcessorCount - 1);
            _modelParams = new ModelParams(modelPath)
            {
                ContextSize = 2048,
                GpuLayerCount = 0, // CPU low-RAM execution
                Threads = cpuThreads
            };

            _weights = LLamaWeights.LoadFromFile(_modelParams);
            _loadedModelFile = modelFile;
            _logger.LogInformation("LLM model '{File}' successfully loaded into CPU memory.", modelFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load LLM model '{File}'.", modelFile);
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string> ProcessAsync(
        string rawTranscript,
        ApplicationContext context,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrWhiteSpace(rawTranscript))
        {
            return string.Empty;
        }

        if (!IsEnabled || !string.Equals(_loadedModelFile, _settings.Value.Ai.ModelFile, StringComparison.OrdinalIgnoreCase))
        {
            await LoadModelAsync(cancellationToken);
        }

        _logger.LogInformation("Starting LLM text refinement on transcript: '{Raw}'", rawTranscript);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            string systemPrompt = ResolveSystemPrompt(_settings.Value.Ai, context);
            if (_settings.Value.Ai.EnableContextProfiles && context.IsValid)
            {
                _logger.LogInformation("Context profile resolved for process '{Process}' ({Title})", context.ProcessName, context.WindowTitle);
            }

            // Standard ChatML format for Qwen2.5, SmolLM2, Llama-3 instruction models
            string prompt =
                $"<|im_start|>system\n{systemPrompt}\n<|im_end|>\n" +
                $"<|im_start|>user\n{rawTranscript}\n<|im_end|>\n" +
                $"<|im_start|>assistant\n";

            var executor = new StatelessExecutor(_weights!, _modelParams!);
            var inferenceParams = new InferenceParams
            {
                MaxTokens = _settings.Value.Ai.MaxTokens > 0 ? _settings.Value.Ai.MaxTokens : 128,
                AntiPrompts =
                [
                    "<|im_end|>",
                    "<|endoftext|>",
                    "<|im_start|>",
                    "</s>",
                    "\nNote:",
                    "\nExplanation:",
                    "\nThe text you provided"
                ]
            };

            var sb = new StringBuilder();
            await foreach (var token in executor.InferAsync(prompt, inferenceParams, cancellationToken))
            {
                sb.Append(token);
                TokenGenerated?.Invoke(this, new TokenEventArgs(token));

                // Early exit if token stream starts generating chatter on a new line
                string currentText = sb.ToString();
                if (currentText.Contains("\nNote:", StringComparison.OrdinalIgnoreCase) ||
                    currentText.Contains("\nExplanation:", StringComparison.OrdinalIgnoreCase) ||
                    currentText.Contains("\nThe text you provided", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
            }

            string rawOutput = sb.ToString().Trim();

            // Post-processing: extract cleaned content and strip notes/explanations
            string cleaned = SanitizeLlmOutput(rawOutput);

            _logger.LogInformation("LLM refinement completed: '{Result}'", cleaned);
            return string.IsNullOrWhiteSpace(cleaned) ? rawTranscript : cleaned;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM refinement failed. Falling back to raw transcript.");
            return rawTranscript;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string> TransformAsync(
        string voiceInstruction,
        string? selectedText,
        ApplicationContext context,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrWhiteSpace(voiceInstruction))
        {
            return selectedText ?? string.Empty;
        }

        if (!IsEnabled || !string.Equals(_loadedModelFile, _settings.Value.Ai.ModelFile, StringComparison.OrdinalIgnoreCase))
        {
            await LoadModelAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Starting LLM Voice Transform: instruction='{Instruction}', hasSelectedText={HasText} ({Length} chars)",
            voiceInstruction, !string.IsNullOrEmpty(selectedText), selectedText?.Length ?? 0);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            string prompt = BuildTransformPrompt(voiceInstruction, selectedText, _settings.Value.Ai, context, out string systemPrompt);

            var executor = new StatelessExecutor(_weights!, _modelParams!);
            var inferenceParams = new InferenceParams
            {
                MaxTokens = _settings.Value.Ai.MaxTokens > 0 ? _settings.Value.Ai.MaxTokens : 256,
                AntiPrompts =
                [
                    "<|im_end|>",
                    "<|endoftext|>",
                    "<|im_start|>",
                    "</s>",
                    "\nNote:",
                    "\nExplanation:",
                    "\nThe text you provided"
                ]
            };

            var sb = new StringBuilder();
            await foreach (var token in executor.InferAsync(prompt, inferenceParams, cancellationToken))
            {
                sb.Append(token);
                TokenGenerated?.Invoke(this, new TokenEventArgs(token));

                string currentText = sb.ToString();
                if (currentText.Contains("\nNote:", StringComparison.OrdinalIgnoreCase) ||
                    currentText.Contains("\nExplanation:", StringComparison.OrdinalIgnoreCase) ||
                    currentText.Contains("\nThe text you provided", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
            }

            string rawOutput = sb.ToString().Trim();
            string cleaned = SanitizeLlmOutput(rawOutput);

            // Guard against LLM parroting the input reference text unchanged
            bool isIdentical = !string.IsNullOrWhiteSpace(selectedText) &&
                               string.Equals(cleaned.Trim(), selectedText.Trim(), StringComparison.OrdinalIgnoreCase);

            if (isIdentical)
            {
                _logger.LogWarning("LLM returned identical text to selection. Retrying with explicit non-echo directive...");
                string retryPrompt =
                    $"<|im_start|>system\n{systemPrompt}\nCRITICAL: You MUST write a new response or reply. Do NOT repeat or echo the reference text.<|im_end|>\n" +
                    $"<|im_start|>user\nWrite a response/reply to the following text according to: {voiceInstruction.Trim()}\n\nReference:\n\"\"\"\n{selectedText!.Trim()}\n\"\"\"<|im_end|>\n" +
                    $"<|im_start|>assistant\n";

                var retrySb = new StringBuilder();
                await foreach (var retryToken in executor.InferAsync(retryPrompt, inferenceParams, cancellationToken))
                {
                    retrySb.Append(retryToken);
                    TokenGenerated?.Invoke(this, new TokenEventArgs(retryToken));
                    string currentText = retrySb.ToString();
                    if (currentText.Contains("\nNote:", StringComparison.OrdinalIgnoreCase) ||
                        currentText.Contains("\nExplanation:", StringComparison.OrdinalIgnoreCase) ||
                        currentText.Contains("\nThe text you provided", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }
                }

                string retryRaw = retrySb.ToString().Trim();
                string retryCleaned = SanitizeLlmOutput(retryRaw);
                if (!string.IsNullOrWhiteSpace(retryCleaned) &&
                    !string.Equals(retryCleaned.Trim(), selectedText.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    cleaned = retryCleaned;
                }
            }

            _logger.LogInformation("LLM Voice Transform completed: '{Result}'", cleaned);
            return string.IsNullOrWhiteSpace(cleaned) ? (selectedText ?? voiceInstruction) : cleaned;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM Voice Transform failed.");
            return selectedText ?? voiceInstruction;
        }
        finally
        {
            _lock.Release();
        }
    }

    public static string BuildTransformPrompt(
        string voiceInstruction,
        string? selectedText,
        AiSettings settings,
        ApplicationContext context,
        out string systemPrompt)
    {
        bool hasSelection = !string.IsNullOrWhiteSpace(selectedText);

        string baseSystemPrompt;
        string userContent;

        if (hasSelection)
        {
            baseSystemPrompt = !string.IsNullOrWhiteSpace(settings.TransformSystemPrompt)
                ? settings.TransformSystemPrompt
                : "You are an expert desktop AI assistant. The user has highlighted reference text in their active application and provided a voice instruction.\n" +
                  "YOUR ROLE:\n" +
                  "- If the instruction asks to REPLY, RESPOND, or FOLLOW UP: Draft a clear, professional, complete reply/response to the reference text.\n" +
                  "- If the instruction asks to REWRITE, EDIT, PARAPHRASE, TRANSLATE, or POLISH: Rewrite the reference text following the user's instructions.\n" +
                  "- If the instruction asks to SUMMARIZE, EXPLAIN, or EXTRACT: Provide the requested summary, explanation, or extracted details based on the reference text.\n" +
                  "CRITICAL RULES:\n" +
                  "1. Output ONLY the resulting content to be inserted.\n" +
                  "2. NEVER simply repeat or echo the reference text unchanged. Always execute the requested reply, rewrite, or action.\n" +
                  "3. Do NOT add conversational filler or preamble (NO 'Here is your reply:', 'Sure!', 'Transformed text:').\n" +
                  "4. Do NOT add notes, explanations, or quotes around the output.";

            userContent =
                $"Reference Text:\n\"\"\"\n{selectedText!.Trim()}\n\"\"\"\n\n" +
                $"Instruction: {voiceInstruction.Trim()}";
        }
        else
        {
            baseSystemPrompt = !string.IsNullOrWhiteSpace(settings.AskAiSystemPrompt)
                ? settings.AskAiSystemPrompt
                : "You are a direct, concise voice AI assistant.\n" +
                  "Answer the user's prompt directly and accurately.\n" +
                  "RULES:\n" +
                  "1. Output ONLY the direct answer/content requested for immediate insertion into the active application.\n" +
                  "2. Do NOT include conversational greetings ('Sure!', 'Here you go:') or trailing commentary.\n" +
                  "3. Format cleanly (e.g. code blocks, bullet points) as appropriate.";

            userContent = voiceInstruction.Trim();
        }

        var sbSys = new StringBuilder(baseSystemPrompt);

        if (settings.EnableContextProfiles && context.IsValid && settings.ContextProfiles is { Count: > 0 })
        {
            var matchedProfile = settings.ContextProfiles.FirstOrDefault(p =>
                p.IsEnabled &&
                !string.IsNullOrWhiteSpace(p.ProcessMatches) &&
                p.ProcessMatches.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Any(m => string.Equals(m, context.ProcessName, StringComparison.OrdinalIgnoreCase)));

            if (matchedProfile is not null && !string.IsNullOrWhiteSpace(matchedProfile.PromptInstruction))
            {
                sbSys.Append("\n\n").Append(matchedProfile.PromptInstruction.Trim());
            }
        }

        systemPrompt = sbSys.ToString();

        return
            $"<|im_start|>system\n{systemPrompt}\n<|im_end|>\n" +
            $"<|im_start|>user\n{userContent}\n<|im_end|>\n" +
            $"<|im_start|>assistant\n";
    }

    public async Task UnloadModelAsync()
    {
        await _lock.WaitAsync();
        try
        {
            _weights?.Dispose();
            _weights = null;
            _loadedModelFile = null;
            _logger.LogInformation("LLM model unloaded from memory.");
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _weights?.Dispose();
        _weights = null;
        _lock.Dispose();
    }

    public static string SanitizeLlmOutput(string rawOutput)
    {
        if (string.IsNullOrWhiteSpace(rawOutput)) return string.Empty;

        var lines = rawOutput.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        var resultLines = new List<string>();

        string[] stopPrefixes =
        [
            "note:",
            "explanation:",
            "the text you provided",
            "the original text",
            "here is what i changed",
            "here's what i changed",
            "changes made:"
        ];

        foreach (var line in lines)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                if (resultLines.Count > 0)
                {
                    resultLines.Add(string.Empty);
                }
                continue;
            }

            // Check if this line begins meta-commentary
            bool isMeta = false;
            foreach (var prefix in stopPrefixes)
            {
                if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    isMeta = true;
                    break;
                }
            }

            if (isMeta)
            {
                // Discard this line and all subsequent lines (trailing commentary)
                break;
            }

            // Strip leading prompt repetition / labels on the first substantive line
            if (resultLines.Count == 0)
            {
                if (trimmed.StartsWith("Cleaned text:", StringComparison.OrdinalIgnoreCase))
                    trimmed = trimmed["Cleaned text:".Length..].Trim();
                else if (trimmed.StartsWith("Output:", StringComparison.OrdinalIgnoreCase))
                    trimmed = trimmed["Output:".Length..].Trim();
                else if (trimmed.StartsWith("Result:", StringComparison.OrdinalIgnoreCase))
                    trimmed = trimmed["Result:".Length..].Trim();
                else if (trimmed.StartsWith("Transformed text:", StringComparison.OrdinalIgnoreCase))
                    trimmed = trimmed["Transformed text:".Length..].Trim();
                else if (trimmed.StartsWith("Transformed:", StringComparison.OrdinalIgnoreCase))
                    trimmed = trimmed["Transformed:".Length..].Trim();
                else if (trimmed.StartsWith("Answer:", StringComparison.OrdinalIgnoreCase))
                    trimmed = trimmed["Answer:".Length..].Trim();
                else if (trimmed.StartsWith("Response:", StringComparison.OrdinalIgnoreCase))
                    trimmed = trimmed["Response:".Length..].Trim();
                else if (trimmed.StartsWith("Reply:", StringComparison.OrdinalIgnoreCase))
                    trimmed = trimmed["Reply:".Length..].Trim();
            }

            resultLines.Add(trimmed);
        }

        // Trim trailing empty lines
        while (resultLines.Count > 0 && string.IsNullOrWhiteSpace(resultLines[^1]))
        {
            resultLines.RemoveAt(resultLines.Count - 1);
        }

        string text = string.Join("\n", resultLines).Trim();

        // Strip surrounding quotes if the whole text is wrapped
        if ((text.StartsWith('"') && text.EndsWith('"')) || (text.StartsWith('\'') && text.EndsWith('\'')))
        {
            if (text.Length >= 2)
            {
                text = text[1..^1].Trim();
            }
        }

        return text;
    }

    /// <summary>
    /// Constructs the final LLM system prompt combining base prompt, ConservativeMode constraints,
    /// and any matching App-Specific Context Profile instructions.
    /// </summary>
    public static string ResolveSystemPrompt(AiSettings settings, ApplicationContext context)
    {
        string basePrompt = string.IsNullOrWhiteSpace(settings.SystemPrompt)
            ? SystemPrompts.DefaultGrammarFix
            : settings.SystemPrompt;

        var sb = new StringBuilder(basePrompt);

        if (settings.ConservativeMode)
        {
            sb.Append("\nCRITICAL: Do not rephrase, reword, or rewrite the text. Only fix obvious spelling mistakes and punctuation. Keep all original words and sentence structures intact.");
        }

        if (settings.EnableContextProfiles && context.IsValid && settings.ContextProfiles is { Count: > 0 })
        {
            var matchedProfile = settings.ContextProfiles.FirstOrDefault(p =>
                p.IsEnabled &&
                !string.IsNullOrWhiteSpace(p.ProcessMatches) &&
                p.ProcessMatches.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Any(m => string.Equals(m, context.ProcessName, StringComparison.OrdinalIgnoreCase)));

            if (matchedProfile is not null && !string.IsNullOrWhiteSpace(matchedProfile.PromptInstruction))
            {
                sb.Append("\n\n").Append(matchedProfile.PromptInstruction.Trim());
            }
        }

        return sb.ToString();
    }
}
