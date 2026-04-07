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
        AppLogger.Log("App startup");

        DispatcherUnhandledException += (_, ex) =>
        {
            AppLogger.Error("DispatcherUnhandled", ex.Exception);
            ex.Handled = true;
            System.Windows.MessageBox.Show(
                $"Error inesperado:\n{ex.Exception.Message}\n\nDetalle guardado en log.txt",
                "Error", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        };

        TaskScheduler.UnobservedTaskException += (_, ex) =>
        {
            AppLogger.Error("UnobservedTask", ex.Exception);
            ex.SetObserved();
        };

        AppDomain.CurrentDomain.UnhandledException += (_, ex) =>
        {
            if (ex.ExceptionObject is Exception e)
                AppLogger.Error("AppDomainUnhandled", e);
            else
                AppLogger.Log($"ERROR [AppDomainUnhandled]: {ex.ExceptionObject}");
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
