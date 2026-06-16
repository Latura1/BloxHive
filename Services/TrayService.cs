using System.Windows;
using BloxHive.Models;
using BloxHive.Views;
using WF = System.Windows.Forms;

namespace BloxHive.Services;

public static class TrayService
{
    private static WF.NotifyIcon? _notifyIcon;
    private static Window? _mainWindow;

    public static void Initialize(Window mainWindow)
    {
        _mainWindow = mainWindow;

        _notifyIcon = new WF.NotifyIcon
        {
            Icon = IconGenerator.GenerateBIcon(),
            Text = "BloxHive",
            Visible = true,
        };

        _notifyIcon.DoubleClick += (_, _) => ShowWindow();

        BuildMenu();

        mainWindow.StateChanged += (_, _) =>
        {
            if (mainWindow.WindowState == WindowState.Minimized)
                mainWindow.Hide();
        };
    }

    public static void RefreshAccounts()
    {
        BuildMenu();
    }

    public static void ShowWindow()
    {
        if (_mainWindow == null) return;
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    public static void Dispose()
    {
        _notifyIcon?.Dispose();
        _notifyIcon = null;
    }

    public static void UpdateIcon()
    {
        if (_notifyIcon != null)
            _notifyIcon.Icon = IconGenerator.GenerateBIcon();
    }

    private static void BuildMenu()
    {
        if (_notifyIcon == null) return;

        var menu = new WF.ContextMenuStrip();
        menu.Items.Clear();

        var accounts = AccountService.Load();
        if (accounts.Count > 0)
        {
            foreach (var acc in accounts)
            {
                var item = menu.Items.Add(acc.DisplayName);
                var captured = acc;
                item.Click += (_, _) => OpenAccount(captured);
            }
            menu.Items.Add(new WF.ToolStripSeparator());
        }

        var showItem = menu.Items.Add("BloxHive öffnen");
        showItem.Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold);
        showItem.Click += (_, _) => ShowWindow();

        var exitItem = menu.Items.Add("Beenden");
        exitItem.Click += (_, _) => Exit();

        _notifyIcon.ContextMenuStrip = menu;
    }

    private static void OpenAccount(AccountInfo account)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var window = new AccountWindow(account);
            window.Show();
        });
    }

    private static void Exit()
    {
        Dispose();
        Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
    }
}
