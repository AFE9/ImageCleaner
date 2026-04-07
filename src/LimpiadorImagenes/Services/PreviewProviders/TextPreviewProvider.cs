using System.IO;
using LimpiadorImagenes.Models;
using LimpiadorImagenes.Services.Interfaces;

namespace LimpiadorImagenes.Services.PreviewProviders;

public class TextPreviewProvider : IPreviewProvider
{
    private const int MaxBytes = 20_000;

    // Binary-format extensions that should show an info card rather than raw bytes
    private static readonly HashSet<string> BinaryFormats = new(StringComparer.OrdinalIgnoreCase)
        { ".rtf", ".xlsx", ".xls", ".pptx", ".ppt", ".ods", ".odp", ".odt" };

    public bool CanHandle(FileItem item) => item.Kind == FileItemKind.Text;

    public Task<PreviewResult> GetPreviewAsync(FileItem item, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            if (BinaryFormats.Contains(item.Extension))
            {
                var ext = item.Extension.TrimStart('.').ToUpperInvariant();
                return new PreviewResult
                {
                    TextContent = $"[ {ext} ]\n\n{item.FileName}\n{item.FormattedSize}\n\n" +
                                  $"Vista previa de contenido no disponible para este formato.\n" +
                                  $"Usá ← para mover a papelera o → para conservar."
                };
            }

            try
            {
                using var stream = new FileStream(item.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var buffer = new byte[MaxBytes];
                int read = stream.Read(buffer, 0, MaxBytes);

                // Check for binary content (high ratio of non-printable bytes)
                int nonPrintable = 0;
                for (int i = 0; i < Math.Min(read, 512); i++)
                    if (buffer[i] < 9 || (buffer[i] > 13 && buffer[i] < 32))
                        nonPrintable++;

                if (nonPrintable > 50)
                    return new PreviewResult { TextContent = $"[Archivo binario — {item.FormattedSize}]\n\n{item.FileName}" };

                var text = System.Text.Encoding.UTF8.GetString(buffer, 0, read);
                if (stream.Length > MaxBytes)
                    text += $"\n\n[... archivo truncado — {item.FormattedSize} total]";

                return new PreviewResult { TextContent = text };
            }
            catch (Exception ex)
            {
                return new PreviewResult { TextContent = $"[Error al leer archivo: {ex.Message}]" };
            }
        }, ct);
    }
}
