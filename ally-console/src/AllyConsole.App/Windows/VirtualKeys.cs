using AllyConsole.Core.Input;

namespace AllyConsole.App.Windows;

internal static class VirtualKeys
{
    public static bool TryMap(KeyboardShortcut shortcut, out uint modifiers, out uint virtualKey)
    {
        modifiers = 0;
        if (shortcut.Alt)
            modifiers |= 0x0001;
        if (shortcut.Control)
            modifiers |= 0x0002;
        if (shortcut.Shift)
            modifiers |= 0x0004;
        if (shortcut.Win)
            modifiers |= 0x0008;

        virtualKey = Map(shortcut.Key);
        return virtualKey != 0;
    }

    private static uint Map(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return 0;

        var name = key.Trim();
        if (name.Length == 1)
        {
            var character = char.ToUpperInvariant(name[0]);
            if (character is >= 'A' and <= 'Z' or >= '0' and <= '9')
                return character;
        }

        if (name.Length is >= 2 and <= 3
            && (name[0] == 'F' || name[0] == 'f')
            && int.TryParse(name[1..], out var function)
            && function is >= 1 and <= 24)
        {
            return (uint)(0x70 + function - 1);
        }

        return name.ToUpperInvariant() switch
        {
            "DELETE" => 0x2E,
            "INSERT" => 0x2D,
            "HOME" => 0x24,
            "END" => 0x23,
            "PAGEUP" => 0x21,
            "PAGEDOWN" => 0x22,
            "SPACE" => 0x20,
            "TAB" => 0x09,
            "ESCAPE" => 0x1B,
            "LEFT" => 0x25,
            "UP" => 0x26,
            "RIGHT" => 0x27,
            "DOWN" => 0x28,
            _ => 0
        };
    }
}
