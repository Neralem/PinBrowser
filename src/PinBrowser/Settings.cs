using System.Text.Json;

namespace PinBrowser;

public sealed class Settings
{
    public string Url { get; set; } = "https://www.google.com/";
    public int WindowX { get; set; } = 100;
    public int WindowY { get; set; } = 100;
    public int WindowWidth { get; set; } = 1000;
    public int WindowHeight { get; set; } = 700;
    public bool Maximized { get; set; }
    public bool AutoStart { get; set; } = true;

    /// <summary>
    /// Stable per-installation id, used so that copies of PinBrowser living in different
    /// folders each get their own Windows-autostart entry instead of overwriting each other's.
    /// </summary>
    public string InstanceId { get; set; } = "";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string FilePath => Path.Combine(AppContext.BaseDirectory, "settings.json");

    public static Settings Load()
    {
        var path = FilePath;
        Settings settings;
        var needsSave = false;

        if (!File.Exists(path))
        {
            settings = new Settings();
            needsSave = true;
        }
        else
        {
            try
            {
                var json = File.ReadAllText(path);
                settings = JsonSerializer.Deserialize<Settings>(json, JsonOptions) ?? new Settings();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"settings.json konnte nicht gelesen werden und wird ignoriert (Standardwerte werden verwendet):\n{ex.Message}",
                    "PinBrowser", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                settings = new Settings();
            }
        }

        if (string.IsNullOrEmpty(settings.InstanceId))
        {
            settings.InstanceId = Guid.NewGuid().ToString("N");
            needsSave = true;
        }

        if (needsSave)
        {
            settings.Save();
        }

        return settings;
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(FilePath, json);
    }
}
