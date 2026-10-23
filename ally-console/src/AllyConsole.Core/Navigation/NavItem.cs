namespace AllyConsole.Core.Navigation;

public enum NavDirection
{
    Up,
    Down,
    Left,
    Right
}

public enum NavItemKind
{
    Action,
    Slider,
    Toggle,
    Status
}

public sealed record NavItem
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public string Detail { get; init; } = "";

    public NavItemKind Kind { get; init; } = NavItemKind.Action;

    public bool Enabled { get; init; } = true;

    public string? DisabledReason { get; init; }

    public int Value { get; init; }

    public int Min { get; init; }

    public int Max { get; init; } = 100;

    public int Step { get; init; } = 1;

    public static NavItem Action(string id, string title, string detail = "")
        => new()
        {
            Id = id,
            Title = title,
            Detail = detail,
            Kind = NavItemKind.Action
        };

    public static NavItem Status(string id, string title, string detail)
        => new()
        {
            Id = id,
            Title = title,
            Detail = detail,
            Kind = NavItemKind.Status
        };

    public static NavItem Slider(
        string id,
        string title,
        string detail,
        int value,
        int min,
        int max,
        int step,
        bool enabled,
        string? reason)
        => new()
        {
            Id = id,
            Title = title,
            Detail = detail,
            Kind = NavItemKind.Slider,
            Value = value,
            Min = min,
            Max = max,
            Step = step,
            Enabled = enabled,
            DisabledReason = reason
        };

    public static NavItem Toggle(string id, string title, string detail, bool on, bool enabled = true, string? reason = null)
        => new()
        {
            Id = id,
            Title = title,
            Detail = detail,
            Kind = NavItemKind.Toggle,
            Value = on ? 1 : 0,
            Enabled = enabled,
            DisabledReason = reason
        };
}
