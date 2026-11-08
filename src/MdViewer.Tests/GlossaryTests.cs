using MdViewer.App;
using MdViewer.Core.Markdown;
using MdViewer.Render;
using MdViewer.Render.Layout;
using MdViewer.Render.Theming;

namespace MdViewer.Tests;

/// <summary>Glossary hover hit-testing on the real document view (no window shown).</summary>
public static class GlossaryTests
{
    public static void Run()
    {
        Harness.Suite("glossary hover");

        const string markdown = """
            The CSP header is set by the browser, and the layout pass measures text.

            | Term | Definition |
            | --- | --- |
            | CSP (Content Security Policy) | A browser policy header that restricts sources. |
            | Layout | Measured geometry of every block. |
            """;

        Harness.Test("hovering a term finds its definition", () =>
        {
            var document = new MarkdownParser().Parse(markdown);
            using var renderer = new MarkdownRenderer(ThemeCatalog.Light, 1f);
            renderer.SetDocument(document);
            using var view = new DocumentView { Renderer = renderer };
            view.SetGlossary(document.Glossary);

            DocumentLayout layout = renderer.EnsureLayout(600f)!;
            var paragraph = (TextVisual)layout.Blocks[0];

            string? found = null;
            float y = paragraph.Top + 2f;
            for (float x = paragraph.Left; x < paragraph.Left + paragraph.Width && found is null; x += 2f)
            {
                found = view.ProbeGlossary(new PointF(x, y));
            }

            Harness.True(found is not null, "a tooltip was found on the paragraph line");
            Harness.Contains(found ?? "", "browser policy header", "definition text");
        });

        Harness.Test("empty space has no tooltip", () =>
        {
            var document = new MarkdownParser().Parse(markdown);
            using var renderer = new MarkdownRenderer(ThemeCatalog.Light, 1f);
            renderer.SetDocument(document);
            using var view = new DocumentView { Renderer = renderer };
            view.SetGlossary(document.Glossary);
            DocumentLayout layout = renderer.EnsureLayout(600f)!;
            var paragraph = (TextVisual)layout.Blocks[0];

            // far right of the first line is past the end of the text
            string? found = view.ProbeGlossary(new PointF(paragraph.Left + paragraph.Width - 1f, paragraph.Top + 2f));
            Harness.True(found is null, "no tooltip past the text");
        });

        Harness.Test("multi-word alias resolves both forms", () =>
        {
            var document = new MarkdownParser().Parse(markdown);
            Harness.Equal(document.Glossary["Content Security Policy"], document.Glossary["CSP"], "same definition");
        });
    }
}
