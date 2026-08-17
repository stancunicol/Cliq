using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cliq.Domain;
using Cliq.VisualStudio.Core.Api;
using Cliq.VisualStudio.Core.Authoring;

namespace Cliq.VisualStudio.Core.Actions
{
    public interface IWorkspaceActionStore
    {
        Task<ScanPageResponse> ScanAsync(
            ScanPageRequest request,
            CancellationToken cancellationToken);

        Task<StagedPageScanResult> ScanPageReviewAsync(
            ScanPageRequest request,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<ScanReview>> ListScanReviewsAsync(
            string? pageId,
            ScanReviewStatus? status,
            CancellationToken cancellationToken);

        Task<ScanReview> GetScanReviewAsync(
            string reviewId,
            CancellationToken cancellationToken);

        Task<ScanReview> UpdateScanReviewDecisionsAsync(
            string reviewId,
            IReadOnlyList<ScanReviewDecisionRequest> decisions,
            CancellationToken cancellationToken);

        Task<ScanReviewApplyResult> ApplyScanReviewAsync(
            string reviewId,
            CancellationToken cancellationToken);

        Task<ScanReview> DiscardScanReviewAsync(
            string reviewId,
            CancellationToken cancellationToken);

        Task<GenerationResult> GenerateAsync(
            GenerationRequest request,
            CancellationToken cancellationToken);

        Task<IReadOnlyList<PendingCustomMethodEntry>> GetPendingCustomMethodsAsync(
            CancellationToken cancellationToken);

        Task DeletePendingCustomMethodAsync(string id, CancellationToken cancellationToken);

        Task LocateElementAsync(
            string pageId,
            string elementId,
            StartRunRequest request,
            CancellationToken cancellationToken);

        Task LocateElementFromSourceAsync(
            LocateFromSourceRequest request,
            CancellationToken cancellationToken);
    }

    public sealed class ApiWorkspaceActionStore : IWorkspaceActionStore
    {
        private readonly ICliqApiSession _session;

        public ApiWorkspaceActionStore(ICliqApiSession session) =>
            _session = session ?? throw new ArgumentNullException(nameof(session));

        public async Task<ScanPageResponse> ScanAsync(
            ScanPageRequest request,
            CancellationToken cancellationToken) =>
            await (await _session.GetClientAsync(cancellationToken).ConfigureAwait(false))
                .ScanPageAsync(request, cancellationToken).ConfigureAwait(false);

        public async Task<StagedPageScanResult> ScanPageReviewAsync(
            ScanPageRequest request,
            CancellationToken cancellationToken) =>
            await (await _session.GetClientAsync(cancellationToken).ConfigureAwait(false))
                .ScanPageReviewAsync(request, cancellationToken).ConfigureAwait(false);

        public async Task<IReadOnlyList<ScanReview>> ListScanReviewsAsync(
            string? pageId,
            ScanReviewStatus? status,
            CancellationToken cancellationToken) =>
            await (await _session.GetClientAsync(cancellationToken).ConfigureAwait(false))
                .ListScanReviewsAsync(pageId, status, cancellationToken).ConfigureAwait(false);

        public async Task<ScanReview> GetScanReviewAsync(
            string reviewId,
            CancellationToken cancellationToken) =>
            await (await _session.GetClientAsync(cancellationToken).ConfigureAwait(false))
                .GetScanReviewAsync(reviewId, cancellationToken).ConfigureAwait(false);

        public async Task<ScanReview> UpdateScanReviewDecisionsAsync(
            string reviewId,
            IReadOnlyList<ScanReviewDecisionRequest> decisions,
            CancellationToken cancellationToken) =>
            await (await _session.GetClientAsync(cancellationToken).ConfigureAwait(false))
                .UpdateScanReviewDecisionsAsync(
                    reviewId,
                    new UpdateScanReviewDecisionsRequest(decisions),
                    cancellationToken).ConfigureAwait(false);

        public async Task<ScanReviewApplyResult> ApplyScanReviewAsync(
            string reviewId,
            CancellationToken cancellationToken) =>
            await (await _session.GetClientAsync(cancellationToken).ConfigureAwait(false))
                .ApplyScanReviewAsync(reviewId, cancellationToken).ConfigureAwait(false);

