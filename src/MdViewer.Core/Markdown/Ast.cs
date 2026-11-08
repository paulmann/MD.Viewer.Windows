// MD.Viewer.Windows — Markdown object model.
// C# 15 note: the AST roots are `closed` hierarchies, so the renderer can switch
// over every block/inline type with compiler-proven exhaustiveness.

namespace MdViewer.Core.Markdown;

/// <summary>Root of the block-level Markdown AST.</summary>
public closed class Block;

/// <summary>Root of the inline-level Markdown AST.</summary>
public closed class Inline;

public enum AlertKind
{
    None,
    Note,
    Tip,
    Important,
    Warning,
    Caution,
}

public enum CellAlign
{
    None,
    Left,
    Center,
    Right,
}

public enum TaskState
{
    None,
    Todo,
    Done,
}

public enum InlineStyle
{
    Emphasis,
    Strong,
    Strike,
    Mark,
    Subscript,
    Superscript,
}

// ------------------------------------------------------------------ inlines

public sealed class TextInline(string text) : Inline
{
    public string Text { get; } = text;
}

public sealed class CodeInline(string text) : Inline
{
    public string Text { get; set; } = text;
}

public sealed class StyledInline(InlineStyle style, Inline[] children) : Inline
{
    public InlineStyle Style { get; } = style;

    public Inline[] Children { get; } = children;
}

public sealed class LinkInline(string url, string? title, Inline[] children) : Inline
{
    public string Url { get; init; } = url;

    public string? Title { get; init; } = title;

    public Inline[] Children { get; } = children;

    /// <summary>True for relative <c>.md</c> targets that do not exist on disk.</summary>
    public bool Missing { get; set; }
}

public sealed class ImageInline(string url, string alt, string? title, int? width) : Inline
{
    public string Url { get; } = url;

    public string Alt { get; } = alt;

    public string? Title { get; } = title;

    public int? Width { get; } = width;
}

public sealed class MathInline(string tex, bool display) : Inline
{
    public string Tex { get; } = tex;

    public bool Display { get; } = display;
}

public sealed class LineBreakInline(bool hard) : Inline
{
    public bool Hard { get; } = hard;
}

public sealed class FootnoteRefInline(int index, string label) : Inline
{
    public int Index { get; } = index;

    public string Label { get; } = label;
}

// ------------------------------------------------------------------- blocks

public sealed class HeadingBlock : Block
{
    public required int Level { get; init; }

    public required Inline[] Inlines { get; init; }

    public required string Text { get; init; }

    public required string Id { get; set; }

    /// <summary>Section number such as "2.3" when heading numbering is on.</summary>
    public string? Number { get; set; }

    public bool ExplicitId { get; init; }

    public int LineNumber { get; init; }
}

public sealed class ParagraphBlock : Block
{
    public required Inline[] Inlines { get; init; }

    public int LineNumber { get; init; }
}

public sealed class CodeBlock : Block
{
    public required string Text { get; init; }

    public string? Language { get; init; }

    public string? Title { get; init; }

    public int LineNumber { get; init; }
}

public sealed class QuoteBlock : Block
{
    public required Block[] Children { get; init; }
}

public sealed class AlertBlock : Block
{
    public required AlertKind Kind { get; init; }

    public required Block[] Children { get; init; }
}

public sealed class ListBlock : Block
{
    public required bool Ordered { get; init; }

    public required int Start { get; init; }

    public required ListItem[] Items { get; init; }

    public bool Tight { get; init; } = true;

    public int LineNumber { get; init; }
}

public sealed class ListItem(TaskState task, Block[] children, int lineNumber)
{
    public TaskState Task { get; } = task;

    public Block[] Children { get; } = children;

    public int LineNumber { get; } = lineNumber;
}

public sealed class TableBlock : Block
{
    public required string[] Headers { get; init; }

    public required CellAlign[] Aligns { get; init; }

    public required string[][] Rows { get; init; }

    /// <summary>A two-column table becomes a glossary: hover definitions in the body.</summary>
    public bool IsGlossary { get; init; }

    public int LineNumber { get; init; }
}

public sealed class ThematicBreakBlock : Block;

public sealed class MathBlock : Block
{
    public required string Tex { get; init; }
}

public sealed class MermaidBlock : Block
{
    public required string Source { get; init; }

    public string? Title { get; init; }

    public int LineNumber { get; init; }
}

public sealed class HtmlBlock : Block
{
    public required string Html { get; init; }
}

public sealed class DefinitionListBlock : Block
{
    public required DefinitionItem[] Items { get; init; }
}

public sealed class DefinitionItem(string term, string[][] definitions)
{
    public string Term { get; } = term;

    public string[][] Definitions { get; } = definitions;
}

/// <summary>
/// A code span that reads like <c>(Client) --&gt; [HTTPS] --&gt; (Server)</c>:
/// MD.Scribe renders those as a small node/chain graphic.
/// </summary>
public sealed class ChainBlock : Block
{
    public required ChainPart[] Parts { get; init; }
}

public sealed class ChainPart(string text, bool isNode, bool isArrow)
{
    public string Text { get; } = text;

    public bool IsNode { get; } = isNode;

    public bool IsArrow { get; } = isArrow;
}

public sealed class ImageBlock : Block
{
    public required string Url { get; init; }

    public required string Alt { get; init; }

    public string? Title { get; init; }

    public int? Width { get; init; }
}

public sealed class TocBlock : Block;

public sealed class FootnoteSectionBlock : Block
{
    public required FootnoteNote[] Notes { get; init; }
}

public sealed class FootnoteNote(int index, string label, Block[] children)
{
    public int Index { get; } = index;

    public string Label { get; } = label;

    public Block[] Children { get; } = children;
}
