using Cliq.Domain;
using Cliq.VisualStudio.Core;
using Cliq.VisualStudio.Core.Actions;
using Cliq.VisualStudio.Core.Api;
using NUnit.Framework;

namespace Cliq.VisualStudio.Tests;

[TestFixture]
public sealed class WorkspaceActionsViewModelTests
{
    [Test]
    public async Task ScanAsync_RejectsNonHttpUrlsWithoutCallingStore()
    {
        var store = new RecordingStore();
        using var viewModel = new WorkspaceActionsViewModel(store);

        var result = await viewModel.ScanAsync("file:///tmp/page.html", "Page", null, true);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Null);
            Assert.That(store.ScanRequest, Is.Null);
            Assert.That(viewModel.ErrorMessage, Does.Contain("HTTP"));
        });
    }

    [Test]
    public async Task ScanAsync_ReportsReconciliationAndHeadlessChoice()
    {
        var store = new RecordingStore();
        using var viewModel = new WorkspaceActionsViewModel(store);

        var result = await viewModel.ScanAsync(
            " https://example.test/login ",
            " Login ",
            BrowserKind.Edge,
            headless: false);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(store.ScanRequest!.Url, Is.EqualTo("https://example.test/login"));
            Assert.That(store.ScanRequest.Name, Is.EqualTo("Login"));
            Assert.That(store.ScanRequest.Browser, Is.EqualTo(BrowserKind.Edge));
            Assert.That(store.ScanRequest.Headless, Is.False);
            Assert.That(viewModel.StatusMessage, Does.Contain("2 matched"));
            Assert.That(viewModel.StatusMessage, Does.Contain("1 new"));
        });
    }

    [Test]
    public async Task ScanReviewAsync_ReportsReconciliationAndHeadlessChoice()
    {
        var store = new RecordingStore();
        using var viewModel = new WorkspaceActionsViewModel(store);

        var result = await viewModel.ScanReviewAsync(
            " https://example.test/login ",
            " Login ",
            BrowserKind.Edge,
            headless: false);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(store.ScanRequest!.Url, Is.EqualTo("https://example.test/login"));
            Assert.That(store.ScanRequest.Name, Is.EqualTo("Login"));
            Assert.That(store.ScanRequest.Browser, Is.EqualTo(BrowserKind.Edge));
            Assert.That(store.ScanRequest.Headless, Is.False);
            Assert.That(viewModel.StatusMessage, Does.Contain("2 matched"));
            Assert.That(viewModel.StatusMessage, Does.Contain("1 new"));
        });
    }

    [NUnit.Framework.TestCase(CliqDocumentKind.Test, "checkout")]
    [NUnit.Framework.TestCase(CliqDocumentKind.Page, "login")]
    public async Task GenerateSelectedAsync_ScopesTheRequest(
        CliqDocumentKind kind,
        string id)
    {
        var store = new RecordingStore();
        using var viewModel = new WorkspaceActionsViewModel(store);

        var result = await viewModel.GenerateSelectedAsync(kind, id);

        Assert.That(result, Is.Not.Null);
        if (kind == CliqDocumentKind.Test)
        {
            Assert.That(store.GenerationRequest!.TestIds, Is.EqualTo(new[] { id }));
            Assert.That(store.GenerationRequest.PageIds, Is.Null);
        }
        else
        {
            Assert.That(store.GenerationRequest!.PageIds, Is.EqualTo(new[] { id }));
            Assert.That(store.GenerationRequest.TestIds, Is.Null);
        }
    }

    [Test]
    public async Task GenerateAllAsync_UsesUnscopedRequestAndExposesArtifacts()
    {
        var store = new RecordingStore();
        using var viewModel = new WorkspaceActionsViewModel(store);

        var result = await viewModel.GenerateAllAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(store.GenerationRequest!.PageIds, Is.Null);
            Assert.That(store.GenerationRequest.TestIds, Is.Null);
            Assert.That(viewModel.Artifacts, Has.Count.EqualTo(1));
            Assert.That(viewModel.StatusMessage, Does.Contain("1 artifact"));
        });
    }

    [Test]
    public async Task LocateElementAsync_SendsPageAndElementIdsAndReportsSuccess()
    {
        var store = new RecordingStore();
        using var viewModel = new WorkspaceActionsViewModel(store);

        var result = await viewModel.LocateElementAsync("login-page", "submit-button", BrowserKind.Edge);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(store.LocateCall, Is.Not.Null);
            Assert.That(store.LocateCall!.Value.PageId, Is.EqualTo("login-page"));
            Assert.That(store.LocateCall.Value.ElementId, Is.EqualTo("submit-button"));
            Assert.That(store.LocateCall.Value.Request.Browser, Is.EqualTo(BrowserKind.Edge));
            Assert.That(viewModel.StatusMessage, Does.Contain("highlighted"));
        });
    }

    [Test]
    public async Task LocateElementFromSourceAsync_SendsClassNameAndLocatorCommentAndReportsSuccess()
    {
        var store = new RecordingStore();
        using var viewModel = new WorkspaceActionsViewModel(store);

        var result = await viewModel.LocateElementFromSourceAsync(
            "LoginPage", "// Locator (xpath): //button[@id='submit']", BrowserKind.Edge);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(store.LocateFromSourceRequest, Is.Not.Null);
            Assert.That(store.LocateFromSourceRequest!.ClassName, Is.EqualTo("LoginPage"));
            Assert.That(store.LocateFromSourceRequest.LocatorComment, Is.EqualTo("// Locator (xpath): //button[@id='submit']"));
            Assert.That(store.LocateFromSourceRequest.Browser, Is.EqualTo(BrowserKind.Edge));
            Assert.That(viewModel.StatusMessage, Does.Contain("highlighted"));
        });
    }

    [Test]
    public async Task StartLiveHighlight_ReportsHoveredElementsAsTheStreamPushesThemAndStopClearsIt()
    {
        var store = new RecordingStore();
        using var viewModel = new WorkspaceActionsViewModel(store);

        viewModel.StartLiveHighlight("page-1", BrowserKind.Chrome);
        await store.HoverStarted.Task;

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.IsLiveHighlightActive, Is.True);
            Assert.That(store.HoverStreamCall!.Value.PageId, Is.EqualTo("page-1"));
            Assert.That(store.HoverStreamCall.Value.Browser, Is.EqualTo(BrowserKind.Chrome));
        });

        await store.InvokeHoverChangedAsync("element-1");
        Assert.That(viewModel.HighlightedElementId, Is.EqualTo("element-1"));

        await store.InvokeHoverChangedAsync(null);
        Assert.That(viewModel.HighlightedElementId, Is.Null);

        viewModel.StopLiveHighlight();

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.IsLiveHighlightActive, Is.False);
            Assert.That(viewModel.HighlightedElementId, Is.Null);
        });
    }

    [Test]
    public async Task StartLiveHighlight_ReplacesAnAlreadyRunningStreamForADifferentPage()
    {
        var store = new RecordingStore();
        using var viewModel = new WorkspaceActionsViewModel(store);

        viewModel.StartLiveHighlight("page-1", null);
        await store.HoverStarted.Task;
        await store.InvokeHoverChangedAsync("element-1");

        store.Reset();
        viewModel.StartLiveHighlight("page-2", null);
        await store.HoverStarted.Task;

        Assert.Multiple(() =>
        {
            Assert.That(store.HoverStreamCall!.Value.PageId, Is.EqualTo("page-2"));
            Assert.That(viewModel.HighlightedElementId, Is.Null, "switching pages should clear the previous page's highlight");
            Assert.That(viewModel.IsLiveHighlightActive, Is.True);
        });
    }

    [Test]
    public async Task StartLiveHighlight_WhenStreamDisconnects_ClearsHighlightAndReportsError()
    {
        var store = new RecordingStore { ThrowOnStream = new HoverStreamDisconnectedException("page-1") };
        using var viewModel = new WorkspaceActionsViewModel(store);
        var errorReported = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(viewModel.ErrorMessage) && viewModel.ErrorMessage is not null)
            {
                errorReported.TrySetResult(true);
            }
        };

        viewModel.StartLiveHighlight("page-1", null);
        await errorReported.Task;

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.ErrorMessage, Does.Contain("disconnected"));
            Assert.That(viewModel.HighlightedElementId, Is.Null);
            Assert.That(viewModel.IsLiveHighlightActive, Is.False);
        });
    }

    private sealed class RecordingStore : FakeWorkspaceActionStore
    {
        public ScanPageRequest? ScanRequest { get; private set; }
        public GenerationRequest? GenerationRequest { get; private set; }
        public (string PageId, string ElementId, StartRunRequest Request)? LocateCall { get; private set; }
        public LocateFromSourceRequest? LocateFromSourceRequest { get; private set; }
        public (string PageId, BrowserKind? Browser)? HoverStreamCall { get; private set; }
        public Exception? ThrowOnStream { get; set; }
        public TaskCompletionSource<bool> HoverStarted { get; private set; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private Func<string?, Task>? _onHoverChanged;

        /// <summary>Resets the per-stream state a new StartLiveHighlight call needs (a fresh
        /// "started" gate) without losing the recorded call history from a previous stream -
        /// used by the "replaces an already-running stream" test.</summary>
        public void Reset() => HoverStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task InvokeHoverChangedAsync(string? elementId) => _onHoverChanged!(elementId);

        public override Task<ScanPageResponse> ScanAsync(
            ScanPageRequest request,
            CancellationToken cancellationToken)
        {
            ScanRequest = request;
            return Task.FromResult(
                new ScanPageResponse
                {
                    Page = new PageDefinition(
                        PageDefinition.CurrentSchemaVersion,
                        "login",
                        request.Name,
                        request.Url,
                        Array.Empty<PageElement>(),
                        DateTimeOffset.UtcNow),
                    Reconciliation = new ScanReconciliationReport(2, 1, 0),
                });
        }

        public override Task<StagedPageScanResult> ScanPageReviewAsync(
            ScanPageRequest request,
            CancellationToken cancellationToken)
        {
            ScanRequest = request;
            return Task.FromResult(
                new StagedPageScanResult(
                    new PageDefinition(
                        PageDefinition.CurrentSchemaVersion,
                        "login",
                        request.Name,
                        request.Url,
                        Array.Empty<PageElement>(),
                        DateTimeOffset.UtcNow),
                    new ScanReconciliationReport(2, 1, 0)));
        }

        public override Task<GenerationResult> GenerateAsync(
            GenerationRequest request,
            CancellationToken cancellationToken)
        {
            GenerationRequest = request;
            return Task.FromResult(
                new GenerationResult(
                    new[]
                    {
                        new GeneratedArtifact(
                            "test",
                            "CheckoutTests",
                            @"C:\workspace\Generated\Tests\CheckoutTests.cs",
                            true),
                    },
                    Array.Empty<CustomMethodRecoveryWarning>(),
                    Array.Empty<PendingCustomMethodMerge>()));
        }

        public override Task<IReadOnlyList<PendingCustomMethodEntry>> GetPendingCustomMethodsAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PendingCustomMethodEntry>>(Array.Empty<PendingCustomMethodEntry>());

        public override Task DeletePendingCustomMethodAsync(string id, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public override Task LocateElementAsync(
            string pageId,
            string elementId,
            StartRunRequest request,
            CancellationToken cancellationToken)
        {
            LocateCall = (pageId, elementId, request);
            return Task.CompletedTask;
        }

        public override Task LocateElementFromSourceAsync(
            LocateFromSourceRequest request,
            CancellationToken cancellationToken)
        {
            LocateFromSourceRequest = request;
            return Task.CompletedTask;
        }

        // Mirrors the real contract - runs until cancellationToken is canceled - by blocking on a
        // never-completing delay linked to it, so StopLiveHighlight() cancelling the ViewModel's
        // CancellationTokenSource is what actually ends this call, exactly like the real SSE read
        // loop ending when the caller stops watching.
        public override async Task StreamHoveredElementAsync(
            string pageId,
            BrowserKind? browser,
            Func<string?, Task> onHoverChanged,
            CancellationToken cancellationToken)
        {
            HoverStreamCall = (pageId, browser);
            _onHoverChanged = onHoverChanged;
            if (ThrowOnStream is { } exception)
            {
                HoverStarted.TrySetResult(true);
                throw exception;
            }

            HoverStarted.TrySetResult(true);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
    }
}
