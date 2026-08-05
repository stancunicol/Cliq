using Cliq.Infrastructure;

namespace Cliq.UnitTests;

[TestFixture]
public sealed class SeleniumTestRunnerTests
{
    [Test]
    public void RadioOptionCssSelector_CombinesGroupNameAndValue()
    {
        var selector = SeleniumTestRunner.RadioOptionCssSelector("color", "red");

        Assert.That(selector, Is.EqualTo("input[name=\"color\"][value=\"red\"]"));
    }

    [Test]
    public void RadioOptionCssSelector_EscapesQuotesAndBackslashesInBothParts()
    {
        var selector = SeleniumTestRunner.RadioOptionCssSelector("gro\"up", "va\\lue");

        Assert.That(selector, Is.EqualTo("input[name=\"gro\\\"up\"][value=\"va\\\\lue\"]"));
    }
}
