using System.Drawing;
using System.Drawing.Imaging;

namespace MdViewer.Render.Media;

/// <summary>A resolved image: bitmap when loadable, otherwise a diagnostic.</summary>
public sealed class ImageAsset
{
    public Bitmap? Bitmap { get; init; }

    public Size PixelSize { get; init; }

    public string? Error { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public bool IsUsable => Bitmap is not null;
}

/// <summary>
/// Loads local images relative to the document (and remote ones never — the
/// viewer stays offline). Bitmaps are cached by path + write time so that live
/// reloads pick up replaced files.
/// </summary>
public sealed class ImageCache : IDisposable
{
    private readonly Dictionary<string, (DateTime Stamp, ImageAsset Asset)> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _warned = new(StringComparer.OrdinalIgnoreCase);

    public string? BaseDirectory { get; set; }

    public bool ShowMissingPlaceholder { get; set; } = true;

    public ImageAsset Resolve(string url, string? baseDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return new ImageAsset { Error = "empty image source" };
        }

        if (url.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            return ResolveDataUri(url);
        }

        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return new ImageAsset { Error = "remote image not loaded (offline viewer)", DisplayName = url };
        }

        string directory = baseDirectory ?? BaseDirectory ?? Directory.GetCurrentDirectory();
        string path;
        try
        {
            path = Path.GetFullPath(url.Replace('/', Path.DirectorySeparatorChar), directory);
        }
        catch (Exception ex)
        {
            return new ImageAsset { Error = ex.Message, DisplayName = url };
        }

        if (!File.Exists(path))
        {
            return new ImageAsset { Error = "file not found", DisplayName = Path.GetFileName(path) };
        }

        DateTime stamp;
        try
        {
            stamp = File.GetLastWriteTimeUtc(path);
        }
        catch
        {
            stamp = DateTime.MinValue;
        }

        if (_cache.TryGetValue(path, out var cached) && cached.Stamp == stamp)
        {
            return cached.Asset;
        }

        ImageAsset asset;
        try
        {
            // Read into memory so the file is never locked on disk.
            byte[] bytes = File.ReadAllBytes(path);
            using var stream = new MemoryStream(bytes);
            using var source = Image.FromStream(stream, useEmbeddedColorManagement: false, validateImageData: true);
            var bitmap = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppPArgb);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(source, 0, 0, bitmap.Width, bitmap.Height);
            }

            asset = new ImageAsset
            {
                Bitmap = bitmap,
                PixelSize = new Size(source.Width, source.Height),
                DisplayName = Path.GetFileName(path),
            };
        }
        catch (Exception ex)
        {
            asset = new ImageAsset { Error = ex.Message, DisplayName = Path.GetFileName(path) };
        }

        _cache[path] = (stamp, asset);
        return asset;
    }

    private ImageAsset ResolveDataUri(string url)
    {
        string key = "data:" + url.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + url.GetHashCode().ToString("x8", System.Globalization.CultureInfo.InvariantCulture);
        if (_cache.TryGetValue(key, out var cached))
        {
            return cached.Asset;
        }

        try
        {
            int comma = url.IndexOf(',');
            string header = url[5..comma];
            if (!header.Contains("base64", StringComparison.OrdinalIgnoreCase))
            {
                return new ImageAsset { Error = "unsupported data URI" };
            }

            byte[] bytes = Convert.FromBase64String(url[(comma + 1)..]);
            using var stream = new MemoryStream(bytes);
            using var source = Image.FromStream(stream);
            var bitmap = new Bitmap(source);
            var asset = new ImageAsset { Bitmap = bitmap, PixelSize = source.Size, DisplayName = "embedded image" };
            _cache[key] = (DateTime.MinValue, asset);
            return asset;
        }
        catch (Exception ex)
        {
            return new ImageAsset { Error = ex.Message };
        }
    }

    public void Clear()
    {
        foreach (var entry in _cache.Values)
        {
            entry.Asset.Bitmap?.Dispose();
        }

        _cache.Clear();
        _warned.Clear();
    }

    public void Dispose() => Clear();
}
