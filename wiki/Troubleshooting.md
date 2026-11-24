# Troubleshooting

## “You must install or update .NET to run this application”

You are running a **development build**, not the shipped program. `dotnet build` produces
`mdview.dll` (no `.exe`, deliberately); the only `mdview.exe` in the repository is
`artifacts/publish/mdview.exe`, and the download from the release page is the same self-contained
file — it carries the runtime inside and starts on a machine without .NET.

How to tell them apart:

| File | Size | Runtime |
| --- | ---: | --- |
| `artifacts/publish/mdview.exe` (release asset) | ~52 MB | inside the file |
| installed `C:\Program Files\Deynekin\MD.Viewer\mdview.exe` | ~114 MB | inside the file |
| an old framework-dependent apphost | ~130 KB | needs .NET 11 |

## A formula renders as source

Unknown command or environment. See [Formulas](Formulas) for the supported set; a formula never
breaks the rest of the page.

## The menu appears on the wrong monitor

Fixed in 1.4.1 — the dropdown is anchored in screen coordinates and moved back if anything relocated
it. Older builds: update.

## The window is not where I left it

The saved position is validated against the virtual desktop, so unplugging a monitor keeps the window
reachable. `--position x,y` overrides it for one session.

## Screenshots are blank

`Control.DrawToBitmap` does not capture double-buffered custom controls. Use `--screenshot`, which
goes through `PrintWindow`, or `-Task shots` for the full smoke suite.

## Where are my settings?

`%APPDATA%\MD.Viewer\settings.json`. They survive upgrades and uninstall; `--reset-settings` restores
the defaults.

## A document opens in the wrong mode

Files always open rendered. If you want the last mode back, enable **View → Remember the last view
mode** (or pass `--mode`).

## Diagnostics

```powershell
mdview.exe --version        # version + whether the runtime is embedded
mdview.exe --diagnostics    # fonts, DPI, system theme, paths
mdview.exe --ui-dump        # control geometry and layout statistics
```