        public async Task<ScanReview> DiscardScanReviewAsync(
            string reviewId,
            CancellationToken cancellationToken) =>
            await (await _session.GetClientAsync(cancellationToken).ConfigureAwait(false))
                .DiscardScanReviewAsync(reviewId, cancellationToken).ConfigureAwait(false);

        public async Task<GenerationResult> GenerateAsync(
            GenerationRequest request,
            CancellationToken cancellationToken) =>
            await (await _session.GetClientAsync(cancellationToken).ConfigureAwait(false))
                .GenerateAsync(request, cancellationToken).ConfigureAwait(false);

        public async Task<IReadOnlyList<PendingCustomMethodEntry>> GetPendingCustomMethodsAsync(
            CancellationToken cancellationToken) =>
            await (await _session.GetClientAsync(cancellationToken).ConfigureAwait(false))
                .GetPendingCustomMethodsAsync(cancellationToken).ConfigureAwait(false);

        public async Task DeletePendingCustomMethodAsync(string id, CancellationToken cancellationToken) =>
            await (await _session.GetClientAsync(cancellationToken).ConfigureAwait(false))
                .DeletePendingCustomMethodAsync(id, cancellationToken).ConfigureAwait(false);

        public async Task LocateElementAsync(
            string pageId,
            string elementId,
            StartRunRequest request,
            CancellationToken cancellationToken) =>
            await (await _session.GetClientAsync(cancellationToken).ConfigureAwait(false))
                .LocateElementAsync(pageId, elementId, request, cancellationToken).ConfigureAwait(false);

        public async Task LocateElementFromSourceAsync(
            LocateFromSourceRequest request,
            CancellationToken cancellationToken) =>
            await (await _session.GetClientAsync(cancellationToken).ConfigureAwait(false))
                .LocateElementFromSourceAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public sealed class WorkspaceActionsViewModel : BindableBase, IDisposable
    {
        private readonly IWorkspaceActionStore _store;
        private CancellationTokenSource? _operation;
        private bool _isBusy;
        private string? _statusMessage;
        private string? _errorMessage;
        private IReadOnlyList<GeneratedArtifact> _artifacts = Array.Empty<GeneratedArtifact>();
        private bool _disposed;

        public WorkspaceActionsViewModel(IWorkspaceActionStore store) =>
            _store = store ?? throw new ArgumentNullException(nameof(store));

        public bool IsBusy
        {
            get => _isBusy;
            private set => SetProperty(ref _isBusy, value);
        }

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

        public IReadOnlyList<GeneratedArtifact> Artifacts
        {
            get => _artifacts;
            private set => SetProperty(ref _artifacts, value);
        }

        public async Task<ScanPageResponse?> ScanAsync(
            string url,
            string name,
            BrowserKind? browser,
            bool headless,
            CancellationToken cancellationToken = default)
        {
            if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                ErrorMessage = "Enter an absolute HTTP or HTTPS URL.";
                return null;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                ErrorMessage = "Enter a page name.";
                return null;
            }

            return await RunAsync(
                "Scanning page...",
                async token =>
                {
                    var response = await _store.ScanAsync(
                        new ScanPageRequest(uri.AbsoluteUri, name.Trim(), browser, headless),
                        token).ConfigureAwait(false);
                    var report = response.Reconciliation;
                    StatusMessage =
                        $"Scanned {response.Page.Name}: {report.MatchedCount} matched, " +
                        $"{report.NewCount} new, {report.MissingCount} missing.";
                    return response;
                },
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<StagedPageScanResult?> ScanReviewAsync(
            string url,
            string name,
            BrowserKind? browser,
            bool headless,
            CancellationToken cancellationToken = default)
        {
            if (!Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                ErrorMessage = "Enter an absolute HTTP or HTTPS URL.";
                return null;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                ErrorMessage = "Enter a page name.";
                return null;
            }

            return await RunAsync(
                "Scanning page...",
                async token =>
                {
                    var response = await _store.ScanPageReviewAsync(
                        new ScanPageRequest(uri.AbsoluteUri, name.Trim(), browser, headless),
                        token).ConfigureAwait(false);
                    var report = response.Reconciliation;
                    StatusMessage =
                        $"Scanned {response.ActivePage.Name}: {report.MatchedCount} matched, " +
                        $"{report.NewCount} new, {report.MissingCount} missing.";
                    return response;
                },
                cancellationToken).ConfigureAwait(false);
        }


        public Task<GenerationResult?> GenerateAllAsync(
            CancellationToken cancellationToken = default) =>
            GenerateAsync(new GenerationRequest(), cancellationToken);

        public Task<GenerationResult?> GenerateSelectedAsync(
            CliqDocumentKind kind,
            string entityId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(entityId) || kind == CliqDocumentKind.Other)
            {
                ErrorMessage = "Select a Cliq test or page to generate.";
                return Task.FromResult<GenerationResult?>(null);
            }

            var request = kind == CliqDocumentKind.Test
                ? new GenerationRequest(testIds: new[] { entityId })
                : new GenerationRequest(pageIds: new[] { entityId });
            return GenerateAsync(request, cancellationToken);
        }

        /// <summary>
        /// Opens a visible browser on the element's page and highlights it - unlike every other
        /// action here, the backend deliberately leaves that browser window open afterward, so
        /// there's nothing further to read back beyond whether the request itself succeeded.
        /// </summary>
        public Task<bool> LocateElementAsync(
            string pageId,
            string elementId,
            BrowserKind? browser,
            CancellationToken cancellationToken = default) =>
            RunAsync(
                "Locating element...",
                async token =>
                {
                    await _store.LocateElementAsync(
                        pageId, elementId, new StartRunRequest(browser), token).ConfigureAwait(false);
                    StatusMessage = "Opened a browser window with the element highlighted.";
                },
                cancellationToken);

        /// <summary>
        /// Same as <see cref="LocateElementAsync"/>, but resolves the page/element server-side
        /// from a generated Page Object's class name and a <c>// Locator (...): ...</c> comment
        /// selected in its source file - the entry point for "Locate in Browser" triggered from
        /// the .cs editor instead of the Workbench grid.
        /// </summary>
        public Task<bool> LocateElementFromSourceAsync(
            string className,
            string locatorComment,
            BrowserKind? browser,
            CancellationToken cancellationToken = default) =>
            RunAsync(
                "Locating element...",
                async token =>
                {
                    await _store.LocateElementFromSourceAsync(
                        new LocateFromSourceRequest(className, locatorComment, browser), token).ConfigureAwait(false);
                    StatusMessage = "Opened a browser window with the element highlighted.";
                },
                cancellationToken);

        public void Cancel() => _operation?.Cancel();

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _operation?.Cancel();
            _operation?.Dispose();
            _operation = null;
        }

