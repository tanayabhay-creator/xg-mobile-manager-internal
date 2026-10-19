using AllyConsole.Core.Navigation;

namespace AllyConsole.Core.Status;

public sealed record BrightnessReading(bool Supported, int? Percent, string? UnavailableReason)
{
    public const string UnsupportedExplanation =
        "Brightness is unavailable. Windows did not expose WmiMonitorBrightness for a panel this app can set. " +
        "The Ally internal screen often depends on the vendor display driver. " +
        "Ally Console will not write firmware or embedded-controller registers to guess.";

    public static BrightnessReading Unsupported(string? reason = null)
        => new(false, null, string.IsNullOrWhiteSpace(reason) ? UnsupportedExplanation : reason.Trim());

    public static BrightnessReading SupportedPercent(int percent)
        => new(true, Math.Clamp(percent, 0, 100), null);

    public NavItem ToNavItem()
    {
        if (!Supported || Percent is null)
        {
            var reason = string.IsNullOrWhiteSpace(UnavailableReason) ? UnsupportedExplanation : UnavailableReason;
            return NavItem.Slider("brightness", "Brightness", reason, 0, 0, 100, 5, enabled: false, reason: reason);
        }

        return NavItem.Slider(
            "brightness",
            "Brightness",
            Percent.Value + "%",
            Percent.Value,
            0,
            100,
            5,
            enabled: true,
            reason: null);
    }
}
