using System.Collections.ObjectModel;
using System.IO;
using BloxHive.Models;

namespace BloxHive.Services;

public static class LogService
{
    private static readonly string _logDir;
    private static readonly ObservableCollection<LogEntry> _entries = [];
    private const int MaxEntries = 500;

    public static ReadOnlyObservableCollection<LogEntry> Entries { get; }
    public static event Action<LogEntry>? EntryAdded;

    static LogService()
    {
        Entries = new ReadOnlyObservableCollection<LogEntry>(_entries);
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BloxHive");
        _logDir = Path.Combine(appData, "logs");
        Directory.CreateDirectory(_logDir);
        CleanOldLogs();
    }

    public static void Info(string source, string message) => Add(new LogEntry { Timestamp = DateTime.Now, Level = LogLevel.Info, Source = source, Message = message });
    public static void Warning(string source, string message) => Add(new LogEntry { Timestamp = DateTime.Now, Level = LogLevel.Warning, Source = source, Message = message });
    public static void Error(string source, string message) => Add(new LogEntry { Timestamp = DateTime.Now, Level = LogLevel.Error, Source = source, Message = message });

    private static void Add(LogEntry entry)
    {
        lock (_entries)
        {
            _entries.Add(entry);
            if (_entries.Count > MaxEntries)
                _entries.RemoveAt(0);
        }

        WriteToFile(entry);
        EntryAdded?.Invoke(entry);
    }

    private static void WriteToFile(LogEntry entry)
    {
        try
        {
            var file = Path.Combine(_logDir, $"app-{entry.Timestamp:yyyy-MM-dd}.log");
            var line = $"[{entry.Timestamp:HH:mm:ss}] [{entry.LevelLabel}] [{entry.Source}] {entry.Message}";
            File.AppendAllText(file, line + Environment.NewLine);
        }
        catch { }
    }

    private static void CleanOldLogs()
    {
        try
        {
            var cutoff = DateTime.Now.AddDays(-7);
            foreach (var file in Directory.GetFiles(_logDir, "app-*.log"))
            {
                if (File.GetLastWriteTime(file) < cutoff)
                    File.Delete(file);
            }
        }
        catch { }
    }

    public static void Clear()
    {
        lock (_entries)
            _entries.Clear();
    }
}
