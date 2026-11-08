# Installer

`MD.Viewer-1.3.0-setup.exe` — one file, **34.8 MB**, published as a release asset
(**[latest release](https://github.com/paulmann/MD.Viewer.Windows/releases/latest)**) and built with
Inno Setup 6.7.4. It installs the same self-contained `mdview.exe` (no .NET needed) and adds
everything Windows expects from a desktop application.

## What it does

| | |
| --- | --- |
| Install path | `C:\Program Files\Deynekin\MD.Viewer` (machine-wide, admin) |
| Payload | `mdview.exe`, README (both languages), CHANGELOG, LICENSE, `docs\`, `samples\` |
| Installed apps entry | “MD Viewer 1.3.0”, publisher, icon, size, support URL, quiet uninstall |
| Uninstaller | `unins000.exe`, removes files, shortcuts, registry entries it created |
| Association | `.md`, `.markdown`, `.mdown` → `MDViewer.Markdown` + “Open with” + Default apps |

## Options the user sees

Tasks page (checkboxes, kept between upgrades):

| Task | Default | Effect |
| --- | --- | --- |
| Start menu shortcut | **on** | Folder `Deynekin\MD Viewer` with MD Viewer, README and Uninstall |
| Desktop icon | off | `MD Viewer.lnk` in the public desktop |
| Quick Launch shortcut | off | `…\AppData\Roaming\Microsoft\Internet Explorer\Quick Launch\MD Viewer.lnk` |
| Favorites shortcut | off | `…\Favorites\MD Viewer.lnk` |
| Open `.md` with MD Viewer | **on** | Registers the ProgID, the default handler and the Default-apps entry |

Finish page (checkboxes):

| Checkbox | Effect |
| --- | --- |
| Launch MD Viewer | Starts the viewer once, right after installation |
| Open README.md | Opens the installed README in the viewer (`README.ru.md` for the Russian UI) |

## Honest notes about two of them

* **Quick Launch** is the legacy per-user folder. Windows 10/11 no longer show that toolbar by
  default, and **pinning to the taskbar cannot be scripted** — Microsoft removed that API in
  Windows 10 and blocks it in the shell. The shortcut is placed where the old toolbar reads it;
  the user can drag it to the taskbar once.
* **The `.md` association** is written to `HKLM\Software\Classes`. If the user (or another app)
  already has a *personal* choice (`FileExts\.md\UserChoice`, hash-protected), Windows keeps it and
  the installer cannot override it. When that is the case, the Finish page gets one extra line
  pointing at Settings → Apps → Default apps → Choose defaults by file type. It is never a modal
  dialog during the progress page: that reads as a failure and blocks the install.
  The program is always registered as an “Open with” candidate either way.

## Silent use

```powershell
# quiet install with everything but the desktop icon
.\MD.Viewer-1.3.0-setup.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART `
    /TASKS="startmenu,quicklaunch,associate"

# custom directory (still needs admin for Program Files)
.\MD.Viewer-1.3.0-setup.exe /VERYSILENT /DIR="D:\Apps\MD.Viewer"

# uninstall quietly
& "C:\Program Files\Deynekin\MD.Viewer\unins000.exe" /VERYSILENT /SUPPRESSMSGBOXES
```

Task names: `startmenu`, `desktopicon`, `quicklaunch`, `favorites`, `associate`. The two Finish-page
entries are `postinstall` items and never run in silent mode.

## Uninstall behaviour

Removed: the program folder, `unins000.*`, all shortcuts the installer created, `HKLM\Software\Classes\MDViewer.Markdown`,
the `Applications\mdview.exe` entry, `RegisteredApplications\MDViewer`, `Software\MDViewer`, and the
`App Paths\mdview.exe` key. The `.md`/`.markdown`/`.mdown` default values are cleared **only if they
still point at MD Viewer** — a choice the user made afterwards is left alone. Document files are
never touched.

## Building it

```powershell
# once: install Inno Setup 6 (winget install JRSoftware.InnoSetup, or jrsoftware.org/isdl.php)
pwsh -File build/build.ps1 -Task installer
```

The task publishes a **second, uncompressed** single-file build into `artifacts\installer-payload`
and lets Inno compress it:

| Payload | Setup size |
| --- | ---: |
| Portable exe (internally compressed, 50 MB) | 45.2 MB |
| Raw single file (114 MB on disk) | **34.8 MB** |

That is why the installer does not simply wrap the portable download: LZMA2 beats the internal
compression by ~10 MB. `installer-art.ps1` draws the wizard bitmaps from `assets/mdview.ico`, so the
repository keeps no hand-made artwork.

## Verification performed

Install → check → uninstall → check, on Windows 11 (build 26300), elevated:

* files land in `C:\Program Files\Deynekin\MD.Viewer`, `mdview.exe --version` reports 1.3.0;
* all six shortcuts appear with the right names, including `Favorites\MD Viewer.lnk`;
* `HKLM\Software\Classes\.md` = `MDViewer.Markdown`, the ProgID command points at the installed exe,
  `App Paths\mdview.exe` works from Win+R;
* Installed apps shows “MD Viewer 1.3.0” with icon, publisher, size and both uninstall strings;
* after uninstall: no folder, no ARP entry, no `MDViewer` keys, no shortcuts, `.md` value cleared.

Not covered by automation: the wizard pages themselves (Inno runs its UI in a temporary
`*.tmp` process, so a scripted screenshot needs UI Automation) and the “already installed” upgrade
path beyond `UsePreviousTasks`/`UsePreviousAppDir`.
