using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using LimpiadorImagenes.Models;

namespace LimpiadorImagenes.ViewModels;

public partial class TrashItemViewModel : ObservableObject
{
    public FileItem File { get; init; } = null!;
    [ObservableProperty] private BitmapSource? _thumbnail;
}
