# W32 Time Keeper

W32 Time Keeper is a Windows desktop utility that monitors your system clock against a configurable list of network time servers and surfaces the live drift for each one. The application runs in the background, records offsets on a schedule you choose, and exposes a lightweight UI for managing servers and reviewing results.

## Features

- **Background monitoring** – Periodically queries up to five user-provided NTP servers and reports their offsets without blocking the UI.
- **Five configurable servers** – The main window now exposes five labeled input slots where you can enter host names (e.g., `time.windows.com`). Empty secondary slots are ignored automatically.
- **Configurable interval** – Choose how often the check cycle runs by setting the "Check interval (seconds)" field directly in the main view.
- **Status dashboard** – See the most recent result for each server, including the timestamp, signed offset, and success/error state.
- **Rolling log files** – Every result is appended to `logs/timechecks-YYYYMMDD.log` alongside the UI updates for later review.
- **Per-user settings** – All preferences are saved to the current user profile and applied at startup.

## Configuration Options

Use the main window to manage all configurable options:

- **Server 1–5:** Enter host names (e.g., `time.windows.com`). Only populated entries are queried; Server 1 is required.
- **Check interval (seconds):** Frequency for background checks. Values below 1 second are rejected.
- **Start/Stop buttons:** Control whether the scheduler is running.

## How It Works

1. On launch, the app loads saved settings (see below), starts the background monitor, and populates the five server slots.
2. A timer triggers periodic checks; each cycle queries the populated servers sequentially (Server 1 → Server 5).
3. The signed offset (server UTC minus system UTC) is calculated for each response and rounded to microseconds.
4. Results (success or per-server error) are shown in the UI and written to the rolling log file.
5. You can pause or resume monitoring with the buttons on the main window; updates take effect immediately and persist.

## Configuration storage

- **Settings file:** `%AppData%\W32TimeKeeper\settings.json`
- **Log directory:** `logs\timechecks-YYYYMMDD.log` (relative to the application directory)

The settings file stores the five server entries and the selected interval. It is updated automatically as fields change in the main window.

## Getting Started

1. Build the solution with dotnet build (requires .NET 8.0 on Windows with WPF/WinForms support).
2. Run the application (dotnet run or launch the generated executable).
3. Enter up to five time servers directly in the main window and set the desired interval.
4. Leave the app running to monitor and log the offset for each configured server.

## Testing

- dotnet build
- dotnet test *(if/when tests are added)*

---

Contributions and feedback are welcome! Submit issues or pull requests via GitHub to help improve W32 Time Keeper.
