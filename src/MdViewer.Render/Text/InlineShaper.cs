using System.Drawing;
using System.Text;
using MdViewer.Core.Markdown;
using MdViewer.Render.Media;
using MdViewer.Render.Theming;

namespace MdViewer.Render.Text;

/// <summary>
/// Turns inline Markdown nodes into positioned lines. Works in device pixels:
/// every size handed in by the layout is already scaled for zoom and DPI.
/// </summary>
public sealed class InlineShaper(FontBook fonts, ImageCache images, Theme theme)
{
    private enum AtomKind
    {
        Word,
        Space,
        HardBreak,
        Image,
    }

    private sealed class Atom
    {
        public string Text = string.Empty;
        public TextStyle Style = null!;
        public AtomKind Kind;
        public float Width;
        public float Height;
        public ImageAsset? Image;
        public float SpaceWidth;
    }

    public string? BaseDirectory { get; set; }

    public bool UnderlineLinks { get; set; }

    /// <summary>Shapes <paramref name="inlines"/> into lines no wider than <paramref name="maxWidth"/>.</summary>
    private float _lineHeightFactor = 1.55f;

    public ShapedText Shape(Inline[] inlines, float maxWidth, TextStyle baseStyle, float lineHeightFactor = 1.55f)
    {
        _lineHeightFactor = lineHeightFactor;
        var atoms = new List<Atom>(16);
        foreach (Inline inline in inlines)
        {
            Flatten(inline, baseStyle, atoms);
        }

        return Layout(atoms, maxWidth, baseStyle);
    }

    /// <summary>Measures text width without wrapping (used for table columns).</summary>
    public float MeasureText(string text, TextStyle style) => fonts.Measure(text, style.Font);

    private void Flatten(Inline inline, TextStyle style, List<Atom> atoms)
    {
        switch (inline)
        {
            case TextInline text:
                AddText(text.Text, style, atoms);
                break;

            case CodeInline code:
            {
                TextStyle codeStyle = style.Clone();
                codeStyle.Family = fonts.MonoFamily;
                codeStyle.SizeDip = style.SizeDip * 0.9f;
                codeStyle.Font = fonts.MonoFont(codeStyle.SizeDip);
                codeStyle.Background = theme.Colors.InlineCodeBg;
                codeStyle.RoundedBackground = true;
                codeStyle.Color = theme.Colors.InlineCodeText;
                codeStyle.PaddingX = MathF.Round(4f * theme.Scale * fonts.DpiScale, 1);
                AddText(code.Text, codeStyle, atoms, breakable: false);
                break;
            }

            case StyledInline styled:
            {
                TextStyle child = style.Clone();
                switch (styled.Style)
                {
                    case InlineStyle.Strong:
                        child.FontStyle = (child.FontStyle & FontStyle.Italic) | FontStyle.Bold;
                        child.Font = fonts.Font(child.Family, child.SizeDip, child.FontStyle);
                        break;
                    case InlineStyle.Emphasis:
                        child.FontStyle = (child.FontStyle & FontStyle.Bold) | FontStyle.Italic;
                        child.Font = fonts.Font(child.Family, child.SizeDip, child.FontStyle);
                        break;
                    case InlineStyle.Strike:
                        child.Strike = true;
                        break;
                    case InlineStyle.Mark:
                        child.Background = theme.Colors.MarkBg;
                        child.PaddingX = MathF.Round(1.5f * theme.Scale * fonts.DpiScale, 1);
                        break;
                    case InlineStyle.Subscript:
                        child.SizeDip = style.SizeDip * 0.78f;
                        child.Font = fonts.Font(child.Family, child.SizeDip, child.FontStyle);
                        child.BaselineShift = -style.SizeDip * 0.18f * theme.Scale * fonts.DpiScale;
                        break;
                    case InlineStyle.Superscript:
                        child.SizeDip = style.SizeDip * 0.78f;
                        child.Font = fonts.Font(child.Family, child.SizeDip, child.FontStyle);
                        child.BaselineShift = style.SizeDip * 0.34f * theme.Scale * fonts.DpiScale;
                        break;
                }

                foreach (Inline nested in styled.Children)
                {
                    Flatten(nested, child, atoms);
                }

                break;
            }

            case LinkInline link:
            {
                TextStyle child = style.Clone();
                child.Color = link.Missing ? theme.Colors.MissingLink : theme.Colors.Link;
                child.Underline = UnderlineLinks || link.Missing;
                child.LinkUrl = link.Url;
                child.MissingLink = link.Missing;

                if (link.Children.Length == 0)
                {
                    AddText(link.Url, child, atoms);
                }
                else
                {
                    foreach (Inline nested in link.Children)
                    {
                        Flatten(nested, child, atoms);
                    }
                }

                break;
            }

            case ImageInline image:
            {
                float maxWidth = MathF.Max(48f, 540f * theme.Scale * fonts.DpiScale);
                ImageAsset asset = images.Resolve(image.Url, BaseDirectory);
                float width;
                float height;

                if (asset.IsUsable)
                {
                    width = image.Width is int explicitWidth
                        ? explicitWidth * theme.Scale * fonts.DpiScale
                        : asset.PixelSize.Width * theme.Scale * fonts.DpiScale;
                    height = asset.PixelSize.Width == 0
                        ? asset.PixelSize.Height * theme.Scale * fonts.DpiScale
                        : width * asset.PixelSize.Height / asset.PixelSize.Width;

                    if (width > maxWidth)
                    {
                        height *= maxWidth / width;
                        width = maxWidth;
                    }
                }
                else
                {
                    width = MathF.Min(maxWidth, fonts.Measure(image.Alt.Length > 0 ? image.Alt : "image", style.Font) + 20f);
                    height = style.SizeDip * theme.Scale * fonts.DpiScale * 1.2f;
                }

                atoms.Add(new Atom
                {
                    Text = image.Alt,
                    Style = style,
                    Kind = AtomKind.Image,
                    Width = MathF.Max(8f, width),
                    Height = MathF.Max(8f, height),
                    Image = asset,
                });
                break;
            }

            case MathInline math:
            {
                TextStyle child = style.Clone();
                child.Family = fonts.SerifFamily;
                child.SizeDip = style.SizeDip * 1.02f;
                child.Font = fonts.Font(child.Family, child.SizeDip, FontStyle.Italic);
                child.Color = style.Color;
                string display = MathText.ToDisplay(math.Tex);
                AddText(display, child, atoms, breakable: false);
                break;
            }

            case FootnoteRefInline foot:
            {
                TextStyle child = style.Clone();
                child.SizeDip = style.SizeDip * 0.7f;
                child.Font = fonts.Font(child.Family, child.SizeDip, child.FontStyle);
                child.BaselineShift = style.SizeDip * 0.38f * theme.Scale * fonts.DpiScale;
                child.Color = theme.Colors.Accent;
                child.Footnote = foot.Index;
                AddText(foot.Index.ToString(System.Globalization.CultureInfo.InvariantCulture), child, atoms, breakable: false);
                break;
            }

            case LineBreakInline lineBreak:
                atoms.Add(new Atom { Kind = AtomKind.HardBreak, Style = style });
                break;
        }
    }

