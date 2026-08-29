using System.Windows;
using CloudQuake.Models;
using Forms = System.Windows.Forms;

namespace CloudQuake.Services;

/// <summary>One connected display, in the form the settings UI shows it.</summary>
public sealed record MonitorInfo(string DeviceName, string DisplayName, bool IsPrimary);

/// <summary>
/// Resolves which display the console should drop down on and converts that display's
/// physical pixel bounds into the device-independent units WPF positions windows with.
/// </summary>
public static class MonitorService
{
    public static IReadOnlyList<MonitorInfo> GetMonitors()
    {
        var screens = Forms.Screen.AllScreens;
        var result = new List<MonitorInfo>(screens.Length);

        for (var i = 0; i < screens.Length; i++)
        {
            var screen = screens[i];
            var bounds = screen.Bounds;
            var label = $"Monitor {i + 1} — {bounds.Width}×{bounds.Height}"
                + (screen.Primary ? " (primary)" : string.Empty);
            result.Add(new MonitorInfo(screen.DeviceName, label, screen.Primary));
        }

        return result;
    }

    /// <summary>
    /// Picks the target screen for the current settings, falling back to the primary
    /// display when a pinned monitor has been disconnected.
    /// </summary>
    public static Forms.Screen ResolveScreen(AppSettings settings)
    {
        switch (settings.MonitorMode)
        {
            case MonitorMode.Primary:
                return Forms.Screen.PrimaryScreen ?? Forms.Screen.AllScreens[0];

            case MonitorMode.Specific:
                var match = Forms.Screen.AllScreens.FirstOrDefault(
                    s => string.Equals(s.DeviceName, settings.MonitorDeviceName, StringComparison.OrdinalIgnoreCase));
                return match ?? Forms.Screen.PrimaryScreen ?? Forms.Screen.AllScreens[0];

            case MonitorMode.FollowCursor:
            default:
                return Forms.Screen.FromPoint(Forms.Cursor.Position);
        }
    }

    /// <summary>
    /// The target screen's work area expressed in WPF device-independent units.
    /// </summary>
    /// <remarks>
    /// WPF window coordinates are DIPs scaled by the *primary* display's DPI, while
    /// <see cref="Forms.Screen"/> reports raw pixels. Dividing by the primary scale factor
    /// is what keeps the console aligned on secondary monitors with a different DPI.
    /// </remarks>
    public static Rect GetWorkAreaInDips(AppSettings settings)
    {
        var screen = ResolveScreen(settings);
        var work = screen.WorkingArea;
        var scale = GetPrimaryScaleFactor();

        return new Rect(
            work.Left / scale,
            work.Top / scale,
            work.Width / scale,
            work.Height / scale);
    }

    /// <summary>
    /// The physical-pixels-per-DIP factor WPF applies to window coordinates.
    /// </summary>
    /// <remarks>
    /// Derived by comparing the primary screen measured in raw pixels against the same
    /// screen measured in WPF units, which needs no window handle and therefore works
    /// before the console has ever been shown.
    /// </remarks>
    private static double GetPrimaryScaleFactor()
    {
        var primary = Forms.Screen.PrimaryScreen;
        var widthInDips = SystemParameters.PrimaryScreenWidth;

        if (primary is null || widthInDips <= 0)
        {
            return 1.0;
        }

        var scale = primary.Bounds.Width / widthInDips;
        return scale > 0 ? scale : 1.0;
    }
}
