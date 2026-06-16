using System.Windows;
using BloxHive.Models;
using BloxHive.Services;
using BloxHive.Views;

namespace BloxHive;

public partial class App : Application
{
    public static AuthSession? CurrentSession { get; set; }
    public App()
    {
        DispatcherUnhandledException += (_, e) =>
        {
            e.Handled = true;
            MessageBox.Show($"An unexpected error occurred.\n\n{e.Exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(1);
        };
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var settings = SettingsService.Load();

            Translation.Instance.IsEnglish = settings.Language == "en";

            var theme = ThemeDefinition.All.FirstOrDefault(t => t.Name == settings.Theme) ?? ThemeDefinition.Default;
            ThemeManager.Instance.ApplyTheme(theme);
            ThemeManager.Instance.Initialize();

            if (!await TryAuthenticateAsync(settings))
                return;

            if (settings.MultiInstanceActive)
                MutexService.Instance.Acquire();

            if (settings.DashboardAutoStart)
                DashboardService.Start(settings.DashboardPort, settings.DashboardPasswordHash);

            var mainWindow = new MainWindow();
            TrayService.Initialize(mainWindow);

            if (settings.StartMinimized)
                mainWindow.WindowState = WindowState.Minimized;
            else
                mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start application.\n\n{ex.Message}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private static async Task<bool> TryAuthenticateAsync(AppSettings settings)
    {
        if (!string.IsNullOrEmpty(settings.AuthToken))
        {
            try
            {
                var (success, session, _) = await AuthService.VerifyTokenAsync(settings.AuthToken);
                if (success && session?.IsValid == true)
                {
                    CurrentSession = session;
                    return true;
                }
            }
            catch { }
        }

        try
        {
            var loginWindow = new AuthLoginView();
            if (loginWindow.ShowDialog() == true && loginWindow.Session?.IsValid == true)
            {
                CurrentSession = loginWindow.Session;
                return true;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Login failed.\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        Application.Current.Shutdown();
        return false;
    }
}
