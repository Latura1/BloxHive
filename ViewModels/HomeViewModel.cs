using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Threading;
using BloxHive.Models;
using BloxHive.Services;
using BloxHive.Views;

namespace BloxHive.ViewModels;

public class HomeViewModel : BaseViewModel, IDisposable
{
    private static HomeViewModel? _instance;
    private readonly MutexService _mutexService = MutexService.Instance;
    private readonly RobloxProcessService _processService = new();
    private readonly WebhookService _webhookService = new();
    private readonly DispatcherTimer _refreshTimer;
    private readonly DispatcherTimer _webhookTimer = new();
    private readonly EventHandler _webhookTickHandler;
    private readonly DispatcherTimer _accountStatusTimer = new();
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
    public ObservableCollection<AccountInfo> SavedAccounts { get; } = [];

    public int ProcessCount
    {
        get => _processCount;
        set => SetProperty(ref _processCount, value);
    }

    public string ProcessCountText => string.Format(Loc.RunningInstancesCount, ProcessCount);
    public bool HasSavedAccounts => SavedAccounts.Count > 0;

    public ICommand KillProcessCommand { get; }
    public ICommand KillAllCommand { get; }
    public ICommand TestWebhookCommand { get; }
    public ICommand TestInstanceCommand { get; }
    public ICommand OpenSavedAccountCommand { get; }

    public HomeViewModel()
    {
        _instance = this;
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

        OpenSavedAccountCommand = new RelayCommand(param =>
        {
            if (param is AccountInfo account)
            {
                var window = new AccountWindow(account);
                window.Show();
            }
        });

        _webhookTickHandler = async (_, _) => await RunWebhookLoop();

        _refreshTimer = new DispatcherTimer(TimeSpan.FromSeconds(2), DispatcherPriority.Background, (_, _) => _ = RefreshProcesses(), Dispatcher.CurrentDispatcher);
        _refreshTimer.Start();

        _accountStatusTimer = new DispatcherTimer(TimeSpan.FromSeconds(30), DispatcherPriority.Background, async (_, _) => await RefreshAccountStatus(), Dispatcher.CurrentDispatcher);
        _accountStatusTimer.Start();

        _ = RefreshProcesses();
        LoadSavedAccounts();
    }

    private void LoadSavedAccounts()
    {
        SavedAccounts.Clear();
        foreach (var account in AccountService.Load())
            SavedAccounts.Add(account);
        OnPropertyChanged(nameof(HasSavedAccounts));
        _ = RefreshAccountStatus();
    }

    private async Task RefreshAccountStatus()
    {
        var accounts = SavedAccounts.ToList();
        if (accounts.Count == 0) return;
        await RobloxApiService.UpdatePresence(accounts);
    }

    private void StartWebhookLoop()
    {
        var interval = SettingsService.Load().AutoWebhookInterval;
        _webhookTimer.Interval = TimeSpan.FromSeconds(Math.Max(1, interval));
        _webhookTimer.Tick += _webhookTickHandler;
        _webhookTimer.Start();
    }

    private void StopWebhookLoop()
    {
        _webhookTimer.Tick -= _webhookTickHandler;
        _webhookTimer.Stop();
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

    public static bool GetLoopActive() => _staticLoopActive;

    public static void ExternalToggleLoop(bool active)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (_instance != null)
                _instance.AutoLoopActive = active;
        });
    }

    internal static List<RobloxProcessInfo> GetProcessList()
    {
        if (_instance != null)
            return _instance.Processes.ToList();
        return new RobloxProcessService().GetProcesses();
    }

    internal static List<AccountInfo> GetAccountList()
    {
        if (_instance != null)
            return _instance.SavedAccounts.ToList();
        return AccountService.Load();
    }

    internal static void SetProcessWebhook(int pid, bool enabled)
    {
        if (_instance == null) { Debug.WriteLine("[HomeViewModel] SetProcessWebhook: _instance ist null"); return; }
        try
        {
            System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var proc = _instance.Processes.FirstOrDefault(p => p.Id == pid);
                if (proc != null)
                {
                    proc.IsWebhookEnabled = enabled;
                    Debug.WriteLine($"[HomeViewModel] Webhook gesetzt: PID={pid}, Enabled={enabled}");
                }
                else
                {
                    Debug.WriteLine($"[HomeViewModel] Prozess PID={pid} nicht gefunden");
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HomeViewModel] Dispatcher FEHLER: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _refreshTimer.Stop();
        _webhookTimer.Stop();
        _accountStatusTimer.Stop();
    }
}
