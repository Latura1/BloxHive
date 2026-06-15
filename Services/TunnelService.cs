using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
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
            StatusChanged?.Invoke("Lade Cloudflare Tunnel herunter (~15 MB)...");
            var ok = await DownloadBinary();
            if (!ok)
            {
                StatusChanged?.Invoke("❌ Cloudflare Download fehlgeschlagen.\r\nLade https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe manuell runter und speichere es als:\r\n" + _binaryPath);
                UrlChanged?.Invoke(null);
                return;
            }
            UnblockFile(_binaryPath);
        }

        if (!File.Exists(_binaryPath))
        {
            StatusChanged?.Invoke("❌ Tunnel-Binary nicht gefunden: " + _binaryPath);
            UrlChanged?.Invoke(null);
            return;
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
            StatusChanged?.Invoke("Verbinde mit Cloudflare (dauert bis zu 30s)...");
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke($"❌ Tunnel-Start fehlgeschlagen: {ex.Message}");
            UrlChanged?.Invoke(null);
        }
    }

    public static Task StopAsync()
    {
        if (_process is { HasExited: false })
        {
            try { _process.Kill(entireProcessTree: true); } catch (Exception ex) { LogService.Error("TunnelService.Stop", ex.Message); }
            try { _process.Close(); } catch (Exception ex) { LogService.Error("TunnelService.Stop.Close", ex.Message); }
        }
        _process = null;
        _publicUrl = null;
        return Task.CompletedTask;
    }

    private static volatile bool _urlFound;

    private static async Task ReadOutput(Process proc)
    {
        _urlFound = false;
        var readOut = ReadStream(proc.StandardOutput, "OUT");
        var readErr = ReadStream(proc.StandardError, "ERR");
        var timeout = Task.Delay(TimeSpan.FromSeconds(60));
        _ = await Task.WhenAny(readOut, readErr, timeout);
            if (!_urlFound)
            {
                if (!proc.HasExited)
                    StatusChanged?.Invoke("⏱ Zeitüberschreitung (60s) – Tunnel nicht verbunden.\r\nLade cloudflared.exe manuell von\r\nhttps://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe\r\nund speichere es als:\r\n" + _binaryPath);
                try { proc.Kill(entireProcessTree: true); } catch (Exception ex) { LogService.Error("TunnelService.ReadOutput.Kill", ex.Message); }
        }
    }

    private static async Task ReadStream(StreamReader reader, string prefix)
    {
        try
        {
            while (!_urlFound)
            {
                var line = await reader.ReadLineAsync();
                if (line == null) break;

                var m = UrlRegex().Match(line);
                if (m.Success)
                {
                    _urlFound = true;
                    _publicUrl = m.Value;
                    StatusChanged?.Invoke($"✅ Tunnel aktiv: {_publicUrl}");
                    UrlChanged?.Invoke(_publicUrl);
                    return;
                }

                var display = line.Length > 120 ? line[..120] + "..." : line;
                StatusChanged?.Invoke($"[{prefix}] {display}");
            }
        }
        catch (Exception ex) { LogService.Error("TunnelService.ReadStream", ex.Message); }
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

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
            var data = await client.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(_binaryPath!, data);
            return true;
        }
        catch (Exception ex)
        {
            LogService.Error("TunnelService.Download", ex.Message);
            return false;
        }
    }

    private static void UnblockFile(string path)
    {
        try
        {
            var zoneId = path + ":Zone.Identifier";
            if (File.Exists(zoneId))
                File.Delete(zoneId);
        }
        catch (Exception ex) { LogService.Error("TunnelService.Unblock", ex.Message); }
    }

    [GeneratedRegex(@"https://([\w-]+\.)+trycloudflare\.com")]
    private static partial Regex UrlRegex();
}
