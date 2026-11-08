#Requires -Version 7.6
#Requires -PSEdition Core
<#
.SYNOPSIS
    Build driver for MD.Viewer.Windows.

.DESCRIPTION
    Wraps the .NET 11 (C# 15) toolchain with the environment quirks of this
    workspace already solved:

      * the `dotnet` on PATH is a bare *runtime* shipped with the host app and
        cannot build anything — the SDK is resolved explicitly;
      * the .NET 11 SDK's NuGet layer aborts with
        "Value cannot be null (Parameter 'path1')" when the APPDATA /
        LOCALAPPDATA environment variables are absent from the session.

.PARAMETER Task
    build | test | publish | run | clean | shots | all  (default: build)

.EXAMPLE
    pwsh -File build\build.ps1 -Task all -Configuration Release
#>
[CmdletBinding()]
param(
    [ValidateSet('build', 'test', 'publish', 'run', 'shots', 'clean', 'all')]
    [string]$Task = 'build',

    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [string]$DotNetRoot
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$OutputEncoding = [Console]::InputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path (Join-Path $repoRoot 'global.json'))) { $repoRoot = (Get-Location).Path }

# ---------------------------------------------------------------- environment
# The host session does not define these; NuGet and the CLR need them.
$script:envFixed = @{}
foreach ($name in 'APPDATA', 'LOCALAPPDATA', 'HOME') {
    if (-not [Environment]::GetEnvironmentVariable($name)) {
        $folder = switch ($name) {
            'APPDATA' { 'ApplicationData' }
            'LOCALAPPDATA' { 'LocalApplicationData' }
            'HOME' { 'UserProfile' }
        }
        $value = [Environment]::GetFolderPath($folder)
        [Environment]::SetEnvironmentVariable($name, $value)
        $script:envFixed[$name] = $value
    }
}
if (-not $env:DOTNET_CLI_HOME) { $env:DOTNET_CLI_HOME = [Environment]::GetFolderPath('UserProfile') }
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'

function Resolve-DotNet {
    param([string]$Hint)

    $candidates = [System.Collections.Generic.List[string]]::new()
    if ($Hint) { $candidates.Add((Join-Path $Hint 'dotnet.exe')) }
    $candidates.Add('C:\GIT\_tools\dotnet11\dotnet.exe')
    $candidates.Add('C:\Program Files\dotnet\dotnet.exe')

    foreach ($candidate in $candidates) {
        if (-not (Test-Path $candidate)) { continue }
        $sdks = & $candidate --list-sdks 2>$null
        if ($sdks -match '^11\.') { return $candidate }
    }

    $onPath = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($onPath) {
        $sdks = & $onPath.Source --list-sdks 2>$null
        if ($sdks -match '^11\.') { return $onPath.Source }
        $found = ($sdks -join '; ')
        throw ".NET 11 SDK not found. Only these SDKs are visible: $found. Install .NET 11 (C# 15) or pass -DotNetRoot."
    }
    throw '.NET 11 SDK not found. Install it (see docs/BUILD.md) or pass -DotNetRoot.'
}

$dotnet = Resolve-DotNet -Hint $DotNetRoot
Write-Host "dotnet   : $dotnet" -ForegroundColor DarkGray
$script:dotnetRootDir = Split-Path -Parent $dotnet
# The host session pins DOTNET_ROOT to an unrelated bare runtime; an apphost
# started from here would look for .NET 11 there and refuse to run.
$env:DOTNET_ROOT = $script:dotnetRootDir
$env:DOTNET_MULTILEVEL_LOOKUP = '0'
Write-Host "config   : $Configuration" -ForegroundColor DarkGray
Write-Host "repo     : $repoRoot" -ForegroundColor DarkGray

