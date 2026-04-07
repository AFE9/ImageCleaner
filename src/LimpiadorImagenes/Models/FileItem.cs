namespace LimpiadorImagenes.Models;

public class FileItem
{
    public string FullPath { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string Extension { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime ModifiedAt { get; init; }
    public FileItemKind Kind { get; init; }

    // Set after preview loads
    public int? WidthPx { get; set; }
    public int? HeightPx { get; set; }

    // AI scan results
    public double? BlurScore { get; set; }
    public ulong? PHashValue { get; set; }
    public bool IsLikelyScreenshot { get; set; }

    // Queue state
    public bool IsMarkedForDeletion { get; set; }

    public string FormattedSize => SizeBytes switch
    {
        >= 1_073_741_824 => $"{SizeBytes / 1_073_741_824.0:F1} GB",
        >= 1_048_576     => $"{SizeBytes / 1_048_576.0:F1} MB",
        >= 1_024         => $"{SizeBytes / 1_024.0:F1} KB",
        _                => $"{SizeBytes} B"
    };

    public string? Dimensions => (WidthPx.HasValue && HeightPx.HasValue)
        ? $"{WidthPx} × {HeightPx} px"
        : null;
}
