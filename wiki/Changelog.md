## Changelog

The canonical history is
[CHANGELOG.md](https://github.com/paulmann/MD.Viewer.Windows/blob/main/CHANGELOG.md) (Keep a
Changelog format).

### 1.4.1 — 2026-11-24

* **Fixed:** the File menu could open on another display; it is now anchored in screen coordinates
  and verified against the button's monitor after opening. Measured on four displays.
* **Added:** draggable tabs — another monitor gives the document its own window at the drop point,
  another window takes it as a tab, and dropping it on its own window changes nothing.
* The ☰ button answers the right mouse button; `--position`, `--menu-dump` for the checks.

### 1.4.0 — 2026-11-24

* **Fixed:** table text ran past the table border; columns are pinned to the page and cells paint
  clipped.
* **Added:** formula typesetting by the built-in TeX engine; the 100/75/50 % width switch; a settings
  dialog (tabs or a new window); several files on the command line open as tabs.

### 1.3.3 — 2026-11-09

* The theme button flips the appearance in one click instead of cycling through a state that looks
  identical.
* Files open in the rendered view; **Remember the last view mode** brings the old behaviour back.

### 1.3.2 — 2026-11-09

* An in-place upgrade purges `docs` and `samples`, so dropped pages do not linger.

### 1.3.1 — 2026-11-09

* Scroll blank-page fix, a visible mode switcher, no modal dialogs during installation.

### 1.3.0 — 2026-11-09

* Installer, application icon drawn in code, session-only command-line switches.

Earlier versions are in the repository changelog.
