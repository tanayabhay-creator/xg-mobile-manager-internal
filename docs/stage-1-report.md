# Ally Console Stage 1 report

Stage 1 is a compiled companion and a tested core library. It was not run on Windows, not run on an ASUS ROG Ally, and not pointed at an XG Mobile. Nothing here is a hardware verification. No CPU, frame-time, or thermal number was measured.

The Decky plugin in the rest of this repository was not modified, except for a one-line pointer in the root `README.md` and an `ally-console/**/bin/` and `obj/` gitignore so plugin scripts under `bin/` stay tracked.

## What works in the tree

Automated on this agent (SDK 8.0.426, `global.json` asks for 8.0.100 with `latestFeature` roll-forward):

- `dotnet test ally-console/tests/AllyConsole.Core.Tests/AllyConsole.Core.Tests.csproj` — Passed 50, Failed 0, Skipped 0.
- `dotnet build ally-console/src/AllyConsole.App/AllyConsole.App.csproj` — succeeded, 0 warnings, 0 errors. `EnableWindowsTargeting` downloads the Windows desktop reference assemblies so the WPF project type-checks on Linux. The output is `AllyConsole.dll` for `net8.0-windows`. It was not executed. A window was not shown.

Core tests cover settings round-trip, newer and corrupt settings left untouched, log rotation, shortcut conflicts, gesture edges, focus and slider behavior, the brightness-unsupported row, normal mode refusing sample battery/GPU/volume, sign-in launch against a temp folder (and not the real Startup folder), missing Steam command construction, and failed `OperationResult` values with a null command.

The WPF shell implements the Stage 1 panel: volume, mute, brightness, battery, labelled GPU and display diagnostics, a Windows network shortcut, Steam, desktop mode, settings, diagnostics, confirmed restart and shut down, and confirmed exit. Controller chord, keyboard hotkey, touch, and mouse are in the UI code. Local settings and rotating logs are wired to `%LOCALAPPDATA%\AllyConsole`. Sign-in launch is an opt-in Startup shortcut. `--dev` shows a banner and sample rows prefixed with “Sample:”.

## What is unsupported

- Display switching. The row is not in the panel.
- ASUS XG Mobile control. Diagnostics say Stage 1 does not control it and that Stage 3 may add a labelled shortcut to the official tool.
- Guide as a chord button.
- Brightness when `WmiMonitorBrightness` is absent. The slider stays disabled. The app does not write the EC or firmware.
- Fake status in normal mode. Before the first successful probe the UI says Windows status has not been read.
- Suspend and resume of a running game.
- Background-app changes, performance profiles, fan control, and power limits.
- An elevated helper, a Windows service, shell replacement, UAC changes, and automatic login.

## Limitations

- Initial input target is a borderless game with this window in front. XInput cannot be taken exclusively without a driver or injection, which is forbidden, so the game still receives the chord buttons.
- Exclusive fullscreen often ignores `RegisterHotKey` and `SetForegroundWindow`. The log records whether focus restore returned true or false. True still does not prove the game accepted focus.
- A window running elevated above this user process can ignore the hotkey and focus return.
- `steam://open/bigpicture` depends on Steam’s URI handler. Focus after Big Picture opens is not controlled.
- Ally internal brightness is often vendor-specific. Unsupported is a normal result, not a crash.
- Adapter names from `Win32_VideoController` are not the rendering GPU and are not an XG Mobile on/off state.
- Startup-folder behavior while Big Picture covers the desktop was not tested. Explorer is still the shell.
- Alt+F4 hides the panel. Exit is separate, so a mis-hit does not kill the companion.
- Idle cost was not measured. By construction, a hidden panel sleeps 150 ms (default), then calls `XInputGetState` for users 0–3 until one responds (one call if user 0 is connected, four if none are). It does not call WMI or Core Audio while hidden. The status loop wakes once a second while hidden and does no probe. Visible input sleeps 50 ms. Visible status refresh defaults to 2000 ms and is not faster than 500 ms.

