namespace AllyConsole.Core.Navigation;

/// <summary>
/// Tracks whether the quick panel is open and whether the host should restore the previous window.
/// The host performs the actual SetForegroundWindow call. Exclusive fullscreen and elevated
/// windows may ignore it. This session never injects input into another process.
/// </summary>
public sealed class PanelSession
{
    public bool IsOpen { get; private set; }

    public bool RestoreFocusRequested { get; private set; }

    public void Open()
    {
        IsOpen = true;
        RestoreFocusRequested = false;
    }

    public void Close()
    {
        if (!IsOpen)
            return;
        IsOpen = false;
        RestoreFocusRequested = true;
    }

    public void AcknowledgeFocusRestore() => RestoreFocusRequested = false;
}
