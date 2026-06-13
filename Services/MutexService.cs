using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BloxHive.Services;

public class MutexService : INotifyPropertyChanged, IDisposable
{
    private static MutexService? _instance;
    public static MutexService Instance => _instance ??= new MutexService();
    private Mutex? _singletonMutex;
    private Mutex? _singletonEvent;
    private bool _isActive;

    public bool IsActive
    {
        get => _isActive;
        private set
        {
            if (_isActive != value)
            {
                _isActive = value;
                OnPropertyChanged();
            }
        }
    }

    public bool Acquire()
    {
        if (IsActive) return true;

        try
        {
            _singletonMutex = new Mutex(true, "ROBLOX_singletonMutex");
            _singletonEvent = new Mutex(true, "ROBLOX_singletonEvent");
            IsActive = true;
            return true;
        }
        catch
        {
            Release();
            return false;
        }
    }

    public void Release()
    {
        try { _singletonMutex?.ReleaseMutex(); } catch { }
        try { _singletonMutex?.Dispose(); } catch { }
        try { _singletonEvent?.ReleaseMutex(); } catch { }
        try { _singletonEvent?.Dispose(); } catch { }

        _singletonMutex = null;
        _singletonEvent = null;
        IsActive = false;
    }

    public void Dispose()
    {
        Release();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
