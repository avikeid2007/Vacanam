namespace Vacanam.Core.Exceptions;

/// <summary>Base exception type for all Vacanam domain errors.</summary>
public class VacanamException : Exception
{
    public VacanamException(string message) : base(message) { }
    public VacanamException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>Thrown when the microphone cannot be opened or accessed.</summary>
public sealed class AudioDeviceException : VacanamException
{
    public AudioDeviceException(string message) : base(message) { }
    public AudioDeviceException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>Thrown when a required model file is missing or corrupted.</summary>
public sealed class ModelNotFoundException : VacanamException
{
    public string ModelPath { get; }
    public ModelNotFoundException(string modelPath)
        : base($"Model not found at: {modelPath}") => ModelPath = modelPath;
}

/// <summary>Thrown when the Whisper or LLM inference fails.</summary>
public sealed class InferenceException : VacanamException
{
    public InferenceException(string message) : base(message) { }
    public InferenceException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>Thrown when text injection into the target application fails.</summary>
public sealed class TextInjectionException : VacanamException
{
    public TextInjectionException(string message) : base(message) { }
    public TextInjectionException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>Thrown when the global hotkey cannot be registered (e.g. combination already in use).</summary>
public sealed class HotkeyRegistrationException : VacanamException
{
    public int Modifiers { get; }
    public int VirtualKey { get; }

    public HotkeyRegistrationException(int modifiers, int virtualKey)
        : base($"Failed to register hotkey (modifiers={modifiers:X2}, vk={virtualKey:X2}). The combination may already be in use by another application.")
    {
        Modifiers = modifiers;
        VirtualKey = virtualKey;
    }
}
