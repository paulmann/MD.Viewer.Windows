using System.Text;

namespace MdViewer.Core.Markdown;

public readonly record struct LinkReference(string Url, string? Title);

/// <summary>Footnotes collected in document order; the renderer numbers them.</summary>
public sealed class FootnoteRegistry
{
    private readonly Dictionary<string, int> _byLabel = new(StringComparer.Ordinal);
    private readonly List<FootnoteEntry> _entries = [];
    private int _inlineCounter;

    public IReadOnlyList<FootnoteEntry> Entries => _entries;

    /// <summary>Registers a <c>[^label]</c> reference and returns its 1-based index.</summary>
    public int Reference(string label)
    {
        if (_byLabel.TryGetValue(label, out int existing))
        {
            return existing;
        }

        var entry = new FootnoteEntry(label, _entries.Count + 1, null, -1);
        _entries.Add(entry);
        _byLabel[label] = entry.Index;
        return entry.Index;
    }

    /// <summary>Registers an inline <c>^[text]</c> note; it is numbered in place.</summary>
    public int Inline(string text)
    {
        int index = _entries.Count + 1;
        string label = $"inline-{++_inlineCounter}";
        _entries.Add(new FootnoteEntry(label, index, text, -1));
        return index;
    }

    /// <summary>Attaches the definition text found later in the document.</summary>
    public void Define(string label, string text, int line)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Label == label && _entries[i].Index == _byLabel.GetValueOrDefault(label))
            {
                if (_entries[i].Text is null)
                {
                    _entries[i] = _entries[i] with { Text = text, Line = line };
                }

                return;
            }
        }
    }

    public int IndexOf(string label) => _byLabel.TryGetValue(label, out int index) ? index : 0;

    public FootnoteEntry? Find(int index) => index >= 1 && index <= _entries.Count ? _entries[index - 1] : null;
}

public sealed record FootnoteEntry(string Label, int Index, string? Text, int Line);

/// <summary>CommonMark + GFM inline parser with the MD.Scribe extension set.</summary>
public sealed class InlineParser(MarkdownOptions options, IReadOnlyDictionary<string, LinkReference> references, FootnoteRegistry footnotes)
{
    private static readonly char[] Escapable =
        ['!', '"', '#', '$', '%', '&', '\'', '(', ')', '*', '+', ',', '-', '.', '/', ':', ';', '<', '=', '>', '?', '@', '[', '\\', ']', '^', '_', '`', '{', '|', '}', '~'];

    public Inline[] Parse(string text, int line = 0)
    {
        int index = 0;
        var nodes = ParseSequence(text, ref index, '\0', 0);
        return nodes.Count == 0 ? [] : nodes.ToArray();
    }

