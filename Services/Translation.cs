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
        ["Home"] = "Haupt",
        ["Settings"] = "Einstellungen",
        ["Navigation"] = "NAVIGATION",
        ["Theme"] = "Design",
        ["ThemeDescription"] = "Wähle ein Farbschema für die App.",
        ["ThemeDefault"] = "Midnight",
        ["ThemeBlue"] = "Ocean",
        ["ThemePurple"] = "Nebula",
        ["MultiInstance"] = "Multi-Instance",
        ["MultiInstanceDescription"] = "Ermöglicht das gleichzeitige Öffnen mehrerer Roblox-Instanzen.",
        ["MultiInstanceToggle"] = "Multi-RBLX aktivieren",
        ["MultiInstanceActive"] = "Aktiv – Mehrere Instanzen möglich",
        ["MultiInstanceInactive"] = "Inaktiv",
        ["RunningInstances"] = "Laufende Instanzen",
        ["RunningInstancesCount"] = "Laufende Instanzen ({0})",
        ["CloseAll"] = "Alle schließen",
        ["NoInstances"] = "Keine Roblox-Instanzen aktiv.",
        ["Refresh"] = "Aktualisieren",
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
        ["Home"] = "Main",
        ["Settings"] = "Settings",
        ["Navigation"] = "NAVIGATION",
        ["Theme"] = "Theme",
        ["ThemeDescription"] = "Choose a color scheme for the app.",
        ["ThemeDefault"] = "Midnight",
        ["ThemeBlue"] = "Ocean",
        ["ThemePurple"] = "Nebula",
        ["MultiInstance"] = "Multi-Instance",
        ["MultiInstanceDescription"] = "Allows opening multiple Roblox instances simultaneously.",
        ["MultiInstanceToggle"] = "Activate Multi-RBLX",
        ["MultiInstanceActive"] = "Active – Multiple instances possible",
        ["MultiInstanceInactive"] = "Inactive",
        ["RunningInstances"] = "Running Instances",
        ["RunningInstancesCount"] = "Running Instances ({0})",
        ["CloseAll"] = "Close All",
        ["NoInstances"] = "No Roblox instances running.",
        ["Refresh"] = "Refresh",
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
    public string MultiInstance => Get("MultiInstance");
    public string MultiInstanceDescription => Get("MultiInstanceDescription");
    public string MultiInstanceToggle => Get("MultiInstanceToggle");
    public string MultiInstanceActive => Get("MultiInstanceActive");
    public string MultiInstanceInactive => Get("MultiInstanceInactive");
    public string RunningInstances => Get("RunningInstances");
    public string RunningInstancesCount => Get("RunningInstancesCount");
    public string CloseAll => Get("CloseAll");
    public string NoInstances => Get("NoInstances");
    public string Refresh => Get("Refresh");

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyAll()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
}
