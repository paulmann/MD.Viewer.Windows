using System.Drawing;
using System.Drawing.Drawing2D;
using MdViewer.Core.Diagrams;
using MdViewer.Render.Rendering;
using MdViewer.Render.Text;

namespace MdViewer.Render.Layout;

/// <summary>Class diagram: layered boxes with member lists and typed relations.</summary>
public sealed class ClassVisual : BlockLayout
{
    private sealed class PlacedClass
    {
        public required ClassNode Node { get; init; }

        public RectangleF Box { get; set; }

        public List<(string Text, PointF Position, Color Color, Font Font)> Lines { get; } = [];
    }

    private sealed class PlacedRelation
    {
        public required ClassRelation Relation { get; init; }

        public PointF From { get; set; }

        public PointF To { get; set; }

        public PointF LabelPosition { get; set; }

        public float LabelWidth { get; set; }
    }

    private readonly List<PlacedClass> _classes = [];
    private readonly List<PlacedRelation> _relations = [];
    private Font _nameFont = null!;
    private Font _memberFont = null!;
    private Font _smallFont = null!;

    public required LayoutContext Ctx { get; init; }

    public required ClassDiagram Diagram { get; init; }

    public static ClassVisual? Create(LayoutContext ctx, MdViewer.Core.Markdown.MermaidBlock source, ClassDiagram diagram, float left, float top, float width)
    {
        var visual = new ClassVisual { Source = source, Ctx = ctx, Diagram = diagram };
        return visual.Build(left, top, width) ? visual : null;
    }

    private bool Build(float left, float top, float width)
    {
        float scale = Ctx.Scale;
        float baseSize = Ctx.Theme.Type.BaseSize;
        _nameFont = Ctx.Fonts.UiFont(baseSize * 0.88f * Ctx.Theme.Scale, FontStyle.Bold);
        _memberFont = Ctx.Fonts.MonoFont(baseSize * 0.72f * Ctx.Theme.Scale);
        _smallFont = Ctx.Fonts.UiFont(baseSize * 0.72f * Ctx.Theme.Scale);

        float padX = 10f * scale;
        float padY = 7f * scale;
        float lineHeight = Ctx.Fonts.Metrics(_memberFont).LineSpacing * 1.12f;
        float nameHeight = Ctx.Fonts.Metrics(_nameFont).LineSpacing + padY;

        foreach (ClassNode node in Diagram.Classes)
        {
            float contentWidth = Ctx.Fonts.Measure(node.Name, _nameFont);
            if (node.Annotation is { Length: > 0 })
            {
                contentWidth = MathF.Max(contentWidth, Ctx.Fonts.Measure("«" + node.Annotation + "»", _smallFont));
            }

            foreach (ClassMember member in node.Members)
            {
                contentWidth = MathF.Max(contentWidth, Ctx.Fonts.Measure(member.ToString(), _memberFont));
            }

            float annotationHeight = node.Annotation is { Length: > 0 } ? Ctx.Fonts.Metrics(_smallFont).LineSpacing : 0f;
            float membersHeight = node.Members.Count == 0 ? 0f : node.Members.Count * lineHeight + padY;
            var box = new RectangleF(0, 0, MathF.Min(contentWidth + padX * 2f, width - 8f * scale), nameHeight + annotationHeight + membersHeight + padY);

            var placed = new PlacedClass { Node = node, Box = box };
            _classes.Add(placed);
        }

        if (_classes.Count == 0)
        {
            return false;
        }

        // layers: parents above children
        var layer = _classes.ToDictionary(c => c.Node.Name, _ => 0, StringComparer.Ordinal);
        for (int pass = 0; pass < 40; pass++)
        {
            bool changed = false;
            foreach (ClassRelation relation in Diagram.Relations)
            {
                bool parentToChild = relation.Kind is RelationKind.Inheritance or RelationKind.Realization or RelationKind.Composition or RelationKind.Aggregation;
                string child = parentToChild ? relation.From : relation.To;
                string parent = parentToChild ? relation.To : relation.From;
                if (!layer.ContainsKey(child) || !layer.ContainsKey(parent))
                {
                    continue;
                }

                int wanted = layer[parent] + 1;
                if (layer[child] < wanted && wanted < 30)
                {
                    layer[child] = wanted;
                    changed = true;
                }
            }

            if (!changed)
            {
                break;
            }
        }

        int layerCount = layer.Values.DefaultIfEmpty(0).Max() + 1;
        var groups = new List<PlacedClass>[layerCount];
        for (int i = 0; i < layerCount; i++)
        {
            groups[i] = [];
        }

        foreach (PlacedClass placed in _classes)
        {
            groups[layer[placed.Node.Name]].Add(placed);
        }

        float gapAlong = 34f * scale;
        float gapAcross = 22f * scale;
        float along = 0f;
        float maxAcross = 0f;

        foreach (List<PlacedClass> group in groups)
        {
            float rowHeight = group.Max(c => c.Box.Height);
            float across = 0f;
            foreach (PlacedClass placed in group)
            {
                placed.Box = new RectangleF(across, along, placed.Box.Width, placed.Box.Height);
                across += placed.Box.Width + gapAcross;
            }

            maxAcross = MathF.Max(maxAcross, across - gapAcross);
            along += rowHeight + gapAlong;
        }

        along = MathF.Max(0f, along - gapAlong);

        foreach (List<PlacedClass> group in groups)
        {
            float extent = group.Max(c => c.Box.Right);
            float shift = (maxAcross - extent) / 2f;
            foreach (PlacedClass placed in group)
            {
                placed.Box = placed.Box with { X = placed.Box.X + shift };
            }
        }

        float fit = maxAcross > width ? width / maxAcross : 1f;
        float offsetX = left + MathF.Max(0f, (width - maxAcross * fit) / 2f);
        float offsetY = top + 6f * scale;

        foreach (PlacedClass placed in _classes)
        {
            RectangleF box = placed.Box;
            var scaled = new RectangleF(offsetX + box.X * fit, offsetY + box.Y * fit, box.Width * fit, box.Height * fit);
            placed.Box = scaled;

            float y = scaled.Top + padY;
            if (placed.Node.Annotation is { Length: > 0 } annotation)
            {
                string text = "«" + annotation + "»";
                float textWidth = Ctx.Fonts.Measure(text, _smallFont) * fit;
                placed.Lines.Add((text, new PointF(scaled.Left + (scaled.Width - textWidth) / 2f, y), Ctx.Colors.TextFaint, _smallFont));
                y += Ctx.Fonts.Metrics(_smallFont).LineSpacing * fit;
            }

            float nameWidth = Ctx.Fonts.Measure(placed.Node.Name, _nameFont) * fit;
            placed.Lines.Add((placed.Node.Name, new PointF(scaled.Left + (scaled.Width - nameWidth) / 2f, y), Ctx.Colors.Text, _nameFont));
            y += Ctx.Fonts.Metrics(_nameFont).LineSpacing * fit + padY * 0.5f;

            foreach (ClassMember member in placed.Node.Members)
            {
                string text = member.ToString();
                placed.Lines.Add((text, new PointF(scaled.Left + padX * fit, y), member.IsMethod ? Ctx.Colors.Function : Ctx.Colors.TextMuted, _memberFont));
                y += lineHeight * fit;
            }
        }

        foreach (ClassRelation relation in Diagram.Relations)
        {
            PlacedClass? from = _classes.FirstOrDefault(c => c.Node.Name == relation.From);
            PlacedClass? to = _classes.FirstOrDefault(c => c.Node.Name == relation.To);
            if (from is null || to is null)
            {
                continue;
            }

            PointF start = Center(from.Box);
            PointF end = Center(to.Box);
            var placed = new PlacedRelation
            {
                Relation = relation,
                From = Exit(from.Box, end),
                To = Exit(to.Box, start),
            };

            if (relation.Label is { Length: > 0 } label)
            {
                placed.LabelWidth = Ctx.Fonts.Measure(label, _smallFont);
                placed.LabelPosition = new PointF((placed.From.X + placed.To.X) / 2f - placed.LabelWidth / 2f, (placed.From.Y + placed.To.Y) / 2f - 14f * scale);
            }

            _relations.Add(placed);
        }

        Top = top;
        Left = left;
        Width = width;
        Height = offsetY - top + along * fit + 10f * scale;
        return true;
    }

