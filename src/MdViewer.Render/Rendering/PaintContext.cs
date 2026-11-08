using System.Drawing;
using System.Drawing.Drawing2D;
using MdViewer.Render.Text;
using MdViewer.Render.Theming;

namespace MdViewer.Render.Rendering;

/// <summary>
/// Everything a visual needs while painting: the target surface, the palette,
/// cached GDI+ objects, and the current hover state.
/// </summary>
public sealed class PaintContext : IDisposable
{
    private readonly Dictionary<int, SolidBrush> _brushes = [];
    private readonly Dictionary<(int Color, float Width), Pen> _pens = [];

    public required Graphics G { get; init; }

    public required Theme Theme { get; init; }

    public required FontBook Fonts { get; init; }

    /// <summary>Link URL currently under the pointer (underline cue).</summary>
    public string? HoverLink { get; set; }

    /// <summary>Identifier of the control being hovered, e.g. <c>copy:12</c>.</summary>
    public string? HoverControl { get; set; }

    public Palette Colors => Theme.Colors;

    /// <summary>Theme DIP → device pixels.</summary>
    public float Px(float dip) => dip * Theme.Scale * Fonts.DpiScale;

    public SolidBrush Brush(Color color)
    {
        int key = color.ToArgb();
        if (_brushes.TryGetValue(key, out SolidBrush? cached))
        {
            return cached;
        }

        var brush = new SolidBrush(color);
        _brushes[key] = brush;
        return brush;
    }

    public Pen Pen(Color color, float width)
    {
        var key = (color.ToArgb(), width);
        if (_pens.TryGetValue(key, out Pen? cached))
        {
            return cached;
        }

        var pen = new Pen(color, width) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        _pens[key] = pen;
        return pen;
    }

    public void Fill(RectangleF rect, Color color) => G.FillRectangle(Brush(color), rect);

    public void FillRound(RectangleF rect, float radius, Color color)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        using GraphicsPath path = RoundedPath(rect, radius);
        G.FillPath(Brush(color), path);
    }

    public void StrokeRound(RectangleF rect, float radius, Color color, float width = 1f)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        using GraphicsPath path = RoundedPath(rect, radius);
        G.DrawPath(Pen(color, width), path);
    }

    public static GraphicsPath RoundedPath(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        float r = MathF.Min(radius, MathF.Min(rect.Width, rect.Height) / 2f);
        if (r <= 0.5f)
        {
            path.AddRectangle(rect);
            return path;
        }

        float d = r * 2f;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    public void DrawRun(Run run, float dx, float dy)
    {
        TextStyle style = run.Style;
        float x = dx + run.X;
        float y = dy + run.Baseline - run.Ascent;

        if (style.Background is Color background)
        {
            float pad = style.PaddingX;
            var rect = new RectangleF(x, y + run.Ascent * 0.08f, run.Width, run.Ascent + (run.Height - run.Ascent) * 0.9f);
            if (style.RoundedBackground)
            {
                FillRound(rect, 4f * Theme.Scale, background);
            }
            else
            {
                Fill(rect, background);
            }

            _ = pad;
        }

        float textX = x + style.PaddingX;
        if (style.LinkUrl is not null && Theme.Colors.Text is var _ && string.Equals(style.LinkUrl, HoverLink, StringComparison.Ordinal))
        {
            G.DrawString(run.Text, style.Font, Brush(style.Color), textX, y, TextFormat.Typographic);
            float underlineY = y + run.Ascent + 1.5f;
            G.DrawLine(Pen(style.Color, MathF.Max(1f, Theme.Scale)), textX, underlineY, textX + run.Width - style.PaddingX * 2, underlineY);
        }
        else
        {
            G.DrawString(run.Text, style.Font, Brush(style.Color), textX, y, TextFormat.Typographic);
        }

        if (style.Underline && style.LinkUrl is null)
        {
            float underlineY = y + run.Ascent + 1.5f;
            G.DrawLine(Pen(style.Color, MathF.Max(1f, Theme.Scale)), textX, underlineY, textX + run.Width - style.PaddingX * 2, underlineY);
        }

        if (style.Strike)
        {
            float strikeY = y + run.Ascent * 0.62f;
            G.DrawLine(Pen(style.Color, MathF.Max(1f, Theme.Scale)), textX, strikeY, textX + run.Width, strikeY);
        }
    }

    public void DrawRuns(IReadOnlyList<Run> runs, float dx, float dy)
    {
        foreach (Run run in runs)
        {
            DrawRun(run, dx, dy);
        }
    }

    public SizeF Measure(string text, Font font) => G.MeasureString(text, font, PointF.Empty, TextFormat.Typographic);

    public void Dispose()
    {
        foreach (SolidBrush brush in _brushes.Values)
        {
            brush.Dispose();
        }

        foreach (Pen pen in _pens.Values)
        {
            pen.Dispose();
        }

        _brushes.Clear();
        _pens.Clear();
    }
}

