using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MdViewer.Render.Rendering;
using MdViewer.Render.Text;
using MdViewer.Render.Theming;

namespace MdViewer.App;

/// <summary>
/// About page modelled on the TrayIconGuard one: product card with version, licence
/// and author, a links card, a diagnostics button, and the credit/privacy lines.
/// </summary>
public sealed class AboutDialog : Form
{
    private const string RepoUrl = "https://github.com/paulmann/MD.Viewer.Windows";
    private const string LicenseUrl = "https://github.com/paulmann/MD.Viewer.Windows/blob/main/LICENSE";
    private const string DocsUrl = "https://github.com/paulmann/MD.Viewer.Windows/tree/main/docs";
    private const string AuthorName = "Mikhail Deynekin";
    private const string AuthorEmail = "Mikhail@Deynekin.com";
    private const string AuthorSite = "https://Deynekin.com";

    private readonly Theme _theme;
    private readonly FontBook _fonts;
    private readonly List<(RectangleF Bounds, string Action)> _hits = [];
    private string? _hover;

    public AboutDialog(Theme theme)
    {
        _theme = theme;
        _fonts = new FontBook(DeviceDpi / 96f);

        Text = L.T("about.title");
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = theme.Colors.Surface;
        ForeColor = theme.Colors.Text;
        ClientSize = new Size((int)S(660f), (int)S(500f));

        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        Icon = AppIcon.Create();
    }

    private float Dpi => DeviceDpi / 96f;

    private float S(float value) => value * _theme.Scale * Dpi;

    private Palette Colors => _theme.Colors;

    private Font Ui(float size, FontStyle style = FontStyle.Regular) => _fonts.UiFont(size * _theme.Scale, style);

    private static string Version => typeof(AboutDialog).Assembly.GetName().Version?.ToString(3) ?? "1.1.0";

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.Clear(Colors.Surface);

        float margin = S(26f);
        float y = margin;

        // icon + product title
        var iconRect = new RectangleF(margin, y, S(46f), S(46f));
        using (Icon? icon = AppIcon.Create())
        {
            g.DrawIcon(icon, Rectangle.Round(iconRect));
        }

        float textLeft = margin + S(46f) + S(16f);
        Font titleFont = Ui(19f, FontStyle.Bold);
        Font subFont = Ui(12f);
        g.DrawString($"MD Viewer {Version} «Scribe»", titleFont, new SolidBrush(Colors.Text), textLeft, y - S(2f));
        g.DrawString(L.T("about.subtitle"), subFont, new SolidBrush(Colors.TextMuted), textLeft, y + S(27f));

        y += S(62f);

        // product card
        var card = new RectangleF(margin, y, ClientSize.Width - margin * 2f, S(126f));
        FillCard(g, card);

        float rowTop = card.Top + S(14f);
        float labelWidth = S(92f);
        DrawRow(g, card.Left + S(16f), ref rowTop, labelWidth, L.T("about.version"), $"{Version}  ·  .NET {Environment.Version}  ·  C# 15");
        DrawRow(g, card.Left + S(16f), ref rowTop, labelWidth, L.T("about.license"), "PolyForm Noncommercial 1.0.0", LicenseUrl, "license");
        DrawRow(g, card.Left + S(16f), ref rowTop, labelWidth, L.T("about.author"), $"{AuthorName}  <{AuthorEmail}>", AuthorSite, "site");
        DrawRow(g, card.Left + S(16f), ref rowTop, labelWidth, L.T("about.build"), L.T("about.buildValue"));

        y = card.Bottom + S(14f);

        // links card
        var links = new RectangleF(margin, y, ClientSize.Width - margin * 2f, S(104f));
        FillCard(g, links);

        g.DrawString(L.T("about.links"), Ui(11f, FontStyle.Bold), new SolidBrush(Colors.TextFaint), links.Left + S(16f), links.Top + S(12f));

