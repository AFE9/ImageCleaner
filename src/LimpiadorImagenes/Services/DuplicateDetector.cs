using System.Numerics;
using OpenCvSharp;
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

        int total = imageItems.Count * 2; // phase 1 + phase 2
        int done = 0;
        int lastReported = 0;

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount),
            CancellationToken = ct
        };

        await Parallel.ForEachAsync(imageItems, options, (item, token) =>
        {
            token.ThrowIfCancellationRequested();
            item.PHashValue = ComputeDHash(item.FullPath);
            var current = Interlocked.Increment(ref done);
            if (current - Volatile.Read(ref lastReported) >= 50 || current == imageItems.Count)
            {
                Interlocked.Exchange(ref lastReported, current);
                progress?.Report((current, total));
            }
            return ValueTask.CompletedTask;
        });

        ct.ThrowIfCancellationRequested();

        var itemsWithHash = imageItems.Where(f => f.PHashValue.HasValue).ToList();
        var groups = await Task.Run(() => GroupByHash(itemsWithHash, hammingDistanceThreshold, progress, imageItems.Count), ct);

        return groups;
    }

    private static ulong? ComputeDHash(string path)
    {
        try
        {
            using var img = Cv2.ImRead(path, ImreadModes.Grayscale);
            if (img.Empty()) return null;

            using var resized = new Mat();
            Cv2.Resize(img, resized, new OpenCvSharp.Size(9, 8), interpolation: InterpolationFlags.Area);

            ulong hash = 0;
            for (int row = 0; row < 8; row++)
                for (int col = 0; col < 8; col++)
                    if (resized.At<byte>(row, col) > resized.At<byte>(row, col + 1))
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
