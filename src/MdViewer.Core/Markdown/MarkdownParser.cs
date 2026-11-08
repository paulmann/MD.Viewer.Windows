using System.Text;

namespace MdViewer.Core.Markdown;

public sealed record HeadingInfo(int Level, string Text, string Id, string? Number, int BlockIndex, int Line);

public sealed record MarkdownDiagnostic(string Message, int Line, string Severity = "info");

/// <summary>A parsed document plus everything the viewer needs to navigate it.</summary>
public sealed class MarkdownDocument
{
    public required SourceText Source { get; init; }

    public FrontMatter FrontMatter { get; init; } = FrontMatter.Empty;

    public required Block[] Blocks { get; init; }

    public IReadOnlyList<HeadingInfo> Outline { get; init; } = [];

    public IReadOnlyList<FootnoteEntry> Footnotes { get; init; } = [];

    public IReadOnlyList<MarkdownDiagnostic> Diagnostics { get; init; } = [];

    /// <summary>Terms from two-column tables, used for hover tooltips.</summary>
    public IReadOnlyDictionary<string, string> Glossary { get; init; } = new Dictionary<string, string>();

    public MarkdownOptions Options { get; init; } = MarkdownOptions.Default;

    public string? FilePath { get; init; }

    public string? Title { get; init; }

    public int WordCount { get; init; }

    public int CharacterCount { get; init; }

    public TimeSpan ReadingTime { get; init; }

    public string FileName => FilePath is null ? "untitled.md" : Path.GetFileName(FilePath);

    /// <summary>Replaces the document text and re-parses; used by the live editor preview.</summary>
    public MarkdownDocument Reparse(string newText, MarkdownParser parser) => parser.Parse(newText, Options, FilePath);
}

/// <summary>Turns Markdown source into a <see cref="MarkdownDocument"/>.</summary>
public sealed class MarkdownParser
{
    public MarkdownDocument Parse(string? text, MarkdownOptions? options = null, string? filePath = null)
    {
        var opts = options ?? MarkdownOptions.Default;
        var source = SourceText.Parse(text);
        var diagnostics = new List<MarkdownDiagnostic>();

        FrontMatter frontMatter = FrontMatter.Empty;
        int bodyStart = 0;
        if (opts.FrontMatter)
        {
            frontMatter = FrontMatter.Parse(source.Lines, out bodyStart);
        }

        var lines = (string[])source.Lines.Clone();
        var references = CollectLinkReferences(lines, bodyStart);
        var footnoteTexts = CollectFootnoteDefinitions(lines, bodyStart, diagnostics);

        var registry = new FootnoteRegistry();
        var inlineParser = new InlineParser(opts, references, registry);
        var blockParser = new BlockParser(opts, inlineParser);
        Block[] blocks = blockParser.Parse(lines, bodyStart, lines.Length);

        blocks = PostProcess(blocks, opts, registry, footnoteTexts, diagnostics, topLevel: true);

        var slugger = new Slugger();
        var outline = new List<HeadingInfo>();
        var counters = new int[7];
        bool numbering = opts.HeadingNumbering || frontMatter.Numbering;
        int blockIndex = 0;

        foreach (Block block in blocks)
        {
            if (block is not HeadingBlock heading)
            {
                blockIndex++;
                continue;
            }

            if (string.IsNullOrEmpty(heading.Id) || heading.ExplicitId is false && heading.Id.Length == 0)
            {
                heading.Id = slugger.Slug(heading.Text);
            }

            if (heading.ExplicitId)
            {
                slugger.Slug(heading.Text);
            }

            if (numbering)
            {
                counters[heading.Level]++;
                for (int level = heading.Level + 1; level < counters.Length; level++)
                {
                    counters[level] = 0;
                }

                heading.Number = string.Join('.', counters.Skip(1).Take(heading.Level).Where((_, i) => i < heading.Level));
            }

            outline.Add(new HeadingInfo(heading.Level, heading.Text, heading.Id, heading.Number, blockIndex, heading.LineNumber));
            blockIndex++;
        }

        string plain = PlainText(blocks);
        int words = CountWords(plain);
        string? title = frontMatter.Title;
        if (string.IsNullOrWhiteSpace(title))
        {
            title = outline.FirstOrDefault(h => h.Level == 1)?.Text;
        }

        return new MarkdownDocument
        {
            Source = source,
            FrontMatter = frontMatter,
            Blocks = blocks,
            Outline = outline,
            Footnotes = registry.Entries,
            Diagnostics = diagnostics,
            Glossary = BuildGlossary(blocks, inlineParser),
            Options = opts,
            FilePath = filePath,
            Title = title,
            WordCount = words,
            CharacterCount = plain.Length,
            ReadingTime = TimeSpan.FromMinutes(Math.Max(1, words / 200.0)),
        };
    }

