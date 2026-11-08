using System.Globalization;
using System.Text.RegularExpressions;

namespace MdViewer.Core.Diagrams;

/// <summary>
/// <c>gantt</c>: sections and tasks with explicit dates, durations and
/// <c>after</c> dependencies. Timeline for the chart is derived by the renderer.
/// </summary>
public static partial class MermaidGantt
{
    [GeneratedRegex(@"^(\d+)\s*(d|w|h|m|y)?$", RegexOptions.IgnoreCase)]
    private static partial Regex DurationPattern();

    public static bool TryParse(string source, out GanttDiagram? diagram, out string? error)
    {
        diagram = null;
        error = null;

        var result = new GanttDiagram();
        bool sawHeader = false;
        GanttSection current = result.Section(null);
        var byId = new Dictionary<string, GanttTask>(StringComparer.Ordinal);
        DateTime cursor = DateTime.MinValue;

        foreach (string rawLine in source.Replace("\r\n", "\n").Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("%%", StringComparison.Ordinal))
            {
                continue;
            }

            if (!sawHeader)
            {
                if (!line.Equals("gantt", StringComparison.OrdinalIgnoreCase))
                {
                    error = "not a gantt chart";
                    return false;
                }

                sawHeader = true;
                continue;
            }

            if (line.StartsWith("title ", StringComparison.OrdinalIgnoreCase))
            {
                result.Title = MermaidDiagrams.Clean(line[6..]);
                continue;
            }

            if (line.StartsWith("dateFormat ", StringComparison.OrdinalIgnoreCase))
            {
                result.DateFormat = line[11..].Trim();
                continue;
            }

            if (line.StartsWith("axisFormat ", StringComparison.OrdinalIgnoreCase) || line.StartsWith("excludes ", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("todayMarker ", StringComparison.OrdinalIgnoreCase) || line.StartsWith("tickInterval ", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("weekday ", StringComparison.OrdinalIgnoreCase) || line.StartsWith("inclusive", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (line.StartsWith("section ", StringComparison.OrdinalIgnoreCase))
            {
                current = result.Section(MermaidDiagrams.Clean(line[8..]));
                continue;
            }

            int colon = line.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }

            string name = MermaidDiagrams.Clean(line[..colon]);
            string meta = line[(colon + 1)..];

            if (!TryParseTask(name, meta, result.DateFormat, byId, ref cursor, out GanttTask? task, out string? taskError))
            {
                error = taskError;
                return false;
            }

            current.Tasks.Add(task!);
            if (task!.Id.Length > 0)
            {
                byId[task.Id] = task;
            }

            cursor = task.End > cursor ? task.End : cursor;
        }

        GanttSection[] withTasks = result.Sections.Where(s => s.Tasks.Count > 0).ToArray();
        if (!sawHeader || withTasks.Length == 0)
        {
            error = sawHeader ? "no tasks" : "not a gantt chart";
            return false;
        }

        result.Sections.Clear();
        foreach (GanttSection section in withTasks)
        {
            result.Sections.Add(section);
        }

        diagram = result;
        return true;
    }

    private static bool TryParseTask(
        string name,
        string meta,
        string dateFormat,
        Dictionary<string, GanttTask> byId,
        ref DateTime cursor,
        out GanttTask? task,
        out string? error)
    {
        task = null;
        error = null;

        string id = string.Empty;
        string? after = null;
        string? until = null;
        DateTime? start = null;
        TimeSpan? duration = null;
        bool milestone = false;
        bool done = false;

        foreach (string rawToken in meta.Split(','))
        {
            string token = rawToken.Trim();
            if (token.Length == 0)
            {
                continue;
            }

            string lower = token.ToLowerInvariant();
            if (lower is "done" or "active" or "crit" or "inactive")
            {
                done |= lower is "done" or "active";
                continue;
            }

            if (lower == "milestone")
            {
                milestone = true;
                continue;
            }

            if (lower.StartsWith("after ", StringComparison.Ordinal))
            {
                after = token[6..].Trim();
                continue;
            }

            if (lower.StartsWith("until ", StringComparison.Ordinal))
            {
                until = token[6..].Trim();
                continue;
            }

            if (TryParseDate(token, dateFormat, out DateTime parsed))
            {
                start ??= parsed;
                continue;
            }

            Match match = DurationPattern().Match(token);
            if (match.Success)
            {
                int amount = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                string unit = match.Groups[2].Success ? match.Groups[2].Value.ToLowerInvariant() : "d";
                duration = unit switch
                {
                    "w" => TimeSpan.FromDays(amount * 7),
                    "h" => TimeSpan.FromHours(amount),
                    "m" => TimeSpan.FromDays(Math.Max(1, amount / 1440)),
                    "y" => TimeSpan.FromDays(amount * 365),
                    _ => TimeSpan.FromDays(amount),
                };
                continue;
            }

            if (Regex.IsMatch(token, @"^[\w.-]+$"))
            {
                id = token;
            }
        }

        if (after is not null)
        {
            if (!byId.TryGetValue(after, out GanttTask? dependency))
            {
                error = $"unknown task in 'after {after}'";
                return false;
            }

            start = dependency.End;
        }

        if (start is null)
        {
            start = cursor == DateTime.MinValue ? DateTime.Today : cursor;
        }

        DateTime begin = start.Value;

        if (until is not null && TryParseDate(until, dateFormat, out DateTime endDate))
        {
            duration = endDate - begin;
        }

        if (milestone)
        {
            duration = TimeSpan.Zero;
        }

        TimeSpan length = duration ?? TimeSpan.FromDays(1);
        if (length < TimeSpan.Zero)
        {
            length = TimeSpan.Zero;
        }

        task = new GanttTask(name, id, begin, begin + length, milestone, done);
        return true;
    }

    /// <summary>Translates the Mermaid date format to a .NET pattern and parses.</summary>
    public static bool TryParseDate(string text, string format, out DateTime value)
    {
        value = default;
        string pattern = format
            .Replace("YYYY", "yyyy")
            .Replace("YY", "yy")
            .Replace("DD", "dd")
            .Replace("HH", "HH")
            .Replace("mm", "mm");

        if (DateTime.TryParseExact(text.Trim(), pattern, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
        {
            return true;
        }

        return DateTime.TryParse(text.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    }
}
