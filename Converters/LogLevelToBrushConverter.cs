using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using BloxHive.Models;

namespace BloxHive.Converters;

public class LogLevelToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is LogLevel level
            ? level switch
            {
                LogLevel.Warning => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                LogLevel.Error => new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                _ => new SolidColorBrush(Color.FromRgb(34, 197, 94))
            }
            : new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
