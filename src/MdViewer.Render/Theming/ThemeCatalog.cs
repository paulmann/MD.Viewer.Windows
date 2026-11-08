using System.Drawing;

namespace MdViewer.Render.Theming;

/// <summary>The two built-in appearances plus Windows "app mode" detection.</summary>
public static class ThemeCatalog
{
    public static Theme Light { get; } = new()
    {
        Id = "light",
        Name = "Light",
        IsDark = false,
        Colors = new Palette
        {
            Canvas = Color.FromArgb(0xF1, 0xF2, 0xF4),
            Surface = Color.FromArgb(0xFF, 0xFF, 0xFF),
            Elevated = Color.FromArgb(0xFF, 0xFF, 0xFF),
            Text = Color.FromArgb(0x15, 0x17, 0x1C),
            TextMuted = Color.FromArgb(0x5B, 0x64, 0x72),
            TextFaint = Color.FromArgb(0x6E, 0x78, 0x88),
            Border = Color.FromArgb(0xE4, 0xE6, 0xEB),
            BorderStrong = Color.FromArgb(0xD2, 0xD6, 0xDE),
            Accent = Color.FromArgb(0x25, 0x63, 0xEB),
            AccentText = Color.FromArgb(0xFF, 0xFF, 0xFF),
            AccentSoft = Color.FromArgb(0xEA, 0xF1, 0xFE),
            Link = Color.FromArgb(0x1D, 0x5B, 0xD8),
            CodeBg = Color.FromArgb(0xF7, 0xF8, 0xFA),
            CodeBorder = Color.FromArgb(0xE7, 0xE9, 0xEE),
            CodeText = Color.FromArgb(0x24, 0x29, 0x33),
            InlineCodeBg = Color.FromArgb(0xF1, 0xF3, 0xF6),
            InlineCodeText = Color.FromArgb(0x9A, 0x33, 0x5B),
            MarkBg = Color.FromArgb(0xFF, 0xF1, 0xB8),
            QuoteBar = Color.FromArgb(0xCB, 0xD2, 0xDC),
            QuoteBg = Color.FromArgb(0xFA, 0xFB, 0xFC),
            TableHeaderBg = Color.FromArgb(0xF7, 0xF8, 0xFA),
            TableBorder = Color.FromArgb(0xE4, 0xE6, 0xEB),
            TableStripe = Color.FromArgb(0xFB, 0xFB, 0xFC),
            Selection = Color.FromArgb(0xCF, 0xE0, 0xFF),
            Hover = Color.FromArgb(0xF2, 0xF4, 0xF7),
            Shadow = Color.FromArgb(24, 0x0F, 0x17, 0x2A),
            ScrollThumb = Color.FromArgb(0xC7, 0xCC, 0xD6),
            ScrollThumbHover = Color.FromArgb(0xA8, 0xAF, 0xBC),
            NoteBar = Color.FromArgb(0x25, 0x63, 0xEB),
            NoteBg = Color.FromArgb(0xF3, 0xF7, 0xFF),
            TipBar = Color.FromArgb(0x0D, 0x7A, 0x45),
            TipBg = Color.FromArgb(0xF1, 0xFA, 0xF5),
            ImportantBar = Color.FromArgb(0x7C, 0x3A, 0xED),
            ImportantBg = Color.FromArgb(0xF7, 0xF3, 0xFF),
            WarningBar = Color.FromArgb(0xA8, 0x5C, 0x04),
            WarningBg = Color.FromArgb(0xFF, 0xF8, 0xEC),
            CautionBar = Color.FromArgb(0xC3, 0x1F, 0x1F),
            CautionBg = Color.FromArgb(0xFF, 0xF3, 0xF2),
            MissingLink = Color.FromArgb(0xDC, 0x26, 0x26),
            Keyword = Color.FromArgb(0x82, 0x50, 0xDF),
            String = Color.FromArgb(0x0A, 0x7D, 0x32),
            Number = Color.FromArgb(0xB5, 0x56, 0x0A),
            Comment = Color.FromArgb(0x8A, 0x93, 0xA2),
            TypeName = Color.FromArgb(0x17, 0x5F, 0xD6),
            Function = Color.FromArgb(0x66, 0x39, 0xBA),
            Punctuation = Color.FromArgb(0x57, 0x60, 0x6E),
        },
    };

