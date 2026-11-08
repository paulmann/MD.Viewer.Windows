using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MdViewer.Core.Markdown;
using MdViewer.Render.Rendering;
using MdViewer.Render.Text;
using MdViewer.Render.Theming;

namespace MdViewer.App.Controls;

/// <summary>Common drawing helpers for the chrome (toolbar, tabs, status bar).</summary>
public abstract class ChromeControl : Control
{
    protected ChromeControl()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
    }

    protected Theme Theme { get; private set; } = ThemeCatalog.Light;

    protected FontBook Fonts { get; private set; } = new(1f);

    protected Palette Colors => Theme.Colors;

    protected float Dpi => DeviceDpi / 96f;

    protected float S(float value) => value * Theme.Scale * Dpi;

    public virtual void ApplyTheme(Theme theme, FontBook fonts)
    {
        Theme = theme;
        Fonts = fonts;
        BackColor = Colors.Canvas;
        Invalidate();
    }

    protected Font UiFont(float size, FontStyle style = FontStyle.Regular) => Fonts.UiFont(size * Theme.Scale, style);

    protected void DrawIcon(Graphics g, IconKind icon, RectangleF box, Color color, float stroke = 1.6f)
    {
        Icons.Draw(g, icon, box, color, MathF.Max(1.1f, stroke * Dpi * Theme.Scale));
    }

    protected static GraphicsPath RoundRect(RectangleF rect, float radius) => PaintContext.RoundedPath(rect, radius);
}

/// <summary>Icon button with optional label and active state.</summary>
public sealed class ToolButton : ChromeControl
{
    private bool _hover;
    private bool _pressed;

    public ToolButton()
    {
        SetStyle(ControlStyles.Selectable, false);
        TabStop = false;
    }

    public IconKind Icon { get; set; } = IconKind.More;

    public string? Label { get; set; }

    public bool Active { get; set; }

    public bool ShowHoverBackground { get; set; } = true;

    public string? ToolTipText { get; set; }

    public float IconSize { get; set; } = 16f;

    public override Size GetPreferredSize(Size proposedSize)
    {
        float height = S(32f);
        if (string.IsNullOrEmpty(Label))
        {
            return new Size((int)MathF.Ceiling(height), (int)MathF.Ceiling(height));
        }

        float width = S(IconSize) + S(20f) + Fonts.Measure(Label, UiFont(12.4f, FontStyle.Regular));
        return new Size((int)MathF.Ceiling(width), (int)MathF.Ceiling(height));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var background = new SolidBrush(Parent?.BackColor ?? Colors.Surface))
        {
            g.FillRectangle(background, ClientRectangle);
        }

        float height = MathF.Min(ClientSize.Height, S(32f));
        var rect = new RectangleF(0, (ClientSize.Height - height) / 2f, ClientSize.Width, height);

        if (ShowHoverBackground && (_hover || Active))
        {
            using var path = RoundRect(rect, S(8f));
            using var brush = new SolidBrush(Active ? Colors.AccentSoft : _pressed ? Colors.Border : Colors.Hover);
            g.FillPath(brush, path);
        }

        Color color = Active ? Colors.Accent : Colors.TextMuted;
        float iconBox = S(IconSize);
        float x = string.IsNullOrEmpty(Label) ? (ClientSize.Width - iconBox) / 2f : S(12f);
        DrawIcon(g, Icon, new RectangleF(x, (ClientSize.Height - iconBox) / 2f, iconBox, iconBox), color);

        if (!string.IsNullOrEmpty(Label))
        {
            Font font = UiFont(12.4f);
            float textX = x + iconBox + S(7f);
            float textY = (ClientSize.Height - Fonts.Metrics(font).LineSpacing) / 2f;
            TextRenderer.DrawText(g, Label, font, new Point((int)textX, (int)textY), color, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        _pressed = true;
        Invalidate();
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(e);
    }
}

/// <summary>Three-way view-mode switcher (Rendered / Source / Editor).</summary>
public sealed class ModeSwitcher : ChromeControl
{
    private int _selected;
    private int _hover = -1;

