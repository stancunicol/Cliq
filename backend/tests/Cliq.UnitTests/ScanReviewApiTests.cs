using System.Text.Json;
using System.Text.Json.Serialization;
using Cliq.Api;
using Cliq.Application;
using Cliq.Domain;
using Microsoft.AspNetCore.Http;

namespace Cliq.UnitTests;

[TestFixture]
public sealed class ScanReviewApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    [Test]
    public void DecisionRequest_UsesCamelCaseEnumValues()
    {
        var request = new UpdateScanReviewDecisionsRequest(
            [new ScanReviewDecisionRequest("item-id", ScanReviewDecision.Accept)]);

        var json = JsonSerializer.Serialize(request, JsonOptions);

        Assert.That(
            json,
            Is.EqualTo("""{"decisions":[{"itemId":"item-id","decision":"accept"}]}"""));
    }

    [Test]
    public void Conflict_ProducesStructuredProblemDetailsWithReferences()
    {
        var references = new List<ScanReviewReference>
        {
            new("checkout", "click-submit"),
        };

        var problem = ApiErrors.Create(new ScanReviewConflictException(
            "scan_review_removal_referenced",
            "Removal is referenced.",
            references));

        Assert.Multiple(() =>
        {
            Assert.That(problem.Status, Is.EqualTo(StatusCodes.Status409Conflict));
            Assert.That(problem.Title, Is.EqualTo("Scan review conflict"));
            Assert.That(problem.Extensions["code"], Is.EqualTo("scan_review_removal_referenced"));
            Assert.That(problem.Extensions["references"], Is.EqualTo(references));
        });
    }
}
