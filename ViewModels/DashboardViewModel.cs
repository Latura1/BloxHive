namespace BloxHive.ViewModels;

public class DashboardViewModel : BaseViewModel
{
    private string _welcomeMessage = "Willkommen bei BloxHive";
    public string WelcomeMessage
    {
        get => _welcomeMessage;
        set => SetProperty(ref _welcomeMessage, value);
    }

    private string _statValue1 = "128";
    public string StatValue1
    {
        get => _statValue1;
        set => SetProperty(ref _statValue1, value);
    }

    private string _statLabel1 = "Projekte";
    public string StatLabel1
    {
        get => _statLabel1;
        set => SetProperty(ref _statLabel1, value);
    }

    private string _statValue2 = "2.4k";
    public string StatValue2
    {
        get => _statValue2;
        set => SetProperty(ref _statValue2, value);
    }

    private string _statLabel2 = "Aufgaben";
    public string StatLabel2
    {
        get => _statLabel2;
        set => SetProperty(ref _statLabel2, value);
    }

    private string _statValue3 = "47";
    public string StatValue3
    {
        get => _statValue3;
        set => SetProperty(ref _statValue3, value);
    }

    private string _statLabel3 = "Mitarbeiter";
    public string StatLabel3
    {
        get => _statLabel3;
        set => SetProperty(ref _statLabel3, value);
    }

    private string _statValue4 = "99.9%";
    public string StatValue4
    {
        get => _statValue4;
        set => SetProperty(ref _statValue4, value);
    }

    private string _statLabel4 = "Uptime";
    public string StatLabel4
    {
        get => _statLabel4;
        set => SetProperty(ref _statLabel4, value);
    }
}
