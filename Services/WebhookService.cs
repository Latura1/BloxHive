using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace BloxHive.Services;

public class WebhookService
{
    private static readonly HttpClient _client = new();

    public async Task<bool> SendTest(string url)
    {
        try
        {
            var loc = Translation.Instance;
            var payload = new
            {
                content = loc.WebhookTestMessage,
                username = "BloxHive",
            };

            var response = await _client.PostAsJsonAsync(url, payload);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SendScreenshot(string url, int processId, string processName)
    {
        try
        {
            var screenshotPath = ScreenCaptureService.CaptureWindow(processId);
            if (screenshotPath == null)
                return false;

            using var form = new MultipartFormDataContent();
            var loc = Translation.Instance;
            var content = string.Format(loc.WebhookScreenshotMessage, processName, processId);
            var payload = new
            {
                content = content,
                username = "BloxHive",
            };
            form.Add(new StringContent(JsonSerializer.Serialize(payload)), "payload_json");

            var fileBytes = await File.ReadAllBytesAsync(screenshotPath);
            form.Add(new ByteArrayContent(fileBytes), "file", $"roblox_{processId}.png");

            var response = await _client.PostAsync(url, form);

            try { File.Delete(screenshotPath); } catch { }

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
