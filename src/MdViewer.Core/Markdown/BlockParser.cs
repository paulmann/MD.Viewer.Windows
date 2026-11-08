using System.Text;

namespace MdViewer.Core.Markdown;

/// <summary>
/// Line-oriented block parser (ATX/setext headings, fences, lists, quotes, tables,
/// definition lists, alerts, math, HTML, paragraphs). Container blocks recurse on
/// their de-indented content, which keeps the implementation linear and readable.
/// </summary>
public sealed class BlockParser(MarkdownOptions options, InlineParser inline)
{
    private static readonly string[] HtmlBlockTags =
    [
        "address", "article", "aside", "base", "blockquote", "body", "caption", "center", "col", "colgroup",
        "dd", "details", "dialog", "dir", "div", "dl", "dt", "fieldset", "figcaption", "figure", "footer",
        "form", "frame", "frameset", "h1", "h2", "h3", "h4", "h5", "h6", "head", "header", "hr", "html",
        "iframe", "legend", "li", "link", "main", "menu", "menuitem", "nav", "noframes", "ol", "optgroup",
        "option", "p", "param", "search", "section", "summary", "table", "tbody", "td", "tfoot", "th",
        "thead", "title", "tr", "track", "ul", "video", "audio", "svg", "picture", "source",
    ];

    public Block[] Parse(string[] lines, int start, int end)
    {
        var blocks = new List<Block>();
        int i = start;

        while (i < end)
        {
            string line = lines[i];

            if (IsBlank(line))
            {
                i++;
                continue;
            }

            if (TryToc(line, ref i, blocks))
            {
                continue;
            }

            if (TryFencedCode(lines, ref i, end, out Block? code))
            {
                blocks.Add(code!);
                continue;
            }

            if (TryMathBlock(lines, ref i, end, out Block? math))
            {
                blocks.Add(math!);
                continue;
            }

            if (TryAtxHeading(line, out HeadingBlock? heading))
            {
                blocks.Add(heading!);
                i++;
                continue;
            }

            if (TryThematicBreak(line))
            {
                blocks.Add(new ThematicBreakBlock());
                i++;
                continue;
            }

            if (TryBlockQuote(lines, ref i, end, out Block? quote))
            {
                blocks.Add(quote!);
                continue;
            }

            if (TryList(lines, ref i, end, out Block? list))
            {
                blocks.Add(list!);
                continue;
            }

            if (options.Tables && TryTable(lines, ref i, end, out Block? table))
            {
                blocks.Add(table!);
                continue;
            }

            if (TryHtmlBlock(lines, ref i, end, out Block? html))
            {
                blocks.Add(html!);
                continue;
            }

            if (TryIndentedCode(lines, ref i, end, out Block? indented))
            {
                blocks.Add(indented!);
                continue;
            }

            if (TrySetextHeading(lines, ref i, end, out HeadingBlock? setext))
            {
                blocks.Add(setext!);
                continue;
            }

            if (TryParagraph(lines, ref i, end, out Block? paragraph))
            {
                blocks.Add(paragraph!);
                continue;
            }

            i++;
        }

        return [.. blocks];
    }

    // ------------------------------------------------------------- headings

    private bool TryAtxHeading(string line, out HeadingBlock? heading)
    {
        heading = null;
        int i = 0;
        while (i < line.Length && i < 4 && line[i] == ' ')
        {
            i++;
        }

        if (i > 3 || i >= line.Length || line[i] != '#')
        {
            return false;
        }

        int level = 0;
        while (i < line.Length && line[i] == '#')
        {
            level++;
            i++;
        }

        if (level > 6)
        {
            return false;
        }

        if (i < line.Length && line[i] != ' ')
        {
            return false;
        }

        string text = line[i..].Trim();
        int closing = text.Length;
        while (closing > 0 && text[closing - 1] == '#')
        {
            closing--;
        }

        if (closing < text.Length && (closing == 0 || text[closing - 1] == ' '))
        {
            text = text[..closing].TrimEnd();
        }

        heading = BuildHeading(level, text);
        return true;
    }

