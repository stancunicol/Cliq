using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using Cliq.Domain;
using Cliq.VisualStudio.Core.Actions;
using Cliq.VisualStudio.Core.Api;
using Cliq.VisualStudio.Core.Authoring;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Cliq.VisualStudio;

internal sealed class ScanPageDialog : CliqDialogWindow
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
        _browser = new ComboBox
        {
            ItemsSource = Enum.GetValues(typeof(BrowserKind)),
            SelectedItem = defaultBrowser,
            Margin = new Thickness(8, 4, 0, 4),
            MinWidth = 180,
        };
        AutomationProperties.SetName(_browser, "Browser");
        Grid.SetRow(_browser, 2);
        Grid.SetColumn(_browser, 1);
        grid.Children.Add(_browser);
        AddLabel(grid, 2, "_Browser:", _browser);

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
        var field = new TextBox
        {
            Text = value,
            Margin = new Thickness(8, 4, 0, 4),
            MinWidth = 350,
        };
        AutomationProperties.SetName(
            field,
            label.Replace("_", string.Empty).TrimEnd(':'));
        Grid.SetRow(field, row);
        Grid.SetColumn(field, 1);
        grid.Children.Add(field);
        AddLabel(grid, row, label, field);
        return field;
    }

    private static void AddLabel(Grid grid, int row, string text, UIElement target)
    {
        var label = new Label
        {
            Content = text,
            VerticalAlignment = VerticalAlignment.Center,
            Target = target,
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

internal sealed class GenerationResultDialog : CliqDialogWindow
{
    private readonly ListBox _artifacts;

    public GenerationResultDialog(GenerationResult result)
    {
        var artifacts = result.Artifacts;
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

        if (result.Warnings.Count > 0)
        {
            var warning = new TextBlock
            {
                Text = string.Join(
                    Environment.NewLine,
                    result.Warnings.Select(item =>
                        $"{item.PageObjectName}: {item.RecoveredCount} of {item.ExpectedCount} custom methods need your decision - " +
                        $"this file was left untouched, not regenerated. Resolve via Pending Merges. ({item.Path})")),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10),
            };
            AutomationProperties.SetName(warning, "Custom method recovery warnings");
            DockPanel.SetDock(warning, Dock.Top);
            panel.Children.Add(warning);
        }

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

internal sealed class ScanResultDialog : CliqDialogWindow
{
    public ScanResultDialog(ScanPageResponse response)
    {
        var report = response.Reconciliation;
        Title = "Cliq Scan Results";
        Width = 640;
        SizeToContent = SizeToContent.Height;
        MaxHeight = 620;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;

        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock
        {
            Text = $"Scanned {response.Page.Name}",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6),
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"Matched: {report.MatchedCount}    New: {report.NewCount}    Missing: {report.MissingCount}",
            Margin = new Thickness(0, 0, 0, 12),
        });

        var diff = response.LocatorDiff;
        if (diff is not null && (diff.New.Count > 0 || diff.Missing.Count > 0))
        {
            panel.Children.Add(SectionHeading("Locator changes"));
            if (diff.New.Count > 0)
            {
                panel.Children.Add(SubHeading($"New ({diff.New.Count})"));
                panel.Children.Add(EntryList(diff.New.Select(DescribeLocatorEntry)));
            }

            if (diff.Missing.Count > 0)
            {
                panel.Children.Add(SubHeading($"Missing ({diff.Missing.Count})"));
                panel.Children.Add(EntryList(diff.Missing.Select(DescribeLocatorEntry)));
            }
        }

        var suggestions = response.DynamicElementSuggestions;
        if (suggestions is { Count: > 0 })
        {
            panel.Children.Add(SectionHeading($"Dynamic element suggestions ({suggestions.Count})"));
            AutomationProperties.SetName(panel.Children[panel.Children.Count - 1], "Dynamic element suggestions");
            panel.Children.Add(EntryList(suggestions.Select(item =>
                $"{item.Name}: {item.Action} via {item.Strategy} ({item.SampleCount} similar elements found)")));
        }

        var close = new Button
        {
            Content = "Close",
            IsCancel = true,
            IsDefault = true,
            MinWidth = 82,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 14, 0, 0),
        };
        panel.Children.Add(close);

        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = panel,
        };
    }

    private static string DescribeLocatorEntry(LocatorDiffEntry entry) =>
        $"{entry.Name} ({entry.Tag}) - {entry.Locator.Strategy}: {entry.Locator.Value}";

    private static TextBlock SectionHeading(string text) => new()
    {
        Text = text,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 8, 0, 4),
    };

    private static TextBlock SubHeading(string text) => new()
    {
        Text = text,
        Margin = new Thickness(0, 4, 0, 2),
    };

    private static ItemsControl EntryList(IEnumerable<string> items) => new()
    {
        ItemsSource = items.ToArray(),
        Margin = new Thickness(8, 0, 0, 4),
    };
}

