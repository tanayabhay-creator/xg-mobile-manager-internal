namespace AllyConsole.Core.Status;

public sealed record VolumeReading(int Percent, bool Muted);

public sealed record BatteryReading(int Percent, bool Charging, bool NoBatteryPresent);

public sealed record AdapterReading(string Name, string Status);

public sealed record DisplayReading(string Name, bool IsPrimary, bool IsActive, int Width, int Height);

public sealed record Reading<T>(bool Available, T? Value, string? Reason)
{
    public static Reading<T> Of(T value) => new(true, value, null);

    public static Reading<T> Unavailable(string reason) => new(false, default, reason);
}

public sealed record SystemStatus(
    Reading<VolumeReading> Volume,
    BrightnessReading Brightness,
    Reading<BatteryReading> Battery,
    IReadOnlyList<AdapterReading> Adapters,
    string? AdaptersUnavailableReason,
    IReadOnlyList<DisplayReading> Displays,
    string? DisplaysUnavailableReason,
    Reading<string> OperatingSystem,
    bool DevelopmentMode)
{
    public static SystemStatus Unavailable(string reason) => new(
        Reading<VolumeReading>.Unavailable(reason),
        BrightnessReading.Unsupported(reason),
        Reading<BatteryReading>.Unavailable(reason),
        Array.Empty<AdapterReading>(),
        reason,
        Array.Empty<DisplayReading>(),
        reason,
        Reading<string>.Unavailable(reason),
        false);
}
