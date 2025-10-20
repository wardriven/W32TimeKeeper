using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TimeKeeperApp.ViewModels;

namespace TimeKeeperApp;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = (MainViewModel)DataContext;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeAsync();
        if (_viewModel.CanStart)
        {
            await _viewModel.StartAsync();
        }
    }

    private async void OnStartChecks(object sender, RoutedEventArgs e)
    {
        await _viewModel.StartAsync();
    }

    private void OnStopChecks(object sender, RoutedEventArgs e)
    {
        _viewModel.Stop();
    }

    private void OnIntervalPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !IsNumeric(e.Text);
    }

    private void OnIntervalPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(DataFormats.Text))
        {
            e.CancelCommand();
            return;
        }

        if (e.DataObject.GetData(DataFormats.Text) is string text && !IsNumeric(text))
        {
            e.CancelCommand();
        }
    }

    private void OnServerPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !IsHostnameText(e.Text);
    }

    private void OnServerPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(DataFormats.Text))
        {
            e.CancelCommand();
            return;
        }

        if (e.DataObject.GetData(DataFormats.Text) is string text && !IsHostnameText(text))
        {
            e.CancelCommand();
        }
    }

    private static bool IsNumeric(string? text)
    {
        return !string.IsNullOrEmpty(text) && text.All(char.IsDigit);
    }

    private static bool IsHostnameText(string? text)
    {
        return !string.IsNullOrEmpty(text) && text.All(c => char.IsLetterOrDigit(c) || c is '-' or '.');
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        _viewModel.Dispose();
    }
}
