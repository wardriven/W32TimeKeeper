using System;

namespace TimeKeeperApp.Services;

public class NtpClientException : Exception
{
    public NtpClientException(string message) : base(message)
    {
    }

    public NtpClientException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
