using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cliq.IntegrationTests;

internal sealed class FixtureWebServer : IAsyncDisposable
{
    private readonly WebApplication _app;

    private FixtureWebServer(WebApplication app, Uri address)
    {
        _app = app;
        BaseAddress = address;
    }

    internal Uri BaseAddress { get; }

    internal static async Task<FixtureWebServer> StartAsync()
    {
        var fixtureDirectory = Path.Combine(AppContext.BaseDirectory, "Fixtures", "site");
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production,
            ContentRootPath = AppContext.BaseDirectory,
        });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        var app = builder.Build();
        app.MapGet("/", () => ServeFileAsync(Path.Combine(fixtureDirectory, "index.html")));
        app.MapGet("/complete.html", () => ServeFileAsync(Path.Combine(fixtureDirectory, "complete.html")));
        await app.StartAsync();

        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()?.Addresses.SingleOrDefault()
            ?? throw new InvalidOperationException("The browser fixture did not publish an address.");
        return new FixtureWebServer(app, new Uri(address));
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    private static async Task<IResult> ServeFileAsync(string path)
    {
        var html = await File.ReadAllTextAsync(path);
        return Results.Content(html, "text/html");
    }
}
