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

$moduleOutput = Join-Path $OutputPath "processor"

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

$projectPath = Join-Path $repoRoot "src\InvoiceAiProcessor\InvoiceAiProcessor.csproj"
if (-not (Test-Path $projectPath)) {
    throw "Project file not found: $projectPath"
}

$readinessScriptSource = Join-Path $scriptRoot "check-customer-environment.ps1"
if (-not (Test-Path $readinessScriptSource)) {
    throw "Script not found: $readinessScriptSource"
}

$readinessScriptTarget = Join-Path $OutputPath "check-customer-environment.ps1"
$powershellPath = Join-Path $PSHOME "powershell.exe"
if (-not (Test-Path $powershellPath)) {
    $powershellPath = "powershell.exe"
}

$selfContained = if ($FrameworkDependent) { "false" } else { "true" }

Write-Host "Publishing InvoiceAiProcessor..." -ForegroundColor Cyan
& $dotnetPath publish $projectPath -c $Configuration -r $Runtime --self-contained $selfContained -o $moduleOutput
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed for InvoiceAiProcessor."
}

$configDir = Join-Path $OutputPath "config"

$null = New-Item -ItemType Directory -Path $configDir -Force

Copy-Item (Join-Path $repoRoot "config\processor.ini") (Join-Path $configDir "processor.ini") -Force
Copy-Item $readinessScriptSource $readinessScriptTarget -Force

Write-Host "Running customer environment readiness check..." -ForegroundColor Cyan
& $powershellPath -NoProfile -ExecutionPolicy Bypass -File $readinessScriptTarget -PackageRoot $OutputPath -Profile processor
if ($LASTEXITCODE -ne 0) {
    throw "Customer environment readiness check failed for processor package."
}

Write-Host "Packaging complete." -ForegroundColor Green
Write-Host "Package Root: $OutputPath"
Write-Host "Processor:    $moduleOutput"
Write-Host "Config:       $configDir"
Write-Host "Env Check:    $readinessScriptTarget"
Write-Host "Entry:        $([System.IO.Path]::Combine($moduleOutput, 'InvoiceAiProcessor.exe'))"
Write-Host "Note:         Put source invoice files into the Folder A path defined in config/processor.ini before running the processor."
