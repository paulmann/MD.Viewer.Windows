#Requires -Version 7.6
#Requires -PSEdition Core
<#
.SYNOPSIS
    Normalises the repository sources to UTF-8 without BOM and LF line endings.

.DESCRIPTION
    The clean-bom-senior.sh gate covers shell/PHP/text files; this script covers the
    C#/MSBuild/Markdown set the same way, byte for byte, and reports what it changed.
#>
[CmdletBinding()]
param(
    [switch]$Check,
    [string]$Root = (Split-Path -Parent $PSScriptRoot)
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$OutputEncoding = [Console]::InputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

$patterns = @('*.cs', '*.md', '*.csproj', '*.slnx', '*.props', '*.targets', '*.json', '*.ps1',
    '*.py', '*.yml', '*.yaml', '*.manifest', '*.editorconfig', '*.gitignore', '*.gitattributes',
    '*.xml', '*.html', '*.css', '*.js')

$encoding = [System.Text.UTF8Encoding]::new($false)
$utf8Strict = [System.Text.UTF8Encoding]::new($false, $true)
$changed = [System.Collections.Generic.List[string]]::new()
$scanned = 0

$files = Get-ChildItem -Path $Root -Recurse -File -Include $patterns -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch '\\(bin|obj|artifacts|node_modules|\.git|\.zwork)\\' -and $_.FullName -notmatch '\\docs\\img\\' }

foreach ($file in $files) {
    $scanned++
    $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
    if ($bytes.Length -eq 0) { continue }

    $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    $offset = if ($hasBom) { 3 } else { 0 }

    try {
        $text = $utf8Strict.GetString($bytes, $offset, $bytes.Length - $offset)
    }
    catch {
        Write-Warning "not valid UTF-8, skipped: $($file.FullName)"
        continue
    }

    $hasCrlf = $text.Contains("`r`n")
    if (-not $hasBom -and -not $hasCrlf) { continue }

    if ($Check) {
        $changed.Add($file.FullName)
        continue
    }

    $normalised = $text.Replace("`r`n", "`n").Replace("`r", "`n")
    [System.IO.File]::WriteAllText($file.FullName, $normalised, $encoding)
    $changed.Add($file.FullName)
}

Write-Host "scanned: $scanned files"
if ($Check) {
    if ($changed.Count -eq 0) {
        Write-Host 'clean: BOM=False, CRLF=False everywhere' -ForegroundColor Green
        exit 0
    }

    Write-Host "needs normalising: $($changed.Count)" -ForegroundColor Yellow
    $changed | ForEach-Object { Write-Host "  $_" }
    exit 10
}

if ($changed.Count -eq 0) {
    Write-Host 'nothing to do' -ForegroundColor Green
}
else {
    Write-Host "normalised: $($changed.Count) files" -ForegroundColor Green
    $changed | ForEach-Object { Write-Host "  $_" }
}
