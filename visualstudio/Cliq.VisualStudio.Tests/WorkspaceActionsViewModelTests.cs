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

    private sealed class RecordingStore : IWorkspaceActionStore
    {
        public ScanPageRequest? ScanRequest { get; private set; }
        public GenerationRequest? GenerationRequest { get; private set; }

        public Task<ScanPageResponse> ScanAsync(
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

        public Task<GenerationResult> GenerateAsync(
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
                    }));
        }
    }
}
