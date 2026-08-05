using Cliq.Domain;
using Cliq.Infrastructure;

namespace Cliq.UnitTests;

[TestFixture]
public sealed class WorkspaceCodeGenerationTests
{
    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "cliq-generation-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Test]
    public async Task GenerateAsync_CreatesPageObjectWorkflowAndAttributeFile()
    {
        var repository = new WorkspaceRepository();
        var snapshot = await repository.OpenAsync(_root, CancellationToken.None);
        var page = new PageDefinition(
            PageDefinition.CurrentSchemaVersion,
            "login",
            "Login",
            "https://example.com/login",
            [
                new PageElement(
                    "username",
                    "username",
                    "input",
                    StepAction.Fill,
                    new Locator(LocatorStrategy.TestId, "username")),
            ],
            DateTimeOffset.UtcNow);
        var test = new TestCase(
            TestCase.CurrentSchemaVersion,
            "login-flow",
            "Login flow",
            null,
            [],
            [
                new TestStep("navigate", StepAction.Navigate, "Open", true, Url: page.Url),
                new TestStep(
                    "fill",
                    StepAction.Fill,
                    "Fill username",
                    true,
                    page.Id,
                    "username",
                    Value: StepValue.Secret("username")),
            ],
            DateTimeOffset.UtcNow);
        var generator = new WorkspaceCodeGeneration(repository);

        var result = await generator.GenerateAsync(
            snapshot.Project,
            [page],
            [test],
            _root,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(
                File.ReadAllText(result.Artifacts.Single(item => item.Kind == "attribute").Path),
                Does.Contain("class CliqCustomMethodAttribute : Attribute"));
            Assert.That(
                File.ReadAllText(result.Artifacts.Single(item => item.Kind == "pageObject").Path),
                Does.Contain("public sealed class LoginPage"));
            Assert.That(
                File.ReadAllText(result.Artifacts.Single(item => item.Kind == "test").Path),
                Does.Contain("RequiredSecret(\"USERNAME\")"));
        });
    }

    [Test]
    public async Task GenerateAsync_PreservesMethodsMarkedAsCustomAcrossRegeneration()
    {
        var repository = new WorkspaceRepository();
        var snapshot = await repository.OpenAsync(_root, CancellationToken.None);
        var page = new PageDefinition(
            PageDefinition.CurrentSchemaVersion,
            "login",
            "Login",
            "https://example.com/login",
            [
                new PageElement(
                    "username",
                    "username",
                    "input",
                    StepAction.Fill,
                    new Locator(LocatorStrategy.TestId, "username")),
            ],
            DateTimeOffset.UtcNow);
        var generator = new WorkspaceCodeGeneration(repository);

        var first = await generator.GenerateAsync(snapshot.Project, [page], [], _root, CancellationToken.None);
        var pageObjectPath = first.Artifacts.Single(item => item.Kind == "pageObject").Path;
        var generated = await File.ReadAllTextAsync(pageObjectPath);
        const string handWrittenMethod = """
                [CliqCustomMethod]
                public void WaitForSpinnerToClear()
                {
                    _wait.Until(d => !d.FindElements(By.CssSelector(".spinner")).Any());
                }

            """;
        // Insert just before the class's own closing brace (the last one in the file).
        var classCloseIndex = generated.LastIndexOf('}');
        var withHandWrittenMethod = generated[..classCloseIndex] + handWrittenMethod + generated[classCloseIndex..];

        await File.WriteAllTextAsync(pageObjectPath, withHandWrittenMethod);

        var second = await generator.GenerateAsync(snapshot.Project, [page], [], _root, CancellationToken.None);
        var regenerated = await File.ReadAllTextAsync(
            second.Artifacts.Single(item => item.Kind == "pageObject").Path);

        Assert.Multiple(() =>
        {
            Assert.That(regenerated, Does.Contain("[CliqCustomMethod]"));
            Assert.That(regenerated, Does.Contain("public void WaitForSpinnerToClear()"));
            Assert.That(regenerated, Does.Contain("Username = new ElementWrapper<string>("));
            Assert.That(second.Warnings, Is.Empty);
        });
    }

    [Test]
    public async Task GenerateAsync_MarkerCountExceedsRecoveredMethods_BacksUpFileAndReturnsWarning()
    {
        var repository = new WorkspaceRepository();
        var snapshot = await repository.OpenAsync(_root, CancellationToken.None);
        var page = new PageDefinition(
            PageDefinition.CurrentSchemaVersion,
            "login",
            "Login",
            "https://example.com/login",
            [
                new PageElement(
                    "username",
                    "username",
                    "input",
                    StepAction.Fill,
                    new Locator(LocatorStrategy.TestId, "username")),
            ],
            DateTimeOffset.UtcNow);
        var generator = new WorkspaceCodeGeneration(repository);

        var first = await generator.GenerateAsync(snapshot.Project, [page], [], _root, CancellationToken.None);
        var pageObjectPath = first.Artifacts.Single(item => item.Kind == "pageObject").Path;
        var generated = await File.ReadAllTextAsync(pageObjectPath);

        // A stray mention of the marker text inside a comment is not a real attribute application -
        // CustomMethodExtractor.Extract correctly skips it, but the naive marker-count cross-check
        // does not, so this deliberately produces a mismatch to exercise the warning path.
        var withStrayMention = generated.Replace(
            "public sealed class LoginPage",
            "// TODO: remove [CliqCustomMethod] before merging\npublic sealed class LoginPage");
        await File.WriteAllTextAsync(pageObjectPath, withStrayMention);

        var second = await generator.GenerateAsync(snapshot.Project, [page], [], _root, CancellationToken.None);

        Assert.That(second.Warnings, Has.Count.EqualTo(1));
        var warning = second.Warnings[0];
        Assert.Multiple(() =>
        {
            Assert.That(warning.PageObjectName, Is.EqualTo("LoginPage"));
            Assert.That(warning.ExpectedCount, Is.EqualTo(1));
            Assert.That(warning.RecoveredCount, Is.Zero);
            Assert.That(File.Exists(warning.Path), Is.True);
            Assert.That(File.ReadAllText(warning.Path), Is.EqualTo(withStrayMention));
        });
    }

    [Test]
    public async Task GenerateAsync_RenamedPage_DeletesStaleFileWhenItHasNoCustomMethods()
    {
        var repository = new WorkspaceRepository();
        var snapshot = await repository.OpenAsync(_root, CancellationToken.None);
        var page = new PageDefinition(
            PageDefinition.CurrentSchemaVersion,
            "login",
            "Login",
            "https://example.com/login",
            [
                new PageElement(
                    "username",
                    "username",
                    "input",
                    StepAction.Fill,
                    new Locator(LocatorStrategy.TestId, "username")),
            ],
            DateTimeOffset.UtcNow);
        var generator = new WorkspaceCodeGeneration(repository);

        var first = await generator.GenerateAsync(snapshot.Project, [page], [], _root, CancellationToken.None);
        var oldPath = first.Artifacts.Single(item => item.Kind == "pageObject").Path;

        var renamed = page with { Name = "Sign In" };
        var second = await generator.GenerateAsync(snapshot.Project, [renamed], [], _root, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(second.Warnings, Is.Empty);
            Assert.That(File.Exists(oldPath), Is.False);
            Assert.That(
                File.Exists(second.Artifacts.Single(item => item.Kind == "pageObject").Path), Is.True);
        });
    }

    [Test]
    public async Task GenerateAsync_RenamedPage_KeepsStaleFileAndWarnsWhenItHasCustomMethods()
    {
        var repository = new WorkspaceRepository();
        var snapshot = await repository.OpenAsync(_root, CancellationToken.None);
        var page = new PageDefinition(
            PageDefinition.CurrentSchemaVersion,
            "login",
            "Login",
            "https://example.com/login",
            [
                new PageElement(
                    "username",
                    "username",
                    "input",
                    StepAction.Fill,
                    new Locator(LocatorStrategy.TestId, "username")),
            ],
            DateTimeOffset.UtcNow);
        var generator = new WorkspaceCodeGeneration(repository);

        var first = await generator.GenerateAsync(snapshot.Project, [page], [], _root, CancellationToken.None);
        var oldPath = first.Artifacts.Single(item => item.Kind == "pageObject").Path;
        var generated = await File.ReadAllTextAsync(oldPath);
        const string handWrittenMethod = """
                [CliqCustomMethod]
                public void WaitForSpinnerToClear()
                {
                    _wait.Until(d => !d.FindElements(By.CssSelector(".spinner")).Any());
                }

            """;
        var classCloseIndex = generated.LastIndexOf('}');
        var withHandWrittenMethod = generated[..classCloseIndex] + handWrittenMethod + generated[classCloseIndex..];
        await File.WriteAllTextAsync(oldPath, withHandWrittenMethod);

        var renamed = page with { Name = "Sign In" };
        var second = await generator.GenerateAsync(snapshot.Project, [renamed], [], _root, CancellationToken.None);

        Assert.That(second.Warnings, Has.Count.EqualTo(1));
        var warning = second.Warnings[0];
        Assert.Multiple(() =>
        {
            Assert.That(warning.PageObjectName, Is.EqualTo("LoginPage"));
            Assert.That(warning.RecoveredCount, Is.Zero);
            Assert.That(warning.ExpectedCount, Is.EqualTo(1));
            Assert.That(warning.Path, Is.EqualTo(oldPath));
            Assert.That(File.Exists(oldPath), Is.True, "the untouched original should be left in place, not deleted");
            Assert.That(File.ReadAllText(oldPath), Is.EqualTo(withHandWrittenMethod));
            Assert.That(
                File.Exists(second.Artifacts.Single(item => item.Kind == "pageObject").Path), Is.True);
        });
    }

    [Test]
    public async Task GenerateAsync_EscapesQuotedTestIdInVisibilityAssertion()
    {
        var repository = new WorkspaceRepository();
        var snapshot = await repository.OpenAsync(_root, CancellationToken.None);
        var page = new PageDefinition(
            PageDefinition.CurrentSchemaVersion,
            "quoted",
            "Quoted",
            "https://example.com",
            [
                new PageElement(
                    "save",
                    "save",
                    "button",
                    StepAction.AssertVisible,
                    new Locator(LocatorStrategy.TestId, "save\"draft")),
            ],
            DateTimeOffset.UtcNow);
        var test = new TestCase(
            TestCase.CurrentSchemaVersion,
            "visibility",
            "Visibility",
            null,
            [],
            [new TestStep("visible", StepAction.AssertVisible, "Visible", true, page.Id, "save")],
            DateTimeOffset.UtcNow);
        var generator = new WorkspaceCodeGeneration(repository);

        var result = await generator.GenerateAsync(
            snapshot.Project,
            [page],
            [test],
            _root,
            CancellationToken.None);
        var code = await File.ReadAllTextAsync(result.Artifacts.Single(item => item.Kind == "test").Path);

        Assert.That(code, Does.Contain("By.CssSelector(\"[data-testid=\\\"save\\\\\\\"draft\\\"]\")"));
    }
}
