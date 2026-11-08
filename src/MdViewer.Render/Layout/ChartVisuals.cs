using System.Drawing;
using System.Globalization;
using MdViewer.Core.Diagrams;
using MdViewer.Render.Rendering;
using MdViewer.Render.Text;

namespace MdViewer.Render.Layout;

/// <summary>Colour cycle for charts, tuned per appearance.</summary>
internal static class ChartPalette
{
    private static readonly Color[] Light =
    [
        Color.FromArgb(0x25, 0x63, 0xEB), Color.FromArgb(0x0E, 0xA5, 0xA4), Color.FromArgb(0xF5, 0x9E, 0x0B),
        Color.FromArgb(0xEF, 0x44, 0x44), Color.FromArgb(0x8B, 0x5C, 0xF6), Color.FromArgb(0x10, 0xB9, 0x81),
        Color.FromArgb(0xEC, 0x48, 0x99), Color.FromArgb(0x64, 0x74, 0x8B),
    ];

    private static readonly Color[] Dark =
    [
        Color.FromArgb(0x7A, 0xA2, 0xFF), Color.FromArgb(0x34, 0xD3, 0xC8), Color.FromArgb(0xF7, 0xB9, 0x55),
        Color.FromArgb(0xFF, 0x7B, 0x72), Color.FromArgb(0xB7, 0x9C, 0xFF), Color.FromArgb(0x4A, 0xDE, 0x9B),
        Color.FromArgb(0xFF, 0x8F, 0xC7), Color.FromArgb(0x94, 0xA3, 0xB8),
    ];

    public static Color Get(int index, bool dark) => dark ? Dark[index % Dark.Length] : Light[index % Light.Length];
}

/// <summary>Pie chart with a legend.</summary>
public sealed class PieVisual : BlockLayout
{
    private float _radius;
    private PointF _center;
    private float _legendX;
    private float _legendTop;

    public required LayoutContext Ctx { get; init; }

    public required PieDiagram Diagram { get; init; }

    public static PieVisual Create(LayoutContext ctx, MdViewer.Core.Markdown.MermaidBlock source, PieDiagram diagram, float left, float top, float width)
    {
        var visual = new PieVisual { Source = source, Ctx = ctx, Diagram = diagram };
        visual.Build(left, top, width);
        return visual;
    }

    private void Build(float left, float top, float width)
    {
        float scale = Ctx.Scale;
        Font titleFont = Ctx.Fonts.UiFont(Ctx.Theme.Type.BaseSize * 1.0f * Ctx.Theme.Scale, FontStyle.Bold);
        float titleHeight = Diagram.Title is { Length: > 0 } ? Ctx.Fonts.Metrics(titleFont).LineSpacing + 10f * scale : 0f;

        float legendWidth = 0f;
        Font legendFont = Ctx.Fonts.UiFont(Ctx.Theme.Type.BaseSize * 0.82f * Ctx.Theme.Scale);
        foreach (PieSlice slice in Diagram.Slices)
        {
            legendWidth = MathF.Max(legendWidth, Ctx.Fonts.Measure(slice.Label, legendFont));
        }

        legendWidth = MathF.Min(legendWidth + 90f * scale, width * 0.5f);
        float available = width - legendWidth - 20f * scale;
        _radius = MathF.Max(42f * scale, MathF.Min(130f * scale, MathF.Min(available, 240f * scale) / 2f));
        _center = new PointF(left + _radius + 8f * scale, top + titleHeight + _radius + 10f * scale);
        _legendX = left + _radius * 2f + 28f * scale;
        _legendTop = top + titleHeight + 8f * scale;

        float legendHeight = Diagram.Slices.Count * (Ctx.Fonts.Metrics(legendFont).LineSpacing + 8f * scale);
        Top = top;
        Left = left;
        Width = width;
        Height = titleHeight + MathF.Max(_radius * 2f + 20f * scale, legendHeight) + 6f * scale;
        _ = titleFont;
    }

