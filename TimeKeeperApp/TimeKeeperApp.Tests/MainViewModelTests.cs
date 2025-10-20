using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TimeKeeperApp.Models;
using TimeKeeperApp.Services;
using TimeKeeperApp.ViewModels;
using Xunit;

namespace TimeKeeperApp.Tests;

public class MainViewModelTests
{
    [Fact]
    public async Task PrimaryServerMustBeProvided()
    {
        var settings = new TimeCheckSettings
        {
            IntervalSeconds = 10,
            Servers = new List<string> { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty }
        };

        using var harness = CreateHarness(settings);
        await harness.ViewModel.InitializeAsync();

        Assert.Equal("Primary time server required.", harness.ViewModel.Servers[0].Error);
        Assert.False(harness.ViewModel.CanStart);
    }

    [Fact]
    public async Task IntervalValidationRequiresPositiveValue()
    {
        var settings = new TimeCheckSettings
        {
            IntervalSeconds = 5,
            Servers = new List<string> { "time.test", string.Empty, string.Empty, string.Empty, string.Empty }
        };

        using var harness = CreateHarness(settings);
        await harness.ViewModel.InitializeAsync();

        harness.ViewModel.CheckIntervalSeconds = 0;

        Assert.Equal("Interval must be 1 second or greater.", harness.ViewModel.IntervalError);
        Assert.False(harness.ViewModel.CanStart);
    }

    [Fact]
    public async Task EmptyServersAreSkipped()
    {
        var settings = new TimeCheckSettings
        {
            IntervalSeconds = 5,
            Servers = new List<string> { "primary.test", string.Empty, "secondary.test", string.Empty, string.Empty }
        };

        using var harness = CreateHarness(settings);
        await harness.ViewModel.InitializeAsync();

        Assert.Equal(2, harness.ViewModel.ServerStatuses.Count);
        Assert.All(harness.ViewModel.ServerStatuses, status => Assert.False(string.IsNullOrWhiteSpace(status.Server)));
    }

    [Fact]
    public async Task OffsetCalculationUsesServerTimeDifference()
    {
        var settings = new TimeCheckSettings
        {
            IntervalSeconds = 5,
            Servers = new List<string> { "primary.test", string.Empty, string.Empty, string.Empty, string.Empty }
        };

        var fakeClock = new FakeClock
        {
            Now = new DateTime(2024, 1, 1, 8, 0, 0),
            UtcNow = new DateTime(2024, 1, 1, 13, 0, 0, DateTimeKind.Utc)
        };

        var ntpResponse = fakeClock.UtcNow.AddSeconds(1.234567);
        var fakeClient = new FakeNtpClient(new Dictionary<string, DateTime>
        {
            ["primary.test"] = ntpResponse
        });

        using var harness = CreateHarness(settings, fakeClient, fakeClock);
        await harness.ViewModel.InitializeAsync();

        await harness.ViewModel.StartAsync();
        harness.ViewModel.Stop();

        var status = Assert.Single(harness.ViewModel.ServerStatuses);
        Assert.Equal(1.234567, status.OffsetSeconds);
        Assert.Equal("Success", status.StatusMessage);
    }

    private static ViewModelHarness CreateHarness(
        TimeCheckSettings settings,
        INtpClient? ntpClient = null,
        ISystemClock? clock = null)
    {
        var fakeSettings = new FakeSettingsStore(settings);
        var fakeLogger = new FakeLogger();
        var syncContext = new TestSynchronizationContext();
        var viewModel = new MainViewModel(
            ntpClient ?? new FakeNtpClient(new Dictionary<string, DateTime>()),
            fakeSettings,
            fakeLogger,
            clock ?? new FakeClock(),
            syncContext);

        return new ViewModelHarness(viewModel, fakeLogger);
    }

    private sealed class ViewModelHarness : IDisposable
    {
        public ViewModelHarness(MainViewModel viewModel, FakeLogger logger)
        {
            ViewModel = viewModel;
            Logger = logger;
        }

        public MainViewModel ViewModel { get; }

        public FakeLogger Logger { get; }

        public void Dispose()
        {
            ViewModel.Dispose();
        }
    }

    private sealed class FakeSettingsStore : ISettingsStore
    {
        private readonly TimeCheckSettings _settings;

        public FakeSettingsStore(TimeCheckSettings settings)
        {
            _settings = new TimeCheckSettings
            {
                IntervalSeconds = settings.IntervalSeconds,
                Servers = settings.Servers.ToList()
            };
        }

        public TimeCheckSettings Load()
        {
            return new TimeCheckSettings
            {
                IntervalSeconds = _settings.IntervalSeconds,
                Servers = _settings.Servers.ToList()
            };
        }

        public void Save(TimeCheckSettings settings)
        {
            _settings.IntervalSeconds = settings.IntervalSeconds;
            _settings.Servers = settings.Servers.ToList();
        }
    }

    private sealed class FakeNtpClient : INtpClient
    {
        private readonly Dictionary<string, DateTime> _responses;

        public FakeNtpClient(Dictionary<string, DateTime> responses)
        {
            _responses = responses;
        }

        public Task<NtpQueryResult> QueryAsync(string host, int timeoutMilliseconds, CancellationToken cancellationToken)
        {
            if (_responses.TryGetValue(host, out var response))
            {
                return Task.FromResult(new NtpQueryResult(response));
            }

            throw new NtpClientException($"No response configured for {host}.");
        }
    }

    private sealed class FakeLogger : ITimeCheckLogger
    {
        public List<(DateTime Timestamp, string Server, double? Offset, string Status)> Entries { get; } = new();

        public Task LogAsync(DateTime timestamp, string server, double? offsetSeconds, string status, CancellationToken cancellationToken)
        {
            Entries.Add((timestamp, server, offsetSeconds, status));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeClock : ISystemClock
    {
        public DateTime UtcNow { get; set; } = DateTime.UtcNow;

        public DateTime Now { get; set; } = DateTime.Now;
    }

    private sealed class TestSynchronizationContext : SynchronizationContext
    {
        public override void Send(SendOrPostCallback d, object? state)
        {
            d(state);
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            d(state);
        }
    }
}
