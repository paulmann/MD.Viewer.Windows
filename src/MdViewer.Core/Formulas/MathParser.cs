using System.Globalization;
using System.Text;

namespace MdViewer.Core.Formulas;

/// <summary>
/// Recursive-descent TeX math reader. It covers the constructs people actually
/// write in Markdown — fractions, roots, scripts, fences, matrices, accents,
/// operators with limits, text and spacing — and never throws: anything it cannot
/// read becomes <see cref="MathUnsupported"/> so the renderer can show the source.
/// </summary>
public sealed class MathParser
{
    private static readonly Dictionary<string, string> Negations = new(StringComparer.Ordinal)
    {
        ["="] = "≠", ["<"] = "≮", [">"] = "≯", ["∈"] = "∉", ["∋"] = "∌",
        ["⊂"] = "⊄", ["⊃"] = "⊅", ["⊆"] = "⊈", ["⊇"] = "⊉", ["≡"] = "≢",
        ["≈"] = "≉", ["≤"] = "≰", ["≥"] = "≱", ["∼"] = "≁", ["∣"] = "∤",
        ["∥"] = "∦", ["→"] = "↛", ["⇒"] = "⇏", ["←"] = "↚", ["∃"] = "∄",
    };

    private readonly string _tex;
    private readonly bool _display;
    private int _i;
    private bool _errors;

    private MathParser(string tex, bool display)
    {
        _tex = tex ?? string.Empty;
        _display = display;
    }

    public static MathLines Parse(string tex, bool display) => new MathParser(tex, display).ParseLines();

    /// <summary>Plain-text form used by the text/HTML exporters.</summary>
    public static string ToPlainText(string tex, bool display)
    {
        var sb = new StringBuilder();
        MathLines lines = Parse(tex, display);
        foreach (MathRow row in lines.Rows)
        {
            if (sb.Length > 0)
            {
                sb.Append('\n');
            }

            AppendPlain(row, sb);
        }

        return sb.ToString();
    }

    private static void AppendPlain(MathNode node, StringBuilder sb)
    {
        switch (node)
        {
            case MathAtom atom:
                sb.Append(atom.Text);
                break;
            case MathText text:
                sb.Append(text.Text);
                break;
            case MathRow row:
                foreach (MathNode child in row.Children)
                {
                    AppendPlain(child, sb);
                }

                break;
            case MathFraction fraction:
                AppendPlain(fraction.Numerator, sb);
                sb.Append('/');
                AppendPlain(fraction.Denominator, sb);
                break;
            case MathRadical radical:
                sb.Append('√');
                AppendPlain(radical.Body, sb);
                break;
            case MathScripts scripts:
                if (scripts.Base is not null)
                {
                    AppendPlain(scripts.Base, sb);
                }

                if (scripts.Sub is not null)
                {
                    sb.Append('_');
                    AppendPlain(scripts.Sub, sb);
                }

                if (scripts.Sup is not null)
                {
                    sb.Append('^');
                    AppendPlain(scripts.Sup, sb);
                }

                break;
            case MathLeftRight delimited:
                sb.Append(delimited.Open);
                AppendPlain(delimited.Body, sb);
                sb.Append(delimited.Close);
                break;
            case MathAccent accent:
                AppendPlain(accent.Body, sb);
                break;
            case MathMatrix matrix:
                sb.Append(matrix.Open);
                for (int r = 0; r < matrix.Rows.Count; r++)
                {
                    if (r > 0)
                    {
                        sb.Append("; ");
                    }

                    IReadOnlyList<MathNode> cells = matrix.Rows[r];
                    for (int c = 0; c < cells.Count; c++)
                    {
                        if (c > 0)
                        {
                            sb.Append(", ");
                        }

                        AppendPlain(cells[c], sb);
                    }
                }

                sb.Append(matrix.Close);
                break;
            case MathSpace space:
                sb.Append(' ', (int)MathF.Round(space.Em * 3f));
                break;
            case MathStyle style:
                AppendPlain(style.Body, sb);
                break;
            case MathUnsupported unsupported:
                sb.Append(unsupported.Tex);
                break;
        }
    }

