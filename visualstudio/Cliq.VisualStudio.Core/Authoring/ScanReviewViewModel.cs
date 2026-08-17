using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cliq.Domain;
using Cliq.VisualStudio.Core.Actions;
using Cliq.VisualStudio.Core.Api;

namespace Cliq.VisualStudio.Core.Authoring
{
    public sealed class ScanReviewItemViewModel : BindableBase
    {
        private ScanReviewDecision _decision;

        public ScanReviewItemViewModel(ScanReviewItem item)
        {
            Model = item ?? throw new ArgumentNullException(nameof(item));
            _decision = item.Decision;
        }

        public ScanReviewItem Model { get; }

        public string Id => Model.Id;

        public ScanReviewChangeKind Kind => Model.Kind;

        public PageElement Element => Model.Element;

        public string Name => Element.Name;

        public string Tag => Element.Tag;

        public StepAction Action => Element.Action;

        public LocatorStrategy Strategy => Element.Locator.Strategy;

        public string LocatorValue => Element.Locator.Value;

        public string LocatorSummary => $"{Element.Locator.Strategy}: {Element.Locator.Value}";

        public string KindDescription => Kind == ScanReviewChangeKind.New ? "New element" : "Missing element";

        public string AcceptLabel => Kind == ScanReviewChangeKind.New
            ? "Accept (Add)"
            : "Accept (Remove)";

        public string DenyLabel => Kind == ScanReviewChangeKind.New
            ? "Deny (Omit)"
            : "Deny (Keep)";

        public string AcceptToolTip => Kind == ScanReviewChangeKind.New
            ? "Adds this newly discovered element to the page definition."
            : "Removes this element from the page definition.";

        public string DenyToolTip => Kind == ScanReviewChangeKind.New
            ? "Omits this new element from the page definition."
            : "Retains this element in the page definition as defined in the baseline.";

        public ScanReviewDecision Decision
        {
            get => _decision;
            set
            {
                if (SetProperty(ref _decision, value))
                {
                    OnPropertyChanged(nameof(IsAccept));
                    OnPropertyChanged(nameof(IsDeny));
                    OnPropertyChanged(nameof(IsWait));
                    OnPropertyChanged(nameof(DecisionText));
                }
            }
        }

        public bool IsAccept
        {
            get => Decision == ScanReviewDecision.Accept;
            set
            {
                if (value)
                {
                    Decision = ScanReviewDecision.Accept;
                }
            }
        }

        public bool IsDeny
        {
            get => Decision == ScanReviewDecision.Deny;
            set
            {
                if (value)
                {
                    Decision = ScanReviewDecision.Deny;
                }
            }
        }

        public bool IsWait
        {
            get => Decision == ScanReviewDecision.Wait;
            set
            {
                if (value)
                {
                    Decision = ScanReviewDecision.Wait;
                }
            }
        }

        public string DecisionText => Decision switch
        {
            ScanReviewDecision.Accept => AcceptLabel,
            ScanReviewDecision.Deny => DenyLabel,
            _ => "Pending decision"
        };
    }

    public sealed class ScanReviewViewModel : BindableBase
    {
        private readonly IWorkspaceActionStore _store;
        private ScanReview _model;
        private bool _isBusy;
        private string? _statusMessage;
        private string? _errorMessage;
        private string? _conflictMessage;
        private IReadOnlyList<ScanReviewReference> _conflictReferences = Array.Empty<ScanReviewReference>();

        public ScanReviewViewModel(IWorkspaceActionStore store, ScanReview review)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _model = review ?? throw new ArgumentNullException(nameof(review));

            Items = new ObservableCollection<ScanReviewItemViewModel>(
                review.Items.Select(item =>
                {
                    var itemVm = new ScanReviewItemViewModel(item);
                    itemVm.PropertyChanged += OnItemPropertyChanged;
                    return itemVm;
                }));
        }

        public event EventHandler<PageDefinition>? Applied;
        public event EventHandler? Discarded;

        public ScanReview Model => _model;

        public string ReviewId => _model.Id;

        public string PageId => _model.PageId;

        public string PageName => _model.BaselinePage?.Name ?? _model.ProposedPage?.Name ?? "Page";

        public string PageUrl => _model.BaselinePage?.Url ?? _model.ProposedPage?.Url ?? string.Empty;

        public ScanReviewStatus Status => _model.Status;

        public ObservableCollection<ScanReviewItemViewModel> Items { get; }

        public IReadOnlyList<ScanReviewItemViewModel> NewItems =>
            Items.Where(item => item.Kind == ScanReviewChangeKind.New).ToArray();

        public IReadOnlyList<ScanReviewItemViewModel> MissingItems =>
            Items.Where(item => item.Kind == ScanReviewChangeKind.Missing).ToArray();

        public int NewCount => NewItems.Count;

        public int MissingCount => MissingItems.Count;

        public int UnresolvedCount => Items.Count(item => item.Decision == ScanReviewDecision.Wait);

        public bool HasItems => Items.Count > 0;

        public bool CanApply => IsNotBusy && UnresolvedCount == 0 && Status == ScanReviewStatus.Pending;

