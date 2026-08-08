namespace Vacanam.Audio.Capture;

/// <summary>
/// High-performance, pure C# audio converter and resampler.
/// Converts WASAPI native formats (IEEE 32-bit float / 16-bit PCM, multi-channel, 44.1k/48k/96k Hz)
/// into 16 kHz 16-bit mono PCM required by Whisper.
///
/// Pure managed implementation — zero native COM or MediaFoundation dependencies.
/// </summary>
internal static class AudioConverter
{
    /// <summary>
    /// Converts a raw WASAPI buffer to 16 kHz 16-bit mono PCM bytes.
    /// </summary>
    public static byte[] To16kHzMonoPcm16(byte[] inputBuffer, int bytesRecorded, NAudio.Wave.WaveFormat inputFormat)
    {
        if (bytesRecorded <= 0) return Array.Empty<byte>();

        int channels = Math.Max(1, inputFormat.Channels);
        int inSampleRate = inputFormat.SampleRate;
        bool isFloat = inputFormat.Encoding == NAudio.Wave.WaveFormatEncoding.IeeeFloat || inputFormat.BitsPerSample == 32;

        int bytesPerSample = isFloat ? 4 : 2;
        int totalNativeSamples = bytesRecorded / bytesPerSample;
        int frameCount = totalNativeSamples / channels;
        if (frameCount <= 0) return Array.Empty<byte>();

        // Step 1: Extract float samples [-1.0, 1.0] downmixed to mono
        var monoFloats = new float[frameCount];

        if (isFloat)
        {
            for (int i = 0; i < frameCount; i++)
            {
                float sum = 0;
                for (int ch = 0; ch < channels; ch++)
                {
                    int sampleIdx = (i * channels) + ch;
                    if ((sampleIdx + 1) * 4 <= bytesRecorded)
                        sum += BitConverter.ToSingle(inputBuffer, sampleIdx * 4);
                }
                monoFloats[i] = sum / channels;
            }
        }
        else // 16-bit PCM
        {
            for (int i = 0; i < frameCount; i++)
            {
                float sum = 0;
                for (int ch = 0; ch < channels; ch++)
                {
                    int sampleIdx = (i * channels) + ch;
                    if ((sampleIdx + 1) * 2 <= bytesRecorded)
                        sum += BitConverter.ToInt16(inputBuffer, sampleIdx * 2) / 32768f;
                }
                monoFloats[i] = sum / channels;
            }
        }

        // Step 2: Resample from inSampleRate to 16000 Hz using linear interpolation
        const int targetSampleRate = 16000;
        if (inSampleRate == targetSampleRate)
        {
            return FloatToPcm16Bytes(monoFloats);
        }

        double ratio = (double)inSampleRate / targetSampleRate;
        int outputSampleCount = (int)Math.Floor(frameCount / ratio);
        if (outputSampleCount <= 0) return Array.Empty<byte>();

        var resampledFloats = new float[outputSampleCount];
        for (int i = 0; i < outputSampleCount; i++)
        {
            double srcIdx = i * ratio;
            int idx1 = (int)srcIdx;
            int idx2 = Math.Min(idx1 + 1, frameCount - 1);
            double frac = srcIdx - idx1;

            resampledFloats[i] = (float)((1.0 - frac) * monoFloats[idx1] + frac * monoFloats[idx2]);
        }

        return FloatToPcm16Bytes(resampledFloats);
    }

    private static byte[] FloatToPcm16Bytes(float[] samples)
    {
        var result = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            float s = Math.Clamp(samples[i], -1.0f, 1.0f);
            short pcm16 = (short)(s * 32767f);
            result[i * 2] = (byte)(pcm16 & 0xFF);
            result[i * 2 + 1] = (byte)((pcm16 >> 8) & 0xFF);
        }
        return result;
    }
}
