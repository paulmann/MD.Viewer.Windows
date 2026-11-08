using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using MdViewer.App;
using MdViewer.Core.Markdown;
using MdViewer.Render;
using MdViewer.Render.Theming;

internal static partial class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        LaunchOptions options = LaunchOptions.Parse(args);
        L.Use(options.Language is null or "auto" ? null : options.Language);

        if (options.Help || options.Errors.Count > 0)
        {
            using var bridge = new ConsoleBridge();
            foreach (string error in options.Errors)
            {
                Console.Error.WriteLine("error: " + error);
            }

            Console.WriteLine(LaunchOptions.HelpText());
            return options.Errors.Count > 0 ? 2 : 0;
        }

        if (options.Version)
        {
            using var bridge = new ConsoleBridge();
            Console.WriteLine("MD Viewer " + (typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "1.0.0"));
            Console.WriteLine(RuntimeInformation.FrameworkDescription);
            return 0;
        }

        if (options.Diagnostics)
        {
            using var bridge = new ConsoleBridge();
            PrintDiagnostics();
            return 0;
        }

        if (options.ResetSettings)
        {
            AppSettings fresh = AppSettings.Load();
            fresh.Reset();
            fresh.Save();
            using var bridge = new ConsoleBridge();
            Console.WriteLine("settings reset: " + fresh.FilePath);
            return 0;
        }

        if (options.ExportFormat is { } format)
        {
            using var bridge = new ConsoleBridge();
            return ExportHeadless(options, format);
        }

        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => ShowFatal(e.Exception);

        AppSettings settings = ApplyOverrides(AppSettings.Load(), options);

        using var form = new MainForm(settings);

        if (options.FullScreen)
        {
            form.FormBorderStyle = FormBorderStyle.None;
            form.WindowState = FormWindowState.Maximized;
        }
        else if (options.Maximized)
        {
            form.WindowState = FormWindowState.Maximized;
        }

        if (options.ReadingWidth is float width)
        {
            form.SetReadingWidth(width);
        }

        List<string> files = [];
        if (options.FilePath is not null)
        {
            files.Add(options.FilePath);
        }

        files.AddRange(options.ExtraFiles);

        if (!options.NoSession && files.Count == 0 && settings.Session.Count > 0)
        {
            foreach (string path in settings.Session.Take(8))
            {
                if (File.Exists(path))
                {
                    files.Add(path);
                }
            }
        }

        foreach (string file in files)
        {
            form.OpenFile(file, options.Mode, options.Anchor, options.Line);
        }

        if (form.SessionCount == 0)
        {
            form.OpenUntitled(WelcomeText(), "welcome.md");
        }

        if (options.Print)
        {
            form.Shown += (_, _) => form.BeginInvoke(form.PrintCurrentDocument);
        }

        if (options.AboutShot is { Length: > 0 } aboutShot)
        {
            var about = new AboutDialog(ThemeCatalog.Resolve(settings.Theme).WithScale(settings.Zoom));
            var timer = new System.Windows.Forms.Timer { Interval = 600 };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                try
                {
                    WindowCapture.Save(about, aboutShot, clientOnly: true);
                    Console.WriteLine("about: " + aboutShot);
                }
                finally
                {
                    about.Close();
                }
            };
            about.Shown += (_, _) => timer.Start();
            Application.Run(about);
            return 0;
        }

        if (options.UiDump)
        {
            form.Shown += (_, _) =>
            {
                using var bridge = new ConsoleBridge();
                Console.WriteLine(form.DescribeLayout());
                form.Close();
            };
        }

        if (options.ScreenshotPath is { Length: > 0 } screenshot)
        {
            var capture = new System.Windows.Forms.Timer { Interval = Math.Max(100, options.ScreenshotDelay) };
            capture.Tick += (_, _) =>
            {
                capture.Stop();
                using var bridge = new ConsoleBridge();
                try
                {
                    WindowCapture.Save(form, screenshot, clientOnly: true);
                    Console.WriteLine("screenshot: " + screenshot);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("screenshot failed: " + ex.Message);
                }
                finally
                {
                    form.Close();
                }
            };
            form.Shown += (_, _) => capture.Start();
        }

        Application.Run(form);
        return 0;
    }

    /// <summary>Command-line switches win over the saved preferences.</summary>
    private static AppSettings ApplyOverrides(AppSettings settings, LaunchOptions options)
    {
        if (options.Theme is ThemeMode theme)
        {
            settings.Theme = theme.ToString().ToLowerInvariant();
        }

        if (options.Mode is ViewMode mode)
        {
            settings.Mode = mode.ToString().ToLowerInvariant();
        }

        if (options.Zoom is float zoom)
        {
            settings.Zoom = zoom;
        }

        if (options.ReadingWidth is float readingWidth)
        {
            settings.ReadingWidth = readingWidth;
        }

        if (options.Serif is bool serif)
        {
            settings.Serif = serif;
        }

        if (options.LineNumbers is bool lineNumbers)
        {
            settings.LineNumbers = lineNumbers;
        }

        if (options.WrapCode is bool wrap)
        {
            settings.WrapCode = wrap;
        }

        if (options.Numbering is bool numbering)
        {
            settings.Numbering = numbering;
        }

        if (options.ToggleOutline is bool outline)
        {
            settings.Outline = outline;
        }

        if (options.Watch)
        {
            settings.Watch = true;
        }

        if (options.NoSession)
        {
            settings.Session.Clear();
        }

        return settings;
    }

    private static void ShowFatal(Exception exception)
    {
        try
        {
            MessageBox.Show(
                exception.ToString(),
                "MD Viewer — unexpected error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch
        {
            // nothing else we can do
        }
    }

    private static void PrintDiagnostics() => Console.WriteLine(Diagnostics.Text());

    private static int ExportHeadless(LaunchOptions options, string format)
    {
        if (options.FilePath is null)
        {
            Console.Error.WriteLine("error: --export needs a source file");
            return 2;
        }

        string source = Path.GetFullPath(options.FilePath);
        if (!File.Exists(source))
        {
            Console.Error.WriteLine($"error: file not found: {source}");
            return 2;
        }

        string text = File.ReadAllText(source);
        MarkdownDocument document = new MarkdownParser().Parse(text, MarkdownOptions.Default, source);

        string output = options.ExportPath ?? Path.ChangeExtension(source, format switch
        {
            "html" => ".html",
            "png" => ".png",
            "text" or "txt" => ".txt",
            _ => ".md",
        });

        Theme theme = ThemeCatalog.Resolve(options.Theme?.ToString().ToLowerInvariant() ?? "light").WithScale(options.Zoom ?? 1f);
        switch (format)
        {
            case "html":
                File.WriteAllText(output, HtmlExporter.ToHtml(document));
                break;

            case "text" or "txt" or "plain":
                File.WriteAllText(output, PlainTextExporter.ToText(document));
                break;

            case "png":
            {
                using var renderer = new MarkdownRenderer(theme, 1f);
                renderer.SetDocument(document);
                float width = (options.ReadingWidth is float w ? w : 720f) + 2f * 56f;
                using Bitmap bitmap = renderer.RenderToBitmap(width);
                bitmap.Save(output, ImageFormat.Png);
                break;
            }

            case "md" or "markdown":
                File.WriteAllText(output, document.Source.Text);
                break;

            default:
                Console.Error.WriteLine($"error: unknown export format '{format}' (html | text | png | md)");
                return 2;
        }

        Console.WriteLine($"{format}: {output} ({new FileInfo(output).Length} bytes)");
        return 0;
    }

    private static string WelcomeText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# " + L.T("app.title"));
        sb.AppendLine();
        sb.AppendLine(L.T("empty.hint"));
        sb.AppendLine();
        sb.AppendLine("## " + L.T("menu.shortcuts"));
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine(L.T("shortcuts.text"));
        sb.AppendLine("```");
        return sb.ToString();
    }
}

/// <summary>Attaches to the parent console so a GUI app can still print CLI output.</summary>
internal sealed partial class ConsoleBridge : IDisposable
{
    private const int AttachParentProcess = -1;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AttachConsole(int processId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool FreeConsole();

    private readonly bool _attached;

    public ConsoleBridge()
    {
        _attached = AttachConsole(AttachParentProcess);
    }

    public void Dispose()
    {
        if (_attached)
        {
            FreeConsole();
        }
    }
}


