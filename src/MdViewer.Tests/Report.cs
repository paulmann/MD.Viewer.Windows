using System.Drawing;
using MdViewer.Core.Markdown;
using MdViewer.Render;
using MdViewer.Render.Layout;
using MdViewer.Render.Text;
using MdViewer.Render.Theming;

namespace MdViewer.Tests;

public static class Report
{
    public static int Run(string[] args)
    {
        string? input = null; float width = 900f; string theme = "light"; bool deep = false;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--report") input = args[++i];
            else if (args[i] == "--width") width = float.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture);
            else if (args[i] == "--theme") theme = args[++i];
            else if (args[i] == "--deep") deep = true;
        }
        if (input is null) { Console.Error.WriteLine("usage: --report file.md [--deep]"); return 2; }

        var doc = new MarkdownParser().Parse(File.ReadAllText(input), MarkdownOptions.Default, Path.GetFullPath(input));
        using var renderer = new MarkdownRenderer(ThemeCatalog.Resolve(theme), 1f);
        renderer.SetDocument(doc);
        var layout = renderer.EnsureLayout(width)!;

        Console.WriteLine($"doc: {doc.FileName}  blocks={layout.Blocks.Count}  height={layout.Height:0.0}  width={layout.Width:0.0}");
        float previousBottom = -1f;
        foreach (var b in layout.Blocks)
        {
            string name = b.Source.GetType().Name.Replace("Block", "");
            string flag = b.Top < previousBottom - 0.5f ? "  <-- OVERLAP" : "";
            if (b.Height <= 0) flag += "  <-- ZERO HEIGHT";
            Console.WriteLine($"{"",2}{name,-20} {b.Top,8:0.0} {b.Height,8:0.0} {b.Width,8:0.0}{flag}");
            if (deep) Dump(b, 4);
            previousBottom = b.Bottom;
        }
        Console.WriteLine($"anchors={layout.Anchors.Count}  links={layout.Links.Count}");
        return 0;
    }

    private static void Dump(BlockLayout block, int indent)
    {
        string pad = new string(' ', indent);
        Console.WriteLine($"{pad}{block.Source.GetType().Name} h={block.Height:0} top={block.Top:0}");
        switch (block)
        {
            case TextVisual t:
                Console.WriteLine($"{pad}text lines={t.Lines.Count} widths={string.Join(",", t.Lines.Select(l => l.Width.ToString("0")))}");
                break;
            case ListVisual l:
                foreach (var item in l.Items)
                {
                    Console.WriteLine($"{pad}item marker={item.Marker.Top:0}/{item.Marker.Height:0} task={item.Task} n={item.Number}");
                    foreach (var child in item.Children) Dump(child, indent + 2);
                }
                break;
            case QuoteVisual q:
                foreach (var child in q.Children) Dump(child, indent + 2);
                break;
            case AlertVisual a:
                foreach (var child in a.Children) Dump(child, indent + 2);
                break;
            case TableVisual tb:
                Console.WriteLine($"{pad}cols={string.Join(",", tb.ColumnWidths.Select(w => w.ToString("0")))} rows={string.Join(",", tb.RowHeights.Select(w => w.ToString("0")))} header={tb.HeaderHeight:0}");
                break;
            case CodeVisual c:
                Console.WriteLine($"{pad}code lines={c.Lines.Count} height={c.Height:0}");
                break;
        }
    }
}

