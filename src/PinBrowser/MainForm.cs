using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace PinBrowser;

public sealed class MainForm : Form
{
    private const int WM_SETTINGCHANGE = 0x001A;

    private readonly Settings _settings;
    private readonly WebView2 _webView;
    private readonly bool _useFixedTitle;
    private readonly bool _useFavicon;
    private Icon? _faviconIcon;

    public MainForm()
    {
        _settings = Settings.Load();
        AutoStart.Apply(_settings.InstanceId, _settings.AutoStart);

        _useFixedTitle = !string.IsNullOrWhiteSpace(_settings.Title);
        Text = _useFixedTitle ? _settings.Title : "PinBrowser";

        var icon = TryLoadIcon(_settings.IconPath);
        _useFavicon = icon is null;
        if (icon is not null)
        {
            Icon = icon;
        }

        StartPosition = FormStartPosition.Manual;
        Bounds = EnsureOnScreen(new Rectangle(
            _settings.WindowX, _settings.WindowY, _settings.WindowWidth, _settings.WindowHeight));

        _webView = new WebView2
        {
            Dock = DockStyle.Fill,
            DefaultBackgroundColor = ThemeHelper.IsSystemDarkMode() ? Color.Black : Color.White,
        };
        Controls.Add(_webView);

        if (Uri.TryCreate(_settings.Url, UriKind.Absolute, out var startUri))
        {
            _webView.Source = startUri;
        }

        if (_settings.Maximized)
        {
            WindowState = FormWindowState.Maximized;
        }

        FormClosing += OnFormClosing;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ThemeHelper.ApplyTitleBarTheme(Handle, ThemeHelper.IsSystemDarkMode());
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_SETTINGCHANGE && m.LParam != IntPtr.Zero &&
            Marshal.PtrToStringUni(m.LParam) == "ImmersiveColorSet")
        {
            ThemeHelper.ApplyTitleBarTheme(Handle, ThemeHelper.IsSystemDarkMode());
        }

        base.WndProc(ref m);
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        try
        {
            await _webView.EnsureCoreWebView2Async();

            if (!_useFixedTitle)
            {
                _webView.CoreWebView2.DocumentTitleChanged += (_, _) =>
                    Text = _webView.CoreWebView2.DocumentTitle;
            }

            if (_useFavicon)
            {
                _webView.CoreWebView2.FaviconChanged += async (_, _) => await UpdateFaviconFromPageAsync();
                await UpdateFaviconFromPageAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                $"Die WebView2-Runtime konnte nicht initialisiert werden:\n{ex.Message}\n\n" +
                "Bitte installiere die \"Microsoft Edge WebView2 Runtime\" (auf aktuellem Windows normalerweise vorinstalliert).",
                "PinBrowser", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task UpdateFaviconFromPageAsync()
    {
        try
        {
            using var stream = await _webView.CoreWebView2.GetFaviconAsync(CoreWebView2FaviconImageFormat.Png);
            if (stream is null || stream.Length == 0)
            {
                return;
            }

            using var bitmap = new Bitmap(stream);
            var newIcon = IconFromBitmap(bitmap);
            Icon = newIcon;
            _faviconIcon?.Dispose();
            _faviconIcon = newIcon;
        }
        catch
        {
            // Keine Favicon-Daten verfügbar (z. B. lokale Datei ohne Favicon) - Standard-Icon bleibt.
        }
    }

    private static Icon? TryLoadIcon(string iconPath)
    {
        if (string.IsNullOrWhiteSpace(iconPath))
        {
            return null;
        }

        var resolvedPath = Path.IsPathRooted(iconPath)
            ? iconPath
            : Path.Combine(AppContext.BaseDirectory, iconPath);

        if (!File.Exists(resolvedPath))
        {
            return null;
        }

        try
        {
            if (string.Equals(Path.GetExtension(resolvedPath), ".ico", StringComparison.OrdinalIgnoreCase))
            {
                return new Icon(resolvedPath);
            }

            using var bitmap = new Bitmap(resolvedPath);
            return IconFromBitmap(bitmap);
        }
        catch
        {
            return null;
        }
    }

    private static Icon IconFromBitmap(Bitmap bitmap)
    {
        var hIcon = bitmap.GetHicon();
        try
        {
            using var handleIcon = Icon.FromHandle(hIcon);
            return (Icon)handleIcon.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        var maximized = WindowState == FormWindowState.Maximized;
        var normalBounds = maximized ? RestoreBounds : Bounds;

        _settings.Maximized = maximized;
        _settings.WindowX = normalBounds.X;
        _settings.WindowY = normalBounds.Y;
        _settings.WindowWidth = normalBounds.Width;
        _settings.WindowHeight = normalBounds.Height;
        _settings.Save();
    }

    private static Rectangle EnsureOnScreen(Rectangle bounds)
    {
        var titleBarProbe = new Rectangle(bounds.X, bounds.Y, Math.Max(bounds.Width, 50), 40);
        foreach (var screen in Screen.AllScreens)
        {
            if (screen.WorkingArea.IntersectsWith(titleBarProbe))
            {
                return bounds;
            }
        }

        var workingArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1024, 768);
        var width = Math.Min(bounds.Width, workingArea.Width);
        var height = Math.Min(bounds.Height, workingArea.Height);
        return new Rectangle(
            workingArea.X + (workingArea.Width - width) / 2,
            workingArea.Y + (workingArea.Height - height) / 2,
            width, height);
    }
}
