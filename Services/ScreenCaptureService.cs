using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace BloxHive.Services;

public static class ScreenCaptureService
{
    private const uint PW_RENDERFULLCONTENT = 2;

    [DllImport("user32.dll")]
    private static extern int GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hWnd, IntPtr hDC, uint nFlags);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_RESTORE = 9;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public static string? CaptureWindow(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            if (process.MainWindowHandle == IntPtr.Zero)
                return null;

            var handle = process.MainWindowHandle;

            if (GetWindowRect(handle, out RECT rect) == 0)
                return null;

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            if (width <= 0 || height <= 0)
                return null;

            var tempDir = Path.Combine(Path.GetTempPath(), "BloxHive");
            Directory.CreateDirectory(tempDir);
            var path = Path.Combine(tempDir, $"screenshot_{processId}_{DateTime.Now:yyyyMMddHHmmss}.png");

            using var bitmap = new System.Drawing.Bitmap(width, height);
            using var g = System.Drawing.Graphics.FromImage(bitmap);
            var hdc = g.GetHdc();

            bool printed = PrintWindow(handle, hdc, PW_RENDERFULLCONTENT);

            g.ReleaseHdc(hdc);

            if (!printed || IsAllWhite(bitmap))
            {
                using var fallback = CaptureFromScreen(handle, rect);
                if (fallback != null)
                {
                    fallback.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                    return path;
                }
                return null;
            }

            bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            return path;
        }
        catch
        {
            return null;
        }
    }

    private static System.Drawing.Bitmap? CaptureFromScreen(IntPtr handle, RECT rect)
    {
        bool wasMinimized = IsIconic(handle);
        if (wasMinimized)
            ShowWindow(handle, SW_RESTORE);

        SetForegroundWindow(handle);
        System.Threading.Thread.Sleep(300);

        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;

        var bitmap = new System.Drawing.Bitmap(width, height);
        using var g = System.Drawing.Graphics.FromImage(bitmap);
        g.CopyFromScreen(rect.Left, rect.Top, 0, 0, new System.Drawing.Size(width, height));

        return bitmap;
    }

    private static bool IsAllWhite(System.Drawing.Bitmap bitmap)
    {
        if (bitmap.Width < 10 || bitmap.Height < 10)
            return false;

        for (int x = 0; x < bitmap.Width; x += 8)
        {
            for (int y = 0; y < bitmap.Height; y += 8)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.R < 240 || pixel.G < 240 || pixel.B < 240)
                    return false;
            }
        }
        return true;
    }
}
