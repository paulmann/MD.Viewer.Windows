using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MdViewer.App;

/// <summary>
/// The payload of a dragged tab. A tab can be dropped on another MD Viewer window
/// (it becomes a tab there), on this window (nothing happens) or anywhere else —
/// which opens the document in a new window on the screen where it was dropped.
/// </summary>
public static class TabDrag
{
    /// <summary>Clipboard format carrying <c>sourceHandle\npath</c>.</summary>
    public const string Format = "MDViewer.Tab";

    /// <summary>Prefix used when the payload travels as plain text (different process, no format).</summary>
    public const string TextPrefix = "MDViewer.Tab:";

    public static DataObject Create(string path, long sourceHandle)
    {
        var data = new DataObject();
        data.SetData(Format, sourceHandle.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n" + path);
        // A custom format crosses process boundaries as an OLE blob, and plain text is also
        // readable for a human (and for debugging), so the same payload travels both ways.
        data.SetData(DataFormats.UnicodeText,
            TextPrefix + sourceHandle.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + path);
        return data;
    }

    public static bool TryRead(IDataObject? data, out string path, out long sourceHandle)
    {
        path = string.Empty;
        sourceHandle = 0;
        if (data is null)
        {
            return false;
        }

        if (data.GetDataPresent(Format) && TryParsePayload(ToText(data.GetData(Format)), out path, out sourceHandle))
        {
            return true;
        }

        return data.GetDataPresent(DataFormats.UnicodeText)
            && TryParseText(ToText(data.GetData(DataFormats.UnicodeText)), out path, out sourceHandle);
    }

    /// <summary>The custom format: <c>handle\npath</c>.</summary>
    private static bool TryParsePayload(string? payload, out string path, out long sourceHandle)
    {
        path = string.Empty;
        sourceHandle = 0;
        if (payload is null)
        {
            return false;
        }

        int split = payload.IndexOf('\n');
        if (split <= 0)
        {
            return false;
        }

        if (!long.TryParse(payload[..split], System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out sourceHandle))
        {
            return false;
        }

        path = payload[(split + 1)..];
        return path.Length > 0;
    }

    /// <summary>The text form: <c>MDViewer.Tab:handle:path</c>.</summary>
    private static bool TryParseText(string? text, out string path, out long sourceHandle)
    {
        path = string.Empty;
        sourceHandle = 0;
        if (text is null || !text.StartsWith(TextPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        string rest = text[TextPrefix.Length..];
        int split = rest.IndexOf(':');
        if (split <= 0)
        {
            return false;
        }

        if (!long.TryParse(rest[..split], System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out sourceHandle))
        {
            return false;
        }

        path = rest[(split + 1)..];
        return path.Length > 0;
    }

    /// <summary>OLE hands custom formats over as a string, a byte array or a stream.</summary>
    private static string? ToText(object? raw) => raw switch
    {
        null => null,
        string text => text,
        string[] { Length: > 0 } many => many[0],
        byte[] bytes => Encoding.UTF8.GetString(bytes),
        MemoryStream stream => Encoding.UTF8.GetString(stream.ToArray()),
        _ => raw.ToString(),
    };

    /// <summary>
    /// What a window does with a dropped tab: take it (Move → the source closes its tab),
    /// keep everything as it is (Link → the source keeps it), or refuse (None).
    /// </summary>
    public static DragDropEffects Decide(bool fromThisWindow, bool alreadyOpenHere) => fromThisWindow
        ? (alreadyOpenHere ? DragDropEffects.Link : DragDropEffects.None)
        : DragDropEffects.Move;

    /// <summary>
    /// Where the File menu may sit: below the anchor, flipped above when it would leave
    /// the screen, always inside <paramref name="workingArea"/>.
    /// </summary>
    public static Rectangle FitMenu(Rectangle anchor, Size menu, Rectangle workingArea)
    {
        int x = anchor.Left;
        int y = anchor.Bottom;

        if (x + menu.Width > workingArea.Right)
        {
            x = Math.Max(workingArea.Left, workingArea.Right - menu.Width);
        }

        if (y + menu.Height > workingArea.Bottom)
        {
            y = Math.Max(workingArea.Top, anchor.Top - menu.Height);
        }

        return new Rectangle(x, y, menu.Width, menu.Height);
    }
}
