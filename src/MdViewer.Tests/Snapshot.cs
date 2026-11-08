using System.Drawing;
using System.Drawing.Imaging;
using MdViewer.Core.Markdown;
using MdViewer.Render;
using MdViewer.Render.Theming;

namespace MdViewer.Tests;

/// <summary>
/// Headless rendering used by the development loop and by the screenshot suite:
/// the same renderer the window uses, drawn into a bitmap.
/// </summary>
public static class Snapshot
{
    public static int Run(string[] args)
    {
        string? input = null;
        string? output = null;
        string themeId = "light";
        float width = 900f;
        float zoom = 1f;
        int height = 0;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--snapshot":
                    input = args[++i];
                    output = args[++i];
                    break;
                case "--theme":
                    themeId = args[++i];
                    break;
                case "--width":
                    width = float.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "--zoom":
                    zoom = float.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "--height":
                    height = int.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture);
                    break;
            }
        }

        if (input is null || output is null)
        {
            Console.Error.WriteLine("usage: mdview-tests --snapshot <file.md> <out.png> [--theme light|dark] [--width 900] [--zoom 1] [--height N]");
            return 2;
        }

        string text = File.ReadAllText(input);
        var document = new MarkdownParser().Parse(text, MarkdownOptions.Default, Path.GetFullPath(input));

        Theme theme = ThemeCatalog.Resolve(themeId).WithScale(zoom);
        using var renderer = new MarkdownRenderer(theme, 1f);
        renderer.SetDocument(document);

        using Bitmap bitmap = renderer.RenderToBitmap(width, height);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        bitmap.Save(output, ImageFormat.Png);

        Console.WriteLine($"{output}: {bitmap.Width}x{bitmap.Height}px, blocks={renderer.Layout?.Blocks.Count}, headings={document.Outline.Count}");
        return 0;
    }
}