    private bool TrySetextHeading(string[] lines, ref int i, int end, out HeadingBlock? heading)
    {
        heading = null;
        if (i + 1 >= end)
        {
            return false;
        }

        string first = lines[i];
        if (IsBlank(first) || IsBlockStart(first))
        {
            return false;
        }

        string underline = lines[i + 1].TrimEnd();
        if (underline.Length == 0)
        {
            return false;
        }

        bool equals = underline.All(c => c == '=');
        bool dashes = underline.All(c => c == '-');
        if ((!equals && !dashes) || underline.Length > first.TrimEnd().Length + 3)
        {
            return false;
        }

        int level = equals ? 1 : 2;
        heading = BuildHeading(level, first.Trim());
        i += 2;
        return true;
    }

    private HeadingBlock BuildHeading(int level, string text)
    {
        bool explicitId = false;
        string id = string.Empty;

        if (options.AttributeSyntax)
        {
            int brace = text.LastIndexOf('{');
            if (brace > 0 && text.EndsWith('}'))
            {
                string attrs = text[(brace + 1)..^1].Trim();
                string? found = null;
                foreach (string part in attrs.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (part.StartsWith('#'))
                    {
                        found = part[1..];
                    }
                }

                if (found is { Length: > 0 })
                {
                    id = found;
                    explicitId = true;
                    text = text[..brace].TrimEnd();
                }
            }
        }

        Inline[] inlines = inline.Parse(text);
        string plain = InlineParser.PlainText(inlines).Trim();

        return new HeadingBlock
        {
            Level = level,
            Inlines = inlines,
            Text = plain,
            Id = id,
            ExplicitId = explicitId,
        };
    }

    // ----------------------------------------------------------------- code

    private bool TryFencedCode(string[] lines, ref int i, int end, out Block? block)
    {
        block = null;
        string line = lines[i];
        int indent = 0;
        while (indent < line.Length && indent < 4 && line[indent] == ' ')
        {
            indent++;
        }

        if (indent > 3 || indent >= line.Length)
        {
            return false;
        }

        char fenceChar = line[indent];
        if (fenceChar is not ('`' or '~'))
        {
            return false;
        }

        int fenceLength = 0;
        int k = indent;
        while (k < line.Length && line[k] == fenceChar)
        {
            fenceLength++;
            k++;
        }

        if (fenceLength < 3)
        {
            return false;
        }

        string info = line[k..].Trim();
        if (fenceChar == '`' && info.Contains('`'))
        {
            return false;
        }

        string? language = null;
        string? title = null;
        if (info.Length > 0)
        {
            int space = info.IndexOf(' ');
            string firstToken = space < 0 ? info : info[..space];
            language = firstToken.Trim('{', '}', '.');
            if (space > 0)
            {
                string rest = info[(space + 1)..];
                int titleIndex = rest.IndexOf("title=", StringComparison.OrdinalIgnoreCase);
                if (titleIndex >= 0)
                {
                    string raw = rest[(titleIndex + 6)..].Trim();
                    if (raw.Length > 0)
                    {
                        char quote = raw[0];
                        if (quote is '"' or '\'')
                        {
                            int close = raw.IndexOf(quote, 1);
                            title = close > 0 ? raw[1..close] : raw[1..];
                        }
                        else
                        {
                            int space2 = raw.IndexOf(' ');
                            title = space2 < 0 ? raw : raw[..space2];
                        }
                    }
                }
            }
        }

        int startLine = i + 1;
        var sb = new StringBuilder();
        int j = startLine;
        bool closed = false;

        while (j < end)
        {
            string current = lines[j];
            string trimmed = current.TrimStart();
            if (trimmed.Length - trimmed.TrimStart(fenceChar).Length >= fenceLength
                && trimmed.TrimStart(fenceChar).Trim().Length == 0
                && (current.Length - current.TrimStart().Length) <= 3)
            {
                closed = true;
                j++;
                break;
            }

            sb.Append(StripIndent(current, indent)).Append('\n');
            j++;
        }

        string text = sb.ToString();
        if (text.EndsWith("\r\n", StringComparison.Ordinal))
        {
            text = text[..^2];
        }
        else if (text.EndsWith('\n'))
        {
            text = text[..^1];
        }

        i = closed ? j : end;

        if (options.Mermaid && language is "mermaid" or "mmd")
        {
            block = new MermaidBlock { Source = text, Title = title, LineNumber = startLine };
            return true;
        }

        block = new CodeBlock
        {
            Text = text,
            Language = language is { Length: 0 } ? null : language,
            Title = title,
            LineNumber = startLine,
        };
        return true;
    }

