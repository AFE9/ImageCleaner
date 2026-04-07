using LimpiadorImagenes.Models;

namespace LimpiadorImagenes.Services.Interfaces;

public interface IBlurDetector
{
    Task<IReadOnlyList<FileItem>> ScanAsync(
        IReadOnlyList<FileItem> items,
        double threshold = 100.0,
        IProgress<(int Done, int Total)>? progress = null,
        CancellationToken ct = default);
}
