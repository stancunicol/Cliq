using System;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
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
    "Integrates Cliq UI test automation into Visual Studio.",
    "0.1.0")]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideToolWindow(typeof(CliqWorkbenchToolWindow))]
[ProvideToolWindow(typeof(CliqRunResultsToolWindow))]
[ProvideOptionPage(typeof(CliqOptionsPage), "Cliq", "General", 0, 0, true)]
[Guid(PackageGuidString)]
public sealed class CliqPackage : AsyncPackage, ICliqShellCommandTarget
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
        _actions = new WorkspaceActionsViewModel(new ApiWorkspaceActionStore(_session));
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
            var decision = MessageBox.Show(
                "The Visual Studio workspace changed while Cliq has unsaved edits.\n\n" +
                "Yes: save before switching\nNo: discard and switch\nCancel: keep the current Cliq workspace",
                "Cliq",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning,
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

    internal async Task OpenWorkbenchAsync()
    {
        await ShowToolWindowAsync(
            typeof(CliqWorkbenchToolWindow),
            0,
            create: true,
            DisposalToken);
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

    internal async Task ScanPageAsync()
    {
        if (_context is null || Workbench is null || _actions is null ||
            !await PrepareForPersistedActionAsync("scan a page"))
        {
            return;
        }

        await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
        var context = _context.Capture();
        CliqDocumentClassifier.TryParseHttpSelection(context.SelectedText, out var selectedUrl);
        var defaultUrl = selectedUrl?.AbsoluteUri
            ?? Workbench.SelectedPage?.Url
            ?? Workbench.Project?.BaseUrl;
        var defaultName = Workbench.SelectedPage?.Name
            ?? (selectedUrl is null ? null : selectedUrl.Host);
        var defaultBrowser = Workbench.Project?.DefaultBrowser ?? BrowserKind.Chrome;
        var dialog = new ScanPageDialog(defaultUrl, defaultName, defaultBrowser)
        {
            Owner = Application.Current?.MainWindow,
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        ScanPageResponse? response = null;
        await RunProgressAsync(
            "Scanning the page with the installed browser...",
            async cancellationToken =>
            {
                response = await _actions.ScanAsync(
                    dialog.PageUrl,
                    dialog.PageName,
                    dialog.Browser,
                    dialog.Headless,
                    cancellationToken);
            });
        if (response is null)
        {
            ShowActionError();
            return;
        }

        await Workbench.InitializeAsync(DisposalToken);
        await OpenWorkbenchAsync();
        await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
        var report = response.Reconciliation;
        MessageBox.Show(
            $"Scan complete.\n\nMatched: {report.MatchedCount}\nNew: {report.NewCount}\nMissing: {report.MissingCount}",
            "Cliq",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    internal async Task GenerateSelectedAsync()
    {
        if (_context is null || Workbench is null || _actions is null ||
            !await PrepareForPersistedActionAsync("generate the selected item"))
        {
            return;
        }

        await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
        var context = _context.Capture();
        var kind = context.SelectedDocumentKind;
        var id = context.SelectedCliqEntityId;
        if (kind == CliqDocumentKind.Other)
        {
            if (Workbench.SelectedTest is not null)
            {
                kind = CliqDocumentKind.Test;
                id = Workbench.SelectedTest.Id;
            }
            else if (Workbench.SelectedPage is not null)
            {
                kind = CliqDocumentKind.Page;
                id = Workbench.SelectedPage.Id;
            }
        }

        if (id is null || kind == CliqDocumentKind.Other)
        {
            MessageBox.Show(
                "Select a Cliq test or page in Solution Explorer or the Workbench.",
                "Cliq",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        await GenerateAsync(
            cancellationToken => _actions.GenerateSelectedAsync(kind, id, cancellationToken));
    }

    internal async Task GenerateAllAsync()
    {
        if (_actions is null || !await PrepareForPersistedActionAsync("generate all artifacts"))
        {
            return;
        }

        await GenerateAsync(_actions.GenerateAllAsync);
    }

    internal async Task RunSelectedTestAsync()
    {
        if (_context is null || _session is null || Workbench is null || RunResults is null ||
            !await PrepareForPersistedActionAsync("run the selected test"))
        {
            return;
        }

        await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
        var context = _context.Capture();
        var testId = context.SelectedDocumentKind == CliqDocumentKind.Test
            ? context.SelectedCliqEntityId
            : Workbench.SelectedTest?.Id;
        if (string.IsNullOrWhiteSpace(testId))
        {
            MessageBox.Show(
                "Select a Cliq test in Solution Explorer or the Workbench.",
                "Cliq",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var test = Workbench.Tests
            .FirstOrDefault(candidate =>
                string.Equals(candidate.Id, testId, StringComparison.Ordinal))
            ?.ToModel();
        if (test is null)
        {
            var client = await _session.GetClientAsync(DisposalToken);
            test = await client.GetTestAsync(testId!, DisposalToken);
        }

        await ShowToolWindowAsync(
            typeof(CliqRunResultsToolWindow),
            0,
            create: true,
            DisposalToken);
        var browser = Workbench.Project?.DefaultBrowser ?? BrowserKind.Chrome;
        await RunResults.StartAsync(test, browser, DisposalToken);
    }

    internal async Task CancelRunAsync()
    {
        if (RunResults is not null)
        {
            await RunResults.CancelAsync(DisposalToken);
        }
    }

    private async Task GenerateAsync(
        Func<CancellationToken, Task<GenerationResult?>> generate)
    {
        GenerationResult? result = null;
        await RunProgressAsync(
            "Generating Cliq page objects and tests...",
            async cancellationToken => result = await generate(cancellationToken));
        if (result is null)
        {
            ShowActionError();
            return;
        }

        await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
        var dialog = new GenerationResultDialog(result.Artifacts)
        {
            Owner = Application.Current?.MainWindow,
        };
        dialog.OpenRequested += OpenGeneratedFile;
        dialog.ShowDialog();
    }

    private async Task<bool> PrepareForPersistedActionAsync(string operation)
    {
        if (Workbench?.IsDirty != true)
        {
            return true;
        }

        await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
        var decision = MessageBox.Show(
            $"Cliq has unsaved edits. Save them before you {operation}?",
            "Cliq",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning,
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
        var dialog = new CliqProgressDialog(message, operation)
        {
            Owner = Application.Current?.MainWindow,
        };
        dialog.ShowDialog();
        if (dialog.Error is not null)
        {
            _logger?.Write(dialog.Error.ToString());
            MessageBox.Show(
                dialog.Error.Message,
                "Cliq",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ShowActionError()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        var errorMessage = _actions?.ErrorMessage;
        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            MessageBox.Show(
                errorMessage,
                "Cliq",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
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
                case CliqShellCommand.Settings:
                    await OpenWorkbenchAsync();
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
            MessageBox.Show(
                exception.Message,
                "Cliq command failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
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

    private async Task RecordInDesktopAsync()
    {
        if (_session is null || Workbench is null)
        {
            return;
        }

        if (_desktopHandoff is { IsCompleted: false })
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
            MessageBox.Show(
                "Cliq is already waiting for the Desktop recording session to finish.",
                "Cliq",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        if (!await PrepareForPersistedActionAsync("record in Cliq Desktop"))
        {
            return;
        }

        var workspaceRoot = _session.WorkspaceRoot
            ?? await _session.ResolveCurrentWorkspaceAsync(DisposalToken);
        if (workspaceRoot is null)
        {
            throw new InvalidOperationException("Open or bind a Cliq workspace before recording.");
        }

        await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
        var options = (CliqOptionsPage)GetDialogPage(typeof(CliqOptionsPage));
        var executable = CliqDesktopDiscovery.FindExecutable(options.DesktopExecutablePath);
        if (executable is null)
        {
            MessageBox.Show(
                "Cliq Desktop was not found. Configure Cliq.exe under Tools > Options > Cliq > General.",
                "Cliq",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var coordinator = new DesktopHandoffCoordinator(
            new VisualStudioWorkspaceLease(_session),
            new ProcessDesktopApplicationLauncher());
        try
        {
            _desktopHandoff = coordinator.HandoffAsync(
                executable,
                workspaceRoot,
                message => _logger?.Write(message),
                DisposalToken);
            await _desktopHandoff;
            await Workbench.InitializeAsync(DisposalToken);
        }
        catch (OperationCanceledException) when (DisposalToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger?.Write($"Cliq Desktop handoff failed: {exception}");
            await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
            MessageBox.Show(
                exception.Message,
                "Cliq Desktop handoff failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            _desktopHandoff = null;
        }
    }
}
