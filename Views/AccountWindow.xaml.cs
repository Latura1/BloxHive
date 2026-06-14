using System.Windows;
using System.Windows.Input;
using BloxHive.Models;
using BloxHive.Services;
using Microsoft.Web.WebView2.Core;

namespace BloxHive.Views;

public partial class AccountWindow : Window
{
    private readonly AccountInfo _account;

    public AccountWindow(AccountInfo account)
    {
        _account = account;
        InitializeComponent();
        Owner = Application.Current.MainWindow;
        TitleText.Text = $"BloxHive - {account.DisplayName}";
        Loaded += async (_, _) => await InitializeWebView();
    }

    private async Task InitializeWebView()
    {
        var userDataFolder = AccountService.GetAccountUserDataFolder(_account.UserId);
        var env = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
        await AccountWebView.EnsureCoreWebView2Async(env);

        AccountWebView.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = false;
        AccountWebView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;

        var cookie = AccountService.Decrypt(_account.CookieEncrypted);
        var cookieObj = AccountWebView.CoreWebView2.CookieManager.CreateCookie(
            ".ROBLOSECURITY", cookie, ".roblox.com", "/");
        AccountWebView.CoreWebView2.CookieManager.AddOrUpdateCookie(cookieObj);

        AccountWebView.CoreWebView2.Navigate("https://www.roblox.com/home");
    }

    private void ReloadClick(object sender, RoutedEventArgs e)
    {
        AccountWebView?.Reload();
    }

    private async void ClearCacheClick(object sender, RoutedEventArgs e)
    {
        AccountWebView.CoreWebView2?.CookieManager?.DeleteAllCookies();
        AccountService.ClearWebView2Cache();
        AccountWebView.Reload();
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void CloseClick(object sender, RoutedEventArgs e) => Close();
}