    public override void Paint(PaintContext ctx)
    {
        float scale = Ctx.Scale;
        float y = Top;

        if (Diagram.Title is { Length: > 0 } title)
        {
            Font titleFont = ctx.Fonts.UiFont(Ctx.Theme.Type.BaseSize * Ctx.Theme.Scale, FontStyle.Bold);
            float titleWidth = ctx.Fonts.Measure(title, titleFont);
            ctx.G.DrawString(title, titleFont, ctx.Brush(Ctx.Colors.Text), Left + (Width - titleWidth) / 2f, y);
            y += ctx.Fonts.Metrics(titleFont).LineSpacing + 10f * scale;
        }

        double total = Diagram.Slices.Sum(s => s.Value);
        if (total <= 0)
        {
            return;
        }

        float startAngle = -90f;
        var legendFont = ctx.Fonts.UiFont(Ctx.Theme.Type.BaseSize * 0.82f * Ctx.Theme.Scale);
        float legendRow = ctx.Fonts.Metrics(legendFont).LineSpacing + 8f * scale;
        float legendY = _legendTop;

        for (int i = 0; i < Diagram.Slices.Count; i++)
        {
            PieSlice slice = Diagram.Slices[i];
            float sweep = (float)(slice.Value / total * 360.0);
            Color color = ChartPalette.Get(i, Ctx.Theme.IsDark);
            var bounds = new RectangleF(_center.X - _radius, _center.Y - _radius, _radius * 2f, _radius * 2f);

            ctx.G.FillPie(ctx.Brush(color), bounds, startAngle, MathF.Max(0.2f, sweep));
            ctx.G.DrawPie(ctx.Pen(Ctx.Colors.Surface, MathF.Max(1.4f, 2f * scale)), bounds, startAngle, MathF.Max(0.2f, sweep));

            if (sweep > 18f)
            {
                double midAngle = (startAngle + sweep / 2f) * Math.PI / 180.0;
                float tx = _center.X + (float)Math.Cos(midAngle) * _radius * 0.62f;
                float ty = _center.Y + (float)Math.Sin(midAngle) * _radius * 0.62f;
                string percent = (slice.Value / total * 100.0).ToString("0", CultureInfo.InvariantCulture) + "%";
                float textWidth = ctx.Fonts.Measure(percent, legendFont);
                ctx.G.DrawString(percent, legendFont, ctx.Brush(Ctx.Colors.Surface), tx - textWidth / 2f, ty - ctx.Fonts.Metrics(legendFont).LineSpacing / 2f);
            }

            var swatch = new RectangleF(_legendX, legendY + 4f * scale, 10f * scale, 10f * scale);
            ctx.FillRound(swatch, 2f * scale, color);
            ctx.G.DrawString(slice.Label, legendFont, ctx.Brush(Ctx.Colors.Text), _legendX + 18f * scale, legendY);

            if (Diagram.ShowData)
            {
                string value = slice.Value.ToString("0.##", CultureInfo.InvariantCulture);
                float valueWidth = ctx.Fonts.Measure(value, legendFont);
                ctx.G.DrawString(value, legendFont, ctx.Brush(Ctx.Colors.TextFaint), Left + Width - valueWidth - 4f * scale, legendY);
            }

            legendY += legendRow;
            startAngle += sweep;
        }
    }
}

/// <summary>Gantt chart: sections, bars, milestones and a date axis.</summary>
public sealed class GanttVisual : BlockLayout
{
    private static readonly string[] MonthNames =
    [
        "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec",
    ];

    private readonly List<(string? Section, GanttTask? Task, float Top, float Height)> _rows = [];
    private DateTime _start;
    private DateTime _end;
    private float _labelWidth;
    private float _chartLeft;
    private float _chartWidth;
    private float _axisTop;
    private TimeSpan _tick;

    public required LayoutContext Ctx { get; init; }

    public required GanttDiagram Diagram { get; init; }

    public static GanttVisual Create(LayoutContext ctx, MdViewer.Core.Markdown.MermaidBlock source, GanttDiagram diagram, float left, float top, float width)
    {
        var visual = new GanttVisual { Source = source, Ctx = ctx, Diagram = diagram };
        visual.Build(left, top, width);
        return visual;
    }

