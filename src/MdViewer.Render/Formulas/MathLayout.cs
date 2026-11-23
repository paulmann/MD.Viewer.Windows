using System.Drawing;
using MdViewer.Core.Formulas;
using MdViewer.Render.Text;
using FormulaText = MdViewer.Core.Formulas.MathText;

namespace MdViewer.Render.Formulas;

/// <summary>
/// Lays a formula out the way TeX does: every node becomes a box with a width, an
/// ascent and a depth. Sizes are device pixels; the font size is the em.
/// </summary>
public sealed class MathLayout(FontBook fonts, Color color)
{
    private const float Axis = 0.25f;
    private const float RuleThickness = 0.05f;

    private readonly record struct Ctx(float Size, bool Display, int Level)
    {
        public Ctx Script() => this with { Size = Size * 0.72f, Display = false, Level = Level + 1 };

        public Ctx TextStyle() => this with { Display = false, Size = Size * 0.94f };
    }

    public MathBox Build(MathLines lines, float size, bool display)
    {
        var ctx = new Ctx(size, display, 0);
        if (lines.Rows.Count == 1)
        {
            return Build(lines.Rows[0], ctx);
        }

        var stack = new MathStackBox();
        float y = 0f;
        foreach (MathRow row in lines.Rows)
        {
            MathBox rowBox = Build(row, ctx);
            if (stack.Width > 0 || stack.Height > 0)
            {
                y += rowBox.Ascent + ctx.Size * 0.5f;
            }

            stack.Add(rowBox, y, 0f);
            y += rowBox.Depth;
        }

        return stack;
    }

    private MathBox Build(MathNode node, Ctx ctx)
    {
        switch (node)
        {
            case MathRow row:
                return BuildRow(row, ctx);

            case MathAtom atom:
                return BuildAtom(atom, ctx);

            case FormulaText text:
                return BuildText(text, ctx);

            case MathSpace space:
                return new MathSpaceBox(space.Em * ctx.Size, ctx.Size * 0.55f, ctx.Size * 0.2f);

            case MathFraction fraction:
                return BuildFraction(fraction, ctx);

            case MathRadical radical:
                return BuildRadical(radical, ctx);

            case MathScripts scripts:
                return BuildScripts(scripts, ctx);

            case MathLeftRight delimited:
                return BuildDelimited(delimited, ctx);

            case MathAccent accent:
                return BuildAccent(accent, ctx);

            case MathMatrix matrix:
                return BuildMatrix(matrix, ctx);

            case MathStyle style:
                return Build(style.Body, ctx with { Display = style.Display });

            case MathUnsupported unsupported:
                return BuildFallback("\\" + unsupported.Tex.TrimStart('\\'), ctx);

            default:
                return BuildFallback(node.ToString() ?? "?", ctx);
        }
    }

    // ------------------------------------------------------------------- rows

    private MathBox BuildRow(MathRow row, Ctx ctx)
    {
        var box = new MathRowBox();
        float x = 0f;
        MathAtomKind? previous = null;

        foreach (MathNode child in MergeAtoms(row.Children))
        {
            MathBox childBox = Build(child, ctx);
            MathAtomKind kind = KindOf(child);
            if (previous is MathAtomKind prev)
            {
                x += Spacing(prev, kind, ctx.Size);
            }

            box.Add(childBox, x);
            x += childBox.Width;
            previous = kind;
        }

        box.Finish(x);
        if (box.Height <= 0f)
        {
            return new MathSpaceBox(0f, ctx.Size * 0.75f, ctx.Size * 0.22f);
        }

        return box;
    }

    /// <summary>
    /// Neighbouring letters are drawn as one run: GDI+ draws each string on its own, so
    /// splitting "abc" into three atoms loses the face's kerning.
    /// </summary>
    private static List<MathNode> MergeAtoms(IReadOnlyList<MathNode> nodes)
    {
        if (nodes.Count < 2)
        {
            return [.. nodes];
        }

        var merged = new List<MathNode>(nodes.Count);
        var pending = new System.Text.StringBuilder();
        bool pendingItalic = false;

        void Flush()
        {
            if (pending.Length > 0)
            {
                merged.Add(new MathAtom(pending.ToString(), MathAtomKind.Ordinary, pendingItalic));
                pending.Clear();
            }
        }

        foreach (MathNode node in nodes)
        {
            if (node is MathAtom { Kind: MathAtomKind.Ordinary } atom
                && atom.Text.Length > 0
                && (pending.Length == 0 || atom.Italic == pendingItalic))
            {
                pendingItalic = atom.Italic;
                pending.Append(atom.Text);
                continue;
            }

            Flush();
            merged.Add(node);
        }

        Flush();
        return merged;
    }

