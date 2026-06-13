using System.ComponentModel;
using System.Windows;

namespace BloxHive.Services;

public class ThemeManager : INotifyPropertyChanged
{
    private static ThemeManager? _instance;
    public static ThemeManager Instance => _instance ??= new ThemeManager();

    private ThemeDefinition _current = ThemeDefinition.Default;
    public ThemeDefinition Current
    {
        get => _current;
        set
        {
            if (_current != value)
            {
                _current = value;
                Apply(value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Current)));
            }
        }
    }

    private bool _initialized;

    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        Apply(_current);
    }

    private void Apply(ThemeDefinition theme)
    {
        var app = Application.Current;
        if (app is null) return;

        var merged = app.Resources.MergedDictionaries;

        var old = merged.FirstOrDefault(d =>
        {
            var src = d.Source?.ToString();
            return src is not null && (src.Contains("Styles/Themes/") || src.Contains("Styles/Colors.xaml"));
        });

        if (old is not null)
            merged.Remove(old);

        var rd = new ResourceDictionary { Source = new Uri(theme.Source, UriKind.Relative) };
        merged.Insert(0, rd);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
