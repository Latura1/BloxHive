using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BloxHive.Services;

public static class IconGenerator
{
    public static Icon GenerateBIcon(int size = 64)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;

        var accent = System.Drawing.Color.FromArgb(99, 102, 241);
        var pad = size / 16;
        var radius = size / 5;

        using (var brush = new SolidBrush(accent))
        using (var path = RoundedRect(pad, pad, size - pad * 2, size - pad * 2, radius))
        {
            g.FillPath(brush, path);
        }

        using (var font = new Font("Segoe UI", size * 0.55f, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel))
        using (var brush = new SolidBrush(System.Drawing.Color.White))
        {
            var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString("B", font, brush, new RectangleF(0, 0, size, size), sf);
        }

        return Icon.FromHandle(bmp.GetHicon());
    }

    private static GraphicsPath RoundedRect(float x, float y, float w, float h, float r)
    {
        var path = new GraphicsPath();
        path.AddArc(x, y, r * 2, r * 2, 180, 90);
        path.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
        path.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0, 90);
        path.AddArc(x, y + h - r * 2, r * 2, r * 2, 90, 90);
        path.CloseFigure();
        return path;
    }
}

public static class IconExtensions
{
    public static ImageSource ToImageSource(this Icon icon)
    {
        return Imaging.CreateBitmapSourceFromHIcon(
            icon.Handle,
            Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());
    }
}
