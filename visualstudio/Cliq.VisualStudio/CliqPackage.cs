using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Cliq.Domain;
using Cliq.VisualStudio.Core;
using Cliq.VisualStudio.Core.Actions;
using Cliq.VisualStudio.Core.Api;
using Cliq.VisualStudio.Core.Authoring;
using Cliq.VisualStudio.Core.Desktop;
using Cliq.VisualStudio.Core.Runs;
using Cliq.VisualStudio.Core.Shell;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Cliq.VisualStudio;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[InstalledProductRegistration(
    "Cliq for Visual Studio",
    "Capture browser workflows and generate maintainable Selenium tests.",
    "0.1.15")]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideToolWindow(typeof(CliqWorkbenchToolWindow))]
[ProvideToolWindow(typeof(CliqRunResultsToolWindow))]
[ProvideToolWindow(typeof(CliqMergeToolWindow))]
[ProvideOptionPage(typeof(CliqOptionsPage), "Cliq", "General", 110, 111, true)]
[Guid(PackageGuidString)]
public sealed partial class CliqPackage : AsyncPackage, ICliqShellCommandTarget, ICliqWorkbenchCommands
{
    public const string PackageGuidString = "2C0AF2DA-640F-41B8-B02D-E847AF06C29E";
    private CliqOutputLogger? _logger;
    private CliqSidecarRuntime? _runtime;
    private VisualStudioContextService? _context;
    private VisualStudioCliqSession? _session;
    private WorkspaceActionsViewModel? _actions;
    private Task? _desktopHandoff;

    internal static CliqPackage? Current { get; private set; }

    internal WorkbenchViewModel? Workbench { get; private set; }

    internal WorkspaceActionsViewModel? Actions => _actions;

    internal RunResultsViewModel? RunResults { get; private set; }

    internal MergeViewModel? Merge { get; private set; }

    public bool HasSelectedTest => Workbench?.SelectedTest is not null;

    public bool HasSelectedPage => Workbench?.SelectedPage is not null;

    protected override async Task InitializeAsync(
        CancellationToken cancellationToken,
        IProgress<ServiceProgressData> progress)
    {
        Current = this;
        _logger = new CliqOutputLogger(this);
        await _logger.InitializeAsync(cancellationToken);
        _runtime = new CliqSidecarRuntime(_logger.Write);
        _context = new VisualStudioContextService(this);
        await _context.InitializeAsync(cancellationToken);
        _session = new VisualStudioCliqSession(this, _context, _runtime);
        var store = new ApiWorkbenchStore(_session);
        Workbench = new WorkbenchViewModel(store, new MessageBoxWorkbenchPrompt(), store);
        var actionStore = new ApiWorkspaceActionStore(_session);
        _actions = new WorkspaceActionsViewModel(actionStore);
        Merge = new MergeViewModel(actionStore);
        RunResults = new RunResultsViewModel(
            new ApiRunStore(_session, SynchronizationContext.Current));
        _context.ContextChanged += OnContextChanged;
        await new CliqCommandRegistrar(this, _context, this).InitializeAsync(cancellationToken);
        _logger.Write("Cliq extension initialized. The private sidecar will start with the first command.");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_context is not null)
            {
                _context.ContextChanged -= OnContextChanged;
                JoinableTaskFactory.Run(
                    async () =>
                    {
                        await JoinableTaskFactory.SwitchToMainThreadAsync();
                        _context.Dispose();
                    });
            }

