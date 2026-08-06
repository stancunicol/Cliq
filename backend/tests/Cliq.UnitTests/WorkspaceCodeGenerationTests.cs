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
    public async Task GenerateAsync_ScannedRadioGroup_GeneratesARadioMethodNotABrokenSelectProperty()
    {
        // Reproduces a real scan of https://demoqa.com/radio-button: a radio group's Action is
        // StepAction.Select (the domain model has no dedicated Radio value - see ToDescriptor's
        // comment), distinguished only by its Name-strategy locator (the group's shared `name`
        // attribute). Before the fix, this generated (or, after the css/xpath-only ByExpression
        // restriction, threw trying to generate) an ordinary Select property instead of a Radio
        // method.
        var repository = new WorkspaceRepository();
        var snapshot = await repository.OpenAsync(_root, CancellationToken.None);
        var page = new PageDefinition(
            PageDefinition.CurrentSchemaVersion,
            "radio-button",
            "Radio Button",
            "https://demoqa.com/radio-button",
            [
                new PageElement(
                    "like-12",
                    "like",
                    "input",
                    StepAction.Select,
                    new Locator(LocatorStrategy.Name, "like")),
            ],
            DateTimeOffset.UtcNow);
        var generator = new WorkspaceCodeGeneration(repository);

        var result = await generator.GenerateAsync(snapshot.Project, [page], [], _root, CancellationToken.None);
        var code = await File.ReadAllTextAsync(result.Artifacts.Single(item => item.Kind == "pageObject").Path);

        Assert.Multiple(() =>
        {
            Assert.That(code, Does.Contain("public void SelectLike(string value)"));
            Assert.That(code, Does.Contain("input[name='like'][value='"));
            Assert.That(code, Does.Not.Contain("ElementWrapper<string> Like"));
        });
    }

    [Test]
    public async Task GenerateAsync_ScannedMultiSelect_GeneratesAMultiSelectPropertyAndArrayUploadCall()
    {
        // Same class of bug as the Radio case above: a scanned <select multiple> is also just
        // StepAction.Select (no dedicated MultiSelect value in the domain model either), and
        // PageElement.Multiple is the only place that distinguishes it after the scan.
        var repository = new WorkspaceRepository();
        var snapshot = await repository.OpenAsync(_root, CancellationToken.None);
        var page = new PageDefinition(
            PageDefinition.CurrentSchemaVersion,
            "form",
            "Form",
            "https://example.com/form",
            [
                new PageElement(
                    "colors-1",
                    "colors",
                    "select",
                    StepAction.Select,
                    new Locator(LocatorStrategy.Css, "#colors"),
                    Multiple: true),
            ],
            DateTimeOffset.UtcNow);
        var test = new TestCase(
            TestCase.CurrentSchemaVersion,
            "pick-colors",
            "Pick colors",
            null,
            [],
            [
                new TestStep(
                    "select-colors",
                    StepAction.Select,
                    "Select colors",
                    true,
                    page.Id,
                    "colors-1",
                    Values: [StepValue.Literal("red"), StepValue.Literal("blue")]),
            ],
            DateTimeOffset.UtcNow);
        var generator = new WorkspaceCodeGeneration(repository);

        var result = await generator.GenerateAsync(snapshot.Project, [page], [test], _root, CancellationToken.None);
        var pageObjectCode = await File.ReadAllTextAsync(
            result.Artifacts.Single(item => item.Kind == "pageObject").Path);
        var testCode = await File.ReadAllTextAsync(result.Artifacts.Single(item => item.Kind == "test").Path);

        Assert.Multiple(() =>
        {
            Assert.That(pageObjectCode, Does.Contain("public ElementWrapper<string[]> Colors { get; }"));
            Assert.That(pageObjectCode, Does.Contain("select.DeselectAll();"));
            Assert.That(testCode, Does.Contain(".Colors.Upload(new[] { \"red\", \"blue\" });"));
        });
    }

    [Test]
    public async Task GenerateAsync_ScannedCheckbox_GeneratesASetMethodAndBoolCallNotAClickCall()
    {
        // Same class of bug once more: a scanned checkbox's Action is StepAction.Click (no
        // dedicated Check value in the domain model either), and PageElement.Checkbox is the
        // only place that distinguishes it after the scan.
        var repository = new WorkspaceRepository();
        var snapshot = await repository.OpenAsync(_root, CancellationToken.None);
        var page = new PageDefinition(
            PageDefinition.CurrentSchemaVersion,
            "form",
            "Form",
            "https://example.com/form",
            [
                new PageElement(
                    "agree-1",
                    "agree",
                    "input",
                    StepAction.Click,
                    new Locator(LocatorStrategy.Css, "#agree"),
                    Checkbox: true),
            ],
            DateTimeOffset.UtcNow);
        var test = new TestCase(
            TestCase.CurrentSchemaVersion,
            "agree-flow",
            "Agree flow",
            null,
            [],
            [
                new TestStep(
                    "check-agree",
                    StepAction.Click,
                    "Check agree",
                    true,
                    page.Id,
                    "agree-1",
                    Value: StepValue.Literal("true")),
            ],
            DateTimeOffset.UtcNow);
        var generator = new WorkspaceCodeGeneration(repository);

        var result = await generator.GenerateAsync(snapshot.Project, [page], [test], _root, CancellationToken.None);
        var pageObjectCode = await File.ReadAllTextAsync(
            result.Artifacts.Single(item => item.Kind == "pageObject").Path);
        var testCode = await File.ReadAllTextAsync(result.Artifacts.Single(item => item.Kind == "test").Path);

        Assert.Multiple(() =>
        {
            Assert.That(pageObjectCode, Does.Contain("public void SetAgree(bool isChecked)"));
            Assert.That(testCode, Does.Contain(".SetAgree(true);"));
            Assert.That(testCode, Does.Not.Contain(".ClickAgree();"));
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

    [Test]
    public async Task GenerateAsync_FillStep_CallsUploadOnTheElementWrapperProperty()
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
                new TestStep(
                    "fill",
                    StepAction.Fill,
                    "Fill username",
                    true,
                    page.Id,
                    "username",
                    Value: StepValue.Literal("alice")),
            ],
            DateTimeOffset.UtcNow);
        var generator = new WorkspaceCodeGeneration(repository);

        var result = await generator.GenerateAsync(
            snapshot.Project,
            [page],
            [test],
            _root,
            CancellationToken.None);
        var code = await File.ReadAllTextAsync(result.Artifacts.Single(item => item.Kind == "test").Path);

        Assert.Multiple(() =>
        {
            Assert.That(code, Does.Contain(".Username.Upload("));
            Assert.That(code, Does.Not.Contain(".FillUsername("));
        });
    }

    [Test]
    public async Task GenerateAsync_DynamicClickStep_GeneratesFactoryMethodAndParameterizedCall()
    {
        var repository = new WorkspaceRepository();
        var snapshot = await repository.OpenAsync(_root, CancellationToken.None);
        var dynamicElement = new DynamicElement(
            "row-button",
            "row button",
            StepAction.Click,
            LocatorStrategy.XPath,
            "//table/tbody/tr[{param}]/td[1]/button");
        var page = new PageDefinition(
            PageDefinition.CurrentSchemaVersion,
            "orders",
            "Orders",
            "https://example.com/orders",
            [],
            DateTimeOffset.UtcNow,
            [dynamicElement]);
        var test = new TestCase(
            TestCase.CurrentSchemaVersion,
            "orders-flow",
            "Orders flow",
            null,
            [],
            [
                new TestStep(
                    "click-row",
                    StepAction.Click,
                    "Click row 3",
                    true,
                    page.Id,
                    DynamicElementId: dynamicElement.Id,
                    DynamicParameterValue: "3"),
            ],
            DateTimeOffset.UtcNow);
        var generator = new WorkspaceCodeGeneration(repository);

        var result = await generator.GenerateAsync(
            snapshot.Project,
            [page],
            [test],
            _root,
            CancellationToken.None);
        var pageObjectCode = await File.ReadAllTextAsync(
            result.Artifacts.Single(item => item.Kind == "pageObject").Path);
        var testCode = await File.ReadAllTextAsync(result.Artifacts.Single(item => item.Kind == "test").Path);

        Assert.Multiple(() =>
        {
            Assert.That(pageObjectCode, Does.Contain("public void ClickRowButton(int param)"));
            Assert.That(testCode, Does.Contain(".ClickRowButton(3);"));
        });
    }
}
