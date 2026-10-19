namespace AllyConsole.Core.Input;

public sealed class KeyboardShortcut
{
    public bool Control { get; set; } = true;

    public bool Alt { get; set; } = true;

    public bool Shift { get; set; } = true;

    public bool Win { get; set; }

    public string Key { get; set; } = "F1";

    public static KeyboardShortcut Default() => new();

    public KeyboardShortcut Clone() => new()
    {
        Control = Control,
        Alt = Alt,
        Shift = Shift,
        Win = Win,
        Key = Key
    };

    public string Canonical()
    {
        var parts = new List<string>(5);
        if (Control)
            parts.Add("Ctrl");
        if (Alt)
            parts.Add("Alt");
        if (Shift)
            parts.Add("Shift");
        if (Win)
            parts.Add("Win");
        parts.Add(string.IsNullOrWhiteSpace(Key) ? "" : Key.Trim());
        return string.Join("+", parts);
    }
}
