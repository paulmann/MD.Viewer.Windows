#Requires -Version 7.6
#Requires -PSEdition Core
<#
.SYNOPSIS
    Publishes the pages in wiki/ to the GitHub wiki of this repository.

.DESCRIPTION
    GitHub creates the wiki repository only after the first page has been created in
    the web interface, so this script checks for the clone and says exactly what to do
    when it is missing:

        https://github.com/paulmann/MD.Viewer.Windows/wiki/_new

    Once the wiki exists, the script clones it, copies every wiki/*.md page over the
    clone (Home.md, _Sidebar.md, _Footer.md included), commits as the repository owner
    and pushes.

.PARAMETER Check
    Report what would be published and exit without touching the network.

.PARAMETER Message
    Commit message (default: "wiki: publish from the repository").
#>
[CmdletBinding()]
param(
    [switch]$Check,

    [string]$Message = 'wiki: publish from the repository',

    [string]$WikiUrl = 'https://github.com/paulmann/MD.Viewer.Windows.wiki.git'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$OutputEncoding = [Console]::InputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

$repoRoot = Split-Path -Parent $PSScriptRoot
$sourceDir = Join-Path $repoRoot 'wiki'
$workDir = Join-Path ([System.IO.Path]::GetTempPath()) ('mdview-wiki-' + [Guid]::NewGuid().ToString('N'))

if (-not (Test-Path $sourceDir)) {
    throw "no wiki sources: $sourceDir"
}

$pages = Get-ChildItem -Path $sourceDir -Filter *.md | Sort-Object Name
if ($pages.Count -eq 0) {
    throw "no pages in $sourceDir"
}

Write-Host "pages to publish: $($pages.Count)" -ForegroundColor Cyan
foreach ($page in $pages) {
    Write-Host ("  {0,-28} {1,6} B" -f $page.Name, $page.Length)
}

if ($Check) {
    Write-Host 'check only: nothing was pushed' -ForegroundColor DarkGray
    exit 0
}

try {
    & git clone --quiet $WikiUrl $workDir 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw 'clone failed'
    }
} catch {
    Remove-Item -Recurse -Force $workDir -ErrorAction SilentlyContinue
    throw @"
The wiki repository does not exist yet.

GitHub creates it with the first page, and only from the web interface:

    https://github.com/paulmann/MD.Viewer.Windows/wiki/_new

Create any page there once (the title "Home" and one line are enough), then run this
script again — it will replace that page with the pages in wiki/ and push the rest.
"@
}

try {
    foreach ($page in $pages) {
        Copy-Item -Path $page.FullName -Destination (Join-Path $workDir $page.Name) -Force
    }

    Push-Location $workDir
    try {
        & git add -A
        & git -c 'user.name=Mikhail Deynekin' -c 'user.email=Mikhail@Deynekin.com' commit --quiet -m $Message
        if ($LASTEXITCODE -ne 0) {
            Write-Host 'nothing changed: the wiki is up to date' -ForegroundColor DarkGray
            exit 0
        }

        & git push --quiet origin HEAD
        if ($LASTEXITCODE -ne 0) {
            throw "push failed ($LASTEXITCODE)"
        }
    } finally {
        Pop-Location
    }
} finally {
    Remove-Item -Recurse -Force $workDir -ErrorAction SilentlyContinue
}

Write-Host 'wiki published' -ForegroundColor Green
