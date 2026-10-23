using AllyConsole.Core.Input;
using AllyConsole.Core.Navigation;

namespace AllyConsole.Core.Tests;

public class ControllerGestureTests
{
    [Fact]
    public void Chord_fires_once_while_held_and_again_after_release()
    {
        var decoder = new ControllerGestureDecoder();
        var chord = ControllerChord.Default;
        var held = Snapshot(GamepadButton.LeftShoulder, GamepadButton.RightShoulder, GamepadButton.Back);

        var first = decoder.Observe(chord, held, TimeSpan.Zero);
        var second = decoder.Observe(chord, held, TimeSpan.FromMilliseconds(50));
        var released = decoder.Observe(chord, ControllerSnapshot.Empty, TimeSpan.FromMilliseconds(80));
        var third = decoder.Observe(chord, held, TimeSpan.FromMilliseconds(100));

        Assert.True(first.OpenOrClosePanel);
        Assert.False(first.Back);
        Assert.False(second.OpenOrClosePanel);
        Assert.False(released.OpenOrClosePanel);
        Assert.True(third.OpenOrClosePanel);
    }

    [Fact]
    public void Back_after_the_chord_is_released_is_navigation()
    {
        var decoder = new ControllerGestureDecoder();
        var chord = ControllerChord.Default;
        var held = Snapshot(GamepadButton.LeftShoulder, GamepadButton.RightShoulder, GamepadButton.Back);
        decoder.Observe(chord, held, TimeSpan.Zero);
        decoder.Observe(chord, ControllerSnapshot.Empty, TimeSpan.FromMilliseconds(20));

        var back = decoder.Observe(chord, Snapshot(GamepadButton.B), TimeSpan.FromMilliseconds(40));

        Assert.False(back.OpenOrClosePanel);
        Assert.True(back.Back);
        Assert.False(back.Confirm);
    }

    [Fact]
    public void Empty_chord_never_opens_the_panel()
    {
        var decoder = new ControllerGestureDecoder();
        var gesture = decoder.Observe(
            new ControllerChord(Array.Empty<GamepadButton>()),
            Snapshot(GamepadButton.A, GamepadButton.B),
            TimeSpan.Zero);

        Assert.False(gesture.OpenOrClosePanel);
    }

    [Fact]
    public void Direction_repeats_only_after_the_initial_delay()
    {
        var decoder = new ControllerGestureDecoder();
        var chord = ControllerChord.Default;
        var down = new ControllerSnapshot(new[] { GamepadButton.DPadDown }, 0, 0);

        var first = decoder.Observe(chord, down, TimeSpan.Zero);
        var early = decoder.Observe(chord, down, TimeSpan.FromMilliseconds(100));
        var repeated = decoder.Observe(chord, down, ControllerGestureDecoder.InitialRepeat);
        var tooSoon = decoder.Observe(chord, down, ControllerGestureDecoder.InitialRepeat + TimeSpan.FromMilliseconds(50));
        var again = decoder.Observe(
            chord,
            down,
            ControllerGestureDecoder.InitialRepeat + ControllerGestureDecoder.RepeatInterval);

        Assert.Equal(NavDirection.Down, first.Direction);
        Assert.Null(early.Direction);
        Assert.Equal(NavDirection.Down, repeated.Direction);
        Assert.Null(tooSoon.Direction);
        Assert.Equal(NavDirection.Down, again.Direction);
    }

    [Fact]
    public void Stick_past_the_deadzone_moves_focus()
    {
        var decoder = new ControllerGestureDecoder();
        var gesture = decoder.Observe(
            ControllerChord.Default,
            new ControllerSnapshot(Array.Empty<GamepadButton>(), 0, 0.9f),
            TimeSpan.Zero);

        Assert.Equal(NavDirection.Up, gesture.Direction);
    }

    private static ControllerSnapshot Snapshot(params GamepadButton[] buttons)
        => new(buttons, 0, 0);
}
