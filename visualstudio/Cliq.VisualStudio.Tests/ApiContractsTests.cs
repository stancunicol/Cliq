using System.Net;
using System.Net.Http;
using System.Text;
using Cliq.Domain;
using Cliq.VisualStudio.Core.Api;
using Cliq.VisualStudio.Core.Runtime;
using NUnit.Framework;

namespace Cliq.VisualStudio.Tests;

[TestFixture]
public sealed class ApiContractsTests
{
    [Test]
    public void RunStreamEvent_StepCompleted_IsNotTerminalAndCarriesStep()
    {
        var step = new StepResult("s1", RunStatus.Passed, 5);

        var item = RunStreamEvent.StepCompleted(step);

        Assert.Multiple(() =>
        {
            Assert.That(item.EventName, Is.EqualTo("step"));
            Assert.That(item.IsTerminal, Is.False);
            Assert.That(item.Step, Is.EqualTo(step));
            Assert.That(item.Run, Is.Null);
        });
    }

    [Test]
    public void RunStreamEvent_Done_IsTerminalAndCarriesRun()
    {
        var run = new TestRun("r1", "t1", RunStatus.Passed, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, []);

        var item = RunStreamEvent.Done(run);

        Assert.Multiple(() =>
        {
            Assert.That(item.EventName, Is.EqualTo("done"));
            Assert.That(item.IsTerminal, Is.True);
            Assert.That(item.Run, Is.EqualTo(run));
            Assert.That(item.Step, Is.Null);
        });
    }

    [Test]
    public async Task SidecarConnection_CreateApiClient_SendsBearerTokenToBaseAddress()
    {
        const string runJson = """
            {"id":"r1","testId":"t1","status":"passed","startedAt":"2026-07-29T10:00:00Z","completedAt":"2026-07-29T10:00:01Z","steps":[]}
            """;
        var handler = new StubHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(runJson, Encoding.UTF8, "application/json"),
        }));
        var connection = new SidecarConnection(new Uri("http://127.0.0.1:43123/"), "secret-token", 42);

        using var httpClient = new HttpClient(handler);
        var client = connection.CreateApiClient(httpClient);
        var run = await client.GetRunAsync("r1", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(run.Id, Is.EqualTo("r1"));
            Assert.That(connection.Address, Is.EqualTo(new Uri("http://127.0.0.1:43123/")));
            Assert.That(connection.ProcessId, Is.EqualTo(42));
        });
    }

    [Test]
    public void RunEventStreamDisconnectedException_CarriesRunId()
    {
        var exception = new RunEventStreamDisconnectedException("r1");

        Assert.Multiple(() =>
        {
            Assert.That(exception.RunId, Is.EqualTo("r1"));
            Assert.That(exception.Message, Does.Contain("r1"));
        });
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
