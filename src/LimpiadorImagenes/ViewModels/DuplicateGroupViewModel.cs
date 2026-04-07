using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LimpiadorImagenes.Models;
using LimpiadorImagenes.Services.Interfaces;

namespace LimpiadorImagenes.ViewModels;

public partial class DuplicateGroupViewModel : ObservableObject
{
    public ObservableCollection<DuplicateItemViewModel> Items { get; } = new();

    [ObservableProperty] private bool _isActive;
    [ObservableProperty] private int _groupIndex;
    [ObservableProperty] private int _totalGroups;

    private List<FileGroup> _groups = new();
    private readonly TempTrash _trash;
    private readonly SemaphoreSlim _semaphore = new(4);
    private IThumbnailCache? _cache;

    public DuplicateGroupViewModel(TempTrash trash)
    {
        _trash = trash;
    }

    public async Task LoadGroupsAsync(
        IReadOnlyList<FileGroup> groups,
        IThumbnailCache cache,
        CancellationToken ct = default)
    {
        _cache = cache;
        _groups = groups.ToList();
        TotalGroups = _groups.Count;
        GroupIndex = 0;
        IsActive = _groups.Count > 0;
        if (IsActive) await LoadCurrentGroupAsync(ct);
    }

    private async Task LoadCurrentGroupAsync(CancellationToken ct = default)
    {
        Items.Clear();
        if (GroupIndex >= _groups.Count) return;

        var group = _groups[GroupIndex];
        var keeper = group.Keeper; // newest file — not pre-marked

        foreach (var file in group.Members)
        {
            Items.Add(new DuplicateItemViewModel
            {
                File = file,
                IsMarkedForDeletion = file != keeper
            });
        }

        // Load thumbnails concurrently
        var tasks = Items.Select(async vm =>
        {
            await _semaphore.WaitAsync(ct);
            try { vm.Thumbnail = await _cache!.GetThumbnailAsync(vm.File, 200, ct); }
            catch { }
            finally { _semaphore.Release(); }
        });

        try { await Task.WhenAll(tasks); }
        catch (OperationCanceledException) { }
    }

    /// <summary>Sends marked items to trash and advances to next group. Returns false when all groups done.</summary>
    public bool CommitAndAdvance()
    {
        foreach (var vm in Items.Where(v => v.IsMarkedForDeletion))
            _trash.Add(vm.File);
        return Advance();
    }

    /// <summary>Skips current group without marking anything. Returns false when all groups done.</summary>
    public bool SkipAndAdvance() => Advance();

    private bool Advance()
    {
        if (GroupIndex < _groups.Count - 1)
        {
            GroupIndex++;
            _ = LoadCurrentGroupAsync();
            return true;
        }
        IsActive = false;
        Items.Clear();
        return false;
    }
}