    // ------------------------------------------------------------------ lines

    private MathLines ParseLines()
    {
        var rows = new List<MathRow>();
        var nodes = new List<MathNode>();

        while (!AtEnd)
        {
            SkipSpaces();
            if (AtEnd)
            {
                break;
            }

            if (Peek() == '\\' && PeekAt(1) == '\\')
            {
                _i += 2;
                rows.Add(new MathRow(nodes));
                nodes = [];
                continue;
            }

            MathNode? node = ParseAtom();
            if (node is null)
            {
                continue;
            }

            nodes.Add(ParseScripts(node));
        }

        if (nodes.Count > 0 || rows.Count == 0)
        {
            rows.Add(new MathRow(nodes));
        }

        return new MathLines(rows, _display) { HasErrors = _errors };
    }

    /// <summary>Parses until <paramref name="stop"/> (left unconsumed) or the end.</summary>
    private MathRow ParseList(char? stop, bool stopAtRight = false)
    {
        var nodes = new List<MathNode>();

        while (!AtEnd)
        {
            SkipSpaces();
            if (AtEnd)
            {
                break;
            }

            char c = Peek();
            if (stop is char terminator && c == terminator)
            {
                break;
            }

            if (stopAtRight && c == '\\' && PeekWord() == "right")
            {
                break;
            }

            if (c == '\\' && PeekAt(1) == '\\')
            {
                // A row break inside a group is not a matrix row: keep the rows apart
                // with a space instead of failing the whole formula.
                _i += 2;
                nodes.Add(new MathSpace(0.6f));
                continue;
            }

            MathNode? node = ParseAtom();
            if (node is null)
            {
                continue;
            }

            nodes.Add(ParseScripts(node));
        }

        return new MathRow(nodes);
    }

    private MathNode ParseScripts(MathNode baseNode)
    {
        MathNode? sub = null;
        MathNode? sup = null;
        bool any = false;

        while (true)
        {
            SkipSpaces();
            if (AtEnd)
            {
                break;
            }

            char c = Peek();
            if (c == '\'')
            {
                _i++;
                MathNode prime = new MathAtom("′", MathAtomKind.Ordinary);
                sup = sup is null ? prime : new MathRow([sup, prime]);
                any = true;
                continue;
            }

            if (c != '^' && c != '_')
            {
                break;
            }

            _i++;
            MathNode argument = ParseArgument();
            if (c == '^')
            {
                sup = sup is null ? argument : new MathRow([sup, argument]);
            }
            else
            {
                sub = sub is null ? argument : new MathRow([sub, argument]);
            }

            any = true;
        }

        if (!any)
        {
            return baseNode;
        }

        return new MathScripts(baseNode, sub, sup);
    }

    private MathNode? ParseAtom()
    {
        SkipSpaces();
        if (AtEnd)
        {
            return null;
        }

        char c = Peek();
        switch (c)
        {
            case '{':
                _i++;
                {
                    MathRow group = ParseList('}');
                    Expect('}');
                    return group;
                }

            case '}':
                _i++;
                return null;

            case '&':
                _i++;
                return new MathSpace(0.6f);

            case '\\':
                return ParseCommand();

            case '^' or '_':
                return new MathAtom(string.Empty, MathAtomKind.Ordinary);

            case '~':
                _i++;
                return new MathSpace(0.35f);

            default:
                return ParseCharacter();
        }
    }

