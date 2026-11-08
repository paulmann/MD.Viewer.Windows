namespace MdViewer.Core.Diagrams;

/// <summary><c>journey</c>: sections of tasks with a 1…5 satisfaction score.</summary>
public static class MermaidJourney
{
    public static bool TryParse(string source, out JourneyDiagram? diagram, out string? error)
    {
        diagram = null;
        error = null;

        var result = new JourneyDiagram();
        bool sawHeader = false;
        JourneySection current = result.Section("general");

        foreach (string rawLine in source.Replace("\r\n", "\n").Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("%%", StringComparison.Ordinal))
            {
                continue;
            }

            if (!sawHeader)
            {
                if (!line.StartsWith("journey", StringComparison.OrdinalIgnoreCase))
                {
                    error = "not a journey diagram";
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

            if (line.StartsWith("section ", StringComparison.OrdinalIgnoreCase))
            {
                current = result.Section(MermaidDiagrams.Clean(line[8..]));
                continue;
            }

            string[] parts = line.Split(':');
            if (parts.Length < 2)
            {
                continue;
            }

            string name = MermaidDiagrams.Clean(parts[0]);
            string scoreText = parts[1].Trim();
            string actors = parts.Length > 2 ? string.Join(", ", parts[2..].Select(p => p.Trim()).Where(p => p.Length > 0)) : string.Empty;

            if (!int.TryParse(scoreText, out int score))
            {
                continue;
            }

            current.Tasks.Add(new JourneyTask(name, score, actors));
        }

        JourneySection[] withTasks = result.Sections.Where(s => s.Tasks.Count > 0).ToArray();
        if (!sawHeader || withTasks.Length == 0)
        {
            error = sawHeader ? "no tasks" : "not a journey diagram";
            return false;
        }

        result.Sections.Clear();
        foreach (JourneySection section in withTasks)
        {
            result.Sections.Add(section);
        }

        diagram = result;
        return true;
    }
}
