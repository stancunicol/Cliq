using Cliq.Application;
using Cliq.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Cliq.Api;

internal static class PageEndpoints
{
    public static void MapPageEndpoints(this RouteGroupBuilder api)
    {
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

        api.MapGet("/pages/{id}/generated-path", async (
            string id,
            IWorkspaceRepository repository,
            CancellationToken cancellationToken) =>
        {
            var page = await repository.GetPageAsync(id, cancellationToken);
            if (page is null)
            {
                return Results.NotFound();
            }

            var path = await repository.GetGeneratedPageObjectPathAsync(page, cancellationToken);
            return Results.Ok(new GeneratedPathResponse(path));
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
                saved,
                result.Reconciliation,
                result.LocatorDiff,
                result.DynamicElementSuggestions,
                result.ScreenshotPath));
        });

        api.MapPost("/pages/{id}/elements/{elementId}/locate", async (
            string id,
            string elementId,
            StartRunRequest request,
            IWorkspaceRepository repository,
            ITestRunner testRunner,
            CancellationToken cancellationToken) =>
        {
            var page = await repository.GetPageAsync(id, cancellationToken);
            if (page is null)
            {
                return Results.NotFound();
            }

            var snapshot = await repository.GetSnapshotAsync(cancellationToken);
            await testRunner.LocateElementAsync(
                page,
                elementId,
                request.Browser ?? snapshot.Project.DefaultBrowser,
                cancellationToken);
            return Results.NoContent();
        });

        api.MapPost("/pages/locate-from-source", async (
            LocateFromSourceRequest request,
            IWorkspaceRepository repository,
            ITestRunner testRunner,
            CancellationToken cancellationToken) =>
        {
            var snapshot = await repository.GetSnapshotAsync(cancellationToken);
            await testRunner.LocateElementFromSourceAsync(
                request.ClassName,
                request.LocatorComment,
                request.Browser ?? snapshot.Project.DefaultBrowser,
                cancellationToken);
            return Results.NoContent();
        });

        api.MapGet("/pages/discovery-screenshots/{fileName}", (
            string fileName,
            IWorkspaceRepository repository) =>
            repository.GetDiscoveryScreenshotPath(fileName) is { } path
                ? Results.File(path, "image/png")
                : Results.NotFound());
    }
}
