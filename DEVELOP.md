# Develop

Build, test, and package FreeFlow for Windows.

## Prerequisites

- Windows 10 or later
- .NET 6 SDK

## Build

```powershell
dotnet build FreeFlowWindows\FreeFlowWindows.csproj
```

## Test

```powershell
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

## Project Structure

- `FreeFlowWindows/`: Windows tray app.
- `FreeFlowWindows.Tests/`: lightweight test runner for settings, Groq request
  contracts, WAV encoding, and Win32 interop layout.
- `scripts/package-windows.ps1`: build, test, publish, and ZIP packaging.

## Runtime Notes

FreeFlow for Windows uses:

- a low-level keyboard hook for the activation key
- WinMM for microphone capture
- Groq HTTP APIs for transcription and optional cleanup
- DPAPI for local secret storage
- clipboard paste and synthetic input for text insertion
