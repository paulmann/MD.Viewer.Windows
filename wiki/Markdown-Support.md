# Markdown support

CommonMark core plus the GFM and MD.Scribe extensions. The contract, including what degrades, is
[docs/MARKDOWN.md](https://github.com/paulmann/MD.Viewer.Windows/blob/main/docs/MARKDOWN.md).

| Area | Covered |
| --- | --- |
| Headings | ATX and setext, `{#custom-id}`, optional numbering |
| Emphasis | bold, italic, both, strike, `==mark==`, `^sup^`, `~sub~` |
| Code | fenced with `title="…"`, indented, highlighting for ~20 language families, copy button |
| Tables | per-column alignment, zebra rows, glossary tooltips from two-column tables |
| Lists | nested, ordered with start values, task lists with drawn checkboxes |
| Quotes | quotes and GitHub alerts (`> [!NOTE]` … `> [!CAUTION]`) |
| Footnotes | reference and inline, collected at the bottom |
| Extras | `[TOC]`, YAML front matter, definition lists, emoji shortcodes (255), typographic replacements |
| Formulas | `$inline$`, `$$display$$` — see [Formulas](Formulas) |
| Diagrams | Mermaid: `flowchart`/`graph`, `sequenceDiagram`, `pie`, `classDiagram`, `stateDiagram-v2`, `gantt`, `journey` |
| Images | resolved relative to the document, captions from alt text, placeholder when missing |

## Deliberate limits

* **Raw HTML is never executed.** It is shown as source in a labelled card — a security decision, not
  a missing feature.
* **Nothing is fetched from the network.** Remote images are marked as not loaded; the application
  makes no requests at all.
* Unsupported Mermaid types (`erDiagram`, `gitGraph`, `mindmap`, `timeline`, `quadrantChart`,
  `sankey`, `xychart`, `C4`, `block`) show a source card with a diagnostic instead of breaking the
  page.
