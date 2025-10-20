using System;
using System.Linq;
using TimeKeeperApp.Services;
using Xunit;

namespace TimeKeeperApp.Tests;

public class NtpClientTests
{
    [Fact]
    public void ExtractNetworkTime_ReturnsExpectedUtcTime()
    {
        var expectedUtc = new DateTime(2024, 1, 15, 12, 34, 56, DateTimeKind.Utc).AddMilliseconds(789);
        var ntpData = new byte[48];
        WriteTimestamp(ntpData, expectedUtc);

        var result = NtpClient.ExtractNetworkTime(ntpData);

        Assert.Equal(expectedUtc, result);
    }

    private static void WriteTimestamp(byte[] buffer, DateTime timestampUtc)
    {
        var epoch = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var seconds = (timestampUtc - epoch).TotalSeconds;
        var intPart = (ulong)Math.Floor(seconds);
        var fractionalSeconds = seconds - Math.Floor(seconds);
        var fractPart = (ulong)Math.Round(fractionalSeconds * 0x100000000L);

        var bytes = BitConverter.GetBytes(intPart).Reverse().ToArray();
        Array.Copy(bytes, bytes.Length - 4, buffer, 40, 4);

        bytes = BitConverter.GetBytes(fractPart).Reverse().ToArray();
        Array.Copy(bytes, bytes.Length - 4, buffer, 44, 4);
    }
}
