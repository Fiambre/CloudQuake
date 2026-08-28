using System.IO;
using System.Text.Json;

namespace CloudQuake.Models;

public class AppSettings
{
    public string Url { get; set; } = "https://cloud.fiambre.dev";

    // Hotkey configuration (Win32 MOD_* flags stored as booleans for clarity)
    public bool ModWin { get; set; }
    public bool ModCtrl { get; set; } = true;
    public bool ModAlt { get; set; } = true;
    public bool ModShift { get; set; } = true;
    public string Key { get; set; } = "Q";

    // Layout: fraction of the primary screen the console occupies when shown
    public double WidthPercent { get; set; } = 1.0;
    public double HeightPercent { get; set; } = 0.5;

    public bool StartWithWindows { get; set; } = true;

    private static string SettingsDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CloudQuake");

    private static string SettingsPath => Path.Combine(SettingsDir, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded is not null)
                {
                    return loaded;
                }
            }
        }
        catch
        {
            // Corrupt or unreadable settings: fall back to defaults.
        }

        return new AppSettings();
    }

    public void Save()
    {
        Directory.CreateDirectory(SettingsDir);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }

    public static string WebViewUserDataFolder =>
        Path.Combine(SettingsDir, "WebView2Data");
}
