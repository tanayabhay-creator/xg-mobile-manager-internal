namespace AllyConsole.Core.Status;

public static class DevelopmentReadings
{
    public const int SampleBatteryPercent = 76;

    public const string SampleAdapterName = "Sample GPU (development mode)";

    public static SystemStatus Create() => new(
        Reading<VolumeReading>.Of(new VolumeReading(40, false)),
        BrightnessReading.SupportedPercent(55),
        Reading<BatteryReading>.Of(new BatteryReading(SampleBatteryPercent, Charging: true, NoBatteryPresent: false)),
        new[] { new AdapterReading(SampleAdapterName, "OK") },
        "Development sample. Not a detected adapter.",
        new[] { new DisplayReading("Sample display", true, true, 1920, 1080) },
        "Development sample. Not a connected display.",
        Reading<string>.Of("Development mode"),
        DevelopmentMode: true);
}

public static class StatusPolicy
{
    /// <summary>
    /// Development mode is the only path that returns sample battery, volume, or GPU data.
    /// Normal mode returns the probe, or an explicit unavailable state. It never invents numbers.
    /// </summary>
    public static SystemStatus ForSession(bool developmentMode, SystemStatus? probed)
    {
        if (developmentMode)
            return DevelopmentReadings.Create();
        return probed ?? SystemStatus.Unavailable("Status has not been read from Windows.");
    }
}

public static class PollPolicy
{
    public const int MinimumIntervalMs = 50;

    public static int InputIntervalMs(bool panelVisible, int hiddenMs, int visibleMs)
    {
        var raw = panelVisible ? visibleMs : hiddenMs;
        if (raw < MinimumIntervalMs)
            return MinimumIntervalMs;
        return raw;
    }

    /// <summary>
    /// Volume, brightness, battery, and adapter probes run only while the panel is visible.
    /// </summary>
    public static bool RefreshHardware(bool panelVisible) => panelVisible;
}