    // ------------------------------------------------------------ pre-pass

    private static Dictionary<string, LinkReference> CollectLinkReferences(string[] lines, int start)
    {
        var map = new Dictionary<string, LinkReference>(StringComparer.Ordinal);

        for (int i = start; i < lines.Length; i++)
        {
            string line = lines[i];
            int indent = LeadingSpaces(line);
            if (indent > 3 || line.Length < indent + 4 || line[indent] != '[')
            {
                continue;
            }

            int close = line.IndexOf("]:", indent, StringComparison.Ordinal);
            if (close < indent + 2)
            {
                continue;
            }

            string label = line[(indent + 1)..close];
            if (label.StartsWith('^'))
            {
                continue;
            }

            string rest = line[(close + 2)..].Trim();
            if (rest.Length == 0)
            {
                continue;
            }

            string url;
            string? title = null;

            if (rest.StartsWith('<'))
            {
                int gt = rest.IndexOf('>');
                if (gt < 0)
                {
                    continue;
                }

                url = rest[1..gt];
                string tail = rest[(gt + 1)..].Trim();
                title = ExtractTitle(tail);
            }
            else
            {
                int space = rest.IndexOfAny([' ', '\t']);
                if (space < 0)
                {
                    url = rest;
                }
                else
                {
                    url = rest[..space];
                    title = ExtractTitle(rest[space..].Trim());
                }
            }

            // A title may sit on the following line, quoted on its own.
            if (title is null && i + 1 < lines.Length)
            {
                string next = lines[i + 1].Trim();
                bool quoted = next.Length >= 3
                    && ((next[0] == '"' && next[^1] == '"') || (next[0] == '\'' && next[^1] == '\'') || (next[0] == '(' && next[^1] == ')'));
                if (quoted)
                {
                    title = next[1..^1];
                    lines[i + 1] = string.Empty;
                }
            }

            string key = InlineParser.NormalizeLabel(label);
            if (key.Length > 0)
            {
                map[key] = new LinkReference(url, title);
                lines[i] = string.Empty;
            }
        }

        return map;
    }

    private static string? ExtractTitle(string tail)
    {
        if (tail.Length < 2)
        {
            return null;
        }

        char quote = tail[0];
        char closeQuote = quote switch
        {
            '"' => '"',
            '\'' => '\'',
            '(' => ')',
            _ => '\0',
        };

        if (closeQuote == '\0')
        {
            return null;
        }

        int close = tail.IndexOf(closeQuote, 1);
        return close > 0 ? tail[1..close] : null;
    }

