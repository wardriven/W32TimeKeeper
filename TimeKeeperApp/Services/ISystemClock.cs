using System;

namespace TimeKeeperApp.Services;

public interface ISystemClock
{
    DateTime UtcNow { get; }
    DateTime Now { get; }
}
