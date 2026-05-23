# Instructions for Coding Agents

## App

The app is called **FreeFlow for Windows**.

## Scope

This repository contains the Windows app only.

## Formatting

- End every text file with exactly one newline.
- Do not leave trailing whitespace.
- Prefer clear, direct user-facing copy.

## Build and Test

```powershell
dotnet build FreeFlowWindows\FreeFlowWindows.csproj
dotnet run --project FreeFlowWindows.Tests\FreeFlowWindows.Tests.csproj
```

## Package

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\package-windows.ps1
```

The release ZIP is written to:

```text
releases\FreeFlow-for-Windows-win-x64.zip
```

## Git

- Keep generated build outputs out of git.
- The `releases/` folder is ignored.
- Commit source, tests, docs, scripts, and workflow changes together when they
  belong to the same Windows release.