    private bool TryMathBlock(string[] lines, ref int i, int end, out Block? block)
    {
        block = null;
        if (!options.Math)
        {
            return false;
        }

        string line = lines[i].Trim();
        if (!line.StartsWith("$$", StringComparison.Ordinal))
        {
            return false;
        }

        if (line.Length > 4 && line.EndsWith("$$", StringComparison.Ordinal))
        {
            string single = line[2..^2].Trim();
            if (single.Length > 0)
            {
                block = new MathBlock { Tex = single };
                i++;
                return true;
            }
        }

        string remainder = line[2..].Trim();
        var sb = new StringBuilder();
        int j = i + 1;
        bool closed = false;

        if (remainder.Length > 0)
        {
            sb.Append(remainder).Append('\n');
        }

        while (j < end)
        {
            string current = lines[j].Trim();
            if (current.EndsWith("$$", StringComparison.Ordinal))
            {
                string before = current[..^2].Trim();
                if (before.Length > 0)
                {
                    sb.Append(before).Append('\n');
                }

                closed = true;
                j++;
                break;
            }

            sb.Append(lines[j]).Append('\n');
            j++;
        }

        if (!closed)
        {
            return false;
        }

        block = new MathBlock { Tex = sb.ToString().TrimEnd() };
        i = j;
        return true;
    }

    private bool TryIndentedCode(string[] lines, ref int i, int end, out Block? block)
    {
        block = null;
        if (!lines[i].StartsWith("    ", StringComparison.Ordinal))
        {
            return false;
        }

        var sb = new StringBuilder();
        int j = i;
        int lastContent = i;

        while (j < end)
        {
            string line = lines[j];
            if (IsBlank(line))
            {
                sb.Append('\n');
                j++;
                continue;
            }

            if (!line.StartsWith("    ", StringComparison.Ordinal))
            {
                break;
            }

            sb.Append(line[4..]).Append('\n');
            lastContent = j;
            j++;
        }

        i = lastContent + 1;
        string text = sb.ToString().TrimEnd('\n');
        block = new CodeBlock { Text = text, LineNumber = lastContent + 2 };
        return true;
    }

    // --------------------------------------------------------------- quotes

    private bool TryBlockQuote(string[] lines, ref int i, int end, out Block? block)
    {
        block = null;
        int indent = LeadingSpaces(lines[i]);
        if (indent > 3 || indent >= lines[i].Length || lines[i][indent] != '>')
        {
            return false;
        }

        var content = new List<string>();
        int j = i;

        while (j < end)
        {
            string line = lines[j];
            if (IsBlank(line))
            {
                // A blank line ends the quote unless the next non-blank line
                // still carries a '>' marker (CommonMark container rule).
                int probe = j + 1;
                while (probe < end && IsBlank(lines[probe]))
                {
                    probe++;
                }

                int probeIndent = probe < end ? LeadingSpaces(lines[probe]) : int.MaxValue;
                bool continues = probe < end && probeIndent <= 3 && probeIndent < lines[probe].Length && lines[probe][probeIndent] == '>';
                if (!continues)
                {
                    break;
                }

                content.Add(string.Empty);
                j++;
                continue;
            }

            int pad = LeadingSpaces(line);
            if (pad <= 3 && pad < line.Length && line[pad] == '>')
            {
                string rest = line[(pad + 1)..];
                if (rest.StartsWith(' '))
                {
                    rest = rest[1..];
                }

                content.Add(rest);
                j++;
                continue;
            }

            // lazy continuation of the last paragraph line
            if (content.Count > 0 && content[^1].Length > 0 && !IsBlockStart(line))
            {
                content.Add(line.TrimStart());
                j++;
                continue;
            }

            break;
        }

        AlertKind kind = AlertKind.None;
        string? alertTitle = null;
        int skip = 0;

        if (content.Count > 0)
        {
            string first = content[0].Trim();
            if (first.StartsWith("[!", StringComparison.Ordinal) && first.Contains(']'))
            {
                int close = first.IndexOf(']');
                string marker = first[2..close].Trim().ToUpperInvariant();
                kind = marker switch
                {
                    "NOTE" => AlertKind.Note,
                    "TIP" => AlertKind.Tip,
                    "IMPORTANT" => AlertKind.Important,
                    "WARNING" => AlertKind.Warning,
                    "CAUTION" => AlertKind.Caution,
                    _ => AlertKind.None,
                };

                if (kind != AlertKind.None)
                {
                    string tail = first[(close + 1)..].Trim();
                    if (tail.Length > 0)
                    {
                        alertTitle = tail;
                    }

                    skip = 1;
                }
            }
        }

        Block[] children = Parse([.. content], skip, content.Count);
        block = kind == AlertKind.None
            ? new QuoteBlock { Children = children }
            : new AlertBlock { Kind = kind, Children = children };

        _ = alertTitle;
        i = j;
        return true;
    }

