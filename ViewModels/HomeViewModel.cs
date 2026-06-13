using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using BloxHive.Models;
using BloxHive.Services;

namespace BloxHive.ViewModels;

public class HomeViewModel : BaseViewModel, IDisposable
{
    private readonly MutexService _mutexService = MutexService.Instance;
    private readonly RobloxProcessService _processService = new();
    private readonly DispatcherTimer _refreshTimer;
    private int _processCount;

    public bool IsMultiInstanceActive
    {
        get => _mutexService.IsActive;
        set
        {
            if (value == _mutexService.IsActive) return;

            if (value)
                _mutexService.Acquire();
            else
                _mutexService.Release();

            var settings = SettingsService.Load();
            settings.MultiInstanceActive = _mutexService.IsActive;
            SettingsService.Save(settings);

            OnPropertyChanged();
            OnPropertyChanged(nameof(MultiInstanceStatus));
        }
    }

    public string MultiInstanceStatus => _mutexService.IsActive ? Loc.MultiInstanceActive : Loc.MultiInstanceInactive;

    public ObservableCollection<RobloxProcessInfo> Processes { get; } = [];

    public int ProcessCount
    {
        get => _processCount;
        set => SetProperty(ref _processCount, value);
    }

    public string ProcessCountText => string.Format(Loc.RunningInstancesCount, ProcessCount);

    public ICommand KillProcessCommand { get; }
    public ICommand KillAllCommand { get; }

    public HomeViewModel()
    {
        _mutexService.PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(IsMultiInstanceActive));
            OnPropertyChanged(nameof(MultiInstanceStatus));
        };

        KillProcessCommand = new RelayCommand(id =>
        {
            if (id is int pid)
            {
                _processService.Kill(pid);
                RefreshProcesses();
            }
        });

        KillAllCommand = new RelayCommand(_ =>
        {
            _processService.KillAll();
            RefreshProcesses();
        });

        _refreshTimer = new DispatcherTimer(TimeSpan.FromSeconds(2), DispatcherPriority.Background, (_, _) => RefreshProcesses(), Dispatcher.CurrentDispatcher);
        _refreshTimer.Start();

        RefreshProcesses();
    }

    public void RefreshProcesses()
    {
        var running = _processService.GetProcesses();
        var currentIds = running.Select(p => p.Id).ToHashSet();

        for (int i = Processes.Count - 1; i >= 0; i--)
        {
            if (!currentIds.Contains(Processes[i].Id))
                Processes.RemoveAt(i);
        }

        foreach (var process in running)
        {
            if (!Processes.Any(p => p.Id == process.Id))
                Processes.Add(process);
        }

        ProcessCount = Processes.Count;
        OnPropertyChanged(nameof(ProcessCountText));
        OnPropertyChanged(nameof(HasProcesses));
        OnPropertyChanged(nameof(HasNoProcesses));
    }

    public bool HasProcesses => ProcessCount > 0;
    public bool HasNoProcesses => ProcessCount == 0;

    public void Dispose()
    {
        _refreshTimer.Stop();
    }
}
