using System.Net.Http.Headers;
using Cliq.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Cliq.IntegrationTests;

internal sealed class IntegrationApiHost : IAsyncDisposable
{
    private const string ApiToken = "integration-test-token";
    private readonly WebApplication _app;

    private IntegrationApiHost(WebApplication app, Uri address)
    {
        _app = app;
        Client = new HttpClient { BaseAddress = address };
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiToken);
        UnauthenticatedClient = new HttpClient { BaseAddress = address };
    }

    internal HttpClient Client { get; }
    internal HttpClient UnauthenticatedClient { get; }

    internal static async Task<IntegrationApiHost> StartAsync(
        Action<IServiceCollection>? configureServices = null)
    {
        var app = CliqApiApplication.Build(
            [
                "--urls=http://127.0.0.1:0",
                $"--CLIQ_API_TOKEN={ApiToken}",
                "--environment=Production",
            ],
            configureServices);
        await app.StartAsync();
        var addresses = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()?.Addresses;
        var address = addresses?.SingleOrDefault()
            ?? throw new InvalidOperationException("The integration-test API did not publish an address.");
        return new IntegrationApiHost(app, new Uri(address));
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        UnauthenticatedClient.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
