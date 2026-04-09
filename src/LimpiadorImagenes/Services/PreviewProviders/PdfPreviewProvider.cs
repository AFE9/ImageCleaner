using System.Windows.Media.Imaging;
using LimpiadorImagenes;
using LimpiadorImagenes.Models;
using LimpiadorImagenes.Services;
using LimpiadorImagenes.Services.Interfaces;

namespace LimpiadorImagenes.Services.PreviewProviders;

public class PdfPreviewProvider : IPreviewProvider
{
    public bool CanHandle(FileItem item) => item.Kind == FileItemKind.Pdf;

    public async Task<PreviewResult> GetPreviewAsync(FileItem item, CancellationToken ct = default)
    {
        await PdfLock.Gate.WaitAsync(ct);
        try
        {
            return await Task.Run(() =>
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

                    if (width <= 0 || height <= 0 || rawBytes == null || rawBytes.Length < width * height * 4)
                        return new PreviewResult { TextContent = "[PDF vacío o dañado]" };

                    CompositeOnWhite(rawBytes);

                    var bitmap = BitmapSource.Create(width, height, 96, 96,
                        System.Windows.Media.PixelFormats.Bgra32, null, rawBytes, width * 4);
                    bitmap.Freeze();

                    item.WidthPx = width;
                    item.HeightPx = height;

                    return new PreviewResult { StaticImage = bitmap };
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"PdfPreview [{item.FileName}]", ex);
                    return new PreviewResult { TextContent = $"[Error al cargar PDF: {ex.Message}]" };
                }
            }, ct);
        }
        finally
        {
            PdfLock.Gate.Release();
        }
    }

    /// <summary>Docnet returns pre-multiplied BGRA where background alpha=0 means RGB=0 (black).
    /// Composite onto a white background so the PDF looks correct.</summary>
    internal static void CompositeOnWhite(byte[] bgra)
    {
        for (int i = 0; i < bgra.Length; i += 4)
        {
            int a = bgra[i + 3];
            int blend = 255 - a;
            bgra[i]     = (byte)Math.Min(255, bgra[i] + blend);
            bgra[i + 1] = (byte)Math.Min(255, bgra[i + 1] + blend);
            bgra[i + 2] = (byte)Math.Min(255, bgra[i + 2] + blend);
            bgra[i + 3] = 255;
        }
    }
}
