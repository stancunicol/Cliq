using Cliq.Infrastructure.Generation;

namespace Cliq.UnitTests;

[TestFixture]
public sealed class CustomMethodExtractorTests
{
    [Test]
    public void Extract_NoMarker_ReturnsNothing()
    {
        const string source = """
            public sealed class LoginPage
            {
                public void ClickSubmit()
                {
                    _wait.Until(d => d.FindElement(By.Id("submit"))).Click();
                }
            }
            """;

        Assert.That(CustomMethodExtractor.Extract(source), Is.Empty);
    }

    [Test]
    public void Extract_BlockBodiedMethod_CapturesAttributeThroughClosingBrace()
    {
        const string source = """
            public sealed class LoginPage
            {
                [CliqCustomMethod]
                public void WaitForSpinnerToClear()
                {
                    _wait.Until(d => !d.FindElements(By.CssSelector(".spinner")).Any());
                }
            }
            """;

        var blocks = CustomMethodExtractor.Extract(source);

        Assert.That(blocks, Has.Count.EqualTo(1));
        Assert.That(blocks[0], Does.StartWith("    [CliqCustomMethod]"));
        Assert.That(blocks[0], Does.EndWith("}"));
        Assert.That(blocks[0], Does.Contain("public void WaitForSpinnerToClear()"));
    }

    [Test]
    public void Extract_ExpressionBodiedMethod_EndsAtTheSemicolon()
    {
        const string source = """
            public sealed class LoginPage
            {
                [CliqCustomMethod]
                public bool IsSpinnerGone() => !_driver.FindElements(By.CssSelector(".spinner")).Any();

                public void ClickSubmit() { }
            }
            """;

        var blocks = CustomMethodExtractor.Extract(source);

        Assert.That(blocks, Has.Count.EqualTo(1));
        Assert.That(blocks[0], Does.Contain("IsSpinnerGone() => !_driver"));
        Assert.That(blocks[0], Does.Not.Contain("ClickSubmit"));
    }

    [Test]
    public void Extract_BracesInsideStringsAndComments_DoNotConfuseTheDepthCount()
    {
        const string source = """
            public sealed class LoginPage
            {
                [CliqCustomMethod]
                public void LogState()
                {
                    // a stray brace in a comment: {
                    var template = "{ not real json }";
                    var items = new List<int> { 1, 2, 3 };
                    Console.WriteLine($"state = {template}");
                }
            }
            """;

        var blocks = CustomMethodExtractor.Extract(source);

        Assert.That(blocks, Has.Count.EqualTo(1));
        Assert.That(blocks[0], Does.Contain("Console.WriteLine($\"state = {template}\");"));
        Assert.That(blocks[0].TrimEnd(), Does.EndWith("}"));
    }

    [Test]
    public void Extract_MultipleMarkedMethods_ReturnsEachSeparately()
    {
        const string source = """
            public sealed class LoginPage
            {
                [CliqCustomMethod]
                public void First()
                {
                    DoSomething();
                }

                public void Generated()
                {
                    Click();
                }

                [CliqCustomMethod]
                public void Second()
                {
                    DoSomethingElse();
                }
            }
            """;

        var blocks = CustomMethodExtractor.Extract(source);

        Assert.That(blocks, Has.Count.EqualTo(2));
        Assert.That(blocks[0], Does.Contain("First"));
        Assert.That(blocks[1], Does.Contain("Second"));
        Assert.That(blocks[0], Does.Not.Contain("Generated"));
        Assert.That(blocks[1], Does.Not.Contain("Generated"));
    }

    [Test]
    public void Extract_RoundTrip_ReExtractingAppendedBlockYieldsTheSameBlock()
    {
        const string customMethod = """
                [CliqCustomMethod]
                public void WaitForSpinnerToClear()
                {
                    _wait.Until(d => !d.FindElements(By.CssSelector(".spinner")).Any());
                }
            """;

        var regenerated = PageObjectCodeGenerator.Generate(
            "LoginPage", "Generated", [], 10_000, [customMethod]);
        var reExtracted = CustomMethodExtractor.Extract(regenerated);

        Assert.That(reExtracted, Has.Count.EqualTo(1));
        Assert.That(reExtracted[0], Is.EqualTo(customMethod));
    }
}