    private void AddText(string text, TextStyle style, List<Atom> atoms, bool breakable = true)
    {
        if (text.Length == 0)
        {
            return;
        }

        if (!breakable)
        {
            atoms.Add(new Atom
            {
                Text = text,
                Style = style,
                Kind = AtomKind.Word,
                Width = fonts.Measure(text, style.Font),
                Height = fonts.Metrics(style.Font).LineSpacing,
            });
            return;
        }

        int start = 0;
        for (int i = 0; i <= text.Length; i++)
        {
            bool boundary = i == text.Length || text[i] == ' ';
            if (!boundary)
            {
                continue;
            }

            if (i > start)
            {
                string word = text[start..i];
                atoms.Add(new Atom
                {
                    Text = word,
                    Style = style,
                    Kind = AtomKind.Word,
                    Width = fonts.Measure(word, style.Font),
                    Height = fonts.Metrics(style.Font).LineSpacing,
                });
            }

            if (i < text.Length)
            {
                float spaceWidth = fonts.Measure(" ", style.Font);
                atoms.Add(new Atom
                {
                    Text = " ",
                    Style = style,
                    Kind = AtomKind.Space,
                    Width = spaceWidth,
                    SpaceWidth = spaceWidth,
                    Height = fonts.Metrics(style.Font).LineSpacing,
                });
            }

            start = i + 1;
        }
    }

