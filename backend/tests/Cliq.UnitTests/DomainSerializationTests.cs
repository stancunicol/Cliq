using System.Text.Json;
using Cliq.Domain;

namespace Cliq.UnitTests;

[TestFixture]
public sealed class DomainSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = TestJson.Options;

    [Test]
    public void ProjectManifest_PreservesWorkspaceWireShape()
    {
        var manifest = new ProjectManifest(
            1,
            "sample",
            "Sample",
            "https://example.com",
            BrowserKind.Edge,
            new OutputSettings(),
            DateTimeOffset.Parse("2026-07-29T10:15:30+00:00"));

        var json = JsonSerializer.Serialize(manifest, JsonOptions);

        Assert.That(
            json,
            Is.EqualTo(
                """{"schemaVersion":1,"id":"sample","name":"Sample","baseUrl":"https://example.com","defaultBrowser":"edge","output":{"pageObjectsDirectory":"Generated/PageObjects","testsDirectory":"Generated/Tests","pageObjectsNamespace":"Generated.PageObjects","testsNamespace":"Generated.Tests"},"updatedAt":"2026-07-29T10:15:30+00:00"}"""));
    }

    [Test]
    public void TestStep_PreservesOptionalFieldsAndEnumWireShape()
    {
        var step = new TestStep(
            "fill-email",
            StepAction.Fill,
            "Fill email",
            true,
            "login",
            "email",
            Value: StepValue.Secret("user-email"),
            TimeoutMs: 5_000);

        var json = JsonSerializer.Serialize(step, JsonOptions);

        Assert.That(
            json,
            Is.EqualTo(
                """{"id":"fill-email","action":"fill","label":"Fill email","enabled":true,"pageId":"login","elementId":"email","url":null,"value":{"kind":"secret","value":"user-email"},"timeoutMs":5000}"""));
    }
}
