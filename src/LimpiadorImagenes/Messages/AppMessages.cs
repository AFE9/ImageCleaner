namespace LimpiadorImagenes.Messages;

public record NavigateToFileMessage(int Index);
public record NavigateToPathMessage(string FullPath);
public record ErrorMessage(string Title, string Detail);
public record ScanCompleteMessage(Models.WorkMode Mode, int FileCount);
public record CleanupCompleteMessage(int FilesDeleted, long BytesFreed);
public record ShowGridReviewMessage(Models.ScanResult Result);