    public ModeSwitcher()
    {
        SetStyle(ControlStyles.Selectable, false);
        TabStop = false;
    }

    public string[] Items { get; set; } = ["Rendered", "Source", "Editor"];

    public int SelectedIndex
    {
        get => _selected;
        set
        {
            if (_selected == value)
            {
                return;
            }

            _selected = Math.Clamp(value, 0, Math.Max(0, Items.Length - 1));
            Invalidate();
        }
    }

    public event EventHandler? SelectedIndexChanged;

    public override Size GetPreferredSize(Size proposedSize)
    {
        float width = S(12f);
        Font font = UiFont(12.4f, FontStyle.Bold);
        foreach (string item in Items)
        {
            width += Fonts.Measure(item, font) + S(26f);
        }

        return new Size((int)MathF.Ceiling(width), (int)MathF.Ceiling(S(34f)));
    }

    private RectangleF[] SegmentBounds()
    {
        var result = new RectangleF[Items.Length];
        float pad = S(3f);
        float x = pad;
        float total = ClientSize.Width - pad * 2f;
        Font font = UiFont(12.4f, FontStyle.Bold);
        float weights = Items.Sum(i => Fonts.Measure(i, font) + S(26f));
        for (int i = 0; i < Items.Length; i++)
        {
            float width = total * ((Fonts.Measure(Items[i], font) + S(26f)) / weights);
            result[i] = new RectangleF(x, pad, width, ClientSize.Height - pad * 2f);
            x += width;
        }

        return result;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using (var background = new SolidBrush(Parent?.BackColor ?? Colors.Surface))
        {
            g.FillRectangle(background, ClientRectangle);
        }

        var outer = new RectangleF(0, (ClientSize.Height - S(34f)) / 2f, ClientSize.Width, S(34f));
        using (var path = RoundRect(outer, S(9f)))
        using (var brush = new SolidBrush(Theme.IsDark ? Colors.Elevated : Colors.Hover))
        {
            g.FillPath(brush, path);
        }

        RectangleF[] bounds = SegmentBounds();
        Font font = UiFont(12.4f, FontStyle.Bold);

        for (int i = 0; i < Items.Length; i++)
        {
            RectangleF rect = bounds[i];
            rect.Y += outer.Y;

            if (i == _selected)
            {
                using var path = RoundRect(rect, S(7f));
                using var brush = new SolidBrush(Colors.Surface);
                g.FillPath(brush, path);
                using var pen = new Pen(Theme.IsDark ? Colors.BorderStrong : Colors.Border, 1f);
                g.DrawPath(pen, path);
            }
            else if (i == _hover)
            {
                using var path = RoundRect(rect, S(7f));
                using var brush = new SolidBrush(Color.FromArgb(60, Colors.Border));
                g.FillPath(brush, path);
            }

            Color color = i == _selected ? Colors.Text : Colors.TextMuted;
            float width = Fonts.Measure(Items[i], font);
            float y = rect.Top + (rect.Height - Fonts.Metrics(font).LineSpacing) / 2f;
            g.DrawString(Items[i], font, new SolidBrush(color), rect.Left + (rect.Width - width) / 2f, y);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        int index = IndexAt(e.X, e.Y);
        if (index != _hover)
        {
            _hover = index;
            Invalidate();
        }

        Cursor = index >= 0 ? Cursors.Hand : Cursors.Default;
        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = -1;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        int index = IndexAt(e.X, e.Y);
        if (index >= 0 && index != _selected)
        {
            _selected = index;
            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }

        base.OnMouseDown(e);
    }

    private int IndexAt(int x, int y)
    {
        RectangleF outer = new(0, (ClientSize.Height - S(34f)) / 2f, ClientSize.Width, S(34f));
        if (y < outer.Top || y > outer.Bottom)
        {
            return -1;
        }

        RectangleF[] bounds = SegmentBounds();
        for (int i = 0; i < bounds.Length; i++)
        {
            RectangleF rect = bounds[i];
            rect.Y += outer.Y;
            if (rect.Contains(x, y))
            {
                return i;
            }
        }

        return -1;
    }
}

public sealed class TabItem
{
    public required string Title { get; init; }

