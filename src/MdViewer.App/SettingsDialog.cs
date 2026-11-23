using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MdViewer.Render.Rendering;
using MdViewer.Render.Text;
using MdViewer.Render.Theming;

namespace MdViewer.App;

/// <summary>
/// Preferences: new files in tabs or in a separate window, and the rendered width.
/// Drawn by hand like the rest of the chrome (the app carries no control theming),
/// with a small keyboard model so it stays usable without a mouse.
/// </summary>
public sealed class SettingsDialog : Form
{
    private enum RowKind
    {
        Radio,
        Check,
        Button,
    }

    private sealed class Row
    {
        public required string Id { get; init; }
        public required RowKind Kind { get; init; }
        public string Text { get; set; } = string.Empty;
        public bool Checked { get; set; }
        public RectangleF Bounds { get; set; }
        public bool IsPrimary { get; init; }
    }

    private readonly Theme _theme;
    private readonly FontBook _fonts;
    private readonly List<Row> _rows = [];
    private int _focus = -1;
    private int _hover = -1;

    public SettingsDialog(AppSettings settings, string widthMode, Theme theme)
    {
        _theme = theme;
        _fonts = new FontBook(DeviceDpi / 96f);

        OpenInNewWindow = settings.OpenInNewWindow;
        RememberLastMode = settings.RememberMode;
        ShowOutline = settings.Outline;
        WidthMode = Normalize(widthMode);

        Text = L.T("prefs.title");
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = theme.Colors.Surface;
        ForeColor = theme.Colors.Text;
        ClientSize = new Size((int)S(520f), (int)S(430f));
        KeyPreview = true;
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Icon = AppIcon.Create();

        BuildRows();
    }

    public bool OpenInNewWindow { get; private set; }

    public bool RememberLastMode { get; private set; }

    public bool ShowOutline { get; private set; }

    public string WidthMode { get; private set; }

    private float Dpi => DeviceDpi / 96f;

    private float S(float value) => value * _theme.Scale * Dpi;

    private Palette Colors => _theme.Colors;

    private Font Ui(float size, FontStyle style = FontStyle.Regular) => _fonts.UiFont(size * _theme.Scale, style);

    private static string Normalize(string? mode) => mode switch
    {
        "75" => "75",
        "50" => "50",
        _ => "full",
    };

    private void BuildRows()
    {
        _rows.Clear();
        _rows.Add(new Row { Id = "tabs", Kind = RowKind.Radio, Text = L.T("prefs.tabs"), Checked = !OpenInNewWindow });
        _rows.Add(new Row { Id = "window", Kind = RowKind.Radio, Text = L.T("prefs.newWindow"), Checked = OpenInNewWindow });
        _rows.Add(new Row { Id = "w-full", Kind = RowKind.Radio, Text = L.T("width.full"), Checked = WidthMode == "full" });
        _rows.Add(new Row { Id = "w-75", Kind = RowKind.Radio, Text = L.T("width.75"), Checked = WidthMode == "75" });
        _rows.Add(new Row { Id = "w-50", Kind = RowKind.Radio, Text = L.T("width.50"), Checked = WidthMode == "50" });
        _rows.Add(new Row { Id = "remember", Kind = RowKind.Check, Text = L.T("prefs.rememberMode"), Checked = RememberLastMode });
        _rows.Add(new Row { Id = "outline", Kind = RowKind.Check, Text = L.T("prefs.outline"), Checked = ShowOutline });
        _rows.Add(new Row { Id = "ok", Kind = RowKind.Button, Text = L.T("prefs.ok"), IsPrimary = true });
        _rows.Add(new Row { Id = "cancel", Kind = RowKind.Button, Text = L.T("prefs.cancel") });
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.Clear(Colors.Surface);

        float margin = S(24f);
        float y = margin;

        y = DrawCard(g, margin, y, L.T("prefs.files"), ["tabs", "window"], S(96f));
        y = DrawHint(g, margin, y, L.T("prefs.filesHint"));
        y += S(10f);
        y = DrawCard(g, margin, y, L.T("prefs.width"), ["w-full", "w-75", "w-50"], S(112f));
        y = DrawCard(g, margin, y, L.T("prefs.view"), ["remember", "outline"], S(80f));

        LayoutButtons(g, margin);
    }

