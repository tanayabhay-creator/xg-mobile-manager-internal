using System.Diagnostics;
using System.IO;
using AllyConsole.Core.Input;
using AllyConsole.Core.Logging;
using AllyConsole.Core.Navigation;
using AllyConsole.Core.Operations;
using AllyConsole.Core.Power;
using AllyConsole.Core.Settings;
using AllyConsole.Core.Status;
using AllyConsole.Core.Steam;
using AllyConsole.App.Windows;

namespace AllyConsole.App.Hosting;

/// <summary>
/// Owns settings, the hidden-panel XInput poll, and quick-panel actions.
/// Hardware probes run while the panel is open. The hidden poll is a slow XInput read, not a spin.
/// </summary>
internal sealed class CompanionController : IDisposable
{
    private readonly bool _developmentMode;
    private readonly SynchronizationContext? _ui;
    private readonly SettingsStore _store = new();
    private readonly WindowsStatusReader _reader = new();
    private readonly HotkeyRegistrar _hotkeys = new();
    private readonly WindowsActions _actions;
    private readonly XInputReader _xinput = new();
    private readonly ControllerGestureDecoder _decoder = new();
    private readonly FailureLogGate _failures = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly HashSet<GamepadButton> _chordDraft = new();
    private readonly string _settingsPath;
    private readonly string _logDirectory;
    private readonly bool _canPersist;
    private readonly string? _settingsWarning;
    private readonly RotatingLog? _log;
    private readonly PanelSession _session = new();

    private string _page = "quick";
    private string? _lastControllerSummary;
    private IntPtr _hwnd;
    private IntPtr _previousForeground;
    private bool _exiting;
    private bool _disposed;

    public CompanionController(bool developmentMode, SynchronizationContext? ui)
    {
        _developmentMode = developmentMode;
        _ui = ui;
        _actions = new WindowsActions(_hotkeys);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _settingsPath = AppPaths.SettingsFile(localAppData);
        _logDirectory = AppPaths.LogDirectory(localAppData);
        var load = _store.Load(_settingsPath);
        Settings = load.Settings;
        _canPersist = load.CanPersist;
        _settingsWarning = load.Warning;
        Error = load.Warning;
        try
        {
            _log = new RotatingLog(_logDirectory, Settings.LogMaxBytes, Settings.LogMaxFiles);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            _log = null;
            Error = "Logging is unavailable. " + ex.Message;
        }

        Status = developmentMode
            ? DevelopmentReadings.Create()
            : SystemStatus.Unavailable("Reading Windows status.");
        Log("info", "app.start", developmentMode
            ? "Started in development mode. Sample readings are labelled and are not this PC."
            : "Started.");
        if (load.Warning is not null)
            LogFailure("warn", "startup.settings", load.Warning);
        Focus.SetItems(PanelCatalog.Quick(Status));
        Subtitle = QuickSubtitle();
    }

    public event Action? StateChanged;

    public event Action? UiOpen;

    public event Action? UiClose;

    public event Action? ExitRequested;

    public FocusModel Focus { get; } = new();

    public AppSettings Settings { get; }

    public SystemStatus Status { get; private set; }

    public string Page => _page;

    public string Title => _page switch
    {
        "settings" => "Settings",
        "diagnostics" => "Diagnostics",
        "confirm" => "Confirm",
        "uri" => "Steam URI",
        _ => "Ally Console"
    };

    public string Subtitle { get; private set; } = "";

    public string? Error { get; private set; }

    public string? HotkeyMessage { get; private set; }

    public bool EditingUri { get; private set; }

    public string UriDraft { get; private set; } = "";

    public bool RecordingKeyboard { get; private set; }

    public bool DevelopmentMode => Status.DevelopmentMode;

    public void SetUriDraft(string text) => UriDraft = text ?? "";

    public void Start()
    {
        ReconcileStartup();
        if (Settings.OpenSteamBigPictureOnStartup)
        {
            var steam = _actions.OpenSteam(Settings.SteamLaunchUri);
            if (!steam.Succeeded)
            {
                Error = steam.Message;
                LogFailure("warn", "startup.steam", steam.Code + " " + steam.Message);
            }
            else
            {
                _failures.Recovered("startup.steam");
                Log("info", "startup.steam", steam.Code + " " + steam.Message);
            }

            Notify();
        }

        _ = RunLoop(InputLoop);
        _ = RunLoop(StatusLoop);
    }

