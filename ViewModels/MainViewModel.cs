using System.Windows;
using System.Windows.Input;
using BloxHive.Models;
using BloxHive.Services;

namespace BloxHive.ViewModels;

public class MainViewModel : BaseViewModel
{
    private BaseViewModel? _currentView;
    private readonly Dictionary<Type, BaseViewModel> _cachedViews = [];
    private bool _showProfile;

    public BaseViewModel? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public bool ShowProfile { get => _showProfile; set => SetProperty(ref _showProfile, value); }

    public AuthSession? Session => App.CurrentSession;

    public string SessionUsername => Session?.Username ?? "";
    public string SessionAge => Session?.CreatedAt.HasValue == true
        ? $"{(DateTime.UtcNow - Session.CreatedAt.Value).TotalDays:F0} days"
        : "";
    public string SessionExpiry => Session?.ExpiresAt.HasValue == true
        ? (Session.ExpiresAt.Value > DateTime.UtcNow
            ? $"{(Session.ExpiresAt.Value - DateTime.UtcNow).TotalDays:F0} days"
            : "Expired")
        : "Permanent";

    public ICommand NavigateToHomeCommand { get; }
    public ICommand NavigateToSettingsCommand { get; }
    public ICommand NavigateToAccountsCommand { get; }
    public ICommand NavigateToDashboardCommand { get; }
    public ICommand NavigateToLogsCommand { get; }
    public ICommand NavigateToBackupCommand { get; }
    public ICommand ToggleProfileCommand { get; }
    public ICommand LogoutCommand { get; }

    public MainViewModel()
    {
        NavigateToHomeCommand = new RelayCommand(_ => NavigateTo<HomeViewModel>());
        NavigateToSettingsCommand = new RelayCommand(_ => NavigateTo<SettingsViewModel>());
        NavigateToAccountsCommand = new RelayCommand(_ => NavigateTo<AccountsViewModel>());
        NavigateToDashboardCommand = new RelayCommand(_ => NavigateTo<DashboardViewModel>());
        NavigateToLogsCommand = new RelayCommand(_ => NavigateTo<LogsViewModel>());
        NavigateToBackupCommand = new RelayCommand(_ => NavigateTo<BackupViewModel>());
        ToggleProfileCommand = new RelayCommand(_ => ShowProfile = !ShowProfile);
        LogoutCommand = new RelayCommand(_ => DoLogout());

        CurrentView = GetOrCreate<HomeViewModel>();
    }

    public void RefreshSession()
    {
        OnPropertyChanged(nameof(Session));
        OnPropertyChanged(nameof(SessionUsername));
        OnPropertyChanged(nameof(SessionAge));
        OnPropertyChanged(nameof(SessionExpiry));
    }

    private void DoLogout()
    {
        AuthService.Logout();
        Application.Current.Shutdown();
    }

    private void NavigateTo<T>() where T : BaseViewModel, new()
    {
        CurrentView = GetOrCreate<T>();
    }

    private T GetOrCreate<T>() where T : BaseViewModel, new()
    {
        if (!_cachedViews.TryGetValue(typeof(T), out var view))
        {
            view = new T();
            _cachedViews[typeof(T)] = view;
        }
        return (T)view;
    }
}
