using System.Text.Json;
using Cliq.Application;
using Cliq.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Cliq.Api;

internal static class RunEndpoints
{
    public static void MapRunEndpoints(this RouteGroupBuilder api, JsonSerializerOptions eventJsonOptions)
    {
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
    }
}
