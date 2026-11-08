using System.Drawing;
using System.Globalization;
using MdViewer.Core.Highlighting;
using MdViewer.Core.Markdown;
using MdViewer.Render.Media;
using MdViewer.Render.Text;
using MdViewer.Render.Theming;

namespace MdViewer.Render.Layout;

/// <summary>Measures a parsed document into positioned visuals for a given width.</summary>
public sealed class LayoutEngine
{
    private readonly Theme _theme;
    private readonly FontBook _fonts;
    private readonly ImageCache _images;
    private readonly RenderSettings _settings;
    private readonly LayoutContext _context;
    private readonly InlineShaper _shaper;
    private MarkdownDocument _document = null!;
    private int _listDepth;

    public LayoutEngine(Theme theme, FontBook fonts, ImageCache images, RenderSettings settings)
    {
        _theme = theme;
        _fonts = fonts;
        _images = images;
        _settings = settings;
        _shaper = new InlineShaper(fonts, images, theme) { UnderlineLinks = settings.UnderlineLinks };
        _context = new LayoutContext
        {
            Theme = theme,
            Fonts = fonts,
            Images = images,
            Settings = settings,
            Shaper = _shaper,
        };
    }

    private Typography Type => _theme.Type;

    private float Px(float dip) => dip * _theme.Scale * _fonts.DpiScale;

    public DocumentLayout Layout(MarkdownDocument document, float width)
    {
        _document = document;
        _images.BaseDirectory = document.FilePath is null ? Directory.GetCurrentDirectory() : Path.GetDirectoryName(document.FilePath);
        _shaper.BaseDirectory = _images.BaseDirectory;

        var layout = new DocumentLayout { Width = width };
        float y = Px(Type.PagePaddingTop);
        layout.ContentTop = y;

        BlockLayout[] blocks = LayoutBlocks(document.Blocks, 0f, width, ref y);
        foreach (BlockLayout block in blocks)
        {
            layout.Blocks.Add(block);
            if (block.AnchorId is { Length: > 0 } id)
            {
                layout.Anchors[id] = block;
            }
        }

        layout.Height = y + Px(Type.PagePaddingBottom);

        if (_settings.ShowFrontMatter)
        {
            // front matter is drawn by the view as a header card, not as a block
        }

        foreach (BlockLayout block in layout.Blocks)
        {
            block.CollectLinks(layout.Links);
        }

        return layout;
    }

    // ------------------------------------------------------------- block flow

    private BlockLayout[] LayoutBlocks(Block[] blocks, float left, float width, ref float y)
    {
        var result = new List<BlockLayout>(blocks.Length);
        bool first = true;
        float carry = 0f;

        foreach (Block block in blocks)
        {
            float before = first ? 0f : Before(block);
            y += MathF.Max(carry, before);

            BlockLayout visual = LayoutBlock(block, left, width, ref y);
            result.Add(visual);
            carry = After(block);
            first = false;
        }

        y += carry > 0 ? 0f : 0f;
        return [.. result];
    }

    private float Before(Block block) => block switch
    {
        HeadingBlock heading => Px(Type.HeadingSpaceBefore[Math.Clamp(heading.Level - 1, 0, 5)]),
        FootnoteSectionBlock => Px(28f),
        _ => 0f,
    };

    private float After(Block block) => block switch
    {
        HeadingBlock heading => Px(Type.HeadingSpaceAfter[Math.Clamp(heading.Level - 1, 0, 5)]),
        ParagraphBlock => Px(Type.ParagraphGap),
        CodeBlock or MermaidBlock => Px(Type.BlockGap),
        TableBlock => Px(Type.BlockGap),
        ListBlock => Px(Type.BlockGap),
        QuoteBlock or AlertBlock => Px(Type.BlockGap),
        ImageBlock => Px(Type.BlockGap),
        MathBlock => Px(Type.BlockGap),
        DefinitionListBlock => Px(Type.BlockGap * 0.9f),
        TocBlock => Px(Type.BlockGap),
        ChainBlock => Px(Type.ParagraphGap),
        HtmlBlock => Px(Type.BlockGap),
        _ => Px(Type.BlockGap * 0.6f),
    };

