using Microsoft.Win32;

namespace FreeFlowWindows;

internal static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "FreeFlow for Windows";
    private const string LegacyAppName = "FreeFlow";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(AppName) is string value
            && value.Contains(Application.ExecutablePath, StringComparison.OrdinalIgnoreCase);
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (key == null)
        {
            return;
        }

        key.DeleteValue(LegacyAppName, throwOnMissingValue: false);

        if (enabled)
        {
            var executablePath = Application.ExecutablePath;
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                key.SetValue(AppName, $"\"{executablePath}\"");
            }
        }
        else
        {
            key.DeleteValue(AppName, throwOnMissingValue: false);
        }
    }
}
