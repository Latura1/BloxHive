using System.Windows.Input;

namespace BloxHive.ViewModels;

public class MainViewModel : BaseViewModel
{
    private BaseViewModel? _currentView;
    private readonly Dictionary<Type, BaseViewModel> _cachedViews = [];

    public BaseViewModel? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public ICommand NavigateToHomeCommand { get; }
    public ICommand NavigateToSettingsCommand { get; }
    public ICommand NavigateToAccountsCommand { get; }
    public ICommand NavigateToDashboardCommand { get; }

    public MainViewModel()
    {
        NavigateToHomeCommand = new RelayCommand(_ => NavigateTo<HomeViewModel>());
        NavigateToSettingsCommand = new RelayCommand(_ => NavigateTo<SettingsViewModel>());
        NavigateToAccountsCommand = new RelayCommand(_ => NavigateTo<AccountsViewModel>());
        NavigateToDashboardCommand = new RelayCommand(_ => NavigateTo<DashboardViewModel>());

        CurrentView = GetOrCreate<HomeViewModel>();
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