    private BlockLayout LayoutBlock(Block block, float left, float width, ref float y)
    {
        switch (block)
        {
            case HeadingBlock heading:
                return LayoutHeading(heading, left, width, ref y);
            case ParagraphBlock paragraph:
                return LayoutParagraph(paragraph, left, width, ref y);
            case CodeBlock code:
                return LayoutCode(code, left, width, ref y);
            case MathBlock math:
                return LayoutMath(math, left, width, ref y);
            case QuoteBlock quote:
                return LayoutQuote(quote, left, width, ref y);
            case AlertBlock alert:
                return LayoutAlert(alert, left, width, ref y);
            case ListBlock list:
                return LayoutList(list, left, width, ref y, 0);
            case TableBlock table:
                return LayoutTable(table, left, width, ref y);
            case ThematicBreakBlock:
            {
                float height = Px(26f);
                var visual = new DividerVisual { Source = block, Ctx = _context, Top = y, Left = left, Width = width, Height = height };
                y += height;
                return visual;
            }

            case ImageBlock image:
                return LayoutImage(image, left, width, ref y);
            case HtmlBlock html:
                return LayoutHtml(html, left, width, ref y);
            case ChainBlock chain:
                return LayoutChain(chain, left, width, ref y);
            case DefinitionListBlock definitions:
                return LayoutDefinitions(definitions, left, width, ref y);
            case TocBlock:
                return LayoutToc(left, width, ref y);
            case MermaidBlock mermaid:
                return LayoutMermaid(mermaid, left, width, ref y);
            case FootnoteSectionBlock footnotes:
                return LayoutFootnotes(footnotes, left, width, ref y);
            default:
            {
                var visual = new DividerVisual { Source = block, Ctx = _context, Top = y, Left = left, Width = width, Height = Px(8f) };
                y += Px(8f);
                return visual;
            }
        }
    }

    // --------------------------------------------------------------- headings

    private BlockLayout LayoutHeading(HeadingBlock heading, float left, float width, ref float y)
    {
        float size = Type.HeadingSizes[Math.Clamp(heading.Level - 1, 0, 5)] * _theme.Scale;
        Font font = _fonts.DisplayFont(size, FontStyle.Bold);
        TextStyle style = BodyStyle(font, size, _theme.Colors.Text);
        ShapedText shaped = _shaper.Shape(heading.Inlines, width, style, 1.24f);

        bool rule = heading.Level <= 2;
        float ruleSpace = rule ? Px(12f) : 0f;
        float height = shaped.Height + ruleSpace;

        var visual = new HeadingVisual
        {
            Source = heading,
            Ctx = _context,
            Text = shaped,
            Level = heading.Level,
            Number = _settings.NumberHeadings ? heading.Number : null,
            ShowRule = rule,
            GutterLeft = left - Px(46f),
            Top = y,
            Left = left,
            Width = width,
            Height = height,
            AnchorId = heading.Id,
        };

        y += height;
        return visual;
    }

    private BlockLayout LayoutParagraph(ParagraphBlock paragraph, float left, float width, ref float y)
    {
        TextStyle style = BodyStyle(_fonts.ReadingFont(Type.BaseSize * _theme.Scale, _theme.SerifReading), Type.BaseSize * _theme.Scale, _theme.Colors.Text);
        ShapedText shaped = _shaper.Shape(paragraph.Inlines, width, style, Type.LineHeight);

        var visual = new TextVisual
        {
            Source = paragraph,
            Ctx = _context,
            Text = shaped,
            Top = y,
            Left = left,
            Width = width,
            Height = shaped.Height,
        };

        y += shaped.Height;
        return visual;
    }

    private TextStyle BodyStyle(Font font, float sizeDip, Color color) => new()
    {
        Font = font,
        Family = font.FontFamily.Name,
        SizeDip = sizeDip,
        Color = color,
    };

    // ------------------------------------------------------------------- code

