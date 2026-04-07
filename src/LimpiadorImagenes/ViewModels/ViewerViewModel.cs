using CommunityToolkit.Mvvm.ComponentModel;
using LimpiadorImagenes;
using LimpiadorImagenes.Models;
using LimpiadorImagenes.Services;
using LimpiadorImagenes.Services.Interfaces;

namespace LimpiadorImagenes.ViewModels;

public partial class ViewerViewModel : ObservableObject
{
    private readonly PreviewProviderFactory _factory;

    [ObservableProperty] private PreviewResult? _currentPreview;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isMagnifierActive;

    private CancellationTokenSource? _cts;

    public ViewerViewModel(PreviewProviderFactory factory)
    {
        _factory = factory;
    }

    public async Task LoadFileAsync(FileItem? item)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        CurrentPreview = null;

        if (item == null) return;

        IsLoading = true;
        AppLogger.Log($"Viewer: loading [{item.FileName}] kind={item.Kind}");
        try
        {
            var provider = _factory.Resolve(item);
            var result = await provider.GetPreviewAsync(item, ct);
            if (!ct.IsCancellationRequested)
                CurrentPreview = result;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            AppLogger.Error($"ViewerViewModel.LoadFile [{item?.FileName}]", ex);
        }
        finally
        {
            if (!ct.IsCancellationRequested)
                IsLoading = false;
        }
    }
}
