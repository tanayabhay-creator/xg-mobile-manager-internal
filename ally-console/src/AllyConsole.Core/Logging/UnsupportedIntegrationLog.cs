using AllyConsole.Core.Status;

namespace AllyConsole.Core.Logging;

/// <summary>
/// Failure lines for a probed status. Development samples are not failures.
/// Callers pass the list through <see cref="FailureLogGate"/> so a 2-second
/// status refresh does not write the same unsupported brightness line again.
/// </summary>
public static class UnsupportedIntegrationLog
{
    public static readonly string[] Events =
    {
        "integration.brightness",
        "integration.volume",
        "integration.battery",
        "integration.gpu",
        "integration.display",
        "integration.os"
    };

    public static IReadOnlyList<FailureNotice> Failures(SystemStatus status)
    {
        if (status.DevelopmentMode)
            return Array.Empty<FailureNotice>();

        var notices = new List<FailureNotice>();
        if (!status.Brightness.Supported)
        {
            notices.Add(new FailureNotice(
                "integration.brightness",
                Reason(status.Brightness.UnavailableReason, "Brightness is unavailable.")));
        }

        if (!status.Volume.Available)
        {
            notices.Add(new FailureNotice(
                "integration.volume",
                Reason(status.Volume.Reason, "Volume is unavailable.")));
        }

        if (!status.Battery.Available)
        {
            notices.Add(new FailureNotice(
                "integration.battery",
                Reason(status.Battery.Reason, "Battery status is unavailable.")));
        }

        if (status.Adapters.Count == 0)
        {
            notices.Add(new FailureNotice(
                "integration.gpu",
                Reason(status.AdaptersUnavailableReason, "No GPU adapter name was returned.")));
        }

        if (status.Displays.Count == 0)
        {
            notices.Add(new FailureNotice(
                "integration.display",
                Reason(status.DisplaysUnavailableReason, "No display was returned.")));
        }

        if (!status.OperatingSystem.Available)
        {
            notices.Add(new FailureNotice(
                "integration.os",
                Reason(status.OperatingSystem.Reason, "Operating system description is unavailable.")));
        }

        return notices;
    }

    private static string Reason(string? reason, string fallback)
        => string.IsNullOrWhiteSpace(reason) ? fallback : reason.Trim();
}
