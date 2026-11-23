#Requires -Version 7.6
#Requires -PSEdition Core
<#
.SYNOPSIS
    Runs the UI smoke suite: opens the real window, walks modes and themes, and
    captures it with PrintWindow.

.DESCRIPTION
    The suite lives in the test runner (`mdview-tests.exe --ui-smoke`). This
    wrapper exists so `build.ps1 -Task shots` keeps working from any directory
    and so the .NET 11 SDK/runtime quirk is solved in one place.

    Needs an interactive desktop session: it opens a window.

.PARAMETER Configuration
    Build configuration whose test runner is used (default: Release).

.PARAMETER Input
    Markdown file to open (default: samples/demo.md).

.PARAMETER Out
    Directory for the PNG captures and ui-smoke.log
    (default: artifacts/screenshots).
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [string]$Input = 'samples/demo.md',

    [string]$Out = 'artifacts/screenshots'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$OutputEncoding = [Console]::InputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path (Join-Path $repoRoot 'global.json'))) { $repoRoot = (Get-Location).Path }

$exe = Join-Path $repoRoot "src/MdViewer.Tests/bin/$Configuration/net11.0-windows/mdview-tests.exe"
if (-not (Test-Path $exe)) {
    throw "test runner not found: $exe (run build.ps1 -Task build first)"
}

$inputPath = if ([System.IO.Path]::IsPathRooted($Input)) { $Input } else { Join-Path $repoRoot $Input }
if (-not (Test-Path $inputPath)) {
    throw "input not found: $inputPath"
}

$outPath = if ([System.IO.Path]::IsPathRooted($Out)) { $Out } else { Join-Path $repoRoot $Out }
New-Item -ItemType Directory -Force -Path $outPath | Out-Null

# The apphost resolves the runtime from DOTNET_ROOT; the session points it at a
# runtime-only copy, so aim it at the SDK that produced the build.
if ($env:DOTNET_ROOT) {
    $candidate = $env:DOTNET_ROOT
} else {
    $candidate = 'C:\GIT\_tools\dotnet11'
}

Push-Location $repoRoot
try {
    if (Test-Path $candidate) { $env:DOTNET_ROOT = $candidate }
    & $exe --ui-smoke $inputPath --out $outPath
    $code = $LASTEXITCODE
} finally {
    Pop-Location
}

if ($code -ne 0) {
    throw "UI smoke failed with exit code $code (see $(Join-Path $outPath 'ui-smoke.log'))"
}

Write-Host "done"
exit 0
