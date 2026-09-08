using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vacanam.Audio.Feedback;
using Vacanam.Core.Interfaces;
using Vacanam.Core.Models;
using Xunit;

namespace Vacanam.Tests;

public class AudioFeedbackServiceTests
{
    [Theory]
    [InlineData(AudioCue.Start)]
    [InlineData(AudioCue.Stop)]
    [InlineData(AudioCue.Success)]
    [InlineData(AudioCue.Error)]
    public void SynthesizeCue_ProducesValidWavHeaderAndData(AudioCue cue)
    {
        byte[] wav = AudioFeedbackService.SynthesizeCue(cue);

        // WAV header must be at least 44 bytes
        Assert.NotNull(wav);
        Assert.True(wav.Length > 44, "WAV byte array should contain header plus PCM audio data");

        // RIFF chunk descriptor
        string riffHeader = Encoding.ASCII.GetString(wav, 0, 4);
        Assert.Equal("RIFF", riffHeader);

        string waveFormat = Encoding.ASCII.GetString(wav, 8, 4);
        Assert.Equal("WAVE", waveFormat);

        // "fmt " subchunk
        string fmtHeader = Encoding.ASCII.GetString(wav, 12, 4);
        Assert.Equal("fmt ", fmtHeader);

        int subchunk1Size = BitConverter.ToInt32(wav, 16);
        Assert.Equal(16, subchunk1Size); // 16 for PCM

        short audioFormat = BitConverter.ToInt16(wav, 20);
        Assert.Equal(1, audioFormat); // 1 = PCM

        short numChannels = BitConverter.ToInt16(wav, 22);
        Assert.Equal(1, numChannels); // Mono

        int sampleRate = BitConverter.ToInt32(wav, 24);
        Assert.Equal(44100, sampleRate); // 44.1 kHz

        short bitsPerSample = BitConverter.ToInt16(wav, 34);
        Assert.Equal(16, bitsPerSample); // 16-bit

        // "data" subchunk
        string dataHeader = Encoding.ASCII.GetString(wav, 36, 4);
        Assert.Equal("data", dataHeader);

        int dataSize = BitConverter.ToInt32(wav, 40);
        Assert.Equal(wav.Length - 44, dataSize);
    }

    [Fact]
    public void Play_WhenDisabledInSettings_DoesNotThrow()
    {
        var settings = Options.Create(new AppSettings
        {
            Audio = new AudioSettings { EnableSoundEffects = false }
        });

        using var service = new AudioFeedbackService(settings, NullLogger<AudioFeedbackService>.Instance);

        // Should safely no-op without exceptions
        var ex = Record.Exception(() =>
        {
            service.Play(AudioCue.Start);
            service.Play(AudioCue.Stop);
            service.Play(AudioCue.Success);
            service.Play(AudioCue.Error);
        });

        Assert.Null(ex);
    }
}
