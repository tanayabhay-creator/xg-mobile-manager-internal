namespace AllyConsole.Core.Status;

public static class StatusText
{
    public const string GpuDisclaimer =
        "Detected adapters are not proof of which GPU is rendering the current game, and they are not an XG Mobile activation state.";

    public const string XgMobile =
        "Stage 1 does not control ASUS XG Mobile. There is no invented activation command. " +
        "Stage 3 may add a clearly labelled shortcut to the official ASUS tool. " +
        "Do not read adapter names as an activation state.";

    public const string InputCapture =
        "The controller chord is read with XInput. XInput is shared with games; this app does not inject, hook graphics APIs, or block the device. " +
        "Keyboard focus can stay with Ally Console for a borderless game. Exclusive fullscreen and a window running elevated above this app can ignore the hotkey and focus return.";

    public static string Volume(SystemStatus status)
    {
        if (status.DevelopmentMode)
            return Sample(VolumeBody(status));
        return VolumeBody(status);
    }

    public static string Battery(SystemStatus status)
    {
        if (status.DevelopmentMode)
            return Sample(BatteryBody(status));
        return BatteryBody(status);
    }

    public static string Adapters(SystemStatus status)
    {
        if (status.DevelopmentMode)
            return Sample(AdapterBody(status));
        return AdapterBody(status);
    }

    public static string Displays(SystemStatus status)
    {
        if (status.DevelopmentMode)
            return Sample(DisplayBody(status));
        return DisplayBody(status);
    }

    public static string OperatingSystem(SystemStatus status)
    {
        if (status.DevelopmentMode)
            return "Sample: development mode is on. This is not the PC operating system.";
        if (!status.OperatingSystem.Available || status.OperatingSystem.Value is null)
            return Unavailable(status.OperatingSystem.Reason);
        return status.OperatingSystem.Value;
    }

    private static string VolumeBody(SystemStatus status)
    {
        if (!status.Volume.Available || status.Volume.Value is null)
            return Unavailable(status.Volume.Reason);
        var muted = status.Volume.Value.Muted ? "Muted" : "Unmuted";
        return status.Volume.Value.Percent + "% · " + muted;
    }

    private static string BatteryBody(SystemStatus status)
    {
        if (!status.Battery.Available || status.Battery.Value is null)
            return Unavailable(status.Battery.Reason);
        if (status.Battery.Value.NoBatteryPresent)
            return "No battery reported by Windows.";
        var charging = status.Battery.Value.Charging ? "Charging" : "Not charging";
        return status.Battery.Value.Percent + "% · " + charging;
    }

    private static string AdapterBody(SystemStatus status)
    {
        if (status.Adapters.Count == 0)
            return Unavailable(status.AdaptersUnavailableReason) + " " + GpuDisclaimer;
        var names = string.Join("; ", status.Adapters.Select(adapter => adapter.Name + " (" + adapter.Status + ")"));
        return names + ". " + GpuDisclaimer;
    }

    private static string DisplayBody(SystemStatus status)
    {
        if (status.Displays.Count == 0)
            return Unavailable(status.DisplaysUnavailableReason);
        return string.Join("; ", status.Displays.Select(FormatDisplay));
    }

    private static string FormatDisplay(DisplayReading display)
    {
        var flags = new List<string>();
        if (display.IsPrimary)
            flags.Add("primary");
        flags.Add(display.IsActive ? "active" : "inactive");
        if (display.Width > 0 && display.Height > 0)
            flags.Add(display.Width + "x" + display.Height);
        var name = string.IsNullOrWhiteSpace(display.Name) ? "Unnamed display" : display.Name;
        return name + " (" + string.Join(", ", flags) + ")";
    }

    private static string Unavailable(string? reason)
        => "Unavailable: " + (string.IsNullOrWhiteSpace(reason) ? "Windows did not return this reading." : reason.Trim());

    private static string Sample(string body) => "Sample: " + body;
}
