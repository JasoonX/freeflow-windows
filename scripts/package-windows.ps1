param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SelfContained
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$project = Join-Path $repoRoot "FreeFlowWindows\FreeFlowWindows.csproj"
$releaseDir = Join-Path $repoRoot "releases"
$publishArgs = @(
    "publish",
    $project,
    "-c", $Configuration,
    "-r", $Runtime,
    "--self-contained", ($(if ($SelfContained) { "true" } else { "false" }))
)

New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null

dotnet build $project
dotnet run --project (Join-Path $repoRoot "FreeFlowWindows.Tests\FreeFlowWindows.Tests.csproj")
dotnet @publishArgs

$publishDir = Join-Path $repoRoot "FreeFlowWindows\bin\$Configuration\net6.0-windows\$Runtime\publish"
$zipPath = Join-Path $releaseDir "FreeFlow-for-Windows-$Runtime.zip"
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath
Write-Host "Created $zipPath"