    private void Build(float left, float top, float width)
    {
        float scale = Ctx.Scale;
        DateTime start = DateTime.MaxValue;
        DateTime end = DateTime.MinValue;
        float labelWidth = 0f;
        Font nameFont = Ctx.Fonts.UiFont(Ctx.Theme.Type.BaseSize * 0.8f * Ctx.Theme.Scale);
        Font titleFont = Ctx.Fonts.UiFont(Ctx.Theme.Type.BaseSize * Ctx.Theme.Scale, FontStyle.Bold);

        foreach (GanttSection section in Diagram.Sections)
        {
            foreach (GanttTask task in section.Tasks)
            {
                start = task.Start < start ? task.Start : start;
                end = task.End > end ? task.End : end;
                labelWidth = MathF.Max(labelWidth, Ctx.Fonts.Measure(task.Name, nameFont));
            }
        }

        if (start == DateTime.MaxValue)
        {
            start = DateTime.Today;
            end = start.AddDays(7);
        }

        if (end <= start)
        {
            end = start.AddDays(1);
        }

        _start = start.Date.AddDays(-1);
        _end = end.Date.AddDays(1);
        int span = Math.Max(1, (int)(_end - _start).TotalDays);
        _tick = span switch
        {
            <= 16 => TimeSpan.FromDays(1),
            <= 60 => TimeSpan.FromDays(7),
            <= 180 => TimeSpan.FromDays(14),
            <= 400 => TimeSpan.FromDays(30),
            _ => TimeSpan.FromDays(90),
        };

        float titleHeight = Diagram.Title is { Length: > 0 } ? Ctx.Fonts.Metrics(titleFont).LineSpacing + 12f * scale : 0f;
        float axisHeight = 24f * scale;
        float rowHeight = 24f * scale;
        float sectionHeight = 22f * scale;

        _labelWidth = MathF.Min(labelWidth + 20f * scale, width * 0.42f);
        _chartLeft = left + _labelWidth;
        _chartWidth = width - _labelWidth;
        _axisTop = top + titleHeight;

        float y = _axisTop + axisHeight + 4f * scale;
        foreach (GanttSection section in Diagram.Sections)
        {
            if (section.Name is { Length: > 0 })
            {
                _rows.Add((section.Name, null, y, sectionHeight));
                y += sectionHeight;
            }

            foreach (GanttTask task in section.Tasks)
            {
                _rows.Add((null, task, y, rowHeight));
                y += rowHeight;
            }
        }

        Top = top;
        Left = left;
        Width = width;
        Height = y - top + 4f * scale;
    }

