using LimpiadorImagenes.Models;

namespace LimpiadorImagenes.Services.Interfaces;

public interface IDuplicateDetector
{
    Task<IReadOnlyList<FileGroup>> ScanAsync(
        IReadOnlyList<FileItem> items,
        int hammingDistanceThreshold = 8,
        IProgress<(int Done, int Total)>? progress = null,
        CancellationToken ct = default);
}
