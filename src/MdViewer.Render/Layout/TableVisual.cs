using System.Drawing;
using MdViewer.Core.Markdown;
using MdViewer.Render.Rendering;
using MdViewer.Render.Text;

namespace MdViewer.Render.Layout;

public sealed class TableCellVisual
{
    public required ShapedText Text { get; init; }

    public required CellAlign Align { get; init; }

    public CellAlign EffectiveAlign { get; set; }
}

/// <summary>GFM table with measured columns, header band and zebra rows.</summary>
public sealed class TableVisual : BlockLayout
{
    private const float CellPadX = 12f;
    private const float CellPadY = 8f;

    public required LayoutContext Ctx { get; init; }

    public required TableCellVisual[] Headers { get; init; }

    public required TableCellVisual[][] Rows { get; init; }

    public required float[] ColumnWidths { get; init; }

    public required float[] RowHeights { get; init; }

    public float HeaderHeight { get; init; }

    public bool IsGlossary { get; init; }

    public override void Paint(PaintContext ctx)
    {
        float x0 = Left;
        float y = Top;
        float radius = Ctx.Px(8f);

        var outer = new RectangleF(Left, Top, Width, Height);
        ctx.FillRound(outer, radius, Ctx.Colors.Surface);
        ctx.StrokeRound(outer, radius, Ctx.Colors.TableBorder, MathF.Max(1f, Ctx.Scale));

        // header band
        var headerRect = new RectangleF(Left, Top, Width, HeaderHeight);
        using (var path = PaintContext.RoundedPath(outer, radius))
        {
            var state = ctx.G.Save();
            ctx.G.SetClip(path);
            ctx.Fill(new RectangleF(Left, Top, Width, HeaderHeight), Ctx.Colors.TableHeaderBg);
            ctx.G.Restore(state);
        }

        float headerBottom = Top + HeaderHeight;
        ctx.G.DrawLine(ctx.Pen(Ctx.Colors.TableBorder, MathF.Max(1f, Ctx.Scale)), Left, headerBottom, Left + Width, headerBottom);

        // zebra rows
        float rowTop = headerBottom;
        for (int r = 0; r < Rows.Length; r++)
        {
            float rowHeight = RowHeights[r];
            if (r % 2 == 1)
            {
                ctx.Fill(new RectangleF(Left + 1, rowTop, Width - 2, rowHeight), Ctx.Colors.TableStripe);
            }

            if (r < Rows.Length - 1)
            {
                float lineY = rowTop + rowHeight;
                ctx.G.DrawLine(ctx.Pen(Ctx.Colors.TableBorder, MathF.Max(1f, Ctx.Scale)), Left + 1, lineY, Left + Width - 1, lineY);
            }

            rowTop += rowHeight;
        }

        // cells
        float x = x0;
        for (int c = 0; c < ColumnWidths.Length; c++)
        {
            float cellWidth = ColumnWidths[c];
            DrawCell(ctx, Headers[c], x, Top, cellWidth, HeaderHeight, bold: true);
            x += cellWidth;
        }

        rowTop = headerBottom;
        for (int r = 0; r < Rows.Length; r++)
        {
            x = x0;
            for (int c = 0; c < ColumnWidths.Length; c++)
            {
                DrawCell(ctx, Rows[r][c], x, rowTop, ColumnWidths[c], RowHeights[r], bold: false);
                x += ColumnWidths[c];
            }

            rowTop += RowHeights[r];
        }
    }

    private void DrawCell(PaintContext ctx, TableCellVisual cell, float x, float y, float width, float height, bool bold)
    {
        float contentWidth = width - Ctx.Px(CellPadX * 2f);
        float textHeight = cell.Text.Lines.Sum(l => l.Height);
        float offsetY = y + MathF.Max(Ctx.Px(CellPadY), (height - textHeight) / 2f);

        CellAlign align = cell.EffectiveAlign;
        float lineTop = offsetY;
        foreach (ShapedLine line in cell.Text.Lines)
        {
            float dx = align switch
            {
                CellAlign.Center => x + (width - line.Width) / 2f,
                CellAlign.Right => x + width - Ctx.Px(CellPadX) - line.Width,
                _ => x + Ctx.Px(CellPadX),
            };

            foreach (Run run in line.Runs)
            {
                ctx.DrawRun(run, dx, lineTop);
            }

            lineTop += line.Height;
        }

        _ = !bold || contentWidth >= 0;
    }

    public override void CollectLinks(List<LinkRegion> regions)
    {
        float y = Top + HeaderHeight;
        for (int r = 0; r < Rows.Length; r++)
        {
            float x = Left;
            for (int c = 0; c < ColumnWidths.Length; c++)
            {
                float lineTop = y + Ctx.Px(CellPadY);
                foreach (ShapedLine line in Rows[r][c].Text.Lines)
                {
                    foreach (Run run in line.Runs)
                    {
                        if (run.Style.LinkUrl is { Length: > 0 } url)
                        {
                            regions.Add(new LinkRegion
                            {
                                Bounds = new RectangleF(x + Ctx.Px(CellPadX) + run.X, lineTop, MathF.Max(2f, run.Width), line.Height),
                                Target = url,
                                IsAnchor = url.StartsWith('#'),
                                IsExternal = url.StartsWith("http", StringComparison.OrdinalIgnoreCase),
                                Missing = run.Style.MissingLink,
                            });
                        }
                    }

                    lineTop += line.Height;
                }

                x += ColumnWidths[c];
            }

            y += RowHeights[r];
        }
    }
}
