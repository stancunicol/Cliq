using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cliq.Domain;
using Cliq.VisualStudio.Core.Actions;
using Cliq.VisualStudio.Core.Api;

namespace Cliq.VisualStudio.Core.Authoring;

public enum CustomMethodDecision
{
    CarryOver,
    Defer,
    Discard,
}

public sealed class CustomMethodCandidateViewModel : BindableBase
{
    private bool _isExpanded;
    private CustomMethodDecision _decision;

    public CustomMethodCandidateViewModel(CustomMethodCandidate model)
    {
        Model = model;
        // A method the extractor couldn't cleanly recover or that fails a real parse defaults to
        // Defer, not CarryOver - injecting known-broken code into the regenerated file by default
        // would trade one problem (a lost method) for a worse one (a page object that no longer
        // compiles).
        _decision = model.HasSyntaxError ? CustomMethodDecision.Defer : CustomMethodDecision.CarryOver;
    }

    public CustomMethodCandidate Model { get; }

    public string Signature => Model.Signature;

    public string SourceText => Model.SourceText;

    public bool HasSyntaxError => Model.HasSyntaxError;

    public string SyntaxErrorSummary => string.Join(Environment.NewLine, Model.SyntaxErrors);

    // Gates the "Carry over" radio in the UI (see MergeControl.xaml) - kept in sync with the
    // IsCarryOver setter's own refusal below so the two can never disagree about whether this
    // candidate is eligible.
    public bool CanCarryOver => !HasSyntaxError;

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public CustomMethodDecision Decision
    {
        get => _decision;
        set
        {
            if (SetProperty(ref _decision, value))
            {
                OnPropertyChanged(nameof(IsCarryOver));
                OnPropertyChanged(nameof(IsDefer));
                OnPropertyChanged(nameof(IsDiscard));
            }
        }
    }

    // Each setter only acts on becoming true - WPF's RadioButton grouping already drives mutual
    // exclusivity visually, and with three options relying on "becoming false" to decide which of
    // the other two to fall back to would depend on the order WPF happens to raise the outgoing
    // and incoming IsChecked changes in, which isn't something to lean on.
    public bool IsCarryOver
    {
        get => Decision == CustomMethodDecision.CarryOver;
        set
        {
            // A candidate the extractor couldn't cleanly recover has a SourceText that may not
            // even be this method's own text (see CustomMethodExtractor.IsBalanced) - carrying it
            // over verbatim, from here or from ResolveAsync, would re-inject that corrupted text
            // no matter how the user reaches this setter. The only way out is to fix the source
            // file itself and let the next regeneration re-extract it cleanly from scratch.
            if (value && !HasSyntaxError)
            {
                Decision = CustomMethodDecision.CarryOver;
            }
        }
    }

    public bool IsDefer
    {
        get => Decision == CustomMethodDecision.Defer;
        set
        {
            if (value)
            {
                Decision = CustomMethodDecision.Defer;
            }
        }
    }

    // Permanently drops the method: unlike Defer, it never lands in the global waiting list at
    // all (or is removed from it if it was already there from a prior conflict) - see
    // WorkspaceCodeGeneration.DeferUnresolvedAsync's discardedSet handling. Never a default
    // decision (see the constructor) - only ever reached by an explicit user choice.
    public bool IsDiscard
    {
        get => Decision == CustomMethodDecision.Discard;
        set
        {
            if (value)
            {
                Decision = CustomMethodDecision.Discard;
            }
        }
    }
}

public sealed class PendingPageMergeViewModel
{
    public PendingPageMergeViewModel(PendingCustomMethodMerge model)
    {
        PageId = model.PageId;
        PageObjectName = model.PageObjectName;
        Candidates = model.Candidates.Select(candidate => new CustomMethodCandidateViewModel(candidate)).ToArray();
    }

    public string PageId { get; }

    public string PageObjectName { get; }

    public IReadOnlyList<CustomMethodCandidateViewModel> Candidates { get; }
}

