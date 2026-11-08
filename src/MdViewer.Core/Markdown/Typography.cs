using System.Text;

namespace MdViewer.Core.Markdown;

/// <summary>
/// Opt-in typographic replacements. Deliberately limited to dashes, ellipsis and
/// common symbols: quotes are left alone because documentation is full of code,
/// paths and measurements where straight quotes are correct.
/// </summary>
public static class SmartTypography
{
    private static readonly (string From, string To)[] Rules =
    [
        ("...", "…"),
        ("---", "—"),
        ("--", "–"),
        ("(c)", "©"),
        ("(C)", "©"),
        ("(r)", "®"),
        ("(R)", "®"),
        ("(tm)", "™"),
        ("(TM)", "™"),
        ("!=", "≠"),
        ("<=", "≤"),
        (">=", "≥"),
        ("+-", "±"),
    ];

    public static string Apply(string text)
    {
        if (text.Length < 2)
        {
            return text;
        }

        var sb = new StringBuilder(text.Length);
        int i = 0;
        while (i < text.Length)
        {
            bool replaced = false;
            foreach ((string from, string to) in Rules)
            {
                if (i + from.Length > text.Length)
                {
                    continue;
                }

                if (string.CompareOrdinal(text, i, from, 0, from.Length) != 0)
                {
                    continue;
                }

                sb.Append(to);
                i += from.Length;
                replaced = true;
                break;
            }

            if (replaced)
            {
                continue;
            }

            sb.Append(text[i]);
            i++;
        }

        return sb.ToString();
    }
}

