using Cliq.Application;

namespace Cliq.Api;

internal static class SecretEndpoints
{
    public static void MapSecretEndpoints(this RouteGroupBuilder api)
    {
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
    }
}
