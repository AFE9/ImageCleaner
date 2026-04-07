using LimpiadorImagenes.Models;

namespace LimpiadorImagenes.Services.Interfaces;

public interface IFileScanner
{
    Task<IReadOnlyList<FileItem>> ScanAsync(
        string rootPath,
        bool includeSubdirectories,
        IProgress<int>? progress = null,
        CancellationToken ct = default);
}
