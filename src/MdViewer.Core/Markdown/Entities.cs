using System.Globalization;
using System.Text;

namespace MdViewer.Core.Markdown;

/// <summary>Minimal HTML entity support (named + numeric), no external tables.</summary>
public static class Entities
{
    private static readonly Dictionary<string, string> Named = new(StringComparer.Ordinal)
    {
        ["amp"] = "&", ["lt"] = "<", ["gt"] = ">", ["quot"] = "\"", ["apos"] = "'",
        ["nbsp"] = "\u00A0", ["ensp"] = "\u2002", ["emsp"] = "\u2003", ["thinsp"] = "\u2009",
        ["copy"] = "©", ["reg"] = "®", ["trade"] = "™", ["deg"] = "°", ["plusmn"] = "±",
        ["times"] = "×", ["divide"] = "÷", ["minus"] = "−", ["micro"] = "µ", ["para"] = "¶",
        ["sect"] = "§", ["middot"] = "·", ["bull"] = "•", ["hellip"] = "…",
        ["ndash"] = "–", ["mdash"] = "—", ["lsquo"] = "‘", ["rsquo"] = "’",
        ["ldquo"] = "“", ["rdquo"] = "”", ["laquo"] = "«", ["raquo"] = "»",
        ["dagger"] = "†", ["Dagger"] = "‡", ["larr"] = "←", ["uarr"] = "↑",
        ["rarr"] = "→", ["darr"] = "↓", ["harr"] = "↔", ["rArr"] = "⇒",
        ["lArr"] = "⇐", ["hArr"] = "⇔", ["forall"] = "∀", ["part"] = "∂",
        ["exist"] = "∃", ["empty"] = "∅", ["nabla"] = "∇", ["isin"] = "∈",
        ["notin"] = "∉", ["ni"] = "∋", ["prod"] = "∏", ["sum"] = "∑",
        ["lowast"] = "∗", ["radic"] = "√", ["prop"] = "∝", ["infin"] = "∞",
        ["ang"] = "∠", ["and"] = "∧", ["or"] = "∨", ["cap"] = "∩", ["cup"] = "∪",
        ["int"] = "∫", ["there4"] = "∴", ["sim"] = "∼", ["cong"] = "≅",
        ["asymp"] = "≈", ["ne"] = "≠", ["equiv"] = "≡", ["le"] = "≤", ["ge"] = "≥",
        ["sub"] = "⊂", ["sup"] = "⊃", ["nsub"] = "⊄", ["sube"] = "⊆", ["supe"] = "⊇",
        ["oplus"] = "⊕", ["otimes"] = "⊗", ["perp"] = "⊥", ["sdot"] = "⋅",
        ["lceil"] = "⌈", ["rceil"] = "⌉", ["lfloor"] = "⌊", ["rfloor"] = "⌋",
        ["lang"] = "〈", ["rang"] = "〉", ["loz"] = "◊", ["spades"] = "♠",
        ["clubs"] = "♣", ["hearts"] = "♥", ["diams"] = "♦", ["check"] = "✓",
        ["cross"] = "✗", ["star"] = "☆", ["starf"] = "★", ["nbsp2"] = "\u00A0",
        ["shy"] = "\u00AD", ["zwj"] = "\u200D", ["zwnj"] = "\u200C", ["lrm"] = "\u200E",
        ["rlm"] = "\u200F", ["iexcl"] = "¡", ["iquest"] = "¿", ["cent"] = "¢",
        ["pound"] = "£", ["curren"] = "¤", ["yen"] = "¥", ["euro"] = "€",
        ["beta"] = "β", ["alpha"] = "α", ["gamma"] = "γ", ["delta"] = "δ",
        ["pi"] = "π", ["sigma"] = "σ", ["omega"] = "ω", ["mu"] = "μ",
    };

    /// <summary>Decodes entities in a single pass; unknown entities stay verbatim.</summary>
    public static string Decode(string text)
    {
        int ampersand = text.IndexOf('&');
        if (ampersand < 0)
        {
            return text;
        }

        var sb = new StringBuilder(text.Length);
        sb.Append(text, 0, ampersand);

        for (int i = ampersand; i < text.Length; i++)
        {
            char c = text[i];
            if (c != '&')
            {
                sb.Append(c);
                continue;
            }

            int semicolon = text.IndexOf(';', i + 1);
            if (semicolon < 0 || semicolon - i > 12)
            {
                sb.Append(c);
                continue;
            }

            string name = text[(i + 1)..semicolon];
            if (TryResolve(name, out string? resolved))
            {
                sb.Append(resolved);
                i = semicolon;
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    private static bool TryResolve(string name, out string resolved)
    {
        resolved = string.Empty;
        if (name.Length == 0)
        {
            return false;
        }

        if (name[0] == '#')
        {
            bool hex = name.Length > 1 && (name[1] == 'x' || name[1] == 'X');
            string digits = hex ? name[2..] : name[1..];
            NumberStyles style = hex ? NumberStyles.HexNumber : NumberStyles.Integer;
            if (int.TryParse(digits, style, CultureInfo.InvariantCulture, out int code)
                && code > 0 && code <= 0x10FFFF
                && (code < 0xD800 || code > 0xDFFF))
            {
                resolved = char.ConvertFromUtf32(code);
                return true;
            }

            return false;
        }

        return Named.TryGetValue(name, out resolved!);
    }
}
