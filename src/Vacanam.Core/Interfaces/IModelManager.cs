namespace Vacanam.Core.Interfaces;

/// <summary>
/// Manages the lifecycle of local AI model files (Whisper + LLM).
/// Handles storage path resolution, download progress, and validation.
/// </summary>
public interface IModelManager
{
    /// <summary>Base directory where all models are stored.</summary>
    string ModelsRootPath { get; }

    /// <summary>Returns the full path to a Whisper model file by size name.</summary>
    string GetWhisperModelPath(string modelSize);

    /// <summary>Returns the full path to an LLM model file.</summary>
    string GetLlmModelPath(string modelFileName);

    /// <summary>Returns true if the specified Whisper model exists on disk.</summary>
    bool WhisperModelExists(string modelSize);

    /// <summary>Returns true if the specified LLM model file exists on disk.</summary>
    bool LlmModelExists(string modelFileName);
}