    private MathNode ParseCharacter()
    {
        char c = Peek();
        if (char.IsAsciiLetter(c))
        {
            int start = _i;
            while (!AtEnd && char.IsAsciiLetter(Peek()))
            {
                _i++;
            }

            return new MathAtom(_tex[start.._i], MathAtomKind.Ordinary, italic: true);
        }

        if (char.IsAsciiDigit(c))
        {
            int start = _i;
            while (!AtEnd && (char.IsAsciiDigit(Peek()) || (Peek() == '.' && char.IsAsciiDigit(PeekAt(1)))))
            {
                _i++;
            }

            return new MathAtom(_tex[start.._i], MathAtomKind.Ordinary);
        }

        _i++;
        return c switch
        {
            '+' or '-' => new MathAtom(c == '-' ? "−" : "+", MathAtomKind.Binary),
            '*' => new MathAtom("∗", MathAtomKind.Binary),
            '/' => new MathAtom("/", MathAtomKind.Ordinary),
            '=' => new MathAtom("=", MathAtomKind.Relation),
            '<' => new MathAtom("<", MathAtomKind.Relation),
            '>' => new MathAtom(">", MathAtomKind.Relation),
            '(' or '[' => new MathAtom(c.ToString(), MathAtomKind.Open),
            ')' or ']' => new MathAtom(c.ToString(), MathAtomKind.Close),
            ',' or ';' => new MathAtom(c.ToString(), MathAtomKind.Punctuation),
            ':' => new MathAtom(":", MathAtomKind.Punctuation),
            '!' => new MathAtom("!", MathAtomKind.Ordinary),
            '?' => new MathAtom("?", MathAtomKind.Ordinary),
            '|' => new MathAtom("|", MathAtomKind.Ordinary),
            '.' => new MathAtom(".", MathAtomKind.Ordinary),
            '\'' => new MathAtom("′", MathAtomKind.Ordinary),
            '\u2212' => new MathAtom("−", MathAtomKind.Binary),
            _ => new MathAtom(c.ToString(), MathAtomKind.Ordinary, italic: char.IsLetter(c) && !char.IsUpper(c)),
        };
    }

    // --------------------------------------------------------------- commands

    private string PeekWord()
    {
        if (Peek() != '\\')
        {
            return string.Empty;
        }

        int j = _i + 1;
        while (j < _tex.Length && char.IsAsciiLetter(_tex[j]))
        {
            j++;
        }

        return _tex[(_i + 1)..j];
    }

    private string ReadCommandName()
    {
        // assumes the cursor is on the backslash
        _i++;
        if (AtEnd)
        {
            return string.Empty;
        }

        char c = Peek();
        if (!char.IsAsciiLetter(c))
        {
            _i++;
            return c.ToString();
        }

        int start = _i;
        while (!AtEnd && char.IsAsciiLetter(Peek()))
        {
            _i++;
        }

        return _tex[start.._i];
    }

