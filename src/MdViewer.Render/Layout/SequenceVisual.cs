using System.Drawing;
using System.Drawing.Drawing2D;
using MdViewer.Core.Diagrams;
using MdViewer.Render.Rendering;
using MdViewer.Render.Text;

namespace MdViewer.Render.Layout;

/// <summary>Word wrapping shared by the diagram painters.</summary>
internal static class DiagramText
{
    public static List<string> Wrap(string text, Font font, float width, FontBook fonts)
    {
        var lines = new List<string>();
        foreach (string paragraph in text.Split('\n'))
        {
            string current = string.Empty;
            foreach (string word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = current.Length == 0 ? word : current + " " + word;
                if (fonts.Measure(candidate, font) <= width || current.Length == 0)
                {
                    current = candidate;
                    continue;
                }

                lines.Add(current);
                current = word;
            }

            lines.Add(current);
        }

        return lines.Count == 0 ? [string.Empty] : lines;
    }

    public static void Arrow(PaintContext ctx, PointF from, PointF to, Color color, float scale, bool filled = true, bool open = false)
    {
        float dx = to.X - from.X;
        float dy = to.Y - from.Y;
        float length = MathF.Sqrt(dx * dx + dy * dy);
        if (length < 2f)
        {
            return;
        }

        float ux = dx / length;
        float uy = dy / length;
        float size = 7f * scale;
        var tip = to;
        var left = new PointF(tip.X - ux * size - uy * size * 0.55f, tip.Y - uy * size + ux * size * 0.55f);
        var right = new PointF(tip.X - ux * size + uy * size * 0.55f, tip.Y - uy * size - ux * size * 0.55f);

        if (open)
        {
            ctx.G.DrawLines(ctx.Pen(color, MathF.Max(1f, 1.2f * scale)), [left, tip, right]);
            return;
        }

        if (filled)
        {
            ctx.G.FillPolygon(ctx.Brush(color), [tip, left, right]);
        }
        else
        {
            ctx.G.DrawPolygon(ctx.Pen(color, MathF.Max(1f, 1.2f * scale)), [tip, left, right]);
        }
    }
}

/// <summary>Sequence diagram: participants, lifelines, messages, notes and frames.</summary>
public sealed class SequenceVisual : BlockLayout
{
    private readonly List<(SequenceStep Step, float Top, float Height)> _rows = [];
    private readonly List<(float Top, float Bottom, string Kind, string Label)> _frames = [];
    private readonly List<float> _elseLines = [];
    private float[] _columns = [];
    private float _headerHeight;
    private float _diagramTop;
    private Font _actorFont = null!;
    private Font _messageFont = null!;
    private Font _smallFont = null!;

    public required LayoutContext Ctx { get; init; }

    public required SequenceDiagram Diagram { get; init; }

    public static SequenceVisual? Create(LayoutContext ctx, MdViewer.Core.Markdown.MermaidBlock source, SequenceDiagram diagram, float left, float top, float width)
    {
        var visual = new SequenceVisual { Source = source, Ctx = ctx, Diagram = diagram };
        if (!visual.Build(left, top, width))
        {
            return null;
        }

        return visual;
    }

