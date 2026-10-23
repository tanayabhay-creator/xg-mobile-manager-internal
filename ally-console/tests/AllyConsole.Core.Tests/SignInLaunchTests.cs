using AllyConsole.Core.Startup;

namespace AllyConsole.Core.Tests;

public class SignInLaunchTests
{
    [Fact]
    public void Default_plan_is_off()
    {
        var plan = SignInLaunchPlan.Create(enabled: false, targetPath: null);

        Assert.False(plan.Enabled);
        Assert.Equal("AllyConsole.lnk", plan.ShortcutFileName);
    }

    [Fact]
    public void Apply_writes_and_undo_removes_only_inside_the_given_folder()
    {
        using var folder = new TempFolder();
        var store = new DescriptorStartupShortcutStore();
        var realStartup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        var leakedBefore = string.IsNullOrEmpty(realStartup)
            ? null
            : Path.Combine(realStartup, SignInLaunchPlan.DefaultFileName);
        var leakedExisted = leakedBefore is not null && File.Exists(leakedBefore);

        var enabled = SignInLaunchPlan.Create(true, Path.Combine(folder.Path, "AllyConsole.exe"));
        var written = SignInLaunch.Apply(folder.Path, enabled, store);

        Assert.True(written.Succeeded);
        var shortcut = Path.Combine(folder.Path, "AllyConsole.lnk");
        Assert.True(File.Exists(shortcut));
        Assert.Equal(enabled.TargetPath, store.ReadTarget(folder.Path, "AllyConsole.lnk"));
        Assert.Contains("AllyConsole.exe", File.ReadAllText(shortcut), StringComparison.Ordinal);
        Assert.StartsWith(folder.Path, shortcut, StringComparison.Ordinal);

        var disabled = SignInLaunch.Apply(folder.Path, SignInLaunchPlan.Create(false, enabled.TargetPath), store);

        Assert.True(disabled.Succeeded);
        Assert.False(File.Exists(shortcut));
        if (leakedBefore is not null && !leakedExisted)
            Assert.False(File.Exists(leakedBefore));
    }

    [Fact]
    public void Undo_does_not_require_an_executable_path()
    {
        using var folder = new TempFolder();
        var store = new DescriptorStartupShortcutStore();
        SignInLaunch.Apply(folder.Path, SignInLaunchPlan.Create(true, Path.Combine(folder.Path, "AllyConsole.exe")), store);

        var result = SignInLaunch.Apply(folder.Path, SignInLaunchPlan.Create(false, targetPath: ""), store);

        Assert.True(result.Succeeded);
        Assert.False(store.Exists(folder.Path, SignInLaunchPlan.DefaultFileName));
    }

    [Fact]
    public void Missing_startup_folder_or_target_fails_without_throwing()
    {
        var store = new DescriptorStartupShortcutStore();

        var missingFolder = SignInLaunch.Apply("  ", SignInLaunchPlan.Create(true, "AllyConsole.exe"), store);
        var missingTarget = SignInLaunch.Apply(Path.GetTempPath(), SignInLaunchPlan.Create(true, " "), store);

        Assert.False(missingFolder.Succeeded);
        Assert.Equal("startup-folder-missing", missingFolder.Code);
        Assert.Null(missingFolder.Command);
        Assert.False(missingTarget.Succeeded);
        Assert.Equal("startup-target-missing", missingTarget.Code);
        Assert.Null(missingTarget.Command);
    }
}
