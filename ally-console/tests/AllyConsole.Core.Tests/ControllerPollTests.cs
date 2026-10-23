using AllyConsole.Core.Input;
using AllyConsole.Core.Settings;
using AllyConsole.Core.Status;

namespace AllyConsole.Core.Tests;

public class ControllerPollTests
{
    [Fact]
    public void Hidden_poll_defaults_to_150ms_and_that_is_the_detection_delay_bound()
    {
        var settings = AppSettings.CreateDefault();

        Assert.Equal(150, settings.HiddenPollIntervalMs);
        Assert.Equal(50, settings.VisiblePollIntervalMs);
        Assert.Equal(
            150,
            PollPolicy.MaxHeldChordDetectionDelayMs(false, settings.HiddenPollIntervalMs, settings.VisiblePollIntervalMs));
        Assert.Equal(
            50,
            PollPolicy.MaxHeldChordDetectionDelayMs(true, settings.HiddenPollIntervalMs, settings.VisiblePollIntervalMs));
        Assert.Equal(
            ControllerChord.Default.Buttons,
            new[] { GamepadButton.LeftShoulder, GamepadButton.RightShoulder, GamepadButton.Back });
    }

    [Fact]
    public void Normalize_keeps_the_hidden_interval_inside_the_poll_floor()
    {
        var settings = AppSettings.CreateDefault();
        settings.HiddenPollIntervalMs = 1;
        settings.VisiblePollIntervalMs = 1;

        var (normalized, _) = settings.Normalize();

        Assert.Equal(50, normalized.HiddenPollIntervalMs);
        Assert.Equal(50, normalized.VisiblePollIntervalMs);
        Assert.Equal(50, PollPolicy.MaxHeldChordDetectionDelayMs(false, normalized.HiddenPollIntervalMs, normalized.VisiblePollIntervalMs));
    }
}
