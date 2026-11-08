using System.Text;

namespace MdViewer.Core.Diagrams;

public enum NodeShape
{
    Rectangle,
    Rounded,
    Stadium,
    Diamond,
    Circle,
}

public sealed class FlowNode(string id, string label, NodeShape shape)
{
    public string Id { get; } = id;

    public string Label { get; } = label;

    public NodeShape Shape { get; } = shape;
}

public sealed class FlowEdge(string from, string to, string? label, bool dashed)
{
    public string From { get; } = from;

    public string To { get; } = to;

    public string? Label { get; } = label;

    public bool Dashed { get; } = dashed;
}

public sealed class Flowchart
{
    public bool LeftToRight { get; init; }

    public List<FlowNode> Nodes { get; } = [];

    public List<FlowEdge> Edges { get; } = [];

    public FlowNode? Find(string id) => Nodes.FirstOrDefault(n => n.Id == id);
}

/// <summary>
/// Mermaid flowchart parser — the subset that documentation actually uses:
/// <c>graph TD/LR</c>, node shapes, solid/dotted edges with labels, subgraphs
/// flattened. Anything else reports a diagnostic instead of failing.
/// </summary>
public static class MermaidFlowchart
{
    public static bool TryParse(string source, out Flowchart? chart, out string? error)
    {
        chart = null;
        error = null;
        string[] lines = source.Replace("\r\n", "\n").Split('\n');

        Flowchart result = null!;
        bool sawHeader = false;
        var nodes = new List<FlowNode>();
        var edges = new List<FlowEdge>();

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("%%", StringComparison.Ordinal))
            {
                continue;
            }

            if (!sawHeader)
            {
                string head = line.Split(' ')[0].ToLowerInvariant();
                if (head is "graph" or "flowchart")
                {
                    string direction = line.Length > head.Length ? line[head.Length..].Trim() : "TD";
                    result = new Flowchart { LeftToRight = direction.StartsWith("LR", StringComparison.OrdinalIgnoreCase) || direction.StartsWith("RL", StringComparison.OrdinalIgnoreCase) };
                    sawHeader = true;
                    continue;
                }

                error = "not a flowchart";
                return false;
            }

