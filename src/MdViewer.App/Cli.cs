using System.Globalization;

namespace MdViewer.App;

public enum ViewMode
{
    Rendered,
    Source,
    Editor,
}

public enum ThemeMode
{
    Auto,
    Light,
    Dark,
}

/// <summary>Everything the command line can say, parsed once at start-up.</summary>
public sealed class LaunchOptions
{
    public string? FilePath { get; set; }

    public List<string> ExtraFiles { get; } = [];

    public ViewMode? Mode { get; set; }

    public ThemeMode? Theme { get; set; }

    public float? Zoom { get; set; }

    public float? ReadingWidth { get; set; }

    public bool? ToggleOutline { get; set; }

    public bool? LineNumbers { get; set; }

    public bool? WrapCode { get; set; }

    public bool? Serif { get; set; }

    public bool? Numbering { get; set; }

    public int? Line { get; set; }

    public string? Anchor { get; set; }

    public bool Maximized { get; set; }

    public bool FullScreen { get; set; }

    public bool Print { get; set; }

    public bool Watch { get; set; }

    public string? ExportFormat { get; set; }

    public string? ExportPath { get; set; }

    public bool ReadOnlySource { get; set; }

    public string? Language { get; set; }

    public bool NoSession { get; set; }

    public bool ResetSettings { get; set; }

    public bool Help { get; set; }

    public bool Version { get; set; }

    public bool Diagnostics { get; set; }

    /// <summary>Path for a headless window screenshot (development / CI).</summary>
    public string? ScreenshotPath { get; set; }

    /// <summary>Print the live control tree and geometry (development).</summary>
    public bool UiDump { get; set; }

    public int ScreenshotDelay { get; set; } = 700;

    public List<string> Errors { get; } = [];

    public static LaunchOptions Parse(string[] args)
    {
        var options = new LaunchOptions();
        int i = 0;

        while (i < args.Length)
        {
            string arg = args[i];
            string? NextValue()
            {
                if (i + 1 >= args.Length)
                {
                    options.Errors.Add($"missing value for {arg}");
                    return null;
                }

                i++;
                return args[i];
            }

            switch (arg.ToLowerInvariant())
            {
                case "-h" or "--help" or "-?" or "/?":
                    options.Help = true;
                    break;

                case "--version" or "-v":
                    options.Version = true;
                    break;

                case "--mode" or "--view" or "-m":
                {
                    string? value = NextValue();
                    if (value is null)
                    {
                        break;
                    }

                    switch (value.ToLowerInvariant())
                    {
                        case "render" or "rendered" or "view" or "preview":
                            options.Mode = ViewMode.Rendered;
                            break;
                        case "source" or "raw" or "text" or "md":
                            options.Mode = ViewMode.Source;
                            break;
                        case "edit" or "editor" or "write":
                            options.Mode = ViewMode.Editor;
                            break;
                        default:
                            options.Errors.Add($"unknown mode '{value}' (render | source | editor)");
                            break;
                    }
                    break;
                }

                case "--theme" or "-t":
                {
                    string? value = NextValue();
                    if (value is null)
                    {
                        break;
                    }

                    switch (value.ToLowerInvariant())
                    {
                        case "light" or "day" or "светлая":
                            options.Theme = ThemeMode.Light;
                            break;
                        case "dark" or "night" or "тёмная" or "темная":
                            options.Theme = ThemeMode.Dark;
                            break;
                        case "auto" or "system" or "авто":
                            options.Theme = ThemeMode.Auto;
                            break;
                        default:
                            options.Errors.Add($"unknown theme '{value}' (light | dark | auto)");
                            break;
                    }
                    break;
                }

                case "--zoom" or "-z":
                {
                    string? value = NextValue();
                    if (value is null)
                    {
                        break;
                    }

                    options.Zoom = ParseZoom(value, options);
                    break;
                }

                case "--width" or "-w":
                {
                    string? value = NextValue();
                    if (value is null)
                    {
                        break;
                    }

                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float width) && width is >= 380 and <= 2400)
                    {
                        options.ReadingWidth = width;
                    }
                    else
                    {
                        options.Errors.Add($"bad width '{value}' (380..2400)");
                    }

                    break;
                }

                case "--line" or "-l":
                {
                    string? value = NextValue();
                    if (value is not null && int.TryParse(value, out int line) && line > 0)
                    {
                        options.Line = line;
                    }
                    else
                    {
                        options.Errors.Add($"bad line number '{value}'");
                    }

                    break;
                }

                case "--anchor" or "-a":
                    options.Anchor = NextValue();
                    break;

                case "--toc" or "--outline" or "--sidebar":
                    options.ToggleOutline = true;
                    break;

                case "--no-toc" or "--no-outline" or "--no-sidebar":
                    options.ToggleOutline = false;
                    break;

                case "--line-numbers":
                    options.LineNumbers = true;
                    break;

                case "--no-line-numbers":
                    options.LineNumbers = false;
                    break;

                case "--wrap":
                    options.WrapCode = true;
                    break;

                case "--no-wrap":
                    options.WrapCode = false;
                    break;

                case "--serif":
                    options.Serif = true;
                    break;

                case "--sans":
                    options.Serif = false;
                    break;

                case "--numbering":
                    options.Numbering = true;
                    break;

                case "--no-numbering":
                    options.Numbering = false;
                    break;

                case "--maximized" or "--max":
                    options.Maximized = true;
                    break;

                case "--fullscreen":
                    options.FullScreen = true;
                    break;

                case "--print" or "-p":
                    options.Print = true;
                    break;

                case "--watch":
                    options.Watch = true;
                    break;

                case "--no-watch":
                    options.Watch = false;
                    break;

                case "--export" or "-e":
                    options.ExportFormat = NextValue()?.ToLowerInvariant();
                    break;

                case "--out" or "-o":
                    options.ExportPath = NextValue();
                    break;

                case "--read-only":
                    options.ReadOnlySource = true;
                    break;

                case "--lang":
                    options.Language = NextValue();
                    break;

                case "--no-session":
                    options.NoSession = true;
                    break;

                case "--reset-settings":
                    options.ResetSettings = true;
                    break;

                case "--diagnostics" or "--info":
                    options.Diagnostics = true;
                    break;

                case "--ui-dump":
                    options.UiDump = true;
                    break;

                case "--screenshot":
                    options.ScreenshotPath = NextValue();
                    break;

                case "--screenshot-delay":
                    if (int.TryParse(NextValue(), out int delay) && delay >= 0)
                    {
                        options.ScreenshotDelay = delay;
                    }

                    break;

                default:
                    if (arg.StartsWith('-'))
                    {
                        options.Errors.Add($"unknown option '{arg}'");
                    }
                    else if (options.FilePath is null)
                    {
                        options.FilePath = arg;
                    }
                    else
                    {
                        options.ExtraFiles.Add(arg);
                    }

                    break;
            }

