using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LimpiadorImagenes.Models;
using LimpiadorImagenes.Services.Interfaces;

namespace LimpiadorImagenes.ViewModels;

public partial class ProximityItemViewModel : ObservableObject
{
    public FileItem File { get; init; } = null!;
    [ObservableProperty] private System.Windows.Media.Imaging.BitmapSource? _thumbnail;
    [ObservableProperty] private bool _isCurrent;
}

public partial class ProximityBarViewModel : ObservableObject
{
    public ObservableCollection<ProximityItemViewModel> Items { get; } = new();

    private CancellationTokenSource? _cts;

    public async Task UpdateAsync(
        IReadOnlyList<FileItem> queue,
        int currentIndex,
        IThumbnailCache cache,
        int lookahead = 7)
    {
        // Cancel previous update
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        // Build the window: 2 before current + current + lookahead after
        int start = Math.Max(0, currentIndex - 2);
        int end = Math.Min(queue.Count - 1, currentIndex + lookahead);

        var windowItems = queue.Skip(start).Take(end - start + 1).ToList();

        Items.Clear();
        foreach (var item in windowItems)
        {
            Items.Add(new ProximityItemViewModel
            {
                File = item,
                IsCurrent = item == (currentIndex < queue.Count ? queue[currentIndex] : null)
            });
        }

        // Load thumbnails asynchronously
        var semaphore = new SemaphoreSlim(4);
        var tasks = Items.Select(async vm =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                if (ct.IsCancellationRequested) return;
                var thumb = await cache.GetThumbnailAsync(vm.File, 120, ct);
                if (!ct.IsCancellationRequested)
                    vm.Thumbnail = thumb;
            }
            catch (OperationCanceledException) { }
            finally
            {
                semaphore.Release();
            }
        });

        try { await Task.WhenAll(tasks); }
        catch (OperationCanceledException) { }
    }
}
