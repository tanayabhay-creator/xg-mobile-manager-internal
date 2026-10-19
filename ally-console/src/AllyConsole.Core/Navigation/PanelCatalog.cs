using AllyConsole.Core.Input;
using AllyConsole.Core.Settings;
using AllyConsole.Core.Status;

namespace AllyConsole.Core.Navigation;

public sealed record DiagnosticFacts(
    SystemStatus Status,
    string Hotkey,
    string Controller,
    string Startup,
    string LogDirectory,
    string SettingsPath);

public static class PanelCatalog
{
    public static IReadOnlyList<NavItem> Quick(SystemStatus status)
    {
        var volumeEnabled = status.Volume.Available && status.Volume.Value is not null;
        var volume = status.Volume.Value;
        var items = new List<NavItem>
        {
            NavItem.Slider(
                "volume",
                "Volume",
                volumeEnabled ? StatusText.Volume(status) : StatusText.Volume(status),
                volumeEnabled ? volume!.Percent : 0,
                0,
                100,
                5,
                volumeEnabled,
                volumeEnabled ? null : status.Volume.Reason ?? "Volume is unavailable."),
            NavItem.Toggle(
                "mute",
                "Mute",
                volumeEnabled
                    ? (volume!.Muted ? "Muted" : "Sound on")
                    : StatusText.Volume(status),
                volumeEnabled && volume!.Muted,
                volumeEnabled,
                volumeEnabled ? null : status.Volume.Reason ?? "Volume is unavailable."),
            status.Brightness.ToNavItem(),
            NavItem.Status("battery", "Battery", StatusText.Battery(status)),
            NavItem.Status("gpu", "GPUs and displays", StatusText.Adapters(status) + " " + StatusText.Displays(status)),
            NavItem.Action("network", "Network settings", "Opens Windows Settings. This is a shortcut, not an in-app network stack."),
            NavItem.Action("steam", "Steam Big Picture", "Launches steam://open/bigpicture, or the URI in settings."),
            NavItem.Action("desktop", "Desktop mode", "Shows the Windows desktop and keeps Ally Console running."),
            NavItem.Action("settings", "Settings", "Shortcuts, sign-in launch, and the Steam URI."),
            NavItem.Action("diagnostics", "Diagnostics", "Read-only OS, power, display, and adapter details."),
            NavItem.Action("restart", "Restart", "Asks for confirmation, then uses Windows shutdown.exe."),
            NavItem.Action("shutdown", "Shut down", "Asks for confirmation, then uses Windows shutdown.exe."),
            NavItem.Action("exit", "Exit Ally Console", "Closes this companion. Windows stays as it is.")
        };
        return items;
    }

    public static IReadOnlyList<NavItem> Settings(AppSettings settings, IReadOnlyCollection<GamepadButton> chordDraft, string chordPreview)
    {
        var items = new List<NavItem>
        {
            NavItem.Toggle(
                "start-with-windows",
                "Start Ally Console when I sign in",
                "Per-user Startup folder shortcut. Default off. No administrator and no machine-wide startup entry.",
                settings.StartWithWindows),
            NavItem.Toggle(
                "open-steam-on-startup",
                "Open Steam Big Picture when Ally Console starts",
                "Default off. Sign-in launch is the switch above. This one runs only after the companion starts.",
                settings.OpenSteamBigPictureOnStartup),
            NavItem.Action("edit-uri", "Steam launch URI", settings.SteamLaunchUri),
            NavItem.Status("chord-preview", "Controller shortcut", chordPreview),
        };

        foreach (var button in GamepadText.EditorOrder)
        {
            var reserved = button == GamepadButton.Guide;
            items.Add(NavItem.Toggle(
                "chord:" + button,
                GamepadText.DisplayName(button),
                reserved
                    ? "Reserved. Official XInput does not report Guide reliably."
                    : "Include this button in the chord.",
                on: chordDraft.Contains(button),
                enabled: !reserved,
                reason: reserved ? "Guide is reserved." : null));
        }

        items.Add(NavItem.Action("save-chord", "Save controller shortcut", "Checks for an empty chord, duplicate buttons, and reserved buttons."));
        items.Add(NavItem.Action(
            "record-keyboard",
            "Keyboard shortcut",
            settings.KeyboardShortcut.Canonical() + ". If Windows rejects it, Ally Console says so and keeps the previous one when it can."));
        items.Add(NavItem.Action("back-to-quick", "Back to quick panel", ""));
        return items;
    }

    public static IReadOnlyList<NavItem> Diagnostics(DiagnosticFacts facts)
    {
        return new[]
        {
            NavItem.Status("diag-os", "Operating system", StatusText.OperatingSystem(facts.Status)),
            NavItem.Status("diag-battery", "Battery", StatusText.Battery(facts.Status)),
            NavItem.Status("diag-displays", "Displays", StatusText.Displays(facts.Status)),
            NavItem.Status("diag-gpu", "GPU adapters", StatusText.Adapters(facts.Status)),
            NavItem.Status("diag-xg", "XG Mobile", StatusText.XgMobile),
            NavItem.Status("diag-input", "Input", facts.Controller + " " + StatusText.InputCapture),
            NavItem.Status("diag-hotkey", "Keyboard shortcut", facts.Hotkey),
            NavItem.Status("diag-startup", "Sign-in launch", facts.Startup),
            NavItem.Status("diag-logs", "Local logs", facts.LogDirectory),
            NavItem.Status("diag-settings", "Settings file", facts.SettingsPath),
            NavItem.Action("back-to-quick", "Back to quick panel", "")
        };
    }

    public static IReadOnlyList<NavItem> Confirm(string action)
    {
        var (title, detail) = action switch
        {
            "restart" => ("Restart Windows", "Windows will restart this PC. Ally Console does not force-close programs."),
            "shutdown" => ("Shut down Windows", "Windows will shut down this PC. Ally Console does not force-close programs."),
            _ => ("Exit Ally Console", "The companion closes. Windows, Steam, and your game stay as they are.")
        };

        return new[]
        {
            NavItem.Action("cancel", "Cancel", "Return to the quick panel."),
            NavItem.Status("confirm-text", title, detail),
            NavItem.Action("confirm-" + (action is "restart" or "shutdown" ? action : "exit"), title, "Confirm.")
        };
    }
}