            i++;
        }

        return options;
    }

    private static float? ParseZoom(string value, LaunchOptions options)
    {
        string text = value.Trim();
        bool percent = text.EndsWith('%');
        if (percent)
        {
            text = text[..^1];
        }

        if (text.StartsWith('+') || text.StartsWith('-'))
        {
            // relative zoom: handled by the caller relative to the saved value
            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float delta))
            {
                return 1f + delta / (percent ? 100f : 1f);
            }
        }

        if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
        {
            options.Errors.Add($"bad zoom '{value}'");
            return null;
        }

        if (percent || parsed > 10f)
        {
            parsed /= 100f;
        }

        return Math.Clamp(parsed, 0.6f, 3f);
    }

    public static string HelpText()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("MD Viewer — a native Markdown viewer and editor for Windows");
        sb.AppendLine();
        sb.AppendLine("USAGE");
        sb.AppendLine("  mdview [file.md] [options]");
        sb.AppendLine();
        sb.AppendLine("APPEARANCE");
        sb.AppendLine("  --mode <render|source|editor>  start in a specific view mode");
        sb.AppendLine("  --theme <light|dark|auto>      colour theme (auto follows Windows)");
        sb.AppendLine("  --zoom <1.25|125%|+10%>        zoom factor");
        sb.AppendLine("  --width <px>                   reading column width (380..2400)");
        sb.AppendLine("  --serif | --sans               reading font family");
        sb.AppendLine("  --numbering                    number the headings");
        sb.AppendLine("  --toc | --no-toc               show or hide the outline sidebar");
        sb.AppendLine("  --line-numbers                 line numbers in source/editor");
        sb.AppendLine("  --wrap | --no-wrap             wrap long code lines");
        sb.AppendLine("  --maximized | --fullscreen     window state");
        sb.AppendLine();
        sb.AppendLine("NAVIGATION");
        sb.AppendLine("  --line <n>                     jump to line n");
        sb.AppendLine("  --anchor <id>                  jump to a heading anchor");
        sb.AppendLine();
        sb.AppendLine("OUTPUT");
        sb.AppendLine("  --export <html|text|png|md>    export instead of showing a window");
        sb.AppendLine("  --out <file>                   export target (default: next to the source)");
        sb.AppendLine("  --print                        open the print dialog");
        sb.AppendLine();
        sb.AppendLine("OTHER");
        sb.AppendLine("  --watch                        reload the file when it changes on disk");
        sb.AppendLine("  --lang <ru|en>                 interface language");
        sb.AppendLine("  --no-session                   ignore the saved session");
        sb.AppendLine("  --reset-settings               restore defaults and exit");
        sb.AppendLine("  --diagnostics                  print environment information");
        sb.AppendLine("  --version, --help");
        return sb.ToString();
    }
}



