using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using BloxHive.Models;
using BloxHive.Services;
using Microsoft.Web.WebView2.Core;

namespace BloxHive.Views;

public partial class AccountLoginWindow : Window
{
    private readonly System.Timers.Timer _cookieCheckTimer = new(500);
    private bool _cookieFound;
    private string? _authCookie;
    private bool _completed;

    public AccountInfo? Result { get; private set; }

    public AccountLoginWindow()
    {
        InitializeComponent();
        Owner = Application.Current.MainWindow;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var env = await CoreWebView2Environment.CreateAsync(userDataFolder: AccountService.GetWebView2UserDataFolder());
        await LoginWebView.EnsureCoreWebView2Async(env);

        LoginWebView.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = false;
        LoginWebView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;

        LoginWebView.CoreWebView2.NavigationStarting += OnNavigationStarting;
        LoginWebView.CoreWebView2.SourceChanged += OnSourceChanged;

        _cookieCheckTimer.Elapsed += (_, _) => Dispatcher.Invoke(CheckCookie);
        _cookieCheckTimer.Start();

        LoginWebView.CoreWebView2.Navigate("https://www.roblox.com/login");
        LoadingOverlay.Visibility = Visibility.Collapsed;
    }

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (_cookieFound) return;
        Dispatcher.Invoke(() =>
        {
            InfoText.Text = "Navigiere...";
        });
    }

    private void OnSourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
    {
        if (_cookieFound) return;
        var url = LoginWebView.Source?.ToString() ?? "";
        Dispatcher.Invoke(() =>
        {
            if (url.StartsWith("https://www.roblox.com/home") || url.Contains("/login?returnUrl"))
            {
                InfoText.Text = "Login erkannt, prüfe Cookie...";
                CheckCookie();
            }
            else
            {
                InfoText.Text = $"Seite geladen: {url}";
            }
        });
    }

    private async void CheckCookie()
    {
        if (_cookieFound) return;
        try
        {
            var cookies = await LoginWebView.CoreWebView2.CookieManager.GetCookiesAsync("https://www.roblox.com");
            var auth = cookies.FirstOrDefault(c => c.Name == ".ROBLOSECURITY");
            if (auth != null && !string.IsNullOrEmpty(auth.Value))
            {
                _cookieFound = true;
                _authCookie = auth.Value;
                _cookieCheckTimer.Stop();
                StatusText.Text = "Cookie gefunden! Hole Account-Daten...";
                InfoText.Text = "Cookie gefunden, verarbeite...";
                DoneButton.IsEnabled = true;
                await FetchAndComplete();
            }
        }
        catch { }
    }

    private async Task FetchAndComplete()
    {
        if (_completed || string.IsNullOrEmpty(_authCookie)) return;

        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Cookie", $".ROBLOSECURITY={_authCookie}");
            var response = await client.GetAsync("https://users.roblox.com/v1/users/authenticated");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var userId = root.GetProperty("id").GetInt64();
                var name = root.GetProperty("name").GetString() ?? "Unknown";

                _completed = true;
                Result = new AccountInfo
                {
                    DisplayName = name,
                    UserId = userId,
                    CookieEncrypted = AccountService.Encrypt(_authCookie)
                };

                Dispatcher.Invoke(() =>
                {
                    InfoText.Text = $"✅ Angemeldet als {name}";
                    DialogResult = true;
                    Close();
                });
            }
        }
        catch { }

        if (!_completed)
        {
            Dispatcher.Invoke(() =>
            {
                InfoText.Text = "❌ Konnte Account-Daten nicht abrufen. Fertig drücken um trotzdem zu speichern.";
                DoneButton.IsEnabled = true;
            });
        }
    }

    private async void DoneClick(object sender, RoutedEventArgs e)
    {
        if (_completed) return;

        if (!string.IsNullOrEmpty(_authCookie))
        {
            _completed = true;
            _cookieCheckTimer.Stop();

            string name = "Unknown";
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Cookie", $".ROBLOSECURITY={_authCookie}");
                var response = await client.GetAsync("https://users.roblox.com/v1/users/authenticated");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    name = doc.RootElement.GetProperty("name").GetString() ?? "Unknown";
                }
            }
            catch { }

            Result = new AccountInfo
            {
                DisplayName = name,
                CookieEncrypted = AccountService.Encrypt(_authCookie)
            };

            DialogResult = true;
            Close();
        }
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void CloseClick(object sender, RoutedEventArgs e)
    {
        _cookieCheckTimer.Stop();
        DialogResult = false;
        Close();
    }
}