    private MathNode? ParseCommand()
    {
        string command = ReadCommandName();
        switch (command)
        {
            case "":
                return null;

            case "\\":
                return new MathSpace(0.6f);

            case ",":
                return new MathSpace(0.16f);

            case ":" or ">":
                return new MathSpace(0.22f);

            case ";":
                return new MathSpace(0.28f);

            case "!":
                return new MathSpace(-0.16f);

            case " ":
                return new MathSpace(0.25f);

            case "{" or "}" or "%" or "$" or "&" or "#" or "_":
            {
                MathAtomKind atomKind = command == "{" ? MathAtomKind.Open
                    : command == "}" ? MathAtomKind.Close
                    : MathAtomKind.Ordinary;
                return new MathAtom(command, atomKind);
            }

            case "|":
                return new MathAtom("‖", MathAtomKind.Ordinary);

            case "frac" or "dfrac" or "tfrac" or "cfrac":
            {
                MathNode numerator = ParseArgument();
                MathNode denominator = ParseArgument();
                MathNode fraction = new MathFraction(numerator, denominator, barred: true);
                return command == "dfrac" ? new MathStyle(true, fraction) : fraction;
            }

            case "binom" or "dbinom" or "tbinom":
            {
                MathNode top = ParseArgument();
                MathNode bottom = ParseArgument();
                var fraction = new MathFraction(top, bottom, barred: false);
                return new MathLeftRight("(", ")", fraction);
            }

            case "sqrt":
            {
                MathNode? index = null;
                SkipSpaces();
                if (!AtEnd && Peek() == '[')
                {
                    _i++;
                    index = ParseList(']');
                    Expect(']');
                }

                MathNode body = ParseArgument();
                return new MathRadical(body, index);
            }

            case "text" or "textrm" or "textnormal" or "mbox" or "textup":
                return new MathText(ReadRawGroup(), MathTextStyle.Upright);

            case "textbf":
                return new MathText(ReadRawGroup(), MathTextStyle.Bold);

            case "textit" or "emph":
                return new MathText(ReadRawGroup(), MathTextStyle.Italic);

            case "texttt":
                return new MathText(ReadRawGroup(), MathTextStyle.Monospace);

            case "mathrm" or "mathup":
                return new MathText(ReadRawGroup(), MathTextStyle.Upright);

            case "mathbf":
                return new MathText(ReadRawGroup(), MathTextStyle.Bold);

            case "mathit":
                return new MathText(ReadRawGroup(), MathTextStyle.Italic);

            case "mathsf":
                return new MathText(ReadRawGroup(), MathTextStyle.SansSerif);

            case "mathtt":
                return new MathText(ReadRawGroup(), MathTextStyle.Monospace);

            case "mathbb":
                return new MathText(TransformRaw(ReadRawGroup(), MathSymbols.Blackboard), MathTextStyle.Upright);

            case "mathcal" or "mathscr":
                return new MathText(TransformRaw(ReadRawGroup(), MathSymbols.Script), MathTextStyle.Normal);

            case "mathfrak":
                return new MathText(TransformRaw(ReadRawGroup(), MathSymbols.Fraktur), MathTextStyle.Normal);

            case "operatorname" or "operatorname*":
            {
                string name = ReadRawGroup().Trim();
                bool limits = command.EndsWith('*') || MathSymbols.TakesLimits(name);
                return new MathText(name, MathTextStyle.Upright)
                {
                    Kind = limits ? MathAtomKind.LargeOperator : MathAtomKind.Ordinary,
                };
            }

            case "overline" or "bar" or "hat" or "widehat" or "vec" or "overrightarrow" or
                 "dot" or "ddot" or "tilde" or "widetilde" or "check" or "breve" or "acute" or "grave":
            {
                MathNode body = ParseArgument();
                string mark = MathSymbols.TryGetAccent(command, out string glyph, out _) ? glyph : "\u00AF";
                return new MathAccent(body, mark, under: false);
            }

            case "underline":
            {
                MathNode body = ParseArgument();
                return new MathAccent(body, "\u005F", under: true);
            }

            case "left":
            {
                string open = ReadDelimiter();
                MathRow body = ParseList(null, stopAtRight: true);
                string close = string.Empty;
                if (!AtEnd && PeekWord() == "right")
                {
                    _ = ReadCommandName();
                    close = ReadDelimiter();
                }
                else
                {
                    _errors = true;
                }

                return new MathLeftRight(open, close, body);
            }

            case "right":
                _errors = true;
                _ = ReadDelimiter();
                return null;

            case "big" or "Big" or "bigg" or "Bigg" or "bigl" or "bigr" or "Bigl" or "Bigr" or
                 "biggl" or "biggr" or "Biggl" or "Biggr":
            {
                string delimiter = ReadDelimiter();
                return new MathAtom(delimiter, MathAtomKind.Ordinary);
            }

            case "middle":
                _ = ReadDelimiter();
                return null;

            case "quad":
                return new MathSpace(1f);

            case "qquad":
                return new MathSpace(2f);

            case "enspace":
                return new MathSpace(0.5f);

            case "thinspace":
                return new MathSpace(0.16f);

            case "hspace" or "mspace" or "kern":
            {
                string size = ReadRawGroup();
                return new MathSpace(ParseEm(size));
            }

            case "displaystyle":
                return new MathStyle(true, MathRow.Empty);

            case "textstyle" or "scriptstyle" or "scriptscriptstyle":
                return new MathStyle(false, MathRow.Empty);

            case "limits" or "nolimits":
                return null;

            case "not":
            {
                MathNode? next = ParseAtom();
                if (next is MathAtom atom && Negations.TryGetValue(atom.Text, out string? negated))
                {
                    return new MathAtom(negated, atom.Kind);
                }

                return next is null ? null : new MathRow([new MathAtom("\u0338", MathAtomKind.Ordinary), next]);
            }

            case "begin":
                return ParseEnvironment();

            case "end":
                _errors = true;
                _ = ReadRawGroup();
                return null;

            case "pmod" or "bmod" or "pod":
            {
                MathNode value = ParseArgument();
                var parts = new List<MathNode>
                {
                    new MathSpace(0.4f),
                    new MathText("(", MathTextStyle.Upright),
                    new MathText("mod", MathTextStyle.Upright),
                    new MathSpace(0.3f),
                    value,
                    new MathText(")", MathTextStyle.Upright),
                };
                return new MathRow(parts);
            }

            case "color" or "textcolor" or "colorbox":
            {
                _ = ReadRawGroup();
                return ParseAtom();
            }

            case "phantom" or "hphantom" or "vphantom":
            {
                _ = ReadRawGroup();
                return new MathSpace(0.4f);
            }

            case "strut" or "mathstrut" or "displaystyle*":
                return null;

            case "tag" or "tag*" or "label" or "nonumber" or "notag":
                _ = ReadRawGroupIfAny();
                return null;

            case "ce" or "pu":
                return new MathText(ReadRawGroup(), MathTextStyle.Upright);
        }

        if (MathSymbols.TryGet(command, out string text, out MathAtomKind kind))
        {
            bool italic = kind == MathAtomKind.Ordinary && text.Length == 1
                && (char.IsAsciiLetter(text[0]) || IsLowerGreek(text[0])) && !char.IsUpper(text[0]);
            return new MathAtom(text, kind, italic);
        }

        if (MathSymbols.IsFunction(command))
        {
            bool limits = MathSymbols.TakesLimits(command);
            return new MathText(command, MathTextStyle.Upright)
            {
                Kind = limits ? MathAtomKind.LargeOperator : MathAtomKind.Ordinary,
            };
        }

        _errors = true;
        return new MathAtom("\\" + command, MathAtomKind.Ordinary);
    }

