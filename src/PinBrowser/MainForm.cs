using Microsoft.Web.WebView2.WinForms;

namespace PinBrowser;

public sealed class MainForm : Form
{
    private readonly Settings _settings;
    private readonly WebView2 _webView;

    public MainForm()
    {
        _settings = Settings.Load();
        AutoStart.Apply(_settings.AutoStart);

        Text = "PinBrowser";
        StartPosition = FormStartPosition.Manual;
        Bounds = EnsureOnScreen(new Rectangle(
            _settings.WindowX, _settings.WindowY, _settings.WindowWidth, _settings.WindowHeight));

        _webView = new WebView2 { Dock = DockStyle.Fill };
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

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        try
        {
            await _webView.EnsureCoreWebView2Async();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                $"Die WebView2-Runtime konnte nicht initialisiert werden:\n{ex.Message}\n\n" +
                "Bitte installiere die \"Microsoft Edge WebView2 Runtime\" (auf aktuellem Windows normalerweise vorinstalliert).",
                "PinBrowser", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

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
