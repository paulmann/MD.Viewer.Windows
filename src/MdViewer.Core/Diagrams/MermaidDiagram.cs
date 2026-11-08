namespace MdViewer.Core.Diagrams;

/// <summary>Root of every supported Mermaid diagram model.</summary>
public closed class MermaidDiagram;

public sealed class FlowchartDiagram(Flowchart chart, bool isState) : MermaidDiagram
{
    public Flowchart Chart { get; } = chart;

    /// <summary>State diagrams reuse the flowchart engine with different node shapes.</summary>
    public bool IsState { get; } = isState;
}

public sealed class SequenceDiagram : MermaidDiagram
{
    public List<SequenceActor> Actors { get; } = [];

    public List<SequenceStep> Steps { get; } = [];

    public bool AutoNumber { get; set; }

    public string? Title { get; set; }

    public SequenceActor Actor(string id)
    {
        SequenceActor? existing = Actors.FirstOrDefault(a => a.Id == id);
        if (existing is not null)
        {
            return existing;
        }

        var created = new SequenceActor(id, id, false);
        Actors.Add(created);
        return created;
    }

    public SequenceActor Ensure(string id, string? label, bool actor)
    {
        SequenceActor? existing = Actors.FirstOrDefault(a => a.Id == id);
        if (existing is not null)
        {
            if (label is not null && existing.Label == existing.Id)
            {
                existing.Label = label;
            }

            return existing;
        }

        var created = new SequenceActor(id, label ?? id, actor);
        Actors.Add(created);
        return created;
    }
}

public sealed class SequenceActor(string id, string label, bool isActor)
{
    public string Id { get; } = id;

    public string Label { get; set; } = label;

    public bool IsActor { get; } = isActor;
}

public closed class SequenceStep;

public sealed class SequenceMessageStep(string from, string to, string text, bool dashed, bool openArrow, int number) : SequenceStep
{
    public string From { get; } = from;

    public string To { get; } = to;

    public string Text { get; } = text;

    public bool Dashed { get; } = dashed;

    /// <summary>Open arrow (async) instead of a filled head.</summary>
    public bool OpenArrow { get; } = openArrow;

    public int Number { get; } = number;
}

public sealed class SequenceNoteStep(string placement, string actorA, string? actorB, string text) : SequenceStep
{
    /// <summary><c>left</c>, <c>right</c> or <c>over</c>.</summary>
    public string Placement { get; } = placement;

    public string ActorA { get; } = actorA;

    public string? ActorB { get; } = actorB;

    public string Text { get; } = text;
}

public sealed class SequenceFrameStep(string kind, string label, bool isEnd, bool isElse) : SequenceStep
{
    public string Kind { get; } = kind;

    public string Label { get; } = label;

    public bool IsEnd { get; } = isEnd;

    public bool IsElse { get; } = isElse;
}

public sealed class SequenceDividerStep(string text) : SequenceStep
{
    public string Text { get; } = text;
}

public sealed class PieDiagram : MermaidDiagram
{
    public string? Title { get; set; }

    public bool ShowData { get; set; }

    public List<PieSlice> Slices { get; } = [];
}

public sealed class PieSlice(string label, double value)
{
    public string Label { get; } = label;

    public double Value { get; } = value;
}

public sealed class ClassDiagram : MermaidDiagram
{
    public List<ClassNode> Classes { get; } = [];

    public List<ClassRelation> Relations { get; } = [];

    public ClassNode Node(string name)
    {
        ClassNode? existing = Classes.FirstOrDefault(c => c.Name == name);
        if (existing is not null)
        {
            return existing;
        }

        var created = new ClassNode(name);
        Classes.Add(created);
        return created;
    }
}

public sealed class ClassNode(string name)
{
    public string Name { get; } = name;

    public List<ClassMember> Members { get; } = [];

    public string? Annotation { get; set; }
}

public sealed class ClassMember(string text, string visibility)
{
    public string Text { get; } = text;

    /// <summary><c>+</c>, <c>-</c>, <c>#</c>, <c>~</c> or empty.</summary>
    public string Visibility { get; } = visibility;

    public bool IsMethod => Text.Contains('(');

    public override string ToString() => Visibility + Text;
}

public sealed class ClassRelation(string from, string to, RelationKind kind, string? label, string? fromCardinality, string? toCardinality)
{
    public string From { get; } = from;

    public string To { get; } = to;

    public RelationKind Kind { get; } = kind;

    public string? Label { get; } = label;

    public string? FromCardinality { get; } = fromCardinality;

    public string? ToCardinality { get; } = toCardinality;
}

public enum RelationKind
{
    Association,
    Inheritance,
    Composition,
    Aggregation,
    Dependency,
    Realization,
}

public sealed class GanttDiagram : MermaidDiagram
{
    public string? Title { get; set; }

