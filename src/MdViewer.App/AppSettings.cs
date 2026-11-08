using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MdViewer.App;

/// <summary>Persisted preferences and session (one JSON file in %APPDATA%).</summary>
public sealed class AppSettings
{
    public string Theme { get; set; } = "auto";

    public string Mode { get; set; } = "render";

    public float Zoom { get; set; } = 1f;

    public float ReadingWidth { get; set; } = 720f;

    public bool Serif { get; set; }

    public bool Outline { get; set; } = true;

    public bool LineNumbers { get; set; }

    public bool WrapCode { get; set; } = true;

    public bool Numbering { get; set; }

    /// <summary>A single newline inside a paragraph starts a new line (MD.Viewer legacy).</summary>
    public bool LineBreaks { get; set; } = true;

    /// <summary>Dashes, ellipsis and (c)/(r)/(tm) become typographic symbols.</summary>
    public bool SmartPunctuation { get; set; }

    public bool EditorPreview { get; set; }

    public bool Watch { get; set; } = true;

    public string Language { get; set; } = "auto";

    public int WindowX { get; set; } = int.MinValue;

    public int WindowY { get; set; } = int.MinValue;

    public int WindowWidth { get; set; } = 1120;

    public int WindowHeight { get; set; } = 820;

    public bool Maximized { get; set; }

    public List<string> Recent { get; set; } = [];

    public List<string> Session { get; set; } = [];

    public string? LastFile { get; set; }

    [JsonIgnore]
    public string Directory { get; private set; } = string.Empty;

    [JsonIgnore]
    public string FilePath { get; private set; } = string.Empty;

    /// <summary>Test hook: keeps integration runs out of the real profile.</summary>
    public static string? DirectoryOverride { get; set; }

    public static string SettingsDirectory
    {
        get
        {
            if (!string.IsNullOrEmpty(DirectoryOverride))
            {
                return DirectoryOverride;
            }

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrEmpty(appData))
            {
                appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Roaming");
            }

            return Path.Combine(appData, "MD.Viewer");
        }
    }

    public static AppSettings Load()
    {
        string directory = SettingsDirectory;
        string path = Path.Combine(directory, "settings.json");
        var settings = new AppSettings { Directory = directory, FilePath = path };

        try
        {
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                AppSettings? loaded = JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions);
                if (loaded is not null)
                {
                    loaded.Directory = directory;
                    loaded.FilePath = path;
                    return loaded;
                }
            }
        }
        catch
        {
            // a broken settings file must never stop the viewer from starting
        }

        return settings;
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public void Save()
    {
        try
        {
            System.IO.Directory.CreateDirectory(Directory);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, SerializerOptions));
        }
        catch
        {
            // ignore: preferences are a convenience, not a requirement
        }
    }

    public void AddRecent(string path)
    {
        Recent.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
        Recent.Insert(0, path);
        while (Recent.Count > 12)
        {
            Recent.RemoveAt(Recent.Count - 1);
        }

        LastFile = path;
    }

    public void Reset()
    {
        Theme = "auto";
        Mode = "render";
        Zoom = 1f;
        ReadingWidth = 720f;
        Serif = false;
        Outline = true;
        LineNumbers = false;
        WrapCode = true;
        Numbering = false;
        LineBreaks = true;
        SmartPunctuation = false;
        EditorPreview = false;
        Watch = true;
        Recent.Clear();
        Session.Clear();
        LastFile = null;
        WindowWidth = 1120;
        WindowHeight = 820;
        WindowX = int.MinValue;
        WindowY = int.MinValue;
        Maximized = false;
    }
}



