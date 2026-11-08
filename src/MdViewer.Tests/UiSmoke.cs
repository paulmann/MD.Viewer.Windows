using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using MdViewer.App;
using MdViewer.App.Controls;

namespace MdViewer.Tests;

/// <summary>
/// Drives the real window: opens a document, walks every mode and theme, and
/// captures the live window with PrintWindow. This is the integration net that
/// catches layout and chrome regressions a unit test cannot see.
/// </summary>
public static class UiSmoke
{
    public static int Run(string[] args)
    {
        string input = args.Length > 0 && !args[0].StartsWith("--") ? args[0] : "samples/demo.md";
        string outputDirectory = "screenshots";
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--out")
            {
                outputDirectory = args[i + 1];
            }
        }

        if (!File.Exists(input))
        {
            Console.Error.WriteLine($"input not found: {input}");
            return 2;
        }

        Directory.CreateDirectory(outputDirectory);

        AppSettings.DirectoryOverride = Path.Combine(Path.GetTempPath(), "mdview-ui-smoke");
        Directory.CreateDirectory(AppSettings.DirectoryOverride);

        ApplicationConfiguration.Initialize();
        AppSettings settings = new()
        {
            Outline = true,
            Zoom = 1f,
            Theme = "light",
            Mode = "render",
            WindowWidth = 1180,
            WindowHeight = 860,
        };

        var failures = new List<string>();
        string logPath = Path.Combine(outputDirectory, "ui-smoke.log");
        void Log(string message)
        {
            File.AppendAllText(logPath, DateTime.Now.ToString("HH:mm:ss.fff") + " " + message + Environment.NewLine);
        }

        File.WriteAllText(logPath, "");
        Log("start");
        using var form = new MainForm(settings);
        Log("form created");
        form.OpenFile(Path.GetFullPath(input));
        Log("file opened: " + input);

        form.Shown += (_, _) =>
        {
            Log("shown");
            var timer = new System.Windows.Forms.Timer { Interval = 450 };
            int step = 0;

            (ViewMode Mode, ThemeMode Theme, string Name)[] steps =
            [
                (ViewMode.Rendered, ThemeMode.Light, "render-light"),
                (ViewMode.Rendered, ThemeMode.Dark, "render-dark"),
                (ViewMode.Source, ThemeMode.Light, "source-light"),
                (ViewMode.Editor, ThemeMode.Dark, "editor-dark"),
            ];

            timer.Tick += (_, _) =>
            {
                if (step >= steps.Length)
                {
                    timer.Stop();
                    form.Close();
                    return;
                }

                var current = steps[step];
                Log("step " + current.Name);
                if (step == 0)
                {
                    form.SetThemeMode(current.Theme);
                    form.SetMode(current.Mode);
                }
                else
                {
                    form.SetMode(current.Mode);
                    form.SetThemeMode(current.Theme);
                }

                Application.DoEvents();
                string path = Path.Combine(outputDirectory, $"ui-{current.Name}.png");
                try
                {
                    WindowCapture.Save(form, path, clientOnly: true);
                    Log("captured " + path);
                    using var bitmap = Image.FromFile(path);
                    if (bitmap.Width < 400 || bitmap.Height < 300)
                    {
                        failures.Add($"{current.Name}: tiny capture {bitmap.Width}x{bitmap.Height}");
                    }

                    int ink = Ink(bitmap);
                    if (ink < 20)
                    {
                        failures.Add($"{current.Name}: blank capture (ink={ink})");
                    }

                    Console.WriteLine($"  {current.Name,-14} {bitmap.Width}x{bitmap.Height} ink={ink}");
                }
                catch (Exception ex)
                {
                    failures.Add($"{current.Name}: {ex.Message}");
                }

                step++;
            };

            timer.Start();
        };

        Log("run");
        Application.Run(form);
        Log("exit");

        if (failures.Count > 0)
        {
            Console.WriteLine("UI SMOKE FAILED");
            foreach (string failure in failures)
            {
                Console.WriteLine("  • " + failure);
            }

            return 1;
        }

        Console.WriteLine($"UI smoke passed — {Directory.GetFiles(outputDirectory, "ui-*.png").Length} captures in {outputDirectory}");
        return 0;
    }

    /// <summary>Counts pixels that differ from the most common colour (a crude "has content" check).</summary>
    private static int Ink(Image image)
    {
        using var bitmap = new Bitmap(image);
        var counts = new Dictionary<int, int>();
        int total = 0;

        for (int y = 0; y < bitmap.Height; y += 3)
        {
            for (int x = 0; x < bitmap.Width; x += 3)
            {
                int color = bitmap.GetPixel(x, y).ToArgb();
                counts[color] = counts.GetValueOrDefault(color) + 1;
                total++;
            }
        }

        int dominant = counts.Count == 0 ? 0 : counts.Values.Max();
        return total - dominant;
    }
}


