using MdViewer.Core.Formulas;
using MdViewer.Render;
using MdViewer.Render.Formulas;
using MdViewer.Render.Text;
using MdViewer.Render.Theming;

namespace MdViewer.Tests;

/// <summary>
/// The KaTeX/MathJax command surface, formula by formula. Every entry must parse without a
/// <see cref="MathUnsupported"/> node and lay out to a box with a positive width — that is what
/// "full MathJax/KaTeX support" means here: no external engine, but the same vocabulary.
/// </summary>
public static class MathCoverageTests
{
    private static readonly string[] Inline =
    [
        // ---------------------------------------------------------------- symbols
        @"\alpha\beta\gamma\delta\epsilon\varepsilon\zeta\eta\theta\vartheta\iota\kappa\varkappa\lambda\mu\nu\xi\pi\varpi\rho\varrho\sigma\varsigma\tau\upsilon\phi\varphi\chi\psi\omega",
        @"\Gamma\Delta\Theta\Lambda\Xi\Pi\Sigma\Upsilon\Phi\Psi\Omega",
        @"\pm\mp\times\div\cdot\ast\star\circ\bullet\oplus\ominus\otimes\oslash\odot",
        @"\cup\cap\setminus\wedge\vee\land\lor\barwedge\veebar\curlywedge\curlyvee",
        @"\dag\ddagger\uplus\sqcup\sqcap\amalg\diamond\bigtriangleup\bigtriangledown\triangleleft\triangleright",
        @"\lhd\rhd\unlhd\unrhd\boxtimes\boxplus\boxminus\boxdot\ltimes\rtimes",
        @"\le\leq\ge\geq\leqq\geqq\leqslant\geqslant\ne\neq\approx\approxeq\cong\equiv\sim\simeq",
        @"\propto\ll\gg\lll\ggg\doteq\doteqdot\fallingdotseq\risingdotseq\triangleq\circeq\eqcirc",
        @"\prec\succ\preceq\succeq\precsim\succsim\subset\supset\subseteq\supseteq\subsetneq\supsetneq",
        @"\sqsubset\sqsupset\sqsubseteq\sqsupseteq\in\notin\ni\owns\notni\perp\parallel\nparallel\mid\nmid",
        @"\models\vdash\dashv\vDash\Vdash\Vvdash\top\bot\smile\frown\between\pitchfork\lessgtr\gtrless",
        @"\trianglelefteq\trianglerighteq\bowtie\leftthreetimes\rightthreetimes\divideontimes\intercal\wr\dotplus",
        @"\to\rightarrow\leftarrow\gets\leftrightarrow\Rightarrow\Leftarrow\Leftrightarrow\implies\impliedby\iff",
        @"\longrightarrow\longleftarrow\longleftrightarrow\Longrightarrow\Longleftarrow\Longleftrightarrow",
        @"\mapsto\longmapsto\hookrightarrow\hookleftarrow\nearrow\searrow\swarrow\nwarrow\uparrow\downarrow\updownarrow",
        @"\Uparrow\Downarrow\Updownarrow\rightharpoonup\rightharpoondown\leftharpoonup\leftharpoondown",
        @"\rightleftharpoons\leftrightharpoons\rightrightarrows\leftleftarrows\rightleftarrows\curvearrowright",
        @"\circlearrowright\circlearrowleft\dashrightarrow\dashleftarrow\twoheadrightarrow\twoheadleftarrow",
        @"\rightarrowtail\leftarrowtail\nrightarrow\nleftarrow\nRightarrow\nLeftarrow\rightsquigarrow\leadsto",
        @"\looparrowright\looparrowleft\Rsh\Lsh\multimap\upuparrows\downdownarrows",
        @"\sum\prod\coprod\int\iint\iiint\iiiint\oint\oiint\oiiint\bigcup\bigcap\bigoplus\bigotimes\bigodot\biguplus\bigsqcup\bigwedge\bigvee",
        @"\infty\partial\nabla\forall\exists\nexists\neg\lnot\emptyset\varnothing\aleph\beth\gimel\daleth",
        @"\hbar\hslash\ell\Re\Im\wp\imath\jmath\angle\measuredangle\sphericalangle",
        @"\triangle\vartriangle\triangledown\square\Box\blacksquare\lozenge\blacklozenge\bigcirc\circledS",
        @"\prime\backprime\degree\therefore\because\ldots\dots\cdots\vdots\ddots\iddots\dotsc\dotsb\dotsm\dotsi\dotso",
        @"\S\P\copyright\circledR\pounds\yen\checkmark\maltese\dag\ddag\complement\mho\eth\Finv\Game",
        @"\ulcorner\urcorner\llcorner\lrcorner\backslash",
        // ------------------------------------------------------------- structures
        @"\frac{a}{b}\dfrac{a}{b}\tfrac{a}{b}\cfrac{a}{b}\genfrac{(}{)}{0pt}{}{a}{b}\genfrac{}{}{1pt}{}{a}{b}",
        @"\binom{n}{k}\dbinom{n}{k}\tbinom{n}{k}{n \choose k}{n \brace k}{n \brack k}",
        @"a \over b, c \atop d, e \above{2pt} f",
        @"\sqrt{x}\sqrt[3]{x^3+y^3}\sqrt[\,n]{a}",
        @"x_i x^2 x_{i,j} x^{2n+1} x_i^j {x_i}^2 \sum_{i=1}^{n} \int_0^\infty e^{-x}\,dx",
        @"\lim_{x\to0}\frac{\sin x}{x}=1\quad \limsup_{n\to\infty} a_n \quad \max_{i} x_i",
        @"\left( \frac{a}{b} \right) \left[ x \right] \left\{ y \right\} \left\langle z \right\rangle",
        @"\left\lfloor x \right\rfloor \left\lceil y \right\rceil \left| z \right| \left\| v \right\|",
        @"\left. \frac{df}{dx} \right|_{x=0} \left( a \middle| b \right)",
        @"\big( \Big[ \bigg\{ \Bigg\langle x \Bigg\rangle \bigg\} \Big] \big)",
        @"\overset{def}{=} \underset{n\to\infty}{\lim} \stackrel{?}{=}",
        @"\overbrace{a+b+\cdots+z}^{26} \underbrace{x+y}_{2} \overparen{abc} \underbracket{uv}",
        @"\boxed{E=mc^2} \fbox{x} \cancel{x} \bcancel{y} \xcancel{z}",
        @"\boldsymbol{\alpha} \bm{\beta} \pmb{\gamma} \mathbfit{\delta} \mathnormal{x}",
        @"\mathop{\mathrm{argmax}}_x \mathbin{\star} \mathrel{\sim} \mathopen{(} \mathclose{)} \mathpunct{,}",
        @"\phantom{x} \hphantom{x} \vphantom{x} \smash{x} \llap{x} \rlap{x} \clap{x}",
        @"\rule{1em}{0.5pt} \raisebox{0.5em}{x} \hspace{1em} \hspace*{2em} \kern{3pt} \mkern{2mu} \mskip{4mu}",
        @"a\,b\:c\;d\!e\ f\quad g\qquad h\enspace i\thinspace j\negthinspace k\negmedspace l\negthickspace m",
        @"\text{if } x>0 \text{ and } \textbf{bold} \textit{italic} \texttt{mono} \textsf{sans} \textrm{rm}",
        @"\mathrm{sin} \mathbf{v} \mathit{x} \mathtt{t} \mathsf{s} \mathbb{R} \mathcal{L} \mathfrak{g} \mathscr{F}",
        @"\operatorname{sgn}(x) \operatorname*{arg\,min}_{y} f(y)",
        @"\not= \not\in \not\subset \not\equiv \not\le \not\ge \not\sim \not\approx",
        @"\pmod{n} a \bmod b \pod{m} a \mod n",
        @"\sin^2 x + \cos^2 x = 1, \tan\theta, \log_2 n, \ln x, \exp(x), \det A, \gcd(a,b), \Pr[X]",
        @"\href{https://example.com}{x} \url{https://example.com}",
        @"{\displaystyle \sum_{k=1}^n k} \textstyle\sum_k \scriptstyle x \scriptscriptstyle y",
        @"\left[ \begin{array}{lcr} a & b & c \\ d & e & f \end{array} \right]",
        @"\begin{matrix} a & b \\ c & d \end{matrix}",
        @"\begin{pmatrix} 1 & 0 \\ 0 & 1 \end{pmatrix}",
        @"\begin{bmatrix} 1 & 2 \\ 3 & 4 \end{bmatrix}",
        @"\begin{Bmatrix} a \end{Bmatrix} \begin{vmatrix} a & b \\ c & d \end{vmatrix} \begin{Vmatrix} e \end{Vmatrix}",
        @"\begin{smallmatrix} a & b \\ c & d \end{smallmatrix}",
        @"f(x) = \begin{cases} 1 & x>0 \\ 0 & x\le 0 \end{cases}",
        @"\begin{dcases} a & b \\ c & d \end{dcases} \begin{rcases} a & b \end{rcases}",
        @"\begin{aligned} a &= b + c \\ &= d \end{aligned}",
        @"\begin{align} x &= 1 \\ y &= 2 \end{align}",
        @"\begin{gathered} a \\ b \end{gathered} \begin{split} p &= q \end{split}",
        @"\begin{equation} e^{i\pi}+1=0 \end{equation}",
        @"\begin{array}{c|c} 1 & 2 \\ \hline 3 & 4 \end{array}",
        @"\begin{alignedat}{2} a &= b & c &= d \end{alignedat}",
        @"\begin{multline} a+b+c \\ +d+e \end{multline}",
        @"\begin{subarray}{l} i<j \\ k \end{subarray}",
        @"\sum_{\substack{0<i<m \\ 0<j<n}} P(i,j)",        @"\left( \begin{array}{c} x_1 \\ \vdots \\ x_n \end{array} \right)",
        @"\displaystyle\int_{-\infty}^{\infty} e^{-x^2}\,dx = \sqrt{\pi}",
        @"\frac{\partial^2 u}{\partial x^2} + \frac{\partial^2 u}{\partial y^2} = 0",
        @"\nabla \times \mathbf{B} = \mu_0 \mathbf{J} + \mu_0\varepsilon_0 \frac{\partial \mathbf{E}}{\partial t}",
        @"\hat{a} \bar{b} \vec{c} \dot{d} \ddot{e} \tilde{f} \check{g} \breve{h} \acute{i} \grave{j} \mathring{k}",
        @"\widehat{abc} \widetilde{xyz} \overline{AB} \underline{CD} \overrightarrow{PQ} \overleftarrow{RS}",
        @"\dddot{x} \ddddot{y} \widecheck{z} \overleftrightarrow{AB}",
        @"x' + y'' + z'''",
        @"\color{red} x + \textcolor{blue}{y} \colorbox{yellow}{z}",
        @"\begin{cases} 1 & \text{if } n \text{ is even} \\ -1 & \text{otherwise} \end{cases}",
        @"\binom{n}{k} = \frac{n!}{k!(n-k)!}",
        @"\sum_{k=0}^{n} \binom{n}{k} x^k y^{n-k} = (x+y)^n",
        @"\left[\begin{smallmatrix}1&2\\3&4\end{smallmatrix}\right]^{-1}",
        @"\underbrace{\overbrace{a+b}^{2}+\overbrace{c+d}^{2}}_{\text{Gauss}}",
        @"\sqrt{\frac{1}{n}\sum_{i=1}^{n}(x_i-\bar{x})^2}",
        @"\lim_{n\to\infty}\left(1+\frac{1}{n}\right)^n = e",
        @"\forall\varepsilon>0\;\exists\delta>0:\ |x-a|<\delta \Rightarrow |f(x)-f(a)|<\varepsilon",
        @"A \subseteq B \iff A\cap B = A",
        @"\frac{\mathrm{d}}{\mathrm{d}x}\left(\int_a^x f(t)\,\mathrm{d}t\right) = f(x)",
        @"\begin{array}{rr} 1 & -2 \\ -3 & 4 \end{array}",
        @"\text{\TeX} \text{\LaTeX} \TeX \LaTeX",
        @"\mathbb{N}\subset\mathbb{Z}\subset\mathbb{Q}\subset\mathbb{R}\subset\mathbb{C}",
        @"\mathcal{O}(n\log n) \quad \Theta(n^2) \quad \Omega(1)",
        @"\left\{ x \in \mathbb{R} \mid x > 0 \right\}",
        @"|x| = \begin{cases} x & x\ge 0 \\ -x & x<0 \end{cases}",
        @"\int\limits_0^1 f(x)\,dx \sum\nolimits_{i} a_i",
        @"x_{\text{max}} + y_{\text{min}}",
        @"\frac{1}{2}\left(\frac{a}{b}\right)^{\!2}",
        @"\Bigl( \frac{a}{b} \Bigr) \bigl[ x \bigr]",
        @"\substack{a\\b} \text{ in a row}",
        @"\delta_{ij} \varepsilon_{ijk} \Gamma^{\mu}_{\nu\lambda}",
        @"\vec{F} = m\vec{a} \quad \oint_{\partial S}\vec{B}\cdot\mathrm{d}\vec{l} = \mu_0 I",
        @"\langle \psi | \hat{H} | \psi \rangle = E",
        @"\left( \frac{\partial^2}{\partial x^2} \right) f",
        @"e^{i\theta} = \cos\theta + i\sin\theta",
        @"\log_{10} x \quad \ln x \quad \lg x \quad \log x",
        @"\max(a,b) \min(c,d) \sup A \inf B \arg\max_z f(z)",
        @"\overline{z} = a - bi \quad |z| = \sqrt{a^2+b^2}",
        @"\liminf_{n} a_n \le \limsup_{n} b_n",
    ];

