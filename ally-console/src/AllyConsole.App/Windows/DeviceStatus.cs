using System.Management;
using System.Runtime.InteropServices;
using AllyConsole.Core.Status;

namespace AllyConsole.App.Windows;

internal static class DeviceStatus
{
    private const int AttachedToDesktop = 0x00000001;
    private const int PrimaryDevice = 0x00000004;
    private const int MirroringDriver = 0x00000008;
    private const int MonitorActive = 0x00000001;

    public static Reading<BatteryReading> Battery()
    {
        try
        {
            if (!GetSystemPowerStatus(out var status))
                return Reading<BatteryReading>.Unavailable("GetSystemPowerStatus failed.");

            if ((status.BatteryFlag & 128) != 0)
                return Reading<BatteryReading>.Of(new BatteryReading(0, false, true));
            if (status.BatteryLifePercent == 255)
                return Reading<BatteryReading>.Unavailable("Windows did not report a battery percentage.");

            var charging = (status.BatteryFlag & 8) != 0;
            return Reading<BatteryReading>.Of(new BatteryReading(status.BatteryLifePercent, charging, false));
        }
        catch (Exception ex) when (ex is COMException or InvalidOperationException)
        {
            return Reading<BatteryReading>.Unavailable(Trim(ex.Message));
        }
    }

    public static (IReadOnlyList<AdapterReading> Adapters, string? Reason) Adapters()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name, Status FROM Win32_VideoController");
            using var results = searcher.Get();
            var adapters = new List<AdapterReading>();
            foreach (ManagementObject item in results)
            {
                using (item)
                {
                    var name = item["Name"] as string;
                    if (string.IsNullOrWhiteSpace(name))
                        continue;
                    var status = item["Status"] as string;
                    adapters.Add(new AdapterReading(name.Trim(), string.IsNullOrWhiteSpace(status) ? "Status not reported" : status.Trim()));
                }
            }

            if (adapters.Count == 0)
                return (adapters, "Win32_VideoController returned no adapter names.");
            return (adapters, null);
        }
        catch (Exception ex) when (ex is ManagementException or UnauthorizedAccessException or PlatformNotSupportedException or InvalidOperationException)
        {
            return (Array.Empty<AdapterReading>(), Trim(ex.Message));
        }
    }

    public static (IReadOnlyList<DisplayReading> Displays, string? Reason) Displays()
    {
        try
        {
            var screens = new Dictionary<string, System.Windows.Forms.Screen>(StringComparer.OrdinalIgnoreCase);
            foreach (var screen in System.Windows.Forms.Screen.AllScreens)
            {
                if (!string.IsNullOrWhiteSpace(screen.DeviceName))
                    screens[screen.DeviceName] = screen;
            }

            var displays = new List<DisplayReading>();
            var device = NewDevice();
            for (uint index = 0; EnumDisplayDevices(null, index, ref device, 0); index++)
            {
                if ((device.StateFlags & MirroringDriver) != 0)
                {
                    device = NewDevice();
                    continue;
                }

                var adapterName = device.DeviceName;
                if (string.IsNullOrWhiteSpace(adapterName))
                {
                    device = NewDevice();
                    continue;
                }

                var friendly = device.DeviceString;
                var attached = (device.StateFlags & AttachedToDesktop) != 0;
                var primary = (device.StateFlags & PrimaryDevice) != 0;
                screens.TryGetValue(adapterName, out var screen);
                if (screen is not null)
                    primary = primary || screen.Primary;

                var monitor = NewDevice();
                if (EnumDisplayDevices(adapterName, 0, ref monitor, 0))
                {
                    if (!string.IsNullOrWhiteSpace(monitor.DeviceString))
                        friendly = monitor.DeviceString;
                    attached = (monitor.StateFlags & MonitorActive) != 0;
                }

                var width = screen?.Bounds.Width ?? 0;
                var height = screen?.Bounds.Height ?? 0;
                var name = string.IsNullOrWhiteSpace(friendly) ? adapterName : friendly.Trim();
                displays.Add(new DisplayReading(name, primary, attached, width, height));
                device = NewDevice();
            }

            if (displays.Count == 0 && screens.Count > 0)
            {
                foreach (var screen in screens.Values)
                {
                    displays.Add(new DisplayReading(
                        screen.DeviceName,
                        screen.Primary,
                        true,
                        screen.Bounds.Width,
                        screen.Bounds.Height));
                }
            }

            if (displays.Count == 0)
                return (displays, "Windows reported no display devices.");
            return (displays, null);
        }
        catch (Exception ex) when (ex is COMException or InvalidOperationException or ManagementException)
        {
            return (Array.Empty<DisplayReading>(), Trim(ex.Message));
        }
    }

    public static Reading<string> OperatingSystem()
    {
        try
        {
            var description = RuntimeInformation.OSDescription;
            if (string.IsNullOrWhiteSpace(description))
                return Reading<string>.Unavailable("The runtime did not report an operating system description.");
            return Reading<string>.Of(description.Trim());
        }
        catch (Exception ex) when (ex is InvalidOperationException)
        {
            return Reading<string>.Unavailable(Trim(ex.Message));
        }
    }

    private static DisplayDevice NewDevice() => new() { cb = Marshal.SizeOf<DisplayDevice>() };

    private static string Trim(string message)
    {
        var text = string.IsNullOrWhiteSpace(message) ? "The status API failed." : message.Trim();
        return text.Length <= 180 ? text : text[..180];
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemPowerStatus(out SystemPowerStatus status);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplayDevices(string? device, uint index, ref DisplayDevice displayDevice, uint flags);

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemPowerStatus
    {
        public byte AcLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DisplayDevice
    {
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string? DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string? DeviceString;
        public int StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string? DeviceId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string? DeviceKey;
    }
}
