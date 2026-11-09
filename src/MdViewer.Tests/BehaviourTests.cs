using MdViewer.App;
using MdViewer.Render.Theming;

namespace MdViewer.Tests;

/// <summary>Start-up defaults and the theme gesture, checked on the real form.</summary>
public static class BehaviourTests
{
    private static MainForm CreateForm(Action<AppSettings> configure, LaunchOptions? options = null)
    {
        var settings = new AppSettings
        {
            Theme = "auto",
            Mode = "rendered",
            Zoom = 1f,
            ReadingWidth = 720f,
            Outline = true,
        };

        configure(settings);
        AppSettings.DirectoryOverride = Path.Combine(Path.GetTempPath(), "mdview-behaviour-" + Guid.NewGuid().ToString("N"));
        return new MainForm(settings, options);
    }

    public static void Run()
    {
        Harness.Suite("start-up defaults");

        Harness.Test("a file opens in Rendered even if the last mode was Editor", () =>
        {
            using MainForm form = CreateForm(s => s.Mode = "editor");
            Harness.Equal(ViewMode.Rendered, form.CurrentMode, "mode");
        });

        Harness.Test("Remember the last view mode restores Editor", () =>
        {
            using MainForm form = CreateForm(s =>
            {
                s.Mode = "editor";
                s.RememberMode = true;
            });
            Harness.Equal(ViewMode.Editor, form.CurrentMode, "mode");
        });

        Harness.Test("--mode wins over both", () =>
        {
            using MainForm form = CreateForm(s => s.Mode = "editor", new LaunchOptions { Mode = ViewMode.Source });
            Harness.Equal(ViewMode.Source, form.CurrentMode, "mode");
        });

        Harness.Suite("theme gesture");

        Harness.Test("one click turns a dark window light", () =>
        {
            using MainForm form = CreateForm(s => s.Theme = "dark");
            Harness.Equal(ThemeMode.Dark, form.CurrentThemeMode, "starts dark");

            form.ToggleTheme();
            Harness.Equal(ThemeMode.Light, form.CurrentThemeMode, "one click");
            Harness.False(form.Theme.IsDark, "painted light");
        });

        Harness.Test("one click turns a light window dark", () =>
        {
            using MainForm form = CreateForm(s => s.Theme = "light");
            form.ToggleTheme();
            Harness.Equal(ThemeMode.Dark, form.CurrentThemeMode, "one click");
            Harness.True(form.Theme.IsDark, "painted dark");
        });

        Harness.Test("from Auto the first click always changes the appearance", () =>
        {
            using MainForm form = CreateForm(s => s.Theme = "auto");
            bool before = form.Theme.IsDark;
            form.ToggleTheme();
            Harness.True(form.Theme.IsDark != before, "appearance flipped on the first click");
            Harness.Equal(before ? ThemeMode.Light : ThemeMode.Dark, form.CurrentThemeMode, "explicit mode");
        });

        Harness.Test("clicking twice returns to the original appearance", () =>
        {
            using MainForm form = CreateForm(s => s.Theme = "auto");
            bool before = form.Theme.IsDark;
            form.ToggleTheme();
            form.ToggleTheme();
            Harness.Equal(before, form.Theme.IsDark, "back to the start");
        });
    }
}
