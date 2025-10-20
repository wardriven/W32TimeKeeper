using System;
using System.Threading;
using System.Threading.Tasks;

namespace TimeKeeperApp.Services;

public interface ITimeCheckLogger
{
    Task LogAsync(DateTime timestamp, string server, double? offsetSeconds, string status, CancellationToken cancellationToken);
}
