using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace PinBrowser;

internal static class ThemeHelper
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int attributeValue, int attributeSize);

    public static bool IsSystemDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch
        {
            return false;
        }
    }

    public static void ApplyTitleBarTheme(IntPtr windowHandle, bool useDarkMode)
    {
        var value = useDarkMode ? 1 : 0;
        if (DwmSetWindowAttribute(windowHandle, DwmwaUseImmersiveDarkMode, ref value, sizeof(int)) != 0)
        {
            DwmSetWindowAttribute(windowHandle, DwmwaUseImmersiveDarkModeBefore20H1, ref value, sizeof(int));
        }
    }
}
