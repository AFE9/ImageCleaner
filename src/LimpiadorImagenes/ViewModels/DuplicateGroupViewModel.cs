using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LimpiadorImagenes;
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
        AppLogger.Log($"DuplicateGroup.LoadGroups: {groups.Count} groups, IsActive will be {groups.Count > 0}");
        _cache = cache;
        _groups = groups.ToList();
        TotalGroups = _groups.Count;
        GroupIndex = 0;
        IsActive = _groups.Count > 0;
        if (IsActive) await LoadCurrentGroupAsync(ct);
        AppLogger.Log($"DuplicateGroup.LoadGroups done: Items.Count={Items.Count}");
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
            try
            {
                AppLogger.Log($"DuplicateGroup: loading thumbnail for {vm.File.FileName}");
                vm.Thumbnail = await _cache!.GetThumbnailAsync(vm.File, 200, ct);
                AppLogger.Log($"DuplicateGroup: thumbnail loaded={vm.Thumbnail != null} for {vm.File.FileName}");
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                AppLogger.Error($"DuplicateGroup.LoadThumbnail [{vm.File.FileName}]", ex);
            }
            finally { _semaphore.Release(); }
        });

        try { await Task.WhenAll(tasks); }
        catch (OperationCanceledException) { }
        catch (Exception ex) { AppLogger.Error("DuplicateGroup.WhenAll", ex); }
    }

    public void Reset()
    {
        IsActive = false;
        Items.Clear();
        _groups = new List<FileGroup>();
        GroupIndex = 0;
        TotalGroups = 0;
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
