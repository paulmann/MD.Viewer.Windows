using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MdViewer.Core.Highlighting;
using MdViewer.Render.Text;
using MdViewer.Render.Theming;

namespace MdViewer.App;

/// <summary>
/// A small, self-contained Markdown text editor: soft wrap, syntax colouring,
/// line numbers, block indent, undo history, find highlighting and the usual
/// caret/selection editing verbs. No RichEdit, no third-party control.
/// </summary>
public sealed class MarkdownEditor : Control
{
    private sealed class VisualLine
    {
        public int Start;
        public int Length;
        public bool HardBreak;
    }

    private sealed class UndoEntry
    {
        public string Text = string.Empty;
        public int Caret;
        public int Anchor;
        public DateTime Stamp;
        public bool Typing;
    }

    private readonly List<UndoEntry> _undo = [];
    private readonly List<UndoEntry> _redo = [];
    private readonly System.Windows.Forms.Timer _blink;
    private readonly VScroll _scroll = new();

    private string _text = string.Empty;
    private List<VisualLine> _lines = [];
    private int _wrapWidth = -1;
    private string _wrapText = "\u0000";

    private int _caret;
    private int _anchor;
    private bool _caretVisible = true;
    private bool _dragging;
    private int _clickCount;
    private DateTime _lastClick = DateTime.MinValue;
    private Point _lastClickPoint;
    private float _scrollY;
    private List<Token> _tokens = [];
    private List<(int Start, int Length)> _hits = [];
    private int _currentHit = -1;
    private Theme _theme = ThemeCatalog.Light;
    private FontBook _fonts = new(1f);

    public MarkdownEditor()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
        TabStop = true;

