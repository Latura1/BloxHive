using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using BloxHive.Models;

namespace BloxHive.Services;

public static class AuthService
{
    private static readonly HttpClient _client = new() { Timeout = TimeSpan.FromSeconds(15) };

    private static string GetBaseUrl()
    {
        try
        {
            return SettingsService.Load().ServerUrl.TrimEnd('/');
        }
        catch
        {
            return "https://bloxhive-api.your-server.com";
        }
    }

    public static async Task<(bool Success, string? Message)> RegisterAsync(string key, string username, string password)
    {
        try
        {
            var response = await _client.PostAsJsonAsync($"{GetBaseUrl()}/api/auth/register", new
            {
                key,
                username,
                password
            });

            var body = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
                return (true, null);

            var err = TryGetError(body);
            return (false, err);
        }
        catch (TaskCanceledException)
        {
            return (false, "AuthErrorServer");
        }
        catch (HttpRequestException)
        {
            return (false, "AuthErrorServer");
        }
        catch
        {
            return (false, "AuthErrorServer");
        }
    }

    public static async Task<(bool Success, AuthSession? Session, string? Message)> LoginAsync(string username, string password)
    {
        try
        {
            var hwid = HwidService.Generate();

            var response = await _client.PostAsJsonAsync($"{GetBaseUrl()}/api/auth/login", new
            {
                username,
                password,
                hwid
            });

            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var err = TryGetError(body);
                return (false, null, err);
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var token = root.GetProperty("token").GetString() ?? "";
            var user = root.GetProperty("user");

            var session = new AuthSession
            {
                Token = token,
                Username = user.GetProperty("username").GetString() ?? username,
                UserId = user.GetProperty("id").GetInt32(),
                ExpiresAt = user.TryGetProperty("expiresAt", out var exp) && exp.ValueKind == JsonValueKind.String
                    ? DateTime.Parse(exp.GetString()!)
                    : null,
                CreatedAt = user.TryGetProperty("createdAt", out var ca) && ca.ValueKind == JsonValueKind.String
                    ? DateTime.Parse(ca.GetString()!)
                    : null,
                IsActive = user.GetProperty("isActive").GetBoolean(),
            };

            var settings = SettingsService.Load();
            settings.AuthToken = token;
            settings.AuthUsername = session.Username;
            SettingsService.Save(settings);

            return (true, session, null);
        }
        catch (TaskCanceledException)
        {
            return (false, null, "AuthErrorServer");
        }
        catch (HttpRequestException)
        {
            return (false, null, "AuthErrorServer");
        }
        catch (JsonException)
        {
            return (false, null, "AuthErrorServer");
        }
        catch
        {
            return (false, null, "AuthErrorServer");
        }
    }

    public static async Task<(bool Success, AuthSession? Session, string? Message)> VerifyTokenAsync(string token)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{GetBaseUrl()}/api/auth/verify");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var err = TryGetError(body);
                return (false, null, err);
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var session = new AuthSession
            {
                Token = token,
                Username = root.GetProperty("username").GetString() ?? "",
                UserId = root.GetProperty("id").GetInt32(),
                ExpiresAt = root.TryGetProperty("expiresAt", out var exp) && exp.ValueKind == JsonValueKind.String
                    ? DateTime.Parse(exp.GetString()!)
                    : null,
                CreatedAt = root.TryGetProperty("createdAt", out var ca) && ca.ValueKind == JsonValueKind.String
                    ? DateTime.Parse(ca.GetString()!)
                    : null,
                IsActive = root.GetProperty("isActive").GetBoolean(),
            };

            return (true, session, null);
        }
        catch (TaskCanceledException)
        {
            return (false, null, "AuthErrorServer");
        }
        catch (HttpRequestException)
        {
            return (false, null, "AuthErrorServer");
        }
        catch (JsonException)
        {
            return (false, null, "AuthErrorServer");
        }
        catch
        {
            return (false, null, "AuthErrorServer");
        }
    }

    public static void Logout()
    {
        try
        {
            var settings = SettingsService.Load();
            settings.AuthToken = null;
            settings.AuthUsername = null;
            SettingsService.Save(settings);
        }
        catch { }
    }

    private static string? TryGetError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var err))
                return err.GetString();
        }
        catch { }
        return "Unknown error";
    }
}
