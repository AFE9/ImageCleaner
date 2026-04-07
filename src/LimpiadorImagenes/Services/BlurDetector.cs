using LimpiadorImagenes.Models;
using LimpiadorImagenes.Services.Interfaces;
using OpenCvSharp;

namespace LimpiadorImagenes.Services;

public class BlurDetector : IBlurDetector
{
    public async Task<IReadOnlyList<FileItem>> ScanAsync(
        IReadOnlyList<FileItem> items,
        double threshold = 100.0,
        IProgress<(int Done, int Total)>? progress = null,
        CancellationToken ct = default)
    {
        var imageItems = items
            .Where(f => f.Kind is FileItemKind.Image or FileItemKind.RawImage)
            .ToList();

        int done = 0;
        var blurry = new System.Collections.Concurrent.ConcurrentBag<FileItem>();

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1),
            CancellationToken = ct
        };

        await Parallel.ForEachAsync(imageItems, options, async (item, token) =>
        {
            token.ThrowIfCancellationRequested();

            var score = await Task.Run(() => ComputeBlurScore(item.FullPath), token);
            item.BlurScore = score;

            if (score < threshold)
                blurry.Add(item);

            var current = System.Threading.Interlocked.Increment(ref done);
            progress?.Report((current, imageItems.Count));
        });

        return blurry.OrderBy(f => f.BlurScore).ToList();
    }

    private static double ComputeBlurScore(string path)
    {
        try
        {
            using var src = Cv2.ImRead(path, ImreadModes.Grayscale);
            if (src.Empty()) return double.MaxValue;

            using var small = new Mat();
            double scale = 512.0 / Math.Max(src.Rows, src.Cols);
            if (scale < 1.0)
                Cv2.Resize(src, small, new Size(), scale, scale);
            else
                src.CopyTo(small);

            using var laplacian = new Mat();
            Cv2.Laplacian(small, laplacian, MatType.CV_64F);

            using var meanMat   = new Mat();
            using var stddevMat = new Mat();
            Cv2.MeanStdDev(laplacian, meanMat, stddevMat);
            double stddevVal = stddevMat.At<double>(0);
            return stddevVal * stddevVal; // variance = blur score (lower = blurrier)
        }
        catch
        {
            return double.MaxValue; // can't read → not blurry, skip
        }
    }
}
