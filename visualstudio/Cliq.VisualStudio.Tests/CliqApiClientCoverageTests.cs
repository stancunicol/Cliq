using System.Net;
using System.Net.Http;
using System.Text;
using Cliq.Domain;
using Cliq.VisualStudio.Core.Api;
using NUnit.Framework;

namespace Cliq.VisualStudio.Tests;

[TestFixture]
public sealed class CliqApiClientCoverageTests
{
    [Test]
    public async Task Generate_SerializesResolutionsAndDiscardsWithCamelCaseEnum()
    {
        string? body = null;
        var handler = new StubHttpHandler(async request =>
        {
            body = await request.Content!.ReadAsStringAsync();
            return Json(HttpStatusCode.OK, """{"artifacts":[],"warnings":[],"pendingMerges":[]}""");
        });
        var client = CreateClient(handler);
        var resolutions = new Dictionary<string, IReadOnlyList<string>> { ["login"] = new[] { "method()" } };
        var discards = new Dictionary<string, IReadOnlyList<string>> { ["login"] = new[] { "old()" } };

        var result = await client.GenerateAsync(
            new GenerationRequest(null, new[] { "test-1" }, resolutions, discards),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(body, Does.Contain("\"testIds\":[\"test-1\"]"));
            Assert.That(body, Does.Contain("\"customMethodResolutions\":{\"login\":[\"method()\"]}"));
            Assert.That(body, Does.Contain("\"customMethodDiscards\":{\"login\":[\"old()\"]}"));
            Assert.That(result.Artifacts, Is.Empty);
        });
    }

    [Test]
    public async Task GetSettingsAsync_RoundTripsCamelCaseEnums()
    {
        const string settingsJson = """
            {"schemaVersion":1,"elementIdentification":{"testIdAttribute":"data-testid","strategyOrder":["testId","xpath"],"ignoreIdPatterns":[]},"testExecution":{"defaultStepTimeoutMs":10000,"pageReadyTimeoutMs":15000,"captureScreenshotOnFailure":true,"highlightPauseMs":400},"scanning":{"loadTimeoutMs":30000,"networkIdleGraceMs":3000,"captureScreenshot":true}}
            """;
        var client = CreateClient(new StubHttpHandler(
            _ => Task.FromResult(Json(HttpStatusCode.OK, settingsJson))));

        var settings = await client.GetSettingsAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(settings.ElementIdentification.StrategyOrder, Is.EqualTo(new[] { "testId", "xpath" }));
            Assert.That(settings.TestExecution.HighlightPauseMs, Is.EqualTo(400));
        });
    }

    [Test]
    public async Task SaveTestAsync_RoundTripsARejectedSecretStep()
    {
        const string testJson = """
            {"schemaVersion":1,"id":"t1","name":"Login","description":null,"tags":[],"steps":[{"id":"s1","action":"fill","label":"Fill","enabled":true,"pageId":"login","elementId":"email","url":null,"value":{"kind":"secret","value":"password"},"timeoutMs":null}],"updatedAt":"2026-07-29T10:00:00Z"}
            """;
        var client = CreateClient(new StubHttpHandler(
            _ => Task.FromResult(Json(HttpStatusCode.OK, testJson))));

        var saved = await client.SaveTestAsync(new TestCase(
            1, "t1", "Login", null, [], [], DateTimeOffset.UtcNow), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(saved.Steps.Single().Value!.Kind, Is.EqualTo("secret"));
            Assert.That(saved.Steps.Single().Value!.Value, Is.EqualTo("password"));
        });
    }

    [Test]
    public async Task GetPageAsync_ReturnsThePage()
    {
        const string pageJson = """
            {"schemaVersion":1,"id":"orders","name":"Orders","url":"https://example.com/orders","elements":[],"updatedAt":"2026-07-29T10:00:00Z"}
            """;
        var client = CreateClient(new StubHttpHandler(
            _ => Task.FromResult(Json(HttpStatusCode.OK, pageJson))));

        var page = await client.GetPageAsync("orders", CancellationToken.None);

        Assert.That(page.Name, Is.EqualTo("Orders"));
    }

    [Test]
    public async Task SecretKey_IsEscapedInTheUrlPath()
    {
        HttpRequestMessage? captured = null;
        var client = CreateClient(new StubHttpHandler(request =>
        {
            captured = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        }));

        await client.DeleteSecretAsync("connection string", CancellationToken.None);

        Assert.That(captured!.RequestUri!.AbsolutePath, Is.EqualTo("/api/v1/secrets/connection%20string"));
    }

    [Test]
    public async Task GetPendingCustomMethodsAsync_RoundTripsPendingEntries()
    {
        const string pendingJson = """
            [{"id":"e1","pageId":"login","pageObjectName":"LoginPage","signature":"public void A()","sourceText":"public void A() { }","hasSyntaxError":false,"deferredAt":"2026-07-29T10:00:00Z"}]
            """;
        var client = CreateClient(new StubHttpHandler(
            _ => Task.FromResult(Json(HttpStatusCode.OK, pendingJson))));

        var entries = await client.GetPendingCustomMethodsAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(entries, Has.Count.EqualTo(1));
            Assert.That(entries[0].PageObjectName, Is.EqualTo("LoginPage"));
            Assert.That(entries[0].HasSyntaxError, Is.False);
        });
    }

    [Test]
    public async Task StartRunAsync_PostsAndReturnsTheQueuedRun()
    {
        HttpRequestMessage? captured = null;
        var client = CreateClient(new StubHttpHandler(request =>
        {
            captured = request;
            return Task.FromResult(Json(HttpStatusCode.OK, """{"id":"r1","testId":"t1","status":"queued","startedAt":"2026-07-29T10:00:00Z","completedAt":null,"steps":[]}"""));
        }));

        var run = await client.StartRunAsync("t1", new StartRunRequest(BrowserKind.Edge), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(captured!.RequestUri!.AbsolutePath, Is.EqualTo("/api/v1/tests/t1/runs"));
            Assert.That(run.Status, Is.EqualTo(RunStatus.Queued));
        });
    }

    [Test]
    public async Task GetRunAsync_ReturnsTheRun()
    {
        const string runJson = "{\"id\":\"r1\",\"testId\":\"t1\",\"status\":\"passed\",\"startedAt\":\"2026-07-29T10:00:00Z\",\"completedAt\":\"2026-07-29T10:00:01Z\",\"steps\":[]}";
        var client = CreateClient(new StubHttpHandler(
            _ => Task.FromResult(Json(HttpStatusCode.OK, runJson))));

        var run = await client.GetRunAsync("r1", CancellationToken.None);

        Assert.That(run.Id, Is.EqualTo("r1"));
    }

    private static CliqApiClient CreateClient(HttpMessageHandler handler, string token = "token") =>
        new(new HttpClient(handler), new Uri("http://127.0.0.1:43123/"), token);

    private static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class StubHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public StubHttpHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) => _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            _handler(request);
    }
}
