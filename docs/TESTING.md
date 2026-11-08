# Testing

```powershell
pwsh -File build/build.ps1 -Task test     # everything below, exit code 0/1
```

121 checks across eight suites; the runner is a console executable (`mdview-tests.exe`) with no test
framework dependency.

| Suite | Checks | What it protects |
| --- | --- | --- |
| `parser` | 63 | Every Markdown construct, front matter, footnotes, chains, entity decoding, statistics |
| `layout` | 11 | Wrapping inside the column, anchors, link regions, tables filling the width, zoom, pathologies |
| `theme` | 24 | WCAG contrast for every palette pair that carries text, in both themes |
| `diagrams` | 15 | Flowchart parsing, shapes, labels, subgraphs, fallbacks, every type lays out |
| `editor` | 4 | Insert/replace/selection, find hits, caret coordinates |
| `print` | 2 | Pagination terminates and splits a long document |

Mermaid is covered by its own suites inside `diagrams`: `mermaid: sequence`, `mermaid: pie`,
`mermaid: class`, `mermaid: state`, `mermaid: gantt`, `mermaid: journey` and `mermaid: dispatch`.

## Tools in the test runner

```powershell
# structural dump of the parsed document
mdview-tests.exe --report samples\demo.md --width 900 [--deep]

# render a document straight to PNG (no window) — used for visual review
mdview-tests.exe --snapshot samples\demo.md out.png --theme dark --width 900 [--zoom 1.25]

# drive the real window through every mode and theme, verify the captures are not blank
mdview-tests.exe --ui-smoke samples\demo.md --out screenshots
```

`--ui-smoke` opens `MainForm`, switches Rendered/Source/Editor and Light/Dark, captures the client
area with `PrintWindow` (the only reliable way for double-buffered controls) and fails if a capture
is smaller than 400×300 or contains fewer than 20 non-background samples.

## Visual review helpers

Two Python scripts (Pillow) make screenshots checkable without an image viewer:

```powershell
python build\ascii_preview.py docs\img\rendered-light.png 110   # coarse luminance map
python build\probe.py docs\img\rendered-light.png               # colours at named points + ink density
```

`probe.py` prints the colour at the toolbar, sidebar, sheet and status bar, plus the ink ratio of
each region — enough to catch a chrome element that stopped painting or a theme that stopped
applying.

## Manual checks worth doing after a layout change

1. `--ui-smoke` captures: toolbar icons visible, sheet centred with margins, outline present.
2. Open a document with a long table and a code fence; scroll to the bottom — the outline highlight
   must follow, the scrollbar thumb must stay inside the track.
3. `Ctrl+F` in rendered mode: matches are painted, `Enter` scrolls to the current one.
4. Editor: type a list item, press `Enter` twice — the list ends; `Tab`/`Shift+Tab` indent blocks.
5. `--export html` and open the file in a browser: colours, tables and code cards must match.
6. `--print` → "Microsoft Print to PDF": page breaks must not cut a code card in half.

## What is not covered

* No golden-image comparison: bitmap diffs are brittle across Windows versions and DPI settings.
  The UI smoke suite checks geometry and ink, not pixel equality.
* Printing is verified through `PrintPageEventArgs` on a bitmap, not through a real printer driver.
* IME composition and accessibility (screen readers) are not tested.

