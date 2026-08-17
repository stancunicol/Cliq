using Cliq.Application;
using Cliq.Domain;

namespace Cliq.Api;

internal static class ScanReviewEndpoints
{
    public static void MapScanReviewEndpoints(this RouteGroupBuilder api)
    {
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
    }
}
