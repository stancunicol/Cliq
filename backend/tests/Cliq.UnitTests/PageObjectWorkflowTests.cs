using Cliq.Infrastructure.Generation;

namespace Cliq.Infrastructure.Generation.Tests;

[TestFixture]
public class PageObjectWorkflowTests
{
    [Test]
    public void DeriveClassName_DifferentDomains_SameLastSegment_ProduceDifferentNames()
    {
        var a = PageObjectWorkflow.DeriveClassName("https://site-a.com/login");
        var b = PageObjectWorkflow.DeriveClassName("https://site-b.com/login");

        Assert.That(a, Is.Not.EqualTo(b));
    }

    [Test]
    public void DeriveClassName_NoPathSegment_UsesHostOnly()
    {
        Assert.That(PageObjectWorkflow.DeriveClassName("https://example.com/"), Is.EqualTo("ExampleComPage"));
    }

    [TestCase("https://the-internet.herokuapp.com/login", "TheInternetHerokuappComLoginPage")]
    public void DeriveClassName_ProducesExpectedName(string url, string expected)
    {
        Assert.That(PageObjectWorkflow.DeriveClassName(url), Is.EqualTo(expected));
    }
}