    private static PointF Center(RectangleF box) => new(box.Left + box.Width / 2f, box.Top + box.Height / 2f);

    private static PointF Exit(RectangleF box, PointF target)
    {
        PointF center = Center(box);
        float dx = target.X - center.X;
        float dy = target.Y - center.Y;
        if (MathF.Abs(dx) < 0.01f && MathF.Abs(dy) < 0.01f)
        {
            return center;
        }

        float sx = MathF.Abs(dx) < 0.001f ? float.MaxValue : box.Width / 2f / MathF.Abs(dx);
        float sy = MathF.Abs(dy) < 0.001f ? float.MaxValue : box.Height / 2f / MathF.Abs(dy);
        float scale = MathF.Min(sx, sy);
        return new PointF(center.X + dx * scale, center.Y + dy * scale);
    }

    public override void Paint(PaintContext ctx)
    {
        float scale = Ctx.Scale;

        foreach (PlacedRelation relation in _relations)
        {
            RelationKind kind = relation.Relation.Kind;
            bool dashed = kind is RelationKind.Dependency or RelationKind.Realization;
            Color color = kind == RelationKind.Inheritance ? Ctx.Colors.BorderStrong : Ctx.Colors.TextMuted;

            using var pen = new Pen(color, MathF.Max(1f, 1.2f * scale)) { DashStyle = dashed ? DashStyle.Dash : DashStyle.Solid };
            ctx.G.DrawLine(pen, relation.From, relation.To);

            switch (kind)
            {
                case RelationKind.Inheritance:
                case RelationKind.Realization:
                    DrawTriangle(ctx, relation.From, relation.To, color, scale, filled: false);
                    break;

                case RelationKind.Composition:
                    DrawDiamond(ctx, relation.From, relation.To, color, scale, filled: true);
                    break;

                case RelationKind.Aggregation:
                    DrawDiamond(ctx, relation.From, relation.To, color, scale, filled: false);
                    break;

                case RelationKind.Dependency:
                case RelationKind.Association:
                    DiagramText.Arrow(ctx, relation.From, relation.To, color, scale, filled: false, open: true);
                    break;
            }

            if (relation.Relation.Label is { Length: > 0 } label)
            {
                ctx.FillRound(new RectangleF(relation.LabelPosition.X - 4f * scale, relation.LabelPosition.Y - 1f * scale, relation.LabelWidth + 8f * scale, 16f * scale), 3f * scale, Ctx.Colors.Surface);
                ctx.G.DrawString(label, _smallFont, ctx.Brush(Ctx.Colors.TextMuted), relation.LabelPosition.X, relation.LabelPosition.Y);
            }

            if (relation.Relation.FromCardinality is { Length: > 0 } fromCard)
            {
                ctx.G.DrawString(fromCard, _smallFont, ctx.Brush(Ctx.Colors.TextFaint), relation.From.X + 4f * scale, relation.From.Y + 2f * scale);
            }

            if (relation.Relation.ToCardinality is { Length: > 0 } toCard)
            {
                ctx.G.DrawString(toCard, _smallFont, ctx.Brush(Ctx.Colors.TextFaint), relation.To.X + 4f * scale, relation.To.Y + 2f * scale);
            }
        }

        foreach (PlacedClass placed in _classes)
        {
            RectangleF box = placed.Box;
            ctx.FillRound(box, 4f * scale, Ctx.Colors.Surface);
            ctx.StrokeRound(box, 4f * scale, Ctx.Colors.BorderStrong, MathF.Max(1f, 1.2f * scale));

            float nameBottom = box.Top + Ctx.Fonts.Metrics(_nameFont).LineSpacing + 6f * scale;
            if (placed.Node.Annotation is { Length: > 0 })
            {
                nameBottom += Ctx.Fonts.Metrics(_smallFont).LineSpacing;
            }

            using (var path = PaintContext.RoundedPath(box, 4f * scale))
            {
                var state = ctx.G.Save();
                ctx.G.SetClip(path);
                ctx.Fill(new RectangleF(box.Left, box.Top, box.Width, nameBottom - box.Top), Ctx.Colors.TableHeaderBg);
                ctx.G.Restore(state);
            }

            ctx.G.DrawLine(ctx.Pen(Ctx.Colors.Border, MathF.Max(1f, scale)), box.Left, nameBottom, box.Right, nameBottom);

            foreach ((string text, PointF position, Color color, Font font) in placed.Lines)
            {
                ctx.G.DrawString(text, font, ctx.Brush(color), position.X, position.Y);
            }
        }
    }