    private float DrawCard(Graphics g, float margin, float top, string title, string[] ids, float height)
    {
        var card = new RectangleF(margin, top, ClientSize.Width - margin * 2f, height);
        using (var path = PaintContext.RoundedPath(card, S(10f)))
        using (var fill = new SolidBrush(Colors.Canvas))
        using (var border = new Pen(Colors.Border, MathF.Max(1f, S(1f))))
        {
            g.FillPath(fill, path);
            g.DrawPath(border, path);
        }

        g.DrawString(title, Ui(11.2f, FontStyle.Bold), new SolidBrush(Colors.TextFaint), card.Left + S(14f), card.Top + S(10f));

        float rowTop = card.Top + S(32f);
        foreach (string id in ids)
        {
            Row row = _rows.First(r => r.Id == id);
            row.Bounds = new RectangleF(card.Left + S(14f), rowTop, card.Width - S(28f), S(24f));
            DrawMarker(g, row);
            g.DrawString(row.Text, Ui(12.4f), new SolidBrush(Colors.Text), row.Bounds.Left + S(24f), rowTop + S(3f));
            rowTop += S(26f);
        }

        return card.Bottom + S(8f);
    }

    private float DrawHint(Graphics g, float margin, float top, string text)
    {
        float width = ClientSize.Width - margin * 2f - S(28f);
        using var format = new StringFormat { FormatFlags = StringFormatFlags.LineLimit };
        SizeF size = g.MeasureString(text, Ui(11f), new SizeF(width, S(48f)), format);
        g.DrawString(text, Ui(11f), new SolidBrush(Colors.TextMuted), margin + S(14f), top + S(2f), format);
        return top + size.Height + S(6f);
    }

    private void LayoutButtons(Graphics g, float margin)
    {
        float bottom = ClientSize.Height - margin - S(28f);
        float x = ClientSize.Width - margin - S(92f);
        Row cancel = _rows.First(r => r.Id == "cancel");
        Row ok = _rows.First(r => r.Id == "ok");
        cancel.Bounds = new RectangleF(x, bottom, S(92f), S(28f));
        ok.Bounds = new RectangleF(x - S(100f), bottom, S(92f), S(28f));
        DrawButton(g, ok);
        DrawButton(g, cancel);
    }

    private void DrawMarker(Graphics g, Row row)
    {
        float size = S(14f);
        var box = new RectangleF(row.Bounds.Left + S(2f), row.Bounds.Top + (row.Bounds.Height - size) / 2f, size, size);
        bool focused = _focus >= 0 && ReferenceEquals(_rows[_focus], row);

        if (row.Kind == RowKind.Check)
        {
            using var path = PaintContext.RoundedPath(box, S(3f));
            using var fill = new SolidBrush(row.Checked ? Colors.Accent : Colors.Surface);
            using var pen = new Pen(focused ? Colors.Accent : Colors.Border, MathF.Max(1f, S(1.2f)));
            g.FillPath(fill, path);
            g.DrawPath(pen, path);
            if (row.Checked)
            {
                using var tick = new Pen(Colors.Surface, MathF.Max(1.4f, S(1.8f))) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawLines(tick,
                [
                    new PointF(box.Left + size * 0.24f, box.Top + size * 0.52f),
                    new PointF(box.Left + size * 0.44f, box.Top + size * 0.72f),
                    new PointF(box.Left + size * 0.78f, box.Top + size * 0.28f),
                ]);
            }

            return;
        }

        using var ring = new Pen(focused || row.Checked ? Colors.Accent : Colors.Border, MathF.Max(1.2f, S(1.4f)));
        g.DrawEllipse(ring, box);
        if (row.Checked)
        {
            using var dot = new SolidBrush(Colors.Accent);
            g.FillEllipse(dot, box.Left + size * 0.28f, box.Top + size * 0.28f, size * 0.44f, size * 0.44f);
        }
    }

