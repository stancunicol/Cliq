using Cliq.Domain;
using Cliq.Infrastructure;

namespace Cliq.UnitTests;

[TestFixture]
public sealed class WorkspaceCodeGenerationTests
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

    [Test]
    public async Task GenerateAsync_CreatesPageObjectWorkflowAndAttributeFile()
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
        using var repository = new WorkspaceRepository();
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
        using var repository = new WorkspaceRepository();
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
        using var repository = new WorkspaceRepository();
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
    public async Task GenerateAsync_ExistingCustomMethod_BlocksRegenerationUntilExplicitlyResolvedEvenWhenPerfectlyClean()
    {
        // "Always ask" policy: a page with at least one [CliqCustomMethod] needs an explicit
        // per-method decision on every regeneration, even when every one of them would recover
        // perfectly cleanly on its own - there is no silent auto-carry-over anymore. No Warnings
        // entry fires in this case though (that's reserved for genuine recovery failures, aimed at
        // the Electron frontend) - a perfectly clean method needing a decision isn't a "problem".
        using var repository = new WorkspaceRepository();
        var (snapshot, generator, page) = await OpenLoginPageAsync(repository);

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
        var withHandWrittenMethod = InsertBeforeClassClose(generated, handWrittenMethod);
        await File.WriteAllTextAsync(pageObjectPath, withHandWrittenMethod);

        var detection = await generator.GenerateAsync(snapshot.Project, [page], [], _root, CancellationToken.None);
        var afterDetection = await File.ReadAllTextAsync(pageObjectPath);

        Assert.Multiple(() =>
        {
            Assert.That(detection.Warnings, Is.Empty, "nothing actually failed to recover - no Electron-facing warning expected");
            Assert.That(detection.PendingMerges, Has.Count.EqualTo(1));
            Assert.That(detection.PendingMerges[0].Candidates.Single().HasSyntaxError, Is.False);
            Assert.That(afterDetection, Is.EqualTo(withHandWrittenMethod), "unresolved, so the file must stay untouched");
            Assert.That(detection.Artifacts.Any(artifact => artifact.Kind == "pageObject"), Is.False);
        });

        var keptCandidate = detection.PendingMerges[0].Candidates.Single();
        var resolutions = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [page.Id] = [keptCandidate.SourceText],
        };
        var resolved = await generator.GenerateAsync(
            snapshot.Project, [page], [], _root, CancellationToken.None, resolutions);
        var regenerated = await File.ReadAllTextAsync(
            resolved.Artifacts.Single(item => item.Kind == "pageObject").Path);

        Assert.Multiple(() =>
        {
            Assert.That(regenerated, Does.Contain("[CliqCustomMethod]"));
            Assert.That(regenerated, Does.Contain("public void WaitForSpinnerToClear()"));
            Assert.That(regenerated, Does.Contain("Username = new ElementWrapper<string>("));
            Assert.That(resolved.Warnings, Is.Empty);
        });
    }

    [Test]
    public async Task GenerateAsync_MarkerCountExceedsRecoveredMethods_LeavesFileUntouchedAndReturnsWarning()
    {
        using var repository = new WorkspaceRepository();
        var (snapshot, generator, page) = await OpenLoginPageAsync(repository);

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
            // warning.Path points at the live, untouched file itself - an unresolved conflict is
            // never written over, so there's nothing to back up.
            Assert.That(File.Exists(warning.Path), Is.True);
            Assert.That(File.ReadAllText(warning.Path), Is.EqualTo(withStrayMention));
            Assert.That(second.Artifacts.Any(artifact => artifact.Kind == "pageObject"), Is.False);
            // The stray marker is inside a comment, so there's genuinely nothing extractable behind
            // it - the merge UI has nothing real to offer, unlike the malformed-method case below.
            Assert.That(second.PendingMerges, Is.Empty);
        });
    }

    [Test]
    public async Task GenerateAsync_CustomMethodMissingItsOwnClosingBrace_IsNotSilentlyRepairedByBorrowingTheClassesBrace()
    {
        // Regression test: deleting a custom method's own closing brace, when that method is the
        // last member before the class's, can make the naive depth-walk in CustomMethodExtractor
        // "succeed" by matching the CLASS's closing brace instead - the extracted span then reads
        // as a complete, valid-looking method on its own, and the class's brace effectively gets
        // "borrowed" to close it. Silently re-emitting that instead of flagging a mismatch would
        // make the corruption invisible instead of raising a warning + merge conflict.
        using var repository = new WorkspaceRepository();
        var (snapshot, generator, page) = await OpenLoginPageAsync(repository);

        var first = await generator.GenerateAsync(snapshot.Project, [page], [], _root, CancellationToken.None);
        var pageObjectPath = first.Artifacts.Single(item => item.Kind == "pageObject").Path;
        var generated = await File.ReadAllTextAsync(pageObjectPath);
        const string methodMissingItsOwnClosingBrace = """

                [CliqCustomMethod]
                public void WaitForSpinnerToClear()
                {
                    _wait.Until(d => !d.FindElements(By.CssSelector(".spinner")).Any());

            """;
        // Insert right before the class's own closing brace, WITHOUT stripping that brace - it's
        // what the malformed method's depth-walk ends up (wrongly) matching against.
        var corrupted = InsertBeforeClassClose(generated, methodMissingItsOwnClosingBrace);
        await File.WriteAllTextAsync(pageObjectPath, corrupted);

        var second = await generator.GenerateAsync(snapshot.Project, [page], [], _root, CancellationToken.None);
        var afterDetection = await File.ReadAllTextAsync(pageObjectPath);

        Assert.Multiple(() =>
        {
            Assert.That(second.Warnings, Has.Count.EqualTo(1), "the corrupted file should be caught, not silently trusted");
            Assert.That(second.Warnings[0].RecoveredCount, Is.Zero);
            Assert.That(afterDetection, Is.EqualTo(corrupted), "an unresolved conflict must leave the file untouched");
            Assert.That(second.Artifacts.Any(artifact => artifact.Kind == "pageObject"), Is.False);
            Assert.That(second.PendingMerges, Has.Count.EqualTo(1));
            Assert.That(second.PendingMerges[0].Candidates[0].SourceText, Does.Contain("WaitForSpinnerToClear"));
            // The borrowed brace makes this span parse as a syntactically valid method on its own,
            // so only the file-wide IsBalanced check (not CustomMethodSyntaxChecker) can catch it -
            // without it, the merge UI would default this candidate to Carry Over instead of Defer.
            Assert.That(
                second.PendingMerges[0].Candidates[0].HasSyntaxError,
                Is.True,
                "a candidate recovered from an unbalanced file must never be trusted as clean, even if it parses in isolation");
        });
    }

    [Test]
    public async Task GenerateAsync_MalformedCustomMethod_ReturnsPendingMergeWithSyntaxErrorFlagged()
    {
        using var repository = new WorkspaceRepository();
        var (snapshot, generator, page) = await OpenLoginPageAsync(repository);

        var first = await generator.GenerateAsync(snapshot.Project, [page], [], _root, CancellationToken.None);
        var pageObjectPath = first.Artifacts.Single(item => item.Kind == "pageObject").Path;
        var generated = await File.ReadAllTextAsync(pageObjectPath);
        // An unclosed custom method: CustomMethodExtractor.Extract recovers nothing for it (hence
        // the legacy mismatch/backup path still fires), but ExtractAll can still hand the merge UI
        // the raw text so the user isn't left with nothing to look at.
        const string malformedMethod = """

                [CliqCustomMethod]
                public void Broken()
                {
                    DoSomething();
            """;
        var withMalformedMethod = InsertBeforeClassCloseWithoutClosingBrace(generated, malformedMethod);
        await File.WriteAllTextAsync(pageObjectPath, withMalformedMethod);

        var second = await generator.GenerateAsync(snapshot.Project, [page], [], _root, CancellationToken.None);

        var pending = second.PendingMerges.Single();
        Assert.Multiple(() =>
        {
            Assert.That(second.Warnings, Has.Count.EqualTo(1));
            Assert.That(pending.PageId, Is.EqualTo(page.Id));
            Assert.That(pending.PageObjectName, Is.EqualTo("LoginPage"));
            Assert.That(pending.Candidates, Has.Count.EqualTo(1));
            Assert.That(pending.Candidates[0].HasSyntaxError, Is.True);
            Assert.That(pending.Candidates[0].SyntaxErrors, Is.Not.Empty);
            Assert.That(pending.Candidates[0].SourceText, Does.Contain("Broken"));
        });
    }

    [Test]
    public async Task GenerateAsync_RenamedPage_DeletesStaleFileWhenItHasNoCustomMethods()
    {
        using var repository = new WorkspaceRepository();
        var (snapshot, generator, page) = await OpenLoginPageAsync(repository);

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
        using var repository = new WorkspaceRepository();
        var (snapshot, generator, page) = await OpenLoginPageAsync(repository);

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
        var withHandWrittenMethod = InsertBeforeClassClose(generated, handWrittenMethod);
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

        var pending = second.PendingMerges.Single();
        Assert.Multiple(() =>
        {
            Assert.That(pending.PageId, Is.EqualTo(page.Id));
            Assert.That(pending.PageObjectName, Is.EqualTo("LoginPage"));
            Assert.That(pending.Candidates, Has.Count.EqualTo(1));
            Assert.That(pending.Candidates[0].SourceText, Does.Contain("WaitForSpinnerToClear"));
            Assert.That(pending.Candidates[0].HasSyntaxError, Is.False);
        });
    }

    [Test]
    public async Task GenerateAsync_ResolvingRenameConflict_CarriesOverChosenMethodAndDefersTheRest()
    {
        using var repository = new WorkspaceRepository();
        var (snapshot, generator, page) = await OpenLoginPageAsync(repository);

        var first = await generator.GenerateAsync(snapshot.Project, [page], [], _root, CancellationToken.None);
        var oldPath = first.Artifacts.Single(item => item.Kind == "pageObject").Path;
        var generated = await File.ReadAllTextAsync(oldPath);
        const string keptMethod = """

                [CliqCustomMethod]
                public void WaitForSpinnerToClear()
                {
                    _wait.Until(d => !d.FindElements(By.CssSelector(".spinner")).Any());
                }
            """;
        const string deferredMethod = """

                [CliqCustomMethod]
                public void OldHelper()
                {
                    DoSomethingElse();
                }
            """;
        await File.WriteAllTextAsync(oldPath, InsertBeforeClassClose(generated, keptMethod + deferredMethod));

        var renamed = page with { Name = "Sign In" };
        var conflict = await generator.GenerateAsync(snapshot.Project, [renamed], [], _root, CancellationToken.None);
        var pending = conflict.PendingMerges.Single();
        Assert.That(pending.Candidates, Has.Count.EqualTo(2));
        var keptCandidate = pending.Candidates.Single(candidate => candidate.SourceText.Contains("WaitForSpinnerToClear"));

        var resolutions = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [page.Id] = [keptCandidate.SourceText],
        };
        var resolved = await generator.GenerateAsync(
            snapshot.Project, [renamed], [], _root, CancellationToken.None, resolutions);
        var newPath = resolved.Artifacts.Single(item => item.Kind == "pageObject").Path;
        var newSource = await File.ReadAllTextAsync(newPath);
        var waitingList = await repository.ListPendingCustomMethodsAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(oldPath), Is.False, "resolved rename should delete the stale file");
            Assert.That(newSource, Does.Contain("WaitForSpinnerToClear"));
            Assert.That(newSource, Does.Not.Contain("OldHelper"));
            Assert.That(resolved.PendingMerges, Is.Empty);
            Assert.That(waitingList, Has.Count.EqualTo(1));
            Assert.That(waitingList[0].PageId, Is.EqualTo(page.Id));
            Assert.That(waitingList[0].SourceText, Does.Contain("OldHelper"));
        });
    }

    [Test]
    public async Task GenerateAsync_EscapesQuotedTestIdInVisibilityAssertion()
    {
        using var repository = new WorkspaceRepository();
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
        using var repository = new WorkspaceRepository();
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
        using var repository = new WorkspaceRepository();
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

    [Test]
    public async Task GenerateAsync_PartialResolutionOnAPageWithAnUnmentionedExistingMethod_DefersItRatherThanSilentlyKeepingIt()
    {
        // "Always ask" policy, the flip side of the test above: a resolution for this page is only
        // ever trustworthy as the FULL, exhaustive decision for every one of its current
        // candidates (that's what the merge UI actually shows and lets the user decide on). A
        // resolution that only mentions ONE of two current methods must not silently keep the
        // unmentioned one in the file - that would defeat "always ask" by letting an existing
        // method slide through without an explicit decision. It gets deferred instead, same as any
        // other undecided candidate.
        using var repository = new WorkspaceRepository();
        var (snapshot, generator, page) = await OpenLoginPageAsync(repository);

        var first = await generator.GenerateAsync(snapshot.Project, [page], [], _root, CancellationToken.None);
        var pageObjectPath = first.Artifacts.Single(item => item.Kind == "pageObject").Path;
        var generated = await File.ReadAllTextAsync(pageObjectPath);
        const string existingMethod = """

                [CliqCustomMethod]
                public void WaitForSpinnerToClear()
                {
                    _wait.Until(d => !d.FindElements(By.CssSelector(".spinner")).Any());
                }

            """;
        await File.WriteAllTextAsync(pageObjectPath, InsertBeforeClassClose(generated, existingMethod));

        const string includedMethod = """
            [CliqCustomMethod]
            public void OldHelper()
            {
                DoSomethingElse();
            }
            """;
        var resolutions = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [page.Id] = [includedMethod],
        };

        var resolved = await generator.GenerateAsync(
            snapshot.Project, [page], [], _root, CancellationToken.None, resolutions);
        var regenerated = await File.ReadAllTextAsync(
            resolved.Artifacts.Single(item => item.Kind == "pageObject").Path);
        var waitingList = await repository.ListPendingCustomMethodsAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(regenerated, Does.Contain("OldHelper"), "the explicitly resolved method must be added");
            Assert.That(regenerated, Does.Not.Contain("WaitForSpinnerToClear"), "the unmentioned method must not silently stay");
            Assert.That(waitingList, Has.Count.EqualTo(1));
            Assert.That(waitingList[0].SourceText, Does.Contain("WaitForSpinnerToClear"));
        });
    }

    [Test]
    public async Task GenerateAsync_IncludeNowOnAPageWithNoCurrentCustomMethods_AddsItAndClearsTheWaitingListEntry()
    {
        // Regression for MergeViewModel.IncludeNowAsync's actual common case: the target page
        // currently has zero custom methods (needsDecision is false, so no full merge-UI cycle is
        // required), and the resolution is just the one method being pulled back from the global
        // waiting list. Also covers a real pre-existing bug found while adding this test: the
        // server never deleted the persisted waiting-list entry for the included method - only the
        // VS extension's own in-memory WaitingList collection dropped it client-side - so it would
        // silently resurface on the next RefreshWaitingListAsync call.
        using var repository = new WorkspaceRepository();
        var (snapshot, generator, page) = await OpenLoginPageAsync(repository);

        await generator.GenerateAsync(snapshot.Project, [page], [], _root, CancellationToken.None);

        const string includedMethod = """
            [CliqCustomMethod]
            public void OldHelper()
            {
                DoSomethingElse();
            }
            """;
        await repository.SavePendingCustomMethodAsync(
            new PendingCustomMethodEntry(
                "entry-1",
                page.Id,
                "LoginPage",
                "public void OldHelper()",
                includedMethod,
                false,
                DateTimeOffset.UtcNow),
            CancellationToken.None);

        var resolutions = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [page.Id] = [includedMethod],
        };
        var resolved = await generator.GenerateAsync(
            snapshot.Project, [page], [], _root, CancellationToken.None, resolutions);
        var regenerated = await File.ReadAllTextAsync(
            resolved.Artifacts.Single(item => item.Kind == "pageObject").Path);
        var waitingList = await repository.ListPendingCustomMethodsAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(regenerated, Does.Contain("OldHelper"));
            Assert.That(
                waitingList, Is.Empty,
                "the included entry must be removed server-side, not just from the client's in-memory copy");
        });
    }

    [Test]
    public async Task GenerateAsync_SameNameConflict_LeavesFileUntouchedUntilResolved()
    {
        // Regression: a same-name conflict must not silently rewrite ("auto-correct") the file
        // down to its auto-recoverable methods the moment it's detected - that would both destroy
        // the very candidates a later resolve call needs to still see, and stop a repeat generate
        // call from re-offering the conflict at all, since nothing would look mismatched anymore
        // once the file "healed" itself. The file has to stay exactly as it is - same principle as
        // a renamed page's untouched old file - until an actual resolution arrives.
        using var repository = new WorkspaceRepository();
        var (snapshot, generator, page) = await OpenLoginPageAsync(repository);

        var first = await generator.GenerateAsync(snapshot.Project, [page], [], _root, CancellationToken.None);
        var pageObjectPath = first.Artifacts.Single(item => item.Kind == "pageObject").Path;
        var generated = await File.ReadAllTextAsync(pageObjectPath);
        // An unclosed custom method - deliberately never recoverable by Extract.
        const string malformedMethod = """

                [CliqCustomMethod]
                public void Broken()
                {
                    DoSomething();
            """;
        var corrupted = InsertBeforeClassCloseWithoutClosingBrace(generated, malformedMethod);
        await File.WriteAllTextAsync(pageObjectPath, corrupted);

        var conflict = await generator.GenerateAsync(snapshot.Project, [page], [], _root, CancellationToken.None);
        var afterDetection = await File.ReadAllTextAsync(pageObjectPath);
        var waitingListAfterDetection = await repository.ListPendingCustomMethodsAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(conflict.PendingMerges, Has.Count.EqualTo(1));
            Assert.That(
                conflict.Artifacts.Any(artifact => artifact.Kind == "pageObject"), Is.False,
                "an unresolved conflict must not produce a pageObject artifact - nothing was written");
            Assert.That(afterDetection, Is.EqualTo(corrupted), "the file must be left exactly as it was");
            Assert.That(
                waitingListAfterDetection, Is.Empty,
                "nothing is deferred to the waiting list until the user actually resolves the conflict");
        });

        // Resolve: discard the malformed candidate (carry nothing over for this page).
        var resolutions = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [page.Id] = [],
        };
        var resolved = await generator.GenerateAsync(
            snapshot.Project, [page], [], _root, CancellationToken.None, resolutions);
        var regenerated = await File.ReadAllTextAsync(
            resolved.Artifacts.Single(item => item.Kind == "pageObject").Path);
        var waitingListAfterResolve = await repository.ListPendingCustomMethodsAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(regenerated, Does.Not.Contain("Broken"));
            Assert.That(resolved.PendingMerges, Is.Empty);
            Assert.That(waitingListAfterResolve, Has.Count.EqualTo(1));
            Assert.That(waitingListAfterResolve[0].PageId, Is.EqualTo(page.Id));
            Assert.That(waitingListAfterResolve[0].SourceText, Does.Contain("Broken"));
            Assert.That(waitingListAfterResolve[0].HasSyntaxError, Is.True);
        });
    }

    [Test]
    public async Task GenerateAsync_DiscardedCandidate_IsDroppedPermanentlyNotDeferred()
    {
        // Regression for the merge UI's "Discard permanently" option: unlike a candidate that's
        // simply not chosen to carry over (which lands in the waiting list), an explicitly
        // discarded one must not appear anywhere afterward - not in the regenerated file, and not
        // in the waiting list either.
        using var repository = new WorkspaceRepository();
        var (snapshot, generator, page) = await OpenLoginPageAsync(repository);

        var first = await generator.GenerateAsync(snapshot.Project, [page], [], _root, CancellationToken.None);
        var pageObjectPath = first.Artifacts.Single(item => item.Kind == "pageObject").Path;
        var generated = await File.ReadAllTextAsync(pageObjectPath);
        const string keptMethod = """

                [CliqCustomMethod]
                public void Keep()
                {
                    DoSomething();
                }
            """;
        // Missing its own closing brace - makes the whole file unbalanced, so neither method
        // auto-recovers and a conflict is triggered (same shape used elsewhere in this file).
        const string dropMethod = """

                [CliqCustomMethod]
                public void DropMe()
                {
                    DoSomethingElse();
            """;
        await File.WriteAllTextAsync(
            pageObjectPath, InsertBeforeClassCloseWithoutClosingBrace(generated, keptMethod + dropMethod));

        var conflict = await generator.GenerateAsync(snapshot.Project, [page], [], _root, CancellationToken.None);
        var pending = conflict.PendingMerges.Single();
        var keptCandidate = pending.Candidates.Single(candidate => candidate.SourceText.Contains("Keep"));
        var dropCandidate = pending.Candidates.Single(candidate => candidate.SourceText.Contains("DropMe"));

        var resolutions = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [page.Id] = [keptCandidate.SourceText],
        };
        var discards = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [page.Id] = [dropCandidate.SourceText],
        };

        var resolved = await generator.GenerateAsync(
            snapshot.Project, [page], [], _root, CancellationToken.None, resolutions, discards);
        var regenerated = await File.ReadAllTextAsync(
            resolved.Artifacts.Single(item => item.Kind == "pageObject").Path);
        var waitingList = await repository.ListPendingCustomMethodsAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(regenerated, Does.Contain("Keep"));
            Assert.That(regenerated, Does.Not.Contain("DropMe"));
            Assert.That(resolved.PendingMerges, Is.Empty);
            Assert.That(waitingList, Is.Empty, "a discarded candidate must not land in the waiting list");
        });
    }

    [Test]
    public async Task GenerateAsync_DiscardingAnAlreadyDeferredCandidate_RemovesItFromTheWaitingList()
    {
        // Regression for DeferUnresolvedAsync's other branch: a candidate can also reach "Discard
        // permanently" after it was already sitting in the waiting list from an earlier, separate
        // conflict on the same page (BuildPendingMergeAsync merges fresh candidates with waiting-
        // list ones) - discarding it there must delete that existing entry, not just skip creating
        // a new one.
        using var repository = new WorkspaceRepository();
        var (snapshot, generator, page) = await OpenLoginPageAsync(repository);

        var first = await generator.GenerateAsync(snapshot.Project, [page], [], _root, CancellationToken.None);
        var pageObjectPath = first.Artifacts.Single(item => item.Kind == "pageObject").Path;

        // First conflict: an unclosed "DropMe" method, resolved by deferring it (carrying nothing
        // over) - it ends up in the waiting list and the file is regenerated clean.
        var generated = await File.ReadAllTextAsync(pageObjectPath);
        const string dropMeMethod = """

                [CliqCustomMethod]
                public void DropMe()
                {
                    DoSomething();
            """;
        await File.WriteAllTextAsync(
            pageObjectPath, InsertBeforeClassCloseWithoutClosingBrace(generated, dropMeMethod));
        await generator.GenerateAsync(snapshot.Project, [page], [], _root, CancellationToken.None);
        var noResolutions = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [page.Id] = [],
        };
        await generator.GenerateAsync(
            snapshot.Project, [page], [], _root, CancellationToken.None, noResolutions);
        var waitingListAfterFirstDefer = await repository.ListPendingCustomMethodsAsync(CancellationToken.None);
        Assert.That(waitingListAfterFirstDefer, Has.Count.EqualTo(1), "DropMe should be deferred after the first resolve");

        // Second, separate conflict on the same (now-clean) page: a new unclosed "AnotherBroken"
        // method. BuildPendingMergeAsync should offer both the fresh candidate AND the
        // already-deferred DropMe as a combined set.
        var cleanedUp = await File.ReadAllTextAsync(pageObjectPath);
        const string anotherBrokenMethod = """

                [CliqCustomMethod]
                public void AnotherBroken()
                {
                    DoSomethingElse();
            """;
        await File.WriteAllTextAsync(
            pageObjectPath, InsertBeforeClassCloseWithoutClosingBrace(cleanedUp, anotherBrokenMethod));
        var conflict = await generator.GenerateAsync(snapshot.Project, [page], [], _root, CancellationToken.None);
        var pending = conflict.PendingMerges.Single();
        Assert.That(pending.Candidates, Has.Count.EqualTo(2), "should offer both the fresh and the already-deferred candidate");
        var dropMeCandidate = pending.Candidates.Single(candidate => candidate.SourceText.Contains("DropMe"));

        // Resolve: discard DropMe (this time, for good) - AnotherBroken is left undecided, so it
        // should simply get (re-)deferred.
        var resolutions = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [page.Id] = [],
        };
        var discards = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [page.Id] = [dropMeCandidate.SourceText],
        };
        await generator.GenerateAsync(
            snapshot.Project, [page], [], _root, CancellationToken.None, resolutions, discards);
        var waitingListAfterDiscard = await repository.ListPendingCustomMethodsAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(waitingListAfterDiscard, Has.Count.EqualTo(1));
            Assert.That(waitingListAfterDiscard[0].SourceText, Does.Contain("AnotherBroken"));
            Assert.That(waitingListAfterDiscard.Any(entry => entry.SourceText.Contains("DropMe")), Is.False);
        });
    }

    // Every custom-method-merge scenario in this file (rename, discard, defer, waiting-list,
    // same-name conflicts, partial resolution) exercises the same "Login" page with a single
    // Fill/TestId element - the merge behavior under test never depends on the page's own shape,
    // only on what's already in the generated file. Shared here so each test's setup is one line
    // instead of the ~15-line PageDefinition literal repeated per test.
    private static PageDefinition CreateLoginPage() =>
        new(
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

    private async Task<(WorkspaceSnapshot Snapshot, WorkspaceCodeGeneration Generator, PageDefinition Page)>
        OpenLoginPageAsync(WorkspaceRepository repository)
    {
        var snapshot = await repository.OpenAsync(_root, CancellationToken.None);
        return (snapshot, new WorkspaceCodeGeneration(repository), CreateLoginPage());
    }

    // Inserts methodText just before the class's own closing brace (the last one in the file),
    // keeping that brace and everything after it - a regeneration-safe way to simulate a
    // hand-written custom method landing in an otherwise-valid generated file.
    private static string InsertBeforeClassClose(string source, string methodText)
    {
        var classCloseIndex = source.LastIndexOf('}');
        return source[..classCloseIndex] + methodText + source[classCloseIndex..];
    }

    // Like InsertBeforeClassClose, but deliberately corrupts the file: drops the class's closing
    // brace (and anything after it) instead of keeping it, leaving unbalanced braces - used by
    // tests exercising the malformed/undetected-corruption recovery paths.
    private static string InsertBeforeClassCloseWithoutClosingBrace(string source, string methodText)
    {
        var classCloseIndex = source.LastIndexOf('}');
        return source[..classCloseIndex] + methodText;
    }
}
