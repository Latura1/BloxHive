namespace BloxHive.Models;

public class AppSettings
{
    public string Language { get; set; } = "de";
    public string Theme { get; set; } = "Midnight";
    public bool MultiInstanceActive { get; set; }
    public string WebhookUrl { get; set; } = "";
    public int AutoWebhookInterval { get; set; } = 10;
}
