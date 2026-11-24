# Testing

```powershell
pwsh -File build/build.ps1 -Task test     # everything below, exit code 0/1
```

188 checks; the runner is a console executable (`mdview-tests.exe`) with no test
framework dependency.

| Suite | Checks | What it protects |
| --- | --- | --- |
| `parser` (12 sub-suites) | 65 | Every Markdown construct, front matter, footnotes, chains, entities, statistics |
| `layout` | 13 | Wrapping, anchors, link regions, table width and overflow, zoom, pathological input |
| `math parser` | 9 | Fractions, scripts, roots, fences, matrices, unknown environments, broken input |
| `math layout` | 7 | Box metrics: fractions taller than letters, raised/lowered scripts, growing fences, matrix width |
| `theme` | 25 | WCAG contrast for every palette pair that carries text, both themes, plus registry detection |
| `start-up defaults`, `theme gesture` | 7 | Rendered start-up, remember-mode switch, `--mode` precedence, one-click theme flip |
| `view width`, `tabs`, `dragged tabs`, `menu placement` | 17 | 100/75/50 % narrowing, persisted choice, two documents in tabs, tab payload across processes, drop effects, menu geometry || `diagrams` + `mermaid:*` | 17 | Flowchart and six more diagram types, shapes, labels, dispatch, fallbacks |
| `editor` | 4 | Insert/replace/selection, find hits, caret coordinates |
| `extras` | 19 | Emphasis corners, reference links, definition lists, typography, line breaks, glossary, images |
| `glossary hover` | 3 | Term hit-testing on the real document view |
| `print` | 2 | Pagination terminates and splits a long document |

The runner prints the per-suite tally at the end of every run, so the numbers above cannot drift
silently from the code.

## Tools in the test runner

```powershell
# structural dump of the parsed document
mdview-tests.exe --report samples\demo.md --width 900 [--deep]

# render a document straight to PNG (no window) — used for visual review
mdview-tests.exe --snapshot samples\demo.md out.png --theme dark --width 900 [--zoom 1.25]

# drive the real window through every mode and theme, verify the captures are not blank
mdview-tests.exe --ui-smoke samples\demo.md --out screenshots

# the same, but as documentation captures for one language
mdview-tests.exe --ui-smoke samples\demo.md --out docs\img --lang en   # rendered-light.png …
mdview-tests.exe --ui-smoke samples\ru-пример.md --out docs\img --lang ru  # …-ru.png
```

`--ui-smoke` opens `MainForm`, switches Rendered/Source/Editor and Light/Dark, captures the client
area with `PrintWindow` (the only reliable way for double-buffered controls) and fails if a capture
is smaller than 400×300 or contains fewer than 20 non-background samples.

`--lang en` writes the plain names (`rendered-light.png`) used by `README.md`, `--lang ru` writes the
`-ru` names used by `README.ru.md`. Both sets are produced by one command:

```powershell
pwsh -File build/doc-shots.ps1      # 12 files in docs/img, both languages
```

**A screenshot must not mix languages.** The English page shows English documents and English
interface text; the Russian page shows Russian ones. The picture and the reader's language are part
of the same claim, and a mismatch is a bug, not a cosmetic detail. The comment diagrams (`mermaid*.png`)
are rendered headless with `--export png`, so they contain the document and nothing else.

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

