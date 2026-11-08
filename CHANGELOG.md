# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.3.2] — 2026-11-09

Upgrades now clean up after themselves, and the upgrade/uninstall behaviour is measured instead of
assumed.

### Fixed

- **An in-place upgrade kept files the new version no longer ships.** `[InstallDelete]` now purges
  `{app}\docs` and `{app}\samples` before copying. Verified with planted `docs\obsolete-page.md` and
  `samples\old-sample.md`: both gone after upgrading 1.3.1 → 1.3.2.

### Documented and verified

- Upgrade 1.3.0 → 1.3.1 → 1.3.2: one Installed-apps entry (version updated, no duplicates), task
  selection remembered, all four shortcuts kept, `%APPDATA%\MD.Viewer\settings.json` byte-identical
  (same SHA-256), and the app starts with those very settings — dark theme, 125 % zoom, Source mode.
- Uninstall leaves `%APPDATA%\MD.Viewer` alone, so settings survive removal and reinstall;
  `--reset-settings` is the deliberate way to clear them. See docs/INSTALLER.md.

## [1.3.1] — 2026-11-09

Two bugs found by installing and using the product, both visible in the first minute.

### Fixed

- **Scrolling past the first screen left the page blank.** `DocumentView` applied the scroll offset
  twice — once as a transform and once inside `MarkdownRenderer.PaintContent`, which also does the
  viewport culling. The culling then measured the wrong slice and stopped painting every block below
  the first viewport. The offset is now applied in one place. Verified with captures at 0, 700, 1500
  and 2400 px: 22 683 / 23 671 / 15 126 / 16 027 inked samples instead of an empty page.
- **The three-mode switcher never appeared.** `ModeSwitcher` was created, themed and positioned but
  never added to the toolbar's control collection, so Rendered / Source / Editor was invisible since
  the first release (the shortcuts and the menu still worked). Now it is added and sits centred.
- **The installer no longer shows a modal message during “Installing…”.** The per-user `.md` choice
  note is appended to the Finish page instead of interrupting the progress page with a dialog that
  looks like an error.

## [1.3.0] — 2026-11-09

Installer, application icon, and the theme bug the owner hit.

### Added

- **Windows installer** (`installer\MD.Viewer.iss`, Inno Setup 6): installs to
  `C:\Program Files\Deynekin\MD.Viewer`, registers itself in **Installed apps** with a working
  uninstaller, offers checkboxes for the Start menu folder, desktop icon, Quick Launch, Favorites
  and the `.md` association, and two Finish-page checkboxes — launch the viewer and open README.md.
  Setup is 34.8 MB (LZMA2 over an uncompressed payload; see docs/INSTALLER.md for the measurement).
- **File association**: `.md`, `.markdown` and `.mdown` get the `MDViewer.Markdown` ProgID, an
  “Open with” entry and a Default-apps registration. Uninstall clears the default only if it still
  points at the viewer.
- **Application icon**, drawn in code and written as a nine-resolution `.ico`
  (`--save-icon`), embedded in the exe so Explorer, the taskbar and Installed apps show it.
- `--save-icon <file>` for regenerating that icon; the installer wizard artwork is rendered from
  the same source by `build/installer-art.ps1`.
- `-Task installer` in the build script.

### Fixed

- **The saved theme could pin light mode forever.** `--theme light` (and `--zoom`, `--mode`,
  `--serif`, `--no-toc`, `--line-numbers`, `--wrap`, `--numbering`) wrote the value into the
  preferences, so every later launch ignored the Windows setting. Command-line switches are now
  session-only; a gesture in the UI still persists.
- **“Auto” now follows Windows live**: the window listens for `WM_SETTINGCHANGE` /
  `WM_THEMECHANGED` and repaints when the OS app mode changes.
- Theme detection falls back to `SystemUsesLightTheme` when `AppsUseLightTheme` is missing.
- `--width` was parsed but never applied to the reading column; `--toc/--no-toc` and the other
  switches now reach the window instead of the settings file.

## [1.2.1] — 2026-11-09

Nothing about the runtime changed — only the wording that made it look like a requirement.

### Changed

