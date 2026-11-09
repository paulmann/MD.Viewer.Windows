using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Text;
using System.Windows.Forms;
using MdViewer.App.Controls;
using MdViewer.Core.Markdown;
using MdViewer.Render;
using MdViewer.Render.Layout;
using MdViewer.Render.Text;
using MdViewer.Render.Theming;

namespace MdViewer.App;

/// <summary>One open document and its per-view state.</summary>
public sealed class Session
{
    public string? Path { get; set; }

    public string Text { get; set; } = string.Empty;

    public bool Dirty { get; set; }

    public MarkdownDocument? Document { get; set; }

    public float ScrollY { get; set; }

    public int Caret { get; set; }

    public string? PendingAnchor { get; set; }

    public bool Missing { get; set; }

    public string Title => Path is null ? L.T("tabs.new") : System.IO.Path.GetFileName(Path);

    public string Subtitle => Path is null ? string.Empty : System.IO.Path.GetDirectoryName(Path) ?? string.Empty;
}

public sealed class MainForm : Form
{
    private readonly AppSettings _settings;
    private readonly List<Session> _sessions = [];
    private readonly MarkdownRenderer _renderer;
    private readonly System.Windows.Forms.Timer _previewTimer;
    private readonly System.Windows.Forms.Timer _toastTimer;

    private readonly Panel _toolbar;
    private readonly ToolButton _btnOutline;
    private readonly ToolButton _btnOpen;
    private readonly ToolButton _btnSave;
    private readonly ToolButton _btnSearch;
    private readonly ToolButton _btnZoomOut;
    private readonly ToolButton _btnZoomIn;
    private readonly ToolButton _btnTheme;
    private readonly ToolButton _btnMore;
    private readonly ModeSwitcher _modes;
    private readonly TabStrip _tabs;
    private readonly OutlinePanel _outline;
    private readonly DocumentView _view;
    private readonly MarkdownEditor _editor;
    private readonly FindBar _find;
    private readonly StatusBarView _status;

    private Session? _active;
    private Theme _theme;
    private ThemeMode _themeMode;
    private ViewMode _mode;
    private bool _outlineVisible;
    private bool _previewVisible;
    private string? _toast;
    private FileSystemWatcher? _watcher;
    private bool _suppressEditorEvents;
    private bool _uiReady;

    // Runtime mirror of the persisted preferences: command-line switches change these
    // for the session only, while user gestures write through to _settings.
    private bool _serif;
    private bool _lineNumbers;
    private bool _wrapCode;
    private bool _numbering;

    public MainForm(AppSettings settings, LaunchOptions? launch = null)
    {
        LaunchOptions options = launch ?? new LaunchOptions();
        _settings = settings;

        _serif = options.Serif ?? settings.Serif;
        _lineNumbers = options.LineNumbers ?? settings.LineNumbers;
        _wrapCode = options.WrapCode ?? settings.WrapCode;
        _numbering = options.Numbering ?? settings.Numbering;

        _themeMode = options.Theme ?? ParseTheme(settings.Theme);
        float zoom = options.Zoom ?? settings.Zoom;
        _theme = ThemeCatalog.Resolve(_themeMode.ToString().ToLowerInvariant()).WithScale(zoom).WithSerif(_serif);
        // Files open in the rendered view unless the user asked to remember the last mode
        // (or passed --mode). The saved mode is still kept for the case it is enabled.
        _mode = options.Mode ?? (settings.RememberMode ? ParseMode(settings.Mode) : ViewMode.Rendered);
        _outlineVisible = options.ToggleOutline ?? settings.Outline;
        _previewVisible = settings.EditorPreview;

        _renderer = new MarkdownRenderer(_theme, DeviceDpi / 96f)
        {
            Settings = new RenderSettings
            {
                NumberHeadings = _numbering,
                CodeLineNumbers = _lineNumbers,
                WrapCode = _wrapCode,
                UnderlineLinks = false,
            },
        };

        Text = L.T("app.title");
        MinimumSize = new Size(640, 420);
        StartPosition = FormStartPosition.Manual;
        Icon = AppIcon.Create();
        KeyPreview = true;
        AllowDrop = true;

        _toolbar = new Panel { Dock = DockStyle.None, Height = 52 };
        _toolbar.Paint += PaintToolbar;

        _btnOutline = NewButton(IconKind.Outline, L.T("menu.outline"), () => ToggleOutline());
        _btnOpen = NewButton(IconKind.Folder, L.T("menu.open"), OpenFileDialog);
        _btnSave = NewButton(IconKind.Save, L.T("menu.save"), () => SaveActive(false));
        _btnSearch = NewButton(IconKind.Search, L.T("find.placeholder"), () => ShowFind());
        _btnZoomOut = NewButton(IconKind.Minus, L.T("menu.zoomOut"), () => SetZoom(_theme.Scale - 0.1f));
        _btnZoomIn = NewButton(IconKind.Plus, L.T("menu.zoomIn"), () => SetZoom(_theme.Scale + 0.1f));
        _btnTheme = NewButton(IconKind.Auto, L.T("menu.theme.tip"), ToggleTheme);
        _btnMore = NewButton(IconKind.More, L.T("menu.file"), ShowMenu);

        _modes = new ModeSwitcher
        {
            Items = [L.T("mode.render"), L.T("mode.source"), L.T("mode.editor")],
            SelectedIndex = (int)_mode,
        };
        _modes.SelectedIndexChanged += (_, _) => SetMode((ViewMode)_modes.SelectedIndex);
        _toolbar.Controls.Add(_modes);

        _tabs = new TabStrip { Visible = false };
        _tabs.TabSelected += (_, index) => ActivateSession(index);
        _tabs.TabClosed += (_, index) => CloseSession(index);

        _outline = new OutlinePanel { Visible = _outlineVisible };
        _outline.AnchorActivated += (_, id) => NavigateToAnchor(id);

        _view = new DocumentView { Renderer = _renderer, Dock = DockStyle.None };
        _view.ReadingWidthOverride = options.ReadingWidth ?? settings.ReadingWidth;
        _view.LinkActivated += (_, url) => HandleLink(url);
        _view.ScrollChanged += (_, _) => OnViewScrolled();
        _view.ZoomRequested += (_, direction) => SetZoom(_theme.Scale + direction * 0.1f, fromWheel: true);
        _view.Toast += (_, message) => Toast(message);

        _editor = new MarkdownEditor { Dock = DockStyle.None, ShowLineNumbers = _lineNumbers };
        _editor.CaretMoved += (_, _) => UpdateStatus();
        _editor.TextChangedByUser += (_, _) => OnEditorChanged();
        _editor.ZoomRequested += (_, direction) => SetZoom(_theme.Scale + direction * 0.1f, fromWheel: true);

        _find = new FindBar { Visible = false };
        _find.QueryChanged += (_, query) => RunSearch(query);
        _find.Navigate += (_, direction) => NavigateSearch(direction);
        _find.CloseRequested += (_, _) => HideFind();

        _status = new StatusBarView { Dock = DockStyle.None };

        _previewTimer = new System.Windows.Forms.Timer { Interval = 220 };
        _previewTimer.Tick += (_, _) =>
        {
            _previewTimer.Stop();
            RefreshPreview();
        };

        _toastTimer = new System.Windows.Forms.Timer { Interval = 2200 };
        _toastTimer.Tick += (_, _) =>
        {
            _toastTimer.Stop();
            _toast = null;
            UpdateStatus();
        };

        Controls.AddRange([_toolbar, _tabs, _outline, _view, _editor, _find, _status]);

        _uiReady = true;
        ApplyTheme();
        RestoreWindow();

        DragEnter += OnDragEnter;
        DragDrop += OnDragDrop;
        FormClosing += OnFormClosing;
    }

