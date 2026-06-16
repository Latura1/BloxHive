using System.Windows.Input;
using BloxHive.Models;
using BloxHive.Services;

namespace BloxHive.ViewModels;

public class AuthViewModel : BaseViewModel
{
    private string _licenseKey = "";
    private string _username = "";
    private string _password = "";
    private string _confirmPassword = "";
    private string _statusMessage = "";
    private bool _isBusy;
    private bool _isLoginMode = true;
    private AuthSession? _session;

    public string LicenseKey { get => _licenseKey; set => SetProperty(ref _licenseKey, value); }
    public string Username { get => _username; set => SetProperty(ref _username, value); }
    public string Password { get => _password; set => SetProperty(ref _password, value); }
    public string ConfirmPassword { get => _confirmPassword; set => SetProperty(ref _confirmPassword, value); }
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
    public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }
    public bool IsLoginMode { get => _isLoginMode; set { SetProperty(ref _isLoginMode, value); OnPropertyChanged(nameof(IsRegisterMode)); } }
    public bool IsRegisterMode => !IsLoginMode;

    public AuthSession? Session { get => _session; private set => SetProperty(ref _session, value); }

    public ICommand SwitchToRegisterCommand { get; }
    public ICommand SwitchToLoginCommand { get; }
    public ICommand SubmitCommand { get; }

    public AuthViewModel()
    {
        SwitchToRegisterCommand = new RelayCommand(_ =>
        {
            IsLoginMode = false;
            StatusMessage = "";
        });

        SwitchToLoginCommand = new RelayCommand(_ =>
        {
            IsLoginMode = true;
            StatusMessage = "";
        });

        SubmitCommand = new RelayCommand(async _ =>
        {
            try
            {
                if (IsLoginMode)
                    await LoginAsync();
                else
                    await RegisterAsync();
            }
            catch
            {
                IsBusy = false;
                StatusMessage = Loc.AuthErrorServer;
            }
        }, _ => !IsBusy);
    }

    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            StatusMessage = Loc.AuthErrorInvalidCredentials;
            return;
        }

        IsBusy = true;
        StatusMessage = Loc.AuthLoggingIn;

        var (success, session, error) = await AuthService.LoginAsync(Username.Trim(), Password);

        IsBusy = false;

        if (success && session != null)
        {
            Session = session;
            StatusMessage = "";
            return;
        }

        StatusMessage = MapError(error ?? "Unknown");
    }

    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(LicenseKey) || string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            StatusMessage = Loc.AuthErrorInvalidKey;
            return;
        }

        if (Username.Trim().Length < 3 || Username.Trim().Length > 50)
        {
            StatusMessage = Loc.AuthErrorUsernameLength;
            return;
        }

        if (Password.Length < 6)
        {
            StatusMessage = Loc.AuthErrorPasswordLength;
            return;
        }

        if (Password != ConfirmPassword)
        {
            StatusMessage = Loc.AuthErrorPasswordsDontMatch;
            return;
        }

        IsBusy = true;
        StatusMessage = Loc.AuthRegistering;

        var (success, error) = await AuthService.RegisterAsync(LicenseKey.Trim(), Username.Trim(), Password);

        IsBusy = false;

        if (success)
        {
            IsLoginMode = true;
            StatusMessage = Loc.AuthRegisterSuccess;
            LicenseKey = "";
            ConfirmPassword = "";
            return;
        }

        StatusMessage = MapError(error ?? "Unknown");
    }

    private string MapError(string error)
    {
        return error switch
        {
            "AuthErrorServer" => Loc.AuthErrorServer,
            "AuthErrorExpired" => Loc.AuthErrorExpired,
            "AuthErrorHwidBound" => Loc.AuthErrorHwidBound,
            "AuthErrorDeactivated" => Loc.AuthErrorDeactivated,
            "Ungültiger oder bereits verwendeter Key." or "Invalid or already used key." => Loc.AuthErrorInvalidKey,
            "Username bereits vergeben." or "Username already taken." => Loc.AuthErrorUsernameTaken,
            "Falscher Username oder Passwort." or "Invalid username or password." => Loc.AuthErrorInvalidCredentials,
            "Account ist abgelaufen." or "Account has expired." => Loc.AuthErrorExpired,
            "HWID gebunden an anderen Rechner. Kontaktiere den Admin." or "HWID bound to another machine. Contact admin." => Loc.AuthErrorHwidBound,
            "Account wurde deaktiviert." or "Account has been deactivated." => Loc.AuthErrorDeactivated,
            _ => error
        };
    }
}
