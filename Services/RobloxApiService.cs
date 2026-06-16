using System.Net.Http;
using System.Text;
using System.Text.Json;
using BloxHive.Models;

namespace BloxHive.Services;

public static class RobloxApiService
{
    private static readonly HttpClient _client = new();
    private static string? _cachedCookie;

    private static string? GetAnyCookie(List<AccountInfo> accounts)
    {
        if (_cachedCookie != null) return _cachedCookie;
        foreach (var account in accounts)
        {
            if (!string.IsNullOrEmpty(account.CookieEncrypted))
            {
                try
                {
                    _cachedCookie = AccountService.Decrypt(account.CookieEncrypted);
                    return _cachedCookie;
                }
                catch (Exception ex) { LogService.Error("RobloxApiService.GetAnyCookie", ex.Message); }
            }
        }
        return null;
    }

    public static async Task UpdatePresence(List<AccountInfo> accounts)
    {
        if (accounts.Count == 0) return;
        var cookie = GetAnyCookie(accounts);
        if (cookie == null) return;

        foreach (var a in accounts)
        {
            a.StatusText = "Offline";
            a.IsOnline = false;
        }

        var userIds = accounts.Select(a => a.UserId).ToArray();
        var payload = JsonSerializer.Serialize(new { userIds });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "https://presence.roblox.com/v1/presence/users")
        {
            Content = content
        };
        request.Headers.Add("Cookie", $".ROBLOSECURITY={cookie}");

        try
        {
            var response = await _client.SendAsync(request);
            if (!response.IsSuccessStatusCode) return;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var presences = doc.RootElement.GetProperty("userPresences");

            foreach (var p in presences.EnumerateArray())
            {
                var uid = p.GetProperty("userPresenceType").GetInt32();
                var userId = p.GetProperty("userId").GetInt64();
                var account = accounts.FirstOrDefault(a => a.UserId == userId);
                if (account == null) continue;

                var universeId = p.TryGetProperty("universeId", out var uidEl) && uidEl.ValueKind == JsonValueKind.Number ? uidEl.GetInt64() : 0;

                if (uid == 2 && universeId > 0)
                {
                    var gameName = await GetGameName(universeId);
                    account.StatusText = gameName;
                    account.IsOnline = true;
                }
                else if (uid == 1)
                {
                    account.StatusText = "Online";
                    account.IsOnline = true;
                }
                else
                {
                    account.StatusText = "Offline";
                    account.IsOnline = false;
                }
            }
        }
        catch (Exception ex)
        {
            LogService.Error("RobloxApiService.UpdatePresence", ex.Message);
            _cachedCookie = null;
        }
    }

    private static async Task<string> GetGameName(long universeId)
    {
        try
        {
            var response = await _client.GetAsync($"https://games.roblox.com/v1/games?universeIds={universeId}");
            if (!response.IsSuccessStatusCode) return "In Game";

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");
            if (data.GetArrayLength() > 0)
            {
                return data[0].GetProperty("name").GetString() ?? "In Game";
            }
        }
        catch (Exception ex) { LogService.Error("RobloxApiService.GetGameName", ex.Message); }
        return "In Game";
    }

    public static void ClearCookieCache() => _cachedCookie = null;
}
