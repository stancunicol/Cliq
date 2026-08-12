using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cliq.Api;
using Cliq.Application;
using Cliq.Domain;
using Cliq.Infrastructure;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;

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

var app = builder.Build();
var eventJsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
eventJsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
var apiToken = Environment.GetEnvironmentVariable("CLIQ_API_TOKEN");
if (string.IsNullOrWhiteSpace(apiToken))
{
    throw new InvalidOperationException("CLIQ_API_TOKEN must be provided by the sidecar host.");
}

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/health") || context.Request.Path.StartsWithSegments("/swagger"))
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

var api = app.MapGroup("/api/v1");

api.MapPost("/workspaces/open", async (
    OpenWorkspaceRequest request,
    IWorkspaceRepository repository,
    CancellationToken cancellationToken) =>
    Results.Ok(await repository.OpenAsync(request.RootPath, cancellationToken)));

api.MapPost("/workspaces/close", async (
    IWorkspaceRepository repository,
    CancellationToken cancellationToken) =>
{
    await repository.CloseAsync(cancellationToken);
    return Results.NoContent();
});

api.MapGet("/workspace", async (
    IWorkspaceRepository repository,
    CancellationToken cancellationToken) =>
    Results.Ok(await repository.GetSnapshotAsync(cancellationToken)));

api.MapGet("/project", async (
    IWorkspaceRepository repository,
    CancellationToken cancellationToken) =>
    Results.Ok((await repository.GetSnapshotAsync(cancellationToken)).Project));

api.MapPut("/project", async (
    ProjectManifest project,
    IWorkspaceRepository repository,
    CancellationToken cancellationToken) =>
    Results.Ok(await repository.SaveProjectAsync(project, cancellationToken)));

api.MapGet("/settings", async (
    IWorkspaceRepository repository,
    CancellationToken cancellationToken) =>
    Results.Ok(await repository.GetSettingsAsync(cancellationToken)));

api.MapPut("/settings", async (
    CliqSettings settings,
    IWorkspaceRepository repository,
    CancellationToken cancellationToken) =>
    Results.Ok(await repository.SaveSettingsAsync(settings, cancellationToken)));

api.MapGet("/pages", async (
    IWorkspaceRepository repository,
    CancellationToken cancellationToken) =>
    Results.Ok(await repository.ListPagesAsync(cancellationToken)));

api.MapGet("/pages/{id}", async (
    string id,
    IWorkspaceRepository repository,
    CancellationToken cancellationToken) =>
    await repository.GetPageAsync(id, cancellationToken) is { } page
        ? Results.Ok(page)
        : Results.NotFound());

api.MapPut("/pages/{id}", async (
    string id,
    PageDefinition page,
    IWorkspaceRepository repository,
    CancellationToken cancellationToken) =>
{
    if (!string.Equals(id, page.Id, StringComparison.Ordinal))
    {
        return Results.BadRequest(new ProblemDetails { Title = "Page ID mismatch." });
    }

    return Results.Ok(await repository.SavePageAsync(page, cancellationToken));
});

api.MapDelete("/pages/{id}", async (
    string id,
    IWorkspaceRepository repository,
    CancellationToken cancellationToken) =>
{
    await repository.DeletePageAsync(id, cancellationToken);
    return Results.NoContent();
});

api.MapPost("/pages/scan", async (
    ScanPageRequest request,
    IPageDiscovery discovery,
    IWorkspaceRepository repository,
    CancellationToken cancellationToken) =>
{
    var snapshot = await repository.GetSnapshotAsync(cancellationToken);
    var result = await discovery.ScanAsync(
        request.Url,
        request.Name,
        request.Browser ?? snapshot.Project.DefaultBrowser,
        request.Headless,
        cancellationToken);
    var saved = await repository.SavePageAsync(result.Page, cancellationToken);
    return Results.Ok(new ScanPageResponse(
        saved, result.Reconciliation, result.LocatorDiff, result.DynamicElementSuggestions, result.ScreenshotPath));
});

