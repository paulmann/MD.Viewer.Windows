using System.Globalization;

namespace MdViewer.Core.Markdown;

/// <summary>YAML-lite front matter value: a scalar, a list, or a flag.</summary>
public union FrontMatterValue(string, IReadOnlyList<string>, bool, long);

/// <summary>The <c>---</c>…<c>---</c> block at the top of a document.</summary>
public sealed class FrontMatter
{
    public static FrontMatter Empty { get; } = new();

    public Dictionary<string, FrontMatterValue> Values { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Number of source lines the block occupied (0 when absent).</summary>
    public int LineCount { get; set; }

    public bool Present => LineCount > 0;

    public string? Title => String("title");

    public string? Description => String("description");

    public string? Author => String("author");

    public string? Date => String("date");

    public bool Draft => Flag("draft");

    /// <summary>null = follow the application default.</summary>
    public bool? Toc => NullableFlag("toc");

    public bool Numbering => Flag("numbering");

    public IReadOnlyList<string> Tags => List("tags");

    public string? String(string key) =>
        Values.TryGetValue(key, out var value) && value is string s ? s : null;

    public bool Flag(string key, bool fallback = false)
    {
        if (!Values.TryGetValue(key, out var value))
        {
            return fallback;
        }

        return value switch
        {
            bool b => b,
            string s => s.Equals("true", StringComparison.OrdinalIgnoreCase) || s.Equals("yes", StringComparison.OrdinalIgnoreCase),
            _ => fallback,
        };
    }

    public bool? NullableFlag(string key) =>
        Values.ContainsKey(key) ? Flag(key) : null;

    public IReadOnlyList<string> List(string key)
    {
        if (!Values.TryGetValue(key, out var value))
        {
            return [];
        }

        return value switch
        {
            IReadOnlyList<string> list => list,
            string s when s.Length > 0 => [s],
            _ => [],
        };
    }

    /// <summary>Reads a leading <c>---</c> fenced YAML-lite block; returns the first body line.</summary>
    public static FrontMatter Parse(string[] lines, out int bodyStart)
    {
        bodyStart = 0;
        if (lines.Length < 2 || !IsFence(lines[0]))
        {
            return Empty;
        }

        int end = -1;
        for (int i = 1; i < Math.Min(lines.Length, 400); i++)
        {
            if (IsFence(lines[i]))
            {
                end = i;
                break;
            }
        }

        if (end < 0)
        {
            return Empty;
        }

        var front = new FrontMatter { LineCount = end + 1 };
        bodyStart = end + 1;

        string? pendingKey = null;
        var pendingList = new List<string>();

        void FlushPending()
        {
            if (pendingKey is not null)
            {
                front.Values[pendingKey] = pendingList.Count == 0
                    ? string.Empty
                    : (FrontMatterValue)pendingList.ToArray();
            }

            pendingKey = null;
            pendingList = [];
        }

        for (int i = 1; i < end; i++)
        {
            string line = lines[i];
            if (line.Length == 0 || line.TrimStart().StartsWith('#'))
            {
                continue;
            }

            string trimmed = line.TrimStart();
            if (trimmed.StartsWith("- ", StringComparison.Ordinal) && pendingKey is not null)
            {
                pendingList.Add(Unquote(TrimComment(trimmed[2..]).Trim()));
                continue;
            }

            int colon = line.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }

            FlushPending();

            string key = line[..colon].Trim();
            string rawValue = TrimComment(line[(colon + 1)..]).Trim();

            if (rawValue.Length == 0)
            {
                pendingKey = key;
                continue;
            }

            front.Values[key] = ParseScalar(rawValue);
        }

        FlushPending();
        return front;
    }

    private static bool IsFence(string line)
    {
        string t = line.TrimEnd();
        return t == "---" || t == "...";
    }

    private static FrontMatterValue ParseScalar(string raw)
    {
        if (raw.StartsWith('[') && raw.EndsWith(']'))
        {
            string inner = raw[1..^1].Trim();
            if (inner.Length == 0)
            {
                return Array.Empty<string>();
            }

            return inner.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(Unquote)
                .ToArray();
        }

        string value = Unquote(raw);
        if (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("yes", StringComparison.OrdinalIgnoreCase))
        {
            return (FrontMatterValue)true;
        }

        if (value.Equals("false", StringComparison.OrdinalIgnoreCase) || value.Equals("no", StringComparison.OrdinalIgnoreCase))
        {
            return (FrontMatterValue)false;
        }

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long number) && value.Length < 19)
        {
            return (FrontMatterValue)number;
        }

        return (FrontMatterValue)value;
    }

    private static string TrimComment(string value)
    {
        // A '#' starts a comment only after whitespace or at the start.
        for (int i = 1; i < value.Length; i++)
        {
            if (value[i] == '#' && char.IsWhiteSpace(value[i - 1]))
            {
                return value[..i];
            }
        }

        return value;
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2)
        {
            char first = value[0];
            char last = value[^1];
            if ((first == '"' && last == '"') || (first == '\'' && last == '\''))
            {
                return value[1..^1];
            }
        }

        return value;
    }
}