    private static float Spacing(MathAtomKind previous, MathAtomKind next, float size)
    {
        if (next is MathAtomKind.Close or MathAtomKind.Punctuation)
        {
            return 0f;
        }

        if (previous == MathAtomKind.Open)
        {
            return 0f;
        }

        if (previous == MathAtomKind.Binary || next == MathAtomKind.Binary)
        {
            return size * 0.22f;
        }

        if (previous == MathAtomKind.Relation || next == MathAtomKind.Relation)
        {
            return size * 0.28f;
        }

        if (previous is MathAtomKind.LargeOperator)
        {
            return size * 0.16f;
        }

        if (previous == MathAtomKind.Punctuation)
        {
            return size * 0.16f;
        }

        return 0f;
    }

    private static MathAtomKind KindOf(MathNode node) => node switch
    {
        MathAtom atom => atom.Kind,
        FormulaText text => text.Kind,
        MathScripts scripts => scripts.Base is null ? MathAtomKind.Ordinary : KindOf(scripts.Base),
        _ => MathAtomKind.Ordinary,
    };

    // ------------------------------------------------------------------ atoms

    private MathBox BuildAtom(MathAtom atom, Ctx ctx)
    {
        if (atom.Text.Length == 0)
        {
            return new MathSpaceBox(0f, 0f, 0f);
        }

        float size = ctx.Size;
        if (atom.Kind == MathAtomKind.LargeOperator)
        {
            size *= ctx.Display ? 1.45f : 1.08f;
        }

        string family = atom.Italic ? fonts.MathItalicFamily : fonts.MathFamily;
        FontStyle style = atom.Italic ? FontStyle.Italic : FontStyle.Regular;
        Font font = fonts.PixelFont(family, size, style);
        float width = fonts.Measure(atom.Text, font);
        float ascent = size * (atom.Kind == MathAtomKind.LargeOperator ? 0.78f : 0.75f);
        float depth = size * 0.22f;
        return new MathGlyphBox(atom.Text, font, fonts.Metrics(font).Ascent, width, color, ascent, depth);
    }

    private MathBox BuildText(FormulaText text, Ctx ctx)
    {
        string content = text.Text;
        if (content.Length == 0)
        {
            return new MathSpaceBox(0f, 0f, 0f);
        }

        (string family, FontStyle style) = text.Style switch
        {
            MathTextStyle.Bold => (fonts.MathFamily, FontStyle.Bold),
            MathTextStyle.Italic => (fonts.MathItalicFamily, FontStyle.Italic),
            MathTextStyle.BoldItalic => (fonts.MathItalicFamily, FontStyle.Bold | FontStyle.Italic),
            MathTextStyle.Monospace => (fonts.MathMonospaceFamily, FontStyle.Regular),
            MathTextStyle.SansSerif => (fonts.UiFamily, FontStyle.Regular),
            _ => (fonts.MathFamily, FontStyle.Regular),
        };

        Font font = fonts.PixelFont(family, ctx.Size, style);
        float width = fonts.Measure(content, font);
        return new MathGlyphBox(content, font, fonts.Metrics(font).Ascent, width, color, ctx.Size * 0.75f, ctx.Size * 0.22f);
    }

    private MathBox BuildFallback(string tex, Ctx ctx)
    {
        Font font = fonts.PixelFont(fonts.MathMonospaceFamily, ctx.Size * 0.9f, FontStyle.Regular);
        float width = fonts.Measure(tex, font);
        return new MathFallbackBox(tex, font, fonts.Metrics(font).Ascent, width, color);
    }

    // -------------------------------------------------------------- fractions

    private MathBox BuildFraction(MathFraction fraction, Ctx ctx)
    {
        Ctx inner = ctx.Display ? ctx : ctx.TextStyle();
        MathBox numerator = Build(fraction.Numerator, inner);
        MathBox denominator = Build(fraction.Denominator, inner);

        float axis = ctx.Size * Axis;
        float bar = MathF.Max(1f, ctx.Size * RuleThickness);
        float gap = ctx.Size * 0.17f;
        float pad = ctx.Size * 0.12f;

        float barY = fraction.Barred ? -axis : 0f;
        float top = barY - bar / 2f - gap;
        float bottom = barY + bar / 2f + gap;
        float numeratorBaseline = top - numerator.Depth;
        float denominatorBaseline = bottom + denominator.Ascent;

        float width = MathF.Max(numerator.Width, denominator.Width) + pad * 2f;
        float ascent = -(MathF.Min(numeratorBaseline - numerator.Ascent, barY - bar));
        float depth = MathF.Max(denominatorBaseline + denominator.Depth, barY + bar);

        return new MathFractionBox(numerator, denominator, barY, bar, fraction.Barred,
            width, ascent, depth, numeratorBaseline, denominatorBaseline);
    }

