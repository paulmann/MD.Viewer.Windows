# Building

Zero dependencies, no NuGet packages, no binary assets: `dotnet list package` must stay empty.

## Toolchain

* Windows 10/11 x64, **.NET 11 SDK** (the pinned version is in `global.json`; `LangVersion` is
  `preview` so C# 15 works).
* PowerShell 7 for the build driver.

## Tasks

```powershell
pwsh -File build/build.ps1 -Task build      # compile everything
pwsh -File build/build.ps1 -Task test       # build + 193 checks
pwsh -File build/build.ps1 -Task publish    # self-contained single-file mdview.exe (52 MB)
pwsh -File build/build.ps1 -Task installer  # Inno Setup package (needs Inno Setup 6)
pwsh -File build/build.ps1 -Task shots      # UI smoke suite (needs a desktop session)
pwsh -File build/build.ps1 -Task run        # start the development build
```

## Where the artefacts land

| Build | Path | Needs .NET installed |
| --- | --- | --- |
| `-Task build` | `src/MdViewer.App/bin/Release/net11.0-windows/mdview.dll` | yes — and there is deliberately **no `.exe`** |
| `-Task publish` | `artifacts/publish/mdview.exe` (52 MB) | **no** |
| `-Task installer` | `artifacts/installer/MD.Viewer-x.y.z-setup.exe` | **no** |

Development builds set `UseAppHost=false`, because a framework-dependent `mdview.exe` looks like the
shipped program but greets the reader with “You must install or update .NET”. The only `mdview.exe`
in the tree is the published one.

## Traps of this workspace

1. **`dotnet` on PATH may be a runtime-only copy** shipped with a host application; it cannot build.
   The build script resolves a real SDK itself.
2. **Missing `APPDATA`/`LOCALAPPDATA` breaks NuGet** with `Value cannot be null (Parameter 'path1')`.
   The script sets them.
3. **`DOTNET_ROOT` may point at that bare runtime**, so an apphost refuses to start; the script
   repoints it.
4. **PowerShell does not wait for GUI executables** — use `Start-Process -Wait` in scripts.
5. **`Control.DrawToBitmap` renders nothing** for double-buffered custom controls; window captures go
   through `PrintWindow` (`WindowCapture.Save`).
