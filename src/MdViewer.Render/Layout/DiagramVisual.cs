using System.Drawing;
using System.Drawing.Drawing2D;
using MdViewer.Core.Diagrams;
using MdViewer.Render.Rendering;
using MdViewer.Render.Text;

namespace MdViewer.Render.Layout;

/// <summary>Flowchart drawn natively (no browser): layered layout, boxes, arrows.</summary>
public sealed class DiagramVisual : BlockLayout
{
    private sealed class PlacedNode
    {
        public required FlowNode Node { get; init; }

        public RectangleF Box { get; set; }

        public List<(string Text, PointF Position, float Width)> Lines { get; } = [];
    }

    private sealed class PlacedEdge
    {
        public required FlowEdge Edge { get; init; }

        public PointF From { get; set; }

        public PointF To { get; set; }

        public RectangleF LabelBox { get; set; }

        public bool HasLabel { get; set; }
    }

    private readonly List<PlacedNode> _nodes = [];
    private readonly List<PlacedEdge> _edges = [];
    private float _fontSize;

    public required LayoutContext Ctx { get; init; }

    public required Flowchart Chart { get; init; }

    public static DiagramVisual? Create(LayoutContext ctx, Flowchart chart, float left, float top, float width)
    {
        var visual = new DiagramVisual { Source = new MdViewer.Core.Markdown.MermaidBlock { Source = string.Empty }, Ctx = ctx, Chart = chart };
        if (!visual.Build(left, top, width))
        {
            return null;
        }

        return visual;
    }

