using AllyConsole.Core.Input;
using AllyConsole.Core.Settings;
using AllyConsole.Core.Steam;

namespace AllyConsole.Core.Tests;

public class SettingsStoreTests
{
    [Fact]
    public void Missing_file_uses_defaults_and_does_not_launch_at_sign_in()
    {
        using var folder = new TempFolder();
        var path = AppPaths.SettingsFile(folder.Path);
        var load = new SettingsStore().Load(path);

        Assert.False(load.FromDisk);
        Assert.True(load.CanPersist);
        Assert.Equal(AppSettings.CurrentSchemaVersion, load.Settings.SchemaVersion);
        Assert.False(load.Settings.StartWithWindows);
        Assert.False(load.Settings.OpenSteamBigPictureOnStartup);
        Assert.Equal(SteamLaunch.DefaultUri, load.Settings.SteamLaunchUri);
        Assert.Equal(new[] { "LeftShoulder", "RightShoulder", "Back" }, load.Settings.ControllerChord);
        Assert.Equal("Ctrl+Alt+Shift+F1", load.Settings.KeyboardShortcut.Canonical());
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Round_trip_preserves_versioned_settings()
    {
        using var folder = new TempFolder();
        var path = Path.Combine(folder.Path, "settings.json");
        var store = new SettingsStore();
        var settings = AppSettings.CreateDefault();
        settings.StartWithWindows = true;
        settings.OpenSteamBigPictureOnStartup = true;
        settings.SteamLaunchUri = "steam://open/bigpicture";
        settings.ControllerChord = new List<string> { "LeftShoulder", "Start" };
        settings.KeyboardShortcut = new KeyboardShortcut
        {
            Control = true,
            Alt = false,
            Shift = true,
            Win = false,
            Key = "f8"
        };
        settings.HiddenPollIntervalMs = 200;
        settings.VisiblePollIntervalMs = 80;
        settings.StatusRefreshIntervalMs = 1500;
        settings.LogMaxBytes = 8192;
        settings.LogMaxFiles = 4;

        store.Save(path, settings);
        var raw = File.ReadAllText(path);
        Assert.Contains("\"schemaVersion\": 1", raw, StringComparison.Ordinal);

        var loaded = store.Load(path);
        Assert.True(loaded.FromDisk);
        Assert.Null(loaded.Warning);
        Assert.True(loaded.Settings.StartWithWindows);
        Assert.True(loaded.Settings.OpenSteamBigPictureOnStartup);
        Assert.Equal("steam://open/bigpicture", loaded.Settings.SteamLaunchUri);
        Assert.Equal(new[] { "LeftShoulder", "Start" }, loaded.Settings.ControllerChord);
        Assert.Equal("Ctrl+Shift+F8", loaded.Settings.KeyboardShortcut.Canonical());
        Assert.Equal(200, loaded.Settings.HiddenPollIntervalMs);
        Assert.Equal(80, loaded.Settings.VisiblePollIntervalMs);
        Assert.Equal(1500, loaded.Settings.StatusRefreshIntervalMs);
        Assert.Equal(8192, loaded.Settings.LogMaxBytes);
        Assert.Equal(4, loaded.Settings.LogMaxFiles);
    }

    [Fact]
    public void Newer_schema_is_not_overwritten()
    {
        using var folder = new TempFolder();
        var path = Path.Combine(folder.Path, "settings.json");
        const string original = "{\n  \"schemaVersion\": 99,\n  \"startWithWindows\": true,\n  \"marker\": \"keep\"\n}\n";
        File.WriteAllText(path, original);

        var load = new SettingsStore().Load(path);

        Assert.False(load.CanPersist);
        Assert.False(load.Settings.StartWithWindows);
        Assert.Contains("newer", load.Warning, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(original, File.ReadAllText(path));
    }

    [Fact]
    public void Corrupt_file_is_left_in_place()
    {
        using var folder = new TempFolder();
        var path = Path.Combine(folder.Path, "settings.json");
        File.WriteAllText(path, "{");

        var load = new SettingsStore().Load(path);

        Assert.False(load.CanPersist);
        Assert.False(load.Settings.StartWithWindows);
        Assert.Equal("{", File.ReadAllText(path));
    }

    [Fact]
    public void Invalid_chord_in_an_otherwise_valid_file_falls_back_without_hiding_the_warning()
    {
        using var folder = new TempFolder();
        var path = Path.Combine(folder.Path, "settings.json");
        File.WriteAllText(path, """
            {
              "schemaVersion": 1,
              "controllerChord": ["Guide"]
            }
            """);

        var load = new SettingsStore().Load(path);

        Assert.True(load.CanPersist);
        Assert.NotNull(load.Warning);
        Assert.Equal(new[] { "LeftShoulder", "RightShoulder", "Back" }, load.Settings.ControllerChord);
    }
}