public sealed class WaitingListEntryViewModel
{
    public WaitingListEntryViewModel(PendingCustomMethodEntry model) => Model = model;

    public PendingCustomMethodEntry Model { get; }

    public string Id => Model.Id;

    public string PageId => Model.PageId;

    public string PageObjectName => Model.PageObjectName;

    public string Signature => Model.Signature;

    public bool HasSyntaxError => Model.HasSyntaxError;

    // Gates the "Include now" button in the UI (see MergeControl.xaml) - a deferred entry's
    // SourceText is a snapshot from whenever it was deferred, not a fresh read of the file, so
    // including it can never pick up a fix the user made to the file afterward. Kept in sync with
    // IncludeNowAsync's own refusal below.
    public bool CanInclude => !HasSyntaxError;

    public DateTimeOffset DeferredAt => Model.DeferredAt;
}

/// <summary>
/// Backs the merge tool window: the "Resolve conflicts" tab shows one <see cref="PendingPageMergeViewModel"/>
/// per page from the most recent generate call's <c>GenerationResult.PendingMerges</c>, and the
/// "Waiting list" tab shows the full global inbox from <see cref="IWorkspaceActionStore.GetPendingCustomMethodsAsync"/>.
/// </summary>
public sealed class MergeViewModel : BindableBase
{
    private readonly IWorkspaceActionStore _store;
    private bool _isBusy;
    private string? _statusMessage;
    private string? _errorMessage;

    public MergeViewModel(IWorkspaceActionStore store) =>
        _store = store ?? throw new ArgumentNullException(nameof(store));

    public ObservableCollection<PendingPageMergeViewModel> PendingPages { get; } = new();

    public ObservableCollection<WaitingListEntryViewModel> WaitingList { get; } = new();

    public bool HasPendingPages => PendingPages.Count > 0;

    /// <summary>
    /// Read by <c>CliqCommandRegistrar.QueryStatus</c> to append a count to the "Pending Merges"
    /// menu label - only as fresh as the last <see cref="RefreshWaitingListAsync"/> call (after a
    /// Generate, or after the merge tool window was opened), not live-polled.
    /// </summary>
    public int PendingCount => WaitingList.Count;

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public bool IsNotBusy => !IsBusy;

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public void LoadPendingMerges(IReadOnlyList<PendingCustomMethodMerge> pendingMerges)
    {
        PendingPages.Clear();
        foreach (var merge in pendingMerges)
        {
            PendingPages.Add(new PendingPageMergeViewModel(merge));
        }

        OnPropertyChanged(nameof(HasPendingPages));
    }

    public async Task RefreshWaitingListAsync(CancellationToken cancellationToken = default)
    {
        var entries = await _store.GetPendingCustomMethodsAsync(cancellationToken);
        WaitingList.Clear();
        foreach (var entry in entries.OrderByDescending(entry => entry.DeferredAt))
        {
            WaitingList.Add(new WaitingListEntryViewModel(entry));
        }
    }

    /// <summary>
    /// Same as <see cref="RefreshWaitingListAsync"/>, but routed through <see cref="RunAsync"/> so
    /// a standalone caller (the tool window's "Loaded" handler and its "Refresh" button - see
    /// <c>MergeControl</c>) surfaces a failure via <see cref="ErrorMessage"/> instead of it only
    /// reaching <c>FileAndForget</c>'s logging. <see cref="ResolveAsync"/> keeps calling the plain
    /// <see cref="RefreshWaitingListAsync"/> directly since it already runs inside its own
    /// <see cref="RunAsync"/> call - wrapping it again here would flip <see cref="IsBusy"/> back to
    /// false partway through that still-running operation.
    /// </summary>
    public Task<bool> RefreshWaitingListSafeAsync(CancellationToken cancellationToken = default) =>
        RunAsync(
            "Refreshing waiting list...",
            async token =>
            {
                await RefreshWaitingListAsync(token);
                return true;
            },
            cancellationToken);

