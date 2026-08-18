using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cliq.VisualStudio.Core.Authoring;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Cliq.VisualStudio;

internal sealed class ScanReviewDialog : CliqDialogWindow
{
    // Named theme resources this dialog's imperatively-built elements bind to via
    // SetResourceReference, so the conflict/error banner and item rows re-theme along with the
    // rest of the window instead of carrying literal colors that would stay wrong in dark or
    // high-contrast VS themes. Populated in OnThemeApplied.
    private const string BannerBackgroundKey = "Cliq.ScanReview.BannerBackground";
    private const string BannerBorderKey = "Cliq.ScanReview.BannerBorder";
    private const string BannerTextKey = "Cliq.ScanReview.BannerText";
    private const string MutedForegroundKey = "Cliq.ScanReview.MutedForeground";
    private const string RowBorderKey = "Cliq.ScanReview.RowBorder";

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
        var pageUrlText = new TextBlock
        {
            Text = viewModel.PageUrl,
            Margin = new Thickness(0, 0, 0, 8)
        };
        pageUrlText.SetResourceReference(TextBlock.ForegroundProperty, MutedForegroundKey);
        headerPanel.Children.Add(pageUrlText);

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
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(CliqButtonTemplate.DefaultCornerRadius),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 10),
            Visibility = Visibility.Collapsed
        };
        _conflictBorder.SetResourceReference(Border.BackgroundProperty, BannerBackgroundKey);
        _conflictBorder.SetResourceReference(Border.BorderBrushProperty, BannerBorderKey);
        _conflictText = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap
        };
        _conflictText.SetResourceReference(TextBlock.ForegroundProperty, BannerTextKey);
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

    protected override void OnThemeApplied()
    {
        var bannerTint = Brush(EnvironmentColors.SystemInfoTextColorKey);
        Resources[BannerBackgroundKey] = new SolidColorBrush(bannerTint.Color) { Opacity = 0.16 };
        Resources[BannerBorderKey] = new SolidColorBrush(bannerTint.Color) { Opacity = 0.5 };
        Resources[BannerTextKey] = Brush(EnvironmentColors.ToolWindowValidationErrorTextColorKey);

        var muted = Brush(EnvironmentColors.ToolWindowTextColorKey);
        muted.Opacity = 0.72;
        Resources[MutedForegroundKey] = muted;

        Resources[RowBorderKey] = Brush(EnvironmentColors.ToolWindowBorderColorKey);
    }

    private UIElement CreateItemRow(ScanReviewItemViewModel item)
    {
        var border = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(CliqButtonTemplate.DefaultCornerRadius),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 0, 0, 6)
        };
        border.SetResourceReference(Border.BorderBrushProperty, RowBorderKey);

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
        var tagText = new TextBlock { Text = $"<{item.Tag}>", Margin = new Thickness(0, 0, 8, 0) };
        tagText.SetResourceReference(TextBlock.ForegroundProperty, MutedForegroundKey);
        titlePanel.Children.Add(tagText);

        var actionText = new TextBlock { Text = $"[{item.Action}]" };
        actionText.SetResourceReference(TextBlock.ForegroundProperty, MutedForegroundKey);
        titlePanel.Children.Add(actionText);
        infoPanel.Children.Add(titlePanel);

        var locatorText = new TextBlock
        {
            Text = item.LocatorSummary,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0)
        };
        locatorText.SetResourceReference(TextBlock.ForegroundProperty, MutedForegroundKey);
        infoPanel.Children.Add(locatorText);
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
