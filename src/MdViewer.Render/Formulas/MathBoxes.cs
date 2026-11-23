using System.Drawing;
using MdViewer.Core.Formulas;
using MdViewer.Render.Rendering;
using MdViewer.Render.Text;

namespace MdViewer.Render.Formulas;

/// <summary>
/// A laid-out piece of a formula: everything is measured in device pixels, with the
/// baseline between <see cref="Ascent"/> and <see cref="Depth"/>. Boxes know how to
/// paint themselves, so the inline shaper can drop one into a text line unchanged.
/// </summary>
public abstract class MathBox
{
    public float Width { get; protected set; }

    /// <summary>Height above the baseline.</summary>
    public float Ascent { get; protected set; }

    /// <summary>Depth below the baseline.</summary>
    public float Depth { get; protected set; }

    public float Height => Ascent + Depth;

    public abstract void Paint(PaintContext ctx, float x, float baseline);

    protected static void Glyph(PaintContext ctx, string text, Font font, float fontAscent, Color color, float x, float baseline)
    {
        if (text.Length == 0)
        {
            return;
        }

        ctx.G.DrawString(text, font, ctx.Brush(color), x, baseline - fontAscent, TextFormat.Typographic);
    }
}

/// <summary>One run of upright/italic glyphs.</summary>
public sealed class MathGlyphBox : MathBox
{
    public MathGlyphBox(string text, Font font, float fontAscent, float width, Color color, float ascent, float depth)
    {
        Text = text;
        Font = font;
        FontAscent = fontAscent;
        Color = color;
        Width = width;
        Ascent = ascent;
        Depth = depth;
    }

    public MathGlyphBox(string text, Font font, float fontAscent, float width, Color color)
        : this(text, font, fontAscent, width, color, fontAscent, MathF.Max(2f, fontAscent * 0.22f))
    {
    }

    public string Text { get; }

    public Font Font { get; }

    public float FontAscent { get; }

    public Color Color { get; }

    public override void Paint(PaintContext ctx, float x, float baseline) => Glyph(ctx, Text, Font, FontAscent, Color, x, baseline);
}

/// <summary>Horizontal sequence with explicit offsets (already includes TeX spacing).</summary>
public sealed class MathRowBox : MathBox
{
    private readonly List<(MathBox Box, float X)> _children = [];

    public void Add(MathBox box, float x)
    {
        _children.Add((box, x));
        Width = MathF.Max(Width, x + box.Width);
        Ascent = MathF.Max(Ascent, box.Ascent);
        Depth = MathF.Max(Depth, box.Depth);
    }

    public void Finish(float width)
    {
        Width = width;
    }

    public override void Paint(PaintContext ctx, float x, float baseline)
    {
        foreach ((MathBox box, float dx) in _children)
        {
            box.Paint(ctx, x + dx, baseline);
        }
    }
}

/// <summary>Vertical stack (multi-line display math, \\\\ rows).</summary>
public sealed class MathStackBox : MathBox
{
    private readonly List<(MathBox Box, float Y, float X)> _rows = [];

    public void Add(MathBox box, float y, float x)
    {
        _rows.Add((box, y, x));
        Width = MathF.Max(Width, x + box.Width);
        Ascent = MathF.Max(Ascent, -y + box.Ascent);
        Depth = MathF.Max(Depth, y + box.Depth);
    }

    public override void Paint(PaintContext ctx, float x, float baseline)
    {
        foreach ((MathBox box, float y, float dx) in _rows)
        {
            box.Paint(ctx, x + dx, baseline + y);
        }
    }
}

public sealed class MathSpaceBox : MathBox
{
    public MathSpaceBox(float width, float ascent = 0f, float depth = 0f)
    {
        Width = MathF.Max(0f, width);
        Ascent = ascent;
        Depth = depth;
    }

    public override void Paint(PaintContext ctx, float x, float baseline)
    {
    }
}

public sealed class MathFractionBox : MathBox
{
    private readonly MathBox _numerator;
    private readonly MathBox _denominator;
    private readonly float _barY;
    private readonly float _barThickness;
    private readonly bool _barred;

    public MathFractionBox(MathBox numerator, MathBox denominator, float barY, float barThickness, bool barred,
        float width, float ascent, float depth, float numeratorBaseline, float denominatorBaseline)
    {
        _numerator = numerator;
        _denominator = denominator;
        _barY = barY;
        _barThickness = barThickness;
        _barred = barred;
        Width = width;
        Ascent = ascent;
        Depth = depth;
        NumeratorBaseline = numeratorBaseline;
        DenominatorBaseline = denominatorBaseline;
    }

    public float NumeratorBaseline { get; init; }

    public float DenominatorBaseline { get; init; }

