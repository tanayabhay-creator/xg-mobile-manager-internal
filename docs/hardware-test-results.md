# Ally Console hardware results

Blank on purpose. Fill this in on the ASUS ROG Ally after a real run. Do not copy design numbers (the 150 ms poll) into the result cells, and do not treat a build-machine publish as a hardware result.

| Field | Result |
| --- | --- |
| Date | |
| Tester | |
| `build-info.txt` version | |
| `build-info.txt` commit | |
| Windows build | |
| Ally model | |
| XG Mobile attached through the official ASUS tool | |

Cancel restart and shut-down prompts. Do not deactivate the XG Mobile for this sheet. Do not remove Armoury Crate, AMD, or NVIDIA software.

## Normal and `--dev` modes

| Check | Result |
| --- | --- |
| Normal start has no development banner | |
| Battery, volume, and GPU are live readings or “Unavailable” with a reason | |
| Normal mode does not show the 76% sample battery or “Sample GPU (development mode)” | |
| `--dev` shows the banner and a “Sample:” prefix | |
| After exiting `--dev`, the next normal start has no samples and did not save the flag | |

## Controller navigation and chord

| Check | Result |
| --- | --- |
| D-pad or stick, A, and B move the same focus as touch and mouse | |
| Readable at 720p on the 7-inch panel | |
| Readable at 1080p on the 7-inch panel | |
| Left bumper + Right bumper + View (Back) opens the panel | |
| The chord does not repeat while it is held | |
| Back does not navigate until the chord is released | |
| The game still receives those buttons | |
| A tap shorter than the poll gap can be missed (hold the chord if it does) | |
| Ctrl+Alt+Shift+F1 opens the panel, or the UI says registration failed | |
| A bare key and Win+L are rejected | |

## Volume, brightness, and battery

| Check | Result |
| --- | --- |
| Volume matches the Windows flyout | |
| Mute matches the Windows flyout | |
| Brightness works, or the slider stays disabled and names `WmiMonitorBrightness` | |
| Battery percent and charging match Windows on battery | |
| Battery percent and charging match Windows while charging | |

## Steam launch and focus return

| Check | Result |
| --- | --- |
| Steam installed: panel opens Big Picture at the configured URI | |
| Steam missing, or a non-`steam://` URI: clear error, app stays open | |
| Desktop mode shows the desktop and the companion keeps running | |
| Focus returns after close from a borderless game | |
| Exclusive fullscreen (expected to fail) | |
| Elevated window (expected to fail) | |

## Officially activated XG Mobile

Use only the official ASUS flow. This app does not activate or deactivate the dock.

| Check | Result |
| --- | --- |
| Adapter names listed while the dock is on | |
| Text does not claim which GPU is rendering | |
| Text does not claim an activation state | |
| No command from Ally Console changed the dock | |

## Hidden-panel resource usage

Run `ally-console/publish/measure-process.ps1` against `AllyConsole` while the panel is hidden. Paste the CSV summary here. Leave the cells empty until that script has been run on Windows.

| Check | Result |
| --- | --- |
| Duration | |
| Sample interval | |
| Working set | |
| CPU percent from the script | |
| CSV path | |

The 150 ms hidden poll is a sleep in the code. It is not a CPU measurement.

## Matched game comparisons

Same game, same settings, one pass with Ally Console exited and one pass with it running and the panel hidden. This sheet does not record frame time or thermals from the build machine.

| Game | Settings | App exited | App running, panel hidden |
| --- | --- | --- | --- |
| | | | |
| | | | |

## Startup enable and disable, clean exit, and recovery

| Check | Result |
| --- | --- |
| Sign-in launch off by default | |
| Turning it on starts the companion at the next sign-in | |
| Turning it off removes `AllyConsole.lnk` from the per-user Startup folder | |
| “Open Steam Big Picture when Ally Console starts” off, then on | |
| Exit from the panel | |
| Exit from the tray | |
| Explorer still the shell, no new service | |
| Deleting `%LOCALAPPDATA%\AllyConsole` restores defaults, including sign-in launch off | |
