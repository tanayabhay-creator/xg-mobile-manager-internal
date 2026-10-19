using AllyConsole.Core.Desktop;
using AllyConsole.Core.Network;
using AllyConsole.Core.Operations;
using AllyConsole.Core.Power;
using AllyConsole.Core.Steam;

namespace AllyConsole.Core.Tests;

public class SteamAndOperationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_steam_uri_fails_and_builds_no_command(string? uri)
    {
        var result = SteamLaunch.Build(uri, steamAvailable: true);

        Assert.False(result.Succeeded);
        Assert.Equal("steam-uri-missing", result.Code);
        Assert.Null(result.Command);
        Assert.Null(result.Arguments);
    }

    [Fact]
    public void Non_steam_uri_fails()
    {
        var result = SteamLaunch.Build("https://store.steampowered.com", steamAvailable: true);

        Assert.False(result.Succeeded);
        Assert.Equal("steam-uri-invalid", result.Code);
        Assert.Null(result.Command);
    }

    [Fact]
    public void Missing_steam_install_fails_and_does_not_throw()
    {
        var result = SteamLaunch.Build(SteamLaunch.DefaultUri, steamAvailable: false);

        Assert.False(result.Succeeded);
        Assert.Equal("steam-missing", result.Code);
        Assert.Null(result.Command);
        Assert.Contains("not found", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Available_steam_builds_the_big_picture_command()
    {
        var result = SteamLaunch.Build("  STEAM://open/bigpicture  ", steamAvailable: true);

        Assert.True(result.Succeeded);
        Assert.Equal("STEAM://open/bigpicture", result.Command);
        Assert.Equal("ok", result.Code);
    }

    [Fact]
    public void Failed_operation_has_no_command()
    {
        var result = OperationResult.Fail("confirmation-required", "Restart and shutdown need confirmation.");

        Assert.False(result.Succeeded);
        Assert.Equal("confirmation-required", result.Code);
        Assert.Null(result.Command);
        Assert.Null(result.Arguments);
    }

    [Fact]
    public void Power_actions_require_confirmation_and_use_shutdown_exe()
    {
        var unconfirmed = PowerActions.Build(PowerIntent.Shutdown, confirmed: false);
        var shutdown = PowerActions.Build(PowerIntent.Shutdown, confirmed: true);
        var restart = PowerActions.Build(PowerIntent.Restart, confirmed: true);

        Assert.False(unconfirmed.Succeeded);
        Assert.Null(unconfirmed.Command);
        Assert.Equal("shutdown.exe", shutdown.Command);
        Assert.Equal("/s /t 0", shutdown.Arguments);
        Assert.Equal("shutdown.exe", restart.Command);
        Assert.Equal("/r /t 0", restart.Arguments);
        Assert.DoesNotContain("/f", shutdown.Arguments, StringComparison.Ordinal);
        Assert.DoesNotContain("/f", restart.Arguments, StringComparison.Ordinal);
    }

    [Fact]
    public void Desktop_and_network_are_labelled_shortcuts()
    {
        var desktop = DesktopMode.BuildReveal();
        var network = NetworkSettings.Build();

        Assert.True(desktop.Succeeded);
        Assert.Equal("Shell.Application", desktop.Command);
        Assert.Equal("ToggleDesktop", desktop.Arguments);
        Assert.Contains("keeps running", desktop.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("ms-settings:network", network.Command);
    }
}
