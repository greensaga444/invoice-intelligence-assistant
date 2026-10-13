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
    $DeployRoot = Join-Path $repoRoot "deploy"
}

$downloaderScript = Join-Path $scriptRoot "publish-downloader.ps1"
$processorScript = Join-Path $scriptRoot "publish-processor.ps1"

if (-not (Test-Path $downloaderScript)) {
    throw "Script not found: $downloaderScript"
}

if (-not (Test-Path $processorScript)) {
    throw "Script not found: $processorScript"
}

$downloaderOutput = Join-Path $DeployRoot "downloader"
$processorOutput = Join-Path $DeployRoot "processor"

$commonParams = @{
    Configuration = $Configuration
    Runtime = $Runtime
}

if ($FrameworkDependent) {
    $commonParams.FrameworkDependent = $true
}

Write-Host "Publishing Downloader package..." -ForegroundColor Cyan
& $downloaderScript @commonParams -OutputPath $downloaderOutput
if ($LASTEXITCODE -ne 0) {
    throw "Downloader publish failed."
}

Write-Host "Publishing Processor package..." -ForegroundColor Cyan
& $processorScript @commonParams -OutputPath $processorOutput
if ($LASTEXITCODE -ne 0) {
    throw "Processor publish failed."
}

if ($SkipZip) {
    Write-Host "Done. Zip packaging skipped." -ForegroundColor Yellow
    Write-Host "Downloader: $downloaderOutput"
    Write-Host "Processor:  $processorOutput"
    return
}

$artifactsDir = Join-Path $DeployRoot "artifacts"
$null = New-Item -ItemType Directory -Path $artifactsDir -Force

$downloaderZip = Join-Path $artifactsDir ("downloader-{0}-{1}.zip" -f $Runtime, $Configuration.ToLowerInvariant())
$processorZip = Join-Path $artifactsDir ("processor-{0}-{1}.zip" -f $Runtime, $Configuration.ToLowerInvariant())

if (Test-Path $downloaderZip) {
    Remove-Item $downloaderZip -Force
}

if (Test-Path $processorZip) {
    Remove-Item $processorZip -Force
}

Compress-Archive -Path (Join-Path $downloaderOutput "*") -DestinationPath $downloaderZip -Force
Compress-Archive -Path (Join-Path $processorOutput "*") -DestinationPath $processorZip -Force

Write-Host "Done. Packages and zip artifacts are ready." -ForegroundColor Green
Write-Host "Downloader: $downloaderOutput"
Write-Host "Processor:  $processorOutput"
Write-Host "Zip A:      $downloaderZip"
Write-Host "Zip B:      $processorZip"
