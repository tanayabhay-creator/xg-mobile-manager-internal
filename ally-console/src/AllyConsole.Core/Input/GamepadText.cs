namespace AllyConsole.Core.Input;

public static class GamepadText
{
    public static string DisplayName(GamepadButton button) => button switch
    {
        GamepadButton.LeftShoulder => "Left bumper",
        GamepadButton.RightShoulder => "Right bumper",
        GamepadButton.Back => "View (Back)",
        GamepadButton.Start => "Menu (Start)",
        GamepadButton.LeftThumb => "Left stick click",
        GamepadButton.RightThumb => "Right stick click",
        GamepadButton.LeftTrigger => "Left trigger",
        GamepadButton.RightTrigger => "Right trigger",
        GamepadButton.DPadUp => "D-pad up",
        GamepadButton.DPadDown => "D-pad down",
        GamepadButton.DPadLeft => "D-pad left",
        GamepadButton.DPadRight => "D-pad right",
        GamepadButton.Guide => "Guide",
        _ => button.ToString()
    };

    public static string FormatChord(IReadOnlyList<GamepadButton> buttons)
    {
        if (buttons.Count == 0)
            return "No buttons selected";
        return string.Join(" + ", buttons.Select(DisplayName));
    }

    public static IReadOnlyList<GamepadButton> EditorOrder { get; } = new[]
    {
        GamepadButton.LeftShoulder,
        GamepadButton.RightShoulder,
        GamepadButton.Back,
        GamepadButton.Start,
        GamepadButton.A,
        GamepadButton.B,
        GamepadButton.X,
        GamepadButton.Y,
        GamepadButton.LeftTrigger,
        GamepadButton.RightTrigger,
        GamepadButton.LeftThumb,
        GamepadButton.RightThumb,
        GamepadButton.DPadUp,
        GamepadButton.DPadDown,
        GamepadButton.DPadLeft,
        GamepadButton.DPadRight,
        GamepadButton.Guide
    };
}
