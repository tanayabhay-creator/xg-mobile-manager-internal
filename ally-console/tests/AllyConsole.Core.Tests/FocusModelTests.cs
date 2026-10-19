using AllyConsole.Core.Navigation;
using AllyConsole.Core.Status;

namespace AllyConsole.Core.Tests;

public class FocusModelTests
{
    [Fact]
    public void Vertical_movement_stops_at_the_ends_and_keeps_the_focused_id_on_refresh()
    {
        var model = new FocusModel();
        model.SetItems(SampleItems());

        Assert.Equal("volume", model.Focused!.Id);
        model.Move(NavDirection.Up);
        Assert.Equal("volume", model.Focused.Id);
        model.Move(NavDirection.Down);
        Assert.Equal("mute", model.Focused.Id);

        model.SetItems(SampleItems());
        Assert.Equal("mute", model.Focused!.Id);

        while (model.FocusIndex < model.Items.Count - 1)
            model.Move(NavDirection.Down);
        var last = model.Focused!.Id;
        model.Move(NavDirection.Down);
        Assert.Equal(last, model.Focused!.Id);
    }

    [Fact]
    public void Slider_left_and_right_change_value_and_do_not_move_focus()
    {
        var model = new FocusModel();
        model.SetItems(new[]
        {
            NavItem.Slider("volume", "Volume", "40%", 40, 0, 100, 5, true, null)
        });

        model.Move(NavDirection.Right);
        Assert.Equal(0, model.FocusIndex);
        Assert.Equal(45, model.Focused!.Value);

        model.Move(NavDirection.Left);
        Assert.Equal(40, model.Focused.Value);

        for (var step = 0; step < 30; step++)
            model.Move(NavDirection.Left);
        Assert.Equal(0, model.Focused.Value);
    }

    [Fact]
    public void Disabled_slider_does_not_change_or_activate()
    {
        var model = new FocusModel();
        var reason = BrightnessReading.UnsupportedExplanation;
        model.SetItems(new[]
        {
            BrightnessReading.Unsupported().ToNavItem()
        });

        model.Move(NavDirection.Right);
        Assert.Equal(0, model.Focused!.Value);
        Assert.False(model.Focused.Enabled);
        Assert.Equal(reason, model.Focused.DisabledReason);
        Assert.False(model.TryActivate(out _));
    }

    [Fact]
    public void Toggle_activate_flips_and_status_rows_do_not()
    {
        var model = new FocusModel();
        model.SetItems(new[]
        {
            NavItem.Toggle("mute", "Mute", "Sound on", on: false),
            NavItem.Status("battery", "Battery", "No battery reported by Windows.")
        });

        Assert.True(model.TryActivate(out var mute));
        Assert.Equal(1, mute!.Value);
        model.Move(NavDirection.Down);
        Assert.False(model.TryActivate(out _));
        Assert.Equal("battery", model.Focused!.Id);
    }

    [Fact]
    public void Closing_the_panel_requests_focus_restore_once()
    {
        var session = new PanelSession();
        session.Open();
        Assert.True(session.IsOpen);
        Assert.False(session.RestoreFocusRequested);

        session.Close();
        Assert.False(session.IsOpen);
        Assert.True(session.RestoreFocusRequested);

        session.AcknowledgeFocusRestore();
        session.Close();
        Assert.False(session.RestoreFocusRequested);
    }

    [Fact]
    public void Confirm_page_starts_on_cancel()
    {
        var model = new FocusModel();
        model.SetItems(PanelCatalog.Confirm("shutdown"));

        Assert.Equal("cancel", model.Focused!.Id);
        Assert.Contains(model.Items, item => item.Id == "confirm-shutdown");
    }

    [Fact]
    public void Quick_panel_lists_the_stage_1_actions()
    {
        var items = PanelCatalog.Quick(SystemStatus.Unavailable("probe failed"));
        var ids = items.Select(item => item.Id).ToArray();

        Assert.Equal(
            new[]
            {
                "volume", "mute", "brightness", "battery", "gpu", "network", "steam", "desktop",
                "settings", "diagnostics", "restart", "shutdown", "exit"
            },
            ids);
    }

    private static IReadOnlyList<NavItem> SampleItems() => new[]
    {
        NavItem.Slider("volume", "Volume", "10%", 10, 0, 100, 5, true, null),
        NavItem.Toggle("mute", "Mute", "", false),
        NavItem.Action("steam", "Steam Big Picture")
    };
}
