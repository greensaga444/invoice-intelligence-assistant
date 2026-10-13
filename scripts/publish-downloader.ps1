[CmdletBinding()]
param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",

    [ValidateSet("win-x64", "win-x86", "win-arm64")]
    [string]$Runtime = "win-x64",

    [switch]$FrameworkDependent,

    [string]$OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $repoRoot "build\deploy\InvoiceIntelligence"
}

$moduleOutput = Join-Path $OutputPath "downloader"

$dotnetPath = (Get-Command dotnet -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -ErrorAction SilentlyContinue)
if (-not $dotnetPath) {
    $fallbackDotnetPath = "C:\PROGRA~1\dotnet\dotnet.exe"
    if (Test-Path $fallbackDotnetPath) {
        $dotnetPath = $fallbackDotnetPath
    }
}

if (-not $dotnetPath) {
    throw "Cannot find dotnet executable. Install .NET SDK 8 or add dotnet to PATH."
}

$projectPath = Join-Path $repoRoot "src\InvoiceDownloader\InvoiceDownloader.csproj"
if (-not (Test-Path $projectPath)) {
    throw "Project file not found: $projectPath"
}

$selfContained = if ($FrameworkDependent) { "false" } else { "true" }

Write-Host "Publishing InvoiceDownloader..." -ForegroundColor Cyan
& $dotnetPath publish $projectPath -c $Configuration -r $Runtime --self-contained $selfContained -o $moduleOutput
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed for InvoiceDownloader."
}

$configDir = Join-Path $OutputPath "config"

$null = New-Item -ItemType Directory -Path $configDir -Force

Copy-Item (Join-Path $repoRoot "config\downloader.ini") (Join-Path $configDir "downloader.ini") -Force

Write-Host "Packaging complete." -ForegroundColor Green
Write-Host "Package Root: $OutputPath"
Write-Host "Downloader:   $moduleOutput"
Write-Host "Config:       $configDir"
Write-Host "Entry:        $([System.IO.Path]::Combine($moduleOutput, 'InvoiceDownloader.exe'))"
