using Cliq.Application;
using Cliq.Domain;

namespace Cliq.Api;

internal static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/project", async (
            IWorkspaceRepository repository,
            CancellationToken cancellationToken) =>
            Results.Ok((await repository.GetSnapshotAsync(cancellationToken)).Project));

        api.MapPut("/project", async (
            ProjectManifest project,
            IWorkspaceRepository repository,
            CancellationToken cancellationToken) =>
            Results.Ok(await repository.SaveProjectAsync(project, cancellationToken)));
    }
}
