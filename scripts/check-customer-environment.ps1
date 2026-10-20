[CmdletBinding()]
param(
    [ValidateSet("all", "downloader", "processor", "trainer")]
    [string]$Profile = "all",

    [ValidateSet("framework-dependent", "self-contained")]
    [string]$PublishMode = "framework-dependent",

    [ValidateSet("win-x64", "win-x86", "win-arm64")]
    [string]$Runtime = "win-x64",

    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$RequiredRuntimeVersion = "8.0.0",

    [switch]$SkipDotnetRuntimeCheck
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Add-Result {
    param(
        [System.Collections.Generic.List[object]]$Results,
        [string]$Name,
        [bool]$Passed,
        [string]$Detail,
        [ValidateSet("Error", "Warning", "Info")]
        [string]$Severity = "Error"
    )

    $Results.Add([PSCustomObject]@{
        Name = $Name
        Passed = $Passed
        Detail = $Detail
        Severity = $Severity
    })
}

function Get-InstalledRuntimes {
    $dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($null -eq $dotnetCommand) {
        return $null
    }

    $runtimeMap = @{}
    $runtimeLines = & $dotnetCommand.Source --list-runtimes 2>$null
    foreach ($line in $runtimeLines) {
        if ($line -notmatch '^(?<name>\S+)\s+(?<version>\d+\.\d+\.\d+)\s+\[') {
            continue
        }

        $name = $Matches.name
        $version = [Version]$Matches.version

        if (-not $runtimeMap.ContainsKey($name)) {
            $runtimeMap[$name] = New-Object System.Collections.Generic.List[Version]
        }

        $runtimeMap[$name].Add($version)
    }

    return $runtimeMap
}

function Test-FrameworkInstalled {
    param(
        [hashtable]$RuntimeMap,
        [string]$FrameworkName,
        [string]$FrameworkVersion
    )

    if (-not $RuntimeMap.ContainsKey($FrameworkName)) {
        return $false
    }

    $requiredVersion = [Version]$FrameworkVersion
    foreach ($installedVersion in $RuntimeMap[$FrameworkName]) {
        if ($installedVersion.Major -ne $requiredVersion.Major) {
            continue
        }

        if ($installedVersion.Minor -ne $requiredVersion.Minor) {
            continue
        }

        if ($installedVersion -ge $requiredVersion) {
            return $true
        }
    }

    return $false
}

function Get-RequiredFrameworks {
    param(
        [string]$RequestedPublishMode,
        [string]$MinimumRuntimeVersion
    )

    if ($RequestedPublishMode -eq "self-contained") {
        return @()
    }

    return @(
        [PSCustomObject]@{ Name = "Microsoft.NETCore.App"; Version = $MinimumRuntimeVersion },
        [PSCustomObject]@{ Name = "Microsoft.WindowsDesktop.App"; Version = $MinimumRuntimeVersion }
    )
}

function Test-RuntimeArchitecture {
    param(
        [string]$RequestedRuntime
    )

    $is64BitOs = [System.Environment]::Is64BitOperatingSystem
    $arch = $env:PROCESSOR_ARCHITECTURE

    switch ($RequestedRuntime) {
        "win-x64" {
            if (-not $is64BitOs) {
                return [PSCustomObject]@{ Passed = $false; Detail = "win-x64 package requires a 64-bit Windows OS." }
            }

            return [PSCustomObject]@{ Passed = $true; Detail = "64-bit Windows OS detected for win-x64 package." }
        }
        "win-x86" {
            return [PSCustomObject]@{ Passed = $true; Detail = "win-x86 package can run on both x86 and x64 Windows." }
        }
        "win-arm64" {
            if ($arch -eq "ARM64") {
                return [PSCustomObject]@{ Passed = $true; Detail = "ARM64 Windows detected for win-arm64 package." }
            }

            return [PSCustomObject]@{ Passed = $false; Detail = "win-arm64 package requires ARM64 Windows." }
        }
        default {
            return [PSCustomObject]@{ Passed = $false; Detail = "Unsupported runtime value: $RequestedRuntime" }
        }
    }
}

$results = New-Object System.Collections.Generic.List[object]

Write-Host "Environment readiness check" -ForegroundColor Cyan
Write-Host "Host: $env:COMPUTERNAME"
Write-Host "Profile: $Profile"
Write-Host "Publish mode: $PublishMode"
Write-Host "Runtime target: $Runtime"

$isWindows = $env:OS -eq "Windows_NT"
if ($isWindows) {
    Add-Result -Results $results -Name "OS" -Passed $true -Detail "Windows OS detected: $([System.Environment]::OSVersion.VersionString)"
}
else {
    Add-Result -Results $results -Name "OS" -Passed $false -Detail "This package is Windows-targeted and must run on Windows."
}

if ($isWindows) {
    $archCheck = Test-RuntimeArchitecture -RequestedRuntime $Runtime
    Add-Result -Results $results -Name "OSArchitecture" -Passed $archCheck.Passed -Detail $archCheck.Detail
}

if ($PSVersionTable.PSVersion.Major -ge 5) {
    Add-Result -Results $results -Name "PowerShell" -Passed $true -Detail "PowerShell version $($PSVersionTable.PSVersion) detected."
}
else {
    Add-Result -Results $results -Name "PowerShell" -Passed $false -Detail "PowerShell 5.1 or newer is required."
}

if ($SkipDotnetRuntimeCheck) {
    Add-Result -Results $results -Name "runtime-prereq" -Passed $true -Detail "Runtime checks were skipped by parameter."
}
elseif ($PublishMode -eq "self-contained") {
    Add-Result -Results $results -Name "runtime-prereq" -Passed $true -Detail "Self-contained package selected. Shared .NET runtime is not required."
}
else {
    $requiredFrameworks = Get-RequiredFrameworks -RequestedPublishMode $PublishMode -MinimumRuntimeVersion $RequiredRuntimeVersion
    $installedRuntimes = Get-InstalledRuntimes

    if ($null -eq $installedRuntimes) {
        Add-Result -Results $results -Name "dotnet" -Passed $false -Detail "dotnet command not found. Install .NET Desktop Runtime 8 (x64) and retry."
    }
    else {
        Add-Result -Results $results -Name "dotnet" -Passed $true -Detail "dotnet command found; validating required shared runtimes for framework-dependent package."

        foreach ($framework in $requiredFrameworks) {
            $isInstalled = Test-FrameworkInstalled -RuntimeMap $installedRuntimes -FrameworkName $framework.Name -FrameworkVersion $framework.Version
            if ($isInstalled) {
                Add-Result -Results $results -Name "runtime:$($framework.Name)" -Passed $true -Detail "Found compatible runtime for $($framework.Name) $($framework.Version)."
            }
            else {
                Add-Result -Results $results -Name "runtime:$($framework.Name)" -Passed $false -Detail "Missing compatible runtime for $($framework.Name) $($framework.Version)."
            }
        }
    }
}

Write-Host ""
Write-Host "Check results:" -ForegroundColor Cyan
foreach ($result in $results) {
    $statusLabel = if ($result.Passed) { "PASS" } else { if ($result.Severity -eq "Warning") { "WARN" } else { "FAIL" } }
    $color = if ($statusLabel -eq "PASS") { "Green" } elseif ($statusLabel -eq "WARN") { "Yellow" } else { "Red" }
    Write-Host ("[{0}] {1} - {2}" -f $statusLabel, $result.Name, $result.Detail) -ForegroundColor $color
}

$failureCount = @($results | Where-Object { -not $_.Passed -and $_.Severity -eq "Error" }).Count
$warningCount = @($results | Where-Object { $_.Severity -eq "Warning" }).Count

Write-Host ""
if ($failureCount -eq 0) {
    Write-Host "Environment status: READY" -ForegroundColor Green
    Write-Host ("Summary: 0 failures, {0} warnings." -f $warningCount)
    exit 0
}

Write-Host "Environment status: NOT READY" -ForegroundColor Red
Write-Host ("Summary: {0} failures, {1} warnings." -f $failureCount, $warningCount)
exit 1
