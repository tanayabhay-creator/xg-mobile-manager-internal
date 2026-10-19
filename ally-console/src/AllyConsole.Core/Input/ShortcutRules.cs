namespace AllyConsole.Core.Input;

public sealed record ShortcutDecision(bool Accepted, string Code, string Message)
{
    public static ShortcutDecision Accept() => new(true, "ok", "");

    public static ShortcutDecision Reject(string code, string message) => new(false, code, message);
}

public static class ShortcutRules
{
    private static readonly HashSet<string> KnownKeys = BuildKnownKeys();

    public static ShortcutDecision ValidateController(
        IReadOnlyList<GamepadButton> buttons,
        IReadOnlyCollection<IReadOnlyList<GamepadButton>> otherChords)
    {
        if (buttons.Count == 0)
            return ShortcutDecision.Reject("empty-chord", "The controller shortcut is empty. Choose at least one button.");

        if (buttons.Count != buttons.Distinct().Count())
            return ShortcutDecision.Reject("duplicate-button", "That controller shortcut lists the same button more than once.");

        if (buttons.Contains(GamepadButton.Guide))
        {
            return ShortcutDecision.Reject(
                "reserved-clash",
                "Guide is reserved. The documented XInput API does not report it reliably, and Steam or Windows often takes it.");
        }

        var ordered = buttons.OrderBy(button => button).ToArray();
        foreach (var other in otherChords)
        {
            if (other.Count == 0)
                continue;
            if (other.OrderBy(button => button).SequenceEqual(ordered))
            {
                return ShortcutDecision.Reject(
                    "duplicate-chord",
                    "That controller shortcut is already used by another action.");
            }
        }

        return ShortcutDecision.Accept();
    }

    public static ShortcutDecision ParseControllerButtons(
        IReadOnlyList<string>? names,
        out IReadOnlyList<GamepadButton> buttons)
    {
        if (names is null || names.Count == 0)
        {
            buttons = Array.Empty<GamepadButton>();
            return ShortcutDecision.Reject("empty-chord", "The controller shortcut is empty. Choose at least one button.");
        }

        var parsed = new List<GamepadButton>(names.Count);
        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name) || !Enum.TryParse(name.Trim(), ignoreCase: true, out GamepadButton button))
            {
                buttons = Array.Empty<GamepadButton>();
                return ShortcutDecision.Reject("unknown-button", $"Unknown controller button \"{name}\".");
            }

            parsed.Add(button);
        }

        buttons = parsed;
        return ValidateController(parsed, Array.Empty<IReadOnlyList<GamepadButton>>());
    }

    public static ShortcutDecision ValidateKeyboard(
        KeyboardShortcut? shortcut,
        IReadOnlyCollection<string> otherCanonical)
    {
        if (shortcut is null || string.IsNullOrWhiteSpace(shortcut.Key))
            return ShortcutDecision.Reject("empty-chord", "The keyboard shortcut needs a key.");

        var key = CanonicalKey(shortcut.Key);
        if (key is null)
            return ShortcutDecision.Reject("unknown-key", $"The key \"{shortcut.Key.Trim()}\" cannot be registered.");

        shortcut.Key = key;
        if (!shortcut.Control && !shortcut.Alt && !shortcut.Shift && !shortcut.Win)
        {
            return ShortcutDecision.Reject(
                "modifier-required",
                "The keyboard shortcut needs Ctrl, Alt, Shift, or Win. A bare key would be captured everywhere.");
        }

        if (shortcut.Win && key.Equals("L", StringComparison.OrdinalIgnoreCase))
            return ShortcutDecision.Reject("reserved-clash", "Win+L is reserved by Windows for locking the screen.");

        if (shortcut.Win && key.Equals("Tab", StringComparison.OrdinalIgnoreCase))
            return ShortcutDecision.Reject("reserved-clash", "Win+Tab is reserved by Windows for task view.");

        if (shortcut.Control && shortcut.Alt && key.Equals("Delete", StringComparison.OrdinalIgnoreCase))
            return ShortcutDecision.Reject("reserved-clash", "Ctrl+Alt+Delete is reserved by Windows.");

        var canonical = shortcut.Canonical();
        foreach (var other in otherCanonical)
        {
            if (string.Equals(other, canonical, StringComparison.OrdinalIgnoreCase))
            {
                return ShortcutDecision.Reject(
                    "duplicate-chord",
                    "That keyboard shortcut is already used by another action.");
            }
        }

        return ShortcutDecision.Accept();
    }

    public static string? CanonicalKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;
        var trimmed = key.Trim();
        foreach (var known in KnownKeys)
        {
            if (known.Equals(trimmed, StringComparison.OrdinalIgnoreCase))
                return known;
        }

        return null;
    }

    private static HashSet<string> BuildKnownKeys()
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var letter = 'A'; letter <= 'Z'; letter++)
            keys.Add(letter.ToString());
        for (var digit = 0; digit <= 9; digit++)
            keys.Add(digit.ToString());
        for (var function = 1; function <= 24; function++)
            keys.Add("F" + function);
        foreach (var named in new[]
                 {
                     "Delete", "Insert", "Home", "End", "PageUp", "PageDown",
                     "Space", "Tab", "Escape", "Left", "Right", "Up", "Down"
                 })
        {
            keys.Add(named);
        }

        return keys;
    }
}
