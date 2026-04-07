using System.Windows.Media.Imaging;
using LimpiadorImagenes.Models;

namespace LimpiadorImagenes.Services.Interfaces;

public interface IThumbnailCache
{
    Task<BitmapSource?> GetThumbnailAsync(FileItem item, int pixelSize = 160, CancellationToken ct = default);
    void Evict(string fullPath);
    void Clear();
}
