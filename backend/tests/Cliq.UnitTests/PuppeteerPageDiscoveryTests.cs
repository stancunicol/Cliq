using Cliq.Infrastructure;

namespace Cliq.UnitTests;

[TestFixture]
public sealed class PuppeteerPageDiscoveryTests
{
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
}
