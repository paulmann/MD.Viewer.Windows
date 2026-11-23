using MdViewer.Core.Formulas;
using MdViewer.Render.Formulas;
using MdViewer.Render.Text;
using MdViewer.Render.Theming;
using MdViewer.Render;

namespace MdViewer.Tests;

/// <summary>TeX reader and math typesetting: structure, metrics, and graceful failure.</summary>
public static class MathTests
{
    private static MathLines Parse(string tex, bool display = false) => MathParser.Parse(tex, display);

    private static MathBox Build(string tex, float size = 22f, bool display = true)
    {
        var fonts = new FontBook(1f);
        var layout = new MathLayout(fonts, ThemeCatalog.Light.Colors.Text);
        MathBox box = layout.Build(Parse(tex, display), size, display);
        fonts.Dispose();
        return box;
    }

    public static void Run()
    {
        Harness.Suite("math parser");

        Harness.Test("a fraction becomes a fraction node", () =>
        {
            var lines = Parse(@"\frac{a}{b}");
            var row = (MathRow)lines.Rows[0];
            Harness.True(row.Children[0] is MathFraction, "fraction");
            var fraction = (MathFraction)row.Children[0];
            Harness.True(fraction.Barred, "barred");
            Harness.True(fraction.Numerator is MathRow { Children.Count: 1 }, "numerator");
        });

        Harness.Test("scripts attach to the preceding atom", () =>
        {
            var row = (MathRow)Parse("x_i^2").Rows[0];
            Harness.True(row.Children[0] is MathScripts, "scripts");
            var scripts = (MathScripts)row.Children[0];
            Harness.True(scripts.Sub is not null, "sub");
            Harness.True(scripts.Sup is not null, "sup");
        });

        Harness.Test("root with an index keeps both parts", () =>
        {
            var row = (MathRow)Parse(@"\sqrt[3]{x}").Rows[0];
            Harness.True(row.Children[0] is MathRadical, "radical");
            Harness.True(((MathRadical)row.Children[0]).Index is not null, "index");
        });

        Harness.Test("left/right brackets capture their body", () =>
        {
            var row = (MathRow)Parse(@"\left( \frac{1}{2} \right)").Rows[0];
            Harness.True(row.Children[0] is MathLeftRight, "delimited");
            var delimited = (MathLeftRight)row.Children[0];
            Harness.Equal("(", delimited.Open, "open");
            Harness.Equal(")", delimited.Close, "close");
        });

        Harness.Test("matrix environment rows and cells", () =>
        {
            var row = (MathRow)Parse(@"\begin{pmatrix} 1 & 2 \\ 3 & 4 \end{pmatrix}").Rows[0];
            Harness.True(row.Children[0] is MathMatrix, "matrix");
            var matrix = (MathMatrix)row.Children[0];
            Harness.Equal(2, matrix.Rows.Count, "rows");
            Harness.Equal(2, matrix.Rows[0].Count, "columns");
            Harness.Equal("(", matrix.Open, "open fence");
        });

        Harness.Test("double backslash splits display rows", () =>
        {
            var lines = Parse("a = b \\\\ c = d", display: true);
            Harness.Equal(2, lines.Rows.Count, "rows");
        });

        Harness.Test("an unknown environment falls back instead of throwing", () =>
        {
            var lines = Parse(@"\begin{tikzcd} a \end{tikzcd}");
            Harness.True(lines.HasErrors, "error flagged");
            MathBox box = Build(@"\begin{tikzcd} a \end{tikzcd}");
            Harness.True(box.Width >= 0f, "box built");
        });

        Harness.Test("unbalanced input does not throw", () =>
        {
            foreach (string tex in new[] { @"\frac{a}", @"x^", @"\left(", @"\sqrt", "{{{{" })
            {
                MathBox box = Build(tex);
                Harness.True(box.Width >= 0f, "box for " + tex);
            }
        });

        Harness.Test("plain text conversion keeps the letters", () =>
        {
            string text = MathParser.ToPlainText(@"\frac{a}{b} + \sqrt{x}", display: false);
            Harness.Contains(text, "a", "a");
            Harness.Contains(text, "x", "x");
        });

        Harness.Suite("math layout");

        Harness.Test("a fraction is taller than a plain letter", () =>
        {
            MathBox plain = Build("x");
            MathBox fraction = Build(@"\frac{a}{b}");
            Harness.True(fraction.Height > plain.Height * 1.4f, $"fraction {fraction.Height} vs {plain.Height}");
        });

        Harness.Test("a superscript raises the box above a plain letter", () =>
        {
            MathBox plain = Build("x");
            MathBox power = Build("x^2");
            Harness.True(power.Ascent > plain.Ascent, $"ascent {power.Ascent} vs {plain.Ascent}");
            Harness.True(power.Depth <= plain.Depth + 0.5f, "depth unchanged");
        });

        Harness.Test("a subscript lowers the depth", () =>
        {
            MathBox plain = Build("x");
            MathBox sub = Build("x_2");
            Harness.True(sub.Depth > plain.Depth, $"depth {sub.Depth} vs {plain.Depth}");
        });

        Harness.Test("stretched brackets cover their content", () =>
        {
            MathBox plain = Build("x");
            MathBox delimited = Build(@"\left( \frac{a}{b} \right)");
            Harness.True(delimited.Height > plain.Height, "taller");
        });

        Harness.Test("a matrix is wider than one of its cells", () =>
        {
            MathBox cell = Build("1");
            MathBox matrix = Build(@"\begin{pmatrix} 1 & 2 \\ 3 & 4 \end{pmatrix}");
            Harness.True(matrix.Width > cell.Width * 2f, $"width {matrix.Width}");
        });

        Harness.Test("empty formula has zero width", () =>
        {
            MathBox box = Build("   ");
            Harness.True(box.Width < 1f, $"width {box.Width}");
        });

        Harness.Test("display math renders into a bitmap", () =>
        {
            using var renderer = new MarkdownRenderer(ThemeCatalog.Light, 1f);
            renderer.SetDocument(new MdViewer.Core.Markdown.MarkdownParser().Parse("$$\n\\frac{a+b}{2} \\ge \\sqrt{ab}\n$$"));
            using System.Drawing.Bitmap bitmap = renderer.RenderToBitmap(700f, 400);
            Harness.True(bitmap.Width == 700, "width");
        });
    }
}
