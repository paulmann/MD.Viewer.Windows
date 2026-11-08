using System.Text;
using MdViewer.Core.Markdown;

namespace MdViewer.Tests;

/// <summary>Compact, stable text rendering of the AST — used by structural tests.</summary>
public static class AstDump
{
    public static string Blocks(Block[] blocks)
    {
        var sb = new StringBuilder();
        foreach (Block block in blocks)
        {
            Append(block, sb, 0);
        }

        return sb.ToString().TrimEnd('\n');
    }

    private static void Line(StringBuilder sb, string text)
    {
        sb.Append(text).Append('\n');
    }

    private static void Indent(StringBuilder sb, int level) => sb.Append(' ', level * 2);

    private static void Append(Block block, StringBuilder sb, int level)
    {
        Indent(sb, level);
        switch (block)
        {
            case HeadingBlock h:
            {
                string head = $"h{h.Level} #{h.Id}" + (h.Number is null ? "" : $" [{h.Number}]") + ": " + Inline(h.Inlines);
                Line(sb, head);
                break;
            }

            case ParagraphBlock p:
                Line(sb, "p: " + Inline(p.Inlines));
                break;

            case CodeBlock c:
            {
                string head = $"code({c.Language ?? "-"})" + (c.Title is null ? "" : $" title='{c.Title}'") + ": ";
                Line(sb, head + c.Text.Replace("\n", "\\n"));
                break;
            }

            case MermaidBlock m:
                Line(sb, "mermaid: " + m.Source.Replace("\n", "\\n"));
                break;

            case MathBlock mb:
                Line(sb, "math: " + mb.Tex);
                break;

            case ThematicBreakBlock:
                Line(sb, "hr");
                break;

            case QuoteBlock q:
                Line(sb, "quote");
                foreach (Block child in q.Children)
                {
                    Append(child, sb, level + 1);
                }

                break;

            case AlertBlock a:
                Line(sb, $"alert({a.Kind})");
                foreach (Block child in a.Children)
                {
                    Append(child, sb, level + 1);
                }

                break;

            case ListBlock l:
                Line(sb, $"list(ordered={l.Ordered},start={l.Start},tight={l.Tight})");
                foreach (ListItem item in l.Items)
                {
                    Indent(sb, level + 1);
                    Line(sb, $"item({item.Task})");
                    foreach (Block child in item.Children)
                    {
                        Append(child, sb, level + 2);
                    }
                }

                break;

            case TableBlock t:
            {
                string head = $"table({string.Join('|', t.Aligns)}) " + string.Join('|', t.Headers) + (t.IsGlossary ? " glossary" : "");
                Line(sb, head);
                foreach (string[] row in t.Rows)
                {
                    Indent(sb, level + 1);
                    Line(sb, "row: " + string.Join('|', row));
                }

                break;
            }

            case DefinitionListBlock d:
                Line(sb, "deflist");
                foreach (DefinitionItem item in d.Items)
                {
                    Indent(sb, level + 1);
                    Line(sb, "term: " + item.Term);
                    foreach (string[] def in item.Definitions)
                    {
                        Indent(sb, level + 2);
                        Line(sb, "def: " + string.Join("\\n", def));
                    }
                }

                break;

            case ChainBlock ch:
                Line(sb, "chain: " + string.Join(" ", ch.Parts.Select(p => p.IsArrow ? p.Text : "[" + p.Text + "]")));
                break;

            case ImageBlock img:
            {
                string head = $"image: {img.Url} alt='{img.Alt}'" + (img.Width is null ? "" : $" w={img.Width}");
                Line(sb, head);
                break;
            }

            case HtmlBlock html:
                Line(sb, "html: " + html.Html.Replace("\n", "\\n"));
                break;

            case TocBlock:
                Line(sb, "toc");
                break;

            case FootnoteSectionBlock f:
                Line(sb, $"footnotes({f.Notes.Length})");
                foreach (FootnoteNote note in f.Notes)
                {
                    Indent(sb, level + 1);
                    Line(sb, $"{note.Index} [{note.Label}]");
                    foreach (Block child in note.Children)
                    {
                        Append(child, sb, level + 2);
                    }
                }

                break;

            default:
                Line(sb, block.GetType().Name);
                break;
        }
    }

    public static string Inline(Inline[] inlines)
    {
        var sb = new StringBuilder();
        AppendInline(inlines, sb);
        return sb.ToString();
    }

    private static void AppendInline(Inline[] inlines, StringBuilder sb)
    {
        foreach (Inline inline in inlines)
        {
            switch (inline)
            {
                case TextInline t:
                    sb.Append(t.Text);
                    break;
                case CodeInline c:
                    sb.Append('`').Append(c.Text).Append('`');
                    break;
                case StyledInline s:
                    string marker = s.Style switch
                    {
                        InlineStyle.Strong => "**",
                        InlineStyle.Emphasis => "*",
                        InlineStyle.Strike => "~~",
                        InlineStyle.Mark => "==",
                        InlineStyle.Subscript => "~",
                        _ => "^",
                    };
                    sb.Append(marker);
                    AppendInline(s.Children, sb);
                    sb.Append(marker);
                    break;
                case LinkInline l:
                    sb.Append('[');
                    AppendInline(l.Children, sb);
                    sb.Append("](").Append(l.Url).Append(')');
                    if (l.Missing)
                    {
                        sb.Append('!');
                    }

                    break;
                case ImageInline i:
                    sb.Append("![").Append(i.Alt).Append("](").Append(i.Url).Append(')');
                    break;
                case MathInline m:
                    sb.Append('$').Append(m.Tex).Append('$');
                    break;
                case LineBreakInline br:
                    sb.Append(br.Hard ? "<br>" : " ");
                    break;
                case FootnoteRefInline f:
                    sb.Append("[^").Append(f.Index).Append(']');
                    break;
            }
        }
    }
}
