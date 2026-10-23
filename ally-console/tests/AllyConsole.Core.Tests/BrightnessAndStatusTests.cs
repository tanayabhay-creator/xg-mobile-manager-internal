using AllyConsole.Core.Navigation;
using AllyConsole.Core.Status;

namespace AllyConsole.Core.Tests;

public class BrightnessAndStatusTests
{
    [Fact]
    public void Unsupported_brightness_is_disabled_and_explains_why()
    {
        var item = BrightnessReading.Unsupported().ToNavItem();

        Assert.Equal("brightness", item.Id);
        Assert.False(item.Enabled);
        Assert.NotNull(item.DisabledReason);
        Assert.Contains("WmiMonitorBrightness", item.DisabledReason, StringComparison.Ordinal);
        Assert.Contains("firmware", item.DisabledReason, StringComparison.OrdinalIgnoreCase);
        Assert.Null(BrightnessReading.Unsupported().Percent);
    }

    [Fact]
    public void Supported_brightness_is_an_enabled_slider()
    {
        var item = BrightnessReading.SupportedPercent(140).ToNavItem();

        Assert.True(item.Enabled);
        Assert.Equal(100, item.Value);
        Assert.Null(item.DisabledReason);
    }

    [Fact]
    public void Normal_mode_does_not_invent_battery_volume_or_gpu_samples()
    {
        var status = StatusPolicy.ForSession(developmentMode: false, probed: null);
        var items = PanelCatalog.Quick(status);
        var battery = Assert.Single(items, item => item.Id == "battery");
        var gpu = Assert.Single(items, item => item.Id == "gpu");

        Assert.False(status.DevelopmentMode);
        Assert.False(status.Battery.Available);
        Assert.False(status.Volume.Available);
        Assert.Empty(status.Adapters);
        Assert.DoesNotContain(DevelopmentReadings.SampleBatteryPercent.ToString(), battery.Detail, StringComparison.Ordinal);
        Assert.DoesNotContain(DevelopmentReadings.SampleAdapterName, gpu.Detail, StringComparison.Ordinal);
        Assert.DoesNotContain("Sample:", battery.Detail, StringComparison.Ordinal);
        Assert.Contains("Unavailable", battery.Detail, StringComparison.Ordinal);
        Assert.Contains("not proof of which GPU", gpu.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Development_mode_labels_sample_readings()
    {
        var status = StatusPolicy.ForSession(developmentMode: true, probed: null);
        var battery = StatusText.Battery(status);
        var adapters = StatusText.Adapters(status);

        Assert.True(status.DevelopmentMode);
        Assert.StartsWith("Sample:", battery, StringComparison.Ordinal);
        Assert.Contains(DevelopmentReadings.SampleAdapterName, adapters, StringComparison.Ordinal);
        Assert.Contains("not proof of which GPU", adapters, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Diagnostics_do_not_claim_xg_mobile_control()
    {
        var facts = new DiagnosticFacts(
            SystemStatus.Unavailable("not read"),
            "Ctrl+Alt+Shift+F1",
            "No XInput controller reported.",
            "Off",
            "logs",
            "settings.json");
        var items = PanelCatalog.Diagnostics(facts);
        var xg = Assert.Single(items, item => item.Id == "diag-xg");

        Assert.Contains("does not control ASUS XG Mobile", xg.Detail, StringComparison.Ordinal);
        Assert.Contains("Stage 3", xg.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Hidden_panel_does_not_refresh_hardware_and_poll_interval_has_a_floor()
    {
        Assert.False(PollPolicy.RefreshHardware(panelVisible: false));
        Assert.True(PollPolicy.RefreshHardware(panelVisible: true));
        Assert.Equal(150, PollPolicy.InputIntervalMs(false, 150, 50));
        Assert.Equal(PollPolicy.MinimumIntervalMs, PollPolicy.InputIntervalMs(false, 1, 50));
        Assert.True(PollPolicy.MinimumIntervalMs >= 50);
    }
}
