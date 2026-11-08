using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MdViewer.Render;
using MdViewer.Render.Layout;
using MdViewer.Render.Rendering;
using MdViewer.Render.Text;
using MdViewer.Render.Theming;

namespace MdViewer.App;

/// <summary>
/// The rendered document surface: a scrolling sheet with the measured layout
/// painted on it, plus link hover/click, per-code copy buttons and search hits.
/// Everything is custom-drawn so it looks the same at every zoom level.
/// </summary>
public sealed class DocumentView : Control
{
    private readonly ScrollState _scroll = new();
    private readonly System.Windows.Forms.Timer _scrollTimer;
    private string? _hoverLink;
    private string? _hoverControl;
    private LinkRegion? _pressedLink;
    private List<RectangleF> _searchHits = [];
    private int _searchCurrent = -1;
    private bool _draggingScrollbar;
    private float _dragOffset;

    public DocumentView()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        TabStop = true;
        BackColor = Color.White;

        _scrollTimer = new System.Windows.Forms.Timer { Interval = 15 };
        _scrollTimer.Tick += (_, _) => AnimateScroll();
    }

    public MarkdownRenderer? Renderer { get; set; }

    public event EventHandler<string>? LinkActivated;

    public event EventHandler<int>? ZoomRequested;

    public event EventHandler? ScrollChanged;

    public float ScrollY => _scroll.Value;

    public float ContentHeight => Renderer?.Layout?.Height ?? 0f;

    public bool ShowSheet { get; set; } = true;

    /// <summary>Overrides the theme reading width (points at 100 % zoom).</summary>
    public float? ReadingWidthOverride { get; set; }

    public string? EmptyTitle { get; set; }

    public string? EmptyHint { get; set; }

    public Palette Colors => Renderer?.Theme.Colors ?? ThemeCatalog.Light.Colors;

    public void RefreshTheme()
    {
        BackColor = Colors.Canvas;
        Invalidate();
    }

    public void OnLayoutChanged()
    {
        _scroll.Max = Math.Max(0f, ContentHeight);
        if (_scroll.Value > _scroll.Max)
        {
            _scroll.Value = _scroll.Max;
        }

        Invalidate();
    }

    public void ScrollTo(float y, bool animate = false)
    {
        float target = Math.Clamp(y, 0f, Math.Max(0f, ContentHeight - ClientSize.Height + 40f));
        if (animate)
        {
            _scroll.Target = target;
            _scrollTimer.Start();
            return;
        }

        _scrollTimer.Stop();
        _scroll.Value = target;
        _scroll.Target = target;
        Invalidate();
        ScrollChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ScrollBy(float delta) => ScrollTo(_scroll.Value + delta, animate: MathF.Abs(delta) < 400f);

    public void ScrollToAnchor(string id)
    {
        if (Renderer?.Layout is null)
        {
            return;
        }

        if (Renderer.Layout.Anchors.TryGetValue(id, out BlockLayout? block))
        {
            ScrollTo(block.Top - Padding.Top + 12f, animate: true);
        }
    }

    public void ScrollToRatio(float ratio)
    {
        float max = Math.Max(0f, ContentHeight - ClientSize.Height);
        ScrollTo(max * Math.Clamp(ratio, 0f, 1f));
    }

    public void SetSearchHits(IReadOnlyList<RectangleF> hits, int current)
    {
        _searchHits = [.. hits];
        _searchCurrent = current;
        if (current >= 0 && current < hits.Count)
        {
            var hit = hits[current];
            float target = hit.Top - ClientSize.Height * 0.4f;
            ScrollTo(target, animate: true);
        }

        Invalidate();
    }

    public void ClearSearch()
    {
        _searchHits = [];
        _searchCurrent = -1;
        Invalidate();
    }

    // ------------------------------------------------------------- geometry

    private float DpiScale => DeviceDpi / 96f;

    private float PaddingX => (Renderer?.Theme.Type.PagePaddingX ?? 56f) * (Renderer?.Theme.Scale ?? 1f) * DpiScale;

    private float ContentWidth
    {
        get
        {
            Theme theme = Renderer?.Theme ?? ThemeCatalog.Light;
            float reading = (ReadingWidthOverride ?? theme.Type.MaxReadingWidth) * theme.Scale * DpiScale;
            if (Renderer is not null)
            {
                reading = Math.Min(reading, theme.Type.MaxPageWidth * theme.Scale * DpiScale);
            }

            float available = ClientSize.Width - 2f * PaddingX - 40f * DpiScale;
            return MathF.Max(240f, Math.Min(reading, available));
        }
    }

    private float SheetWidth => ContentWidth + 2f * PaddingX;

    private float SheetLeft => MathF.Max(0f, (ClientSize.Width - SheetWidth) / 2f);

    public void RebindLayout()
    {
        if (Renderer?.Document is null)
        {
            OnLayoutChanged();
            return;
        }

        Renderer.EnsureLayout(ContentWidth);
        OnLayoutChanged();
    }

    public RectangleF SheetRect => new(SheetLeft, -_scroll.Value, SheetWidth, ContentHeight);

    public float MeasuredContentWidth => ContentWidth;

    public float MeasuredSheetLeft => SheetLeft;

    // --------------------------------------------------------------- painting

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using (var canvas = new SolidBrush(Colors.Canvas))
        {
            g.FillRectangle(canvas, ClientRectangle);
        }

        if (Renderer?.Layout is null || Renderer.Document is null)
        {
            PaintEmptyState(g);
            PaintScrollbar(g);
            return;
        }

        DocumentLayout layout = Renderer.Layout;
        float sheetTop = -_scroll.Value;
        float sheetHeight = MathF.Max(layout.Height, ClientSize.Height) + _scroll.Value;

        var sheet = new RectangleF(SheetLeft, sheetTop, SheetWidth, sheetHeight != 0 ? layout.Height : ClientSize.Height);

        if (ShowSheet)
        {
            PaintSheetShadow(g, sheet);
            using var surface = new SolidBrush(Colors.Surface);
            g.FillRectangle(surface, sheet);
            using var border = new Pen(Colors.Border, 1f);
            g.DrawRectangle(border, sheet.X, sheet.Y, sheet.Width, sheet.Height);
        }

        GraphicsState state = g.Save();
        var clip = new RectangleF(0, 0, ClientSize.Width, ClientSize.Height);
        g.SetClip(clip);
        g.TranslateTransform(sheet.Left + PaddingX, sheetTop);

        Renderer.PaintContent(g, 0f, _hoverLink, _hoverControl);

        // search highlights
        if (_searchHits.Count > 0)
        {
            for (int i = 0; i < _searchHits.Count; i++)
            {
                bool current = i == _searchCurrent;
                Color fill = current ? Color.FromArgb(150, Colors.Accent) : Color.FromArgb(70, Color.Goldenrod);
                using var brush = new SolidBrush(fill);
                RectangleF hit = _searchHits[i];
                g.FillRectangle(brush, hit);
                if (current)
                {
                    using var pen = new Pen(Colors.Accent, 1.4f);
                    g.DrawRectangle(pen, hit.X, hit.Y, hit.Width, hit.Height);
                }
            }
        }

        g.Restore(state);
        PaintScrollbar(g);
    }

    private void PaintSheetShadow(Graphics g, RectangleF sheet)
    {
        for (int i = 6; i >= 1; i--)
        {
            int alpha = (int)(Colors.Shadow.A * 0.16f / i);
            using var pen = new Pen(Color.FromArgb(Math.Max(2, alpha), Colors.Shadow), i * 1.2f);
            g.DrawRectangle(pen, sheet.X - i, sheet.Y - i, sheet.Width + i * 2, sheet.Height + i * 2);
        }
    }

    private void PaintEmptyState(Graphics g)
    {
        Theme theme = Renderer?.Theme ?? ThemeCatalog.Light;
        Font title = new(theme.SerifReading ? "Georgia" : "Segoe UI", 17f * theme.Scale, FontStyle.Bold);
        Font hint = new("Segoe UI", 10.5f * theme.Scale);
        try
        {
            string text = EmptyTitle ?? L.T("empty.title");
            string sub = EmptyHint ?? L.T("empty.hint");
            SizeF size = g.MeasureString(text, title, int.MaxValue, TextFormat.Typographic);
            SizeF size2 = g.MeasureString(sub, hint, int.MaxValue, TextFormat.Typographic);
            float cx = ClientSize.Width / 2f;
            float cy = ClientSize.Height / 2f - 30f;

            using (var brush = new SolidBrush(Color.FromArgb(26, Colors.Text)))
            {
                var docRect = new RectangleF(cx - 22f, cy - 92f, 44f, 54f);
                g.FillRoundRect(brush, docRect, 7f);
            }

            using (var brush = new SolidBrush(Colors.Text))
            using (var brush2 = new SolidBrush(Colors.TextMuted))
            {
                g.DrawString(text, title, brush, cx - size.Width / 2f, cy);
                g.DrawString(sub, hint, brush2, cx - size2.Width / 2f, cy + size.Height + 10f);
            }
        }
        finally
        {
            title.Dispose();
            hint.Dispose();
        }
    }

    private void PaintScrollbar(Graphics g)
    {
        float contentHeight = ContentHeight;
        if (contentHeight <= ClientSize.Height + 4 || _scroll.Max <= 0)
        {
            return;
        }

        const float width = 8f;
        float x = ClientSize.Width - width - 3f;
        float trackTop = 6f;
        float trackHeight = ClientSize.Height - 12f;
        float viewport = ClientSize.Height;
        float thumbHeight = MathF.Max(40f, trackHeight * (viewport / contentHeight));
        float progress = _scroll.Max <= 0 ? 0f : Math.Clamp(_scroll.Value / _scroll.Max, 0f, 1f);
        float thumbTop = trackTop + (trackHeight - thumbHeight) * progress;

        Color color = _draggingScrollbar ? Colors.ScrollThumbHover : Colors.ScrollThumb;
        using var brush = new SolidBrush(Color.FromArgb(_draggingScrollbar ? 230 : 170, color));
        var rect = new RectangleF(x, thumbTop, width, thumbHeight);
        using GraphicsPath path = PaintContext.RoundedPath(rect, width / 2f);
        g.FillPath(brush, path);
    }

    // ----------------------------------------------------------------- input

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (ModifierKeys.HasFlag(Keys.Control))
        {
            ZoomRequested?.Invoke(this, e.Delta > 0 ? 1 : -1);
            return;
        }

        float delta = -e.Delta * 0.9f;
        _scroll.Target = Math.Clamp(_scroll.Target + delta, 0f, Math.Max(0f, ContentHeight - ClientSize.Height + 40f));
        _scrollTimer.Start();
        base.OnMouseWheel(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_draggingScrollbar)
        {
            float trackHeight = ClientSize.Height - 12f;
            float thumbHeight = MathF.Max(40f, trackHeight * (ClientSize.Height / MathF.Max(1f, ContentHeight)));
            float ratio = Math.Clamp((e.Y - _dragOffset - 6f) / MathF.Max(1f, trackHeight - thumbHeight), 0f, 1f);
            ScrollTo(_scroll.Max * ratio);
            return;
        }

        if (Renderer?.Layout is null)
        {
            return;
        }

        PointF content = new(e.X - SheetLeft - PaddingX, e.Y + _scroll.Value);
        LinkRegion? link = Renderer.Layout.HitTestLink(content);
        string? hoverControl = null;

        if (link is null)
        {
            foreach (BlockLayout block in Renderer.Layout.Blocks)
            {
                if (block is CodeVisual code && content.Y >= code.Top && content.Y <= code.Bottom && code.CopyButton.Contains(content.X - 0, content.Y))
                {
                    hoverControl = "copy:" + code.AnchorId;
                    break;
                }
            }
        }

        string? hoverLink = link?.Target;
        if (hoverLink != _hoverLink || hoverControl != _hoverControl)
        {
            _hoverLink = hoverLink;
            _hoverControl = hoverControl;
            Invalidate();
        }

        Cursor = link is null ? (hoverControl is null ? Cursors.IBeam : Cursors.Hand) : Cursors.Hand;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        float contentHeight = ContentHeight;
        if (contentHeight > ClientSize.Height + 4 && e.X >= ClientSize.Width - 14)
        {
            _draggingScrollbar = true;
            float trackHeight = ClientSize.Height - 12f;
            float thumbHeight = MathF.Max(40f, trackHeight * (ClientSize.Height / MathF.Max(1f, contentHeight)));
            float progress = _scroll.Max <= 0 ? 0f : Math.Clamp(_scroll.Value / _scroll.Max, 0f, 1f);
            float thumbTop = 6f + (trackHeight - thumbHeight) * progress;
            _dragOffset = e.Y >= thumbTop && e.Y <= thumbTop + thumbHeight ? e.Y - thumbTop : thumbHeight / 2f;
            if (e.Y < thumbTop || e.Y > thumbTop + thumbHeight)
            {
                float ratio = Math.Clamp((e.Y - _dragOffset - 6f) / MathF.Max(1f, trackHeight - thumbHeight), 0f, 1f);
                ScrollTo(_scroll.Max * ratio);
            }

            Invalidate();
            return;
        }

        if (Renderer?.Layout is null)
        {
            return;
        }

        PointF content = new(e.X - SheetLeft - PaddingX, e.Y + _scroll.Value);
        _pressedLink = Renderer.Layout.HitTestLink(content);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (_draggingScrollbar)
        {
            _draggingScrollbar = false;
            Invalidate();
            return;
        }

        if (Renderer?.Layout is null)
        {
            return;
        }

        PointF content = new(e.X - SheetLeft - PaddingX, e.Y + _scroll.Value);
        LinkRegion? link = Renderer.Layout.HitTestLink(content);

        if (link is not null && _pressedLink is not null && link.Target == _pressedLink.Target)
        {
            LinkActivated?.Invoke(this, link.Target);
            _pressedLink = null;
            return;
        }

        _pressedLink = null;

        foreach (BlockLayout block in Renderer.Layout.Blocks)
        {
            if (block is CodeVisual code
                && content.Y >= code.Top && content.Y <= code.Bottom
                && code.CopyButton.Contains(content.X, content.Y))
            {
                CopyCode(code);
                return;
            }
        }
    }

    private void CopyCode(CodeVisual code)
    {
        if (code.Source is not MdViewer.Core.Markdown.CodeBlock block)
        {
            return;
        }

        try
        {
            Clipboard.SetText(block.Text);
            Toast?.Invoke(this, L.T("toast.copiedCode"));
        }
        catch
        {
            // clipboard busy — ignore
        }
    }

    /// <summary>Raised with a short message for the status toast.</summary>
    public event EventHandler<string>? Toast;

    protected override bool IsInputKey(Keys keyData) => keyData switch
    {
        Keys.Up or Keys.Down or Keys.Left or Keys.Right or Keys.PageUp or Keys.PageDown or Keys.Home or Keys.End or Keys.Space => true,
        _ => base.IsInputKey(keyData),
    };

    protected override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Down:
                ScrollBy(50f);
                e.Handled = true;
                break;
            case Keys.Up:
                ScrollBy(-50f);
                e.Handled = true;
                break;
            case Keys.PageDown:
            case Keys.Space:
                ScrollBy(ClientSize.Height * 0.9f);
                e.Handled = true;
                break;
            case Keys.PageUp:
                ScrollBy(-ClientSize.Height * 0.9f);
                e.Handled = true;
                break;
            case Keys.Home:
                ScrollTo(0f);
                e.Handled = true;
                break;
            case Keys.End:
                ScrollTo(ContentHeight);
                e.Handled = true;
                break;
        }

        base.OnKeyDown(e);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        RebindLayout();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hoverLink is not null || _hoverControl is not null)
        {
            _hoverLink = null;
            _hoverControl = null;
            Invalidate();
        }
    }

    private void AnimateScroll()
    {
        float distance = _scroll.Target - _scroll.Value;
        if (MathF.Abs(distance) < 1.2f)
        {
            _scroll.Value = _scroll.Target;
            _scrollTimer.Stop();
        }
        else
        {
            _scroll.Value += distance * 0.32f;
        }

        Invalidate();
        ScrollChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scrollTimer.Dispose();
        }

        base.Dispose(disposing);
    }

    private sealed class ScrollState
    {
        public float Value;
        public float Target;
        public float Max;
    }
}

internal static class GraphicsExtensions
{
    public static void FillRoundRect(this Graphics g, Brush brush, RectangleF rect, float radius)
    {
        using GraphicsPath path = PaintContext.RoundedPath(rect, radius);
        g.FillPath(brush, path);
    }
}




