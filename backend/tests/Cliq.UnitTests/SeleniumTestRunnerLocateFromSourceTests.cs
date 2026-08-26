using System.Text.RegularExpressions;
using Cliq.Application;
using Cliq.Domain;
using Cliq.Infrastructure;

namespace Cliq.UnitTests;

[TestFixture]
public sealed class SeleniumTestRunnerLocateFromSourceTests
{
    private TempWorkspaceDirectory _workspace = null!;
    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _workspace = new TempWorkspaceDirectory();
        _root = _workspace.RootPath;
    }

    [TearDown]
    public void TearDown() => _workspace.Dispose();

    private static readonly Regex CommentLine = new(@"^\s*// Locator .*$", RegexOptions.Multiline);

    [Test]
    public async Task ResolveElementFromComment_MatchesEveryLocatorCommentTheGeneratorActuallyWrote()
    {
        using var repository = new WorkspaceRepository();
        var snapshot = await repository.OpenAsync(_root, CancellationToken.None);
        var page = new PageDefinition(
            PageDefinition.CurrentSchemaVersion,
            "login",
            "Login",
            "https://example.com/login",
            [
                new PageElement(
                    "username", "username", "input", StepAction.Fill, new Locator(LocatorStrategy.TestId, "username")),
                new PageElement(
                    "submit", "submit", "button", StepAction.Click, new Locator(LocatorStrategy.XPath, "//button[@type='submit']")),
                new PageElement(
                    "logo", "logo", "img", StepAction.AssertVisible, new Locator(LocatorStrategy.Css, "img.logo")),
            ],
            DateTimeOffset.UtcNow);
        var generator = new WorkspaceCodeGeneration(repository);

        var result = await generator.GenerateAsync(snapshot.Project, [page], [], _root, CancellationToken.None);

        var source = File.ReadAllText(result.Artifacts.Single(item => item.Kind == "pageObject").Path);
        var commentLines = CommentLine.Matches(source).Select(match => match.Value).ToArray();
        Assert.That(commentLines, Has.Length.EqualTo(3));

        var settings = await repository.GetSettingsAsync(CancellationToken.None);
        var expectedIds = new[] { "username", "submit", "logo" };
        var resolvedIds = commentLines
            .Select(line => SeleniumTestRunner.ResolveElementFromComment(page, line, settings.ElementIdentification.TestIdAttribute).Id)
            .ToArray();
        Assert.That(resolvedIds, Is.EquivalentTo(expectedIds));
    }

    [Test]
    public void ResolveElementFromComment_UnrecognizableText_Throws()
    {
        var page = SinglePage();

        Assert.That(
            () => SeleniumTestRunner.ResolveElementFromComment(page, "not a locator comment", "data-testid"),
            Throws.TypeOf<WorkspaceValidationException>());
    }

    [Test]
    public void ResolveElementFromComment_DynamicTemplateComment_ThrowsASpecificMessage()
    {
        var page = SinglePage();

        Assert.That(
            () => SeleniumTestRunner.ResolveElementFromComment(
                page, "// Locator (xpath, template): //li[{param}]", "data-testid"),
            Throws.TypeOf<WorkspaceValidationException>()
                .With.Message.Contains("parameter value"));
    }

    [Test]
    public void ResolveElementFromComment_NoElementMatchesTheValue_Throws()
    {
        var page = SinglePage();

        Assert.That(
            () => SeleniumTestRunner.ResolveElementFromComment(page, "// Locator (css): .nonexistent", "data-testid"),
            Throws.TypeOf<WorkspaceValidationException>());
    }

    [Test]
    public void ResolveElementFromComment_MatchesByNameWhenPresent()
    {
        var page = SinglePage();

        var element = SeleniumTestRunner.ResolveElementFromComment(
            page, "// Locator (css): Username = [data-testid=\"username\"]", "data-testid");

        Assert.That(element.Id, Is.EqualTo("username"));
    }

    [Test]
    public void ResolveElementFromComment_MatchesByAbsoluteXPath()
    {
        var page = new PageDefinition(
            PageDefinition.CurrentSchemaVersion,
            "login",
            "Login",
            "https://example.com/login",
            [
                new PageElement(
                    "username", "username", "input", StepAction.Fill,
                    new Locator(LocatorStrategy.XPath, "//input[@data-testid='username']"),
                    AbsoluteXpath: "/html[1]/body[1]/div[2]/input[1]"),
            ],
            DateTimeOffset.UtcNow);

        // Name deliberately doesn't match ("Renamed" != "Username") so only the absolute path can
        // resolve it - exercising the absolute-xpath fallback rather than the name shortcut.
        var element = SeleniumTestRunner.ResolveElementFromComment(
            page, "// Locator (xpath): Renamed = /html[1]/body[1]/div[2]/input[1]", "data-testid");

        Assert.That(element.Id, Is.EqualTo("username"));
    }

    private static PageDefinition SinglePage() => new(
        PageDefinition.CurrentSchemaVersion,
        "login",
        "Login",
        "https://example.com/login",
        [
            new PageElement(
                "username", "username", "input", StepAction.Fill, new Locator(LocatorStrategy.TestId, "username")),
        ],
        DateTimeOffset.UtcNow);
}
