using System.Windows.Media.Imaging;
using LimpiadorImagenes.Models;

namespace LimpiadorImagenes.Services.Interfaces;

public class PreviewResult
{
    public BitmapSource? StaticImage { get; init; }
    public string? VideoPath { get; init; }
    public string? TextContent { get; init; }
    public bool IsVideo => VideoPath != null;
    public bool IsText => TextContent != null;
    public bool IsImage => StaticImage != null && !IsVideo && !IsText;
}

public interface IPreviewProvider
{
    bool CanHandle(FileItem item);
    Task<PreviewResult> GetPreviewAsync(FileItem item, CancellationToken ct = default);
}
