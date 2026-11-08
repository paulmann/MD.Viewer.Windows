using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Windows.Forms;
using MdViewer.Core.Markdown;
using MdViewer.Render;
using MdViewer.Render.Layout;
using MdViewer.Render.Theming;

namespace MdViewer.App;

/// <summary>Window icon drawn at runtime (no binary assets in the repository).</summary>
internal static partial class AppIcon
{
    public static Icon Create()
    {
        using var bitmap = new Bitmap(64, 64);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            var sheet = new RectangleF(10, 6, 44, 52);
            using (var path = MdViewer.Render.Rendering.PaintContext.RoundedPath(sheet, 8f))
            using (var brush = new LinearGradientBrush(sheet, Color.FromArgb(0x2F, 0x6F, 0xEB), Color.FromArgb(0x1B, 0x4C, 0xB8), 60f))
            {
                g.FillPath(brush, path);
            }

            using (var white = new SolidBrush(Color.White))
            {
                g.FillRectangle(white, 18, 16, 28, 4);
                g.FillRectangle(white, 18, 25, 20, 4);
                g.FillRectangle(white, 18, 34, 24, 4);
            }

            using (var accent = new SolidBrush(Color.FromArgb(0xFF, 0xD1, 0x66)))
            {
                g.FillRectangle(accent, 18, 44, 14, 5);
            }

            using var pen = new Pen(Color.FromArgb(40, 0, 0, 0), 1.5f);
            g.DrawLine(pen, 46, 12, 46, 44);
        }

        IntPtr handle = bitmap.GetHicon();
        try
        {
            using Icon temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [System.Runtime.InteropServices.LibraryImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static partial bool DestroyIcon(IntPtr handle);
}

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




