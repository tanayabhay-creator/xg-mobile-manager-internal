namespace AllyConsole.Core.Input;

public sealed class ControllerChord
{
    public ControllerChord(IEnumerable<GamepadButton>? buttons)
    {
        Buttons = buttons?.ToArray() ?? Array.Empty<GamepadButton>();
    }

    public IReadOnlyList<GamepadButton> Buttons { get; }

    public IReadOnlyList<string> ButtonNames => Buttons.Select(button => button.ToString()).ToArray();

    /// <summary>
    /// Left bumper + right bumper + View (XInput Back). A chord, not a single in-game button.
    /// </summary>
    public static ControllerChord Default { get; } = new(new[]
    {
        GamepadButton.LeftShoulder,
        GamepadButton.RightShoulder,
        GamepadButton.Back
    });
}
