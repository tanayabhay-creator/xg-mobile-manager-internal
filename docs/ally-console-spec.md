# Ally Console specification

Stage 1 companion for Tanay Prasad. This is a lightweight, controller-first Windows app for the original ASUS ROG Ally (Z1 Extreme) and the proprietary ASUS XG Mobile RTX 3080 16 GB. It is not a generic Thunderbolt eGPU tool.

The everyday target is a SteamOS-like gaming flow on normal Windows, so official drivers stay available. Stage 1 is the companion shell only. It does not change an ISO, replace Windows files, replace Explorer, disable UAC, or configure automatic login. If Ally Console fails, Windows stays usable.

The code lives in `ally-console/` because this repository already contains the Decky XG Mobile plugin. That plugin is untouched.

## Goals

- After sign-in, Steam Big Picture can open automatically. This is opt-in and default off. A second, independent switch starts the companion itself, also default off.
- Everyday panel actions work with the Ally controller, with keyboard, touch, and mouse as fallbacks.
- A configurable controller chord opens the quick-access panel. A keyboard shortcut does the same.
- The user can adjust the supported settings and return to the game.
- Desktop mode shows the normal Windows desktop without closing the companion.
- Read-only diagnostics name the OS, battery, displays, and GPU adapters when Windows reports them.
- Settings and logs stay local. There is no telemetry.

## Non-goals

- Recreating Steam’s library, store, or overlay. The app opens Steam.
- Reliable suspend and resume of a running game.
- An invisible Windows. UAC, the secure desktop, installers, and some launchers can still require interaction.
- Replacing Explorer, disabling UAC, or automatic login.
- Display switching (Stage 3).
- An official ASUS XG Mobile workflow (Stage 3, and only as a labelled shortcut to the official tool unless a supported integration exists).
- Background-app inventory or changes (Stage 2).
- Performance profiles, power limits, fan curves, firmware, or EC writes.
- Automatic XG Mobile deactivation, including while a game is running.
- Removing or “optimizing” official ASUS, AMD, or NVIDIA components.
- Injection into games, graphics-API hooks, or anti-cheat bypass.
- A permanent elevated helper or privileged service. Stage 1 runs as the signed-in user.

## Architecture

Two projects share one solution:

- `AllyConsole.Core` (`net8.0`). Navigation, shortcut rules, settings, logs, status policy, and command construction. It has no Windows UI and is tested on Linux.
- `AllyConsole.App` (`net8.0-windows`). WPF window, XInput poll, RegisterHotKey, Core Audio volume, WMI brightness, battery and adapter probes, Startup-folder shortcut, and the tray icon. `UseWindowsForms` is only for the tray and display bounds.

Microsoft’s Windows developer FAQ (checked 2026-10-19) still supports WPF and says it is a reasonable choice when the requirements fit. The same FAQ names WinUI 3 with the Windows App SDK as the primary recommendation for new general-purpose desktop apps. WPF is the Stage 1 choice anyway: the runtime is the .NET desktop pack, not the Windows App SDK; the UI stays a thin shell over a testable core; and current docs do not make WPF the wrong tool for this companion. There is no browser runtime.

The app process is a normal user (`asInvoker`). A second launch signals `Local\AllyConsole.Show` and exits. The first process holds `Local\AllyConsole.SingleInstance`.

Status is probed when the panel is open. While the panel is hidden, a slow XInput poll stays alive so the chord still works. Hardware probes pause. The poll sleeps; it does not spin. Default intervals: 150 ms hidden, 50 ms visible, status refresh 2000 ms and never faster than 500 ms in the status loop. The floor for input is 50 ms.

`--dev` is the only path that shows sample battery, volume, GPU, brightness, or displays. The banner says they are samples. The flag is not saved. Normal mode shows “unavailable” plus the reason when a probe has not run or has failed.

## Module boundaries

