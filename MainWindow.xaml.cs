using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using BloxHive.Services;
using BloxHive.ViewModels;

namespace BloxHive;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly DispatcherTimer _verifyTimer;

    public MainWindow()
    {
        InitializeComponent();
        Icon = IconGenerator.GenerateBIcon().ToImageSource();
        _vm = new MainViewModel();
        DataContext = _vm;

        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.ShowProfile))
            {
                ProfilePanel.Visibility = _vm.ShowProfile ? Visibility.Visible : Visibility.Collapsed;
            }
        };

        _verifyTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(2),
        };
        _verifyTimer.Tick += async (_, _) => await VerifySessionAsync();
        _verifyTimer.Start();
    }

    private async Task VerifySessionAsync()
    {
        var settings = SettingsService.Load();
        if (string.IsNullOrEmpty(settings.AuthToken)) return;

        var (success, session, _) = await AuthService.VerifyTokenAsync(settings.AuthToken);
        if (!success || session?.IsValid != true)
        {
            _verifyTimer.Stop();
            AuthService.Logout();
            Application.Current.Shutdown();
        }
        else
        {
            App.CurrentSession = session;
            _vm.RefreshSession();
        }
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void CloseClick(object sender, RoutedEventArgs e) => Close();

    private void MaximizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void MinimizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
        Hide();
    }
}
