using Cliq.Application;

namespace Cliq.Api;

internal static class GenerationEndpoints
{
    public static void MapGenerationEndpoints(this RouteGroupBuilder api)
    {
        api.MapPost("/generation", async (
            GenerationRequest request,
            IWorkspaceRepository repository,
            ICodeGeneration generation,
            CancellationToken cancellationToken) =>
        {
            var snapshot = await repository.GetSnapshotAsync(cancellationToken);
            var (pages, tests) = GenerationRequestPlanner.Plan(request, snapshot);
            return Results.Ok(await generation.GenerateAsync(
                snapshot.Project,
                pages,
                tests,
                snapshot.RootPath,
                cancellationToken,
                request.CustomMethodResolutions,
                request.CustomMethodDiscards));
        });

        api.MapGet("/pending-custom-methods", async (
            IWorkspaceRepository repository,
            CancellationToken cancellationToken) =>
            Results.Ok(await repository.ListPendingCustomMethodsAsync(cancellationToken)));

        api.MapDelete("/pending-custom-methods/{id}", async (
            string id,
            IWorkspaceRepository repository,
            CancellationToken cancellationToken) =>
        {
            await repository.DeletePendingCustomMethodAsync(id, cancellationToken);
            return Results.NoContent();
        });
    }
}