    public string DateFormat { get; set; } = "YYYY-MM-DD";

    public List<GanttSection> Sections { get; } = [];

    public GanttSection Section(string? name)
    {
        GanttSection? existing = string.IsNullOrEmpty(name) ? Sections.FirstOrDefault(s => s.Name is null) : Sections.FirstOrDefault(s => s.Name == name);
        if (existing is not null)
        {
            return existing;
        }

        var created = new GanttSection(name);
        Sections.Add(created);
        return created;
    }
}

public sealed class GanttSection(string? name)
{
    public string? Name { get; } = name;

    public List<GanttTask> Tasks { get; } = [];
}

public sealed class GanttTask(string name, string id, DateTime start, DateTime end, bool milestone, bool done)
{
    public string Name { get; } = name;

    public string Id { get; } = id;

    public DateTime Start { get; } = start;

    public DateTime End { get; } = end;

    public bool Milestone { get; } = milestone;

    public bool Done { get; } = done;
}

public sealed class JourneyDiagram : MermaidDiagram
{
    public string? Title { get; set; }

    public List<JourneySection> Sections { get; } = [];

    public JourneySection Section(string name)
    {
        JourneySection? existing = Sections.FirstOrDefault(s => s.Name == name);
        if (existing is not null)
        {
            return existing;
        }

        var created = new JourneySection(name);
        Sections.Add(created);
        return created;
    }
}

public sealed class JourneySection(string name)
{
    public string Name { get; } = name;

    public List<JourneyTask> Tasks { get; } = [];
}

public sealed class JourneyTask(string name, int score, string actors)
{
    public string Name { get; } = name;

    public int Score { get; } = Math.Clamp(score, 1, 5);

    public string Actors { get; } = actors;
}

/// <summary>Chooses a parser by the diagram header and reports why others are unsupported.</summary>
public static class MermaidDiagrams
{
    private static readonly string[] KnownUnsupported =
    [
        "erDiagram", "gitGraph", "mindmap", "timeline", "quadrantChart", "sankey-beta", "xychart-beta", "C4Context", "block-beta",
    ];

    public static bool TryParse(string source, out MermaidDiagram? diagram, out string? error)
    {
        diagram = null;
        error = null;

        string[] lines = source.Replace("\r\n", "\n").Split('\n');
        string? header = null;
        foreach (string raw in lines)
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("%%", StringComparison.Ordinal))
            {
                continue;
            }

            header = line;
            break;
        }

        if (header is null)
        {
            error = "empty diagram";
            return false;
        }

        string keyword = header.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries)[0].ToLowerInvariant();

        switch (keyword)
        {
            case "graph" or "flowchart":
                if (MermaidFlowchart.TryParse(source, out Flowchart? chart, out error) && chart is not null)
                {
                    diagram = new FlowchartDiagram(chart, isState: false);
                    return true;
                }

                return false;

            case "statediagram" or "statediagram-v2" or "state":
                if (MermaidState.TryParse(source, out Flowchart? states, out error) && states is not null)
                {
                    diagram = new FlowchartDiagram(states, isState: true);
                    return true;
                }

                return false;

            case "sequencediagram":
                if (MermaidSequence.TryParse(source, out SequenceDiagram? sequence, out error) && sequence is not null)
                {
                    diagram = sequence;
                    return true;
                }

                return false;

            case "pie":
                if (MermaidPie.TryParse(source, out PieDiagram? pie, out error) && pie is not null)
                {
                    diagram = pie;
                    return true;
                }

                return false;

            case "classdiagram":
                if (MermaidClass.TryParse(source, out ClassDiagram? classes, out error) && classes is not null)
                {
                    diagram = classes;
                    return true;
                }

                return false;

            case "gantt":
                if (MermaidGantt.TryParse(source, out GanttDiagram? gantt, out error) && gantt is not null)
                {
                    diagram = gantt;
                    return true;
                }

                return false;

            case "journey":
                if (MermaidJourney.TryParse(source, out JourneyDiagram? journey, out error) && journey is not null)
                {
                    diagram = journey;
                    return true;
                }

                return false;
        }

        foreach (string unsupported in KnownUnsupported)
        {
            if (header.StartsWith(unsupported, StringComparison.OrdinalIgnoreCase))
            {
                error = $"{unsupported} is not supported yet";
                return false;
            }
        }

        error = $"unknown diagram type: {keyword}";
        return false;
    }

    /// <summary>Mermaid text decorations: <c>&lt;br&gt;</c> and quoted labels.</summary>
    public static string Clean(string raw)
    {
        string text = raw.Trim();
        if (text.Length >= 2 && text[0] == '"' && text[^1] == '"')
        {
            text = text[1..^1];
        }

        return text
            .Replace("<br/>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("<br>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("\\n", "\n", StringComparison.Ordinal)
            .Trim();
    }
}
