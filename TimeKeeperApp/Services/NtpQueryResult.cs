using System;

namespace TimeKeeperApp.Services;

public class NtpQueryResult
{
    public NtpQueryResult(DateTime serverTimeUtc)
    {
        ServerTimeUtc = serverTimeUtc;
    }

    public DateTime ServerTimeUtc { get; }
}
