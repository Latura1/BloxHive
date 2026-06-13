using System.Collections.ObjectModel;
using System.Windows.Input;
using BloxHive.Models;
using BloxHive.Services;

namespace BloxHive.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private string _webhookStatus = "";
    private bool _isTesting;

    public ObservableCollection<string> Languages { get; } = ["Deutsch", "English"];

    public int SelectedLanguageIndex
    {
        get => Loc.IsEnglish ? 1 : 0;
        set
        {
            Loc.IsEnglish = value == 1;
            OnPropertyChanged();
            Save();
        }
    }

    public int SelectedThemeIndex
    {
        get => ThemeDefinition.All.IndexOf(ThemeManager.Instance.Current);
        set
        {
            if (value >= 0 && value < ThemeDefinition.All.Count)
                ThemeManager.Instance.Current = ThemeDefinition.All[value];
            OnPropertyChanged();
            Save();
        }
    }

    public string WebhookUrl
    {
        get => SettingsService.Load().WebhookUrl;
        set
        {
            var settings = SettingsService.Load();
            settings.WebhookUrl = value ?? "";
            SettingsService.Save(settings);
            OnPropertyChanged();
        }
    }

    public string IntervalText
    {
        get => SettingsService.Load().AutoWebhookInterval.ToString();
        set
        {
            if (int.TryParse(value, out var num) && num >= 1 && num <= 3600)
            {
                var settings = SettingsService.Load();
                settings.AutoWebhookInterval = num;
                SettingsService.Save(settings);
            }
            OnPropertyChanged();
        }
    }

    public string WebhookStatus
    {
        get => _webhookStatus;
        set => SetProperty(ref _webhookStatus, value);
    }

    public bool IsTesting
    {
        get => _isTesting;
        set => SetProperty(ref _isTesting, value);
    }

    public ICommand SetThemeCommand { get; }
    public ICommand TestWebhookCommand { get; }

    public SettingsViewModel()
    {
        Loc.PropertyChanged += (_, _) => OnPropertyChanged(nameof(SelectedLanguageIndex));
        ThemeManager.Instance.PropertyChanged += (_, _) => OnPropertyChanged(nameof(SelectedThemeIndex));

        SetThemeCommand = new RelayCommand(param =>
        {
            if (param is string s && int.TryParse(s, out var index) && index >= 0 && index < ThemeDefinition.All.Count)
            {
                ThemeManager.Instance.Current = ThemeDefinition.All[index];
                Save();
            }
        });

        TestWebhookCommand = new RelayCommand(async _ =>
        {
            var settings = SettingsService.Load();
            if (string.IsNullOrWhiteSpace(settings.WebhookUrl))
            {
                WebhookStatus = Loc.WebhookNoUrl;
                return;
            }

            IsTesting = true;
            WebhookStatus = "";

            var service = new WebhookService();
            var success = await service.SendTest(settings.WebhookUrl);
            WebhookStatus = success ? Loc.TestWebhookSent : Loc.TestWebhookFailed;
            IsTesting = false;
        });
    }

    private void Save()
    {
        var settings = SettingsService.Load();
        settings.Language = Loc.IsEnglish ? "en" : "de";
        settings.Theme = ThemeManager.Instance.Current.Name;
        SettingsService.Save(settings);
    }
}