api.MapPost("/pages/scan-reviews", async (
    ScanPageRequest request,
    IScanReviewService reviews,
    IWorkspaceRepository repository,
    CancellationToken cancellationToken) =>
{
    var snapshot = await repository.GetSnapshotAsync(cancellationToken);
    return Results.Ok(await reviews.ScanAsync(
        request.Url,
        request.Name,
        request.Browser ?? snapshot.Project.DefaultBrowser,
        request.Headless,
        cancellationToken));
});

api.MapGet("/scan-reviews", async (
    string? pageId,
    ScanReviewStatus? status,
    IScanReviewService reviews,
    CancellationToken cancellationToken) =>
    Results.Ok(await reviews.ListAsync(pageId, status, cancellationToken)));

api.MapGet("/scan-reviews/{reviewId}", async (
    string reviewId,
    IScanReviewService reviews,
    CancellationToken cancellationToken) =>
    Results.Ok(await reviews.GetAsync(reviewId, cancellationToken)));

api.MapPut("/scan-reviews/{reviewId}/decisions", async (
    string reviewId,
    UpdateScanReviewDecisionsRequest request,
    IScanReviewService reviews,
    CancellationToken cancellationToken) =>
    Results.Ok(await reviews.UpdateDecisionsAsync(
        reviewId,
        (request.Decisions ?? []).Select(decision =>
            new ScanReviewDecisionUpdate(decision.ItemId, decision.Decision)).ToList(),
        cancellationToken)));

api.MapPost("/scan-reviews/{reviewId}/apply", async (
    string reviewId,
    IScanReviewService reviews,
    CancellationToken cancellationToken) =>
    Results.Ok(await reviews.ApplyAsync(reviewId, cancellationToken)));

api.MapPost("/scan-reviews/{reviewId}/discard", async (
    string reviewId,
    IScanReviewService reviews,
    CancellationToken cancellationToken) =>
    Results.Ok(await reviews.DiscardAsync(reviewId, cancellationToken)));

api.MapGet("/pages/discovery-screenshots/{fileName}", (
    string fileName,
    IWorkspaceRepository repository) =>
    repository.GetDiscoveryScreenshotPath(fileName) is { } path
        ? Results.File(path, "image/png")
        : Results.NotFound());

api.MapGet("/tests", async (
    IWorkspaceRepository repository,
    CancellationToken cancellationToken) =>
    Results.Ok(await repository.ListTestsAsync(cancellationToken)));

api.MapGet("/tests/{id}", async (
    string id,
    IWorkspaceRepository repository,
    CancellationToken cancellationToken) =>
    await repository.GetTestAsync(id, cancellationToken) is { } test
        ? Results.Ok(test)
        : Results.NotFound());

api.MapPut("/tests/{id}", async (
    string id,
    TestCase test,
    IWorkspaceRepository repository,
    CancellationToken cancellationToken) =>
{
    if (!string.Equals(id, test.Id, StringComparison.Ordinal))
    {
        return Results.BadRequest(new ProblemDetails { Title = "Test ID mismatch." });
    }

    return Results.Ok(await repository.SaveTestAsync(test, cancellationToken));
});

api.MapDelete("/tests/{id}", async (
    string id,
    IWorkspaceRepository repository,
    CancellationToken cancellationToken) =>
{
    await repository.DeleteTestAsync(id, cancellationToken);
    return Results.NoContent();
});

api.MapPut("/secrets", async (
    Dictionary<string, string> values,
    ISecretStore secretStore,
    CancellationToken cancellationToken) =>
{
    await secretStore.SaveAsync(values, cancellationToken);
    return Results.NoContent();
});

api.MapGet("/secrets", async (
    ISecretStore secretStore,
    CancellationToken cancellationToken) =>
    Results.Ok(await secretStore.ReadAsync(cancellationToken)));

api.MapDelete("/secrets/{key}", async (
    string key,
    ISecretStore secretStore,
    CancellationToken cancellationToken) =>
{
    await secretStore.DeleteAsync(key, cancellationToken);
    return Results.NoContent();
});

