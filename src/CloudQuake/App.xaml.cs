using System.IO;
using System.Windows;
using System.Windows.Resources;
using CloudQuake.Models;
using CloudQuake.Services;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace CloudQuake;

public partial class App : System.Windows.Application
{
    private const string MutexName = "CloudQuake_SingleInstance_9F3B2C";

    private System.Threading.Mutex? _mutex;
    private Forms.NotifyIcon? _trayIcon;
    private Drawing.Icon? _trayIconImage;
    private HotkeyManager? _hotkeyManager;
    private MainWindow? _mainWindow;
    private AppSettings _settings = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            Logger.Log($"Unhandled exception: {args.Exception}");
            args.Handled = true;
        };

        _mutex = new System.Threading.Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (!createdNew)
        {
            Forms.MessageBox.Show(
                "CloudQuake is already running — look for it in the system tray.",
                "CloudQuake", Forms.MessageBoxButtons.OK, Forms.MessageBoxIcon.Information);
            Shutdown();
            return;
        }

        _settings = AppSettings.Load();
        Logger.IsEnabled = _settings.EnableDiagnosticLogging;
        AutoStartService.Apply(_settings.StartWithWindows);

        // The tray icon is the app's only always-available surface, so it comes up first:
        // startup problems below need somewhere to report themselves.
        SetupTrayIcon();

        if (_settings.NeedsSetup && !RunFirstTimeSetup())
        {
            Shutdown();
            return;
        }

        _mainWindow = new MainWindow(_settings);

        _hotkeyManager = new HotkeyManager();
        _hotkeyManager.HotkeyPressed += () => Dispatcher.Invoke(() => _mainWindow?.ToggleVisibility());
        RegisterHotkeyWithFeedback(_settings);

        SettingsWindow.SettingsSaved += async settings =>
        {
            _settings = settings;
            RegisterHotkeyWithFeedback(settings);
            if (_mainWindow is not null)
            {
                await _mainWindow.ApplySettingsAsync();
            }
        };
    }

    /// <summary>Prompts for the CloudCLI URL on first launch. Returns false if the user quits.</summary>
    private bool RunFirstTimeSetup()
    {
        var setup = new SettingsWindow(_settings, isFirstRun: true);
        if (setup.ShowDialog() != true)
        {
            return false;
        }

        Logger.IsEnabled = _settings.EnableDiagnosticLogging;
        return true;
    }

    private void RegisterHotkeyWithFeedback(AppSettings settings)
    {
        if (_hotkeyManager?.Register(settings) == false)
        {
            NotificationService.ShowWarning(
                "CloudQuake",
                "That hotkey is already in use by another app. Pick a different one in Settings.");
        }
    }

    private void SetupTrayIcon()
    {
        var menu = new Forms.ContextMenuStrip();

        var toggleItem = new Forms.ToolStripMenuItem("Show / Hide");
        toggleItem.Click += (_, _) => _mainWindow?.ToggleVisibility();
        menu.Items.Add(toggleItem);

        var settingsItem = new Forms.ToolStripMenuItem("Settings");
        settingsItem.Click += (_, _) => OpenSettings();
        menu.Items.Add(settingsItem);

        menu.Items.Add(new Forms.ToolStripSeparator());

        var exitItem = new Forms.ToolStripMenuItem("Quit");
        exitItem.Click += (_, _) => ExitApplication();
        menu.Items.Add(exitItem);

        _trayIconImage = LoadAppIcon();

        _trayIcon = new Forms.NotifyIcon
        {
            Icon = _trayIconImage,
            Visible = true,
            Text = "CloudQuake",
            ContextMenuStrip = menu,
        };
        _trayIcon.DoubleClick += (_, _) => _mainWindow?.ToggleVisibility();

        NotificationService.Attach(_trayIcon);
    }

    private void OpenSettings()
    {
        if (_mainWindow is null)
        {
            new SettingsWindow(_settings).ShowDialog();
            return;
        }

        // Route through the console so it can suspend auto-hide while the dialog is open.
        _mainWindow.OpenSettings();
    }

    /// <summary>Loads the packaged application icon, falling back to a stock icon.</summary>
    private static Drawing.Icon LoadAppIcon()
    {
        try
        {
            StreamResourceInfo? resource = GetResourceStream(
                new Uri("pack://application:,,,/Assets/cloudquake.ico"));

            if (resource is not null)
            {
                using var stream = resource.Stream;
                return new Drawing.Icon(stream, new Drawing.Size(32, 32));
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Could not load the packaged icon: {ex.Message}");
        }

        return Drawing.SystemIcons.Application;
    }

    private void ExitApplication()
    {
        _mainWindow?.ForceClose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyManager?.Dispose();

        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        _trayIconImage?.Dispose();
        _trayIconImage = null;

        _mutex?.Dispose();
        _mutex = null;

        base.OnExit(e);
    }
}