    private bool Build(float left, float top, float width)
    {
        float scale = Ctx.Scale;
        float baseSize = Ctx.Theme.Type.BaseSize;
        _actorFont = Ctx.Fonts.UiFont(baseSize * 0.9f * Ctx.Theme.Scale, FontStyle.Bold);
        _messageFont = Ctx.Fonts.UiFont(baseSize * 0.84f * Ctx.Theme.Scale);
        _smallFont = Ctx.Fonts.UiFont(baseSize * 0.76f * Ctx.Theme.Scale);

        float lineHeight = Ctx.Fonts.Metrics(_messageFont).LineSpacing * 1.1f;
        float gap = 26f * scale;

        // actor columns
        int count = Diagram.Actors.Count;
        if (count == 0)
        {
            return false;
        }

        var widths = new float[count];
        for (int i = 0; i < count; i++)
        {
            string label = Diagram.Actors[i].Label;
            widths[i] = MathF.Min(Ctx.Fonts.Measure(label, _actorFont) + 26f * scale, width / MathF.Max(2f, count) - 8f);
        }

        float total = widths.Sum() + gap * (count - 1);
        if (total > width && count > 1)
        {
            gap = MathF.Max(8f * scale, (width - widths.Sum()) / (count - 1));
        }

        _columns = new float[count];
        float x = left + MathF.Max(0f, (width - (widths.Sum() + gap * (count - 1))) / 2f);
        for (int i = 0; i < count; i++)
        {
            _columns[i] = x + widths[i] / 2f;
            x += widths[i] + gap;
        }

        _headerHeight = 36f * scale;
        _diagramTop = top;
        float y = top + _headerHeight + 12f * scale;

        var open = new Stack<(int Index, string Kind, string Label)>();
        for (int index = 0; index < Diagram.Steps.Count; index++)
        {
            SequenceStep step = Diagram.Steps[index];
            float height;
            switch (step)
            {
                case SequenceMessageStep:
                    height = 30f * scale;
                    break;

                case SequenceNoteStep note:
                {
                    int lines = DiagramText.Wrap(note.Text, _smallFont, MathF.Min(260f * scale, width * 0.42f), Ctx.Fonts).Count;
                    height = (lines * lineHeight + 20f * scale) + 8f * scale;
                    break;
                }

                case SequenceFrameStep frame when !frame.IsEnd && !frame.IsElse:
                    height = 24f * scale;
                    break;

                case SequenceFrameStep frame when frame.IsElse:
                    height = 18f * scale;
                    _elseLines.Add(y + height / 2f);
                    break;

                case SequenceFrameStep:
                    height = 8f * scale;
                    break;

                default:
                    height = 28f * scale;
                    break;
            }

            _rows.Add((step, y, height));

            if (step is SequenceFrameStep { IsEnd: false, IsElse: false } start)
            {
                open.Push((_rows.Count - 1, start.Kind, start.Label));
            }
            else if (step is SequenceFrameStep { IsEnd: true } && open.Count > 0)
            {
                (int startIndex, string kind, string label) = open.Pop();
                _frames.Add((_rows[startIndex].Top, y + height, kind, label));
            }

            y += height;
        }

        float bottom = y + 8f * scale;
        Top = top;
        Left = left;
        Width = width;
        Height = bottom - top;
        return true;
    }

