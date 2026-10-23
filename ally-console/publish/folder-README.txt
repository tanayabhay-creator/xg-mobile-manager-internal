Ally Console — self-contained Windows x64

Run AllyConsole.exe in this folder. This PC does not need the .NET SDK.
See build-info.txt for the version and the git commit used at publish time.
publishTrimmed is false. The folder contains the app and its dependencies.

Start AllyConsole.exe for normal readings.
AllyConsole.exe --dev shows labelled samples. Do not use those samples as this PC's battery, volume, or GPU.

The window was not verified on this copy by the publish step. Hardware notes belong in docs/hardware-test-results.md after you fill them in on the Ally.

Sign-in launch stays off until you turn it on in Settings.
Exit from the Exit action or the tray icon. Alt+F4 only hides the panel.
To undo sign-in launch, turn the setting off or delete AllyConsole.lnk from the per-user Startup folder (shell:startup).
To reset settings and logs, exit the app and delete %LOCALAPPDATA%\AllyConsole.
This folder does not install a service and does not replace Explorer.
