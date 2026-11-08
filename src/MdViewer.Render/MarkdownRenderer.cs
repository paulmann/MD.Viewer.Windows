using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using MdViewer.Core.Markdown;
using MdViewer.Render.Layout;
using MdViewer.Render.Media;
using MdViewer.Render.Rendering;
using MdViewer.Render.Text;
using MdViewer.Render.Theming;

namespace MdViewer.Render;

/// <summary>
/// Owns the theme-dependent font book, the image cache and the measured layout
/// of the current document. The window paints through this object; the headless
/// exporter uses the same code, so a screenshot is the real thing.
/// </summary>
public sealed class MarkdownRenderer : IDisposable
{
    private Theme _theme;
    private FontBook _fonts;
    private readonly ImageCache _images = new();
    private LayoutEngine? _engine;
    private DocumentLayout? _layout;
    private float _layoutWidth = -1f;
    private RenderSettings? _layoutSettings;
    private string? _layoutSignature;

    public MarkdownRenderer(Theme theme, float dpiScale)
    {
        _theme = theme;
        _fonts = new FontBook(dpiScale);
    }

    public Theme Theme => _theme;

    public FontBook Fonts => _fonts;

    public ImageCache Images => _images;

    public MarkdownDocument? Document { get; private set; }

    public DocumentLayout? Layout => _layout;

    public RenderSettings Settings { get; set; } = new();

    public event EventHandler? LayoutChanged;

    public void SetTheme(Theme theme)
    {
        if (Math.Abs(theme.Scale - _theme.Scale) > 0.0001f)
        {
            _fonts.Dispose();
            _fonts = new FontBook(_fonts.DpiScale);
        }

        _theme = theme;
        Invalidate();
    }

    public void SetDpiScale(float dpiScale)
    {
        if (Math.Abs(dpiScale - _fonts.DpiScale) < 0.001f)
        {
            return;
        }

        _fonts.Dispose();
        _fonts = new FontBook(dpiScale);
        Invalidate();
    }

    public void SetDocument(MarkdownDocument? document)
    {
        Document = document;
        Invalidate();
    }

    public void Invalidate()
    {
        _layout = null;
        _layoutWidth = -1f;
        _layoutSettings = null;
        _layoutSignature = null;
        _engine = null;
    }

    /// <summary>Lays the document out for the given content width (device pixels).</summary>
    public DocumentLayout? EnsureLayout(float contentWidth)
    {
        if (Document is null)
        {
            return null;
        }

        string signature = $"{Settings.NumberHeadings}|{Settings.CodeLineNumbers}|{Settings.WrapCode}|{Settings.UnderlineLinks}";
        if (_layout is not null
            && Math.Abs(contentWidth - _layoutWidth) < 0.5f
            && _layoutSignature == signature)
        {
            return _layout;
        }

        _engine = new LayoutEngine(_theme, _fonts, _images, Settings);
        _layoutWidth = contentWidth;
        _layoutSettings = Settings;
        _layoutSignature = signature;
        _layout = _engine.Layout(Document, contentWidth);
        LayoutChanged?.Invoke(this, EventArgs.Empty);
        return _layout;
    }

    /// <summary>Paints the document content (no sheet background, no chrome).</summary>
    public void PaintContent(Graphics g, float scrollY, string? hoverLink = null, string? hoverControl = null)
    {
        if (_layout is null)
        {
            return;
        }

        GraphicsState state = g.Save();
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.TranslateTransform(0f, -scrollY);

        using var context = new PaintContext
        {
            G = g,
            Theme = _theme,
            Fonts = _fonts,
            HoverLink = hoverLink,
            HoverControl = hoverControl,
        };

        foreach (BlockLayout block in _layout.Blocks)
        {
            if (block.Bottom < scrollY - 40f)
            {
                continue;
            }

            if (block.Top > scrollY + g.VisibleClipBounds.Height + 40f)
            {
                break;
            }

            block.Paint(context);
        }

        g.Restore(state);
    }

    /// <summary>Paints the reading sheet with the document on it (used by exports).</summary>
    public void PaintSheet(Graphics g, RectangleF sheet, float scrollY, float paddingX)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.FillRectangle(new SolidBrush(_theme.Colors.Surface), sheet);
        GraphicsState state = g.Save();
        g.TranslateTransform(sheet.Left + paddingX, sheet.Top);
        PaintContent(g, scrollY);
        g.Restore(state);
    }

    /// <summary>Renders the whole document to a bitmap (headless export / tests).</summary>
    public Bitmap RenderToBitmap(float width, int maxHeight = 0)
    {
        Typography type = _theme.Type;
        float dpi = _fonts.DpiScale;
        float padding = type.PagePaddingX * _theme.Scale * dpi;
        float contentWidth = MathF.Max(120f, width - padding * 2f);
        DocumentLayout layout = EnsureLayout(contentWidth)!;

        int height = maxHeight > 0 ? maxHeight : (int)Math.Ceiling(layout.Height);
        var bitmap = new Bitmap((int)Math.Ceiling(width), Math.Max(1, height), PixelFormat.Format32bppPArgb);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.Clear(_theme.Colors.Surface);
            g.TranslateTransform(padding, 0f);
            PaintContent(g, 0f);
        }

        return bitmap;
    }

    public void Dispose()
    {
        _fonts.Dispose();
        _images.Dispose();
    }
}

