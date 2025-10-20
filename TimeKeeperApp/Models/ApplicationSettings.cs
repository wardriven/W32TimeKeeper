using System.Collections.Generic;

namespace TimeKeeperApp.Models;

public class ApplicationSettings
{
    public List<string> TimeServers { get; set; } = new() { "pool.ntp.org", "time.google.com" };

    public int DriftAllowanceMilliseconds { get; set; } = 1000;

    public int SyncIntervalSeconds { get; set; } = 300;

    public bool AutoStartWithWindows { get; set; }
        = false;

    public bool NotificationsEnabled { get; set; } = true;

    public bool AdjustmentNotificationsEnabled { get; set; } = true;

    public ThemePreference ThemePreference { get; set; } = ThemePreference.System;
}
