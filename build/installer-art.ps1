#Requires -Version 7.6
#Requires -PSEdition Core
<#
.SYNOPSIS
    Generates the installer wizard artwork (24-bit BMP, no alpha) from the app icon.

.DESCRIPTION
    Inno Setup wants 24-bit bitmaps for its wizard: a large panel (164x314) and a
    small one (55x55). Drawing them here keeps the repository free of binary
    artwork that nobody can edit.
#>
[CmdletBinding()]
param(
    [string]$Output = (Join-Path (Split-Path -Parent $PSScriptRoot) 'installer'),
    [string]$IconPath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'assets\mdview.ico')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$OutputEncoding = [Console]::InputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
Add-Type -AssemblyName System.Drawing

if (-not (Test-Path $IconPath)) { throw "icon not found: $IconPath" }
New-Item -ItemType Directory -Force -Path $Output | Out-Null

function New-Canvas([int]$width, [int]$height) {
    $bitmap = [System.Drawing.Bitmap]::new($width, $height, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit
    return @{ Bitmap = $bitmap; Graphics = $graphics }
}

# ---------------------------------------------------------------- large panel
$large = New-Canvas 164 314
$g = $large.Graphics
$rect = [System.Drawing.Rectangle]::new(0, 0, 164, 314)
$brush = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
    $rect,
    [System.Drawing.Color]::FromArgb(0x2F, 0x6F, 0xEB),
    [System.Drawing.Color]::FromArgb(0x12, 0x2A, 0x66),
    [System.Drawing.Drawing2D.LinearGradientMode]::Vertical)
$g.FillRectangle($brush, $rect)
$brush.Dispose()

# soft light circle behind the icon
$glow = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(28, 255, 255, 255))
$g.FillEllipse($glow, 22, 46, 120, 120)
$glow.Dispose()

$appIcon = [System.Drawing.Icon]::new((Resolve-Path $IconPath).Path, 96, 96)
$target = [System.Drawing.Rectangle]::new(34, 58, 96, 96)
$g.DrawIcon($appIcon, $target)
$appIcon.Dispose()

$titleFont = [System.Drawing.Font]::new('Segoe UI', 19, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$subFont = [System.Drawing.Font]::new('Segoe UI', 11, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$white = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
$dim = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(205, 255, 255, 255))

$format = [System.Drawing.StringFormat]::new()
$format.Alignment = [System.Drawing.StringAlignment]::Center
$g.DrawString('MD Viewer', $titleFont, $white, [System.Drawing.RectangleF]::new(0, 176, 164, 30), $format)
$g.DrawString('Markdown', $subFont, $dim, [System.Drawing.RectangleF]::new(0, 208, 164, 18), $format)
$g.DrawString('Windows 10 / 11', $subFont, $dim, [System.Drawing.RectangleF]::new(0, 226, 164, 18), $format)

$footerFont = [System.Drawing.Font]::new('Segoe UI', 10, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$g.DrawString('Deynekin', $footerFont, $dim, [System.Drawing.RectangleF]::new(0, 274, 164, 16), $format)

$titleFont.Dispose(); $subFont.Dispose(); $footerFont.Dispose(); $white.Dispose(); $dim.Dispose(); $format.Dispose()
$g.Dispose()
$largePath = Join-Path $Output 'wizard-large.bmp'
$large.Bitmap.Save($largePath, [System.Drawing.Imaging.ImageFormat]::Bmp)
$large.Bitmap.Dispose()

# ---------------------------------------------------------------- small panel
$small = New-Canvas 55 55
$sg = $small.Graphics
$sg.Clear([System.Drawing.Color]::White)
$smallIcon = [System.Drawing.Icon]::new((Resolve-Path $IconPath).Path, 40, 40)
$smallTarget = [System.Drawing.Rectangle]::new(8, 8, 40, 40)
$sg.DrawIcon($smallIcon, $smallTarget)
$smallIcon.Dispose()
$sg.Dispose()
$smallPath = Join-Path $Output 'wizard-small.bmp'
$small.Bitmap.Save($smallPath, [System.Drawing.Imaging.ImageFormat]::Bmp)
$small.Bitmap.Dispose()

Get-Item $largePath, $smallPath | Select-Object Name, Length

