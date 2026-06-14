using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BloxHive.Models;

public class AccountInfo : INotifyPropertyChanged
{
    public string DisplayName { get; set; } = "";
    public long UserId { get; set; }
    public string CookieEncrypted { get; set; } = "";
    public string Proxy { get; set; } = "";

    private string _statusText = "";
    private bool _isOnline;

    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusDotColor)); }
    }

    public bool IsOnline
    {
        get => _isOnline;
        set { _isOnline = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusDotColor)); }
    }

    public string StatusDotColor => IsOnline ? "#22C55E" : "#555555";

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
