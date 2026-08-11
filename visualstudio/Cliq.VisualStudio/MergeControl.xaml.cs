using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cliq.VisualStudio.Core.Authoring;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Cliq.VisualStudio;

public partial class MergeControl : UserControl
{
    private readonly MergeViewModel _viewModel;

    public MergeControl(MergeViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        DataContext = _viewModel;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        ApplyTheme();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        VSColorTheme.ThemeChanged -= OnThemeChanged;
        VSColorTheme.ThemeChanged += OnThemeChanged;
        ApplyTheme();
        RunOperation(() => _viewModel.RefreshWaitingListAsync());
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        VSColorTheme.ThemeChanged -= OnThemeChanged;
    }

    private void OnThemeChanged(ThemeChangedEventArgs e) => ApplyTheme();

    private void Resolve_Click(object sender, RoutedEventArgs e) =>
        RunOperation(() => _viewModel.ResolveAsync());

    private void RefreshWaitingList_Click(object sender, RoutedEventArgs e) =>
        RunOperation(() => _viewModel.RefreshWaitingListAsync());

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

    private void ApplyTheme()
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

    private void SetBrush(string key, ThemeResourceKey colorKey, double opacity = 1)
    {
        var color = VSColorTheme.GetThemedColor(colorKey);
        Resources[key] = new SolidColorBrush(
            Color.FromArgb(color.A, color.R, color.G, color.B))
        {
            Opacity = opacity,
        };
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

    [SuppressMessage(
        "Usage",
        "VSSDK007:Await or join tasks created from ThreadHelper.JoinableTaskFactory.RunAsync",
        Justification = "The WPF event intentionally starts an observed joinable task.")]
    private static void RunOperation(Func<System.Threading.Tasks.Task> operation)
    {
        ThreadHelper.JoinableTaskFactory
            .RunAsync(operation)
            .FileAndForget("Cliq/Merge");
    }
}
