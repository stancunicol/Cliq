using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Cliq.Domain;
using Cliq.VisualStudio.Core.Actions;
using Cliq.VisualStudio.Core.Api;
using Cliq.VisualStudio.Core.Authoring;
using NUnit.Framework;

namespace Cliq.VisualStudio.Tests
{
    [TestFixture]
    public sealed class ScanReviewViewModelTests
    {
        [Test]
        public void Initialization_CategorizesNewAndMissingItems_AndSetsDefaults()
        {
            var review = CreateSampleReview();
            var store = new RecordingActionStore();
            var viewModel = new ScanReviewViewModel(store, review);

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.ReviewId, Is.EqualTo("rev-1"));
                Assert.That(viewModel.PageName, Is.EqualTo("Orders"));
                Assert.That(viewModel.PageUrl, Is.EqualTo("https://example.com/orders"));
                Assert.That(viewModel.Items, Has.Count.EqualTo(2));
                Assert.That(viewModel.NewCount, Is.EqualTo(1));
                Assert.That(viewModel.MissingCount, Is.EqualTo(1));
                Assert.That(viewModel.UnresolvedCount, Is.EqualTo(2));
                Assert.That(viewModel.CanApply, Is.False);
                Assert.That(viewModel.CanDiscard, Is.True);
            });
        }

        [Test]
        public void ItemViewModel_SettingIsAcceptOrIsDeny_UpdatesDecisionAndOtherFlags()
        {
            var item = new ScanReviewItem(
                "item-1",
                ScanReviewChangeKind.New,
                new PageElement("e1", "Submit", "button", StepAction.Click, new Locator(LocatorStrategy.Css, "#submit")),
                ScanReviewDecision.Wait);
            var itemVm = new ScanReviewItemViewModel(item);

            Assert.Multiple(() =>
            {
                Assert.That(itemVm.IsWait, Is.True);
                Assert.That(itemVm.IsAccept, Is.False);
                Assert.That(itemVm.IsDeny, Is.False);
            });

            itemVm.IsAccept = true;

            Assert.Multiple(() =>
            {
                Assert.That(itemVm.Decision, Is.EqualTo(ScanReviewDecision.Accept));
                Assert.That(itemVm.IsAccept, Is.True);
                Assert.That(itemVm.IsDeny, Is.False);
                Assert.That(itemVm.IsWait, Is.False);
            });

            itemVm.IsDeny = true;

            Assert.Multiple(() =>
            {
                Assert.That(itemVm.Decision, Is.EqualTo(ScanReviewDecision.Deny));
                Assert.That(itemVm.IsAccept, Is.False);
                Assert.That(itemVm.IsDeny, Is.True);
                Assert.That(itemVm.IsWait, Is.False);
            });
        }

        [Test]
        public void AcceptAll_And_DenyAll_BatchUpdateDecisions()
        {
            var review = CreateSampleReview();
            var store = new RecordingActionStore();
            var viewModel = new ScanReviewViewModel(store, review);

            viewModel.AcceptAll();

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.Items.All(i => i.Decision == ScanReviewDecision.Accept), Is.True);
                Assert.That(viewModel.UnresolvedCount, Is.EqualTo(0));
                Assert.That(viewModel.CanApply, Is.True);
            });

            viewModel.DenyAll();

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.Items.All(i => i.Decision == ScanReviewDecision.Deny), Is.True);
                Assert.That(viewModel.UnresolvedCount, Is.EqualTo(0));
                Assert.That(viewModel.CanApply, Is.True);
            });

            viewModel.ResetToWait();

            Assert.Multiple(() =>
            {
                Assert.That(viewModel.Items.All(i => i.Decision == ScanReviewDecision.Wait), Is.True);
                Assert.That(viewModel.UnresolvedCount, Is.EqualTo(2));
                Assert.That(viewModel.CanApply, Is.False);
            });
        }

        [Test]
        public async Task ApplyAsync_WhenAllDecided_SendsUpdatesAndApplies()
        {
            var review = CreateSampleReview();
            var store = new RecordingActionStore();
            var viewModel = new ScanReviewViewModel(store, review);

            viewModel.AcceptAll();
            var applied = await viewModel.ApplyAsync();

            Assert.Multiple(() =>
            {
                Assert.That(applied, Is.True);
                Assert.That(store.LastUpdatedReviewId, Is.EqualTo("rev-1"));
                Assert.That(store.LastDecisions, Has.Count.EqualTo(2));
                Assert.That(store.LastAppliedReviewId, Is.EqualTo("rev-1"));
                Assert.That(viewModel.Status, Is.EqualTo(ScanReviewStatus.Applied));
                Assert.That(viewModel.StatusMessage, Does.Contain("Applied"));
            });
        }

        [Test]
        public async Task ApplyAsync_WhenConflictWithReferencedRemoval_CapturesConflictDetails()
        {
            var review = CreateSampleReview();
            var store = new RecordingActionStore
            {
                ApplyException = new ScanReviewConflictApiException(
                    HttpStatusCode.Conflict,
                    new CliqProblemDetails
                    {
                        Code = "scan_review_removal_referenced",
                        Detail = "One or more accepted element removals are referenced by saved tests."
                    },
                    new[] { new ScanReviewReference("checkout-test", "step-3") })
            };
            var viewModel = new ScanReviewViewModel(store, review);

            viewModel.AcceptAll();
            var applied = await viewModel.ApplyAsync();

            Assert.Multiple(() =>
            {
                Assert.That(applied, Is.False);
                Assert.That(viewModel.HasConflict, Is.True);
                Assert.That(viewModel.ConflictReferences, Has.Count.EqualTo(1));
                Assert.That(viewModel.ConflictReferences[0].TestId, Is.EqualTo("checkout-test"));
                Assert.That(viewModel.ConflictReferences[0].StepId, Is.EqualTo("step-3"));
                Assert.That(viewModel.ConflictMessage, Does.Contain("checkout-test"));
                Assert.That(viewModel.ConflictMessage, Does.Contain("step-3"));
            });
        }

        [Test]
        public async Task ApplyAsync_WhenStale_CapturesStaleErrorMessage()
        {
            var review = CreateSampleReview();
            var store = new RecordingActionStore
            {
                ApplyException = new ScanReviewConflictApiException(
                    HttpStatusCode.Conflict,
                    new CliqProblemDetails
                    {
                        Code = "scan_review_stale",
                        Detail = "Page changed after scan review was created."
                    })
            };
            var viewModel = new ScanReviewViewModel(store, review);

            viewModel.AcceptAll();
            var applied = await viewModel.ApplyAsync();

            Assert.Multiple(() =>
            {
                Assert.That(applied, Is.False);
                Assert.That(viewModel.ErrorMessage, Does.Contain("modified"));
                Assert.That(viewModel.ErrorMessage, Does.Contain("re-scan"));
            });
        }

        [Test]
        public async Task DiscardAsync_CallsStoreAndMarksDiscarded()
        {
            var review = CreateSampleReview();
            var store = new RecordingActionStore();
            var viewModel = new ScanReviewViewModel(store, review);

            var discarded = await viewModel.DiscardAsync();

            Assert.Multiple(() =>
            {
                Assert.That(discarded, Is.True);
                Assert.That(store.LastDiscardedReviewId, Is.EqualTo("rev-1"));
                Assert.That(viewModel.Status, Is.EqualTo(ScanReviewStatus.Discarded));
                Assert.That(viewModel.StatusMessage, Does.Contain("Discarded"));
            });
        }

        private static ScanReview CreateSampleReview()
        {
            var now = DateTimeOffset.UtcNow;
            var baseline = new PageDefinition(
                PageDefinition.CurrentSchemaVersion,
                "orders",
                "Orders",
                "https://example.com/orders",
                new[]
                {
                    new PageElement("old-btn", "Old button", "button", StepAction.Click, new Locator(LocatorStrategy.Css, "#old")),
                },
                now);

            var proposed = new PageDefinition(
                PageDefinition.CurrentSchemaVersion,
                "orders",
                "Orders",
                "https://example.com/orders",
                new[]
                {
                    new PageElement("new-btn", "New button", "button", StepAction.Click, new Locator(LocatorStrategy.Css, "#new")),
                },
                now);

            var items = new[]
            {
                new ScanReviewItem(
                    "item-new",
                    ScanReviewChangeKind.New,
                    proposed.Elements[0],
                    ScanReviewDecision.Wait),
                new ScanReviewItem(
                    "item-missing",
                    ScanReviewChangeKind.Missing,
                    baseline.Elements[0],
                    ScanReviewDecision.Wait),
            };

            return new ScanReview(
                ScanReview.CurrentSchemaVersion,
                "rev-1",
                "orders",
                ScanReviewStatus.Pending,
                now,
                now,
                null,
                baseline,
                proposed,
                items);
        }

        private sealed class RecordingActionStore : FakeWorkspaceActionStore
        {
            public string? LastUpdatedReviewId { get; private set; }
            public IReadOnlyList<ScanReviewDecisionRequest>? LastDecisions { get; private set; }
            public string? LastAppliedReviewId { get; private set; }
            public string? LastDiscardedReviewId { get; private set; }
            public Exception? ApplyException { get; set; }

            public override Task<ScanReview> UpdateScanReviewDecisionsAsync(
                string reviewId,
                IReadOnlyList<ScanReviewDecisionRequest> decisions,
                CancellationToken cancellationToken)
            {
                LastUpdatedReviewId = reviewId;
                LastDecisions = decisions;
                return Task.FromResult(CreateSampleReview());
            }

            public override Task<ScanReviewApplyResult> ApplyScanReviewAsync(string reviewId, CancellationToken cancellationToken)
            {
                if (ApplyException != null)
                {
                    throw ApplyException;
                }

                LastAppliedReviewId = reviewId;
                var review = CreateSampleReview() with { Status = ScanReviewStatus.Applied };
                return Task.FromResult(new ScanReviewApplyResult(review, review.BaselinePage));
            }

            public override Task<ScanReview> DiscardScanReviewAsync(string reviewId, CancellationToken cancellationToken)
            {
                LastDiscardedReviewId = reviewId;
                var review = CreateSampleReview() with { Status = ScanReviewStatus.Discarded };
                return Task.FromResult(review);
            }
        }
    }
}
