using System.IO;
using System.IO.Compression;
using System.Text.Json;
using BloxHive.Models;

namespace BloxHive.Services;

public static class BackupService
{
    public static async Task ExportAsync(string filePath)
    {
        var accounts = AccountService.Load();
        var settings = SettingsService.Load();

        var accountsJson = JsonSerializer.Serialize(accounts, new JsonSerializerOptions { WriteIndented = true });
        var settingsJson = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });

        await Task.Run(() =>
        {
            using var stream = new FileStream(filePath, FileMode.Create);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Create);

            var accEntry = archive.CreateEntry("accounts.json");
            using (var writer = new StreamWriter(accEntry.Open()))
                writer.Write(accountsJson);

            var setEntry = archive.CreateEntry("settings.json");
            using (var writer = new StreamWriter(setEntry.Open()))
                writer.Write(settingsJson);
        });
    }

    public static async Task<(List<AccountInfo> Accounts, AppSettings Settings)> PreviewAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            using var stream = new FileStream(filePath, FileMode.Open);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            var accounts = new List<AccountInfo>();
            var settings = new AppSettings();

            foreach (var entry in archive.Entries)
            {
                using var reader = new StreamReader(entry.Open());
                var content = reader.ReadToEnd();

                if (entry.Name == "accounts.json")
                    accounts = JsonSerializer.Deserialize<List<AccountInfo>>(content) ?? [];
                else if (entry.Name == "settings.json")
                    settings = JsonSerializer.Deserialize<AppSettings>(content) ?? new AppSettings();
            }

            return (accounts, settings);
        });
    }

    public static async Task ImportAsync(string filePath)
    {
        var (accounts, settings) = await PreviewAsync(filePath);
        AccountService.Save(accounts);
        SettingsService.Save(settings);
    }
}