    public bool Dirty { get; set; }

    public string? Path { get; set; }
}

/// <summary>Document tabs with close buttons, drawn as pills on the canvas colour.</summary>
public sealed class TabStrip : ChromeControl
{
    private int _hover = -1;
    private int _hoverClose = -1;

    public TabStrip()
    {
        SetStyle(ControlStyles.Selectable, false);
        TabStop = false;
        Height = (int)S(36f);
    }

    public List<TabItem> Tabs { get; } = [];

    public int SelectedIndex { get; set; }

    public event EventHandler<int>? TabSelected;

    public event EventHandler<int>? TabClosed;


    private RectangleF[] MeasureTabs()
    {
        var widths = new List<RectangleF>();
        Font font = UiFont(12.2f);
        float x = S(10f);
        foreach (TabItem tab in Tabs)
        {
            float width = MathF.Min(S(220f), Fonts.Measure(tab.Title, font) + S(56f));
            widths.Add(new RectangleF(x, S(4f), width, ClientSize.Height - S(4f)));
            x += width + S(4f);
        }

        return [.. widths];
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using (var brush = new SolidBrush(Colors.Canvas))
        {
            g.FillRectangle(brush, ClientRectangle);
        }

        if (Tabs.Count == 0)
        {
            return;
        }

        RectangleF[] rects = MeasureTabs();
        Font font = UiFont(12.2f);

        for (int i = 0; i < Tabs.Count && i < rects.Length; i++)
        {
            RectangleF rect = rects[i];
            bool active = i == SelectedIndex;

            if (active)
            {
                using var path = RoundRect(new RectangleF(rect.X, rect.Y, rect.Width, rect.Height + S(6f)), S(8f));
                using var brush = new SolidBrush(Colors.Surface);
                g.FillPath(brush, path);
                using var border = new Pen(Colors.Border, 1f);
                g.DrawPath(border, path);
            }
            else if (i == _hover)
            {
                using var path = RoundRect(rect, S(8f));
                using var brush = new SolidBrush(Colors.Hover);
                g.FillPath(brush, path);
            }

            Color textColor = active ? Colors.Text : Colors.TextMuted;
            float textWidth = Fonts.Measure(Tabs[i].Title, font);
            float available = rect.Width - S(40f);
            string title = Tabs[i].Title;
            if (textWidth > available)
            {
                while (title.Length > 1 && Fonts.Measure(title + "…", font) > available)
                {
                    title = title[..^1];
                }

                title += "…";
            }

            float y = rect.Top + (rect.Height - Fonts.Metrics(font).LineSpacing) / 2f;
            g.DrawString(title, font, new SolidBrush(textColor), rect.Left + S(14f), y);

            if (Tabs[i].Dirty)
            {
                using var dot = new SolidBrush(Colors.Accent);
                g.FillEllipse(dot, rect.Right - S(28f), rect.Top + rect.Height / 2f - S(2.5f), S(5f), S(5f));
            }

            if (i == _hoverClose || active)
            {
                float closeSize = S(14f);
                var closeRect = new RectangleF(rect.Right - closeSize - S(10f), rect.Top + (rect.Height - closeSize) / 2f, closeSize, closeSize);
                DrawIcon(g, IconKind.Close, closeRect, i == _hoverClose ? Colors.Text : Colors.TextFaint, 1.4f);
            }
        }
    }

