using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Messaging;
using LimpiadorImagenes.Behaviors;
using LimpiadorImagenes.Messages;
using LimpiadorImagenes.ViewModels;

namespace LimpiadorImagenes.Views;

public partial class MainWindow : Window,
    IRecipient<CleanupCompleteMessage>,
    IRecipient<ErrorMessage>,
    IRecipient<ShowGridReviewMessage>
{
    private readonly MainViewModel _vm;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        KeyboardNavigationBehavior.SetViewer(MainViewer);
        WeakReferenceMessenger.Default.RegisterAll(this);

        // Wire up dynamic progress bar widths
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(MainViewModel.TotalDirectoryBytes)
                                  or nameof(MainViewModel.Trash))
                UpdateProgressBars();
        };
        vm.Trash.PropertyChanged += (_, _) => UpdateProgressBars();
        SizeChanged += (_, _) => UpdateProgressBars();
    }

    private void UpdateProgressBars()
    {
        if (_vm.TotalDirectoryBytes <= 0) return;

        // Directory weight bar: remaining = total - trash
        double remaining = Math.Max(0, _vm.TotalDirectoryBytes - _vm.Trash.TotalBytes);
        double barWidth = DirectoryWeightFill.Parent is System.Windows.Controls.Grid g ? g.ActualWidth : 0;
        DirectoryWeightFill.Width = barWidth * (remaining / _vm.TotalDirectoryBytes);

        // Savings bar (200px total)
        SavingsFill.Width = 200 * Math.Min(1.0, _vm.Trash.TotalBytes / (double)_vm.TotalDirectoryBytes);
    }

    protected override void OnClosed(EventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        base.OnClosed(e);
    }

    // ── Message Handlers ────────────────────────────────────────────────────

    public void Receive(CleanupCompleteMessage message)
    {
        Dispatcher.Invoke(() =>
        {
            var size = message.BytesFreed switch
            {
                >= 1_048_576 => $"{message.BytesFreed / 1_048_576.0:F1} MB",
                >= 1_024     => $"{message.BytesFreed / 1_024.0:F1} KB",
                _            => $"{message.BytesFreed} B"
            };
            ShowToast($"Limpieza completada: {message.FilesDeleted} archivos → Papelera ({size} liberados)");
        });
    }

    public void Receive(ErrorMessage message)
    {
        Dispatcher.Invoke(() => ShowToast($"Error: {message.Title} — {message.Detail}"));
    }

    public void Receive(ShowGridReviewMessage message)
    {
        Dispatcher.Invoke(() =>
        {
            var gridVm = new GridReviewViewModel(_vm.Trash);
            var gridWindow = new GridReviewWindow(gridVm, _vm);
            _ = gridVm.LoadAsync(
                message.Result.FlaggedItems,
                App.ThumbnailCache,
                $"Revisión — {message.Result.Mode}");

            if (message.Result.IsGrouped)
                gridVm.PreSelectAllButKeeper(message.Result.Groups);

            gridWindow.Owner = this;
            gridWindow.ShowDialog();
        });
    }

    // ── Toast notification ──────────────────────────────────────────────────

    private System.Windows.Threading.DispatcherTimer? _toastTimer;

    private void ShowToast(string message)
    {
        ToastText.Text = message;
        ToastBorder.Visibility = Visibility.Visible;

        _toastTimer?.Stop();
        _toastTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(4)
        };
        _toastTimer.Tick += (_, _) =>
        {
            ToastBorder.Visibility = Visibility.Collapsed;
            _toastTimer.Stop();
        };
        _toastTimer.Start();
    }
}
