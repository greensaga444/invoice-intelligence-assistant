[CmdletBinding()]
param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot

function Resolve-DotnetPath {
    $dotnetPath = (Get-Command dotnet -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -ErrorAction SilentlyContinue)
    if ($dotnetPath) {
        return $dotnetPath
    }

    $fallbackDotnetPath = "C:\PROGRA~1\dotnet\dotnet.exe"
    if (Test-Path -LiteralPath $fallbackDotnetPath -PathType Leaf) {
        return $fallbackDotnetPath
    }

    throw "Cannot find dotnet executable. Install .NET SDK 8+ and ensure dotnet is on PATH."
}

function Invoke-ExternalCommand {
    param(
        [string]$Executable,
        [string[]]$Arguments,
        [string]$Description
    )

    Write-Host ""
    Write-Host "==> $Description" -ForegroundColor Cyan
    Write-Host "$Executable $($Arguments -join ' ')" -ForegroundColor DarkGray

    & $Executable @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Description failed with exit code $LASTEXITCODE."
    }
}

function Assert-FileExists {
    param(
        [string]$Path,
        [string]$Name
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Missing required file ($Name): $Path"
    }
}

$solutionPath = Join-Path $repoRoot "InvoiceIntelligenceAssistant.slnx"

try {
    Write-Host "Developer build readiness verification" -ForegroundColor Green
    Write-Host "Repository: $repoRoot"
    Write-Host "Configuration: $Configuration"

    if ($env:OS -ne "Windows_NT") {
        throw "This repository contains Windows desktop projects and must be validated on Windows."
    }

    Assert-FileExists -Path $solutionPath -Name "Solution"

    $dotnetPath = Resolve-DotnetPath

    $sdkLines = & $dotnetPath --list-sdks
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet --list-sdks failed."
    }

    $hasSupportedSdk = $false
    foreach ($line in $sdkLines) {
        if ($line -notmatch '^(?<version>\d+\.\d+\.\d+)') {
            continue
        }

        $sdkVersion = [Version]$Matches.version
        if ($sdkVersion.Major -ge 8) {
            $hasSupportedSdk = $true
            break
        }
    }

    if (-not $hasSupportedSdk) {
        throw "No .NET SDK 8+ found. Installed SDKs: $($sdkLines -join '; ')"
    }

    Invoke-ExternalCommand -Executable $dotnetPath -Arguments @("restore", $solutionPath, "-v", "minimal") -Description "Restore solution"
    Invoke-ExternalCommand -Executable $dotnetPath -Arguments @("build", $solutionPath, "-c", $Configuration, "--no-restore", "-v", "minimal") -Description "Build solution"

    Write-Host ""
    Write-Host "Developer build readiness status: READY" -ForegroundColor Green
    Write-Host ".NET SDK check: PASS (8+)"
    Write-Host "You can restore and build this repository on the current machine."
    exit 0
}
catch {
    Write-Host ""
    Write-Host "Developer build readiness status: NOT READY" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
