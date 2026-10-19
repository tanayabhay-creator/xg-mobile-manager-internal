using AllyConsole.Core.Operations;

namespace AllyConsole.Core.Power;

public enum PowerIntent
{
    Restart,
    Shutdown
}

public static class PowerActions
{
    public static OperationResult Build(PowerIntent intent, bool confirmed)
    {
        if (!confirmed)
        {
            return OperationResult.Fail(
                "confirmation-required",
                "Restart and shutdown need confirmation.");
        }

        return intent switch
        {
            PowerIntent.Restart => OperationResult.Ok("Restart Windows.", "shutdown.exe", "/r /t 0"),
            PowerIntent.Shutdown => OperationResult.Ok("Shut down Windows.", "shutdown.exe", "/s /t 0"),
            _ => OperationResult.Fail("power-unknown", "That power action is not supported.")
        };
    }
}
