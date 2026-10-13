[CmdletBinding()]
param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",

    [ValidateSet("win-x64", "win-x86", "win-arm64")]
    [string]$Runtime = "win-x64",

    [switch]$FrameworkDependent,

    [string]$DeployRoot,

    [switch]$SkipZip
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot

if ([string]::IsNullOrWhiteSpace($DeployRoot)) {
    $DeployRoot = Join-Path $repoRoot "build\deploy"
}

$downloaderScript = Join-Path $scriptRoot "publish-downloader.ps1"
$processorScript = Join-Path $scriptRoot "publish-processor.ps1"

if (-not (Test-Path $downloaderScript)) {
    throw "Script not found: $downloaderScript"
}

if (-not (Test-Path $processorScript)) {
    throw "Script not found: $processorScript"
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

$launcherProjectPath = Join-Path $repoRoot "src\InvoiceLauncher\InvoiceLauncher.csproj"
if (-not (Test-Path $launcherProjectPath)) {
    throw "Project file not found: $launcherProjectPath"
}

$packageRoot = Join-Path $DeployRoot "InvoiceIntelligence"
$downloaderOutput = Join-Path $packageRoot "downloader"
$processorOutput = Join-Path $packageRoot "processor"
$configOutput = Join-Path $packageRoot "config"

$selfContained = if ($FrameworkDependent) { "false" } else { "true" }

$commonParams = @{
    Configuration = $Configuration
    Runtime = $Runtime
}

if ($FrameworkDependent) {
    $commonParams.FrameworkDependent = $true
}

Write-Host "Publishing Main UI package..." -ForegroundColor Cyan
& $dotnetPath publish $launcherProjectPath -c $Configuration -r $Runtime --self-contained $selfContained -o $packageRoot
if ($LASTEXITCODE -ne 0) {
    throw "Main UI publish failed."
}

$null = New-Item -ItemType Directory -Path $configOutput -Force
Copy-Item (Join-Path $repoRoot "config\downloader.ini") (Join-Path $configOutput "downloader.ini") -Force
Copy-Item (Join-Path $repoRoot "config\processor.ini") (Join-Path $configOutput "processor.ini") -Force

Write-Host "Publishing Downloader package..." -ForegroundColor Cyan
& $downloaderScript @commonParams -OutputPath $packageRoot
if ($LASTEXITCODE -ne 0) {
    throw "Downloader publish failed."
}

Write-Host "Publishing Processor package..." -ForegroundColor Cyan
& $processorScript @commonParams -OutputPath $packageRoot
if ($LASTEXITCODE -ne 0) {
    throw "Processor publish failed."
}

if ($SkipZip) {
    Write-Host "Done. Zip packaging skipped." -ForegroundColor Yellow
    Write-Host "Package:    $packageRoot"
    Write-Host "Downloader: $downloaderOutput"
    Write-Host "Processor:  $processorOutput"
    Write-Host "Config:     $configOutput"
    return
}

$artifactsDir = Join-Path $DeployRoot "artifacts"
$null = New-Item -ItemType Directory -Path $artifactsDir -Force

$packageZip = Join-Path $artifactsDir ("InvoiceIntelligence-{0}-{1}.zip" -f $Runtime, $Configuration.ToLowerInvariant())

if (Test-Path $packageZip) {
    Remove-Item $packageZip -Force
}

Compress-Archive -Path (Join-Path $packageRoot "*") -DestinationPath $packageZip -Force

Write-Host "Done. Packages and zip artifacts are ready." -ForegroundColor Green
Write-Host "Package:    $packageRoot"
Write-Host "Downloader: $downloaderOutput"
Write-Host "Processor:  $processorOutput"
Write-Host "Config:     $configOutput"
Write-Host "Zip:        $packageZip"
