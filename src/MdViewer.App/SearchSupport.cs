using System.Drawing;
using MdViewer.Render.Layout;
using MdViewer.Render.Text;

namespace MdViewer.App;

public readonly record struct SearchHit(RectangleF Bounds, int Index);

/// <summary>Text hit-testing shared by the blocks that can contain search hits.</summary>
internal static class SearchSupport
{
    public static void Collect(IReadOnlyList<Run> runs, float left, float top, string query, List<RectangleF> hits, FontBook fonts)
    {
        if (query.Length == 0 || runs.Count == 0)
        {
            return;
        }

        foreach (Run run in runs)
        {
            if (run.Text.Length == 0)
            {
                continue;
            }

            int start = 0;
            while (start <= run.Text.Length - query.Length)
            {
                int index = run.Text.IndexOf(query, start, StringComparison.CurrentCultureIgnoreCase);
                if (index < 0)
                {
                    break;
                }

                float prefix = fonts.Measure(run.Text[..index], run.Style.Font);
                float width = fonts.Measure(run.Text.Substring(index, query.Length), run.Style.Font);
                hits.Add(new RectangleF(left + run.X + prefix, top, MathF.Max(2f, width), run.Height > 0 ? run.Height : 18f));
                start = index + Math.Max(1, query.Length);
            }
        }
    }
}
