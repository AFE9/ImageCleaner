using System.IO;
using LimpiadorImagenes.Models;
using Microsoft.VisualBasic.FileIO;

namespace LimpiadorImagenes.Services;

public class RecycleBinService
{
    public async Task<(int Deleted, long BytesFreed)> ExecuteCleanupAsync(
        IEnumerable<FileItem> items,
        IProgress<(int Done, int Total)>? progress = null,
        CancellationToken ct = default)
    {
        var list = items.ToList();
        int deleted = 0;
        long bytesFreed = 0;

        await Task.Run(() =>
        {
            for (int i = 0; i < list.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                var item = list[i];
                try
                {
                    if (File.Exists(item.FullPath))
                    {
                        FileSystem.DeleteFile(item.FullPath,
                            UIOption.OnlyErrorDialogs,
                            RecycleOption.SendToRecycleBin);
                        deleted++;
                        bytesFreed += item.SizeBytes;
                    }
                }
                catch
                {
                    // Skip files that can't be deleted (locked, access denied, etc.)
                }
                progress?.Report((i + 1, list.Count));
            }
        }, ct);

        return (deleted, bytesFreed);
    }
}
