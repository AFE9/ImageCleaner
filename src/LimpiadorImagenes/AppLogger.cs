using System.Diagnostics;
using System.IO;

namespace LimpiadorImagenes;

/// <summary>
/// Simple file + debug logger. Writes to log.txt next to the EXE and to the VS Output window.
/// </summary>
public static class AppLogger
{
    private static readonly string LogPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "log.txt");

    static AppLogger()
    {
        try
        {
            // Truncate log on startup so it doesn't grow forever
            File.WriteAllText(LogPath,
                $"=== Session started: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==={Environment.NewLine}");
        }
        catch { }
    }

    public static void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
        Debug.WriteLine(line);
        WriteRaw(line);
    }

    public static void Error(string context, Exception ex)
    {
        Log($"ERROR [{context}]: {ex.GetType().Name}: {ex.Message}");
        if (ex.InnerException != null)
            Log($"  Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
        // First line of stack trace is usually enough to locate the crash
        var firstFrame = ex.StackTrace?.Split('\n').FirstOrDefault(l => l.Trim().StartsWith("at"))?.Trim();
        if (firstFrame != null)
            Log($"  Stack: {firstFrame}");
    }

    private static void WriteRaw(string line)
    {
        try { File.AppendAllText(LogPath, line + Environment.NewLine); }
        catch { }
    }
}