            _session?.Dispose();
            _actions?.Dispose();
            RunResults?.Dispose();
            _runtime?.Dispose();
            Current = null;
        }

        base.Dispose(disposing);
    }

    [SuppressMessage(
        "Usage",
        "VSSDK007:Await or join tasks created from ThreadHelper.JoinableTaskFactory.RunAsync",
        Justification = "Solution events intentionally queue an observed transition operation.")]
    private void OnContextChanged(object? sender, EventArgs eventArgs)
    {
        JoinableTaskFactory
            .RunAsync(OnContextChangedAsync)
            .FileAndForget("Cliq/ContextChanged");
    }

    private async Task OnContextChangedAsync()
    {
        if (_session is null || Workbench is null)
        {
            return;
        }

        var nextRoot = await _session.ResolveCurrentWorkspaceAsync(DisposalToken);
        if (nextRoot is null ||
            string.Equals(nextRoot, _session.WorkspaceRoot, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (Workbench.IsDirty)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
            var decision = CliqMessageBox.AskYesNoCancel(
                "The Visual Studio workspace changed while Cliq has unsaved edits.\n\n" +
                "Yes: save before switching\nNo: discard and switch\nCancel: keep the current Cliq workspace",
                MessageBoxResult.Cancel);
            if (decision == MessageBoxResult.Cancel)
            {
                return;
            }

            if (decision == MessageBoxResult.Yes &&
                !await Workbench.SaveAllAsync(DisposalToken))
            {
                return;
            }
        }

        await _session.OpenAsync(nextRoot, DisposalToken);
        await Workbench.InitializeAsync(DisposalToken);
        _logger?.Write($"Cliq workspace bound to {nextRoot}.");
    }

    internal async Task OpenWorkbenchAsync(WorkbenchSection? section = null)
    {
        var window = await ShowToolWindowAsync(
            typeof(CliqWorkbenchToolWindow),
            0,
            create: true,
            DisposalToken);
        if (section is not null && window?.Content is WorkbenchControl control)
        {
            control.SelectSection(section.Value);
        }
    }

    internal async Task RefreshWorkspaceAsync()
    {
        if (Workbench is null)
        {
            return;
        }

        await OpenWorkbenchAsync();
        await Workbench.ReloadAsync(DisposalToken);
    }

    private async Task<bool> PrepareForPersistedActionAsync(string operation)
    {
        if (Workbench?.IsDirty != true)
        {
            return true;
        }

        await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
        var decision = CliqMessageBox.AskYesNoCancel(
            $"Cliq has unsaved edits. Save them before you {operation}?",
            MessageBoxResult.Yes);
        if (decision == MessageBoxResult.Cancel)
        {
            return false;
        }

        if (decision == MessageBoxResult.Yes)
        {
            return await Workbench.SaveAllAsync(DisposalToken);
        }

        await Workbench.InitializeAsync(DisposalToken);
        return true;
    }

    private async Task RunProgressAsync(
        string message,
        Func<CancellationToken, Task> operation)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
        var dialog = new CliqProgressDialog(message, operation);
        CliqDialogWindow.ShowOwned(dialog);
        if (dialog.Error is not null)
        {
            _logger?.Write(dialog.Error.ToString());
            CliqMessageBox.ShowError(dialog.Error.Message);
        }
    }

    private void ShowActionError()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var errorMessage = _actions?.ErrorMessage;
        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            // net472's string.IsNullOrWhiteSpace isn't annotated [NotNullWhen(false)], so the
            // compiler can't narrow errorMessage from this check alone the way it would on a
            // nullable-aware target framework.
            CliqMessageBox.ShowError(errorMessage!);
        }
    }

    private void OpenGeneratedFile(string? path)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
        {
            return;
        }

        var dte = GetService(typeof(DTE)) as DTE2;
        dte?.ItemOperations.OpenFile(path, Constants.vsViewKindCode);
    }

    // Explicit implementations forwarding to the existing internal members - keeps those members
    // internal for direct in-assembly callers while still satisfying ICliqWorkbenchCommands for
    // WorkbenchControl, since implicit interface implementation requires a public member.
    Task ICliqWorkbenchCommands.GenerateSelectedAsync() => GenerateSelectedAsync();

    Task ICliqWorkbenchCommands.ScanPageAsync() => ScanPageAsync();

    Task ICliqWorkbenchCommands.OpenGeneratedPageObjectAsync(string pageId) => OpenGeneratedPageObjectAsync(pageId);

    Task ICliqWorkbenchCommands.RunSelectedTestAsync() => RunSelectedTestAsync();

    WorkspaceActionsViewModel? ICliqWorkbenchCommands.Actions => Actions;

    async Task ICliqShellCommandTarget.ExecuteAsync(
        CliqShellCommand command,
        IdeContextSnapshot context,
        CancellationToken cancellationToken)
    {
        try
        {
            switch (command)
            {
                case CliqShellCommand.OpenWorkbench:
                    await OpenWorkbenchAsync();
                    break;
                case CliqShellCommand.Settings:
                    await OpenWorkbenchAsync(WorkbenchSection.Settings);
                    break;
                case CliqShellCommand.OpenMerge:
                    await OpenMergeAsync();
                    break;
                case CliqShellCommand.ScanPage:
                case CliqShellCommand.ScanSelectedUrl:
                    await ScanPageAsync();
                    break;
                case CliqShellCommand.RunSelectedTest:
                case CliqShellCommand.RunCliqTest:
                    await RunSelectedTestAsync();
                    break;
                case CliqShellCommand.GenerateSelected:
                case CliqShellCommand.GenerateCliqDocument:
                    await GenerateSelectedAsync();
                    break;
                case CliqShellCommand.GenerateAll:
                    await GenerateAllAsync();
                    break;
                case CliqShellCommand.Refresh:
                    await RefreshWorkspaceAsync();
                    break;
                case CliqShellCommand.BindWorkspace:
                case CliqShellCommand.OpenScopedWorkbench:
                case CliqShellCommand.BindProjectAndOpen:
                    if (await BindContextWorkspaceAsync(context))
                    {
                        await OpenWorkbenchAsync();
                    }
                    break;
                case CliqShellCommand.GenerateScopedAll:
                    if (await BindContextWorkspaceAsync(context))
                    {
                        await GenerateAllAsync();
                    }
                    break;
                case CliqShellCommand.OpenCliqDocument:
                    await OpenCliqDocumentAsync(context);
                    break;
                case CliqShellCommand.RescanCliqPage:
                    await SelectCliqDocumentAsync(context);
                    await ScanPageAsync();
                    break;
                case CliqShellCommand.RecordInDesktop:
                    await RecordInDesktopAsync();
                    break;
                case CliqShellCommand.LocateElementFromSource:
                    await LocateElementFromSourceAsync(context);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(command), command, null);
            }
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested || DisposalToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger?.Write($"Cliq command '{command}' failed: {exception}");
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            CliqMessageBox.ShowError(exception.Message, "Cliq command failed");
        }
    }

    private async Task<bool> BindContextWorkspaceAsync(IdeContextSnapshot context)
    {
        if (_context is null || _session is null || Workbench is null)
        {
            return false;
        }

        var root = WorkspacePathResolver.ResolveForSelectedScope(context);
        if (root is null)
        {
            return false;
        }

        if (!await PrepareForPersistedActionAsync("switch workspaces"))
        {
            return false;
        }

        await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
        if (context.SolutionDirectory is not null)
        {
            _context.SetOverride(context, root);
        }

        await _session.OpenAsync(root, DisposalToken);
        await Workbench.InitializeAsync(DisposalToken);
        _logger?.Write($"Cliq workspace explicitly bound to {root}.");
        return true;
    }

    private async Task OpenCliqDocumentAsync(IdeContextSnapshot context)
    {
        await OpenWorkbenchAsync();
        await SelectCliqDocumentAsync(context);
    }

    private async Task SelectCliqDocumentAsync(IdeContextSnapshot context)
    {
        if (Workbench is null || context.SelectedCliqEntityId is not { } entityId)
        {
            return;
        }

        if (context.SelectedDocumentKind == CliqDocumentKind.Test)
        {
            Workbench.SelectedTest = Workbench.Tests.FirstOrDefault(
                test => string.Equals(test.Id, entityId, StringComparison.Ordinal));
        }
        else if (context.SelectedDocumentKind == CliqDocumentKind.Page)
        {
            Workbench.SelectedPage = Workbench.Pages.FirstOrDefault(
                page => string.Equals(page.Id, entityId, StringComparison.Ordinal));
        }

        await Task.CompletedTask;
    }
}