    private static readonly string[] Metadata =
    [
        @"\tag{1} \label{eq:1} \nonumber \notag",
        @"\begin{align} x &= 1 \label{a} \tag{a} \end{align}",
    ];

    public static void Run()
    {
        Harness.Suite("math coverage");

        using var fonts = new FontBook(1f);
        Theme theme = ThemeCatalog.Resolve("light");
        var layout = new MathLayout(fonts, theme.Colors.Text);
        var failures = new List<string>();

        foreach (string tex in Inline)
        {
            MathLines lines = MathParser.Parse(tex, display: false);
            string unsupported = FindUnsupported(lines);
            if (lines.HasErrors || unsupported.Length > 0)
            {
                failures.Add(unsupported.Length > 0 ? $"{tex}\n      unsupported: {unsupported}" : tex);
                continue;
            }

            try
            {
                MathBox box = layout.Build(lines, 16f, display: false);
                if (box.Width <= 0f || box.Height <= 0f)
                {
                    failures.Add($"{tex}\n      empty box {box.Width}x{box.Height}");
                }
            }
            catch (Exception ex)
            {
                failures.Add($"{tex}\n      threw {ex.GetType().Name}: {ex.Message}");
            }
        }

        Harness.Test($"all {Inline.Length} KaTeX/MathJax constructs parse and lay out", () =>
            Harness.True(failures.Count == 0, "failures:\n    - " + string.Join("\n    - ", failures)));

        // \tag, \label and the numbering switches produce no ink at all, so only their
        // parse is checked — a zero-width box is the correct result for them.
        Harness.Test("numbering metadata parses without a single unsupported node", () =>
        {
            var bad = new List<string>();
            foreach (string tex in Metadata)
            {
                MathLines lines = MathParser.Parse(tex, display: true);
                string unsupported = FindUnsupported(lines);
                if (lines.HasErrors || unsupported.Length > 0)
                {
                    bad.Add(tex + " -> " + unsupported);
                }
            }

            Harness.True(bad.Count == 0, string.Join("; ", bad));
        });

        // The corpus is only meaningful if it is big and if the parser really sees the constructs.
        Harness.Test("the coverage corpus is wide enough to mean something", () =>
        {
            Harness.True(Inline.Length >= 100, $"corpus {Inline.Length}");
            int commands = Inline.Sum(line => line.Count(c => c == '\\'));
            Harness.True(commands >= 800, $"commands {commands}");
        });

        // Symbols resolve to real glyphs, not to their TeX source.
        Harness.Test("every symbol command has a glyph", () =>
        {
            string[] probes = ["\\alpha", "\\hbar", "\\Re", "\\nexists", "\\varnothing", "\\lll", "\\twoheadrightarrow", "\\oiint", "\\beth"];
            foreach (string probe in probes)
            {
                MathLines lines = MathParser.Parse(probe, display: false);
                Harness.True(FindUnsupported(lines).Length == 0, probe + " unsupported");
                string plain = MathParser.ToPlainText(probe, display: false);
                Harness.False(plain.Contains('\\'), probe + " kept its source: " + plain);
            }
        });

        // Unknown material keeps the promise: shown as source, never thrown.
        Harness.Test("an unknown command degrades to its source", () =>
        {
            MathLines lines = MathParser.Parse(@"x + \notarealcommand{y}", display: false);
            Harness.True(lines.HasErrors, "errors flagged");
            Harness.Contains(MathParser.ToPlainText(@"\notarealcommand{y}", display: false), "notarealcommand", "source kept");
        });
    }

