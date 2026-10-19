# Ally Console

Stage 1 Windows companion. Spec, rules, stage list, and the Stage 1 report are in `../docs/`. This file is only build, test, run, and recovery.

The app runs as the signed-in user. It does not replace Explorer. Closing it leaves Windows usable.

## Build and test

.NET 8 SDK. On Windows, include the desktop workload.

```bat
cd ally-console
dotnet test AllyConsole.sln
dotnet build src\AllyConsole.App\AllyConsole.App.csproj
```

Core tests do not need Windows, a GPU, or ASUS software:

```bash
dotnet test tests/AllyConsole.Core.Tests/AllyConsole.Core.Tests.csproj
```

A Linux machine can type-check the WPF project (`EnableWindowsTargeting` is set) and still cannot run it. Do not treat a successful cross build as a launch.

## Run

On Windows:

```bat
dotnet run --project src\AllyConsole.App\AllyConsole.App.csproj
```

`--dev` shows labelled sample battery, volume, GPU, brightness, and display rows. It is not saved. Normal mode never uses those samples.

Defaults: controller chord is Left bumper + Right bumper + View (Back). Keyboard shortcut is Ctrl+Alt+Shift+F1. Sign-in launch and “open Steam Big Picture at startup” are off.

Settings and logs: `%LOCALAPPDATA%\AllyConsole\`.

## Recovery

- Exit from the confirm page or the tray icon. Alt+F4 only hides the panel.
- Turn off “Start Ally Console when I sign in”, or delete `AllyConsole.lnk` from the per-user Startup folder (`shell:startup`).
- Delete `%LOCALAPPDATA%\AllyConsole` to drop settings and logs.
- Do not remove ASUS, AMD, or NVIDIA software as a recovery step. This app does not install a service.
