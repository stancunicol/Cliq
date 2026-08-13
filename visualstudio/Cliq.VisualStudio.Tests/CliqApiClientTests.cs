using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Cliq.Domain;
using Cliq.VisualStudio.Core.Api;
using NUnit.Framework;

namespace Cliq.VisualStudio.Tests;

[TestFixture]
public sealed class CliqApiClientTests
{
    [Test]
    public async Task OpenWorkspace_SendsBearerTokenAndCamelCaseBody()
    {
        HttpRequestMessage? captured = null;
        string? body = null;
        var handler = new StubHttpHandler(async request =>
        {
            captured = request;
            body = await request.Content!.ReadAsStringAsync();
            return Json(HttpStatusCode.OK, WorkspaceJson);
        });
        var client = CreateClient(handler, "very-secret-token");

        var workspace = await client.OpenWorkspaceAsync(@"C:\repo", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(captured!.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(captured.RequestUri!.AbsolutePath, Is.EqualTo("/api/v1/workspaces/open"));
            Assert.That(captured.Headers.Authorization, Is.EqualTo(
                new AuthenticationHeaderValue("Bearer", "very-secret-token")));
            Assert.That(body, Does.Contain("\"rootPath\":\"C:\\\\repo\""));
            Assert.That(workspace.Project.DefaultBrowser, Is.EqualTo(BrowserKind.Edge));
        });
    }

    [Test]
    public async Task SaveProject_UsesCamelCaseEnumSerialization()
    {
        string? body = null;
        var handler = new StubHttpHandler(async request =>
        {
            body = await request.Content!.ReadAsStringAsync();
            return Json(HttpStatusCode.OK, ProjectJson);
        });
        var client = CreateClient(handler);
        var project = new ProjectManifest(
            1,
            "sample",
            "Sample",
            "https://example.com",
            BrowserKind.Edge,
            new OutputSettings(),
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"));

        var saved = await client.SaveProjectAsync(project, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(body, Does.Contain("\"defaultBrowser\":\"edge\""));
            Assert.That(saved.DefaultBrowser, Is.EqualTo(BrowserKind.Edge));
        });
    }

    [Test]
    public async Task SavePage_SerializesXPathStrategyAsLowercase()
    {
        string? body = null;
        var handler = new StubHttpHandler(async request =>
        {
            body = await request.Content!.ReadAsStringAsync();
            return Json(HttpStatusCode.OK, body);
        });
        var client = CreateClient(handler);
        var page = new PageDefinition(
            PageDefinition.CurrentSchemaVersion,
            "orders",
            "Orders",
            "https://example.com/orders",
            new[]
            {
                new PageElement(
                    "edit-order",
                    "Edit order",
                    "button",
                    StepAction.Click,
                    new Locator(LocatorStrategy.XPath, "//button[@data-action='edit']")),
            },
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"));

        var saved = await client.SavePageAsync(page, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(body, Does.Contain("\"strategy\":\"xpath\""));
            Assert.That(body, Does.Not.Contain("\"strategy\":\"xPath\""));
            Assert.That(saved.Elements[0].Locator.Strategy, Is.EqualTo(LocatorStrategy.XPath));
        });
    }

    [Test]
    public void WorkspaceConflict_MapsStructuredProblemAndOwner()
    {
        const string problem = """
            {
              "type":"https://example/errors/workspace",
              "title":"Workspace is already in use",
              "status":409,
              "detail":"The workspace is open in another host.",
              "code":"workspace_in_use",
              "owner":{"hostKind":"electron","processId":42,"acquiredAt":"2026-07-29T10:00:00Z"},
              "traceId":"abc"
            }
            """;
        var client = CreateClient(new StubHttpHandler(
            _ => Task.FromResult(Json(HttpStatusCode.Conflict, problem))));

        var exception = Assert.ThrowsAsync<WorkspaceInUseApiException>(
            async () => await client.OpenWorkspaceAsync(@"C:\repo", CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
            Assert.That(exception.Problem.Code, Is.EqualTo("workspace_in_use"));
            Assert.That(exception.Owner!.HostKind, Is.EqualTo("electron"));
            Assert.That(exception.Owner.ProcessId, Is.EqualTo(42));
            Assert.That(exception.Problem.Extensions, Contains.Key("traceId"));
        });
    }

