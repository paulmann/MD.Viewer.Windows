namespace MdViewer.Core.Diagrams;

/// <summary>
/// <c>stateDiagram-v2</c>: converted into the flowchart model so the same layered
/// layout draws both. <c>[*]</c> becomes a start/end dot.
/// </summary>
public static class MermaidState
{
    public static bool TryParse(string source, out Flowchart? chart, out string? error)
    {
        chart = null;
        error = null;

        var result = new Flowchart();
        bool sawHeader = false;
        int startCounter = 0;
        int endCounter = 0;
        var aliases = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (string rawLine in source.Replace("\r\n", "\n").Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("%%", StringComparison.Ordinal))
            {
                continue;
            }

            if (!sawHeader)
            {
                string head = line.Split(' ')[0].ToLowerInvariant();
                if (head is not ("statediagram" or "statediagram-v2" or "state"))
                {
                    error = "not a state diagram";
                    return false;
                }

                sawHeader = true;
                continue;
            }

            if (line.StartsWith("direction ", StringComparison.OrdinalIgnoreCase))
            {
                result.LeftToRight = line.EndsWith("LR", StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (line.Equals("}", StringComparison.Ordinal) || line.EndsWith("{", StringComparison.Ordinal))
            {
                // composite states are flattened: their inner states become nodes
                string header = line.TrimEnd('{').Trim();
                if (header.StartsWith("state ", StringComparison.OrdinalIgnoreCase) && header.Length > 6)
                {
                    Register(header[6..].Trim(), aliases, result);
                }

                continue;
            }

            if (line.StartsWith("state ", StringComparison.OrdinalIgnoreCase))
            {
                Register(line[6..].Trim(), aliases, result);
                continue;
            }

            if (line.StartsWith("note ", StringComparison.OrdinalIgnoreCase) || line.StartsWith("classDef", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("class ", StringComparison.OrdinalIgnoreCase) || line.StartsWith("style", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            int colon = line.IndexOf(':');
            string transition = colon < 0 ? line : line[..colon].Trim();
            string label = colon < 0 ? string.Empty : MermaidDiagrams.Clean(line[(colon + 1)..]);

            int arrow = transition.IndexOf("-->", StringComparison.Ordinal);
            if (arrow <= 0)
            {
                continue;
            }

            string from = transition[..arrow].Trim();
            string to = transition[(arrow + 3)..].Trim();

            if (from.Contains(','))
            {
                // "A, B --> C" is a shorthand for two transitions
                foreach (string part in from.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                {
                    AddEdge(result, part, to, label, aliases, ref startCounter, ref endCounter);
                }

                continue;
            }

            AddEdge(result, from, to, label, aliases, ref startCounter, ref endCounter);
        }

        if (!sawHeader || result.Nodes.Count == 0)
        {
            error = sawHeader ? "no states" : "not a state diagram";
            return false;
        }

        chart = result;
        return true;
    }

    private static void Register(string text, Dictionary<string, string> aliases, Flowchart chart)
    {
        // forms: Id | "Label" as Id | Id <<choice>>
        string id = text;
        string label = text;

        int asIndex = text.IndexOf(" as ", StringComparison.OrdinalIgnoreCase);
        if (asIndex > 0)
        {
            label = MermaidDiagrams.Clean(text[..asIndex]);
            id = text[(asIndex + 4)..].Trim();
        }

        int stereotype = id.IndexOf("<<", StringComparison.Ordinal);
        if (stereotype > 0)
        {
            id = id[..stereotype].Trim();
        }

        id = id.Trim('"');
        aliases[id] = label == id ? id : label;

        if (chart.Find(id) is null)
        {
            chart.Nodes.Add(new FlowNode(id, aliases[id], NodeShape.Rounded));
        }
    }

    private static void AddEdge(Flowchart chart, string from, string to, string label, Dictionary<string, string> aliases, ref int startCounter, ref int endCounter)
    {
        string fromId = Resolve(from, aliases, chart, isStart: true, ref startCounter, ref endCounter);
        string toId = Resolve(to, aliases, chart, isStart: false, ref startCounter, ref endCounter);
        chart.Edges.Add(new FlowEdge(fromId, toId, label.Length == 0 ? null : label, dashed: false));
    }

    private static string Resolve(string token, Dictionary<string, string> aliases, Flowchart chart, bool isStart, ref int startCounter, ref int endCounter)
    {
        string id = token.Trim();
        if (id is "[*]" or "[*] ")
        {
            string generated = isStart ? $"__start{++startCounter}" : $"__end{++endCounter}";
            chart.Nodes.Add(new FlowNode(generated, string.Empty, NodeShape.StartEnd));
            return generated;
        }

        if (!aliases.ContainsKey(id))
        {
            aliases[id] = id;
            if (chart.Find(id) is null)
            {
                chart.Nodes.Add(new FlowNode(id, id, NodeShape.Rounded));
            }
        }

        return id;
    }
}

