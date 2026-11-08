# Design system

The interface is small on purpose. These are the rules the code follows; the numbers live in
`src/MdViewer.Render/Theming/` and `src/MdViewer.App/Controls/`.

## Type scale

| Role | Size | Weight | Line height |
| --- | --- | --- | --- |
| H1 | 31 | 650 | 1.24 |
| H2 | 24.5 | 650 | 1.24 (+ hairline rule) |
| H3 | 20 | 650 | 1.24 |
| H4 | 17 | 650 | 1.24 |
| H5–H6 | 15.5 / 13.8 | 650 | 1.24 |
| Body | 16 | 400 | 1.62 |
| Code | 13.6 | 400 | 1.55 |
| UI chrome | 12.4 | 400/600 | — |
| Status bar | 11.2 | 400 | — |

Reading measure: **720 pt maximum**, 60–85 characters per line. Serif option swaps Segoe UI for
Cambria without touching the scale.

## Spacing rhythm

Everything is a multiple of 4, most gaps are 8 or 12:

| Element | Gap |
| --- | --- |
| Paragraph after | 13 |
| Before H1/H2 | 34 / 30 |
| After H1/H2 | 12 / 11 |
| Between blocks (code, table, list, quote) | 16 |
| List item | 5 (tight) / 9 (loose) |
| Sheet padding | 56 × 44 (sides × top) |

## Colour

Two neutral scales with one accent:

* **Light** — canvas `#F1F2F4`, sheet `#FFFFFF`, text `#15171C`, muted `#5B6472`, border `#E4E6EB`,
  accent `#2563EB`.
* **Dark** — canvas `#0E1014`, sheet `#171A20`, text `#E8EAEE`, muted `#A0A8B4`, border `#262A33`,
  accent `#7AA2FF`.

Semantic colours for alerts (note/tip/important/warning/caution) are separate bar + tint pairs, and
syntax highlighting has its own six-token palette per theme.

**Contrast is enforced by tests.** `ThemeTests` checks every pair that carries text — body 7:1,
muted and links 4.5:1, code 7:1, each syntax token 4.5:1, each alert title 4.5:1. Changing a colour
means changing the test with a measurement, not relaxing the threshold.

## Shape and elevation

* Radius: 3 (code cards' child), 6 (chips), 8–10 (cards, buttons), sheet is square by design.
* The sheet sits on the canvas with a 1 px border and a 6-step alpha shadow — no gradients.
* Toolbar is 52 px, tabs 36 px (only when more than one document), status bar 28 px.

## Interaction

* Everything clickable is 32 px tall minimum, with a hover fill on the whole hit area.
* Links underline on hover; the pointer becomes a hand over links, copy buttons and tabs.
* The outline tracks the scrolled section continuously; the active row gets the accent tint.
* Find results are painted on top of the layout (amber for matches, accent for the current one) —
  in rendered mode this is the only overlay, because text selection is block-level by design.
* Keyboard first: every action in the toolbar has a shortcut, listed in `F1`.

## Window chrome

Per-monitor V2 DPI awareness, DWM dark-mode title bar that follows the theme, rounded corners on
Windows 11, and a runtime-drawn application icon (no binary assets in the repository).

## Why not standard controls?

A WinForms `TextBox` cannot show code cards, tables or callouts; a `RichTextBox` cannot do the type
scale, the measure or the padding. The document surface is one custom control that paints
`BlockLayout` objects — the same objects the print pipeline and the PNG exporter use, so what you
see, what prints and what exports cannot drift apart.
