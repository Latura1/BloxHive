using System.Windows;
using System.Windows.Input;
using BloxHive.Models;
using BloxHive.Services;
using Microsoft.Web.WebView2.Core;

namespace BloxHive.Views;

public partial class AccountWindow : Window
{
    public AccountWindow(AccountInfo account)
    {
        InitializeComponent();
        Owner = Application.Current.MainWindow;
        TitleText.Text = $"BloxHive - {account.DisplayName}";
        Loaded += async (_, _) => await InitializeWebView(account);
    }

    private async Task InitializeWebView(AccountInfo account)
    {
        var env = await CoreWebView2Environment.CreateAsync(userDataFolder: AccountService.GetWebView2UserDataFolder());
        await AccountWebView.EnsureCoreWebView2Async(env);

        AccountWebView.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = false;
        AccountWebView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;

        var cookie = AccountService.Decrypt(account.CookieEncrypted);
        var cookieObj = AccountWebView.CoreWebView2.CookieManager.CreateCookie(
            ".ROBLOSECURITY", cookie, ".roblox.com", "/");
        AccountWebView.CoreWebView2.CookieManager.AddOrUpdateCookie(cookieObj);

        AccountWebView.CoreWebView2.Navigate("https://www.roblox.com/home");
    }

    public async Task ClearCache()
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

    private async void ClearCacheClick(object sender, RoutedEventArgs e)
    {
        await ClearCache();
    }

    private void CloseClick(object sender, RoutedEventArgs e) => Close();
}
