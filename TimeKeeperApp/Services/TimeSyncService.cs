using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TimeKeeperApp.Models;

namespace TimeKeeperApp.Services;

public class TimeSyncService : IDisposable
{
    private readonly Func<ApplicationSettings> _settingsProvider;
    private readonly SystemTimeAdjuster _systemTimeAdjuster;
    private readonly Timer _timer;
    private readonly object _syncLock = new();
    private bool _disposed;

    public TimeSyncService(Func<ApplicationSettings> settingsProvider, SystemTimeAdjuster systemTimeAdjuster)
    {
        _settingsProvider = settingsProvider;
        _systemTimeAdjuster = systemTimeAdjuster;
        _timer = new Timer(OnTimerElapsed, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public event EventHandler<TimeSyncResultEventArgs>? SyncResult;

    public void Start()
    {
        _timer.Change(TimeSpan.Zero, TimeSpan.FromMinutes(5));
    }

    public void Stop()
    {
        _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    private void OnTimerElapsed(object? state)
    {
        _ = PerformSyncAsync();
    }

    public async Task PerformSyncAsync()
    {
        if (!Monitor.TryEnter(_syncLock))
        {
            return;
        }

        try
        {
            var settings = _settingsProvider();
            if (settings.TimeServers == null || settings.TimeServers.Count == 0)
            {
                settings.TimeServers = new ApplicationSettings().TimeServers;
            }

            DateTime? networkTime = null;
            string? serverUsed = null;

            foreach (var server in settings.TimeServers.Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                networkTime = await NtpClient.GetNetworkTimeAsync(server.Trim());
                if (networkTime.HasValue)
                {
                    serverUsed = server;
                    break;
                }
            }

            if (!networkTime.HasValue)
            {
                SyncResult?.Invoke(this, TimeSyncResultEventArgs.Fail("Unable to reach any configured time server."));
                return;
            }

            var currentUtc = DateTime.UtcNow;
            var drift = (networkTime.Value - currentUtc).TotalMilliseconds;
            var driftAbs = Math.Abs(drift);
            var allowance = Math.Max(0, settings.DriftAllowanceMilliseconds);

            if (driftAbs <= allowance)
            {
                SyncResult?.Invoke(this, TimeSyncResultEventArgs.Success(drift, false, serverUsed));
                return;
            }

            var adjusted = _systemTimeAdjuster.TryApply(networkTime.Value, out var errorMessage);
            if (adjusted)
            {
                SyncResult?.Invoke(this, TimeSyncResultEventArgs.Success(drift, true, serverUsed));
            }
            else
            {
                SyncResult?.Invoke(this, TimeSyncResultEventArgs.Fail($"Failed to adjust system time: {errorMessage}"));
            }
        }
        catch (Exception ex)
        {
            SyncResult?.Invoke(this, TimeSyncResultEventArgs.Fail($"Unexpected error: {ex.Message}"));
        }
        finally
        {
            Monitor.Exit(_syncLock);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _timer.Dispose();
        _disposed = true;
    }
}
