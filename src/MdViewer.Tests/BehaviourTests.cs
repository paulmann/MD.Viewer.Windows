using System.Drawing;
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

        Harness.Suite("view width");

        Harness.Test("100 % of the pane is wider than 75 % and 50 %", () =>
        {
            using MainForm form = CreateForm(s => s.WidthMode = "full");
            form.ClientSize = new Size(1600, 900);
            float full = form.MeasuredWidth;

            form.CycleWidth();
            Harness.Equal("75", form.CurrentWidthMode, "75 after one click");
            float threeQuarters = form.MeasuredWidth;

            form.CycleWidth();
            Harness.Equal("50", form.CurrentWidthMode, "50 after two clicks");
            float half = form.MeasuredWidth;

            form.CycleWidth();
            Harness.Equal("full", form.CurrentWidthMode, "back to 100");

            Harness.True(full > threeQuarters && threeQuarters > half, $"{full} > {threeQuarters} > {half}");
            Harness.True(full > 1100f, $"a wide window is used: {full}");
            Harness.True(half < full * 0.55f, $"half is half: {half} of {full}");
        });

        Harness.Test("the width choice is persisted", () =>
        {
            var settings = new AppSettings
            {
                Theme = "auto",
                Mode = "rendered",
                WidthMode = "full",
                ReadingWidth = 720f,
                Outline = true,
            };

            AppSettings.DirectoryOverride = Path.Combine(Path.GetTempPath(), "mdview-behaviour-" + Guid.NewGuid().ToString("N"));
            using (var form = new MainForm(settings))
            {
                form.CycleWidth();
            }

            Harness.Equal("75", settings.WidthMode, "persisted after the click");
            Harness.False(settings.OpenInNewWindow, "tabs by default");
        });

        Harness.Suite("tabs");

        Harness.Test("two files open as two tabs", () =>
        {
            string first = WriteTemp("# One\n\ntext\n");
            string second = WriteTemp("# Two\n\nmore\n");
            using MainForm form = CreateForm(_ => { });

            form.OpenFile(first);
            Harness.Equal(1, form.TabCount, "one tab");
            form.OpenFile(second);
            Harness.Equal(2, form.TabCount, "two tabs");
            Harness.True(form.TabsRequested, "tab strip shown with two documents");
        });

        Harness.Test("opening the same file twice reuses its tab", () =>
        {
            string file = WriteTemp("# Same\n");
            using MainForm form = CreateForm(_ => { });
            form.OpenFile(file);
            form.OpenFile(file);
            Harness.Equal(1, form.SessionCount, "still one session");
        });

        Harness.Test("a new window is started with the file as its argument", () =>
        {
            System.Diagnostics.ProcessStartInfo start = MainForm.NewWindowStart(@"C:\bin\mdview.exe", @"C:\docs\a.md");
            Harness.Equal(@"C:\bin\mdview.exe", start.FileName, "exe");
            Harness.True(start.UseShellExecute, "shell execute");
            Harness.Equal(1, start.ArgumentList.Count, "one argument");
            Harness.Equal(@"C:\docs\a.md", start.ArgumentList[0], "file");
        });
    }

    private static string WriteTemp(string text)
    {
        string path = Path.Combine(Path.GetTempPath(), "mdview-tab-" + Guid.NewGuid().ToString("N") + ".md");
        File.WriteAllText(path, text);
        return path;
    }
}
