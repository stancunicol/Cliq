using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cliq.Domain;
using Cliq.VisualStudio.Core.Actions;
using Cliq.VisualStudio.Core.Api;
using Cliq.VisualStudio.Core.Authoring;

namespace Cliq.VisualStudio.Tests;

/// <summary>
/// Base fake for <see cref="IWorkspaceActionStore"/>: every method throws
/// <see cref="NotSupportedException"/> by default, so each test file's own subclass only has to
/// override the handful of methods it actually exercises. Previously three files
/// (MergeViewModelTests, ScanReviewViewModelTests, WorkspaceActionsViewModelTests) each
/// hand-rolled this same 12-method interface independently and had already drifted on which
/// methods were implemented.
/// </summary>
internal class FakeWorkspaceActionStore : IWorkspaceActionStore
{
    public virtual Task<ScanPageResponse> ScanAsync(
        ScanPageRequest request, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public virtual Task<StagedPageScanResult> ScanPageReviewAsync(
        ScanPageRequest request, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public virtual Task<IReadOnlyList<ScanReview>> ListScanReviewsAsync(
        string? pageId, ScanReviewStatus? status, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public virtual Task<ScanReview> GetScanReviewAsync(
        string reviewId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public virtual Task<ScanReview> UpdateScanReviewDecisionsAsync(
        string reviewId,
        IReadOnlyList<ScanReviewDecisionRequest> decisions,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public virtual Task<ScanReviewApplyResult> ApplyScanReviewAsync(
        string reviewId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public virtual Task<ScanReview> DiscardScanReviewAsync(
        string reviewId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public virtual Task<GenerationResult> GenerateAsync(
        GenerationRequest request, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public virtual Task<IReadOnlyList<PendingCustomMethodEntry>> GetPendingCustomMethodsAsync(
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public virtual Task DeletePendingCustomMethodAsync(string id, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public virtual Task LocateElementAsync(
        string pageId,
        string elementId,
        StartRunRequest request,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public virtual Task LocateElementFromSourceAsync(
        LocateFromSourceRequest request, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}

/// <summary>
/// Base fake for <see cref="IWorkbenchStore"/>: every method throws
/// <see cref="NotSupportedException"/> by default, mirroring <see cref="FakeWorkspaceActionStore"/>
/// for the same reason - WorkbenchAuthoringTests and WorkspaceConfigurationViewModelTests each
/// hand-rolled this 4-method interface independently.
/// </summary>
internal class FakeWorkbenchStore : IWorkbenchStore
{
    public virtual Task<WorkspaceSnapshot> LoadAsync(CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public virtual Task<TestCase> SaveTestAsync(TestCase test, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public virtual Task DeleteTestAsync(string testId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public virtual Task<PageDefinition> SavePageAsync(PageDefinition page, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public virtual Task DeletePageAsync(string pageId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}
