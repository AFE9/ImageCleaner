using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LimpiadorImagenes.Models;

public partial class TempTrash : ObservableObject
{
    private readonly ObservableCollection<FileItem> _items = new();

    public ReadOnlyObservableCollection<FileItem> Items { get; }

    [ObservableProperty]
    private long _totalBytes;

    [ObservableProperty]
    private int _count;

    public TempTrash()
    {
        Items = new ReadOnlyObservableCollection<FileItem>(_items);
    }

    public void Add(FileItem item)
    {
        if (_items.Contains(item)) return;
        item.IsMarkedForDeletion = true;
        _items.Add(item);
        TotalBytes += item.SizeBytes;
        Count = _items.Count;
    }

    public bool Remove(FileItem item)
    {
        if (!_items.Remove(item)) return false;
        item.IsMarkedForDeletion = false;
        TotalBytes -= item.SizeBytes;
        Count = _items.Count;
        return true;
    }

    public FileItem? PopLast()
    {
        if (_items.Count == 0) return null;
        var last = _items[^1];
        Remove(last);
        return last;
    }

    public void Clear()
    {
        foreach (var item in _items)
            item.IsMarkedForDeletion = false;
        _items.Clear();
        TotalBytes = 0;
        Count = 0;
    }
}
