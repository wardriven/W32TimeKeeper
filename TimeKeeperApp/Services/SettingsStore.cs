using System;
using System.IO;
using System.Text.Json;
using TimeKeeperApp.Models;

namespace TimeKeeperApp.Services;

public class SettingsStore : ISettingsStore
{
    private const string AppFolderName = "W32TimeKeeper";
    private const string SettingsFileName = "settings.json";

    public TimeCheckSettings Load()
    {
        try
        {
            var path = GetSettingsPath();
            if (!File.Exists(path))
            {
                return new TimeCheckSettings();
            }

            var json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<TimeCheckSettings>(json);
            return settings ?? new TimeCheckSettings();
        }
        catch
        {
            return new TimeCheckSettings();
        }
    }

    public void Save(TimeCheckSettings settings)
    {
        var directory = GetSettingsDirectory();
        Directory.CreateDirectory(directory);
        var path = GetSettingsPath();

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(path, json);
    }

    private static string GetSettingsDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, AppFolderName);
    }

    private static string GetSettingsPath()
    {
        return Path.Combine(GetSettingsDirectory(), SettingsFileName);
    }
}