    // ---------------------------------------------------------------- lists

    private bool TryList(string[] lines, ref int i, int end, out Block? block)
    {
        block = null;
        string line = lines[i];
        if (!TryReadMarker(line, out bool ordered, out int number, out int markerWidth, out int contentOffset))
        {
            return false;
        }

        var items = new List<ListItem>();
        bool loose = false;
        int j = i;
        int safety = 0;

        while (j < end && safety++ < 100_000)
        {
            string current = lines[j];
            if (IsBlank(current))
            {
                // blank line between items keeps the list going only if the next
                // non-blank line is another item at the same level
                int probe = j + 1;
                while (probe < end && IsBlank(lines[probe]))
                {
                    probe++;
                }

                if (probe < end && TryReadMarker(lines[probe], out bool o2, out _, out _, out _) && o2 == ordered)
                {
                    loose = true;
                    j = probe;
                    continue;
                }

                break;
            }

            if (!TryReadMarker(current, out bool isOrdered, out int itemNumber, out int width, out int offset) || isOrdered != ordered)
            {
                break;
            }

            _ = itemNumber;
            int itemStart = j;
            var content = new List<string> { current[Math.Min(offset, current.Length)..] };

            j++;
            int contentIndent = offset;

            while (j < end)
            {
                string next = lines[j];
                if (IsBlank(next))
                {
                    // look ahead: continue if the following line is indented or a new item
                    int probe = j + 1;
                    while (probe < end && IsBlank(lines[probe]))
                    {
                        probe++;
                    }

                    if (probe >= end)
                    {
                        break;
                    }

                    string afterBlank = lines[probe];
                    int afterIndent = LeadingSpaces(afterBlank);
                    if (afterIndent >= contentIndent)
                    {
                        content.Add(string.Empty);
                        j++;
                        continue;
                    }

                    if (TryReadMarker(afterBlank, out bool o3, out _, out _, out _) && o3 == ordered)
                    {
                        loose = true;
                        j = probe;
                        break;
                    }

                    break;
                }

                int indent2 = LeadingSpaces(next);
                if (indent2 >= contentIndent)
                {
                    content.Add(StripIndent(next, contentIndent));
                    j++;
                    continue;
                }

                if (TryReadMarker(next, out bool o4, out _, out _, out _) && o4 == ordered && indent2 <= width)
                {
                    break;
                }

                if (IsBlockStart(next))
                {
                    break;
                }

                // lazy paragraph continuation
                if (content.Count > 0 && content[^1].Length > 0)
                {
                    content.Add(next.TrimStart());
                    j++;
                    continue;
                }

                break;
            }

            TaskState task = TaskState.None;
            if (options.TaskLists && content.Count > 0)
            {
                string first = content[0].TrimStart();
                if (first.Length >= 4 && first[0] == '[' && (first[1] == ' ' || first[1] == 'x' || first[1] == 'X') && first[2] == ']')
                {
                    task = first[1] is 'x' or 'X' ? TaskState.Done : TaskState.Todo;
                    content[0] = first[3..].TrimStart();
                }
            }

            Block[] children = Parse([.. content], 0, content.Count);
            items.Add(new ListItem(task, children, itemStart + 1));
        }

        if (items.Count == 0)
        {
            return false;
        }

        block = new ListBlock
        {
            Ordered = ordered,
            Start = ordered ? number : 1,
            Items = [.. items],
            Tight = !loose,
            LineNumber = i + 1,
        };

        _ = markerWidth;
        i = j;
        return true;
    }

