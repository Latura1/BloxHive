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

    public static async Task StartAsync(int localPort)
    {
        await StopAsync();

        _binaryPath = GetBinaryPath();
        if (!File.Exists(_binaryPath))
        {
            await DownloadBinary();
            if (!File.Exists(_binaryPath))
            {
                UrlChanged?.Invoke(null);
                return;
            }
        }

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
        _process.Start();

        _ = Task.Run(() => ReadOutput(_process));
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

                Debug.WriteLine($"[TunnelService] {line}");

                var m = UrlRegex().Match(line);
                if (m.Success)
                {
                    _publicUrl = m.Groups[1].Value;
                    UrlChanged?.Invoke(_publicUrl);
                }
            }

            var error = await proc.StandardError.ReadToEndAsync();
            if (!string.IsNullOrEmpty(error))
                Debug.WriteLine($"[TunnelService] STDERR: {error}");
        }
        catch { }
    }

    private static string GetBinaryPath()
    {
        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "cloudflared.exe");
    }

    private static async Task DownloadBinary()
    {
        try
        {
            var url = Environment.Is64BitOperatingSystem
                ? "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe"
                : "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-386.exe";

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var data = await client.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(_binaryPath!, data);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[TunnelService] Download fehlgeschlagen: {ex.Message}");
        }
    }

    [GeneratedRegex(@"https://[\w-]+\.trycloudflare\.com")]
    private static partial Regex UrlRegex();
}