    private MathBox BuildRadical(MathRadical radical, Ctx ctx)
    {
        MathBox body = Build(radical.Body, ctx);
        float em = ctx.Size;
        float signSize = em;
        float rule = MathF.Max(1f, em * RuleThickness);

        // The sign must reach above the radicand: grow it until its ascent covers it.
        for (int step = 0; step < 12; step++)
        {
            Font probe = fonts.PixelFont(fonts.MathFamily, signSize, FontStyle.Regular);
            if (fonts.Metrics(probe).Ascent >= body.Ascent + em * 0.12f || signSize > em * 5f)
            {
                break;
            }

            signSize *= 1.14f;
        }

        Font signFont = fonts.PixelFont(fonts.MathFamily, signSize, FontStyle.Regular);
        float signWidth = fonts.Measure("√", signFont);
        MathBox? index = radical.Index is null ? null : Build(radical.Index, ctx.Script());

        return new MathRadicalBox(body, index, signFont, fonts.Metrics(signFont).Ascent, "√", signWidth, rule, color);
    }

    // ---------------------------------------------------------------- scripts

    private MathBox BuildScripts(MathScripts scripts, Ctx ctx)
    {
        MathBox baseBox = scripts.Base is null
            ? new MathSpaceBox(0f, ctx.Size * 0.75f, 0.22f * ctx.Size)
            : Build(scripts.Base, ctx);

        bool limits = ctx.Display && scripts.Base is not null && KindOf(scripts.Base) == MathAtomKind.LargeOperator;
        MathBox? sup = scripts.Sup is null ? null : Build(scripts.Sup, ctx.Script());
        MathBox? sub = scripts.Sub is null ? null : Build(scripts.Sub, ctx.Script());

        if (limits && (sup is not null || sub is not null))
        {
            return new MathLimitsBox(baseBox, sup, sub, ctx.Size * 0.16f);
        }

        float supBaseline = MathF.Max(baseBox.Ascent - ctx.Size * 0.22f, ctx.Size * 0.42f);
        float subBaseline = MathF.Max(baseBox.Depth * 0.55f, ctx.Size * 0.2f);
        float dx = baseBox.Width - ctx.Size * 0.02f;
        return new MathScriptBox(baseBox, sup, sub, dx, dx, supBaseline, subBaseline);
    }

    // ------------------------------------------------------------- delimiters

    private MathBox BuildDelimited(MathLeftRight delimited, Ctx ctx)
    {
        MathBox body = Build(delimited.Body, ctx);
        float target = body.Height + ctx.Size * 0.16f;
        MathBox open = Delimiter(delimited.Open, target, ctx);
        MathBox close = Delimiter(delimited.Close, target, ctx);

        var box = new MathRowBox();
        float x = 0f;
        box.Add(open, x);
        x += open.Width + ctx.Size * 0.05f;
        box.Add(body, x);
        x += body.Width + ctx.Size * 0.05f;
        box.Add(close, x);
        x += close.Width;
        box.Finish(x);
        return box;
    }

    private MathBox Delimiter(string text, float target, Ctx ctx)
    {
        if (text.Length == 0 || text == ".")
        {
            return new MathSpaceBox(0f, ctx.Size * 0.75f, ctx.Size * 0.22f);
        }

        float axis = ctx.Size * Axis;
        float ascent = target / 2f + axis;
        float depth = MathF.Max(ctx.Size * 0.1f, target / 2f - axis);

        if (text is "|" or "‖" or "||")
        {
            float width = text == "|" ? MathF.Max(1.2f, ctx.Size * 0.05f) : MathF.Max(2f, ctx.Size * 0.12f);
            return new MathDelimiterBox(text, fonts.PixelFont(fonts.MathFamily, ctx.Size), ctx.Size, ctx.Size, line: true,
                color, text == "|" ? width : width / 2f, ascent, depth);
        }

        float size = ctx.Size;
        while (size * 0.75f < target && size < ctx.Size * 5f)
        {
            size *= 1.22f;
        }

        Font font = fonts.PixelFont(fonts.MathFamily, size, FontStyle.Regular);
        float glyphWidth = fonts.Measure(text, font);
        return new MathDelimiterBox(text, font, fonts.Metrics(font).Ascent, size, line: false, color, glyphWidth, ascent, depth)
        {
            AxisOffset = axis,
        };
    }