internal sealed class ScanReviewDialog : CliqDialogWindow
{
    private readonly ScanReviewViewModel _viewModel;
    private readonly TextBlock _summaryText;
    private readonly TextBlock _conflictText;
    private readonly Border _conflictBorder;
    private readonly TextBlock _statusText;
    private readonly Button _applyButton;
    private readonly Button _discardButton;
    private readonly Button _acceptAllButton;
    private readonly Button _denyAllButton;
    private readonly Button _closeButton;

    public ScanReviewDialog(ScanReviewViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        Title = $"Cliq Staged Scan Review - {viewModel.PageName}";
        Width = 720;
        Height = 560;
        MinHeight = 400;
        MinWidth = 550;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;

        var mainGrid = new Grid { Margin = new Thickness(16) };
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Header
        var headerPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
        headerPanel.Children.Add(new TextBlock
        {
            Text = $"Review Changes for {viewModel.PageName}",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 4)
        });
        headerPanel.Children.Add(new TextBlock
        {
            Text = viewModel.PageUrl,
            Foreground = SystemColors.GrayTextBrush,
            Margin = new Thickness(0, 0, 0, 8)
        });

        var summaryDock = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
        var batchButtons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        _acceptAllButton = new Button { Content = "Accept All", Margin = new Thickness(0, 0, 6, 0), Padding = new Thickness(8, 3, 8, 3) };
        _acceptAllButton.Click += (_, _) => _viewModel.AcceptAll();
        _denyAllButton = new Button { Content = "Deny All", Margin = new Thickness(0, 0, 0, 0), Padding = new Thickness(8, 3, 8, 3) };
        _denyAllButton.Click += (_, _) => _viewModel.DenyAll();
        batchButtons.Children.Add(_acceptAllButton);
        batchButtons.Children.Add(_denyAllButton);
        DockPanel.SetDock(batchButtons, Dock.Right);
        summaryDock.Children.Add(batchButtons);

        _summaryText = new TextBlock
        {
            Text = GetSummaryString(),
            VerticalAlignment = VerticalAlignment.Center
        };
        summaryDock.Children.Add(_summaryText);
        headerPanel.Children.Add(summaryDock);
        Grid.SetRow(headerPanel, 0);
        mainGrid.Children.Add(headerPanel);

