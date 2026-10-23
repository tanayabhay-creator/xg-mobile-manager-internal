using System.Runtime.InteropServices;
using AllyConsole.Core.Operations;
using AllyConsole.Core.Status;

namespace AllyConsole.App.Windows;

/// <summary>
/// Default console playback endpoint through the documented Core Audio interfaces.
/// </summary>
internal static class MasterVolume
{
    private static readonly Guid VolumeInterfaceId = new("5CDF2C82-841E-4546-9722-0CF74078229A");

    public static Reading<VolumeReading> Read()
    {
        if (!TryOpen(out var volume, out var reason) || volume is null)
            return Reading<VolumeReading>.Unavailable(reason);
        try
        {
            var level = volume.GetMasterVolumeLevelScalar(out var scalar);
            if (level != 0)
                return Reading<VolumeReading>.Unavailable("Master volume could not be read (0x" + level.ToString("X8") + ").");
            var mute = volume.GetMute(out var muted);
            if (mute != 0)
                return Reading<VolumeReading>.Unavailable("Mute could not be read (0x" + mute.ToString("X8") + ").");
            var percent = (int)Math.Round(Math.Clamp(scalar, 0f, 1f) * 100f);
            return Reading<VolumeReading>.Of(new VolumeReading(percent, muted));
        }
        catch (Exception ex) when (ex is COMException or InvalidCastException)
        {
            return Reading<VolumeReading>.Unavailable(Trim(ex.Message));
        }
        finally
        {
            Release(volume);
        }
    }

    public static OperationResult SetPercent(int percent)
    {
        if (!TryOpen(out var volume, out var reason) || volume is null)
            return OperationResult.Fail("volume-unavailable", reason);
        try
        {
            var context = Guid.Empty;
            var scalar = Math.Clamp(percent, 0, 100) / 100f;
            var hr = volume.SetMasterVolumeLevelScalar(scalar, ref context);
            return hr == 0
                ? OperationResult.Ok("Volume updated.")
                : OperationResult.Fail("volume-failed", "Volume was not changed (0x" + hr.ToString("X8") + ").");
        }
        catch (Exception ex) when (ex is COMException or InvalidCastException)
        {
            return OperationResult.Fail("volume-failed", Trim(ex.Message));
        }
        finally
        {
            Release(volume);
        }
    }

    public static OperationResult SetMuted(bool muted)
    {
        if (!TryOpen(out var volume, out var reason) || volume is null)
            return OperationResult.Fail("volume-unavailable", reason);
        try
        {
            var context = Guid.Empty;
            var hr = volume.SetMute(muted, ref context);
            return hr == 0
                ? OperationResult.Ok(muted ? "Muted." : "Unmuted.")
                : OperationResult.Fail("volume-failed", "Mute was not changed (0x" + hr.ToString("X8") + ").");
        }
        catch (Exception ex) when (ex is COMException or InvalidCastException)
        {
            return OperationResult.Fail("volume-failed", Trim(ex.Message));
        }
        finally
        {
            Release(volume);
        }
    }

    private static bool TryOpen(out IAudioEndpointVolume? volume, out string reason)
    {
        volume = null;
        object? enumerator = null;
        object? device = null;
        try
        {
            enumerator = new MMDeviceEnumerator();
            var deviceEnumerator = (IMMDeviceEnumerator)enumerator;
            var hr = deviceEnumerator.GetDefaultAudioEndpoint(0, 0, out var endpoint);
            if (hr != 0 || endpoint is null)
            {
                Release(endpoint);
                reason = "No default playback device was found (0x" + hr.ToString("X8") + ").";
                return false;
            }

            device = endpoint;
            var iid = VolumeInterfaceId;
            hr = endpoint.Activate(ref iid, 1, IntPtr.Zero, out var activated);
            if (hr != 0 || activated is not IAudioEndpointVolume opened)
            {
                reason = "The playback device has no master volume control (0x" + hr.ToString("X8") + ").";
                Release(activated);
                return false;
            }

            volume = opened;
            reason = "";
            return true;
        }
        catch (Exception ex) when (ex is COMException or InvalidCastException or InvalidOperationException)
        {
            reason = Trim(ex.Message);
            return false;
        }
        finally
        {
            Release(device);
            Release(enumerator);
        }
    }

    private static void Release(object? com)
    {
        if (com is not null && Marshal.IsComObject(com))
            Marshal.FinalReleaseComObject(com);
    }

    private static string Trim(string message)
    {
        var text = string.IsNullOrWhiteSpace(message) ? "Volume control failed." : message.Trim();
        return text.Length <= 180 ? text : text[..180];
    }

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumerator
    {
    }

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig]
        int EnumAudioEndpoints(int dataFlow, uint stateMask, out IntPtr devices);

        [PreserveSig]
        int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice? endpoint);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig]
        int Activate(ref Guid iid, int classContext, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object? instance);
    }

    [ComImport]
    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        [PreserveSig] int RegisterControlChangeNotify(IntPtr client);
        [PreserveSig] int UnregisterControlChangeNotify(IntPtr client);
        [PreserveSig] int GetChannelCount(out uint count);
        [PreserveSig] int SetMasterVolumeLevel(float levelDb, ref Guid eventContext);
        [PreserveSig] int SetMasterVolumeLevelScalar(float level, ref Guid eventContext);
        [PreserveSig] int GetMasterVolumeLevel(out float levelDb);
        [PreserveSig] int GetMasterVolumeLevelScalar(out float level);
        [PreserveSig] int SetChannelVolumeLevel(uint channel, float levelDb, ref Guid eventContext);
        [PreserveSig] int SetChannelVolumeLevelScalar(uint channel, float level, ref Guid eventContext);
        [PreserveSig] int GetChannelVolumeLevel(uint channel, out float levelDb);
        [PreserveSig] int GetChannelVolumeLevelScalar(uint channel, out float level);
        [PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, ref Guid eventContext);
        [PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)] out bool mute);
    }
}
