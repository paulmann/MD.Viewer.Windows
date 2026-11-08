using System.Drawing;
using MdViewer.Core.Markdown;
using MdViewer.Render.Rendering;
using MdViewer.Render.Text;

namespace MdViewer.Render.Layout;

public sealed class ListItemVisual
{
    public required BlockLayout[] Children { get; init; }

    /// <summary>Marker box (bullet / number / checkbox).</summary>
    public required RectangleF Marker { get; init; }

    public string? Number { get; init; }

    public bool IsOrdered { get; init; }

    public int Level { get; init; }

    public TaskState Task { get; init; }
}

public sealed class ListVisual : BlockLayout
{
    public required LayoutContext Ctx { get; init; }

    public required ListItemVisual[] Items { get; init; }

    public override void Paint(PaintContext ctx)
    {
        foreach (ListItemVisual item in Items)
        {
            foreach (BlockLayout child in item.Children)
            {
                child.Paint(ctx);
            }

            if (item.Task != TaskState.None)
            {
                DrawCheckbox(ctx, item.Marker, item.Task == TaskState.Done);
            }
            else if (item.IsOrdered)
            {
                Font font = ctx.Fonts.UiFont(Ctx.Theme.Type.BaseSize * 0.92f * Ctx.Theme.Scale);
                string label = item.Number ?? "1";
                float width = ctx.Fonts.Measure(label, font);
                var style = new TextStyle
                {
                    Font = font,
                    Family = ctx.Fonts.UiFamily,
                    SizeDip = Ctx.Theme.Type.BaseSize * 0.92f * Ctx.Theme.Scale,
                    Color = Ctx.Colors.TextMuted,
                };
                var run = new Run
                {
                    Text = label,
                    Style = style,
                    Width = width,
                    Height = ctx.Fonts.Metrics(font).LineSpacing,
                    Ascent = ctx.Fonts.Metrics(font).Ascent,
                };
                ctx.DrawRun(run, item.Marker.Right - width, item.Marker.Top);
            }
            else
            {
                DrawBullet(ctx, item.Marker, item.Level);
            }
        }
    }

    private void DrawBullet(PaintContext ctx, RectangleF marker, int level)
    {
        float size = level % 3 switch
        {
            0 => Ctx.Px(4.6f),
            1 => Ctx.Px(4.0f),
            _ => Ctx.Px(3.4f),
        };

        float cx = marker.Left + Ctx.Px(9f);
        float cy = marker.Top + marker.Height * 0.42f;
        var rect = new RectangleF(cx - size / 2f, cy - size / 2f, size, size);
        Color color = level == 0 ? Ctx.Colors.TextFaint : Ctx.Colors.BorderStrong;

        if (level % 3 == 1)
        {
            ctx.G.DrawEllipse(ctx.Pen(color, MathF.Max(1.2f, Ctx.Scale)), rect);
        }
        else if (level % 3 == 2)
        {
            ctx.Fill(rect, color);
        }
        else
        {
            ctx.FillRound(rect, size / 2f, color);
        }
    }

    private void DrawCheckbox(PaintContext ctx, RectangleF marker, bool done)
    {
        float size = Ctx.Px(14f);
        var box = new RectangleF(marker.Left + Ctx.Px(4f), marker.Top + Ctx.Px(3f), size, size);
        ctx.FillRound(box, Ctx.Px(4f), done ? Ctx.Colors.Accent : Ctx.Colors.Surface);
        ctx.StrokeRound(box, Ctx.Px(4f), done ? Ctx.Colors.Accent : Ctx.Colors.BorderStrong, MathF.Max(1f, Ctx.Scale));

        if (!done)
        {
            return;
        }

        float s = size;
        var points = new[]
        {
            new PointF(box.Left + s * 0.24f, box.Top + s * 0.52f),
            new PointF(box.Left + s * 0.43f, box.Top + s * 0.71f),
            new PointF(box.Left + s * 0.78f, box.Top + s * 0.30f),
        };
        ctx.G.DrawLines(ctx.Pen(Ctx.Colors.Surface, MathF.Max(1.6f, Ctx.Scale * 1.8f)), points);
    }
}

public sealed class QuoteVisual : BlockLayout
{
    public required LayoutContext Ctx { get; init; }

    public required BlockLayout[] Children { get; init; }

    public override void Paint(PaintContext ctx)
    {
        var card = new RectangleF(Left, Top, Width, Height);
        ctx.FillRound(card, Ctx.Px(8f), Ctx.Colors.QuoteBg);
        float barWidth = Ctx.Px(3f);
        var bar = new RectangleF(Left + Ctx.Px(2f), Top + Ctx.Px(2f), barWidth, Height - Ctx.Px(4f));
        ctx.FillRound(bar, barWidth / 2f, Ctx.Colors.QuoteBar);

        foreach (BlockLayout child in Children)
        {
            child.Paint(ctx);
        }
    }
}

