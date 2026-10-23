using System.Runtime.InteropServices;
using AllyConsole.Core.Input;
using AllyConsole.Core.Operations;

namespace AllyConsole.App.Windows;

internal sealed class HotkeyRegistrar : IDisposable
{
    public const int HotkeyId = 0xA11C;
    private const uint ModNorepeat = 0x4000;

    private IntPtr _hwnd;
    private KeyboardShortcut? _current;
    private bool _registered;

    public OperationResult TryReplace(IntPtr hwnd, KeyboardShortcut shortcut)
    {
        if (hwnd == IntPtr.Zero)
        {
            return OperationResult.Fail(
                "hotkey-window",
                "The companion window is not ready, so the keyboard shortcut was not registered.");
        }

        if (!VirtualKeys.TryMap(shortcut, out var modifiers, out var virtualKey))
        {
            return OperationResult.Fail(
                "hotkey-key",
                "That key cannot be registered with RegisterHotKey.");
        }

        var previous = _current?.Clone();
        Unregister();
        _hwnd = hwnd;
        if (!RegisterHotKey(hwnd, HotkeyId, modifiers | ModNorepeat, virtualKey))
        {
            var error = Marshal.GetLastWin32Error();
            var restored = false;
            if (previous is not null && VirtualKeys.TryMap(previous, out var oldModifiers, out var oldKey))
            {
                restored = RegisterHotKey(hwnd, HotkeyId, oldModifiers | ModNorepeat, oldKey);
                if (restored)
                {
                    _current = previous;
                    _registered = true;
                }
            }

            return OperationResult.Fail(
                "hotkey-register-failed",
                "RegisterHotKey failed (Win32 " + error + "). Another program may already own this shortcut, or Windows reserved it. "
                + (restored
                    ? "The previous shortcut is still registered."
                    : "No keyboard shortcut is registered."));
        }

        _current = shortcut.Clone();
        _registered = true;
        return OperationResult.Ok("Keyboard shortcut registered.");
    }

    public void Dispose() => Unregister();

    private void Unregister()
    {
        if (_hwnd != IntPtr.Zero && _registered)
            UnregisterHotKey(_hwnd, HotkeyId);
        _registered = false;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
