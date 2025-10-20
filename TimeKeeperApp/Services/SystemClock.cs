using System;

namespace TimeKeeperApp.Services;

public class SystemClock : ISystemClock
{
    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime Now => DateTime.Now;
}