    private List<Inline> ParseSequence(string s, ref int i, char terminator, int runLength)
    {
        var nodes = new List<Inline>(8);
        var buffer = new StringBuilder(64);

        void FlushBuffer()
        {
            if (buffer.Length == 0)
            {
                return;
            }

            string chunk = buffer.ToString();
            buffer.Clear();
            chunk = Collapse(chunk);
            if (chunk.Length == 0)
            {
                return;
            }

            nodes.Add(new TextInline(Entities.Decode(chunk)));
        }

        while (i < s.Length)
        {
            char c = s[i];

            if (terminator != 0 && c == terminator && CountRun(s, i, c) >= runLength)
            {
                int run = CountRun(s, i, c);
                if (run == runLength || (runLength == 1 && run > 1))
                {
                    break;
                }
            }

            switch (c)
            {
                case '\\':
                    if (i + 1 < s.Length && s[i + 1] == '\n')
                    {
                        FlushBuffer();
                        nodes.Add(new LineBreakInline(true));
                        i += 2;
                        TrimTrailingSpaces(buffer);
                        continue;
                    }

                    if (i + 1 < s.Length && Array.IndexOf(Escapable, s[i + 1]) >= 0)
                    {
                        buffer.Append(s[i + 1]);
                        i += 2;
                        continue;
                    }

                    buffer.Append(c);
                    i++;
                    continue;

                case '\n':
                {
                    bool hard = EndsWithTwoSpaces(buffer) || options.DoubleLineBreaks;
                    TrimTrailingSpaces(buffer);
                    FlushBuffer();
                    nodes.Add(new LineBreakInline(hard));
                    i++;
                    while (i < s.Length && s[i] == ' ')
                    {
                        i++;
                    }

                    continue;
                }

                case '`':
                {
                    int tickRun = CountRun(s, i, '`');
                    int close = FindBacktickClose(s, i + tickRun, tickRun);
                    if (close < 0)
                    {
                        buffer.Append('`', tickRun);
                        i += tickRun;
                        continue;
                    }

                    FlushBuffer();
                    string code = s[(i + tickRun)..close].Replace('\n', ' ');
                    if (code.Length > 2 && code[0] == ' ' && code[^1] == ' ' && code.Trim().Length > 0)
                    {
                        code = code[1..^1];
                    }

                    nodes.Add(new CodeInline(code));
                    i = close + tickRun;
                    continue;
                }

                case '!' when i + 1 < s.Length && s[i + 1] == '[':
                {
                    if (TryParseImage(s, ref i, out Inline? image))
                    {
                        FlushBuffer();
                        nodes.Add(image!);
                        continue;
                    }

                    buffer.Append(c);
                    i++;
                    continue;
                }

                case '[':
                {
                    if (TryParseLinkOrFootnote(s, ref i, out Inline? link))
                    {
                        FlushBuffer();
                        nodes.Add(link!);
                        continue;
                    }

                    buffer.Append(c);
                    i++;
                    continue;
                }

                case '^' when options.Footnotes && i + 1 < s.Length && s[i + 1] == '[':
                {
                    if (TryParseInlineNote(s, ref i, out Inline? note))
                    {
                        FlushBuffer();
                        nodes.Add(note!);
                        continue;
                    }

                    buffer.Append(c);
                    i++;
                    continue;
                }

                case '*':
                case '_':
                case '~':
                case '=':
                case '^':
                {
                    int run = CountRun(s, i, c);
                    if (TryParseStyled(s, ref i, c, run, out Inline? styled))
                    {
                        FlushBuffer();
                        nodes.Add(styled!);
                        continue;
                    }

                    buffer.Append(c, run);
                    i += run;
                    continue;
                }

                case '$' when options.Math:
                {
                    if (TryParseMath(s, ref i, out Inline? math))
                    {
                        FlushBuffer();
                        nodes.Add(math!);
                        continue;
                    }

                    buffer.Append(c);
                    i++;
                    continue;
                }

                case ':' when options.Emoji:
                {
                    int probe = i;
                    if (Emoji.TryParseAt(s, ref probe, out string? emoji))
                    {
                        FlushBuffer();
                        nodes.Add(new TextInline(emoji!));
                        i = probe;
                        continue;
                    }

                    buffer.Append(c);
                    i++;
                    continue;
                }

                case '<':
                {
                    if (TryParseAutolink(s, ref i, out Inline? auto))
                    {
                        FlushBuffer();
                        nodes.Add(auto!);
                        continue;
                    }

                    buffer.Append(c);
                    i++;
                    continue;
                }

                case 'h' or 'H' or 'w' or 'W' or 'f' or 'F' when options.Autolinks:
                {
                    if (TryParseBareUrl(s, ref i, out Inline? bare))
                    {
                        FlushBuffer();
                        nodes.Add(bare!);
                        continue;
                    }

                    buffer.Append(c);
                    i++;
                    continue;
                }

                default:
                    buffer.Append(c);
                    i++;
                    continue;
            }
        }

        TrimTrailingSpaces(buffer);
        FlushBuffer();
        return nodes;
    }

    // ------------------------------------------------------------- emphasis