    [Test]
    public void NonJsonError_IsMappedWithoutLeakingBody()
    {
        var client = CreateClient(new StubHttpHandler(
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("token=server-secret"),
            })));

        var exception = Assert.ThrowsAsync<CliqApiException>(
            async () => await client.GetWorkspaceAsync(CancellationToken.None));

        Assert.That(exception!.Message, Does.Not.Contain("server-secret"));
        Assert.That(exception.Message, Does.Contain("non-JSON"));
    }

    [Test]
    public async Task CloseAndCancel_AcceptNoContentAndAcceptedResponses()
    {
        var requests = new List<(HttpMethod Method, string Path)>();
        var handler = new StubHttpHandler(request =>
        {
            requests.Add((request.Method, request.RequestUri!.AbsolutePath));
            var status = request.RequestUri.AbsolutePath.EndsWith("/cancel", StringComparison.Ordinal)
                ? HttpStatusCode.Accepted
                : HttpStatusCode.NoContent;
            return Task.FromResult(new HttpResponseMessage(status));
        });
        var client = CreateClient(handler);

        await client.CloseWorkspaceAsync(CancellationToken.None);
        await client.CancelRunAsync("run/one", CancellationToken.None);

        Assert.That(requests, Is.EqualTo(new[]
        {
            (HttpMethod.Post, "/api/v1/workspaces/close"),
            (HttpMethod.Post, "/api/v1/runs/run%2Fone/cancel"),
        }));
    }

    [Test]
    public async Task LocateElement_PostsToElementRouteWithBrowserBody()
    {
        HttpRequestMessage? captured = null;
        string? body = null;
        var handler = new StubHttpHandler(async request =>
        {
            captured = request;
            body = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var client = CreateClient(handler);

        await client.LocateElementAsync(
            "login-page", "submit-button", new StartRunRequest(BrowserKind.Edge), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(captured!.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(
                captured.RequestUri!.AbsolutePath,
                Is.EqualTo("/api/v1/pages/login-page/elements/submit-button/locate"));
            Assert.That(body, Does.Contain("\"browser\":\"edge\""));
        });
    }

    [Test]
    public async Task LocateElementFromSource_PostsToLocateFromSourceRouteWithClassNameAndCommentBody()
    {
        HttpRequestMessage? captured = null;
        string? body = null;
        var handler = new StubHttpHandler(async request =>
        {
            captured = request;
            body = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var client = CreateClient(handler);

        await client.LocateElementFromSourceAsync(
            new LocateFromSourceRequest("LoginPage", "// Locator (xpath): //button[@id='submit']", BrowserKind.Edge),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(captured!.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(captured.RequestUri!.AbsolutePath, Is.EqualTo("/api/v1/pages/locate-from-source"));
            Assert.That(body, Does.Contain("\"className\":\"LoginPage\""));
            Assert.That(body, Does.Contain("\"browser\":\"edge\""));
        });
    }

    [Test]
    public async Task GetGeneratedPageObjectPath_GetsFromGeneratedPathRouteAndDeserializesPath()
    {
        Uri? requestedUri = null;
        var handler = new StubHttpHandler(request =>
        {
            requestedUri = request.RequestUri;
            return Task.FromResult(Json(HttpStatusCode.OK, "{\"path\":\"C:\\\\workspace\\\\Generated\\\\PageObjects\\\\LoginPage.cs\"}"));
        });
        var client = CreateClient(handler);

        var response = await client.GetGeneratedPageObjectPathAsync("login-page", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(requestedUri!.AbsolutePath, Is.EqualTo("/api/v1/pages/login-page/generated-path"));
            Assert.That(response.Path, Is.EqualTo(@"C:\workspace\Generated\PageObjects\LoginPage.cs"));
        });
    }

    [Test]
    public async Task Screenshot_ReturnsBytesAndEscapesPathSegments()
    {
        Uri? uri = null;
        var handler = new StubHttpHandler(request =>
        {
            uri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new byte[] { 1, 2, 3 }),
            });
        });
        var client = CreateClient(handler);

        var bytes = await client.GetScreenshotAsync("run one", "failure #1.png", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(uri!.AbsolutePath, Is.EqualTo("/api/v1/runs/run%20one/screenshots/failure%20%231.png"));
        });
    }

    [Test]
    public async Task RunEvents_ParsesStepAndDoneWithCamelCaseEnums()
    {
        const string events = """
            event: step
            data: {"stepId":"s1","status":"passed","durationMs":7}

            event: done
            data: {"id":"r1","testId":"t1","status":"passed","startedAt":"2026-07-29T10:00:00Z","completedAt":"2026-07-29T10:00:01Z","steps":[]}

            """;
        var client = CreateClient(new StubHttpHandler(
            _ => Task.FromResult(EventStream(events))));
        var received = new List<RunStreamEvent>();

        await client.StreamRunEventsAsync(
            "r1",
            item =>
            {
                received.Add(item);
                return Task.CompletedTask;
            },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(received, Has.Count.EqualTo(2));
            Assert.That(received[0].Step!.Status, Is.EqualTo(RunStatus.Passed));
            Assert.That(received[1].Run!.Status, Is.EqualTo(RunStatus.Passed));
        });
    }

    [Test]
    public void RunEvents_DisconnectBeforeDone_HasExplicitRetrySurface()
    {
        const string events = """
            event: step
            data: {"stepId":"s1","status":"passed","durationMs":7}

            """;
        var client = CreateClient(new StubHttpHandler(
            _ => Task.FromResult(EventStream(events))));

        var exception = Assert.ThrowsAsync<RunEventStreamDisconnectedException>(
            async () => await client.StreamRunEventsAsync(
                "r1",
                _ => Task.CompletedTask,
                CancellationToken.None));

        Assert.That(exception!.RunId, Is.EqualTo("r1"));
    }

    [Test]
    public void RequestCancellation_IsPropagated()
    {
        var handler = new StubHttpHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var client = CreateClient(handler);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.That(
            Assert.CatchAsync(
                async () => await client.GetWorkspaceAsync(cancellation.Token)),
            Is.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public async Task ScanPage_DeserializesLocatorDiffAndDynamicElementSuggestions()
    {
        var handler = new StubHttpHandler(_ => Task.FromResult(Json(HttpStatusCode.OK, ScanResponseJson)));
        var client = CreateClient(handler);

        var response = await client.ScanPageAsync(
            new ScanPageRequest("https://example.com/orders", "Orders"), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(response.LocatorDiff, Is.Not.Null);
            Assert.That(response.LocatorDiff!.New, Has.Count.EqualTo(1));
            Assert.That(response.LocatorDiff.New[0].Locator.Strategy, Is.EqualTo(LocatorStrategy.Css));
            Assert.That(response.LocatorDiff.Missing, Is.Empty);
            Assert.That(response.DynamicElementSuggestions, Is.Not.Null);
            Assert.That(response.DynamicElementSuggestions!, Has.Count.EqualTo(1));
            Assert.That(response.DynamicElementSuggestions![0].SampleCount, Is.EqualTo(5));
        });
    }

    private static CliqApiClient CreateClient(HttpMessageHandler handler, string token = "token") =>
        new(new HttpClient(handler), new Uri("http://127.0.0.1:43123/"), token);

    private static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    private static HttpResponseMessage EventStream(string value) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(value, Encoding.UTF8, "text/event-stream"),
        };

    private const string ProjectJson = """
        {
          "schemaVersion":1,
          "id":"sample",
          "name":"Sample",
          "baseUrl":"https://example.com",
          "defaultBrowser":"edge",
          "output":{
            "pageObjectsDirectory":"Generated/PageObjects",
            "testsDirectory":"Generated/Tests",
            "pageObjectsNamespace":"Generated.PageObjects",
            "testsNamespace":"Generated.Tests"
          },
          "updatedAt":"2026-01-01T00:00:00Z"
        }
        """;

    private const string ScanResponseJson = """
        {
          "page":{
            "schemaVersion":1,
            "id":"orders",
            "name":"Orders",
            "url":"https://example.com/orders",
            "elements":[],
            "updatedAt":"2026-01-01T00:00:00Z"
          },
          "reconciliation":{"matchedCount":1,"newCount":1,"missingCount":0},
          "locatorDiff":{
            "pageId":"orders",
            "scannedAt":"2026-01-01T00:00:00Z",
            "new":[{"elementId":"e1","name":"Submit","tag":"button","locator":{"strategy":"css","value":"#submit"}}],
            "missing":[]
          },
          "dynamicElementSuggestions":[
            {"name":"rowAction","action":"click","strategy":"css","locatorTemplate":"#row-{0}","sampleCount":5}
          ]
        }
        """;

    private const string WorkspaceJson = """
        {
          "rootPath":"C:\\repo",
          "project":{
            "schemaVersion":1,
            "id":"sample",
            "name":"Sample",
            "baseUrl":"https://example.com",
            "defaultBrowser":"edge",
            "output":{
              "pageObjectsDirectory":"Generated/PageObjects",
              "testsDirectory":"Generated/Tests",
              "pageObjectsNamespace":"Generated.PageObjects",
              "testsNamespace":"Generated.Tests"
            },
            "updatedAt":"2026-01-01T00:00:00Z"
          },
          "pages":[],
          "tests":[]
        }
        """;

    private sealed class StubHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public StubHttpHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
            : this((request, _) => handler(request))
        {
        }

        public StubHttpHandler(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) =>
            _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            _handler(request, cancellationToken);
    }
}
