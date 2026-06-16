using System.Windows;
using BloxHive.Models;
using BloxHive.Services;
using BloxHive.ViewModels;

namespace BloxHive.Views;

public partial class AuthLoginView : Window
{
    private readonly AuthViewModel _vm;

    public AuthSession? Session => _vm.Session;

    public Translation Loc => Translation.Instance;

    public AuthLoginView()
    {
        try
        {
            InitializeComponent();
        }
        catch
        {
            MessageBox.Show("Failed to initialize login window.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(1);
        }

        _vm = new AuthViewModel();
        DataContext = _vm;
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AuthViewModel.Session) && _vm.Session != null)
            {
                DialogResult = true;
                Close();
            }
        };
    }

    private void LoginPasswordChanged(object sender, RoutedEventArgs e)
    {
        _vm.Password = LoginPasswordBox.Password;
    }

    private void RegisterPasswordChanged(object sender, RoutedEventArgs e)
    {
        _vm.Password = RegisterPasswordBox.Password;
    }

    private void RegisterConfirmPasswordChanged(object sender, RoutedEventArgs e)
    {
        _vm.ConfirmPassword = RegisterConfirmPasswordBox.Password;
    }
}
