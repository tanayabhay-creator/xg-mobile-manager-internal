namespace AllyConsole.Core.Input;

/// <summary>
/// Buttons on an XInput gamepad. Ally face and shoulder controls usually arrive this way.
/// Guide is reserved: the documented XInputGetState API does not report it.
/// </summary>
public enum GamepadButton
{
    DPadUp,
    DPadDown,
    DPadLeft,
    DPadRight,
    Start,
    Back,
    LeftThumb,
    RightThumb,
    LeftShoulder,
    RightShoulder,
    Guide,
    A,
    B,
    X,
    Y,
    LeftTrigger,
    RightTrigger
}
