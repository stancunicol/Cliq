using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cliq.VisualStudio.Core.Authoring;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace Cliq.VisualStudio;

internal enum WorkbenchSection
{
    TestEditor,
    PageInspector,
    Project,
    Settings,
    Secrets,
}

public partial class WorkbenchControl : CliqThemedUserControl
{
    private readonly WorkbenchViewModel _viewModel;
    private bool _initialized;

    public WorkbenchControl(WorkbenchViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        InitializeComponent();
        DataContext = _viewModel;
        PreviewKeyDown += OnPreviewKeyDown;
    }

    protected override string DiagnosticTag => "Cliq/Workbench";

    protected override void OnControlLoaded()
    {
        if (!_initialized)
        {
            _initialized = true;
            RunOperation(() => _viewModel.InitializeAsync());
        }
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
            RunOperation(async () => { await _viewModel.SaveAllAsync(); });
        }
        else
        {
            RunOperation(async () => { await _viewModel.SaveSelectedAsync(); });
        }
    }

    private void NewTest_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.CreateTest();
        SelectSection(WorkbenchSection.TestEditor);
    }

    internal void SelectSection(WorkbenchSection section)
    {
        MainTabs.SelectedIndex = (int)section;
        if (section == WorkbenchSection.TestEditor)
        {
            NavigatorTabs.SelectedIndex = 0;
        }
        else if (section == WorkbenchSection.PageInspector)
        {
            NavigatorTabs.SelectedIndex = 1;
        }
    }

    private void Tests_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel.SelectedTest != null)
        {
            SelectSection(WorkbenchSection.TestEditor);
        }
    }

    private void RemoveNotFoundElements_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.SelectedPage?.RemoveNotFoundElements();
    }

    private void ElementsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LocateElementButton.IsEnabled = ElementsGrid.SelectedItem is PageElementInspectorViewModel;
    }

    private void ElementsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        LocateSelectedElement();
    }

    private void LocateElement_Click(object sender, RoutedEventArgs e)
    {
        LocateSelectedElement();
    }

    private void RegeneratePage_Click(object sender, RoutedEventArgs e)
    {
        var package = CliqPackage.Current;
        if (package != null)
        {
            RunOperation(package.GenerateSelectedAsync);
        }
    }

    private void RescanPage_Click(object sender, RoutedEventArgs e)
    {
        var package = CliqPackage.Current;
        if (package != null)
        {
            RunOperation(package.ScanPageAsync);
        }
    }

    private void OpenGeneratedFile_Click(object sender, RoutedEventArgs e)
    {
        var pageId = _viewModel.SelectedPage?.Id;
        var package = CliqPackage.Current;
        if (pageId is null || package is null)
        {
            return;
        }

        RunOperation(async () => { await package.OpenGeneratedPageObjectAsync(pageId); });
    }

    private void LocateSelectedElement()
    {
        var pageId = _viewModel.SelectedPage?.Id;
        if (pageId is null || ElementsGrid.SelectedItem is not PageElementInspectorViewModel element)
        {
            return;
        }

        var actions = CliqPackage.Current?.Actions;
        if (actions is null)
        {
            return;
        }

        RunOperation(async () => { await actions.LocateElementAsync(pageId, element.Model.Id, browser: null); });
    }

    private void Pages_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel.SelectedPage != null)
        {
            SelectSection(WorkbenchSection.PageInspector);
        }
    }

    private void DeleteTest_Click(object sender, RoutedEventArgs e)
    {
        RunOperation(async () => { await _viewModel.DeleteSelectedTestAsync(); });
    }

    private void SaveSelected_Click(object sender, RoutedEventArgs e)
    {
        RunOperation(async () => { await _viewModel.SaveSelectedAsync(); });
    }

    private void RunTest_Click(object sender, RoutedEventArgs e)
    {
        var package = CliqPackage.Current;
        if (package != null)
        {
            RunOperation(package.RunSelectedTestAsync);
        }
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

    private void SaveProject_Click(object sender, RoutedEventArgs e)
    {
        RunOperation(async () => { await _viewModel.SaveProjectAsync(); });
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        RunOperation(async () => { await _viewModel.SaveSettingsAsync(); });
    }

    private void AddStrategy_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.Settings?.AddSelectedStrategy();
    }

    private void RemoveStrategy_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.Settings?.RemoveStrategy(StrategyList.SelectedItem as string);
    }

    private void MoveStrategyUp_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.Settings?.MoveStrategy(StrategyList.SelectedItem as string, -1);
    }

    private void MoveStrategyDown_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.Settings?.MoveStrategy(StrategyList.SelectedItem as string, 1);
    }

    private void AddSecret_Click(object sender, RoutedEventArgs e)
    {
        ShowSecretEditor(null);
    }

    private void UpdateSecret_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedSecret != null)
        {
            ShowSecretEditor(_viewModel.SelectedSecret.Key);
        }
    }

    private void DeleteSecret_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedSecret is not { } secret ||
            MessageBox.Show(
                Window.GetWindow(this),
                $"Delete secret '{secret.Key}'? This cannot be undone.",
                "Cliq",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No) != MessageBoxResult.Yes)
        {
            return;
        }

        RunOperation(async () => { await _viewModel.DeleteSelectedSecretAsync(); });
    }

    private void ShowSecretEditor(string? existingKey)
    {
        var dialog = new SecretValueDialog(existingKey)
        {
            Owner = Window.GetWindow(this),
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var key = dialog.SecretKey;
        var secret = dialog.TakeSecret();
        RunOperation(
            async () =>
            {
                try
                {
                    await _viewModel.SaveSecretAsync(key, secret);
                }
                finally
                {
                    secret = string.Empty;
                }
            });
    }

    protected override void ApplyTheme()
    {
        SetBrush("Cliq.Background", EnvironmentColors.ToolWindowBackgroundColorKey);
        SetBrush("Cliq.SidebarBackground", EnvironmentColors.CommandBarGradientBeginColorKey);
        SetBrush("Cliq.ToolbarBackground", EnvironmentColors.CommandBarGradientMiddleColorKey);
        SetBrush("Cliq.InputBackground", EnvironmentColors.ToolWindowBackgroundColorKey);
        SetBrush("Cliq.ComboBackground", EnvironmentColors.ComboBoxBackgroundColorKey);
        SetBrush("Cliq.ComboForeground", EnvironmentColors.ComboBoxTextColorKey);
        SetBrush("Cliq.Foreground", EnvironmentColors.ToolWindowTextColorKey);
        SetBrush("Cliq.MutedForeground", EnvironmentColors.ToolWindowTextColorKey, 0.72);
        SetBrush("Cliq.Border", EnvironmentColors.ToolWindowBorderColorKey);
        SetBrush("Cliq.ButtonBackground", EnvironmentColors.CommandBarGradientBeginColorKey);
        SetBrush("Cliq.Accent", EnvironmentColors.SystemHighlightColorKey);
        SetBrush("Cliq.SelectedForeground", EnvironmentColors.SystemHighlightTextColorKey);
        SetBrush("Cliq.Warning", EnvironmentColors.SystemInfoTextColorKey);
        SetBrush("Cliq.Error", EnvironmentColors.ToolWindowValidationErrorTextColorKey);
    }
}