$sln = Join-Path $repoRoot 'MD.Viewer.Windows.slnx'
$appProj = Join-Path $repoRoot 'src\MdViewer.App\MdViewer.App.csproj'
$testProj = Join-Path $repoRoot 'src\MdViewer.Tests\MdViewer.Tests.csproj'
$publishDir = Join-Path $repoRoot 'artifacts\publish'
$shotScript = Join-Path $repoRoot 'build\screenshots.ps1'

function Invoke-DotNet {
    param([string[]]$Arguments, [switch]$AllowFailure)
    $output = & $dotnet @Arguments 2>&1
    $code = $LASTEXITCODE
    foreach ($item in $output) {
        $line = "$item"
        if ($line -match ': error ') { Write-Host $line -ForegroundColor Red }
        elseif ($line -match ': warning ') { Write-Host $line -ForegroundColor Yellow }
    }
    if (-not $AllowFailure -and $code -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $code"
    }
    return $code
}

function Build-Solution {
    Write-Host '» build' -ForegroundColor Cyan
    $common = @($sln, '-c', $Configuration, '-v', 'minimal', '--nologo')
    Invoke-DotNet -Arguments (@('build') + $common) | Out-Null
}

function Test-All {
    Write-Host '» test' -ForegroundColor Cyan
    Invoke-DotNet -Arguments @('build', $testProj, '-c', $Configuration, '-v', 'quiet', '--nologo') | Out-Null
    $exe = Join-Path $repoRoot "src\MdViewer.Tests\bin\$Configuration\net11.0-windows\mdview-tests.exe"
    if (-not (Test-Path $exe)) { throw "test runner not found: $exe" }
    & $exe --verbose
    if ($LASTEXITCODE -ne 0) { throw "tests failed (exit $LASTEXITCODE)" }
}

function Publish-App {
    Write-Host '» publish (self-contained single file)' -ForegroundColor Cyan
    if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }
    Invoke-DotNet -Arguments @(
        'publish', $appProj,
        '-c', 'Release',
        '-r', 'win-x64',
        '--self-contained', 'true',
        '-p:PublishSingleFile=true',
        '-p:IncludeNativeLibrariesForSelfExtract=true',
        '-p:EnableCompressionInSingleFile=true',
        '-p:DebugType=none',
        '-p:GenerateDocumentationFile=false',
        '-o', $publishDir,
        '-v', 'minimal', '--nologo'
    ) | Out-Null
    $exe = Join-Path $publishDir 'mdview.exe'
    if (-not (Test-Path $exe)) { throw "publish produced no mdview.exe in $publishDir" }
    $size = [math]::Round((Get-Item $exe).Length / 1MB, 1)
    Write-Host "  mdview.exe: $size MB" -ForegroundColor Green
}

function Invoke-App {
    Write-Host '» run' -ForegroundColor Cyan
    $exe = Join-Path $repoRoot "src\MdViewer.App\bin\$Configuration\net11.0-windows\mdview.exe"
    if (-not (Test-Path $exe)) { throw "app not built: $exe" }
    Start-Process -FilePath $exe -ArgumentList $args
}

function Clear-All {
    Write-Host '» clean' -ForegroundColor Cyan
    foreach ($dir in 'src\MdViewer.App\bin', 'src\MdViewer.App\obj',
        'src\MdViewer.Core\bin', 'src\MdViewer.Core\obj',
        'src\MdViewer.Render\bin', 'src\MdViewer.Render\obj',
        'src\MdViewer.Tests\bin', 'src\MdViewer.Tests\obj',
        'artifacts') {
        $full = Join-Path $repoRoot $dir
        if (Test-Path $full) { Remove-Item -Recurse -Force $full }
    }
}

switch ($Task) {
    'build' { Build-Solution }
    'test' { Build-Solution; Test-All }
    'publish' { Build-Solution; Publish-App }
    'run' { Build-Solution; Invoke-App }
    'clean' { Clear-All }
    'shots' { Build-Solution; & $shotScript -Configuration $Configuration }
    'all' { Build-Solution; Test-All; Publish-App }
}

Write-Host 'done' -ForegroundColor Green
