using System.Drawing;

namespace MdViewer.Render.Theming;

/// <summary>Semantic colour set for one appearance (light or dark).</summary>
public sealed class Palette
{
    public required Color Canvas { get; init; }

    public required Color Surface { get; init; }

    public required Color Elevated { get; init; }

    public required Color Text { get; init; }

    public required Color TextMuted { get; init; }

    public required Color TextFaint { get; init; }

    public required Color Border { get; init; }

    public required Color BorderStrong { get; init; }

    public required Color Accent { get; init; }

    public required Color AccentText { get; init; }

    public required Color AccentSoft { get; init; }

    public required Color Link { get; init; }

    public required Color CodeBg { get; init; }

    public required Color CodeBorder { get; init; }

    public required Color CodeText { get; init; }

    public required Color InlineCodeBg { get; init; }

    public required Color InlineCodeText { get; init; }

    public required Color MarkBg { get; init; }

    public required Color QuoteBar { get; init; }

    public required Color QuoteBg { get; init; }

    public required Color TableHeaderBg { get; init; }

    public required Color TableBorder { get; init; }

    public required Color TableStripe { get; init; }

    public required Color Selection { get; init; }

    public required Color Hover { get; init; }

    public required Color Shadow { get; init; }

    public required Color ScrollThumb { get; init; }

    public required Color ScrollThumbHover { get; init; }

    public required Color NoteBar { get; init; }

    public required Color NoteBg { get; init; }

    public required Color TipBar { get; init; }

    public required Color TipBg { get; init; }

    public required Color ImportantBar { get; init; }

    public required Color ImportantBg { get; init; }

    public required Color WarningBar { get; init; }

    public required Color WarningBg { get; init; }

    public required Color CautionBar { get; init; }

    public required Color CautionBg { get; init; }

    public required Color MissingLink { get; init; }

    public required Color Keyword { get; init; }

    public required Color String { get; init; }

    public required Color Number { get; init; }

    public required Color Comment { get; init; }

    public required Color TypeName { get; init; }

    public required Color Function { get; init; }

    public required Color Punctuation { get; init; }

    public Color WithAlpha(Color color, int alpha) => Color.FromArgb(alpha, color);

    /// <summary>Linear blend of two colours (0 = a, 1 = b).</summary>
    public static Color Blend(Color a, Color b, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromArgb(
            (int)Math.Round(a.A + (b.A - a.A) * amount),
            (int)Math.Round(a.R + (b.R - a.R) * amount),
            (int)Math.Round(a.G + (b.G - a.G) * amount),
            (int)Math.Round(a.B + (b.B - a.B) * amount));
    }
}

/// <summary>Type scale and spacing, expressed in device-independent pixels at 100 % zoom.</summary>
public sealed class Typography
{
    public float BaseSize { get; init; } = 16f;

    public float LineHeight { get; init; } = 1.62f;

    public float MonoSize { get; init; } = 13.6f;

    public float MonoLineHeight { get; init; } = 1.55f;

    public float[] HeadingSizes { get; init; } = [31f, 24.5f, 20f, 17f, 15.5f, 13.8f];

    public float[] HeadingSpaceBefore { get; init; } = [34f, 30f, 26f, 22f, 20f, 18f];

    public float[] HeadingSpaceAfter { get; init; } = [12f, 11f, 10f, 9f, 8f, 8f];

    public float ParagraphGap { get; init; } = 13f;

    public float ListIndent { get; init; } = 26f;

    public float ListItemGap { get; init; } = 5f;

    public float BlockGap { get; init; } = 16f;

    /// <summary>Padding inside the reading "sheet".</summary>
    public float PagePaddingX { get; init; } = 56f;

    public float PagePaddingTop { get; init; } = 44f;

    public float PagePaddingBottom { get; init; } = 56f;

    public float MaxReadingWidth { get; init; } = 720f;

    public float MinReadingWidth { get; init; } = 420f;

    public float MaxPageWidth { get; init; } = 980f;
}

public sealed class Theme
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required bool IsDark { get; init; }

    public required Palette Colors { get; init; }

    public Typography Type { get; init; } = new();

    /// <summary>Zoom factor applied to every size (1.0 = 100 %).</summary>
    public float Scale { get; init; } = 1f;

    public bool SerifReading { get; init; }

    public Theme WithScale(float scale) => new()
    {
        Id = Id,
        Name = Name,
        IsDark = IsDark,
        Colors = Colors,
        Type = Type,
        Scale = Math.Clamp(scale, 0.6f, 3f),
        SerifReading = SerifReading,
    };

    public Theme WithSerif(bool serif) => new()
    {
        Id = Id,
        Name = Name,
        IsDark = IsDark,
        Colors = Colors,
        Type = Type,
        Scale = Scale,
        SerifReading = serif,
    };

    public float S(float value) => value * Scale;
}