    public int SessionCount => _sessions.Count;

    // ------------------------------------------------------------------- UI

    private ToolButton NewButton(IconKind icon, string tooltip, Action action)
    {
        var button = new ToolButton { Icon = icon, ToolTipText = tooltip, Size = new Size(34, 34) };
        button.Click += (_, _) => action();
        var tip = new ToolTip();
        tip.SetToolTip(button, tooltip);
        _toolbar.Controls.Add(button);
        return button;
    }

    private void PaintToolbar(object? sender, PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        Palette colors = _theme.Colors;
        using (var brush = new SolidBrush(colors.Surface))
        {
            g.FillRectangle(brush, _toolbar.ClientRectangle);
        }

        using (var pen = new Pen(colors.Border, 1f))
        {
            g.DrawLine(pen, 0, _toolbar.Height - 1, _toolbar.Width, _toolbar.Height - 1);
        }

        if (_active is null)
        {
            return;
        }

        Font nameFont = _renderer.Fonts.UiFont(13f * _theme.Scale, FontStyle.Bold);
        Font pathFont = _renderer.Fonts.UiFont(10.8f * _theme.Scale);
        float x = 122f * (_theme.Scale * DeviceDpi / 96f);
        float maxWidth = Math.Max(80f, _modes.Left - x - 16f);
        string title = _active.Title + (_active.Dirty ? " •" : string.Empty);
        g.DrawString(title, nameFont, new SolidBrush(colors.Text), x, _toolbar.Height / 2f - 17f);
        string subtitle = _active.Subtitle;
        if (subtitle.Length > 0)
        {
            g.DrawString(subtitle, pathFont, new SolidBrush(colors.TextFaint), x, _toolbar.Height / 2f + 3f);
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        LayoutChrome();
    }

    private void LayoutChrome()
    {
        if (!_uiReady || WindowState == FormWindowState.Minimized)
        {
            return;
        }

        float dpi = DeviceDpi / 96f;
        int toolbarHeight = (int)(52 * dpi);
        int tabsHeight = _tabs.Visible ? (int)(36 * dpi) : 0;
        int statusHeight = (int)(28 * dpi);
        int outlineWidth = _outlineVisible ? (int)(262 * dpi) : 0;

        _toolbar.SetBounds(0, 0, ClientSize.Width, toolbarHeight);
        _tabs.SetBounds(0, toolbarHeight, ClientSize.Width, tabsHeight);
        _status.SetBounds(0, ClientSize.Height - statusHeight, ClientSize.Width, statusHeight);

        int bodyTop = toolbarHeight + tabsHeight;
        int bodyHeight = Math.Max(40, ClientSize.Height - bodyTop - statusHeight);

        _outline.SetBounds(0, bodyTop, outlineWidth, bodyHeight);

        int contentLeft = outlineWidth;
        int contentWidth = Math.Max(120, ClientSize.Width - outlineWidth);

        bool split = _mode == ViewMode.Editor && _previewVisible;
        if (split)
        {
            int half = contentWidth / 2;
            _editor.SetBounds(contentLeft, bodyTop, half - 1, bodyHeight);
            _view.SetBounds(contentLeft + half, bodyTop, contentWidth - half, bodyHeight);
            _view.Visible = true;
            _editor.Visible = true;
        }
        else
        {
            _view.SetBounds(contentLeft, bodyTop, contentWidth, bodyHeight);
            _editor.SetBounds(contentLeft, bodyTop, contentWidth, bodyHeight);
            _view.Visible = _mode == ViewMode.Rendered;
            _editor.Visible = _mode != ViewMode.Rendered;
        }

        int findWidth = (int)(420 * dpi);
        _find.SetBounds(contentLeft + contentWidth - findWidth - (int)(24 * dpi), bodyTop + (int)(16 * dpi), findWidth, (int)(40 * dpi));
        _find.BringToFront();

        LayoutToolbar(dpi);
    }

    private void LayoutToolbar(float dpi)
    {
        int y = (int)(9 * dpi);
        int x = (int)(10 * dpi);
        int gap = (int)(2 * dpi);

        void Place(ToolButton button)
        {
            button.Size = new Size((int)(34 * dpi), (int)(34 * dpi));
            button.Location = new Point(x, y);
            x += button.Width + gap;
        }

        Place(_btnOutline);
        Place(_btnOpen);
        Place(_btnSave);

        Size modesSize = _modes.GetPreferredSize(Size.Empty);
        _modes.Bounds = new Rectangle(0, 0, modesSize.Width + (int)(6 * dpi), (int)(34 * dpi));

        int right = ClientSize.Width - (int)(10 * dpi);
        void PlaceRight(ToolButton button)
        {
            button.Size = new Size((int)(34 * dpi), (int)(34 * dpi));
            right -= button.Width;
            button.Location = new Point(right, y);
            right -= gap;
        }

        PlaceRight(_btnMore);
        PlaceRight(_btnTheme);
        PlaceRight(_btnZoomIn);
        PlaceRight(_btnZoomOut);
        PlaceRight(_btnSearch);

        int centerX = (_toolbar.Width - _modes.Width) / 2;
        _modes.Location = new Point(Math.Max(x + 8, centerX), (int)(9 * dpi));
    }

    // -------------------------------------------------------------- sessions

    public void OpenFile(string path, ViewMode? mode = null, string? anchor = null, int? line = null)
    {
        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, string.Format(L.T("error.open"), path, ex.Message), L.T("app.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Session? existing = _sessions.FirstOrDefault(s => string.Equals(s.Path, fullPath, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            ActivateSession(_sessions.IndexOf(existing));
            if (anchor is not null)
            {
                NavigateToAnchor(anchor);
            }

            if (line is int lineNumber)
            {
                GoToLine(lineNumber);
            }

            return;
        }

        var session = new Session { Path = fullPath };
        try
        {
            session.Text = File.ReadAllText(fullPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, string.Format(L.T("error.open"), fullPath, ex.Message), L.T("app.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        session.Document = Parse(session.Text, fullPath);
        session.PendingAnchor = anchor;
        _sessions.Add(session);
        _settings.AddRecent(fullPath);
        ActivateSession(_sessions.Count - 1);

        if (mode is ViewMode requested)
        {
            SetMode(requested);
        }

        if (line is int target)
        {
            GoToLine(target);
        }
    }

    public void OpenUntitled(string? initialText = null, string? suggestedName = null)
    {
        var session = new Session { Path = null, Text = initialText ?? string.Empty };
        session.Document = Parse(session.Text, null);
        _sessions.Add(session);
        ActivateSession(_sessions.Count - 1);
    }

    private MarkdownOptions ParseOptions() => new()
    {
        DoubleLineBreaks = _settings.LineBreaks,
        SmartPunctuation = _settings.SmartPunctuation,
        HeadingNumbering = _numbering,
    };

    private MarkdownDocument Parse(string text, string? path) =>
        new MarkdownParser().Parse(text, ParseOptions(), path);

    private void ActivateSession(int index)
    {
        if (index < 0 || index >= _sessions.Count)
        {
            return;
        }

        if (_active is not null)
        {
            _active.ScrollY = _view.ScrollY;
            if (_mode != ViewMode.Rendered)
            {
                _active.Caret = _editor.CaretIndex;
            }
        }

        _active = _sessions[index];
        _tabs.SelectedIndex = index;
        _tabs.Invalidate();

        _renderer.SetDocument(_active.Document);
        _view.RebindLayout();
        _view.ScrollTo(_active.ScrollY, animate: false);

        _suppressEditorEvents = true;
        _editor.Text = _active.Text;
        _editor.Dirty = _active.Dirty;
        _editor.IsReadOnly = _mode == ViewMode.Source;
        _editor.GoToOffset(_active.Caret);
        _suppressEditorEvents = false;

        _outline.SetDocument(_active.Document);
        _outline.SetHeader(_active.Title, Stats(_active));
        SyncGlossary();

        UpdateTitle();
        UpdateTabs();
        UpdateModeVisibility();
        UpdateStatus();

        if (_active.PendingAnchor is { Length: > 0 } anchor)
        {
            _active.PendingAnchor = null;
            NavigateToAnchor(anchor);
        }

        WatchFile();
        HideFind();
    }

    private void SyncGlossary() => _view.SetGlossary(_active?.Document?.Glossary ?? new Dictionary<string, string>());

    private string Stats(Session session)
    {
        MarkdownDocument? document = session.Document;
        if (document is null)
        {
            return string.Empty;
        }

        return string.Format(System.Globalization.CultureInfo.CurrentCulture, L.T("status.words"), document.WordCount)
            + " · "
            + string.Format(System.Globalization.CultureInfo.CurrentCulture, L.T("status.reading"), Math.Max(1, (int)Math.Round(document.ReadingTime.TotalMinutes)));
    }

    private void UpdateTabs()
    {
        _tabs.Tabs.Clear();
        foreach (Session session in _sessions)
        {
            _tabs.Tabs.Add(new TabItem { Title = session.Title, Dirty = session.Dirty, Path = session.Path });
        }

        bool show = _sessions.Count > 1;
        if (_tabs.Visible != show)
        {
            _tabs.Visible = show;
            LayoutChrome();
        }

        _tabs.Invalidate();
    }

    private void UpdateTitle()
    {
        if (_active is null)
        {
            Text = L.T("app.title");
            return;
        }

        Text = _active.Title + (_active.Dirty ? " •" : string.Empty) + " — " + L.T("app.title");
    }

    private void UpdateModeVisibility()
    {
        bool rendered = _mode == ViewMode.Rendered;
        bool split = _mode == ViewMode.Editor && _previewVisible;

        _view.Visible = rendered || split;
        _editor.Visible = !rendered;
        _editor.IsReadOnly = _mode == ViewMode.Source;
        _modes.SelectedIndex = (int)_mode;
        _btnOutline.Active = _outlineVisible;
        LayoutChrome();

        if (rendered)
        {
            _view.Focus();
            _view.RebindLayout();
        }
        else
        {
            _editor.Focus();
        }
    }

    // --------------------------------------------------------------- editing

    private void OnEditorChanged()
    {
        if (_suppressEditorEvents || _active is null)
        {
            return;
        }

        _active.Text = _editor.Text;
        if (!_active.Dirty)
        {
            _active.Dirty = true;
            UpdateTitle();
            UpdateTabs();
        }

        UpdateStatus();

        if (_mode == ViewMode.Editor && _previewVisible)
        {
            _previewTimer.Stop();
            _previewTimer.Start();
        }
    }

    private void RefreshPreview()
    {
        if (_active is null)
        {
            return;
        }

        float scroll = _view.ScrollY;
        _active.Document = Parse(_active.Text, _active.Path);
        _renderer.SetDocument(_active.Document);
        _view.RebindLayout();
        _view.ScrollTo(scroll, animate: false);
        _outline.SetDocument(_active.Document);
        _outline.SetHeader(_active.Title, Stats(_active));
        SyncGlossary();
    }

    private bool SaveActive(bool saveAs)
    {
        if (_active is null)
        {
            return false;
        }

        string? path = _active.Path;
        if (saveAs || path is null)
        {
            using var dialog = new SaveFileDialog
            {
                Title = L.T("dialog.saveAs"),
                Filter = "Markdown (*.md)|*.md|" + L.T("dialog.allFiles") + " (*.*)|*.*",
                FileName = path is null ? "untitled.md" : Path.GetFileName(path),
                DefaultExt = "md",
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return false;
            }

            path = dialog.FileName;
        }

        try
        {
            File.WriteAllText(path, _editor.Text);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, string.Format(L.T("error.save"), path, ex.Message), L.T("app.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        _active.Path = path;
        _active.Text = _editor.Text;
        _active.Dirty = false;
        _active.Document = Parse(_active.Text, path);
        _renderer.SetDocument(_active.Document);
        _view.RebindLayout();
        _settings.AddRecent(path);
        _editor.Dirty = false;
        UpdateTitle();
        UpdateTabs();
        UpdateStatus();
        _outline.SetDocument(_active.Document);
        _outline.SetHeader(_active.Title, Stats(_active));
        WatchFile();
        Toast(string.Format(System.Globalization.CultureInfo.CurrentCulture, L.T("export.done"), Path.GetFileName(path)));
        return true;
    }

    private void CloseSession(int index)
    {
        if (index < 0 || index >= _sessions.Count)
        {
            return;
        }

        Session session = _sessions[index];
        if (session.Dirty)
        {
            DialogResult result = MessageBox.Show(
                this,
                string.Format(L.T("dialog.unsaved"), session.Title),
                L.T("app.title"),
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (result == DialogResult.Cancel)
            {
                return;
            }

            if (result == DialogResult.Yes && !SaveActive(false))
            {
                return;
            }
        }

        _sessions.RemoveAt(index);

        if (_sessions.Count == 0)
        {
            _active = null;
            _renderer.SetDocument(null);
            _view.RebindLayout();
            _outline.SetDocument(null);
            _editor.Text = string.Empty;
            UpdateTitle();
            UpdateTabs();
            UpdateStatus();
            return;
        }

        ActivateSession(Math.Min(index, _sessions.Count - 1));
        UpdateTabs();
    }

    // ----------------------------------------------------------------- modes

    public void SetMode(ViewMode mode, bool persist = true)
    {
        if (_mode == mode)
        {
            return;
        }

        if (_active is not null)
        {
            _active.ScrollY = _view.ScrollY;
            _active.Caret = _editor.CaretIndex;
        }

        _mode = mode;
        if (persist)
        {
            _settings.Mode = mode.ToString().ToLowerInvariant();
        }

        if (mode == ViewMode.Rendered && _active is not null)
        {
            _active.Document = Parse(_active.Text, _active.Path);
            _renderer.SetDocument(_active.Document);
            _view.RebindLayout();
            _outline.SetDocument(_active.Document);
            SyncGlossary();
        }

        _suppressEditorEvents = true;
        _editor.Text = _active?.Text ?? string.Empty;
        _suppressEditorEvents = false;

        UpdateModeVisibility();
        UpdateStatus();
        HideFind();
    }

    private void SetZoom(float zoom, bool fromWheel = false, bool persist = true)
    {
        float clamped = Math.Clamp(MathF.Round(zoom * 20f) / 20f, 0.6f, 2.6f);
        if (MathF.Abs(clamped - _theme.Scale) < 0.001f)
        {
            return;
        }

        float anchorRatio = 0.5f;
        if (_view.Visible && _view.ContentHeight > 0)
        {
            anchorRatio = _view.ScrollY / MathF.Max(1f, _view.ContentHeight);
        }

        _theme = _theme.WithScale(clamped);
        if (persist)
        {
            _settings.Zoom = clamped;
        }

        _renderer.SetTheme(_theme);
        ApplyTheme();

        if (_view.Visible)
        {
            _view.RebindLayout();
            _view.ScrollTo(anchorRatio * _view.ContentHeight, animate: false);
        }

        UpdateStatus();
        _ = fromWheel;
    }

    /// <summary>
    /// Flips the actual appearance instead of walking Auto → Light → Dark. With Windows in dark
    /// mode "Auto" looks exactly like "Dark", so a three-step cycle made the first click look dead.
    /// "Follow Windows" stays one click away in the View menu.
    /// </summary>
    public void ToggleTheme()
    {
        _themeMode = _theme.IsDark ? ThemeMode.Light : ThemeMode.Dark;
        ApplyTheme(persist: true);
    }

    /// <param name="persist">True for a user gesture; false for start-up, launch switches and
    /// following the Windows theme — those must not rewrite the saved preference.</param>
    private void ApplyTheme(bool persist = false)
    {
        _theme = ThemeCatalog.Resolve(_themeMode.ToString().ToLowerInvariant()).WithScale(_theme.Scale).WithSerif(_serif);
        if (persist)
        {
            _settings.Theme = _themeMode.ToString().ToLowerInvariant();
        }
        _renderer.SetTheme(_theme);

        // The icon shows what you see, not which of the three modes is stored.
        _btnTheme.Icon = _theme.IsDark ? IconKind.Moon : IconKind.Sun;
        _btnTheme.Active = _themeMode == ThemeMode.Auto;

        BackColor = _theme.Colors.Canvas;
        _toolbar.BackColor = _theme.Colors.Surface;
        _toolbar.Invalidate();
        _status.ApplyTheme(_theme, _renderer.Fonts);
        _tabs.ApplyTheme(_theme, _renderer.Fonts);
        _outline.ApplyTheme(_theme, _renderer.Fonts);
        _find.ApplyTheme(_theme, _renderer.Fonts);
        _modes.ApplyTheme(_theme, _renderer.Fonts);

        foreach (ToolButton button in new[] { _btnOutline, _btnOpen, _btnSave, _btnSearch, _btnZoomIn, _btnZoomOut, _btnTheme, _btnMore })
        {
            button.ApplyTheme(_theme, _renderer.Fonts);
        }

        _view.RefreshTheme();
        _view.RebindLayout();

        _editor.ApplyTheme(_theme, _renderer.Fonts);
        _suppressEditorEvents = true;
        _editor.Text = _active?.Text ?? string.Empty;
        _suppressEditorEvents = false;
        _editor.GoToOffset(_active?.Caret ?? 0);

        WindowChrome.Apply(Handle, _theme.IsDark, _theme.Colors.Surface, _theme.Colors.Text);
        UpdateStatus();
    }

    private void ToggleOutline()
    {
        _outlineVisible = !_outlineVisible;
        _settings.Outline = _outlineVisible;
        _outline.Visible = _outlineVisible;
        _btnOutline.Active = _outlineVisible;
        LayoutChrome();
        _view.RebindLayout();
    }

    private void TogglePreview()
    {
        _previewVisible = !_previewVisible;
        _settings.EditorPreview = _previewVisible;

        if (_mode != ViewMode.Editor)
        {
            SetMode(ViewMode.Editor);
        }
        else
        {
            UpdateModeVisibility();
        }
    }

    // -------------------------------------------------------------- navigation

    private void NavigateToAnchor(string id)
    {
        if (id.Length == 0)
        {
            return;
        }

        if (_mode == ViewMode.Rendered || _previewVisible)
        {
            _view.ScrollToAnchor(id);
        }
        else
        {
            GoToLine(1);
        }
    }

    private void GoToLine(int line)
    {
        if (_active is null)
        {
            return;
        }

        if (_mode == ViewMode.Rendered && _active.Document is { } document)
        {
            HeadingInfo? heading = document.Outline.FirstOrDefault(h => h.Line >= line);
            if (heading is not null)
            {
                _view.ScrollToAnchor(heading.Id);
                return;
            }
        }

        if (_mode != ViewMode.Rendered)
        {
            _editor.ScrollToLine(line);
            _editor.Focus();
        }
    }

    private void HandleLink(string url)
    {
        if (url.StartsWith('#'))
        {
            string id = url[1..];
            _view.ScrollToAnchor(id);
            return;
        }

        if (url.StartsWith("file:", StringComparison.Ordinal))
        {
            string target = url[5..];
            if (File.Exists(target))
            {
                OpenExternal(target);
            }
            else
            {
                Toast(string.Format(L.T("toast.missing"), Path.GetFileName(target)));
            }

            return;
        }

        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            OpenExternal(url);
            return;
        }

        if (_active?.Path is null)
        {
            return;
        }

        string baseDirectory = Path.GetDirectoryName(_active.Path) ?? string.Empty;
        string candidate;
        try
        {
            candidate = Path.GetFullPath(url.Replace('/', Path.DirectorySeparatorChar), baseDirectory);
        }
        catch
        {
            return;
        }

        if (!File.Exists(candidate))
        {
            Toast(string.Format(L.T("toast.missing"), url));
            return;
        }

        string extension = Path.GetExtension(candidate).ToLowerInvariant();
        if (extension is ".md" or ".markdown" or ".txt")
        {
            OpenFile(candidate);
        }
        else
        {
            OpenExternal(candidate);
        }
    }

    private void OpenExternal(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Toast(ex.Message);
        }
    }

    // ------------------------------------------------------------------ find

    private void ShowFind()
    {
        _find.Visible = true;
        _find.BringToFront();
        string? selection = _editor.Visible ? _editor.SelectedText : null;
        _find.FocusInput(selection is { Length: > 0 } ? selection : null);
        LayoutChrome();
    }

    private void HideFind()
    {
        _find.Visible = false;
        _find.Clear();
        _view.ClearSearch();
        _editor.ClearSearch();
    }

    private void RunSearch(string query)
    {
        if (query.Length == 0)
        {
            _view.ClearSearch();
            _editor.ClearSearch();
            _find.SetCount(0, 0);
            return;
        }

        if (_view.Visible)
        {
            var hits = CollectRenderedHits(query);
            _view.SetSearchHits(hits, hits.Count > 0 ? 0 : -1);
            _find.SetCount(0, hits.Count);
            return;
        }

        _editor.SetSearch(query, selectFirst: true);
        _find.SetCount(_editor.CurrentHit, _editor.HitCount);
    }

    private void NavigateSearch(int direction)
    {
        if (_view.Visible)
        {
            return;
        }

        if (_editor.HitCount == 0)
        {
            return;
        }

        _editor.SelectHit(_editor.CurrentHit + direction);
        _find.SetCount(_editor.CurrentHit, _editor.HitCount);
    }

    private List<RectangleF> CollectRenderedHits(string query)
    {
        var hits = new List<RectangleF>();
        DocumentLayout? layout = _renderer.Layout;
        if (layout is null)
        {
            return hits;
        }

        foreach (BlockLayout block in layout.Blocks)
        {
            switch (block)
            {
                case TextVisual text:
                {
                    float y = text.Top;
                    foreach (ShapedLine line in text.Lines)
                    {
                        SearchSupport.Collect(line.Runs, text.Left, y, query, hits, _renderer.Fonts);
                        y += line.Height;
                    }

                    break;
                }

                case CodeVisual code:
                {
                    float y = code.Top + (code.HasHeader ? 30f * _theme.Scale * DeviceDpi / 96f : 0f) + 9f * _theme.Scale * DeviceDpi / 96f;
                    foreach (CodeLine line in code.Lines)
                    {
                        SearchSupport.Collect(line.Runs, code.Left, y, query, hits, _renderer.Fonts);
                        y += line.Height;
                    }

                    break;
                }
            }
        }

        return hits;
    }

    // ----------------------------------------------------------------- status

    private void OnViewScrolled()
    {
        if (_active is not null)
        {
            _active.ScrollY = _view.ScrollY;
        }

        string? anchor = _renderer.Layout?.AnchorAt(_view.ScrollY + 60f);
        _outline.SetActive(anchor);
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (_active is null)
        {
            _status.Set(L.T("empty.title"), string.Empty, string.Empty, $"{Math.Round(_theme.Scale * 100)}%");
            return;
        }

        string left = _toast ?? ((_active.Dirty ? L.T("status.modified") : L.T("status.saved")) + "  ·  " + _active.Title);
        string middle = Stats(_active);
        string right;
        if (_view.Visible)
        {
            int percent = (int)Math.Round(_view.ContentHeight <= 0 ? 0 : Math.Clamp(_view.ScrollY / MathF.Max(1f, _view.ContentHeight), 0f, 1f) * 100);
            right = $"{percent}%";
        }
        else
        {
            (int line, int column) = _editor.CaretPosition;
            right = string.Format(System.Globalization.CultureInfo.CurrentCulture, L.T("status.line"), line, column) + "  ·  " + L.T("status.utf8");
        }

        _status.Set(left, middle, right, $"{Math.Round(_theme.Scale * 100)}%");
        _status.Invalidate();
    }

    private void Toast(string message)
    {
        _toast = message;
        UpdateStatus();
        _toastTimer.Stop();
        _toastTimer.Start();
    }

    // ---------------------------------------------------------------- dialogs

    private void OpenFileDialog()
    {
        using var dialog = new OpenFileDialog
        {
            Title = L.T("dialog.open"),
            Filter = "Markdown (*.md;*.markdown)|*.md;*.markdown|" + L.T("dialog.allFiles") + " (*.*)|*.*",
            Multiselect = true,
            CheckFileExists = true,
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        foreach (string file in dialog.FileNames)
        {
            OpenFile(file);
        }
    }

    private void ShowMenu()
    {
        var menu = new ContextMenuStrip { ShowImageMargin = false, Renderer = new ToolStripProfessionalRenderer() };

        void Add(string text, Action action, Keys shortcut = Keys.None)
        {
            var item = new ToolStripMenuItem(text) { ShortcutKeyDisplayString = ShortcutText(shortcut) };
            item.Click += (_, _) => action();
            menu.Items.Add(item);
        }

        void Separator() => menu.Items.Add(new ToolStripSeparator());

        Add(L.T("menu.open"), OpenFileDialog, Keys.Control | Keys.O);
        Add(L.T("menu.reload"), ReloadActive, Keys.F5);
        Add(L.T("menu.save"), () => SaveActive(false), Keys.Control | Keys.S);
        Add(L.T("menu.saveAs"), () => SaveActive(true), Keys.Control | Keys.Shift | Keys.S);
        Separator();
        Add(L.T("menu.exportHtml"), () => Export("html"), Keys.None);
        Add(L.T("menu.exportText"), () => Export("text"), Keys.None);
        Add(L.T("menu.exportPng"), () => Export("png"), Keys.None);
        Separator();
        Add(L.T("menu.copyAll"), CopyAllText, Keys.None);
        Add(L.T("menu.copyHtml"), CopyHtml, Keys.None);
        Add(L.T("menu.print"), PrintDocument, Keys.Control | Keys.P);
        Separator();

        var view = new ToolStripMenuItem(L.T("menu.view"));
        AddViewItem(view, L.T("menu.outline"), _outlineVisible, ToggleOutline);
        AddViewItem(view, L.T("menu.preview"), _previewVisible, TogglePreview);
        AddViewItem(view, L.T("menu.rememberMode"), _settings.RememberMode, () =>
        {
            _settings.RememberMode = !_settings.RememberMode;
        });
        AddViewItem(view, L.T("menu.lineNumbers"), _lineNumbers, () =>
        {
            _lineNumbers = !_lineNumbers;
            _settings.LineNumbers = _lineNumbers;
            _editor.ShowLineNumbers = _lineNumbers;
            _renderer.Settings = _renderer.Settings with { CodeLineNumbers = _lineNumbers };
            _renderer.Invalidate();
            _view.RebindLayout();
            _editor.Invalidate();
        });
        AddViewItem(view, L.T("menu.wrapCode"), _wrapCode, () =>
        {
            _wrapCode = !_wrapCode;
            _settings.WrapCode = _wrapCode;
            _renderer.Settings = _renderer.Settings with { WrapCode = _wrapCode };
            _renderer.Invalidate();
            _view.RebindLayout();
        });
        AddViewItem(view, L.T("menu.numbering"), _numbering, () =>
        {
            _numbering = !_numbering;
            _settings.Numbering = _numbering;
            _renderer.Settings = _renderer.Settings with { NumberHeadings = _numbering };
            _renderer.Invalidate();
            _view.RebindLayout();
        });
        AddViewItem(view, L.T("menu.lineBreaks"), _settings.LineBreaks, () =>
        {
            _settings.LineBreaks = !_settings.LineBreaks;
            ReparseActive();
        });
        AddViewItem(view, L.T("menu.smartPunctuation"), _settings.SmartPunctuation, () =>
        {
            _settings.SmartPunctuation = !_settings.SmartPunctuation;
            ReparseActive();
        });
        AddViewItem(view, L.T("menu.serif"), _serif, () =>
        {
            _serif = !_serif;
            _settings.Serif = _serif;
            _theme = _theme.WithSerif(_serif);
            _renderer.SetTheme(_theme);
            _view.RebindLayout();
            ApplyTheme(persist: true);
        });
        menu.Items.Add(view);

        var theme = new ToolStripMenuItem(L.T("menu.theme"));
        foreach (ThemeMode mode in new[] { ThemeMode.Auto, ThemeMode.Light, ThemeMode.Dark })
        {
            var item = new ToolStripMenuItem(mode switch
            {
                ThemeMode.Light => L.T("menu.theme.light"),
                ThemeMode.Dark => L.T("menu.theme.dark"),
                _ => L.T("menu.theme.auto"),
            })
            {
                Checked = _themeMode == mode,
            };
            item.Click += (_, _) =>
            {
                _themeMode = mode;
                ApplyTheme(persist: true);
            };
            theme.DropDownItems.Add(item);
        }

        menu.Items.Add(theme);
        Separator();
        Add(L.T("menu.shortcuts"), ShowShortcuts, Keys.F1);
        Add(L.T("menu.settings"), () => OpenExternal(_settings.Directory), Keys.None);
        Add(L.T("menu.about"), ShowAbout, Keys.None);
        Separator();
        Add(L.T("menu.exit"), Close, Keys.Alt | Keys.F4);

        menu.Show(_btnMore, new Point(0, _btnMore.Height));
    }

    private static void AddViewItem(ToolStripMenuItem parent, string text, bool isChecked, Action action)
    {
        var item = new ToolStripMenuItem(text) { Checked = isChecked, CheckOnClick = false };
        item.Click += (_, _) => action();
        parent.DropDownItems.Add(item);
    }

    private static string ShortcutText(Keys keys)
    {
        if (keys == Keys.None)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        if (keys.HasFlag(Keys.Control))
        {
            sb.Append("Ctrl+");
        }

        if (keys.HasFlag(Keys.Shift))
        {
            sb.Append("Shift+");
        }

        if (keys.HasFlag(Keys.Alt))
        {
            sb.Append("Alt+");
        }

        sb.Append(keys & Keys.KeyCode);
        return sb.ToString();
    }

    private void ShowShortcuts()
    {
        using var dialog = new InfoDialog(L.T("shortcuts.title"), L.T("shortcuts.text"), _theme);
        dialog.ShowDialog(this);
    }

    private void ShowAbout()
    {
        using var dialog = new AboutDialog(_theme);
        dialog.ShowDialog(this);
    }

    /// <summary>Re-parses the active document after a parser-level option changed.</summary>
    private void ReparseActive()
    {
        if (_active is null)
        {
            return;
        }

        float scroll = _view.ScrollY;
        _active.Document = Parse(_active.Text, _active.Path);
        _renderer.SetDocument(_active.Document);
        _view.RebindLayout();
        _view.ScrollTo(scroll, animate: false);
        _outline.SetDocument(_active.Document);
        SyncGlossary();
        UpdateStatus();
    }

    private void ReloadActive()
    {
        if (_active?.Path is null)
        {
            return;
        }

        try
        {
            string text = File.ReadAllText(_active.Path);
            _active.Text = text;
            _active.Dirty = false;
            _active.Document = Parse(text, _active.Path);
            _renderer.SetDocument(_active.Document);
            _view.RebindLayout();
            _outline.SetDocument(_active.Document);
            SyncGlossary();
            _suppressEditorEvents = true;
            _editor.Text = text;
            _suppressEditorEvents = false;
            _editor.Dirty = false;
            UpdateTitle();
            UpdateTabs();
            UpdateStatus();
            Toast(L.T("menu.reload"));
        }
        catch (Exception ex)
        {
            Toast(ex.Message);
        }
    }

    // ---------------------------------------------------------------- exports

    private void CopyAllText()
    {
        if (_active?.Document is null)
        {
            return;
        }

        Clipboard.SetText(PlainTextExporter.ToText(_active.Document));
        Toast(L.T("toast.copied"));
    }

    private void CopyHtml()
    {
        if (_active?.Document is null)
        {
            return;
        }

        Clipboard.SetText(HtmlExporter.ToHtml(_active.Document));
        Toast(L.T("toast.copied"));
    }

    public void Export(string format)
    {
        if (_active is null)
        {
            return;
        }

        if (_active.Document is null)
        {
            _active.Document = Parse(_active.Text, _active.Path);
        }

        if (format is "html" or "text")
        {
            bool html = format == "html";
            using var dialog = new SaveFileDialog
            {
                Title = html ? L.T("dialog.exportHtml") : L.T("dialog.exportText"),
                Filter = html ? "HTML (*.html)|*.html" : "Text (*.txt)|*.txt",
                FileName = Path.GetFileNameWithoutExtension(_active.Title) + (html ? ".html" : ".txt"),
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                if (html)
                {
                    File.WriteAllText(dialog.FileName, HtmlExporter.ToHtml(_active.Document));
                }
                else
                {
                    File.WriteAllText(dialog.FileName, PlainTextExporter.ToText(_active.Document));
                }

                Toast(string.Format(L.T("export.done"), Path.GetFileName(dialog.FileName)));
            }
            catch (Exception ex)
            {
                Toast(string.Format(L.T("export.failed"), ex.Message));
            }

            return;
        }

        if (format == "png")
        {
            using var dialog = new SaveFileDialog
            {
                Title = L.T("dialog.exportPng"),
                Filter = "PNG (*.png)|*.png",
                FileName = Path.GetFileNameWithoutExtension(_active.Title) + ".png",
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                ExportPng(dialog.FileName);
                Toast(string.Format(L.T("export.done"), Path.GetFileName(dialog.FileName)));
            }
            catch (Exception ex)
            {
                Toast(string.Format(L.T("export.failed"), ex.Message));
            }
        }
    }

    /// <summary>Headless PNG export used by <c>--export png</c> and the menu.</summary>
    public void ExportPng(string path, float? width = null)
    {
        if (_active?.Document is null)
        {
            return;
        }

        float pixelWidth = width ?? 1000f * DeviceDpi / 96f;
        using Bitmap bitmap = _renderer.RenderToBitmap(pixelWidth);
        bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
    }

    private void PrintDocument()
    {
        if (_active?.Document is null)
        {
            return;
        }

        using var dialog = new PrintDialog();
        var document = new PrintDocument { DocumentName = _active.Title };
        var printer = new DocumentPrinter(_active.Document, _theme);
        document.PrintPage += printer.PrintPage;
        dialog.Document = document;

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                document.Print();
            }
            catch (Exception ex)
            {
                Toast(ex.Message);
            }
        }
    }

    // ------------------------------------------------------------------ watch

    private void WatchFile()
    {
        _watcher?.Dispose();
        _watcher = null;

        if (!_settings.Watch || _active?.Path is null)
        {
            return;
        }

        try
        {
            string directory = Path.GetDirectoryName(_active.Path) ?? ".";
            _watcher = new FileSystemWatcher(directory, Path.GetFileName(_active.Path))
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                EnableRaisingEvents = true,
            };

            _watcher.Changed += OnFileChanged;
            _watcher.Renamed += OnFileChanged;
        }
        catch
        {
            _watcher = null;
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        BeginInvoke(() =>
        {
            if (_active?.Path is null)
            {
                return;
            }

            if (_active.Dirty)
            {
                Toast(L.T("dialog.reload"));
                return;
            }

            ReloadActive();
        });
    }

    // ------------------------------------------------------------- window state

    private void RestoreWindow()
    {
        Rectangle bounds = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 800);
        int width = Math.Min(_settings.WindowWidth, bounds.Width);
        int height = Math.Min(_settings.WindowHeight, bounds.Height);

        if (_settings.WindowX != int.MinValue && _settings.WindowY != int.MinValue)
        {
            var candidate = new Rectangle(_settings.WindowX, _settings.WindowY, width, height);
            if (candidate.Right > 0 && candidate.Bottom > 0 && candidate.Left < bounds.Right && candidate.Top < bounds.Bottom)
            {
                Bounds = candidate;
            }
            else
            {
                Bounds = new Rectangle(bounds.Left + (bounds.Width - width) / 2, bounds.Top + (bounds.Height - height) / 2, width, height);
            }
        }
        else
        {
            Bounds = new Rectangle(bounds.Left + (bounds.Width - width) / 2, bounds.Top + (bounds.Height - height) / 2, width, height);
        }

        if (_settings.Maximized)
        {
            WindowState = FormWindowState.Maximized;
        }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        bool anyDirty = _sessions.Any(s => s.Dirty);
        if (anyDirty)
        {
            DialogResult result = MessageBox.Show(
                this,
                L.T("dialog.unsaved").Replace("{0}", L.T("app.title")),
                L.T("app.title"),
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (result == DialogResult.Cancel)
            {
                e.Cancel = true;
                return;
            }
        }

        SaveWindowState();
    }

    private void SaveWindowState()
    {
        _settings.Maximized = WindowState == FormWindowState.Maximized;
        if (WindowState == FormWindowState.Normal)
        {
            _settings.WindowX = Bounds.X;
            _settings.WindowY = Bounds.Y;
            _settings.WindowWidth = Bounds.Width;
            _settings.WindowHeight = Bounds.Height;
        }

        _settings.EditorPreview = _previewVisible;
        _settings.Session = _sessions.Where(s => s.Path is not null).Select(s => s.Path!).ToList();
        _settings.Save();
    }

    // ------------------------------------------------------------------ input

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Control | Keys.O:
                OpenFileDialog();
                return true;
            case Keys.Control | Keys.S:
                SaveActive(false);
                return true;
            case Keys.Control | Keys.Shift | Keys.S:
                SaveActive(true);
                return true;
            case Keys.Control | Keys.W:
                if (_active is not null)
                {
                    CloseSession(_sessions.IndexOf(_active));
                }

                return true;
            case Keys.Control | Keys.T:
                OpenUntitled();
                return true;
            case Keys.Control | Keys.Tab:
                if (_sessions.Count > 1 && _active is not null)
                {
                    ActivateSession((_sessions.IndexOf(_active) + 1) % _sessions.Count);
                }

                return true;
            case Keys.Control | Keys.F:
                ShowFind();
                return true;
            case Keys.Control | Keys.R:
                SetMode(ViewMode.Rendered);
                return true;
            case Keys.Control | Keys.U:
                SetMode(ViewMode.Source);
                return true;
            case Keys.Control | Keys.E:
                SetMode(ViewMode.Editor);
                return true;
            case Keys.Control | Keys.Shift | Keys.M:
                SetMode((ViewMode)(((int)_mode + 1) % 3));
                return true;
            case Keys.Control | Keys.Shift | Keys.L:
                ToggleTheme();
                return true;
            case Keys.Control | Keys.Oemplus:
            case Keys.Control | Keys.Add:
                SetZoom(_theme.Scale + 0.1f);
                return true;
            case Keys.Control | Keys.OemMinus:
            case Keys.Control | Keys.Subtract:
                SetZoom(_theme.Scale - 0.1f);
                return true;
            case Keys.Control | Keys.D0:
                SetZoom(1f);
                return true;
            case Keys.Control | Keys.OemBackslash:
                ToggleOutline();
                return true;
            case Keys.Control | Keys.P:
                PrintDocument();
                return true;
            case Keys.F1:
                ShowShortcuts();
                return true;
            case Keys.F5:
                ReloadActive();
                return true;
            case Keys.Escape when _find.Visible:
                HideFind();
                return true;
            case Keys.F3:
                ShowFind();
                return true;
            case Keys.Control | Keys.Shift | Keys.P:
                TogglePreview();
                return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
        {
            e.Effect = DragDropEffects.Copy;
        }
    }

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files)
        {
            return;
        }

        foreach (string file in files)
        {
            if (file.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".markdown", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".mdown", StringComparison.OrdinalIgnoreCase))
            {
                OpenFile(file);
            }
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        LayoutChrome();
        WindowChrome.Apply(Handle, _theme.IsDark, _theme.Colors.Surface, _theme.Colors.Text);
        _view.Focus();
    }

    private static ThemeMode ParseTheme(string value) => value.ToLowerInvariant() switch
    {
        "light" => ThemeMode.Light,
        "dark" => ThemeMode.Dark,
        _ => ThemeMode.Auto,
    };

    private static ViewMode ParseMode(string value) => value.ToLowerInvariant() switch
    {
        "source" => ViewMode.Source,
        "editor" => ViewMode.Editor,
        _ => ViewMode.Rendered,
    };

    public ThemeMode CurrentThemeMode => _themeMode;

    /// <summary>The palette currently painted (what the user sees, not the stored mode).</summary>
    public Theme Theme => _theme;

    public ViewMode CurrentMode => _mode;

    public void SetThemeMode(ThemeMode mode)
    {
        _themeMode = mode;
        ApplyTheme(persist: false);
    }

    public void SetOutlineVisible(bool visible)
    {
        if (_outlineVisible != visible)
        {
            ToggleOutline();
        }
    }

    public void SetPreviewVisible(bool visible)
    {
        if (_previewVisible != visible)
        {
            TogglePreview();
        }
    }

    /// <summary>Development hook: positions the rendered view for a screenshot.</summary>
    public void ScrollContent(float y) => _view.ScrollTo(y, animate: false);

    public void SetReadingWidth(float width)
    {
        _view.ReadingWidthOverride = Math.Clamp(width, 380f, 1600f);
        _view.RebindLayout();
    }

    /// <summary>
    /// "Auto" follows the Windows app mode; Windows broadcasts WM_SETTINGCHANGE when the
    /// user flips it, and the window must repaint without a restart.
    /// </summary>
    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        const int WmSettingChange = 0x001A;
        const int WmThemeChanged = 0x031A;
        if ((m.Msg == WmSettingChange || m.Msg == WmThemeChanged) && _themeMode == ThemeMode.Auto && _uiReady)
        {
            BeginInvoke(FollowWindowsTheme);
        }
    }

