using AllyConsole.Core.Input;

namespace AllyConsole.Core.Tests;

public class ShortcutRulesTests
{
    [Fact]
    public void Empty_controller_chord_is_rejected()
    {
        var decision = ShortcutRules.ValidateController(Array.Empty<GamepadButton>(), Array.Empty<IReadOnlyList<GamepadButton>>());

        Assert.False(decision.Accepted);
        Assert.Equal("empty-chord", decision.Code);
    }

    [Fact]
    public void Duplicate_button_is_rejected()
    {
        var decision = ShortcutRules.ValidateController(
            new[] { GamepadButton.LeftShoulder, GamepadButton.LeftShoulder },
            Array.Empty<IReadOnlyList<GamepadButton>>());

        Assert.False(decision.Accepted);
        Assert.Equal("duplicate-button", decision.Code);
    }

    [Fact]
    public void Duplicate_chord_is_rejected()
    {
        IReadOnlyList<GamepadButton> existing = new[] { GamepadButton.LeftShoulder, GamepadButton.Back };
        var decision = ShortcutRules.ValidateController(
            new[] { GamepadButton.Back, GamepadButton.LeftShoulder },
            new[] { existing });

        Assert.False(decision.Accepted);
        Assert.Equal("duplicate-chord", decision.Code);
    }

    [Fact]
    public void Guide_is_a_reserved_clash()
    {
        var decision = ShortcutRules.ValidateController(
            new[] { GamepadButton.Guide, GamepadButton.LeftShoulder },
            Array.Empty<IReadOnlyList<GamepadButton>>());

        Assert.False(decision.Accepted);
        Assert.Equal("reserved-clash", decision.Code);
        Assert.Contains("Guide", decision.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Default_controller_chord_is_accepted()
    {
        var decision = ShortcutRules.ValidateController(
            ControllerChord.Default.Buttons,
            Array.Empty<IReadOnlyList<GamepadButton>>());

        Assert.True(decision.Accepted);
        Assert.Equal(
            new[] { GamepadButton.LeftShoulder, GamepadButton.RightShoulder, GamepadButton.Back },
            ControllerChord.Default.Buttons);
    }

    [Fact]
    public void Empty_keyboard_shortcut_is_rejected()
    {
        var decision = ShortcutRules.ValidateKeyboard(new KeyboardShortcut { Key = "  " }, Array.Empty<string>());

        Assert.False(decision.Accepted);
        Assert.Equal("empty-chord", decision.Code);
    }

    [Fact]
    public void Bare_key_is_rejected_so_register_hotkey_cannot_swallow_typing()
    {
        var decision = ShortcutRules.ValidateKeyboard(
            new KeyboardShortcut { Control = false, Alt = false, Shift = false, Win = false, Key = "A" },
            Array.Empty<string>());

        Assert.False(decision.Accepted);
        Assert.Equal("modifier-required", decision.Code);
    }

    [Theory]
    [InlineData(true, true, false, false, "Delete")]
    [InlineData(false, false, false, true, "L")]
    [InlineData(false, false, false, true, "Tab")]
    public void Reserved_windows_shortcuts_are_rejected(bool control, bool alt, bool shift, bool win, string key)
    {
        var decision = ShortcutRules.ValidateKeyboard(
            new KeyboardShortcut { Control = control, Alt = alt, Shift = shift, Win = win, Key = key },
            Array.Empty<string>());

        Assert.False(decision.Accepted);
        Assert.Equal("reserved-clash", decision.Code);
    }

    [Fact]
    public void Duplicate_keyboard_shortcut_is_rejected()
    {
        var decision = ShortcutRules.ValidateKeyboard(KeyboardShortcut.Default(), new[] { "Ctrl+Alt+Shift+F1" });

        Assert.False(decision.Accepted);
        Assert.Equal("duplicate-chord", decision.Code);
    }

    [Fact]
    public void Default_keyboard_shortcut_is_accepted()
    {
        var decision = ShortcutRules.ValidateKeyboard(KeyboardShortcut.Default(), Array.Empty<string>());

        Assert.True(decision.Accepted);
        Assert.Equal("Ctrl+Alt+Shift+F1", KeyboardShortcut.Default().Canonical());
    }
}