    private int IndexAt(Point point, out bool onClose)
    {
        RectangleF[] rects = MeasureTabs();
        onClose = false;
        for (int i = 0; i < rects.Length; i++)
        {
            RectangleF rect = rects[i];
            if (!rect.Contains(point))
            {
                continue;
            }

            float closeSize = S(18f);
            onClose = point.X >= rect.Right - closeSize - S(8f);
            return i;
        }

        return -1;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        int index = IndexAt(e.Location, out bool onClose);
        int closeIndex = onClose ? index : -1;
        if (index != _hover || closeIndex != _hoverClose)
        {
            _hover = index;
            _hoverClose = closeIndex;
            Invalidate();
        }

        Cursor = index >= 0 ? Cursors.Hand : Cursors.Default;
        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = -1;
        _hoverClose = -1;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        int index = IndexAt(e.Location, out bool onClose);
        if (index < 0)
        {
            base.OnMouseDown(e);
            return;
        }

        if (onClose)
        {
            TabClosed?.Invoke(this, index);
            return;
        }

        if (index != SelectedIndex)
        {
            SelectedIndex = index;
            Invalidate();
            TabSelected?.Invoke(this, index);
        }

        base.OnMouseDown(e);
    }
}

/// <summary>Heading outline with scroll, hover and the current section highlighted.</summary>
public sealed class OutlinePanel : ChromeControl
{
    private readonly List<HeadingInfo> _items = [];
    private int _hover = -1;
    private float _scroll;
    private string? _activeId;
    private string _title = string.Empty;
    private string _subtitle = string.Empty;

    public OutlinePanel()
    {
        SetStyle(ControlStyles.Selectable, true);
        TabStop = false;
    }

    public event EventHandler<string>? AnchorActivated;

    public void SetDocument(MarkdownDocument? document)
    {
        _items.Clear();
        if (document is not null)
        {
            foreach (HeadingInfo heading in document.Outline)
            {
                if (heading.Level <= 4)
                {
                    _items.Add(heading);
                }
            }
        }

        _scroll = 0f;
        Invalidate();
    }

    public void SetHeader(string title, string subtitle)
    {
        _title = title;
        _subtitle = subtitle;
        Invalidate();
    }

    public void SetActive(string? id)
    {
        if (_activeId == id)
        {
            return;
        }

        _activeId = id;
        EnsureActiveVisible();
        Invalidate();
    }

    private float RowHeight => S(30f);

    private float HeaderHeight => S(64f);

