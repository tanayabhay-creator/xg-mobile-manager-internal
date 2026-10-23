using AllyConsole.Core.Operations;

namespace AllyConsole.Core.Desktop;

public static class DesktopMode
{
    public static OperationResult BuildReveal()
        => OperationResult.Ok(
            "Show the Windows desktop. Ally Console keeps running.",
            "Shell.Application",
            "ToggleDesktop");
}
