using System;
using System.Linq;
using System.Threading.Tasks;
using Cliq.Domain;
using Cliq.VisualStudio.Core;
using Microsoft.VisualStudio.Threading;

namespace Cliq.VisualStudio;

public sealed partial class CliqPackage
{
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
            CliqMessageBox.ShowInfo("Select a Cliq test in Solution Explorer or the Workbench.");
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
        if (RunResults.SelectedBrowser == BrowserKind.Chrome)
        {
            RunResults.SelectedBrowser = Workbench.Project?.DefaultBrowser ?? BrowserKind.Chrome;
        }

        await RunResults.StartAsync(test, RunResults.SelectedBrowser, DisposalToken);
    }

    internal async Task CancelRunAsync()
    {
        if (RunResults is not null)
        {
            await RunResults.CancelAsync(DisposalToken);
        }
    }
}
