using AllyConsole.Core.Operations;

namespace AllyConsole.Core.Startup;

public sealed record SignInLaunchPlan(
    bool Enabled,
    string ShortcutFileName,
    string TargetPath,
    string Arguments,
    string Description)
{
    public const string DefaultFileName = "AllyConsole.lnk";

    public static SignInLaunchPlan Create(bool enabled, string? targetPath, string? arguments = null)
        => new(enabled, DefaultFileName, targetPath ?? "", arguments ?? "", "Ally Console");
}

public interface IStartupShortcutStore
{
    bool Exists(string directory, string shortcutFileName);

    string? ReadTarget(string directory, string shortcutFileName);

    void Write(string directory, SignInLaunchPlan plan);

    void Remove(string directory, string shortcutFileName);
}

/// <summary>
/// Test and planning writer. It records the shortcut target in the directory it is given.
/// The Windows app writes a real .lnk through IShellLink instead. Neither writer chooses the Startup folder itself.
/// </summary>
public sealed class DescriptorStartupShortcutStore : IStartupShortcutStore
{
    public bool Exists(string directory, string shortcutFileName)
        => File.Exists(Path.Combine(directory, shortcutFileName));

    public string? ReadTarget(string directory, string shortcutFileName)
    {
        var path = Path.Combine(directory, shortcutFileName);
        if (!File.Exists(path))
            return null;

        foreach (var line in File.ReadAllLines(path))
        {
            const string prefix = "target=";
            if (line.StartsWith(prefix, StringComparison.Ordinal))
                return line[prefix.Length..];
        }

        return null;
    }

    public void Write(string directory, SignInLaunchPlan plan)
    {
        Directory.CreateDirectory(directory);
        var body = string.Join('\n', new[]
        {
            "# ally-console-startup-plan",
            "file=" + plan.ShortcutFileName,
            "target=" + plan.TargetPath,
            "arguments=" + plan.Arguments,
            "description=" + plan.Description
        }) + "\n";
        File.WriteAllText(Path.Combine(directory, plan.ShortcutFileName), body);
    }

    public void Remove(string directory, string shortcutFileName)
    {
        var path = Path.Combine(directory, shortcutFileName);
        if (File.Exists(path))
            File.Delete(path);
    }
}

public static class SignInLaunch
{
    public static OperationResult Apply(string? startupDirectory, SignInLaunchPlan plan, IStartupShortcutStore store)
    {
        if (string.IsNullOrWhiteSpace(startupDirectory))
        {
            return OperationResult.Fail(
                "startup-folder-missing",
                "The per-user Startup folder path is missing, so no shortcut was written.");
        }

        if (plan.Enabled && string.IsNullOrWhiteSpace(plan.TargetPath))
        {
            return OperationResult.Fail(
                "startup-target-missing",
                "The companion executable path is missing, so the sign-in shortcut was not written.");
        }

        Directory.CreateDirectory(startupDirectory);
        if (!plan.Enabled)
        {
            store.Remove(startupDirectory, plan.ShortcutFileName);
            return OperationResult.Ok("Sign-in launch is off. Any Ally Console shortcut in this folder was removed.");
        }

        store.Write(startupDirectory, plan);
        return OperationResult.Ok(
            "Sign-in shortcut written for the current user.",
            Path.Combine(startupDirectory, plan.ShortcutFileName),
            plan.TargetPath);
    }
}
