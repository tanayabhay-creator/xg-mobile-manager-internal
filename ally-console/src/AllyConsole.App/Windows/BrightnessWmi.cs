using System.Management;
using System.Runtime.InteropServices;
using AllyConsole.Core.Operations;
using AllyConsole.Core.Status;

namespace AllyConsole.App.Windows;

/// <summary>
/// Uses WmiMonitorBrightness only. If Windows does not expose it, the control stays disabled.
/// No embedded-controller or firmware writes.
/// </summary>
internal static class BrightnessWmi
{
    public static BrightnessReading Read()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT CurrentBrightness FROM WmiMonitorBrightness");
            using var results = searcher.Get();
            foreach (ManagementObject item in results)
            {
                using (item)
                {
                    return BrightnessReading.SupportedPercent(Convert.ToInt32(item["CurrentBrightness"]));
                }
            }

            return BrightnessReading.Unsupported();
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            return BrightnessReading.Unsupported(BrightnessReading.UnsupportedExplanation + " Windows said: " + Trim(ex.Message));
        }
    }

    public static OperationResult Set(int percent)
    {
        ManagementObject? chosen = null;
        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT * FROM WmiMonitorBrightnessMethods");
            using var results = searcher.Get();
            foreach (ManagementObject item in results)
            {
                var active = item["Active"] is bool flag && flag;
                if (chosen is null || active)
                {
                    chosen?.Dispose();
                    chosen = item;
                    if (active)
                        continue;
                }
                else
                {
                    item.Dispose();
                }
            }

            if (chosen is null)
                return OperationResult.Fail("brightness-unsupported", BrightnessReading.UnsupportedExplanation);

            var result = chosen.InvokeMethod("WmiSetBrightness", new object[] { (uint)1, (byte)Math.Clamp(percent, 0, 100) });
            if (result is null || Convert.ToUInt32(result) == 0)
                return OperationResult.Ok("Brightness updated.");
            return OperationResult.Fail(
                "brightness-failed",
                "WmiSetBrightness returned " + result + ". No undocumented brightness path was tried.");
        }
        catch (Exception ex) when (IsExpected(ex))
        {
            return OperationResult.Fail(
                "brightness-unsupported",
                BrightnessReading.UnsupportedExplanation + " Windows said: " + Trim(ex.Message));
        }
        finally
        {
            chosen?.Dispose();
        }
    }

    private static bool IsExpected(Exception ex)
        => ex is ManagementException or UnauthorizedAccessException or PlatformNotSupportedException or InvalidOperationException or COMException;

    private static string Trim(string message)
    {
        var text = string.IsNullOrWhiteSpace(message) ? "The brightness provider failed." : message.Trim();
        return text.Length <= 180 ? text : text[..180];
    }
}
