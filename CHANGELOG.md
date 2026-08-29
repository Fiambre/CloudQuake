# Changelog

All notable changes to this project are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-08-29

First public release.

### Added

- Quake-style dropdown console that slides in over your CloudCLI instance.
- Configurable global hotkey (modifiers + key), defaulting to `Ctrl+Alt+Shift+Q`.
- Monitor selection: follow the mouse cursor (default), always the primary display, or a
  specific monitor — with correct placement across displays running different DPI scaling.
- Pin toggle to keep the console open when it loses focus, from the title bar or Settings.
- Windows toast notifications forwarded from the embedded page, clickable to reopen the
  console. Only while CloudQuake is running; push notifications delivered to a service worker
  while the app is closed are not supported.
- First-run setup asking for the CloudCLI URL.
- System tray icon with show/hide, settings, and quit.
- Persistent WebView2 profile, so the login survives restarts.
- Optional autostart with Windows.
- Opt-in troubleshooting log, size-capped and rolled.
- A clear, actionable error screen when the WebView2 runtime is missing, with a retry button.

[Unreleased]: https://github.com/Fiambre/CloudQuake/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/Fiambre/CloudQuake/releases/tag/v1.0.0