        // Conflict Warning Banner
        _conflictBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(40, 255, 165, 0)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(120, 255, 165, 0)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 10),
            Visibility = Visibility.Collapsed
        };
        _conflictText = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(220, 50, 50))
        };
        _conflictBorder.Child = _conflictText;
        Grid.SetRow(_conflictBorder, 1);
        mainGrid.Children.Add(_conflictBorder);

        // Items Content
        var itemsPanel = new StackPanel();
        if (viewModel.NewItems.Count > 0)
        {
            itemsPanel.Children.Add(new TextBlock
            {
                Text = $"New Elements ({viewModel.NewItems.Count})",
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                Margin = new Thickness(0, 4, 0, 6)
            });

            foreach (var item in viewModel.NewItems)
            {
                itemsPanel.Children.Add(CreateItemRow(item));
            }
        }

        if (viewModel.MissingItems.Count > 0)
        {
            itemsPanel.Children.Add(new TextBlock
            {
                Text = $"Missing Elements ({viewModel.MissingItems.Count})",
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                Margin = new Thickness(0, 12, 0, 6)
            });

            foreach (var item in viewModel.MissingItems)
            {
                itemsPanel.Children.Add(CreateItemRow(item));
            }
        }

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = itemsPanel,
            Margin = new Thickness(0, 0, 0, 12)
        };
        Grid.SetRow(scrollViewer, 2);
        mainGrid.Children.Add(scrollViewer);

        // Bottom Actions & Status
        var bottomPanel = new DockPanel();
        _statusText = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 0, 10, 0)
        };
        bottomPanel.Children.Add(_statusText);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        DockPanel.SetDock(buttons, Dock.Right);

        _applyButton = new Button
        {
            Content = "_Apply",
            IsDefault = true,
            MinWidth = 82,
            Margin = new Thickness(0, 0, 8, 0)
        };
        _applyButton.Click += (_, _) => RunOperation(OnApplyAsync);

        _discardButton = new Button
        {
            Content = "_Discard",
            MinWidth = 82,
            Margin = new Thickness(0, 0, 8, 0)
        };
        _discardButton.Click += (_, _) => RunOperation(OnDiscardAsync);


        _closeButton = new Button
        {
            Content = "Close",
            IsCancel = true,
            MinWidth = 82
        };
        _closeButton.Click += (_, _) => DialogResult = false;

        buttons.Children.Add(_applyButton);
        buttons.Children.Add(_discardButton);
        buttons.Children.Add(_closeButton);
        bottomPanel.Children.Add(buttons);

        Grid.SetRow(bottomPanel, 3);
        mainGrid.Children.Add(bottomPanel);

        Content = mainGrid;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        UpdateUiState();
    }

    private UIElement CreateItemRow(ScanReviewItemViewModel item)
    {
        var border = new Border
        {
            BorderBrush = SystemColors.ControlLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 0, 0, 6)
        };

        var rowGrid = new Grid();
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var infoPanel = new StackPanel { Margin = new Thickness(0, 0, 12, 0) };
        var titlePanel = new StackPanel { Orientation = Orientation.Horizontal };
        titlePanel.Children.Add(new TextBlock
        {
            Text = item.Name,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 8, 0)
        });
        titlePanel.Children.Add(new TextBlock
        {
            Text = $"<{item.Tag}>",
            Foreground = SystemColors.GrayTextBrush,
            Margin = new Thickness(0, 0, 8, 0)
        });
        titlePanel.Children.Add(new TextBlock
        {
            Text = $"[{item.Action}]",
            Foreground = SystemColors.GrayTextBrush
        });
        infoPanel.Children.Add(titlePanel);

        infoPanel.Children.Add(new TextBlock
        {
            Text = item.LocatorSummary,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11,
            Foreground = SystemColors.GrayTextBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0)
        });
        Grid.SetColumn(infoPanel, 0);
        rowGrid.Children.Add(infoPanel);

        var radioPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        var groupName = "Item_" + item.Id;

        var acceptRadio = new RadioButton
        {
            Content = item.AcceptLabel,
            GroupName = groupName,
            Margin = new Thickness(0, 0, 8, 0),
            ToolTip = item.AcceptToolTip,
            IsChecked = item.IsAccept
        };
        acceptRadio.Checked += (_, _) => item.IsAccept = true;

        var denyRadio = new RadioButton
        {
            Content = item.DenyLabel,
            GroupName = groupName,
            ToolTip = item.DenyToolTip,
            IsChecked = item.IsDeny
        };
        denyRadio.Checked += (_, _) => item.IsDeny = true;

        item.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ScanReviewItemViewModel.Decision) ||
                e.PropertyName == nameof(ScanReviewItemViewModel.IsAccept) ||
                e.PropertyName == nameof(ScanReviewItemViewModel.IsDeny))
            {
                acceptRadio.IsChecked = item.IsAccept;
                denyRadio.IsChecked = item.IsDeny;
            }
        };

        radioPanel.Children.Add(acceptRadio);
        radioPanel.Children.Add(denyRadio);
        Grid.SetColumn(radioPanel, 1);
        rowGrid.Children.Add(radioPanel);

        border.Child = rowGrid;
        return border;
    }

    private async Task OnApplyAsync()
    {
        _applyButton.IsEnabled = false;
        _discardButton.IsEnabled = false;
        _acceptAllButton.IsEnabled = false;
        _denyAllButton.IsEnabled = false;
        try
        {
            var success = await _viewModel.ApplyAsync(CancellationToken.None);
            if (success)
            {
                DialogResult = true;
                Close();
            }
        }
        finally
        {
            UpdateUiState();
        }
    }

    private async Task OnDiscardAsync()
    {
        var result = MessageBox.Show(
            this,
            $"Are you sure you want to discard the staged scan review for {_viewModel.PageName}? The page will remain unmodified.",
            "Discard Scan Review",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        _applyButton.IsEnabled = false;
        _discardButton.IsEnabled = false;
        try
        {
            var success = await _viewModel.DiscardAsync(CancellationToken.None);
            if (success)
            {
                DialogResult = false;
                Close();
            }
        }
        finally
        {
            UpdateUiState();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        UpdateUiState();
    }

    private void UpdateUiState()
    {
        _summaryText.Text = GetSummaryString();
        _applyButton.IsEnabled = _viewModel.CanApply;
        _discardButton.IsEnabled = _viewModel.CanDiscard;
        _acceptAllButton.IsEnabled = _viewModel.IsNotBusy;
        _denyAllButton.IsEnabled = _viewModel.IsNotBusy;

        if (_viewModel.HasConflict)
        {
            _conflictBorder.Visibility = Visibility.Visible;
            _conflictText.Text = _viewModel.ConflictMessage;
        }
        else if (!string.IsNullOrEmpty(_viewModel.ErrorMessage))
        {
            _conflictBorder.Visibility = Visibility.Visible;
            _conflictText.Text = _viewModel.ErrorMessage;
        }
        else
        {
            _conflictBorder.Visibility = Visibility.Collapsed;
            _conflictText.Text = string.Empty;
        }

        _statusText.Text = _viewModel.StatusMessage ?? string.Empty;
    }

    private string GetSummaryString() =>
        $"New: {_viewModel.NewCount}    Missing: {_viewModel.MissingCount}    Pending Decisions: {_viewModel.UnresolvedCount}";

    [SuppressMessage(
        "Usage",
        "VSSDK007:Await or join tasks created from ThreadHelper.JoinableTaskFactory.RunAsync",
        Justification = "The operation is intentionally fire-and-forget from a WPF event and is observed by FileAndForget.")]
    private static void RunOperation(Func<Task> operation)
    {
        ThreadHelper.JoinableTaskFactory
            .RunAsync(operation)
            .FileAndForget("Cliq/ScanReviewDialog");
    }
}


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
