using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QRCoder;
using BloxHive.Models;
using BloxHive.Services;

namespace BloxHive.ViewModels;

public class DashboardViewModel : BaseViewModel, IDisposable
{
    private int _port;
    private string _passwordInput = "";
    private bool _isRunning;
    private string _localUrl = "";
    private string _networkUrl = "";
    private ImageSource? _qrCodeImage;
    private bool _hasPassword;
    private bool _isLocalOnly;
    private string _statusMessage = "";

    public int Port
    {
        get => _port;
        set
        {
            if (SetProperty(ref _port, value))
            {
                var s = SettingsService.Load();
                s.DashboardPort = value;
                SettingsService.Save(s);
            }
        }
    }

    public string PasswordInput
    {
        get => _passwordInput;
        set => SetProperty(ref _passwordInput, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if (SetProperty(ref _isRunning, value))
            {
                OnPropertyChanged(nameof(CanStart));
                OnPropertyChanged(nameof(CanStop));
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    public string LocalUrl
    {
        get => _localUrl;
        set => SetProperty(ref _localUrl, value);
    }

    public string NetworkUrl
    {
        get => _networkUrl;
        set => SetProperty(ref _networkUrl, value);
    }

    public ImageSource? QrCodeImage
    {
        get => _qrCodeImage;
        set => SetProperty(ref _qrCodeImage, value);
    }

    public bool HasPassword
    {
        get => _hasPassword;
        set
        {
            if (SetProperty(ref _hasPassword, value))
                OnPropertyChanged(nameof(PasswordStatusText));
        }
    }

    public bool CanStart => !IsRunning;
    public bool CanStop => IsRunning;
    public bool IsLocalOnly
    {
        get => _isLocalOnly;
        set
        {
            if (SetProperty(ref _isLocalOnly, value))
                OnPropertyChanged(nameof(HasWarning));
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (SetProperty(ref _statusMessage, value))
                OnPropertyChanged(nameof(HasWarning));
        }
    }

    public bool HasWarning => !string.IsNullOrEmpty(StatusMessage) || IsLocalOnly;

    public string StatusText => IsRunning ? Loc.DashboardRunning : Loc.DashboardStopped;
    public string PasswordStatusText => HasPassword ? Loc.DashboardPasswordSet : Loc.DashboardPasswordNotSet;

    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand SetPasswordCommand { get; }

    public DashboardViewModel()
    {
        var settings = SettingsService.Load();
        _port = settings.DashboardPort;
        _hasPassword = !string.IsNullOrEmpty(settings.DashboardPasswordHash);

        StartCommand = new RelayCommand(_ => Start());
        StopCommand = new RelayCommand(_ => Stop());
        SetPasswordCommand = new RelayCommand(_ => SetPassword());

        DashboardService.RunningChanged += OnRunningChanged;

        if (DashboardService.IsRunning)
        {
            UpdateUrls();
            IsRunning = true;
        }
    }

    private void Start()
    {
        if (IsRunning) return;
        var settings = SettingsService.Load();
        StatusMessage = "";
        try
        {
            var result = DashboardService.Start(Port, settings.DashboardPasswordHash);
            IsLocalOnly = !DashboardService.IsNetworkAccessible;
            if (IsLocalOnly)
                StatusMessage = Loc.DashboardStartFailed;
            UpdateUrls();
            IsRunning = true;
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Loc.DashboardStartError, ex.Message);
        }
    }

    private void Stop()
    {
        if (!IsRunning) return;
        DashboardService.Stop();
        IsRunning = false;
        LocalUrl = "";
        NetworkUrl = "";
        QrCodeImage = null;
    }

    private void SetPassword()
    {
        if (string.IsNullOrWhiteSpace(PasswordInput))
        {
            var s = SettingsService.Load();
            s.DashboardPasswordHash = "";
            SettingsService.Save(s);
            HasPassword = false;
            PasswordInput = "";
            OnPropertyChanged(nameof(PasswordInput));
            return;
        }

        var hash = DashboardService.ToSha256(PasswordInput.Trim());
        var settings = SettingsService.Load();
        settings.DashboardPasswordHash = hash;
        SettingsService.Save(settings);
        HasPassword = true;
        PasswordInput = "";
        OnPropertyChanged(nameof(PasswordInput));
    }

    private void UpdateUrls()
    {
        LocalUrl = DashboardService.LocalUrl;
        NetworkUrl = DashboardService.NetworkUrl;
        GenerateQrCode(NetworkUrl);
    }

    private void GenerateQrCode(string url)
    {
        try
        {
            using var generator = new QRCodeGenerator();
            using var qrData = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
            using var png = new PngByteQRCode(qrData);
            var bytes = png.GetGraphic(20);

            var image = new BitmapImage();
            using var ms = new MemoryStream(bytes);
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = ms;
            image.EndInit();
            image.Freeze();
            QrCodeImage = image;
        }
        catch
        {
            QrCodeImage = null;
        }
    }

    private void OnRunningChanged(bool running)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (running)
                UpdateUrls();
            else
            {
                LocalUrl = "";
                NetworkUrl = "";
                QrCodeImage = null;
            }
            IsRunning = running;
        });
    }

    public void Dispose()
    {
        DashboardService.RunningChanged -= OnRunningChanged;
    }
}
