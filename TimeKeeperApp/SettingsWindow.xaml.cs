using System;
using System.Windows.Controls;
using System.Windows;
using WpfRadioButton = System.Windows.Controls.RadioButton;
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
    private readonly ThemeService _themeService;
    private readonly ThemeOption[] _themeOptions =
    {
        new ThemeOption("Light", ThemePreference.Light),
        new ThemeOption("Dark", ThemePreference.Dark),
        new ThemeOption("Follow system theme", ThemePreference.System)
    };
    private bool _pendingAdjustmentNotificationPreference;
    private ThemePreference _selectedThemePreference;

    public string? StatusMessage { get; private set; }

    public SettingsWindow(
        ApplicationSettings settings,
        SettingsService settingsService,
        AutoStartService autoStartService,
        TimeSyncService timeSyncService,
        ThemeService themeService)
    {
        InitializeComponent();
        _settings = settings;
        _settingsService = settingsService;
        _autoStartService = autoStartService;
        _timeSyncService = timeSyncService;
        _themeService = themeService;

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        DriftAllowanceTextBox.Text = _settings.DriftAllowanceMilliseconds.ToString();
        SyncIntervalTextBox.Text = _settings.SyncIntervalSeconds.ToString();
        AllNotificationsCheckBox.IsChecked = _settings.NotificationsEnabled;
        AdjustmentNotificationsCheckBox.IsChecked = _settings.AdjustmentNotificationsEnabled;
        _pendingAdjustmentNotificationPreference = _settings.AdjustmentNotificationsEnabled;
        _selectedThemePreference = _settings.ThemePreference;
        InitializeThemeOptions();

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
        var allNotificationsEnabled = AllNotificationsCheckBox.IsChecked == true;
        _settings.NotificationsEnabled = allNotificationsEnabled;
        _settings.AdjustmentNotificationsEnabled = allNotificationsEnabled &&
            AdjustmentNotificationsCheckBox.IsChecked == true;

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

        var selectedTheme = _selectedThemePreference;

        _settings.ThemePreference = selectedTheme;
        _themeService.ApplyTheme(selectedTheme);

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
        if (AdjustmentNotificationsCheckBox is null)
        {
            return;
        }

        AdjustmentNotificationsCheckBox.IsEnabled = notificationsEnabled;
        if (notificationsEnabled)
        {
            AdjustmentNotificationsCheckBox.IsChecked = _pendingAdjustmentNotificationPreference;
        }
        else
        {
            _pendingAdjustmentNotificationPreference =
                AdjustmentNotificationsCheckBox.IsChecked == true;
            AdjustmentNotificationsCheckBox.IsChecked = false;
        }
    }

    private void InitializeThemeOptions()
    {
        ThemeOptionsPanel.Children.Clear();

        for (var i = 0; i < _themeOptions.Length; i++)
        {
            var option = _themeOptions[i];
            var radioButton = new WpfRadioButton
            {
                Content = option.DisplayName,
                GroupName = "ThemePreferenceGroup",
                Tag = option.Preference,
                Margin = new Thickness(0, 0, 0, i < _themeOptions.Length - 1 ? 5 : 0),
                IsChecked = option.Preference == _selectedThemePreference
            };
            radioButton.Checked += OnThemeOptionChecked;
            ThemeOptionsPanel.Children.Add(radioButton);
        }

        if (ThemeOptionsPanel.Children.Count == 0)
        {
            _selectedThemePreference = _settings.ThemePreference;
        }
    }

    private void OnThemeOptionChecked(object sender, RoutedEventArgs e)
    {
        if (sender is WpfRadioButton radioButton &&
            radioButton.Tag is ThemePreference preference)
        {
            _selectedThemePreference = preference;
        }
    }

    private sealed record ThemeOption(string DisplayName, ThemePreference Preference);
}
