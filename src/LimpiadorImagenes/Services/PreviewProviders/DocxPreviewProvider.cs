using DocumentFormat.OpenXml.Packaging;
using LimpiadorImagenes.Models;
using LimpiadorImagenes.Services.Interfaces;

namespace LimpiadorImagenes.Services.PreviewProviders;

public class DocxPreviewProvider : IPreviewProvider
{
    public bool CanHandle(FileItem item) => item.Kind == FileItemKind.Docx;

    public Task<PreviewResult> GetPreviewAsync(FileItem item, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var doc = WordprocessingDocument.Open(item.FullPath, false);
                var body = doc.MainDocumentPart?.Document?.Body;
                if (body == null)
                    return new PreviewResult { TextContent = "[Documento vacío]" };

                var text = string.Join("\n", body
                    .Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>()
                    .Select(p => p.InnerText)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Take(200));

                return new PreviewResult { TextContent = string.IsNullOrWhiteSpace(text) ? "[Sin contenido de texto]" : text };
            }
            catch (Exception ex)
            {
                return new PreviewResult { TextContent = $"[Error al leer DOCX: {ex.Message}]" };
            }
        }, ct);
    }
}
