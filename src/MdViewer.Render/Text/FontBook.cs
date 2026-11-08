using System.Drawing;
using System.Drawing.Text;
using MdViewer.Render.Theming;

namespace MdViewer.Render.Text;

/// <summary>
/// Owns every <see cref="Font"/> the renderer uses (created in pixel units, so
/// zoom and DPI scaling are explicit) and caches text measurements.
/// </summary>
public sealed class FontBook : IDisposable
{
    private readonly Dictionary<string, Font> _fonts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _widths = new(StringComparer.Ordinal);
    private readonly Dictionary<string, FontLineMetrics> _metrics = new(StringComparer.Ordinal);
    private readonly Graphics _ruler;
    private readonly Bitmap _rulerSurface;
    private readonly HashSet<string> _installed;

    public FontBook(float dpiScale = 1f)
    {
        DpiScale = dpiScale <= 0 ? 1f : dpiScale;
        _rulerSurface = new Bitmap(4, 4);
        _ruler = Graphics.FromImage(_rulerSurface);
        _ruler.TextRenderingHint = TextRenderingHint.AntiAlias;

        _installed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var collection = new InstalledFontCollection())
        {
            foreach (FontFamily family in collection.Families)
            {
                _installed.Add(family.Name);
            }
        }

        UiFamily = Pick("Segoe UI Variable Text", "Segoe UI", "Tahoma", "Arial");
        DisplayFamily = Pick("Segoe UI Variable Display", "Segoe UI Semibold", "Segoe UI", "Arial");
        MonoFamily = Pick("Cascadia Mono", "Cascadia Code", "Consolas", "Lucida Console", "Courier New");
        SerifFamily = Pick("Cambria", "Georgia", "Times New Roman", "Segoe UI");
        EmojiFamily = Pick("Segoe UI Emoji", "Segoe UI Symbol", "Segoe UI");
    }

    public float DpiScale { get; }

    public string UiFamily { get; }

    public string DisplayFamily { get; }

    public string MonoFamily { get; }

    public string SerifFamily { get; }

    public string EmojiFamily { get; }

    private string Pick(params string[] candidates)
    {
        foreach (string candidate in candidates)
        {
            if (_installed.Contains(candidate))
            {
                return candidate;
            }
        }

        return candidates[^1];
    }

    /// <summary>Returns a cached font for the family/size/style triple.</summary>
    public Font Font(string family, float pixelSize, FontStyle style = FontStyle.Regular)
    {
        float size = Math.Max(4f, MathF.Round(pixelSize * DpiScale, 2));
        string key = $"{family}|{size:0.##}|{(int)style}";
        if (_fonts.TryGetValue(key, out Font? cached))
        {
            return cached;
        }

        string resolved = _installed.Contains(family) ? family : UiFamily;
        var font = new Font(resolved, size, style, GraphicsUnit.Pixel);
        _fonts[key] = font;
        return font;
    }

    public Font UiFont(float size, FontStyle style = FontStyle.Regular) => Font(UiFamily, size, style);

    public Font DisplayFont(float size, FontStyle style = FontStyle.Bold) => Font(DisplayFamily, size, style);

    public Font MonoFont(float size, FontStyle style = FontStyle.Regular) => Font(MonoFamily, size, style);

    public Font ReadingFont(float size, bool serif, FontStyle style = FontStyle.Regular) =>
        Font(serif ? SerifFamily : UiFamily, size, style);

    /// <summary>Width of a text run in device pixels (the layout unit).</summary>
    public float Measure(string text, Font font)
    {
        if (text.Length == 0)
        {
            return 0f;
        }

        string key = $"{font.Name}|{font.Size:0.##}|{(int)font.Style}|{text}";
        if (_widths.TryGetValue(key, out float cached))
        {
            return cached;
        }

        float width = _ruler.MeasureString(text, font, PointF.Empty, TextFormat.Typographic).Width;
        if (_widths.Count > 60_000)
        {
            _widths.Clear();
        }

        _widths[key] = width;
        return width;
    }

    public FontLineMetrics Metrics(Font font)
    {
        string key = $"{font.Name}|{font.Size:0.##}|{(int)font.Style}";
        if (_metrics.TryGetValue(key, out FontLineMetrics cached))
        {
            return cached;
        }

        FontFamily family = font.FontFamily;
        float emHeight = family.GetEmHeight(font.Style);
        float ascent = family.GetCellAscent(font.Style) / emHeight * font.Size;
        float descent = family.GetCellDescent(font.Style) / emHeight * font.Size;
        float lineSpacing = family.GetLineSpacing(font.Style) / emHeight * font.Size;

        var metrics = new FontLineMetrics(ascent, descent, MathF.Max(lineSpacing, ascent + descent));
        _metrics[key] = metrics;
        return metrics;
    }

    /// <summary>Graphics used by the painter; measurements use their own ruler.</summary>
    public Graphics Ruler => _ruler;

    public void Dispose()
    {
        foreach (Font font in _fonts.Values)
        {
            font.Dispose();
        }

        _fonts.Clear();
        _ruler.Dispose();
        _rulerSurface.Dispose();
    }
}

public readonly record struct FontLineMetrics(float Ascent, float Descent, float LineSpacing);

