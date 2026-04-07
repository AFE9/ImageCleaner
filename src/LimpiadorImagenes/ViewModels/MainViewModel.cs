using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LimpiadorImagenes.Messages;
using LimpiadorImagenes.Models;
using LimpiadorImagenes.Services;
using LimpiadorImagenes.Services.Interfaces;

namespace LimpiadorImagenes.ViewModels;

public partial class MainViewModel : ObservableObject,
    IRecipient<NavigateToPathMessage>
{
    // ── Services ────────────────────────────────────────────────────────────
    private readonly IFileScanner _scanner;
    private readonly IThumbnailCache _thumbCache;
    private readonly RecycleBinService _recycleBin;
    private readonly IBlurDetector _blurDetector;
    private readonly IDuplicateDetector _dupDetector;
    private readonly IScreenshotDetector _screenshotDetector;

    // ── Child ViewModels ─────────────────────────────────────────────────────
    public ViewerViewModel Viewer { get; }
    public ProximityBarViewModel ProximityBar { get; }
    public MetadataViewModel Metadata { get; }
    public TrashPanelViewModel TrashPanel { get; }
    public DuplicateGroupViewModel DuplicateGroup { get; }

    // ── State ────────────────────────────────────────────────────────────────
    [ObservableProperty] private string? _rootPath;
    [ObservableProperty] private bool _includeSubdirectories = true;
    [ObservableProperty] private WorkMode _currentMode = WorkMode.Size;
    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private int _scanProgress;
    [ObservableProperty] private string _scanStatusText = string.Empty;
    [ObservableProperty] private FileItem? _currentFile;
    [ObservableProperty] private int _currentIndex;
    [ObservableProperty] private int _totalFiles;
    [ObservableProperty] private long _totalDirectoryBytes;
    [ObservableProperty] private bool _hasFiles;
    [ObservableProperty] private bool _isCleaningUp;
    [ObservableProperty] private int _cleanupProgress;

    // ── Collections ──────────────────────────────────────────────────────────
    private List<FileItem> _allFiles = new();
    public ObservableCollection<FileItem> Queue { get; } = new();
    public TempTrash Trash { get; } = new();

    private CancellationTokenSource? _scanCts;

    public MainViewModel(
        IFileScanner scanner,
        IThumbnailCache thumbCache,
        RecycleBinService recycleBin,
        IBlurDetector blurDetector,
        IDuplicateDetector dupDetector,
        IScreenshotDetector screenshotDetector,
        PreviewProviderFactory previewFactory)
    {
        _scanner = scanner;
        _thumbCache = thumbCache;
        _recycleBin = recycleBin;
        _blurDetector = blurDetector;
        _dupDetector = dupDetector;
        _screenshotDetector = screenshotDetector;

        Viewer = new ViewerViewModel(previewFactory);
        ProximityBar = new ProximityBarViewModel();
        Metadata = new MetadataViewModel();
        TrashPanel = new TrashPanelViewModel(Trash, thumbCache);
        DuplicateGroup = new DuplicateGroupViewModel(Trash);
        DuplicateGroup.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(DuplicateGroupViewModel.GroupIndex))
                CurrentIndex = DuplicateGroup.GroupIndex;
            else if (args.PropertyName == nameof(DuplicateGroupViewModel.TotalGroups))
                TotalFiles = DuplicateGroup.TotalGroups;
        };

        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public void Receive(NavigateToPathMessage message)
    {
        var idx = Queue.ToList().FindIndex(f => f.FullPath == message.FullPath);
        if (idx >= 0) GoToIndex(idx);
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task SelectFolderAsync()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Seleccionar carpeta raíz",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true) return;

        RootPath = dialog.FolderName;
        await StartScanAsync();
    }

    [RelayCommand]
    private async Task StartScanAsync()
    {
        if (string.IsNullOrWhiteSpace(RootPath)) return;

        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();
        var ct = _scanCts.Token;

        IsScanning = true;
        ScanProgress = 0;
        ScanStatusText = "Escaneando archivos...";
        Queue.Clear();
        _allFiles.Clear();
        Trash.Clear();
        _thumbCache.Clear();
        HasFiles = false;

        try
        {
            var progress = new Progress<int>(n => { ScanProgress = n; });
            _allFiles = (await _scanner.ScanAsync(RootPath, IncludeSubdirectories, progress, ct)).ToList();

            TotalDirectoryBytes = _allFiles.Sum(f => f.SizeBytes);
            TotalFiles = _allFiles.Count;

            ScanStatusText = $"{TotalFiles} archivos encontrados";
            await ApplyModeAsync(CurrentMode, ct);
            HasFiles = Queue.Count > 0;
        }
        catch (OperationCanceledException) { }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private async Task SwitchModeAsync(object? parameter)
    {
        if (_allFiles.Count == 0) return;
        if (!Enum.TryParse<WorkMode>(parameter?.ToString(), out var mode)) return;

        CurrentMode = mode;
        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();
        var ct = _scanCts.Token;

        await ApplyModeAsync(mode, ct);
    }

    private async Task ApplyModeAsync(WorkMode mode, CancellationToken ct)
    {
        IsScanning = true;
        Queue.Clear();

        try
        {
            IReadOnlyList<FileItem> ordered;

            switch (mode)
            {
                case WorkMode.Size:
                    ordered = _allFiles
                        .Where(f => !f.IsMarkedForDeletion)
                        .OrderByDescending(f => f.SizeBytes)
                        .ToList();
                    PopulateQueue(ordered);
                    break;

                case WorkMode.Age:
                    ordered = _allFiles
                        .Where(f => !f.IsMarkedForDeletion)
                        .OrderBy(f => f.CreatedAt)
                        .ToList();
                    PopulateQueue(ordered);
                    break;

                case WorkMode.Blur:
                    ScanStatusText = "Analizando nitidez (AI Scan)...";
                    var progress = new Progress<(int Done, int Total)>(p =>
                    {
                        ScanProgress = (int)(p.Done / (double)Math.Max(1, p.Total) * 100);
                        ScanStatusText = $"Blur: {p.Done}/{p.Total}";
                    });
                    var blurry = await _blurDetector.ScanAsync(
                        _allFiles.Where(f => !f.IsMarkedForDeletion).ToList(),
                        100.0, progress, ct);

                    if (blurry.Count == 0) { ScanStatusText = "No se detectaron imágenes borrosas"; break; }
                    var blurResult = new ScanResult { Mode = mode, FlaggedItems = blurry };
                    WeakReferenceMessenger.Default.Send(new ShowGridReviewMessage(blurResult));
                    PopulateQueue(_allFiles.Where(f => !f.IsMarkedForDeletion).OrderByDescending(f => f.SizeBytes).ToList());
                    break;

                case WorkMode.Screenshot:
                    ScanStatusText = "Detectando capturas de pantalla...";
                    var screenshots = _screenshotDetector.Filter(
                        _allFiles.Where(f => !f.IsMarkedForDeletion).ToList());
                    if (screenshots.Count == 0) { ScanStatusText = "No se detectaron capturas de pantalla"; break; }
                    var ssResult = new ScanResult { Mode = mode, FlaggedItems = screenshots };
                    WeakReferenceMessenger.Default.Send(new ShowGridReviewMessage(ssResult));
                    PopulateQueue(screenshots);
                    break;

                case WorkMode.Duplicates:
                    ScanStatusText = "Buscando duplicados (AI Scan)...";
                    var dupProgress = new Progress<(int Done, int Total)>(p =>
                    {
                        ScanProgress = (int)(p.Done / (double)Math.Max(1, p.Total) * 100);
                        ScanStatusText = $"Duplicados: {p.Done}/{p.Total}";
                    });
                    var groups = await _dupDetector.ScanAsync(
                        _allFiles.Where(f => !f.IsMarkedForDeletion).ToList(),
                        8, dupProgress, ct);

                    if (groups.Count == 0)
                    {
                        ScanStatusText = "No se encontraron duplicados";
                        break;
                    }
                    await DuplicateGroup.LoadGroupsAsync(groups, _thumbCache, ct);
                    break;
            }

            GoToIndex(0);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            WeakReferenceMessenger.Default.Send(new ErrorMessage("Error de escaneo", ex.Message));
        }
        finally
        {
            IsScanning = false;
            ScanStatusText = Queue.Count > 0
                ? $"{Queue.Count} archivos en cola"
                : "Sin archivos para mostrar";
        }
    }

    private void PopulateQueue(IEnumerable<FileItem> items)
    {
        Queue.Clear();
        foreach (var item in items)
            Queue.Add(item);
        TotalFiles = Queue.Count;
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    [RelayCommand]
    private void KeepCurrentFile()
    {
        if (DuplicateGroup.IsActive)
        {
            if (!DuplicateGroup.SkipAndAdvance())
                ScanStatusText = "Revisión de duplicados completada";
            return;
        }
        if (Queue.Count == 0) return;
        AdvanceQueue();
    }

    [RelayCommand]
    private void MarkCurrentForDeletion()
    {
        if (DuplicateGroup.IsActive)
        {
            if (!DuplicateGroup.CommitAndAdvance())
                ScanStatusText = "Revisión de duplicados completada";
            return;
        }
        if (CurrentFile == null || Queue.Count == 0) return;

        var file = CurrentFile;
        Trash.Add(file);
        Queue.Remove(file);

        // Stay at same index (next file slides in), clamp if at end
        GoToIndex(Math.Min(CurrentIndex, Queue.Count - 1));
    }

    [RelayCommand]
    private void UndoLastMark()
    {
        var recovered = Trash.PopLast();
        if (recovered == null) return;

        // Re-insert at current position in queue
        int insertAt = Math.Min(CurrentIndex, Queue.Count);
        Queue.Insert(insertAt, recovered);
        GoToIndex(insertAt);
    }

    private void AdvanceQueue()
    {
        if (Queue.Count == 0) { CurrentFile = null; return; }

        int nextIndex = CurrentIndex + 1;
        if (nextIndex >= Queue.Count) nextIndex = Queue.Count - 1;
        GoToIndex(nextIndex);
    }

    private void GoToIndex(int index)
    {
        if (Queue.Count == 0)
        {
            CurrentFile = null;
            CurrentIndex = 0;
            Viewer.LoadFileAsync(null);
            Metadata.Update(null);
            return;
        }

        CurrentIndex = Math.Max(0, Math.Min(index, Queue.Count - 1));
        CurrentFile = Queue[CurrentIndex];

        _ = Viewer.LoadFileAsync(CurrentFile);
        _ = ProximityBar.UpdateAsync(Queue, CurrentIndex, _thumbCache);
        Metadata.Update(CurrentFile);
    }

    // ── Cleanup ───────────────────────────────────────────────────────────────

    [RelayCommand]
    private void RestoreFromTrash(FileItem? item)
    {
        if (item == null || !Trash.Remove(item)) return;
        int insertAt = Math.Min(CurrentIndex, Queue.Count);
        Queue.Insert(insertAt, item);
    }

    [RelayCommand]
    private async Task ExecuteCleanupAsync()
    {
        if (Trash.Count == 0) return;

        IsCleaningUp = true;
        CleanupProgress = 0;

        try
        {
            var progress = new Progress<(int Done, int Total)>(p =>
                CleanupProgress = (int)(p.Done / (double)Math.Max(1, p.Total) * 100));

            var (deleted, bytesFreed) = await _recycleBin.ExecuteCleanupAsync(
                Trash.Items.ToList(), progress);

            // Remove from allFiles and clear trash
            foreach (var item in Trash.Items.ToList())
                _allFiles.Remove(item);

            TotalDirectoryBytes = _allFiles.Sum(f => f.SizeBytes);
            Trash.Clear();

            WeakReferenceMessenger.Default.Send(new CleanupCompleteMessage(deleted, bytesFreed));
        }
        finally
        {
            IsCleaningUp = false;
        }
    }
}
