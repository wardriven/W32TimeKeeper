using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace TimeKeeperApp.Services;

public class AutoStartService
{
    private const string RunKeyPath = @"Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string AppName = "W32TimeKeeper";

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        var value = key?.GetValue(AppName) as string;
        return !string.IsNullOrWhiteSpace(value);
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("Unable to open registry key for autorun.");

        if (enabled)
        {
            var processPath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrWhiteSpace(processPath))
            {
                throw new InvalidOperationException("Could not determine application path for auto-start registration.");
            }

            key.SetValue(AppName, $"\"{processPath}\"");
        }
        else
        {
            key.DeleteValue(AppName, throwOnMissingValue: false);
        }
    }
}