        public bool CanDiscard => IsNotBusy && Status == ScanReviewStatus.Pending;

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(IsNotBusy));
                    OnPropertyChanged(nameof(CanApply));
                    OnPropertyChanged(nameof(CanDiscard));
                }
            }
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

        public string? ConflictMessage
        {
            get => _conflictMessage;
            private set
            {
                if (SetProperty(ref _conflictMessage, value))
                {
                    OnPropertyChanged(nameof(HasConflict));
                }
            }
        }

        public bool HasConflict => !string.IsNullOrEmpty(ConflictMessage);

        public IReadOnlyList<ScanReviewReference> ConflictReferences
        {
            get => _conflictReferences;
            private set => SetProperty(ref _conflictReferences, value ?? Array.Empty<ScanReviewReference>());
        }

        public void AcceptAll()
        {
            foreach (var item in Items)
            {
                item.Decision = ScanReviewDecision.Accept;
            }
            ClearConflicts();
            RaiseDecisionState();
        }

        public void DenyAll()
        {
            foreach (var item in Items)
            {
                item.Decision = ScanReviewDecision.Deny;
            }
            ClearConflicts();
            RaiseDecisionState();
        }

        public void ResetToWait()
        {
            foreach (var item in Items)
            {
                item.Decision = ScanReviewDecision.Wait;
            }
            ClearConflicts();
            RaiseDecisionState();
        }

        public async Task<bool> ApplyAsync(CancellationToken cancellationToken = default)
        {
            if (UnresolvedCount > 0)
            {
                ErrorMessage = "Every scan review item must be accepted or denied before applying.";
                return false;
            }

            ClearConflicts();
            ErrorMessage = null;

            return await RunAsync(
                "Applying scan review...",
                async token =>
                {
                    var updates = Items
                        .Select(item => new ScanReviewDecisionRequest(item.Id, item.Decision))
                        .ToArray();

                    await _store.UpdateScanReviewDecisionsAsync(ReviewId, updates, token).ConfigureAwait(false);
                    var applyResult = await _store.ApplyScanReviewAsync(ReviewId, token).ConfigureAwait(false);

                    _model = applyResult.Review;
                    StatusMessage = $"Applied scan review for {PageName}.";
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(CanApply));
                    OnPropertyChanged(nameof(CanDiscard));

                    Applied?.Invoke(this, applyResult.Page);
                    return true;
                },
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> DiscardAsync(CancellationToken cancellationToken = default)
        {
            ClearConflicts();
            ErrorMessage = null;

            return await RunAsync(
                "Discarding scan review...",
                async token =>
                {
                    var discarded = await _store.DiscardScanReviewAsync(ReviewId, token).ConfigureAwait(false);
                    _model = discarded;
                    StatusMessage = $"Discarded scan review for {PageName}.";
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(CanApply));
                    OnPropertyChanged(nameof(CanDiscard));

                    Discarded?.Invoke(this, EventArgs.Empty);
                    return true;
                },
                cancellationToken).ConfigureAwait(false);
        }

        private void OnItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ScanReviewItemViewModel.Decision))
            {
                ClearConflicts();
                RaiseDecisionState();
            }
        }

        private void RaiseDecisionState()
        {
            OnPropertyChanged(nameof(UnresolvedCount));
            OnPropertyChanged(nameof(CanApply));
        }

        private void ClearConflicts()
        {
            ConflictMessage = null;
            ConflictReferences = Array.Empty<ScanReviewReference>();
        }

        private async Task<bool> RunAsync(
            string status,
            Func<CancellationToken, Task<bool>> operation,
            CancellationToken cancellationToken)
        {
            // Without this guard, a second click while IsBusy is already true (the UI doesn't
            // reliably disable every trigger fast enough) would run two operations concurrently
            // against the same scan review state.
            if (IsBusy)
            {
                ErrorMessage = "Another Cliq action is already running.";
                return false;
            }

            return await AsyncOperation.RunAsync<bool>(
                busy => IsBusy = busy,
                s => StatusMessage = s,
                e => ErrorMessage = e,
                status,
                async () =>
                {
                    // Caught here, inside the operation itself, rather than left to
                    // AsyncOperation's own generic catch (Exception) - a conflict needs its
                    // References/ConflictMessage broken out, not just reported as ErrorMessage.
                    try
                    {
                        return await operation(cancellationToken).ConfigureAwait(false);
                    }
                    catch (ScanReviewConflictApiException conflict)
                    {
                        if (conflict.References.Count > 0)
                        {
                            ConflictReferences = conflict.References;
                            var referenceDetails = string.Join(
                                Environment.NewLine,
                                conflict.References.Select(r => $"• Test '{r.TestId}', Step '{r.StepId}'"));
                            ConflictMessage =
                                "Cannot remove element(s): One or more accepted element removals are referenced by saved tests." +
                                Environment.NewLine + Environment.NewLine +
                                "Referenced by:" + Environment.NewLine +
                                referenceDetails;
                        }
                        else if (string.Equals(conflict.Code, "scan_review_stale", StringComparison.Ordinal))
                        {
                            ErrorMessage = "The page was modified since this scan review was created. Please re-scan the page.";
                        }
                        else
                        {
                            ErrorMessage = conflict.Message;
                        }

                        return false;
                    }
                });
        }
    }
}