| Module | Stage 1 |
| --- | --- |
| UI and navigation | WPF quick panel, settings, diagnostics, confirm, and URI editor. `FocusModel` owns focus. |
| Controller input | `ControllerGestureDecoder` in Core. `XInputReader` calls documented `XInputGetState` (`xinput1_4.dll`, then `xinput9_1_0.dll`). Users 0–3, first connected pad. No `XInputGetStateEx` ordinal 100. |
| Keyboard shortcut | `RegisterHotKey` with `MOD_NOREPEAT`. Failure is shown. The previous registration is restored when Windows still accepts it. |
| Windows settings adapters | Volume (`IAudioEndpointVolume` on the default console render endpoint), brightness (WMI only), `ms-settings:network`, `shutdown.exe`. |
| Hardware and status discovery | `GetSystemPowerStatus`, `Win32_VideoController` name and status, `EnumDisplayDevices` plus screen bounds, `RuntimeInformation.OSDescription`. |
| Steam | Builds `steam://open/bigpicture` (configurable). The Windows host checks the Steam install path in the registry and that the path exists, then `Process.Start` with the shell. |
| ASUS / XG Mobile | Text only. No activation, no deactivation, no invented flags. |
| Optimization and rollback | Not implemented. Stage 2 rules are in `project-rules.md`. |
| Diagnostics and configuration | Versioned `settings.json`. Rotating JSON-line logs. Diagnostics page is read-only. |

Failed actions use `OperationResult`. A failure has no command and does not throw to the user.

## UX

Dark slate panel (`#12151C` cards `#1C2230`, amber `#E4A15A`). No Steam, ASUS, Xbox, or Nintendo artwork. Default size 440×680, capped to the work area, docked to the right. Rows are at least 64 px with a visible focus ring. Intended for 720p and 1080p on a 7-inch screen. That layout was not checked on hardware.

Quick panel: volume, mute, brightness, battery, GPU and display status, network settings shortcut, Steam Big Picture, desktop mode, settings, diagnostics, restart, shut down, exit.

Controller: D-pad and left stick move. A confirms. B goes back. Left and right change a slider and do not move focus. A disabled slider does nothing. Direction repeats after 400 ms, then every 140 ms. Stick dead zone is 0.55. Triggers count as buttons at a byte of 128 or higher.

The chord fires on the rising edge once. Navigation is suppressed until every chord button is released, so Back inside the default chord does not also press Back.

Keyboard: arrows, Enter, Space, Tab, Escape, Backspace. Alt+F4 hides the panel. Exit is a confirmed action, also available from the tray.

Confirm screens start on Cancel. Restart and shut down use `shutdown.exe /r /t 0` and `/s /t 0` with no `/f`, and only after confirm. The executable path is `Environment.SystemDirectory\shutdown.exe`.

Closing the panel asks Windows to restore the foreground window captured before the panel opened (`SetForegroundWindow`, with `AttachThreadInput` when the threads differ). Success and failure are logged. Desktop mode hides the panel and does not restore the game.

## Settings, logs, and sign-in

`%LOCALAPPDATA%\AllyConsole\settings.json`, schema version 1, camelCase, written by temp file then move. A newer schema or unreadable JSON stays on disk (`CanPersist` false). The app uses in-memory defaults and does not overwrite that file. An invalid chord or hotkey resets to the default and surfaces a warning.

Logs: `%LOCALAPPDATA%\AllyConsole\logs\ally-console.log`. JSON lines with `ts`, `level`, `event`, `message`. Rotation is by size (default 256 KiB, 5 files including the active file). No credentials and no extra personal data.

Sign-in launch is a per-user Startup folder shortcut named `AllyConsole.lnk`. No administrator and no HKLM. Turning it off deletes that shortcut. Tests use `DescriptorStartupShortcutStore` and a temp directory. The Windows app uses `IShellLink` / `IPersistFile` and is the only writer that receives `Environment.SpecialFolder.Startup`.

`OpenSteamBigPictureOnStartup` runs only after the companion has started. It does not by itself create the Startup shortcut.

## Shortcuts

