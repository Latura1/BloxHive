using System.Collections.ObjectModel;
using System.Windows.Input;
using BloxHive.Models;
using BloxHive.Services;

namespace BloxHive.ViewModels;

public class SettingsViewModel : BaseViewModel
{
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

    public ICommand SetThemeCommand { get; }

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
    }

    private void Save()
    {
        SettingsService.Save(new AppSettings
        {
            Language = Loc.IsEnglish ? "en" : "de",
            Theme = ThemeManager.Instance.Current.Name,
        });
    }
}