    private static string FindUnsupported(MathNode node) => node switch
    {
        MathUnsupported unsupported => "\\" + unsupported.Tex.TrimStart('\\'),
        MathRow row => row.Children.Select(FindUnsupported).FirstOrDefault(s => s.Length > 0) ?? string.Empty,
        MathFraction fraction => First(FindUnsupported(fraction.Numerator), FindUnsupported(fraction.Denominator)),
        MathRadical radical => First(FindUnsupported(radical.Body), radical.Index is null ? string.Empty : FindUnsupported(radical.Index)),
        MathScripts scripts => First(
            scripts.Base is null ? string.Empty : FindUnsupported(scripts.Base),
            First(scripts.Sub is null ? string.Empty : FindUnsupported(scripts.Sub),
                scripts.Sup is null ? string.Empty : FindUnsupported(scripts.Sup))),
        MathLeftRight delimited => FindUnsupported(delimited.Body),
        MathAccent accent => FindUnsupported(accent.Body),
        MathMatrix matrix => matrix.Rows
            .SelectMany(r => r)
            .Select(FindUnsupported)
            .FirstOrDefault(s => s.Length > 0) ?? string.Empty,
        MathStack stack => First(
            FindUnsupported(stack.Body),
            First(stack.Above is null ? string.Empty : FindUnsupported(stack.Above),
                stack.Below is null ? string.Empty : FindUnsupported(stack.Below))),
        MathStacked stacked => stacked.Lines.Select(FindUnsupported).FirstOrDefault(s => s.Length > 0) ?? string.Empty,
        MathFramed framed => FindUnsupported(framed.Body),
        MathCancel cancel => FindUnsupported(cancel.Body),        MathShift shift => FindUnsupported(shift.Body),
        MathClass klass => FindUnsupported(klass.Body),
        MathBold bold => FindUnsupported(bold.Body),
        MathPhantom phantom => FindUnsupported(phantom.Body),
        MathStyle style => FindUnsupported(style.Body),
        _ => string.Empty,
    };

    private static string First(string first, string second) => first.Length > 0 ? first : second;

    private static string FindUnsupported(MathLines lines) =>
        lines.Rows.Select(FindUnsupported).FirstOrDefault(s => s.Length > 0) ?? string.Empty;
}
