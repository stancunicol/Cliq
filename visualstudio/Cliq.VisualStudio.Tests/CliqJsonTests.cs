using System.Text.Json;
using Cliq.Domain;
using Cliq.VisualStudio.Core.Api;
using NUnit.Framework;

namespace Cliq.VisualStudio.Tests;

[TestFixture]
public sealed class CliqJsonTests
{
    [Test]
    public void Options_SerializesXPathAsLowercase_NotCamelCase()
    {
        var locator = new Locator(LocatorStrategy.XPath, "//button");

        var json = JsonSerializer.Serialize(locator, CliqJson.Options);

        Assert.That(json, Does.Contain("\"strategy\":\"xpath\""));
        Assert.That(json, Does.Not.Contain("\"strategy\":\"xPath\""));
    }

    [TestCase(LocatorStrategy.TestId, "testId")]
    [TestCase(LocatorStrategy.Name, "name")]
    [TestCase(LocatorStrategy.Css, "css")]
    public void Options_SerializesOtherStrategiesAsCamelCase(LocatorStrategy strategy, string expected)
    {
        var json = JsonSerializer.Serialize(new Locator(strategy, "value"), CliqJson.Options);

        Assert.That(json, Does.Contain($"\"strategy\":\"{expected}\""));
    }

    [Test]
    public void Options_RoundTripsXPathLocator()
    {
        var locator = new Locator(LocatorStrategy.XPath, "//div");

        var json = JsonSerializer.Serialize(locator, CliqJson.Options);
        var parsed = JsonSerializer.Deserialize<Locator>(json, CliqJson.Options);

        Assert.Multiple(() =>
        {
            Assert.That(parsed!.Strategy, Is.EqualTo(LocatorStrategy.XPath));
            Assert.That(parsed.Value, Is.EqualTo("//div"));
        });
    }
}
