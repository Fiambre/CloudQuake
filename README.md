# CloudQuake

[![Build](https://github.com/Fiambre/CloudQuake/actions/workflows/build.yml/badge.svg)](https://github.com/Fiambre/CloudQuake/actions/workflows/build.yml)
[![Release](https://img.shields.io/github/v/release/Fiambre/CloudQuake?sort=semver)](https://github.com/Fiambre/CloudQuake/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0078D4)](https://github.com/Fiambre/CloudQuake/releases)

A Quake-style dropdown console for [CloudCLI](https://cloudcli.ai) on Windows.

CloudQuake lives in the system tray and slides your CloudCLI instance down from the top of the
screen on a global hotkey — then gets out of the way. No browser window, no tab hunting, no
alt-tabbing to a window you lost. Press the key, it's there; press it again, it's gone.

```
    ┌──────────────────────────────────────────┐
    │  CloudQuake            📌  ↻  ⚙  ✕       │   ← Ctrl+Alt+Shift+Q
    ├──────────────────────────────────────────┤
    │                                          │
    │            your CloudCLI instance        │
    │                                          │
    └──────────────────────────────────────────┘
              ↑ slides down over whatever you were doing
```

## Features

- **Global hotkey toggle** — show/hide from anywhere, any modifier combination you like.
- **Multi-monitor aware** — drop down on the monitor under your cursor, always the primary, or
  one specific display. Placement stays correct across monitors with different DPI scaling.
- **Pin it open** — one click keeps the console up instead of auto-hiding when it loses focus.
- **Windows notifications** — notifications from CloudCLI surface as real Windows toasts; click
  one to jump straight back into the console.
- **Persistent session** — log in once; the WebView2 profile keeps you signed in across restarts.
- **Adjustable size** — width and height as a percentage of the screen.
- **Starts with Windows** — optional, straight into the tray.
- **Light footprint** — no bundled browser. Uses the WebView2 runtime Windows already ships.

## Requirements

- Windows 10 or 11
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) —
  preinstalled on current Windows; CloudQuake tells you (with a download link) if it's missing
- A [CloudCLI](https://cloudcli.ai) instance to point it at, self-hosted or cloud

## Install

Download `CloudQuake.exe` from the [latest release](https://github.com/Fiambre/CloudQuake/releases/latest).
It's a single self-contained executable — no installer, no .NET runtime to install first. Put it
anywhere and run it.

On first launch it asks for your CloudCLI URL, then minimizes to the tray.

## Usage

| Action | How |
|---|---|
| Show / hide the console | `Ctrl+Alt+Shift+Q` (configurable) |
| Hide it | `Esc`, the hotkey again, or click outside |
| Keep it open | 📌 in the title bar, or turn off *Hide when it loses focus* |
| Reload the page | ↻ in the title bar |
| Settings | ⚙ in the title bar, or right-click the tray icon |
| Quit | Right-click the tray icon → **Quit** |

### Why the odd default hotkey?

`Ctrl+Alt+Shift+Q` is deliberately out of the way. Windows reserves most `Win`+letter
combinations (`Win+C` goes to Copilot, for example), and other tools — AutoHotkey scripts,
launchers, capture utilities — install low-level keyboard hooks that swallow common shortcuts
before any application sees them. If your hotkey silently stops working, something else has
claimed it; pick another one in Settings.

### Multiple monitors

Under **Settings → Show on**:

- **Monitor under the mouse cursor** (default) — the console follows you between screens.
- **Primary monitor** — always the same screen, wherever the mouse is.
- **A specific monitor** — pinned to one display. If that display is disconnected, CloudQuake
  falls back to the primary monitor instead of opening off-screen.

### Notifications

With *Forward page notifications to Windows* enabled, notifications raised by CloudCLI appear as
Windows toasts, and clicking one brings the console back up. CloudQuake also grants the page's
notification permission automatically, so you're not prompted every session.

One real limitation: this only works **while CloudQuake is running**. WebView2 exposes no way to
receive push notifications delivered to a service worker while the app is closed, so those don't
reach Windows.

## Configuration

Settings live in `%APPDATA%\CloudQuake`:

| File | What it is |
|---|---|
| `settings.json` | Your configuration |
| `WebView2Data\` | The browser profile — cookies, session, local storage |
| `cloudquake.log` | Troubleshooting log, only when you enable it in Settings |

Deleting `WebView2Data\` signs you out and resets the embedded page's state, nothing else.

## Building from source

Requires the [.NET SDK](https://dotnet.microsoft.com/download) matching the `TargetFramework` in
`src/CloudQuake/CloudQuake.csproj`.

```powershell
git clone https://github.com/Fiambre/CloudQuake.git
cd CloudQuake
dotnet build src/CloudQuake/CloudQuake.csproj -c Release
```

To produce the same single-file executable the releases ship:

```powershell
dotnet publish src/CloudQuake/CloudQuake.csproj -c Release -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o publish
```

See [CONTRIBUTING.md](CONTRIBUTING.md) for the project layout and how to debug it.

## How it works

CloudQuake is a borderless, always-on-top WPF window parked above the top edge of the screen. On
toggle it animates into view and hosts a [WebView2](https://www.nuget.org/packages/Microsoft.Web.WebView2)
control pointed at your CloudCLI URL, using a dedicated persistent profile so your session
survives restarts.

Two details make it behave the way a Quake console should:

- The hotkey is registered through the Win32 `RegisterHotKey` API on a hidden message-only
  window, separate from the console window itself — so it keeps working while the console is
  hidden, and survives the window being closed.
- WebView2 renders into its own child HWND and swallows keystrokes, so `Esc` pressed inside the
  page would never reach the host window. A small `keydown` listener injected into the page
  relays it back — and deliberately stays out of the way when you're typing in a field, so `Esc`
  still does what the page expects.

## Releasing

Push a `v*.*.*` tag. CI builds, stamps the version, and publishes a GitHub release with the
executable attached.

```powershell
git tag v1.0.0
git push origin v1.0.0
```

## License

[MIT](LICENSE)
