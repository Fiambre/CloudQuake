using System.IO;

namespace CloudQuake.Services;

/// <summary>Minimal append-only diagnostic log used while troubleshooting hotkey/toggle behavior.</summary>
public static class Logger
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CloudQuake", "diagnostic.log");

    private static readonly object Lock = new();

    public static void Log(string message)
    {
        try
        {
            lock (Lock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
                File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Diagnostics must never crash the app.
        }
    }
}