public sealed class AlertVisual : BlockLayout
{
    public required LayoutContext Ctx { get; init; }

    public required AlertKind Kind { get; init; }

    public required BlockLayout[] Children { get; init; }

    public required string Label { get; init; }

    public override void Paint(PaintContext ctx)
    {
        (Color accent, Color bg, Color icon) = Kind switch
        {
            AlertKind.Tip => (Ctx.Colors.TipBar, Ctx.Colors.TipBg, Ctx.Colors.TipBar),
            AlertKind.Important => (Ctx.Colors.ImportantBar, Ctx.Colors.ImportantBg, Ctx.Colors.ImportantBar),
            AlertKind.Warning => (Ctx.Colors.WarningBar, Ctx.Colors.WarningBg, Ctx.Colors.WarningBar),
            AlertKind.Caution => (Ctx.Colors.CautionBar, Ctx.Colors.CautionBg, Ctx.Colors.CautionBar),
            _ => (Ctx.Colors.NoteBar, Ctx.Colors.NoteBg, Ctx.Colors.NoteBar),
        };

        var card = new RectangleF(Left, Top, Width, Height);
        ctx.FillRound(card, Ctx.Px(8f), bg);

        float barWidth = Ctx.Px(3f);
        var bar = new RectangleF(Left + Ctx.Px(1f), Top + Ctx.Px(2f), barWidth, Height - Ctx.Px(4f));
        ctx.FillRound(bar, barWidth / 2f, accent);

        DrawIcon(ctx, new RectangleF(Left + Ctx.Px(14f), Top + Ctx.Px(14f), Ctx.Px(16f), Ctx.Px(16f)), icon);

        Font labelFont = ctx.Fonts.UiFont(Ctx.Theme.Type.BaseSize * 0.78f * Ctx.Theme.Scale, FontStyle.Bold);
        ctx.G.DrawString(Label.ToUpperInvariant(), labelFont, ctx.Brush(accent), Left + Ctx.Px(38f), Top + Ctx.Px(14f));

        foreach (BlockLayout child in Children)
        {
            child.Paint(ctx);
        }
    }

    private void DrawIcon(PaintContext ctx, RectangleF box, Color color)
    {
        float cx = box.Left + box.Width / 2f;
        float cy = box.Top + box.Height / 2f;
        Pen pen = ctx.Pen(color, MathF.Max(1.4f, Ctx.Scale * 1.6f));

        switch (Kind)
        {
            case AlertKind.Tip:
                ctx.G.DrawEllipse(pen, box.Left + 1, box.Top + 1, box.Width - 2, box.Height - 2);
                ctx.G.DrawLine(pen, cx - box.Width * 0.16f, cy, cx - box.Width * 0.02f, cy + box.Height * 0.16f);
                ctx.G.DrawLine(pen, cx - box.Width * 0.02f, cy + box.Height * 0.16f, cx + box.Width * 0.2f, cy - box.Height * 0.18f);
                break;

            case AlertKind.Warning:
            case AlertKind.Caution:
                ctx.G.DrawLines(pen, [
                    new PointF(cx, box.Top + 1),
                    new PointF(box.Right - 1, box.Bottom - 2),
                    new PointF(box.Left + 1, box.Bottom - 2),
                    new PointF(cx, box.Top + 1),
                ]);
                ctx.G.DrawLine(pen, cx, box.Top + box.Height * 0.36f, cx, box.Top + box.Height * 0.6f);
                ctx.Fill(new RectangleF(cx - Ctx.Px(0.8f), box.Top + box.Height * 0.66f, Ctx.Px(1.6f), Ctx.Px(1.6f)), color);
                break;

            case AlertKind.Important:
            case AlertKind.Note:
            default:
                ctx.G.DrawEllipse(pen, box.Left + 1, box.Top + 1, box.Width - 2, box.Height - 2);
                ctx.G.DrawLine(pen, cx, box.Top + box.Height * 0.42f, cx, box.Bottom - box.Height * 0.22f);
                ctx.Fill(new RectangleF(cx - Ctx.Px(0.8f), box.Top + box.Height * 0.24f, Ctx.Px(1.6f), Ctx.Px(1.6f)), color);
                break;
        }
    }
}

public sealed class DefinitionVisual : BlockLayout
{
    public required LayoutContext Ctx { get; init; }

    public required ShapedText Term { get; init; }

    public required BlockLayout[][] Definitions { get; init; }

