using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using Vacanam.Core.Interfaces;
using Vacanam.Core.Models;
using Vacanam.Windows.Interop;

namespace Vacanam.Windows.ForegroundWindow;

/// <summary>
/// Production implementation of IForegroundWindowService using Win32 APIs.
/// Captures a snapshot of the active window without stealing focus.
/// All Win32 calls are isolated to Win32Interop — no P/Invoke elsewhere.
/// </summary>
public sealed class WindowsForegroundWindowService(ILogger<WindowsForegroundWindowService> logger)
    : IForegroundWindowService
{
    private const int MaxTitleLength = 256;

    public ApplicationContext GetCurrentContext()
    {
        try
        {
            IntPtr hwnd = Win32Interop.GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
            {
                logger.LogDebug("GetForegroundWindow returned zero — no foreground window.");
                return ApplicationContext.Unknown;
            }

            // Get process ID
            Win32Interop.GetWindowThreadProcessId(hwnd, out uint processId);
            if (processId == 0)
            {
                logger.LogDebug("Could not get process ID for HWND={Hwnd:X}.", hwnd);
                return ApplicationContext.Unknown;
            }

            // Get process name
            string processName = GetProcessName((int)processId);

            // Get window title
            string windowTitle = GetWindowTitle(hwnd);

            var context = new ApplicationContext(
                WindowHandle: hwnd,
                ProcessId: (int)processId,
                ProcessName: processName,
                WindowTitle: windowTitle);

            logger.LogDebug(
                "Foreground window: Process={Process}, Title={Title}, HWND={Hwnd:X}",
                processName, windowTitle, hwnd);

            return context;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to capture foreground window context.");
            return ApplicationContext.Unknown;
        }
    }

    // -- Helpers ---------------------------------------------------------------

    private static string GetProcessName(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return process.ProcessName;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string GetWindowTitle(IntPtr hwnd)
    {
        int length = Win32Interop.GetWindowTextLength(hwnd);
        if (length <= 0) return string.Empty;

        var sb = new StringBuilder(Math.Min(length + 1, MaxTitleLength));
        Win32Interop.GetWindowText(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }
}
