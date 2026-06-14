using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using BloxHive.Models;
using BloxHive.Services;
using BloxHive.Views;

namespace BloxHive.ViewModels;

public class AccountsViewModel : BaseViewModel
{
    private string _statusMessage = "";

    public ObservableCollection<AccountInfo> Accounts { get; } = [];
    public int AccountCount => Accounts.Count;
    public string AccountCountText => string.Format(Loc.AccountsCount, AccountCount);
    public bool HasNoAccounts => AccountCount == 0;
    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

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
        ClearCacheCommand = new RelayCommand(_ =>
        {
            AccountService.ClearWebView2Cache();
            StatusMessage = Loc.CacheCleared;
        });

        LoadAccounts();
    }

    private void LoadAccounts()
    {
        Accounts.Clear();
        foreach (var account in AccountService.Load())
            Accounts.Add(account);
        RefreshCounts();
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
            RefreshCounts();
            StatusMessage = string.Format(Loc.AccountDeleted, account.DisplayName);
        }
    }
}
