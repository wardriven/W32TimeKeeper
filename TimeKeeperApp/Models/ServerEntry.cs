using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TimeKeeperApp.Models;

public class ServerEntry : INotifyPropertyChanged
{
    private string _hostname = string.Empty;
    private string? _error;

    public ServerEntry(int index)
    {
        Index = index;
        Label = $"Server {index + 1}";
    }

    public int Index { get; }

    public string Label { get; }

    public string Hostname
    {
        get => _hostname;
        set
        {
            var trimmed = (value ?? string.Empty).Trim();
            if (_hostname == trimmed)
            {
                return;
            }

            _hostname = trimmed;
            OnPropertyChanged();
        }
    }

    public string? Error
    {
        get => _error;
        set
        {
            if (_error == value)
            {
                return;
            }

            _error = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
