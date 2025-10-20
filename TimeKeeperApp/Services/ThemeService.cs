using System;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using TimeKeeperApp.Models;

namespace TimeKeeperApp.Services;

public class ThemeService : IDisposable
{
    private const string ThemeDictionaryPrefix = "Themes/";
    private readonly System.Windows.Application _application;
    private ThemePreference _requestedPreference = ThemePreference.System;
    private ThemePreference? _activeThemeDictionary;
    private bool _isListeningForSystemChanges;

    public ThemeService(System.Windows.Application application)
    {
        _application = application;
    }

    public ThemePreference EffectiveTheme { get; private set; } = ThemePreference.System;

    public void ApplyTheme(ThemePreference preference)
    {
        _requestedPreference = preference;
        var targetTheme = preference == ThemePreference.System
            ? DetectSystemTheme()
            : preference;

        EffectiveTheme = targetTheme;
        if (_activeThemeDictionary != targetTheme)
        {
            ReplaceThemeDictionary(targetTheme);
            _activeThemeDictionary = targetTheme;
        }

        UpdateSystemThemeSubscription();
    }

    private void ReplaceThemeDictionary(ThemePreference theme)
    {
        var newDictionary = new ResourceDictionary
        {
            Source = new Uri($"{ThemeDictionaryPrefix}{theme}Theme.xaml", UriKind.Relative)
        };

        var dictionaries = _application.Resources.MergedDictionaries;
        for (var index = dictionaries.Count - 1; index >= 0; index--)
        {
            var dictionary = dictionaries[index];
            if (dictionary.Source is not null &&
                dictionary.Source.OriginalString.StartsWith(ThemeDictionaryPrefix, StringComparison.OrdinalIgnoreCase))
            {
                dictionaries.RemoveAt(index);
            }
        }

        dictionaries.Add(newDictionary);
    }

    private void UpdateSystemThemeSubscription()
    {
        var shouldListen = _requestedPreference == ThemePreference.System;
        if (shouldListen && !_isListeningForSystemChanges)
        {
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
            _isListeningForSystemChanges = true;
        }
        else if (!shouldListen && _isListeningForSystemChanges)
        {
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
            _isListeningForSystemChanges = false;
        }
    }

    private void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (_requestedPreference != ThemePreference.System)
        {
            return;
        }

        if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.Color)
        {
            _application.Dispatcher.Invoke(() => ApplyTheme(ThemePreference.System));
        }
    }

    private static ThemePreference DetectSystemTheme()
    {
        try
        {
            using var key =
                Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int value)
            {
                return value > 0 ? ThemePreference.Light : ThemePreference.Dark;
            }
        }
        catch
        {
            // ignored - fallback to light theme
        }

        return ThemePreference.Light;
    }

    public void Dispose()
    {
        if (_isListeningForSystemChanges)
        {
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
            _isListeningForSystemChanges = false;
        }
    }
}
