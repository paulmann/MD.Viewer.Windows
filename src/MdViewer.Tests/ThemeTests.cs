using System.Drawing;
using MdViewer.Render.Theming;

namespace MdViewer.Tests;

/// <summary>Design-system guards: contrast ratios of every documented colour pair.</summary>
public static class ThemeTests
{
    private static double Luminance(Color color)
    {
        static double Channel(int value)
        {
            double c = value / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(color.R) + 0.7152 * Channel(color.G) + 0.0722 * Channel(color.B);
    }

    private static double Contrast(Color foreground, Color background)
    {
        double a = Luminance(foreground);
        double b = Luminance(background);
        double lighter = Math.Max(a, b);
        double darker = Math.Min(a, b);
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static string _themeName = "?";

    private static void CheckPair(Palette palette, Color foreground, Color background, double minimum, string what)
    {
        double ratio = Contrast(foreground, background);
        Harness.True(ratio >= minimum, $"{what}: contrast {ratio:0.00} < {minimum} ({_themeName})");
    }

    public static void Run()
    {
        Harness.Suite("theme");

        foreach (Theme theme in new[] { ThemeCatalog.Light, ThemeCatalog.Dark })
        {
            Palette palette = theme.Colors;
            string name = theme.Id;
            _themeName = name;

            Harness.Test($"{name}: body text is readable", () =>
                CheckPair(palette, palette.Text, palette.Surface, 7.0, "body"));

            Harness.Test($"{name}: muted text passes AA", () =>
                CheckPair(palette, palette.TextMuted, palette.Surface, 4.5, "muted"));

            Harness.Test($"{name}: faint labels are at least 4.0", () =>
                CheckPair(palette, palette.TextFaint, palette.Surface, 4.0, "faint"));

            Harness.Test($"{name}: links pass AA", () =>
                CheckPair(palette, palette.Link, palette.Surface, 4.5, "link"));

            Harness.Test($"{name}: code text on code background", () =>
                CheckPair(palette, palette.CodeText, palette.CodeBg, 7.0, "code"));

            Harness.Test($"{name}: inline code is distinguishable", () =>
                CheckPair(palette, palette.InlineCodeText, palette.InlineCodeBg, 4.5, "inline code"));

            Harness.Test($"{name}: syntax tokens pass AA", () =>
            {
                CheckPair(palette, palette.Keyword, palette.CodeBg, 4.5, "keyword");
                CheckPair(palette, palette.String, palette.CodeBg, 4.5, "string");
                CheckPair(palette, palette.Number, palette.CodeBg, 4.5, "number");
                CheckPair(palette, palette.TypeName, palette.CodeBg, 4.5, "type");
                CheckPair(palette, palette.Function, palette.CodeBg, 4.5, "function");
            });

            Harness.Test($"{name}: alert titles pass AA on their tints", () =>
            {
                CheckPair(palette, palette.NoteBar, palette.NoteBg, 4.5, "note");
                CheckPair(palette, palette.TipBar, palette.TipBg, 4.5, "tip");
                CheckPair(palette, palette.WarningBar, palette.WarningBg, 4.5, "warning");
                CheckPair(palette, palette.CautionBar, palette.CautionBg, 4.5, "caution");
            });

            Harness.Test($"{name}: table header text on header fill", () =>
                CheckPair(palette, palette.Text, palette.TableHeaderBg, 7.0, "table header"));

            Harness.Test($"{name}: sheet stands out from the canvas", () =>
            {
                double ratio = Contrast(palette.Surface, palette.Canvas);
                Harness.True(ratio > 1.05 || theme.IsDark, $"{name}: sheet vs canvas contrast {ratio:0.00}");
            });
        }

        Harness.Test("light and dark palettes are not accidental copies", () =>
        {
            Harness.True(ThemeCatalog.Light.Colors.Surface != ThemeCatalog.Dark.Colors.Surface, "surfaces differ");
            Harness.True(ThemeCatalog.Light.Colors.Text != ThemeCatalog.Dark.Colors.Text, "text differs");
        });

        Harness.Test("system preference resolves to one of the two themes", () =>
        {
            Theme resolved = ThemeCatalog.Resolve("auto");
            Harness.True(resolved.Id is "light" or "dark", "auto resolves");
        });

        Harness.Test("system preference matches the registry", () =>
        {
            bool expected = false;
            try
            {
                using Microsoft.Win32.RegistryKey? key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                object? apps = key?.GetValue("AppsUseLightTheme");
                object? system = key?.GetValue("SystemUsesLightTheme");
                expected = apps is int a ? a == 0 : system is int s && s == 0;
            }
            catch
            {
                // registry unavailable: the resolver must simply answer "light"
            }

            Harness.Equal(expected, ThemeCatalog.SystemPrefersDark(), "detection reads AppsUseLightTheme");
        });

        Harness.Test("explicit theme ignores the system setting", () =>
        {
            Harness.Equal("light", ThemeCatalog.Resolve("light").Id, "light");
            Harness.Equal("dark", ThemeCatalog.Resolve("dark").Id, "dark");
        });

        Harness.Test("zoom scales every size", () =>
        {
            Theme zoomed = ThemeCatalog.Light.WithScale(1.5f);
            Harness.Equal(1.5f, zoomed.Scale, "scale");
            Harness.True(zoomed.S(16f) > ThemeCatalog.Light.S(16f), "scaled size grows");
        });
    }
}