    public override void Paint(PaintContext ctx)
    {
        float scale = Ctx.Scale;
        Font titleFont = ctx.Fonts.UiFont(Ctx.Theme.Type.BaseSize * Ctx.Theme.Scale, FontStyle.Bold);
        Font nameFont = ctx.Fonts.UiFont(Ctx.Theme.Type.BaseSize * 0.8f * Ctx.Theme.Scale);
        Font smallFont = ctx.Fonts.UiFont(Ctx.Theme.Type.BaseSize * 0.72f * Ctx.Theme.Scale);

        if (Diagram.Title is { Length: > 0 } title)
        {
            float titleWidth = ctx.Fonts.Measure(title, titleFont);
            ctx.G.DrawString(title, titleFont, ctx.Brush(Ctx.Colors.Text), Left + (Width - titleWidth) / 2f, Top);
        }

        // axis
        float axisY = _axisTop + 18f * scale;
        ctx.G.DrawLine(ctx.Pen(Ctx.Colors.BorderStrong, MathF.Max(1f, scale)), _chartLeft, axisY, _chartLeft + _chartWidth, axisY);

        for (DateTime tick = _start; tick <= _end; tick = tick.Add(_tick))
        {
            float x = XFor(tick);
            ctx.G.DrawLine(ctx.Pen(Ctx.Colors.Border, MathF.Max(1f, scale)), x, axisY, x, Top + Height);
            string label = _tick >= TimeSpan.FromDays(30)
                ? MonthNames[tick.Month - 1] + " " + tick.Year.ToString(CultureInfo.InvariantCulture)
                : tick.Day.ToString(CultureInfo.InvariantCulture) + " " + MonthNames[tick.Month - 1];
            ctx.G.DrawString(label, smallFont, ctx.Brush(Ctx.Colors.TextFaint), x + 3f * scale, axisY - 16f * scale);
        }

        foreach ((string? section, GanttTask? task, float top, float height) in _rows)
        {
            if (section is not null)
            {
                ctx.Fill(new RectangleF(Left, top, Width, height), Ctx.Colors.TableHeaderBg);
                ctx.G.DrawString(section.ToUpperInvariant(), smallFont, ctx.Brush(Ctx.Colors.TextMuted), Left + 6f * scale, top + 4f * scale);
                continue;
            }

            if (task is null)
            {
                continue;
            }

            float rowCenter = top + height / 2f;
            string name = task.Name;
            float maxNameWidth = _labelWidth - 12f * scale;
            if (ctx.Fonts.Measure(name, nameFont) > maxNameWidth)
            {
                while (name.Length > 1 && ctx.Fonts.Measure(name + "…", nameFont) > maxNameWidth)
                {
                    name = name[..^1];
                }

                name += "…";
            }

            ctx.G.DrawString(name, nameFont, ctx.Brush(Ctx.Colors.Text), Left + 4f * scale, rowCenter - ctx.Fonts.Metrics(nameFont).LineSpacing / 2f);

            if (task.Milestone)
            {
                float cx = XFor(task.Start);
                float size = 6f * scale;
                PointF[] diamond =
                [
                    new(cx, rowCenter - size), new(cx + size, rowCenter), new(cx, rowCenter + size), new(cx - size, rowCenter),
                ];
                ctx.G.FillPolygon(ctx.Brush(Ctx.Colors.WarningBar), diamond);
                continue;
            }

            float x1 = XFor(task.Start);
            float x2 = MathF.Max(x1 + 3f * scale, XFor(task.End));
            var bar = new RectangleF(x1, rowCenter - 7f * scale, x2 - x1, 14f * scale);
            ctx.FillRound(bar, 3f * scale, task.Done ? Ctx.Colors.Accent : Ctx.Colors.AccentSoft);
            ctx.StrokeRound(bar, 3f * scale, Ctx.Colors.Accent, MathF.Max(1f, scale));

            int days = Math.Max(0, (int)(task.End - task.Start).TotalDays);
            string suffix = days == 1 ? "1 day" : days + " days";
            float labelWidth = ctx.Fonts.Measure(suffix, smallFont);
            if (bar.Width > labelWidth + 12f * scale)
            {
                ctx.G.DrawString(suffix, smallFont, ctx.Brush(task.Done ? Ctx.Colors.Surface : Ctx.Colors.TextMuted), bar.Right - labelWidth - 6f * scale, rowCenter - ctx.Fonts.Metrics(smallFont).LineSpacing / 2f);
            }
            else
            {
                ctx.G.DrawString(suffix, smallFont, ctx.Brush(Ctx.Colors.TextFaint), bar.Right + 6f * scale, rowCenter - ctx.Fonts.Metrics(smallFont).LineSpacing / 2f);
            }
        }
    }

    private float XFor(DateTime date)
    {
        double total = (_end - _start).TotalDays;
        double offset = (date - _start).TotalDays;
        return _chartLeft + (float)(Math.Clamp(offset / Math.Max(1.0, total), 0.0, 1.0) * _chartWidth);
    }
}

/// <summary>User-journey chart: sections, tasks and 1…5 satisfaction dots.</summary>
public sealed class JourneyVisual : BlockLayout
{
    private readonly List<(string? Section, JourneyTask? Task, float Top, float Height)> _rows = [];
    private float _labelWidth;
    private float _scoreLeft;
    private float _scoreWidth;

    public required LayoutContext Ctx { get; init; }

    public required JourneyDiagram Diagram { get; init; }

    public static JourneyVisual Create(LayoutContext ctx, MdViewer.Core.Markdown.MermaidBlock source, JourneyDiagram diagram, float left, float top, float width)
    {
        var visual = new JourneyVisual { Source = source, Ctx = ctx, Diagram = diagram };
        visual.Build(left, top, width);
        return visual;
    }