        float buttonX = links.Left + S(16f);
        float buttonY = links.Top + S(36f);
        buttonX = DrawButton(g, buttonX, buttonY, L.T("about.repo"), "repo", accent: true) + S(10f);
        buttonX = DrawButton(g, buttonX, buttonY, L.T("about.licenseButton"), "license") + S(10f);
        buttonX = DrawButton(g, buttonX, buttonY, L.T("about.docs"), "docs") + S(10f);
        DrawButton(g, buttonX, buttonY, L.T("about.copyDiag"), "diag");

        g.DrawString($"{RepoUrl}  ·  {AuthorSite}", Ui(10.6f), new SolidBrush(Colors.TextFaint), links.Left + S(16f), links.Top + S(74f));

        y = links.Bottom + S(14f);

        // credits + privacy
        Font credits = Ui(11.2f);
        float width = ClientSize.Width - margin * 2f;
        foreach (string line in Wrap(L.T("about.credits"), credits, width))
        {
            g.DrawString(line, credits, new SolidBrush(Colors.TextMuted), margin, y);
            y += _fonts.Metrics(credits).LineSpacing;
        }

        y += S(4f);
        foreach (string line in Wrap(L.T("about.privacy"), credits, width))
        {
            g.DrawString(line, credits, new SolidBrush(Colors.TextFaint), margin, y);
            y += _fonts.Metrics(credits).LineSpacing;
        }

        // OK button
        var ok = new RectangleF(ClientSize.Width - margin - S(92f), ClientSize.Height - margin - S(32f), S(92f), S(32f));
        _hits.RemoveAll(h => h.Action == "ok");
        _hits.Add((ok, "ok"));
        bool hoverOk = _hover == "ok";
        using (var path = PaintContext.RoundedPath(ok, S(7f)))
        using (var brush = new SolidBrush(hoverOk ? Palette.Blend(Colors.Accent, Colors.Text, 0.12) : Colors.Accent))
        {
            g.FillPath(brush, path);
        }