    public static Theme Dark { get; } = new()
    {
        Id = "dark",
        Name = "Dark",
        IsDark = true,
        Colors = new Palette
        {
            Canvas = Color.FromArgb(0x0E, 0x10, 0x14),
            Surface = Color.FromArgb(0x17, 0x1A, 0x20),
            Elevated = Color.FromArgb(0x1E, 0x22, 0x2A),
            Text = Color.FromArgb(0xE8, 0xEA, 0xEE),
            TextMuted = Color.FromArgb(0xA0, 0xA8, 0xB4),
            TextFaint = Color.FromArgb(0x8A, 0x93, 0xA2),
            Border = Color.FromArgb(0x25, 0x29, 0x32),
            BorderStrong = Color.FromArgb(0x33, 0x38, 0x44),
            Accent = Color.FromArgb(0x7A, 0xA2, 0xFF),
            AccentText = Color.FromArgb(0x0E, 0x10, 0x14),
            AccentSoft = Color.FromArgb(0x1B, 0x24, 0x36),
            Link = Color.FromArgb(0x86, 0xB0, 0xFF),
            CodeBg = Color.FromArgb(0x1C, 0x20, 0x27),
            CodeBorder = Color.FromArgb(0x2A, 0x2F, 0x39),
            CodeText = Color.FromArgb(0xDF, 0xE3, 0xEA),
            InlineCodeBg = Color.FromArgb(0x25, 0x2A, 0x33),
            InlineCodeText = Color.FromArgb(0xF0, 0x9D, 0xB4),
            MarkBg = Color.FromArgb(0x55, 0x42, 0x0A),
            QuoteBar = Color.FromArgb(0x3A, 0x41, 0x50),
            QuoteBg = Color.FromArgb(0x1A, 0x1E, 0x24),
            TableHeaderBg = Color.FromArgb(0x1E, 0x22, 0x2A),
            TableBorder = Color.FromArgb(0x2A, 0x2E, 0x37),
            TableStripe = Color.FromArgb(0x1A, 0x1D, 0x24),
            Selection = Color.FromArgb(0x2B, 0x3A, 0x57),
            Hover = Color.FromArgb(0x22, 0x26, 0x2E),
            Shadow = Color.FromArgb(90, 0, 0, 0),
            ScrollThumb = Color.FromArgb(0x38, 0x3E, 0x49),
            ScrollThumbHover = Color.FromArgb(0x4C, 0x54, 0x62),
            NoteBar = Color.FromArgb(0x6E, 0x9E, 0xFF),
            NoteBg = Color.FromArgb(0x16, 0x1E, 0x2E),
            TipBar = Color.FromArgb(0x4A, 0xCF, 0x8B),
            TipBg = Color.FromArgb(0x14, 0x22, 0x1B),
            ImportantBar = Color.FromArgb(0xA9, 0x8B, 0xFF),
            ImportantBg = Color.FromArgb(0x1E, 0x19, 0x2E),
            WarningBar = Color.FromArgb(0xF0, 0xB4, 0x5C),
            WarningBg = Color.FromArgb(0x26, 0x1F, 0x12),
            CautionBar = Color.FromArgb(0xFF, 0x8B, 0x86),
            CautionBg = Color.FromArgb(0x2A, 0x17, 0x16),
            MissingLink = Color.FromArgb(0xFF, 0x8B, 0x86),
            Keyword = Color.FromArgb(0xC7, 0x92, 0xEA),
            String = Color.FromArgb(0x9A, 0xD2, 0x9A),
            Number = Color.FromArgb(0xF0, 0xA3, 0x5E),
            Comment = Color.FromArgb(0x6B, 0x74, 0x86),
            TypeName = Color.FromArgb(0x93, 0xB7, 0xFF),
            Function = Color.FromArgb(0xD2, 0xA8, 0xFF),
            Punctuation = Color.FromArgb(0x9A, 0xA2, 0xB0),
        },
    };

    public static Theme Get(string? id) => string.Equals(id, "dark", StringComparison.OrdinalIgnoreCase) ? Dark : Light;

    /// <summary>Reads the Windows "Choose your default app mode" setting (Personalize).</summary>
    public static bool SystemPrefersDark()
    {
        try
        {
            using Microsoft.Win32.RegistryKey? key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            object? value = key?.GetValue("AppsUseLightTheme");
            if (value is int number)
            {
                return number == 0;
            }

            // Some Windows builds only maintain the system-wide value.
            object? system = key?.GetValue("SystemUsesLightTheme");
            if (system is int systemNumber)
            {
                return systemNumber == 0;
            }
        }
        catch
        {
            // registry unavailable — fall through to light
        }

        return false;
    }

    /// <summary>Resolves <c>auto</c> against the current system setting.</summary>
    public static Theme Resolve(string? id) =>
        string.Equals(id, "auto", StringComparison.OrdinalIgnoreCase) || string.Equals(id, "system", StringComparison.OrdinalIgnoreCase)
            ? SystemPrefersDark() ? Dark : Light
            : Get(id);
}



