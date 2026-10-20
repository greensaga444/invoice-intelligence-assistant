[CmdletBinding()]
param(
    [string]$PackageRoot,

    [ValidateSet("all", "downloader", "processor", "trainer")]
    [string]$Profile = "all"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot

function Resolve-PackageRoot {
    param(
        [string]$InputPath,
        [string]$RepoPath
    )

    if (-not [string]::IsNullOrWhiteSpace($InputPath)) {
        return [System.IO.Path]::GetFullPath($InputPath)
    }

    $candidates = @(
        (Join-Path $RepoPath "build\deploy\InvoiceIntelligence"),
        (Join-Path $RepoPath "deploy\InvoiceIntelligence"),
        $PWD.Path
    )

    foreach ($candidate in $candidates) {
        if (-not (Test-Path -LiteralPath $candidate -PathType Container)) {
            continue
        }

        $markerPaths = @(
            (Join-Path $candidate "InvoiceLauncher.exe"),
            (Join-Path $candidate "downloader\InvoiceDownloader.exe"),
            (Join-Path $candidate "processor\InvoiceAiProcessor.exe"),
            (Join-Path $candidate "trainer\InvoiceTrainer.exe")
        )

        foreach ($markerPath in $markerPaths) {
            if (Test-Path -LiteralPath $markerPath -PathType Leaf) {
                return [System.IO.Path]::GetFullPath($candidate)
            }
        }
    }

    throw "Unable to resolve package root automatically. Use -PackageRoot to specify the deploy folder."
}

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

function Test-RequiredFile {
    param(
        [System.Collections.Generic.List[object]]$Results,
        [string]$CheckName,
        [string]$FullPath
    )

    if (Test-Path -LiteralPath $FullPath -PathType Leaf) {
        $length = (Get-Item -LiteralPath $FullPath).Length
        Add-Result -Results $Results -Name $CheckName -Passed $true -Detail "Found: $FullPath ($length bytes)"
        return $true
    }

    Add-Result -Results $Results -Name $CheckName -Passed $false -Detail "Missing file: $FullPath"
    return $false
}

function Test-LibsLayout {
    param(
        [System.Collections.Generic.List[object]]$Results,
        [string]$AppName,
        [string]$AppBasePath
    )

    $libsFolderPath = Join-Path $AppBasePath "libs"
    if (-not (Test-Path -LiteralPath $libsFolderPath -PathType Container)) {
        Add-Result -Results $Results -Name ("{0}:libs-folder" -f $AppName) -Passed $false -Detail "Missing libs folder: $libsFolderPath"
        return
    }

    Add-Result -Results $Results -Name ("{0}:libs-folder" -f $AppName) -Passed $true -Detail "Found libs folder: $libsFolderPath"

    $rootDllFiles = @(Get-ChildItem -LiteralPath $AppBasePath -File -Filter "*.dll")
    if ($rootDllFiles.Count -gt 0) {
        $rootDllList = [string]::Join(", ", ($rootDllFiles | Select-Object -ExpandProperty Name))
        Add-Result -Results $Results -Name ("{0}:root-dll-clean" -f $AppName) -Passed $false -Detail "Root folder still contains DLL files: $rootDllList"
        return
    }

    Add-Result -Results $Results -Name ("{0}:root-dll-clean" -f $AppName) -Passed $true -Detail "Root folder has no DLL files."

    $libsDllFiles = @(Get-ChildItem -LiteralPath $libsFolderPath -Recurse -File -Filter "*.dll")
    if ($libsDllFiles.Count -eq 0) {
        Add-Result -Results $Results -Name ("{0}:libs-dll-count" -f $AppName) -Passed $false -Detail "No DLL files found in libs folder."
        return
    }

    Add-Result -Results $Results -Name ("{0}:libs-dll-count" -f $AppName) -Passed $true -Detail "libs contains $($libsDllFiles.Count) DLL files."
}

function Get-RuntimeRequirement {
    param(
        [string]$RuntimeConfigPath
    )

    $runtimeConfig = Get-Content -LiteralPath $RuntimeConfigPath -Raw | ConvertFrom-Json
    $runtimeOptions = $runtimeConfig.runtimeOptions

    $requiredFrameworks = New-Object System.Collections.Generic.List[object]
    $includedFrameworks = New-Object System.Collections.Generic.List[object]

    if ($runtimeOptions.PSObject.Properties.Match("framework").Count -gt 0 -and $null -ne $runtimeOptions.framework) {
        $requiredFrameworks.Add([PSCustomObject]@{
            Name = [string]$runtimeOptions.framework.name
            Version = [string]$runtimeOptions.framework.version
        })
    }

    if ($runtimeOptions.PSObject.Properties.Match("frameworks").Count -gt 0 -and $null -ne $runtimeOptions.frameworks) {
        foreach ($framework in $runtimeOptions.frameworks) {
            $requiredFrameworks.Add([PSCustomObject]@{
                Name = [string]$framework.name
                Version = [string]$framework.version
            })
        }
    }

    if ($runtimeOptions.PSObject.Properties.Match("includedFrameworks").Count -gt 0 -and $null -ne $runtimeOptions.includedFrameworks) {
        foreach ($framework in $runtimeOptions.includedFrameworks) {
            $includedFrameworks.Add([PSCustomObject]@{
                Name = [string]$framework.name
                Version = [string]$framework.version
            })
        }
    }

    return [PSCustomObject]@{
        FrameworkDependent = ($requiredFrameworks.Count -gt 0)
        RequiredFrameworks = $requiredFrameworks
        IncludedFrameworks = $includedFrameworks
    }
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

function Get-TargetApps {
    param(
        [string]$RequestedProfile,
        [string]$ResolvedPackageRoot
    )

    $launcher = [PSCustomObject]@{
        Name = "Launcher"
        BasePath = "."
        Exe = "InvoiceLauncher.exe"
        Dll = "InvoiceLauncher.dll"
        Deps = "InvoiceLauncher.deps.json"
        RuntimeConfig = "InvoiceLauncher.runtimeconfig.json"
    }

    $downloader = [PSCustomObject]@{
        Name = "Downloader"
        BasePath = "downloader"
        Exe = "InvoiceDownloader.exe"
        Dll = "InvoiceDownloader.dll"
        Deps = "InvoiceDownloader.deps.json"
        RuntimeConfig = "InvoiceDownloader.runtimeconfig.json"
    }

    $processor = [PSCustomObject]@{
        Name = "Processor"
        BasePath = "processor"
        Exe = "InvoiceAiProcessor.exe"
        Dll = "InvoiceAiProcessor.dll"
        Deps = "InvoiceAiProcessor.deps.json"
        RuntimeConfig = "InvoiceAiProcessor.runtimeconfig.json"
    }

    $trainer = [PSCustomObject]@{
        Name = "Trainer"
        BasePath = "trainer"
        Exe = "InvoiceTrainer.exe"
        Dll = "InvoiceTrainer.dll"
        Deps = "InvoiceTrainer.deps.json"
        RuntimeConfig = "InvoiceTrainer.runtimeconfig.json"
    }

    switch ($RequestedProfile) {
        "all" { return @($launcher, $downloader, $processor, $trainer) }
        "downloader" { return @($downloader) }
        "processor" { return @($processor) }
        "trainer" { return @($trainer) }
        default { throw "Unsupported profile: $RequestedProfile" }
    }
}

$resolvedPackageRoot = Resolve-PackageRoot -InputPath $PackageRoot -RepoPath $repoRoot
$results = New-Object System.Collections.Generic.List[object]

Write-Host "Environment readiness check" -ForegroundColor Cyan
Write-Host "Package root: $resolvedPackageRoot"
Write-Host "Profile: $Profile"

$isWindows = $env:OS -eq "Windows_NT"
if ($isWindows) {
    Add-Result -Results $results -Name "OS" -Passed $true -Detail "Windows OS detected: $([System.Environment]::OSVersion.VersionString)"
}
else {
    Add-Result -Results $results -Name "OS" -Passed $false -Detail "This package is Windows-targeted and must run on Windows." 
}

if (Test-Path -LiteralPath $resolvedPackageRoot -PathType Container) {
    Add-Result -Results $results -Name "PackageRoot" -Passed $true -Detail "Found deploy folder: $resolvedPackageRoot"
}
else {
    Add-Result -Results $results -Name "PackageRoot" -Passed $false -Detail "Deploy folder does not exist: $resolvedPackageRoot"
}

$targetApps = Get-TargetApps -RequestedProfile $Profile -ResolvedPackageRoot $resolvedPackageRoot

$requiredConfigFiles = switch ($Profile) {
    "all" { @("config\\downloader.ini", "config\\processor.ini") }
    "downloader" { @("config\\downloader.ini") }
    "processor" { @("config\\processor.ini") }
    "trainer" { @("config\\processor.ini") }
    default { @() }
}

foreach ($configRelPath in $requiredConfigFiles) {
    $configPath = Join-Path $resolvedPackageRoot $configRelPath
    [void](Test-RequiredFile -Results $results -CheckName "Config:$configRelPath" -FullPath $configPath)
}

$requiredFrameworks = New-Object System.Collections.Generic.List[object]

foreach ($app in $targetApps) {
    $appBase = if ($app.BasePath -eq ".") { $resolvedPackageRoot } else { Join-Path $resolvedPackageRoot $app.BasePath }
    $libsBase = Join-Path $appBase "libs"

    [void](Test-RequiredFile -Results $results -CheckName "$($app.Name):exe" -FullPath (Join-Path $appBase $app.Exe))

    if (Test-Path -LiteralPath $libsBase -PathType Container) {
        Add-Result -Results $results -Name "$($app.Name):libs-base" -Passed $true -Detail "Found libs folder: $libsBase"
    }
    else {
        Add-Result -Results $results -Name "$($app.Name):libs-base" -Passed $false -Detail "Missing libs folder: $libsBase"
    }

    [void](Test-RequiredFile -Results $results -CheckName "$($app.Name):libs-exe" -FullPath (Join-Path $libsBase $app.Exe))
    [void](Test-RequiredFile -Results $results -CheckName "$($app.Name):libs-dll" -FullPath (Join-Path $libsBase $app.Dll))
    [void](Test-RequiredFile -Results $results -CheckName "$($app.Name):libs-deps" -FullPath (Join-Path $libsBase $app.Deps))

    $runtimeConfigPath = Join-Path $libsBase $app.RuntimeConfig
    if (Test-RequiredFile -Results $results -CheckName "$($app.Name):libs-runtimeconfig" -FullPath $runtimeConfigPath) {
        try {
            $runtimeRequirement = Get-RuntimeRequirement -RuntimeConfigPath $runtimeConfigPath
            if ($runtimeRequirement.FrameworkDependent) {
                foreach ($framework in $runtimeRequirement.RequiredFrameworks) {
                    $requiredFrameworks.Add($framework)
                }

                Add-Result -Results $results -Name "$($app.Name):runtime-mode" -Passed $true -Detail "Framework-dependent publish detected in runtimeconfig."
            }
            elseif ($runtimeRequirement.IncludedFrameworks.Count -gt 0) {
                Add-Result -Results $results -Name "$($app.Name):runtime-mode" -Passed $true -Detail "Self-contained publish detected (includedFrameworks in runtimeconfig)."
            }
            else {
                Add-Result -Results $results -Name "$($app.Name):runtime-mode" -Passed $true -Detail "Runtime mode could not be inferred from runtimeconfig; no framework requirement declared." -Severity "Warning"
            }
        }
        catch {
            Add-Result -Results $results -Name "$($app.Name):runtimeconfig-parse" -Passed $false -Detail "Invalid runtimeconfig JSON: $runtimeConfigPath. $($_.Exception.Message)"
        }
    }

    Test-LibsLayout -Results $results -AppName $app.Name -AppBasePath $appBase
}

if ($requiredFrameworks.Count -gt 0) {
    $installedRuntimes = Get-InstalledRuntimes
    if ($null -eq $installedRuntimes) {
        Add-Result -Results $results -Name "dotnet" -Passed $false -Detail "dotnet command not found. Framework-dependent package requires installed .NET runtimes."
    }
    else {
        Add-Result -Results $results -Name "dotnet" -Passed $true -Detail "dotnet command found; validating required shared runtimes."

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
else {
    Add-Result -Results $results -Name "runtime-prereq" -Passed $true -Detail "No shared runtime prerequisites detected for selected profile."
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