    private bool Build(float left, float top, float width)
    {
        float scale = Ctx.Scale;
        _fontSize = Ctx.Theme.Type.BaseSize * 0.82f * Ctx.Theme.Scale;
        Font font = Ctx.Fonts.UiFont(_fontSize);
        FontLineMetrics metrics = Ctx.Fonts.Metrics(font);
        float lineHeight = metrics.LineSpacing * 1.15f;
        float padX = 14f * scale;
        float padY = 9f * scale;

        // ---- measure nodes
        foreach (FlowNode node in Chart.Nodes)
        {
            string[] lines = node.Label.Split('\n');
            float textWidth = lines.Max(l => Ctx.Fonts.Measure(l, font));
            float nodeHeight = lines.Length * lineHeight + padY * 2f;
            float boxWidth = textWidth + padX * 2f;

            switch (node.Shape)
            {
                case NodeShape.Diamond:
                    boxWidth = textWidth * 1.5f + padX * 2f;
                    nodeHeight *= 1.5f;
                    break;
                case NodeShape.Circle:
                    float diameter = MathF.Max(boxWidth, nodeHeight) + 10f * scale;
                    boxWidth = diameter;
                    nodeHeight = diameter;
                    break;
                case NodeShape.Stadium:
                    boxWidth += 10f * scale;
                    break;
            }

            var placed = new PlacedNode { Node = node };
            placed.Box = new RectangleF(0, 0, MathF.Max(boxWidth, 46f * scale), MathF.Max(nodeHeight, 30f * scale));
            _nodes.Add(placed);
        }

        // ---- layers (longest path)
        var incoming = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (PlacedNode node in _nodes)
        {
            incoming[node.Node.Id] = 0;
        }

        foreach (FlowEdge edge in Chart.Edges)
        {
            incoming[edge.To] = incoming.GetValueOrDefault(edge.To) + 1;
        }

        var layer = _nodes.ToDictionary(n => n.Node.Id, _ => 0, StringComparer.Ordinal);
        var queue = new Queue<string>(_nodes.Where(n => incoming[n.Node.Id] == 0).Select(n => n.Node.Id));
        var visited = new HashSet<string>(StringComparer.Ordinal);

        while (queue.Count > 0)
        {
            string id = queue.Dequeue();
            if (!visited.Add(id))
            {
                continue;
            }

            foreach (FlowEdge edge in Chart.Edges.Where(e => e.From == id))
            {
                layer[edge.To] = Math.Max(layer.GetValueOrDefault(edge.To), layer[id] + 1);
                queue.Enqueue(edge.To);
            }
        }

        foreach (PlacedNode node in _nodes)
        {
            if (!visited.Contains(node.Node.Id))
            {
                layer[node.Node.Id] = 0;
            }
        }

        // ---- group
        int layerCount = layer.Values.DefaultIfEmpty(0).Max() + 1;
        var groups = new List<PlacedNode>[layerCount];
        for (int i = 0; i < layerCount; i++)
        {
            groups[i] = [];
        }

        foreach (PlacedNode node in _nodes)
        {
            groups[layer[node.Node.Id]].Add(node);
        }

        bool horizontal = Chart.LeftToRight;
        float gapAlong = 34f * scale;
        float gapAcross = 26f * scale;
        float along = 0f;
        float maxAcross = 0f;

        foreach (List<PlacedNode> group in groups)
        {
            float groupThickness = group.Max(n => horizontal ? n.Box.Width : n.Box.Height);
            float across = 0f;
            foreach (PlacedNode node in group)
            {
                if (horizontal)
                {
                    node.Box = new RectangleF(along, across, node.Box.Width, node.Box.Height);
                    across += node.Box.Height + gapAcross;
                }
                else
                {
                    node.Box = new RectangleF(across, along, node.Box.Width, node.Box.Height);
                    across += node.Box.Width + gapAcross;
                }
            }

            across = MathF.Max(0f, across - gapAcross);
            maxAcross = MathF.Max(maxAcross, across);
            along += groupThickness + gapAlong;
        }

        along = MathF.Max(0f, along - gapAlong);

        // centre each layer across the diagram
        foreach (List<PlacedNode> group in groups)
        {
            float extent = 0f;
            foreach (PlacedNode node in group)
            {
                extent = MathF.Max(extent, horizontal ? node.Box.Bottom : node.Box.Right);
            }

            float shift = (maxAcross - extent) / 2f;
            foreach (PlacedNode node in group)
            {
                node.Box = horizontal
                    ? node.Box with { Y = node.Box.Y + shift }
                    : node.Box with { X = node.Box.X + shift };
            }
        }

        float diagramWidth = horizontal ? along : maxAcross;
        float diagramHeight = horizontal ? maxAcross : along;

        // fit into the column
        float available = width;
        float fit = diagramWidth > available ? available / diagramWidth : 1f;
        fit = MathF.Min(fit, 1f);

        float offsetX = left + MathF.Max(0f, (available - diagramWidth * fit) / 2f);
        float offsetY = top + 8f * scale;

        foreach (PlacedNode node in _nodes)
        {
            RectangleF box = node.Box;
            var scaled = new RectangleF(
                offsetX + box.X * fit,
                offsetY + box.Y * fit,
                box.Width * fit,
                box.Height * fit);
            node.Box = scaled;

            string[] lines = node.Node.Label.Split('\n');
            float totalText = lines.Length * lineHeight * fit;
            float lineTop = scaled.Top + (scaled.Height - totalText) / 2f;
            foreach (string line in lines)
            {
                float textWidth = Ctx.Fonts.Measure(line, font) * fit;
                node.Lines.Add((line, new PointF(scaled.Left + (scaled.Width - textWidth) / 2f, lineTop), textWidth));
                lineTop += lineHeight * fit;
            }
        }

        // ---- edges
        foreach (FlowEdge edge in Chart.Edges)
        {
            PlacedNode? from = _nodes.FirstOrDefault(n => n.Node.Id == edge.From);
            PlacedNode? to = _nodes.FirstOrDefault(n => n.Node.Id == edge.To);
            if (from is null || to is null)
            {
                continue;
            }

            PointF start = Center(from.Box);
            PointF end = Center(to.Box);
            PointF boundaryStart = ExitPoint(from.Box, end);
            PointF boundaryEnd = ExitPoint(to.Box, start);

            var placed = new PlacedEdge
            {
                Edge = edge,
                From = boundaryStart,
                To = boundaryEnd,
            };

            if (edge.Label is { Length: > 0 })
            {
                float labelWidth = Ctx.Fonts.Measure(edge.Label, font) * fit;
                float midX = (boundaryStart.X + boundaryEnd.X) / 2f;
                float midY = (boundaryStart.Y + boundaryEnd.Y) / 2f;
                placed.LabelBox = new RectangleF(midX - labelWidth / 2f - 4f, midY - lineHeight * 0.7f, labelWidth + 8f, lineHeight * 1.1f);
                placed.HasLabel = true;
            }

            _edges.Add(placed);
        }

        float height = diagramHeight * fit + 26f * scale;
        Left = left;
        Top = top;
        Width = width;
        Height = MathF.Max(60f * scale, height);
        return true;
    }

    private static PointF Center(RectangleF box) => new(box.Left + box.Width / 2f, box.Top + box.Height / 2f);

