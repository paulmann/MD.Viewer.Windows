## Formulas

`$inline$` and `$$display$$` are parsed into a TeX syntax tree and **typeset by the viewer itself** —
no MathJax, no KaTeX, no web fonts, no additional dependency. The upright face is Cambria Math, which
ships with Windows.

### Supported

| Construct | Example |
| --- | --- |
| Fractions | `\frac{a}{b}`, `\dfrac`, `\tfrac`, `\binom{n}{k}` |
| Roots | `\sqrt{x}`, `\sqrt[3]{x^3+y^3}` |
| Scripts | `x_i`, `x^2`, `x_{1,2}`, `x'`, scripts on operators |
| Fences that grow | `\left( … \right)`, `[ ]`, `\{ \}`, `⟨ ⟩`, `\|` |
| Matrices | `matrix`, `pmatrix`, `bmatrix`, `Bmatrix`, `vmatrix`, `Vmatrix`, `cases`, `aligned`, `array` |
| Accents | `\hat`, `\bar`, `\vec`, `\dot`, `\ddot`, `\tilde`, `\overline`, `\underline` |
| Text and alphabets | `\text`, `\mathrm`, `\mathbf`, `\mathit`, `\mathtt`, `\mathsf`, `\mathbb`, `\mathcal`, `\mathfrak` |
| Functions | `\sin`, `\cos`, `\log`, `\ln`, `\lim`, `\max`, `\min`, `\operatorname{sgn}` |
| Big operators | `\sum`, `\prod`, `\int`, `\oint`, `\bigcup` — with limits above/below in display style |
| Spacing | `\quad`, `\qquad`, `\,`, `\;`, `\:`, `\!`, `\hspace{1em}` |
| Symbols | the usual Greek, relation, arrow and set tables |

Multi-line display math works: `\\` starts a new row, `&` marks an alignment point:

```markdown
$$
\begin{aligned}
(a+b)^2 &= a^2 + 2ab + b^2 \\
(a-b)^2 &= a^2 - 2ab + b^2
\end{aligned}
$$
```

### Not supported

* `\label` / `\tag` / `\ref` — there is no equation numbering.
* Colours (`\color`, `\textcolor`) are parsed and ignored.
* Environments outside the list above (`tikzcd`, `alignat`, …) are printed as their source.

Unknown commands and broken input **never throw**: they are shown as text and the rest of the
document renders normally.

### Exports

The screen, the printer and the PNG export share the same typeset layout. The HTML export keeps the
TeX source in `<span class="math-inline">` so it can be handed to a web renderer later.