    public override void Paint(PaintContext ctx)
    {
        float scale = Ctx.Scale;
        float lineHeight = ctx.Fonts.Metrics(_messageFont).LineSpacing * 1.1f;
        float lifeTop = _diagramTop + _headerHeight;
        float lifeBottom = Top + Height - 8f * scale;

        // frames behind everything
        foreach ((float top, float bottom, string kind, string label) in _frames)
        {
            var rect = new RectangleF(Left + 6f * scale, top, Width - 12f * scale, bottom - top);
            ctx.StrokeRound(rect, 6f * scale, Ctx.Colors.BorderStrong, MathF.Max(1f, scale));
            var tab = new RectangleF(rect.Left, rect.Top, MathF.Min(rect.Width - 8f * scale, ctx.Fonts.Measure(label, _smallFont) + 60f * scale), 20f * scale);
            ctx.FillRound(tab, 5f * scale, Ctx.Colors.Surface);
            ctx.G.DrawString(kind.ToUpperInvariant(), _smallFont, ctx.Brush(Ctx.Colors.Accent), tab.Left + 8f * scale, tab.Top + 3f * scale);
            ctx.G.DrawString(label, _smallFont, ctx.Brush(Ctx.Colors.TextMuted), tab.Left + ctx.Fonts.Measure(kind.ToUpperInvariant(), _smallFont) + 12f * scale, tab.Top + 3f * scale);
        }

        // lifelines
        using (var pen = new Pen(Ctx.Colors.Border, MathF.Max(1f, scale)) { DashStyle = DashStyle.Dash })
        {
            foreach (float column in _columns)
            {
                ctx.G.DrawLine(pen, column, lifeTop, column, lifeBottom);
            }
        }

        // participant headers
        for (int i = 0; i < Diagram.Actors.Count; i++)
        {
            SequenceActor actor = Diagram.Actors[i];
            float boxWidth = Ctx.Fonts.Measure(actor.Label, _actorFont) + 26f * scale;
            var box = new RectangleF(_columns[i] - boxWidth / 2f, _diagramTop, boxWidth, _headerHeight - 6f * scale);
            ctx.FillRound(box, 6f * scale, actor.IsActor ? Ctx.Colors.AccentSoft : Ctx.Colors.Surface);
            ctx.StrokeRound(box, 6f * scale, Ctx.Colors.BorderStrong, MathF.Max(1f, scale));
            float textWidth = Ctx.Fonts.Measure(actor.Label, _actorFont);
            ctx.G.DrawString(actor.Label, _actorFont, ctx.Brush(Ctx.Colors.Text), box.Left + (box.Width - textWidth) / 2f, box.Top + 7f * scale);
        }

        foreach (float y in _elseLines)
        {
            using var pen = new Pen(Ctx.Colors.BorderStrong, MathF.Max(1f, scale)) { DashStyle = DashStyle.Dash };
            ctx.G.DrawLine(pen, Left + 12f * scale, y, Left + Width - 12f * scale, y);
        }

        foreach ((SequenceStep step, float top, float height) in _rows)
        {
            switch (step)
            {
                case SequenceMessageStep message:
                    DrawMessage(ctx, message, top, height, scale);
                    break;

                case SequenceNoteStep note:
                    DrawNote(ctx, note, top, height, scale, lineHeight);
                    break;

                case SequenceDividerStep divider:
                {
                    float y = top + height / 2f;
                    ctx.G.DrawLine(ctx.Pen(Ctx.Colors.BorderStrong, MathF.Max(1f, scale)), Left + 10f * scale, y, Left + Width - 10f * scale, y);
                    float textWidth = Ctx.Fonts.Measure(divider.Text, _smallFont);
                    ctx.FillRound(new RectangleF(Left + (Width - textWidth) / 2f - 8f * scale, y - 9f * scale, textWidth + 16f * scale, 18f * scale), 4f * scale, Ctx.Colors.Surface);
                    ctx.G.DrawString(divider.Text, _smallFont, ctx.Brush(Ctx.Colors.TextMuted), Left + (Width - textWidth) / 2f, y - 8f * scale);
                    break;
                }
            }
        }
    }

