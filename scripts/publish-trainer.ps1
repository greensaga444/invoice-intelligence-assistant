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

$moduleOutput = Join-Path $OutputPath "trainer"

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

$projectPath = Join-Path $repoRoot "src\InvoiceTrainer\InvoiceTrainer.csproj"
if (-not (Test-Path $projectPath)) {
    throw "Project file not found: $projectPath"
}

$bootstrapperProjectPath = Join-Path $repoRoot "src\InvoiceAppBootstrapper\InvoiceAppBootstrapper.csproj"
if (-not (Test-Path $bootstrapperProjectPath)) {
    throw "Project file not found: $bootstrapperProjectPath"
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

if (Test-Path -LiteralPath $moduleOutput -PathType Container) {
    Remove-Item -LiteralPath $moduleOutput -Recurse -Force
}

Write-Host "Publishing InvoiceTrainer..." -ForegroundColor Cyan
& $dotnetPath publish $projectPath -c $Configuration -r $Runtime --self-contained $selfContained -o $moduleOutput
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed for InvoiceTrainer."
}

$bootstrapperExecutablePath = Publish-BootstrapperBinary -PublishRuntime $Runtime -PublishConfiguration $Configuration -PublishSelfContained $selfContained
$trainerLibsPath = Convert-ToBootstrapLayout -ApplicationOutputPath $moduleOutput -ApplicationExecutableName "InvoiceTrainer.exe" -BootstrapperExecutablePath $bootstrapperExecutablePath
$trainerLibsDllCount = @(Get-ChildItem -LiteralPath $trainerLibsPath -Recurse -File -Filter "*.dll").Count
Write-Host ("Trainer runtime relocated to {0} ({1} dll files under libs)." -f $trainerLibsPath, $trainerLibsDllCount)

$configDir = Join-Path $OutputPath "config"

$null = New-Item -ItemType Directory -Path $configDir -Force

Copy-Item (Join-Path $repoRoot "config\processor.ini") (Join-Path $configDir "processor.ini") -Force
Copy-Item $readinessScriptSource $readinessScriptTarget -Force

Write-Host "Running customer environment readiness check..." -ForegroundColor Cyan
& $powershellPath -NoProfile -ExecutionPolicy Bypass -File $readinessScriptTarget -PackageRoot $OutputPath -Profile trainer
if ($LASTEXITCODE -ne 0) {
    throw "Customer environment readiness check failed for trainer package."
}

Write-Host "Packaging complete." -ForegroundColor Green
Write-Host "Package Root: $OutputPath"
Write-Host "Trainer:      $moduleOutput"
Write-Host "Runtime libs: $trainerLibsPath"
Write-Host "Config:       $configDir"
Write-Host "Env Check:    $readinessScriptTarget"
Write-Host "Entry:        $([System.IO.Path]::Combine($moduleOutput, 'InvoiceTrainer.exe'))"
