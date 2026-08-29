using Forms = System.Windows.Forms;

namespace CloudQuake.Services;

/// <summary>
/// Bridges web notifications raised inside the embedded page to Windows toasts.
/// </summary>
/// <remarks>
/// WebView2 does not surface web notifications to the OS on its own — it raises
/// <c>NotificationReceived</c> and leaves presentation to the host. Windows 10/11 route
/// tray balloon tips through the Action Center as real toasts, so the existing tray icon
/// is used as the presenter and no extra packaging or dependency is required.
/// <para>
/// Only notifications raised while CloudQuake is running can be bridged. Push
/// notifications delivered to a service worker while the app is closed are out of reach:
/// the installed WebView2 SDK exposes no push-receiving API.
/// </para>
/// </remarks>
public static class NotificationService
{
    private static Forms.NotifyIcon? _trayIcon;

    /// <summary>Invoked when the user clicks the most recently shown toast.</summary>
    private static Action? _pendingClickHandler;

    public static void Attach(Forms.NotifyIcon trayIcon)
    {
        _trayIcon = trayIcon;
        _trayIcon.BalloonTipClicked += (_, _) =>
        {
            var handler = _pendingClickHandler;
            _pendingClickHandler = null;
            handler?.Invoke();
        };
        _trayIcon.BalloonTipClosed += (_, _) => _pendingClickHandler = null;
    }

    /// <summary>Shows a Windows toast. Returns false when no tray icon is available to present it.</summary>
    public static bool Show(string title, string body, Action? onClick = null)
    {
        if (_trayIcon is null)
        {
            return false;
        }

        _pendingClickHandler = onClick;

        // Empty text is ignored by the shell, so fall back to the app name.
        var safeTitle = string.IsNullOrWhiteSpace(title) ? "CloudQuake" : title;
        var safeBody = string.IsNullOrWhiteSpace(body) ? " " : body;

        _trayIcon.ShowBalloonTip(5000, safeTitle, safeBody, Forms.ToolTipIcon.None);
        return true;
    }

    /// <summary>Shows a warning toast for app-level problems (failed hotkey, etc.).</summary>
    public static void ShowWarning(string title, string body)
    {
        if (_trayIcon is null)
        {
            return;
        }

        _pendingClickHandler = null;
        _trayIcon.ShowBalloonTip(5000, title, body, Forms.ToolTipIcon.Warning);
    }
}
