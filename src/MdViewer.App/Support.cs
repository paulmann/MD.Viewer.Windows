using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Windows.Forms;
using MdViewer.Core.Markdown;
using MdViewer.Render;
using MdViewer.Render.Layout;
using MdViewer.Render.Theming;

namespace MdViewer.App;

/// <summary>Themed modal dialog for the shortcut list and the About box.</summary>
internal sealed class InfoDialog : Form
{
    public InfoDialog(string title, string body, Theme theme)
    {
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(560, 340);
        BackColor = theme.Colors.Surface;
        ForeColor = theme.Colors.Text;

        var text = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = theme.Colors.Surface,
            ForeColor = theme.Colors.Text,
            Font = new Font("Segoe UI", 10.5f),
            Text = body.Replace("\n", Environment.NewLine),
            Bounds = new Rectangle(22, 20, ClientSize.Width - 44, ClientSize.Height - 84),
            ScrollBars = ScrollBars.Vertical,
        };

        var ok = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Bounds = new Rectangle(ClientSize.Width - 108, ClientSize.Height - 48, 86, 30),
            FlatStyle = FlatStyle.System,
        };

        Controls.Add(text);
        Controls.Add(ok);
        AcceptButton = ok;
        CancelButton = ok;
    }
}

/// <summary>Paginates the rendered document for <see cref="PrintDocument"/>.</summary>
public sealed class DocumentPrinter
{
    private readonly MarkdownRenderer _renderer;
    private int _page;

    public DocumentPrinter(MarkdownDocument document, Theme theme)
    {
        // 100 units per inch (GraphicsUnit.Display) — scale DIPs the same way.
        _renderer = new MarkdownRenderer(ThemeCatalog.Light.WithScale(1f), 100f / 96f);
        _renderer.SetDocument(document);
        _ = theme;
    }

    public void PrintPage(object? sender, PrintPageEventArgs e)
    {
        Graphics g = e.Graphics!;
        Rectangle margin = e.MarginBounds;
        float contentWidth = margin.Width;

        DocumentLayout? layout = _renderer.EnsureLayout(contentWidth);
        if (layout is null)
        {
            e.HasMorePages = false;
            return;
        }

        g.SmoothingMode = SmoothingMode.AntiAlias;

        GraphicsState state = g.Save();
        g.TranslateTransform(margin.Left, margin.Top);
        var pageClip = new RectangleF(0, _page * margin.Height, contentWidth, margin.Height);
        g.SetClip(pageClip);
        _renderer.PaintContent(g, _page * margin.Height);
        g.Restore(state);

        _page++;
        e.HasMorePages = _page * margin.Height < layout.Height;
    }
}





