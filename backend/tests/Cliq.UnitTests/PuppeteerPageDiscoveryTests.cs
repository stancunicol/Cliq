using System.Text.Json;
using System.Text.Json.Serialization;
using Cliq.Domain;
using Cliq.Infrastructure;

namespace Cliq.UnitTests;

[TestFixture]
public sealed class PuppeteerPageDiscoveryTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "cliq-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static PuppeteerPageDiscovery.RawElement Raw(string tag, string strategy, string locatorValue, string name) =>
        new()
        {
            Tag = tag,
            Kind = "fill",
            Strategy = strategy,
            LocatorValue = locatorValue,
            Name = name,
        };

    [Test]
    public void BuildElements_TwoElementsShareAnXPath_KeepsOnlyTheFirst()
    {
        var raw = new[]
        {
            Raw("input", "xpath", "//label[contains(.,'Username')]/following::input[1]", "username"),
            Raw("input", "xpath", "//label[contains(.,'Username')]/following::input[1]", "username-again"),
        };

        var elements = PuppeteerPageDiscovery.BuildElements(raw, "data-testid");

        Assert.Multiple(() =>
        {
            Assert.That(elements, Has.Count.EqualTo(1));
            Assert.That(elements[0].Name, Is.EqualTo("username"));
        });
    }

    [TestCase(null, "string")]
    [TestCase("text", "string")]
    [TestCase("email", "string")]
    [TestCase("number", "int")]
    [TestCase("range", "int")]
    [TestCase("date", "DateTime")]
    [TestCase("datetime-local", "DateTime")]
    public void BuildElements_Fill_InfersDataTypeFromScannedInputType(string? inputType, string expectedDataType)
    {
        var raw = new[]
        {
            new PuppeteerPageDiscovery.RawElement
            {
                Tag = "input",
                Kind = "fill",
                Strategy = "id",
                LocatorValue = "amount",
                Name = "amount",
                InputType = inputType,
            },
        };

        var elements = PuppeteerPageDiscovery.BuildElements(raw, "data-testid");

        Assert.That(elements[0].DataType, Is.EqualTo(expectedDataType));
    }

    [Test]
    public void BuildElements_NonFillKind_IgnoresInputTypeAndDefaultsToString()
    {
        var raw = new[]
        {
            new PuppeteerPageDiscovery.RawElement
            {
                Tag = "button",
                Kind = "click",
                Strategy = "id",
                LocatorValue = "submit",
                Name = "submit",
                InputType = "number",
            },
        };

        var elements = PuppeteerPageDiscovery.BuildElements(raw, "data-testid");

        Assert.That(elements[0].DataType, Is.EqualTo("string"));
    }

    [Test]
    public void BuildElements_DistinctXPaths_KeepsBoth()
    {
        var raw = new[]
        {
            Raw("input", "xpath", "//label[contains(.,'Username')]/following::input[1]", "username"),
            Raw("input", "xpath", "//label[contains(.,'Password')]/following::input[1]", "password"),
        };

        var elements = PuppeteerPageDiscovery.BuildElements(raw, "data-testid");

        Assert.That(elements, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task WriteLocatorDiffReportAsync_WritesJsonFileUnderCliqRunsDiffs()
    {
        var scannedAt = DateTimeOffset.Parse("2026-08-03T12:00:00Z");
        var newElements = new[]
        {
            new PageElement("fresh-1", "submit", "button", StepAction.Click, new Locator(LocatorStrategy.XPath, "//button[contains(.,'Submit')]")),
        };
        var missingElements = new[]
        {
            new PageElement("old-1", "cancel", "button", StepAction.Click, new Locator(LocatorStrategy.Css, "*:nth-child(2) > button:nth-child(1)"), NotFound: true),
        };

        var diff = await PuppeteerPageDiscovery.WriteLocatorDiffReportAsync(
            _root, "login-page", scannedAt, newElements, missingElements, CancellationToken.None);

        var directory = Path.Combine(_root, ".cliq", "runs", "diffs");
        var files = Directory.GetFiles(directory, "*.json");

        Assert.Multiple(() =>
        {
            Assert.That(files, Has.Length.EqualTo(1));
            Assert.That(diff.PageId, Is.EqualTo("login-page"));
            Assert.That(diff.New, Has.Count.EqualTo(1));
            Assert.That(diff.New[0].ElementId, Is.EqualTo("fresh-1"));
            Assert.That(diff.Missing, Has.Count.EqualTo(1));
            Assert.That(diff.Missing[0].ElementId, Is.EqualTo("old-1"));
        });

        var json = await File.ReadAllTextAsync(files[0]);
        var persisted = JsonSerializer.Deserialize<LocatorDiffReport>(json, JsonOptions);

        Assert.That(persisted, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(json, Does.Contain("\n"), "the report should be indented, not a single line");
            Assert.That(json, Does.Contain("\"xpath\""), "the locator strategy should be written as its name, not an enum ordinal");
            Assert.That(persisted!.PageId, Is.EqualTo(diff.PageId));
            Assert.That(persisted.New.Single().ElementId, Is.EqualTo(diff.New.Single().ElementId));
            Assert.That(persisted.New.Single().Locator.Strategy, Is.EqualTo(LocatorStrategy.XPath));
            Assert.That(persisted.Missing.Single().ElementId, Is.EqualTo(diff.Missing.Single().ElementId));
            Assert.That(persisted.Missing.Single().Locator.Strategy, Is.EqualTo(LocatorStrategy.Css));
        });
    }
}
