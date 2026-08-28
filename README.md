# CloudQuake

[![Build](https://github.com/Fiambre/CloudQuake/actions/workflows/build.yml/badge.svg)](https://github.com/Fiambre/CloudQuake/actions/workflows/build.yml)

A Quake-style dropdown console for [CloudCLI](https://cloudcli.ai) on Windows. Lives in the system tray, slides down from the top of the screen on a global hotkey, and embeds your CloudCLI instance via [WebView2](https://developer.microsoft.com/microsoft-edge/webview2/) — no browser window, no tab switching.

## Features

- **Global hotkey toggle** — show/hide the console from anywhere, fully configurable (modifiers + key).
- **Slide animation** — Quake-console-style drop-down/retract from the top of the screen.
- **System tray resident** — no taskbar clutter; right-click for Show/Hide, Settings, or Exit.
- **Persistent session** — logs into your CloudCLI instance once; the WebView2 profile keeps you signed in across restarts.
- **Configurable layout** — width/height as a percentage of screen size, position, hotkey, and target URL are all editable from Settings.
- **Optional autostart** — launches with Windows, minimized to the tray.

## Requirements

- Windows 10/11
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) (already installed on most up-to-date Windows systems)
- A running [CloudCLI](https://cloudcli.ai) instance (self-hosted or cloud) to point it at

## Download

Grab the latest build from [Releases](../../releases) — a single self-contained `CloudQuake.exe`, no install required.

## Usage

1. Run `CloudQuake.exe`. It starts minimized in the system tray.
2. Press the hotkey (default: `Ctrl+Alt+Shift+Q`) to drop down the console.
3. Log into your CloudCLI instance the first time — the session persists after that.
4. Press the hotkey again, hit `Esc`, or click outside the window to hide it.
5. Right-click the tray icon → **Configuración** to change the URL, hotkey, console size, or autostart.

> The default hotkey is `Ctrl+Alt+Shift+Q` because plain `Win+<letter>` combos are frequently reserved by Windows (Copilot, Search, etc.) or grabbed by other apps' global hooks (e.g. AutoHotkey scripts) before they ever reach CloudQuake. Change it freely in Settings if it conflicts with something on your machine.

## Building from source

Requires the [.NET SDK](https://dotnet.microsoft.com/download) (see `src/CloudQuake/CloudQuake.csproj` for the target version).

```powershell
git clone https://github.com/Fiambre/CloudQuake.git
cd CloudQuake
dotnet build src/CloudQuake/CloudQuake.csproj -c Release
```

To produce a single-file, self-contained executable like the one published in Releases:

```powershell
dotnet publish src/CloudQuake/CloudQuake.csproj -c Release -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o publish
```

## Project layout

```
.
├── .github/workflows/   CI: build, publish, and (on tags) release
├── src/CloudQuake/       WPF application source
│   ├── Models/            Settings model
│   ├── Services/          Hotkey registration, autostart, logging
│   ├── App.xaml(.cs)      App entry point, tray icon, startup wiring
│   ├── MainWindow.xaml(.cs)     The dropdown console window + WebView2 host
│   └── SettingsWindow.xaml(.cs) Settings UI
├── LICENSE
└── README.md
```

## How it works

CloudQuake is a borderless, always-on-top WPF window that stays hidden above the screen until toggled. On toggle it animates its position into view and hosts a [WebView2](https://www.nuget.org/packages/Microsoft.Web.WebView2) control pointed at your CloudCLI URL, using a dedicated, persistent WebView2 profile so your login session survives app restarts. The global hotkey is registered via the Win32 `RegisterHotKey` API on a hidden message-only window, independent of the console window's own visibility state, so it keeps working even while the console is hidden.

## License

[MIT](LICENSE)
