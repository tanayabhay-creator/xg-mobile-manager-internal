namespace AllyConsole.Core.Settings;

public static class AppPaths
{
    public const string FolderName = "AllyConsole";

    public static string Root(string localAppData) => Path.Combine(localAppData, FolderName);

    public static string SettingsFile(string localAppData) => Path.Combine(Root(localAppData), "settings.json");

    public static string LogDirectory(string localAppData) => Path.Combine(Root(localAppData), "logs");
}
