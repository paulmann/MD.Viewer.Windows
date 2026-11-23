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

        Harness.Suite("dragged tabs");

        Harness.Test("a dragged tab carries its path and its window", () =>
        {
            DataObject data = TabDrag.Create(@"C:\docs\a.md", 4242);
            Harness.True(TabDrag.TryRead(data, out string path, out long source), "readable");
            Harness.Equal(@"C:\docs\a.md", path, "path");
            Harness.Equal(4242L, source, "source");
        });

        Harness.Test("foreign data is not a dragged tab", () =>
        {
            var data = new DataObject(DataFormats.UnicodeText, "hello");
            Harness.False(TabDrag.TryRead(data, out _, out _), "plain text refused");
            Harness.False(TabDrag.TryRead(null, out _, out _), "null refused");
        });

        Harness.Test("a tab survives a process boundary as a blob", () =>
        {
            // OLE hands a custom format over as a byte array, not as the original string.
            byte[] blob = System.Text.Encoding.UTF8.GetBytes("777\nC:\\docs\\b.md");
            var data = new DataObject();
            data.SetData(TabDrag.Format, blob);

            Harness.True(TabDrag.TryRead(data, out string path, out long source), "readable");
            Harness.Equal(@"C:\docs\b.md", path, "path");
            Harness.Equal(777L, source, "source");
        });

        Harness.Test("a tab survives as plain text too", () =>
        {
            var data = new DataObject(DataFormats.UnicodeText, "MDViewer.Tab:5150:C:\\docs\\c.md");
            Harness.True(TabDrag.TryRead(data, out string path, out long source), "readable");
            Harness.Equal(@"C:\docs\c.md", path, "path");
            Harness.Equal(5150L, source, "source");
        });

        Harness.Test("another window takes the tab, this one keeps it", () =>
        {
            Harness.Equal(DragDropEffects.Move, TabDrag.Decide(fromThisWindow: false, alreadyOpenHere: false), "other window");
            Harness.Equal(DragDropEffects.Move, TabDrag.Decide(fromThisWindow: false, alreadyOpenHere: true), "other window, already open");
            Harness.Equal(DragDropEffects.Link, TabDrag.Decide(fromThisWindow: true, alreadyOpenHere: true), "own tab back");
            Harness.Equal(DragDropEffects.None, TabDrag.Decide(fromThisWindow: true, alreadyOpenHere: false), "own tab, unknown file");
        });

        Harness.Test("a tab dropped from another window becomes a tab here", () =>
        {
            string file = WriteTemp("# Dropped\n\ntext\n");
            using MainForm form = CreateForm(_ => { });
            DragDropEffects effect = form.HandleTabDrop(file, sourceHandle: 999);

            Harness.Equal(DragDropEffects.Move, effect, "effect");
            Harness.Equal(1, form.TabCount, "one tab");
        });

        Harness.Test("dropping a tab back on its own window changes nothing", () =>
        {
            string file = WriteTemp("# Back\n");
            using MainForm form = CreateForm(_ => { });
            form.OpenFile(file);

            DragDropEffects effect = form.HandleTabDrop(file, form.Handle.ToInt64());
            Harness.Equal(DragDropEffects.Link, effect, "effect");
            Harness.Equal(1, form.TabCount, "still one tab");
        });

        Harness.Test("a detached tab starts a window at the drop point", () =>
        {
            System.Diagnostics.ProcessStartInfo start = MainForm.BuildStartInfo(@"C:\bin\mdview.exe", null, @"C:\docs\a.md", new Point(5300, 240));
            Harness.Equal(@"C:\bin\mdview.exe", start.FileName, "host");
            Harness.Equal(@"C:\docs\a.md", start.ArgumentList[0], "file");
            Harness.Equal("--position", start.ArgumentList[1], "switch");
            Harness.Equal("5300,240", start.ArgumentList[2], "position");
        });

        Harness.Test("a dotnet host passes our assembly first", () =>
        {
            System.Diagnostics.ProcessStartInfo start = MainForm.BuildStartInfo(@"C:\dotnet\dotnet.exe", @"C:\app\mdview.dll", @"C:\docs\a.md", new Point(10, 20));
            Harness.Equal(@"C:\app\mdview.dll", start.ArgumentList[0], "assembly");
            Harness.Equal(@"C:\docs\a.md", start.ArgumentList[1], "file");
        });

        Harness.Suite("menu placement");

        Harness.Test("a menu fits under its button", () =>
        {
            var button = new Rectangle(700, 100, 34, 34);
            Rectangle fitted = TabDrag.FitMenu(button, new Size(202, 408), new Rectangle(0, 0, 2560, 1440));
            Harness.Equal(new Point(700, 134), fitted.Location, "anchored at the button's bottom-left");
        });

        Harness.Test("a menu never leaves the monitor on the right", () =>
        {
            var button = new Rectangle(2540, 100, 34, 34);
            Rectangle fitted = TabDrag.FitMenu(button, new Size(202, 408), new Rectangle(0, 0, 2560, 1440));
            Harness.True(fitted.Right <= 2560, $"right {fitted.Right}");
            Harness.Equal(100 + 34, fitted.Top, "still below the button");
        });

        Harness.Test("a menu near the bottom flips above its button", () =>
        {
            var button = new Rectangle(700, 1300, 34, 34);
            Rectangle fitted = TabDrag.FitMenu(button, new Size(202, 408), new Rectangle(0, 0, 2560, 1440));
            Harness.True(fitted.Bottom <= 1440, $"bottom {fitted.Bottom}");
            Harness.True(fitted.Bottom <= button.Top, "above the button");
        });
    }

    private static string WriteTemp(string text)
    {
        string path = Path.Combine(Path.GetTempPath(), "mdview-tab-" + Guid.NewGuid().ToString("N") + ".md");
        File.WriteAllText(path, text);
        return path;
    }
}
