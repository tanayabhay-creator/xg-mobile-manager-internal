using System.Runtime.InteropServices;
using AllyConsole.Core.Input;

namespace AllyConsole.App.Windows;

/// <summary>
/// Polls the documented XInputGetState entry points. This does not call the undocumented
/// Guide-button ordinal, and it cannot stop a game from reading the same pad.
/// </summary>
internal sealed class XInputReader
{
    private const int ErrorSuccess = 0;
    private const byte TriggerThreshold = 128;

    private readonly object _gate = new();
    private int _api;

    public string? UnavailableReason { get; private set; }

    public int? ConnectedUserIndex { get; private set; }

    public string Summary
    {
        get
        {
            lock (_gate)
            {
                if (UnavailableReason is not null)
                    return "XInput unavailable: " + UnavailableReason;
                if (ConnectedUserIndex is int index)
                    return "XInput controller " + index + " is connected. Ally controls usually appear as an XInput gamepad.";
                return "No XInput controller reported.";
            }
        }
    }

    public ControllerSnapshot Read()
    {
        lock (_gate)
        {
            if (_api == 0)
                _api = Probe();
            if (_api < 0)
                return ControllerSnapshot.Empty;

            for (var user = 0; user < 4; user++)
            {
                XInputState state;
                var code = _api == 1
                    ? Native.XInputGetState14(user, out state)
                    : Native.XInputGetState910(user, out state);
                if (code != ErrorSuccess)
                    continue;

                ConnectedUserIndex = user;
                return Map(state);
            }

            ConnectedUserIndex = null;
            return ControllerSnapshot.Empty;
        }
    }

    private int Probe()
    {
        try
        {
            Native.XInputGetState14(0, out _);
            return 1;
        }
        catch (DllNotFoundException)
        {
            try
            {
                Native.XInputGetState910(0, out _);
                return 2;
            }
            catch (DllNotFoundException ex)
            {
                UnavailableReason = ex.Message;
                return -1;
            }
        }
    }

    private static ControllerSnapshot Map(XInputState state)
    {
        var pad = state.Gamepad;
        var buttons = new HashSet<GamepadButton>();
        Add(buttons, pad.Buttons, 0x0001, GamepadButton.DPadUp);
        Add(buttons, pad.Buttons, 0x0002, GamepadButton.DPadDown);
        Add(buttons, pad.Buttons, 0x0004, GamepadButton.DPadLeft);
        Add(buttons, pad.Buttons, 0x0008, GamepadButton.DPadRight);
        Add(buttons, pad.Buttons, 0x0010, GamepadButton.Start);
        Add(buttons, pad.Buttons, 0x0020, GamepadButton.Back);
        Add(buttons, pad.Buttons, 0x0040, GamepadButton.LeftThumb);
        Add(buttons, pad.Buttons, 0x0080, GamepadButton.RightThumb);
        Add(buttons, pad.Buttons, 0x0100, GamepadButton.LeftShoulder);
        Add(buttons, pad.Buttons, 0x0200, GamepadButton.RightShoulder);
        Add(buttons, pad.Buttons, 0x1000, GamepadButton.A);
        Add(buttons, pad.Buttons, 0x2000, GamepadButton.B);
        Add(buttons, pad.Buttons, 0x4000, GamepadButton.X);
        Add(buttons, pad.Buttons, 0x8000, GamepadButton.Y);
        if (pad.LeftTrigger >= TriggerThreshold)
            buttons.Add(GamepadButton.LeftTrigger);
        if (pad.RightTrigger >= TriggerThreshold)
            buttons.Add(GamepadButton.RightTrigger);

        return new ControllerSnapshot(buttons, Normalize(pad.ThumbLX), Normalize(pad.ThumbLY));
    }

    private static void Add(HashSet<GamepadButton> buttons, ushort flags, ushort mask, GamepadButton button)
    {
        if ((flags & mask) != 0)
            buttons.Add(button);
    }

    private static float Normalize(short value)
    {
        if (value >= 0)
            return value / 32767f;
        return value / 32768f;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct XInputGamepad
    {
        public ushort Buttons;
        public byte LeftTrigger;
        public byte RightTrigger;
        public short ThumbLX;
        public short ThumbLY;
        public short ThumbRX;
        public short ThumbRY;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct XInputState
    {
        public uint PacketNumber;
        public XInputGamepad Gamepad;
    }

    private static class Native
    {
        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
        internal static extern int XInputGetState14(int userIndex, out XInputState state);

        [DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")]
        internal static extern int XInputGetState910(int userIndex, out XInputState state);
    }
}
