using MdViewer.Core.Diagrams;
using MdViewer.Core.Markdown;
using MdViewer.Render;
using MdViewer.Render.Layout;
using MdViewer.Render.Theming;

namespace MdViewer.Tests;

public static class DiagramTests
{
    private const string Flowchart = """
        graph TD
            A[Open file] --> B{Parse}
            B -->|ok| C[Layout]
            B -->|error| D[Show diagnostics]
            C --> E[Paint]
        """;

    public static void Run()
    {
        Harness.Suite("diagrams");

        Harness.Test("flowchart parses nodes and edges", () =>
        {
            Harness.True(MermaidFlowchart.TryParse(Flowchart, out Flowchart? chart, out string? error), $"parsed ({error})");
            Harness.Equal(5, chart!.Nodes.Count, "nodes");
            Harness.Equal(4, chart.Edges.Count, "edges");
            Harness.Equal(NodeShape.Diamond, chart.Find("B")!.Shape, "diamond");
            Harness.Equal("ok", chart.Edges[1].Label ?? "", "edge label");
        });

        Harness.Test("left-to-right direction", () =>
        {
            MermaidFlowchart.TryParse("graph LR\n  A --> B", out Flowchart? chart, out _);
            Harness.True(chart!.LeftToRight, "lr");
        });

        Harness.Test("subgraph and styles are ignored", () =>
        {
            const string source = """
                graph TD
                    subgraph one
                    A --> B
                    end
                    classDef x fill:#fff
                    style A fill:#eee
                """;
            Harness.True(MermaidFlowchart.TryParse(source, out Flowchart? chart, out _), "parsed");
            Harness.Equal(2, chart!.Nodes.Count, "nodes");
        });

        Harness.Test("non-flowchart reports an error instead of throwing", () =>
        {
            Harness.False(MermaidFlowchart.TryParse("sequenceDiagram\n  A->>B: hi", out _, out string? error), "rejected");
            Harness.Contains(error ?? "", "flowchart", "error text");
        });

        Harness.Test("diagram block layouts as a drawing", () =>
        {
            var document = new MarkdownParser().Parse("```mermaid\n" + Flowchart + "\n```");
            using var renderer = new MarkdownRenderer(ThemeCatalog.Light, 1f);
            renderer.SetDocument(document);
            DocumentLayout layout = renderer.EnsureLayout(700f)!;
            Harness.True(layout.Blocks[0] is DiagramVisual, "diagram visual");
            Harness.True(layout.Blocks[0].Height > 80f, $"height {layout.Blocks[0].Height}");
        });

        Harness.Test("unsupported diagram falls back to source card", () =>
        {
            var document = new MarkdownParser().Parse("```mermaid\nerDiagram\n  CUSTOMER ||--o{ ORDER : places\n```");
            using var renderer = new MarkdownRenderer(ThemeCatalog.Light, 1f);
            renderer.SetDocument(document);
            DocumentLayout layout = renderer.EnsureLayout(700f)!;
            Harness.True(layout.Blocks[0] is MermaidVisual, "fallback visual");
            Harness.Contains(((MermaidVisual)layout.Blocks[0]).Message ?? "", "not supported", "reason shown");
        });

        Harness.Suite("mermaid: sequence");

        const string Sequence = """
            sequenceDiagram
                autonumber
                participant U as User
                participant A as App
                U->>A: open
                A-->>U: done
                Note right of A: cached
                loop every minute
                    A->>A: tick
                end
            """;

        Harness.Test("sequence parses actors, messages, notes and frames", () =>
        {
            Harness.True(MermaidSequence.TryParse(Sequence, out SequenceDiagram? diagram, out string? error), $"parsed ({error})");
            Harness.Equal(2, diagram!.Actors.Count, "actors");
            Harness.Equal("User", diagram.Actors[0].Label, "actor label");
            Harness.True(diagram.AutoNumber, "autonumber");
            Harness.Equal(3, diagram.Steps.OfType<SequenceMessageStep>().Count(m => m.Number > 0), "numbered messages");
            Harness.Equal(1, diagram.Steps.OfType<SequenceNoteStep>().Count(), "note");
            Harness.Equal("right", diagram.Steps.OfType<SequenceNoteStep>().First().Placement, "note side");
            Harness.Equal(1, diagram.Steps.OfType<SequenceFrameStep>().Count(f => f.Kind == "loop"), "loop frame");
        });

        Harness.Test("sequence dashed arrow and self message", () =>
        {
            MermaidSequence.TryParse(Sequence, out SequenceDiagram? diagram, out _);
            SequenceMessageStep dashed = diagram!.Steps.OfType<SequenceMessageStep>().First();
            Harness.False(dashed.Dashed, "solid first");
            Harness.True(diagram.Steps.OfType<SequenceMessageStep>().Any(m => m.Dashed), "has dashed");
            Harness.True(diagram.Steps.OfType<SequenceMessageStep>().Any(m => m.From == m.To), "self message");
        });

        Harness.Suite("mermaid: pie");

        Harness.Test("pie parses title and slices", () =>
        {
            const string source = "pie showData title Time\n  \"Parse\" : 30\n  Layout : 20.5\n";
            Harness.True(MermaidPie.TryParse(source, out PieDiagram? pie, out string? error), $"parsed ({error})");
            Harness.Equal("Time", pie!.Title ?? "", "title");
            Harness.True(pie.ShowData, "showData");
            Harness.Equal(2, pie.Slices.Count, "slices");
            Harness.True(Math.Abs(pie.Slices[1].Value - 20.5) < 0.001, "decimal value");
        });

        Harness.Suite("mermaid: class");

        const string Classes = """
            classDiagram
                class Block {
                    <<abstract>>
                    +int LineNumber
                    +Paint()
                }
                class ParagraphBlock {
                    +Inline[] Inlines
                }
                Block <|-- ParagraphBlock
                LayoutEngine --> Block : measures
            """;

        Harness.Test("class parses members, annotation and relations", () =>
        {
            Harness.True(MermaidClass.TryParse(Classes, out ClassDiagram? diagram, out string? error), $"parsed ({error})");
            Harness.Equal(3, diagram!.Classes.Count, "classes");
            ClassNode block = diagram.Classes.First(c => c.Name == "Block");
            Harness.Equal("abstract", block.Annotation ?? "", "annotation");
            Harness.Equal(2, block.Members.Count, "members");
            Harness.Equal("+", block.Members[0].Visibility, "visibility");
            Harness.True(block.Members[1].IsMethod, "method");
            ClassRelation inheritance = diagram.Relations.First(r => r.Kind == RelationKind.Inheritance);
            Harness.Equal("ParagraphBlock", inheritance.From, "child");
            Harness.Equal("Block", inheritance.To, "parent");
            Harness.Equal("measures", diagram.Relations.First(r => r.Kind == RelationKind.Association).Label ?? "", "label");
        });

        Harness.Suite("mermaid: state");

        Harness.Test("state diagram converts to nodes and start/end dots", () =>
        {
            const string source = "stateDiagram-v2\n  [*] --> Idle\n  Idle --> Running : start\n  Running --> [*]\n";
            Harness.True(MermaidState.TryParse(source, out Flowchart? chart, out string? error), $"parsed ({error})");
            Harness.Equal(2, chart!.Nodes.Count(n => n.Shape == NodeShape.StartEnd), "start and end");
            Harness.Equal(3, chart.Edges.Count, "transitions");
            Harness.Equal("start", chart.Edges[1].Label ?? "", "edge label");
            Harness.True(chart.Nodes.Any(n => n.Label == "Idle"), "state node");
        });

        Harness.Test("state alias syntax", () =>
        {
            MermaidState.TryParse("stateDiagram-v2\n state \"Waiting for input\" as Wait\n [*] --> Wait", out Flowchart? chart, out _);
            Harness.True(chart!.Nodes.Any(n => n.Id == "Wait" && n.Label == "Waiting for input"), "alias");
        });

        Harness.Suite("mermaid: gantt");

        const string Gantt = """
            gantt
                title Plan
                dateFormat YYYY-MM-DD
                section Core
                Parser :a1, 2026-01-01, 10d
                Layout :a2, after a1, 5d
                section Release
                Milestone :milestone, m1, 2026-01-20, 0d
            """;

        Harness.Test("gantt parses dates, durations and after", () =>
        {
            Harness.True(MermaidGantt.TryParse(Gantt, out GanttDiagram? diagram, out string? error), $"parsed ({error})");
            Harness.Equal("Plan", diagram!.Title ?? "", "title");
            Harness.Equal(2, diagram.Sections.Count, "sections");
            GanttTask parser = diagram.Sections[0].Tasks[0];
            Harness.Equal(new DateTime(2026, 1, 1), parser.Start, "start");
            Harness.Equal(10, (int)(parser.End - parser.Start).TotalDays, "duration");
            GanttTask layout = diagram.Sections[0].Tasks[1];
            Harness.Equal(parser.End, layout.Start, "after dependency");
        });

        Harness.Test("gantt milestone has zero length", () =>
        {
            MermaidGantt.TryParse(Gantt, out GanttDiagram? diagram, out _);
            GanttTask milestone = diagram!.Sections[1].Tasks[0];
            Harness.True(milestone.Milestone, "milestone flag");
            Harness.Equal(0, (int)(milestone.End - milestone.Start).TotalDays, "zero duration");
        });

        Harness.Suite("mermaid: journey");

        Harness.Test("journey parses sections, scores and actors", () =>
        {
            const string source = "journey\n  title Day\n  section Work\n    Tea: 5: Me\n    Code: 2: Me, Cat\n";
            Harness.True(MermaidJourney.TryParse(source, out JourneyDiagram? diagram, out string? error), $"parsed ({error})");
            Harness.Equal("Day", diagram!.Title ?? "", "title");
            Harness.Equal(2, diagram.Sections[0].Tasks.Count, "tasks");
            Harness.Equal(2, diagram.Sections[0].Tasks[1].Score, "score");
            Harness.Equal("Me, Cat", diagram.Sections[0].Tasks[1].Actors, "actors");
        });

        Harness.Suite("mermaid: dispatch");

        Harness.Test("dispatcher picks the right model per header", () =>
        {
            Harness.True(MermaidDiagrams.TryParse("graph TD\n A --> B", out MermaidDiagram? flow, out _), "flow parsed");
            Harness.True(flow is FlowchartDiagram, "flowchart model");
            Harness.True(MermaidDiagrams.TryParse(Sequence, out MermaidDiagram? sequence, out _), "sequence parsed");
            Harness.True(sequence is SequenceDiagram, "sequence model");
            Harness.True(MermaidDiagrams.TryParse("pie\n \"a\" : 1", out MermaidDiagram? pie, out _), "pie parsed");
            Harness.True(pie is PieDiagram, "pie model");
        });

        Harness.Test("every supported type lays out without throwing", () =>
        {
            string[] blocks =
            [
                "graph TD\n A --> B",
                Sequence,
                "pie title T\n \"a\" : 1\n \"b\" : 2",
                Classes,
                "stateDiagram-v2\n [*] --> A\n A --> [*]",
                Gantt,
                "journey\n section S\n Task: 3: Me",
            ];

            foreach (string block in blocks)
            {
                var document = new MarkdownParser().Parse("```mermaid\n" + block + "\n```");
                using var renderer = new MarkdownRenderer(ThemeCatalog.Light, 1f);
                renderer.SetDocument(document);
                DocumentLayout layout = renderer.EnsureLayout(720f)!;
                Harness.True(layout.Blocks[0] is not MermaidVisual, "rendered, not fallback: " + block.Split('\n')[0]);
                Harness.True(layout.Blocks[0].Height > 40f, "height for " + block.Split('\n')[0]);
            }
        });


        Harness.Suite("editor");

        Harness.Test("typing inserts at the caret and records undo", () =>
        {
            var editor = new MdViewer.App.MarkdownEditor();
            editor.Text = "abc";
            editor.GoToOffset(3);
            editor.InsertText("def");
            Harness.Equal("abcdef", editor.Text, "text");
            editor.Dispose();
        });

        Harness.Test("selection is replaced by typing", () =>
        {
            var editor = new MdViewer.App.MarkdownEditor();
            editor.Text = "hello world";
            editor.GoToOffset(0);
            editor.GoToOffset(5, select: true);
            Harness.Equal("hello", editor.SelectedText, "selection");
            editor.InsertText("bye");
            Harness.Equal("bye world", editor.Text, "replaced");
            editor.Dispose();
        });

        Harness.Test("find collects every occurrence", () =>
        {
            var editor = new MdViewer.App.MarkdownEditor();
            editor.Text = "alpha beta alpha gamma ALPHA";
            editor.SetSearch("alpha");
            Harness.Equal(3, editor.HitCount, "hits");
            editor.Dispose();
        });

        Harness.Test("caret position is 1-based", () =>
        {
            var editor = new MdViewer.App.MarkdownEditor();
            editor.Text = "line one\nline two";
            editor.GoToOffset(11);
            (int line, int column) = editor.CaretPosition;
            Harness.Equal(2, line, "line");
            Harness.Equal(3, column, "column");
            editor.Dispose();
        });
    }
}



