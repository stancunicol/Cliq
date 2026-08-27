using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cliq.VisualStudio.Core.Actions;
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
    private readonly ICliqWorkbenchCommands? _commands;
    private bool _initialized;
    private WorkspaceActionsViewModel? _hoverActions;

    internal WorkbenchControl(WorkbenchViewModel viewModel, ICliqWorkbenchCommands? commands = null)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _commands = commands;
        InitializeComponent();
        DataContext = _viewModel;
        PreviewKeyDown += OnPreviewKeyDown;
        Unloaded += WorkbenchControl_Unloaded;
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

    private void ElementsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        LocateSelectedElement();
    }

    private void RegeneratePage_Click(object sender, RoutedEventArgs e)
    {
        if (_commands != null)
        {
            RunOperation(_commands.GenerateSelectedAsync);
        }
    }

    private void RescanPage_Click(object sender, RoutedEventArgs e)
    {
        if (_commands != null)
        {
            RunOperation(_commands.ScanPageAsync);
        }
    }

    private void OpenGeneratedFile_Click(object sender, RoutedEventArgs e)
    {
        var pageId = _viewModel.SelectedPage?.Id;
        if (pageId is null || _commands is null)
        {
            return;
        }

        RunOperation(async () => { await _commands.OpenGeneratedPageObjectAsync(pageId); });
    }

    private void LocateSelectedElement()
    {
        var pageId = _viewModel.SelectedPage?.Id;
        if (pageId is null || ElementsGrid.SelectedItem is not PageElementInspectorViewModel element)
        {
            return;
        }

        var actions = _commands?.Actions;
        if (actions is null)
        {
            return;
        }

        RunOperation(async () => { await actions.LocateElementAsync(pageId, element.Model.Id, browser: null); });
    }

    private void Pages_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // A live-highlight stream is bound to whichever page it was started for - switching pages
        // makes it stale (it would keep reporting hovers for a page the grid no longer shows), so
        // the toggle turns itself off rather than silently keeping a stream open for a page that's
        // no longer visible. Unchecking raises LiveHighlightToggle_Unchecked, which does the
        // actual StopLiveHighlight().
        if (LiveHighlightToggle.IsChecked == true)
        {
            LiveHighlightToggle.IsChecked = false;
        }

        if (_viewModel.SelectedPage != null)
        {
            SelectSection(WorkbenchSection.PageInspector);
        }
    }

    private void LiveHighlightToggle_Checked(object sender, RoutedEventArgs e)
    {
        var pageId = _viewModel.SelectedPage?.Id;
        var actions = _commands?.Actions;
        if (pageId is null || actions is null)
        {
            LiveHighlightToggle.IsChecked = false;
            return;
        }

        SubscribeToHoverActions(actions);
        actions.StartLiveHighlight(pageId, browser: null);
    }

    private void LiveHighlightToggle_Unchecked(object sender, RoutedEventArgs e) =>
        _commands?.Actions?.StopLiveHighlight();

    private void SubscribeToHoverActions(WorkspaceActionsViewModel actions)
    {
        if (ReferenceEquals(_hoverActions, actions))
        {
            return;
        }

        if (_hoverActions is not null)
        {
            _hoverActions.PropertyChanged -= HoverActions_PropertyChanged;
        }

        _hoverActions = actions;
        actions.PropertyChanged += HoverActions_PropertyChanged;
    }

    private void HoverActions_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WorkspaceActionsViewModel.HighlightedElementId))
        {
            _viewModel.SelectedPage?.SetHighlightedElement(_hoverActions?.HighlightedElementId);
        }
        else if (e.PropertyName == nameof(WorkspaceActionsViewModel.IsLiveHighlightActive)
                 && _hoverActions?.IsLiveHighlightActive == false)
        {
            // The stream ended on its own (disconnect, error) rather than through the checkbox -
            // reflect that back onto the toggle instead of leaving it checked with nothing
            // actually running behind it.
            LiveHighlightToggle.IsChecked = false;
        }
    }

    private void WorkbenchControl_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_hoverActions is null)
        {
            return;
        }

        _hoverActions.PropertyChanged -= HoverActions_PropertyChanged;
        _hoverActions.StopLiveHighlight();
        _hoverActions = null;
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
        if (_commands != null)
        {
            RunOperation(_commands.RunSelectedTestAsync);
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
            !CliqMessageBox.ConfirmYesNo(Window.GetWindow(this), $"Delete secret '{secret.Key}'? This cannot be undone."))
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
