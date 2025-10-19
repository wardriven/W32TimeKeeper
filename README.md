# W32 Time Keeper

W32 Time Keeper is a Windows desktop utility that monitors your system clock against a configurable list of network time servers and keeps it synchronized without relying on the built-in w32tm service. The application runs in the background, informs you when adjustments are made, and exposes a lightweight UI for manual syncs and configuration.

## Features

- **Background monitoring** – Periodically queries multiple NTP servers and adjusts the Windows system clock when drift exceeds your configured allowance.
- **Manual sync** – Trigger an on-demand synchronization from the main window at any time.
- **Start/Stop monitoring** – Use the toggle button to pause background checks or resume them as needed.
- **Flexible server list** – Add or remove time servers directly from the UI; changes are persisted automatically.
- **Tray integration** – Minimizes to the system tray with balloon notifications and restores on double-click.
- **Configurable notifications** – Fine-tune notifications with separate controls for adjustment alerts and an overall mute.
- **Windows auto-start** – Enable the app to launch automatically when Windows starts.
- **Per-user settings** – All preferences are saved to the current user profile and applied at startup.

## Configuration Options

Open the **Settings** dialog to manage all configurable options:

- **Drift allowance (ms):** Maximum drift tolerated before the clock is corrected.
- **Check interval (s):** Frequency for background sync checks.
- **Auto start with Windows:** Toggle to register/unregister the app as a startup program.
- **Enable all notifications:** Master switch for tray notifications.
- **Notify on time adjustment:** Optional alert shown only when the clock is adjusted.
- **Time server list:** Manage servers from the main window using *Add* and *Remove Selected*.

## How It Works

1. On launch, the app loads saved settings, starts the background monitor, and populates the server list.
2. A timer triggers periodic checks; each check queries your servers (first successful response wins).
3. When a valid network time is retrieved, the app compares it to the local clock. If the drift exceeds your allowance, it attempts to adjust the system time.
4. Results (success, error, server used) are shown in the UI and optionally as tray notifications.
5. You can manually toggle monitoring, run one-off syncs, or update settings at any time. Changes take effect immediately and are stored for future sessions.

## Getting Started

1. Build the solution with dotnet build (requires .NET 8.0 on Windows with WPF/WinForms support).
2. Run the application (dotnet run or launch the generated executable).
3. Configure your desired servers and thresholds via the **Settings** button.
4. Leave the app running (minimized to tray) to keep your system clock aligned.

## Testing

- dotnet build
- dotnet test *(if/when tests are added)*

---

Contributions and feedback are welcome! Submit issues or pull requests via GitHub to help improve W32 Time Keeper.