    private BlockLayout LayoutCode(CodeBlock code, float left, float width, ref float y)
    {
        float lineHeight = Type.MonoSize * Type.MonoLineHeight * _theme.Scale;
        Font mono = _fonts.MonoFont(Type.MonoSize * _theme.Scale);
        FontLineMetrics metrics = _fonts.Metrics(mono);
        float ascent = metrics.Ascent;
        float padding = Px(14f);
        float header = code.Language is { Length: > 0 } || code.Title is { Length: > 0 } ? Px(30f) : 0f;
        float available = width - padding * 2f;

        LanguageMode mode = SyntaxHighlighter.Resolve(code.Language);
        var state = new HighlightState();
        List<Token> tokens = SyntaxHighlighter.Tokenize(code.Text, mode, state);

        string[] rawLines = code.Text.Length == 0 ? [string.Empty] : code.Text.Split('\n');
        var lines = new List<CodeLine>(rawLines.Length);
        int offset = 0;

        foreach (string raw in rawLines)
        {
            var line = new CodeLine { Height = MathF.Max(lineHeight, ascent + metrics.Descent) };
            int lineStart = offset;
            int lineEnd = offset + raw.Length;

            float column = 0f;
            int i = lineStart;
            while (i < lineEnd)
            {
                TokenKind kind = TokenKindOf(tokens, i);
                int runStart = i;
                while (i < lineEnd && TokenKindOf(tokens, i) == kind)
                {
                    i++;
                }

                string text = code.Text[runStart..i];
                float widthPx = _fonts.Measure(text, mono);

                if (_settings.WrapCode && column > 0 && column + widthPx > available)
                {
                    // simple character wrap: split this run at the boundary
                    string head = string.Empty;
                    float headWidth = 0f;
                    foreach (char ch in text)
                    {
                        float candidate = _fonts.Measure(head + ch, mono);
                        if (column + candidate > available && head.Length > 0)
                        {
                            break;
                        }

                        head += ch;
                        headWidth = candidate;
                    }

                    if (head.Length > 0)
                    {
                        line.Runs.Add(MakeCodeRun(head, kind, column, headWidth, mono, ascent, metrics));
                        line.Width = column + headWidth;
                        lines.Add(line);

                        line = new CodeLine { Height = lineHeight };
                        text = text[head.Length..];
                        widthPx = _fonts.Measure(text, mono);
                        column = 0;
                    }
                }

                if (text.Length > 0)
                {
                    line.Runs.Add(MakeCodeRun(text, kind, column, widthPx, mono, ascent, metrics));
                    column += widthPx;
                    line.Width = column;
                }
            }

            lines.Add(line);
            offset = lineEnd + 1;
        }

        float contentHeight = lines.Sum(l => l.Height);
        float height = header + padding + contentHeight + padding * 0.75f;

        var visual = new CodeVisual
        {
            Source = code,
            Ctx = _context,
            Language = code.Language ?? string.Empty,
            Title = code.Title,
            Lines = lines,
            Top = y,
            Left = left,
            Width = width,
            Height = height,
            AnchorId = "code-" + y.ToString("0", CultureInfo.InvariantCulture),
        };

        y += height;
        return visual;
    }

    private Run MakeCodeRun(string text, TokenKind kind, float x, float width, Font mono, float ascent, FontLineMetrics metrics) => new()
    {
        Text = text,
        Style = new TextStyle
        {
            Font = mono,
            Family = _fonts.MonoFamily,
            SizeDip = Type.MonoSize * _theme.Scale,
            Color = CodeColor(kind),
        },
        X = x,
        Width = width,
        Height = metrics.LineSpacing,
        Ascent = ascent,
    };

    private Color CodeColor(TokenKind kind) => kind switch
    {
        TokenKind.Keyword => _theme.Colors.Keyword,
        TokenKind.String => _theme.Colors.String,
        TokenKind.Number => _theme.Colors.Number,
        TokenKind.Comment => _theme.Colors.Comment,
        TokenKind.Type => _theme.Colors.TypeName,
        TokenKind.Function => _theme.Colors.Function,
        TokenKind.Punctuation => _theme.Colors.Punctuation,
        TokenKind.Property => _theme.Colors.TypeName,
        TokenKind.Tag => _theme.Colors.Keyword,
        TokenKind.Attribute => _theme.Colors.Function,
        _ => _theme.Colors.CodeText,
    };

