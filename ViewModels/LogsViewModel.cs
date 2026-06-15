using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Input;
using BloxHive.Models;
using BloxHive.Services;

namespace BloxHive.ViewModels;

public class LogsViewModel : BaseViewModel
{
    private readonly ObservableCollection<LogEntry> _filtered = [];
    private string _statusText = "";

    public ReadOnlyObservableCollection<LogEntry> FilteredEntries { get; }

    public bool FilterInfo { get; set; } = true;
    public bool FilterWarning { get; set; } = true;
    public bool FilterError { get; set; } = true;

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public ICommand ClearCommand { get; }
    public ICommand FilterCommand { get; }
    public ICommand CopyFilteredCommand { get; }
    public ICommand DownloadLogsCommand { get; }

    public LogsViewModel()
    {
        FilteredEntries = new ReadOnlyObservableCollection<LogEntry>(_filtered);
        ClearCommand = new RelayCommand(_ => { LogService.Clear(); Refresh(); });
        FilterCommand = new RelayCommand(_ => Refresh());
        CopyFilteredCommand = new RelayCommand(_ => CopyFiltered());
        DownloadLogsCommand = new RelayCommand(_ => DownloadLogs());

        foreach (var e in LogService.Entries)
            _filtered.Add(e);

        LogService.EntryAdded += OnEntryAdded;
    }

    private void OnEntryAdded(LogEntry entry)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (MatchesFilter(entry))
                _filtered.Add(entry);
        });
    }

    public void Refresh()
    {
        _filtered.Clear();
        foreach (var e in LogService.Entries)
        {
            if (MatchesFilter(e))
                _filtered.Add(e);
        }
    }

    private bool MatchesFilter(LogEntry e) => e.Level switch
    {
        LogLevel.Info => FilterInfo,
        LogLevel.Warning => FilterWarning,
        LogLevel.Error => FilterError,
        _ => true
    };

    private string FormatEntries() => string.Join(Environment.NewLine,
        _filtered.Select(e => $"[{e.Timestamp:HH:mm:ss}] [{e.LevelLabel}] [{e.Source}] {e.Message}"));

    private void CopyFiltered()
    {
        var text = FormatEntries();
        var lines = text.Split(Environment.NewLine);
        var capped = string.Join(Environment.NewLine, lines.Take(40));

        try
        {
            Clipboard.SetText(capped);
            StatusText = Loc.Copied;
            ClearStatusAfterDelay();
        }
        catch (Exception ex)
        {
            StatusText = $"{Loc.BackupError}: {ex.Message}";
        }
    }

    private void DownloadLogs()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = Loc.LogsDownload,
            FileName = $"BloxHive-Logs-{DateTime.Now:yyyy-MM-dd_HHmmss}.txt",
            Filter = "Textdateien (*.txt)|*.txt"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var text = FormatEntries();
            System.IO.File.WriteAllText(dialog.FileName, text, Encoding.UTF8);
            StatusText = Loc.LogsDownloadSuccess;
            ClearStatusAfterDelay();
        }
        catch (Exception ex)
        {
            StatusText = $"{Loc.BackupError}: {ex.Message}";
        }
    }

    private async void ClearStatusAfterDelay()
    {
        await Task.Delay(2500);
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (StatusText == Loc.Copied || StatusText == Loc.LogsDownloadSuccess)
                StatusText = "";
        });
    }
}
