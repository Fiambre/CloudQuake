using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using CloudQuake.Models;
using CloudQuake.Services;
using Microsoft.Web.WebView2.Core;

namespace CloudQuake;

public partial class MainWindow : Window
{
    private const string PinnedGlyph = "\U0001F4CC";   // 📌
    private const double ShowDurationMs = 220;
    private const double HideDurationMs = 180;

    private const string EscapeMessage = "cloudquake:escape";

    /// <summary>
    /// Relays Escape from inside the page back to the host so the console can close,
    /// while leaving the key available to the app when it is being used for something
    /// else (a focused text field, an open dialog).
    /// </summary>
    private const string EscapeRelayScript = """
        (function () {
          if (window.__cloudQuakeEscapeRelay) { return; }
          window.__cloudQuakeEscapeRelay = true;
          window.addEventListener('keydown', function (e) {
            if (e.key !== 'Escape' || e.defaultPrevented) { return; }
            var t = e.target;
            var typing = t && (t.isContentEditable
              || t.tagName === 'INPUT' || t.tagName === 'TEXTAREA' || t.tagName === 'SELECT');
            if (typing && t.value) { return; }
            window.chrome.webview.postMessage('cloudquake:escape');
          }, true);
        })();
        """;

    private readonly AppSettings _settings;
    private bool _webViewReady;
    private bool _isShown;
    private bool _animating;
    private bool _forceClose;
    private string? _loadedUrl;

