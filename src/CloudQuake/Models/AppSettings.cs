using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CloudQuake.Models;

/// <summary>Which display the console drops down on.</summary>
public enum MonitorMode
{
    /// <summary>The monitor the mouse cursor is currently on (default).</summary>
    FollowCursor,

    /// <summary>Always the Windows primary monitor.</summary>
    Primary,

    /// <summary>A fixed monitor identified by <see cref="AppSettings.MonitorDeviceName"/>.</summary>
    Specific,
}

public class AppSettings
{
    /// <summary>
    /// Base URL of the CloudCLI instance to embed. Empty until the user completes
    /// first-run setup, which is what <see cref="NeedsSetup"/> keys off.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    // Hotkey configuration (Win32 MOD_* flags stored as booleans for clarity)
    public bool ModWin { get; set; }
    public bool ModCtrl { get; set; } = true;
    public bool ModAlt { get; set; } = true;
    public bool ModShift { get; set; } = true;
    public string Key { get; set; } = "Q";

    // Layout: fraction of the target monitor's work area the console occupies when shown
    public double WidthPercent { get; set; } = 1.0;
    public double HeightPercent { get; set; } = 0.5;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public MonitorMode MonitorMode { get; set; } = MonitorMode.FollowCursor;

    /// <summary>
    /// Win32 device name (e.g. <c>\\.\DISPLAY2</c>) used when <see cref="MonitorMode"/>
    /// is <see cref="MonitorMode.Specific"/>. Falls back to the primary monitor when the
    /// display is no longer connected.
    /// </summary>
    public string MonitorDeviceName { get; set; } = string.Empty;

    /// <summary>Hide the console automatically when it loses focus (click outside, Alt+Tab).</summary>
    public bool HideOnFocusLoss { get; set; } = true;

    /// <summary>
    /// Forward notifications raised by the embedded page to Windows toasts, and
    /// auto-grant the page's notification permission.
    /// </summary>
    public bool EnableNotifications { get; set; } = true;

    public bool StartWithWindows { get; set; } = true;

    /// <summary>Opt-in troubleshooting log written next to the settings file.</summary>
    public bool EnableDiagnosticLogging { get; set; }

    [JsonIgnore]
    public bool NeedsSetup => string.IsNullOrWhiteSpace(Url);

    private static string SettingsDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CloudQuake");

    private static string SettingsPath => Path.Combine(SettingsDir, "settings.json");

    public static string LogPath => Path.Combine(SettingsDir, "cloudquake.log");

    public static string WebViewUserDataFolder => Path.Combine(SettingsDir, "WebView2Data");

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

    /// <summary>Copies every user-facing value onto <paramref name="target"/>.</summary>
    /// <remarks>
    /// The app shares one settings instance across windows, so edits are applied in place
    /// rather than by swapping references.
    /// </remarks>
    public void CopyTo(AppSettings target)
    {
        target.Url = Url;
        target.ModWin = ModWin;
        target.ModCtrl = ModCtrl;
        target.ModAlt = ModAlt;
        target.ModShift = ModShift;
        target.Key = Key;
        target.WidthPercent = WidthPercent;
        target.HeightPercent = HeightPercent;
        target.MonitorMode = MonitorMode;
        target.MonitorDeviceName = MonitorDeviceName;
        target.HideOnFocusLoss = HideOnFocusLoss;
        target.EnableNotifications = EnableNotifications;
        target.StartWithWindows = StartWithWindows;
        target.EnableDiagnosticLogging = EnableDiagnosticLogging;
    }

    public AppSettings Clone()
    {
        var copy = new AppSettings();
        CopyTo(copy);
        return copy;
    }
}
