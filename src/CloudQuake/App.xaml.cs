using System.Drawing;
using System.Windows;
using CloudQuake.Models;
using CloudQuake.Services;
using Forms = System.Windows.Forms;

namespace CloudQuake;

public partial class App : System.Windows.Application
{
    private const string MutexName = "CloudQuake_SingleInstance_9F3B2C";

    private System.Threading.Mutex? _mutex;
    private Forms.NotifyIcon? _trayIcon;
    private HotkeyManager? _hotkeyManager;
    private MainWindow? _mainWindow;
    private AppSettings _settings = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, ex) =>
        {
            Logger.Log($"UNHANDLED EXCEPTION: {ex.Exception}");
            ex.Handled = true;
        };

        _mutex = new System.Threading.Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (!createdNew)
        {
            Forms.MessageBox.Show(
                "CloudQuake ya está corriendo. Buscalo en la bandeja del sistema.",
                "CloudQuake", Forms.MessageBoxButtons.OK, Forms.MessageBoxIcon.Information);
            Shutdown();
            return;
        }

        _settings = AppSettings.Load();
        Logger.Log($"App.OnStartup: settings loaded. Url={_settings.Url} Win={_settings.ModWin} Ctrl={_settings.ModCtrl} Alt={_settings.ModAlt} Shift={_settings.ModShift} Key={_settings.Key}");
        AutoStartService.Apply(_settings.StartWithWindows);

        _mainWindow = new MainWindow(_settings);

        _hotkeyManager = new HotkeyManager();
        _hotkeyManager.HotkeyPressed += () =>
        {
            Logger.Log("App: HotkeyPressed event received, dispatching ToggleVisibility");
            Dispatcher.Invoke(() => _mainWindow?.ToggleVisibility());
        };
        RegisterHotkeyWithFeedback(_settings);

        SettingsWindow.SettingsSaved += settings =>
        {
            _settings = settings;
            RegisterHotkeyWithFeedback(settings);
        };

        SetupTrayIcon();
    }

    private void RegisterHotkeyWithFeedback(AppSettings settings)
    {
        var ok = _hotkeyManager?.Register(settings) ?? false;
        if (!ok)
        {
            _trayIcon?.ShowBalloonTip(
                4000,
                "CloudQuake",
                "No se pudo registrar la hotkey (puede estar en uso por otra app). Cambiala en Configuración.",
                Forms.ToolTipIcon.Warning);
        }
    }

    private void SetupTrayIcon()
    {
        var menu = new Forms.ContextMenuStrip();

        var toggleItem = new Forms.ToolStripMenuItem("Mostrar / Ocultar");
        toggleItem.Click += (_, _) => _mainWindow?.ToggleVisibility();
        menu.Items.Add(toggleItem);

        var settingsItem = new Forms.ToolStripMenuItem("Configuración");
        settingsItem.Click += (_, _) =>
        {
            var settingsWindow = new SettingsWindow(_settings);
            settingsWindow.ShowDialog();
        };
        menu.Items.Add(settingsItem);

        menu.Items.Add(new Forms.ToolStripSeparator());

        var exitItem = new Forms.ToolStripMenuItem("Salir");
        exitItem.Click += (_, _) => ExitApplication();
        menu.Items.Add(exitItem);

        _trayIcon = new Forms.NotifyIcon
        {
            Icon = BuildTrayIcon(),
            Visible = true,
            Text = "CloudQuake",
            ContextMenuStrip = menu,
        };
        _trayIcon.DoubleClick += (_, _) => _mainWindow?.ToggleVisibility();
    }

    /// <summary>Draws a small "cloud console" glyph at runtime so the app ships with no external icon asset.</summary>
    private static Icon BuildTrayIcon()
    {
        using var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var bg = new SolidBrush(Color.FromArgb(255, 13, 17, 23));
            g.FillEllipse(bg, 0, 0, 32, 32);

            using var accentPen = new Pen(Color.FromArgb(255, 88, 166, 255), 2f);
            g.DrawRectangle(accentPen, 6, 9, 20, 14);

            using var accentBrush = new SolidBrush(Color.FromArgb(255, 88, 166, 255));
            using var font = new Font("Consolas", 11f, System.Drawing.FontStyle.Bold);
            g.DrawString(">", font, accentBrush, 9, 10);
        }

        var hIcon = bitmap.GetHicon();
        return Icon.FromHandle(hIcon);
    }

    private void ExitApplication()
    {
        _mainWindow?.ForceClose();
        _mutex?.ReleaseMutex();
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
        base.OnExit(e);
    }
}
