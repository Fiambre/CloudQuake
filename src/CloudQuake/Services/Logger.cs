using System.IO;
using CloudQuake.Models;

namespace CloudQuake.Services;

/// <summary>
/// Opt-in troubleshooting log. Disabled unless the user enables it in Settings, and
/// capped so an enabled log can never grow without bound.
/// </summary>
public static class Logger
{
    private const long MaxBytes = 1024 * 1024; // 1 MB, then rolled to .log.old

    private static readonly object Gate = new();

    /// <summary>Set from the loaded settings at startup and whenever settings are saved.</summary>
    public static bool IsEnabled { get; set; }

    public static void Log(string message)
    {
        if (!IsEnabled)
        {
            return;
        }

        try
        {
            lock (Gate)
            {
                var path = AppSettings.LogPath;
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                RollIfTooLarge(path);
                File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Diagnostics must never take the app down.
        }
    }

    private static void RollIfTooLarge(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists || info.Length < MaxBytes)
        {
            return;
        }

        var rolled = path + ".old";
        File.Delete(rolled);
        File.Move(path, rolled);
    }
}