    private static bool TryReadMarker(string line, out bool ordered, out int number, out int markerWidth, out int contentOffset)
    {
        ordered = false;
        number = 1;
        markerWidth = 0;
        contentOffset = 0;

        int i = LeadingSpaces(line);
        if (i > 3 || i >= line.Length)
        {
            return false;
        }

        char c = line[i];
        if (c is '-' or '*' or '+')
        {
            markerWidth = i + 1;
            if (markerWidth < line.Length && line[markerWidth] != ' ')
            {
                return false;
            }

            contentOffset = markerWidth;
            while (contentOffset < line.Length && line[contentOffset] == ' ')
            {
                contentOffset++;
            }

            if (contentOffset - markerWidth > 4)
            {
                contentOffset = markerWidth + 1;
            }

            return true;
        }

        if (!char.IsAsciiDigit(c))
        {
            return false;
        }

        int digits = 0;
        while (i + digits < line.Length && char.IsAsciiDigit(line[i + digits]))
        {
            digits++;
        }

        if (digits is 0 or > 9 || i + digits >= line.Length)
        {
            return false;
        }

        char separator = line[i + digits];
        if (separator is not ('.' or ')'))
        {
            return false;
        }

        markerWidth = i + digits + 1;
        if (markerWidth < line.Length && line[markerWidth] != ' ')
        {
            return false;
        }

        number = int.TryParse(line.AsSpan(i, digits), out int parsed) ? parsed : 1;
        contentOffset = markerWidth;
        while (contentOffset < line.Length && line[contentOffset] == ' ')
        {
            contentOffset++;
        }

        if (contentOffset - markerWidth > 4)
        {
            contentOffset = markerWidth + 1;
        }

        ordered = true;
        return true;
    }

    // --------------------------------------------------------------- tables

    private bool TryTable(string[] lines, ref int i, int end, out Block? block)
    {
        block = null;
        if (i + 1 >= end)
        {
            return false;
        }

        string header = lines[i];
        if (!header.Contains('|'))
        {
            return false;
        }

        string delimiter = lines[i + 1];
        if (!IsDelimiterRow(delimiter))
        {
            return false;
        }

        string[] headers = SplitRow(header);
        string[] delimiters = SplitRow(delimiter);
        if (headers.Length != delimiters.Length || headers.Length == 0)
        {
            return false;
        }

        var aligns = new CellAlign[delimiters.Length];
        for (int c = 0; c < delimiters.Length; c++)
        {
            string cell = delimiters[c].Trim();
            bool left = cell.StartsWith(':');
            bool right = cell.EndsWith(':');
            aligns[c] = (left, right) switch
            {
                (true, true) => CellAlign.Center,
                (true, false) => CellAlign.Left,
                (false, true) => CellAlign.Right,
                _ => CellAlign.None,
            };
        }

        var rows = new List<string[]>();
        int j = i + 2;
        while (j < end)
        {
            string line = lines[j];
            if (IsBlank(line) || !line.Contains('|'))
            {
                break;
            }

            string[] cells = SplitRow(line);
            if (cells.Length < headers.Length)
            {
                Array.Resize(ref cells, headers.Length);
            }

            if (cells.Length > headers.Length)
            {
                Array.Resize(ref cells, headers.Length);
            }

            rows.Add(cells);
            j++;
        }

        bool glossary = options.GlossaryTables && headers.Length == 2 && rows.Count > 0;

        block = new TableBlock
        {
            Headers = headers,
            Aligns = aligns,
            Rows = [.. rows],
            IsGlossary = glossary,
            LineNumber = i + 1,
        };

        i = j;
        return true;
    }

