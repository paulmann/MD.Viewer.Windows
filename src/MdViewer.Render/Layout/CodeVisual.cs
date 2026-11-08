using System.Drawing;
using System.Globalization;
using MdViewer.Core.Highlighting;
using MdViewer.Core.Markdown;
using MdViewer.Render.Rendering;
using MdViewer.Render.Text;

namespace MdViewer.Render.Layout;

public sealed class CodeLine
{
    public List<Run> Runs { get; } = [];

    public float Width { get; set; }

    public float Height { get; set; }
}

/// <summary>Fenced code card: language header, highlighted runs, copy control.</summary>
public sealed class CodeVisual : BlockLayout
{
    private const float HeaderHeight = 30f;
    private const float Padding = 14f;

    public required LayoutContext Ctx { get; init; }

    public required string Language { get; init; }

    public string? Title { get; init; }

    public required IReadOnlyList<CodeLine> Lines { get; init; }

    public RectangleF CopyButton { get; set; }

    public bool HasHeader => Title is not null || Language.Length > 0;

    public override void Paint(PaintContext ctx)
    {
        var card = new RectangleF(Left, Top, Width, Height);
        float radius = Ctx.Px(10f);
        ctx.FillRound(card, radius, Ctx.Colors.CodeBg);
        ctx.StrokeRound(card, radius, Ctx.Colors.CodeBorder, MathF.Max(1f, Ctx.Scale));

        float y = Top;
        if (HasHeader)
        {
            Font labelFont = ctx.Fonts.MonoFont(Ctx.Theme.Type.BaseSize * 0.72f * Ctx.Theme.Scale);
            string label = Title is { Length: > 0 } ? Title : Language;
            ctx.G.DrawString(label, labelFont, ctx.Brush(Ctx.Colors.TextMuted), Left + Ctx.Px(Padding), Top + Ctx.Px(8f));

            bool hoverCopy = string.Equals(ctx.HoverControl, "copy:" + AnchorId, StringComparison.Ordinal);
            var copyStyle = new TextStyle
            {
                Font = ctx.Fonts.UiFont(Ctx.Theme.Type.BaseSize * 0.72f * Ctx.Theme.Scale),
                Family = ctx.Fonts.UiFamily,
                SizeDip = Ctx.Theme.Type.BaseSize * 0.72f * Ctx.Theme.Scale,
                Color = hoverCopy ? Ctx.Colors.Accent : Ctx.Colors.TextFaint,
            };
            const string copyLabel = "Copy";
            float copyWidth = ctx.Fonts.Measure(copyLabel, copyStyle.Font);
            var copyRun = new Run
            {
                Text = copyLabel,
                Style = copyStyle,
                Width = copyWidth,
                Height = ctx.Fonts.Metrics(copyStyle.Font).LineSpacing,
                Ascent = ctx.Fonts.Metrics(copyStyle.Font).Ascent,
            };
            float copyX = Left + Width - Ctx.Px(Padding) - copyWidth;
            CopyButton = new RectangleF(copyX - Ctx.Px(6f), Top + Ctx.Px(4f), copyWidth + Ctx.Px(12f), Ctx.Px(22f));
            ctx.DrawRun(copyRun, copyX, Top + Ctx.Px(8f));

            float lineY = Top + Ctx.Px(HeaderHeight - 1f);
            ctx.G.DrawLine(ctx.Pen(Ctx.Colors.CodeBorder, MathF.Max(1f, Ctx.Scale)), Left, lineY, Left + Width, lineY);
            y = Top + Ctx.Px(HeaderHeight);
        }

        y += Ctx.Px(9f);
        foreach (CodeLine line in Lines)
        {
            foreach (Run run in line.Runs)
            {
                ctx.DrawRun(run, Left + Ctx.Px(Padding), y);
            }

            y += line.Height;
        }
    }
}

/// <summary>Mermaid source shown as a diagram card (rendering happens in <see cref="DiagramVisual"/>).</summary>
public sealed class MermaidVisual : BlockLayout
{
    public required LayoutContext Ctx { get; init; }

    public required string SourceText { get; init; }

    public string? Message { get; init; }

