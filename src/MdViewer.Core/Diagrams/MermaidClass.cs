namespace MdViewer.Core.Diagrams;

/// <summary><c>classDiagram</c>: classes with members and typed relations.</summary>
public static class MermaidClass
{
    private static readonly (string Token, RelationKind Kind, bool PointsRight)[] Operators =
    [
        ("<|..", RelationKind.Realization, false),
        ("..|>", RelationKind.Realization, true),
        ("<|--", RelationKind.Inheritance, false),
        ("--|>", RelationKind.Inheritance, true),
        ("*--", RelationKind.Composition, true),
        ("--*", RelationKind.Composition, false),
        ("o--", RelationKind.Aggregation, true),
        ("--o", RelationKind.Aggregation, false),
        ("..>", RelationKind.Dependency, true),
        ("<..", RelationKind.Dependency, false),
        ("-->", RelationKind.Association, true),
        ("<--", RelationKind.Association, false),
        ("--", RelationKind.Association, true),
        ("..", RelationKind.Dependency, true),
    ];

    public static bool TryParse(string source, out ClassDiagram? diagram, out string? error)
    {
        diagram = null;
        error = null;

        var result = new ClassDiagram();
        bool sawHeader = false;
        ClassNode? open = null;

        foreach (string rawLine in source.Replace("\r\n", "\n").Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("%%", StringComparison.Ordinal))
            {
                continue;
            }

            if (!sawHeader)
            {
                if (!line.StartsWith("classDiagram", StringComparison.OrdinalIgnoreCase))
                {
                    error = "not a class diagram";
                    return false;
                }

                sawHeader = true;
                continue;
            }

            if (line.StartsWith("direction ", StringComparison.OrdinalIgnoreCase) || line.StartsWith("namespace ", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("style ", StringComparison.OrdinalIgnoreCase) || line.StartsWith("classDef ", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("cssClass ", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (open is not null)
            {
                if (line == "}")
                {
                    open = null;
                    continue;
                }

                if (line.StartsWith("<<", StringComparison.Ordinal) && line.EndsWith(">>", StringComparison.Ordinal))
                {
                    open.Annotation = line[2..^2].Trim();
                    continue;
                }

                AddMember(open, line);
                continue;
            }

            if (line.StartsWith("class ", StringComparison.OrdinalIgnoreCase))
            {
                string body = line[6..].Trim();
                int brace = body.IndexOf('{');
                if (brace >= 0)
                {
                    string name = body[..brace].Trim();
                    open = result.Node(CleanName(name));
                    string rest = body[(brace + 1)..].Trim();
                    if (rest.Length > 0)
                    {
                        if (rest.StartsWith("<<", StringComparison.Ordinal) && rest.EndsWith(">>", StringComparison.Ordinal))
                        {
                            open.Annotation = rest[2..^2].Trim();
                        }
                        else
                        {
                            AddMember(open, rest.TrimEnd('}'));
                        }
                    }

                    if (body.EndsWith('}'))
                    {
                        open = null;
                    }

                    continue;
                }

                result.Node(CleanName(body));
                continue;
            }

            if (TryParseRelation(line, out ClassRelation? relation, out string? relationError))
            {
                result.Node(relation!.From);
                result.Node(relation.To);
                result.Relations.Add(relation);
                continue;
            }

            // "Animal : +feed()" adds a member to an existing class
            int colon = line.IndexOf(':');
            if (colon > 0 && !line.Contains('<') && !line.Contains('>') && !line.Contains("--"))
            {
                ClassNode node = result.Node(CleanName(line[..colon]));
                AddMember(node, line[(colon + 1)..].Trim());
                continue;
            }

            _ = relationError;
        }

        if (!sawHeader || result.Classes.Count == 0)
        {
            error = sawHeader ? "no classes" : "not a class diagram";
            return false;
        }

        diagram = result;
        return true;
    }

    private static string CleanName(string name)
    {
        string cleaned = name.Trim().Trim('"');
        int generic = cleaned.IndexOf('~');
        return generic > 0 ? cleaned[..generic] : cleaned;
    }

    private static void AddMember(ClassNode node, string line)
    {
        string text = line.Trim().TrimEnd(';');
        if (text.Length == 0)
        {
            return;
        }

        string visibility = string.Empty;
        if (text.Length > 0 && text[0] is '+' or '-' or '#' or '~')
        {
            visibility = text[0].ToString();
            text = text[1..].Trim();
        }

        text = text.Replace("$", string.Empty).Replace("*", string.Empty).Trim();
        if (text.Length > 0)
        {
            node.Members.Add(new ClassMember(text, visibility));
        }
    }

    private static bool TryParseRelation(string line, out ClassRelation? relation, out string? error)
    {
        relation = null;
        error = null;

        foreach ((string token, RelationKind kind, bool pointsRight) in Operators)
        {
            int index = line.IndexOf(token, StringComparison.Ordinal);
            if (index <= 0)
            {
                continue;
            }

            string left = line[..index].Trim();
            string rest = line[(index + token.Length)..].Trim();

            string? label = null;
            int colon = rest.IndexOf(':');
            if (colon >= 0)
            {
                label = MermaidDiagrams.Clean(rest[(colon + 1)..]);
                rest = rest[..colon].Trim();
            }

            (string leftName, string? leftCard) = SplitCardinality(left);
            (string rightName, string? rightCard) = SplitCardinality(rest);

            if (leftName.Length == 0 || rightName.Length == 0)
            {
                continue;
            }

            string from = pointsRight ? leftName : rightName;
            string to = pointsRight ? rightName : leftName;
            relation = new ClassRelation(from, to, kind, label, pointsRight ? leftCard : rightCard, pointsRight ? rightCard : leftCard);
            return true;
        }

        return false;
    }

    private static (string Name, string? Cardinality) SplitCardinality(string value)
    {
        string text = value.Trim();
        string? cardinality = null;

        if (text.StartsWith('"'))
        {
            int close = text.IndexOf('"', 1);
            if (close > 0)
            {
                cardinality = text[1..close];
                text = text[(close + 1)..].Trim();
            }
        }

        int space = text.IndexOf(' ');
        if (space > 0)
        {
            string tail = text[(space + 1)..].Trim();
            if (tail.Length > 0 && tail[0] is '"' or '\'')
            {
                cardinality ??= tail.Trim('"', '\'');
                text = text[..space].Trim();
            }
        }

        return (CleanName(text), cardinality);
    }
}