    public void AttachHotkey(IntPtr hwnd)
    {
        _hwnd = hwnd;
        var result = _actions.ReplaceHotkey(hwnd, Settings.KeyboardShortcut);
        HotkeyMessage = result.Succeeded ? null : result.Message;
        LogShortcut(result);
        Notify();
    }

    public void OpenPanel()
    {
        if (_session.IsOpen)
        {
            UiOpen?.Invoke();
            return;
        }

        _previousForeground = CaptureForeground();
        _session.Open();
        ShowQuick();
        UiOpen?.Invoke();
        _ = RefreshStatusAsync();
    }

    public void HidePanel(bool restoreFocus)
    {
        if (!_session.IsOpen)
            return;

        var previous = _previousForeground;
        _session.Close();
        Notify();
        UiClose?.Invoke();
        if (restoreFocus && previous != IntPtr.Zero)
        {
            var restored = _actions.RestoreForeground(previous);
            Log("info", "focus.restore", restored
                ? "SetForegroundWindow returned true. Exclusive fullscreen and elevated windows can still ignore it."
                : "SetForegroundWindow returned false. Exclusive fullscreen and elevated windows often do.");
        }

        _session.AcknowledgeFocusRestore();
    }

    public void TogglePanel()
    {
        if (_session.IsOpen)
            HidePanel(restoreFocus: true);
        else
            OpenPanel();
    }

    public void Move(NavDirection direction)
    {
        var before = Focus.Focused;
        Focus.Move(direction);
        var after = Focus.Focused;
        if (before is not null
            && after is not null
            && before.Id == after.Id
            && before.Kind == NavItemKind.Slider
            && before.Value != after.Value)
        {
            ApplySlider(after.Id, after.Value);
            return;
        }

        Notify();
    }

    public void SetSlider(string id, int value)
    {
        if (!Focus.TrySetSlider(id, value))
        {
            Error = Focus.Items.FirstOrDefault(item => item.Id == id)?.DisabledReason;
            Notify();
            return;
        }

        var updated = Focus.Items.First(item => item.Id == id);
        ApplySlider(id, updated.Value);
    }

    public void Select(string id, bool activate)
    {
        Focus.FocusId(id);
        if (activate)
            Activate();
        else
            Notify();
    }

    public void Activate()
    {
        if (!Focus.TryActivate(out var item) || item is null)
        {
            Error = Focus.Focused?.DisabledReason;
            Notify();
            return;
        }

        switch (item.Id)
        {
            case "mute":
                SetMuted(item.Value == 1);
                break;
            case "start-with-windows":
                SetStartWithWindows(item.Value == 1);
                break;
            case "open-steam-on-startup":
                Settings.OpenSteamBigPictureOnStartup = item.Value == 1;
                Persist();
                Error = Settings.OpenSteamBigPictureOnStartup
                    ? "Steam Big Picture will open the next time Ally Console starts. Sign-in launch is a separate switch."
                    : "Steam Big Picture will not open when Ally Console starts.";
                ShowSettings();
                break;
            case "save-chord":
                SaveChord();
                break;
            case "edit-uri":
                BeginEditUri();
                break;
            case "save-uri":
                SaveUri();
                break;
            case "record-keyboard":
                RecordingKeyboard = true;
                Error = "Press the new keyboard shortcut. Esc cancels.";
                Notify();
                break;
            case "network":
                RunExternal(() => _actions.OpenNetwork(), hideOnSuccess: true, restoreFocus: false);
                break;
            case "steam":
                RunExternal(() => _actions.OpenSteam(Settings.SteamLaunchUri), hideOnSuccess: true, restoreFocus: false);
                break;
            case "desktop":
                HidePanel(restoreFocus: false);
                var desktop = _actions.ShowDesktop();
                Error = desktop.Succeeded ? null : desktop.Message;
                Log("info", "desktop", desktop.Code + " " + desktop.Message);
                Notify();
                break;
            case "settings":
                EnterSettings();
                break;
            case "diagnostics":
                ShowDiagnostics();
                break;
            case "restart":
                ShowConfirm("restart");
                break;
            case "shutdown":
                ShowConfirm("shutdown");
                break;
            case "exit":
                ShowConfirm("exit");
                break;
            case "cancel":
            case "back-to-quick":
                ShowQuick();
                break;
            case "confirm-restart":
                ConfirmPower(PowerIntent.Restart);
                break;
            case "confirm-shutdown":
                ConfirmPower(PowerIntent.Shutdown);
                break;
            case "confirm-exit":
                Exit();
                break;
            default:
                if (item.Id.StartsWith("chord:", StringComparison.Ordinal))
                    UpdateChordDraft(item);
                else
                    Notify();
                break;
        }
    }

