using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using CloudQuake.Models;
using CloudQuake.Services;
using Microsoft.Web.WebView2.Core;

namespace CloudQuake;

public partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private bool _webViewReady;
    private bool _isShown;
    private bool _animating;
    private bool _forceClose;

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
            // Clicking outside the console hides it, like Quake's console losing focus.
            if (_isShown && !_animating)
            {
                Hide(animate: true);
            }
        };

        Loaded += async (_, _) => await EnsureWebViewInitializedAsync();
    }

    private async Task EnsureWebViewInitializedAsync()
    {
        if (_webViewReady)
        {
            return;
        }

        LoadingOverlay.Visibility = Visibility.Visible;

        var env = await CoreWebView2Environment.CreateAsync(
            userDataFolder: AppSettings.WebViewUserDataFolder);
        await WebView.EnsureCoreWebView2Async(env);

        WebView.CoreWebView2.NavigationCompleted += (_, args) =>
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            StatusText.Text = args.IsSuccess ? "" : "Error al cargar la página";
        };

        WebView.Source = new Uri(_settings.Url);
        _webViewReady = true;
    }

    public void Navigate(string url)
    {
        if (_webViewReady)
        {
            WebView.Source = new Uri(url);
        }
    }

    public void Reload()
    {
        if (_webViewReady)
        {
            WebView.Reload();
        }
    }

    /// <summary>Computed geometry for the console window on the primary screen's work area.</summary>
    private (double left, double top, double width, double height) ComputeGeometry()
    {
        var workArea = SystemParameters.WorkArea;
        var width = workArea.Width * Math.Clamp(_settings.WidthPercent, 0.2, 1.0);
        var height = workArea.Height * Math.Clamp(_settings.HeightPercent, 0.2, 1.0);
        var left = workArea.Left + (workArea.Width - width) / 2;
        var top = workArea.Top;
        return (left, top, width, height);
    }

    public void ToggleVisibility()
    {
        Logger.Log($"MainWindow.ToggleVisibility called. _isShown={_isShown} _animating={_animating} Visibility={Visibility}");
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
        var (left, top, width, height) = ComputeGeometry();
        Left = left;
        Width = width;
        Height = height;
        Top = top - height;

        Visibility = Visibility.Visible;
        Show();
        Activate();
        Topmost = true;

        _animating = true;
        var anim = new DoubleAnimation(top - height, top, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
        };
        anim.Completed += (_, _) => _animating = false;
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
            Visibility = Visibility.Hidden;
            return;
        }

        var (_, top, _, height) = ComputeGeometry();
        _animating = true;
        var anim = new DoubleAnimation(Top, top - height, TimeSpan.FromMilliseconds(180))
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

    private void ReloadButton_Click(object sender, RoutedEventArgs e) => Reload();

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var owner = this;
        var settingsWindow = new SettingsWindow(_settings) { Owner = owner };
        settingsWindow.ShowDialog();
    }

    private void HideButton_Click(object sender, RoutedEventArgs e) => Hide(animate: true);

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
