# W32 Time Keeper

W32 Time Keeper is a Windows 11 desktop application that monitors the local system clock and keeps it aligned with user-configurable NTP servers. When the configured drift allowance is exceeded the application automatically corrects the clock and can optionally notify the user about the change.

## Features

- Monitor and synchronise the Windows system clock against a configurable list of time servers.
- Set a drift allowance (in milliseconds) to control when the clock will be corrected.
- Optional toast-style notifications whenever the system time is adjusted or a sync failure occurs.
- Toggle automatic start-up with Windows (using the current user `Run` registry key).
- Manual "Sync Now" option and continuous background synchronisation every five minutes.
- Minimises to the system tray while continuing to operate.

## Getting started

1. Open the solution `W32TimeKeeper.sln` in Visual Studio 2022 (or newer) on Windows 11.
2. Build the solution in the desired configuration (Debug or Release).
3. Run the `TimeKeeperApp` project.

> **Note**
> Adjusting the system clock requires administrative privileges. Launch the application with elevated rights for the automatic adjustments to succeed.

## Configuration files

Application preferences (time servers, drift allowance, notification settings, auto-start preference) are stored in `%APPDATA%\\W32TimeKeeper\\settings.json` and are automatically created/updated by the app.
