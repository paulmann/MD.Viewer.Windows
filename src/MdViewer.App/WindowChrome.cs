using System.Drawing;
using System.Runtime.InteropServices;

namespace MdViewer.App;

/// <summary>Native window polish: dark title bar, rounded corners, taskbar name.</summary>
internal static partial class WindowChrome
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwaBorderColor = 34;
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;

    private const int DwmwcpRound = 2;

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    public static void Apply(IntPtr handle, bool dark, Color caption, Color text)
    {
        if (handle == IntPtr.Zero)
        {
            return;
        }

        try
        {
            int darkMode = dark ? 1 : 0;
            DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref darkMode, sizeof(int));

            int corners = DwmwcpRound;
            DwmSetWindowAttribute(handle, DwmwaWindowCornerPreference, ref corners, sizeof(int));

            int border = ToColorRef(caption);
            DwmSetWindowAttribute(handle, DwmwaBorderColor, ref border, sizeof(int));

            int captionColor = ToColorRef(caption);
            DwmSetWindowAttribute(handle, DwmwaCaptionColor, ref captionColor, sizeof(int));

            int textColor = ToColorRef(text);
            DwmSetWindowAttribute(handle, DwmwaTextColor, ref textColor, sizeof(int));
        }
        catch (DllNotFoundException)
        {
            // Windows 10 pre-1809: no dark title bar support, carry on
        }
        catch (EntryPointNotFoundException)
        {
            // older dwmapi: ignore
        }
    }

    private static int ToColorRef(Color color) => color.R | (color.G << 8) | (color.B << 16);

    [LibraryImport("user32.dll")]
    private static partial IntPtr SendMessageW(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private const int WmSetRedraw = 0x000B;

    public static void SuspendRedraw(IntPtr handle)
    {
        if (handle != IntPtr.Zero)
        {
            SendMessageW(handle, WmSetRedraw, IntPtr.Zero, IntPtr.Zero);
        }
    }

    public static void ResumeRedraw(IntPtr handle)
    {
        if (handle != IntPtr.Zero)
        {
            SendMessageW(handle, WmSetRedraw, new IntPtr(1), IntPtr.Zero);
            InvalidateRect(handle, IntPtr.Zero, true);
        }
    }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool InvalidateRect(IntPtr hWnd, IntPtr rect, [MarshalAs(UnmanagedType.Bool)] bool erase);
}
