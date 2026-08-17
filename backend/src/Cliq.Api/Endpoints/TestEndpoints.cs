using Cliq.Application;
using Cliq.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Cliq.Api;

internal static class TestEndpoints
{
    public static void MapTestEndpoints(this RouteGroupBuilder api)
    {
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
    }
}
