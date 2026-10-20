# Ally Console project rules

Rules for anyone changing this companion. Stage 1 code is in `ally-console/`. The Decky plugin elsewhere in the repository is a separate product; do not mix the two.

## Product boundary

- Ally Console is a companion and a reversible configuration package. Do not modify an ISO or replace Windows system files.
- Do not replace Explorer as the shell. Do not disable UAC. Do not configure automatic login.
- If the companion fails, Windows must still be usable.
- Do not promise an invisible Windows. UAC, the secure desktop, installers, and some launchers may need a person.
- Use Steam for the library, store, and existing Steam features. Do not recreate Steam.
- Reliable suspend and resume of a running game is out of scope.
- The device target is the original ASUS ROG Ally Z1 Extreme and the proprietary ASUS XG Mobile RTX 3080 16 GB. Do not treat that dock as a generic Thunderbolt eGPU.

## Process and privilege

- Stage 1 runs as the signed-in user. Do not add an elevated helper or a permanent privileged service to hit a Stage 1 gap.
- Opt-in launch at sign-in stays a per-user Startup folder shortcut. No administrator, no HKLM. Default off, and undo must keep working.
- Tests that plan a Startup shortcut take a caller-supplied directory. They must not write `SpecialFolder.Startup`.
- Restart and shut down go through supported Windows APIs or commands, with confirmation. Do not force-close other programs (`shutdown.exe` without `/f`).

## Hardware

- Retain official ASUS, AMD, and NVIDIA components. Do not remove or “optimize” them from this app.
- XG Mobile starts from the official ASUS workflow only. Do not invent command-line flags, activation APIs, or undocumented control methods.
- If a direct integration does not exist, add a clearly labelled shortcut to the official tool. Do not guess the tool’s path.
- Never automatically deactivate the XG Mobile. Do not deactivate it while a game is running. Do not implement deactivation until that rule is explicit in the design and the user action is manual.
- Do not write firmware, EC registers, power limits, fan controls, or GPU settings by speculative methods.
- Performance profiles are allowed only behind a verified supported interface, in a later stage.
- The owner has seen freezing and thermal shutdowns on Bazzite. Do not add a competing hardware-control loop. Do not assume Windows fixes an unresolved hardware fault.
- Do not apply optimizations on a build or CI machine. Do not claim Ally CPU, frame-time, or thermal results that were not measured on that hardware.
- Brightness is the documented WMI monitor class only. If it is unsupported, disable the control and explain why. Do not fall back to EC or firmware writes.
- GPU and display rows are diagnostics. Do not label a detected adapter as the one rendering the current game, or as an XG Mobile activation state.

## Input

- Do not inject into games, hook graphics APIs, or bypass anti-cheat.
- The compatibility claim is borderless games. Document exclusive fullscreen and elevated-window limits. Do not claim universal input capture.
- XInput is shared. A chord must not be a single in-game button by default. Do not call undocumented XInput ordinals to read Guide.
- Keyboard shortcuts need a modifier. Reject empty chords, duplicate buttons, duplicate bindings, and reserved Windows chords (Guide, Win+L, Win+Tab, Ctrl+Alt+Delete).
- If `RegisterHotKey` fails, say so in the UI.
- When the panel closes, restore focus with a supported window API. Log failure. Do not send the panel’s navigation into the game when the panel is the foreground window.
- A hidden panel may poll XInput slowly. Pause hardware probes that are unnecessary while hidden. Do not busy-spin. Keep a minimum sleep (50 ms in Stage 1). The default hidden sleep is 150 ms. Do not add an interception driver, hook, or game injection to shorten that delay or to hide the chord from the game.

## Data

- Settings are local JSON with `schemaVersion`. A newer or corrupt file stays on disk and must not be overwritten.
- Logs are structured, local, and rotated, under the user-writable app data location.
- Do not log the same startup failure, shortcut-registration failure, or unsupported-integration failure on every poll or status refresh. Log it when it starts or the message changes, and again only after it recovers.
- No telemetry. Do not collect credentials or unnecessary personal data.
- Do not sample CPU or memory inside the companion. A timed reading is an external script run by a person, and it must exit. Do not install a monitoring service. Do not present the 150 ms poll as a measured CPU result.
- Unsupported features are disabled with an explanation.
- Mock battery, GPU, volume, brightness, or displays appear only in `--dev`, behind the visible development banner. Normal mode shows unavailable plus the reason.

## Background apps (Stage 2)

Do not implement this stage early, and do not land a generic cleanup script ahead of it.

- Inventory first. Preview before apply. Each change is opt-in.
- Journal what changed, why, dependencies, side effects, original state, and whether rollback exists.
- Do not call removal perfectly reversible unless restoration is implemented.
- No “disable all services” script, registry cleaner, forced Windows Update removal, or unverified gaming tweak.
- Do not terminate arbitrary processes.
- Preserve Windows security, firewall, servicing, drivers, audio, networking, Bluetooth, controller support, required ASUS and XG Mobile components, and store, licensing, and game dependencies.
- Change update timing only through supported settings. Do not promise that every update or restart can be suppressed.

## Engineering

- Default stack: C# / .NET 8, `AllyConsole.Core` plus WPF `AllyConsole.App`, tests in `AllyConsole.Core.Tests`.
- Stay on WPF unless current Microsoft docs make it the wrong choice. If you switch, write the reason in `docs/ally-console-spec.md`. WinUI 3 is Microsoft’s primary recommendation for new general-purpose apps; WPF remains supported and is chosen here for a smaller runtime and a testable core.
- No browser runtime.
- Keep UI, controller input, Windows settings adapters, hardware discovery, Steam and ASUS integration, optimization and rollback, and diagnostics in separate modules.
- Actions that can fail return `OperationResult`. Failure carries no command string.
- UI strings and docs for humans are English.
- Do not copy proprietary Steam, ASUS, Xbox, or Nintendo artwork.
- If the WPF project cannot run on a Linux agent, say so. Do not fake a successful launch. Core tests must still pass.
