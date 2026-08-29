# Contributing to CloudQuake

Thanks for taking the time to help out. CloudQuake is a small, focused utility, so the bar
for changes is mostly "does it keep the app simple and predictable".

## Getting set up

You need the [.NET SDK](https://dotnet.microsoft.com/download) matching the `TargetFramework`
in `src/CloudQuake/CloudQuake.csproj`, and Windows 10/11 with the
[WebView2 runtime](https://developer.microsoft.com/microsoft-edge/webview2/).

```powershell
git clone https://github.com/Fiambre/CloudQuake.git
cd CloudQuake
dotnet build src/CloudQuake/CloudQuake.csproj -c Release
```

Run it straight from the build output:

```powershell
.\src\CloudQuake\bin\Release\net10.0-windows\CloudQuake.exe
```

The app is single-instance, so stop any running copy (tray icon → **Quit**) before starting
a new build.

## Where things live

| Path | What it holds |
|---|---|
| `src/CloudQuake/App.xaml.cs` | Startup, single-instance guard, tray icon, first-run setup |
| `src/CloudQuake/MainWindow.xaml(.cs)` | The dropdown console window and the WebView2 host |
| `src/CloudQuake/SettingsWindow.xaml(.cs)` | Settings dialog, also used as the first-run screen |
| `src/CloudQuake/Models/AppSettings.cs` | Persisted settings and their defaults |
| `src/CloudQuake/Services/` | Hotkey registration, monitor resolution, autostart, notifications, logging |

User data lives in `%APPDATA%\CloudQuake` — `settings.json`, the WebView2 profile, and the
optional troubleshooting log.

## Debugging

Turn on **Write a troubleshooting log** in Settings; it writes to
`%APPDATA%\CloudQuake\cloudquake.log` (capped at 1 MB, then rolled to `.log.old`). It's off by
default so a normal install writes nothing.

## Pull requests

- Keep the diff focused; one concern per PR.
- Match the surrounding style — the `.editorconfig` covers formatting, and the build runs with
  `EnforceCodeStyleInBuild`.
- Build cleanly (`dotnet build -c Release`) with no new warnings.
- Test the actual behavior you changed. A lot of this app is window management and global
  hotkeys, which unit tests don't meaningfully cover — say what you tested in the PR.
- UI strings are English.

## Reporting bugs

Open an [issue](https://github.com/Fiambre/CloudQuake/issues) using the bug template. The
Windows version, CloudQuake version, and monitor setup (how many, mixed DPI or scaling?) are
usually what makes a report actionable — a lot of the tricky bugs here are multi-monitor and
DPI related.
