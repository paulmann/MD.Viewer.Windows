## Formulas

`$inline$` and `$$display$$` are read by the built-in TeX parser and typeset by the built-in layout
engine. The **vocabulary is KaTeX's** — a formula written for KaTeX or MathJax parses here — but
nothing is loaded: no MathJax, no KaTeX, no web fonts, no network. The upright face is Cambria Math,
which ships with Windows.

### Structures

| Construct | Commands |
| --- | --- |
| Fractions | `\frac`, `\dfrac`, `\tfrac`, `\cfrac`, `\genfrac{}{}{}{}{a}{b}`, and the infix `a \over b`, `\atop`, `\above` |
| Binomials | `\binom`, `\dbinom`, `\tbinom`, `n \choose k`, `n \brace k`, `n \brack k` |
| Roots | `\sqrt{x}`, `\sqrt[3]{x}` |
| Scripts | `x_i`, `x^2`, `x_i^j`, `\sum_{i=1}^{n}`, `\lim_{x\to0}` — limits go above and below an operator in display style |
| Fences | `\left( … \middle| … \right)`, every delimiter name, `\big`/`\Big`/`\bigg`/`\Bigg` and their `l`/`r` forms, `\left.` / `\right.` |
| Above and below | `\overset`, `\underset`, `\stackrel`, `\overbrace`/`\underbrace` (+ paren, group, bracket variants), `\substack` |
| Frames and strikes | `\boxed`, `\fbox`, `\cancel`, `\bcancel`, `\xcancel` |
| Spacing | `\quad`, `\qquad`, `\,`, `\;`, `\:`, `\!`, `\enspace`, `\thinspace`, `\medspace`, `\thickspace`, `\negthinspace`, `\negmedspace`, `\negthickspace`, `\hspace{1em}`, `\hspace*`, `\kern`, `\mkern`, `\mskip` |
| Visibility | `\phantom`, `\hphantom`, `\vphantom`, `\smash`, `\llap`, `\rlap`, `\clap`, `\raisebox`, `\rule` |
| Faces | `\text`, `\textrm`, `\textsf`, `\texttt`, `\textbf`, `\textit`, `\textup`, `\textnormal`, `\mathrm`, `\mathbf`, `\mathit`, `\mathtt`, `\mathsf`, `\mathbb`, `\mathcal`, `\mathscr`, `\mathfrak`, `\boldsymbol`, `\bm`, `\pmb`, `\mathbfit` |
| Functions | `\sin` … `\csc`, `\sinh` … `\artanh`, `\log`, `\ln`, `\lg`, `\exp`, `\det`, `\gcd`, `\lim`, `\liminf`, `\limsup`, `\max`, `\min`, `\sup`, `\inf`, `\Pr`, `\sgn`, `\operatorname{sgn}`, `\operatorname*{arg\,min}` |
| Classes | `\mathop`, `\mathbin`, `\mathrel`, `\mathord`, `\mathopen`, `\mathclose`, `\mathpunct` |
| Moduli | `\pmod{n}`, `\bmod`, `\mod`, `\pod` |
| Undoing | `\not=`, `\not\in`, `\not\subset`, … |
| Links | `\href{url}{text}`, `\url{…}` |
| Styles | `\displaystyle`, `\textstyle`, `\scriptstyle`, `\scriptscriptstyle`, `\limits`, `\nolimits` |

### Symbols

About 210 commands: Greek with variants (`\epsilon`/`\varepsilon`, `\phi`/`\varphi`, `\varkappa`),
the binary operators (`\oplus` … `\curlyvee`), relations (`\leqslant`, `\precsim`, `\vDash`,
`\pitchfork`, `\between`), arrows of every length and direction (`\longrightarrow`,
`\Longleftrightarrow`, `\rightleftharpoons`, `\twoheadrightarrow`, `\rightsquigarrow`), large operators
(`\sum`, `\iiiint`, `\oiint`, `\bigsqcup`), `\hbar`, `\ell`, `\Re`, `\Im`, `\aleph`, `\beth`, `\imath`,
`\jmath`, `\wp`, `\infty`, `\partial`, `\nabla`, `\forall`, `\exists`, `\nexists`, the ellipses
(`\ldots`, `\cdots`, `\vdots`, `\ddots`, `\iddots`), corners, `\checkmark`, `\maltese`, `\pounds`,
`\yen`, `\TeX`, `\LaTeX`.

### Environments

`matrix`, `pmatrix`, `bmatrix`, `Bmatrix`, `vmatrix`, `Vmatrix`, `smallmatrix` (and the `*` forms),
`array` with a per-column preamble — `\begin{array}{l|cr}` — `darray`, `subarray`, `cases`, `dcases`,
`rcases`, `aligned`, `alignedat`, `align`, `gather`, `gathered`, `split`, `multline`, `equation`,
`eqnarray`, `displaylines`.

Rows break with `\\`, columns with `&`, and `\hline` / `\hdashline` / `\cline{1-2}` draw rules inside
an array.

```markdown
$$
\begin{array}{c|r}
\hline
n & n^2 \\
\hline
1 & 1 \\
2 & 4 \\
\hline
\end{array}
$$
```

### What is not supported

* **Equation numbering** — `\label`, `\tag`, `\nonumber`, `\notag` are parsed and produce no ink.
* **Colours** — `\color`, `\textcolor`, `\colorbox` are parsed and ignored; a formula has the colour of
  the text around it.
* **`\def` and `\newcommand`** — no macro definitions; every command must be one we know.
* Anything else — an unknown command, an unknown `\begin{}` environment, broken input — is printed as
  its source, in place, and the rest of the page keeps rendering. **A formula can never throw.**

### How it is verified

`math coverage` in the test suite parses 110 formulas covering roughly 900 commands and requires each
one to produce a laid-out box with real metrics and no fallback node. A vocabulary regression fails the
build; it cannot reach a document as raw TeX.

### Exports

The screen, the printer and the PNG export share the same typeset layout. The HTML export keeps the
TeX source in `<span class="math-inline">` / `<span class="math-display">`, ready for a web renderer.
