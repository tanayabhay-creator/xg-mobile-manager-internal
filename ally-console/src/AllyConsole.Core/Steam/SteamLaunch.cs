using AllyConsole.Core.Operations;

namespace AllyConsole.Core.Steam;

public static class SteamLaunch
{
    public const string DefaultUri = "steam://open/bigpicture";

    public static OperationResult ValidateUri(string? uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            return OperationResult.Fail(
                "steam-uri-missing",
                "The Steam launch URI is empty. Set steamLaunchUri in settings.");
        }

        var trimmed = uri.Trim();
        if (!trimmed.StartsWith("steam://", StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult.Fail(
                "steam-uri-invalid",
                "The Steam launch URI must start with steam://.");
        }

        return OperationResult.Ok("Steam URI accepted.", trimmed);
    }

    /// <summary>
    /// Builds the launch command. A missing Steam install or a bad URI returns a failed result and no command.
    /// </summary>
    public static OperationResult Build(string? uri, bool steamAvailable)
    {
        var shape = ValidateUri(uri);
        if (!shape.Succeeded)
            return shape;

        if (!steamAvailable)
        {
            return OperationResult.Fail(
                "steam-missing",
                "Steam was not found. Install the Steam client, then try again. Ally Console was not closed.");
        }

        return OperationResult.Ok("Open Steam.", shape.Command);
    }
}
