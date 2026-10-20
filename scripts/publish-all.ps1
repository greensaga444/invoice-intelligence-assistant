[CmdletBinding()]
param(
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",

    [ValidateSet("win-x64", "win-x86", "win-arm64")]
    [string]$Runtime = "win-x64",

    [switch]$SelfContained,

    [switch]$FrameworkDependent,

    [string]$DeployRoot,

    [switch]$SkipZip,

    [switch]$SkipDeveloperEnvironmentCheck
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
$trainerScript = Join-Path $scriptRoot "publish-trainer.ps1"
$readinessScriptSource = Join-Path $scriptRoot "check-customer-environment.ps1"
$developerReadinessScript = Join-Path $scriptRoot "check-developer-environment.ps1"

if (-not (Test-Path $downloaderScript)) {
    throw "Script not found: $downloaderScript"
}

if (-not (Test-Path $processorScript)) {
    throw "Script not found: $processorScript"
}

if (-not (Test-Path $trainerScript)) {
    throw "Script not found: $trainerScript"
}

if (-not (Test-Path $readinessScriptSource)) {
    throw "Script not found: $readinessScriptSource"
}

if (-not (Test-Path $developerReadinessScript)) {
    throw "Script not found: $developerReadinessScript"
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

$bootstrapperProjectPath = Join-Path $repoRoot "src\InvoiceAppBootstrapper\InvoiceAppBootstrapper.csproj"
if (-not (Test-Path $bootstrapperProjectPath)) {
    throw "Project file not found: $bootstrapperProjectPath"
}

$packageRoot = Join-Path $DeployRoot "InvoiceIntelligence"
$downloaderOutput = Join-Path $packageRoot "downloader"
$processorOutput = Join-Path $packageRoot "processor"
$trainerOutput = Join-Path $packageRoot "trainer"
$configOutput = Join-Path $packageRoot "config"
$launcherLibsOutput = Join-Path $packageRoot "libs"
$downloaderLibsOutput = Join-Path $downloaderOutput "libs"
$processorLibsOutput = Join-Path $processorOutput "libs"
$trainerLibsOutput = Join-Path $trainerOutput "libs"
$readinessScriptTarget = Join-Path $packageRoot "check-customer-environment.ps1"
$powershellPath = Join-Path $PSHOME "powershell.exe"
if (-not (Test-Path $powershellPath)) {
    $powershellPath = "powershell.exe"
}

if ($SelfContained -and $FrameworkDependent) {
    throw "Parameters -SelfContained and -FrameworkDependent cannot be used together."
}

$selfContainedMode = if ($SelfContained) { "true" } else { "false" }

if (-not $SkipDeveloperEnvironmentCheck) {
    Write-Host "Running developer build readiness check..." -ForegroundColor Cyan
    & $powershellPath -NoProfile -ExecutionPolicy Bypass -File $developerReadinessScript -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "Developer environment readiness check failed."
    }
}

function Publish-BootstrapperBinary {
    param(
        [string]$PublishRuntime,
        [string]$PublishConfiguration,
        [string]$PublishSelfContained
    )

    $bootstrapperOutputPath = Join-Path ([System.IO.Path]::GetTempPath()) ("invoice-bootstrapper-{0}" -f [System.Guid]::NewGuid().ToString("N"))
    $null = New-Item -ItemType Directory -Path $bootstrapperOutputPath -Force

    & $dotnetPath publish $bootstrapperProjectPath -c $PublishConfiguration -r $PublishRuntime --self-contained $PublishSelfContained -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o $bootstrapperOutputPath | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for InvoiceAppBootstrapper."
    }

    $bootstrapperExecutablePath = Join-Path $bootstrapperOutputPath "InvoiceAppBootstrapper.exe"
    if (-not (Test-Path -LiteralPath $bootstrapperExecutablePath -PathType Leaf)) {
        throw "Bootstrapper executable not found: $bootstrapperExecutablePath"
    }

    return $bootstrapperExecutablePath
}

function Convert-ToBootstrapLayout {
    param(
        [string]$ApplicationOutputPath,
        [string]$ApplicationExecutableName,
        [string]$BootstrapperExecutablePath
    )

    if (-not (Test-Path -LiteralPath $ApplicationOutputPath -PathType Container)) {
        throw "Application output path not found: $ApplicationOutputPath"
    }

    $applicationExecutablePath = Join-Path $ApplicationOutputPath $ApplicationExecutableName
    if (-not (Test-Path -LiteralPath $applicationExecutablePath -PathType Leaf)) {
        throw "Application executable not found: $applicationExecutablePath"
    }

    $libsOutputPath = Join-Path $ApplicationOutputPath "libs"
    if (Test-Path -LiteralPath $libsOutputPath -PathType Container) {
        Remove-Item -LiteralPath $libsOutputPath -Recurse -Force
    }

    $null = New-Item -ItemType Directory -Path $libsOutputPath -Force

    $topLevelItems = @(Get-ChildItem -LiteralPath $ApplicationOutputPath -Force)
    foreach ($topLevelItem in $topLevelItems) {
        if ($topLevelItem.Name -eq "libs") {
            continue
        }

        Move-Item -LiteralPath $topLevelItem.FullName -Destination (Join-Path $libsOutputPath $topLevelItem.Name) -Force
    }

    Copy-Item -LiteralPath $BootstrapperExecutablePath -Destination (Join-Path $ApplicationOutputPath $ApplicationExecutableName) -Force

    return $libsOutputPath
}

