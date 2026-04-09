using System.Text;
using DocumentFormat.OpenXml.Packaging;
using LimpiadorImagenes.Models;
using LimpiadorImagenes.Services.Interfaces;
using A = DocumentFormat.OpenXml.Drawing;

namespace LimpiadorImagenes.Services.PreviewProviders;

public class PresentationPreviewProvider : IPreviewProvider
{
    public bool CanHandle(FileItem item) => item.Kind == FileItemKind.Presentation;

    public Task<PreviewResult> GetPreviewAsync(FileItem item, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            if (item.Extension is ".ppt")
                return new PreviewResult
                {
                    TextContent = $"[ PPT ]\n\n{item.FileName}\n{item.FormattedSize}\n\n" +
                                  "Formato binario antiguo (.ppt) — sin vista previa disponible."
                };

            try
            {
                using var doc = PresentationDocument.Open(item.FullPath, false);
                var presPart = doc.PresentationPart;
                if (presPart == null)
                    return new PreviewResult { TextContent = "[Presentación vacía]" };

                var slideIds = presPart.Presentation.SlideIdList?
                    .Elements<DocumentFormat.OpenXml.Presentation.SlideId>().ToList();

                if (slideIds == null || slideIds.Count == 0)
                    return new PreviewResult { TextContent = "[Sin diapositivas]" };

                var sb = new StringBuilder();
                int maxSlides = Math.Min(slideIds.Count, 5);

                for (int i = 0; i < maxSlides; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var slideId = slideIds[i];
                    var slidePart = (SlidePart)presPart.GetPartById(slideId.RelationshipId!);

                    sb.AppendLine($"═══  Diapositiva {i + 1}  ═══\n");

                    var texts = slidePart.Slide.Descendants<A.Text>()
                        .Select(t => t.Text)
                        .Where(t => !string.IsNullOrWhiteSpace(t));

                    foreach (var text in texts)
                        sb.AppendLine(text);

                    sb.AppendLine();
                }

                var result = sb.ToString().Trim();
                return new PreviewResult
                {
                    TextContent = string.IsNullOrWhiteSpace(result)
                        ? "[Diapositivas sin contenido de texto]"
                        : result
                };
            }
            catch (Exception ex)
            {
                return new PreviewResult { TextContent = $"[Error al leer PPTX: {ex.Message}]" };
            }
        }, ct);
    }
}
