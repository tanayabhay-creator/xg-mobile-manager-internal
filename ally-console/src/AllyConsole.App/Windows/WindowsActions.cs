using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using AllyConsole.Core.Desktop;
using AllyConsole.Core.Input;
using AllyConsole.Core.Network;
using AllyConsole.Core.Operations;
using AllyConsole.Core.Power;
using AllyConsole.Core.Startup;
using AllyConsole.Core.Steam;
using Microsoft.Win32;

namespace AllyConsole.App.Windows;

internal sealed class WindowsActions
{
    private readonly ShellLinkStartupShortcutStore _shortcuts = new();
    private readonly HotkeyRegistrar _hotkeys;

    public WindowsActions(HotkeyRegistrar hotkeys)
    {
        _hotkeys = hotkeys;
    }

    public static bool SteamIsAvailable()
    {
        return RegistryPathExists(Registry.CurrentUser, @"Software\Valve\Steam", "SteamPath")
            || RegistryPathExists(Registry.LocalMachine, @"Software\WOW6432Node\Valve\Steam", "InstallPath")
            || RegistryPathExists(Registry.LocalMachine, @"Software\Valve\Steam", "InstallPath");
    }

    public OperationResult OpenSteam(string? uri)
    {
        var built = SteamLaunch.Build(uri, SteamIsAvailable());
        if (!built.Succeeded || built.Command is null)
            return built;

        try
        {
            Process.Start(new ProcessStartInfo(built.Command) { UseShellExecute = true });
            return built;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or FileNotFoundException)
        {
            return OperationResult.Fail("steam-launch-failed", "Could not open Steam. " + ex.Message);
        }
    }

    public OperationResult OpenNetwork()
    {
        var built = NetworkSettings.Build();
        try
        {
            Process.Start(new ProcessStartInfo(built.Command!) { UseShellExecute = true });
            return built;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            return OperationResult.Fail("network-failed", "Could not open Windows network settings. " + ex.Message);
        }
    }

    public OperationResult ShowDesktop()
    {
        object? shell = null;
        try
        {
            var type = Type.GetTypeFromProgID("Shell.Application");
            if (type is null)
                return OperationResult.Fail("desktop-unavailable", "Shell.Application is unavailable, so the desktop was not shown.");
            shell = Activator.CreateInstance(type);
            type.InvokeMember("ToggleDesktop", BindingFlags.InvokeMethod, null, shell, null);
            return DesktopMode.BuildReveal();
        }
        catch (Exception ex)
        {
            return OperationResult.Fail("desktop-failed", "Could not show the desktop. Ally Console is still running. " + ex.Message);
        }
        finally
        {
            if (shell is not null && Marshal.IsComObject(shell))
                Marshal.FinalReleaseComObject(shell);
        }
    }

    public OperationResult Power(PowerIntent intent)
    {
        var built = PowerActions.Build(intent, confirmed: true);
        if (!built.Succeeded || built.Command is null)
            return built;

        var exe = Path.Combine(Environment.SystemDirectory, "shutdown.exe");
        try
        {
            Process.Start(new ProcessStartInfo(exe, built.Arguments ?? "")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            });
            return built;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or FileNotFoundException)
        {
            return OperationResult.Fail("power-failed", "Windows did not start the power action. " + ex.Message);
        }
    }

    public OperationResult ApplyStartup(bool enabled, string? executablePath)
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        try
        {
            return SignInLaunch.Apply(folder, SignInLaunchPlan.Create(enabled, executablePath), _shortcuts);
        }
        catch (Exception ex) when (ex is COMException or IOException or UnauthorizedAccessException)
        {
            return OperationResult.Fail("startup-failed", "Could not update the Startup folder shortcut. " + ex.Message);
        }
    }

    public OperationResult ReplaceHotkey(IntPtr hwnd, KeyboardShortcut shortcut)
        => _hotkeys.TryReplace(hwnd, shortcut);

    public IntPtr ForegroundWindow() => GetForegroundWindow();

    public bool RestoreForeground(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
            return false;

        var foregroundThread = GetWindowThreadProcessId(hwnd, out _);
        var thisThread = GetCurrentThreadId();
        var attached = false;
        if (foregroundThread != 0 && foregroundThread != thisThread)
            attached = AttachThreadInput(thisThread, foregroundThread, true);
        try
        {
            return SetForegroundWindow(hwnd);
        }
        finally
        {
            if (attached)
                AttachThreadInput(thisThread, foregroundThread, false);
        }
    }

    private static bool RegistryPathExists(RegistryKey hive, string subKey, string valueName)
    {
        using var key = hive.OpenSubKey(subKey);
        if (key?.GetValue(valueName) is not string path || string.IsNullOrWhiteSpace(path))
            return false;
        return Directory.Exists(path) || File.Exists(path);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool attach);
}
