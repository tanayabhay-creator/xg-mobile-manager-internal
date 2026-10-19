using AllyConsole.Core.Input;
using AllyConsole.Core.Steam;

namespace AllyConsole.Core.Settings;

public sealed class AppSettings
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public bool StartWithWindows { get; set; }

    public bool OpenSteamBigPictureOnStartup { get; set; }

    public string SteamLaunchUri { get; set; } = SteamLaunch.DefaultUri;

    public List<string> ControllerChord { get; set; } = new()
    {
        nameof(GamepadButton.LeftShoulder),
        nameof(GamepadButton.RightShoulder),
        nameof(GamepadButton.Back)
    };

    public KeyboardShortcut KeyboardShortcut { get; set; } = new();

    public int HiddenPollIntervalMs { get; set; } = 150;

    public int VisiblePollIntervalMs { get; set; } = 50;

    public int StatusRefreshIntervalMs { get; set; } = 2000;

    public int LogMaxBytes { get; set; } = 256 * 1024;

    public int LogMaxFiles { get; set; } = 5;

    public static AppSettings CreateDefault() => new();

    public (AppSettings Settings, string? Warning) Normalize()
    {
        var warnings = new List<string>();
        SchemaVersion = CurrentSchemaVersion;

        if (string.IsNullOrWhiteSpace(SteamLaunchUri)
            || !SteamLaunchUri.Trim().StartsWith("steam://", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Steam launch URI was reset to steam://open/bigpicture.");
            SteamLaunchUri = SteamLaunch.DefaultUri;
        }
        else
        {
            SteamLaunchUri = SteamLaunchUri.Trim();
        }

        HiddenPollIntervalMs = Math.Clamp(HiddenPollIntervalMs == 0 ? 150 : HiddenPollIntervalMs, 50, 2000);
        VisiblePollIntervalMs = Math.Clamp(VisiblePollIntervalMs == 0 ? 50 : VisiblePollIntervalMs, 50, 500);
        StatusRefreshIntervalMs = Math.Clamp(StatusRefreshIntervalMs == 0 ? 2000 : StatusRefreshIntervalMs, 500, 60_000);
        LogMaxBytes = Math.Clamp(LogMaxBytes == 0 ? 256 * 1024 : LogMaxBytes, 4_096, 10_000_000);
        LogMaxFiles = Math.Clamp(LogMaxFiles == 0 ? 5 : LogMaxFiles, 1, 20);

        ControllerChord ??= new List<string>();
        var chordDecision = ShortcutRules.ParseControllerButtons(ControllerChord, out var buttons);
        if (!chordDecision.Accepted)
        {
            warnings.Add("Controller shortcut was reset to the default. " + chordDecision.Message);
            ControllerChord = DefaultChordNames();
        }
        else
        {
            ControllerChord = buttons.Select(button => button.ToString()).ToList();
        }

        KeyboardShortcut ??= new KeyboardShortcut();
        var keyDecision = ShortcutRules.ValidateKeyboard(KeyboardShortcut, Array.Empty<string>());
        if (!keyDecision.Accepted)
        {
            warnings.Add("Keyboard shortcut was reset to the default. " + keyDecision.Message);
            KeyboardShortcut = new KeyboardShortcut();
        }

        return (this, warnings.Count == 0 ? null : string.Join(" ", warnings));
    }

    private static List<string> DefaultChordNames() => new()
    {
        nameof(GamepadButton.LeftShoulder),
        nameof(GamepadButton.RightShoulder),
        nameof(GamepadButton.Back)
    };
}
