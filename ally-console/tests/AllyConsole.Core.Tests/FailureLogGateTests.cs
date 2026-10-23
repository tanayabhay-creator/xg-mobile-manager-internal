using AllyConsole.Core.Logging;
using AllyConsole.Core.Status;

namespace AllyConsole.Core.Tests;

public class FailureLogGateTests
{
    [Fact]
    public void Same_failure_is_written_once_until_it_recovers_or_changes()
    {
        var gate = new FailureLogGate();

        Assert.True(gate.ShouldWrite("startup.shortcut", "startup-folder-missing The folder is missing."));
        Assert.False(gate.ShouldWrite("startup.shortcut", "startup-folder-missing The folder is missing."));
        Assert.True(gate.ShouldWrite("startup.shortcut", "startup-failed Access denied."));
        Assert.False(gate.ShouldWrite("startup.shortcut", "startup-failed Access denied."));

        gate.Recovered("startup.shortcut");
        Assert.True(gate.ShouldWrite("startup.shortcut", "startup-failed Access denied."));
    }

    [Fact]
    public void Shortcut_registration_failure_does_not_repeat_until_it_clears()
    {
        var gate = new FailureLogGate();
        const string message = "hotkey-register-failed RegisterHotKey failed (Win32 1409).";

        Assert.True(gate.ShouldWrite("shortcut.hotkey", message));
        Assert.False(gate.ShouldWrite("shortcut.hotkey", message));
        gate.Recovered("shortcut.hotkey");
        Assert.True(gate.ShouldWrite("shortcut.hotkey", message));
    }

    [Fact]
    public void Unsupported_brightness_is_logged_once_across_status_refreshes()
    {
        var gate = new FailureLogGate();
        var unsupported = SystemStatus.Unavailable("WmiMonitorBrightness was not returned.");

        var first = gate.SelectNew(UnsupportedIntegrationLog.Failures(unsupported), UnsupportedIntegrationLog.Events);
        var second = gate.SelectNew(UnsupportedIntegrationLog.Failures(unsupported), UnsupportedIntegrationLog.Events);

        Assert.Contains(first, notice => notice.EventName == "integration.brightness");
        Assert.DoesNotContain(second, notice => notice.EventName == "integration.brightness");
        Assert.Empty(second);
    }

    [Fact]
    public void Supported_reading_clears_the_failure_so_a_later_loss_is_logged()
    {
        var gate = new FailureLogGate();
        var unsupported = SystemStatus.Unavailable("WmiMonitorBrightness was not returned.");
        var supported = new SystemStatus(
            Reading<VolumeReading>.Of(new VolumeReading(10, false)),
            BrightnessReading.SupportedPercent(40),
            Reading<BatteryReading>.Of(new BatteryReading(50, false, false)),
            new[] { new AdapterReading("AMD Radeon Graphics", "OK") },
            null,
            new[] { new DisplayReading("Internal", true, true, 1920, 1080) },
            null,
            Reading<string>.Of("Microsoft Windows"),
            DevelopmentMode: false);

        Assert.NotEmpty(gate.SelectNew(UnsupportedIntegrationLog.Failures(unsupported), UnsupportedIntegrationLog.Events));
        Assert.Empty(gate.SelectNew(UnsupportedIntegrationLog.Failures(supported), UnsupportedIntegrationLog.Events));

        var again = gate.SelectNew(UnsupportedIntegrationLog.Failures(unsupported), UnsupportedIntegrationLog.Events);
        Assert.Contains(again, notice => notice.EventName == "integration.brightness");
    }

    [Fact]
    public void Development_samples_are_not_unsupported_integration_failures()
    {
        var gate = new FailureLogGate();
        var notices = gate.SelectNew(
            UnsupportedIntegrationLog.Failures(DevelopmentReadings.Create()),
            UnsupportedIntegrationLog.Events);

        Assert.Empty(notices);
    }
}