    private void DrawButton(Graphics g, Row row)
    {
        bool primary = row.IsPrimary;
        bool hovered = _hover >= 0 && ReferenceEquals(_rows[_hover], row);
        bool focused = _focus >= 0 && ReferenceEquals(_rows[_focus], row);

        Color fill = primary
            ? (hovered ? ControlPaint.Light(Colors.Accent, 0.22f) : Colors.Accent)
            : (hovered ? Colors.Hover : Colors.Surface);
        Color text = primary ? Colors.Surface : Colors.Text;
        Color border = focused ? Colors.Accent : Colors.Border;

        using (var path = PaintContext.RoundedPath(row.Bounds, S(8f)))
        using (var brush = new SolidBrush(fill))
        using (var pen = new Pen(border, MathF.Max(1f, S(1f))))
        {
            g.FillPath(brush, path);
            g.DrawPath(pen, path);
        }

        Font font = Ui(12.2f);
        SizeF size = g.MeasureString(row.Text, font);
        g.DrawString(row.Text, font, new SolidBrush(text),
            row.Bounds.Left + (row.Bounds.Width - size.Width) / 2f,
            row.Bounds.Top + (row.Bounds.Height - size.Height) / 2f);
    }

    private Row? RowAt(Point point)
    {
        foreach (Row row in _rows)
        {
            if (row.Bounds.Contains(point))
            {
                return row;
            }
        }

        return null;
    }

    private void Activate(Row row)
    {
        switch (row.Id)
        {
            case "tabs":
                OpenInNewWindow = false;
                break;
            case "window":
                OpenInNewWindow = true;
                break;
            case "w-full":
                WidthMode = "full";
                break;
            case "w-75":
                WidthMode = "75";
                break;
            case "w-50":
                WidthMode = "50";
                break;
            case "remember":
                RememberLastMode = !RememberLastMode;
                break;
            case "outline":
                ShowOutline = !ShowOutline;
                break;
            case "ok":
                DialogResult = DialogResult.OK;
                Close();
                return;
            case "cancel":
                DialogResult = DialogResult.Cancel;
                Close();
                return;
        }

        SyncRows();
        Invalidate();
    }

    private void SyncRows()
    {
        foreach (Row row in _rows)
        {
            row.Checked = row.Id switch
            {
                "tabs" => !OpenInNewWindow,
                "window" => OpenInNewWindow,
                "w-full" => WidthMode == "full",
                "w-75" => WidthMode == "75",
                "w-50" => WidthMode == "50",
                "remember" => RememberLastMode,
                "outline" => ShowOutline,
                _ => row.Checked,
            };
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        Row? row = RowAt(e.Location);
        int index = row is null ? -1 : _rows.IndexOf(row);
        if (index != _hover)
        {
            _hover = index;
            Cursor = row is not null ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

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
        Row? row = RowAt(e.Location);
        if (row is not null)
        {
            _focus = _rows.IndexOf(row);
            Activate(row);
            return;
        }

        base.OnMouseDown(e);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Escape:
                DialogResult = DialogResult.Cancel;
                Close();
                return true;

            case Keys.Enter:
                Activate(_rows.First(r => r.Id == (_focus >= 0 && _rows[_focus].Id is "cancel" ? "cancel" : "ok")));
                return true;

            case Keys.Down or Keys.Tab:
                MoveFocus(1);
                return true;

            case Keys.Up or Keys.Shift | Keys.Tab:
                MoveFocus(-1);
                return true;

            case Keys.Space:
                if (_focus >= 0)
                {
                    Activate(_rows[_focus]);
                }

                return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void MoveFocus(int delta)
    {
        _focus = _focus < 0
            ? 0
            : (_focus + delta + _rows.Count) % _rows.Count;
        Invalidate();
    }
}
