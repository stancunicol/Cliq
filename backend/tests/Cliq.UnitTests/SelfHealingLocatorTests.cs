using Cliq.Domain;
using Cliq.Infrastructure;

namespace Cliq.UnitTests;

[TestFixture]
public sealed class SelfHealingLocatorTests
{
    private static SelfHealingLocator.Candidate Candidate(
        string locatorStrategy, string locatorValue, string? testId = null, string? id = null, string? name = null, string? placeholder = null, string? text = null) =>
        new()
        {
            TestId = testId,
            Id = id,
            Name = name,
            Placeholder = placeholder,
            Text = text,
            LocatorStrategy = locatorStrategy,
            LocatorValue = locatorValue,
        };

    [Test]
    public void SelectMatch_UniqueMatch_ReturnsHealedLocator()
    {
        var candidates = new[] { Candidate("id", "new-username-id", id: "username") };

        var result = SelfHealingLocator.SelectMatch(candidates, "username", "data-testid");

        Assert.That(result, Is.EqualTo(new Locator(LocatorStrategy.Id, "new-username-id")));
    }

    [Test]
    public void SelectMatch_NoCandidateMatches_ReturnsNull()
    {
        var candidates = new[] { Candidate("id", "password-field", id: "password") };

        var result = SelfHealingLocator.SelectMatch(candidates, "username", "data-testid");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void SelectMatch_AmbiguousMatch_ReturnsNullRatherThanGuess()
    {
        var candidates = new[]
        {
            Candidate("css", "div > button:nth-child(1)", text: "Submit"),
            Candidate("css", "div > button:nth-child(4)", text: "Submit"),
        };

        var result = SelfHealingLocator.SelectMatch(candidates, "submit", "data-testid");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void SelectMatch_TestIdFlavoredCss_ClassifiesAsTestIdStrategy()
    {
        var candidates = new[] { Candidate("css", "[data-testid=\"login-btn\"]", testId: "login-btn") };

        var result = SelfHealingLocator.SelectMatch(candidates, "login_btn", "data-testid");

        Assert.That(result, Is.EqualTo(new Locator(LocatorStrategy.TestId, "login-btn")));
    }

    [Test]
    public void SelectMatch_EmptyCandidateList_ReturnsNull()
    {
        var result = SelfHealingLocator.SelectMatch([], "username", "data-testid");

        Assert.That(result, Is.Null);
    }
}