    public override void Paint(PaintContext ctx, float x, float baseline)
    {
        float y = baseline + _barY;
        if (_barred)
        {
            ctx.Fill(new RectangleF(x, y - _barThickness / 2f, Width, _barThickness), ctx.Colors.Text);
        }

        _numerator.Paint(ctx, x + (Width - _numerator.Width) / 2f, baseline + NumeratorBaseline);
        _denominator.Paint(ctx, x + (Width - _denominator.Width) / 2f, baseline + DenominatorBaseline);
    }
}

public sealed class MathRadicalBox : MathBox
{
    private readonly MathBox _body;
    private readonly MathBox? _index;
    private readonly Font _font;
    private readonly float _fontAscent;
    private readonly string _sign;
    private readonly float _signWidth;
    private readonly float _ruleThickness;
    private readonly Color _color;

    public MathRadicalBox(MathBox body, MathBox? index, Font font, float fontAscent, string sign, float signWidth,
        float ruleThickness, Color color)
    {
        _body = body;
        _index = index;
        _font = font;
        _fontAscent = fontAscent;
        _sign = sign;
        _signWidth = signWidth;
        _ruleThickness = ruleThickness;
        _color = color;
        Width = _signWidth + body.Width;
        Ascent = body.Ascent + ruleThickness * 2f;
        Depth = body.Depth;
    }

    public override void Paint(PaintContext ctx, float x, float baseline)
    {
        // The radical sign sits on the baseline; its overbar covers the radicand.
        Glyph(ctx, _sign, _font, _fontAscent, _color, x, baseline);
        float ruleStart = x + _signWidth * 0.62f;
        ctx.Fill(new RectangleF(ruleStart, baseline - Ascent, Width - _signWidth * 0.62f, _ruleThickness), ctx.Colors.Text);
        _body.Paint(ctx, x + _signWidth, baseline);
        _index?.Paint(ctx, x + _signWidth * 0.05f, baseline - Ascent - _index.Depth * 0.2f);
    }
}

public sealed class MathAccentBox : MathBox
{
    private readonly MathBox _body;
    private readonly string _mark;
    private readonly Font _font;
    private readonly float _fontAscent;
    private readonly float _markX;
    private readonly float _markBaseline;
    private readonly bool _under;
    private readonly bool _rule;
    private readonly float _ruleThickness;
    private readonly Color _color;

    public MathAccentBox(MathBox body, string mark, Font font, float fontAscent, float markX, float markBaseline,
        bool under, bool rule, float ruleThickness, Color color)
    {
        _body = body;
        _mark = mark;
        _font = font;
        _fontAscent = fontAscent;
        _markX = markX;
        _markBaseline = markBaseline;
        _under = under;
        _rule = rule;
        _ruleThickness = ruleThickness;
        _color = color;
        Width = body.Width;
        Ascent = under ? body.Ascent : body.Ascent + ruleThickness * 3.2f;
        Depth = under ? body.Depth + ruleThickness * 3.2f : body.Depth;
    }

    public override void Paint(PaintContext ctx, float x, float baseline)
    {
        _body.Paint(ctx, x, baseline);
        if (_under)
        {
            ctx.Fill(new RectangleF(x, baseline + Depth - _ruleThickness, Width, _ruleThickness), ctx.Colors.Text);
            return;
        }

        if (_rule)
        {
            ctx.Fill(new RectangleF(x, baseline - Ascent, Width, _ruleThickness), ctx.Colors.Text);
            return;
        }

        Glyph(ctx, _mark, _font, _fontAscent, _color, x + _markX, baseline + _markBaseline);
    }
}

public sealed class MathDelimiterBox : MathBox
{
    private readonly string _text;
    private readonly Font _font;
    private readonly float _fontAscent;
    private readonly float _size;
    private readonly bool _line;
    private readonly Color _color;

    public MathDelimiterBox(string text, Font font, float fontAscent, float size, bool line, Color color, float width,
        float ascent, float depth)
    {
        _text = text;
        _font = font;
        _fontAscent = fontAscent;
        _size = size;
        _line = line;
        _color = color;
        Ascent = ascent;
        Depth = depth;
        Width = width;
    }

    public float AxisOffset { get; init; }

    public override void Paint(PaintContext ctx, float x, float baseline)
    {
        if (_line)
        {
            float width = MathF.Max(1.2f, Width);
            float top = baseline - Ascent;
            ctx.Fill(new RectangleF(x, top, width, Height), ctx.Colors.Text);
            return;
        }

        // Glyphs are centred on the math axis rather than sitting on the baseline.
        float glyphCenter = _fontAscent - _size * 0.5f;
        float y = baseline - AxisOffset + glyphCenter;
        Glyph(ctx, _text, _font, _fontAscent, _color, x, y);
    }
}

