using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TimeKeeperApp.Models;

public class ServerStatus : INotifyPropertyChanged
{
    private string _server = string.Empty;
    private DateTime? _lastChecked;
    private double? _offsetSeconds;
    private string _statusMessage = "Not checked";
    private bool _hasError;
    private int _slotIndex;

    public string Server
    {
        get => _server;
        set
        {
            if (_server == value)
            {
                return;
            }

            _server = value;
            OnPropertyChanged();
        }
    }

    public int SlotIndex
    {
        get => _slotIndex;
        set
        {
            if (_slotIndex == value)
            {
                return;
            }

            _slotIndex = value;
            OnPropertyChanged();
        }
    }

    public DateTime? LastChecked
    {
        get => _lastChecked;
        set
        {
            if (_lastChecked == value)
            {
                return;
            }

            _lastChecked = value;
            OnPropertyChanged();
        }
    }

    public double? OffsetSeconds
    {
        get => _offsetSeconds;
        set
        {
            if (_offsetSeconds == value)
            {
                return;
            }

            _offsetSeconds = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage == value)
            {
                return;
            }

            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public bool HasError
    {
        get => _hasError;
        set
        {
            if (_hasError == value)
            {
                return;
            }

            _hasError = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