    private static bool IsLowerGreek(char c) => c is >= '\u03B1' and <= '\u03C9';

    private MathNode ParseEnvironment()
    {
        string name = ReadRawGroup().Trim();
        if (!MathSymbols.TryGetEnvironment(name, out string open, out string close, out bool leftAligned))
        {
            _errors = true;
            SkipEnvironmentBody(name);
            return new MathUnsupported("\\begin{" + name + "}", "unsupported environment");
        }

        SkipSpaces();
        if (!AtEnd && Peek() == '{')
        {
            // array preamble: {cc} — alignment is uniform here, the spec is consumed
            _ = ReadRawGroup();
        }

        SkipSpaces();
        if (!AtEnd && Peek() == '[')
        {
            _i++;
            _ = ParseList(']');
            Expect(']');
        }

        var rows = new List<IReadOnlyList<MathNode>>();
        var cells = new List<MathNode>();
        var row = new List<MathNode>();

        while (!AtEnd)
        {
            SkipSpaces();
            if (AtEnd)
            {
                break;
            }

            if (Peek() == '\\' && PeekWord() == "end")
            {
                _ = ReadCommandName();
                _ = ReadRawGroup();
                break;
            }

            if (Peek() == '&')
            {
                _i++;
                cells.Add(new MathRow(row));
                row = [];
                continue;
            }

            if (Peek() == '\\' && PeekAt(1) == '\\')
            {
                _i += 2;
                cells.Add(new MathRow(row));
                row = [];
                rows.Add(cells);
                cells = [];
                continue;
            }

            MathNode? node = ParseAtom();
            if (node is null)
            {
                continue;
            }

            row.Add(ParseScripts(node));
        }

        if (row.Count > 0 || cells.Count > 0)
        {
            cells.Add(new MathRow(row));
            rows.Add(cells);
        }

        if (rows.Count == 0)
        {
            rows.Add([MathRow.Empty]);
        }

        return new MathMatrix(rows, open, close, leftAligned || name is "cases" or "aligned" or "align" or "align*");
    }

