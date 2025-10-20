using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TimeKeeperApp.Services;

public class TimeCheckLogger : ITimeCheckLogger
{
    private const string LogsFolderName = "logs";
    private const string FilePrefix = "timechecks-";
    private const string FileExtension = ".log";

    public Task LogAsync(DateTime timestamp, string server, double? offsetSeconds, string status, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, LogsFolderName);
        Directory.CreateDirectory(directory);

        var fileName = $"{FilePrefix}{timestamp:yyyyMMdd}{FileExtension}";
        var path = Path.Combine(directory, fileName);
        var offsetText = offsetSeconds.HasValue ? offsetSeconds.Value.ToString("0.000000", CultureInfo.InvariantCulture) : string.Empty;
        var line = $"{timestamp:O},{server},{offsetText},{status}{Environment.NewLine}";

        return WriteAsync(path, line, cancellationToken);
    }

    private static async Task WriteAsync(string path, string content, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, true);
        var bytes = Encoding.UTF8.GetBytes(content);
        await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
    }
}
