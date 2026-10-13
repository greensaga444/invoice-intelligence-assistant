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
    $OutputPath = Join-Path $repoRoot "deploy\downloader"
}

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
& $dotnetPath publish $projectPath -c $Configuration -r $Runtime --self-contained $selfContained -o $OutputPath
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed for InvoiceDownloader."
}

$configDir = Join-Path $OutputPath "config"
$dataFolderA = Join-Path $OutputPath "data\folder-a"
$dataFolderB = Join-Path $OutputPath "data\folder-b"

$null = New-Item -ItemType Directory -Path $configDir -Force
$null = New-Item -ItemType Directory -Path $dataFolderA -Force
$null = New-Item -ItemType Directory -Path $dataFolderB -Force

Copy-Item (Join-Path $repoRoot "config\downloader.ini") (Join-Path $configDir "downloader.ini") -Force
Copy-Item (Join-Path $repoRoot "config\processor.ini") (Join-Path $configDir "processor.ini") -Force

Write-Host "Packaging complete." -ForegroundColor Green
Write-Host "Output: $OutputPath"
Write-Host "Entry:  $([System.IO.Path]::Combine($OutputPath, 'InvoiceDownloader.exe'))"
Write-Host "Note:   config/processor.ini is also copied for the unified settings UI sync scenario."
