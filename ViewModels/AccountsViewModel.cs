using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using BloxHive.Models;
using BloxHive.Services;
using BloxHive.Views;

namespace BloxHive.ViewModels;

public class AccountsViewModel : BaseViewModel
{
    private string _statusMessage = "";
    private readonly DispatcherTimer _statusTimer;

    public ObservableCollection<AccountInfo> Accounts { get; } = [];
    public int AccountCount => Accounts.Count;
    public string AccountCountText => string.Format(Loc.AccountsCount, AccountCount);
    public bool HasNoAccounts => AccountCount == 0;
    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    public bool IsExperimentalEnabled => SettingsService.Load().ExperimentalFeatures;

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            SetProperty(ref _statusMessage, value);
            OnPropertyChanged(nameof(HasStatusMessage));
        }
    }

    public ICommand AddAccountCommand { get; }
    public ICommand OpenAccountCommand { get; }
    public ICommand DeleteAccountCommand { get; }
    public ICommand SaveProxyCommand { get; }
    public ICommand ClearCacheCommand { get; }

    public AccountsViewModel()
    {
        AddAccountCommand = new RelayCommand(async _ => await AddAccount());
        OpenAccountCommand = new RelayCommand(param =>
        {
            if (param is AccountInfo account)
                OpenAccount(account);
        });
        DeleteAccountCommand = new RelayCommand(param =>
        {
            if (param is AccountInfo account)
                DeleteAccount(account);
        });
        SaveProxyCommand = new RelayCommand(param =>
        {
            if (param is AccountInfo account)
                SaveProxy(account);
        });
        ClearCacheCommand = new RelayCommand(_ =>
        {
            AccountService.ClearWebView2Cache();
            RobloxApiService.ClearCookieCache();
            StatusMessage = Loc.CacheCleared;
        });

        LoadAccounts();

        SettingsService.Saved += () =>
        {
            OnPropertyChanged(nameof(IsExperimentalEnabled));
        };

        _statusTimer = new DispatcherTimer(TimeSpan.FromSeconds(30), DispatcherPriority.Background, async (_, _) => await RefreshStatus(), Dispatcher.CurrentDispatcher);
        _statusTimer.Start();
    }

    private void LoadAccounts()
    {
        Accounts.Clear();
        foreach (var account in AccountService.Load())
            Accounts.Add(account);
        RefreshCounts();
        _ = RefreshStatus();
        TrayService.RefreshAccounts();
    }

    private async Task RefreshStatus()
    {
        var accounts = Accounts.ToList();
        if (accounts.Count == 0) return;
        await RobloxApiService.UpdatePresence(accounts);
    }

    private void RefreshCounts()
    {
        OnPropertyChanged(nameof(AccountCount));
        OnPropertyChanged(nameof(AccountCountText));
        OnPropertyChanged(nameof(HasNoAccounts));
    }

    private async Task AddAccount()
    {
        var window = new AccountLoginWindow();
        if (window.ShowDialog() == true && window.Result != null)
        {
            if (Accounts.Any(a => a.UserId == window.Result.UserId))
            {
                StatusMessage = string.Format(Loc.AccountExists, window.Result.DisplayName);
                return;
            }
            Accounts.Add(window.Result);
            AccountService.Save([.. Accounts]);
            TrayService.RefreshAccounts();
            RefreshCounts();
            StatusMessage = string.Format(Loc.AccountAdded, window.Result.DisplayName);
        }
    }

    private void OpenAccount(AccountInfo account)
    {
        var window = new AccountWindow(account);
        window.Show();
    }

    private void DeleteAccount(AccountInfo account)
    {
        var result = MessageBox.Show(
            string.Format(Loc.DeleteAccountConfirm, account.DisplayName),
            Loc.DeleteAccountTitle,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            Accounts.Remove(account);
            AccountService.Save([.. Accounts]);
            TrayService.RefreshAccounts();
            RefreshCounts();
            StatusMessage = string.Format(Loc.AccountDeleted, account.DisplayName);
        }
    }

    private void SaveProxy(AccountInfo account)
    {
        AccountService.Save([.. Accounts]);
        TrayService.RefreshAccounts();
        StatusMessage = string.Format(Loc.ProxySaved, account.DisplayName);
    }
}
