using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LimpiadorImagenes.Models;
using LimpiadorImagenes.Services.Interfaces;

namespace LimpiadorImagenes.ViewModels;

public partial class GridReviewViewModel : ObservableObject
{
    public ObservableCollection<GridItemViewModel> Items { get; } = new();

    [ObservableProperty] private int _selectedCount;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private bool _isTrashView;

    private readonly TempTrash _trash;

    public GridReviewViewModel(TempTrash trash)
    {
        _trash = trash;
    }

    public async Task LoadAsync(
        IReadOnlyList<FileItem> flaggedItems,
        IThumbnailCache cache,
        string title,
        CancellationToken ct = default)
    {
        Title = title;
        IsLoading = true;
        Items.Clear();

        foreach (var item in flaggedItems)
            Items.Add(new GridItemViewModel { File = item });

        IsLoading = false;

        // Load thumbnails in background
        var semaphore = new SemaphoreSlim(4);
        var tasks = Items.Select(async vm =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                if (ct.IsCancellationRequested) return;
                vm.Thumbnail = await cache.GetThumbnailAsync(vm.File, 200, ct);
            }
            catch (OperationCanceledException) { }
            finally { semaphore.Release(); }
        });

        try { await Task.WhenAll(tasks); }
        catch (OperationCanceledException) { }
    }

    public void PreSelectAllButKeeper(IReadOnlyList<FileGroup> groups)
    {
        var keepers = groups
            .Where(g => g.Keeper != null)
            .Select(g => g.Keeper!)
            .ToHashSet();

        foreach (var vm in Items)
        {
            vm.IsSelected = !keepers.Contains(vm.File);
            vm.PropertyChanged += (_, _) => UpdateSelectedCount();
        }
        UpdateSelectedCount();
    }

    private void UpdateSelectedCount() =>
        SelectedCount = Items.Count(vm => vm.IsSelected);

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var vm in Items) vm.IsSelected = true;
        UpdateSelectedCount();
    }

    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var vm in Items) vm.IsSelected = false;
        UpdateSelectedCount();
    }

    [RelayCommand]
    private void MarkSelectedForDeletion()
    {
        foreach (var vm in Items.Where(v => v.IsSelected))
            _trash.Add(vm.File);

        // Notify caller that marking is done (close window)
        OnMarkingComplete?.Invoke();
    }

    public event Action? OnMarkingComplete;

    [RelayCommand]
    private void RecoverSelected()
    {
        foreach (var vm in Items.Where(v => v.IsSelected).ToList())
        {
            _trash.Remove(vm.File);
            Items.Remove(vm);
        }
        UpdateSelectedCount();
        // Update title to reflect new count
        Title = $"Papelera Temporal — {Items.Count} archivos";
    }
}
