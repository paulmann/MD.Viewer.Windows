using System.Drawing;

namespace MdViewer.Render.Text;

/// <summary>Shared GDI+ string formats (typographic: no extra padding, tight kerning).</summary>
public static class TextFormat
{
    public static readonly StringFormat Typographic = CreateTypographic();

    public static readonly StringFormat Center = CreateAligned(StringAlignment.Center);

    public static readonly StringFormat Right = CreateAligned(StringAlignment.Far);

    private static StringFormat CreateTypographic()
    {
        var format = (StringFormat)StringFormat.GenericTypographic.Clone();
        format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
        format.Trimming = StringTrimming.None;
        return format;
    }

    private static StringFormat CreateAligned(StringAlignment alignment)
    {
        var format = (StringFormat)StringFormat.GenericTypographic.Clone();
        format.Alignment = alignment;
        format.LineAlignment = StringAlignment.Near;
        format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
        return format;
    }
}

/// <summary>Visual attributes of a run of text. Cloned (not mutated) when nesting deepens.</summary>
public sealed class TextStyle
{
    public required Font Font { get; set; }

    public required string Family { get; set; }

    /// <summary>Font size in device-independent pixels (before DPI scaling).</summary>
    public required float SizeDip { get; set; }

    public FontStyle FontStyle { get; set; } = FontStyle.Regular;

    public Color Color { get; set; }

    public bool Underline { get; set; }

    public bool Strike { get; set; }

    public Color? Background { get; set; }

    public bool RoundedBackground { get; set; }

    public Color? BorderColor { get; set; }

    public string? LinkUrl { get; set; }

    public bool MissingLink { get; set; }

    public int Footnote { get; set; }

    /// <summary>Positive shifts the baseline up (superscript), negative down.</summary>
    public float BaselineShift { get; set; }

    /// <summary>Horizontal padding for pill backgrounds.</summary>
    public float PaddingX { get; set; }

    public TextStyle Clone() => (TextStyle)MemberwiseClone();
}

/// <summary>One painted piece of text with its position inside a line.</summary>
public sealed class Run
{
    public required string Text { get; init; }

    public required TextStyle Style { get; init; }

    public float X { get; set; }

    public float Width { get; set; }

    /// <summary>Baseline position relative to the containing block (set by the layout).</summary>
    public float Baseline { get; set; }

    /// <summary>Height of the font box (ascent + descent) in pixels.</summary>
    public float Height { get; set; }

    /// <summary>Font ascent above the baseline, in pixels.</summary>
    public float Ascent { get; set; }

    public float PaddingX => Style.PaddingX;

    public RectangleF Bounds => new(X, Baseline - Ascent, Width, Height);

    public Run Clone() => new()
    {
        Text = Text,
        Style = Style,
        X = X,
        Width = Width,
        Baseline = Baseline,
        Height = Height,
    };
}

/// <summary>A laid-out line: runs positioned on a shared baseline.</summary>
public sealed class ShapedLine
{
    public List<Run> Runs { get; } = [];

    public float Width { get; set; }

    /// <summary>Distance from the top of the line box to the baseline.</summary>
    public float Ascent { get; set; }

    public float Height { get; set; }

    public bool IsEmpty => Runs.Count == 0;

    public float LineTop { get; set; }

    public float Baseline => LineTop + Ascent;

    public void Recalculate(float minAscent, float minDescent)
    {
        float ascent = minAscent;
        float descent = minDescent;
        float width = 0f;

        foreach (Run run in Runs)
        {
            ascent = MathF.Max(ascent, run.Height - (run.Height * 0.28f));
            width = MathF.Max(width, run.X + run.Width);
        }

        Ascent = ascent;
        Height = ascent + descent;
        Width = width;
    }
}

/// <summary>Result of shaping one inline sequence.</summary>
public sealed class ShapedText
{
    public List<ShapedLine> Lines { get; } = [];

    public float Width { get; set; }

    public float Height { get; set; }

    public bool IsEmpty => Lines.Count == 0 || Lines.All(l => l.IsEmpty);
}

