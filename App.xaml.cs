using System.Windows;
using BloxHive.Models;
using BloxHive.Services;

namespace BloxHive;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settings = SettingsService.Load();

        Translation.Instance.IsEnglish = settings.Language == "en";

        var theme = ThemeDefinition.All.FirstOrDefault(t => t.Name == settings.Theme) ?? ThemeDefinition.Default;
        ThemeManager.Instance.ApplyTheme(theme);

        ThemeManager.Instance.Initialize();

        if (settings.MultiInstanceActive)
            MutexService.Instance.Acquire();

        if (settings.DashboardAutoStart)
            DashboardService.Start(settings.DashboardPort, settings.DashboardPasswordHash);
    }
}
