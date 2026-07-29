using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Threading;
using System.Threading.Tasks;
using Cliq.VisualStudio.Core;
using Cliq.VisualStudio.Core.Shell;
using Microsoft.VisualStudio.Shell;

namespace Cliq.VisualStudio;

internal interface ICliqShellCommandTarget
{
    Task ExecuteAsync(
        CliqShellCommand command,
        IdeContextSnapshot context,
        CancellationToken cancellationToken);
}

internal sealed class CliqCommandRegistrar
{
    private static readonly IReadOnlyDictionary<int, CliqShellCommand> CommandMap =
        new Dictionary<int, CliqShellCommand>
        {
            [CommandIds.OpenWorkbench] = CliqShellCommand.OpenWorkbench,
            [CommandIds.ScanPage] = CliqShellCommand.ScanPage,
            [CommandIds.RunSelectedTest] = CliqShellCommand.RunSelectedTest,
            [CommandIds.GenerateSelected] = CliqShellCommand.GenerateSelected,
            [CommandIds.GenerateAll] = CliqShellCommand.GenerateAll,
            [CommandIds.RecordInDesktop] = CliqShellCommand.RecordInDesktop,
            [CommandIds.Refresh] = CliqShellCommand.Refresh,
            [CommandIds.Settings] = CliqShellCommand.Settings,
            [CommandIds.BindWorkspace] = CliqShellCommand.BindWorkspace,
            [CommandIds.OpenScopedWorkbench] = CliqShellCommand.OpenScopedWorkbench,
            [CommandIds.GenerateScopedAll] = CliqShellCommand.GenerateScopedAll,
            [CommandIds.BindProjectAndOpen] = CliqShellCommand.BindProjectAndOpen,
            [CommandIds.OpenCliqDocument] = CliqShellCommand.OpenCliqDocument,
            [CommandIds.RunCliqTest] = CliqShellCommand.RunCliqTest,
            [CommandIds.GenerateCliqDocument] = CliqShellCommand.GenerateCliqDocument,
            [CommandIds.RescanCliqPage] = CliqShellCommand.RescanCliqPage,
            [CommandIds.ScanSelectedUrl] = CliqShellCommand.ScanSelectedUrl
        };

    private readonly AsyncPackage _package;
    private readonly VisualStudioContextService _context;
    private readonly ICliqShellCommandTarget _target;

    public CliqCommandRegistrar(
        AsyncPackage package,
        VisualStudioContextService context,
        ICliqShellCommandTarget target)
    {
        _package = package ?? throw new ArgumentNullException(nameof(package));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _target = target ?? throw new ArgumentNullException(nameof(target));
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var commandService =
            await _package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
        if (commandService is null)
        {
            throw new InvalidOperationException("Visual Studio command service is unavailable.");
        }

        await _package.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        foreach (var pair in CommandMap)
        {
            var command = new OleMenuCommand(
                Execute,
                new CommandID(CommandIds.CommandSet, pair.Key));
            command.BeforeQueryStatus += QueryStatus;
            commandService.AddCommand(command);
        }
    }

    private void QueryStatus(object sender, EventArgs eventArgs)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (sender is not OleMenuCommand menuCommand ||
            !CommandMap.TryGetValue(menuCommand.CommandID.ID, out var command))
        {
            return;
        }

        var state = CliqShellContext.Evaluate(command, _context.Capture());
        menuCommand.Visible = state.Visible;
        menuCommand.Enabled = state.Enabled;
        if (!string.IsNullOrWhiteSpace(state.Text))
        {
            menuCommand.Text = state.Text;
        }
    }

    private void Execute(object sender, EventArgs eventArgs)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        if (sender is not OleMenuCommand menuCommand ||
            !CommandMap.TryGetValue(menuCommand.CommandID.ID, out var command))
        {
            return;
        }

        var context = _context.Capture();
        _package.JoinableTaskFactory.RunAsync(
            () => _target.ExecuteAsync(command, context, CancellationToken.None))
            .FileAndForget("Cliq/ExecuteCommand");
    }
}