## Automated checks (this agent)

```bash
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
dotnet test ally-console/tests/AllyConsole.Core.Tests/AllyConsole.Core.Tests.csproj --nologo
dotnet build ally-console/src/AllyConsole.App/AllyConsole.App.csproj --nologo
```

Result on 2026-10-19: tests passed 50/50. App build succeeded. Do not treat that build as a run.

`dotnet test ally-console/AllyConsole.sln` also builds the WPF project. On Linux that needs the same Windows targeting pack. It does not launch the app.

## Windows build and run

On a Windows machine with the .NET 8 SDK (desktop workload):

```bat
cd ally-console
dotnet test AllyConsole.sln
dotnet run --project src\AllyConsole.App\AllyConsole.App.csproj
```

Development samples only:

```bat
dotnet run --project src\AllyConsole.App\AllyConsole.App.csproj -- --dev
```

Publish is not part of Stage 1. The project is a normal framework-dependent WPF app, `asInvoker`, per-monitor DPI v2.

## Physical Ally test steps

Do these on the Ally. They are not done. Cancel any restart or shut-down prompt. Do not deactivate the XG Mobile as part of this pass. Do not remove Armoury Crate, AMD, or NVIDIA software.

1. Build and start without `--dev`. Confirm there is no development banner and that battery, volume, and GPU rows are either live Windows readings or “Unavailable” with a reason. They must not show the 76% sample battery or “Sample GPU (development mode)”.
2. Start once with `--dev`. Confirm the banner and the “Sample:” prefix, then exit and start again without the flag. Confirm samples are gone and that `--dev` was not saved.
3. At 720p and 1080p, check that rows are readable on the 7-inch panel, focus is obvious, and touch, mouse, and the stick or D-pad all move the same focus.
4. Open and close with Left bumper + Right bumper + View (Back). Confirm the chord does not repeat while held, and that Back does not also navigate until the chord is released. Confirm a game that reads XInput still sees those buttons.
5. Press Ctrl+Alt+Shift+F1. If registration failed, the panel must say so. Try a bare key and Win+L in settings and confirm they are rejected.
6. Move volume, toggle mute, and compare with the Windows volume flyout. If brightness is disabled, read the WMI explanation and stop. Do not try another brightness tool from this app.
7. Check the battery row against Windows while on battery and while charging.
8. Read the GPU and display rows. Confirm the text does not claim which GPU is rendering. Attach or detach the XG Mobile only through the official ASUS flow if you choose to look at names; do not expect this app to drive that flow.
9. Open network settings and confirm it is Windows Settings, then return.
10. With Steam installed, launch Big Picture from the panel and confirm the URI is `steam://open/bigpicture` unless you changed it. With Steam missing, or with a non-`steam://` URI, confirm a clear error and that Ally Console stays open.
11. Choose Desktop mode. The desktop should appear and the tray companion should still be running. Close the panel from a borderless game and see whether focus returns. Repeat from exclusive fullscreen and from an elevated window, and expect those to fail.
12. Open Restart and Shut down, choose Cancel, and confirm nothing happens. Do not confirm unless you intend to power the device.
13. Turn on “Start Ally Console when I sign in”, sign out and back in, and confirm the companion starts. Turn it off and confirm `%USERPROFILE%\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup\AllyConsole.lnk` is gone. Leave “Open Steam Big Picture when Ally Console starts” off for one pass, then on for a second pass.
14. Exit from the panel and from the tray. Confirm Explorer is still the shell and that no new service was installed.
15. Delete `%LOCALAPPDATA%\AllyConsole` and start again. Defaults should return, including sign-in launch off.

## Recovery

Exit the companion, or delete the Startup shortcut above, or delete `%LOCALAPPDATA%\AllyConsole`. Windows does not depend on this process.
