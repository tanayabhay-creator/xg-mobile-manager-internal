namespace AllyConsole.Core.Navigation;

public sealed class FocusModel
{
    private readonly List<NavItem> _items = new();

    public IReadOnlyList<NavItem> Items => _items;

    public int FocusIndex { get; private set; } = -1;

    public NavItem? Focused => FocusIndex >= 0 && FocusIndex < _items.Count ? _items[FocusIndex] : null;

    public void SetItems(IReadOnlyList<NavItem> items, string? preferredId = null)
    {
        var keep = preferredId ?? Focused?.Id;
        _items.Clear();
        _items.AddRange(items);
        if (_items.Count == 0)
        {
            FocusIndex = -1;
            return;
        }

        var index = keep is null ? -1 : _items.FindIndex(item => item.Id == keep);
        FocusIndex = index >= 0 ? index : 0;
    }

    public void FocusId(string id)
    {
        var index = _items.FindIndex(item => item.Id == id);
        if (index >= 0)
            FocusIndex = index;
    }

    public void Move(NavDirection direction)
    {
        if (_items.Count == 0 || FocusIndex < 0)
            return;

        var current = _items[FocusIndex];
        if (current.Kind == NavItemKind.Slider
            && direction is NavDirection.Left or NavDirection.Right)
        {
            if (!current.Enabled)
                return;

            var delta = direction == NavDirection.Right ? current.Step : -current.Step;
            var next = Math.Clamp(current.Value + delta, current.Min, current.Max);
            _items[FocusIndex] = current with { Value = next, Detail = next + "%" };
            return;
        }

        if (direction is NavDirection.Left or NavDirection.Right)
            return;

        var step = direction == NavDirection.Down ? 1 : -1;
        FocusIndex = Math.Clamp(FocusIndex + step, 0, _items.Count - 1);
    }

    public bool TrySetSlider(string id, int value)
    {
        var index = _items.FindIndex(item => item.Id == id);
        if (index < 0)
            return false;
        var item = _items[index];
        if (item.Kind != NavItemKind.Slider || !item.Enabled)
            return false;
        var clamped = Math.Clamp(value, item.Min, item.Max);
        _items[index] = item with { Value = clamped, Detail = clamped + "%" };
        FocusIndex = index;
        return true;
    }

    /// <summary>
    /// Activates the focused row. Toggles flip. Disabled rows and status rows do not activate.
    /// </summary>
    public bool TryActivate(out NavItem? item)
    {
        item = Focused;
        if (item is null || !item.Enabled || item.Kind == NavItemKind.Status || item.Kind == NavItemKind.Slider)
        {
            item = null;
            return false;
        }

        if (item.Kind == NavItemKind.Toggle)
        {
            var updated = item with { Value = item.Value == 0 ? 1 : 0 };
            _items[FocusIndex] = updated;
            item = updated;
        }

        return true;
    }
}