api.MapPost("/generation", async (
    GenerationRequest request,
    IWorkspaceRepository repository,
    ICodeGeneration generation,
    CancellationToken cancellationToken) =>
{
    var snapshot = await repository.GetSnapshotAsync(cancellationToken);
    var testIds = request.TestIds?.ToHashSet(StringComparer.Ordinal);
    var tests = testIds is null
        ? snapshot.Tests
        : snapshot.Tests.Where(test => testIds.Contains(test.Id)).ToList();
    var pageIds = request.PageIds?.ToHashSet(StringComparer.Ordinal);
    if (pageIds is null && testIds is not null)
    {
        pageIds = tests
            .SelectMany(test => test.Steps)
            .Select(step => step.PageId)
            .Where(pageId => !string.IsNullOrWhiteSpace(pageId))
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);
    }

    var pages = pageIds is null
        ? snapshot.Pages
        : snapshot.Pages.Where(page => pageIds.Contains(page.Id)).ToList();
    return Results.Ok(await generation.GenerateAsync(
        snapshot.Project,
        pages,
        tests,
        snapshot.RootPath,
        cancellationToken));
});

api.MapPost("/tests/{testId}/runs", async (
    string testId,
    StartRunRequest request,
    RunCoordinator coordinator,
    CancellationToken cancellationToken) =>
{
    var run = await coordinator.StartAsync(testId, request.Browser, cancellationToken);
    return Results.Accepted($"/api/v1/runs/{run.Id}", run);
});

api.MapGet("/runs/{runId}", (string runId, RunCoordinator coordinator) =>
    coordinator.Get(runId) is { } run ? Results.Ok(run) : Results.NotFound());

api.MapPut("/runs/{runId}", async (
    string runId,
    TestRun run,
    IWorkspaceRepository repository,
    CancellationToken cancellationToken) =>
{
    if (!string.Equals(runId, run.Id, StringComparison.Ordinal))
    {
        return Results.BadRequest(new ProblemDetails { Title = "Run ID mismatch." });
    }

    await repository.SaveRunAsync(run, cancellationToken);
    return Results.Ok(run);
});

api.MapGet("/runs/{runId}/screenshots/{fileName}", (
    string runId,
    string fileName,
    IWorkspaceRepository repository) =>
    repository.GetScreenshotPath(runId, fileName) is { } path
        ? Results.File(path, "image/png")
        : Results.NotFound());

api.MapGet("/runs/{runId}/events", async (
    string runId,
    HttpContext context,
    RunCoordinator coordinator,
    CancellationToken cancellationToken) =>
{
    context.Response.Headers.CacheControl = "no-cache";
    context.Response.Headers.Connection = "keep-alive";
    context.Response.ContentType = "text/event-stream";
    await foreach (var step in coordinator.ReadEventsAsync(runId, cancellationToken))
    {
        await context.Response.WriteAsync(
            $"event: step\ndata: {JsonSerializer.Serialize(step, eventJsonOptions)}\n\n",
            cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);
    }

    if (coordinator.Get(runId) is { } completed)
    {
        await context.Response.WriteAsync(
            $"event: done\ndata: {JsonSerializer.Serialize(completed, eventJsonOptions)}\n\n",
            cancellationToken);
    }
});

api.MapPost("/runs/{runId}/cancel", (string runId, RunCoordinator coordinator) =>
    coordinator.Cancel(runId) ? Results.Accepted() : Results.NotFound());

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }));

await app.StartAsync();
var server = app.Services.GetRequiredService<IServer>();
var address = server.Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault()
    ?? throw new InvalidOperationException("Kestrel did not publish a listening address.");
Console.Out.WriteLine($"CLIQ_READY {JsonSerializer.Serialize(new { address })}");
Console.Out.Flush();
await app.WaitForShutdownAsync();

static bool FixedTimeEquals(string provided, string expected)
{
    var providedBytes = Encoding.UTF8.GetBytes(provided);
    var expectedBytes = Encoding.UTF8.GetBytes(expected);
    return providedBytes.Length == expectedBytes.Length
        && CryptographicOperations.FixedTimeEquals(providedBytes, expectedBytes);
}

public partial class Program;