    private void Build(float left, float top, float width)
    {
        float scale = Ctx.Scale;
        Font nameFont = Ctx.Fonts.UiFont(Ctx.Theme.Type.BaseSize * 0.82f * Ctx.Theme.Scale);
        Font titleFont = Ctx.Fonts.UiFont(Ctx.Theme.Type.BaseSize * Ctx.Theme.Scale, FontStyle.Bold);

        float labelWidth = 0f;
        float actorWidth = 0f;
        foreach (JourneySection section in Diagram.Sections)
        {
            foreach (JourneyTask task in section.Tasks)
            {
                labelWidth = MathF.Max(labelWidth, Ctx.Fonts.Measure(task.Name, nameFont));
                actorWidth = MathF.Max(actorWidth, Ctx.Fonts.Measure(task.Actors, nameFont) * 0.9f);
            }
        }

        _labelWidth = MathF.Min(labelWidth + 16f * scale, width * 0.38f);
        _scoreLeft = left + _labelWidth + 8f * scale;
        _scoreWidth = MathF.Max(120f * scale, width - _labelWidth - actorWidth - 30f * scale);

        float titleHeight = Diagram.Title is { Length: > 0 } ? Ctx.Fonts.Metrics(titleFont).LineSpacing + 12f * scale : 0f;
        float rowHeight = 26f * scale;
        float sectionHeight = 22f * scale;

        float y = top + titleHeight;
        foreach (JourneySection section in Diagram.Sections)
        {
            _rows.Add((section.Name, null, y, sectionHeight));
            y += sectionHeight;
            foreach (JourneyTask task in section.Tasks)
            {
                _rows.Add((null, task, y, rowHeight));
                y += rowHeight;
            }
        }

        Top = top;
        Left = left;
        Width = width;
        Height = y - top + 4f * scale;
    }

    public override void Paint(PaintContext ctx)
    {
        float scale = Ctx.Scale;
        Font titleFont = ctx.Fonts.UiFont(Ctx.Theme.Type.BaseSize * Ctx.Theme.Scale, FontStyle.Bold);
        Font nameFont = ctx.Fonts.UiFont(Ctx.Theme.Type.BaseSize * 0.82f * Ctx.Theme.Scale);
        Font smallFont = ctx.Fonts.UiFont(Ctx.Theme.Type.BaseSize * 0.72f * Ctx.Theme.Scale);

        if (Diagram.Title is { Length: > 0 } title)
        {
            float titleWidth = ctx.Fonts.Measure(title, titleFont);
            ctx.G.DrawString(title, titleFont, ctx.Brush(Ctx.Colors.Text), Left + (Width - titleWidth) / 2f, Top);
        }

        float dotGap = _scoreWidth / 4f;
        foreach ((string? section, JourneyTask? task, float top, float height) in _rows)
        {
            if (section is not null)
            {
                ctx.Fill(new RectangleF(Left, top, Width, height), Ctx.Colors.TableHeaderBg);
                ctx.G.DrawString(section.ToUpperInvariant(), smallFont, ctx.Brush(Ctx.Colors.TextMuted), Left + 6f * scale, top + 4f * scale);
                continue;
            }

            if (task is null)
            {
                continue;
            }

            float center = top + height / 2f;
            string name = task.Name;
            float maxName = _labelWidth - 10f * scale;
            if (ctx.Fonts.Measure(name, nameFont) > maxName)
            {
                while (name.Length > 1 && ctx.Fonts.Measure(name + "…", nameFont) > maxName)
                {
                    name = name[..^1];
                }

                name += "…";
            }

            ctx.G.DrawString(name, nameFont, ctx.Brush(Ctx.Colors.Text), Left + 4f * scale, center - ctx.Fonts.Metrics(nameFont).LineSpacing / 2f);

            // score line and dots
            ctx.G.DrawLine(ctx.Pen(Ctx.Colors.Border, MathF.Max(1f, scale)), _scoreLeft, center, _scoreLeft + _scoreWidth, center);

            for (int step = 1; step <= 5; step++)
            {
                float x = _scoreLeft + dotGap * (step - 1);
                float radius = (step <= task.Score ? 4.5f : 3f) * scale;
                if (step <= task.Score)
                {
                    ctx.G.FillEllipse(ctx.Brush(Ctx.Colors.Accent), x - radius, center - radius, radius * 2f, radius * 2f);
                }
                else
                {
                    ctx.G.DrawEllipse(ctx.Pen(Ctx.Colors.BorderStrong, MathF.Max(1f, scale)), x - radius, center - radius, radius * 2f, radius * 2f);
                }
            }

            if (task.Actors.Length > 0)
            {
                float actorsWidth = ctx.Fonts.Measure(task.Actors, smallFont);
                float x = _scoreLeft + _scoreWidth + 12f * scale;
                if (x + actorsWidth > Left + Width)
                {
                    x = Left + Width - actorsWidth - 2f * scale;
                }

                ctx.G.DrawString(task.Actors, smallFont, ctx.Brush(Ctx.Colors.TextFaint), x, center - ctx.Fonts.Metrics(smallFont).LineSpacing / 2f);
            }
        }
    }
}