    public void Back()
    {
        if (RecordingKeyboard)
        {
            CancelRecording();
            return;
        }

        if (EditingUri || _page == "uri")
        {
            EditingUri = false;
            ShowSettings();
            return;
        }

        if (_page == "quick")
        {
            HidePanel(restoreFocus: true);
            return;
        }

        ShowQuick();
    }

    public bool TryCaptureKey(string keyName, bool control, bool alt, bool shift, bool win)
    {
        if (!RecordingKeyboard)
            return false;

        var shortcut = new KeyboardShortcut
        {
            Control = control,
            Alt = alt,
            Shift = shift,
            Win = win,
            Key = keyName
        };
        var decision = ShortcutRules.ValidateKeyboard(shortcut, Array.Empty<string>());
        if (!decision.Accepted)
        {
            Error = decision.Message;
            Notify();
            return true;
        }

        var registered = _actions.ReplaceHotkey(_hwnd, shortcut);
        if (!registered.Succeeded)
        {
            Error = registered.Message;
            HotkeyMessage = registered.Message;
            LogShortcut(registered);
            Notify();
            return true;
        }

        Settings.KeyboardShortcut = shortcut;
        Persist();
        RecordingKeyboard = false;
        HotkeyMessage = null;
        Error = "Keyboard shortcut registered.";
        ShowSettings();
        return true;
    }

    public void CancelRecording()
    {
        if (!RecordingKeyboard)
            return;
        RecordingKeyboard = false;
        Error = null;
        Notify();
    }

    public void ReportFault(Exception exception)
    {
        LogFailure("error", "fault", exception.GetType().Name + ": " + exception.Message);
        Error = "Ally Console hit an error and is still running. " + exception.Message;
        Notify();
    }

    public void Exit()
    {
        if (_exiting)
            return;
        _exiting = true;
        Log("info", "app.exit", "Ally Console exiting. Windows is left as it is.");
        Dispose();
        ExitRequested?.Invoke();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _cts.Cancel();
        _hotkeys.Dispose();
        _cts.Dispose();
    }

    private void ReconcileStartup()
    {
        if (!_canPersist)
        {
            Log("info", "startup", "Skipped the sign-in shortcut because settings could not be saved.");
            return;
        }

        var result = _actions.ApplyStartup(Settings.StartWithWindows, Environment.ProcessPath);
        LogStartupShortcut(result);
        if (!result.Succeeded && string.IsNullOrEmpty(Error))
            Error = result.Message;
    }

