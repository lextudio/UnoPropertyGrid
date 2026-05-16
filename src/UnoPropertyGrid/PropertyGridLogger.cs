using System;
using System.IO;

namespace UnoPropertyGrid;

/// <summary>
/// File-based logger for debugging property-grid value-change flows.
/// Disabled by default — set <see cref="Enabled"/> = true and call <see cref="Reset"/> from app
/// startup (e.g. OnLaunched) to activate.  Log is written to %TEMP%\unopropertygrid.log.
/// </summary>
public static class PropertyGridLogger
{
    private static readonly string _logPath = Path.Combine(Path.GetTempPath(), "unopropertygrid.log");
    private static readonly object _lock = new();

    public static string LogPath => _logPath;
    public static bool Enabled { get; set; }

    public static void Reset()
    {
        if (!Enabled) return;
        lock (_lock)
        {
            try { File.WriteAllText(_logPath, $"=== UnoPropertyGrid log started {DateTime.Now:O} ===\n"); }
            catch { }
        }
    }

    public static void Log(string message)
    {
        if (!Enabled) return;
        var line = $"{DateTime.Now:HH:mm:ss.fff} {message}";
        lock (_lock)
        {
            try { File.AppendAllText(_logPath, line + "\n"); }
            catch { }
        }
        System.Diagnostics.Debug.WriteLine($"[PG] {line}");
    }
}
