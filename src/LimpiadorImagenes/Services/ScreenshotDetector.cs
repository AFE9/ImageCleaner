using LimpiadorImagenes.Models;
using LimpiadorImagenes.Services.Interfaces;

namespace LimpiadorImagenes.Services;

public class ScreenshotDetector : IScreenshotDetector
{
    private static readonly HashSet<(int W, int H)> CommonScreenResolutions = new()
    {
        (1920, 1080), (2560, 1440), (3840, 2160), (1366, 768), (1280, 720),
        (1280, 800), (1440, 900), (1680, 1050), (1920, 1200), (2560, 1600),
        (3440, 1440), (2560, 1080), (1600, 900), (1024, 768), (1280, 1024),
        (2880, 1800), (3840, 2400), (5120, 2880)
    };

    private static readonly string[] ScreenshotKeywords =
        { "screenshot", "captura", "screen", "snip", "grab", "pantalla", "capture" };

    private static readonly (double Min, double Max)[] CommonAspectRatios =
    {
        (16.0/9 - 0.02, 16.0/9 + 0.02),   // 16:9
        (16.0/10 - 0.02, 16.0/10 + 0.02), // 16:10
        (4.0/3 - 0.02, 4.0/3 + 0.02),     // 4:3
        (21.0/9 - 0.03, 21.0/9 + 0.03),   // 21:9 ultrawide
        (32.0/9 - 0.03, 32.0/9 + 0.03),   // 32:9 super ultrawide
    };

    public bool IsLikelyScreenshot(FileItem item)
    {
        int matchCount = 0;

        // Rule 1: filename contains screenshot keywords
        var nameLower = item.FileName.ToLowerInvariant();
        if (ScreenshotKeywords.Any(k => nameLower.Contains(k)))
            matchCount++;

        // Rule 2: exact resolution match
        if (item.WidthPx.HasValue && item.HeightPx.HasValue)
        {
            if (CommonScreenResolutions.Contains((item.WidthPx.Value, item.HeightPx.Value)) ||
                CommonScreenResolutions.Contains((item.HeightPx.Value, item.WidthPx.Value)))
                matchCount++;

            // Rule 3: aspect ratio matches common screen ratio
            double ratio = (double)item.WidthPx.Value / item.HeightPx.Value;
            if (CommonAspectRatios.Any(r => ratio >= r.Min && ratio <= r.Max))
                matchCount++;
        }

        return matchCount >= 2;
    }

    public IReadOnlyList<FileItem> Filter(IReadOnlyList<FileItem> items)
    {
        return items
            .Where(f => f.Kind is FileItemKind.Image or FileItemKind.RawImage)
            .Where(IsLikelyScreenshot)
            .ToList();
    }
}
