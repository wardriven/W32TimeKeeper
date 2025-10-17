using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
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
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RefreshServerList();
        DriftAllowanceTextBox.Text = _settings.DriftAllowanceMilliseconds.ToString();
        NotificationsCheckBox.IsChecked = _settings.NotificationsEnabled;

        try
        {
            AutoStartCheckBox.IsChecked = _autoStartService.IsEnabled();
            _settings.AutoStartWithWindows = AutoStartCheckBox.IsChecked == true;
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Unable to query auto-start setting: {ex.Message}";
        }

        TimerStatusTextBlock.Text = "Monitoring active";
        _timeSyncService.Start();
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

                if (e.TimeAdjusted && _settings.NotificationsEnabled)
                {
                    ShowNotification($"System time adjusted by {Math.Round(e.DriftMilliseconds)} ms.");
                }
            }
            else
            {
                LastSyncTextBlock.Text = $"Last sync attempt: {DateTime.Now:G} | {e.Message}";
                if (_settings.NotificationsEnabled)
                {
                    ShowNotification(e.Message, ToolTipIcon.Warning);
                }
            }

            StatusTextBlock.Text = e.Message;
        });
    }

    private void ShowNotification(string message, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _notifyIcon.BalloonTipTitle = "W32 Time Keeper";
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipIcon = icon;
        _notifyIcon.Visible = true;
        _notifyIcon.ShowBalloonTip(5000);
    }

    private void OnSaveSettings(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(DriftAllowanceTextBox.Text, out var drift) || drift < 0)
        {
            MessageBox.Show(this, "Please enter a valid non-negative number for drift allowance.", "Invalid value",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _settings.DriftAllowanceMilliseconds = drift;
        _settings.NotificationsEnabled = NotificationsCheckBox.IsChecked == true;

        var requestedAutoStart = AutoStartCheckBox.IsChecked == true;
        try
        {
            _autoStartService.SetEnabled(requestedAutoStart);
            _settings.AutoStartWithWindows = requestedAutoStart;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Unable to update auto-start setting: {ex.Message}", "Auto-start",
                MessageBoxButton.OK, MessageBoxImage.Error);
            AutoStartCheckBox.IsChecked = _settings.AutoStartWithWindows;
        }

        _settingsService.Save(_settings);
        StatusTextBlock.Text = "Settings saved.";
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
        _timeSyncService.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
