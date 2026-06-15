using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace BloxHive.Services;

public static partial class TunnelService
{
    private static Process? _process;
    private static string? _publicUrl;
    private static string? _binaryPath;

    public static string? PublicUrl => _publicUrl;
    public static bool IsRunning => _process is { HasExited: false };

    public static event Action<string?>? UrlChanged;
    public static event Action<string>? StatusChanged;

    public static async Task StartAsync(int localPort)
    {
        await StopAsync();

        _binaryPath = GetBinaryPath();
        if (!File.Exists(_binaryPath))
        {
            StatusChanged?.Invoke("Lade Cloudflare Tunnel herunter...");
            var ok = await DownloadBinary();
            if (!ok)
            {
                StatusChanged?.Invoke("❌ Cloudflare Download fehlgeschlagen – Tunnel nicht verfügbar.");
                UrlChanged?.Invoke(null);
                return;
            }
        }

        StatusChanged?.Invoke("Starte Cloudflare Tunnel...");
        var psi = new ProcessStartInfo
        {
            FileName = _binaryPath,
            Arguments = $"tunnel --url http://localhost:{localPort} --no-autoupdate",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        _process = new Process { StartInfo = psi };
        try
        {
            _process.Start();
            _ = Task.Run(() => ReadOutput(_process));
            StatusChanged?.Invoke("Verbinde mit Cloudflare...");
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke($"❌ Tunnel-Fehler: {ex.Message}");
            UrlChanged?.Invoke(null);
        }
    }

    public static Task StopAsync()
    {
        if (_process is { HasExited: false })
        {
            try { _process.Kill(); } catch { }
            try { _process.Close(); } catch { }
        }
        _process = null;
        _publicUrl = null;
        return Task.CompletedTask;
    }

    private static async Task ReadOutput(Process proc)
    {
        try
        {
            while (!proc.HasExited)
            {
                var line = await proc.StandardOutput.ReadLineAsync();
                if (line == null) break;

                var m = UrlRegex().Match(line);
                if (m.Success)
                {
                    _publicUrl = m.Value;
                    StatusChanged?.Invoke($"✅ Tunnel aktiv: {_publicUrl}");
                    UrlChanged?.Invoke(_publicUrl);
                }
            }

            var error = await proc.StandardError.ReadToEndAsync();
            if (!string.IsNullOrEmpty(error) && error.Contains("error", StringComparison.OrdinalIgnoreCase))
                StatusChanged?.Invoke($"⚠ Tunnel: {error[..Math.Min(error.Length, 200)]}");
        }
        catch { }
    }

    private static string GetBinaryPath()
    {
        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "cloudflared.exe");
    }

    private static async Task<bool> DownloadBinary()
    {
        try
        {
            var url = Environment.Is64BitOperatingSystem
                ? "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe"
                : "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-386.exe";

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            var data = await client.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(_binaryPath!, data);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TunnelService] Download fehlgeschlagen: {ex.Message}");
            return false;
        }
    }

    [GeneratedRegex(@"https://[\w-]+\.trycloudflare\.com")]
    private static partial Regex UrlRegex();
}
