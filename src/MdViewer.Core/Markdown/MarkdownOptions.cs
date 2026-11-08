namespace MdViewer.Core.Markdown;

/// <summary>Switches for optional Markdown extensions (MD.Scribe feature parity).</summary>
public sealed class MarkdownOptions
{
    public static MarkdownOptions Default { get; } = new();

    public bool Tables { get; init; } = true;

    public bool Strikethrough { get; init; } = true;

    public bool TaskLists { get; init; } = true;

    public bool Autolinks { get; init; } = true;

    public bool Footnotes { get; init; } = true;

    public bool DefinitionLists { get; init; } = true;

    public bool SubSuperscript { get; init; } = true;

    public bool Marks { get; init; } = true;

    public bool Emoji { get; init; } = true;

    public bool Math { get; init; } = true;

    public bool Mermaid { get; init; } = true;

    public bool AttributeSyntax { get; init; } = true;

    public bool FrontMatter { get; init; } = true;

    /// <summary>MD.Viewer legacy: a single newline inside a paragraph breaks the line.</summary>
    public bool DoubleLineBreaks { get; init; } = true;

    public bool SmartPunctuation { get; init; }

    /// <summary>Turn <c>(Client) --&gt; [HTTPS]</c> code spans into a chain graphic.</summary>
    public bool PatternChains { get; init; } = true;

    /// <summary>Two-column tables get hover glossary definitions.</summary>
    public bool GlossaryTables { get; init; } = true;

    public bool HeadingNumbering { get; init; }

    /// <summary>Skip raw HTML blocks (they are shown as escaped source, never executed).</summary>
    public bool EscapeRawHtml { get; init; } = true;
}
