using System.Windows.Input;

namespace BloxHive.ViewModels;

public class MainViewModel : BaseViewModel
{
    private BaseViewModel? _currentView;
    public BaseViewModel? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public ICommand NavigateToHomeCommand { get; }
    public ICommand NavigateToSettingsCommand { get; }

    public MainViewModel()
    {
        NavigateToHomeCommand = new RelayCommand(_ => NavigateTo<HomeViewModel>());
        NavigateToSettingsCommand = new RelayCommand(_ => NavigateTo<SettingsViewModel>());

        CurrentView = new HomeViewModel();
    }

    private void NavigateTo<T>() where T : BaseViewModel, new()
    {
        CurrentView = new T();
    }
}
