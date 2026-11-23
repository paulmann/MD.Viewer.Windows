using System.Drawing;
using MdViewer.Core.Markdown;
using MdViewer.Render;
using MdViewer.Render.Layout;
using MdViewer.Render.Text;
using MdViewer.Render.Theming;

namespace MdViewer.Tests;

public static class RenderTests
{
    private static MarkdownDocument Doc(string markdown) => new MarkdownParser().Parse(markdown);

    private static (MarkdownRenderer Renderer, DocumentLayout Layout) LayoutDocument(string markdown, float width = 900f, Theme? theme = null)
    {
        var renderer = new MarkdownRenderer(theme ?? ThemeCatalog.Light, 1f);
        renderer.SetDocument(Doc(markdown));
        DocumentLayout layout = renderer.EnsureLayout(width)!;
        return (renderer, layout);
    }

    public static void Run()
    {
        Harness.Suite("layout");

        Harness.Test("document lays out with positive height", () =>
        {
            var (renderer, layout) = LayoutDocument("# Title\n\nBody text here.\n\n## Section\n\nMore.");
            using (renderer)
            {
                Harness.True(layout.Blocks.Count >= 3, "blocks");
                Harness.True(layout.Height > 40, $"height={layout.Height}");
            }
        });

        Harness.Test("every heading registers an anchor", () =>
        {
            string md = "# One\n\n## Two\n\n### Three\n";
            var (renderer, layout) = LayoutDocument(md);
            using (renderer)
            {
                foreach (HeadingInfo heading in renderer.Document!.Outline)
                {
                    Harness.True(layout.Anchors.ContainsKey(heading.Id), $"anchor {heading.Id}");
                }
            }
        });

        Harness.Test("paragraph wraps inside the column", () =>
        {
            string md = string.Join(' ', Enumerable.Repeat("word", 400));
            var (renderer, layout) = LayoutDocument(md, 420f);
            using (renderer)
            {
                var paragraph = (TextVisual)layout.Blocks[0];
                Harness.True(paragraph.Lines.Count > 5, $"lines={paragraph.Lines.Count}");
                foreach (ShapedLine line in paragraph.Lines)
                {
                    Harness.True(line.Width <= 420f + 1f, $"line width {line.Width}");
                }
            }
        });

        Harness.Test("code block grows with lines", () =>
        {
            var (r1, l1) = LayoutDocument("```\na\n```");
            var (r2, l2) = LayoutDocument("```\na\nb\nc\nd\n```");
            using (r1)
            using (r2)
            {
                Harness.True(l2.Blocks[0].Height > l1.Blocks[0].Height, "taller");
            }
        });

        Harness.Test("table columns fill the width", () =>
        {
            var (renderer, layout) = LayoutDocument("| a | b | c |\n|---|---|---|\n| 1 | 2 | 3 |", 700f);
            using (renderer)
            {
                var table = (TableVisual)layout.Blocks[0];
                float sum = table.ColumnWidths.Sum();
                Harness.True(MathF.Abs(sum - 700f) < 2f, $"sum={sum}");
            }
        });

        Harness.Test("a wide table stays inside the page", () =>
        {
            // The owner's report: prose columns pushed the table past the sheet edge and
            // the border landed inside a column.
            string md = "| Роль | new.batterywizard.ru | bwl.ru | v4.bwl.ru |\n" +
                        "|---|---|---|---|\n" +
                        "| Стек | PHP 8.5 + Perl CGI, MySQL wizard_online на этом же домене уже отдаются `/bw2/` | PHP (динамический кабинет) + Python 3.14, read-only `wizard_online` | PHP 8.5 MVC без фреймворка, `public/BL` bwov1 |\n" +
                        "| Данные | первоисточник 28 333 испытания, 122 млн измерений по ячейкам | читает BWO на чтение; пишет только `Testers` | своя копия архива в bwov_v4 |\n" +
                        "| Состояние | прод ежедневная работа | прод коммерческая оболочка | alpha: 9 разделов, 31 PASS / 2 FAIL |\n";

            foreach (float width in new[] { 520f, 760f, 1100f })
            {
                var (renderer, layout) = LayoutDocument(md, width);
                using (renderer)
                {
                    var table = (TableVisual)layout.Blocks[0];
                    Harness.True(table.Width <= width + 0.5f, $"table {table.Width} of {width}");
                    Harness.True(MathF.Abs(table.ColumnWidths.Sum() - table.Width) < 1f, "border follows the columns");
                    foreach (float column in table.ColumnWidths)
                    {
                        Harness.True(column >= 39f, $"column floor {column}");
                    }

                    foreach (TableCellVisual cell in table.Headers.Concat(table.Rows.SelectMany(r => r)))
                    {
                        foreach (ShapedLine line in cell.Text.Lines)
                        {
                            Harness.True(line.Width <= table.ColumnWidths.Max() + 40f, $"cell line {line.Width}");
                        }
                    }
                }
            }
        });

        Harness.Test("a long url inside a cell is broken, not spilled", () =>
        {
            string md = "| Параметр | Значение |\n|---|---|\n| Путь | `https://example.com/docs/architecture/very/long/path/that/should/be/broken/inside/the/cell.md` |\n";
            var (renderer, layout) = LayoutDocument(md, 640f);
            using (renderer)
            {
                var table = (TableVisual)layout.Blocks[0];
                Harness.True(table.Width <= 640.5f, $"width {table.Width}");
                Harness.True(table.Height > 0f, "laid out");
            }
        });

        Harness.Test("links are collected as regions", () =>
        {
            var (renderer, layout) = LayoutDocument("See [the site](https://example.com) and [local](other.md).");
            using (renderer)
            {
                Harness.Equal(2, layout.Links.Count, "regions");
                Harness.True(layout.Links.Any(l => l.IsExternal), "external");
                Harness.True(layout.Links.Any(l => !l.IsExternal), "internal");
            }
        });

        Harness.Test("anchor hit test finds the nearest heading", () =>
        {
            var (renderer, layout) = LayoutDocument("# One\n\nsome text\n\n## Two\n\nmore text\n");
            using (renderer)
            {
                BlockLayout second = layout.Anchors[renderer.Document!.Outline[1].Id];
                string? anchor = layout.AnchorAt(second.Top + 4);
                Harness.Equal(renderer.Document.Outline[1].Id, anchor ?? "", "nearest anchor");
            }
        });

        Harness.Test("dark theme paints without error", () =>
        {
            var (renderer, layout) = LayoutDocument("# Title\n\nText `code` and **bold**.\n\n```ts\nconst x = 1;\n```", theme: ThemeCatalog.Dark);
            using (renderer)
            {
                using Bitmap bitmap = renderer.RenderToBitmap(600f, 400);
                Harness.True(bitmap.Width == 600, "width");
                var pixel = bitmap.GetPixel(10, 10);
                Harness.True(pixel.R > 5 || pixel.G > 5 || pixel.B > 5, "not transparent");
            }
        });

        Harness.Test("zoom changes text height", () =>
        {
            var normal = new MarkdownRenderer(ThemeCatalog.Light.WithScale(1f), 1f);
            normal.SetDocument(Doc(string.Join(' ', Enumerable.Repeat("word", 60))));
            float h1 = normal.EnsureLayout(400f)!.Height;

            var zoomed = new MarkdownRenderer(ThemeCatalog.Light.WithScale(1.5f), 1f);
            zoomed.SetDocument(Doc(string.Join(' ', Enumerable.Repeat("word", 60))));
            float h2 = zoomed.EnsureLayout(400f)!.Height;

            using (normal)
            using (zoomed)
            {
                Harness.True(h2 > h1 * 1.2f, $"zoomed {h2} vs {h1}");
            }
        });

        Harness.Test("empty document still lays out", () =>
        {
            var (renderer, layout) = LayoutDocument("");
            using (renderer)
            {
                Harness.Equal(0, layout.Blocks.Count, "blocks");
                Harness.True(layout.Height > 0, "height");
            }
        });

        Harness.Test("pathological input does not throw", () =>
        {
            string md = "***\n\n[[[[\n\n```\nunclosed fence\n\n> quote\n- list\n\n| a |\n|---|\n\n$$x";
            var (renderer, layout) = LayoutDocument(md);
            using (renderer)
            {
                Harness.True(layout.Height > 0, "height");
            }
        });
    }
}

