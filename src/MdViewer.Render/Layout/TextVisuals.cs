using System.Drawing;
using MdViewer.Core.Markdown;
using MdViewer.Render.Media;
using MdViewer.Render.Rendering;
using MdViewer.Render.Text;
using MdViewer.Render.Theming;

namespace MdViewer.Render.Layout;

/// <summary>Shared services passed to every visual.</summary>
public sealed class LayoutContext
{
    public required Theme Theme { get; init; }

    public required FontBook Fonts { get; init; }

    public required ImageCache Images { get; init; }

    public required RenderSettings Settings { get; init; }

    public required InlineShaper Shaper { get; init; }

    /// <summary>Multiplier from theme DIP to device pixels.</summary>
    public float Scale => Theme.Scale * Fonts.DpiScale;

    public float Px(float dip) => dip * Scale;

    public Palette Colors => Theme.Colors;
}

/// <summary>Paragraph-like block: shaped lines painted on a baseline grid.</summary>
public class TextVisual : BlockLayout
{
    public required LayoutContext Ctx { get; init; }

    public required ShapedText Text { get; init; }

    public List<ShapedLine> Lines => Text.Lines;

    public override void Paint(PaintContext ctx)
    {
        float y = Top;
        foreach (ShapedLine line in Text.Lines)
        {
            foreach (Run run in line.Runs)
            {
                ctx.DrawRun(run, Left, y);
            }

            y += line.Height;
        }
    }

    public override void CollectLinks(List<LinkRegion> regions)
    {
        float y = Top;
        foreach (ShapedLine line in Text.Lines)
        {
            // Merge runs that belong to the same link on the same line so one
            // anchor is one clickable rectangle, not one per word.
            float? linkStart = null;
            float linkEnd = 0f;
            string? linkUrl = null;
            bool linkMissing = false;

            void FlushLink()
            {
                if (linkUrl is null)
                {
                    return;
                }

                regions.Add(new LinkRegion
                {
                    Bounds = new RectangleF(Left + linkStart!.Value, y, MathF.Max(2f, linkEnd - linkStart.Value), line.Height),
                    Target = linkUrl,
                    IsAnchor = linkUrl.StartsWith('#'),
                    IsExternal = linkUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase),
                    Missing = linkMissing,
                });

                linkStart = null;
                linkUrl = null;
                linkMissing = false;
            }

            foreach (Run run in line.Runs)
            {
                if (run.Style.LinkUrl is { Length: > 0 } url && run.Width > 0)
                {
                    if (linkUrl == url)
                    {
                        linkEnd = run.X + run.Width;
                    }
                    else
                    {
                        FlushLink();
                        linkStart = run.X;
                        linkEnd = run.X + run.Width;
                        linkUrl = url;
                        linkMissing = run.Style.MissingLink;
                    }
                }
                else
                {
                    FlushLink();
                }

                if (run.Style.Footnote > 0)
                {
                    regions.Add(new LinkRegion
                    {
                        Bounds = new RectangleF(Left + run.X, y, MathF.Max(6f, run.Width), line.Height),
                        Target = "#footnote-" + run.Style.Footnote.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        IsAnchor = true,
                    });
                }
            }

            FlushLink();
            y += line.Height;
        }
    }
}

/// <summary>Heading with a numbered gutter, optional rule and hover anchor.</summary>
public sealed class HeadingVisual : TextVisual
{
    public required int Level { get; init; }

    public string? Number { get; set; }

    public bool ShowRule { get; init; }

    public required float GutterLeft { get; init; }

    public override void Paint(PaintContext ctx)
    {
        base.Paint(ctx);

        float scale = Ctx.Scale;
        if (Number is { Length: > 0 })
        {
            Font numberFont = ctx.Fonts.Font(ctx.Fonts.UiFamily, Ctx.Theme.Type.HeadingSizes[Level - 1] * 0.82f * Ctx.Theme.Scale, FontStyle.Regular);
            var style = new TextStyle
            {
                Font = numberFont,
                Family = ctx.Fonts.UiFamily,
                SizeDip = Ctx.Theme.Type.HeadingSizes[Level - 1] * 0.82f * Ctx.Theme.Scale,
                Color = Ctx.Colors.TextFaint,
            };
            var run = new Run { Text = Number, Style = style, X = 0, Width = ctx.Fonts.Measure(Number, numberFont), Height = ctx.Fonts.Metrics(numberFont).LineSpacing, Ascent = ctx.Fonts.Metrics(numberFont).Ascent };
            ctx.DrawRun(run, GutterLeft + 8f * scale, Top);
        }

        bool hovered = string.Equals(ctx.HoverControl, "anchor:" + AnchorId, StringComparison.Ordinal);
        if (hovered)
        {
            Font anchorFont = ctx.Fonts.MonoFont(Ctx.Theme.Type.BaseSize * 0.8f * Ctx.Theme.Scale);
            ctx.G.DrawString("#", anchorFont, ctx.Brush(Ctx.Colors.Accent), GutterLeft + 8f * scale, Top);
        }

        if (ShowRule)
        {
            float y = Top + Height - Ctx.Px(6f);
            ctx.G.DrawLine(ctx.Pen(Ctx.Colors.Border, MathF.Max(1f, scale)), Left, y, Left + Width, y);
        }
    }
}

