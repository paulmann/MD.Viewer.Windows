# Markdown support

What the viewer understands, what it does with the rest, and where the code lives.

Legend: **full** — rendered as you would expect; **graceful** — rendered differently but never
broken; **source** — shown as text in a labelled card.

## Blocks

| Syntax | Result | Status |
| --- | --- | --- |
| `# H1` … `###### H6` | Heading with slug id, optional `{#custom-id}`, optional numbering | full |
| `Title` + `=====` / `-----` | Setext H1/H2 | full |
| Paragraphs | Shaped with word wrap, 1.62 line height | full |
| ` ```lang title="file" ` fences | Code card: header, highlighting, copy button, optional wrap | full |
| Indented code (4 spaces) | Code card without header | full |
| `> quote` | Quote with bar and soft background | full |
| `> [!NOTE]` `TIP` `IMPORTANT` `WARNING` `CAUTION` | Alert card with icon, title and tint | full |
| `- a` / `1. a` / nested | Lists, ordered start values, tight/loose spacing | full |
| `- [ ]` / `- [x]` | Task list with drawn checkboxes | full |
| `\| a \| b \|` + delimiter row | Table, per-column alignment, zebra rows | full |
| `---` / `***` / `___` | Thematic break | full |
| `![alt](path "title")` alone | Figure with caption, width attribute honoured | full |
| `Term` + `: Definition` | Definition list; several terms may share one definition | full |
| `[TOC]` | Inline table of contents built from headings | full |
| `$$ … $$` | Display math | full |
| ` ```mermaid ` with `graph` / `flowchart` | Native flowchart: boxes, diamonds, arrows, edge labels | full |
| ` ```mermaid ` with `sequenceDiagram` | Participants, lifelines, solid/dashed arrows, self-messages, notes, `loop`/`alt` frames, `autonumber` | full |
| ` ```mermaid ` with `pie` | Pie chart with legend, percentages, optional `showData` values | full |
| ` ```mermaid ` with `classDiagram` | Class boxes with annotation, members, typed relations, cardinalities | full |
| ` ```mermaid ` with `stateDiagram-v2` | States, transitions with labels, `[*]` start/end dots | full |
| ` ```mermaid ` with `gantt` | Sections, bars, milestones, `after` dependencies, date axis | full |
| ` ```mermaid ` with `journey` | Sections, tasks, 1…5 score dots, actors | full |
| ` ```mermaid ` with `erDiagram`, `gitGraph`, `mindmap`, `timeline`, `quadrantChart`, `sankey`, `xychart`, `C4`, `block` | Source card labelled "MERMAID · … NOT SUPPORTED YET" | graceful |
| ` ```mermaid ` that fails to parse | Source card with the parser's reason | graceful |
| `<div>…</div>` and other raw HTML | Labelled "RAW HTML · NOT RENDERED" card | source |
| YAML front matter | Parsed into title/description/tags/author/date/draft/toc/numbering | full (not drawn as a block) |
| Footnotes `[^1]: …` | Collected at the bottom with numbers | full |

## Inlines

| Syntax | Result | Status |
| --- | --- | --- |
| `**bold**`, `*italic*`, `***both***` | Styled runs | full |
| `~~strike~~`, `~sub~`, `^sup^`, `==mark==` | Styled runs | full |
| `` `code` `` | Mono pill with tinted background | full |
| `[text](url "title")` | Link, hover underline, click opens | full |
| `[text][ref]`, `[ref][]`, `[ref]` | Reference links via `[ref]: url` | full |
| `<https://…>`, bare `https://…`, `www.…` | Autolinked | full |
| `[x](javascript:…)` | Neutralised: link text kept, href dropped | graceful |
| `:rocket:` and ~255 common shortcodes | Emoji | full |
| `:not_a_code:` | Literal text | graceful |
| `$e^{i\pi}+1=0$` | Inline math (TeX subset) | full |
| `$5 and $10` | Stays prose (currency rule) | full |
| `\*escaped\*` | Literal characters | full |
| `&amp;`, `&#8594;` | Decoded entities | full |
| `(A) --> [rel] --> (B)` in a code span | Chain graphic | full |
| `<kbd>`-style inline HTML | Literal text (never parsed as HTML) | source |

