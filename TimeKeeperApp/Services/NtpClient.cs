using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TimeKeeperApp.Services;

public static class NtpClient
{
    public static async Task<DateTime?> GetNetworkTimeAsync(string host, int timeoutMilliseconds = 3000)
    {
        return await Task.Run(() => QueryServer(host, timeoutMilliseconds));
    }

    private static DateTime? QueryServer(string host, int timeoutMilliseconds)
    {
        const int NtpPort = 123;
        var ntpData = new byte[48];
        ntpData[0] = 0x1B;

        try
        {
            var addresses = Dns.GetHostAddresses(host);
            var address = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
                          ?? addresses.FirstOrDefault();
            if (address == null)
            {
                return null;
            }

            using var socket = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
            {
                ReceiveTimeout = timeoutMilliseconds,
                SendTimeout = timeoutMilliseconds
            };

            var ipEndPoint = new IPEndPoint(address, NtpPort);
            socket.Connect(ipEndPoint);
            socket.Send(ntpData);
            var received = socket.Receive(ntpData);
            if (received < 48)
            {
                return null;
            }
        }
        catch
        {
            return null;
        }

        const byte serverReplyTime = 40;
        ulong intPart = ((ulong)ntpData[serverReplyTime] << 24)
            | ((ulong)ntpData[serverReplyTime + 1] << 16)
            | ((ulong)ntpData[serverReplyTime + 2] << 8)
            | ntpData[serverReplyTime + 3];

        ulong fractPart = ((ulong)ntpData[serverReplyTime + 4] << 24)
            | ((ulong)ntpData[serverReplyTime + 5] << 16)
            | ((ulong)ntpData[serverReplyTime + 6] << 8)
            | ntpData[serverReplyTime + 7];

        var milliseconds = (intPart * 1000) + ((fractPart * 1000) / 0x100000000L);
        var networkDateTime = new DateTime(1900, 1, 1).AddMilliseconds((long)milliseconds);
        return networkDateTime.ToUniversalTime();
    }
}