    private bool TryParseStyled(string s, ref int i, char marker, int run, out Inline? result)
    {
        result = null;

        if (marker == '^' && options.SubSuperscript)
        {
            return TryParseDelimited(s, ref i, '^', 1, InlineStyle.Superscript, out result, allowSpaces: false);
        }

        if (marker == '~' && options.SubSuperscript)
        {
            if (run >= 2 && options.Strikethrough)
            {
                if (TryParseDelimited(s, ref i, '~', 2, InlineStyle.Strike, out result, allowSpaces: true))
                {
                    return true;
                }
            }

            if (run == 1)
            {
                return TryParseDelimited(s, ref i, '~', 1, InlineStyle.Subscript, out result, allowSpaces: false);
            }

            return false;
        }

        if (marker == '=' && options.Marks)
        {
            return TryParseDelimited(s, ref i, '=', 2, InlineStyle.Mark, out result, allowSpaces: true);
        }

        if (marker is not ('*' or '_'))
        {
            return false;
        }

        int use = run >= 3 ? 3 : run;
        int start = i;
        int after = i + use;
        if (after >= s.Length || char.IsWhiteSpace(s[after]))
        {
            return false;
        }

        if (marker == '_' && i > 0 && char.IsLetterOrDigit(s[i - 1]))
        {
            // intraword_underscores are not emphasis
            return false;
        }

        int close = FindCloser(s, after, marker, use);
        if (close < 0)
        {
            return false;
        }

        int innerStart = after;
        int innerEnd = close;
        int innerIndex = innerStart;
        var children = ParseSequence(s[..innerEnd], ref innerIndex, '\0', 0);
        Inline[] kids = children.Count == 0 ? [] : children.ToArray();

        result = use switch
        {
            1 => new StyledInline(InlineStyle.Emphasis, kids),
            2 => new StyledInline(InlineStyle.Strong, kids),
            _ => new StyledInline(InlineStyle.Strong, [new StyledInline(InlineStyle.Emphasis, kids)]),
        };

        i = close + use;
        return true;
    }

    private bool TryParseDelimited(string s, ref int i, char marker, int run, InlineStyle style, out Inline? result, bool allowSpaces)
    {
        result = null;
        int start = i + run;
        if (start >= s.Length)
        {
            return false;
        }

        if (!allowSpaces && char.IsWhiteSpace(s[start]))
        {
            return false;
        }

        int close = -1;
        for (int j = start; j < s.Length; j++)
        {
            if (s[j] == '\\')
            {
                j++;
                continue;
            }

            if (s[j] != marker || CountRun(s, j, marker) < run)
            {
                continue;
            }

            if (!allowSpaces && char.IsWhiteSpace(s[j - 1]))
            {
                continue;
            }

            close = j;
            break;
        }

        if (close < 0)
        {
            return false;
        }

        int innerIndex = start;
        var children = ParseSequence(s[..close], ref innerIndex, '\0', 0);
        result = new StyledInline(style, children.Count == 0 ? [] : children.ToArray());
        i = close + run;
        return true;
    }

    private static int FindCloser(string s, int from, char marker, int need)
    {
        for (int j = from; j < s.Length; j++)
        {
            char c = s[j];
            if (c == '\\')
            {
                j++;
                continue;
            }

            if (c == '`')
            {
                int tickRun = CountRun(s, j, '`');
                int close = FindBacktickClose(s, j + tickRun, tickRun);
                if (close > 0)
                {
                    j = close + tickRun - 1;
                }

                continue;
            }

            if (c != marker)
            {
                continue;
            }

            int run = CountRun(s, j, marker);
            if (run < need)
            {
                j += run - 1;
                continue;
            }

            bool canClose = j > 0 && !char.IsWhiteSpace(s[j - 1]);
            if (!canClose)
            {
                j += run - 1;
                continue;
            }

            return j;
        }

        return -1;
    }

