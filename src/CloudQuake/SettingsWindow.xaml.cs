using System.Windows;
using CloudQuake.Models;
using CloudQuake.Services;

namespace CloudQuake;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;

    /// <summary>Raised after settings are saved so the app can re-register the hotkey and refresh the console.</summary>
    public static event Action<AppSettings>? SettingsSaved;

    public SettingsWindow(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();

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
        if (KeyCombo.SelectedItem is null && KeyCombo.Items.Count > 0)
        {
            KeyCombo.SelectedIndex = 0;
        }

        WidthSlider.Value = _settings.WidthPercent;
        HeightSlider.Value = _settings.HeightPercent;
        AutoStartCheck.IsChecked = _settings.StartWithWindows;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var url = UrlBox.Text.Trim();
        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) || (parsed.Scheme != "http" && parsed.Scheme != "https"))
        {
            HotkeyErrorText.Text = "URL inválida. Debe empezar con http:// o https://";
            HotkeyErrorText.Visibility = Visibility.Visible;
            return;
        }

        var key = KeyCombo.SelectedItem as string ?? _settings.Key;
        var hasModifier = WinCheck.IsChecked == true || CtrlCheck.IsChecked == true
            || AltCheck.IsChecked == true || ShiftCheck.IsChecked == true;
        if (!hasModifier)
        {
            HotkeyErrorText.Text = "Elegí al menos un modificador (Win, Ctrl, Alt o Shift).";
            HotkeyErrorText.Visibility = Visibility.Visible;
            return;
        }

        _settings.Url = url;
        _settings.ModWin = WinCheck.IsChecked == true;
        _settings.ModCtrl = CtrlCheck.IsChecked == true;
        _settings.ModAlt = AltCheck.IsChecked == true;
        _settings.ModShift = ShiftCheck.IsChecked == true;
        _settings.Key = key;
        _settings.WidthPercent = WidthSlider.Value;
        _settings.HeightPercent = HeightSlider.Value;
        _settings.StartWithWindows = AutoStartCheck.IsChecked == true;

        _settings.Save();
        AutoStartService.Apply(_settings.StartWithWindows);
        SettingsSaved?.Invoke(_settings);

        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