    private static bool IsDelimiterRow(string line)
    {
        string trimmed = line.Trim();
        if (trimmed.Length == 0)
        {
            return false;
        }

        bool sawDash = false;
        foreach (char c in trimmed)
        {
            if (c == '-')
            {
                sawDash = true;
                continue;
            }

            if (c is '|' or ':' or ' ' or '\t')
            {
                continue;
            }

            return false;
        }

        return sawDash && trimmed.Contains('|') || sawDash && !trimmed.Contains(' ');
    }

    private static string[] SplitRow(string line)
    {
        string trimmed = line.Trim();
        if (trimmed.StartsWith('|'))
        {
            trimmed = trimmed[1..];
        }

        if (trimmed.EndsWith('|') && !trimmed.EndsWith("\\|", StringComparison.Ordinal))
        {
            trimmed = trimmed[..^1];
        }

        var cells = new List<string>();
        var sb = new StringBuilder();
        for (int i = 0; i < trimmed.Length; i++)
        {
            char c = trimmed[i];
            if (c == '\\' && i + 1 < trimmed.Length && trimmed[i + 1] == '|')
            {
                sb.Append('|');
                i++;
                continue;
            }

            if (c == '|')
            {
                cells.Add(sb.ToString().Trim());
                sb.Clear();
                continue;
            }

            sb.Append(c);
        }

        cells.Add(sb.ToString().Trim());
        return [.. cells];
    }

    // ----------------------------------------------------------- html/misc

    private bool TryHtmlBlock(string[] lines, ref int i, int end, out Block? block)
    {
        block = null;
        string line = lines[i];
        string trimmed = line.TrimStart();
        if (LeadingSpaces(line) > 3 || !trimmed.StartsWith('<'))
        {
            return false;
        }

        bool comment = trimmed.StartsWith("<!--", StringComparison.Ordinal);
        bool tag = false;
        if (!comment)
        {
            int k = 1;
            if (k < trimmed.Length && trimmed[k] == '/')
            {
                k++;
            }

            int nameStart = k;
            while (k < trimmed.Length && (char.IsAsciiLetterOrDigit(trimmed[k])))
            {
                k++;
            }

            string name = trimmed[nameStart..k].ToLowerInvariant();
            tag = name.Length > 0 && Array.IndexOf(HtmlBlockTags, name) >= 0;
            if (!tag && name.Length > 0 && k < trimmed.Length && trimmed[k] is ' ' or '>' or '/')
            {
                // single-line inline html is treated as text content
                tag = name is "br" or "hr" or "img";
            }
        }

        if (!comment && !tag)
        {
            return false;
        }

        var sb = new StringBuilder();
        int j = i;
        while (j < end)
        {
            string current = lines[j];
            if (IsBlank(current))
            {
                break;
            }

            sb.Append(current).Append('\n');
            j++;

            if (comment && current.Contains("-->", StringComparison.Ordinal))
            {
                break;
            }
        }

        block = new HtmlBlock { Html = sb.ToString().TrimEnd() };
        i = j;
        return true;
    }

    private bool TryToc(string line, ref int i, List<Block> blocks)
    {
        if (line.Trim() != "[TOC]")
        {
            return false;
        }

        blocks.Add(new TocBlock());
        i++;
        return true;
    }

    private static bool TryThematicBreak(string line)
    {
        string trimmed = line.Trim();
        if (trimmed.Length < 3)
        {
            return false;
        }

        char c = trimmed[0];
        if (c is not ('-' or '*' or '_'))
        {
            return false;
        }

        int count = 0;
        foreach (char ch in trimmed)
        {
            if (ch == c)
            {
                count++;
                continue;
            }

            if (ch == ' ' || ch == '\t')
            {
                continue;
            }

            return false;
        }

        return count >= 3;
    }

    // ------------------------------------------------------------ paragraph

