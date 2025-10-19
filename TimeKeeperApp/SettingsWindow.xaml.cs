using System;
using System.Windows;
using TimeKeeperApp.Models;
using TimeKeeperApp.Services;
using MessageBox = System.Windows.MessageBox;

namespace TimeKeeperApp;

public partial class SettingsWindow : Window
{
    private readonly ApplicationSettings _settings;
    private readonly SettingsService _settingsService;
    private readonly AutoStartService _autoStartService;
    private readonly TimeSyncService _timeSyncService;

    public string? StatusMessage { get; private set; }

    public SettingsWindow(
        ApplicationSettings settings,
        SettingsService settingsService,
        AutoStartService autoStartService,
        TimeSyncService timeSyncService)
    {
        InitializeComponent();
        _settings = settings;
        _settingsService = settingsService;
        _autoStartService = autoStartService;
        _timeSyncService = timeSyncService;

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        DriftAllowanceTextBox.Text = _settings.DriftAllowanceMilliseconds.ToString();
        SyncIntervalTextBox.Text = _settings.SyncIntervalSeconds.ToString();
        AllNotificationsCheckBox.IsChecked = _settings.NotificationsEnabled;
        AdjustmentNotificationsCheckBox.IsChecked = _settings.AdjustmentNotificationsEnabled;

        try
        {
            var autoStartEnabled = _autoStartService.IsEnabled();
            AutoStartCheckBox.IsChecked = autoStartEnabled;
            _settings.AutoStartWithWindows = autoStartEnabled;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Unable to query auto-start setting: {ex.Message}", "Auto-start",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            AutoStartCheckBox.IsChecked = _settings.AutoStartWithWindows;
        }

        UpdateNotificationCheckboxState();
    }

    private void OnSaveSettings(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(DriftAllowanceTextBox.Text, out var drift) || drift < 0)
        {
            MessageBox.Show(this, "Please enter a valid non-negative number for drift allowance.", "Invalid value",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(SyncIntervalTextBox.Text, out var intervalSeconds) || intervalSeconds <= 0)
        {
            MessageBox.Show(this, "Please enter a valid positive number for the check interval.", "Invalid value",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _settings.DriftAllowanceMilliseconds = drift;
        _settings.SyncIntervalSeconds = intervalSeconds;
        _settings.NotificationsEnabled = AllNotificationsCheckBox.IsChecked == true;
        _settings.AdjustmentNotificationsEnabled = AdjustmentNotificationsCheckBox.IsChecked == true;

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
            return;
        }

        _settingsService.Save(_settings);
        _timeSyncService.UpdateInterval();
        StatusMessage = "Settings saved.";
        DialogResult = true;
    }

    private void OnAllNotificationsChanged(object sender, RoutedEventArgs e)
    {
        UpdateNotificationCheckboxState();
    }

    private void UpdateNotificationCheckboxState()
    {
        var notificationsEnabled = AllNotificationsCheckBox.IsChecked == true;
        AdjustmentNotificationsCheckBox.IsEnabled = notificationsEnabled;
    }
}
