using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BloxHive.Models;

public class RobloxProcessInfo : INotifyPropertyChanged
{
    public int Id { get; init; }
    public string MainWindowTitle { get; init; } = string.Empty;
    public string DisplayName => string.IsNullOrWhiteSpace(MainWindowTitle) ? $"Roblox ({Id})" : $"{MainWindowTitle} ({Id})";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
