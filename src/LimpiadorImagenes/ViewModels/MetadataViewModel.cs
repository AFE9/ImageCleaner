using CommunityToolkit.Mvvm.ComponentModel;
using LimpiadorImagenes.Models;

namespace LimpiadorImagenes.ViewModels;

public partial class MetadataViewModel : ObservableObject
{
    [ObservableProperty] private string _fileName = string.Empty;
    [ObservableProperty] private string _extension = string.Empty;
    [ObservableProperty] private string _formattedSize = string.Empty;
    [ObservableProperty] private string _dimensions = string.Empty;
    [ObservableProperty] private string _created = string.Empty;
    [ObservableProperty] private string _modified = string.Empty;
    [ObservableProperty] private string _fullPath = string.Empty;
    [ObservableProperty] private double? _blurScore;
    [ObservableProperty] private bool _hasBlurScore;

    public void Update(FileItem? item)
    {
        if (item == null)
        {
            FileName = string.Empty;
            Extension = string.Empty;
            FormattedSize = string.Empty;
            Dimensions = string.Empty;
            Created = string.Empty;
            Modified = string.Empty;
            FullPath = string.Empty;
            BlurScore = null;
            HasBlurScore = false;
            return;
        }

        FileName = item.FileName;
        Extension = item.Extension.TrimStart('.').ToUpperInvariant();
        FormattedSize = item.FormattedSize;
        Dimensions = item.Dimensions ?? "—";
        Created = item.CreatedAt.ToString("dd/MM/yyyy  HH:mm");
        Modified = item.ModifiedAt.ToString("dd/MM/yyyy  HH:mm");
        FullPath = item.FullPath;
        BlurScore = item.BlurScore;
        HasBlurScore = item.BlurScore.HasValue;
    }
}
