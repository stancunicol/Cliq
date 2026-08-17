using System;
using System.Windows;
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
