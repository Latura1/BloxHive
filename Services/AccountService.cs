using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BloxHive.Models;

namespace BloxHive.Services;

public static class AccountService
{
    private static readonly string _dir;
    private static readonly string _filePath;

    static AccountService()
    {
        _dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BloxHive");
        _filePath = Path.Combine(_dir, "accounts.json");
    }

    private static readonly byte[] _entropy = [0x42, 0x6C, 0x6F, 0x78, 0x48, 0x69, 0x76, 0x65];

    public static string Encrypt(string plainText)
    {
        var data = Encoding.UTF8.GetBytes(plainText);
        var encrypted = ProtectedData.Protect(data, _entropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encrypted);
    }

    public static string Decrypt(string encryptedBase64)
    {
        var data = Convert.FromBase64String(encryptedBase64);
        var decrypted = ProtectedData.Unprotect(data, _entropy, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(decrypted);
    }

    public static List<AccountInfo> Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<AccountInfo>>(json) ?? [];
            }
        }
        catch (Exception ex) { LogService.Error("AccountService.Load", ex.Message); }
        return [];
    }

    public static void Save(List<AccountInfo> accounts)
    {
        try
        {
            Directory.CreateDirectory(_dir);
            var json = JsonSerializer.Serialize(accounts, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex) { LogService.Error("AccountService.Save", ex.Message); }
    }

    public static string GetWebView2UserDataFolder()
    {
        var dir = Path.Combine(_dir, "WebView2");
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static string GetAccountUserDataFolder(long userId)
    {
        var dir = Path.Combine(_dir, "WebView2", "Accounts", userId.ToString());
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static void ClearWebView2Cache()
    {
        try
        {
            var webViewDir = Path.Combine(_dir, "WebView2");
            if (Directory.Exists(webViewDir))
                Directory.Delete(webViewDir, true);
        }
        catch (Exception ex) { LogService.Error("AccountService.ClearCache", ex.Message); }
    }
}
