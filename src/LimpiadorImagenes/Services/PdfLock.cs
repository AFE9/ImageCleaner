namespace LimpiadorImagenes.Services;

/// <summary>
/// PDFium (Docnet.Core) is not thread-safe. All calls must be serialized.
/// </summary>
public static class PdfLock
{
    public static readonly SemaphoreSlim Gate = new(1, 1);
}
