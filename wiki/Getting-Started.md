# Getting Started

## Install

Run `MD.Viewer-1.4.1-setup.exe`. It installs into `C:\Program Files\Deynekin\MD.Viewer`, adds an
entry to *Installed apps*, offers shortcuts (Start menu, desktop, Quick Launch, Favorites) and
registers `.md`. Silent switches and the install/uninstall checklist:
[docs/INSTALLER.md](https://github.com/paulmann/MD.Viewer.Windows/blob/main/docs/INSTALLER.md).

**Portable:** `mdview.exe` anywhere, no installation. The file carries its own .NET runtime — a
machine without .NET starts it just fine.

## First run

Launching with no arguments restores the previous session (up to 8 tabs); `--no-session` starts
empty. Drag `.md` files onto the window to open them in tabs.

## The three modes

| Mode | Shortcut | What you get |
| --- | --- | --- |
| Rendered | `Ctrl+R` | The laid-out document: typography, tables, callouts, code cards, diagrams, formulas |
| Source | `Ctrl+U` | Raw Markdown, read-only, syntax-coloured, line numbers |
| Editor | `Ctrl+E` | A real editor: soft wrap, block indent, list continuation, undo, find |

`Ctrl+Shift+M` cycles. Files always open **rendered**; **View → Remember the last view mode** brings
back the old behaviour.

## Keyboard

| Keys | Action |
| --- | --- |
| `Ctrl+O` / `Ctrl+S` / `Ctrl+Shift+S` | Open / save / save as |
| `F5` | Reload the file from disk |
| `Ctrl+F`, `F3`, `Shift+F3`, `Esc` | Find, next, previous, close find |
| `Ctrl+B` / `Ctrl+I` / `Ctrl+K` | Bold / italic / link in the editor |
| `Ctrl+W`, `Ctrl+T`, `Ctrl+Tab` | Close tab, new tab, next tab |
| `Ctrl+plus` / `Ctrl+minus` / `Ctrl+0` | Zoom |
| `Ctrl+Shift+M` / `Ctrl+Shift+L` / `Ctrl+\` | Mode / theme / outline |
| `Ctrl+Shift+P` / `Ctrl+P` | Editor and live preview side by side / print |
| `Ctrl+,` | Settings |
| `F1` | Shortcut list |

## Settings

`%APPDATA%\MD.Viewer\settings.json` holds the preferences and the session. **View → Settings…**
(`Ctrl+,`) edits the useful ones: new files in tabs or in a new window, view width, remember the last
mode, outline sidebar. Settings survive both upgrade and uninstall.
