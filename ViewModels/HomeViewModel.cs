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
    private readonly WebhookService _webhookService = new();
    private readonly DispatcherTimer _refreshTimer;
    private readonly DispatcherTimer _webhookTimer = new();
    private int _processCount;
    private string _webhookStatus = "";
    private static bool _staticLoopActive;

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

    public bool AutoLoopActive
    {
        get => _staticLoopActive;
        set
        {
            if (_staticLoopActive == value) return;
            _staticLoopActive = value;
            OnPropertyChanged();

            if (value)
                StartWebhookLoop();
            else
                StopWebhookLoop();
        }
    }

    public string MultiInstanceStatus => _mutexService.IsActive ? Loc.MultiInstanceActive : Loc.MultiInstanceInactive;

    public string WebhookStatus
    {
        get => _webhookStatus;
        set
        {
            SetProperty(ref _webhookStatus, value);
            OnPropertyChanged(nameof(HasWebhookStatus));
        }
    }

    public bool HasWebhookStatus => !string.IsNullOrEmpty(WebhookStatus);

    public ObservableCollection<RobloxProcessInfo> Processes { get; } = [];

    public int ProcessCount
    {
        get => _processCount;
        set => SetProperty(ref _processCount, value);
    }

    public string ProcessCountText => string.Format(Loc.RunningInstancesCount, ProcessCount);

    public ICommand KillProcessCommand { get; }
    public ICommand KillAllCommand { get; }
    public ICommand TestWebhookCommand { get; }
    public ICommand TestInstanceCommand { get; }

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
                _ = RefreshProcesses();
            }
        });

        KillAllCommand = new RelayCommand(_ =>
        {
            _processService.KillAll();
            _reportedProcesses.Clear();
            _ = RefreshProcesses();
        });

        TestWebhookCommand = new RelayCommand(async _ =>
        {
            var settings = SettingsService.Load();
            if (string.IsNullOrWhiteSpace(settings.WebhookUrl))
            {
                WebhookStatus = Loc.WebhookNoUrl;
                return;
            }

            WebhookStatus = "";
            var success = await _webhookService.SendTest(settings.WebhookUrl);
            WebhookStatus = success ? Loc.TestWebhookSent : Loc.TestWebhookFailed;
        });

        TestInstanceCommand = new RelayCommand(async id =>
        {
            if (id is int pid)
            {
                var settings = SettingsService.Load();
                if (string.IsNullOrWhiteSpace(settings.WebhookUrl))
                {
                    WebhookStatus = Loc.WebhookNoUrl;
                    return;
                }

                var process = Processes.FirstOrDefault(p => p.Id == pid);
                if (process == null) return;

                WebhookStatus = "";
                var ok = await _webhookService.SendScreenshot(settings.WebhookUrl, pid, process.DisplayName);
                WebhookStatus = ok ? Loc.ScreenshotSent : Loc.ScreenshotFailed;
            }
        });

        _refreshTimer = new DispatcherTimer(TimeSpan.FromSeconds(2), DispatcherPriority.Background, (_, _) => _ = RefreshProcesses(), Dispatcher.CurrentDispatcher);
        _refreshTimer.Start();

        _ = RefreshProcesses();
    }

    private void StartWebhookLoop()
    {
        var interval = SettingsService.Load().AutoWebhookInterval;
        _webhookTimer.Interval = TimeSpan.FromSeconds(Math.Max(1, interval));
        _webhookTimer.Tick += async (_, _) => await RunWebhookLoop();
        _webhookTimer.Start();
    }

    private void StopWebhookLoop()
    {
        _webhookTimer.Stop();
        _webhookTimer.Tick -= async (_, _) => await RunWebhookLoop();
    }

    private async Task RunWebhookLoop()
    {
        var settings = SettingsService.Load();
        if (string.IsNullOrWhiteSpace(settings.WebhookUrl))
        {
            WebhookStatus = Loc.WebhookNoUrl;
            AutoLoopActive = false;
            return;
        }

        foreach (var process in Processes)
        {
            if (!process.IsWebhookEnabled) continue;

            var ok = await _webhookService.SendScreenshot(settings.WebhookUrl, process.Id, process.DisplayName);
            WebhookStatus = ok ? Loc.ScreenshotSent : Loc.ScreenshotFailed;
        }
    }

    private readonly HashSet<int> _reportedProcesses = [];

    private async Task RefreshProcesses()
    {
        var running = _processService.GetProcesses();
        var currentIds = running.Select(p => p.Id).ToHashSet();

        for (int i = Processes.Count - 1; i >= 0; i--)
        {
            if (!currentIds.Contains(Processes[i].Id))
            {
                _reportedProcesses.Remove(Processes[i].Id);
                Processes.RemoveAt(i);
            }
        }

        foreach (var process in running)
        {
            if (!Processes.Any(p => p.Id == process.Id))
            {
                Processes.Add(process);
            }
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
        _webhookTimer.Stop();
    }
}