            if (line.StartsWith("subgraph", StringComparison.OrdinalIgnoreCase) || line.Equals("end", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("classDef", StringComparison.OrdinalIgnoreCase) || line.StartsWith("class ", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("style", StringComparison.OrdinalIgnoreCase) || line.StartsWith("linkStyle", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("click", StringComparison.OrdinalIgnoreCase) || line.StartsWith("direction", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (string statement in SplitStatements(line))
            {
                string text = statement.Trim();
                if (text.Length == 0)
                {
                    continue;
                }

                if (!TryParseStatement(text, nodes, edges))
                {
                    error = $"cannot read: {text}";
                    return false;
                }
            }
        }

        if (!sawHeader || result is null)
        {
            error = "missing graph header";
            return false;
        }

        if (nodes.Count == 0)
        {
            error = "no nodes";
            return false;
        }

        foreach (FlowNode node in nodes)
        {
            result.Nodes.Add(node);
        }

        var known = new HashSet<string>(result.Nodes.Select(n => n.Id), StringComparer.Ordinal);
        foreach (FlowEdge edge in edges)
        {
            if (known.Contains(edge.From) && known.Contains(edge.To))
            {
                result.Edges.Add(edge);
            }
        }

        chart = result;
        return true;
    }

    private static IEnumerable<string> SplitStatements(string line)
    {
        // semicolons separate statements; keep arrows intact
        var parts = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;
        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }

            if (c == ';' && !inQuotes)
            {
                parts.Add(sb.ToString());
                sb.Clear();
                continue;
            }

            sb.Append(c);
        }

        parts.Add(sb.ToString());
        return parts;
    }

    private static readonly string[] ArrowTokens = ["-.->", "==>", "-->", "---", "->", "--"];

    private static bool TryParseStatement(string statement, List<FlowNode> nodes, List<FlowEdge> edges)
    {
        int cursor = 0;
        if (!TryReadNode(statement, ref cursor, nodes, out string? current))
        {
            return false;
        }

        string from = current!;

        while (cursor < statement.Length)
        {
            while (cursor < statement.Length && char.IsWhiteSpace(statement[cursor]))
            {
                cursor++;
            }

            if (cursor >= statement.Length)
            {
                break;
            }

            string? arrow = ArrowTokens.FirstOrDefault(a => statement.AsSpan(cursor).StartsWith(a, StringComparison.Ordinal));
            if (arrow is null)
            {
                return false;
            }

            cursor += arrow.Length;
            bool dashed = arrow.StartsWith("-.", StringComparison.Ordinal);
            string? label = null;

            if (cursor < statement.Length && statement[cursor] == '|')
            {
                int close = statement.IndexOf('|', cursor + 1);
                if (close < 0)
                {
                    return false;
                }

                label = statement[(cursor + 1)..close].Trim();
                cursor = close + 1;
            }

            while (cursor < statement.Length && char.IsWhiteSpace(statement[cursor]))
            {
                cursor++;
            }

            if (!TryReadNode(statement, ref cursor, nodes, out string? target))
            {
                return false;
            }

            if (label is not null && label.Length == 0)
            {
                label = null;
            }

            if (arrow is "---" or "--")
            {
                // undirected link: still draw a line, no arrow head
            }

            edges.Add(new FlowEdge(from, target!, label, dashed));
            from = target!;
        }

        return true;
    }

    private static bool TryReadNode(string statement, ref int cursor, List<FlowNode> nodes, out string? id)
    {
        id = null;
        while (cursor < statement.Length && char.IsWhiteSpace(statement[cursor]))
        {
            cursor++;
        }

        int start = cursor;
        while (cursor < statement.Length && (char.IsLetterOrDigit(statement[cursor]) || statement[cursor] is '_' or '-' or '.'))
        {
            cursor++;
        }

        if (cursor == start)
        {
            return false;
        }

        string name = statement[start..cursor];
        string label = name;
        NodeShape shape = NodeShape.Rounded;

        if (cursor < statement.Length)
        {
            char open = statement[cursor];
            string? closeToken = null;
            NodeShape? shapeCandidate = null;

            switch (open)
            {
                case '[':
                    closeToken = statement.AsSpan(cursor).StartsWith("[[", StringComparison.Ordinal) ? "]]" : "]";
                    shapeCandidate = closeToken == "]]" ? NodeShape.Rectangle : NodeShape.Rectangle;
                    break;
                case '(':
                    closeToken = statement.AsSpan(cursor).StartsWith("((", StringComparison.Ordinal) ? "))" : ")";
                    shapeCandidate = statement.AsSpan(cursor).StartsWith("((", StringComparison.Ordinal) ? NodeShape.Circle : NodeShape.Stadium;
                    break;
                case '{':
                    closeToken = "}";
                    shapeCandidate = NodeShape.Diamond;
                    break;
                case '>':
                    closeToken = "]";
                    shapeCandidate = NodeShape.Rectangle;
                    break;
            }

            if (closeToken is not null && shapeCandidate is not null)
            {
                int contentStart = cursor + (closeToken.Length == 2 ? 2 : 1);
                int close = statement.IndexOf(closeToken, contentStart, StringComparison.Ordinal);
                if (close < 0)
                {
                    return false;
                }

                label = statement[contentStart..close].Trim().Trim('"');
                cursor = close + closeToken.Length;
                shape = shapeCandidate.Value;
            }
        }

        label = Unescape(label);
        FlowNode? existing = nodes.FirstOrDefault(n => n.Id == name);
        if (existing is null)
        {
            nodes.Add(new FlowNode(name, label, shape));
        }
        else if (existing.Label == name && existing.Shape == NodeShape.Rounded)
        {
            nodes[nodes.IndexOf(existing)] = new FlowNode(name, label, shape);
        }

        id = name;
        return true;
    }

    private static string Unescape(string label) =>
        label.Replace("<br/>", "\n", StringComparison.OrdinalIgnoreCase)
             .Replace("<br>", "\n", StringComparison.OrdinalIgnoreCase)
             .Replace("\\n", "\n", StringComparison.Ordinal);
}
