# Changelog

All notable changes to this project are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
