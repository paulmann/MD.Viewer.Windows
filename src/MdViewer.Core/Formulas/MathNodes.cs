namespace MdViewer.Core.Formulas;

/// <summary>
/// TeX math syntax tree. The parser is deliberately independent of the renderer:
/// layout and GDI+ painting live in <c>MdViewer.Render.Math</c>, so exports and
/// future formats can walk the same tree.
/// </summary>
public abstract class MathNode;

/// <summary>Atoms inside a row (spacing depends on <see cref="MathAtomKind"/>).</summary>
public enum MathAtomKind
{
    Ordinary,
    Binary,
    Relation,
    Open,
    Close,
    Punctuation,
    LargeOperator,
}

public enum MathTextStyle
{
    Normal,
    Upright,
    Bold,
    Italic,
    BoldItalic,
    Monospace,
    Blackboard,
    Script,
    Fraktur,
    SansSerif,
}

/// <summary>One character (or a ligature such as "∑") with its spacing class.</summary>
public sealed class MathAtom(string text, MathAtomKind kind, bool italic = false) : MathNode
{
    public string Text { get; } = text;

    public MathAtomKind Kind { get; } = kind;

    /// <summary>Latin letters and lowercase Greek are italic by convention.</summary>
    public bool Italic { get; } = italic;
}

/// <summary>Text set upright (\\text, \\mathrm, function names).</summary>
public sealed class MathText(string text, MathTextStyle style = MathTextStyle.Upright) : MathNode
{
    public string Text { get; } = text;

    public MathTextStyle Style { get; } = style;

    public MathAtomKind Kind { get; init; } = MathAtomKind.Ordinary;
}

public sealed class MathRow(IReadOnlyList<MathNode> children) : MathNode
{
    public IReadOnlyList<MathNode> Children { get; } = children;

    public static MathRow Empty { get; } = new([]);
}

public sealed class MathFraction(MathNode numerator, MathNode denominator, bool barred) : MathNode
{
    public MathNode Numerator { get; } = numerator;

    public MathNode Denominator { get; } = denominator;

    public bool Barred { get; } = barred;
}

public sealed class MathRadical(MathNode body, MathNode? index) : MathNode
{
    public MathNode Body { get; } = body;

    public MathNode? Index { get; } = index;
}

public sealed class MathScripts(MathNode? baseNode, MathNode? sub, MathNode? sup) : MathNode
{
    public MathNode? Base { get; } = baseNode;

    public MathNode? Sub { get; } = sub;

    public MathNode? Sup { get; } = sup;
}

public sealed class MathLeftRight(string open, string close, MathNode body) : MathNode
{
    public string Open { get; } = open;

    public string Close { get; } = close;

    public MathNode Body { get; } = body;
}

public sealed class MathAccent(MathNode body, string mark, bool under) : MathNode
{
    public MathNode Body { get; } = body;

    public string Mark { get; } = mark;

    public bool Under { get; } = under;
}

public sealed class MathMatrix(
    IReadOnlyList<IReadOnlyList<MathNode>> rows,
    string open,
    string close,
    bool leftAligned) : MathNode
{
    public IReadOnlyList<IReadOnlyList<MathNode>> Rows { get; } = rows;

    public string Open { get; } = open;

    public string Close { get; } = close;

    public bool LeftAligned { get; } = leftAligned;

    /// <summary>Per-column alignment from an array preamble: 'l', 'c' or 'r'. Null = uniform.</summary>
    public IReadOnlyList<char>? Alignments { get; init; }

    /// <summary>Row boundaries that carry a rule (\hline): the line is drawn above that row.</summary>
    public IReadOnlyList<int>? RuleRows { get; init; }
}

public sealed class MathSpace(float em) : MathNode
{
    public float Em { get; } = em;
}

/// <summary>\\displaystyle / \\textstyle switch inside a formula.</summary>
public sealed class MathStyle(bool display, MathNode body) : MathNode
{
    public bool Display { get; } = display;

    public MathNode Body { get; } = body;
}

/// <summary>Where the decoration of a <see cref="MathStack"/> sits.</summary>
public enum MathStackMark
{
    None,
    OverBrace,
    UnderBrace,
}

