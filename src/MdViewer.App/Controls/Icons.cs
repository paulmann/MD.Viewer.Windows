using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MdViewer.App.Controls;

public enum IconKind
{
    Menu,
    Folder,
    Search,
    Sun,
    Moon,
    Auto,
    Close,
    Outline,
    Eye,
    Pencil,
    Text,
    Print,
    Copy,
    Refresh,
    More,
    Minus,
    Plus,
    Gear,
    Info,
    ChevronDown,
    ChevronUp,
    External,
    Check,
    Save,
}

/// <summary>Vector icons drawn with GDI+ — no image assets, crisp at any DPI.</summary>
internal static class Icons
{
    public static void Draw(Graphics g, IconKind kind, RectangleF box, Color color, float stroke)
    {
        using var pen = new Pen(color, stroke) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        SolidBrush brush = new(color);
        try
        {
            float x = box.Left;
            float y = box.Top;
            float w = box.Width;
            float h = box.Height;
            float cx = x + w / 2f;
            float cy = y + h / 2f;

            switch (kind)
            {
                case IconKind.Menu:
                    g.DrawLine(pen, x + w * 0.18f, y + h * 0.3f, x + w * 0.82f, y + h * 0.3f);
                    g.DrawLine(pen, x + w * 0.18f, y + h * 0.5f, x + w * 0.82f, y + h * 0.5f);
                    g.DrawLine(pen, x + w * 0.18f, y + h * 0.7f, x + w * 0.82f, y + h * 0.7f);
                    break;

                case IconKind.Folder:
                    g.DrawLines(pen, [
                        new PointF(x + w * 0.16f, y + h * 0.74f),
                        new PointF(x + w * 0.16f, y + h * 0.3f),
                        new PointF(x + w * 0.44f, y + h * 0.3f),
                        new PointF(x + w * 0.52f, y + h * 0.4f),
                        new PointF(x + w * 0.84f, y + h * 0.4f),
                        new PointF(x + w * 0.84f, y + h * 0.74f),
                        new PointF(x + w * 0.16f, y + h * 0.74f),
                    ]);
                    break;

                case IconKind.Search:
                    g.DrawEllipse(pen, x + w * 0.2f, y + h * 0.2f, w * 0.44f, h * 0.44f);
                    g.DrawLine(pen, x + w * 0.62f, y + h * 0.62f, x + w * 0.82f, y + h * 0.82f);
                    break;

                case IconKind.Sun:
                    g.DrawEllipse(pen, cx - w * 0.17f, cy - h * 0.17f, w * 0.34f, h * 0.34f);
                    for (int i = 0; i < 8; i++)
                    {
                        double angle = i * Math.PI / 4;
                        float r1 = w * 0.29f;
                        float r2 = w * 0.4f;
                        g.DrawLine(pen,
                            cx + (float)Math.Cos(angle) * r1, cy + (float)Math.Sin(angle) * r1,
                            cx + (float)Math.Cos(angle) * r2, cy + (float)Math.Sin(angle) * r2);
                    }

                    break;

                case IconKind.Moon:
                    using (var path = new GraphicsPath())
                    {
                        path.AddArc(x + w * 0.2f, y + h * 0.18f, w * 0.6f, h * 0.64f, 60, 250);
                        path.AddArc(x + w * 0.34f, y + h * 0.1f, w * 0.5f, h * 0.62f, 150, -140);
                        path.CloseFigure();
                        g.FillPath(brush, path);
                    }

                    break;

                case IconKind.Auto:
                    using (var path = new GraphicsPath())
                    {
                        path.AddArc(x + w * 0.2f, y + h * 0.2f, w * 0.6f, h * 0.6f, 90, 180);
                        path.AddLine(x + w * 0.5f, y + h * 0.2f, x + w * 0.5f, y + h * 0.8f);
                        path.CloseFigure();
                        g.FillPath(brush, path);
                    }

                    g.DrawEllipse(pen, x + w * 0.2f, y + h * 0.2f, w * 0.6f, h * 0.6f);
                    break;

                case IconKind.Close:
                    g.DrawLine(pen, x + w * 0.3f, y + h * 0.3f, x + w * 0.7f, y + h * 0.7f);
                    g.DrawLine(pen, x + w * 0.7f, y + h * 0.3f, x + w * 0.3f, y + h * 0.7f);
                    break;

                case IconKind.Outline:
                    g.DrawLine(pen, x + w * 0.18f, y + h * 0.28f, x + w * 0.82f, y + h * 0.28f);
                    g.DrawLine(pen, x + w * 0.34f, y + h * 0.5f, x + w * 0.82f, y + h * 0.5f);
                    g.DrawLine(pen, x + w * 0.34f, y + h * 0.72f, x + w * 0.82f, y + h * 0.72f);
                    g.FillEllipse(brush, x + w * 0.18f, y + h * 0.45f, w * 0.08f, h * 0.09f);
                    g.FillEllipse(brush, x + w * 0.18f, y + h * 0.67f, w * 0.08f, h * 0.09f);
                    break;

                case IconKind.Eye:
                    using (var path = new GraphicsPath())
                    {
                        path.AddBezier(x + w * 0.14f, cy, x + w * 0.34f, y + h * 0.28f, x + w * 0.66f, y + h * 0.28f, x + w * 0.86f, cy);
                        path.AddBezier(x + w * 0.86f, cy, x + w * 0.66f, y + h * 0.72f, x + w * 0.34f, y + h * 0.72f, x + w * 0.14f, cy);
                        g.DrawPath(pen, path);
                    }

                    g.DrawEllipse(pen, cx - w * 0.11f, cy - h * 0.11f, w * 0.22f, h * 0.22f);
                    break;

                case IconKind.Pencil:
                    g.DrawLines(pen, [
                        new PointF(x + w * 0.24f, y + h * 0.76f),
                        new PointF(x + w * 0.3f, y + h * 0.58f),
                        new PointF(x + w * 0.68f, y + h * 0.2f),
                        new PointF(x + w * 0.8f, y + h * 0.32f),
                        new PointF(x + w * 0.42f, y + h * 0.7f),
                        new PointF(x + w * 0.24f, y + h * 0.76f),
                    ]);
                    break;

                case IconKind.Text:
                    g.DrawLine(pen, x + w * 0.22f, y + h * 0.26f, x + w * 0.78f, y + h * 0.26f);
                    g.DrawLine(pen, cx, y + h * 0.26f, cx, y + h * 0.76f);
                    g.DrawLine(pen, x + w * 0.38f, y + h * 0.76f, x + w * 0.62f, y + h * 0.76f);
                    break;

                case IconKind.Print:
                    g.DrawRectangle(pen, x + w * 0.22f, y + h * 0.4f, w * 0.56f, h * 0.3f);
                    g.DrawRectangle(pen, x + w * 0.32f, y + h * 0.2f, w * 0.36f, h * 0.2f);
                    g.DrawRectangle(pen, x + w * 0.32f, y + h * 0.62f, w * 0.36f, h * 0.2f);
                    break;

                case IconKind.Copy:
                    g.DrawRectangle(pen, x + w * 0.2f, y + h * 0.2f, w * 0.44f, h * 0.44f);
                    g.DrawLines(pen, [
                        new PointF(x + w * 0.4f, y + h * 0.72f),
                        new PointF(x + w * 0.8f, y + h * 0.72f),
                        new PointF(x + w * 0.8f, y + h * 0.36f),
                    ]);
                    break;

                case IconKind.Refresh:
                    g.DrawArc(pen, x + w * 0.22f, y + h * 0.22f, w * 0.56f, h * 0.56f, 40, 280);
                    g.DrawLines(pen, [
                        new PointF(x + w * 0.66f, y + h * 0.16f),
                        new PointF(x + w * 0.78f, y + h * 0.3f),
                        new PointF(x + w * 0.62f, y + h * 0.34f),
                    ]);
                    break;

                case IconKind.More:
                    g.FillEllipse(brush, cx - w * 0.3f, cy - h * 0.06f, w * 0.11f, h * 0.11f);
                    g.FillEllipse(brush, cx - w * 0.05f, cy - h * 0.06f, w * 0.11f, h * 0.11f);
                    g.FillEllipse(brush, cx + w * 0.2f, cy - h * 0.06f, w * 0.11f, h * 0.11f);
                    break;

                case IconKind.Minus:
                    g.DrawLine(pen, x + w * 0.24f, cy, x + w * 0.76f, cy);
                    break;

                case IconKind.Plus:
                    g.DrawLine(pen, x + w * 0.24f, cy, x + w * 0.76f, cy);
                    g.DrawLine(pen, cx, y + h * 0.24f, cx, y + h * 0.76f);
                    break;

                case IconKind.Gear:
                    g.DrawEllipse(pen, cx - w * 0.14f, cy - h * 0.14f, w * 0.28f, h * 0.28f);
                    for (int i = 0; i < 8; i++)
                    {
                        double angle = i * Math.PI / 4;
                        float r1 = w * 0.24f;
                        float r2 = w * 0.38f;
                        g.DrawLine(pen,
                            cx + (float)Math.Cos(angle) * r1, cy + (float)Math.Sin(angle) * r1,
                            cx + (float)Math.Cos(angle) * r2, cy + (float)Math.Sin(angle) * r2);
                    }

                    break;

                case IconKind.Info:
                    g.DrawEllipse(pen, cx - w * 0.34f, cy - h * 0.34f, w * 0.68f, h * 0.68f);
                    g.DrawLine(pen, cx, cy - h * 0.02f, cx, cy + h * 0.22f);
                    g.FillEllipse(brush, cx - w * 0.045f, cy - h * 0.2f, w * 0.09f, h * 0.09f);
                    break;

                case IconKind.ChevronDown:
                    g.DrawLines(pen, [
                        new PointF(x + w * 0.28f, y + h * 0.4f),
                        new PointF(cx, y + h * 0.62f),
                        new PointF(x + w * 0.72f, y + h * 0.4f),
                    ]);
                    break;

                case IconKind.ChevronUp:
                    g.DrawLines(pen, [
                        new PointF(x + w * 0.28f, y + h * 0.6f),
                        new PointF(cx, y + h * 0.38f),
                        new PointF(x + w * 0.72f, y + h * 0.6f),
                    ]);
                    break;

                case IconKind.External:
                    g.DrawLines(pen, [
                        new PointF(x + w * 0.24f, y + h * 0.76f),
                        new PointF(x + w * 0.24f, y + h * 0.24f),
                        new PointF(x + w * 0.6f, y + h * 0.24f),
                    ]);
                    g.DrawLine(pen, x + w * 0.46f, y + h * 0.54f, x + w * 0.78f, y + h * 0.22f);
                    g.DrawLine(pen, x + w * 0.6f, y + h * 0.22f, x + w * 0.78f, y + h * 0.22f);
                    g.DrawLine(pen, x + w * 0.78f, y + h * 0.22f, x + w * 0.78f, y + h * 0.4f);
                    break;

                case IconKind.Check:
                    g.DrawLines(pen, [
                        new PointF(x + w * 0.24f, y + h * 0.52f),
                        new PointF(x + w * 0.43f, y + h * 0.72f),
                        new PointF(x + w * 0.78f, y + h * 0.28f),
                    ]);
                    break;

                case IconKind.Save:
                    g.DrawRectangle(pen, x + w * 0.2f, y + h * 0.2f, w * 0.6f, h * 0.6f);
                    g.DrawRectangle(pen, x + w * 0.34f, y + h * 0.2f, w * 0.3f, h * 0.22f);
                    g.DrawRectangle(pen, x + w * 0.34f, y + h * 0.56f, w * 0.32f, h * 0.24f);
                    break;
            }
        }
        finally
        {
            brush.Dispose();
        }
    }
}
