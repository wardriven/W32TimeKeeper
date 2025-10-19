using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using TimeKeeperApp.Models;
using TimeKeeperApp.Services;
using MessageBox = System.Windows.MessageBox;

namespace TimeKeeperApp;

public partial class MainWindow : Window
{
    private readonly SettingsService _settingsService = new();
    private readonly AutoStartService _autoStartService = new();
    private readonly SystemTimeAdjuster _systemTimeAdjuster = new();
    private readonly NotifyIcon _notifyIcon;
    private ApplicationSettings _settings;
    private readonly TimeSyncService _timeSyncService;
    private bool _balloonShownOnce;
    private readonly DispatcherTimer _statusTimer;
    private DateTime? _lastCheckTime;
    private bool _isMonitoringActive;

    public MainWindow()
    {
        InitializeComponent();

        _settings = _settingsService.Load();
        _timeSyncService = new TimeSyncService(() => _settings, _systemTimeAdjuster);
        _timeSyncService.SyncResult += OnSyncResult;

        _notifyIcon = new NotifyIcon
        {
            Visible = false,
            Icon = System.Drawing.SystemIcons.Application,
            Text = "W32 Time Keeper"
        };
        _notifyIcon.DoubleClick += (_, _) => RestoreFromTray();

        Loaded += OnLoaded;
        Closing += OnClosing;
        StateChanged += OnStateChanged;

        _statusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _statusTimer.Tick += (_, _) => UpdateTimerStatusText();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RefreshServerList();

        try
        {
            var autoStartEnabled = _autoStartService.IsEnabled();
            _settings.AutoStartWithWindows = autoStartEnabled;
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Unable to query auto-start setting: {ex.Message}";
        }

        TimerStatusTextBlock.Text = "Monitoring active - no checks yet";
        _statusTimer.Start();
        _timeSyncService.Start();
        _isMonitoringActive = true;
        UpdateToggleSyncButton();
        UpdateTimerStatusText();
    }

    private void RefreshServerList()
    {
        ServersListBox.ItemsSource = null;
        ServersListBox.ItemsSource = _settings.TimeServers;
    }

    private async void OnSyncResult(object? sender, TimeSyncResultEventArgs e)
    {
        await Dispatcher.InvokeAsync(() =>
        {
            if (e.Success)
            {
                var drift = Math.Round(e.DriftMilliseconds, 2);
                var serverDetails = string.IsNullOrWhiteSpace(e.Server) ? string.Empty : $" (server: {e.Server})";
                LastSyncTextBlock.Text = $"Last sync: {DateTime.Now:G} | Drift: {drift} ms{serverDetails}";

                if (e.TimeAdjusted && _settings.AdjustmentNotificationsEnabled)
                {
                    ShowNotification($"System time adjusted by {Math.Round(e.DriftMilliseconds)} ms.");
                }
            }
            else
            {
                LastSyncTextBlock.Text = $"Last sync attempt: {DateTime.Now:G} | {e.Message}";
                ShowNotification(e.Message, ToolTipIcon.Warning);
            }

            StatusTextBlock.Text = e.Message;
            _lastCheckTime = DateTime.Now;
            UpdateTimerStatusText();
        });
    }

    private void ShowNotification(string message, ToolTipIcon icon = ToolTipIcon.Info)
    {
        if (!_settings.NotificationsEnabled)
        {
            return;
        }

        _notifyIcon.BalloonTipTitle = "W32 Time Keeper";
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipIcon = icon;
        _notifyIcon.Visible = true;
        _notifyIcon.ShowBalloonTip(5000);
    }

    private void OnOpenSettings(object sender, RoutedEventArgs e)
    {
        var wasMonitoring = _isMonitoringActive;
        var settingsWindow = new SettingsWindow(_settings, _settingsService, _autoStartService, _timeSyncService)
        {
            Owner = this
        };

        var result = settingsWindow.ShowDialog();
        if (result == true)
        {
            StatusTextBlock.Text = settingsWindow.StatusMessage ?? "Settings saved.";
            if (wasMonitoring)
            {
                _timeSyncService.UpdateInterval();
                _isMonitoringActive = true;
            }
            else
            {
                _timeSyncService.Stop();
                _isMonitoringActive = false;
            }

            UpdateToggleSyncButton();
            UpdateTimerStatusText();
        }
    }

    private void OnAddServer(object sender, RoutedEventArgs e)
    {
        var newServer = NewServerTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(newServer))
        {
            return;
        }

        if (_settings.TimeServers.Any(s => string.Equals(s, newServer, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(this, "This server is already in the list.", "Duplicate", MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        _settings.TimeServers.Add(newServer);
        RefreshServerList();
        NewServerTextBox.Clear();
        _settingsService.Save(_settings);
    }

    private void OnRemoveServer(object sender, RoutedEventArgs e)
    {
        if (ServersListBox.SelectedItem is not string server)
        {
            return;
        }

        _settings.TimeServers.Remove(server);
        RefreshServerList();
        _settingsService.Save(_settings);
    }

    private async void OnSyncNow(object sender, RoutedEventArgs e)
    {
        StatusTextBlock.Text = "Performing manual sync...";
        await _timeSyncService.PerformSyncAsync();
    }

    private void OnToggleSyncMonitoring(object sender, RoutedEventArgs e)
    {
        if (_isMonitoringActive)
        {
            _timeSyncService.Stop();
            _isMonitoringActive = false;
            _lastCheckTime = null;
            StatusTextBlock.Text = "Monitoring stopped.";
        }
        else
        {
            _timeSyncService.Start();
            _isMonitoringActive = true;
            _lastCheckTime = null;
            StatusTextBlock.Text = "Monitoring started.";
        }

        UpdateToggleSyncButton();
        UpdateTimerStatusText();
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            _notifyIcon.Visible = true;
            if (!_balloonShownOnce)
            {
                ShowNotification("Time Keeper continues to run in the background.");
                _balloonShownOnce = true;
            }
        }
        else if (WindowState == WindowState.Normal)
        {
            _notifyIcon.Visible = false;
        }
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        _statusTimer.Stop();
        _timeSyncService.Stop();
        _timeSyncService.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _isMonitoringActive = false;
    }

    private void UpdateToggleSyncButton()
    {
        if (!IsLoaded || ToggleSyncButton is null)
        {
            return;
        }

        ToggleSyncButton.Content = _isMonitoringActive ? "Stop Sync" : "Start Sync";
    }

    private void UpdateTimerStatusText()
    {
        if (!IsLoaded)
        {
            return;
        }

        if (!_isMonitoringActive)
        {
            TimerStatusTextBlock.Text = "Monitoring stopped";
            return;
        }

        if (_lastCheckTime is null)
        {
            TimerStatusTextBlock.Text = "Monitoring active - no checks yet";
            return;
        }

        var elapsed = DateTime.Now - _lastCheckTime.Value;
        if (elapsed < TimeSpan.Zero)
        {
            elapsed = TimeSpan.Zero;
        }

        var formatted = elapsed.TotalHours >= 1
            ? elapsed.ToString(@"hh\:mm\:ss")
            : elapsed.ToString(@"mm\:ss");

        TimerStatusTextBlock.Text = $"Monitoring active - last check {formatted} ago";
    }

}