public sealed class DividerVisual : BlockLayout
{
    public required LayoutContext Ctx { get; init; }

    public override void Paint(PaintContext ctx)
    {
        float y = Top + Height / 2f;
        ctx.G.DrawLine(ctx.Pen(Ctx.Colors.Border, MathF.Max(1f, Ctx.Scale)), Left, y, Left + Width, y);
    }
}

/// <summary>Display math: centred serif text on a soft card.</summary>
public sealed class MathBlockVisual : BlockLayout
{
    public required LayoutContext Ctx { get; init; }

    public required ShapedText Text { get; init; }

    public override void Paint(PaintContext ctx)
    {
        var card = new RectangleF(Left, Top, Width, Height);
        ctx.FillRound(card, Ctx.Px(10f), Ctx.Colors.CodeBg);

        float y = Top + Ctx.Px(14f);
        foreach (ShapedLine line in Text.Lines)
        {
            foreach (Run run in line.Runs)
            {
                ctx.DrawRun(run, Left + (Width - line.Width) / 2f, y);
            }

            y += line.Height;
        }
    }
}

/// <summary>Raw HTML: shown as source, never executed.</summary>
public sealed class HtmlVisual : BlockLayout
{
    public required LayoutContext Ctx { get; init; }

    public required ShapedText Text { get; init; }

    public override void Paint(PaintContext ctx)
    {
        var card = new RectangleF(Left, Top, Width, Height);
        ctx.FillRound(card, Ctx.Px(10f), Ctx.Colors.CodeBg);
        ctx.StrokeRound(card, Ctx.Px(10f), Ctx.Colors.CodeBorder, MathF.Max(1f, Ctx.Scale));

        Font labelFont = ctx.Fonts.UiFont(Ctx.Theme.Type.BaseSize * 0.72f * Ctx.Theme.Scale, FontStyle.Bold);
        ctx.G.DrawString("RAW HTML  ·  NOT RENDERED", labelFont, ctx.Brush(Ctx.Colors.TextFaint), Left + Ctx.Px(14f), Top + Ctx.Px(9f));

        float y = Top + Ctx.Px(28f);
        foreach (ShapedLine line in Text.Lines)
        {
            foreach (Run run in line.Runs)
            {
                ctx.DrawRun(run, Left + Ctx.Px(14f), y);
            }

            y += line.Height;
        }
    }
}

/// <summary>A <c>(A) --&gt; [label] --&gt; (B)</c> code span rendered as a node chain.</summary>
public sealed class ChainVisual : BlockLayout
{
    public required LayoutContext Ctx { get; init; }

    public required IReadOnlyList<(string Text, bool IsNode, bool IsArrow, RectangleF Bounds)> Parts { get; init; }

    public override void Paint(PaintContext ctx)
    {
        foreach (var part in Parts)
        {
            var rect = part.Bounds;
            if (part.IsArrow)
            {
                float y = rect.Top + rect.Height / 2f;
                float arrowSize = Ctx.Px(5f);
                ctx.G.DrawLine(ctx.Pen(Ctx.Colors.TextFaint, MathF.Max(1f, Ctx.Scale)), rect.Left + 4, y, rect.Right - arrowSize - 2, y);
                var head = new[]
                {
                    new PointF(rect.Right - arrowSize - 2, y - arrowSize * 0.7f),
                    new PointF(rect.Right - 2, y),
                    new PointF(rect.Right - arrowSize - 2, y + arrowSize * 0.7f),
                };
                ctx.G.FillPolygon(ctx.Brush(Ctx.Colors.TextFaint), head);
                continue;
            }

            if (part.IsNode)
            {
                ctx.FillRound(rect, Ctx.Px(6f), Ctx.Colors.AccentSoft);
                ctx.StrokeRound(rect, Ctx.Px(6f), Color.FromArgb(60, Ctx.Colors.Accent), MathF.Max(1f, Ctx.Scale));
            }

            Font font = ctx.Fonts.UiFont(Ctx.Theme.Type.BaseSize * 0.86f * Ctx.Theme.Scale);
            var style = new TextStyle
            {
                Font = font,
                Family = ctx.Fonts.UiFamily,
                SizeDip = Ctx.Theme.Type.BaseSize * 0.86f * Ctx.Theme.Scale,
                Color = part.IsNode ? Ctx.Colors.Accent : Ctx.Colors.Text,
            };
            var run = new Run
            {
                Text = part.Text,
                Style = style,
                X = 0,
                Width = ctx.Fonts.Measure(part.Text, font),
                Height = ctx.Fonts.Metrics(font).LineSpacing,
                Ascent = ctx.Fonts.Metrics(font).Ascent,
            };
            float textY = rect.Top + (rect.Height - run.Height) / 2f;
            ctx.DrawRun(run, rect.Left + (rect.Width - run.Width) / 2f, textY);
        }
    }
}


