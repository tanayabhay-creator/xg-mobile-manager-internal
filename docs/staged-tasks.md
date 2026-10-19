# Ally Console staged tasks

Checkboxes match the tree in `ally-console/` as of Stage 1. Later stages are not started. Physical checks stay open because this change was not run on an Ally or on Windows.

## Stage 1 — companion vertical slice

- [x] `AllyConsole.Core` (`net8.0`) with navigation, shortcut rules, versioned settings, rotating logs, status policy, and failed `OperationResult` values that carry no command.
- [x] WPF `AllyConsole.App` (`net8.0-windows`) for the quick panel, settings, diagnostics, confirm, and URI pages. Controller, keyboard, touch, and mouse input are implemented. The project compiles on the Linux agent against the Windows desktop reference assemblies. The process was not launched.
- [x] Steam action builds `steam://open/bigpicture` (configurable). A missing or invalid URI, or a missing Steam install, returns an error and does not throw.
- [x] Quick panel: volume and mute, brightness (disabled with a reason when `WmiMonitorBrightness` is unavailable), battery and charging status.
- [x] Read-only GPU adapter names and display connection rows, labelled so they are not treated as the rendering GPU or as XG Mobile state.
- [x] Network row opens `ms-settings:network`.
- [x] Desktop mode asks the shell to show the desktop and leaves the companion running. Clean exit is a confirmed action and a tray item.
- [x] Restart and shut down ask for confirmation, then call `shutdown.exe /r /t 0` or `/s /t 0` with no `/f`. Those commands were not executed on the build machine.
- [x] Controller chord and keyboard shortcut, including empty, duplicate, and reserved-clash rejection. Defaults: Left bumper + Right bumper + View (Back), and Ctrl+Alt+Shift+F1.
- [x] Local `settings.json` schema 1. Rotating JSON logs under the user-writable app data folder. No telemetry.
- [x] Opt-in per-user Startup shortcut, default off, with undo. Tests write only to a temp directory.
- [x] `--dev` sample data is labelled. Normal mode does not invent battery, volume, or GPU numbers.
- [x] `dotnet test` on Core: 50 passed, 0 failed (SDK 8.0.426).
- [ ] Run `AllyConsole.exe` on Windows and exercise the window.
- [ ] Physical Ally checks in `docs/stage-1-report.md`.

## Stage 2 — background apps

Not implemented. No service, startup, or app-removal change is in this tree, and none was applied on the build machine.

- [ ] Inventory installed and startup components before any change.
- [ ] Preview every change and require an individual opt-in.
- [ ] Journal what, why, dependencies, side effects, original state, and whether rollback exists.
- [ ] Implement restoration before calling a removal reversible.
- [ ] Change update timing only through supported settings.
- [ ] Preserve Windows security, firewall, servicing, drivers, audio, networking, Bluetooth, controller support, required ASUS and XG Mobile components, and store, licensing, and game dependencies.

## Stage 3 — displays and official XG Mobile shortcut

- [ ] Display switching through a supported Windows API, with the current target labelled.
- [ ] A clearly labelled shortcut to the official ASUS XG Mobile tool, after its location is known.
- [ ] No invented activation flags and no automatic deactivation, including while a game is running.

## Stage 4 — later performance and focus work

- [ ] Performance profiles only through a verified supported interface. No EC, firmware, or speculative GPU writes.
- [ ] Stronger return-to-game behavior that still does not inject, hook graphics APIs, or bypass anti-cheat.
- [ ] Sign-in and Big Picture polish that still leaves Explorer as the shell.
