using Cliq.Infrastructure.Generation;

namespace Cliq.UnitTests;

[TestFixture]
public sealed class CustomMethodSyntaxCheckerTests
{
    [Test]
    public void FindSyntaxErrors_CleanMethod_ReturnsEmpty()
    {
        const string method = """
            [CliqCustomMethod]
            public void WaitForSpinnerToClear()
            {
                _wait.Until(d => !d.FindElements(By.CssSelector(".spinner")).Any());
            }
            """;

        Assert.That(CustomMethodSyntaxChecker.FindSyntaxErrors(method), Is.Empty);
    }

    [Test]
    public void FindSyntaxErrors_MissingSemicolon_ReturnsAtLeastOneError()
    {
        const string method = """
            [CliqCustomMethod]
            public void Broken()
            {
                var x = 1
                DoSomething(x);
            }
            """;

        Assert.That(CustomMethodSyntaxChecker.FindSyntaxErrors(method), Is.Not.Empty);
    }

    [Test]
    public void FindSyntaxErrors_UnbalancedBraces_ReturnsAtLeastOneError()
    {
        const string method = """
            [CliqCustomMethod]
            public void Broken()
            {
                DoSomething();
            """;

        Assert.That(CustomMethodSyntaxChecker.FindSyntaxErrors(method), Is.Not.Empty);
    }
}
