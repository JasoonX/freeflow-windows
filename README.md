<p align="center">
  <img src="FreeFlowWindows/Resources/AppLogo.png" width="128" height="128" alt="FreeFlow for Windows icon">
</p>

<h1 align="center">FreeFlow for Windows</h1>

<p align="center">
  Push-to-talk dictation for Windows using Groq.
</p>

<p align="center">
  <a href="https://github.com/JasoonX/freeflow-windows/releases/download/v0.1.0-alpha.2/FreeFlow-for-Windows-win-x64.zip"><b>Download for Windows</b></a><br>
  <sub>Alpha release. Unsigned app.</sub>
</p>

---

## Overview

FreeFlow for Windows is a tray app for voice dictation. Hold an activation key,
speak naturally, release, and the app inserts the transcript into the focused
text field.

This Windows version is based on the original
[FreeFlow](https://github.com/zachlatta/freeflow) project.

This first alpha is intentionally small:

- Groq transcription
- Optional text cleanup
- Configurable activation key
- Optional launch at Windows startup
- Clipboard paste into the active app

The app is unsigned, so Windows may show a SmartScreen warning on first launch.

## Quick Start

1. Download `FreeFlow-for-Windows-win-x64.zip` from the latest release.
2. Extract it to a folder such as `%LOCALAPPDATA%\FreeFlow for Windows`.
3. Run `FreeFlowWindows.exe`.
4. Create a Groq API key at `https://console.groq.com/keys`.
5. Paste the key into setup and choose an activation key.
6. Click into a text field, hold the activation key, speak, then release.

## Settings

Open Settings from the tray icon to change:

- Groq API key
- Activation key
- Startup behavior
- Transcription model
- Polish model
- Cleanup
- Language

## Privacy

Recorded audio is sent directly from this app to Groq for transcription. The raw
transcript is optionally sent to Groq chat completions for cleanup.

Secrets are stored in `%LOCALAPPDATA%\FreeFlow\settings.json` and protected with
Windows DPAPI for the current Windows user.

## Build

```powershell
dotnet build FreeFlowWindows\FreeFlowWindows.csproj
dotnet run --project FreeFlowWindows.Tests\FreeFlowWindows.Tests.csproj
```

## Package

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\package-windows.ps1
```

The ZIP is written to:

```text
releases\FreeFlow-for-Windows-win-x64.zip
```

## Troubleshooting

- Nothing inserts: choose **Copy last transcript** from the tray menu and paste
  manually. If that works, the target app may be blocking synthetic input.
- Groq returns an authentication error: verify the key is current and has not
  been revoked.
- No audio captured: check Windows microphone permissions and default input
  device.
- The activation key conflicts with another app: choose a different key in
  Settings.

## Demo

<p align="center">
  <img src="assets/demo.gif" alt="FreeFlow for Windows demo" width="700">
</p>
