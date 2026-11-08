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
| `Term` + `: Definition` | Definition list | full |
| `[TOC]` | Inline table of contents built from headings | full |
| `$$ … $$` | Display math | full |
| ` ```mermaid ` with `graph TD/LR` | Native flowchart drawing | full |
| ` ```mermaid ` with other diagram types | Source card labelled "MERMAID" | graceful |
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
| `:rocket:` and ~300 common shortcodes | Emoji | full |
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

## Known gaps

* Mermaid: sequence, class, state, gantt, pie and ER diagrams show source.
* Math: no matrices, `\begin{}` environments, or multi-line alignment; unknown commands pass
  through as text.
* Emoji: unknown codes pass through; the table covers the common ~300.
* Front matter is available to the app (title, stats, TOC flag) but is not drawn as a card.