    private ShapedText Layout(List<Atom> atoms, float maxWidth, TextStyle baseStyle)
    {
        var result = new ShapedText();
        FontLineMetrics baseMetrics = fonts.Metrics(baseStyle.Font);
        float baseAscent = baseMetrics.Ascent;
        float baseDescent = baseMetrics.Descent;

        var line = new List<(Atom Atom, float X)>();
        float width = 0f;
        bool pendingSpace = false;
        float pendingSpaceWidth = 0f;

        void Flush(bool force)
        {
            if (line.Count == 0 && !force)
            {
                pendingSpace = false;
                pendingSpaceWidth = 0f;
                return;
            }

            var shaped = new ShapedLine();
            float ascent = baseAscent;
            float descent = baseDescent;

            foreach ((Atom atom, float x) in line)
            {
                if (atom.Kind == AtomKind.Image)
                {
                    ascent = MathF.Max(ascent, atom.Height);
                    continue;
                }

                FontLineMetrics metrics = fonts.Metrics(atom.Style.Font);
                ascent = MathF.Max(ascent, metrics.Ascent + atom.Style.BaselineShift);
                descent = MathF.Max(descent, metrics.Descent - atom.Style.BaselineShift);
            }

            foreach ((Atom atom, float x) in line)
            {
                if (atom.Kind == AtomKind.Image)
                {
                    shaped.Runs.Add(new Run
                    {
                        Text = atom.Text,
                        Style = atom.Style,
                        X = x,
                        Width = atom.Width,
                        Height = atom.Height,
                        Ascent = atom.Height,
                        Baseline = ascent,
                    });
                    continue;
                }

                shaped.Runs.Add(new Run
                {
                    Text = atom.Text,
                    Style = atom.Style,
                    X = x,
                    Width = atom.Width,
                    Height = fonts.Metrics(atom.Style.Font).LineSpacing,
                    Ascent = fonts.Metrics(atom.Style.Font).Ascent,
                    Baseline = ascent - atom.Style.BaselineShift,
                });
            }

            MergeRuns(shaped);
            shaped.Ascent = ascent;
            float minimum = baseStyle.SizeDip * _lineHeightFactor * fonts.DpiScale;
            shaped.Height = MathF.Max(ascent + descent, minimum);
            shaped.Width = width;
            result.Lines.Add(shaped);

            line.Clear();
            width = 0f;
            pendingSpace = false;
            pendingSpaceWidth = 0f;
        }

        foreach (Atom atom in atoms)
        {
            switch (atom.Kind)
            {
                case AtomKind.HardBreak:
                    Flush(force: true);
                    continue;

                case AtomKind.Space:
                    if (line.Count == 0)
                    {
                        continue;
                    }

                    pendingSpace = true;
                    pendingSpaceWidth = atom.SpaceWidth;
                    continue;
            }

            float extra = pendingSpace ? pendingSpaceWidth : 0f;
            if (maxWidth > 0 && line.Count > 0 && width + extra + atom.Width > maxWidth)
            {
                Flush(force: false);
                extra = 0f;
            }

            if (pendingSpace && line.Count > 0)
            {
                width += pendingSpaceWidth;
                pendingSpace = false;
            }

            if (maxWidth > 0 && atom.Width > maxWidth && line.Count == 0)
            {
                // a single token wider than the column: break it by characters
                foreach ((string chunk, float chunkWidth) in SplitLongToken(atom, maxWidth))
                {
                    line.Add((new Atom
                    {
                        Text = chunk,
                        Style = atom.Style,
                        Kind = AtomKind.Word,
                        Width = chunkWidth,
                        Height = atom.Height,
                    }, width));
                    width += chunkWidth;
                    Flush(force: false);
                }

                continue;
            }

            line.Add((atom, width));
            width += atom.Width;
        }

        Flush(force: false);
        if (result.Lines.Count == 0)
        {
            result.Lines.Add(new ShapedLine { Ascent = baseAscent, Height = baseAscent + baseDescent });
        }

        result.Width = result.Lines.Count == 0 ? 0f : result.Lines.Max(l => l.Width);
        result.Height = result.Lines.Sum(l => l.Height);
        return result;
    }

    /// <summary>Joins neighbouring runs that share one style, so kerning stays tight.</summary>
    private static void MergeRuns(ShapedLine line)
    {
        if (line.Runs.Count < 2)
        {
            return;
        }

        var merged = new List<Run>(line.Runs.Count);
        foreach (Run run in line.Runs)
        {
            if (merged.Count > 0)
            {
                Run previous = merged[^1];
                bool sameStyle = ReferenceEquals(previous.Style, run.Style)
                    && previous.Style.Background is null
                    && run.Style.Background is null
                    && MathF.Abs(previous.X + previous.Width - run.X) < 0.6f;

                if (sameStyle)
                {
                    merged[^1] = new Run
                    {
                        Text = previous.Text + run.Text,
                        Style = previous.Style,
                        X = previous.X,
                        Width = previous.Width + run.Width,
                        Height = previous.Height,
                        Ascent = previous.Ascent,
                        Baseline = previous.Baseline,
                    };
                    continue;
                }
            }

            merged.Add(run);
        }

        line.Runs.Clear();
        line.Runs.AddRange(merged);
    }

    private IEnumerable<(string Chunk, float Width)> SplitLongToken(Atom atom, float maxWidth)
    {
        var sb = new StringBuilder();
        float width = 0f;
        foreach (char c in atom.Text)
        {
            string candidate = sb.ToString() + c;
            float candidateWidth = fonts.Measure(candidate, atom.Style.Font);
            if (candidateWidth > maxWidth && sb.Length > 0)
            {
                yield return (sb.ToString(), width);
                sb.Clear();
                width = 0f;
                candidate = c.ToString();
                candidateWidth = fonts.Measure(candidate, atom.Style.Font);
            }

            sb.Append(c);
            width = candidateWidth;
        }

        if (sb.Length > 0)
        {
            yield return (sb.ToString(), width);
        }
    }
}



