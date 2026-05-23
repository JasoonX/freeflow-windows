using System.Diagnostics;

namespace FreeFlowWindows;

internal sealed class WindowsAppContext
{
    public string BundleId { get; init; } = "windows.unknown";
    public string AppName { get; init; } = "Unknown";
    public string WindowTitle { get; init; } = "";
}

internal static class WindowsAppContextReader
{
    public static WindowsAppContext Read()
    {
        var handle = NativeMethods.GetForegroundWindow();
        if (handle == IntPtr.Zero)
        {
            return new WindowsAppContext();
        }

        var titleBuffer = new char[NativeMethods.MaxWindowTitleLength];
        var titleLength = NativeMethods.GetWindowText(
            handle,
            titleBuffer,
            titleBuffer.Length);
        var title = titleLength > 0
            ? new string(titleBuffer, 0, titleLength)
            : "";

        NativeMethods.GetWindowThreadProcessId(handle, out var processId);
        try
        {
            using var process = Process.GetProcessById((int)processId);
            var appName = string.IsNullOrWhiteSpace(process.MainWindowTitle)
                ? process.ProcessName
                : process.ProcessName;
            return new WindowsAppContext
            {
                BundleId = process.ProcessName,
                AppName = appName,
                WindowTitle = title
            };
        }
        catch
        {
            return new WindowsAppContext
            {
                WindowTitle = title
            };
        }
    }
}
