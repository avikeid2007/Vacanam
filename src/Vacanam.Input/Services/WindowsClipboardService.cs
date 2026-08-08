using System.Windows;
using Microsoft.Extensions.Logging;
using Vacanam.Core.Interfaces;

namespace Vacanam.Input.Services;

/// <summary>
/// Production implementation of IClipboardService.
/// Handles WPF STA thread requirements and transient clipboard lock retries safely.
/// </summary>
public sealed class WindowsClipboardService : IClipboardService
{
    private readonly ILogger<WindowsClipboardService> _logger;
    private const int MaxRetries = 5;
    private const int RetryDelayMs = 25;

    public WindowsClipboardService(ILogger<WindowsClipboardService> logger)
    {
        _logger = logger;
    }

    public Task<string?> GetTextAsync()
    {
        return RunInStaAsync(() =>
        {
            for (int i = 0; i < MaxRetries; i++)
            {
                try
                {
                    if (Clipboard.ContainsText())
                        return Clipboard.GetText();
                    return null;
                }
                catch (System.Runtime.InteropServices.COMException ex) when (i < MaxRetries - 1)
                {
                    _logger.LogDebug(ex, "Clipboard locked during GetText. Retry {Attempt}/{Max}", i + 1, MaxRetries);
                    Thread.Sleep(RetryDelayMs);
                }
            }
            return null;
        });
    }

    public Task SetTextAsync(string text)
    {
        if (text is null) return Task.CompletedTask;

        return RunInStaAsync(() =>
        {
            for (int i = 0; i < MaxRetries; i++)
            {
                try
                {
                    Clipboard.SetText(text, TextDataFormat.UnicodeText);
                    return;
                }
                catch (System.Runtime.InteropServices.COMException ex) when (i < MaxRetries - 1)
                {
                    _logger.LogDebug(ex, "Clipboard locked during SetText. Retry {Attempt}/{Max}", i + 1, MaxRetries);
                    Thread.Sleep(RetryDelayMs);
                }
            }
        });
    }

    public Task<object?> BackupAsync()
    {
        return RunInStaAsync(() =>
        {
            for (int i = 0; i < MaxRetries; i++)
            {
                try
                {
                    IDataObject? data = Clipboard.GetDataObject();
                    if (data is null) return (object?)null;

                    // Create a snapshot copy of the data object so original formatting is preserved
                    var dataCopy = new DataObject();
                    foreach (string format in data.GetFormats())
                    {
                        try
                        {
                            object? rawData = data.GetData(format);
                            if (rawData is not null)
                                dataCopy.SetData(format, rawData);
                        }
                        catch { /* format data retrieval best effort */ }
                    }
                    return (object?)dataCopy;
                }
                catch (System.Runtime.InteropServices.COMException ex) when (i < MaxRetries - 1)
                {
                    _logger.LogDebug(ex, "Clipboard locked during Backup. Retry {Attempt}/{Max}", i + 1, MaxRetries);
                    Thread.Sleep(RetryDelayMs);
                }
            }
            return null;
        });
    }

    public Task RestoreAsync(object? backup)
    {
        if (backup is not IDataObject dataObject) return Task.CompletedTask;

        return RunInStaAsync(() =>
        {
            for (int i = 0; i < MaxRetries; i++)
            {
                try
                {
                    Clipboard.SetDataObject(dataObject, true);
                    return;
                }
                catch (System.Runtime.InteropServices.COMException ex) when (i < MaxRetries - 1)
                {
                    _logger.LogDebug(ex, "Clipboard locked during Restore. Retry {Attempt}/{Max}", i + 1, MaxRetries);
                    Thread.Sleep(RetryDelayMs);
                }
            }
        });
    }

    // -- Helper ----------------------------------------------------------------

    private static Task<T> RunInStaAsync<T>(Func<T> action)
    {
        var tcs = new TaskCompletionSource<T>();
        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
        {
            try { tcs.SetResult(action()); }
            catch (Exception ex) { tcs.SetException(ex); }
            return tcs.Task;
        }

        var thread = new Thread(() =>
        {
            try { tcs.SetResult(action()); }
            catch (Exception ex) { tcs.SetException(ex); }
        })
        {
            IsBackground = true
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return tcs.Task;
    }

    private static Task RunInStaAsync(Action action)
    {
        return RunInStaAsync(() =>
        {
            action();
            return true;
        });
    }
}