- The About page printed a bare `.NET 11` next to the version, which read as "requires .NET 11".
  It now has separate rows: **Version** (`1.2.1 · C# 15`) and **Runtime**
  (`11.0.0-rc.3 · embedded in the exe, nothing to install`).
- `--version` and `--diagnostics` say the same thing explicitly: the runtime is embedded and no
  .NET installation is required.

### Documentation

- Requirements explain that Windows carries only .NET Framework 4.8 — not .NET 8/10/11 — and give
  the measurement: 52.5 MB published file versus a 74 KB framework-dependent build, i.e. ~50 MB is
  the embedded runtime.

## [1.2.0] — 2026-11-09

Markdown coverage closed against the audit, and an About page that names its author.

### Added

- **About page** with product card (version, licence, author, build), links to the repository,
  licence and documentation, a diagnostics button, and the credit/privacy lines — the same shape
  as the TrayIconGuard About page, with this repository and product name.
- **Glossary tooltips**: any two-column table feeds a glossary; hovering the term in the body shows
  a floating definition card. `Term (ABBR)` registers the abbreviation and the long form too.
- **Smart punctuation**, now implemented behind a View switch: `--`/`---`/`...`, `(c)/(r)/(tm)`,
  `!=`, `<=`, `>=`, `+-`. Code spans and code blocks are never touched.
- **Single-newline switch** in View: legacy behaviour (on) versus classic CommonMark paragraph flow.
- **Clickable images**: clicking a picture opens the file in the default viewer.
- `--about-shot <png>` for the screenshot suite.

### Fixed

