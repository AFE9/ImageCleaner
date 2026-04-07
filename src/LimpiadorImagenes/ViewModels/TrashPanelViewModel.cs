using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using LimpiadorImagenes.Models;
using LimpiadorImagenes.Services.Interfaces;

namespace LimpiadorImagenes.ViewModels;

public partial class TrashPanelViewModel : ObservableObject
{
    public ObservableCollection<TrashItemViewModel> Items { get; } = new();

    private readonly IThumbnailCache _cache;
    private readonly SemaphoreSlim _semaphore = new(4);

    public TrashPanelViewModel(TempTrash trash, IThumbnailCache cache)
    {
        _cache = cache;
        ((INotifyCollectionChanged)trash.Items).CollectionChanged += OnTrashChanged;
    }

    private void OnTrashChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (FileItem item in e.NewItems!)
                    {
                        var vm = new TrashItemViewModel { File = item };
                        Items.Add(vm);
                        _ = LoadThumbnailAsync(vm);
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (FileItem item in e.OldItems!)
                    {
                        var vm = Items.FirstOrDefault(x => x.File == item);
                        if (vm != null) Items.Remove(vm);
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    Items.Clear();
                    break;
            }
        });
    }

    private async Task LoadThumbnailAsync(TrashItemViewModel vm)
    {
        await _semaphore.WaitAsync();
        try { vm.Thumbnail = await _cache.GetThumbnailAsync(vm.File, 160); }
        catch { }
        finally { _semaphore.Release(); }
    }
}