    private async Task RunLoop(Func<CancellationToken, Task> loop)
    {
        try
        {
            await loop(_cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Post(() => ReportFault(ex));
        }
    }

    private async Task InputLoop(CancellationToken token)
    {
        var clock = Stopwatch.StartNew();
        while (!token.IsCancellationRequested)
        {
            // Hidden default is 150 ms. A held chord is seen on the next sample, so
            // detection waits at most one interval plus this XInputGetState call.
            // The call does not consume the pad; the game still sees the buttons.
            var interval = PollPolicy.InputIntervalMs(
                _session.IsOpen,
                Settings.HiddenPollIntervalMs,
                Settings.VisiblePollIntervalMs);
            await Task.Delay(interval, token).ConfigureAwait(false);
            ControllerSnapshot snapshot;
            try
            {
                snapshot = _xinput.Read();
            }
            catch (Exception ex)
            {
                LogFailure("error", "controller.read", ex.GetType().Name + ": " + ex.Message);
                Post(() =>
                {
                    Error = "Controller read failed. " + ex.Message;
                    Notify();
                });
                continue;
            }

            _failures.Recovered("controller.read");

            var summary = _xinput.Summary;
            var gesture = _decoder.Observe(CurrentChord(), snapshot, clock.Elapsed);
            if (!string.Equals(summary, _lastControllerSummary, StringComparison.Ordinal))
            {
                _lastControllerSummary = summary;
                var xinputReason = _xinput.UnavailableReason;
                if (xinputReason is null)
                    _failures.Recovered("integration.xinput");
                else
                    LogFailure("warn", "integration.xinput", xinputReason);
                Post(RefreshControllerSubtitle);
            }

            if (!gesture.OpenOrClosePanel && gesture.Direction is null && !gesture.Confirm && !gesture.Back)
                continue;
            Post(() => ApplyGesture(gesture));
        }
    }

    private async Task StatusLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var visible = _session.IsOpen && !_developmentMode;
            var delay = visible ? Math.Max(500, Settings.StatusRefreshIntervalMs) : 1000;
            await Task.Delay(delay, token).ConfigureAwait(false);
            if (!PollPolicy.RefreshHardware(_session.IsOpen) || _developmentMode)
                continue;
            if (_page is not ("quick" or "diagnostics"))
                continue;

            var status = await Task.Run(() => _reader.Read(false), token).ConfigureAwait(false);
            Post(() => ApplyProbedStatus(status));
        }
    }

    private async Task RefreshStatusAsync()
    {
        if (_developmentMode)
            return;
        try
        {
            var status = await Task.Run(() => _reader.Read(false));
            Post(() => ApplyProbedStatus(status));
        }
        catch (Exception ex)
        {
            Post(() => ReportFault(ex));
        }
    }

    private void RefreshControllerSubtitle()
    {
        if (_page == "quick")
            Subtitle = QuickSubtitle();
        if (_session.IsOpen && _page is "quick" or "diagnostics")
        {
            if (_page == "diagnostics")
                Focus.SetItems(PanelCatalog.Diagnostics(BuildFacts()));
            Notify();
        }
    }

    private void ApplyGesture(ControllerGesture gesture)
    {
        if (gesture.OpenOrClosePanel)
        {
            TogglePanel();
            return;
        }

        if (!_session.IsOpen || RecordingKeyboard)
            return;
        if (gesture.Direction is { } direction)
            Move(direction);
        else if (gesture.Confirm)
            Activate();
        else if (gesture.Back)
            Back();
    }

    private void ApplyProbedStatus(SystemStatus status)
    {
        if (_developmentMode || !_session.IsOpen)
            return;
        Status = status;
        LogIntegrations(status);
        if (_page == "quick")
            Focus.SetItems(PanelCatalog.Quick(Status));
        else if (_page == "diagnostics")
            Focus.SetItems(PanelCatalog.Diagnostics(BuildFacts()));
        else
            return;
        Notify();
    }

    private void ApplySlider(string id, int value)
    {
        if (id == "volume")
            ApplyVolume(value);
        else if (id == "brightness")
            ApplyBrightness(value);
        if (_page == "quick")
            Focus.SetItems(PanelCatalog.Quick(Status));
        Notify();
    }

    private void ApplyVolume(int value)
    {
        if (Status.DevelopmentMode)
        {
            var muted = Status.Volume.Value?.Muted ?? false;
            Status = Status with { Volume = Reading<VolumeReading>.Of(new VolumeReading(value, muted)) };
            return;
        }

        var result = MasterVolume.SetPercent(value);
        Error = result.Succeeded ? null : result.Message;
        if (!result.Succeeded)
            return;
        var stillMuted = Status.Volume.Value?.Muted ?? false;
        Status = Status with { Volume = Reading<VolumeReading>.Of(new VolumeReading(value, stillMuted)) };
    }

    private void ApplyBrightness(int value)
    {
        if (Status.DevelopmentMode)
        {
            Status = Status with { Brightness = BrightnessReading.SupportedPercent(value) };
            return;
        }

        if (!Status.Brightness.Supported)
        {
            Error = Status.Brightness.UnavailableReason;
            return;
        }

        var result = BrightnessWmi.Set(value);
        Error = result.Succeeded ? null : result.Message;
        if (result.Succeeded)
            Status = Status with { Brightness = BrightnessReading.SupportedPercent(value) };
    }

    private void SetMuted(bool muted)
    {
        if (Status.DevelopmentMode)
        {
            var percent = Status.Volume.Value?.Percent ?? 0;
            Status = Status with { Volume = Reading<VolumeReading>.Of(new VolumeReading(percent, muted)) };
            if (_page == "quick")
                Focus.SetItems(PanelCatalog.Quick(Status));
            Notify();
            return;
        }

        var result = MasterVolume.SetMuted(muted);
        Error = result.Succeeded ? null : result.Message;
        if (result.Succeeded)
        {
            var percent = Status.Volume.Value?.Percent ?? 0;
            Status = Status with { Volume = Reading<VolumeReading>.Of(new VolumeReading(percent, muted)) };
        }

        if (_page == "quick")
            Focus.SetItems(PanelCatalog.Quick(Status));
        Notify();
    }

    private void SetStartWithWindows(bool enabled)
    {
        if (!_canPersist)
        {
            Error = _settingsWarning ?? "Settings were not changed.";
            ShowSettings();
            return;
        }

        var previous = Settings.StartWithWindows;
        Settings.StartWithWindows = enabled;
        var result = _actions.ApplyStartup(enabled, Environment.ProcessPath);
        LogStartupShortcut(result);
        if (!result.Succeeded)
        {
            Settings.StartWithWindows = previous;
            Error = result.Message;
            ShowSettings();
            return;
        }

        Persist();
        Error = result.Message;
        ShowSettings();
    }

    private void EnterSettings()
    {
        LoadDraft();
        ShowSettings();
    }

    private void ShowSettings()
    {
        _page = "settings";
        EditingUri = false;
        RecordingKeyboard = false;
        var preview = "Default is Left bumper + Right bumper + View (Back). "
            + "Ally controls usually appear as an XInput gamepad. Current: "
            + GamepadText.FormatChord(DraftButtons());
        Focus.SetItems(PanelCatalog.Settings(Settings, _chordDraft, preview));
        Subtitle = "These settings stay on this Windows user. No administrator prompt.";
        Notify();
    }

    private void ShowQuick()
    {
        _page = "quick";
        EditingUri = false;
        RecordingKeyboard = false;
        Focus.SetItems(PanelCatalog.Quick(Status));
        Subtitle = QuickSubtitle();
        Notify();
    }

    private void ShowDiagnostics()
    {
        _page = "diagnostics";
        EditingUri = false;
        RecordingKeyboard = false;
        Focus.SetItems(PanelCatalog.Diagnostics(BuildFacts()));
        Subtitle = "Read only. A detected GPU is not necessarily rendering the current game.";
        Notify();
    }

    private void ShowConfirm(string action)
    {
        _page = "confirm";
        EditingUri = false;
        RecordingKeyboard = false;
        Focus.SetItems(PanelCatalog.Confirm(action), "cancel");
        Subtitle = "Cancel is selected. Nothing happens until you confirm.";
        Error = null;
        Notify();
    }

    private void BeginEditUri()
    {
        UriDraft = Settings.SteamLaunchUri;
        EditingUri = true;
        RecordingKeyboard = false;
        _page = "uri";
        Focus.SetItems(new[]
        {
            NavItem.Action("save-uri", "Save Steam URI", "Must start with steam://."),
            NavItem.Action("cancel", "Cancel", "")
        });
        Subtitle = "Edit with the keyboard or the touch keyboard.";
        Error = null;
        Notify();
    }

    private void SaveUri()
    {
        var decision = SteamLaunch.ValidateUri(UriDraft);
        if (!decision.Succeeded)
        {
            Error = decision.Message;
            Notify();
            return;
        }

        Settings.SteamLaunchUri = decision.Command ?? SteamLaunch.DefaultUri;
        EditingUri = false;
        Persist();
        Error = "Steam URI saved.";
        ShowSettings();
    }

    private void SaveChord()
    {
        var buttons = DraftButtons();
        var decision = ShortcutRules.ValidateController(buttons, Array.Empty<IReadOnlyList<GamepadButton>>());
        if (!decision.Accepted)
        {
            Error = decision.Message;
            Notify();
            return;
        }

        Settings.ControllerChord = buttons.Select(button => button.ToString()).ToList();
        Persist();
        Error = "Controller shortcut saved. " + GamepadText.FormatChord(buttons);
        ShowSettings();
    }

    private void UpdateChordDraft(NavItem item)
    {
        var name = item.Id["chord:".Length..];
        if (!Enum.TryParse(name, out GamepadButton button) || button == GamepadButton.Guide)
        {
            Error = "Guide is reserved.";
            Notify();
            return;
        }

        if (item.Value == 1)
            _chordDraft.Add(button);
        else
            _chordDraft.Remove(button);

        var decision = ShortcutRules.ValidateController(DraftButtons(), Array.Empty<IReadOnlyList<GamepadButton>>());
        Error = decision.Accepted ? null : decision.Message + " It is not saved yet.";
        Notify();
    }

    private void ConfirmPower(PowerIntent intent)
    {
        var result = _actions.Power(intent);
        Log("info", "power", result.Code + " " + result.Message);
        if (!result.Succeeded)
        {
            Error = result.Message;
            Notify();
        }
    }

    private void RunExternal(Func<OperationResult> action, bool hideOnSuccess, bool restoreFocus)
    {
        var result = action();
        Log("info", "action", result.Code + " " + result.Message);
        if (!result.Succeeded)
        {
            Error = result.Message;
            Notify();
            return;
        }

        Error = null;
        if (hideOnSuccess)
            HidePanel(restoreFocus);
        else
            Notify();
    }

    private void Persist()
    {
        if (!_canPersist)
        {
            Error = _settingsWarning ?? "Settings were not saved.";
            return;
        }

        try
        {
            _store.Save(_settingsPath, Settings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Error = "Could not save settings. " + ex.Message;
        }
    }

    private void LoadDraft()
    {
        _chordDraft.Clear();
        if (!ShortcutRules.ParseControllerButtons(Settings.ControllerChord, out var buttons).Accepted)
            return;
        foreach (var button in buttons)
            _chordDraft.Add(button);
    }

    private GamepadButton[] DraftButtons()
        => GamepadText.EditorOrder.Where(button => _chordDraft.Contains(button) && button != GamepadButton.Guide).ToArray();

    private ControllerChord CurrentChord()
    {
        if (ShortcutRules.ParseControllerButtons(Settings.ControllerChord, out var buttons).Accepted)
            return new ControllerChord(buttons);
        return ControllerChord.Default;
    }

    private string QuickSubtitle()
    {
        var chord = CurrentChord();
        return "Open or close: " + GamepadText.FormatChord(chord.Buttons)
            + ", or " + Settings.KeyboardShortcut.Canonical()
            + ". " + _xinput.Summary;
    }

    private DiagnosticFacts BuildFacts() => new(
        Status,
        HotkeyMessage ?? ("Registered " + Settings.KeyboardShortcut.Canonical()),
        _xinput.Summary,
        Settings.StartWithWindows
            ? "On. The per-user Startup shortcut is AllyConsole.lnk. Turn the setting off to remove it."
            : "Off. Ally Console does not start at sign-in.",
        _logDirectory,
        _settingsPath);

    private IntPtr CaptureForeground()
    {
        var hwnd = _actions.ForegroundWindow();
        if (hwnd == IntPtr.Zero || hwnd == _hwnd)
            return IntPtr.Zero;
        return hwnd;
    }

    private void Notify() => StateChanged?.Invoke();

    private void Post(Action action)
    {
        if (_ui is null || SynchronizationContext.Current == _ui)
            action();
        else
            _ui.Post(_ => action(), null);
    }

    private void LogStartupShortcut(OperationResult result)
    {
        if (result.Succeeded)
        {
            _failures.Recovered("startup.shortcut");
            Log("info", "startup.shortcut", result.Code + " " + result.Message);
            return;
        }

        LogFailure("warn", "startup.shortcut", result.Code + " " + result.Message);
    }

    private void LogShortcut(OperationResult result)
    {
        if (result.Succeeded)
        {
            _failures.Recovered("shortcut.hotkey");
            Log("info", "shortcut.hotkey", result.Code + " " + result.Message);
            return;
        }

        LogFailure("warn", "shortcut.hotkey", result.Code + " " + result.Message);
    }

    private void LogIntegrations(SystemStatus status)
    {
        var admitted = _failures.SelectNew(
            UnsupportedIntegrationLog.Failures(status),
            UnsupportedIntegrationLog.Events);
        foreach (var notice in admitted)
            Log("warn", notice.EventName, notice.Message);
    }

    private void LogFailure(string level, string eventName, string message)
    {
        if (!_failures.ShouldWrite(eventName, message))
            return;
        Log(level, eventName, message);
    }

    private void Log(string level, string eventName, string message)
    {
        try
        {
            _log?.Write(level, eventName, message);
        }
        catch (IOException)
        {
        }
    }
}