    /// <summary>
    /// Builds a carry-over decision map from every currently-loaded pending page and re-generates
    /// exactly those pages - <c>WorkspaceCodeGeneration</c> uses it to inject the chosen methods
    /// and defer the rest, so a follow-up <see cref="RefreshWaitingListAsync"/> picks up whatever
    /// wasn't carried over.
    /// </summary>
    public async Task<bool> ResolveAsync(CancellationToken cancellationToken = default)
    {
        if (PendingPages.Count == 0)
        {
            return false;
        }

        var resolutions = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        var discards = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var page in PendingPages)
        {
            resolutions[page.PageId] = page.Candidates
                .Where(candidate => candidate.Decision == CustomMethodDecision.CarryOver)
                .Select(candidate => candidate.SourceText)
                .ToArray();
            discards[page.PageId] = page.Candidates
                .Where(candidate => candidate.Decision == CustomMethodDecision.Discard)
                .Select(candidate => candidate.SourceText)
                .ToArray();
        }

        return await RunAsync(
            "Resolving custom methods...",
            async token =>
            {
                await _store.GenerateAsync(
                    new GenerationRequest(
                        pageIds: resolutions.Keys.ToArray(),
                        customMethodResolutions: resolutions,
                        customMethodDiscards: discards),
                    token);
                PendingPages.Clear();
                OnPropertyChanged(nameof(HasPendingPages));
                await RefreshWaitingListAsync(token);
                var discardedCount = discards.Values.Sum(sourceTexts => sourceTexts.Count);
                StatusMessage = discardedCount > 0
                    ? $"Resolved. Deferred methods are in the waiting list; {discardedCount} discarded permanently."
                    : "Resolved. Deferred methods are in the waiting list.";
                return true;
            },
            cancellationToken);
    }

    public async Task<bool> DiscardAsync(WaitingListEntryViewModel entry, CancellationToken cancellationToken = default) =>
        await RunAsync(
            "Discarding...",
            async token =>
            {
                await _store.DeletePendingCustomMethodAsync(entry.Id, token);
                WaitingList.Remove(entry);
                StatusMessage = $"Discarded '{entry.Signature}'.";
                return true;
            },
            cancellationToken);

    /// <summary>
    /// Regenerates just this one entry's page with it carried over immediately, without needing a
    /// fresh conflict to reopen the "Resolve conflicts" tab for it.
    /// </summary>
    public async Task<bool> IncludeNowAsync(WaitingListEntryViewModel entry, CancellationToken cancellationToken = default)
    {
        if (entry.HasSyntaxError)
        {
            // entry.Model.SourceText is a snapshot from whenever this was deferred - even if the
            // user has since fixed the method in the actual file, including it here would still
            // re-inject that old, corrupted snapshot. Refused here too (not just via CanInclude's
            // IsEnabled binding) so this can't be forced through the click handler regardless of
            // what the UI currently disables.
            ErrorMessage = $"'{entry.Signature}' still has a syntax error and can't be included as-is. " +
                "Fix it in the generated file, regenerate, and it will be re-extracted cleanly.";
            return false;
        }

        return await RunAsync(
            "Including...",
            async token =>
            {
                var resolutions = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
                {
                    [entry.PageId] = [entry.Model.SourceText],
                };
                await _store.GenerateAsync(
                    new GenerationRequest(pageIds: [entry.PageId], customMethodResolutions: resolutions),
                    token);
                WaitingList.Remove(entry);
                StatusMessage = $"Included '{entry.Signature}' in {entry.PageObjectName}.";
                return true;
            },
            cancellationToken);
    }

    private async Task<bool> RunAsync(
        string status,
        Func<CancellationToken, Task<bool>> operation,
        CancellationToken cancellationToken)
    {
        IsBusy = true;
        OnPropertyChanged(nameof(IsNotBusy));
        ErrorMessage = null;
        StatusMessage = status;
        try
        {
            return await operation(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Operation cancelled.";
            return false;
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
            return false;
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(IsNotBusy));
        }
    }
}