    private static Dictionary<string, string> CollectFootnoteDefinitions(
        string[] lines,
        int start,
        List<MarkdownDiagnostic> diagnostics)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);

        for (int i = start; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.Length < 5 || !line.StartsWith("[^", StringComparison.Ordinal))
            {
                continue;
            }

            int close = line.IndexOf("]:", StringComparison.Ordinal);
            if (close < 3)
            {
                continue;
            }

            string label = line[2..close];
            var sb = new StringBuilder(line[(close + 2)..].Trim());
            int j = i + 1;
            int lastLine = i;

            while (j < lines.Length)
            {
                string next = lines[j];
                if (next.Length == 0)
                {
                    // blank line: keep the definition going if the next line is indented
                    int probe = j + 1;
                    while (probe < lines.Length && lines[probe].Length == 0)
                    {
                        probe++;
                    }

                    if (probe < lines.Length && LeadingSpaces(lines[probe]) >= 2)
                    {
                        sb.Append('\n');
                        j = probe;
                        continue;
                    }

                    break;
                }

                if (LeadingSpaces(next) >= 2)
                {
                    sb.Append('\n').Append(next.TrimStart());
                    lastLine = j;
                    j++;
                    continue;
                }

                break;
            }

            map[label] = sb.ToString();
            for (int k = i; k <= lastLine; k++)
            {
                lines[k] = string.Empty;
            }

            if (lastLine > i)
            {
                diagnostics.Add(new MarkdownDiagnostic($"footnote '{label}' spans {lastLine - i + 1} lines", i + 1));
            }

            i = lastLine;
        }

        return map;
    }

    private static int LeadingSpaces(string line)
    {
        int i = 0;
        while (i < line.Length && line[i] == ' ')
        {
            i++;
        }

        return i;
    }

    // ---------------------------------------------------------- post-pass

    private Block[] PostProcess(
        Block[] blocks,
        MarkdownOptions options,
        FootnoteRegistry registry,
        Dictionary<string, string> footnoteTexts,
        List<MarkdownDiagnostic> diagnostics,
        bool topLevel = false)
    {
        List<Block> output = [with(capacity: blocks.Length + 2)];

        foreach (Block block in blocks)
        {
            switch (block)
            {
                case ParagraphBlock paragraph when paragraph.Inlines.Length == 1 && paragraph.Inlines[0] is ImageInline image:
                    output.Add(new ImageBlock
                    {
                        Url = image.Url,
                        Alt = image.Alt,
                        Title = image.Title,
                        Width = image.Width,
                    });
                    continue;

                case ParagraphBlock paragraph when options.PatternChains
                    && paragraph.Inlines.Length == 1
                    && paragraph.Inlines[0] is CodeInline code
                    && PatternChain.TryParse(code.Text, out ChainPart[]? parts):
                    output.Add(new ChainBlock { Parts = parts! });
                    continue;

                case QuoteBlock quote:
                    output.Add(new QuoteBlock { Children = PostProcess(quote.Children, options, registry, footnoteTexts, diagnostics) });
                    continue;

                case AlertBlock alert:
                    output.Add(new AlertBlock
                    {
                        Kind = alert.Kind,
                        Children = PostProcess(alert.Children, options, registry, footnoteTexts, diagnostics),
                    });
                    continue;

                case ListBlock list:
                {
                    var items = new List<ListItem>(list.Items.Length);
                    foreach (ListItem item in list.Items)
                    {
                        items.Add(new ListItem(item.Task, PostProcess(item.Children, options, registry, footnoteTexts, diagnostics), item.LineNumber));
                    }

                    output.Add(new ListBlock
                    {
                        Ordered = list.Ordered,
                        Start = list.Start,
                        Items = [.. items],
                        Tight = list.Tight,
                        LineNumber = list.LineNumber,
                    });
                    continue;
                }

                default:
                    output.Add(block);
                    continue;
            }
        }

        if (topLevel && registry.Entries.Count > 0)
        {
            var notes = new List<FootnoteNote>(registry.Entries.Count);
            var inlineParser = new InlineParser(options, new Dictionary<string, LinkReference>(), registry);
            var blockParser = new BlockParser(options, inlineParser);
            FootnoteEntry[] entries = [.. registry.Entries];

            foreach (FootnoteEntry entry in entries)
            {
                string text = entry.Text ?? string.Empty;
                if (entry.Text is null && footnoteTexts.TryGetValue(entry.Label, out string? defined))
                {
                    text = defined;
                    registry.Define(entry.Label, defined, entry.Line);
                }

                SourceText source = SourceText.Parse(text);
                Block[] children = text.Length == 0
                    ? []
                    : blockParser.Parse(source.Lines, 0, source.LineCount);

                notes.Add(new FootnoteNote(entry.Index, entry.Label, children));
            }

            output.Add(new FootnoteSectionBlock { Notes = [.. notes] });
        }

        return [.. output];
    }

    // --------------------------------------------------------------- glossary

    /// <summary>
    /// Two-column tables double as glossary sources: the first column is a term,
    /// the second its definition. "Term (ABBR)" also registers both parts.
    /// </summary>
    private static Dictionary<string, string> BuildGlossary(Block[] blocks, InlineParser inlineParser)
    {
        var glossary = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

        void Add(string term, string definition)
        {
            string key = term.Trim().Trim('*', '`', '_');
            if (key.Length >= 2 && key.Length <= 80)
            {
                glossary[key] = definition;
            }
        }

        foreach (Block block in blocks)
        {
            if (block is not TableBlock { IsGlossary: true } table)
            {
                continue;
            }

            foreach (string[] row in table.Rows)
            {
                if (row.Length < 2)
                {
                    continue;
                }

                string term = InlineParser.PlainText(inlineParser.Parse(row[0])).Trim();
                string definition = InlineParser.PlainText(inlineParser.Parse(row[1])).Trim();
                if (term.Length == 0 || definition.Length < 3)
                {
                    continue;
                }

                Add(term, definition);

                int open = term.LastIndexOf('(');
                int close = term.EndsWith(')') ? term.Length - 1 : -1;
                if (open > 0 && close > open + 1)
                {
                    Add(term[..open], definition);
                    Add(term[(open + 1)..close], definition);
                }
            }
        }

        return glossary;
    }

    // ------------------------------------------------------------- metrics

    private static string PlainText(Block[] blocks)
    {
        var sb = new StringBuilder();
        foreach (Block block in blocks)
        {
            AppendText(block, sb);
        }

        return sb.ToString();
    }

    private static void AppendText(Block block, StringBuilder sb)
    {
        switch (block)
        {
            case HeadingBlock h:
                sb.AppendLine(InlineParser.PlainText(h.Inlines));
                break;
            case ParagraphBlock p:
                sb.AppendLine(InlineParser.PlainText(p.Inlines));
                break;
            case CodeBlock c:
                sb.AppendLine(c.Text);
                break;
            case MermaidBlock m:
                sb.AppendLine(m.Source);
                break;
            case QuoteBlock q:
                foreach (Block child in q.Children)
                {
                    AppendText(child, sb);
                }

                break;
            case AlertBlock a:
                foreach (Block child in a.Children)
                {
                    AppendText(child, sb);
                }

                break;
            case ListBlock l:
                foreach (ListItem item in l.Items)
                {
                    foreach (Block child in item.Children)
                    {
                        AppendText(child, sb);
                    }
                }

                break;
            case TableBlock t:
                sb.AppendLine(string.Join(' ', t.Headers));
                foreach (string[] row in t.Rows)
                {
                    sb.AppendLine(string.Join(' ', row));
                }

                break;
            case DefinitionListBlock d:
                foreach (DefinitionItem item in d.Items)
                {
                    sb.AppendLine(item.Term);
                    foreach (string[] def in item.Definitions)
                    {
                        sb.AppendLine(string.Join(' ', def));
                    }
                }

                break;
        }
    }

    private static int CountWords(string text)
    {
        int count = 0;
        bool inWord = false;
        foreach (char c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                if (inWord)
                {
                    count++;
                    inWord = false;
                }

                continue;
            }

            inWord = true;
        }

        return inWord ? count + 1 : count;
    }
}

