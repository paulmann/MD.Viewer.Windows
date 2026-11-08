using System.Drawing;
using MdViewer.Core.Markdown;
using MdViewer.Render.Rendering;
using MdViewer.Render.Text;

namespace MdViewer.Render.Layout;

/// <summary>A clickable rectangle (link, footnote, copy button).</summary>
public sealed class LinkRegion
{
    public required RectangleF Bounds { get; init; }

    public required string Target { get; init; }

    public bool IsAnchor { get; init; }

    public bool IsExternal { get; init; }

    public bool Missing { get; init; }

    public string? Title { get; init; }
}

/// <summary>Base class for every positioned block visual (coordinates are sheet-absolute).</summary>
public abstract class BlockLayout
{
    public required Block Source { get; init; }

    public float Top { get; set; }

    public float Left { get; set; }

    public float Width { get; set; }

    public float Height { get; set; }

    public float Bottom => Top + Height;

    /// <summary>Heading anchor id, when the block exposes one.</summary>
    public string? AnchorId { get; set; }

    public abstract void Paint(PaintContext ctx);

    /// <summary>Registers clickable regions; called once after layout.</summary>
    public virtual void CollectLinks(List<LinkRegion> regions)
    {
    }

    /// <summary>First line box for scroll-to-block behaviour.</summary>
    public virtual RectangleF SelectionBounds => new(Left, Top, Width, Height);
}

public sealed record RenderSettings
{
    public bool NumberHeadings { get; init; }

    public bool ShowHeadingAnchors { get; init; } = true;

    public bool CodeLineNumbers { get; init; }

    public bool WrapCode { get; init; } = true;

    public bool ShowFrontMatter { get; init; } = true;

    public bool UnderlineLinks { get; init; }

    /// <summary>When set, only this anchor is highlighted (hover feedback).</summary>
    public string? HoverLink { get; set; }
}

/// <summary>A fully measured document: visuals, anchor index and link regions.</summary>
public sealed class DocumentLayout
{
    public List<BlockLayout> Blocks { get; } = [];

    public Dictionary<string, BlockLayout> Anchors { get; } = new(StringComparer.Ordinal);

    public List<LinkRegion> Links { get; } = [];

    public float Height { get; set; }

    public float Width { get; set; }

    /// <summary>Top inset used by the reading sheet.</summary>
    public float ContentTop { get; set; }

    public BlockLayout? FindBlock(float y)
    {
        int lo = 0;
        int hi = Blocks.Count - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2;
            BlockLayout block = Blocks[mid];
            if (y < block.Top)
            {
                hi = mid - 1;
            }
            else if (y > block.Bottom)
            {
                lo = mid + 1;
            }
            else
            {
                return block;
            }
        }

        return null;
    }

    public LinkRegion? HitTestLink(PointF point)
    {
        foreach (LinkRegion region in Links)
        {
            if (region.Bounds.Contains(point))
            {
                return region;
            }
        }

        return null;
    }

    /// <summary>Nearest anchor at or above <paramref name="y"/> (for the outline).</summary>
    public string? AnchorAt(float y)
    {
        string? best = null;
        float bestTop = float.NegativeInfinity;
        foreach ((string id, BlockLayout block) in Anchors)
        {
            if (block.Top <= y + 8f && block.Top >= bestTop)
            {
                bestTop = block.Top;
                best = id;
            }
        }

        return best;
    }
}


