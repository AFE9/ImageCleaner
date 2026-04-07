using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using LimpiadorImagenes.Models;

namespace LimpiadorImagenes.ViewModels;

public partial class DuplicateItemViewModel : ObservableObject
{
    public FileItem File { get; init; } = null!;
    [ObservableProperty] private BitmapSource? _thumbnail;
    [ObservableProperty] private bool _isMarkedForDeletion;

    public string FileName => File.FileName;
    public string FormattedSize => File.FormattedSize;
}
