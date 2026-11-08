using System.Drawing;
using System.Drawing.Printing;
using MdViewer.App;
using MdViewer.Core.Markdown;
using MdViewer.Render.Theming;

namespace MdViewer.Tests;

public static class PrintTests
{
    public static void Run()
    {
        Harness.Suite("print");

        Harness.Test("paginates a long document into several pages", () =>
        {
            string markdown = string.Join("\n\n", Enumerable.Range(0, 120).Select(i => $"## Section {i}\n\nParagraph {i} with some text to fill the line and wrap a little."));
            MarkdownDocument document = new MarkdownParser().Parse(markdown);
            var printer = new DocumentPrinter(document, ThemeCatalog.Light);

            using var document_ = new PrintDocument();
            using var bitmap = new Bitmap(827, 1169);
            using Graphics graphics = Graphics.FromImage(bitmap);
            var bounds = new Rectangle(0, 0, 827, 1169);

            int pages = 0;
            bool more = true;
            while (more && pages < 400)
            {
                var args = new PrintPageEventArgs(graphics, bounds, bounds, new PageSettings());
                printer.PrintPage(null, args);
                more = args.HasMorePages;
                pages++;
            }

            Harness.True(pages >= 2, $"pages={pages}");
            Harness.True(pages < 400, "pagination terminates");
        });

        Harness.Test("short document fits on one page", () =>
        {
            MarkdownDocument document = new MarkdownParser().Parse("# Title\n\nOne line.");
            var printer = new DocumentPrinter(document, ThemeCatalog.Light);
            using var bitmap = new Bitmap(827, 1169);
            using Graphics graphics = Graphics.FromImage(bitmap);
            var bounds = new Rectangle(0, 0, 827, 1169);
            var args = new PrintPageEventArgs(graphics, bounds, bounds, new PageSettings());
            printer.PrintPage(null, args);
            Harness.False(args.HasMorePages, "single page");
        });
    }
}