function Remove-DirectorySafely {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        return
    }

    $lastError = $null
    for ($attempt = 1; $attempt -le 3; $attempt++) {
        try {
            if (Test-Path -LiteralPath $Path -PathType Container) {
                Get-ChildItem -LiteralPath $Path -Recurse -Force -ErrorAction SilentlyContinue | ForEach-Object {
                    if ($_.Attributes -band [IO.FileAttributes]::ReadOnly) {
                        $_.Attributes = $_.Attributes -bxor [IO.FileAttributes]::ReadOnly
                    }
                }

                $rootItem = Get-Item -LiteralPath $Path -Force -ErrorAction SilentlyContinue
                if ($null -ne $rootItem -and ($rootItem.Attributes -band [IO.FileAttributes]::ReadOnly)) {
                    $rootItem.Attributes = $rootItem.Attributes -bxor [IO.FileAttributes]::ReadOnly
                }

                Remove-Item -LiteralPath $Path -Recurse -Force -ErrorAction Stop
            }

            return
        }
        catch {
            $lastError = $_
            if ($attempt -lt 3) {
                Start-Sleep -Milliseconds 400
            }
        }
    }

    throw "Unable to clean deploy folder '$Path'. Close running InvoiceLauncher/Downloader/Processor/Trainer processes and any open Explorer window under this folder, then retry. Details: $($lastError.Exception.Message)"
}

$commonParams = @{
    Configuration = $Configuration
    Runtime = $Runtime
}

$commonParams.SkipDeveloperEnvironmentCheck = $true

if ($SelfContained) {
    $commonParams.SelfContained = $true
}

if (Test-Path -LiteralPath $packageRoot -PathType Container) {
    Remove-DirectorySafely -Path $packageRoot
}

$null = New-Item -ItemType Directory -Path $packageRoot -Force

Write-Host "Publishing Main UI package..." -ForegroundColor Cyan
& $dotnetPath publish $launcherProjectPath -c $Configuration -r $Runtime --self-contained $selfContainedMode -o $packageRoot
if ($LASTEXITCODE -ne 0) {
    throw "Main UI publish failed."
}

$bootstrapperExecutablePath = Publish-BootstrapperBinary -PublishRuntime $Runtime -PublishConfiguration $Configuration -PublishSelfContained $selfContainedMode
$launcherRuntimePath = Convert-ToBootstrapLayout -ApplicationOutputPath $packageRoot -ApplicationExecutableName "InvoiceLauncher.exe" -BootstrapperExecutablePath $bootstrapperExecutablePath
$launcherRuntimeDllCount = @(Get-ChildItem -LiteralPath $launcherRuntimePath -Recurse -File -Filter "*.dll").Count
Write-Host ("Launcher runtime relocated to {0} ({1} dll files under libs)." -f $launcherRuntimePath, $launcherRuntimeDllCount)

$null = New-Item -ItemType Directory -Path $configOutput -Force
Copy-Item (Join-Path $repoRoot "config\downloader.ini") (Join-Path $configOutput "downloader.ini") -Force
Copy-Item (Join-Path $repoRoot "config\processor.ini") (Join-Path $configOutput "processor.ini") -Force
Copy-Item $readinessScriptSource $readinessScriptTarget -Force

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

Write-Host "Publishing Trainer package..." -ForegroundColor Cyan
& $trainerScript @commonParams -OutputPath $packageRoot
if ($LASTEXITCODE -ne 0) {
    throw "Trainer publish failed."
}

if ($SkipZip) {
    Write-Host "Done. Zip packaging skipped." -ForegroundColor Yellow
    Write-Host "Package:    $packageRoot"
    Write-Host "Downloader: $downloaderOutput"
    Write-Host "Processor:  $processorOutput"
    Write-Host "Trainer:    $trainerOutput"
    Write-Host "Launcher runtime:   $launcherLibsOutput"
    Write-Host "Downloader runtime: $downloaderLibsOutput"
    Write-Host "Processor runtime:  $processorLibsOutput"
    Write-Host "Trainer runtime:    $trainerLibsOutput"
    Write-Host "Config:     $configOutput"
    Write-Host "Env Check:  $readinessScriptTarget"
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
Write-Host "Trainer:    $trainerOutput"
Write-Host "Launcher runtime:   $launcherLibsOutput"
Write-Host "Downloader runtime: $downloaderLibsOutput"
Write-Host "Processor runtime:  $processorLibsOutput"
Write-Host "Trainer runtime:    $trainerLibsOutput"
Write-Host "Config:     $configOutput"
Write-Host "Env Check:  $readinessScriptTarget"
Write-Host "Zip:        $packageZip"