        var okText = L.T("about.ok");
        float okWidth = _fonts.Measure(okText, Ui(12.4f, FontStyle.Bold));
        g.DrawString(okText, Ui(12.4f, FontStyle.Bold), new SolidBrush(Colors.AccentText), ok.Left + (ok.Width - okWidth) / 2f, ok.Top + S(7f));
    }

    private void FillCard(Graphics g, RectangleF rect)
    {
        using GraphicsPath path = PaintContext.RoundedPath(rect, S(10f));
        using var fill = new SolidBrush(_theme.IsDark ? Colors.Elevated : Colors.QuoteBg);
        g.FillPath(fill, path);
        using var pen = new Pen(Colors.Border, MathF.Max(1f, S(1f)));
        g.DrawPath(pen, path);
    }

    private void DrawRow(Graphics g, float x, ref float top, float labelWidth, string label, string value, string? url = null, string? action = null)
    {
        g.DrawString(label, Ui(11.4f), new SolidBrush(Colors.TextFaint), x, top);

        Color color = url is null ? Colors.Text : Colors.Link;
        Font font = Ui(11.8f);
        g.DrawString(value, font, new SolidBrush(color), x + labelWidth, top);

        if (url is not null && action is not null)
        {
            float width = _fonts.Measure(value, font);
            var bounds = new RectangleF(x + labelWidth, top - S(2f), width, _fonts.Metrics(font).LineSpacing + S(4f));
            _hits.RemoveAll(h => h.Action == action);
            _hits.Add((bounds, action));

            if (_hover == action)
            {
                float underline = top + _fonts.Metrics(font).Ascent + S(2f);
                using var pen = new Pen(Colors.Link, MathF.Max(1f, S(1f)));
                g.DrawLine(pen, bounds.Left, underline, bounds.Right, underline);
            }
        }

        top += _fonts.Metrics(font).LineSpacing + S(7f);
    }

    private float DrawButton(Graphics g, float x, float y, string text, string action, bool accent = false)
    {
        Font font = Ui(11.6f, accent ? FontStyle.Bold : FontStyle.Regular);
        float width = _fonts.Measure(text, font) + S(22f);
        var rect = new RectangleF(x, y, width, S(30f));

        _hits.RemoveAll(h => h.Action == action);
        _hits.Add((rect, action));

        bool hover = _hover == action;
        using (GraphicsPath path = PaintContext.RoundedPath(rect, S(7f)))
        {
            Color fill = accent
                ? (hover ? Palette.Blend(Colors.Accent, Colors.Text, 0.12) : Colors.Accent)
                : (hover ? Colors.Hover : (Colors.Surface));
            using var brush = new SolidBrush(fill);
            g.FillPath(brush, path);

            if (!accent)
            {
                using var pen = new Pen(Colors.BorderStrong, MathF.Max(1f, S(1f)));
                g.DrawPath(pen, path);
            }
        }

        Color textColor = accent ? Colors.AccentText : Colors.Text;
        g.DrawString(text, font, new SolidBrush(textColor), rect.Left + S(11f), rect.Top + (rect.Height - _fonts.Metrics(font).LineSpacing) / 2f);
        return rect.Right;
    }

    private IEnumerable<string> Wrap(string text, Font font, float width)
    {
        foreach (string paragraph in text.Split('\n'))
        {
            string current = string.Empty;
            foreach (string word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = current.Length == 0 ? word : current + " " + word;
                if (_fonts.Measure(candidate, font) <= width || current.Length == 0)
                {
                    current = candidate;
                    continue;
                }

                yield return current;
                current = word;
            }

            yield return current;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        string? hover = null;
        foreach ((RectangleF bounds, string action) in _hits)
        {
            if (bounds.Contains(e.Location))
            {
                hover = action;
                break;
            }
        }

        Cursor = hover is null ? Cursors.Default : Cursors.Hand;
        if (hover != _hover)
        {
            _hover = hover;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hover = null;
        Cursor = Cursors.Default;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        foreach ((RectangleF bounds, string action) in _hits)
        {
            if (!bounds.Contains(e.Location))
            {
                continue;
            }

            switch (action)
            {
                case "ok":
                    Close();
                    return;
                case "repo":
                    Open(RepoUrl);
                    return;
                case "license":
                    Open(LicenseUrl);
                    return;
                case "docs":
                    Open(DocsUrl);
                    return;
                case "site":
                    Open(AuthorSite);
                    return;
                case "diag":
                    try
                    {
                        Clipboard.SetText(Diagnostics.Text());
                    }
                    catch
                    {
                        // clipboard busy
                    }

                    return;
            }
        }
    }

    private static void Open(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // shell refused
        }
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData is Keys.Escape or Keys.Enter)
        {
            Close();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fonts.Dispose();
        }

        base.Dispose(disposing);
    }
}

/// <summary>Environment snapshot used by the About button and `--diagnostics`.</summary>
internal static class Diagnostics
{
    public static string Text()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("MD Viewer " + (typeof(Diagnostics).Assembly.GetName().Version?.ToString(3) ?? "1.1.0"));
        sb.AppendLine(RuntimeInformation.FrameworkDescription);
        sb.AppendLine(RuntimeInformation.OSDescription + " (" + RuntimeInformation.OSArchitecture + ")");
        sb.AppendLine("64-bit process: " + Environment.Is64BitProcess);
        sb.AppendLine("System dark: " + ThemeCatalog.SystemPrefersDark());

        using var fonts = new FontBook(1f);
        sb.AppendLine("UI font     : " + fonts.UiFamily);
        sb.AppendLine("Display font: " + fonts.DisplayFamily);
        sb.AppendLine("Mono font   : " + fonts.MonoFamily);
        sb.AppendLine("Serif font  : " + fonts.SerifFamily);
        sb.AppendLine("Settings    : " + AppSettings.SettingsDirectory);
        sb.AppendLine("Emoji codes : " + MdViewer.Core.Markdown.Emoji.Count);
        return sb.ToString();
    }
}

