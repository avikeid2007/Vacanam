using System.Media;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vacanam.Core.Interfaces;
using Vacanam.Core.Models;

namespace Vacanam.Audio.Feedback;

/// <summary>
/// Plays low-latency, programmatically synthesized micro-chimes (earcons)
/// for recording lifecycle events without needing external audio files.
/// </summary>
public sealed class AudioFeedbackService : IAudioFeedbackService, IDisposable
{
    private const int SampleRate = 44100;
    private readonly IOptions<AppSettings> _settings;
    private readonly ILogger<AudioFeedbackService> _logger;

    private readonly Dictionary<AudioCue, MemoryStream> _cueStreams = new();
    private readonly Dictionary<AudioCue, SoundPlayer> _players = new();
    private bool _disposed;

    public AudioFeedbackService(
        IOptions<AppSettings> settings,
        ILogger<AudioFeedbackService> logger)
    {
        _settings = settings;
        _logger = logger;

        InitializeCues();
    }

    private void InitializeCues()
    {
        foreach (AudioCue cue in Enum.GetValues<AudioCue>())
        {
            try
            {
                byte[] wavData = SynthesizeCue(cue);
                var stream = new MemoryStream(wavData);
                var player = new SoundPlayer(stream);
                player.Load(); // Preload into memory

                _cueStreams[cue] = stream;
                _players[cue] = player;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize sound effect for cue {Cue}", cue);
            }
        }
    }

    public void Play(AudioCue cue)
    {
        if (!_settings.Value.Audio.EnableSoundEffects)
            return;

        try
        {
            if (_players.TryGetValue(cue, out var player) && _cueStreams.TryGetValue(cue, out var stream))
            {
                lock (stream)
                {
                    stream.Position = 0;
                    player.Play();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to play audio cue {Cue}", cue);
        }
    }

    /// <summary>
    /// Synthesizes pure 16-bit 44.1kHz mono PCM WAV bytes for a given cue.
    /// Pure function suitable for deterministic testing.
    /// </summary>
    public static byte[] SynthesizeCue(AudioCue cue)
    {
        short[] samples = cue switch
        {
            AudioCue.Start => GenerateChirp(
                startFreq: 480,
                endFreq: 720,
                durationSeconds: 0.070,
                maxAmplitude: 0.25,
                attackSeconds: 0.015,
                decaySeconds: 0.025),

            AudioCue.Stop => GenerateChirp(
                startFreq: 720,
                endFreq: 480,
                durationSeconds: 0.055,
                maxAmplitude: 0.22,
                attackSeconds: 0.010,
                decaySeconds: 0.025),

            AudioCue.Success => GenerateChord(
                freq1: 587.33, // D5
                freq2: 880.00, // A5
                durationSeconds: 0.115,
                maxAmplitude: 0.24,
                decayTau: 0.035),

            AudioCue.Error => GenerateTone(
                frequency: 220, // A3 low thud
                durationSeconds: 0.080,
                maxAmplitude: 0.20,
                decayTau: 0.025),

            _ => []
        };

        return BuildWav(samples, SampleRate);
    }

    private static short[] GenerateChirp(
        double startFreq,
        double endFreq,
        double durationSeconds,
        double maxAmplitude,
        double attackSeconds,
        double decaySeconds)
    {
        int numSamples = (int)(SampleRate * durationSeconds);
        short[] buffer = new short[numSamples];

        double k = (endFreq - startFreq) / durationSeconds;

        for (int i = 0; i < numSamples; i++)
        {
            double t = (double)i / SampleRate;

            // Frequency sweep phase: theta(t) = 2 * pi * (startFreq * t + 0.5 * k * t^2)
            double phase = 2.0 * Math.PI * (startFreq * t + 0.5 * k * t * t);
            double sample = Math.Sin(phase);

            // Envelope: smooth raised-cosine attack and decay
            double env = 1.0;
            if (t < attackSeconds)
            {
                env = 0.5 * (1.0 - Math.Cos(Math.PI * t / attackSeconds));
            }
            else if (t > durationSeconds - decaySeconds)
            {
                double decayElapsed = t - (durationSeconds - decaySeconds);
                env = 0.5 * (1.0 + Math.Cos(Math.PI * decayElapsed / decaySeconds));
            }

            buffer[i] = (short)(sample * env * maxAmplitude * short.MaxValue);
        }

        return buffer;
    }

    private static short[] GenerateChord(
        double freq1,
        double freq2,
        double durationSeconds,
        double maxAmplitude,
        double decayTau)
    {
        int numSamples = (int)(SampleRate * durationSeconds);
        short[] buffer = new short[numSamples];

        for (int i = 0; i < numSamples; i++)
        {
            double t = (double)i / SampleRate;

            // Dual tone harmony
            double wave = 0.6 * Math.Sin(2.0 * Math.PI * freq1 * t) +
                          0.4 * Math.Sin(2.0 * Math.PI * freq2 * t);

            // Fast attack (8ms) + exponential decay
            double attack = Math.Min(1.0, t / 0.008);
            double decay = Math.Exp(-t / decayTau);
            double env = attack * decay;

            buffer[i] = (short)(wave * env * maxAmplitude * short.MaxValue);
        }

        return buffer;
    }

    private static short[] GenerateTone(
        double frequency,
        double durationSeconds,
        double maxAmplitude,
        double decayTau)
    {
        int numSamples = (int)(SampleRate * durationSeconds);
        short[] buffer = new short[numSamples];

        for (int i = 0; i < numSamples; i++)
        {
            double t = (double)i / SampleRate;

            // Fundamental + subtle 2nd harmonic
            double wave = 0.8 * Math.Sin(2.0 * Math.PI * frequency * t) +
                          0.2 * Math.Sin(4.0 * Math.PI * frequency * t);

            double attack = Math.Min(1.0, t / 0.005);
            double decay = Math.Exp(-t / decayTau);
            double env = attack * decay;

            buffer[i] = (short)(wave * env * maxAmplitude * short.MaxValue);
        }

        return buffer;
    }

    private static byte[] BuildWav(short[] pcmSamples, int sampleRate)
    {
        int subChunk2Size = pcmSamples.Length * 2;
        int chunkSize = 36 + subChunk2Size;

        using var ms = new MemoryStream(44 + subChunk2Size);
        using var bw = new BinaryWriter(ms);

        // RIFF header
        bw.Write("RIFF"u8);
        bw.Write(chunkSize);
        bw.Write("WAVE"u8);

        // fmt chunk
        bw.Write("fmt "u8);
        bw.Write(16); // Subchunk1Size for PCM
        bw.Write((short)1); // AudioFormat (1 = PCM)
        bw.Write((short)1); // Channels (1 = Mono)
        bw.Write(sampleRate);
        bw.Write(sampleRate * 2); // ByteRate
        bw.Write((short)2); // BlockAlign
        bw.Write((short)16); // BitsPerSample

        // data chunk
        bw.Write("data"u8);
        bw.Write(subChunk2Size);

        for (int i = 0; i < pcmSamples.Length; i++)
        {
            bw.Write(pcmSamples[i]);
        }

        return ms.ToArray();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var player in _players.Values)
        {
            player.Dispose();
        }
        _players.Clear();

        foreach (var stream in _cueStreams.Values)
        {
            stream.Dispose();
        }
        _cueStreams.Clear();
    }
}