    private void SkipEnvironmentBody(string name)
    {
        while (!AtEnd)
        {
            if (Peek() == '\\' && PeekWord() == "end")
            {
                _ = ReadCommandName();
                _ = ReadRawGroup();
                return;
            }

            _i++;
        }
    }

    // ------------------------------------------------------------------ utils

    private MathNode ParseArgument()
    {
        SkipSpaces();
        if (AtEnd)
        {
            return MathRow.Empty;
        }

        if (Peek() == '{')
        {
            _i++;
            MathRow group = ParseList('}');
            Expect('}');
            return group;
        }

        if (Peek() == '\\')
        {
            MathNode? command = ParseCommand();
            return command ?? MathRow.Empty;
        }

        MathNode? atom = ParseCharacter();
        return atom ?? MathRow.Empty;
    }

    private string ReadDelimiter()
    {
        SkipSpaces();
        if (AtEnd)
        {
            return string.Empty;
        }

        if (Peek() == '\\')
        {
            string name = ReadCommandName();
            if (name == ".")
            {
                return string.Empty;
            }

            if (MathSymbols.TryGet(name, out string text, out _))
            {
                return text;
            }

            return name switch
            {
                "lbrace" => "{",
                "rbrace" => "}",
                "|" => "‖",
                _ => name,
            };
        }

        char c = Peek();
        _i++;
        return c == '.' ? string.Empty : c.ToString();
    }

    private string ReadRawGroup()
    {
        SkipSpaces();
        if (AtEnd || Peek() != '{')
        {
            return AtEnd ? string.Empty : _tex[_i++].ToString();
        }

        _i++;
        int depth = 1;
        int start = _i;
        var sb = new StringBuilder();
        while (!AtEnd)
        {
            char c = Peek();
            if (c == '{')
            {
                depth++;
            }
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                {
                    sb.Append(_tex[start.._i]);
                    _i++;
                    return sb.ToString();
                }
            }
            else if (c == '\\' && depth == 1)
            {
                // keep control sequences as their literal text (\{ etc.)
                sb.Append(_tex[start.._i]);
                int commandStart = _i;
                _ = ReadCommandName();
                sb.Append(_tex[commandStart.._i]);
                start = _i;
                continue;
            }

            _i++;
        }

        sb.Append(_tex[start..]);
        return sb.ToString();
    }

    private string ReadRawGroupIfAny()
    {
        SkipSpaces();
        if (!AtEnd && Peek() == '{')
        {
            return ReadRawGroup();
        }

        if (!AtEnd && Peek() == '*')
        {
            _i++;
        }

        return string.Empty;
    }

    private static string TransformRaw(string raw, Func<char, string> map)
    {
        var sb = new StringBuilder(raw.Length);
        foreach (char c in raw)
        {
            sb.Append(char.IsAsciiLetterOrDigit(c) ? map(c) : c.ToString());
        }

        return sb.ToString();
    }

    private static float ParseEm(string text)
    {
        text = text.Trim();
        int digits = 0;
        while (digits < text.Length && (char.IsAsciiDigit(text[digits]) || text[digits] == '.'))
        {
            digits++;
        }

        if (!float.TryParse(text[..digits], NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
        {
            return 0.25f;
        }

        string unit = text[digits..].Trim();
        return unit switch
        {
            "pt" => value / 10f,
            "ex" => value * 0.45f,
            "mu" => value / 18f,
            "px" => value / 16f,
            _ => value,
        };
    }

    private void SkipSpaces()
    {
        while (!AtEnd && Peek() is ' ' or '\t' or '\r' or '\n')
        {
            _i++;
        }
    }

    private void Expect(char c)
    {
        SkipSpaces();
        if (!AtEnd && Peek() == c)
        {
            _i++;
            return;
        }

        _errors = true;
    }

    private bool AtEnd => _i >= _tex.Length;

    private char Peek() => _tex[_i];

    private char PeekAt(int offset) => _i + offset < _tex.Length ? _tex[_i + offset] : '\0';
}
