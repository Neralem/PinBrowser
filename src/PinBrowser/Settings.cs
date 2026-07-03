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

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string FilePath => Path.Combine(AppContext.BaseDirectory, "settings.json");

    public static Settings Load()
    {
        var path = FilePath;

        if (!File.Exists(path))
        {
            var defaults = new Settings();
            defaults.Save();
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Settings>(json, JsonOptions) ?? new Settings();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"settings.json konnte nicht gelesen werden und wird ignoriert (Standardwerte werden verwendet):\n{ex.Message}",
                "PinBrowser", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return new Settings();
        }
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(FilePath, json);
    }
}