    /// <summary>Point where the segment towards <paramref name="target"/> leaves the box.</summary>
    private static PointF ExitPoint(RectangleF box, PointF target)
    {
        PointF center = Center(box);
        float dx = target.X - center.X;
        float dy = target.Y - center.Y;
        if (MathF.Abs(dx) < 0.01f && MathF.Abs(dy) < 0.01f)
        {
            return center;
        }

        float halfWidth = box.Width / 2f;
        float halfHeight = box.Height / 2f;
        float scaleX = MathF.Abs(dx) < 0.001f ? float.MaxValue : halfWidth / MathF.Abs(dx);
        float scaleY = MathF.Abs(dy) < 0.001f ? float.MaxValue : halfHeight / MathF.Abs(dy);
        float scale = MathF.Min(scaleX, scaleY);
        return new PointF(center.X + dx * scale, center.Y + dy * scale);
    }

    public override void Paint(PaintContext ctx)
    {
        float scale = Ctx.Scale;
        Font font = ctx.Fonts.UiFont(_fontSize);

        // edges first so boxes cover their ends
        foreach (PlacedEdge edge in _edges)
        {
            Color color = edge.Edge.Dashed ? Ctx.Colors.TextFaint : Ctx.Colors.BorderStrong;
            Pen pen = ctx.Pen(color, MathF.Max(1.1f, 1.25f * scale));
            if (edge.Edge.Dashed)
            {
                pen.DashStyle = DashStyle.Dash;
            }

            float midY = (edge.From.Y + edge.To.Y) / 2f;
            using var path = new GraphicsPath();
            path.AddBezier(edge.From, new PointF(edge.From.X, midY), new PointF(edge.To.X, midY), edge.To);
            ctx.G.DrawPath(pen, path);
            pen.DashStyle = DashStyle.Solid;

            DrawArrow(ctx, edge.From, edge.To, color, scale);

            if (edge.HasLabel && edge.Edge.Label is { Length: > 0 })
            {
                ctx.FillRound(edge.LabelBox, 4f * scale, Ctx.Colors.Surface);
                ctx.G.DrawString(edge.Edge.Label, font, ctx.Brush(Ctx.Colors.TextMuted), edge.LabelBox.Left + 4f, edge.LabelBox.Top);
            }
        }

        foreach (PlacedNode node in _nodes)
        {
            PaintNode(ctx, node, scale);
            foreach ((string text, PointF position, _) in node.Lines)
            {
                ctx.G.DrawString(text, font, ctx.Brush(Ctx.Colors.Text), position.X, position.Y);
            }
        }
    }

    private void PaintNode(PaintContext ctx, PlacedNode node, float scale)
    {
        RectangleF box = node.Box;
        Color fill = Ctx.Colors.Surface;
        Color border = Ctx.Colors.BorderStrong;

        switch (node.Node.Shape)
        {
            case NodeShape.Diamond:
            {
                var points = new[]
                {
                    new PointF(box.Left + box.Width / 2f, box.Top),
                    new PointF(box.Right, box.Top + box.Height / 2f),
                    new PointF(box.Left + box.Width / 2f, box.Bottom),
                    new PointF(box.Left, box.Top + box.Height / 2f),
                };
                ctx.G.FillPolygon(ctx.Brush(fill), points);
                ctx.G.DrawPolygon(ctx.Pen(border, MathF.Max(1f, 1.2f * scale)), points);
                break;
            }

            case NodeShape.Circle:
                ctx.G.FillEllipse(ctx.Brush(fill), box);
                ctx.G.DrawEllipse(ctx.Pen(border, MathF.Max(1f, 1.2f * scale)), box);
                break;

            case NodeShape.Rectangle:
                ctx.FillRound(box, 3f * scale, fill);
                ctx.StrokeRound(box, 3f * scale, border, MathF.Max(1f, 1.2f * scale));
                break;

            default:
                ctx.FillRound(box, MathF.Min(12f * scale, box.Height / 2f), fill);
                ctx.StrokeRound(box, MathF.Min(12f * scale, box.Height / 2f), border, MathF.Max(1f, 1.2f * scale));
                break;
        }
    }

    private void DrawArrow(PaintContext ctx, PointF from, PointF to, Color color, float scale)
    {
        float dx = to.X - from.X;
        float dy = to.Y - from.Y;
        float length = MathF.Sqrt(dx * dx + dy * dy);
        if (length < 1f)
        {
            return;
        }

        float ux = dx / length;
        float uy = dy / length;
        float size = 7f * scale;
        var tip = to;
        var left = new PointF(tip.X - ux * size - uy * size * 0.55f, tip.Y - uy * size + ux * size * 0.55f);
        var right = new PointF(tip.X - ux * size + uy * size * 0.55f, tip.Y - uy * size - ux * size * 0.55f);

        ctx.G.FillPolygon(ctx.Brush(color), [tip, left, right]);
    }
}

