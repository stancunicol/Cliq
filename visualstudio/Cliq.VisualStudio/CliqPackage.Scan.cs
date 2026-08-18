using System.Threading;
using System.Threading.Tasks;
using Cliq.Domain;
using Cliq.VisualStudio.Core;
using Cliq.VisualStudio.Core.Actions;
using Cliq.VisualStudio.Core.Api;
using Cliq.VisualStudio.Core.Authoring;
using Microsoft.VisualStudio.Threading;

namespace Cliq.VisualStudio;

public sealed partial class CliqPackage
{
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
        var dialog = new ScanPageDialog(defaultUrl, defaultName, defaultBrowser);
        if (CliqDialogWindow.ShowOwned(dialog) != true)
        {
            return;
        }

        StagedPageScanResult? response = null;
        await RunProgressAsync(
            "Scanning the page with the installed browser...",
            async cancellationToken =>
            {
                response = await _actions.ScanReviewAsync(
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

        if (response.Review is null)
        {
            await Workbench.InitializeAsync(DisposalToken);
            await OpenWorkbenchAsync();
            await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
            var resultDialog = new ScanResultDialog(new ScanPageResponse
            {
                Page = response.ActivePage,
                Reconciliation = response.Reconciliation,
                DynamicElementSuggestions = response.DynamicElementSuggestions,
                ScreenshotPath = response.ScreenshotPath
            });
            CliqDialogWindow.ShowOwned(resultDialog);
        }
        else if (_session is not null)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
            var actionStore = new ApiWorkspaceActionStore(_session);
            var reviewVm = new ScanReviewViewModel(actionStore, response.Review);
            var reviewDialog = new ScanReviewDialog(reviewVm);
            if (CliqDialogWindow.ShowOwned(reviewDialog) == true)
            {
                await Workbench.InitializeAsync(DisposalToken);
                await OpenWorkbenchAsync();
            }
        }
    }

    internal async Task OpenScanReviewAsync(ScanReview review)
    {
        if (_session is null || Workbench is null)
        {
            return;
        }

        await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
        var actionStore = new ApiWorkspaceActionStore(_session);
        var reviewVm = new ScanReviewViewModel(actionStore, review);
        var reviewDialog = new ScanReviewDialog(reviewVm);
        if (CliqDialogWindow.ShowOwned(reviewDialog) == true)
        {
            await Workbench.InitializeAsync(DisposalToken);
            await OpenWorkbenchAsync();
        }
    }

    private async Task LocateElementFromSourceAsync(IdeContextSnapshot context)
    {
        if (_actions is null)
        {
            return;
        }

        await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
        var documentPath = context.ActiveDocumentPath ?? context.SelectedItemPath;
        var locatorComment = CliqDocumentClassifier.ResolveLocatorFieldComment(context.SelectedText, context.LineAboveSelection);
        if (documentPath is null || locatorComment is null)
        {
            return;
        }

        var className = System.IO.Path.GetFileNameWithoutExtension(documentPath);
        var succeeded = false;
        await RunProgressAsync(
            "Locating element...",
            async cancellationToken =>
                succeeded = await _actions.LocateElementFromSourceAsync(
                    className, locatorComment, browser: null, cancellationToken));
        if (!succeeded)
        {
            ShowActionError();
        }
    }
}
