using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cliq.VisualStudio.Core.Authoring;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Cliq.VisualStudio;

public partial class WorkbenchControl : UserControl
{
    private readonly WorkbenchViewModel _viewModel;
    private bool _initialized;

    public WorkbenchControl(WorkbenchViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        DataContext = _viewModel;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        PreviewKeyDown += OnPreviewKeyDown;
        ApplyTheme();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        VSColorTheme.ThemeChanged -= OnThemeChanged;
        VSColorTheme.ThemeChanged += OnThemeChanged;
        ApplyTheme();
        if (!_initialized)
        {
            _initialized = true;
            RunOperation(() => _viewModel.InitializeAsync());
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        VSColorTheme.ThemeChanged -= OnThemeChanged;
    }

    private void OnThemeChanged(ThemeChangedEventArgs e)
    {
        ApplyTheme();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.S || (Keyboard.Modifiers & ModifierKeys.Control) == 0)
        {
            return;
        }

        e.Handled = true;
        if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
        {
            _ = _viewModel.SaveAllAsync();
        }
        else
        {
            _ = _viewModel.SaveSelectedAsync();
        }
    }

    private void NewTest_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.CreateTest();
        MainTabs.SelectedIndex = 0;
    }

    private void DeleteTest_Click(object sender, RoutedEventArgs e)
    {
        RunOperation(async () => { await _viewModel.DeleteSelectedTestAsync(); });
    }

    private void SaveSelected_Click(object sender, RoutedEventArgs e)
    {
        RunOperation(async () => { await _viewModel.SaveSelectedAsync(); });
    }

    private void SaveAll_Click(object sender, RoutedEventArgs e)
    {
        RunOperation(async () => { await _viewModel.SaveAllAsync(); });
    }

    private void Reload_Click(object sender, RoutedEventArgs e)
    {
        RunOperation(async () => { await _viewModel.ReloadAsync(); });
    }

    private void AddStep_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SelectedTest?.AddStep();
    }

    private void DuplicateStep_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SelectedTest?.DuplicateSelectedStep();
    }

    private void DeleteStep_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SelectedTest?.DeleteSelectedStep();
    }

    private void MoveStepUp_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SelectedTest?.MoveSelectedStep(-1);
    }

    private void MoveStepDown_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SelectedTest?.MoveSelectedStep(1);
    }

    private void ApplyTheme()
    {
        SetBrush("Cliq.Background", EnvironmentColors.ToolWindowBackgroundColorKey);
        SetBrush("Cliq.SidebarBackground", EnvironmentColors.CommandBarGradientBeginColorKey);
        SetBrush("Cliq.ToolbarBackground", EnvironmentColors.CommandBarGradientMiddleColorKey);
        SetBrush("Cliq.InputBackground", EnvironmentColors.ComboBoxBackgroundColorKey);
        SetBrush("Cliq.Foreground", EnvironmentColors.ToolWindowTextColorKey);
        SetBrush("Cliq.MutedForeground", EnvironmentColors.ToolWindowTextColorKey, 0.72);
        SetBrush("Cliq.Border", EnvironmentColors.ToolWindowBorderColorKey);
        SetBrush("Cliq.ButtonBackground", EnvironmentColors.CommandBarGradientBeginColorKey);
        SetBrush("Cliq.Accent", EnvironmentColors.SystemHighlightColorKey);
        SetBrush("Cliq.Warning", EnvironmentColors.SystemInfoTextColorKey);
        SetBrush("Cliq.Error", EnvironmentColors.SystemHighlightColorKey);
    }

    private void SetBrush(string resourceKey, ThemeResourceKey colorKey, double opacity = 1)
    {
        var color = VSColorTheme.GetThemedColor(colorKey);
        Resources[resourceKey] = new SolidColorBrush(
            Color.FromArgb(color.A, color.R, color.G, color.B))
        {
            Opacity = opacity,
        };
    }

    [SuppressMessage(
        "Usage",
        "VSSDK007:Await or join tasks created from ThreadHelper.JoinableTaskFactory.RunAsync",
        Justification = "The operation is intentionally fire-and-forget from a WPF event and is observed by FileAndForget.")]
    private static void RunOperation(Func<System.Threading.Tasks.Task> operation)
    {
        ThreadHelper.JoinableTaskFactory
            .RunAsync(operation)
            .FileAndForget("Cliq/Workbench");
    }
}
