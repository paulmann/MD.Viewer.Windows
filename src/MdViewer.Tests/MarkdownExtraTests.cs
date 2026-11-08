using MdViewer.Core.Markdown;
using MdViewer.Render;
using MdViewer.Render.Theming;

namespace MdViewer.Tests;

/// <summary>CommonMark/GFM corners that the first release got wrong or skipped.</summary>
public static class MarkdownExtraTests
{
    private static MarkdownDocument Parse(string markdown, MarkdownOptions? options = null) =>
        new MarkdownParser().Parse(markdown, options);

    private static string Inl(string markdown, MarkdownOptions? options = null)
    {
        MarkdownDocument document = Parse(markdown, options);
        return document.Blocks is [ParagraphBlock paragraph] ? AstDump.Inline(paragraph.Inlines) : string.Empty;
    }

    public static void Run()
    {
        Harness.Suite("emphasis edge cases");

        Harness.Test("emphasis wrapping strong", () =>
            Harness.Equal("*a **b** c*", Inl("*a **b** c*"), "nested strong in em"));

        Harness.Test("strong wrapping emphasis", () =>
            Harness.Equal("**a *b* c**", Inl("**a *b* c**"), "nested em in strong"));

        Harness.Test("underscore emphasis", () =>
            Harness.Equal("*a* **b**", Inl("_a_ __b__"), "underscore forms"));

        Harness.Test("intraword underscore stays literal", () =>
            Harness.Equal("snake_case_name", Inl("snake_case_name"), "intraword"));

        Harness.Test("adjacent italic and bold", () =>
            Harness.Equal("*a* **b**", Inl("*a* **b**"), "adjacent"));

        Harness.Test("unmatched marker stays literal", () =>
            Harness.Equal("a * b", Inl("a * b"), "literal star"));

        Harness.Suite("link references");

        Harness.Test("definition indented by up to three spaces", () =>
        {
            MarkdownDocument document = Parse("see [docs]\n\n   [docs]: https://e.com/d");
            var link = (LinkInline)((ParagraphBlock)document.Blocks[0]).Inlines[1];
            Harness.Equal("https://e.com/d", link.Url, "indented definition");
        });

        Harness.Test("title on the next line", () =>
        {
            MarkdownDocument document = Parse("[a](x) and [docs][d]\n\n[d]: https://e.com/d\n  \"Title here\"");
            SequenceProbe(document, out LinkInline reference);
            Harness.Equal("Title here", reference.Title ?? "", "next-line title");
        });

        Harness.Suite("definition lists");

        Harness.Test("several terms share the definitions", () =>
        {
            MarkdownDocument document = Parse("Term one\nTerm two\n: shared definition");
            var list = (DefinitionListBlock)document.Blocks[0];
            Harness.Equal(2, list.Items.Length, "terms");
            Harness.Equal("Term one", list.Items[0].Term, "first term");
            Harness.Equal("Term two", list.Items[1].Term, "second term");
            Harness.Equal("shared definition", string.Join(' ', list.Items[1].Definitions[0]), "shared definition");
        });

        Harness.Test("two definitions for one term", () =>
        {
            MarkdownDocument document = Parse("Term\n: first\n: second");
            var list = (DefinitionListBlock)document.Blocks[0];
            Harness.Equal(2, list.Items[0].Definitions.Length, "definitions");
        });

        Harness.Suite("typography");

        Harness.Test("smart punctuation is off by default", () =>
            Harness.Equal("a -- b ... c", Inl("a -- b ... c"), "no replacement"));

        Harness.Test("smart punctuation replaces dashes and ellipsis", () =>
        {
            var options = new MarkdownOptions { SmartPunctuation = true };
            Harness.Equal("a – b — c … d", Inl("a -- b --- c ... d", options), "replaced");
        });

        Harness.Test("smart punctuation keeps code spans intact", () =>
        {
            var options = new MarkdownOptions { SmartPunctuation = true };
            Harness.Equal("`--flag` – text", Inl("`--flag` -- text", options), "code untouched");
        });

        Harness.Suite("line breaks");

        Harness.Test("single newline breaks by default (legacy)", () =>
            Harness.Equal("a<br>b", Inl("a\nb"), "break"));

        Harness.Test("single newline flows when disabled", () =>
        {
            var options = new MarkdownOptions { DoubleLineBreaks = false };
            Harness.Equal("a b", Inl("a\nb", options), "flow");
        });

        Harness.Test("two trailing spaces always break", () =>
        {
            var options = new MarkdownOptions { DoubleLineBreaks = false };
            Harness.Equal("a<br>b", Inl("a  \nb", options), "hard break");
        });

        Harness.Suite("glossary");

        Harness.Test("two-column table feeds the glossary", () =>
        {
            MarkdownDocument document = Parse("| Term | Definition |\n|---|---|\n| CSP (Content Security Policy) | A browser policy header. |\n| Layout | Measured geometry. |");
            Harness.Equal(4, document.Glossary.Count, "term, alias, long form, layout");
            Harness.Equal("A browser policy header.", document.Glossary["CSP"], "abbreviation");
            Harness.Equal("A browser policy header.", document.Glossary["Content Security Policy"], "long form");
            Harness.Equal("Measured geometry.", document.Glossary["layout"], "case-insensitive lookup");
        });

        Harness.Test("three-column tables are not glossaries", () =>
        {
            MarkdownDocument document = Parse("| a | b | c |\n|---|---|---|\n| 1 | 2 | 3 |");
            Harness.Equal(0, document.Glossary.Count, "no glossary");
        });

        Harness.Test("glossary text is plain, not markup", () =>
        {
            MarkdownDocument document = Parse("| Term | Definition |\n|---|---|\n| Latency | **Measured** in ms with `stopwatch`. |");
            Harness.Equal("Measured in ms with stopwatch.", document.Glossary["Latency"], "plain text");
        });

        Harness.Suite("images");

        Harness.Test("image region carries the resolved path", () =>
        {
            string directory = Path.Combine(Path.GetTempPath(), "mdview-img-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            string file = Path.Combine(directory, "pic.png");
            using (var bitmap = new System.Drawing.Bitmap(8, 8))
            {
                bitmap.Save(file, System.Drawing.Imaging.ImageFormat.Png);
            }

            try
            {
                using var renderer = new MarkdownRenderer(ThemeCatalog.Light, 1f);
                renderer.SetDocument(new MarkdownParser().Parse("![alt](pic.png)", MarkdownOptions.Default, Path.Combine(directory, "doc.md")));
                var layout = renderer.EnsureLayout(600f)!;
                Harness.True(layout.Links.Any(l => l.Target.StartsWith("file:", StringComparison.Ordinal) && l.Target.EndsWith("pic.png", StringComparison.OrdinalIgnoreCase)), "file region");
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        });
    }

    private static void SequenceProbe(MarkdownDocument document, out LinkInline link)
    {
        var paragraph = (ParagraphBlock)document.Blocks[0];
        link = paragraph.Inlines.OfType<LinkInline>().Last();
    }
}

