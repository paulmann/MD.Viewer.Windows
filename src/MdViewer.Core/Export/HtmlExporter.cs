using System.Text;
using MdViewer.Core.Highlighting;

namespace MdViewer.Core.Markdown;

/// <summary>
/// Standalone HTML export: one self-contained file with embedded CSS, the same
/// feature set as the on-screen renderer, and no external requests at all.
/// </summary>
public static class HtmlExporter
{
    public static string ToHtml(MarkdownDocument document, string? documentTitle = null)
    {
        var sb = new StringBuilder(document.Source.Text.Length * 2 + 4096);
        string title = documentTitle ?? document.Title ?? document.FileName;

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.Append("<title>").Append(Escape(title)).AppendLine("</title>");
        if (!string.IsNullOrWhiteSpace(document.FrontMatter.Description))
        {
            sb.Append("<meta name=\"description\" content=\"").Append(Escape(document.FrontMatter.Description!)).AppendLine("\">");
        }

        sb.AppendLine("<style>");
        sb.AppendLine(Css);
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<main class=\"sheet\">");

        foreach (Block block in document.Blocks)
        {
            WriteBlock(sb, block, document);
        }

        sb.AppendLine("</main>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private static void WriteBlock(StringBuilder sb, Block block, MarkdownDocument document)
    {
        switch (block)
        {
            case HeadingBlock heading:
            {
                string tag = "h" + heading.Level;
                sb.Append('<').Append(tag).Append(" id=\"").Append(Escape(heading.Id)).Append("\">");
                if (heading.Number is { Length: > 0 } number)
                {
                    sb.Append("<span class=\"num\">").Append(Escape(number)).Append("</span> ");
                }

                WriteInlines(sb, heading.Inlines);
                sb.Append("</").Append(tag).AppendLine(">");
                break;
            }

            case ParagraphBlock paragraph:
                sb.Append("<p>");
                WriteInlines(sb, paragraph.Inlines);
                sb.AppendLine("</p>");
                break;

            case CodeBlock code:
            {
                sb.Append("<div class=\"code\">");
                if (code.Language is { Length: > 0 } || code.Title is { Length: > 0 })
                {
                    sb.Append("<div class=\"code-head\"><span>")
                        .Append(Escape(code.Title ?? code.Language ?? string.Empty))
                        .Append("</span><span class=\"lang\">")
                        .Append(Escape(code.Language ?? string.Empty))
                        .Append("</span></div>");
                }

                sb.Append("<pre><code>");
                WriteHighlighted(sb, code.Text, code.Language);
                sb.Append("</code></pre></div>");
                sb.AppendLine();
                break;
            }

            case MermaidBlock mermaid:
                sb.Append("<pre class=\"mermaid-src\"><code>")
                    .Append(Escape(mermaid.Source))
                    .AppendLine("</code></pre>");
                break;

            case MathBlock math:
                sb.Append("<div class=\"math\">").Append(Escape(math.Tex)).AppendLine("</div>");
                break;

            case QuoteBlock quote:
                sb.AppendLine("<blockquote>");
                foreach (Block child in quote.Children)
                {
                    WriteBlock(sb, child, document);
                }

                sb.AppendLine("</blockquote>");
                break;

            case AlertBlock alert:
            {
                string kind = alert.Kind.ToString().ToLowerInvariant();
                sb.Append("<div class=\"alert alert-").Append(kind).AppendLine("\">");
                sb.Append("<div class=\"alert-title\">").Append(alert.Kind.ToString().ToUpperInvariant()).AppendLine("</div>");
                foreach (Block child in alert.Children)
                {
                    WriteBlock(sb, child, document);
                }

                sb.AppendLine("</div>");
                break;
            }

            case ListBlock list:
            {
                string tag = list.Ordered ? "ol" : "ul";
                sb.Append('<').Append(tag);
                if (list.Ordered && list.Start != 1)
                {
                    sb.Append(" start=\"").Append(list.Start).Append('"');
                }

                sb.AppendLine(">");
                foreach (ListItem item in list.Items)
                {
                    sb.Append("<li>");
                    if (item.Task != TaskState.None)
                    {
                        sb.Append("<input type=\"checkbox\" disabled");
                        if (item.Task == TaskState.Done)
                        {
                            sb.Append(" checked");
                        }

                        sb.Append("> ");
                    }

                    WriteItemBody(sb, item.Children, document);
                    sb.AppendLine("</li>");
                }

                sb.Append("</").Append(tag).AppendLine(">");
                break;
            }

            case TableBlock table:
            {
                sb.AppendLine("<div class=\"table\"><table>");
                sb.Append("<thead><tr>");
                for (int c = 0; c < table.Headers.Length; c++)
                {
                    sb.Append("<th").Append(Align(table.Aligns[c])).Append('>');
                    sb.Append(InlineToHtml(table.Headers[c])).Append("</th>");
                }

                sb.AppendLine("</tr></thead><tbody>");
                foreach (string[] row in table.Rows)
                {
                    sb.Append("<tr>");
                    for (int c = 0; c < table.Headers.Length; c++)
                    {
                        string cell = c < row.Length ? row[c] : string.Empty;
                        sb.Append("<td").Append(Align(table.Aligns[c])).Append('>');
                        sb.Append(InlineToHtml(cell)).Append("</td>");
                    }

                    sb.AppendLine("</tr>");
                }

                sb.AppendLine("</tbody></table></div>");
                break;
            }

            case ThematicBreakBlock:
                sb.AppendLine("<hr>");
                break;

            case ImageBlock image:
            {
                sb.Append("<figure><img src=\"").Append(Escape(image.Url)).Append("\" alt=\"").Append(Escape(image.Alt)).Append('"');
                if (image.Width is int width)
                {
                    sb.Append(" width=\"").Append(width).Append('"');
                }

                sb.Append('>');
                if (image.Alt.Length > 0)
                {
                    sb.Append("<figcaption>").Append(Escape(image.Alt)).AppendLine("</figcaption>");
                }

                sb.AppendLine("</figure>");
                break;
            }

            case DefinitionListBlock definitions:
                sb.AppendLine("<dl>");
                foreach (DefinitionItem item in definitions.Items)
                {
                    sb.Append("<dt>").Append(Escape(item.Term)).AppendLine("</dt>");
                    foreach (string[] definition in item.Definitions)
                    {
                        sb.Append("<dd>").Append(InlineToHtml(string.Join(' ', definition))).AppendLine("</dd>");
                    }
                }

                sb.AppendLine("</dl>");
                break;

            case ChainBlock chain:
                sb.Append("<div class=\"chain\">");
                foreach (ChainPart part in chain.Parts)
                {
                    if (part.IsArrow)
                    {
                        sb.Append("<span class=\"arrow\">").Append(Escape(part.Text)).Append("</span>");
                    }
                    else
                    {
                        sb.Append("<span class=\"node\">").Append(Escape(part.Text)).Append("</span>");
                    }
                }

                sb.AppendLine("</div>");
                break;

            case HtmlBlock html:
                sb.Append("<div class=\"raw\"><div class=\"raw-title\">RAW HTML</div><pre><code>")
                    .Append(Escape(html.Html))
                    .AppendLine("</code></pre></div>");
                break;

            case TocBlock:
                sb.AppendLine("<nav class=\"toc\"><div class=\"toc-title\">Contents</div><ul>");
                foreach (HeadingInfo heading in document.Outline)
                {
                    if (heading.Level > 4)
                    {
                        continue;
                    }

                    sb.Append("<li class=\"lvl").Append(heading.Level).Append("\"><a href=\"#").Append(Escape(heading.Id)).Append("\">")
                        .Append(Escape(heading.Text)).AppendLine("</a></li>");
                }

                sb.AppendLine("</ul></nav>");
                break;

            case FootnoteSectionBlock footnotes:
                sb.AppendLine("<section class=\"footnotes\">");
                foreach (FootnoteNote note in footnotes.Notes)
                {
                    sb.Append("<div class=\"footnote\" id=\"footnote-").Append(note.Index).Append("\">");
                    sb.Append("<span class=\"fn-num\">").Append(note.Index).Append("</span>");
                    foreach (Block child in note.Children)
                    {
                        WriteBlock(sb, child, document);
                    }

                    sb.AppendLine("</div>");
                }

                sb.AppendLine("</section>");
                break;
        }
    }

    private static void WriteItemBody(StringBuilder sb, Block[] children, MarkdownDocument document)
    {
        bool first = true;
        foreach (Block child in children)
        {
            if (child is ParagraphBlock paragraph && first)
            {
                WriteInlines(sb, paragraph.Inlines);
            }
            else
            {
                sb.AppendLine();
                WriteBlock(sb, child, document);
            }

            first = false;
        }
    }

    private static string Align(CellAlign align) => align switch
    {
        CellAlign.Center => " style=\"text-align:center\"",
        CellAlign.Right => " style=\"text-align:right\"",
        _ => string.Empty,
    };

    private static string InlineToHtml(string markdown)
    {
        var sb = new StringBuilder();
        var document = new MarkdownParser().Parse(markdown);
        if (document.Blocks is [ParagraphBlock paragraph])
        {
            WriteInlines(sb, paragraph.Inlines);
        }
        else
        {
            sb.Append(Escape(markdown));
        }

        return sb.ToString();
    }

    private static void WriteInlines(StringBuilder sb, Inline[] inlines)
    {
        foreach (Inline inline in inlines)
        {
            switch (inline)
            {
                case TextInline text:
                    sb.Append(Escape(text.Text));
                    break;
                case CodeInline code:
                    sb.Append("<code>").Append(Escape(code.Text)).Append("</code>");
                    break;
                case StyledInline styled:
                {
                    (string open, string close) = styled.Style switch
                    {
                        InlineStyle.Strong => ("<strong>", "</strong>"),
                        InlineStyle.Emphasis => ("<em>", "</em>"),
                        InlineStyle.Strike => ("<del>", "</del>"),
                        InlineStyle.Mark => ("<mark>", "</mark>"),
                        InlineStyle.Subscript => ("<sub>", "</sub>"),
                        _ => ("<sup>", "</sup>"),
                    };
                    sb.Append(open);
                    WriteInlines(sb, styled.Children);
                    sb.Append(close);
                    break;
                }

                case LinkInline link:
                    sb.Append("<a href=\"").Append(Escape(link.Url)).Append('"');
                    if (link.Title is not null)
                    {
                        sb.Append(" title=\"").Append(Escape(link.Title)).Append('"');
                    }

                    sb.Append('>');
                    WriteInlines(sb, link.Children);
                    sb.Append("</a>");
                    break;

                case ImageInline image:
                    sb.Append("<img src=\"").Append(Escape(image.Url)).Append("\" alt=\"").Append(Escape(image.Alt)).Append('"');
                    if (image.Width is int width)
                    {
                        sb.Append(" width=\"").Append(width).Append('"');
                    }

                    sb.Append('>');
                    break;

                case MathInline math:
                    sb.Append("<span class=\"math-inline\">").Append(Escape(math.Tex)).Append("</span>");
                    break;

                case LineBreakInline lineBreak:
                    sb.Append(lineBreak.Hard ? "<br>" : "\n");
                    break;

                case FootnoteRefInline foot:
                    sb.Append("<sup class=\"fn-ref\"><a href=\"#footnote-").Append(foot.Index).Append("\">").Append(foot.Index).Append("</a></sup>");
                    break;
            }
        }
    }

    private static void WriteHighlighted(StringBuilder sb, string text, string? language)
    {
        LanguageMode mode = SyntaxHighlighter.Resolve(language);
        if (mode == LanguageMode.None)
        {
            sb.Append(Escape(text));
            return;
        }

        List<Token> tokens = SyntaxHighlighter.Tokenize(text, mode, new HighlightState());
        int cursor = 0;
        foreach (Token token in tokens)
        {
            if (token.Start > cursor)
            {
                sb.Append(Escape(text[cursor..token.Start]));
            }

            string? css = token.Kind switch
            {
                TokenKind.Keyword => "kw",
                TokenKind.String => "str",
                TokenKind.Number => "num2",
                TokenKind.Comment => "com",
                TokenKind.Type => "typ",
                TokenKind.Function => "fn",
                TokenKind.Punctuation => "pun",
                TokenKind.Property => "prop",
                TokenKind.Tag => "tag",
                TokenKind.Attribute => "attr",
                _ => null,
            };

            string chunk = text.Substring(token.Start, token.Length);
            if (css is null)
            {
                sb.Append(Escape(chunk));
            }
            else
            {
                sb.Append("<span class=\"").Append(css).Append("\">").Append(Escape(chunk)).Append("</span>");
            }

            cursor = token.Start + token.Length;
        }

        if (cursor < text.Length)
        {
            sb.Append(Escape(text[cursor..]));
        }
    }

    public static string Escape(string value)
    {
        var sb = new StringBuilder(value.Length + 8);
        foreach (char c in value)
        {
            switch (c)
            {
                case '&':
                    sb.Append("&amp;");
                    break;
                case '<':
                    sb.Append("&lt;");
                    break;
                case '>':
                    sb.Append("&gt;");
                    break;
                case '"':
                    sb.Append("&quot;");
                    break;
                case '\'':
                    sb.Append("&#39;");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }

    private const string Css = """
        :root {
          --bg: #ffffff; --text: #15171c; --muted: #5b6472; --faint: #8a93a2;
          --border: #e4e6eb; --accent: #2563eb; --code-bg: #f7f8fa; --code-border: #e7e9ee;
          --inline-bg: #f1f3f6; --inline-fg: #9a335b; --quote-bar: #cbd2dc; --quote-bg: #fafbfc;
          --mark: #fff1b8; --kw: #8250df; --str: #0a7d32; --num: #b5560a; --com: #8a93a2;
          --typ: #1f6feb; --fn: #6639ba; --pun: #57606e;
        }
        @media (prefers-color-scheme: dark) {
          :root {
            --bg: #171a20; --text: #e8eaee; --muted: #a0a8b4; --faint: #737b88;
            --border: #262a33; --accent: #7aa2ff; --code-bg: #1c2027; --code-border: #2a2f39;
            --inline-bg: #252a33; --inline-fg: #f09db4; --quote-bar: #3a4150; --quote-bg: #1a1e24;
            --mark: #55420a; --kw: #c792ea; --str: #9ad29a; --num: #f0a35e; --com: #6b7486;
            --typ: #82aaff; --fn: #d2a8ff; --pun: #9aa2b0;
          }
        }
        * { box-sizing: border-box; }
        html, body { margin: 0; padding: 0; background: var(--bg); }
        body {
          font: 400 16px/1.62 "Segoe UI", -apple-system, "Helvetica Neue", Arial, sans-serif;
          color: var(--text); -webkit-font-smoothing: antialiased;
        }
        .sheet { max-width: 780px; margin: 0 auto; padding: 44px 56px 56px; }
        h1, h2, h3, h4, h5, h6 { line-height: 1.24; margin: 1.6em 0 0.5em; font-weight: 650; }
        h1 { font-size: 31px; margin-top: 0; }
        h2 { font-size: 24.5px; border-bottom: 1px solid var(--border); padding-bottom: 8px; }
        h3 { font-size: 20px; }
        h4 { font-size: 17px; }
        h5 { font-size: 15.5px; }
        h6 { font-size: 13.8px; color: var(--muted); }
        h1 .num, h2 .num, h3 .num { color: var(--faint); font-weight: 400; margin-right: 6px; }
        p { margin: 0 0 13px; }
        a { color: var(--accent); text-decoration: none; }
        a:hover { text-decoration: underline; }
        code { font: 400 0.9em/1.5 "Cascadia Mono", Consolas, monospace; background: var(--inline-bg); color: var(--inline-fg);
               padding: 1px 4px; border-radius: 4px; }
        .code { background: var(--code-bg); border: 1px solid var(--code-border); border-radius: 10px; margin: 16px 0; overflow: hidden; }
        .code-head { display: flex; justify-content: space-between; padding: 7px 14px; font: 500 11.5px/1.4 "Cascadia Mono", Consolas, monospace;
                     color: var(--muted); border-bottom: 1px solid var(--code-border); }
        .code-head .lang { color: var(--faint); }
        pre { margin: 0; padding: 14px; overflow-x: auto; }
        pre code { background: none; padding: 0; color: var(--text); font-size: 13px; }
        .kw { color: var(--kw); } .str { color: var(--str); } .num2 { color: var(--num); } .com { color: var(--com); font-style: italic; }
        .typ { color: var(--typ); } .fn { color: var(--fn); } .pun { color: var(--pun); } .prop { color: var(--typ); }
        .tag { color: var(--kw); } .attr { color: var(--fn); }
        blockquote { margin: 16px 0; padding: 12px 18px; border-left: 3px solid var(--quote-bar); background: var(--quote-bg);
                     border-radius: 0 8px 8px 0; color: var(--muted); }
        blockquote p:last-child { margin-bottom: 0; }
        .alert { margin: 16px 0; padding: 12px 16px 12px 18px; border-left: 3px solid var(--accent); border-radius: 0 8px 8px 0;
                 background: color-mix(in srgb, var(--accent) 6%, transparent); }
        .alert-title { font-size: 11.5px; font-weight: 700; letter-spacing: 0.04em; color: var(--accent); margin-bottom: 4px; }
        .alert-tip { border-color: #159e5b; } .alert-tip .alert-title { color: #159e5b; }
        .alert-warning { border-color: #d97706; } .alert-warning .alert-title { color: #d97706; }
        .alert-caution { border-color: #dc2626; } .alert-caution .alert-title { color: #dc2626; }
        .alert-important { border-color: #7c3aed; } .alert-important .alert-title { color: #7c3aed; }
        ul, ol { margin: 0 0 16px; padding-left: 26px; }
        li { margin: 4px 0; }
        li > p { margin: 0 0 6px; }
        .table { overflow-x: auto; margin: 16px 0; }
        table { border-collapse: collapse; width: 100%; font-size: 0.94em; }
        th, td { border: 1px solid var(--border); padding: 8px 12px; text-align: left; vertical-align: top; }
        thead th { background: color-mix(in srgb, var(--text) 4%, transparent); font-weight: 600; }
        tbody tr:nth-child(even) { background: color-mix(in srgb, var(--text) 2%, transparent); }
        hr { border: 0; border-top: 1px solid var(--border); margin: 26px 0; }
        mark { background: var(--mark); color: inherit; padding: 0 2px; border-radius: 3px; }
        figure { margin: 18px 0; text-align: center; }
        figure img { max-width: 100%; border-radius: 8px; }
        figcaption { font-size: 12.7px; color: var(--muted); margin-top: 8px; }
        dl { margin: 16px 0; }
        dt { font-weight: 650; }
        dd { margin: 4px 0 12px 20px; color: var(--muted); }
        .math { margin: 16px 0; padding: 14px; background: var(--code-bg); border-radius: 10px; text-align: center;
                font: italic 17px/1.5 Cambria, Georgia, serif; }
        .math-inline { font-family: Cambria, Georgia, serif; font-style: italic; }
        .mermaid-src, .raw pre { background: var(--code-bg); border: 1px solid var(--code-border); border-radius: 10px; }
        .raw-title { font-size: 11px; letter-spacing: 0.06em; color: var(--faint); margin-bottom: -8px; }
        .chain { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; margin: 14px 0; }
        .chain .node { background: color-mix(in srgb, var(--accent) 10%, transparent); border: 1px solid color-mix(in srgb, var(--accent) 30%, transparent);
                       border-radius: 6px; padding: 3px 10px; font-size: 0.9em; color: var(--accent); }
        .chain .arrow { color: var(--faint); }
        .toc { margin: 20px 0; padding: 14px 18px; border: 1px solid var(--border); border-radius: 10px; background: var(--quote-bg); }
        .toc-title { font-size: 11.5px; letter-spacing: 0.06em; color: var(--faint); margin-bottom: 6px; }
        .toc ul { list-style: none; padding: 0; margin: 0; }
        .toc li { margin: 3px 0; }
        .toc .lvl3 { padding-left: 14px; } .toc .lvl4 { padding-left: 28px; }
        .footnotes { margin-top: 28px; border-top: 1px solid var(--border); padding-top: 14px; font-size: 0.92em; color: var(--muted); }
        .footnote { margin: 6px 0; }
        .fn-num { color: var(--accent); font-weight: 650; margin-right: 6px; }
        .fn-ref a { text-decoration: none; }
        @media print {
          .sheet { max-width: none; padding: 0; }
          .code, blockquote, .alert { break-inside: avoid; }
        }
        """;
}

/// <summary>Plain-text export (Markdown stripped, structure preserved).</summary>
public static class PlainTextExporter
{
    public static string ToText(MarkdownDocument document)
    {
        var sb = new StringBuilder(document.Source.Text.Length);
        foreach (Block block in document.Blocks)
        {
            Write(sb, block, 0);
        }

        return sb.ToString().TrimEnd() + Environment.NewLine;
    }

    private static void Write(StringBuilder sb, Block block, int depth)
    {
        string pad = new(' ', depth * 2);
        switch (block)
        {
            case HeadingBlock heading:
                sb.AppendLine().Append(pad).AppendLine(InlineParser.PlainText(heading.Inlines));
                sb.AppendLine(pad + new string(heading.Level <= 2 ? '=' : '-', Math.Clamp(heading.Text.Length, 3, 60)));
                break;
            case ParagraphBlock paragraph:
                sb.AppendLine(pad + InlineParser.PlainText(paragraph.Inlines));
                sb.AppendLine();
                break;
            case CodeBlock code:
                foreach (string line in code.Text.Split('\n'))
                {
                    sb.AppendLine(pad + "    " + line);
                }

                sb.AppendLine();
                break;
            case QuoteBlock quote:
                foreach (Block child in quote.Children)
                {
                    Write(sb, child, depth + 1);
                }

                break;
            case AlertBlock alert:
                sb.AppendLine(pad + "[" + alert.Kind.ToString().ToUpperInvariant() + "]");
                foreach (Block child in alert.Children)
                {
                    Write(sb, child, depth + 1);
                }

                break;
            case ListBlock list:
            {
                int index = list.Start;
                foreach (ListItem item in list.Items)
                {
                    sb.Append(pad).Append(list.Ordered ? $"{index}. " : "- ");
                    bool first = true;
                    foreach (Block child in item.Children)
                    {
                        if (first && child is ParagraphBlock paragraph)
                        {
                            sb.AppendLine(InlineParser.PlainText(paragraph.Inlines));
                        }
                        else
                        {
                            Write(sb, child, depth + 1);
                        }

                        first = false;
                    }

                    index++;
                }

                sb.AppendLine();
                break;
            }

            case TableBlock table:
            {
                sb.AppendLine(pad + string.Join(" | ", table.Headers));
                sb.AppendLine(pad + new string('-', Math.Max(8, table.Headers.Sum(h => h.Length) + table.Headers.Length * 3)));
                foreach (string[] row in table.Rows)
                {
                    sb.AppendLine(pad + string.Join(" | ", row));
                }

                sb.AppendLine();
                break;
            }

            case ThematicBreakBlock:
                sb.AppendLine(pad + "―――");
                break;
            case MathBlock math:
                sb.AppendLine(pad + math.Tex);
                sb.AppendLine();
                break;
            case ImageBlock image:
                sb.AppendLine(pad + "[image: " + image.Alt + "]");
                break;
            case DefinitionListBlock definitions:
                foreach (DefinitionItem item in definitions.Items)
                {
                    sb.AppendLine(pad + item.Term);
                    foreach (string[] definition in item.Definitions)
                    {
                        sb.AppendLine(pad + "    " + string.Join(' ', definition));
                    }
                }

                sb.AppendLine();
                break;
            case FootnoteSectionBlock footnotes:
                sb.AppendLine();
                foreach (FootnoteNote note in footnotes.Notes)
                {
                    sb.AppendLine(pad + $"[{note.Index}]");
                    foreach (Block child in note.Children)
                    {
                        Write(sb, child, depth + 1);
                    }
                }

                break;
            case ChainBlock chain:
                sb.AppendLine(pad + string.Join(' ', chain.Parts.Select(p => p.Text)));
                sb.AppendLine();
                break;
        }
    }
}
