using LimpiadorImagenes.Models;
using LimpiadorImagenes.Services.Interfaces;

namespace LimpiadorImagenes.Services.PreviewProviders;

/// <summary>Fallback provider for unsupported file types.</summary>
public class UnknownPreviewProvider : IPreviewProvider
{
    public bool CanHandle(FileItem item) => true; // always matches as fallback

    public Task<PreviewResult> GetPreviewAsync(FileItem item, CancellationToken ct = default)
    {
        var ext = item.Extension.TrimStart('.').ToUpperInvariant();
        var message =
            $"[ {ext} ]\n\n" +
            $"{item.FileName}\n" +
            $"{item.FormattedSize}\n\n" +
            $"Este formato no tiene vista previa disponible.\n" +
            $"Usá ← para mover a papelera o → para conservar.";
        return Task.FromResult(new PreviewResult { TextContent = message });
    }
}
