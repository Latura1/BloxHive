using System.IO;
using System.Text.Json;
using BloxHive.Models;

namespace BloxHive.Services;

public static class SettingsService
{
    private static readonly string _filePath;

    static SettingsService()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        _filePath = Path.Combine(dir, "settings.json");
    }

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch (Exception ex) { LogService.Error("SettingsService.Load", ex.Message); }

        return new AppSettings();
    }

    public static event Action? Saved;

    public static void Save(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
            Saved?.Invoke();
        }
        catch (Exception ex) { LogService.Error("SettingsService.Save", ex.Message); }
    }
}
