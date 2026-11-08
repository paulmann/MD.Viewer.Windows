# Agent guide — MD.Viewer.Windows

Read this before changing anything. It records the rules this codebase is held to, the commands
that must pass, and the traps that cost time the first time round.

## What this is

A native Windows Markdown reader and editor. No browser engine, no framework beyond WinForms, no
NuGet packages, no binary assets. Everything is drawn with GDI+ and everything the app renders is
produced by our own parser, shaper and layout engine.

## Hard rules

1. **Zero dependencies.** No NuGet packages, no WebView2, no bundled fonts or images. Icons,
   the window icon and diagrams are drawn in code. `dotnet list package` must stay empty.
2. **The renderer is the product.** Parser → AST → layout → paint is the only path. Do not add a
   shortcut that renders Markdown some other way; the HTML exporter and the PNG exporter both use
   the same AST.
3. **No HTML execution.** Raw HTML in a document is displayed as source. Never reintroduce a
   WebBrowser/WebView for "just" raw HTML.
4. **Contrast is tested.** Every palette pair that carries text has a WCAG check in
   `ThemeTests`. Changing a colour requires the test to pass, not the test to be relaxed.
5. **Two languages, one file.** UI strings live in `Localization.cs`; a new string needs both the
   English and the Russian arm.
6. **C# 15 only through the pinned SDK.** `global.json` pins .NET 11 RC; `LangVersion` is
   `preview`. Do not lower either to make an older SDK happy.

## Commands

```powershell
pwsh -File build/build.ps1 -Task build     # compile everything
pwsh -File build/build.ps1 -Task test      # build + all checks (must be green)
pwsh -File build/build.ps1 -Task publish   # self-contained single-file mdview.exe
pwsh -File build/build.ps1 -Task shots     # run the UI smoke suite (needs a desktop session)
```

Test runner details: `mdview-tests.exe --snapshot <md> <png>`, `--report <md>`, `--ui-smoke`.

## Environment traps (measured)

* The `dotnet` on PATH is a **runtime-only** copy shipped with the host app; it cannot build.
  `build/build.ps1` resolves a real SDK itself — use the script, not bare `dotnet build`.
* The .NET 11 SDK's NuGet layer fails with
  `Value cannot be null. (Parameter 'path1')` when `APPDATA`/`LOCALAPPDATA` are missing from the
  session. The build script sets them.
* `DOTNET_ROOT` points at that same runtime; an apphost started from the shell therefore looks for
  .NET 11 in the wrong place. The script repoints it.
* PowerShell does **not** wait for a GUI-subsystem executable. Use `Start-Process -Wait` in scripts.
* `Control.DrawToBitmap` renders nothing for double-buffered custom controls. Window captures go
  through `PrintWindow` (`WindowCapture.Save`).

## Layout of the code

| Path | Responsibility |
| --- | --- |
| `src/MdViewer.Core/Markdown` | `SourceText`, `BlockParser`, `InlineParser`, AST, front matter, emoji, slugs |
| `src/MdViewer.Core/Highlighting` | Line tokenizer and ~20 language families |
| `src/MdViewer.Core/Diagrams` | Mermaid parsers: flowchart/state, sequence, pie, class, gantt, journey — and the `MermaidDiagrams` dispatcher |
| `src/MdViewer.Core/Export` | Standalone HTML and plain-text exporters |
| `src/MdViewer.Render/Theming` | Palettes, type scale, metrics |
| `src/MdViewer.Render/Text` | Font book, run model, inline shaper, TeX-to-Unicode |
| `src/MdViewer.Render/Layout` | Block visuals, tables, containers, diagrams, the layout engine |
| `src/MdViewer.Render/Rendering` | `PaintContext` (cached brushes/pens, run painting) |
| `src/MdViewer.App/Controls` | Document view, editor, chrome (toolbar, tabs, outline, find, status) |
| `src/MdViewer.App` | `MainForm` orchestration, CLI, settings, export/print, window chrome |

## Where to add things

* **A new Markdown construct**: parser first (`InlineParser`/`BlockParser`), then a `BlockLayout`
  visual, then a line in `docs/MARKDOWN.md` and a parser test.
* **A new theme**: add a `Palette` in `ThemeCatalog`, extend `ThemeTests` with the pairs that carry
  text, then let `MarkdownRenderer.SetTheme` do the rest.
* **A new CLI switch**: `Cli.cs` (parse + help text), consume it in `Program.cs` or `MainForm`.
* **A new diagram type**: a parser in `MdViewer.Core/Diagrams` returning a model derived from
  `MermaidDiagram`, a case in `MermaidDiagrams.TryParse`, a painter in `MdViewer.Render/Layout`
  with a `Create(...)` factory, a case in `LayoutEngine.LayoutMermaid`, then a parser test, a
  layout test and a line in `docs/MARKDOWN.md`. Never let a bad diagram throw — fall back to
  `MermaidVisual` with the parser's reason.
* **A new string**: `Localization.cs`, both arms, then use `L.T("key")`.

## Performance notes

* Layout is measured once per width/theme/zoom; `MarkdownRenderer.Invalidate()` is the reset.
* Text width is cached per (font, string) in `FontBook`; avoid measuring the same word in a loop.
* The document view paints only blocks intersecting the viewport, but it still measures the whole
  document — acceptable up to a few thousand blocks, which is far beyond normal documentation.
