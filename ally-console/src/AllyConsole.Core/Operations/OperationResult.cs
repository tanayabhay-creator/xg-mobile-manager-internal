namespace AllyConsole.Core.Operations;

/// <summary>
/// Outcome of a command that may be shown to the user. Failed results do not carry a command.
/// </summary>
public sealed record OperationResult(
    bool Succeeded,
    string Code,
    string Message,
    string? Command = null,
    string? Arguments = null)
{
    public static OperationResult Ok(string message, string? command = null, string? arguments = null)
        => new(true, "ok", message, command, arguments);

    public static OperationResult Fail(string code, string message)
        => new(false, code, message, null, null);
}
