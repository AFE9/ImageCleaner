using LimpiadorImagenes.Models;

namespace LimpiadorImagenes.Services.Interfaces;

public interface IScreenshotDetector
{
    /// <summary>Returns true if the file is likely a screenshot based on heuristics.</summary>
    bool IsLikelyScreenshot(FileItem item);

    IReadOnlyList<FileItem> Filter(IReadOnlyList<FileItem> items);
}
