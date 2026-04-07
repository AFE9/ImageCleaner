namespace LimpiadorImagenes.Models;

/// <summary>Cluster of visually identical or near-identical files (Duplicates mode).</summary>
public class FileGroup
{
    public ulong RepresentativePHash { get; init; }
    public List<FileItem> Members { get; init; } = new();
    public long TotalGroupSizeBytes => Members.Sum(f => f.SizeBytes);

    /// <summary>The newest file in the group — kept by default, others are candidates for deletion.</summary>
    public FileItem? Keeper => Members.OrderByDescending(f => f.ModifiedAt).FirstOrDefault();
}