## Text layout rules

* Word wrap is greedy at spaces; a single token longer than the column is split by characters.
* Lines are broken after a hard break (two spaces, `\`, or a single newline — MD.Viewer legacy
  behaviour, on by default).
* Code blocks wrap by characters when "Wrap code" is on, keeping the card width fixed.
* Tables measure intrinsic column widths, then distribute the available width; a table never
  exceeds the reading column.

## Code highlighting

Language families recognised (aliases in parentheses):

* **C-like**: c, cpp, cs, java, js, ts, jsx, tsx, go, rust, php, swift, kotlin, scala, dart
* **Python**: py, python, python3
* **Shell**: sh, bash, zsh, console · **PowerShell**: ps1, powershell, pwsh
* **SQL**: sql, mysql, postgresql, sqlite, tsql
* **Data**: json, json5, yaml, yml, toml, ini, conf, env, properties
* **Markup**: html, xml, xaml, svg, vue, csproj · **CSS**: css, scss, less
* **Other**: markdown, diff, patch, gitignore, editorconfig

Anything else renders as plain monospace text — never an error.

## Glossary tooltips

Any two-column table is a glossary source, exactly like MD.Scribe: the first column is a term, the
second its definition, and `Term (ABBR)` registers the abbreviation, the long form and the combined
form. Hovering the term anywhere in the body shows a floating definition card; nothing is
duplicated in the document.

```markdown
| Term | Definition |
| ---- | ---------- |
| CSP (Content Security Policy) | A browser policy header that restricts sources. |
```

## Typography and line breaks

Two switches in **View**:

* **Single Enter breaks the line** (on by default — MD.Viewer legacy). Turn it off for classic
  CommonMark paragraph flow; two trailing spaces and a trailing `\` still break the line.
* **Typographic replacements** (off by default): `---` → —, `--` → –, `...` → …, `(c)/(r)/(tm)`,
  `!=` → ≠, `<=` → ≤, `>=` → ≥, `+-` → ±. Code spans and code blocks are never touched, and
  straight quotes stay straight because documentation is full of paths and measurements.

## Known gaps

What is **not** implemented, so you can plan around it:

* **Inline HTML is never parsed.** `<kbd>`, `<details>`, `<sub>` appear as literal text; block HTML
  is shown in a labelled card. This is deliberate: documents cannot run or style anything.
* **Remote images are not fetched** — the viewer is offline by design; only local files and
  `data:` URIs load.
* **Emphasis follows the common cases**, not the full CommonMark delimiter-stack algorithm: nested
  and adjacent emphasis work, but pathological runs such as `***a**b*` are not guaranteed.
* **Entities cover the common set** (~130 named plus numeric), not all 2200 HTML5 names.
* **Setext headings** apply to a single-line paragraph.
* **Definition lists** accept up to eight term lines per block.
* **Mermaid**: `erDiagram`, `gitGraph`, `mindmap`, `timeline`, `quadrantChart`, `sankey`,
  `xychart`, `C4` and `block` show source. Frames (`loop`/`alt`/`opt`) are labelled bands;
  `activate`/`deactivate` are ignored.
* **Math is a TeX subset**: no matrices, `\begin{}` environments or multi-line alignment.

## Where it lives

| Concern | File |
| --- | --- |
| Blocks, inlines, AST | `src/MdViewer.Core/Markdown/BlockParser.cs`, `InlineParser.cs`, `Ast.cs` |
| Front matter, slugs, emoji, entities | `src/MdViewer.Core/Markdown/FrontMatter.cs`, `Slugger.cs`, `Emoji.cs`, `Entities.cs` |
| Glossary and statistics | `src/MdViewer.Core/Markdown/MarkdownParser.cs` |
| Typographic replacements | `src/MdViewer.Core/Markdown/Typography.cs` (`SmartTypography`) |
| Code highlighting | `src/MdViewer.Core/Highlighting/SyntaxHighlighter.cs` |
| Diagrams | `src/MdViewer.Core/Diagrams/*`, painters in `src/MdViewer.Render/Layout` |
| HTML and text export | `src/MdViewer.Core/Export/HtmlExporter.cs` |

Front matter is available to the application (title, statistics, TOC flag) but is not drawn as a
card of its own.