Default controller chord: Left bumper + Right bumper + View (XInput Back). It is a chord, not one face button. Ally controls are expected to appear as an XInput gamepad. That was not verified on a device.

Rejected controller chords: empty, duplicate button, duplicate of another binding, Guide, unknown name. Guide is reserved because documented `XInputGetState` does not report it reliably.

Default keyboard shortcut: Ctrl+Alt+Shift+F1. Rejected: empty, unknown key, no modifier, Win+L, Win+Tab, Ctrl+Alt+Delete, duplicate of another binding. A bare key is rejected so RegisterHotKey cannot swallow typing.

## Hardware boundaries

- Keep official ASUS, AMD, and NVIDIA software. Do not remove it from this app.
- XG Mobile stays on the official ASUS workflow. Stage 1 does not call it. Stage 3 may add a clearly labelled shortcut to the official tool. Do not invent command-line flags or activation APIs.
- Never deactivate XG Mobile automatically, and do not deactivate it while a game is running. Deactivation is not implemented.
- Do not write firmware, EC registers, power limits, fan controls, or GPU clocks. Performance profiles wait for a verified supported interface.
- Brightness uses `root\wmi` `WmiMonitorBrightness` and `WmiMonitorBrightnessMethods.WmiSetBrightness` only. If that class is missing, the slider stays disabled and explains that the app will not guess via firmware or the EC. The Ally internal panel is often vendor-specific.
- A detected GPU name is not the GPU rendering the current game, and it is not an XG Mobile activation state. The UI says so.
- Freezing and thermal shutdowns on Bazzite are not assumed fixed by Windows. This app does not add a competing hardware-control loop.
- Do not apply optimizations on a build machine, and do not claim hardware verification that was not performed.

## Background-app module (Stage 2, not built)

Required before any change: inventory first, preview before apply, each change opt-in, and a journal of what, why, dependencies, side effects, original state, and whether rollback exists. Removal is not “perfectly reversible” unless restoration is implemented. No generic “disable all services” script, registry cleaner, forced Windows Update removal, or unverified gaming tweak. Do not terminate arbitrary processes. Keep Windows security, firewall, servicing, drivers, audio, networking, Bluetooth, controller support, required ASUS and XG Mobile components, and store, licensing, and game dependencies. Change update timing only through supported settings. Do not promise that every update or restart can be suppressed.

## Uncertain integrations

Named here because Stage 1 does not prove them:

- `steam://open/bigpicture` depends on the Steam client’s URI handler. Returning focus to a game after Big Picture opens is not guaranteed.
- The XInput chord is shared. Games that also read the pad still see those buttons. Exclusive capture would need a driver or injection, which this app will not do.
- `RegisterHotKey` can fail if another process owns the chord, and exclusive fullscreen often does not deliver it. An elevated window above this app can ignore the hotkey and focus return.
- Internal-panel brightness on the Ally often needs the vendor driver. WMI may report unsupported. There is no fallback.
- With XG Mobile attached, `Win32_VideoController` can list more than one adapter. The list does not say which one is rendering.
- The official ASUS XG Mobile UI path is not hardcoded. Stage 3 adds a shortcut only after that location is known.
- A per-user Startup shortcut runs at interactive sign-in. Steam Big Picture is not the Windows shell here. Whether that shortcut is convenient when Big Picture covers the desktop was not tested. Explorer remains the shell.

## Recovery

- Exit from the confirm page or the tray. The companion closes. Windows, Steam, and the game are left as they are.
- Alt+F4 and the header Close button hide the panel. They do not exit.
- Delete `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\AllyConsole.lnk` (the per-user Startup folder) to undo sign-in launch. The in-app switch does the same.
- Delete `%LOCALAPPDATA%\AllyConsole` to drop settings and logs. The app recreates defaults on the next start.
- Do not replace Explorer. A crash of this process must not take the desktop with it. The dispatcher handler logs the exception type and message and keeps the process up when it can. A hard crash still leaves Windows usable.
- Restart and shut down are explicit and confirmed. They are not a recovery path for a failed companion.
