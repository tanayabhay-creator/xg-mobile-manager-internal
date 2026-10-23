using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using AllyConsole.Core.Startup;

namespace AllyConsole.App.Windows;

/// <summary>
/// Writes a real per-user Startup shortcut through IShellLink. The caller supplies the folder.
/// </summary>
internal sealed class ShellLinkStartupShortcutStore : IStartupShortcutStore
{
    public bool Exists(string directory, string shortcutFileName)
        => File.Exists(Path.Combine(directory, shortcutFileName));

    public string? ReadTarget(string directory, string shortcutFileName) => null;

    public void Write(string directory, SignInLaunchPlan plan)
    {
        Directory.CreateDirectory(directory);
        var link = (IShellLinkW)new ShellLink();
        try
        {
            link.SetPath(plan.TargetPath);
            link.SetArguments(plan.Arguments);
            link.SetDescription(plan.Description);
            var workingDirectory = Path.GetDirectoryName(plan.TargetPath);
            if (!string.IsNullOrEmpty(workingDirectory))
                link.SetWorkingDirectory(workingDirectory);
            link.SetShowCmd(1);
            ((IPersistFile)link).Save(Path.Combine(directory, plan.ShortcutFileName), true);
        }
        finally
        {
            if (Marshal.IsComObject(link))
                Marshal.FinalReleaseComObject(link);
        }
    }

    public void Remove(string directory, string shortcutFileName)
    {
        var path = Path.Combine(directory, shortcutFileName);
        if (File.Exists(path))
            File.Delete(path);
    }

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder file, int maxPath, IntPtr findData, int flags);
        void GetIDList(out IntPtr idList);
        void SetIDList(IntPtr idList);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder name, int maxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string name);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder directory, int maxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string directory);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder args, int maxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string args);
        void GetHotkey(out short hotkey);
        void SetHotkey(short hotkey);
        void GetShowCmd(out int showCmd);
        void SetShowCmd(int showCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder iconPath, int maxPath, out int icon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string iconPath, int icon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string path, int reserved);
        void Resolve(IntPtr hwnd, int flags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string file);
    }

    [ComImport]
    [Guid("0000010b-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPersistFile
    {
        void GetClassID(out Guid classId);
        [PreserveSig] int IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string fileName, uint mode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string fileName, [MarshalAs(UnmanagedType.Bool)] bool remember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string fileName);
        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string fileName);
    }
}
