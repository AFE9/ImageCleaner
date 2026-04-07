using System.Collections.Concurrent;
using System.IO;
using System.Numerics;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LimpiadorImagenes.Models;
using LimpiadorImagenes.Services.Interfaces;

namespace LimpiadorImagenes.Services;

public class DuplicateDetector : IDuplicateDetector
{
    public async Task<IReadOnlyList<FileGroup>> ScanAsync(
        IReadOnlyList<FileItem> items,
        int hammingDistanceThreshold = 8,
        IProgress<(int Done, int Total)>? progress = null,
        CancellationToken ct = default)
    {
        var imageItems = items
            .Where(f => f.Kind == FileItemKind.Image)
            .ToList();

        // Phase 1: Compute pHash for all images in parallel
        int done = 0;
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1),
            CancellationToken = ct
        };

        await Parallel.ForEachAsync(imageItems, options, async (item, token) =>
        {
            token.ThrowIfCancellationRequested();
            item.PHashValue = await Task.Run(() => ComputeDHash(item.FullPath), token);
            var current = Interlocked.Increment(ref done);
            progress?.Report((current, imageItems.Count * 2)); // phase 1 of 2
        });

        ct.ThrowIfCancellationRequested();

        // Phase 2: Group using BK-Tree
        var itemsWithHash = imageItems.Where(f => f.PHashValue.HasValue).ToList();
        var groups = await Task.Run(() => GroupByHash(itemsWithHash, hammingDistanceThreshold, progress, imageItems.Count), ct);

        return groups;
    }

    private static ulong? ComputeDHash(string path)
    {
        try
        {
            // Use WPF managed decoder — no native crashes possible
            BitmapFrame frame;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var decoder = BitmapDecoder.Create(stream,
                    BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                frame = decoder.Frames[0];
            }

            if (frame.PixelWidth == 0 || frame.PixelHeight == 0) return null;

            // Scale to 9×8 and convert to grayscale
            var scaled = new TransformedBitmap(frame,
                new System.Windows.Media.ScaleTransform(
                    9.0 / frame.PixelWidth, 8.0 / frame.PixelHeight));
            var gray = new FormatConvertedBitmap(scaled, PixelFormats.Gray8, null, 0);

            int w = gray.PixelWidth;
            int h = gray.PixelHeight;
            var pixels = new byte[w * h];
            gray.CopyPixels(pixels, w, 0);

            // dHash: compare adjacent pixels per row → 64-bit hash
            ulong hash = 0;
            for (int row = 0; row < Math.Min(8, h); row++)
                for (int col = 0; col < Math.Min(8, w - 1); col++)
                    if (pixels[row * w + col] > pixels[row * w + col + 1])
                        hash |= 1UL << (row * 8 + col);

            return hash;
        }
        catch
        {
            return null;
        }
    }

    private static List<FileGroup> GroupByHash(
        List<FileItem> items,
        int threshold,
        IProgress<(int Done, int Total)>? progress,
        int totalForProgress)
    {
        var tree = new BKTree();
        foreach (var item in items)
            tree.Insert(item.PHashValue!.Value, item);

        var grouped = new HashSet<FileItem>();
        var groups = new List<FileGroup>();
        int done = items.Count; // phase 1 already done

        foreach (var item in items)
        {
            if (grouped.Contains(item)) continue;

            var similar = tree.Search(item.PHashValue!.Value, threshold);
            if (similar.Count > 1)
            {
                groups.Add(new FileGroup
                {
                    RepresentativePHash = item.PHashValue!.Value,
                    Members = similar
                });
                foreach (var s in similar)
                    grouped.Add(s);
            }

            done++;
            progress?.Report((done, totalForProgress * 2));
        }

        return groups;
    }

    // ── BK-Tree implementation ───────────────────────────────────────────────

    private class BKTree
    {
        private Node? _root;

        private class Node
        {
            public ulong Hash;
            public List<FileItem> Items = new();
            public Dictionary<int, Node> Children = new();
        }

        public void Insert(ulong hash, FileItem item)
        {
            if (_root == null)
            {
                _root = new Node { Hash = hash };
                _root.Items.Add(item);
                return;
            }

            var current = _root;
            while (true)
            {
                int d = HammingDistance(hash, current.Hash);
                if (d == 0)
                {
                    current.Items.Add(item);
                    return;
                }
                if (!current.Children.TryGetValue(d, out var child))
                {
                    child = new Node { Hash = hash };
                    child.Items.Add(item);
                    current.Children[d] = child;
                    return;
                }
                current = child;
            }
        }

        public List<FileItem> Search(ulong query, int maxDistance)
        {
            var result = new List<FileItem>();
            if (_root == null) return result;

            var stack = new Stack<Node>();
            stack.Push(_root);

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                int d = HammingDistance(query, node.Hash);
                if (d <= maxDistance)
                    result.AddRange(node.Items);

                int low = d - maxDistance;
                int high = d + maxDistance;
                foreach (var (dist, child) in node.Children)
                    if (dist >= low && dist <= high)
                        stack.Push(child);
            }

            return result;
        }

        private static int HammingDistance(ulong a, ulong b)
            => BitOperations.PopCount(a ^ b);
    }
}
