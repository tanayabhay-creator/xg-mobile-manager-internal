using System.Text.Json;
using System.Text.Json.Serialization;

namespace AllyConsole.Core.Settings;

public sealed class SettingsLoad
{
    public required AppSettings Settings { get; init; }

    public bool FromDisk { get; init; }

    public string? Warning { get; init; }

    /// <summary>
    /// When false, the caller must not overwrite the file. The on-disk JSON was newer or unreadable.
    /// </summary>
    public bool CanPersist { get; init; } = true;
}

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public SettingsLoad Load(string path)
    {
        if (!File.Exists(path))
        {
            return new SettingsLoad
            {
                Settings = AppSettings.CreateDefault(),
                FromDisk = false,
                CanPersist = true
            };
        }

        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new SettingsLoad
            {
                Settings = AppSettings.CreateDefault(),
                Warning = "Could not read settings. The file was left in place. " + ex.Message,
                CanPersist = false
            };
        }

        AppSettings? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<AppSettings>(text, Json);
        }
        catch (JsonException)
        {
            return new SettingsLoad
            {
                Settings = AppSettings.CreateDefault(),
                Warning = "Settings file is unreadable. It was left in place. Defaults are in use until the file is fixed.",
                CanPersist = false
            };
        }

        if (parsed is null)
        {
            return new SettingsLoad
            {
                Settings = AppSettings.CreateDefault(),
                Warning = "Settings file was empty. It was left in place.",
                CanPersist = false
            };
        }

        if (parsed.SchemaVersion > AppSettings.CurrentSchemaVersion)
        {
            return new SettingsLoad
            {
                Settings = AppSettings.CreateDefault(),
                Warning = "Settings schema " + parsed.SchemaVersion
                    + " is newer than this app (" + AppSettings.CurrentSchemaVersion
                    + "). The file was not changed.",
                CanPersist = false
            };
        }

        var (normalized, warning) = parsed.Normalize();
        return new SettingsLoad
        {
            Settings = normalized,
            FromDisk = true,
            Warning = warning,
            CanPersist = true
        };
    }

    public void Save(string path, AppSettings settings)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        settings.SchemaVersion = AppSettings.CurrentSchemaVersion;
        var json = JsonSerializer.Serialize(settings, Json) + Environment.NewLine;
        var temp = path + ".tmp";
        File.WriteAllText(temp, json);
        File.Move(temp, path, overwrite: true);
    }
}
