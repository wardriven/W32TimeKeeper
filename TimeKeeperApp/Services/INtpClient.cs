using System.Threading;
using System.Threading.Tasks;

namespace TimeKeeperApp.Services;

public interface INtpClient
{
    Task<NtpQueryResult> QueryAsync(string host, int timeoutMilliseconds, CancellationToken cancellationToken);
}