    // ---------------------------------------------------------------- accents

    private MathBox BuildAccent(MathAccent accent, Ctx ctx)
    {
        MathBox body = Build(accent.Body, ctx);
        float rule = MathF.Max(1f, ctx.Size * RuleThickness);
        bool ruleLike = accent.Mark is "\u00AF" or "\u005F";

        if (ruleLike)
        {
            return new MathAccentBox(body, accent.Mark, fonts.PixelFont(fonts.MathFamily, ctx.Size), ctx.Size,
                0f, 0f, accent.Under, rule: true, rule, color);
        }

        Font markFont = fonts.PixelFont(fonts.MathFamily, ctx.Size * 0.8f, FontStyle.Regular);
        float markWidth = fonts.Measure(accent.Mark, markFont);
        float markX = MathF.Max(0f, (body.Width - markWidth) / 2f);
        // The mark lives in the upper part of its em box, so its baseline sits below
        // the top of the body: that puts the ink just above the letter.
        float markBaseline = -body.Ascent + ctx.Size * 0.3f;
        return new MathAccentBox(body, accent.Mark, markFont, fonts.Metrics(markFont).Ascent, markX, markBaseline,
            under: false, rule: false, rule, color);
    }

    // ---------------------------------------------------------------- matrices

    private MathBox BuildMatrix(MathMatrix matrix, Ctx ctx)
    {
        Ctx inner = ctx with { Display = true };
        float colGap = ctx.Size * 0.5f;
        float rowGap = ctx.Size * 0.4f;

        int columns = matrix.Rows.Count == 0 ? 0 : matrix.Rows.Max(r => r.Count);
        if (columns == 0)
        {
            return new MathSpaceBox(0f, ctx.Size * 0.6f, ctx.Size * 0.2f);
        }

        var cells = new MathBox[matrix.Rows.Count, columns];
        var columnWidths = new float[columns];
        var rowAscent = new float[matrix.Rows.Count];
        var rowDepth = new float[matrix.Rows.Count];

        for (int r = 0; r < matrix.Rows.Count; r++)
        {
            IReadOnlyList<MathNode> row = matrix.Rows[r];
            for (int c = 0; c < columns; c++)
            {
                MathBox cell = c < row.Count ? Build(row[c], inner) : new MathSpaceBox(0f, 0f, 0f);
                cells[r, c] = cell;
                columnWidths[c] = MathF.Max(columnWidths[c], cell.Width);
                rowAscent[r] = MathF.Max(rowAscent[r], cell.Ascent);
                rowDepth[r] = MathF.Max(rowDepth[r], cell.Depth);
            }
        }

        float totalWidth = columnWidths.Sum() + colGap * (columns - 1);
        var grid = new MathMatrixBox();

        // Rows are stacked around the first baseline; the finished grid is then
        // centred on the math axis, the way TeX sets matrices.
        var baselines = new float[matrix.Rows.Count];
        for (int r = 0; r < matrix.Rows.Count; r++)
        {
            if (r == 0)
            {
                baselines[r] = 0f;
                continue;
            }

            baselines[r] = baselines[r - 1] + rowDepth[r - 1] + rowGap + rowAscent[r];
        }

        for (int r = 0; r < matrix.Rows.Count; r++)
        {
            float x = 0f;
            for (int c = 0; c < columns; c++)
            {
                MathBox cell = cells[r, c];
                float cellX = matrix.LeftAligned ? x : x + (columnWidths[c] - cell.Width) / 2f;
                grid.Add(cell, cellX, baselines[r]);
                x += columnWidths[c] + colGap;
            }
        }

        float axis = ctx.Size * Axis;
        float centre = (grid.Ascent - grid.Depth) / 2f;
        float dy = centre - axis;
        MathBox centred = new MathOffsetBox(grid, dy, grid.Width, grid.Ascent + dy, grid.Depth - dy);

        float target = grid.Height + ctx.Size * 0.1f;
        if (matrix.Open.Length == 0 && matrix.Close.Length == 0)
        {
            return centred;
        }

        MathBox open = Delimiter(matrix.Open, target, ctx);
        MathBox close = Delimiter(matrix.Close, target, ctx);
        var box = new MathRowBox();
        float cursor = 0f;
        box.Add(open, cursor);
        cursor += open.Width + ctx.Size * 0.08f;
        box.Add(centred, cursor);
        cursor += centred.Width + ctx.Size * 0.08f;
        box.Add(close, cursor);
        cursor += close.Width;
        box.Finish(cursor);
        _ = totalWidth;
        return box;
    }
}
