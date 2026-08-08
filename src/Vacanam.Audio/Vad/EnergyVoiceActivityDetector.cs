using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vacanam.Core.Interfaces;
using Vacanam.Core.Models;

namespace Vacanam.Audio.Vad;

/// <summary>
/// Energy-threshold Voice Activity Detector.
///
/// Algorithm:
///   1. Compute RMS of the audio frame
///   2. Compare against a configurable threshold
///   3. Apply hysteresis (hold-on / hold-off counters) to avoid
///      choppy transitions from brief silences (e.g. breath gaps)
///
/// This is a lightweight, dependency-free implementation.
/// Can be replaced by a neural VAD (e.g. Silero) in a future phase.
/// </summary>
public sealed class EnergyVoiceActivityDetector : IVoiceActivityDetector
{
    private readonly double _threshold;
    private readonly ILogger<EnergyVoiceActivityDetector> _logger;

    // Hysteresis: keep VAD "on" for N consecutive silent frames after speech
    private const int HoldOffFrames = 8; // ~160ms at 20ms frames
    private int _holdOffCounter = 0;
    private bool _speechActive = false;

    public EnergyVoiceActivityDetector(
        IOptions<AppSettings> settings,
        ILogger<EnergyVoiceActivityDetector> logger)
    {
        _threshold = settings.Value.Audio.VadThreshold;
        _logger = logger;
        _logger.LogDebug("EnergyVAD initialised with threshold={Threshold}", _threshold);
    }

    /// <inheritdoc/>
    public bool IsSpeech(ReadOnlySpan<float> audio)
    {
        if (audio.IsEmpty) return false;

        double rms = ComputeRms(audio);
        bool energyAboveThreshold = rms >= _threshold;

        if (energyAboveThreshold)
        {
            _holdOffCounter = HoldOffFrames;
            if (!_speechActive)
            {
                _speechActive = true;
                _logger.LogTrace("VAD: speech onset detected (rms={Rms:F4})", rms);
            }
        }
        else if (_holdOffCounter > 0)
        {
            // In hold-off: still report speech for a few more frames
            _holdOffCounter--;
        }
        else
        {
            if (_speechActive)
            {
                _speechActive = false;
                _logger.LogTrace("VAD: silence detected (rms={Rms:F4})", rms);
            }
        }

        return _speechActive;
    }

    /// <summary>
    /// Converts a byte buffer of 16-bit PCM to float samples and checks VAD.
    /// Convenience overload for use directly from IAudioRecorder.DataAvailable.
    /// </summary>
    public bool IsSpeechFromPcm16(byte[] buffer, int bytesRecorded)
    {
        if (bytesRecorded <= 1) return false;

        int sampleCount = bytesRecorded / 2;
        var floats = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
            floats[i] = BitConverter.ToInt16(buffer, i * 2) / 32768f;

        return IsSpeech(floats);
    }

    /// <summary>Resets hysteresis state — call at the start of each recording session.</summary>
    public void Reset()
    {
        _holdOffCounter = 0;
        _speechActive = false;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static double ComputeRms(ReadOnlySpan<float> samples)
    {
        double sumSq = 0;
        foreach (float s in samples)
            sumSq += s * s;
        return Math.Sqrt(sumSq / samples.Length);
    }
}
