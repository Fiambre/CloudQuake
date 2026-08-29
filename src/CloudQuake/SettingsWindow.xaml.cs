using System.Windows;
using CloudQuake.Models;
using CloudQuake.Services;

namespace CloudQuake;

public partial class SettingsWindow : Window
{
    /// <summary>One row of the "Show on" picker.</summary>
    private sealed record MonitorChoice(string Label, MonitorMode Mode, string DeviceName);

    private readonly AppSettings _settings;

    /// <summary>Raised after settings are saved so the app can re-apply them.</summary>
    public static event Action<AppSettings>? SettingsSaved;

    /// <param name="settings">The live settings instance; only written on save.</param>
    /// <param name="isFirstRun">Shows the welcome header and requires a URL before continuing.</param>
    public SettingsWindow(AppSettings settings, bool isFirstRun = false)
    {
        _settings = settings;
        InitializeComponent();

        if (isFirstRun)
        {
            WelcomePanel.Visibility = Visibility.Visible;
            CancelButton.Content = "Quit";
            Title = "CloudQuake — Setup";
        }

        UrlBox.Text = _settings.Url;
        WinCheck.IsChecked = _settings.ModWin;
        CtrlCheck.IsChecked = _settings.ModCtrl;
        AltCheck.IsChecked = _settings.ModAlt;
        ShiftCheck.IsChecked = _settings.ModShift;

        foreach (var letter in "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
        {
            KeyCombo.Items.Add(letter.ToString());
        }
        for (var i = 1; i <= 12; i++)
        {
            KeyCombo.Items.Add($"F{i}");
        }
        KeyCombo.SelectedItem = _settings.Key.ToUpperInvariant();
        if (KeyCombo.SelectedItem is null)
        {
            KeyCombo.SelectedIndex = 0;
        }

        PopulateMonitors();

        WidthSlider.Value = _settings.WidthPercent;
        HeightSlider.Value = _settings.HeightPercent;
        HideOnFocusLossCheck.IsChecked = _settings.HideOnFocusLoss;
        NotificationsCheck.IsChecked = _settings.EnableNotifications;
        AutoStartCheck.IsChecked = _settings.StartWithWindows;
        DiagnosticsCheck.IsChecked = _settings.EnableDiagnosticLogging;
        LogPathText.Text = AppSettings.LogPath;
    }

    private void PopulateMonitors()
    {
        var choices = new List<MonitorChoice>
        {
            new("Monitor under the mouse cursor", MonitorMode.FollowCursor, string.Empty),
            new("Primary monitor", MonitorMode.Primary, string.Empty),
        };

        foreach (var monitor in MonitorService.GetMonitors())
        {
            choices.Add(new MonitorChoice(monitor.DisplayName, MonitorMode.Specific, monitor.DeviceName));
        }

        MonitorCombo.ItemsSource = choices;

        var selected = choices.FirstOrDefault(c => c.Mode == _settings.MonitorMode
            && (c.Mode != MonitorMode.Specific
                || string.Equals(c.DeviceName, _settings.MonitorDeviceName, StringComparison.OrdinalIgnoreCase)));

        // A pinned monitor may have been disconnected since it was chosen.
        MonitorCombo.SelectedItem = selected ?? choices[0];
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var url = UrlBox.Text.Trim();
        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed)
            || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
        {
            ShowError("Enter a valid URL starting with http:// or https://");
            return;
        }

        var hasModifier = WinCheck.IsChecked == true || CtrlCheck.IsChecked == true
            || AltCheck.IsChecked == true || ShiftCheck.IsChecked == true;
        if (!hasModifier)
        {
            ShowError("Pick at least one modifier (Win, Ctrl, Alt or Shift).");
            return;
        }

        _settings.Url = url;
        _settings.ModWin = WinCheck.IsChecked == true;
        _settings.ModCtrl = CtrlCheck.IsChecked == true;
        _settings.ModAlt = AltCheck.IsChecked == true;
        _settings.ModShift = ShiftCheck.IsChecked == true;
        _settings.Key = (string)KeyCombo.SelectedItem;
        _settings.WidthPercent = WidthSlider.Value;
        _settings.HeightPercent = HeightSlider.Value;
        _settings.HideOnFocusLoss = HideOnFocusLossCheck.IsChecked == true;
        _settings.EnableNotifications = NotificationsCheck.IsChecked == true;
        _settings.StartWithWindows = AutoStartCheck.IsChecked == true;
        _settings.EnableDiagnosticLogging = DiagnosticsCheck.IsChecked == true;

        if (MonitorCombo.SelectedItem is MonitorChoice choice)
        {
            _settings.MonitorMode = choice.Mode;
            _settings.MonitorDeviceName = choice.DeviceName;
        }

        _settings.Save();
        Logger.IsEnabled = _settings.EnableDiagnosticLogging;
        AutoStartService.Apply(_settings.StartWithWindows);
        SettingsSaved?.Invoke(_settings);

        DialogResult = true;
        Close();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