    private static TokenKind TokenKindOf(List<Token> tokens, int index)
    {
        int lo = 0;
        int hi = tokens.Count - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            Token token = tokens[mid];
            if (index < token.Start)
            {
                hi = mid - 1;
            }
            else if (index >= token.Start + token.Length)
            {
                lo = mid + 1;
            }
            else
            {
                return token.Kind;
            }
        }

        return TokenKind.Plain;
    }

    // ------------------------------------------------------------------- math

    private BlockLayout LayoutMath(MathBlock math, float left, float width, ref float y)
    {
        float size = Type.BaseSize * 1.15f * _theme.Scale;
        Font font = _fonts.Font(_fonts.SerifFamily, size, FontStyle.Italic);
        TextStyle style = BodyStyle(font, size, _theme.Colors.Text);
        string display = MathText.ToDisplay(math.Tex);
        ShapedText shaped = _shaper.Shape([new TextInline(display)], width - Px(32f), style, 1.4f);

        float height = shaped.Height + Px(28f);
        var visual = new MathBlockVisual
        {
            Source = math,
            Ctx = _context,
            Text = shaped,
            Top = y,
            Left = left,
            Width = width,
            Height = height,
        };

        y += height;
        return visual;
    }

    // ---------------------------------------------------------------- quotes

    private BlockLayout LayoutQuote(QuoteBlock quote, float left, float width, ref float y)
    {
        float padX = Px(18f);
        float padY = Px(14f);
        float start = y;
        float innerY = start + padY;
        BlockLayout[] children = LayoutBlocks(quote.Children, left + padX, width - padX - Px(6f), ref innerY);
        float height = innerY - start + padY;

        var visual = new QuoteVisual
        {
            Source = quote,
            Ctx = _context,
            Children = children,
            Top = start,
            Left = left,
            Width = width,
            Height = height,
        };

        y = start + height;
        return visual;
    }

    private BlockLayout LayoutAlert(AlertBlock alert, float left, float width, ref float y)
    {
        float padTop = Px(40f);
        float padBottom = Px(14f);
        float indent = Px(38f);
        float start = y;
        float innerY = start + padTop;
        BlockLayout[] children = LayoutBlocks(alert.Children, left + indent, width - indent - Px(16f), ref innerY);
        float height = innerY - start + padBottom;

        var visual = new AlertVisual
        {
            Source = alert,
            Ctx = _context,
            Kind = alert.Kind,
            Label = alert.Kind.ToString(),
            Children = children,
            Top = start,
            Left = left,
            Width = width,
            Height = height,
        };

        y = start + height;
        return visual;
    }

    // ------------------------------------------------------------------ lists

    private BlockLayout LayoutList(ListBlock list, float left, float width, ref float y, int level)
    {
        float indent = Px(Type.ListIndent);
        float gap = Px(list.Tight ? Type.ListItemGap : Type.ListItemGap * 1.8f);
        float start = y;
        List<ListItemVisual> items = [with(capacity: list.Items.Length)];
        int number = list.Start;

        for (int index = 0; index < list.Items.Length; index++)
        {
            ListItem item = list.Items[index];
            float itemTop = y;
            float contentLeft = left + indent;
            float innerY = itemTop;

            _listDepth = level + 1;
            BlockLayout[] children = LayoutBlocks(item.Children, contentLeft, width - indent, ref innerY);
            _listDepth = level;

            // nested lists are laid out by LayoutBlocks; the marker tracks the first line
            float markerHeight = Px(20f);
            var marker = new RectangleF(left, itemTop, indent - Px(6f), markerHeight);
            items.Add(new ListItemVisual
            {
                Children = children,
                Marker = marker,
                IsOrdered = list.Ordered,
                Number = list.Ordered ? number.ToString(CultureInfo.InvariantCulture) + "." : null,
                Level = level,
                Task = item.Task,
            });

            y = innerY + gap;
            number++;
        }

        if (items.Count > 0)
        {
            y -= gap;
        }

        var visual = new ListVisual
        {
            Source = list,
            Ctx = _context,
            Items = [.. items],
            Top = start,
            Left = left,
            Width = width,
            Height = MathF.Max(0f, y - start),
        };

        return visual;
    }

    // ----------------------------------------------------------------- tables

    private BlockLayout LayoutTable(TableBlock table, float left, float width, ref float y)
    {
        int columns = table.Headers.Length;
        float padX = Px(12f * 2f);
        var headerStyles = new List<ShapedText>[columns];
        var preferred = new float[columns];
        var minimum = new float[columns];
        var cells = new List<string[]>(table.Rows.Length + 1) { table.Headers };
        foreach (string[] row in table.Rows)
        {
            cells.Add(row);
        }

        Font bodyFont = _fonts.ReadingFont(Type.BaseSize * 0.92f * _theme.Scale, _theme.SerifReading);
        Font headerFont = _fonts.UiFont(Type.BaseSize * 0.92f * _theme.Scale, FontStyle.Bold);
        TextStyle bodyStyle = BodyStyle(bodyFont, Type.BaseSize * 0.92f * _theme.Scale, _theme.Colors.Text);
        TextStyle headerStyle = BodyStyle(headerFont, Type.BaseSize * 0.92f * _theme.Scale, _theme.Colors.Text);

        for (int c = 0; c < columns; c++)
        {
            float maxWidth = 0f;
            float maxWord = 0f;
            for (int r = 0; r < cells.Count; r++)
            {
                Inline[] inlines = ParseCellInlines(cells[r][c]);
                TextStyle style = r == 0 ? headerStyle : bodyStyle;
                ShapedText probe = _shaper.Shape(inlines, 0f, style);
                float cellWidth = probe.Width;
                maxWidth = MathF.Max(maxWidth, cellWidth);
                foreach (string word in cells[r][c].Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    maxWord = MathF.Max(maxWord, _fonts.Measure(word, style.Font));
                }
            }

            preferred[c] = maxWidth + padX;
            minimum[c] = MathF.Max(maxWord + padX, Px(56f));
        }

        float available = width;
        float totalPreferred = preferred.Sum();
        float[] widths = new float[columns];

        if (totalPreferred <= available)
        {
            float extra = available - totalPreferred;
            float weightSum = preferred.Sum();
            for (int c = 0; c < columns; c++)
            {
                widths[c] = preferred[c] + extra * (weightSum <= 0 ? 1f / columns : preferred[c] / weightSum);
            }
        }
        else
        {
            float totalMinimum = minimum.Sum();
            float budget = MathF.Max(available, totalMinimum);
            float flexible = MathF.Max(0f, budget - totalMinimum);
            float preferredExtra = preferred.Sum() - minimum.Sum();

            for (int c = 0; c < columns; c++)
            {
                float share = preferredExtra <= 0 ? 1f / columns : (preferred[c] - minimum[c]) / preferredExtra;
                widths[c] = minimum[c] + flexible * share;
            }

            float sum = widths.Sum();
            if (sum > 0)
            {
                float scale = budget / sum;
                for (int c = 0; c < columns; c++)
                {
                    widths[c] *= scale;
                }
            }
        }

        // header cells
        var headerVisuals = new TableCellVisual[columns];
        for (int c = 0; c < columns; c++)
        {
            ShapedText shaped = _shaper.Shape(ParseCellInlines(table.Headers[c]), widths[c] - padX, headerStyle, 1.42f);
            headerVisuals[c] = new TableCellVisual
            {
                Text = shaped,
                Align = table.Aligns[c],
                EffectiveAlign = table.Aligns[c],
            };
        }

        float headerHeight = headerVisuals.Max(c => c.Text.Height) + Px(16f);
        var rowVisuals = new List<TableCellVisual[]>(table.Rows.Length);
        var rowHeights = new List<float>(table.Rows.Length);

        foreach (string[] row in table.Rows)
        {
            var rowCells = new TableCellVisual[columns];
            float rowHeight = 0f;
            for (int c = 0; c < columns; c++)
            {
                ShapedText shaped = _shaper.Shape(ParseCellInlines(row[c]), widths[c] - padX, bodyStyle, 1.42f);
                rowCells[c] = new TableCellVisual
                {
                    Text = shaped,
                    Align = table.Aligns[c],
                    EffectiveAlign = table.Aligns[c],
                };
                rowHeight = MathF.Max(rowHeight, shaped.Height);
            }

            rowVisuals.Add(rowCells);
            rowHeights.Add(rowHeight + Px(16f));
        }

        float height = headerHeight + rowHeights.Sum();

        var visual = new TableVisual
        {
            Source = table,
            Ctx = _context,
            Headers = headerVisuals,
            Rows = [.. rowVisuals],
            ColumnWidths = widths,
            RowHeights = [.. rowHeights],
            HeaderHeight = headerHeight,
            IsGlossary = table.IsGlossary,
            Top = y,
            Left = left,
            Width = width,
            Height = height,
        };

        y += height;
        return visual;
    }

    private Inline[] ParseCellInlines(string cell)
    {
        if (cell.Length == 0)
        {
            return [];
        }

        var parser = new MarkdownParser();
        MarkdownDocument parsed = parser.Parse(cell);
        if (parsed.Blocks is [ParagraphBlock paragraph])
        {
            return paragraph.Inlines;
        }

        return [new TextInline(cell)];
    }

    // ----------------------------------------------------------------- images

    private BlockLayout LayoutImage(ImageBlock image, float left, float width, ref float y)
    {
        ImageAsset asset = _images.Resolve(image.Url);
        float maxWidth = width;
        float imageWidth;
        float imageHeight;

        if (asset.IsUsable)
        {
            imageWidth = image.Width is int explicitWidth
                ? Px(explicitWidth)
                : asset.PixelSize.Width * _theme.Scale * _fonts.DpiScale;

            imageHeight = asset.PixelSize.Width == 0
                ? asset.PixelSize.Height * _theme.Scale * _fonts.DpiScale
                : imageWidth * asset.PixelSize.Height / asset.PixelSize.Width;

            if (imageWidth > maxWidth)
            {
                imageHeight *= maxWidth / imageWidth;
                imageWidth = maxWidth;
            }
        }
        else
        {
            imageWidth = maxWidth;
            imageHeight = Px(72f);
        }

        string? caption = null;
        TextStyle captionStyle = BodyStyle(_fonts.UiFont(Type.BaseSize * 0.82f * _theme.Scale), Type.BaseSize * 0.82f * _theme.Scale, _theme.Colors.TextMuted);
        ShapedText? captionText = null;
        float captionHeight = 0f;

        string captionSource = string.IsNullOrWhiteSpace(image.Alt) ? string.Empty : image.Alt;
        if (captionSource.Length > 0)
        {
            caption = captionSource;
            captionText = _shaper.Shape([new TextInline(captionSource)], width, captionStyle, 1.4f);
            captionHeight = captionText.Height + Px(8f);
        }

        float totalHeight = imageHeight + captionHeight;
        float imageLeft = left + (width - imageWidth) / 2f;

        var visual = new ImageVisual
        {
            Source = image,
            Ctx = _context,
            Asset = asset,
            ImageRect = new RectangleF(imageLeft, y, imageWidth, imageHeight),
            Caption = caption,
            Alt = image.Alt,
            Top = y,
            Left = left,
            Width = width,
            Height = totalHeight,
        };

        if (captionText is not null)
        {
            visual.CaptionText = captionText;
            visual.CaptionTop = y + imageHeight + Px(8f);
        }

        y += totalHeight;
        return visual;
    }

    // ------------------------------------------------------------------- html

    private BlockLayout LayoutHtml(HtmlBlock html, float left, float width, ref float y)
    {
        Font mono = _fonts.MonoFont(Type.MonoSize * 0.94f * _theme.Scale);
        TextStyle style = BodyStyle(mono, Type.MonoSize * 0.94f * _theme.Scale, _theme.Colors.CodeText);
        ShapedText shaped = _shaper.Shape([new TextInline(html.Html)], width - Px(28f), style, 1.5f);
        float height = shaped.Height + Px(40f);

        var visual = new HtmlVisual
        {
            Source = html,
            Ctx = _context,
            Text = shaped,
            Top = y,
            Left = left,
            Width = width,
            Height = height,
        };

        y += height;
        return visual;
    }

    // ----------------------------------------------------------------- chains

    private BlockLayout LayoutChain(ChainBlock chain, float left, float width, ref float y)
    {
        Font font = _fonts.UiFont(Type.BaseSize * 0.86f * _theme.Scale);
        float padX = Px(10f);
        float chipHeight = Px(26f);
        float arrowWidth = Px(26f);
        float rowGap = Px(10f);

        var parts = new List<(string Text, bool IsNode, bool IsArrow, RectangleF Bounds)>(chain.Parts.Length);
        float x = left;
        float rowTop = y;

        foreach (ChainPart part in chain.Parts)
        {
            if (part.IsArrow)
            {
                if (x + arrowWidth > left + width && x > left)
                {
                    x = left;
                    rowTop += chipHeight + rowGap;
                }

                parts.Add((part.Text, false, true, new RectangleF(x, rowTop, arrowWidth, chipHeight)));
                x += arrowWidth;
                continue;
            }

            float textWidth = _fonts.Measure(part.Text, font);
            float chipWidth = textWidth + padX * 2f;
            if (x + chipWidth > left + width && x > left)
            {
                x = left;
                rowTop += chipHeight + rowGap;
            }

            parts.Add((part.Text, true, false, new RectangleF(x, rowTop, chipWidth, chipHeight)));
            x += chipWidth + Px(2f);
        }

        float height = rowTop - y + chipHeight;
        var visual = new ChainVisual
        {
            Source = chain,
            Ctx = _context,
            Parts = parts,
            Top = y,
            Left = left,
            Width = width,
            Height = height,
        };

        y += height;
        return visual;
    }

    // ------------------------------------------------------------ definitions

    private BlockLayout LayoutDefinitions(DefinitionListBlock block, float left, float width, ref float y)
    {
        float start = y;
        float termFontSize = Type.BaseSize * _theme.Scale;
        Font termFont = _fonts.ReadingFont(termFontSize, _theme.SerifReading, FontStyle.Bold);
        TextStyle termStyle = BodyStyle(termFont, termFontSize, _theme.Colors.Text);

        var term = _shaper.Shape([new TextInline(block.Items[0].Term)], width, termStyle);
        float termHeight = term.Height;

        var definitionVisuals = new List<BlockLayout[]>(block.Items.Length);
        float indent = Px(20f);
        float innerY = start + termHeight + Px(6f);

        for (int i = 0; i < block.Items[0].Definitions.Length; i++)
        {
            var lines = new List<Block> { new ParagraphBlock { Inlines = [new TextInline(string.Join('\n', block.Items[0].Definitions[i]))] } };
            definitionVisuals.Add(LayoutBlocks([.. lines], left + indent, width - indent, ref innerY));
            innerY += Px(2f);
        }

        var visual = new DefinitionVisual
        {
            Source = block,
            Ctx = _context,
            Term = term,
            Definitions = [.. definitionVisuals],
            Top = start,
            Left = left,
            Width = width,
            Height = innerY - start,
        };

        y = innerY;
        return visual;
    }

    // -------------------------------------------------------------------- toc

    private BlockLayout LayoutToc(float left, float width, ref float y)
    {
        if (_document.Outline.Count == 0)
        {
            float emptyHeight = Px(40f);
            var empty = new DividerVisual { Source = new TocBlock(), Ctx = _context, Top = y, Left = left, Width = width, Height = emptyHeight };
            y += emptyHeight;
            return empty;
        }

        Font font = _fonts.UiFont(Type.BaseSize * 0.94f * _theme.Scale);
        float rowBase = Type.BaseSize * 0.94f * _theme.Scale * 1.75f;
        var items = new List<(int Level, string Text, string Target, RectangleF Bounds)>();
        float rowTop = y + Px(34f);
        float indentUnit = Px(14f);

        foreach (HeadingInfo heading in _document.Outline)
        {
            if (heading.Level > 4)
            {
                continue;
            }

            float indent = (heading.Level - 1) * indentUnit;
            float rowHeight = rowBase;
            var bounds = new RectangleF(left + Px(16f) + indent, rowTop, width - Px(32f) - indent, rowHeight);
            string text = heading.Number is { Length: > 0 } && _settings.NumberHeadings ? heading.Number + "  " + heading.Text : heading.Text;
            items.Add((heading.Level, text, heading.Id, bounds));
            rowTop += rowHeight;
        }

        float height = rowTop - y + Px(14f);
        var visual = new TocVisual
        {
            Source = new TocBlock(),
            Ctx = _context,
            Items = items,
            Top = y,
            Left = left,
            Width = width,
            Height = height,
        };

        y += height;
        return visual;
    }

    // ---------------------------------------------------------------- mermaid

    private BlockLayout LayoutMermaid(MermaidBlock block, float left, float width, ref float y)
    {
        if (MdViewer.Core.Diagrams.MermaidDiagrams.TryParse(block.Source, out MdViewer.Core.Diagrams.MermaidDiagram? model, out string? error) && model is not null)
        {
            BlockLayout? graphic = model switch
            {
                MdViewer.Core.Diagrams.FlowchartDiagram flow => DiagramVisual.Create(_context, block, flow.Chart, flow.IsState, left, y, width),
                MdViewer.Core.Diagrams.SequenceDiagram sequence => SequenceVisual.Create(_context, block, sequence, left, y, width),
                MdViewer.Core.Diagrams.PieDiagram pie => PieVisual.Create(_context, block, pie, left, y, width),
                MdViewer.Core.Diagrams.ClassDiagram classes => ClassVisual.Create(_context, block, classes, left, y, width),
                MdViewer.Core.Diagrams.GanttDiagram gantt => GanttVisual.Create(_context, block, gantt, left, y, width),
                MdViewer.Core.Diagrams.JourneyDiagram journey => JourneyVisual.Create(_context, block, journey, left, y, width),
                _ => null,
            };

            if (graphic is not null)
            {
                graphic.AnchorId = "diagram-" + y.ToString("0", CultureInfo.InvariantCulture);
                y += graphic.Height;
                return graphic;
            }
        }

        Font mono = _fonts.MonoFont(Type.MonoSize * 0.9f * _theme.Scale);
        float lineHeight = _fonts.Metrics(mono).LineSpacing;
        int lines = Math.Max(1, block.Source.Split('\n').Length);
        float height = Px(30f) + lines * lineHeight + Px(18f);

        var visual = new MermaidVisual
        {
            Source = block,
            Ctx = _context,
            SourceText = block.Source,
            Top = y,
            Left = left,
            Width = width,
            Height = height,
            Message = error,
        };

        y += height;
        return visual;
    }

    // -------------------------------------------------------------- footnotes

    private BlockLayout LayoutFootnotes(FootnoteSectionBlock block, float left, float width, ref float y)
    {
        float start = y + Px(10f);
        float innerY = start + Px(22f);
        var notes = new List<FootnoteVisual>(block.Notes.Length);

        foreach (FootnoteNote note in block.Notes)
        {
            float noteTop = innerY;
            float noteInner = noteTop;
            BlockLayout[] children = LayoutBlocks(note.Children, left + Px(26f), width - Px(26f), ref noteInner);
            notes.Add(new FootnoteVisual
            {
                Source = note.Children.Length > 0 ? note.Children[0] : new TocBlock(),
                Ctx = _context,
                Number = note.Index,
                Children = children,
                AnchorId = "footnote-" + note.Index.ToString(CultureInfo.InvariantCulture),
                Top = noteTop,
                Left = left,
                Width = width,
                Height = noteInner - noteTop,
            });

            innerY = noteInner + Px(10f);
        }

        float height = innerY - y;
        var visual = new FootnotesSectionVisual
        {
            Source = block,
            Ctx = _context,
            Notes = [.. notes],
            Top = y,
            Left = left,
            Width = width,
            Height = height,
            AnchorId = "footnotes",
        };

        y += height;
        return visual;
    }
}








