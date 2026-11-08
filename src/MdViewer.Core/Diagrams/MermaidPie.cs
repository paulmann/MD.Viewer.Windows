using System.Globalization;

namespace MdViewer.Core.Diagrams;

/// <summary><c>pie</c>: a title and labelled slices with numeric values.</summary>
public static class MermaidPie
{
    public static bool TryParse(string source, out PieDiagram? diagram, out string? error)
    {
        diagram = null;
        error = null;

        var result = new PieDiagram();
        bool sawHeader = false;

        foreach (string rawLine in source.Replace("\r\n", "\n").Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("%%", StringComparison.Ordinal))
            {
                continue;
            }

            if (!sawHeader)
            {
                if (!line.StartsWith("pie", StringComparison.OrdinalIgnoreCase))
                {
                    error = "not a pie chart";
                    return false;
                }

                sawHeader = true;
                string rest = line[3..].Trim();
                if (rest.StartsWith("showData", StringComparison.OrdinalIgnoreCase))
                {
                    result.ShowData = true;
                    rest = rest[8..].Trim();
                }

                if (rest.StartsWith("title", StringComparison.OrdinalIgnoreCase))
                {
                    result.Title = MermaidDiagrams.Clean(rest[5..]);
                }
                else if (rest.Length > 0)
                {
                    result.Title = MermaidDiagrams.Clean(rest);
                }

                continue;
            }

            int colon = line.LastIndexOf(':');
            if (colon <= 0)
            {
                error = $"cannot read slice: {line}";
                return false;
            }

            string label = MermaidDiagrams.Clean(line[..colon]);
            string number = line[(colon + 1)..].Trim();

            if (!double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) || value < 0)
            {
                error = $"bad value for '{label}': {number}";
                return false;
            }

            result.Slices.Add(new PieSlice(label, value));
        }

        if (!sawHeader || result.Slices.Count == 0)
        {
            error = sawHeader ? "no slices" : "not a pie chart";
            return false;
        }

        diagram = result;
        return true;
    }
}