    private static int FindBacktickClose(string s, int from, int run)
    {
        for (int j = from; j < s.Length; j++)
        {
            if (s[j] != '`')
            {
                continue;
            }

            int candidate = CountRun(s, j, '`');
            if (candidate == run)
            {
                return j;
            }

            j += candidate - 1;
        }

        return -1;
    }

    // ----------------------------------------------------------------- links

    /// <summary>Parses an inline note <c>^[text]</c> (MD.Scribe / PHP-Markdown style).</summary>
    private bool TryParseInlineNote(string s, ref int i, out Inline? result)
    {
        result = null;
        int depth = 0;
        for (int j = i + 2; j < s.Length; j++)
        {
            if (s[j] == '\\')
            {
                j++;
                continue;
            }

            if (s[j] == '[')
            {
                depth++;
                continue;
            }

            if (s[j] != ']')
            {
                continue;
            }

            if (depth > 0)
            {
                depth--;
                continue;
            }

            string text = s[(i + 2)..j].Trim();
            if (text.Length == 0)
            {
                return false;
            }

            int index = footnotes.Inline(text);
            result = new FootnoteRefInline(index, $"inline-{index}");
            i = j + 1;
            return true;
        }

        return false;
    }

    private bool TryParseLinkOrFootnote(string s, ref int i, out Inline? result)
    {
        result = null;
        int start = i;

        if (options.Footnotes && start + 2 < s.Length && s[start + 1] == '^')
        {
            int closeBracket = s.IndexOf(']', start + 2);
            if (closeBracket > start + 2)
            {
                string label = s[(start + 2)..closeBracket];
                if (!label.Contains('\n'))
                {
                    int index = footnotes.Reference(label);
                    result = new FootnoteRefInline(index, label);
                    i = closeBracket + 1;
                    return true;
                }
            }
        }

        if (options.Footnotes && start + 1 < s.Length && s[start + 1] == '^' && start + 2 < s.Length && s[start + 2] == '[')
        {
            // inline note: ^[text]
            int depth = 0;
            for (int j = start + 2; j < s.Length; j++)
            {
                if (s[j] == '\\')
                {
                    j++;
                    continue;
                }

                if (s[j] == '[')
                {
                    depth++;
                }
                else if (s[j] == ']')
                {
                    if (depth == 0)
                    {
                        string text = s[(start + 3)..j];
                        int index = footnotes.Inline(text);
                        result = new FootnoteRefInline(index, $"inline-{index}");
                        i = j + 1;
                        return true;
                    }

                    depth--;
                }
            }
        }

        int end = FindClosingBracket(s, start);
        if (end < 0)
        {
            return false;
        }

        string label2 = s[(start + 1)..end];
        int after = end + 1;

        if (after < s.Length && s[after] == '(')
        {
            int closeParen = ParseDestination(s, after, out string url, out string? title);
            if (closeParen > 0)
            {
                Inline[] children = ParseChildren(label2);
                result = new LinkInline(SanitizeUrl(url), title, children);
                i = closeParen;
                return true;
            }
        }
        else if (after < s.Length && s[after] == '[')
        {
            int refEnd = s.IndexOf(']', after + 1);
            if (refEnd > after)
            {
                string refLabel = s[(after + 1)..refEnd];
                if (refLabel.Length == 0)
                {
                    refLabel = label2;
                }

                if (references.TryGetValue(NormalizeLabel(refLabel), out LinkReference reference))
                {
                    result = new LinkInline(SanitizeUrl(reference.Url), reference.Title, ParseChildren(label2));
                    i = refEnd + 1;
                    return true;
                }
            }
        }
        else if (references.TryGetValue(NormalizeLabel(label2), out LinkReference shortcut))
        {
            result = new LinkInline(SanitizeUrl(shortcut.Url), shortcut.Title, ParseChildren(label2));
            i = after;
            return true;
        }

        return false;
    }

