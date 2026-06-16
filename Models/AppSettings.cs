namespace BloxHive.Models;

public class AppSettings
{
    public string Language { get; set; } = "de";
    public string Theme { get; set; } = "Midnight";
    public bool MultiInstanceActive { get; set; }
    public string WebhookUrl { get; set; } = "";
    public int AutoWebhookInterval { get; set; } = 10;
    public bool ExperimentalFeatures { get; set; }
    public int DashboardPort { get; set; } = 5000;
    public string DashboardPasswordHash { get; set; } = "";
    public bool DashboardAutoStart { get; set; }
    public string? AuthToken { get; set; }
    public string? AuthUsername { get; set; }
    public string ServerUrl { get; set; } = "https://bloxhive-api.your-server.com";
    public bool StartWithWindows { get; set; }
    public bool StartMinimized { get; set; }
}
