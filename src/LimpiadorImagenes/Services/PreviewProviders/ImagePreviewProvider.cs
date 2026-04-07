using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using LimpiadorImagenes.Models;
using LimpiadorImagenes.Services.Interfaces;

namespace LimpiadorImagenes.Services.PreviewProviders;

public class ImagePreviewProvider : IPreviewProvider
{
    public bool CanHandle(FileItem item) =>
        item.Kind is FileItemKind.Image or FileItemKind.RawImage;

    public Task<PreviewResult> GetPreviewAsync(FileItem item, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var stream = new FileStream(item.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                var frame = decoder.Frames[0];

                // Populate dimensions on the FileItem
                item.WidthPx = frame.PixelWidth;
                item.HeightPx = frame.PixelHeight;

                frame.Freeze();
                return new PreviewResult { StaticImage = frame };
            }
            catch
            {
                return new PreviewResult { StaticImage = null };
            }
        }, ct);
    }
}
