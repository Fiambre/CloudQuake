using System.Runtime.InteropServices;
using System.Windows.Interop;
using CloudQuake.Models;

namespace CloudQuake.Services;

/// <summary>
/// Registers a single system-wide hotkey using a hidden message-only window,
/// independent of the console window's own visibility state.
/// </summary>
public sealed class HotkeyManager : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int MOD_NOREPEAT = 0x4000;
    private const int HOTKEY_ID = 0xB00C; // arbitrary app-scoped id

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private HwndSource? _source;
    private bool _registered;

    public event Action? HotkeyPressed;

    public HotkeyManager()
    {
        var parameters = new HwndSourceParameters("CloudQuakeHotkeySink")
        {
            WindowStyle = 0,
            ParentWindow = new IntPtr(-3), // HWND_MESSAGE: message-only window, never shown
        };
        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);
    }

    public bool Register(AppSettings settings)
    {
        Unregister();
        if (_source is null)
        {
            return false;
        }

        uint modifiers = MOD_NOREPEAT;
        if (settings.ModWin) modifiers |= 0x0008;
        if (settings.ModCtrl) modifiers |= 0x0002;
        if (settings.ModAlt) modifiers |= 0x0001;
        if (settings.ModShift) modifiers |= 0x0004;

        var vk = VirtualKeyFromName(settings.Key);
        if (vk == 0)
        {
            return false;
        }

        _registered = RegisterHotKey(_source.Handle, HOTKEY_ID, modifiers, vk);
        if (!_registered)
        {
            Logger.Log($"Hotkey registration failed (win32 error {Marshal.GetLastWin32Error()}) for "
                + $"Win:{settings.ModWin} Ctrl:{settings.ModCtrl} Alt:{settings.ModAlt} Shift:{settings.ModShift} Key:{settings.Key}");
        }

        return _registered;
    }

    public void Unregister()
    {
        if (_registered && _source is not null)
        {
            UnregisterHotKey(_source.Handle, HOTKEY_ID);
            _registered = false;
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            try
            {
                HotkeyPressed?.Invoke();
            }
            catch (Exception ex)
            {
                Logger.Log($"HotkeyManager: HotkeyPressed handler threw: {ex}");
            }
            handled = true;
        }

        return IntPtr.Zero;
    }

    /// <summary>Maps a human-readable key name (as stored in settings) to a Win32 virtual-key code.</summary>
    public static uint VirtualKeyFromName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return 0;
        }

        name = name.Trim().ToUpperInvariant();

        if (name.Length == 1 && name[0] is (>= 'A' and <= 'Z') or (>= '0' and <= '9'))
        {
            return name[0];
        }

        if (name.Length is 2 or 3 && name[0] == 'F' && int.TryParse(name.AsSpan(1), out var fn) && fn is >= 1 and <= 24)
        {
            return (uint)(0x70 + (fn - 1)); // VK_F1 = 0x70
        }

        return name switch
        {
            "SPACE" => 0x20,
            "TAB" => 0x09,
            "ESC" or "ESCAPE" => 0x1B,
            "OEM_TILDE" or "`" or "~" => 0xC0,
            _ => 0,
        };
    }

    public void Dispose()
    {
        Unregister();
        _source?.RemoveHook(WndProc);
        _source?.Dispose();
        _source = null;
    }
}
