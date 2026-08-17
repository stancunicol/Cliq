using Cliq.Application;

namespace Cliq.Api;

internal static class WorkspaceEndpoints
{
    public static void MapWorkspaceEndpoints(this RouteGroupBuilder api)
    {
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
    }
}
