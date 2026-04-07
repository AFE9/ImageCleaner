using System.Windows;
using LimpiadorImagenes.Services;
using LimpiadorImagenes.Services.Interfaces;
using LimpiadorImagenes.ViewModels;
using LimpiadorImagenes.Views;

namespace LimpiadorImagenes;

public partial class App : Application
{
    // Static reference so MainWindow can pass it to GridReviewWindow
    public static IThumbnailCache ThumbnailCache { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Global safety net — prevents silent crashes from unhandled exceptions
        DispatcherUnhandledException += (_, ex) =>
        {
            ex.Handled = true;
            System.Windows.MessageBox.Show(
                $"Error inesperado:\n{ex.Exception.Message}",
                "Error", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        };
        TaskScheduler.UnobservedTaskException += (_, ex) =>
        {
            ex.SetObserved();
        };

        // Manual DI composition root
        var thumbCache      = new ThumbnailCache();
        var previewFactory  = new PreviewProviderFactory();
        var scanner         = new FileScanner();
        var recycleBin      = new RecycleBinService();
        var blurDetector    = new BlurDetector();
        var dupDetector     = new DuplicateDetector();
        var screenshotDet   = new ScreenshotDetector();

        ThumbnailCache = thumbCache;

        var mainVm = new MainViewModel(
            scanner, thumbCache, recycleBin,
            blurDetector, dupDetector, screenshotDet,
            previewFactory);

        var mainWindow = new MainWindow(mainVm);
        mainWindow.Show();
    }
}
