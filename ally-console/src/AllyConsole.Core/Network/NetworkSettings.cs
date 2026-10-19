using AllyConsole.Core.Operations;

namespace AllyConsole.Core.Network;

public static class NetworkSettings
{
    public static OperationResult Build()
        => OperationResult.Ok("Open Windows network settings.", "ms-settings:network");
}