/// <summary>
/// Anything above and/or below a base: <c>\overset</c>, <c>\underset</c>, <c>\stackrel</c>,
/// the braces, and the limits TeX attaches to an operator.
/// </summary>
public sealed class MathStack(MathNode body, MathNode? above, MathNode? below, MathStackMark mark = MathStackMark.None) : MathNode
{
    public MathNode Body { get; } = body;

    public MathNode? Above { get; } = above;

    public MathNode? Below { get; } = below;

    public MathStackMark Mark { get; } = mark;
}

/// <summary>A column of centred lines: <c>\substack{a \\ b}</c> and friends.</summary>
public sealed class MathStacked(IReadOnlyList<MathNode> lines, float scale) : MathNode
{
    public IReadOnlyList<MathNode> Lines { get; } = lines;

    /// <summary>Font scale relative to the surrounding formula (substack is set small).</summary>
    public float Scale { get; } = scale;
}

public enum MathFrameKind
{
    /// <summary>\boxed — square frame, no padding.</summary>
    Box,

    /// <summary>\fbox — frame with a little padding.</summary>
    Padded,
}

public sealed class MathFramed(MathNode body, MathFrameKind kind) : MathNode
{
    public MathNode Body { get; } = body;

    public MathFrameKind Kind { get; } = kind;
}

public enum MathCancelKind
{
    Forward,
    Backward,
    Both,
}

/// <summary>A struck-through fragment: \cancel, \bcancel, \xcancel.</summary>
public sealed class MathCancel(MathNode body, MathCancelKind kind) : MathNode
{
    public MathNode Body { get; } = body;

    public MathCancelKind Kind { get; } = kind;
}

/// <summary>\rule{width}{height} — a filled rectangle, in em.</summary>
public sealed class MathRule(float widthEm, float heightEm) : MathNode
{
    public float WidthEm { get; } = widthEm;

    public float HeightEm { get; } = heightEm;
}

/// <summary>\raisebox, \llap, \rlap, \clap: the child is moved off its natural place.</summary>
public sealed class MathShift(MathNode body, float dxEm, float dyEm, float widthEm = float.NaN) : MathNode
{
    public MathNode Body { get; } = body;

    public float DxEm { get; } = dxEm;

    /// <summary>Positive moves the box up, as in \raisebox.</summary>
    public float DyEm { get; } = dyEm;

    /// <summary>Fixed advance width (\llap and friends consume none), or NaN for the child's own.</summary>
    public float WidthEm { get; } = widthEm;
}

/// <summary>An explicit spacing class: \mathbin, \mathrel, \mathop and friends.</summary>
public sealed class MathClass(MathNode body, MathAtomKind kind) : MathNode
{
    public MathNode Body { get; } = body;

    public MathAtomKind Kind { get; } = kind;
}

/// <summary>Bold math: \boldsymbol, \bm, \pmb, \mathbfit — the glyphs, not the text.</summary>
public sealed class MathBold(MathNode body, bool italic) : MathNode
{
    public MathNode Body { get; } = body;

    public bool Italic { get; } = italic;
}

/// <summary>\hline inside an array: a rule drawn above the row it starts.</summary>
public sealed class MathHLine(bool dashed) : MathNode
{
    public bool Dashed { get; } = dashed;
}

/// <summary>\phantom and friends: keeps the metrics, paints nothing.</summary>
public sealed class MathPhantom(MathNode body, bool keepWidth, bool keepHeight) : MathNode
{
    public MathNode Body { get; } = body;

    public bool KeepWidth { get; } = keepWidth;

    public bool KeepHeight { get; } = keepHeight;
}

/// <summary>Construct the parser could not read: drawn as the source, never thrown.</summary>
public sealed class MathUnsupported(string tex, string reason) : MathNode
{
    public string Tex { get; } = tex;

    public string Reason { get; } = reason;
}

/// <summary>A whole formula: display math may contain several rows separated by \\\\ .</summary>
public sealed class MathLines(IReadOnlyList<MathRow> rows, bool display)
{
    public IReadOnlyList<MathRow> Rows { get; } = rows;

    public bool Display { get; } = display;

    /// <summary>True when the parser had to give up somewhere (kept for diagnostics).</summary>
    public bool HasErrors { get; init; }
}
