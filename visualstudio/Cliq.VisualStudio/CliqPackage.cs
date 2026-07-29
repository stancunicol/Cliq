using System;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Cliq.Domain;
using Cliq.VisualStudio.Core;
using Cliq.VisualStudio.Core.Actions;
using Cliq.VisualStudio.Core.Api;
using Cliq.VisualStudio.Core.Authoring;
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
[Guid(PackageGuidString)]
public sealed class CliqPackage : AsyncPackage
{
    public const string PackageGuidString = "2C0AF2DA-640F-41B8-B02D-E847AF06C29E";
    private CliqOutputLogger? _logger;
    private CliqSidecarRuntime? _runtime;
    private VisualStudioContextService? _context;
    private VisualStudioCliqSession? _session;
    private WorkspaceActionsViewModel? _actions;

    internal static CliqPackage? Current { get; private set; }

    internal WorkbenchViewModel? Workbench { get; private set; }

    internal WorkspaceActionsViewModel? Actions => _actions;

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
        _context.ContextChanged += OnContextChanged;
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
}
