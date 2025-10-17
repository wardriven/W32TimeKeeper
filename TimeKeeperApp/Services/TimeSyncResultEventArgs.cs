using System;

namespace TimeKeeperApp.Services;

public class TimeSyncResultEventArgs : EventArgs
{
    private TimeSyncResultEventArgs(bool success, string message, double driftMilliseconds, bool adjusted, string? server)
    {
        Success = success;
        Message = message;
        DriftMilliseconds = driftMilliseconds;
        TimeAdjusted = adjusted;
        Server = server;
    }

    public bool Success { get; }

    public string Message { get; }

    public double DriftMilliseconds { get; }

    public bool TimeAdjusted { get; }

    public string? Server { get; }

    public static TimeSyncResultEventArgs Success(double driftMilliseconds, bool adjusted, string? server)
        => new(true, adjusted ? "System time adjusted." : "System time within drift allowance.", driftMilliseconds, adjusted, server);

    public static TimeSyncResultEventArgs Fail(string message)
        => new(false, message, 0, false, null);
}