        _blink = new System.Windows.Forms.Timer { Interval = 530 };
        _blink.Tick += (sender, args) =>
        {
            if (!Focused)
            {
                return;
            }

            _caretVisible = !_caretVisible;
            InvalidateCaret();
        };
    }

    public event EventHandler? TextChangedByUser;

    public event EventHandler? CaretMoved;

    public event EventHandler<int>? ZoomRequested;

    public new event EventHandler? TextChanged;

    public new string Text
    {
        get => _text;
        set
        {
            string normalized = (value ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
            if (normalized == _text)
            {
                return;
            }

            _text = normalized;
            _caret = Math.Min(_caret, _text.Length);
            _anchor = Math.Min(_anchor, _text.Length);
            InvalidateWrap();
            TextChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool IsReadOnly { get; set; }

    public bool Dirty { get; set; }

    public bool ShowLineNumbers { get; set; } = true;

    public bool HighlightMarkdown { get; set; } = true;

    public int CaretIndex => _caret;

    public int LineCount => _lines.Count;

    public (int Line, int Column) CaretPosition
    {
        get
        {
            EnsureWrap();
            int index = LineOf(_caret);
            return (index + 1, _caret - _lines[index].Start + 1);
        }
    }

    public int CaretLine => CaretPosition.Line;

    public string SelectedText
    {
        get
        {
            (int start, int end) = Selection;
            return start == end ? string.Empty : _text[start..end];
        }
    }

    public bool HasSelection => _caret != _anchor;

    public void ApplyTheme(Theme theme, FontBook fonts)
    {
        _theme = theme;
        _fonts = fonts;
        InvalidateWrap();
        Invalidate();
    }

    private float DpiScale => DeviceDpi / 96f;

    private float LineHeight => MathF.Round(_theme.Type.MonoSize * _theme.Scale * DpiScale * 1.62f, 1);

    private Font MonoFont => _fonts.MonoFont(_theme.Type.MonoSize * _theme.Scale);

    private Typography Type => _theme.Type;

    private Palette Colors => _theme.Colors;

    private float GutterWidth
    {
        get
        {
            if (!ShowLineNumbers)
            {
                return 0f;
            }

            int digits = Math.Max(2, LineCount.ToString(System.Globalization.CultureInfo.InvariantCulture).Length);
            return _fonts.Measure(new string('0', digits), _fonts.MonoFont(Type.MonoSize * 0.86f * _theme.Scale)) + 22f * _theme.Scale * DpiScale;
        }
    }

    private float TextLeft => GutterWidth + 12f * _theme.Scale * DpiScale;

    private float WrapWidth => MathF.Max(120f, ClientSize.Width - TextLeft - 18f);

    // ------------------------------------------------------------- wrapping

    private void InvalidateWrap()
    {
        _wrapText = "\u0000";
        _tokens.Clear();
    }

    private void EnsureWrap()
    {
        if (_wrapText == _text && Math.Abs(_wrapWidth - WrapWidth) < 0.5f)
        {
            return;
        }

        _wrapText = _text;
        _wrapWidth = (int)WrapWidth;
        _lines = BuildLines(_text, MonoFont, WrapWidth);
        _tokens = HighlightMarkdown
            ? SyntaxHighlighter.Tokenize(_text, LanguageMode.Markdown, new HighlightState())
            : [];
        _scroll.Max = MathF.Max(0f, _lines.Count * LineHeight - ClientSize.Height + 8f);
    }

    private List<VisualLine> BuildLines(string text, Font font, float width)
    {
        var lines = new List<VisualLine>(Math.Max(4, text.Length / 60));
        int index = 0;

        while (index <= text.Length)
        {
            int newline = text.IndexOf('\n', index);
            int end = newline < 0 ? text.Length : newline;

            if (end - index <= 0)
            {
                lines.Add(new VisualLine { Start = index, Length = 0, HardBreak = newline >= 0 });
                index = end + 1;
                if (newline < 0)
                {
                    break;
                }

                continue;
            }

            int cursor = index;
            while (cursor < end)
            {
                float measured = _fonts.Measure(text[cursor..end], font);
                if (measured <= width)
                {
                    lines.Add(new VisualLine { Start = cursor, Length = end - cursor, HardBreak = newline >= 0 });
                    cursor = end;
                    break;
                }

                // find the furthest word boundary that fits
                int best = -1;
                int probe = cursor + 1;
                while (probe <= end)
                {
                    int space = text.IndexOf(' ', probe, end - probe);
                    int candidate = space < 0 ? end : space;
                    float w = _fonts.Measure(text[cursor..candidate], font);
                    if (w <= width)
                    {
                        best = candidate;
                        if (space < 0)
                        {
                            break;
                        }

                        probe = space + 1;
                        continue;
                    }

                    break;
                }

                if (best <= cursor)
                {
                    // no space fits: hard-break by characters
                    int chars = Math.Max(1, (int)((width / MathF.Max(1f, _fonts.Measure(text[cursor..end], font))) * (end - cursor)));
                    best = Math.Min(end, cursor + chars);
                    if (best <= cursor)
                    {
                        best = cursor + 1;
                    }
                }

                lines.Add(new VisualLine { Start = cursor, Length = best - cursor, HardBreak = false });
                cursor = best;
                if (cursor < end && text[cursor] == ' ')
                {
                    cursor++;
                }
            }

            index = end + 1;
            if (newline < 0)
            {
                break;
            }
        }

        if (lines.Count == 0)
        {
            lines.Add(new VisualLine { Start = 0, Length = 0 });
        }

        return lines;
    }

    private int LineOf(int offset)
    {
        EnsureWrap();
        int lo = 0;
        int hi = _lines.Count - 1;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) / 2;
            if (_lines[mid].Start <= offset)
            {
                lo = mid;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return lo;
    }

    private float LineTop(int index) => index * LineHeight - _scrollY;

    private float XInLine(int lineIndex, int offset)
    {
        VisualLine line = _lines[lineIndex];
        int from = line.Start;
        int length = Math.Clamp(offset - from, 0, line.Length);
        return TextLeft + _fonts.Measure(_text.Substring(from, length), MonoFont);
    }

    // -------------------------------------------------------------- scrolling

    public void ScrollToLine(int oneBasedLine)
    {
        EnsureWrap();
        int index = Math.Clamp(oneBasedLine - 1, 0, _lines.Count - 1);
        int offset = _lines[index].Start;
        SetCaret(offset, select: false);
    }

    public void GoToOffset(int offset, bool select = false)
    {
        SetCaret(Math.Clamp(offset, 0, _text.Length), select);
    }

    private void EnsureCaretVisible()
    {
        int line = LineOf(_caret);
        float top = line * LineHeight;
        float bottom = top + LineHeight;

        if (top - _scrollY < 0)
        {
            _scrollY = MathF.Max(0f, top - LineHeight);
        }
        else if (bottom - _scrollY > ClientSize.Height - 6)
        {
            _scrollY = bottom - ClientSize.Height + 12f;
        }

        ClampScroll();
    }

    private void ClampScroll()
    {
        float max = MathF.Max(0f, _lines.Count * LineHeight - ClientSize.Height + 10f);
        _scrollY = Math.Clamp(_scrollY, 0f, max);
    }

    // ----------------------------------------------------------------- paint

    protected override void OnPaint(PaintEventArgs e)
    {
        EnsureWrap();
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using (var back = new SolidBrush(Colors.Surface))
        {
            g.FillRectangle(back, ClientRectangle);
        }

        Font mono = MonoFont;
        float lineHeight = LineHeight;
        int first = Math.Max(0, (int)(_scrollY / lineHeight) - 1);
        int last = Math.Min(_lines.Count - 1, first + (int)(ClientSize.Height / lineHeight) + 2);

        (int selStart, int selEnd) = Selection;

        // current line highlight
        if (Focused && !HasSelection)
        {
            int caretLine = LineOf(_caret);
            using var brush = new SolidBrush(Color.FromArgb(18, Colors.Accent));
            g.FillRectangle(brush, 0f, LineTop(caretLine), ClientSize.Width, lineHeight);
        }

        for (int i = first; i <= last; i++)
        {
            VisualLine line = _lines[i];
            float y = LineTop(i);

            if (ShowLineNumbers)
            {
                string number = (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
                Font gutterFont = _fonts.MonoFont(Type.MonoSize * 0.86f * _theme.Scale);
                float width = _fonts.Measure(number, gutterFont);
                using var gutterBrush = new SolidBrush(i == LineOf(_caret) ? Colors.TextMuted : Colors.TextFaint);
                g.DrawString(number, gutterFont, gutterBrush, GutterWidth - width - 10f * _theme.Scale * DpiScale, y + 2f);
            }

            // selection
            if (selStart != selEnd)
            {
                int lineFrom = Math.Max(selStart, line.Start);
                int lineTo = Math.Min(selEnd, line.Start + line.Length);
                if (lineFrom < lineTo)
                {
                    float x1 = XInLine(i, lineFrom);
                    float x2 = XInLine(i, lineTo);
                    using var brush = new SolidBrush(Colors.Selection);
                    g.FillRectangle(brush, x1, y, MathF.Max(2f, x2 - x1), lineHeight);
                }
                else if (line.Length == 0 && selStart <= line.Start && selEnd > line.Start)
                {
                    using var brush = new SolidBrush(Colors.Selection);
                    g.FillRectangle(brush, TextLeft, y, 6f, lineHeight);
                }
            }

            if (line.Length == 0)
            {
                continue;
            }

            string content = _text.Substring(line.Start, line.Length);
            int indexInLine = 0;
            foreach (var run in BuildLineRuns(line, content, y, lineHeight))
            {
                g.DrawString(run.Text, mono, run.Brush, run.X, y + 1f);
                indexInLine += run.Text.Length;
            }

            _ = indexInLine;
            _ = content;
        }

        // search hits
        foreach (var hit in _hits)
        {
            int lineIndex = LineOf(hit.Start);
            if (lineIndex < first - 1 || lineIndex > last + 1)
            {
                continue;
            }

            float x1 = XInLine(lineIndex, hit.Start);
            float x2 = XInLine(lineIndex, hit.Start + hit.Length);
            using var brush = new SolidBrush(Color.FromArgb(90, Color.Goldenrod));
            g.FillRectangle(brush, x1, LineTop(lineIndex), MathF.Max(2f, x2 - x1), lineHeight);
        }

        // caret
        if (Focused && _caretVisible)
        {
            int caretLine = LineOf(_caret);
            float x = XInLine(caretLine, _caret);
            using var pen = new Pen(Colors.Accent, MathF.Max(1.4f, _theme.Scale * 1.4f));
            g.DrawLine(pen, x, LineTop(caretLine) + 1f, x, LineTop(caretLine) + lineHeight - 1f);
        }

        PaintScrollbar(g);
    }

    private readonly List<(string Text, float X, Brush Brush)> _runPool = [];

    private List<(string Text, float X, Brush Brush)> BuildLineRuns(VisualLine line, string content, float y, float lineHeight)
    {
        _runPool.Clear();
        Font mono = MonoFont;
        float x = TextLeft;
        int i = 0;

        while (i < content.Length)
        {
            TokenKind kind = TokenAt(line.Start + i);
            int start = i;
            while (i < content.Length && TokenAt(line.Start + i) == kind)
            {
                i++;
            }

            string chunk = content[start..i];
            _runPool.Add((chunk, x, GetBrush(kind)));
            x += _fonts.Measure(chunk, mono);
        }

        _ = y;
        _ = lineHeight;
        return _runPool;
    }

    private readonly Dictionary<TokenKind, SolidBrush> _brushes = [];

    private Brush GetBrush(TokenKind kind)
    {
        if (_brushes.TryGetValue(kind, out SolidBrush? cached) && cached.Color.ToArgb() == ColorFor(kind).ToArgb())
        {
            return cached;
        }

        if (cached is not null)
        {
            cached.Dispose();
        }

        var brush = new SolidBrush(ColorFor(kind));
        _brushes[kind] = brush;
        return brush;
    }

    private Color ColorFor(TokenKind kind) => kind switch
    {
        TokenKind.Keyword => Colors.Keyword,
        TokenKind.String => Colors.String,
        TokenKind.Number => Colors.Number,
        TokenKind.Comment => Colors.Comment,
        TokenKind.Type => Colors.TypeName,
        TokenKind.Function => Colors.Function,
        TokenKind.Punctuation => Colors.Punctuation,
        TokenKind.Property => Colors.TypeName,
        TokenKind.Tag => Colors.Accent,
        TokenKind.Attribute => Colors.Function,
        _ => Colors.Text,
    };

    private TokenKind TokenAt(int index)
    {
        if (_tokens.Count == 0)
        {
            return TokenKind.Plain;
        }

        int lo = 0;
        int hi = _tokens.Count - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            Token token = _tokens[mid];
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

    private void PaintScrollbar(Graphics g)
    {
        float content = _lines.Count * LineHeight;
        if (content <= ClientSize.Height)
        {
            return;
        }

        const float width = 7f;
        float x = ClientSize.Width - width - 2f;
        float trackHeight = ClientSize.Height - 8f;
        float thumbHeight = MathF.Max(36f, trackHeight * (ClientSize.Height / content));
        float progress = _scrollY / MathF.Max(1f, content - ClientSize.Height);
        float top = 4f + (trackHeight - thumbHeight) * Math.Clamp(progress, 0f, 1f);

        using var brush = new SolidBrush(Color.FromArgb(150, Colors.ScrollThumb));
        using GraphicsPath path = MdViewer.Render.Rendering.PaintContext.RoundedPath(new RectangleF(x, top, width, thumbHeight), width / 2f);
        g.FillPath(brush, path);
    }

    private void InvalidateCaret()
    {
        int line = LineOf(_caret);
        Invalidate(new Rectangle(0, (int)LineTop(line) - 2, ClientSize.Width, (int)LineHeight + 4));
    }

    // ------------------------------------------------------------------ input

    private (int Start, int End) Selection => _caret < _anchor ? (_caret, _anchor) : (_anchor, _caret);

    private void SetCaret(int offset, bool select)
    {
        _caret = Math.Clamp(offset, 0, _text.Length);
        if (!select)
        {
            _anchor = _caret;
        }

        Dirty = true;
        EnsureCaretVisible();
        _caretVisible = true;
        _blink.Stop();
        _blink.Start();
        Invalidate();
        CaretMoved?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        if (IsReadOnly || e.KeyChar < ' ' && e.KeyChar != '\t')
        {
            base.OnKeyPress(e);
            return;
        }

        if (e.KeyChar == '\t')
        {
            IndentSelection(indent: !ModifierKeys.HasFlag(Keys.Shift));
            e.Handled = true;
            return;
        }

        InsertText(e.KeyChar.ToString());
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        bool ctrl = e.Control;
        bool shift = e.Shift;

        switch (e.KeyCode)
        {
            case Keys.Left:
                MoveCaret(ctrl ? PreviousWord(_caret) : _caret - 1, shift);
                e.Handled = true;
                return;
            case Keys.Right:
                MoveCaret(ctrl ? NextWord(_caret) : _caret + 1, shift);
                e.Handled = true;
                return;
            case Keys.Up:
                MoveCaret(LineUp(_caret), shift);
                e.Handled = true;
                return;
            case Keys.Down:
                MoveCaret(LineDown(_caret), shift);
                e.Handled = true;
                return;
            case Keys.Home:
                MoveCaret(ctrl ? 0 : LineStart(_caret), shift);
                e.Handled = true;
                return;
            case Keys.End:
                MoveCaret(ctrl ? _text.Length : LineEnd(_caret), shift);
                e.Handled = true;
                return;
            case Keys.PageUp:
                MoveCaret(Math.Max(0, _caret - (int)(ClientSize.Height / LineHeight) * 40), shift);
                e.Handled = true;
                return;
            case Keys.PageDown:
                MoveCaret(Math.Min(_text.Length, _caret + (int)(ClientSize.Height / LineHeight) * 40), shift);
                e.Handled = true;
                return;
        }

        if (ctrl)
        {
            switch (e.KeyCode)
            {
                case Keys.A:
                    _anchor = 0;
                    _caret = _text.Length;
                    Invalidate();
                    e.Handled = true;
                    return;
                case Keys.C:
                    CopySelection();
                    e.Handled = true;
                    return;
                case Keys.X when !IsReadOnly:
                    CopySelection();
                    DeleteSelection();
                    e.Handled = true;
                    return;
                case Keys.V when !IsReadOnly:
                    Paste();
                    e.Handled = true;
                    return;
                case Keys.Z when !IsReadOnly:
                    Undo(shift);
                    e.Handled = true;
                    return;
                case Keys.Y when !IsReadOnly:
                    Redo();
                    e.Handled = true;
                    return;
                case Keys.B when !IsReadOnly:
                    WrapSelection("**");
                    e.Handled = true;
                    return;
                case Keys.I when !IsReadOnly:
                    WrapSelection("*");
                    e.Handled = true;
                    return;
                case Keys.K when !IsReadOnly:
                    WrapSelection("[", "](https://)");
                    e.Handled = true;
                    return;
            }
        }

        if (!IsReadOnly && !ctrl)
        {
            switch (e.KeyCode)
            {
                case Keys.Back:
                    Backspace();
                    e.Handled = true;
                    return;
                case Keys.Delete:
                    DeleteForward();
                    e.Handled = true;
                    return;
                case Keys.Enter:
                    InsertNewline();
                    e.Handled = true;
                    return;
                case Keys.Tab:
                    IndentSelection(indent: !shift);
                    e.Handled = true;
                    return;
            }
        }

        base.OnKeyDown(e);
    }

    private void MoveCaret(int offset, bool select)
    {
        SetCaret(offset, select);
    }

    private int PreviousWord(int from)
    {
        int i = Math.Max(0, from - 1);
        while (i > 0 && char.IsWhiteSpace(_text[i]))
        {
            i--;
        }

        while (i > 0 && !char.IsWhiteSpace(_text[i - 1]))
        {
            i--;
        }

        return i;
    }

    private int NextWord(int from)
    {
        int i = Math.Min(_text.Length, from);
        while (i < _text.Length && !char.IsWhiteSpace(_text[i]))
        {
            i++;
        }

        while (i < _text.Length && char.IsWhiteSpace(_text[i]))
        {
            i++;
        }

        return i;
    }

    private int LineStart(int offset)
    {
        int index = _text.LastIndexOf('\n', Math.Max(0, offset - 1));
        return index < 0 ? 0 : index + 1;
    }

    private int LineEnd(int offset)
    {
        int index = _text.IndexOf('\n', Math.Min(offset, _text.Length));
        return index < 0 ? _text.Length : index;
    }

    private int LineUp(int offset)
    {
        int start = LineStart(offset);
        if (start == 0)
        {
            return 0;
        }

        int previousStart = LineStart(start - 1);
        int column = offset - start;
        return Math.Min(previousStart + column, start - 1);
    }

    private int LineDown(int offset)
    {
        int end = LineEnd(offset);
        if (end >= _text.Length)
        {
            return _text.Length;
        }

        int nextStart = end + 1;
        int nextEnd = LineEnd(nextStart);
        int column = offset - LineStart(offset);
        return Math.Min(nextStart + column, nextEnd);
    }

    // --------------------------------------------------------------- editing

    private void RecordUndo(bool typing)
    {
        DateTime now = DateTime.UtcNow;
        if (typing && _undo.Count > 0 && _undo[^1].Typing && (now - _undo[^1].Stamp).TotalMilliseconds < 900)
        {
            _undo[^1].Stamp = now;
            return;
        }

        _undo.Add(new UndoEntry { Text = _text, Caret = _caret, Anchor = _anchor, Stamp = now, Typing = typing });
        if (_undo.Count > 400)
        {
            _undo.RemoveAt(0);
        }

        _redo.Clear();
    }

    public void InsertText(string value)
    {
        if (IsReadOnly)
        {
            return;
        }

        RecordUndo(typing: true);
        DeleteSelectionCore();

        _text = _text.Insert(_caret, value);
        _caret += value.Length;
        _anchor = _caret;
        Dirty = true;
        InvalidateWrap();
        EnsureCaretVisible();
        Invalidate();
        TextChanged?.Invoke(this, EventArgs.Empty);
        TextChangedByUser?.Invoke(this, EventArgs.Empty);
        CaretMoved?.Invoke(this, EventArgs.Empty);
    }

    private void DeleteSelectionCore()
    {
        (int start, int end) = Selection;
        if (start == end)
        {
            return;
        }

        _text = _text.Remove(start, end - start);
        _caret = start;
        _anchor = start;
        InvalidateWrap();
    }

    private void DeleteSelection()
    {
        if (!HasSelection)
        {
            return;
        }

        RecordUndo(typing: false);
        DeleteSelectionCore();
        Dirty = true;
        Invalidate();
        TextChanged?.Invoke(this, EventArgs.Empty);
        TextChangedByUser?.Invoke(this, EventArgs.Empty);
    }

    private void Backspace()
    {
        if (!HasSelection)
        {
            if (_caret == 0)
            {
                return;
            }

            int start = _caret;
            int length = 1;
            while (start > 0 && char.IsLowSurrogate(_text[Math.Max(0, start - 1)]))
            {
                start--;
                length++;
            }

            RecordUndo(typing: false);
            _text = _text.Remove(start - 1, length);
            _caret = start - 1;
            _anchor = _caret;
        }
        else
        {
            RecordUndo(typing: false);
            DeleteSelectionCore();
        }

        Dirty = true;
        InvalidateWrap();
        EnsureCaretVisible();
        Invalidate();
        TextChanged?.Invoke(this, EventArgs.Empty);
        TextChangedByUser?.Invoke(this, EventArgs.Empty);
    }

    private void DeleteForward()
    {
        if (HasSelection)
        {
            DeleteSelection();
            return;
        }

        if (_caret >= _text.Length)
        {
            return;
        }

        RecordUndo(typing: false);
        _text = _text.Remove(_caret, 1);
        Dirty = true;
        InvalidateWrap();
        Invalidate();
        TextChanged?.Invoke(this, EventArgs.Empty);
        TextChangedByUser?.Invoke(this, EventArgs.Empty);
    }

    private void InsertNewline()
    {
        int lineStart = LineStart(_caret);
        string line = _text[lineStart.._caret];
        string indent = new(' ', line.Length - line.TrimStart().Length);

        string continuation = string.Empty;
        string trimmed = line.TrimStart();
        int markerEnd = ListMarkerLength(trimmed);
        if (markerEnd > 0)
        {
            string marker = trimmed[..markerEnd];
            if (marker.Length > 0 && marker[0] is '-' or '*' or '+' && trimmed.Length <= markerEnd + 1)
            {
                // empty list item: pressing Enter ends the list
                RecordUndo(typing: false);
                DeleteSelectionCore();
                _text = _text.Remove(lineStart, _caret - lineStart);
                _caret = lineStart;
                _anchor = _caret;
                Dirty = true;
                InvalidateWrap();
                Invalidate();
                TextChanged?.Invoke(this, EventArgs.Empty);
                TextChangedByUser?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (char.IsAsciiDigit(marker[0]))
            {
                int digits = 0;
                while (digits < marker.Length && char.IsAsciiDigit(marker[digits]))
                {
                    digits++;
                }

                if (int.TryParse(marker[..digits], out int value))
                {
                    marker = (value + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + marker[digits..];
                }
            }

            continuation = indent + marker;
        }
        else
        {
            continuation = indent;
        }

        InsertText("\n" + continuation);
    }

    private static int ListMarkerLength(string trimmed)
    {
        if (trimmed.Length >= 2 && (trimmed[0] is '-' or '*' or '+') && trimmed[1] == ' ')
        {
            return 2;
        }

        int digits = 0;
        while (digits < trimmed.Length && char.IsAsciiDigit(trimmed[digits]))
        {
            digits++;
        }

        if (digits > 0 && digits + 1 < trimmed.Length && trimmed[digits] is '.' or ')' && trimmed[digits + 1] == ' ')
        {
            return digits + 2;
        }

        return 0;
    }

    private void IndentSelection(bool indent)
    {
        (int start, int end) = Selection;
        int firstLine = LineStart(start);
        int lastLine = LineEnd(end);
        RecordUndo(typing: false);

        string block = _text[firstLine..lastLine];
        string[] lines = block.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (indent)
            {
                lines[i] = "    " + lines[i];
            }
            else if (lines[i].StartsWith("    ", StringComparison.Ordinal))
            {
                lines[i] = lines[i][4..];
            }
            else if (lines[i].StartsWith(' '))
            {
                lines[i] = lines[i][1..];
            }
        }

        string replacement = string.Join('\n', lines);
        _text = _text.Remove(firstLine, lastLine - firstLine).Insert(firstLine, replacement);
        int delta = replacement.Length - block.Length;
        _caret = Math.Clamp(_caret + (indent ? 4 : -Math.Min(4, delta == 0 ? 0 : 4)), 0, _text.Length);
        _anchor = Math.Clamp(_anchor, 0, _text.Length);
        Dirty = true;
        InvalidateWrap();
        Invalidate();
        TextChanged?.Invoke(this, EventArgs.Empty);
        TextChangedByUser?.Invoke(this, EventArgs.Empty);
    }

    private void WrapSelection(string prefix, string? suffix = null)
    {
        suffix ??= prefix;
        (int start, int end) = Selection;
        if (start == end)
        {
            InsertText(prefix + suffix);
            _caret = start + prefix.Length;
            _anchor = _caret;
            Invalidate();
            return;
        }

        RecordUndo(typing: false);
        string selected = _text[start..end];
        _text = _text.Remove(start, end - start).Insert(start, prefix + selected + suffix);
        _caret = start + prefix.Length + selected.Length;
        _anchor = start + prefix.Length;
        Dirty = true;
        InvalidateWrap();
        Invalidate();
        TextChanged?.Invoke(this, EventArgs.Empty);
        TextChangedByUser?.Invoke(this, EventArgs.Empty);
    }

    private void CopySelection()
    {
        string text = SelectedText;
        if (text.Length > 0)
        {
            Clipboard.SetText(text);
        }
    }

    private void Paste()
    {
        try
        {
            if (!Clipboard.ContainsText())
            {
                return;
            }

            InsertText(Clipboard.GetText().Replace("\r\n", "\n"));
        }
        catch
        {
            // clipboard unavailable
        }
    }

    private void Undo(bool redo)
    {
        if (redo)
        {
            Redo();
            return;
        }

        if (_undo.Count == 0)
        {
            return;
        }

        UndoEntry entry = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        _redo.Add(new UndoEntry { Text = _text, Caret = _caret, Anchor = _anchor, Stamp = DateTime.UtcNow });
        _text = entry.Text;
        _caret = Math.Min(entry.Caret, _text.Length);
        _anchor = Math.Min(entry.Anchor, _text.Length);
        Dirty = true;
        InvalidateWrap();
        EnsureCaretVisible();
        Invalidate();
        TextChanged?.Invoke(this, EventArgs.Empty);
        TextChangedByUser?.Invoke(this, EventArgs.Empty);
    }

    private void Redo()
    {
        if (_redo.Count == 0)
        {
            return;
        }

        UndoEntry entry = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        _undo.Add(new UndoEntry { Text = _text, Caret = _caret, Anchor = _anchor, Stamp = DateTime.UtcNow });
        _text = entry.Text;
        _caret = Math.Min(entry.Caret, _text.Length);
        _anchor = Math.Min(entry.Anchor, _text.Length);
        Dirty = true;
        InvalidateWrap();
        EnsureCaretVisible();
        Invalidate();
        TextChanged?.Invoke(this, EventArgs.Empty);
        TextChangedByUser?.Invoke(this, EventArgs.Empty);
    }

    // --------------------------------------------------------------- search

    public void SetSearch(string query, bool selectFirst = false)
    {
        _hits.Clear();
        _currentHit = -1;

        if (query.Length > 0)
        {
            int index = 0;
            while (index <= _text.Length - query.Length)
            {
                int found = _text.IndexOf(query, index, StringComparison.CurrentCultureIgnoreCase);
                if (found < 0)
                {
                    break;
                }

                _hits.Add((found, query.Length));
                index = found + Math.Max(1, query.Length);
            }
        }

        if (selectFirst && _hits.Count > 0)
        {
            SelectHit(0);
        }

        Invalidate();
    }

    public int HitCount => _hits.Count;

    public int CurrentHit => _currentHit;

    public bool SelectHit(int index)
    {
        if (_hits.Count == 0)
        {
            return false;
        }

        _currentHit = ((index % _hits.Count) + _hits.Count) % _hits.Count;
        (int start, int length) = _hits[_currentHit];
        _anchor = start;
        _caret = start + length;
        EnsureCaretVisible();
        Invalidate();
        CaretMoved?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void ClearSearch()
    {
        _hits.Clear();
        _currentHit = -1;
        Invalidate();
    }

    // ---------------------------------------------------------------- mouse

    private int OffsetAt(Point point)
    {
        EnsureWrap();
        int lineIndex = Math.Clamp((int)((point.Y + _scrollY) / LineHeight), 0, _lines.Count - 1);
        VisualLine line = _lines[lineIndex];
        if (line.Length == 0)
        {
            return line.Start;
        }

        float target = point.X - TextLeft;
        if (target <= 0)
        {
            return line.Start;
        }

        string content = _text.Substring(line.Start, line.Length);
        int best = line.Length;
        float previous = 0f;
        for (int i = 1; i <= content.Length; i++)
        {
            float width = _fonts.Measure(content[..i], MonoFont);
            if (target < (previous + width) / 2f)
            {
                best = i - 1;
                break;
            }

            previous = width;
        }

        return line.Start + best;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        int offset = OffsetAt(e.Location);

        _clickCount = (DateTime.UtcNow - _lastClick).TotalMilliseconds < 400 && Math.Abs(e.X - _lastClickPoint.X) < 4 && Math.Abs(e.Y - _lastClickPoint.Y) < 4
            ? _clickCount + 1
            : 1;
        _lastClick = DateTime.UtcNow;
        _lastClickPoint = e.Location;

        switch (_clickCount)
        {
            case 2:
            {
                int start = offset;
                while (start > 0 && !char.IsWhiteSpace(_text[start - 1]))
                {
                    start--;
                }

                int end = offset;
                while (end < _text.Length && !char.IsWhiteSpace(_text[end]))
                {
                    end++;
                }

                _anchor = start;
                _caret = end;
                break;
            }

            case 3:
                _anchor = LineStart(offset);
                _caret = LineEnd(offset);
                break;

            default:
                _caret = offset;
                _anchor = offset;
                _dragging = true;
                break;
        }

        EnsureCaretVisible();
        Invalidate();
        CaretMoved?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_dragging)
        {
            return;
        }

        _caret = OffsetAt(e.Location);
        EnsureCaretVisible();
        Invalidate();
        CaretMoved?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _dragging = false;
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (ModifierKeys.HasFlag(Keys.Control))
        {
            ZoomRequested?.Invoke(this, e.Delta > 0 ? 1 : -1);
            return;
        }

        _scrollY = Math.Clamp(_scrollY - e.Delta * 0.6f, 0f, MathF.Max(0f, _lines.Count * LineHeight - ClientSize.Height + 10f));
        Invalidate();
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        _blink.Start();
        _caretVisible = true;
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        _blink.Stop();
        Invalidate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        InvalidateWrap();
        Invalidate();
    }

    protected override bool IsInputKey(Keys keyData) => true;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _blink.Dispose();
            foreach (SolidBrush brush in _brushes.Values)
            {
                brush.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private sealed class VScroll
    {
        public float Max;
    }
}




