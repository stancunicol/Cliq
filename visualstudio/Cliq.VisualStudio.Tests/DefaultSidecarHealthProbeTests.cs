using System.Net;
using System.Net.Http;
using Cliq.VisualStudio.Core.Runtime;
using NUnit.Framework;

namespace Cliq.VisualStudio.Tests;

[TestFixture]
public sealed class DefaultSidecarHealthProbeTests
{
    [Test]
    public async Task CheckReadyAsync_Healthy_Succeeds()
    {
        var handler = new StubHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        using var httpClient = new HttpClient(handler);
        var probe = new DefaultSidecarHealthProbe(httpClient);

        await probe.CheckReadyAsync(new Uri("http://127.0.0.1:43123/"), CancellationToken.None);
    }

    [Test]
    public void CheckReadyAsync_NonSuccess_ThrowsSidecarStartupExceptionWithStatus()
    {
        var handler = new StubHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        using var httpClient = new HttpClient(handler);
        var probe = new DefaultSidecarHealthProbe(httpClient);

        var exception = Assert.ThrowsAsync<SidecarStartupException>(async () =>
            await probe.CheckReadyAsync(new Uri("http://127.0.0.1:43123/"), CancellationToken.None));

        Assert.That(exception!.Message, Does.Contain("503"));
    }

    [Test]
    public void CheckReadyAsync_ProbesTheHealthReadyPath()
    {
        Uri? requested = null;
        var handler = new StubHandler(request =>
        {
            requested = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:43123/") };
        var probe = new DefaultSidecarHealthProbe(httpClient);

        Assert.DoesNotThrowAsync(async () =>
            await probe.CheckReadyAsync(new Uri("http://127.0.0.1:43123/"), CancellationToken.None));

        Assert.That(requested, Is.EqualTo(new Uri("http://127.0.0.1:43123/health/ready")));
    }

    [Test]
    public void Constructor_RejectsNullHttpClient()
    {
        Assert.Throws<ArgumentNullException>(() => new DefaultSidecarHealthProbe(null!));
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
