using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cliq.Application;
using Cliq.Infrastructure;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;

namespace Cliq.Api;

internal static class CliqApiApplication
{
    internal static WebApplication Build(
        string[] args,
        Action<IServiceCollection>? configureServices = null)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddProblemDetails();
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        });
        builder.Services.AddSingleton<WorkspaceRepository>();
        builder.Services.AddSingleton<IWorkspaceRepository>(services => services.GetRequiredService<WorkspaceRepository>());
        builder.Services.AddSingleton<ISecretStore, WorkspaceSecretStore>();
        builder.Services.AddSingleton<IPageDiscovery, PuppeteerPageDiscovery>();
        builder.Services.AddSingleton<IScanReviewService, ScanReviewService>();
        builder.Services.AddSingleton<ICodeGeneration, WorkspaceCodeGeneration>();
        builder.Services.AddSingleton<ITestRunner, SeleniumTestRunner>();
        builder.Services.AddSingleton<RunCoordinator>();
        configureServices?.Invoke(builder.Services);

        var app = builder.Build();
        var eventJsonOptions = CliqJsonOptions.Compact;
        var apiToken = app.Configuration["CLIQ_API_TOKEN"];
        if (string.IsNullOrWhiteSpace(apiToken))
        {
            throw new InvalidOperationException("CLIQ_API_TOKEN must be provided by the sidecar host.");
        }

        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/health")
                || context.Request.Path.StartsWithSegments("/swagger"))
            {
                await next();
                return;
            }

            var provided = context.Request.Headers.Authorization.ToString();
            var expected = $"Bearer {apiToken}";
            if (!FixedTimeEquals(provided, expected))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = "A valid sidecar token is required.",
                });
                return;
            }

            await next();
        });

        app.Use(async (context, next) =>
        {
            try
            {
                await next();
            }
            catch (Exception exception)
            {
                var problem = ApiErrors.Create(exception);
                context.Response.StatusCode = problem.Status!.Value;
                await context.Response.WriteAsJsonAsync(problem);
            }
        });

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        MapEndpoints(app, eventJsonOptions);
        return app;
    }

    internal static async Task RunAsync(WebApplication app)
    {
        await app.StartAsync();
        var server = app.Services.GetRequiredService<IServer>();
        var address = server.Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault()
            ?? throw new InvalidOperationException("Kestrel did not publish a listening address.");
        Console.Out.WriteLine($"CLIQ_READY {JsonSerializer.Serialize(new { address })}");
        Console.Out.Flush();
        await app.WaitForShutdownAsync();
    }

    private static void MapEndpoints(WebApplication app, JsonSerializerOptions eventJsonOptions)
    {
        var api = app.MapGroup("/api/v1");

        api.MapWorkspaceEndpoints();
        api.MapProjectEndpoints();
        api.MapSettingsEndpoints();
        api.MapPageEndpoints();
        api.MapScanReviewEndpoints();
        api.MapTestEndpoints();
        api.MapSecretEndpoints();
        api.MapGenerationEndpoints();
        api.MapRunEndpoints(eventJsonOptions);

        app.MapHealthEndpoints();
    }

    private static bool FixedTimeEquals(string provided, string expected)
    {
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return providedBytes.Length == expectedBytes.Length
            && CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
    }
}
