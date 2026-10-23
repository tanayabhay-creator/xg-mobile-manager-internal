using AllyConsole.Core.Status;

namespace AllyConsole.App.Windows;

internal sealed class WindowsStatusReader
{
    public SystemStatus Read(bool developmentMode)
    {
        if (developmentMode)
            return DevelopmentReadings.Create();

        var adapters = SafeAdapters();
        var displays = SafeDisplays();
        return new SystemStatus(
            SafeVolume(),
            SafeBrightness(),
            SafeBattery(),
            adapters.Adapters,
            adapters.Reason,
            displays.Displays,
            displays.Reason,
            SafeOs(),
            DevelopmentMode: false);
    }

    private static Reading<VolumeReading> SafeVolume()
    {
        try
        {
            return MasterVolume.Read();
        }
        catch (Exception ex)
        {
            return Reading<VolumeReading>.Unavailable(ex.Message);
        }
    }

    private static BrightnessReading SafeBrightness()
    {
        try
        {
            return BrightnessWmi.Read();
        }
        catch (Exception ex)
        {
            return BrightnessReading.Unsupported(BrightnessReading.UnsupportedExplanation + " " + ex.Message);
        }
    }

    private static Reading<BatteryReading> SafeBattery()
    {
        try
        {
            return DeviceStatus.Battery();
        }
        catch (Exception ex)
        {
            return Reading<BatteryReading>.Unavailable(ex.Message);
        }
    }

    private static (IReadOnlyList<AdapterReading> Adapters, string? Reason) SafeAdapters()
    {
        try
        {
            return DeviceStatus.Adapters();
        }
        catch (Exception ex)
        {
            return (Array.Empty<AdapterReading>(), ex.Message);
        }
    }

    private static (IReadOnlyList<DisplayReading> Displays, string? Reason) SafeDisplays()
    {
        try
        {
            return DeviceStatus.Displays();
        }
        catch (Exception ex)
        {
            return (Array.Empty<DisplayReading>(), ex.Message);
        }
    }

    private static Reading<string> SafeOs()
    {
        try
        {
            return DeviceStatus.OperatingSystem();
        }
        catch (Exception ex)
        {
            return Reading<string>.Unavailable(ex.Message);
        }
    }
}
