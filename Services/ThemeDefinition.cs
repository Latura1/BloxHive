namespace BloxHive.Services;

public class ThemeDefinition
{
    public string Name { get; }
    public string Source { get; }

    public ThemeDefinition(string name, string source)
    {
        Name = name;
        Source = source;
    }

    public static ThemeDefinition Default { get; } = new("Midnight", "Styles/Themes/DarkDefault.xaml");
    public static ThemeDefinition Blue { get; } = new("Ocean", "Styles/Themes/DarkBlue.xaml");
    public static ThemeDefinition Purple { get; } = new("Nebula", "Styles/Themes/DarkPurple.xaml");
    public static ThemeDefinition Forest { get; } = new("Forest", "Styles/Themes/DarkForest.xaml");
    public static ThemeDefinition Ruby { get; } = new("Ruby", "Styles/Themes/DarkRuby.xaml");
    public static ThemeDefinition Cyber { get; } = new("Cyber", "Styles/Themes/DarkCyber.xaml");

    public static List<ThemeDefinition> All { get; } = [Default, Blue, Purple, Forest, Ruby, Cyber];
}