- **Hard line breaks** (two trailing spaces or `\`) were destroyed by the block parser trimming line
  ends — now preserved.
- **Emphasis** no longer closes on a delimiter run of the wrong length: `*a **b** c*` renders as
  emphasis around strong, not two broken fragments.
- **Link reference definitions** may be indented up to three spaces and may carry their title on the
  next line.
- **Definition lists** support several terms per block and several definitions per term.
- A layer with no nodes could crash the flowchart layout (`Sequence contains no elements`).

### Changed

- Unsupported Mermaid types name themselves in the fallback card.
- The test runner prints a per-suite tally, so documentation numbers cannot drift from the code.

## [1.1.0] — 2026-11-09

Mermaid grew from flowcharts to **seven diagram types**, all drawn natively by the same layout and
paint pipeline as the rest of the document.

### Added

- **Sequence diagrams** (`sequenceDiagram`): participants and actors, lifelines, solid and dashed
  arrows with filled/open heads, self-messages, `autonumber`, notes (`left of` / `right of` /
  `over`), `loop` / `alt` / `opt` / `par` frames with labels and `else` dividers, `==` / `---`
  dividers.
- **Pie charts** (`pie`): title, slices with percentages inside the wedges, a legend, `showData`.
- **Class diagrams** (`classDiagram`): class boxes with `<<annotation>>`, visibility sigils,
  attributes and methods, and relations — inheritance, realization, composition, aggregation,
  association, dependency — with labels and cardinalities.
- **State diagrams** (`stateDiagram-v2`): states, labelled transitions, `[*]` start/end dots and
  `state "…" as Id` aliases; composite states are flattened.
- **Gantt charts** (`gantt`): sections, explicit dates with `dateFormat`, `Nd`/`Nw` durations,
  `after` dependencies, milestones, `done` bars and a date axis with adaptive ticks.
- **Journey charts** (`journey`): sections, tasks, 1…5 score dots and actors.
- `samples/mermaid.md` — one page that exercises every supported diagram.

### Changed

- One dispatcher (`MermaidDiagrams`) chooses the parser by header. Unsupported types
  (`erDiagram`, `gitGraph`, `mindmap`, `timeline`, `quadrantChart`, `sankey`, `xychart`, `C4`,
  `block`) now say exactly that in the fallback card instead of a generic message.
- `Flowchart.LeftToRight` is settable so a `direction` statement can switch a state diagram's axis.

### Fixed

- A layer with no nodes could crash the flowchart layout (`Sequence contains no elements`) when a
  dependency chain skipped layer numbers.

## [1.0.0] — 2026-11-08

First release. A native Markdown reader/editor for Windows with its own parser, layout engine and
painter — no browser engine, no runtime dependency.

### Added

**Reading experience**

- Rendered / Source / Editor view modes with per-document state (`Ctrl+R`, `Ctrl+U`, `Ctrl+E`,
  `Ctrl+Shift+M` to cycle).
- Light and dark themes plus "follow Windows", switchable at runtime (`Ctrl+Shift+L`).
- Reading sheet with real margins, 720 pt measure, 1.62 line height, optional serif face.
- Outline sidebar with scroll-spy and click-to-jump (`Ctrl+\`), heading numbering in the gutter.
- Tabs for several documents (`Ctrl+T`, `Ctrl+W`, `Ctrl+Tab`), session restore of up to 8 files.
- Find in every mode (`Ctrl+F`, `F3`), with match count and next/previous.
- Zoom 60 %…260 % through `Ctrl+wheel`, `Ctrl+plus/minus`, `Ctrl+0`.
- File watching with reload, drag-and-drop opening, recent files.

**Markdown**

- CommonMark core: headings (ATX/setext), emphasis, code spans and fences, lists, quotes,
  thematic breaks, inline/reference/shortcut links, autolinks, entities, escapes, hard breaks.
- GFM: tables with per-column alignment, strikethrough, task lists, bare URL autolinking.
- MD.Scribe extensions: GitHub alerts, footnotes (reference and inline), definition lists,
  `==mark==`, `^sup^`, `~sub~`, emoji shortcodes, `{#id}` heading attributes, `[TOC]`,
  YAML front matter, attribute width on images, "universal pattern chains".
- Math: inline `$…$` (currency-safe) and display `$$…$$` through a TeX-subset typesetter.
- Mermaid flowcharts laid out and drawn natively; other diagram types fall back to a labelled
  source card.
- Raw HTML is captured and shown as source, never executed.

**Rendering**

- Own text shaper: word wrapping, mixed runs, pills for inline code, image runs, superscripts,
  footnote markers.
- Code cards with language header, `title="…"` labels, syntax highlighting for ~20 language
  families, per-block copy button, optional wrapping.
- Tables with measured columns, header band, zebra rows, cell-level link hit-testing.
- Callouts, quotes, definition lists, image figures with captions, missing-image placeholders.
- Custom slim scrollbar, smooth scrolling, link hover feedback, heading anchors.

**Editing**

- Editor with soft wrap, syntax colouring, line numbers, current-line highlight, selection,
  double/triple-click selection, block indent (`Tab`/`Shift+Tab`), list continuation on `Enter`,
  undo/redo with typing coalescing, `Ctrl+B/I/K` formatting verbs.
- Optional editor + live preview split (`Ctrl+Shift+P`), re-rendering 220 ms after typing stops.

**Output**

- Headless export: standalone HTML with embedded CSS (light/dark via `prefers-color-scheme`),
  plain text, PNG, Markdown copy.
- Print with pagination through the same layout engine (Print to PDF works out of the box).
- Full command line: file, mode, theme, zoom, width, line, anchor, watch, export, print,
  screenshot, diagnostics, language, session reset.

**Platform**

- Self-contained single-file `mdview.exe` (~50 MB): runs on Windows 10/11 with no .NET installed.
- Per-monitor V2 DPI awareness, dark title bar and rounded corners via DWM.
- Russian and English interface, chosen from the OS language or `--lang`.
- `FrontMatterValue` union type, `closed` AST hierarchies and collection-expression arguments —
  C# 15 on .NET 11.

**Quality**

- 110 automated checks: parser (64), layout (11), theme contrast (24), diagrams/editor (8),
  print pagination (2), plus snapshot and UI-smoke tooling.
- Screenshot suite that drives the real window through every mode and theme and verifies that
  each capture is non-blank.

### Known limitations

- Mermaid support covers flowcharts only.
- Math is a TeX subset (fractions, roots, sums, Greek, scripts); matrices and environments are
  not typeset.
- Emoji coverage is the ~300 most used shortcodes; unknown codes render literally.
- No text selection in rendered mode: copy per block, or "Copy all text" from the menu.
