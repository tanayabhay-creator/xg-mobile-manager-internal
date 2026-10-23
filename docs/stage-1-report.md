# Ally Console Stage 1 report

This file separates what the Linux build environment actually ran from what still needs Windows or the ASUS ROG Ally. It is not a hardware result. The blank sheet for the Ally is [hardware-test-results.md](hardware-test-results.md). Leave that sheet empty until someone fills it on the device.

No CPU, memory, frame-time, or thermal number was measured. The 150 ms figure below is the hidden-panel sleep in code, not a profiler result.

The Decky plugin in the rest of this repository was not modified, except for a one-line pointer in the root `README.md` and gitignore rules so plugin scripts under `bin/` stay tracked. `ally-console/artifacts/` is ignored.

## Verified in this environment (2026-10-20)

SDK 8.0.426. `ally-console/global.json` asks for 8.0.100 with `latestFeature` roll-forward.

```bash
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
dotnet test ally-console/tests/AllyConsole.Core.Tests/AllyConsole.Core.Tests.csproj --nologo
dotnet build ally-console/src/AllyConsole.App/AllyConsole.App.csproj --nologo
dotnet publish ally-console/src/AllyConsole.App/AllyConsole.App.csproj -c Release -r win-x64 --self-contained true -p:PublishTrimmed=false -p:PublishSingleFile=false -p:IncludeNativeLibrariesForSelfExtract=false -p:IncludeSourceRevisionInInformationalVersion=false -p:InformationalVersion=1.0.0+<commit> -o ally-console/artifacts/win-x64
```

- Core tests: passed 57, failed 0, skipped 0. That includes settings, log rotation, the failure-log gate, shortcut conflicts, the 150 ms detection-delay bound, gesture edges, focus, brightness unsupported, sign-in launch against a temp folder, and missing Steam.
- App build: succeeded, 0 warnings, 0 errors. Output is a `net8.0-windows` assembly. It was not executed. A window was not shown.
- `dotnet publish` for `win-x64` succeeded. Output folder: `ally-console/artifacts/win-x64/` (about 162 MB, gitignored). `AllyConsole.exe` is a PE32+ x86-64 Windows GUI binary. `coreclr.dll` and `PresentationFramework.dll` are in the folder. `AllyConsole.runtimeconfig.json` lists included frameworks `Microsoft.NETCore.App` 8.0.32 and `Microsoft.WindowsDesktop.App` 8.0.32, which is a self-contained layout rather than a request to use a machine-wide shared framework.
- Trimming is off. The app project sets `PublishTrimmed` to false, and the publish command passed `-p:PublishTrimmed=false`. `PublishTrimmed` does not appear in `AllyConsole.deps.json`.
- `pwsh` and `powershell` are not installed on this agent, so `ally-console/publish/publish-win-x64.ps1` was not the process that launched. The publish command above is the one in that script, including `InformationalVersion` from the project version plus `git rev-parse` when it succeeds. `README.txt` and `build-info.txt` were then written into the output folder with the same fields the script writes (`publishTrimmed=false`, version, commit or `unavailable`). On Windows, run the script. It is the reproducible path.
- `measure-process.ps1` was not run. There is no Ally Console process here, and the script is not part of the app.

## Design, not measurements

Hidden input sleep defaults to 150 ms (`HiddenPollIntervalMs`). Settings load clamps it to 50–2000 ms, and a stored 0 becomes 150. Visible sleep defaults to 50 ms, clamped to 50–500. The loop sleeps, then calls documented `XInputGetState` (`xinput1_4.dll`, then `xinput9_1_0.dll`) for users 0–3 until one responds. A held chord is seen on the next sample, so the wait is at most one interval plus that call. The call duration was not measured. A chord that is only down between samples can be missed. The read does not hide those buttons from a game. There is no interception driver, hook, or injection.

While the panel is hidden there is no WMI and no Core Audio probe. The status loop wakes once a second and does no probe. While visible, status refresh defaults to 2000 ms and is not faster than 500 ms. None of that was timed with a profiler.

## Still needs Windows or the Ally

- Starting `AllyConsole.exe`, including the self-contained folder, and using the window with touch, mouse, keyboard, and the Ally controller.
- RegisterHotKey, focus return, exclusive fullscreen, and elevated windows.
- Real volume, mute, WMI brightness, battery, displays, and GPU names. Normal mode must not show samples. `--dev` must show the banner and must not stick.
- Steam URI launch and a missing-Steam error on a machine with and without Steam.
- Desktop mode, confirmed restart and shut down (do not confirm unless you mean to power the device), sign-in shortcut create and delete, and clean exit.
- Official ASUS XG Mobile activation, done outside this app. Adapter names are still not a rendering-GPU claim.
- Hidden-panel CPU and memory from `measure-process.ps1`, and any matched game comparison. Record those only in `docs/hardware-test-results.md`.
- Whether a per-user Startup shortcut is convenient while Big Picture covers the desktop. Explorer stays the shell.

## What the tree implements

Quick panel: volume, mute, brightness (disabled, with a reason, when `WmiMonitorBrightness` is missing), battery, labelled GPU and display rows, Windows network shortcut, Steam, desktop mode, settings, diagnostics, confirmed restart and shut down, confirmed exit. Local settings and rotating logs under `%LOCALAPPDATA%\AllyConsole`. Opt-in Startup shortcut, default off. `--dev` samples are labelled.