public sealed class MathScriptBox : MathBox
{
    private readonly MathBox _base;
    private readonly MathBox? _sup;
    private readonly MathBox? _sub;
    private readonly float _supDx;
    private readonly float _subDx;
    private readonly float _supBaseline;
    private readonly float _subBaseline;

    public MathScriptBox(MathBox baseBox, MathBox? sup, MathBox? sub, float supDx, float subDx, float supBaseline, float subBaseline)
    {
        _base = baseBox;
        _sup = sup;
        _sub = sub;
        _supDx = supDx;
        _subDx = subDx;
        _supBaseline = supBaseline;
        _subBaseline = subBaseline;

        float right = baseBox.Width;
        if (sup is not null)
        {
            right = MathF.Max(right, supDx + sup.Width);
        }

        if (sub is not null)
        {
            right = MathF.Max(right, subDx + sub.Width);
        }

        Width = right;
        Ascent = MathF.Max(baseBox.Ascent, sup is null ? 0f : supBaseline + sup.Ascent);
        Depth = MathF.Max(baseBox.Depth, sub is null ? 0f : subBaseline + sub.Depth);
    }

    public override void Paint(PaintContext ctx, float x, float baseline)
    {
        _base.Paint(ctx, x, baseline);
        _sup?.Paint(ctx, x + _supDx, baseline - _supBaseline);
        _sub?.Paint(ctx, x + _subDx, baseline + _subBaseline);
    }
}

/// <summary>Large operator with limits above and below (display style).</summary>
public sealed class MathLimitsBox : MathBox
{
    private readonly MathBox _base;
    private readonly MathBox? _above;
    private readonly MathBox? _below;
    private readonly float _aboveX;
    private readonly float _belowX;
    private readonly float _aboveBaseline;
    private readonly float _belowBaseline;

    public MathLimitsBox(MathBox baseBox, MathBox? above, MathBox? below, float gap)
    {
        _base = baseBox;
        _above = above;
        _below = below;
        Width = MathF.Max(baseBox.Width, MathF.Max(above?.Width ?? 0f, below?.Width ?? 0f));
        _aboveX = (Width - (above?.Width ?? 0f)) / 2f;
        _belowX = (Width - (below?.Width ?? 0f)) / 2f;
        _aboveBaseline = above is null ? 0f : -baseBox.Ascent - gap - above.Depth;
        _belowBaseline = below is null ? 0f : baseBox.Depth + gap + below.Ascent;
        Ascent = MathF.Max(baseBox.Ascent, above is null ? 0f : -_aboveBaseline + above.Ascent);
        Depth = MathF.Max(baseBox.Depth, below is null ? 0f : _belowBaseline + below.Depth);
    }

    public override void Paint(PaintContext ctx, float x, float baseline)
    {
        _base.Paint(ctx, x + (Width - _base.Width) / 2f, baseline);
        _above?.Paint(ctx, x + _aboveX, baseline + _aboveBaseline);
        _below?.Paint(ctx, x + _belowX, baseline + _belowBaseline);
    }
}

public sealed class MathMatrixBox : MathBox{
    private readonly List<(MathBox Box, float X, float Baseline)> _cells = [];

    public void Add(MathBox box, float x, float baseline)
    {
        _cells.Add((box, x, baseline));
        Width = MathF.Max(Width, x + box.Width);
        Ascent = MathF.Max(Ascent, -baseline + box.Ascent);
        Depth = MathF.Max(Depth, baseline + box.Depth);
    }

    public override void Paint(PaintContext ctx, float x, float baseline)
    {
        foreach ((MathBox box, float dx, float dy) in _cells)
        {
            box.Paint(ctx, x + dx, baseline + dy);
        }
    }
}

/// <summary>A box shifted vertically inside its parent (matrix centring).</summary>
public sealed class MathOffsetBox(MathBox child, float dy) : MathBox
{
    public MathBox Child { get; } = child;

    public float DY { get; } = dy;

    public MathOffsetBox(MathBox child, float dy, float width, float ascent, float depth)
        : this(child, dy)
    {
        Width = width;
        Ascent = ascent;
        Depth = depth;
    }

    public override void Paint(PaintContext ctx, float x, float baseline) => Child.Paint(ctx, x, baseline + DY);
}

/// <summary>Anything the parser rejected: the TeX source, muted.</summary>
public sealed class MathFallbackBox : MathBox
{
    private readonly string _text;
    private readonly Font _font;
    private readonly float _fontAscent;
    private readonly Color _color;

    public MathFallbackBox(string text, Font font, float fontAscent, float width, Color color)
    {
        _text = text;
        _font = font;
        _fontAscent = fontAscent;
        _color = color;
        Width = width;
        Ascent = fontAscent;
        Depth = MathF.Max(2f, fontAscent * 0.25f);
    }

    public override void Paint(PaintContext ctx, float x, float baseline) => Glyph(ctx, _text, _font, _fontAscent, _color, x, baseline);
}
