using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Cliq.Domain;

namespace Cliq.VisualStudio;

internal sealed class ScanPageDialog : Window
{
    private readonly TextBox _url;
    private readonly TextBox _name;
    private readonly ComboBox _browser;
    private readonly CheckBox _headless;

    public ScanPageDialog(
        string? initialUrl,
        string? initialName,
        BrowserKind defaultBrowser)
    {
        Title = "Scan Page in Cliq";
        Width = 520;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        ResizeMode = ResizeMode.NoResize;

        var grid = new Grid { Margin = new Thickness(16) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        for (var index = 0; index < 5; index++)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        _url = AddField(grid, 0, "_URL:", initialUrl ?? string.Empty);
        _name = AddField(grid, 1, "_Page name:", initialName ?? string.Empty);
        AddLabel(grid, 2, "_Browser:");
        _browser = new ComboBox
        {
            ItemsSource = Enum.GetValues(typeof(BrowserKind)),
            SelectedItem = defaultBrowser,
            Margin = new Thickness(8, 4, 0, 4),
            MinWidth = 180,
        };
        Grid.SetRow(_browser, 2);
        Grid.SetColumn(_browser, 1);
        grid.Children.Add(_browser);

        _headless = new CheckBox
        {
            Content = "Run scan _headlessly",
            IsChecked = true,
            Margin = new Thickness(8, 8, 0, 8),
        };
        Grid.SetRow(_headless, 3);
        Grid.SetColumn(_headless, 1);
        grid.Children.Add(_headless);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0),
        };
        var scan = new Button
        {
            Content = "_Scan",
            IsDefault = true,
            MinWidth = 82,
            Margin = new Thickness(0, 0, 8, 0),
        };
        scan.Click += (_, _) => Accept();
        var cancel = new Button
        {
            Content = "Cancel",
            IsCancel = true,
            MinWidth = 82,
        };
        buttons.Children.Add(scan);
        buttons.Children.Add(cancel);
        Grid.SetRow(buttons, 4);
        Grid.SetColumnSpan(buttons, 2);
        grid.Children.Add(buttons);
        Content = grid;
    }

    public string PageUrl => _url.Text.Trim();
    public string PageName => _name.Text.Trim();
    public BrowserKind Browser => (BrowserKind)(_browser.SelectedItem ?? BrowserKind.Chrome);
    public bool Headless => _headless.IsChecked == true;

    private static TextBox AddField(Grid grid, int row, string label, string value)
    {
        AddLabel(grid, row, label);
        var field = new TextBox
        {
            Text = value,
            Margin = new Thickness(8, 4, 0, 4),
            MinWidth = 350,
        };
        Grid.SetRow(field, row);
        Grid.SetColumn(field, 1);
        grid.Children.Add(field);
        return field;
    }

    private static void AddLabel(Grid grid, int row, string text)
    {
        var label = new Label
        {
            Content = text,
            VerticalAlignment = VerticalAlignment.Center,
            Target = null,
        };
        Grid.SetRow(label, row);
        grid.Children.Add(label);
    }

    private void Accept()
    {
        if (!Uri.TryCreate(PageUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            MessageBox.Show(
                this,
                "Enter an absolute HTTP or HTTPS URL.",
                "Cliq",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            _url.Focus();
            return;
        }

        if (PageName.Length == 0)
        {
            MessageBox.Show(
                this,
                "Enter a page name.",
                "Cliq",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            _name.Focus();
            return;
        }

        DialogResult = true;
    }
}

internal sealed class GenerationResultDialog : Window
{
    private readonly ListBox _artifacts;

    public GenerationResultDialog(IReadOnlyList<GeneratedArtifact> artifacts)
    {
        Title = "Cliq Generation Results";
        Width = 720;
        Height = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;

        var panel = new DockPanel { Margin = new Thickness(16) };
        var summary = new TextBlock
        {
            Text = $"{artifacts.Count} generated artifact{(artifacts.Count == 1 ? string.Empty : "s")}",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 10),
        };
        DockPanel.SetDock(summary, Dock.Top);
        panel.Children.Add(summary);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 10, 0, 0),
        };
        var open = new Button { Content = "_Open", MinWidth = 82, Margin = new Thickness(0, 0, 8, 0) };
        open.Click += (_, _) => OpenRequested?.Invoke(SelectedPath);
        var reveal = new Button { Content = "_Reveal", MinWidth = 82, Margin = new Thickness(0, 0, 8, 0) };
        reveal.Click += (_, _) => Reveal(SelectedPath);
        var close = new Button { Content = "Close", IsCancel = true, MinWidth = 82 };
        buttons.Children.Add(open);
        buttons.Children.Add(reveal);
        buttons.Children.Add(close);
        DockPanel.SetDock(buttons, Dock.Bottom);
        panel.Children.Add(buttons);

        _artifacts = new ListBox
        {
            ItemsSource = artifacts,
            DisplayMemberPath = nameof(GeneratedArtifact.Path),
        };
        _artifacts.MouseDoubleClick += (_, _) => OpenRequested?.Invoke(SelectedPath);
        if (artifacts.Count > 0)
        {
            _artifacts.SelectedIndex = 0;
        }
        panel.Children.Add(_artifacts);
        Content = panel;
    }

    public event Action<string?>? OpenRequested;

    private string? SelectedPath => (_artifacts.SelectedItem as GeneratedArtifact)?.Path;

    private static void Reveal(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var fullPath = Path.GetFullPath(path);
        Process.Start(
            new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = File.Exists(fullPath)
                    ? $"/select,\"{fullPath}\""
                    : $"\"{Path.GetDirectoryName(fullPath)}\"",
                UseShellExecute = true,
            });
    }
}

internal sealed class CliqProgressDialog : Window
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
