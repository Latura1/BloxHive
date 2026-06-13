using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BloxHive.Services;

public class Translation : INotifyPropertyChanged
{
    private static readonly Dictionary<string, string> _de = new()
    {
        ["SettingsTitle"] = "Einstellungen",
        ["SettingsDescription"] = "Passe BloxHive nach deinen Wünschen an.",
        ["Language"] = "Sprache",
        ["LanguageDescription"] = "Wähle die Anzeigesprache aus.",
        ["German"] = "Deutsch",
        ["English"] = "Englisch",
        ["HomeWelcome"] = "Willkommen bei BloxHive",
        ["HomeHint"] = "Wähle links im Menü, was du tun möchtest.",
        ["Home"] = "Start",
        ["Settings"] = "Einstellungen",
        ["Navigation"] = "NAVIGATION",
        ["Theme"] = "Design",
        ["ThemeDescription"] = "Wähle ein Farbschema für die App.",
        ["ThemeDefault"] = "Midnight",
        ["ThemeBlue"] = "Ocean",
        ["ThemePurple"] = "Nebula",
    };

    private static readonly Dictionary<string, string> _en = new()
    {
        ["SettingsTitle"] = "Settings",
        ["SettingsDescription"] = "Customize BloxHive to your liking.",
        ["Language"] = "Language",
        ["LanguageDescription"] = "Select the display language.",
        ["German"] = "German",
        ["English"] = "English",
        ["HomeWelcome"] = "Welcome to BloxHive",
        ["HomeHint"] = "Select what you want to do from the menu on the left.",
        ["Home"] = "Home",
        ["Settings"] = "Settings",
        ["Navigation"] = "NAVIGATION",
        ["Theme"] = "Theme",
        ["ThemeDescription"] = "Choose a color scheme for the app.",
        ["ThemeDefault"] = "Midnight",
        ["ThemeBlue"] = "Ocean",
        ["ThemePurple"] = "Nebula",
    };

    private static Translation? _instance;
    public static Translation Instance => _instance ??= new Translation();

    private bool _isEnglish;
    public bool IsEnglish
    {
        get => _isEnglish;
        set
        {
            if (_isEnglish != value)
            {
                _isEnglish = value;
                NotifyAll();
            }
        }
    }

    public bool IsGermanChecked
    {
        get => !_isEnglish;
        set { if (value) IsEnglish = false; }
    }

    public bool IsEnglishChecked
    {
        get => _isEnglish;
        set { if (value) IsEnglish = true; }
    }

    private Dictionary<string, string> _current => IsEnglish ? _en : _de;

    private string Get(string key) => _current.TryGetValue(key, out var val) ? val : key;

    public string SettingsTitle => Get("SettingsTitle");
    public string SettingsDescription => Get("SettingsDescription");
    public string Language => Get("Language");
    public string LanguageDescription => Get("LanguageDescription");
    public string German => Get("German");
    public string English => Get("English");
    public string HomeWelcome => Get("HomeWelcome");
    public string HomeHint => Get("HomeHint");
    public string Home => Get("Home");
    public string Settings => Get("Settings");
    public string Navigation => Get("Navigation");
    public string Theme => Get("Theme");
    public string ThemeDescription => Get("ThemeDescription");
    public string ThemeDefault => Get("ThemeDefault");
    public string ThemeBlue => Get("ThemeBlue");
    public string ThemePurple => Get("ThemePurple");

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyAll()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
}
