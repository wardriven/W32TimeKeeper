using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace TimeKeeperApp.Services;

public class SystemTimeAdjuster
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetSystemTime(ref SystemTime systemTime);

    public bool TryApply(DateTime utcTime, out string? errorMessage)
    {
        var st = new SystemTime
        {
            Year = (ushort)utcTime.Year,
            Month = (ushort)utcTime.Month,
            DayOfWeek = (ushort)utcTime.DayOfWeek,
            Day = (ushort)utcTime.Day,
            Hour = (ushort)utcTime.Hour,
            Minute = (ushort)utcTime.Minute,
            Second = (ushort)utcTime.Second,
            Milliseconds = (ushort)utcTime.Millisecond
        };

        if (!SetSystemTime(ref st))
        {
            var error = new Win32Exception(Marshal.GetLastWin32Error());
            errorMessage = error.Message;
            return false;
        }

        errorMessage = null;
        return true;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemTime
    {
        public ushort Year;
        public ushort Month;
        public ushort DayOfWeek;
        public ushort Day;
        public ushort Hour;
        public ushort Minute;
        public ushort Second;
        public ushort Milliseconds;
    }
}