/// <summary>MD.Scribe v2 "universal pattern chain": <c>(A) --&gt; [label] --&gt; (B)</c>.</summary>
public static class PatternChain
{
    private static readonly string[] Arrows = ["-->", "==>", "->", "=>", "→", "⟶", "<-", "<--"];

    public static bool TryParse(string text, out ChainPart[]? parts)
    {
        parts = null;
        if (text.Length is < 5 or > 240)
        {
            return false;
        }

        string candidate = text.Trim();
        if (!Arrows.Any(a => candidate.Contains(a, StringComparison.Ordinal)))
        {
            return false;
        }

        var tokens = new List<string>();
        var arrows = new List<string>();
        int cursor = 0;

        while (cursor < candidate.Length)
        {
            int nearest = -1;
            string? matched = null;
            foreach (string arrow in Arrows)
            {
                int idx = candidate.IndexOf(arrow, cursor, StringComparison.Ordinal);
                if (idx < 0)
                {
                    continue;
                }

                if (nearest < 0 || idx < nearest || idx == nearest && arrow.Length > matched!.Length)
                {
                    nearest = idx;
                    matched = arrow;
                }
            }

            if (nearest < 0 || matched is null)
            {
                break;
            }

            tokens.Add(candidate[cursor..nearest]);
            arrows.Add(matched);
            cursor = nearest + matched.Length;
        }

        tokens.Add(candidate[cursor..]);

        if (arrows.Count < 1 || tokens.Count != arrows.Count + 1)
        {
            return false;
        }

        var result = new List<ChainPart>(tokens.Count + arrows.Count);
        for (int i = 0; i < tokens.Count; i++)
        {
            string token = tokens[i].Trim();
            if (token.Length == 0 || token.Length > 60 || token.Contains('\n'))
            {
                return false;
            }

            bool isNode = (token.StartsWith('(') && token.EndsWith(')'))
                || (token.StartsWith('[') && token.EndsWith(']'))
                || (token.StartsWith('{') && token.EndsWith('}'));

            string label = isNode ? token[1..^1].Trim() : token;
            if (label.Length == 0)
            {
                return false;
            }

            result.Add(new ChainPart(label, isNode, false));
            if (i < arrows.Count)
            {
                result.Add(new ChainPart(arrows[i], false, true));
            }
        }

        parts = [.. result];
        return true;
    }
}


