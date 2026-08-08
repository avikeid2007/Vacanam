using NAudio.Wave;
using System.Collections.Concurrent;
using System.Threading.Channels;
using Vacanam.Core.Interfaces;

namespace Vacanam.Audio.Capture;

/// <summary>
/// Accumulates PCM audio chunks received from IAudioRecorder.DataAvailable
/// for a single recording session and exposes the complete audio as a WAV stream.
/// Supports real-time streaming to a ChannelWriter for low-latency background transcription.
/// </summary>
public sealed class RecordingBuffer : IDisposable
{
    private static readonly WaveFormat WavFormat = new(sampleRate: 16_000, channels: 1);

    private readonly ConcurrentQueue<byte[]> _chunks = new();
    private readonly IAudioRecorder _recorder;
    private ChannelWriter<byte[]>? _channelWriter;
    private bool _capturing;
    private bool _disposed;

    public RecordingBuffer(IAudioRecorder recorder)
    {
        _recorder = recorder;
    }

    /// <summary>Total bytes of PCM data accumulated so far.</summary>
    public int TotalBytes { get; private set; }

    /// <summary>Duration of captured audio.</summary>
    public TimeSpan Duration =>
        TimeSpan.FromSeconds(TotalBytes / (double)WavFormat.AverageBytesPerSecond);

    public void BeginCapture(ChannelWriter<byte[]>? channelWriter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _capturing = true;
        _channelWriter = channelWriter;
        TotalBytes = 0;
        while (_chunks.TryDequeue(out _)) { }
        _recorder.DataAvailable += OnDataAvailable;
    }

    public void EndCapture()
    {
        _capturing = false;
        _recorder.DataAvailable -= OnDataAvailable;
        _channelWriter?.TryComplete();
        _channelWriter = null;
    }

    public MemoryStream ToWavStream(bool trimSilence = true)
    {
        byte[] pcmData = ToRawPcm();

        if (trimSilence && pcmData.Length > 3200)
        {
            pcmData = TrimPcmSilence(pcmData);
        }

        var ms = new MemoryStream();
        using (var writer = new WaveFileWriter(new IgnoreDisposeStream(ms), WavFormat))
        {
            writer.Write(pcmData, 0, pcmData.Length);
            writer.Flush();
        }

        ms.Position = 0;
        return ms;
    }

    public byte[] ToRawPcm()
    {
        var all = new byte[TotalBytes];
        int offset = 0;
        foreach (var chunk in _chunks)
        {
            Buffer.BlockCopy(chunk, 0, all, offset, chunk.Length);
            offset += chunk.Length;
        }
        return all;
    }

    private static byte[] TrimPcmSilence(byte[] pcm, double threshold = 0.015)
    {
        int sampleCount = pcm.Length / 2;
        if (sampleCount < 1600) return pcm;

        const int frameSize = 320; // 20ms frames at 16kHz
        int startSample = 0;
        int endSample = sampleCount;

        for (int i = 0; i < sampleCount - frameSize; i += frameSize)
        {
            if (ComputeFrameRms(pcm, i, frameSize) >= threshold)
            {
                startSample = Math.Max(0, i - (frameSize * 2));
                break;
            }
        }

        for (int i = sampleCount - frameSize; i >= frameSize; i -= frameSize)
        {
            if (ComputeFrameRms(pcm, i, frameSize) >= threshold)
            {
                endSample = Math.Min(sampleCount, i + (frameSize * 3));
                break;
            }
        }

        if (startSample >= endSample || (endSample - startSample) < 1600)
            return pcm;

        int trimmedBytes = (endSample - startSample) * 2;
        var trimmed = new byte[trimmedBytes];
        Buffer.BlockCopy(pcm, startSample * 2, trimmed, 0, trimmedBytes);
        return trimmed;
    }

    private static double ComputeFrameRms(byte[] pcm, int startSample, int count)
    {
        double sumSq = 0;
        for (int i = 0; i < count; i++)
        {
            int idx = (startSample + i) * 2;
            if (idx + 1 < pcm.Length)
            {
                short sample = BitConverter.ToInt16(pcm, idx);
                double norm = sample / 32768.0;
                sumSq += norm * norm;
            }
        }
        return Math.Sqrt(sumSq / count);
    }

    private void OnDataAvailable(object? sender, AudioDataEventArgs e)
    {
        if (!_capturing || e.BytesRecorded <= 0) return;
        var copy = new byte[e.BytesRecorded];
        Buffer.BlockCopy(e.Buffer, 0, copy, 0, e.BytesRecorded);
        _chunks.Enqueue(copy);
        TotalBytes += e.BytesRecorded;

        _channelWriter?.TryWrite(copy);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        EndCapture();
    }
}

public sealed class IgnoreDisposeStream : Stream
{
    private readonly Stream _innerStream;
    public IgnoreDisposeStream(Stream innerStream) => _innerStream = innerStream;

    public override bool CanRead => _innerStream.CanRead;
    public override bool CanSeek => _innerStream.CanSeek;
    public override bool CanWrite => _innerStream.CanWrite;
    public override long Length => _innerStream.Length;
    public override long Position { get => _innerStream.Position; set => _innerStream.Position = value; }

    public override void Flush() => _innerStream.Flush();
    public override int Read(byte[] buffer, int offset, int count) => _innerStream.Read(buffer, offset, count);
    public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);
    public override void SetLength(long value) => _innerStream.SetLength(value);
    public override void Write(byte[] buffer, int offset, int count) => _innerStream.Write(buffer, offset, count);

    protected override void Dispose(bool disposing) { }
}

