using System.IO;
using LimpiadorImagenes.Models;
using LimpiadorImagenes.Services.Interfaces;

namespace LimpiadorImagenes.Services;

public class FileScanner : IFileScanner
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".gif", ".tiff", ".tif", ".heic", ".heif" };

    private static readonly HashSet<string> RawExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".raw", ".cr2", ".cr3", ".nef", ".arw", ".orf", ".rw2", ".dng", ".raf", ".pef" };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".mp4", ".mov", ".avi", ".mkv", ".wmv", ".flv", ".webm", ".m4v", ".3gp", ".mpg", ".mpeg" };

    private static readonly HashSet<string> PdfExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".pdf" };

    private static readonly HashSet<string> DocxExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".docx", ".doc", ".dotx", ".dot" };

    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".txt", ".md", ".csv", ".log", ".ini", ".xml", ".json", ".yaml", ".yml",
          ".rtf", ".xlsx", ".xls", ".pptx", ".ppt", ".odt", ".ods", ".odp",
          ".html", ".htm", ".css", ".js", ".ts", ".py", ".cs", ".cpp", ".c",
          ".h", ".java", ".kt", ".rb", ".php", ".sh", ".bat", ".ps1", ".config",
          ".toml", ".properties", ".env", ".gitignore", ".editorconfig" };

    public Task<IReadOnlyList<FileItem>> ScanAsync(
        string rootPath,
        bool includeSubdirectories,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            var searchOption = includeSubdirectories
                ? SearchOption.AllDirectories
                : SearchOption.TopDirectoryOnly;

            var files = Directory.EnumerateFiles(rootPath, "*.*", searchOption)
                .TakeWhile(_ => !ct.IsCancellationRequested)
                .Select((path, i) =>
                {
                    if (i % 100 == 0) progress?.Report(i);
                    return BuildFileItem(path);
                })
                .Where(f => f != null)
                .Cast<FileItem>()
                .ToList();

            return (IReadOnlyList<FileItem>)files;
        }, ct);
    }

    private static FileItem? BuildFileItem(string path)
    {
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists) return null;

            var ext = info.Extension.ToLowerInvariant();
            return new FileItem
            {
                FullPath = info.FullName,
                FileName = info.Name,
                Extension = ext,
                SizeBytes = info.Length,
                CreatedAt = info.CreationTime,
                ModifiedAt = info.LastWriteTime,
                Kind = ClassifyExtension(ext)
            };
        }
        catch
        {
            return null;
        }
    }

    private static FileItemKind ClassifyExtension(string ext)
    {
        if (ImageExtensions.Contains(ext)) return FileItemKind.Image;
        if (RawExtensions.Contains(ext)) return FileItemKind.RawImage;
        if (VideoExtensions.Contains(ext)) return FileItemKind.Video;
        if (PdfExtensions.Contains(ext)) return FileItemKind.Pdf;
        if (DocxExtensions.Contains(ext)) return FileItemKind.Docx;
        if (TextExtensions.Contains(ext)) return FileItemKind.Text;
        return FileItemKind.Unknown;
    }
}
