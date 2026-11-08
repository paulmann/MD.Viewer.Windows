using System.Drawing;
using System.Runtime.InteropServices;

namespace MdViewer.App;

/// <summary>
/// Captures the live window with <c>PrintWindow</c>. This is the only reliable
/// way to screenshot double-buffered custom controls, and it is what the
/// development screenshot suite uses.
/// </summary>
public static partial class WindowCapture
{
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);

    public static void Save(Form form, string path, bool clientOnly = false)
    {
        Size size = clientOnly ? form.ClientSize : form.Size;
        using var bitmap = new Bitmap(Math.Max(1, size.Width), Math.Max(1, size.Height));
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            IntPtr hdc = graphics.GetHdc();
            try
            {
                PrintWindow(form.Handle, hdc, clientOnly ? 1u : 2u);
            }
            finally
            {
                graphics.ReleaseHdc(hdc);
            }
        }

        bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
    }
}


