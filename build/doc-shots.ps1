#Requires -Version 7.6
#Requires -PSEdition Core
<#
.SYNOPSIS
    Regenerates the screenshots used by README.md and README.ru.md.

.DESCRIPTION
    Two complete sets, so the English page never shows Russian text and the Russian
    page never shows English text:

      English (README.md)                Russian (README.ru.md)
      rendered-light.png                 rendered-light-ru.png
      rendered-dark.png                  rendered-dark-ru.png
      source-light.png                   source-light-ru.png
      editor-dark.png                    editor-dark-ru.png
      mermaid.png                        mermaid-ru.png
      about.png                          about-ru.png

    The four mode captures come from the UI smoke suite, which uses a fixed window size
    and a fixed theme/mode walk; the two figure captures are rendered headless through
    `--screenshot` and cropped to their content.

.PARAMETER Out
    Target directory (default: docs/img).

.PARAMETER SkipModeShots
    Only regenerate the figures (mermaid, about).
#>
[CmdletBinding()]
param(
    [string]$Out = 'docs/img',

    [switch]$SkipModeShots
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$OutputEncoding = [Console]::InputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

$repoRoot = Split-Path -Parent $PSScriptRoot
$outDir = if ([System.IO.Path]::IsPathRooted($Out)) { $Out } else { Join-Path $repoRoot $Out }
$tests = Join-Path $repoRoot 'src/MdViewer.Tests/bin/Release/net11.0-windows/mdview-tests.exe'
$appDll = Join-Path $repoRoot 'src/MdViewer.App/bin/Release/net11.0-windows/mdview.dll'
$work = Join-Path $repoRoot 'artifacts/doc-shots'

foreach ($needed in @($tests, $appDll)) {
    if (-not (Test-Path $needed)) {
        throw "not built: $needed (run build.ps1 -Task build first)"
    }
}

$candidates = @('C:\GIT\_tools\dotnet11', $env:DOTNET_ROOT, 'C:\Program Files\dotnet') | Where-Object { $_ -and (Test-Path $_) }
$sdk = $null
foreach ($candidate in $candidates) {
    $runtime = Join-Path $candidate 'shared/Microsoft.NETCore.App'
    if (-not (Test-Path $runtime)) { continue }
    if (Get-ChildItem $runtime -Directory | Where-Object { $_.Name -like '11.*' }) {
        $sdk = $candidate
        break
    }
}

if (-not $sdk) {
    throw 'no .NET 11 runtime found; set DOTNET_ROOT to the SDK that built this tree'
}

$env:DOTNET_ROOT = $sdk
Write-Host "runtime: $sdk" -ForegroundColor DarkGray

New-Item -ItemType Directory -Force -Path $outDir, $work | Out-Null

# --- 1. mode captures ------------------------------------------------------------
if (-not $SkipModeShots) {
    $sets = @(
        @{ Input = 'samples/demo.md'; Lang = 'en' },
        @{ Input = 'samples/ru-пример.md'; Lang = 'ru' }
    )

    foreach ($set in $sets) {
        $inputPath = Join-Path $repoRoot $set.Input
        Write-Host "capturing $($set.Lang): $($set.Input)" -ForegroundColor Cyan
        Push-Location $repoRoot
        try {
            & $tests --ui-smoke $inputPath --out $outDir --lang $set.Lang
            if ($LASTEXITCODE -ne 0) {
                throw "ui-smoke failed for $($set.Lang) ($LASTEXITCODE)"
            }
        } finally {
            Pop-Location
        }

        # The suite names the dark/light captures after the mode ("render-light"); the
        # documentation has always called them "rendered-light" — keep the public names.
        $renamed = Get-ChildItem $outDir -Filter 'render-*.png'
        foreach ($file in $renamed) {
            $target = $file.FullName -replace '\\render-', '\rendered-'
            Move-Item -Path $file.FullName -Destination $target -Force
        }
    }
}

# --- 2. figures ------------------------------------------------------------------
# A figure is built from the sample itself, so the picture cannot drift from the file:
# the sequence diagram and the pie chart, nothing else.
function New-FigureSource {
    param(
        [Parameter(Mandatory)][string]$Sample,
        [Parameter(Mandatory)][string]$Title,
        [Parameter(Mandatory)][string]$SequenceHeading,
        [Parameter(Mandatory)][string]$PieHeading
    )

    $text = Get-Content -Path $Sample -Raw
    $blocks = [regex]::Matches($text, '(?s)```mermaid\r?\n(?<body>.*?)```')
    $sequence = $blocks | Where-Object { $_.Groups['body'].Value -match 'sequenceDiagram' } | Select-Object -First 1
    $pie = $blocks | Where-Object { $_.Groups['body'].Value -match '(?m)^\s*pie' } | Select-Object -First 1

    if ($null -eq $sequence -or $null -eq $pie) {
        throw "no sequence/pie diagram in $Sample"
    }

    $figure = @(
        "# $Title",
        '',
        "## $SequenceHeading",
        '',
        '```mermaid',
        $sequence.Groups['body'].Value.TrimEnd(),
        '```',
        '',
        "## $PieHeading",
        '',
        '```mermaid',
        $pie.Groups['body'].Value.TrimEnd(),
        '```',
        ''
    ) -join "`n"

    $target = Join-Path $work ('figure-' + [System.IO.Path]::GetFileNameWithoutExtension($Sample) + '.md')
    Set-Content -Path $target -Value $figure -Encoding utf8NoBOM -NoNewline
    return $target
}

function Invoke-Capture {
    param(
        [Parameter(Mandatory)][string]$InputPath,
        [Parameter(Mandatory)][string]$Target,
        [string]$Theme = 'light',
        [int]$ReadingWidth = 720
    )

    if (Test-Path $Target) { Remove-Item -Force $Target }
    # --export png renders the document itself: same parser, same layout, no window chrome,
    # no sidebar, full height — exactly what a figure needs.
    & $sdk\dotnet.exe $appDll $InputPath '--no-session' '--theme' $Theme '--zoom' '1' `
        '--width' $ReadingWidth '--export' 'png' '--out' $Target | Out-Null
    if (-not (Test-Path $Target)) {
        throw "no capture at $Target"
    }
}

# Trims the page margins: crop to the bounding box of everything that differs from the
# page background, then pad by a fixed margin so the figure has air around it.
function Optimize-Figure {
    param(
        [Parameter(Mandatory)][string]$Path,
        [int]$Padding = 18
    )

    $script = @'
import sys
from PIL import Image, ImageChops

path, padding = sys.argv[1], int(sys.argv[2])
im = Image.open(path).convert("RGB")
bg = Image.new("RGB", im.size, im.getpixel((im.width - 2, im.height - 2)))
diff = ImageChops.difference(im, bg).convert("L")
box = diff.point(lambda v: 255 if v > 12 else 0).getbbox()
if not box:
    print("empty figure")
    sys.exit(3)
left, top, right, bottom = box
left = max(0, left - padding)
top = max(0, top - padding)
right = min(im.width, right + padding)
bottom = min(im.height, bottom + padding)
im.crop((left, top, right, bottom)).save(path)
print(f"{im.width}x{im.height} -> {right-left}x{bottom-top}")
'@
    $scriptPath = Join-Path $work 'optimize-figure.py'
    Set-Content -Path $scriptPath -Value $script -Encoding utf8NoBOM -NoNewline
    $result = & python $scriptPath $Path $Padding
    if ($LASTEXITCODE -ne 0) {
        throw "figure optimize failed: $result"
    }

    return $result
}

$figures = @(
    @{
        Lang = 'en'; Suffix = ''
        Source = New-FigureSource -Sample (Join-Path $repoRoot 'samples/mermaid-en.md') `
            -Title 'Mermaid' -SequenceHeading 'Sequence diagram' -PieHeading 'Pie chart'
    },
    @{
        Lang = 'ru'; Suffix = '-ru'
        Source = New-FigureSource -Sample (Join-Path $repoRoot 'samples/mermaid.md') `
            -Title 'Mermaid' -SequenceHeading 'Диаграмма последовательности' -PieHeading 'Круговая диаграмма'
    }
)

foreach ($figure in $figures) {
    $target = Join-Path $outDir ('mermaid' + $figure.Suffix + '.png')
    Write-Host "figure: mermaid$($figure.Suffix).png ($($figure.Lang))" -ForegroundColor Cyan
    Invoke-Capture -InputPath $figure.Source -Target $target
    $shape = Optimize-Figure -Path $target
    Write-Host "  $shape"
}

# --- 3. the About dialog ----------------------------------------------------------
foreach ($pair in @(@{ Lang = 'en'; Suffix = '' }, @{ Lang = 'ru'; Suffix = '-ru' })) {
    $target = Join-Path $outDir ('about' + $pair.Suffix + '.png')
    Write-Host "dialog: about$($pair.Suffix).png ($($pair.Lang))" -ForegroundColor Cyan
    if (Test-Path $target) { Remove-Item -Force $target }
    & $sdk\dotnet.exe $appDll '--no-session' '--theme' 'light' '--lang' $pair.Lang '--about-shot' $target | Out-Null
    if (-not (Test-Path $target)) {
        throw "no capture at $target"
    }
}

Get-ChildItem $outDir -Filter *.png | Sort-Object Name |
    Select-Object Name, Length | Format-Table -AutoSize

Write-Host 'done' -ForegroundColor Green
