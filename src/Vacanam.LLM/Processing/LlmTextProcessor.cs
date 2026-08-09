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
                    "\n\n",
                    "\nNote:",
                    "\nThe text",
                    "Explanation:"
                ]
            };

            var sb = new StringBuilder();
            await foreach (var token in executor.InferAsync(prompt, inferenceParams, cancellationToken))
            {
                sb.Append(token);
                TokenGenerated?.Invoke(this, new TokenEventArgs(token));

                // Early exit if token stream starts generating chatter
                string currentText = sb.ToString();
                if (currentText.Contains("Note:", StringComparison.OrdinalIgnoreCase) ||
                    currentText.Contains("Explanation:", StringComparison.OrdinalIgnoreCase) ||
                    currentText.Contains("The text you provided", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
            }

            string rawOutput = sb.ToString().Trim();

            // Post-processing: extract ONLY the first cleaned line and strip notes/explanations
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
            _modelLockRelease();
        }
    }

    private void _modelLockRelease()
    {
        _lock.Release();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _weights?.Dispose();
        _weights = null;
        _lock.Dispose();
    }

    private static string SanitizeLlmOutput(string rawOutput)
    {
        if (string.IsNullOrWhiteSpace(rawOutput)) return string.Empty;

        string text = rawOutput;

        // Cut off explanations, notes, or meta-commentary
        string[] stopSubstrings = ["Note:", "Explanation:", "The text you provided", "The original text", "The cleaned text", "Cleaned text:"];
        foreach (var sub in stopSubstrings)
        {
            int index = text.IndexOf(sub, StringComparison.OrdinalIgnoreCase);
            if (index > 0)
            {
                text = text[..index].Trim();
            }
        }

        // Take only the first non-empty line
        var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length > 0)
        {
            text = lines[0];
        }

        // Strip prefixes if LLM prefixed "Cleaned Text:" or "Output:"
        if (text.StartsWith("Cleaned text:", StringComparison.OrdinalIgnoreCase))
            text = text["Cleaned text:".Length..].Trim();
        else if (text.StartsWith("Output:", StringComparison.OrdinalIgnoreCase))
            text = text["Output:".Length..].Trim();
        else if (text.StartsWith("Result:", StringComparison.OrdinalIgnoreCase))
            text = text["Result:".Length..].Trim();

        // Strip surrounding quotes
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