    private void EnsureActiveVisible()
    {
        int index = _items.FindIndex(h => h.Id == _activeId);
        if (index < 0)
        {
            return;
        }

        float top = HeaderHeight + index * RowHeight - _scroll;
        if (top < HeaderHeight)
        {
            _scroll = MathF.Max(0f, index * RowHeight);
        }
        else if (top + RowHeight > ClientSize.Height)
        {
            _scroll = index * RowHeight - ClientSize.Height + RowHeight + HeaderHeight;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using (var brush = new SolidBrush(Colors.Canvas))
        {
            g.FillRectangle(brush, ClientRectangle);
        }

        using (var pen = new Pen(Colors.Border, 1f))
        {
            g.DrawLine(pen, ClientSize.Width - 1, 0, ClientSize.Width - 1, ClientSize.Height);
        }

        // header
        Font titleFont = UiFont(13.2f, FontStyle.Bold);
        Font subFont = UiFont(11.2f);
        g.DrawString(string.IsNullOrEmpty(_title) ? "DOCUMENT" : _title, titleFont, new SolidBrush(Colors.Text), S(16f), S(16f));
        if (!string.IsNullOrEmpty(_subtitle))
        {
            g.DrawString(_subtitle, subFont, new SolidBrush(Colors.TextFaint), S(16f), S(36f));
        }

        Font itemFont = UiFont(12.2f);
        float y = HeaderHeight - _scroll;
        for (int i = 0; i < _items.Count; i++)
        {
            HeadingInfo heading = _items[i];
            float indent = (heading.Level - 1) * S(14f);
            var row = new RectangleF(S(6f), y, ClientSize.Width - S(14f), RowHeight);
            bool active = heading.Id == _activeId;

            if (active)
            {
                using var path = RoundRect(new RectangleF(row.X, row.Y + S(1f), row.Width, row.Height - S(2f)), S(6f));
                using var brush = new SolidBrush(Colors.AccentSoft);
                g.FillPath(brush, path);
            }
            else if (i == _hover)
            {
                using var path = RoundRect(new RectangleF(row.X, row.Y + S(1f), row.Width, row.Height - S(2f)), S(6f));
                using var brush = new SolidBrush(Colors.Hover);
                g.FillPath(brush, path);
            }

            if (i > _hover && i < 0)
            {
                continue;
            }

            Color color = active ? Colors.Accent : heading.Level <= 2 ? Colors.Text : Colors.TextMuted;
            string text = heading.Text.Length == 0 ? "(untitled)" : heading.Text;
            float maxWidth = row.Width - indent - S(22f);
            if (Fonts.Measure(text, itemFont) > maxWidth)
            {
                while (text.Length > 1 && Fonts.Measure(text + "…", itemFont) > maxWidth)
                {
                    text = text[..^1];
                }

                text += "…";
            }

            float textY = row.Top + (RowHeight - Fonts.Metrics(itemFont).LineSpacing) / 2f;
            g.DrawString(text, itemFont, new SolidBrush(color), row.Left + S(10f) + indent, textY);

            y += RowHeight;
        }

        if (_items.Count == 0)
        {
            g.DrawString(L.T("empty.title"), subFont, new SolidBrush(Colors.TextFaint), S(18f), HeaderHeight + S(6f));
        }

        // scroll thumb
        float content = HeaderHeight + _items.Count * RowHeight;
        if (content > ClientSize.Height)
        {
            float trackHeight = ClientSize.Height - HeaderHeight - S(8f);
            float thumbHeight = MathF.Max(S(30f), trackHeight * (ClientSize.Height / content));
            float max = MathF.Max(1f, content - ClientSize.Height);
            float top = HeaderHeight + (trackHeight - thumbHeight) * Math.Clamp(_scroll / max, 0f, 1f);
            using var thumb = new SolidBrush(Color.FromArgb(140, Colors.ScrollThumb));
            g.FillRectangle(thumb, ClientSize.Width - S(6f), top, S(3f), thumbHeight);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        int index = (int)((e.Y + _scroll - HeaderHeight) / RowHeight);
        if (index < 0 || index >= _items.Count)
        {
            index = -1;
        }

        if (index != _hover)
        {
            _hover = index;
            Invalidate();
        }

        Cursor = index >= 0 ? Cursors.Hand : Cursors.Default;
        base.OnMouseMove(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = -1;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        int index = (int)((e.Y + _scroll - HeaderHeight) / RowHeight);
        if (index >= 0 && index < _items.Count)
        {
            AnchorActivated?.Invoke(this, _items[index].Id);
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        float content = HeaderHeight + _items.Count * RowHeight;
        _scroll = Math.Clamp(_scroll - e.Delta * 0.5f, 0f, MathF.Max(0f, content - ClientSize.Height));
        Invalidate();
        base.OnMouseWheel(e);
    }
}

/// <summary>Bottom status bar: mode, document stats, zoom.</summary>
public sealed class StatusBarView : ChromeControl
{
    private string _left = string.Empty;
    private string _middle = string.Empty;
    private string _right = string.Empty;
    private string _zoom = "100%";

    public StatusBarView()
    {
        SetStyle(ControlStyles.Selectable, false);
        TabStop = false;
    }

    public void Set(string left, string middle, string right, string zoom)
    {
        _left = left;
        _middle = middle;
        _right = right;
        _zoom = zoom;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        using (var brush = new SolidBrush(Colors.Canvas))
        {
            g.FillRectangle(brush, ClientRectangle);
        }

        using (var pen = new Pen(Colors.Border, 1f))
        {
            g.DrawLine(pen, 0, 0, ClientSize.Width, 0);
        }

        Font font = UiFont(11.2f);
        float y = (ClientSize.Height - Fonts.Metrics(font).LineSpacing) / 2f;

        g.DrawString(_left, font, new SolidBrush(Colors.TextMuted), S(14f), y);
        if (!string.IsNullOrEmpty(_middle))
        {
            float width = Fonts.Measure(_middle, font);
            g.DrawString(_middle, font, new SolidBrush(Colors.TextFaint), (ClientSize.Width - width) / 2f, y);
        }

        if (!string.IsNullOrEmpty(_right))
        {
            float width = Fonts.Measure(_right, font);
            g.DrawString(_right, font, new SolidBrush(Colors.TextFaint), ClientSize.Width - width - S(52f), y);
        }

        float zoomWidth = Fonts.Measure(_zoom, font);
        g.DrawString(_zoom, font, new SolidBrush(Colors.TextMuted), ClientSize.Width - zoomWidth - S(14f), y);
    }
}

/// <summary>Floating search bar: a themed text box plus previous/next/close.</summary>
public sealed class FindBar : ChromeControl
{
    private readonly TextBox _input;
    private string _count = string.Empty;

    public FindBar()
    {
        _input = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 10.5f),
        };

        _input.TextChanged += (_, _) => QueryChanged?.Invoke(this, _input.Text);
        _input.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                Navigate?.Invoke(this, e.Shift ? -1 : 1);
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                CloseRequested?.Invoke(this, EventArgs.Empty);
            }
        };

        Controls.Add(_input);
    }

