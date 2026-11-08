namespace MdViewer.Core.Markdown;

/// <summary>
/// One Markdown source: normalized text (<c>\r\n</c> and <c>\r</c> become <c>\n</c>,
/// tabs are expanded to 4-column tab stops, BOM removed) plus a line index that
/// maps character offsets back to line numbers.
/// </summary>
public sealed class SourceText
{
    private readonly int[] _lineStarts;

    private SourceText(string text, string[] lines, int[] lineStarts)
    {
        Text = text;
        Lines = lines;
        _lineStarts = lineStarts;
    }

    /// <summary>Normalized document text, LF line endings.</summary>
    public string Text { get; }

    /// <summary>Lines without terminators.</summary>
    public string[] Lines { get; }

    public int LineCount => Lines.Length;

    public int Length => Text.Length;

    public static readonly SourceText Empty = Parse(string.Empty);

    public static SourceText Parse(string? raw)
    {
        raw ??= string.Empty;
        if (raw.Length > 0 && raw[0] == '\uFEFF')
        {
            raw = raw[1..];
        }

        bool needsWork = false;
        foreach (char c in raw)
        {
            if (c is '\r' or '\t')
            {
                needsWork = true;
                break;
            }
        }

        string text = needsWork ? Normalize(raw) : raw;

        var lines = new List<string>(Math.Max(1, text.Length / 40));
        var starts = new List<int>(lines.Capacity);
        int start = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] != '\n')
            {
                continue;
            }

            starts.Add(start);
            lines.Add(text[start..i]);
            start = i + 1;
        }

        starts.Add(start);
        lines.Add(text[start..]);

        return new SourceText(text, lines.ToArray(), starts.ToArray());
    }

    private static string Normalize(string raw)
    {
        var sb = new System.Text.StringBuilder(raw.Length + 16);
        int column = 0;
        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i];
            switch (c)
            {
                case '\r':
                    if (i + 1 < raw.Length && raw[i + 1] == '\n')
                    {
                        i++;
                    }

                    sb.Append('\n');
                    column = 0;
                    break;
                case '\n':
                    sb.Append('\n');
                    column = 0;
                    break;
                case '\t':
                    int advance = 4 - (column % 4);
                    sb.Append(' ', advance);
                    column += advance;
                    break;
                default:
                    sb.Append(c);
                    column++;
                    break;
            }
        }

        return sb.ToString();
    }

    public string Line(int index) => index >= 0 && index < Lines.Length ? Lines[index] : string.Empty;

    /// <summary>Zero-based line number that contains <paramref name="offset"/>.</summary>
    public int LineIndexOf(int offset)
    {
        if (offset <= 0)
        {
            return 0;
        }

        int index = Array.BinarySearch(_lineStarts, offset);
        return index >= 0 ? index : ~index - 1;
    }

    /// <summary>Offset of the first character of a zero-based line.</summary>
    public int LineStart(int line) => line < 0 ? 0 : line >= _lineStarts.Length ? Text.Length : _lineStarts[line];

    /// <summary>1-based column of an offset, for caret/status display.</summary>
    public int ColumnOf(int offset)
    {
        int line = LineIndexOf(offset);
        return offset - LineStart(line) + 1;
    }
}