    private static void DrawTriangle(PaintContext ctx, PointF from, PointF to, Color color, float scale, bool filled)
    {
        float size = 10f * scale;
        float dx = to.X - from.X;
        float dy = to.Y - from.Y;
        float length = MathF.Sqrt(dx * dx + dy * dy);
        if (length < 1f)
        {
            return;
        }

        float ux = dx / length;
        float uy = dy / length;
        var tip = to;
        var left = new PointF(tip.X - ux * size - uy * size * 0.6f, tip.Y - uy * size + ux * size * 0.6f);
        var right = new PointF(tip.X - ux * size + uy * size * 0.6f, tip.Y - uy * size - ux * size * 0.6f);

        if (filled)
        {
            ctx.G.FillPolygon(ctx.Brush(color), [tip, left, right]);
            return;
        }

        ctx.G.FillPolygon(ctx.Brush(ctx.Colors.Surface), [tip, left, right]);
        ctx.G.DrawPolygon(ctx.Pen(color, MathF.Max(1f, 1.1f * scale)), [tip, left, right]);
    }

    private static void DrawDiamond(PaintContext ctx, PointF from, PointF to, Color color, float scale, bool filled)
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
        float size = 8f * scale;

        var tip = from;
        var back = new PointF(from.X + ux * size * 2f, from.Y + uy * size * 2f);
        var sideA = new PointF(from.X + ux * size - uy * size * 0.55f, from.Y + uy * size + ux * size * 0.55f);
        var sideB = new PointF(from.X + ux * size + uy * size * 0.55f, from.Y + uy * size - ux * size * 0.55f);

        PointF[] points = [tip, sideA, back, sideB];
        if (filled)
        {
            ctx.G.FillPolygon(ctx.Brush(color), points);
            return;
        }

        ctx.G.FillPolygon(ctx.Brush(ctx.Colors.Surface), points);
        ctx.G.DrawPolygon(ctx.Pen(color, MathF.Max(1f, 1.1f * scale)), points);
    }
}

