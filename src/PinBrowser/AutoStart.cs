using Microsoft.Win32;

namespace PinBrowser;

internal static class AutoStart
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>Fixed value name used before per-instance ids existed. Cleaned up on every run.</summary>
    private const string LegacySharedValueName = "PinBrowser";

    private const string ValueNamePrefix = "PinBrowser_";

    public static void Apply(string instanceId, bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (key is null)
        {
            return;
        }

        // Older builds all shared one fixed value name, so multiple installs kept overwriting
        // each other's autostart entry. Remove it so only the per-instance entries below remain.
        key.DeleteValue(LegacySharedValueName, throwOnMissingValue: false);

        var valueName = ValueNamePrefix + instanceId;

        if (!enabled)
        {
            key.DeleteValue(valueName, throwOnMissingValue: false);
            return;
        }

        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
        {
            return;
        }

        key.SetValue(valueName, $"\"{exePath}\"");
    }
}
