using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TimeKeeperApp.Services;

public class NtpClient : INtpClient
{
    private const int NtpPort = 123;
    private const int NtpPacketLength = 48;
    private const byte TransmitTimestampOffset = 40;

    public async Task<NtpQueryResult> QueryAsync(string host, int timeoutMilliseconds, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new NtpClientException("Host name is required.");
        }

        try
        {
            return await Task.Run(() => QueryInternal(host.Trim(), timeoutMilliseconds, cancellationToken), cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SocketException ex)
        {
            throw new NtpClientException($"Socket error contacting '{host}': {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not NtpClientException)
        {
            throw new NtpClientException($"Unable to query '{host}': {ex.Message}", ex);
        }
    }

    private static NtpQueryResult QueryInternal(string host, int timeoutMilliseconds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var ntpData = new byte[NtpPacketLength];
        ntpData[0] = 0x1B;

        var addresses = Dns.GetHostAddresses(host);
        var address = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
                      ?? addresses.FirstOrDefault();
        if (address is null)
        {
            throw new NtpClientException($"No IP addresses resolved for '{host}'.");
        }

        using var socket = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
        {
            ReceiveTimeout = timeoutMilliseconds,
            SendTimeout = timeoutMilliseconds
        };

        var endPoint = new IPEndPoint(address, NtpPort);
        socket.Connect(endPoint);

        cancellationToken.ThrowIfCancellationRequested();
        socket.Send(ntpData);

        var received = socket.Receive(ntpData);
        if (received < NtpPacketLength)
        {
            throw new NtpClientException("Incomplete NTP response received.");
        }

        var serverTime = ExtractNetworkTime(ntpData);
        return new NtpQueryResult(serverTime);
    }

    internal static DateTime ExtractNetworkTime(byte[] ntpData)
    {
        if (ntpData.Length < NtpPacketLength)
        {
            throw new ArgumentException("Invalid NTP response length.", nameof(ntpData));
        }

        var intPart = ((ulong)ntpData[TransmitTimestampOffset] << 24)
                      | ((ulong)ntpData[TransmitTimestampOffset + 1] << 16)
                      | ((ulong)ntpData[TransmitTimestampOffset + 2] << 8)
                      | ntpData[TransmitTimestampOffset + 3];

        var fractPart = ((ulong)ntpData[TransmitTimestampOffset + 4] << 24)
                        | ((ulong)ntpData[TransmitTimestampOffset + 5] << 16)
                        | ((ulong)ntpData[TransmitTimestampOffset + 6] << 8)
                        | ntpData[TransmitTimestampOffset + 7];

        var milliseconds = (intPart * 1000d) + ((fractPart * 1000d) / 0x100000000L);
        var networkDateTime = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(milliseconds);
        return networkDateTime;
    }
}
