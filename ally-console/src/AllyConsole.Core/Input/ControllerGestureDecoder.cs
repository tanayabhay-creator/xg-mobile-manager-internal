using AllyConsole.Core.Navigation;

namespace AllyConsole.Core.Input;

public readonly record struct ControllerSnapshot(
    IReadOnlyCollection<GamepadButton> Buttons,
    float LeftStickX,
    float LeftStickY)
{
    public static ControllerSnapshot Empty { get; } = new(Array.Empty<GamepadButton>(), 0, 0);
}

public readonly record struct ControllerGesture(
    bool OpenOrClosePanel,
    NavDirection? Direction,
    bool Confirm,
    bool Back);

/// <summary>
/// Turns polled XInput snapshots into edge-triggered gestures.
/// XInput is shared with the game. This decoder only reads; it does not swallow the device.
/// </summary>
public sealed class ControllerGestureDecoder
{
    public static readonly TimeSpan InitialRepeat = TimeSpan.FromMilliseconds(400);
    public static readonly TimeSpan RepeatInterval = TimeSpan.FromMilliseconds(140);
    public const float StickDeadzone = 0.55f;

    private readonly HashSet<GamepadButton> _previous = new();
    private bool _chordHeld;
    private bool _suppressUntilChordReleased;
    private NavDirection? _heldDirection;
    private TimeSpan _directionSince;
    private TimeSpan _lastRepeat;

    public ControllerGesture Observe(ControllerChord chord, ControllerSnapshot snapshot, TimeSpan now)
    {
        var held = snapshot.Buttons as HashSet<GamepadButton> ?? snapshot.Buttons.ToHashSet();
        var chordButtons = chord.Buttons;
        var chordDown = chordButtons.Count > 0 && chordButtons.All(held.Contains);
        var open = chordDown && !_chordHeld;
        _chordHeld = chordDown;
        if (open)
            _suppressUntilChordReleased = true;
        if (_suppressUntilChordReleased && !chordButtons.Any(held.Contains))
            _suppressUntilChordReleased = false;

        var confirm = false;
        var back = false;
        if (!_suppressUntilChordReleased && !open)
        {
            confirm = Rose(GamepadButton.A, held);
            back = Rose(GamepadButton.B, held);
        }

        var direction = _suppressUntilChordReleased || open ? null : ReadDirection(snapshot, held);
        NavDirection? emitted = null;
        if (direction is null)
        {
            _heldDirection = null;
        }
        else if (direction != _heldDirection)
        {
            _heldDirection = direction;
            _directionSince = now;
            _lastRepeat = now;
            emitted = direction;
        }
        else if (now - _directionSince >= InitialRepeat && now - _lastRepeat >= RepeatInterval)
        {
            _lastRepeat = now;
            emitted = direction;
        }

        _previous.Clear();
        foreach (var button in held)
            _previous.Add(button);

        return new ControllerGesture(open, emitted, confirm, back);
    }

    private bool Rose(GamepadButton button, HashSet<GamepadButton> held)
        => held.Contains(button) && !_previous.Contains(button);

    private static NavDirection? ReadDirection(ControllerSnapshot snapshot, HashSet<GamepadButton> held)
    {
        if (held.Contains(GamepadButton.DPadUp))
            return NavDirection.Up;
        if (held.Contains(GamepadButton.DPadDown))
            return NavDirection.Down;
        if (held.Contains(GamepadButton.DPadLeft))
            return NavDirection.Left;
        if (held.Contains(GamepadButton.DPadRight))
            return NavDirection.Right;

        var x = snapshot.LeftStickX;
        var y = snapshot.LeftStickY;
        if (Math.Abs(x) < StickDeadzone && Math.Abs(y) < StickDeadzone)
            return null;
        if (Math.Abs(y) >= Math.Abs(x))
            return y > 0 ? NavDirection.Up : NavDirection.Down;
        return x > 0 ? NavDirection.Right : NavDirection.Left;
    }
}
