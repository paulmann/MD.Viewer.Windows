# Architecture

```
file.md ──► SourceText ──► MarkdownParser ──► MarkdownDocument (AST + outline + footnotes)
                                │
                                ├─► HtmlExporter / PlainTextExporter        (Text output)
                                └─► LayoutEngine ──► DocumentLayout          (Visual output)
                                                        │
                                        ┌───────────────┼────────────────┐
                                        ▼               ▼                ▼
                                  DocumentView     DocumentPrinter   RenderToBitmap
                                   (screen)          (print)          (PNG export)
```

One parser, one measured layout, three consumers. Anything that changes what a document looks like
changes it for the screen, the printer and the exporter at the same time.

## Layers

### `MdViewer.Core` — no UI

| Type | Role |
| --- | --- |
| `SourceText` | Text normalisation (BOM, CRLF, tabs), line index, offset↔line mapping |
| `MarkdownParser` | Orchestrates: front matter, link reference definitions, footnote definitions, block parsing, post-processing, heading ids and numbering, statistics |
| `BlockParser` | Line-oriented block parser; containers recurse on de-indented content |
| `InlineParser` | Emphasis, code spans, links/images/references, autolinks, math, emoji, footnotes, escapes |
| `Ast` | `closed` hierarchies `Block` / `Inline` — exhaustive switches are compiler-checked |
| `SyntaxHighlighter` | Line tokenizer + language families, stateful for block comments and multi-line strings |
| `MermaidFlowchart` | Flowchart subset parser: nodes, shapes, edges, labels |
| `HtmlExporter` | Self-contained HTML with embedded CSS and token classes |
| `PlainTextExporter` | Structure-preserving plain text (copy, export) |

### `MdViewer.Render` — layout and paint

| Type | Role |
| --- | --- |
| `Theme`, `Palette`, `Typography`, `ThemeCatalog` | Design tokens, light/dark palettes, system theme detection |
| `FontBook` | Cached fonts (pixel units), measurement cache, font-file fallbacks |
| `InlineShaper` | Turns inline nodes into `ShapedText` — wrapping, mixed fonts, pills, images, math |
| `LayoutEngine` | Measures blocks into absolutely positioned `BlockLayout` objects |
| `BlockLayout` subclasses | Text, heading, code, table, list, quote, alert, image, diagram, footnotes, TOC… |
| `PaintContext` | Cached brushes/pens, run painting, rounded paths |
| `MarkdownRenderer` | Owns fonts, images and the current layout; paints content and renders bitmaps |

### `MdViewer.App` — shell

| Type | Role |
| --- | --- |
| `Program` | CLI dispatch, headless exports, screenshot mode, fatal handler |
| `MainForm` | Sessions, modes, chrome layout, shortcuts, menus, export and print commands |
| `DocumentView` | Scrolling sheet, link hit-testing, copy buttons, search overlays, smooth scroll |
| `MarkdownEditor` | Custom text editor: wrap, highlighting, selection, undo, list continuation |
| `Chrome.cs` / `Icons.cs` | Toolbar buttons, mode switcher, tabs, outline, find bar, status bar, vector icons |
| `AppSettings` | JSON preferences and session in `%APPDATA%\MD.Viewer` |

## Coordinate model

Everything in the layout is **device pixels at the current zoom and DPI**. `Theme.Scale` is the
zoom, `FontBook.DpiScale` the monitor scale; `LayoutContext.Px(dip)` converts design units once.
Block layouts store absolute sheet coordinates, so hit-testing is a rectangle test and the printer
can translate a page into the same space.

## Threading

Single-threaded by design: parsing, layout and painting all happen on the UI thread, guarded by a
220 ms debounce for live preview. File watching marshals back with `BeginInvoke`. There are no
background tasks, so there is no document state that can be observed half-updated.

## Performance characteristics

* Parse + layout of the 3.5 KB sample: ~30 ms cold, ~6 ms when the layout is cached.
* Layout is recomputed only when width, theme, zoom, or the document text changes.
* Painting walks the block list with an early exit above/below the viewport.
* Text measurement is memoised per (font, string) — wrapping a long paragraph re-measures each
  word once across the whole session.

## Security posture

* No network client exists in the app.
* Raw HTML is never parsed or executed; it is displayed as escaped text.
* `javascript:`/`vbscript:`/`data:text/html` links are stripped at parse time.
* External links open in the default browser through the shell, after the user clicks them.