    private void FollowWindowsTheme()
    {
        Theme resolved = ThemeCatalog.Resolve("auto");
        if (string.Equals(resolved.Id, _theme.Id, StringComparison.Ordinal))
        {
            return;
        }

        ApplyTheme(persist: false);
    }

    public void FocusFind() => ShowFind();

    public void PrintCurrentDocument() => PrintDocument();

    public string DescribeLayout()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"form        : {Width}x{Height} client {ClientSize.Width}x{ClientSize.Height} dpi {DeviceDpi}");
        sb.AppendLine($"view        : {_view.Bounds} visible={_view.Visible}");
        sb.AppendLine($"outline     : {_outline.Bounds} visible={_outline.Visible}");
        sb.AppendLine($"sheet       : {_view.SheetRect} content={_view.MeasuredContentWidth:0.0} left={_view.MeasuredSheetLeft:0.0}");
        sb.AppendLine($"layout      : {(this._renderer.Layout is null ? "null" : $"{_renderer.Layout.Blocks.Count} blocks, height {_renderer.Layout.Height:0.0}")}");
        sb.AppendLine($"theme       : {_theme.Id} scale={_theme.Scale} dark={_theme.IsDark}");
        sb.AppendLine($"mode        : {_mode}  sessions={_sessions.Count}");
        if (_renderer.Layout is { } layout)
        {
            foreach (BlockLayout block in layout.Blocks.Take(6))
            {
                sb.AppendLine($"  block {block.Source.GetType().Name,-18} top={block.Top,7:0.0} h={block.Height,7:0.0} left={block.Left,6:0.0} w={block.Width,6:0.0}");
            }
        }

        return sb.ToString();
    }
}