`FailureLogGate` writes a startup failure, a shortcut-registration failure, or an unsupported-integration failure once per event and message. The same line is not written again on the status timer or the controller poll until the message changes or the event recovers.

## Unsupported

- Display switching.
- ASUS XG Mobile control. Diagnostics say Stage 1 does not control it.
- Guide as a chord button.
- Brightness when `WmiMonitorBrightness` is absent. No EC or firmware write.
- Invented status in normal mode.
- Suspend and resume of a running game.
- Background-app changes, performance profiles, fan control, and power limits.
- An elevated helper, a Windows service, shell replacement, UAC changes, and automatic login.
- A resource sampler inside the process.

## Limitations

- Borderless games are the input target. Exclusive fullscreen often ignores `RegisterHotKey` and `SetForegroundWindow`. An elevated window can ignore both. The log records whether focus restore returned true or false. True does not prove the game took focus.
- `steam://open/bigpicture` depends on Steam’s URI handler.
- Ally internal brightness is often vendor-specific. Unsupported is a normal result.
- `Win32_VideoController` names are not the rendering GPU and are not an XG Mobile on/off state.
- Alt+F4 hides the panel. Exit is a separate confirmed action.

## Windows publish, build, and run

On Windows, with the .NET 8 SDK (the Ally does not need the SDK once the folder exists):

```bat
cd ally-console
dotnet test AllyConsole.sln
powershell -NoProfile -ExecutionPolicy Bypass -File publish\publish-win-x64.ps1
```

Run `artifacts\win-x64\AllyConsole.exe`. Samples only: `AllyConsole.exe --dev`.

Framework-dependent run from source, still on Windows:

```bat
dotnet run --project src\AllyConsole.App\AllyConsole.App.csproj
```

Timed CPU and memory, against an already running process, panel hidden. This exits by itself and does not install a service:

```bat
powershell -NoProfile -ExecutionPolicy Bypass -File publish\measure-process.ps1 -DurationSeconds 60 -IntervalSeconds 1
```

Put the CSV summary in `docs/hardware-test-results.md`. Do not paste it back into this report as if the build machine measured it.

## Physical Ally steps

Do these on the Ally. They are not done. Write each result in [hardware-test-results.md](hardware-test-results.md). Cancel any restart or shut-down prompt. Do not deactivate the XG Mobile. Do not remove Armoury Crate, AMD, or NVIDIA software.

1. Prefer the published folder. Start without `--dev`. Confirm there is no development banner and that battery, volume, and GPU rows are live readings or “Unavailable” with a reason. They must not show the 76% sample battery or “Sample GPU (development mode)”.
2. Start once with `--dev`. Confirm the banner and the “Sample:” prefix, then exit and start again without the flag. Confirm samples are gone and that `--dev` was not saved.
3. At 720p and 1080p, check that rows are readable on the 7-inch panel, focus is obvious, and touch, mouse, and the stick or D-pad all move the same focus.
4. Open and close with Left bumper + Right bumper + View (Back). Confirm the chord does not repeat while held, and that Back does not navigate until the chord is released. Confirm a game that reads XInput still sees those buttons. If a very short tap is missed, that matches the 150 ms hidden sleep; hold the chord.
5. Press Ctrl+Alt+Shift+F1. If registration failed, the panel must say so. The same failure must not flood `ally-console.log` on a timer. Try a bare key and Win+L and confirm they are rejected.
6. Move volume, toggle mute, and compare with the Windows volume flyout. If brightness is disabled, read the WMI explanation and stop.
7. Check the battery row against Windows while on battery and while charging.
8. Read the GPU and display rows. Confirm the text does not claim which GPU is rendering. Attach the XG Mobile only through the official ASUS flow if you look at names.
9. Open network settings and confirm it is Windows Settings, then return.
10. With Steam installed, launch Big Picture from the panel. With Steam missing, or with a non-`steam://` URI, confirm a clear error and that Ally Console stays open.
11. Choose Desktop mode. The desktop should appear and the tray companion should still be running. Close the panel from a borderless game and see whether focus returns. Repeat from exclusive fullscreen and from an elevated window, and expect those to fail.
12. Open Restart and Shut down, choose Cancel, and confirm nothing happens.
13. Turn on “Start Ally Console when I sign in”, sign out and back in, and confirm the companion starts. Turn it off and confirm the per-user Startup `AllyConsole.lnk` is gone. Leave “Open Steam Big Picture when Ally Console starts” off for one pass, then on for a second pass. A failed shortcut write is a log line, not a repeating poll error.
14. With the panel hidden, run `measure-process.ps1` for a timed sample. Record the CSV in the hardware sheet. Do not leave a sampler running.
15. If you compare a game, use the same settings with the app exited and with it running, panel hidden. Record that pair on the hardware sheet.
16. Exit from the panel and from the tray. Confirm Explorer is still the shell and that no new service was installed.
17. Delete `%LOCALAPPDATA%\AllyConsole` and start again. Defaults should return, including sign-in launch off.

## Recovery

Exit the companion, or delete the Startup shortcut above, or delete `%LOCALAPPDATA%\AllyConsole`. Windows does not depend on this process.
