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

    public static List<ThemeDefinition> All { get; } = [Default, Blue, Purple];
}
