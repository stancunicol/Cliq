using System;
using System.Threading.Tasks;
using Cliq.VisualStudio.Core.Desktop;
using Microsoft.VisualStudio.Threading;

namespace Cliq.VisualStudio;

public sealed partial class CliqPackage
{
    private async Task RecordInDesktopAsync()
    {
        if (_session is null || Workbench is null)
        {
            return;
        }

        if (_desktopHandoff is { IsCompleted: false })
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
            CliqMessageBox.ShowInfo("Cliq is already waiting for the Desktop recording session to finish.");
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
            CliqMessageBox.ShowWarning("Cliq Desktop was not found. Configure Cliq.exe under Tools > Options > Cliq > General.");
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
            CliqMessageBox.ShowError(exception.Message, "Cliq Desktop handoff failed");
        }
        finally
        {
            _desktopHandoff = null;
        }
    }
}