    private void DrawMessage(PaintContext ctx, SequenceMessageStep message, float top, float height, float scale)
    {
        int fromIndex = IndexOf(message.From);
        int toIndex = IndexOf(message.To);
        if (fromIndex < 0 || toIndex < 0)
        {
            return;
        }

        float y = top + height * 0.66f;
        float x1 = _columns[fromIndex];
        float x2 = _columns[toIndex];
        Color color = Ctx.Colors.BorderStrong;

        if (message.Number > 0)
        {
            string number = message.Number.ToString(System.Globalization.CultureInfo.InvariantCulture);
            float radius = 8f * scale;
            float nx = MathF.Min(x1, x2) - radius - 4f * scale;
            ctx.G.FillEllipse(ctx.Brush(Ctx.Colors.Accent), nx - radius, y - radius, radius * 2f, radius * 2f);
            float numberWidth = ctx.Fonts.Measure(number, _smallFont);
            ctx.G.DrawString(number, _smallFont, ctx.Brush(Ctx.Colors.Surface), nx - numberWidth / 2f, y - ctx.Fonts.Metrics(_smallFont).LineSpacing / 2f);
        }

        if (MathF.Abs(x2 - x1) < 2f)
        {
            // self message: small loop to the right
            float loop = 26f * scale;
            using var pen = new Pen(color, MathF.Max(1f, scale)) { DashStyle = message.Dashed ? DashStyle.Dash : DashStyle.Solid };
            ctx.G.DrawLines(pen, [
                new PointF(x1, y - 6f * scale),
                new PointF(x1 + loop, y - 6f * scale),
                new PointF(x1 + loop, y + 6f * scale),
                new PointF(x1 + 2f * scale, y + 6f * scale),
            ]);
            DiagramText.Arrow(ctx, new PointF(x1 + loop, y + 6f * scale), new PointF(x1 + 2f * scale, y + 6f * scale), color, scale, filled: !message.OpenArrow, open: message.OpenArrow);
        }
        else
        {
            using var pen = new Pen(color, MathF.Max(1f, scale)) { DashStyle = message.Dashed ? DashStyle.Dash : DashStyle.Solid };
            ctx.G.DrawLine(pen, x1, y, x2, y);
            DiagramText.Arrow(ctx, new PointF(x1, y), new PointF(x2, y), color, scale, filled: !message.OpenArrow, open: message.OpenArrow);
        }

        if (message.Text.Length == 0)
        {
            return;
        }

        float textWidth = Ctx.Fonts.Measure(message.Text, _messageFont);
        float middle = (x1 + x2) / 2f + (MathF.Abs(x2 - x1) < 2f ? 30f * scale : 0f);
        var label = new RectangleF(middle - textWidth / 2f - 5f * scale, y - 20f * scale, textWidth + 10f * scale, 16f * scale);
        ctx.FillRound(label, 3f * scale, Ctx.Colors.Surface);
        ctx.G.DrawString(message.Text, _messageFont, ctx.Brush(Ctx.Colors.Text), label.Left + 5f * scale, label.Top);
    }

    private void DrawNote(PaintContext ctx, SequenceNoteStep note, float top, float height, float scale, float lineHeight)
    {
        int anchor = IndexOf(note.ActorA);
        if (anchor < 0)
        {
            return;
        }

        float width = MathF.Min(260f * scale, Width * 0.42f);
        float x = note.Placement switch
        {
            "left" => _columns[anchor] - width - 14f * scale,
            "right" => _columns[anchor] + 14f * scale,
            _ => _columns[anchor] - width / 2f,
        };

        int second = note.ActorB is null ? -1 : IndexOf(note.ActorB);
        if (note.Placement == "over" && second >= 0)
        {
            float left = MathF.Min(_columns[anchor], _columns[second]) - 10f * scale;
            float right = MathF.Max(_columns[anchor], _columns[second]) + 10f * scale;
            x = left;
            width = right - left;
        }

        x = Math.Clamp(x, Left + 4f * scale, Left + Width - width - 4f * scale);

        var rect = new RectangleF(x, top + 4f * scale, width, height - 8f * scale);
        ctx.FillRound(rect, 4f * scale, Ctx.Colors.WarningBg);
        ctx.StrokeRound(rect, 4f * scale, Ctx.Colors.WarningBar, MathF.Max(1f, scale));

        float y = rect.Top + 8f * scale;
        foreach (string line in DiagramText.Wrap(note.Text, _smallFont, width - 16f * scale, Ctx.Fonts))
        {
            ctx.G.DrawString(line, _smallFont, ctx.Brush(Ctx.Colors.Text), rect.Left + 8f * scale, y);
            y += lineHeight;
        }
    }

    private int IndexOf(string id)
    {
        for (int i = 0; i < Diagram.Actors.Count; i++)
        {
            if (Diagram.Actors[i].Id == id)
            {
                return i;
            }
        }

        return -1;
    }
}

