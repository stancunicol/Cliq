using Cliq.Domain;
using Cliq.Infrastructure;

namespace Cliq.UnitTests;

[TestFixture]
public sealed class ElementReconcilerTests
{
    private static PageElement El(
        string id, string name, string tag, StepAction action, LocatorStrategy strategy, string value, bool notFound = false) =>
        new(id, name, tag, action, new Locator(strategy, value), NotFound: notFound);

    [Test]
    public void Reconcile_TestIdMatch_PreservesIdAndClearsNotFound()
    {
        var previous = new[] { El("old-1", "login_button", "button", StepAction.Click, LocatorStrategy.TestId, "login-btn", notFound: true) };
        var fresh = new[] { El("fresh-9", "login_button_x", "button", StepAction.Click, LocatorStrategy.TestId, "login-btn") };

        var (elements, report) = ElementReconciler.Reconcile(fresh, previous);

        Assert.Multiple(() =>
        {
            Assert.That(elements, Has.Count.EqualTo(1));
            Assert.That(elements[0].Id, Is.EqualTo("old-1"));
            Assert.That(elements[0].Name, Is.EqualTo("login_button_x"));
            Assert.That(elements[0].NotFound, Is.False);
            Assert.That(report, Is.EqualTo(new ScanReconciliationReport(1, 0, 0)));
        });
    }

    [Test]
    public void Reconcile_XPathStrategyMatch_PreservesOldId()
    {
        var previous = new[]
        {
            El("old-2b", "username", "input", StepAction.Fill, LocatorStrategy.XPath, "//label[contains(.,'Username')]/following::input[1]"),
        };
        var fresh = new[]
        {
            El("fresh-1b", "username", "input", StepAction.Fill, LocatorStrategy.XPath, "//label[contains(.,'Username')]/following::input[1]"),
        };

        var (elements, report) = ElementReconciler.Reconcile(fresh, previous);

        Assert.That(elements.Single().Id, Is.EqualTo("old-2b"));
        Assert.That(report.MatchedCount, Is.EqualTo(1));
    }

    [Test]
    public void Reconcile_IdStrategyMatch_PreservesOldId()
    {
        var previous = new[] { El("old-2", "username", "input", StepAction.Fill, LocatorStrategy.Id, "username") };
        var fresh = new[] { El("fresh-1", "username", "input", StepAction.Fill, LocatorStrategy.Id, "username") };

        var (elements, report) = ElementReconciler.Reconcile(fresh, previous);

        Assert.That(elements.Single().Id, Is.EqualTo("old-2"));
        Assert.That(report.MatchedCount, Is.EqualTo(1));
    }

    [Test]
    public void Reconcile_NameStrategyMatch_PreservesOldId()
    {
        var previous = new[] { El("old-3", "country", "select", StepAction.Select, LocatorStrategy.Name, "country") };
        var fresh = new[] { El("fresh-1", "country", "select", StepAction.Select, LocatorStrategy.Name, "country") };

        var (elements, report) = ElementReconciler.Reconcile(fresh, previous);

        Assert.That(elements.Single().Id, Is.EqualTo("old-3"));
        Assert.That(report.MatchedCount, Is.EqualTo(1));
    }

    [Test]
    public void Reconcile_SanitizedNameFallback_MatchesDespiteLocatorDrift()
    {
        // Structural CSS locator shifted (DOM changed elsewhere), but the sanitized name -
        // derived from the same placeholder/text either scan finds - still lines up.
        var previous = new[]
        {
            El("old-4", "submit", "button", StepAction.Click, LocatorStrategy.Css, "div:nth-child(2) > button:nth-child(1)"),
        };
        var fresh = new[]
        {
            El("fresh-1", "submit", "button", StepAction.Click, LocatorStrategy.Css, "div:nth-child(3) > button:nth-child(2)"),
        };

        var (elements, report) = ElementReconciler.Reconcile(fresh, previous);

        Assert.That(elements.Single().Id, Is.EqualTo("old-4"));
        Assert.That(elements.Single().Locator.Value, Is.EqualTo("div:nth-child(3) > button:nth-child(2)"));
        Assert.That(report.MatchedCount, Is.EqualTo(1));
    }

    [Test]
    public void Reconcile_NoMatch_NewElementKeepsFreshId_OldElementMarkedMissing()
    {
        var previous = new[] { El("old-5", "legacy_field", "input", StepAction.Fill, LocatorStrategy.Id, "legacy-field") };
        var fresh = new[] { El("fresh-2", "new_field", "select", StepAction.Select, LocatorStrategy.Id, "new-field") };

        var (elements, report) = ElementReconciler.Reconcile(fresh, previous);

        Assert.Multiple(() =>
        {
            Assert.That(elements, Has.Count.EqualTo(2));
            Assert.That(elements, Has.Some.Matches<PageElement>(e => e.Id == "fresh-2" && !e.NotFound));
            Assert.That(elements, Has.Some.Matches<PageElement>(e => e.Id == "old-5" && e.NotFound));
            Assert.That(report, Is.EqualTo(new ScanReconciliationReport(0, 1, 1)));
        });
    }

    [Test]
    public void Reconcile_PositionFallback_PairsIndistinguishableElementsInOrder()
    {
        var previous = new[]
        {
            El("old-a", "link_x", "a", StepAction.Click, LocatorStrategy.Css, "old-selector-1"),
            El("old-b", "link_y", "a", StepAction.Click, LocatorStrategy.Css, "old-selector-2"),
        };
        var fresh = new[]
        {
            El("fresh-a", "link_p", "a", StepAction.Click, LocatorStrategy.Css, "new-selector-1"),
            El("fresh-b", "link_q", "a", StepAction.Click, LocatorStrategy.Css, "new-selector-2"),
        };

        var (elements, report) = ElementReconciler.Reconcile(fresh, previous);

        Assert.Multiple(() =>
        {
            Assert.That(elements[0].Id, Is.EqualTo("old-a"));
            Assert.That(elements[0].Name, Is.EqualTo("link_p"));
            Assert.That(elements[1].Id, Is.EqualTo("old-b"));
            Assert.That(elements[1].Name, Is.EqualTo("link_q"));
            Assert.That(report, Is.EqualTo(new ScanReconciliationReport(2, 0, 0)));
        });
    }

    [Test]
    public void Reconcile_NoPreviousPage_EverythingReportedAsNew()
    {
        var fresh = new[]
        {
            El("fresh-1", "username", "input", StepAction.Fill, LocatorStrategy.Id, "username"),
            El("fresh-2", "password", "input", StepAction.Fill, LocatorStrategy.Id, "password"),
        };

        var (elements, report) = ElementReconciler.Reconcile(fresh, []);

        Assert.That(elements, Is.EqualTo(fresh));
        Assert.That(report, Is.EqualTo(new ScanReconciliationReport(0, 2, 0)));
    }
}
