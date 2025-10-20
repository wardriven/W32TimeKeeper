using System.Collections.Generic;

namespace TimeKeeperApp.Models;

public class TimeCheckSettings
{
    public List<string> Servers { get; set; } = new() { "time.windows.com", "pool.ntp.org", string.Empty, string.Empty, string.Empty };

    public int IntervalSeconds { get; set; } = 60;
}
