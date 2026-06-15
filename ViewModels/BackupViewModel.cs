using System.Windows.Input;
using BloxHive.Models;
using BloxHive.Services;

namespace BloxHive.ViewModels;

public class BackupViewModel : BaseViewModel
{
    private string _statusMessage = "";
    private bool _isBusy;

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanExport));
                OnPropertyChanged(nameof(CanImport));
            }
        }
    }

    public bool CanExport => !IsBusy;
    public bool CanImport => !IsBusy;

    public ICommand ExportCommand { get; }
    public ICommand ImportCommand { get; }

    public BackupViewModel()
    {
        ExportCommand = new RelayCommand(async _ => await Export());
        ImportCommand = new RelayCommand(async _ => await Import());
    }

    private async Task Export()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = Loc.BackupExportTitle,
            FileName = $"BloxHive-Backup-{DateTime.Now:yyyy-MM-dd_HHmmss}.zip",
            Filter = "ZIP-Dateien (*.zip)|*.zip"
        };

        if (dialog.ShowDialog() != true) return;

        IsBusy = true;
        StatusMessage = "";
        try
        {
            await BackupService.ExportAsync(dialog.FileName);
            StatusMessage = Loc.BackupExportSuccess;
        }
        catch (Exception ex)
        {
            StatusMessage = $"{Loc.BackupError}: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task Import()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = Loc.BackupImportTitle,
            Filter = "ZIP-Dateien (*.zip)|*.zip"
        };

        if (dialog.ShowDialog() != true) return;

        IsBusy = true;
        StatusMessage = "";
        try
        {
            var (accounts, settings) = await BackupService.PreviewAsync(dialog.FileName);

            var msg = string.Format(Loc.BackupPreviewFormat, accounts.Count, settings.Language, settings.Theme);
            var result = System.Windows.MessageBox.Show(msg, Loc.BackupImportConfirm, System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            if (result != System.Windows.MessageBoxResult.Yes) return;

            await BackupService.ImportAsync(dialog.FileName);
            StatusMessage = Loc.BackupImportSuccess;
        }
        catch (Exception ex)
        {
            StatusMessage = $"{Loc.BackupError}: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
