using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TimeKeeperApp.Models;
using TimeKeeperApp.Services;

namespace TimeKeeperApp.ViewModels;

public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private const int MinimumIntervalSeconds = 1;
    private const int DefaultTimeoutMilliseconds = 3000;
    private static readonly Regex HostnamePattern = new("^[A-Za-z0-9.-]+$", RegexOptions.Compiled);

    private readonly INtpClient _ntpClient;
    private readonly ISettingsStore _settingsStore;
    private readonly ITimeCheckLogger _logger;
    private readonly ISystemClock _clock;
    private readonly SynchronizationContext _uiContext;

    private PeriodicTimer? _timer;
    private CancellationTokenSource? _loopCancellation;
    private bool _cycleInProgress;
    private bool _disposed;
    private bool _initializing;

    private int _checkIntervalSeconds = 60;
    private string? _intervalError;
    private string? _globalStatusMessage;
    private string? _globalWarningMessage;
    private DateTime? _lastOverallCheckTime;
    private bool _isRunning;

    public MainViewModel()
        : this(new NtpClient(), new SettingsStore(), new TimeCheckLogger(), new SystemClock(), SynchronizationContext.Current)
    {
    }

    public MainViewModel(
        INtpClient ntpClient,
        ISettingsStore settingsStore,
        ITimeCheckLogger logger,
        ISystemClock clock,
        SynchronizationContext? synchronizationContext)
    {
        _ntpClient = ntpClient;
        _settingsStore = settingsStore;
        _logger = logger;
        _clock = clock;
        _uiContext = synchronizationContext ?? new SynchronizationContext();

        Servers = new ObservableCollection<ServerEntry>();
        ServerStatuses = new ObservableCollection<ServerStatus>();

        for (var i = 0; i < 5; i++)
        {
            var entry = new ServerEntry(i);
            entry.PropertyChanged += OnServerEntryPropertyChanged;
            Servers.Add(entry);
        }
    }

    public ObservableCollection<ServerEntry> Servers { get; }

    public ObservableCollection<ServerStatus> ServerStatuses { get; }

    public int CheckIntervalSeconds
    {
        get => _checkIntervalSeconds;
        set
        {
            if (_checkIntervalSeconds == value)
            {
                return;
            }

            _checkIntervalSeconds = value;
            OnPropertyChanged();
            ValidateInterval();
            PersistSettings();
            RestartTimerIfRunning();
        }
    }

    public string? IntervalError
    {
        get => _intervalError;
        private set
        {
            if (_intervalError == value)
            {
                return;
            }

            _intervalError = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasValidationErrors));
            OnPropertyChanged(nameof(CanStart));
        }
    }

    public string? GlobalStatusMessage
    {
        get => _globalStatusMessage;
        private set
        {
            if (_globalStatusMessage == value)
            {
                return;
            }

            _globalStatusMessage = value;
            OnPropertyChanged();
        }
    }

    public string? GlobalWarningMessage
    {
        get => _globalWarningMessage;
        private set
        {
            if (_globalWarningMessage == value)
            {
                return;
            }

            _globalWarningMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasGlobalWarning));
        }
    }

    public DateTime? LastOverallCheckTime
    {
        get => _lastOverallCheckTime;
        private set
        {
            if (_lastOverallCheckTime == value)
            {
                return;
            }

            _lastOverallCheckTime = value;
            OnPropertyChanged();
        }
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (_isRunning == value)
            {
                return;
            }

            _isRunning = value;
            OnPropertyChanged();
        }
    }

    public bool HasValidationErrors => Servers.Any(s => !string.IsNullOrWhiteSpace(s.Error)) || !string.IsNullOrWhiteSpace(IntervalError);

    public bool CanStart => !HasValidationErrors;

    public bool HasGlobalWarning => !string.IsNullOrEmpty(GlobalWarningMessage);

    public event PropertyChangedEventHandler? PropertyChanged;

    public Task InitializeAsync()
    {
        _initializing = true;
        try
        {
            var settings = _settingsStore.Load();
            if (settings.Servers.Count < 5)
            {
                while (settings.Servers.Count < 5)
                {
                    settings.Servers.Add(string.Empty);
                }
            }

            for (var i = 0; i < Servers.Count; i++)
            {
                Servers[i].Hostname = settings.Servers.ElementAtOrDefault(i) ?? string.Empty;
            }

            CheckIntervalSeconds = Math.Max(settings.IntervalSeconds, MinimumIntervalSeconds);
            ValidateAllServers();
            ValidateInterval();
            UpdateServerStatuses();
        }
        finally
        {
            _initializing = false;
        }

        return Task.CompletedTask;
    }

    public async Task StartAsync()
    {
        if (HasValidationErrors)
        {
            GlobalStatusMessage = "Cannot start checks until validation errors are resolved.";
            return;
        }

        if (IsRunning)
        {
            return;
        }

        _loopCancellation = new CancellationTokenSource();
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(CheckIntervalSeconds));
        RunOnUiThread(() =>
        {
            IsRunning = true;
            GlobalStatusMessage = "Monitoring started.";
        });

        try
        {
            await PerformCheckCycleAsync(_loopCancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation during the initial run
        }

        _ = RunPeriodicChecksAsync(_loopCancellation.Token);
    }

    public void Stop()
    {
        _loopCancellation?.Cancel();
        _loopCancellation?.Dispose();
        _loopCancellation = null;

        _timer?.Dispose();
        _timer = null;

        RunOnUiThread(() =>
        {
            if (IsRunning)
            {
                GlobalStatusMessage = "Monitoring stopped.";
            }

            IsRunning = false;
        });
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        foreach (var server in Servers)
        {
            server.PropertyChanged -= OnServerEntryPropertyChanged;
        }
        _disposed = true;
    }

    private async Task RunPeriodicChecksAsync(CancellationToken cancellationToken)
    {
        var timer = _timer;
        if (timer is null)
        {
            return;
        }

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                if (_cycleInProgress)
                {
                    continue;
                }

                await PerformCheckCycleAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping
        }
        finally
        {
            timer.Dispose();
            if (ReferenceEquals(_timer, timer))
            {
                _timer = null;
            }

            RunOnUiThread(() => IsRunning = false);
        }
    }

    private async Task PerformCheckCycleAsync(CancellationToken cancellationToken)
    {
        if (_cycleInProgress)
        {
            return;
        }

        _cycleInProgress = true;
        try
        {
            var activeServers = Servers.Where(s => !string.IsNullOrWhiteSpace(s.Hostname)).OrderBy(s => s.Index).ToList();
            UpdateServerStatuses();

            if (activeServers.Count == 0)
            {
                RunOnUiThread(() =>
                {
                    GlobalWarningMessage = "No servers configured.";
                    GlobalStatusMessage = "Checks skipped.";
                });
                return;
            }

            var allFailed = true;

            foreach (var entry in activeServers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var status = GetStatusForSlot(entry.Index);
                if (status is null)
                {
                    continue;
                }

                UpdateStatus(status, entry.Hostname, null, null, "Checking...", false);

                try
                {
                    var result = await _ntpClient.QueryAsync(entry.Hostname, DefaultTimeoutMilliseconds, cancellationToken)
                        .ConfigureAwait(false);
                    var serverUtc = result.ServerTimeUtc;
                    var systemUtc = _clock.UtcNow;
                    var offset = Math.Round((serverUtc - systemUtc).TotalSeconds, 6);
                    var now = _clock.Now;

                    UpdateStatus(status, entry.Hostname, now, offset, "Success", false);
                    await _logger.LogAsync(now, entry.Hostname, offset, "Success", cancellationToken).ConfigureAwait(false);
                    allFailed = false;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    var now = _clock.Now;
                    var message = $"Error: {ex.Message}";
                    UpdateStatus(status, entry.Hostname, now, null, message, true);
                    await _logger.LogAsync(now, entry.Hostname, null, message, cancellationToken).ConfigureAwait(false);
                }
            }

            var completedAt = _clock.Now;
            RunOnUiThread(() =>
            {
                LastOverallCheckTime = completedAt;
                GlobalStatusMessage = $"Last check completed at {completedAt:G}.";
                GlobalWarningMessage = allFailed ? "All servers failed during the last cycle." : null;
            });
        }
        finally
        {
            _cycleInProgress = false;
        }
    }

    private void UpdateStatus(ServerStatus status, string server, DateTime? lastChecked, double? offsetSeconds, string message, bool hasError)
    {
        RunOnUiThread(() =>
        {
            status.Server = server;
            status.LastChecked = lastChecked;
            status.OffsetSeconds = offsetSeconds;
            status.StatusMessage = message;
            status.HasError = hasError;
        });
    }

    private void OnServerEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not ServerEntry entry)
        {
            return;
        }

        if (e.PropertyName == nameof(ServerEntry.Hostname))
        {
            ValidateServerEntry(entry);
            UpdateServerStatuses();
            PersistSettings();
            if (!_initializing && entry.Index == 0 && !string.IsNullOrWhiteSpace(entry.Error))
            {
                Stop();
                RunOnUiThread(() => GlobalStatusMessage = "Primary time server required before monitoring can start.");
            }
        }
    }

    private void ValidateAllServers()
    {
        foreach (var server in Servers)
        {
            ValidateServerEntry(server);
        }
    }

    private void ValidateServerEntry(ServerEntry entry)
    {
        if (entry.Index == 0 && string.IsNullOrWhiteSpace(entry.Hostname))
        {
            entry.Error = "Primary time server required.";
        }
        else if (string.IsNullOrWhiteSpace(entry.Hostname))
        {
            entry.Error = null;
        }
        else if (!HostnamePattern.IsMatch(entry.Hostname))
        {
            entry.Error = "Invalid hostname. Use letters, numbers, dots, and hyphens only.";
        }
        else
        {
            entry.Error = null;
        }

        OnPropertyChanged(nameof(HasValidationErrors));
        OnPropertyChanged(nameof(CanStart));
    }

    private void ValidateInterval()
    {
        IntervalError = CheckIntervalSeconds < MinimumIntervalSeconds
            ? "Interval must be 1 second or greater."
            : null;
    }

    private void UpdateServerStatuses()
    {
        var current = ServerStatuses.ToDictionary(s => s.SlotIndex);
        var updated = new List<ServerStatus>();

        foreach (var entry in Servers.OrderBy(s => s.Index))
        {
            if (string.IsNullOrWhiteSpace(entry.Hostname))
            {
                continue;
            }

            if (current.TryGetValue(entry.Index, out var status))
            {
                if (!string.Equals(status.Server, entry.Hostname, StringComparison.OrdinalIgnoreCase))
                {
                    status.Server = entry.Hostname;
                    status.LastChecked = null;
                    status.OffsetSeconds = null;
                    status.StatusMessage = "Not checked";
                    status.HasError = false;
                }

                updated.Add(status);
            }
            else
            {
                updated.Add(new ServerStatus
                {
                    SlotIndex = entry.Index,
                    Server = entry.Hostname,
                    StatusMessage = "Not checked",
                    HasError = false
                });
            }
        }

        RunOnUiThread(() =>
        {
            ServerStatuses.Clear();
            foreach (var status in updated.OrderBy(s => s.SlotIndex))
            {
                ServerStatuses.Add(status);
            }
        });
    }

    private ServerStatus? GetStatusForSlot(int index)
    {
        return ServerStatuses.FirstOrDefault(s => s.SlotIndex == index);
    }

    private void PersistSettings()
    {
        if (_initializing || CheckIntervalSeconds < MinimumIntervalSeconds)
        {
            return;
        }

        var settings = new TimeCheckSettings
        {
            IntervalSeconds = CheckIntervalSeconds,
            Servers = Servers.Select(s => s.Hostname).Take(5).ToList()
        };

        _settingsStore.Save(settings);
    }

    private void RestartTimerIfRunning()
    {
        if (!IsRunning || _timer is null || !string.IsNullOrWhiteSpace(IntervalError))
        {
            return;
        }

        Stop();
        _ = StartAsync();
    }

    private void RunOnUiThread(Action action)
    {
        if (SynchronizationContext.Current == _uiContext)
        {
            action();
        }
        else
        {
            _uiContext.Send(_ => action(), null);
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