    public event EventHandler<string>? QueryChanged;

    public event EventHandler<int>? Navigate;

    public event EventHandler? CloseRequested;

    public void SetCount(int current, int total)
    {
        _count = total == 0 ? L.T("find.none") : string.Format(System.Globalization.CultureInfo.CurrentCulture, L.T("find.count"), current + 1, total);
        Invalidate();
    }

    public void FocusInput(string? selection)
    {
        _input.Text = selection ?? string.Empty;
        _input.Focus();
        _input.SelectAll();
    }

    public override void ApplyTheme(Theme theme, FontBook fonts)
    {
        base.ApplyTheme(theme, fonts);
        _input.BackColor = Colors.Surface;
        _input.ForeColor = Colors.Text;
        PerformLayout();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        _input.SetBounds((int)S(40f), (int)((ClientSize.Height - _input.PreferredHeight) / 2f) + 1, ClientSize.Width - (int)S(150f), _input.PreferredHeight);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new RectangleF(0, 0, ClientSize.Width, ClientSize.Height - 1);
        using (var path = RoundRect(rect, S(9f)))
        {
            using var brush = new SolidBrush(Theme.IsDark ? Colors.Elevated : Colors.Surface);
            g.FillPath(brush, path);
            using var pen = new Pen(Colors.BorderStrong, 1f);
            g.DrawPath(pen, path);
        }

        DrawIcon(g, IconKind.Search, new RectangleF(S(12f), (ClientSize.Height - S(15f)) / 2f - 1, S(15f), S(15f)), Colors.TextFaint);

        Font font = UiFont(11.4f);
        float y = (ClientSize.Height - Fonts.Metrics(font).LineSpacing) / 2f;
        float countWidth = string.IsNullOrEmpty(_count) ? 0f : Fonts.Measure(_count, font);
        g.DrawString(_count, font, new SolidBrush(Colors.TextFaint), ClientSize.Width - S(96f) - countWidth + S(50f), y);

        DrawIcon(g, IconKind.ChevronUp, new RectangleF(ClientSize.Width - S(74f), (ClientSize.Height - S(16f)) / 2f, S(16f), S(16f)), Colors.TextMuted);
        DrawIcon(g, IconKind.ChevronDown, new RectangleF(ClientSize.Width - S(48f), (ClientSize.Height - S(16f)) / 2f, S(16f), S(16f)), Colors.TextMuted);
        DrawIcon(g, IconKind.Close, new RectangleF(ClientSize.Width - S(24f), (ClientSize.Height - S(14f)) / 2f, S(14f), S(14f)), Colors.TextMuted);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.X >= ClientSize.Width - S(34f))
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
        else if (e.X >= ClientSize.Width - S(84f) && e.X < ClientSize.Width - S(34f))
        {
            Navigate?.Invoke(this, e.X < ClientSize.Width - S(58f) ? -1 : 1);
        }

        base.OnMouseDown(e);
    }

    public void Clear() => _input.Text = string.Empty;
}

internal static class L2
{
}


