namespace AllyConsole.Core.Logging;

public readonly record struct FailureNotice(string EventName, string Message);

/// <summary>
/// Lets a startup, shortcut, or unsupported-integration failure through once.
/// The same event and message stays suppressed until the message changes or
/// <see cref="Recovered"/> marks that event clear. This is the guard for poll
/// and status loops. It is not a sampler and it does not drop a new message.
/// </summary>
public sealed class FailureLogGate
{
    private readonly object _gate = new();
    private readonly Dictionary<string, string> _open = new(StringComparer.Ordinal);

    public bool ShouldWrite(string eventName, string message)
    {
        lock (_gate)
            return Admit(eventName, message);
    }

    public void Recovered(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName))
            throw new ArgumentException("Event name is required.", nameof(eventName));

        lock (_gate)
            _open.Remove(eventName);
    }

    public IReadOnlyList<FailureNotice> SelectNew(
        IReadOnlyList<FailureNotice> failures,
        IReadOnlyCollection<string> watchedEvents)
    {
        lock (_gate)
        {
            var present = new HashSet<string>(StringComparer.Ordinal);
            var admitted = new List<FailureNotice>();
            foreach (var failure in failures)
            {
                present.Add(failure.EventName);
                if (Admit(failure.EventName, failure.Message))
                    admitted.Add(failure);
            }

            foreach (var watched in watchedEvents)
            {
                if (!present.Contains(watched))
                    _open.Remove(watched);
            }

            return admitted;
        }
    }

    private bool Admit(string eventName, string message)
    {
        if (string.IsNullOrWhiteSpace(eventName))
            throw new ArgumentException("Event name is required.", nameof(eventName));

        message ??= "";
        if (_open.TryGetValue(eventName, out var previous) && string.Equals(previous, message, StringComparison.Ordinal))
            return false;

        _open[eventName] = message;
        return true;
    }
}
