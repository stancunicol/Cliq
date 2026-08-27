using System;
using System.Threading;
using System.Threading.Tasks;
using Cliq.Domain;
using Cliq.VisualStudio.Core;
using Microsoft.VisualStudio.Threading;

namespace Cliq.VisualStudio;

public sealed partial class CliqPackage
{
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
            CliqMessageBox.ShowInfo("Select a Cliq test or page in Solution Explorer or the Workbench.");
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
        var dialog = new GenerationResultDialog(result);
        dialog.OpenRequested += OpenGeneratedFile;
        CliqDialogWindow.ShowOwned(dialog);

        if (result.PendingMerges.Count > 0)
        {
            await OpenMergeAsync(result.PendingMerges);
        }
    }

    internal async Task OpenGeneratedPageObjectAsync(string pageId)
    {
        var path = await ResolveGeneratedPageObjectPathAsync(pageId);
        if (path is null)
        {
            return;
        }

        await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
        if (!System.IO.File.Exists(path))
        {
            CliqMessageBox.ShowInfo("This page hasn't been generated yet. Use Regenerate first.");
            return;
        }

        OpenGeneratedFile(path);
    }

    public async Task<bool> HasGeneratedPageObjectAsync(string pageId)
    {
        var path = await ResolveGeneratedPageObjectPathAsync(pageId);
        return path is not null && System.IO.File.Exists(path);
    }

    private async Task<string?> ResolveGeneratedPageObjectPathAsync(string pageId)
    {
        if (_session is null)
        {
            return null;
        }

        try
        {
            var client = await _session.GetClientAsync(DisposalToken);
            return (await client.GetGeneratedPageObjectPathAsync(pageId, DisposalToken)).Path;
        }
        catch (OperationCanceledException) when (DisposalToken.IsCancellationRequested)
        {
            return null;
        }
        catch (Exception exception)
        {
            _logger?.Write($"Failed to resolve the generated file path: {exception}");
            return null;
        }
    }
}