    private bool TryParseImage(string s, ref int i, out Inline? result)
    {
        result = null;
        int start = i;
        int bracket = start + 1;
        int end = FindClosingBracket(s, bracket);
        if (end < 0)
        {
            return false;
        }

        string alt = s[(bracket + 1)..end];
        int after = end + 1;
        string url;
        string? title = null;
        int next;

        if (after < s.Length && s[after] == '(')
        {
            int closeParen = ParseDestination(s, after, out url, out title);
            if (closeParen < 0)
            {
                return false;
            }

            next = closeParen;
        }
        else if (after < s.Length && s[after] == '[')
        {
            int refEnd = s.IndexOf(']', after + 1);
            if (refEnd < 0)
            {
                return false;
            }

            string refLabel = s[(after + 1)..refEnd];
            if (refLabel.Length == 0)
            {
                refLabel = alt;
            }

            if (!references.TryGetValue(NormalizeLabel(refLabel), out LinkReference reference))
            {
                return false;
            }

            url = reference.Url;
            title = reference.Title;
            next = refEnd + 1;
        }
        else if (references.TryGetValue(NormalizeLabel(alt), out LinkReference shortcut))
        {
            url = shortcut.Url;
            title = shortcut.Title;
            next = after;
        }
        else
        {
            return false;
        }

        int? width = null;
        if (options.AttributeSyntax && next < s.Length && s[next] == '{')
        {
            int attrEnd = s.IndexOf('}', next);
            if (attrEnd > next && attrEnd - next < 120)
            {
                width = ParseWidthAttribute(s[(next + 1)..attrEnd]);
                next = attrEnd + 1;
            }
        }

        result = new ImageInline(SanitizeUrl(url), alt, title, width);
        i = next;
        return true;
    }

