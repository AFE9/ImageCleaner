namespace LimpiadorImagenes.Models;

public class ScanResult
{
    public WorkMode Mode { get; init; }
    public IReadOnlyList<FileItem> FlaggedItems { get; init; } = Array.Empty<FileItem>();
    public IReadOnlyList<FileGroup> Groups { get; init; } = Array.Empty<FileGroup>();
    public bool IsGrouped => Groups.Count > 0;
}
