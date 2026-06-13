using System.Windows;
using BloxHive.Services;

namespace BloxHive;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ThemeManager.Instance.Initialize();
    }
}
