using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace MdViewer.App;

/// <summary>
/// The application icon, drawn in code (no binary assets) in a 64×64 logical
/// space and scaled to whatever size is asked for.
/// </summary>
internal static class AppIconArt
{
    public const float LogicalSize = 64f;

    public static void Draw(Graphics g, float size)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.ScaleTransform(size / LogicalSize, size / LogicalSize);

        var sheet = new RectangleF(10, 6, 44, 52);
        using (var path = MdViewer.Render.Rendering.PaintContext.RoundedPath(sheet, 8f))
        using (var brush = new LinearGradientBrush(sheet, Color.FromArgb(0x2F, 0x6F, 0xEB), Color.FromArgb(0x1B, 0x4C, 0xB8), 60f))
        {
            g.FillPath(brush, path);
        }

        using (var white = new SolidBrush(Color.White))
        {
            g.FillRectangle(white, 18, 16, 28, 4);
            g.FillRectangle(white, 18, 25, 20, 4);
            g.FillRectangle(white, 18, 34, 24, 4);
        }

        using (var accent = new SolidBrush(Color.FromArgb(0xFF, 0xD1, 0x66)))
        {
            g.FillRectangle(accent, 18, 44, 14, 5);
        }

        using var pen = new Pen(Color.FromArgb(46, 0, 0, 0), 1.5f);
        g.DrawLine(pen, 46, 12, 46, 44);
    }

    public static Bitmap Render(int size)
    {
        var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.Transparent);
            Draw(g, size);
        }

        return bitmap;
    }
}

/// <summary>Window icon plus a multi-resolution .ico writer (16…256 px).</summary>
internal static partial class AppIcon
{
    public static Icon Create()
    {
        using Bitmap bitmap = AppIconArt.Render(64);
        IntPtr handle = bitmap.GetHicon();
        try
        {
            using Icon temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    /// <summary>Writes a Windows .ico with DIB entries up to 64 px and PNG for the large ones.</summary>
    public static void Save(string path, int[]? sizes = null)
    {
        int[] frames = sizes ?? [16, 20, 24, 32, 40, 48, 64, 128, 256];
        var images = new List<(int Size, byte[] Data, bool IsPng)>(frames.Length);

        foreach (int size in frames)
        {
            using Bitmap bitmap = AppIconArt.Render(size);
            images.Add(size >= 128
                ? (size, EncodePng(bitmap), true)
                : (size, EncodeDib(bitmap), false));
        }

        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream);

        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)images.Count);

        int offset = 6 + images.Count * 16;
        foreach ((int size, byte[] data, bool _) in images)
        {
            writer.Write((byte)(size >= 256 ? 0 : size));
            writer.Write((byte)(size >= 256 ? 0 : size));
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write(data.Length);
            writer.Write(offset);
            offset += data.Length;
        }

        foreach ((int _, byte[] data, bool __) in images)
        {
            writer.Write(data);
        }
    }

    private static byte[] EncodePng(Bitmap bitmap)
    {
        using var memory = new MemoryStream();
        bitmap.Save(memory, ImageFormat.Png);
        return memory.ToArray();
    }

    /// <summary>32-bit BGRA DIB with the AND mask, as classic .ico entries expect.</summary>
    private static byte[] EncodeDib(Bitmap bitmap)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;
        int maskStride = (width + 31) / 32 * 4;

        using var memory = new MemoryStream();
        using var writer = new BinaryWriter(memory);

        writer.Write(40);
        writer.Write(width);
        writer.Write(height * 2);
        writer.Write((ushort)1);
        writer.Write((ushort)32);
        writer.Write(0);
        writer.Write(width * height * 4 + maskStride * height);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);

        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = bitmap.GetPixel(x, y);
                writer.Write(pixel.B);
                writer.Write(pixel.G);
                writer.Write(pixel.R);
                writer.Write(pixel.A);
            }
        }

        for (int y = height - 1; y >= 0; y--)
        {
            var row = new byte[maskStride];
            for (int x = 0; x < width; x++)
            {
                if (bitmap.GetPixel(x, y).A < 128)
                {
                    row[x / 8] |= (byte)(1 << (7 - x % 8));
                }
            }

            writer.Write(row);
        }

        return memory.ToArray();
    }

    [System.Runtime.InteropServices.LibraryImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static partial bool DestroyIcon(IntPtr handle);
}
