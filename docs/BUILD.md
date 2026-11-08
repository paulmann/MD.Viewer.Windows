# Building

## Toolchain

| Component | Version used | Notes |
| --- | --- | --- |
| .NET SDK | `11.0.100-rc.3.26522.148` | pinned by `global.json` (`allowPrerelease: true`) |
| Target framework | `net11.0-windows` | Windows Desktop runtime, WinForms |
| Language | C# 15 (`LangVersion=preview`) | union types, closed hierarchies, collection arguments |
| Packages | none | `dotnet list package` is empty by design |

C# 15 ships with the .NET 11 SDK; the three features used here (union types, `closed` classes,
`[with(...)]` collection arguments) are available without extra switches.

## Build

```powershell
pwsh -File build/build.ps1 -Task build      # compile
pwsh -File build/build.ps1 -Task test       # compile + all checks
pwsh -File build/build.ps1 -Task publish    # self-contained single-file exe
pwsh -File build/build.ps1 -Task shots      # UI smoke suite (needs a desktop session)
pwsh -File build/build.ps1 -Task clean
```

`-Configuration Debug|Release` (default `Release`) and `-DotNetRoot <path>` are supported.

## Publishing

`-Task publish` runs:

```
dotnet publish src/MdViewer.App -c Release -r win-x64 --self-contained true
  -p:PublishSingleFile=true
  -p:IncludeNativeLibrariesForSelfExtract=true
  -p:EnableCompressionInSingleFile=true
  -p:DebugType=none
  -o artifacts/publish
```

Result: a single `mdview.exe` (~50 MB) that runs on a Windows 10/11 machine with **no .NET
installed**. Verify by clearing the environment and running it:

```powershell
Remove-Item Env:DOTNET_ROOT -ErrorAction SilentlyContinue
Start-Process .\artifacts\publish\mdview.exe -ArgumentList '--version' -Wait -NoNewWindow
```

## Why the exe is 50 MB

The build is **self-contained**: the .NET runtime and the Windows Desktop framework live inside
`mdview.exe`, so no machine needs .NET installed (Windows only ships .NET Framework 4.8, not
.NET 8/10/11).

| Build | Size | Needs .NET installed |
| --- | ---: | :---: |
| `dotnet build` output (`src/MdViewer.App/bin/Release/net11.0-windows/mdview.exe`) | 74 752 B | yes |
| `-Task publish` (`artifacts/publish/mdview.exe`) | 52 497 132 B | **no** |

The runtime inside the single file is compressed (`EnableCompressionInSingleFile`), which is why a
plain text search does not find `coreclr.dll`; the assembly names (`System.Private.CoreLib`,
`Microsoft.WindowsDesktop.App`) are still visible. Verified by running the published file with
`dotnet` removed from `PATH` and `DOTNET_ROOT` unset: `--version`, `--export html` and the GUI all
work.

## Environment traps (this workspace)

1. **`dotnet` on PATH is not an SDK.** It is a runtime-only copy shipped with the host app;
   `dotnet build` fails with "No SDKs were found". The build script locates a real SDK.
2. **Missing `APPDATA` breaks NuGet.** With the .NET 11 SDK, restore aborts with
   `Value cannot be null. (Parameter 'path1')` when `APPDATA`/`LOCALAPPDATA` are absent from the
   session. The script derives them from the shell folders.
3. **`DOTNET_ROOT` points at the wrong place.** It is set to the runtime-only copy, so an apphost
   started from the shell looks for .NET 11 there and refuses to run. The script repoints it at
   the SDK root for the duration of the build.
4. **GUI processes do not block PowerShell.** `& .\mdview.exe …` returns immediately; use
   `Start-Process -Wait` when ordering matters.
5. **`DrawToBitmap` cannot capture double-buffered controls.** Use `PrintWindow`
   (`WindowCapture.Save`), which the screenshot tooling does.

## Continuous integration

The two commands a CI job needs are:

```powershell
pwsh -File build/build.ps1 -Task test
pwsh -File build/build.ps1 -Task publish
```

`-Task test` exits non-zero on the first failing check and prints the assertion that failed.
