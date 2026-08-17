using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Cliq.VisualStudio;

internal sealed class CliqProgressDialog : CliqDialogWindow
{
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Func<CancellationToken, Task> _operation;
    private bool _started;

    public CliqProgressDialog(string message, Func<CancellationToken, Task> operation)
    {
        _operation = operation ?? throw new ArgumentNullException(nameof(operation));
        Title = "Cliq";
        Width = 430;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;

        var panel = new StackPanel { Margin = new Thickness(18) };
        panel.Children.Add(
            new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12),
            });
        panel.Children.Add(
            new ProgressBar
            {
                IsIndeterminate = true,
                Height = 6,
                Margin = new Thickness(0, 0, 0, 14),
            });
        var cancel = new Button
        {
            Content = "Cancel",
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 82,
        };
        cancel.Click += (_, _) =>
        {
            cancel.IsEnabled = false;
            cancel.Content = "Cancelling...";
            _cancellation.Cancel();
        };
        panel.Children.Add(cancel);
        Content = panel;
        Loaded += OnLoaded;
        Closing += (_, args) =>
        {
            if (_started && DialogResult is null)
            {
                _cancellation.Cancel();
                args.Cancel = true;
            }
        };
    }

    public Exception? Error { get; private set; }

    [SuppressMessage(
        "Usage",
        "VSTHRD100:Avoid async void methods",
        Justification = "WPF loaded handlers require void; all operation exceptions are captured.")]
    private async void OnLoaded(object sender, RoutedEventArgs eventArgs)
    {
        if (_started)
        {
            return;
        }

        _started = true;
        try
        {
            await _operation(_cancellation.Token);
        }
        catch (OperationCanceledException) when (_cancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Error = exception;
        }
        finally
        {
            DialogResult = true;
            _cancellation.Dispose();
        }
    }
}
