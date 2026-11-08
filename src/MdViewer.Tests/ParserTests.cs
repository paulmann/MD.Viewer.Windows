using MdViewer.Core.Markdown;

namespace MdViewer.Tests;

public static class ParserTests
{
    private static string Dump(string markdown, MarkdownOptions? options = null) =>
        AstDump.Blocks(new MarkdownParser().Parse(markdown, options).Blocks);

    private static MarkdownDocument Parse(string markdown, MarkdownOptions? options = null) =>
        new MarkdownParser().Parse(markdown, options);

    private static string Inl(string markdown) => AstDump.Inline(Parse(markdown).Blocks is [ParagraphBlock p] ? p.Inlines : []);

    public static void Run()
    {
        Harness.Suite("headings");

        Harness.Test("atx heading level and slug", () =>
            Harness.Equal("h2 #introduction: Introduction", Dump("## Introduction"), "dump"));

        Harness.Test("atx heading slug", () =>
            Harness.Equal("h1 #hello-world: Hello, World!", Dump("# Hello, World!"), "slug"));

        Harness.Test("atx trailing hashes", () =>
            Harness.Equal("h1 #title: Title", Dump("# Title ###"), "trailing"));

        Harness.Test("setext heading", () =>
            Harness.Equal("h1 #title: Title", Dump("Title\n====="), "setext h1"));

        Harness.Test("setext h2", () =>
            Harness.Equal("h2 #subtitle: Subtitle", Dump("Subtitle\n-----"), "setext h2"));

        Harness.Test("explicit heading id", () =>
        {
            var doc = Parse("## Custom {#my-id}\n");
            Harness.Equal("my-id", doc.Outline[0].Id, "explicit id");
            Harness.Equal("Custom", doc.Outline[0].Text, "explicit id text");
        });

        Harness.Test("duplicate slugs are numbered", () =>
        {
            var doc = Parse("# A\n\n# A\n\n# A\n");
            Harness.Equal("a, a-1, a-2", string.Join(", ", doc.Outline.Select(h => h.Id)), "slug dedupe");
        });

        Harness.Test("heading numbering", () =>
        {
            var doc = Parse("# One\n\n## Two\n\n## Three\n\n### Deep\n", new MarkdownOptions { HeadingNumbering = true });
            Harness.Equal("1, 1.1, 1.2, 1.2.1", string.Join(", ", doc.Outline.Select(h => h.Number)), "numbers");
        });

        Harness.Suite("inline");

        Harness.Test("emphasis nesting", () =>
            Harness.Equal("**bold *it* end**", Inl("**bold *it* end**").Replace("**bold ", "**bold "), "nested"));

        Harness.Test("bold then italic", () =>
            Harness.Equal("*a* **b**", Inl("*a* **b**"), "adjacent"));

        Harness.Test("strong+emphasis triple", () =>
            Harness.Equal("***both***", Inl("***both***"), "triple"));

        Harness.Test("code span protects asterisks", () =>
            Harness.Equal("a `*b*` c", Inl("a `*b*` c"), "code span"));

        Harness.Test("code span with backticks inside", () =>
            Harness.Equal("`x`", Inl("``x``"), "double backtick"));

        Harness.Test("strikethrough", () =>
            Harness.Equal("~~gone~~", Inl("~~gone~~"), "strike"));

        Harness.Test("mark", () =>
            Harness.Equal("==hi==", Inl("==hi=="), "mark"));

        Harness.Test("superscript/subscript", () =>
            Harness.Equal("^2^ H~2~O", Inl("^2^ H~2~O"), "sup sub"));

        Harness.Test("inline code keeps spaces", () =>
            Harness.Equal("`a  b`", Inl("`a  b`"), "code spaces"));

        Harness.Test("escaped asterisk", () =>
            Harness.Equal("*not emphasis*", Inl("\\*not emphasis\\*"), "escape"));

        Harness.Test("entity decoding", () =>
            Harness.Equal("A & B < C ©", Inl("A &amp; B &lt; C &copy;"), "entities"));

        Harness.Test("hard break by double space", () =>
            Harness.Equal("a<br>b", Inl("a  \nb"), "hard break"));

        Harness.Test("single newline breaks the line (MD.Viewer legacy default)", () =>
            Harness.Equal("a<br>b", Inl("a\nb"), "soft break legacy"));

        Harness.Test("classic paragraph flow when DoubleLineBreaks is off", () =>
        {
            var options = new MarkdownOptions { DoubleLineBreaks = false };
            var paragraph = (ParagraphBlock)Parse("a\nb", options).Blocks[0];
            Harness.Equal("a b", AstDump.Inline(paragraph.Inlines), "soft break classic");
        });

        Harness.Suite("links");

        Harness.Test("inline link with title", () =>
        {
            var link = (LinkInline)((ParagraphBlock)Parse("[a](https://x.io \"t\")").Blocks[0]).Inlines[0];
            Harness.Equal("https://x.io", link.Url, "url");
            Harness.Equal("t", link.Title ?? "", "title");
            Harness.Equal("a", AstDump.Inline(link.Children), "text");
        });

        Harness.Test("reference link", () =>
        {
            var doc = Parse("[docs][ref]\n\n[ref]: https://e.com/d \"T\"");
            var link = (LinkInline)((ParagraphBlock)doc.Blocks[0]).Inlines[0];
            Harness.Equal("https://e.com/d", link.Url, "reference url");
            Harness.Equal("T", link.Title ?? "", "reference title");
        });

        Harness.Test("shortcut reference", () =>
        {
            var doc = Parse("[docs]\n\n[docs]: https://e.com/d");
            var link = (LinkInline)((ParagraphBlock)doc.Blocks[0]).Inlines[0];
            Harness.Equal("https://e.com/d", link.Url, "shortcut url");
        });

        Harness.Test("autolink angle", () =>
            Harness.Equal("[https://a.b](https://a.b)", Inl("<https://a.b>"), "autolink"));

        Harness.Test("bare url", () =>
            Harness.Equal("see [https://a.b/c](https://a.b/c).", Inl("see https://a.b/c."), "bare url"));

        Harness.Test("javascript url is neutralised", () =>
        {
            var link = (LinkInline)((ParagraphBlock)Parse("[x](javascript:alert(1))").Blocks[0]).Inlines[0];
            Harness.Equal(string.Empty, link.Url, "blocked");
        });

        Harness.Test("unsafe link keeps text", () =>
            Harness.Equal("[click]()", Inl("[click](javascript:void(0))"), "text kept"));

        Harness.Suite("images");

        Harness.Test("image with width attribute", () =>
        {
            var doc = Parse("![alt](pic.png \"t\"){width=320}");
            var image = (ImageBlock)doc.Blocks[0];
            Harness.Equal("pic.png", image.Url, "url");
            Harness.Equal(320, image.Width ?? 0, "width");
            Harness.Equal("alt", image.Alt, "alt");
        });

        Harness.Test("inline image stays inline among text", () =>
        {
            string dump = Dump("text ![a](b.png) tail");
            Harness.Equal("p: text ![a](b.png) tail", dump, "inline image");
        });

        Harness.Suite("code blocks");

        Harness.Test("fenced code with language", () =>
            Harness.Equal("code(csharp): var x = 1;\\nvar y = 2;", Dump("```csharp\nvar x = 1;\nvar y = 2;\n```"), "fence"));

        Harness.Test("fence with title", () =>
        {
            var code = (CodeBlock)Parse("```php title=\"src/Demo.php\"\n<?php\n```").Blocks[0];
            Harness.Equal("php", code.Language ?? "", "language");
            Harness.Equal("src/Demo.php", code.Title ?? "", "title");
        });

        Harness.Test("tilde fence", () =>
            Harness.Equal("code(-): a", Dump("~~~\na\n~~~"), "tilde"));

        Harness.Test("indented code", () =>
            Harness.Equal("code(-): x = 1", Dump("    x = 1"), "indented"));

        Harness.Test("mermaid fence becomes diagram", () =>
            Harness.Equal("mermaid: graph TD\\n  A --> B", Dump("```mermaid\ngraph TD\n  A --> B\n```"), "mermaid"));

        Harness.Suite("lists");

        Harness.Test("unordered list", () =>
            Harness.Equal("list(ordered=False,start=1,tight=True)\n  item(None)\n    p: a\n  item(None)\n    p: b", Dump("- a\n- b"), "ul"));

        Harness.Test("ordered list start", () =>
        {
            var list = (ListBlock)Parse("3. a\n4. b").Blocks[0];
            Harness.Equal(3, list.Start, "start");
            Harness.Equal(2, list.Items.Length, "items");
        });

        Harness.Test("nested list", () =>
        {
            string dump = Dump("- a\n  - b\n  - c\n- d");
            Harness.Contains(dump, "list(ordered=False,start=1,tight=True)", "outer");
            Harness.Equal(2, dump.Split("list(").Length - 1, "outer + inner list");
        });

        Harness.Test("task list", () =>
        {
            var list = (ListBlock)Parse("- [ ] todo\n- [x] done").Blocks[0];
            Harness.Equal(TaskState.Todo, list.Items[0].Task, "todo");
            Harness.Equal(TaskState.Done, list.Items[1].Task, "done");
        });

        Harness.Test("loose list", () =>
        {
            var list = (ListBlock)Parse("- a\n\n- b").Blocks[0];
            Harness.False(list.Tight, "loose");
        });

        Harness.Suite("tables");

        Harness.Test("table with alignment", () =>
        {
            var table = (TableBlock)Parse("| a | b | c |\n|:--|:-:|--:|\n| 1 | 2 | 3 |").Blocks[0];
            Harness.Equal("Left,Center,Right", string.Join(',', table.Aligns), "aligns");
            Harness.Equal("a|b|c", string.Join('|', table.Headers), "headers");
            Harness.Equal(1, table.Rows.Length, "rows");
            Harness.Equal("3", table.Rows[0][2], "cell");
        });

        Harness.Test("escaped pipe in cell", () =>
        {
            var table = (TableBlock)Parse("| a | b |\n|---|---|\n| x \\| y | z |").Blocks[0];
            Harness.Equal("x | y", table.Rows[0][0], "escaped pipe");
        });

        Harness.Test("two column table flags glossary", () =>
        {
            var table = (TableBlock)Parse("| Term | Definition |\n|---|---|\n| a | b |").Blocks[0];
            Harness.True(table.IsGlossary, "glossary");
        });

        Harness.Suite("quotes and alerts");

        Harness.Test("blockquote", () =>
            Harness.Equal("quote\n  p: hello", Dump("> hello"), "quote"));

        Harness.Test("alert NOTE", () =>
        {
            var alert = (AlertBlock)Parse("> [!NOTE]\n> Careful.").Blocks[0];
            Harness.Equal(AlertKind.Note, alert.Kind, "kind");
            Harness.Equal("p: Careful.", AstDump.Blocks(alert.Children), "body");
        });

        Harness.Test("alert WARNING", () =>
        {
            var alert = (AlertBlock)Parse("> [!WARNING]\n> Boom").Blocks[0];
            Harness.Equal(AlertKind.Warning, alert.Kind, "kind");
        });

        Harness.Test("plain quote is not an alert", () =>
        {
            var block = Parse("> just text").Blocks[0];
            Harness.True(block is QuoteBlock, "quote");
        });

        Harness.Suite("extended blocks");

        Harness.Test("thematic break", () => Harness.Equal("hr", Dump("---"), "hr"));

        Harness.Test("definition list", () =>
        {
            string dump = Dump("Term\n: Definition one");
            Harness.Contains(dump, "deflist", "deflist");
            Harness.Contains(dump, "term: Term", "term");
            Harness.Contains(dump, "def: Definition one", "def");
        });

        Harness.Test("inline math", () =>
        {
            var p = (ParagraphBlock)Parse("value $e^{i\\pi}+1=0$ here").Blocks[0];
            Harness.True(p.Inlines.Any(i => i is MathInline), "math inline");
        });

        Harness.Test("currency is not math", () =>
        {
            var p = (ParagraphBlock)Parse("cost $5 and $10 total").Blocks[0];
            Harness.False(p.Inlines.Any(i => i is MathInline), "currency");
        });

        Harness.Test("display math block", () =>
            Harness.Equal("math: \\int_0^1 x dx", Dump("$$\n\\int_0^1 x dx\n$$"), "math block"));

        Harness.Test("toc placeholder", () => Harness.Equal("toc", Dump("[TOC]"), "toc"));

        Harness.Test("pattern chain", () =>
        {
            string dump = Dump("`(Client) --> [HTTPS] --> (Server)`");
            Harness.Contains(dump, "chain: [Client] --> [HTTPS] --> [Server]", "chain");
        });

        Harness.Test("plain code span is not a chain", () =>
        {
            string dump = Dump("`--basetemp=<path>`");
            Harness.Contains(dump, "p: `--basetemp=<path>`", "no chain");
        });

        Harness.Test("html block is captured, not executed", () =>
        {
            var block = Parse("<div>hello</div>").Blocks[0];
            Harness.True(block is HtmlBlock, "html block");
        });

        Harness.Suite("front matter");

        Harness.Test("front matter values", () =>
        {
            var doc = Parse("---\ntitle: My Doc\ntags: [a, b]\ndraft: true\ntoc: false\n---\n\n# Body\n");
            Harness.Equal("My Doc", doc.FrontMatter.Title ?? "", "title");
            Harness.Equal("a,b", string.Join(',', doc.FrontMatter.Tags), "tags");
            Harness.True(doc.FrontMatter.Draft, "draft");
            Harness.Equal(false, doc.FrontMatter.Toc ?? true, "toc");
            Harness.Equal("Body", doc.Outline[0].Text, "body heading");
        });

        Harness.Test("front matter does not leak into body", () =>
        {
            Harness.Equal("p: x", Dump("---\ntitle: t\n---\nx"), "body");
        });

        Harness.Suite("footnotes");

        Harness.Test("footnote reference and definition", () =>
        {
            var doc = Parse("Text[^1].\n\n[^1]: The note.\n");
            Harness.Equal(1, doc.Footnotes.Count, "count");
            Harness.Equal("The note.", doc.Footnotes[0].Text ?? "", "text");
            Harness.True(doc.Blocks[^1] is FootnoteSectionBlock, "section");
        });

        Harness.Test("inline footnote", () =>
        {
            var doc = Parse("Text^[inline note].");
            Harness.Equal(1, doc.Footnotes.Count, "count");
        });

        Harness.Suite("document");

        Harness.Test("outline and stats", () =>
        {
            var doc = Parse("# Title\n\nOne two three.\n\n## Section\n\nMore text here.");
            Harness.Equal(2, doc.Outline.Count, "outline");
            Harness.Equal("Title", doc.Title ?? "", "title");
            Harness.True(doc.WordCount >= 8, $"words ({doc.WordCount})");
        });

        Harness.Test("empty document", () =>
        {
            var doc = Parse("");
            Harness.Equal(0, doc.Blocks.Length, "blocks");
            Harness.Equal(0, doc.WordCount, "words");
        });

        Harness.Test("crlf and tabs normalise", () =>
        {
            var doc = Parse("# A\r\n\r\n\tcode\r\n");
            Harness.Equal(2, doc.Blocks.Length, "blocks");
        });
    }
}




