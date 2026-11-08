using System.Text;

namespace MdViewer.Core.Markdown;

/// <summary>GitHub-compatible heading slugs, Unicode aware, de-duplicated.</summary>
public sealed class Slugger
{
    private readonly Dictionary<string, int> _seen = new(StringComparer.Ordinal);

    public string Slug(string text)
    {
        var sb = new StringBuilder(text.Length);
        bool pendingDash = false;

        foreach (char c in text)
        {
            if (char.IsWhiteSpace(c) || c == '-' || c == '_')
            {
                pendingDash = sb.Length > 0;
                continue;
            }

            if (char.IsLetterOrDigit(c) || c > 0x7F && char.IsLetterOrDigit(c))
            {
                if (pendingDash)
                {
                    sb.Append('-');
                    pendingDash = false;
                }

                sb.Append(char.ToLowerInvariant(c));
                continue;
            }

            // punctuation is dropped, but it still separates words
            pendingDash = sb.Length > 0;
        }

        string slug = sb.ToString();
        if (slug.Length == 0)
        {
            slug = "section";
        }

        if (_seen.TryGetValue(slug, out int count))
        {
            _seen[slug] = count + 1;
            return $"{slug}-{count}";
        }

        _seen[slug] = 1;
        return slug;
    }

    public void Reset() => _seen.Clear();
}