    public override void Paint(PaintContext ctx)
    {
        var card = new RectangleF(Left, Top, Width, Height);
        ctx.FillRound(card, Ctx.Px(10f), Ctx.Colors.CodeBg);
        ctx.StrokeRound(card, Ctx.Px(10f), Ctx.Colors.CodeBorder, MathF.Max(1f, Ctx.Scale));

        Font mono = ctx.Fonts.MonoFont(Ctx.Theme.Type.MonoSize * Ctx.Theme.Scale);
        Font label = ctx.Fonts.UiFont(Ctx.Theme.Type.BaseSize * 0.72f * Ctx.Theme.Scale, FontStyle.Bold);

        string header = Message is null ? "MERMAID" : "MERMAID  ·  " + Message.ToUpper(CultureInfo.InvariantCulture);
        ctx.G.DrawString(header, label, ctx.Brush(Ctx.Colors.TextFaint), Left + Ctx.Px(14f), Top + Ctx.Px(9f));

        float y = Top + Ctx.Px(30f);
        foreach (string line in SourceText.Split('\n'))
        {
            ctx.G.DrawString(line, mono, ctx.Brush(Ctx.Colors.TextMuted), Left + Ctx.Px(14f), y);
            y += ctx.Fonts.Metrics(mono).LineSpacing;
        }
    }
}

/// <summary>Block image with caption.</summary>
public sealed class ImageVisual : BlockLayout
{
    public required LayoutContext Ctx { get; init; }

    public required MdViewer.Render.Media.ImageAsset Asset { get; init; }

    public required RectangleF ImageRect { get; init; }

    public string? Caption { get; init; }

    public string Alt { get; init; } = string.Empty;

    public ShapedText? CaptionText { get; set; }

    public float CaptionTop { get; set; }

    public override void Paint(PaintContext ctx)
    {
        if (Asset.Bitmap is not null)
        {
            ctx.G.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            var target = Rectangle.Round(ImageRect);
            var saved = ctx.G.PixelOffsetMode;
            ctx.G.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            ctx.G.DrawImage(Asset.Bitmap, target);
            ctx.G.PixelOffsetMode = saved;

            ctx.StrokeRound(ImageRect, Ctx.Px(8f), Color.FromArgb(28, Ctx.Colors.Text), MathF.Max(1f, Ctx.Scale));
            PaintCaption(ctx);
            return;
        }

        var card = new RectangleF(ImageRect.Left, ImageRect.Top, ImageRect.Width, ImageRect.Height);
        ctx.FillRound(card, Ctx.Px(10f), Ctx.Colors.CodeBg);
        ctx.StrokeRound(card, Ctx.Px(10f), Ctx.Colors.Border, MathF.Max(1f, Ctx.Scale));

        Font label = ctx.Fonts.UiFont(Ctx.Theme.Type.BaseSize * 0.82f * Ctx.Theme.Scale);
        string message = "Image not available";
        string detail = Asset.Error ?? Asset.DisplayName;
        float labelWidth = ctx.Fonts.Measure(message, label);
        float x = card.Left + (card.Width - labelWidth) / 2f;
        float y = card.Top + card.Height / 2f - ctx.Px(10f);
        ctx.G.DrawString(message, label, ctx.Brush(Ctx.Colors.TextMuted), x, y);

        Font small = ctx.Fonts.UiFont(Ctx.Theme.Type.BaseSize * 0.72f * Ctx.Theme.Scale);
        float detailWidth = ctx.Fonts.Measure(detail, small);
        ctx.G.DrawString(detail, small, ctx.Brush(Ctx.Colors.TextFaint), card.Left + (card.Width - detailWidth) / 2f, y + ctx.Px(20f));
    }

    private void PaintCaption(PaintContext ctx)
    {
        if (CaptionText is null)
        {
            return;
        }

        float y = CaptionTop;
        foreach (ShapedLine line in CaptionText.Lines)
        {
            foreach (Run run in line.Runs)
            {
                ctx.DrawRun(run, ImageRect.Left + (ImageRect.Width - line.Width) / 2f, y);
            }

            y += line.Height;
        }
    }

    public override void CollectLinks(List<LinkRegion> regions)
    {
        if (Caption is null)
        {
            return;
        }

        regions.Add(new LinkRegion
        {
            Bounds = new RectangleF(ImageRect.Left, Top, ImageRect.Width, Height),
            Target = "image:" + Alt,
        });
    }
}


