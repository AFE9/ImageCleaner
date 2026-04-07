using LimpiadorImagenes.Models;
using LimpiadorImagenes.Services.Interfaces;
using LimpiadorImagenes.Services.PreviewProviders;

namespace LimpiadorImagenes.Services;

public class PreviewProviderFactory
{
    private readonly IReadOnlyList<IPreviewProvider> _providers;

    public PreviewProviderFactory()
    {
        _providers = new List<IPreviewProvider>
        {
            new ImagePreviewProvider(),
            new VideoPreviewProvider(),
            new PdfPreviewProvider(),
            new DocxPreviewProvider(),
            new TextPreviewProvider(),
            new UnknownPreviewProvider()  // fallback — always last
        };
    }

    public IPreviewProvider Resolve(FileItem item) =>
        _providers.First(p => p.CanHandle(item));
}
