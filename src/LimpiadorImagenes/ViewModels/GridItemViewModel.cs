using CommunityToolkit.Mvvm.ComponentModel;
using LimpiadorImagenes.Models;

namespace LimpiadorImagenes.ViewModels;

public partial class GridItemViewModel : ObservableObject
{
    public FileItem File { get; init; } = null!;

    [ObservableProperty] private System.Windows.Media.Imaging.BitmapSource? _thumbnail;
    [ObservableProperty] private bool _isSelected;

    public string FileName => File.FileName;
    public string FormattedSize => File.FormattedSize;
    public string? BlurInfo => File.BlurScore.HasValue ? $"Blur: {File.BlurScore:F0}" : null;
}
