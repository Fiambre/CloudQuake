# Security Policy

## Supported versions

CloudQuake is a small utility with no release branches — fixes land on `main` and go out in
the next release. Please report against the latest release or `main`.

## Reporting a vulnerability

Report privately through
[GitHub Security Advisories](https://github.com/Fiambre/CloudQuake/security/advisories/new).
Please don't open a public issue for a vulnerability.

Include what you did, what happened, and the impact you think it has. I'll acknowledge the
report and let you know whether it's something I'll fix and roughly when.

## What CloudQuake touches

Useful context when judging whether something is a vulnerability here:

- **It embeds a web page you choose.** CloudQuake is a WebView2 host. It doesn't sandbox or
  filter the CloudCLI instance you point it at — that page runs with the privileges WebView2
  gives it. Point it only at instances you trust.
- **Your session lives on disk.** The WebView2 profile in `%APPDATA%\CloudQuake\WebView2Data`
  holds cookies and login state for that instance, protected by normal Windows file
  permissions and nothing more.
- **It injects a small script into the page.** One `keydown` listener that relays Escape to
  the host so the console can close. It reads no page content and sends nothing else.
- **It grants notification permission automatically** when the notification setting is on, so
  the embedded page can raise Windows toasts without prompting.
- **It registers a global hotkey and an autostart entry** under
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.

CloudQuake stores no credentials of its own and talks to no service other than the URL you
configure.
