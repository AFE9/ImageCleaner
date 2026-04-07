using System.Windows.Media.Imaging;
using LimpiadorImagenes.Models;
using LimpiadorImagenes.Services.Interfaces;

namespace LimpiadorImagenes.Services.PreviewProviders;

public class PdfPreviewProvider : IPreviewProvider
{
    public bool CanHandle(FileItem item) => item.Kind == FileItemKind.Pdf;

    public Task<PreviewResult> GetPreviewAsync(FileItem item, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var lib = Docnet.Core.DocLib.Instance;
                using var doc = lib.GetDocReader(item.FullPath,
                    new Docnet.Core.Models.PageDimensions(1200, 1600));
                using var page = doc.GetPageReader(0);

                var width = page.GetPageWidth();
                var height = page.GetPageHeight();
                var rawBytes = page.GetImage();

                var bitmap = BitmapSource.Create(width, height, 96, 96,
                    System.Windows.Media.PixelFormats.Bgra32, null, rawBytes, width * 4);
                bitmap.Freeze();

                item.WidthPx = width;
                item.HeightPx = height;

                return new PreviewResult { StaticImage = bitmap };
            }
            catch (Exception ex)
            {
                return new PreviewResult { TextContent = $"[Error al cargar PDF: {ex.Message}]" };
            }
        }, ct);
    }
}
