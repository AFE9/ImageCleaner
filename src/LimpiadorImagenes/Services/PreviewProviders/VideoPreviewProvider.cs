using LimpiadorImagenes.Models;
using LimpiadorImagenes.Services.Interfaces;

namespace LimpiadorImagenes.Services.PreviewProviders;

public class VideoPreviewProvider : IPreviewProvider
{
    public bool CanHandle(FileItem item) => item.Kind == FileItemKind.Video;

    public Task<PreviewResult> GetPreviewAsync(FileItem item, CancellationToken ct = default)
    {
        // For video, return the path so FileViewerControl can use MediaElement
        return Task.FromResult(new PreviewResult { VideoPath = item.FullPath });
    }
}
