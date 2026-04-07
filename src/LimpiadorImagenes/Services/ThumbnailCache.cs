using System.Collections.Concurrent;
using System.IO;
using System.Windows.Media.Imaging;
using LimpiadorImagenes;
using LimpiadorImagenes.Models;
using LimpiadorImagenes.Services.Interfaces;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using OpenCvSharp;

namespace LimpiadorImagenes.Services;

public class ThumbnailCache : IThumbnailCache
{
    private readonly ConcurrentDictionary<string, WeakReference<BitmapSource>> _cache = new();

    public async Task<BitmapSource?> GetThumbnailAsync(FileItem item, int pixelSize = 160, CancellationToken ct = default)
    {
        var key = $"{item.FullPath}_{pixelSize}";

        if (_cache.TryGetValue(key, out var weakRef) && weakRef.TryGetTarget(out var cached))
            return cached;

        var bitmap = await Task.Run(() => LoadThumbnail(item, pixelSize, ct), ct);

        if (bitmap != null)
            _cache[key] = new WeakReference<BitmapSource>(bitmap);

        return bitmap;
    }

    private static BitmapSource? LoadThumbnail(FileItem item, int pixelSize, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return null;

        AppLogger.Log($"Thumb START {item.Kind} [{item.FileName}]");
        try
        {
            var result = item.Kind switch
            {
                FileItemKind.Image or FileItemKind.RawImage => LoadImageThumbnail(item.FullPath, pixelSize),
                FileItemKind.Video => LoadVideoThumbnail(item.FullPath, pixelSize),
                FileItemKind.Pdf => LoadPdfThumbnail(item.FullPath, pixelSize),
                _ => CreateTextThumbnail(item.Extension, pixelSize)
            };
            AppLogger.Log($"Thumb OK   [{item.FileName}]");
            return result;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"ThumbnailCache.Load [{item.FileName}]", ex);
            return CreateErrorThumbnail(pixelSize);
        }
    }

    private static BitmapSource? LoadImageThumbnail(string path, int pixelSize)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];

        var thumb = new TransformedBitmap(
            frame,
            new System.Windows.Media.ScaleTransform(
                Math.Min(1.0, pixelSize / (double)frame.PixelWidth),
                Math.Min(1.0, pixelSize / (double)frame.PixelHeight)));

        thumb.Freeze();
        return thumb;
    }

    private static BitmapSource? LoadVideoThumbnail(string path, int pixelSize)
    {
        // Use OpenCV to grab frame 0
        try
        {
            using var cap = new OpenCvSharp.VideoCapture(path);
            using var frame = new OpenCvSharp.Mat();
            cap.Read(frame);
            if (frame.Empty()) return CreateIconThumbnail("▶", pixelSize);

            using var rgb = new OpenCvSharp.Mat();
            Cv2.CvtColor(frame, rgb, ColorConversionCodes.BGR2BGRA);
            return MatToBitmapSource(rgb);
        }
        catch
        {
            return CreateIconThumbnail("▶", pixelSize);
        }
    }

    private static BitmapSource? LoadPdfThumbnail(string path, int pixelSize)
    {
        // PDFium via Docnet.Core
        try
        {
            using var lib = Docnet.Core.DocLib.Instance;
            using var doc = lib.GetDocReader(path, new Docnet.Core.Models.PageDimensions(pixelSize, pixelSize));
            using var page = doc.GetPageReader(0);
            var width = page.GetPageWidth();
            var height = page.GetPageHeight();
            var rawBytes = page.GetImage();
            var bitmap = BitmapSource.Create(width, height, 96, 96,
                System.Windows.Media.PixelFormats.Bgra32, null, rawBytes, width * 4);
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return CreateIconThumbnail("PDF", pixelSize);
        }
    }

    private static BitmapSource CreateTextThumbnail(string extension, int pixelSize)
        => CreateIconThumbnail(extension.TrimStart('.').ToUpper(), pixelSize);

    private static BitmapSource CreateIconThumbnail(string label, int size)
    {
        var dv = new System.Windows.Media.DrawingVisual();
        using (var ctx = dv.RenderOpen())
        {
            ctx.DrawRectangle(
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 40, 40)),
                null,
                new System.Windows.Rect(0, 0, size, size));
            ctx.DrawText(
                new System.Windows.Media.FormattedText(label,
                    System.Globalization.CultureInfo.CurrentCulture,
                    System.Windows.FlowDirection.LeftToRight,
                    new System.Windows.Media.Typeface("Segoe UI"),
                    size * 0.2,
                    System.Windows.Media.Brushes.Gray,
                    96),
                new System.Windows.Point(size * 0.1, size * 0.35));
        }
        var rt = new System.Windows.Media.Imaging.RenderTargetBitmap(size, size, 96, 96,
            System.Windows.Media.PixelFormats.Pbgra32);
        rt.Render(dv);
        rt.Freeze();
        return rt;
    }

    private static BitmapSource CreateErrorThumbnail(int size)
        => CreateIconThumbnail("?", size);

    private static BitmapSource MatToBitmapSource(Mat mat)
    {
        int channels = mat.Channels();
        PixelFormat format = channels == 4 ? PixelFormats.Bgra32 :
                             channels == 3 ? PixelFormats.Bgr24  : PixelFormats.Gray8;
        int stride = mat.Width * channels;
        var data = new byte[stride * mat.Height];
        Marshal.Copy(mat.Data, data, 0, data.Length);
        var bitmap = BitmapSource.Create(mat.Width, mat.Height, 96, 96, format, null, data, stride);
        bitmap.Freeze();
        return bitmap;
    }

    public void Evict(string fullPath)
    {
        var keysToRemove = _cache.Keys.Where(k => k.StartsWith(fullPath)).ToList();
        foreach (var key in keysToRemove)
            _cache.TryRemove(key, out _);
    }

    public void Clear() => _cache.Clear();
}
