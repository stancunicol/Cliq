using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Cliq.VisualStudio.Core.Authoring;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace Cliq.VisualStudio;

public partial class MergeControl : CliqThemedUserControl
{
    private readonly MergeViewModel _viewModel;

    public MergeControl(MergeViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        DataContext = _viewModel;
    }

    protected override string DiagnosticTag => "Cliq/Merge";

    protected override void OnControlLoaded() =>
        RunOperation(() => _viewModel.RefreshWaitingListSafeAsync());

    private void Resolve_Click(object sender, RoutedEventArgs e) =>
        RunOperation(() => _viewModel.ResolveAsync());

    private void RefreshWaitingList_Click(object sender, RoutedEventArgs e) =>
        RunOperation(() => _viewModel.RefreshWaitingListSafeAsync());

    private void IncludeNow_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is WaitingListEntryViewModel entry)
        {
            RunOperation(() => _viewModel.IncludeNowAsync(entry));
        }
    }

    private void Discard_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is WaitingListEntryViewModel entry)
        {
            RunOperation(() => _viewModel.DiscardAsync(entry));
        }
    }

    // DataGrid has no built-in "click the selected row again to deselect it" behavior - selecting
    // a row is what shows its RowDetails preview (RowDetailsVisibilityMode="VisibleWhenSelected"),
    // so without this, a second click on an already-selected row does nothing and the preview stays
    // open. Walks up from wherever the click landed; a ButtonBase ancestor (Include now/Discard)
    // found before reaching the row means this was a button click, not a row click, so it's left
    // alone rather than intercepted.
    private void WaitingListGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var current = e.OriginalSource as DependencyObject;
        while (current is not null and not DataGridRow)
        {
            if (current is ButtonBase)
            {
                return;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        if (current is DataGridRow { IsSelected: true } row)
        {
            row.IsSelected = false;
            WaitingListGrid.SelectedItem = null;
            e.Handled = true;
        }
    }

    protected override void ApplyTheme()
    {
        SetBrush("Cliq.Merge.Background", EnvironmentColors.ToolWindowBackgroundColorKey);
        SetBrush("Cliq.Merge.ToolbarBackground", EnvironmentColors.CommandBarGradientBeginColorKey);
        SetDarkenedBrush("Cliq.Merge.TabActiveBackground", EnvironmentColors.CommandBarGradientBeginColorKey, 0.8);
        SetBrush("Cliq.Merge.InputBackground", EnvironmentColors.ToolWindowBackgroundColorKey);
        SetBrush("Cliq.Merge.Foreground", EnvironmentColors.ToolWindowTextColorKey);
        SetBrush("Cliq.Merge.MutedForeground", EnvironmentColors.ToolWindowTextColorKey, 0.72);
        SetBrush("Cliq.Merge.Border", EnvironmentColors.ToolWindowBorderColorKey);
        SetBrush("Cliq.Merge.ButtonBackground", EnvironmentColors.CommandBarGradientMiddleColorKey);
        SetBrush("Cliq.Merge.Accent", EnvironmentColors.SystemHighlightColorKey);
        SetBrush("Cliq.Merge.SelectedForeground", EnvironmentColors.SystemHighlightTextColorKey);
        SetBrush("Cliq.Merge.Warning", EnvironmentColors.SystemInfoTextColorKey);
        SetBrush("Cliq.Merge.Error", EnvironmentColors.ToolWindowValidationErrorTextColorKey);
    }

    // Used for the tab hover/selected background - a plain opacity change (like SetBrush's
    // Opacity) would let the tool window's own background show through at the edges instead of
    // reading as "a shade darker", so this scales each RGB channel down instead.
    private void SetDarkenedBrush(string key, ThemeResourceKey colorKey, double factor)
    {
        var color = VSColorTheme.GetThemedColor(colorKey);
        Resources[key] = new SolidColorBrush(
            Color.FromArgb(
                color.A,
                (byte)(color.R * factor),
                (byte)(color.G * factor),
                (byte)(color.B * factor)));
    }
}