    public MainWindow(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();

        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Hide(animate: true);
            }
        };

        Deactivated += (_, _) =>
        {
            if (_settings.HideOnFocusLoss && _isShown && !_animating)
            {
                Hide(animate: true);
            }
        };

        Loaded += async (_, _) => await EnsureWebViewInitializedAsync();

        UpdatePinButton();
    }

    #region WebView

    private async Task EnsureWebViewInitializedAsync()
    {
        if (_webViewReady || string.IsNullOrWhiteSpace(_settings.Url))
        {
            return;
        }

        ErrorOverlay.Visibility = Visibility.Collapsed;
        LoadingOverlay.Visibility = Visibility.Visible;

        try
        {
            var env = await CoreWebView2Environment.CreateAsync(
                userDataFolder: AppSettings.WebViewUserDataFolder);
            await WebView.EnsureCoreWebView2Async(env);
        }
        catch (Exception ex)
        {
            // Most often a missing/outdated WebView2 runtime; surface it instead of dying silently.
            Logger.Log($"WebView2 initialization failed: {ex}");
            LoadingOverlay.Visibility = Visibility.Collapsed;
            ErrorDetailText.Text = ex.Message;
            ErrorOverlay.Visibility = Visibility.Visible;
            return;
        }

        var core = WebView.CoreWebView2;

        core.NavigationCompleted += (_, args) =>
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            StatusText.Text = args.IsSuccess
                ? string.Empty
                : $"Could not load the page ({args.WebErrorStatus})";
        };

        // WebView2 renders into its own HWND, so Esc pressed while the page has focus never
        // reaches the WPF window. A tiny listener inside the page relays it back instead.
        core.WebMessageReceived += (_, args) =>
        {
            if (args.TryGetWebMessageAsString() == EscapeMessage)
            {
                Dispatcher.BeginInvoke(() => Hide(animate: true));
            }
        };
        await core.AddScriptToExecuteOnDocumentCreatedAsync(EscapeRelayScript);

        core.PermissionRequested += (_, args) =>
        {
            if (args.PermissionKind == CoreWebView2PermissionKind.Notifications)
            {
                args.State = _settings.EnableNotifications
                    ? CoreWebView2PermissionState.Allow
                    : CoreWebView2PermissionState.Deny;
                args.Handled = true;
            }
        };

        core.NotificationReceived += OnNotificationReceived;

        Navigate(_settings.Url);
        _webViewReady = true;
    }

    /// <summary>Presents a page notification as a Windows toast that reopens the console when clicked.</summary>
    private void OnNotificationReceived(object? sender, CoreWebView2NotificationReceivedEventArgs args)
    {
        if (!_settings.EnableNotifications)
        {
            return;
        }

        var notification = args.Notification;
        var shown = NotificationService.Show(
            notification.Title,
            notification.Body,
            onClick: () => Dispatcher.BeginInvoke(() =>
            {
                notification.ReportClicked();
                ShowConsole();
            }));

        if (!shown)
        {
            return;
        }

        // Tell the page we took over presentation, otherwise WebView2 renders its own popup.
        args.Handled = true;
        notification.ReportShown();
    }

    public void Navigate(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || WebView.CoreWebView2 is null)
        {
            return;
        }

        LoadingOverlay.Visibility = Visibility.Visible;
        WebView.CoreWebView2.Navigate(url);
        _loadedUrl = url;
    }

    public void Reload() => WebView.CoreWebView2?.Reload();

    /// <summary>Re-applies settings that changed while the app was running.</summary>
    public async Task ApplySettingsAsync()
    {
        UpdatePinButton();

        if (!_webViewReady)
        {
            // Setup may have just supplied the first URL.
            await EnsureWebViewInitializedAsync();
            return;
        }

        if (!string.Equals(_loadedUrl, _settings.Url, StringComparison.OrdinalIgnoreCase))
        {
            Navigate(_settings.Url);
        }

        if (_isShown)
        {
            ApplyGeometry(out _);
        }
    }

    #endregion

    #region Show / hide

    /// <summary>
    /// Positions and sizes the window over the configured monitor.
    /// </summary>
    /// <param name="top">The final on-screen Y coordinate the slide animation lands on.</param>
    private void ApplyGeometry(out double top)
    {
        var workArea = MonitorService.GetWorkAreaInDips(_settings);

        var width = workArea.Width * Math.Clamp(_settings.WidthPercent, 0.2, 1.0);
        var height = workArea.Height * Math.Clamp(_settings.HeightPercent, 0.2, 1.0);

        Width = width;
        Height = height;
        Left = workArea.Left + ((workArea.Width - width) / 2);
        top = workArea.Top;
    }

    public void ToggleVisibility()
    {
        if (_isShown)
        {
            Hide(animate: true);
        }
        else
        {
            ShowConsole();
        }
    }

    public void ShowConsole()
    {
        ApplyGeometry(out var top);
        Top = top - Height;

        Visibility = Visibility.Visible;
        Show();
        Topmost = true;
        Activate();

        _animating = true;
        var anim = new DoubleAnimation(Top, top, TimeSpan.FromMilliseconds(ShowDurationMs))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
        };
        anim.Completed += (_, _) =>
        {
            _animating = false;
            // Hand focus to the page so typing goes straight into CloudCLI.
            WebView.Focus();
        };
        BeginAnimation(TopProperty, anim);

        _isShown = true;
    }

    public void Hide(bool animate)
    {
        if (!_isShown)
        {
            return;
        }

        _isShown = false;

        if (!animate)
        {
            BeginAnimation(TopProperty, null);
            Visibility = Visibility.Hidden;
            return;
        }

        _animating = true;
        var anim = new DoubleAnimation(Top, Top - Height, TimeSpan.FromMilliseconds(HideDurationMs))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn },
        };
        anim.Completed += (_, _) =>
        {
            _animating = false;
            Visibility = Visibility.Hidden;
        };
        BeginAnimation(TopProperty, anim);
    }

    #endregion

    #region Title bar

    private void UpdatePinButton()
    {
        // Pinned == does not auto-hide on focus loss.
        var pinned = !_settings.HideOnFocusLoss;
        PinButton.Content = PinnedGlyph;
        PinButton.Opacity = pinned ? 1.0 : 0.45;
        PinButton.ToolTip = pinned
            ? "Pinned — stays open when it loses focus (click to unpin)"
            : "Not pinned — hides when it loses focus (click to pin)";
    }

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        _settings.HideOnFocusLoss = !_settings.HideOnFocusLoss;
        _settings.Save();
        UpdatePinButton();
    }

    private void ReloadButton_Click(object sender, RoutedEventArgs e) => Reload();

    private void SettingsButton_Click(object sender, RoutedEventArgs e) => OpenSettings();

    /// <summary>Opens the settings dialog, suspending auto-hide so the console stays put behind it.</summary>
    public void OpenSettings()
    {
        var restore = _settings.HideOnFocusLoss;
        _settings.HideOnFocusLoss = false;

        var dialog = new SettingsWindow(_settings);
        if (IsVisible)
        {
            dialog.Owner = this;
        }

        if (dialog.ShowDialog() != true)
        {
            // On save the dialog already wrote the user's own choice onto the settings.
            _settings.HideOnFocusLoss = restore;
        }

        UpdatePinButton();
    }

    private void HideButton_Click(object sender, RoutedEventArgs e) => Hide(animate: true);

    private async void RetryButton_Click(object sender, RoutedEventArgs e)
        => await EnsureWebViewInitializedAsync();

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    #endregion

    public void ForceClose()
    {
        _forceClose = true;
        Close();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_forceClose)
        {
            return;
        }

        // Closing the window (e.g. via Alt+F4) just hides it; the app lives in the tray.
        e.Cancel = true;
        Hide(animate: false);
    }
}
