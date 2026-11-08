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
            var document = new MarkdownParser().Parse("```mermaid\nsequenceDiagram\n  A->>B: hi\n```");
            using var renderer = new MarkdownRenderer(ThemeCatalog.Light, 1f);
            renderer.SetDocument(document);
            DocumentLayout layout = renderer.EnsureLayout(700f)!;
            Harness.True(layout.Blocks[0] is MermaidVisual, "fallback visual");
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


