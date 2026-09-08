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
            string systemPrompt = _settings.Value.Ai.SystemPrompt;
            if (string.IsNullOrWhiteSpace(systemPrompt))
            {
                systemPrompt = SystemPrompts.DefaultGrammarFix;
            }

            if (_settings.Value.Ai.ConservativeMode)
            {
                systemPrompt += "\nCRITICAL: Do not rephrase, reword, or rewrite the text. Only fix obvious spelling mistakes and punctuation. Keep all original words and sentence structures intact.";
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
}