    public override void Paint(PaintContext ctx)
    {
        foreach (ShapedLine line in Term.Lines)
        {
            foreach (Run run in line.Runs)
            {
                ctx.DrawRun(run, Left, Top);
            }
        }

        foreach (BlockLayout[] definition in Definitions)
        {
            foreach (BlockLayout child in definition)
            {
                child.Paint(ctx);
            }
        }
    }

    public override void CollectLinks(List<LinkRegion> regions)
    {
        foreach (BlockLayout[] definition in Definitions)
        {
            foreach (BlockLayout child in definition)
            {
                child.CollectLinks(regions);
            }
        }
    }
}

public sealed class FootnoteVisual : BlockLayout
{
    public required LayoutContext Ctx { get; init; }

    public required int Number { get; init; }

    public required BlockLayout[] Children { get; init; }

    public override void Paint(PaintContext ctx)
    {
        Font font = ctx.Fonts.UiFont(Ctx.Theme.Type.BaseSize * 0.78f * Ctx.Theme.Scale, FontStyle.Bold);
        var style = new TextStyle
        {
            Font = font,
            Family = ctx.Fonts.UiFamily,
            SizeDip = Ctx.Theme.Type.BaseSize * 0.78f * Ctx.Theme.Scale,
            Color = Ctx.Colors.Accent,
        };
        string label = Number.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var run = new Run
        {
            Text = label,
            Style = style,
            Width = ctx.Fonts.Measure(label, font),
            Height = ctx.Fonts.Metrics(font).LineSpacing,
            Ascent = ctx.Fonts.Metrics(font).Ascent,
        };
        ctx.DrawRun(run, Left, Top);

        foreach (BlockLayout child in Children)
        {
            child.Paint(ctx);
        }
    }
}

public sealed class FootnotesSectionVisual : BlockLayout
{
    public required LayoutContext Ctx { get; init; }

    public required FootnoteVisual[] Notes { get; init; }

    public override void Paint(PaintContext ctx)
    {
        float y = Top;
        ctx.G.DrawLine(ctx.Pen(Ctx.Colors.BorderStrong, MathF.Max(1f, Ctx.Scale)), Left, y, Left + Ctx.Px(72f), y);

        foreach (FootnoteVisual note in Notes)
        {
            note.Paint(ctx);
        }
    }
}

public sealed class TocVisual : BlockLayout
{
    public required LayoutContext Ctx { get; init; }

    public required IReadOnlyList<(int Level, string Text, string Target, RectangleF Bounds)> Items { get; init; }

    public override void Paint(PaintContext ctx)
    {
        var card = new RectangleF(Left, Top, Width, Height);
        ctx.FillRound(card, Ctx.Px(10f), Ctx.Colors.QuoteBg);
        ctx.StrokeRound(card, Ctx.Px(10f), Ctx.Colors.CodeBorder, MathF.Max(1f, Ctx.Scale));

        Font label = ctx.Fonts.UiFont(Ctx.Theme.Type.BaseSize * 0.72f * Ctx.Theme.Scale, FontStyle.Bold);
        ctx.G.DrawString("CONTENTS", label, ctx.Brush(Ctx.Colors.TextFaint), Left + Ctx.Px(16f), Top + Ctx.Px(12f));

        foreach (var item in Items)
        {
            bool hovered = string.Equals(ctx.HoverLink, "#" + item.Target, StringComparison.Ordinal);
            Font font = ctx.Fonts.UiFont(Ctx.Theme.Type.BaseSize * (item.Level <= 2 ? 0.94f : 0.88f) * Ctx.Theme.Scale);
            var style = new TextStyle
            {
                Font = font,
                Family = ctx.Fonts.UiFamily,
                SizeDip = Ctx.Theme.Type.BaseSize * 0.94f * Ctx.Theme.Scale,
                Color = hovered ? Ctx.Colors.Accent : item.Level <= 2 ? Ctx.Colors.Text : Ctx.Colors.TextMuted,
            };
            var run = new Run
            {
                Text = item.Text,
                Style = style,
                Width = ctx.Fonts.Measure(item.Text, font),
                Height = ctx.Fonts.Metrics(font).LineSpacing,
                Ascent = ctx.Fonts.Metrics(font).Ascent,
            };
            ctx.DrawRun(run, item.Bounds.Left, item.Bounds.Top);
        }
    }

    public override void CollectLinks(List<LinkRegion> regions)
    {
        foreach (var item in Items)
        {
            regions.Add(new LinkRegion
            {
                Bounds = item.Bounds,
                Target = "#" + item.Target,
                IsAnchor = true,
            });
        }
    }
}


