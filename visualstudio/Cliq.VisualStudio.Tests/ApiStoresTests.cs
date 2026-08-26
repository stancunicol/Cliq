using System.Net;
using System.Net.Http;
using System.Text;
using Cliq.Domain;
using Cliq.VisualStudio.Core.Api;
using Cliq.VisualStudio.Core.Authoring;
using NUnit.Framework;

namespace Cliq.VisualStudio.Tests;

[TestFixture]
public sealed class ApiStoresTests
{
    [Test]
    public async Task ApiRunStore_StartAsync_DelegatesToStartRunEndpoint()
    {
        var session = new FakeSession(Json(HttpStatusCode.OK, TestRunJson));
        var store = new ApiRunStore(session);

        var run = await store.StartAsync("t1", BrowserKind.Chrome, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(session.LastMethod, Is.EqualTo(HttpMethod.Post));
            Assert.That(session.LastPath, Is.EqualTo("/api/v1/tests/t1/runs"));
            Assert.That(run.Id, Is.EqualTo("r1"));
        });
    }

    [Test]
    public async Task ApiRunStore_GetAsync_DelegatesToGetRunEndpoint()
    {
        var session = new FakeSession(Json(HttpStatusCode.OK, TestRunJson));
        var store = new ApiRunStore(session);

        var run = await store.GetAsync("r1", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(session.LastMethod, Is.EqualTo(HttpMethod.Get));
            Assert.That(session.LastPath, Is.EqualTo("/api/v1/runs/r1"));
            Assert.That(run.Id, Is.EqualTo("r1"));
        });
    }

    [Test]
    public async Task ApiRunStore_CancelAsync_DelegatesToCancelEndpoint()
    {
        var session = new FakeSession(new HttpResponseMessage(HttpStatusCode.Accepted));
        var store = new ApiRunStore(session);

        await store.CancelAsync("r1", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(session.LastMethod, Is.EqualTo(HttpMethod.Post));
            Assert.That(session.LastPath, Is.EqualTo("/api/v1/runs/r1/cancel"));
        });
    }

    [Test]
    public async Task ApiRunStore_GetScreenshotAsync_ReturnsBytes()
    {
        var session = new FakeSession(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(new byte[] { 1, 2, 3 }),
        });
        var store = new ApiRunStore(session);

        var bytes = await store.GetScreenshotAsync("r1", "shot.png", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(session.LastMethod, Is.EqualTo(HttpMethod.Get));
            Assert.That(session.LastPath, Is.EqualTo("/api/v1/runs/r1/screenshots/shot.png"));
            Assert.That(bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public async Task ApiRunStore_StreamAsync_ForwardsStreamEvents()
    {
        const string events = """
            event: done
            data: {"id":"r1","testId":"t1","status":"passed","startedAt":"2026-07-29T10:00:00Z","completedAt":"2026-07-29T10:00:01Z","steps":[]}

            """;
        var session = new FakeSession(EventStream(events));
        var store = new ApiRunStore(session);
        var received = new List<RunStreamEvent>();

        await store.StreamAsync("r1", item =>
        {
            received.Add(item);
            return Task.CompletedTask;
        }, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(session.LastPath, Is.EqualTo("/api/v1/runs/r1/events"));
            Assert.That(received, Has.Count.EqualTo(1));
            Assert.That(received[0].IsTerminal, Is.True);
            Assert.That(received[0].Run!.Status, Is.EqualTo(RunStatus.Passed));
        });
    }

    [Test]
    public async Task ApiRunStore_StreamAsync_WithCallbackContext_MarshalsOntoThatContext()
    {
        const string events = """
            event: step
            data: {"stepId":"s1","status":"passed","durationMs":7}

            event: done
            data: {"id":"r1","testId":"t1","status":"passed","startedAt":"2026-07-29T10:00:00Z","completedAt":"2026-07-29T10:00:01Z","steps":[]}

            """;
        var session = new FakeSession(EventStream(events));
        var captured = new TestSynchronizationContext();
        var store = new ApiRunStore(session, captured);
        var seenContexts = new List<SynchronizationContext?>();

        await store.StreamAsync("r1", item =>
        {
            seenContexts.Add(SynchronizationContext.Current);
            return Task.CompletedTask;
        }, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(seenContexts, Has.Count.EqualTo(2));
            Assert.That(seenContexts, Has.All.EqualTo(captured));
        });
    }

    // A SynchronizationContext that surfaces itself as Current while a posted callback runs -
    // plain SynchronizationContext.Post just invokes the delegate on the calling thread without
    // setting Current, which would defeat the point of this test.
    private sealed class TestSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state)
        {
            var previous = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(this);
            try
            {
                d(state);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }
        }
    }

    [Test]
    public async Task ApiRunStore_StreamAsync_CallbackException_IsPropagatedToCaller()
    {
        const string events = """
            event: done
            data: {"id":"r1","testId":"t1","status":"passed","startedAt":"2026-07-29T10:00:00Z","completedAt":"2026-07-29T10:00:01Z","steps":[]}

            """;
        var session = new FakeSession(EventStream(events));
        var store = new ApiRunStore(session);

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await store.StreamAsync(
                "r1",
                _ => Task.FromException(new InvalidOperationException("callback failed")),
                CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("callback failed"));
    }

    [Test]
    public async Task ApiRunStore_StreamAsync_CallbackException_OnCapturedContext_IsPropagated()
    {
        const string events = """
            event: done
            data: {"id":"r1","testId":"t1","status":"passed","startedAt":"2026-07-29T10:00:00Z","completedAt":"2026-07-29T10:00:01Z","steps":[]}

            """;
        var session = new FakeSession(EventStream(events));
        var store = new ApiRunStore(session, new SynchronizationContext());

        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await store.StreamAsync(
                "r1",
                _ => Task.FromException(new InvalidOperationException("context callback failed")),
                CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo("context callback failed"));
    }

    [Test]
    public async Task ApiWorkbenchStore_LoadSecretsAsync_OrdersKeysCaseInsensitively()
    {
        var session = new FakeSession(Json(HttpStatusCode.OK, """{"zeta":"1","alpha":"2","Beta":"3"}"""));
        var store = new ApiWorkbenchStore(session);

        var secrets = await store.LoadSecretsAsync(CancellationToken.None);

        Assert.That(secrets.Select(secret => secret.Key), Is.EqualTo(new[] { "alpha", "Beta", "zeta" }));
        Assert.That(secrets, Has.All.Matches<SecretDescriptor>(secret => secret.HasValue));
    }

    [Test]
    public async Task ApiWorkbenchStore_LoadAsync_DelegatesToGetWorkspaceEndpoint()
    {
        var session = new FakeSession(Json(HttpStatusCode.OK, WorkspaceJson));
        var store = new ApiWorkbenchStore(session);

        var snapshot = await store.LoadAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(session.LastMethod, Is.EqualTo(HttpMethod.Get));
            Assert.That(session.LastPath, Is.EqualTo("/api/v1/workspace"));
            Assert.That(snapshot.Project.DefaultBrowser, Is.EqualTo(BrowserKind.Edge));
        });
    }

    private const string TestRunJson = """
        {"id":"r1","testId":"t1","status":"passed","startedAt":"2026-07-29T10:00:00Z","completedAt":"2026-07-29T10:00:01Z","steps":[]}
        """;

    private const string WorkspaceJson = """
        {"rootPath":"C:\\repo","project":{"schemaVersion":1,"id":"sample","name":"Sample","baseUrl":"https://example.com","defaultBrowser":"edge","output":{"pageObjectsDirectory":"Generated/PageObjects","testsDirectory":"Generated/Tests","pageObjectsNamespace":"Generated.PageObjects","testsNamespace":"Generated.Tests"},"updatedAt":"2026-07-29T10:00:00Z"},"pages":[],"tests":[]}
        """;

    private static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static HttpResponseMessage EventStream(string value) =>
        new(HttpStatusCode.OK) { Content = new StringContent(value, Encoding.UTF8, "text/event-stream") };

    private sealed class FakeSession : ICliqApiSession
    {
        private readonly CliqApiClient _client;

        public FakeSession(HttpResponseMessage response)
        {
            var handler = new StubHandler(request =>
            {
                LastMethod = request.Method;
                LastPath = request.RequestUri!.AbsolutePath;
                return Task.FromResult(response);
            });
            _client = new CliqApiClient(new HttpClient(handler), new Uri("http://127.0.0.1:43123/"), "token");
        }

        public HttpMethod? LastMethod { get; private set; }
        public string? LastPath { get; private set; }

        public Task<CliqApiClient> GetClientAsync(CancellationToken cancellationToken) =>
            Task.FromResult(_client);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) => _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            _handler(request);
    }
}