    private bool TryParagraph(string[] lines, ref int i, int end, out Block? block)
    {
        block = null;
        string first = lines[i];
        if (IsBlank(first) || LeadingSpaces(first) > 3)
        {
            return false;
        }

        var sb = new StringBuilder();
        int j = i;

        while (j < end)
        {
            string line = lines[j];
            if (IsBlank(line))
            {
                break;
            }

            if (j > i && IsBlockStart(line) && !IsTableDelimiterContinuation(lines, j))
            {
                break;
            }

            if (sb.Length > 0)
            {
                sb.Append('\n');
            }

            sb.Append(line);
            j++;

            if (j < end && options.DefinitionLists && lines[j].TrimStart().StartsWith(": ", StringComparison.Ordinal) && j - i <= 8)
            {
                if (TryDefinitionList(lines, j, end, sb.ToString(), out block, out int nextLine))
                {
                    i = nextLine;
                    return true;
                }
            }
        }

        string text = sb.ToString();
        Inline[] inlines = inline.Parse(text);
        if (inlines.Length == 0)
        {
            i = j;
            return false;
        }

        block = new ParagraphBlock { Inlines = inlines, LineNumber = i + 1 };
        i = j;
        return true;
    }

    private bool IsTableDelimiterContinuation(string[] lines, int j) =>
        options.Tables && j > 0 && lines[j].Contains('|') && IsDelimiterRow(lines[j]) && lines[j - 1].Contains('|');

    private bool TryDefinitionList(string[] lines, int definitionStart, int end, string term, out Block? block, out int nextLine)
    {
        block = null;
        nextLine = definitionStart;
        var items = new List<DefinitionItem>();
        var definitions = new List<string[]>();
        int j = definitionStart;

        while (j < end)
        {
            string line = lines[j];
            if (IsBlank(line))
            {
                break;
            }

            string trimmed = line.TrimStart();
            if (!trimmed.StartsWith(": ", StringComparison.Ordinal) && trimmed != ":")
            {
                break;
            }

            var entry = new List<string> { trimmed.Length > 2 ? trimmed[2..] : string.Empty };
            j++;

            while (j < end)
            {
                string next = lines[j];
                if (IsBlank(next))
                {
                    break;
                }

                if (LeadingSpaces(next) >= 2)
                {
                    entry.Add(next.TrimStart());
                    j++;
                    continue;
                }

                if (next.TrimStart().StartsWith(": ", StringComparison.Ordinal))
                {
                    break;
                }

                break;
            }

            definitions.Add([.. entry]);
            continue;
        }

        if (definitions.Count == 0)
        {
            return false;
        }

        // A block may list several terms that share the definitions.
        foreach (string single in term.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            items.Add(new DefinitionItem(single, [.. definitions]));
        }
        block = new DefinitionListBlock { Items = [.. items] };
        nextLine = j;
        return true;
    }

    // -------------------------------------------------------------- helpers

    private static bool IsBlank(string line) => line.Length == 0 || line.All(c => c == ' ' || c == '\t');

    private static int LeadingSpaces(string line)
    {
        int i = 0;
        while (i < line.Length && line[i] == ' ')
        {
            i++;
        }

        return i;
    }

    private static string StripIndent(string line, int indent)
    {
        int strip = Math.Min(indent, LeadingSpaces(line));
        return line[strip..];
    }

    private static bool IsBlockStart(string line)
    {
        string trimmed = line.TrimStart();
        if (trimmed.Length == 0)
        {
            return false;
        }

        if (trimmed[0] is '#' or '>')
        {
            return true;
        }

        if (trimmed.StartsWith("```", StringComparison.Ordinal) || trimmed.StartsWith("~~~", StringComparison.Ordinal))
        {
            return true;
        }

        if (trimmed.StartsWith("$$", StringComparison.Ordinal))
        {
            return true;
        }

        if (trimmed is "[TOC]")
        {
            return true;
        }

        if (TryReadMarker(line, out _, out _, out _, out _))
        {
            return true;
        }

        string t = trimmed.Trim();
        if (t.Length >= 3 && (t.All(c => c == '-' || c == ' ') || t.All(c => c == '*' || c == ' ') || t.All(c => c == '_' || c == ' ')))
        {
            return t.Count(c => c == '-') >= 3 || t.Count(c => c == '*') >= 3 || t.Count(c => c == '_') >= 3;
        }

        return false;
    }
}


