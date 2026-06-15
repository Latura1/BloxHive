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
        ["Webhook"] = "Webhook",
        ["WebhookDescription"] = "Richte einen Discord-Webhook für Screenshots ein.",
        ["WebhookUrl"] = "Webhook-URL",
        ["WebhookUrlPlaceholder"] = "https://discord.com/api/webhooks/...",
        ["AutoWebhook"] = "Automatisch senden",
        ["AutoWebhookDescription"] = "Sende Screenshots automatisch beim Erkennen neuer Instanzen.",
        ["TestWebhook"] = "Test",
        ["TestWebhookSent"] = "✅ Test-Webhook gesendet!",
        ["TestWebhookFailed"] = "❌ Fehlgeschlagen – URL prüfen.",
        ["WebhookNoUrl"] = "❌ Keine Webhook-URL hinterlegt.",
        ["ScreenshotSent"] = "📸 Screenshot gesendet",
        ["ScreenshotFailed"] = "❌ Screenshot fehlgeschlagen",
        ["WebhookTestMessage"] = "✅ **BloxHive Test-Webhook** – Verbindung erfolgreich!",
        ["WebhookScreenshotMessage"] = "📸 **BloxHive Screenshot** – {0} (PID: {1})",
        ["WebhookInterval"] = "Intervall (Sekunden)",
        ["WebhookIntervalDescription"] = "Wie oft sollen Screenshots automatisch gesendet werden?",
        ["Accounts"] = "Accounts",
        ["AccountsTitle"] = "Account-Verwaltung",
        ["AccountsDescription"] = "Füge Roblox-Accounts hinzu und öffne sie im eingebauten Browser.",
        ["AddAccount"] = "Hinzufügen",
        ["OpenAccount"] = "Öffnen",
        ["DeleteAccount"] = "Löschen",
        ["DeleteAccountTitle"] = "Account löschen",
        ["DeleteAccountConfirm"] = "Möchtest du den Account \"{0}\" wirklich löschen?",
        ["ClearCache"] = "Cache leeren",
        ["CacheCleared"] = "🧹 Cache wurde geleert.",
        ["NoAccounts"] = "Noch keine Accounts hinterlegt.",
        ["AccountsCount"] = "{0} Account(s)",
        ["AccountAdded"] = "✅ Account \"{0}\" hinzugefügt!",
        ["AccountDeleted"] = "🗑 Account \"{0}\" gelöscht.",
        ["AccountExists"] = "⚠ Account \"{0}\" existiert bereits.",
        ["Proxy"] = "Proxy",
        ["ProxyPlaceholder"] = "http://ip:port oder socks5://ip:port",
        ["ProxySaved"] = "✅ Proxy für Account \"{0}\" gespeichert.",
        ["QuickAccess"] = "Schnellzugriff",
        ["QuickAccessDescription"] = "Gespeicherte Accounts – klicke zum Öffnen.",
        ["Experimental"] = "⚙ Experimental",
        ["ExperimentalDescription"] = "Experimentelle Funktionen aktivieren",
        ["Dashboard"] = "Dashboard",
        ["DashboardDescription"] = "Steuere BloxHive remote über das Web-Dashboard.",
        ["DashboardPort"] = "Port",
        ["DashboardPortDescription"] = "Lege den Netzwerkport für den Dashboard-Server fest.",
        ["DashboardPassword"] = "Passwort",
        ["DashboardSetPassword"] = "Setzen",
        ["DashboardStart"] = "Starten",
        ["DashboardStop"] = "Stoppen",
        ["DashboardRunning"] = "🟢 Läuft",
        ["DashboardStopped"] = "🔴 Gestoppt",
        ["DashboardPasswordSet"] = "✓ Passwort gesetzt",
        ["DashboardPasswordNotSet"] = "✗ Kein Passwort (offener Zugriff)",
        ["DashboardQR"] = "QR-Code",
        ["DashboardQRDescription"] = "Scanne den Code mit deinem Smartphone für schnellen Zugriff.",
        ["DashboardUrlHint"] = "Im selben Netzwerk auf einem anderen Gerät öffnen",
        ["DashboardExperimentalHint"] = "Lokale IPs und Port-Änderung nur mit ⚙ Experimental sichtbar",
        ["Copy"] = "Kopieren",
        ["Copied"] = "✅ Kopiert!",
        ["AccountLoginTitle"] = "Account hinzufügen",
        ["AccountLoginLoading"] = "Lade Login-Seite...",
        ["AccountLoginInfo"] = "Nach dem Login automatisch erkennen oder manuell bestätigen.",
        ["Cancel"] = "Abbrechen",
        ["Done"] = "Fertig",
        ["AccountLoginNavigating"] = "Navigiere...",
        ["AccountLoginDetected"] = "Login erkannt, prüfe Cookie...",
        ["AccountLoginPageLoaded"] = "Seite geladen: {0}",
        ["AccountLoginCookieFound"] = "Cookie gefunden! Hole Account-Daten...",
        ["AccountLoginCookieProcessing"] = "Cookie gefunden, verarbeite...",
        ["AccountLoginLoggedIn"] = "✅ Angemeldet als {0}",
        ["AccountLoginFetchFailed"] = "❌ Konnte Account-Daten nicht abrufen. \"{0}\" drücken um trotzdem zu speichern.",
        ["AccountWindowTitle"] = "BloxHive - Account",
        ["Loop"] = "Loop",
        ["Save"] = "Speichern",
        ["DashboardStartFailed"] = "⚠ Administratorrechte benötigt – Dashboard nur lokal verfügbar.",
        ["DashboardStartError"] = "❌ Fehler: {0}",
        ["ThemeForest"] = "Forest",
        ["ThemeRuby"] = "Ruby",
        ["ThemeCyber"] = "Cyber",
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
        ["Webhook"] = "Webhook",
        ["WebhookDescription"] = "Configure a Discord webhook for screenshots.",
        ["WebhookUrl"] = "Webhook URL",
        ["WebhookUrlPlaceholder"] = "https://discord.com/api/webhooks/...",
        ["AutoWebhook"] = "Auto-send",
        ["AutoWebhookDescription"] = "Automatically send screenshots when new instances are detected.",
        ["TestWebhook"] = "Test",
        ["TestWebhookSent"] = "✅ Test webhook sent!",
        ["TestWebhookFailed"] = "❌ Failed – check URL.",
        ["WebhookNoUrl"] = "❌ No webhook URL configured.",
        ["ScreenshotSent"] = "📸 Screenshot sent",
        ["ScreenshotFailed"] = "❌ Screenshot failed",
        ["WebhookTestMessage"] = "✅ **BloxHive Test-Webhook** – Connection successful!",
        ["WebhookScreenshotMessage"] = "📸 **BloxHive Screenshot** – {0} (PID: {1})",
        ["WebhookInterval"] = "Interval (seconds)",
        ["WebhookIntervalDescription"] = "How often should screenshots be sent automatically?",
        ["Accounts"] = "Accounts",
        ["AccountsTitle"] = "Account Management",
        ["AccountsDescription"] = "Add Roblox accounts and open them in the built-in browser.",
        ["AddAccount"] = "Add",
        ["OpenAccount"] = "Open",
        ["DeleteAccount"] = "Delete",
        ["DeleteAccountTitle"] = "Delete Account",
        ["DeleteAccountConfirm"] = "Are you sure you want to delete account \"{0}\"?",
        ["ClearCache"] = "Clear Cache",
        ["CacheCleared"] = "🧹 Cache cleared.",
        ["NoAccounts"] = "No accounts yet.",
        ["AccountsCount"] = "{0} account(s)",
        ["AccountAdded"] = "✅ Account \"{0}\" added!",
        ["AccountDeleted"] = "🗑 Account \"{0}\" deleted.",
        ["AccountExists"] = "⚠ Account \"{0}\" already exists.",
        ["Proxy"] = "Proxy",
        ["ProxyPlaceholder"] = "http://ip:port or socks5://ip:port",
        ["ProxySaved"] = "✅ Proxy for account \"{0}\" saved.",
        ["QuickAccess"] = "Quick Access",
        ["QuickAccessDescription"] = "Saved accounts – click to open.",
        ["Experimental"] = "⚙ Experimental",
        ["ExperimentalDescription"] = "Enable experimental features",
        ["Dashboard"] = "Dashboard",
        ["DashboardDescription"] = "Control BloxHive remotely via the web dashboard.",
        ["DashboardPort"] = "Port",
        ["DashboardPortDescription"] = "Set the network port for the dashboard server.",
        ["DashboardPassword"] = "Password",
        ["DashboardSetPassword"] = "Set",
        ["DashboardStart"] = "Start",
        ["DashboardStop"] = "Stop",
        ["DashboardRunning"] = "🟢 Running",
        ["DashboardStopped"] = "🔴 Stopped",
        ["DashboardPasswordSet"] = "✓ Password set",
        ["DashboardPasswordNotSet"] = "✗ No password (open access)",
        ["DashboardQR"] = "QR Code",
        ["DashboardQRDescription"] = "Scan the code with your phone for quick access.",
        ["DashboardUrlHint"] = "Open on another device in the same network",
        ["DashboardExperimentalHint"] = "Local IPs and port changes only visible with ⚙ Experimental",
        ["Copy"] = "Copy",
        ["Copied"] = "✅ Copied!",
        ["AccountLoginTitle"] = "Add Account",
        ["AccountLoginLoading"] = "Loading login page...",
        ["AccountLoginInfo"] = "Auto-detect after login or confirm manually.",
        ["Cancel"] = "Cancel",
        ["Done"] = "Done",
        ["AccountLoginNavigating"] = "Navigating...",
        ["AccountLoginDetected"] = "Login detected, checking cookie...",
        ["AccountLoginPageLoaded"] = "Page loaded: {0}",
        ["AccountLoginCookieFound"] = "Cookie found! Fetching account data...",
        ["AccountLoginCookieProcessing"] = "Cookie found, processing...",
        ["AccountLoginLoggedIn"] = "✅ Logged in as {0}",
        ["AccountLoginFetchFailed"] = "❌ Could not fetch account data. Press \"{0}\" to save anyway.",
        ["AccountWindowTitle"] = "BloxHive - Account",
        ["Loop"] = "Loop",
        ["Save"] = "Save",
        ["DashboardStartFailed"] = "⚠ Admin rights required – dashboard local only.",
        ["DashboardStartError"] = "❌ Error: {0}",
        ["ThemeForest"] = "Forest",
        ["ThemeRuby"] = "Ruby",
        ["ThemeCyber"] = "Cyber",
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
    public string Webhook => Get("Webhook");
    public string WebhookDescription => Get("WebhookDescription");
    public string WebhookUrl => Get("WebhookUrl");
    public string WebhookUrlPlaceholder => Get("WebhookUrlPlaceholder");
    public string AutoWebhook => Get("AutoWebhook");
    public string AutoWebhookDescription => Get("AutoWebhookDescription");
    public string TestWebhook => Get("TestWebhook");
    public string TestWebhookSent => Get("TestWebhookSent");
    public string TestWebhookFailed => Get("TestWebhookFailed");
    public string WebhookNoUrl => Get("WebhookNoUrl");
    public string ScreenshotSent => Get("ScreenshotSent");
    public string ScreenshotFailed => Get("ScreenshotFailed");
    public string WebhookTestMessage => Get("WebhookTestMessage");
    public string WebhookScreenshotMessage => Get("WebhookScreenshotMessage");
    public string WebhookInterval => Get("WebhookInterval");
    public string WebhookIntervalDescription => Get("WebhookIntervalDescription");
    public string Accounts => Get("Accounts");
    public string AccountsTitle => Get("AccountsTitle");
    public string AccountsDescription => Get("AccountsDescription");
    public string AddAccount => Get("AddAccount");
    public string OpenAccount => Get("OpenAccount");
    public string DeleteAccount => Get("DeleteAccount");
    public string DeleteAccountTitle => Get("DeleteAccountTitle");
    public string DeleteAccountConfirm => Get("DeleteAccountConfirm");
    public string ClearCache => Get("ClearCache");
    public string CacheCleared => Get("CacheCleared");
    public string NoAccounts => Get("NoAccounts");
    public string AccountsCount => Get("AccountsCount");
    public string AccountAdded => Get("AccountAdded");
    public string AccountDeleted => Get("AccountDeleted");
    public string AccountExists => Get("AccountExists");
    public string Proxy => Get("Proxy");
    public string ProxyPlaceholder => Get("ProxyPlaceholder");
    public string ProxySaved => Get("ProxySaved");
    public string QuickAccess => Get("QuickAccess");
    public string QuickAccessDescription => Get("QuickAccessDescription");
    public string Experimental => Get("Experimental");
    public string ExperimentalDescription => Get("ExperimentalDescription");
    public string Dashboard => Get("Dashboard");
    public string DashboardDescription => Get("DashboardDescription");
    public string DashboardPort => Get("DashboardPort");
    public string DashboardPortDescription => Get("DashboardPortDescription");
    public string DashboardPassword => Get("DashboardPassword");
    public string DashboardSetPassword => Get("DashboardSetPassword");
    public string DashboardStart => Get("DashboardStart");
    public string DashboardStop => Get("DashboardStop");
    public string DashboardRunning => Get("DashboardRunning");
    public string DashboardStopped => Get("DashboardStopped");
    public string DashboardPasswordSet => Get("DashboardPasswordSet");
    public string DashboardPasswordNotSet => Get("DashboardPasswordNotSet");
    public string DashboardQR => Get("DashboardQR");
    public string DashboardQRDescription => Get("DashboardQRDescription");
    public string DashboardUrlHint => Get("DashboardUrlHint");
    public string DashboardExperimentalHint => Get("DashboardExperimentalHint");
    public string Copy => Get("Copy");
    public string Copied => Get("Copied");
    public string AccountLoginTitle => Get("AccountLoginTitle");
    public string AccountLoginLoading => Get("AccountLoginLoading");
    public string AccountLoginInfo => Get("AccountLoginInfo");
    public string Cancel => Get("Cancel");
    public string Done => Get("Done");
    public string AccountLoginNavigating => Get("AccountLoginNavigating");
    public string AccountLoginDetected => Get("AccountLoginDetected");
    public string AccountLoginPageLoaded => Get("AccountLoginPageLoaded");
    public string AccountLoginCookieFound => Get("AccountLoginCookieFound");
    public string AccountLoginCookieProcessing => Get("AccountLoginCookieProcessing");
    public string AccountLoginLoggedIn => Get("AccountLoginLoggedIn");
    public string AccountLoginFetchFailed => Get("AccountLoginFetchFailed");
    public string AccountWindowTitle => Get("AccountWindowTitle");
    public string Loop => Get("Loop");
    public string Save => Get("Save");
    public string DashboardStartFailed => Get("DashboardStartFailed");
    public string DashboardStartError => Get("DashboardStartError");
    public string ThemeForest => Get("ThemeForest");
    public string ThemeRuby => Get("ThemeRuby");
    public string ThemeCyber => Get("ThemeCyber");

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyAll()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
}
