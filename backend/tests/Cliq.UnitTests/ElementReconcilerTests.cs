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

        var (elements, report, newElements) = ElementReconciler.Reconcile(fresh, previous);

        Assert.Multiple(() =>
        {
            Assert.That(elements, Has.Count.EqualTo(1));
            Assert.That(elements[0].Id, Is.EqualTo("old-1"));
            Assert.That(elements[0].Name, Is.EqualTo("login_button_x"));
            Assert.That(elements[0].NotFound, Is.False);
            Assert.That(report, Is.EqualTo(new ScanReconciliationReport(1, 0, 0)));
            Assert.That(newElements, Is.Empty);
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

        var (elements, report, newElements) = ElementReconciler.Reconcile(fresh, previous);

        Assert.That(elements.Single().Id, Is.EqualTo("old-2b"));
        Assert.That(report.MatchedCount, Is.EqualTo(1));
        Assert.That(newElements, Is.Empty);
    }

    [Test]
    public void Reconcile_CssStrategyMatch_PreservesOldId()
    {
        var previous = new[] { El("old-2", "submit", "button", StepAction.Click, LocatorStrategy.Css, "*:nth-child(2) > button:nth-child(1)") };
        var fresh = new[] { El("fresh-1", "submit", "button", StepAction.Click, LocatorStrategy.Css, "*:nth-child(2) > button:nth-child(1)") };

        var (elements, report, newElements) = ElementReconciler.Reconcile(fresh, previous);

        Assert.That(elements.Single().Id, Is.EqualTo("old-2"));
        Assert.That(report.MatchedCount, Is.EqualTo(1));
        Assert.That(newElements, Is.Empty);
    }

    [Test]
    public void Reconcile_CssValueDrifted_ReportsNewAndMissingRatherThanGuessing()
    {
        // No fallback beyond the exact-value tiers: once the css value itself changes (DOM
        // shifted), the fresh element is unrelated as far as reconciliation is concerned, even
        // though a human would recognize it as "the same" button.
        var previous = new[]
        {
            El("old-4", "submit", "button", StepAction.Click, LocatorStrategy.Css, "*:nth-child(2) > button:nth-child(1)"),
        };
        var fresh = new[]
        {
            El("fresh-1", "submit", "button", StepAction.Click, LocatorStrategy.Css, "*:nth-child(3) > button:nth-child(2)"),
        };

        var (elements, report, newElements) = ElementReconciler.Reconcile(fresh, previous);

        Assert.Multiple(() =>
        {
            Assert.That(elements, Has.Count.EqualTo(2));
            Assert.That(elements, Has.Some.Matches<PageElement>(e => e.Id == "fresh-1" && !e.NotFound));
            Assert.That(elements, Has.Some.Matches<PageElement>(e => e.Id == "old-4" && e.NotFound));
            Assert.That(report, Is.EqualTo(new ScanReconciliationReport(0, 1, 1)));
            Assert.That(newElements.Single().Id, Is.EqualTo("fresh-1"));
        });
    }

    [Test]
    public void Reconcile_NoMatch_NewElementKeepsFreshId_OldElementMarkedMissing()
    {
        var previous = new[] { El("old-5", "legacy_field", "input", StepAction.Fill, LocatorStrategy.XPath, "legacy-field") };
        var fresh = new[] { El("fresh-2", "new_field", "select", StepAction.Select, LocatorStrategy.XPath, "new-field") };

        var (elements, report, newElements) = ElementReconciler.Reconcile(fresh, previous);

        Assert.Multiple(() =>
        {
            Assert.That(elements, Has.Count.EqualTo(2));
            Assert.That(elements, Has.Some.Matches<PageElement>(e => e.Id == "fresh-2" && !e.NotFound));
            Assert.That(elements, Has.Some.Matches<PageElement>(e => e.Id == "old-5" && e.NotFound));
            Assert.That(report, Is.EqualTo(new ScanReconciliationReport(0, 1, 1)));
            Assert.That(newElements.Single().Id, Is.EqualTo("fresh-2"));
        });
    }

    [Test]
    public void Reconcile_NoPreviousPage_EverythingReportedAsNew()
    {
        var fresh = new[]
        {
            El("fresh-1", "username", "input", StepAction.Fill, LocatorStrategy.XPath, "username"),
            El("fresh-2", "password", "input", StepAction.Fill, LocatorStrategy.XPath, "password"),
        };

        var (elements, report, newElements) = ElementReconciler.Reconcile(fresh, []);

        Assert.That(elements, Is.EqualTo(fresh));
        Assert.That(report, Is.EqualTo(new ScanReconciliationReport(0, 2, 0)));
        Assert.That(newElements, Is.EqualTo(fresh));
    }
}