    private static int? ParseWidthAttribute(string attributes)
    {
        foreach (string part in attributes.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries))
        {
            if (!part.StartsWith("width=", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string value = part[6..].TrimEnd('p', 'x', '%');
            if (int.TryParse(value, out int width) && width > 0)
            {
                return width;
            }
        }

        return null;
    }

    private Inline[] ParseChildren(string text)
    {
        int index = 0;
        var nodes = ParseSequence(text, ref index, '\0', 0);
        return nodes.Count == 0 ? [] : nodes.ToArray();
    }

    private static int FindClosingBracket(string s, int open)
    {
        int depth = 0;
        for (int j = open + 1; j < s.Length; j++)
        {
            char c = s[j];
            if (c == '\\')
            {
                j++;
                continue;
            }

            if (c == '`')
            {
                int run = CountRun(s, j, '`');
                int close = FindBacktickClose(s, j + run, run);
                if (close > 0)
                {
                    j = close + run - 1;
                }

                continue;
            }

            if (c == '[')
            {
                depth++;
            }
            else if (c == ']')
            {
                if (depth == 0)
                {
                    return j;
                }

                depth--;
            }
        }

        return -1;
    }

    /// <summary>Parses <c>(dest "title")</c>; returns the index after <c>)</c> or -1.</summary>
    private static int ParseDestination(string s, int openParen, out string url, out string? title)
    {
        url = string.Empty;
        title = null;
        int j = openParen + 1;

        while (j < s.Length && s[j] is ' ' or '\t')
        {
            j++;
        }

        var sb = new StringBuilder(32);
        if (j < s.Length && s[j] == '<')
        {
            j++;
            while (j < s.Length && s[j] != '>')
            {
                if (s[j] == '\\' && j + 1 < s.Length)
                {
                    sb.Append(s[j + 1]);
                    j += 2;
                    continue;
                }

                sb.Append(s[j]);
                j++;
            }

            if (j >= s.Length)
            {
                return -1;
            }

            j++;
        }
        else
        {
            int depth = 0;
            while (j < s.Length)
            {
                char c = s[j];
                if (c == '\\' && j + 1 < s.Length)
                {
                    sb.Append(s[j + 1]);
                    j += 2;
                    continue;
                }

                if (c == '(')
                {
                    depth++;
                }
                else if (c == ')')
                {
                    if (depth == 0)
                    {
                        break;
                    }

                    depth--;
                }
                else if (char.IsWhiteSpace(c))
                {
                    break;
                }

                sb.Append(c);
                j++;
            }
        }

        url = sb.ToString();

        while (j < s.Length && s[j] is ' ' or '\t')
        {
            j++;
        }

        if (j < s.Length && (s[j] == '"' || s[j] == '\'' || s[j] == '('))
        {
            char quote = s[j] == '(' ? ')' : s[j];
            int closeQuote = s.IndexOf(quote, j + 1);
            if (closeQuote > j)
            {
                title = s[(j + 1)..closeQuote];
                j = closeQuote + 1;
            }
        }

        while (j < s.Length && s[j] is ' ' or '\t')
        {
            j++;
        }

        if (j >= s.Length || s[j] != ')')
        {
            return -1;
        }

        return j + 1;
    }

    public static string NormalizeLabel(string label)
    {
        var sb = new StringBuilder(label.Length);
        bool space = false;
        foreach (char c in label.Trim())
        {
            if (char.IsWhiteSpace(c))
            {
                space = true;
                continue;
            }

            if (space && sb.Length > 0)
            {
                sb.Append(' ');
                space = false;
            }

            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }

    // ------------------------------------------------------------------ math

    private bool TryParseMath(string s, ref int i, out Inline? result)
    {
        result = null;
        int start = i;
        bool display = start + 1 < s.Length && s[start + 1] == '$';
        int run = display ? 2 : 1;
        int contentStart = start + run;

        if (contentStart >= s.Length || s[contentStart] == '$')
        {
            return false;
        }

        if (char.IsWhiteSpace(s[contentStart]))
        {
            return false;
        }

        for (int j = contentStart; j < s.Length; j++)
        {
            if (s[j] == '\\')
            {
                j++;
                continue;
            }

            if (s[j] != '$')
            {
                continue;
            }

            if (!display)
            {
                if (j == contentStart || char.IsWhiteSpace(s[j - 1]))
                {
                    continue;
                }

                if (j + 1 < s.Length && char.IsDigit(s[j + 1]))
                {
                    // "$5 and $10" stays prose
                    continue;
                }

                string tex = s[contentStart..j];
                if (tex.Contains('\n'))
                {
                    return false;
                }

                result = new MathInline(tex, false);
                i = j + 1;
                return true;
            }

            if (j + 1 < s.Length && s[j + 1] == '$')
            {
                result = new MathInline(s[contentStart..j], true);
                i = j + 2;
                return true;
            }
        }

        return false;
    }

    // -------------------------------------------------------------- autolinks

    private bool TryParseAutolink(string s, ref int i, out Inline? result)
    {
        result = null;
        int close = s.IndexOf('>', i + 1);
        if (close < 0)
        {
            return false;
        }

        string inner = s[(i + 1)..close];
        if (inner.Length is < 4 or > 2048 || inner.Contains(' '))
        {
            return false;
        }

        bool looksLikeUrl = inner.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || inner.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || inner.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase);

        bool looksLikeMail = inner.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
            || (inner.Contains('@') && inner.Contains('.') && !inner.Contains(':'));

        if (!looksLikeUrl && !looksLikeMail)
        {
            return false;
        }

        string href = looksLikeMail && !inner.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
            ? "mailto:" + inner
            : inner;

        result = new LinkInline(SanitizeUrl(href), null, [new TextInline(inner)]);
        i = close + 1;
        return true;
    }

    private bool TryParseBareUrl(string s, ref int i, out Inline? result)
    {
        result = null;
        if (i > 0)
        {
            char prev = s[i - 1];
            if (char.IsLetterOrDigit(prev) || prev is '/' or '_' or ':' or '~')
            {
                return false;
            }
        }

        int end;
        string prefix;
        if (s.AsSpan(i).StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            prefix = "https://";
        }
        else if (s.AsSpan(i).StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            prefix = "http://";
        }
        else if (s.AsSpan(i).StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
        {
            prefix = "ftp://";
        }
        else if (s.AsSpan(i).StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            prefix = "www.";
        }
        else
        {
            return false;
        }

        end = i + prefix.Length;
        while (end < s.Length && !char.IsWhiteSpace(s[end]) && s[end] != '<' && s[end] != '>' && s[end] != '"')
        {
            end++;
        }

        while (end > i + prefix.Length)
        {
            char last = s[end - 1];
            if (last is '.' or ',' or ';' or ':' or '!' or '?' or ')' or ']' or '}' or '\'' or '*' or '_' or '~')
            {
                // keep balanced closing parens
                if (last == ')' && s[i..end].Count(ch => ch == '(') >= s[i..end].Count(ch => ch == ')'))
                {
                    break;
                }

                end--;
                continue;
            }

            break;
        }

        if (end <= i + prefix.Length)
        {
            return false;
        }

        string url = s[i..end];
        if (!url.Contains('.'))
        {
            return false;
        }

        string href = prefix == "www." ? "https://" + url : url;
        result = new LinkInline(SanitizeUrl(href), null, [new TextInline(url)]);
        i = end;
        return true;
    }

    public static string SanitizeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        string trimmed = url.Trim();
        string lower = trimmed.ToLowerInvariant();
        if (lower.StartsWith("javascript:") || lower.StartsWith("vbscript:") || lower.StartsWith("data:text/html"))
        {
            return string.Empty;
        }

        return trimmed;
    }

    // --------------------------------------------------------------- helpers

    private static int CountRun(string s, int start, char c)
    {
        int i = start;
        while (i < s.Length && s[i] == c)
        {
            i++;
        }

        return i - start;
    }

    private static bool EndsWithTwoSpaces(StringBuilder sb) =>
        sb.Length >= 2 && sb[^1] == ' ' && sb[^2] == ' ';

    private static void TrimTrailingSpaces(StringBuilder sb)
    {
        while (sb.Length > 0 && sb[^1] == ' ')
        {
            sb.Length--;
        }
    }

    private string Collapse(string text)
    {
        if (text.IndexOf("  ", StringComparison.Ordinal) < 0)
        {
            return text;
        }

        var sb = new StringBuilder(text.Length);
        bool space = false;
        foreach (char c in text)
        {
            if (c == ' ')
            {
                space = true;
                continue;
            }

            if (space && sb.Length > 0)
            {
                sb.Append(' ');
            }

            space = false;
            sb.Append(c);
        }

        if (space && sb.Length > 0)
        {
            sb.Append(' ');
        }

        return sb.ToString();
    }

    /// <summary>Renders inline nodes back to plain text (TOC, search, exports).</summary>
    public static string PlainText(IReadOnlyList<Inline> inlines)
    {
        var sb = new StringBuilder();
        AppendPlainText(inlines, sb);
        return sb.ToString();
    }

    private static void AppendPlainText(IReadOnlyList<Inline> inlines, StringBuilder sb)
    {
        foreach (var inline in inlines)
        {
            switch (inline)
            {
                case TextInline text:
                    sb.Append(text.Text);
                    break;
                case CodeInline code:
                    sb.Append(code.Text);
                    break;
                case StyledInline styled:
                    AppendPlainText(styled.Children, sb);
                    break;
                case LinkInline link:
                    AppendPlainText(link.Children, sb);
                    break;
                case ImageInline image:
                    sb.Append(image.Alt);
                    break;
                case MathInline math:
                    sb.Append(math.Tex);
                    break;
                case LineBreakInline:
                    sb.Append(' ');
                    break;
                case FootnoteRefInline foot:
                    sb.Append('[').Append(foot.Index).Append(']');
                    break;
            }
        }
    }
}

