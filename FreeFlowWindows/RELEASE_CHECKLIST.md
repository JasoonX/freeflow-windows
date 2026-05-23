# Windows Release Checklist

Use this checklist before publishing FreeFlow for Windows to general users.

- Build with `scripts\package-windows.ps1`.
- Run `dotnet run --project FreeFlowWindows.Tests\FreeFlowWindows.Tests.csproj`.
- Test on a clean Windows 10 machine.
- Test on a clean Windows 11 machine.
- Verify first-run setup asks for the Groq API key, activation key, and startup
  preference.
- Verify advanced settings are collapsed by default in Settings.
- Verify Copy last transcript copies the most recent transcript to the clipboard.
- Verify the selected activation key inserts into:
  - Notepad
  - VS Code
  - Chrome
  - Edge
  - Word
  - Google Docs
  - Windows Terminal
  - PowerShell
- Verify Groq billing/usage shows `whisper-large-v3` plus the configured
  polish model.
- Verify the app exits cleanly from the tray menu.
- Sign the executable, or clearly label the release as unsigned.
- Attach `releases\FreeFlow-for-Windows-win-x64.zip` to a GitHub prerelease.
