using Cliq.Application;
using Cliq.Domain;

namespace Cliq.Api;

internal static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/settings", async (
            IWorkspaceRepository repository,
            CancellationToken cancellationToken) =>
            Results.Ok(await repository.GetSettingsAsync(cancellationToken)));

        api.MapPut("/settings", async (
            CliqSettings settings,
            IWorkspaceRepository repository,
            CancellationToken cancellationToken) =>
            Results.Ok(await repository.SaveSettingsAsync(settings, cancellationToken)));
    }
}