        private async Task<GenerationResult?> GenerateAsync(
            GenerationRequest request,
            CancellationToken cancellationToken)
        {
            return await RunAsync(
                "Generating artifacts...",
                async token =>
                {
                    var result = await _store.GenerateAsync(request, token).ConfigureAwait(false);
                    Artifacts = result.Artifacts.ToArray();
                    var created = Artifacts.Count(artifact => artifact.Created);
                    StatusMessage =
                        $"Generated {Artifacts.Count} artifact{(Artifacts.Count == 1 ? string.Empty : "s")} " +
                        $"({created} created, {Artifacts.Count - created} refreshed).";
                    return result;
                },
                cancellationToken).ConfigureAwait(false);
        }

        private async Task<T?> RunAsync<T>(
            string status,
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken)
            where T : class
        {
            ThrowIfDisposed();
            if (IsBusy)
            {
                throw new InvalidOperationException("Another Cliq action is already running.");
            }

            _operation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            try
            {
                return await AsyncOperation.RunAsync<T>(
                    busy => IsBusy = busy,
                    s => StatusMessage = s,
                    e => ErrorMessage = e,
                    status,
                    () => operation(_operation.Token)).ConfigureAwait(false);
            }
            finally
            {
                _operation.Dispose();
                _operation = null;
            }
        }

        private async Task<bool> RunAsync(
            string status,
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (IsBusy)
            {
                throw new InvalidOperationException("Another Cliq action is already running.");
            }

            _operation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            try
            {
                return await AsyncOperation.RunAsync(
                    busy => IsBusy = busy,
                    s => StatusMessage = s,
                    e => ErrorMessage = e,
                    status,
                    () => operation(_operation.Token)).ConfigureAwait(false);
            }
            finally
            {
                _operation.Dispose();
                _operation = null;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WorkspaceActionsViewModel));
            }
        }
    }
}
