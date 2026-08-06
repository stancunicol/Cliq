using Cliq.Application;
using Cliq.Domain;
using Cliq.Infrastructure;

namespace Cliq.UnitTests;

[TestFixture]
public sealed class WorkspaceRepositoryTests
{
    private static readonly string[] DefaultStrategyOrder = ["testId", "xpath"];
    private static readonly string[] CustomStrategyOrder = ["xpath", "testId"];

    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "cliq-tests", Guid.NewGuid().ToString("N"));
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
    public async Task OpenAsync_InitializesVersionedWorkspace()
    {
        var repository = new WorkspaceRepository();

        var snapshot = await repository.OpenAsync(_root, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Project.SchemaVersion, Is.EqualTo(ProjectManifest.CurrentSchemaVersion));
            Assert.That(File.Exists(Path.Combine(_root, ".cliq", "project.json")), Is.True);
            Assert.That(File.ReadAllText(Path.Combine(_root, ".cliq", ".gitignore")), Does.Contain("secrets.local.json"));
        });
    }

    [Test]
    public async Task SaveTestAsync_RoundTripsTestSpecification()
    {
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var test = new TestCase(
            TestCase.CurrentSchemaVersion,
            "checkout",
            "Checkout",
            null,
            ["critical"],
            [new TestStep("open", StepAction.Navigate, "Open shop", true, Url: "https://example.com")],
            DateTimeOffset.UtcNow);

        await repository.SaveTestAsync(test, CancellationToken.None);
        var loaded = await repository.GetTestAsync(test.Id, CancellationToken.None);

        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.Steps.Single().Url, Is.EqualTo("https://example.com"));
    }

    [Test]
    public async Task SaveTestAsync_RejectsStepWithNoPageReference()
    {
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var test = new TestCase(
            TestCase.CurrentSchemaVersion,
            "checkout",
            "Checkout",
            null,
            [],
            [new TestStep("click", StepAction.Click, "Click submit", true)],
            DateTimeOffset.UtcNow);

        Assert.ThrowsAsync<WorkspaceValidationException>(async () =>
            await repository.SaveTestAsync(test, CancellationToken.None));
    }

    [Test]
    public async Task SaveTestAsync_RejectsStepReferencingNonexistentPage()
    {
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var test = new TestCase(
            TestCase.CurrentSchemaVersion,
            "checkout",
            "Checkout",
            null,
            [],
            [new TestStep("click", StepAction.Click, "Click submit", true, PageId: "missing-page", ElementId: "submit")],
            DateTimeOffset.UtcNow);

        Assert.ThrowsAsync<WorkspaceValidationException>(async () =>
            await repository.SaveTestAsync(test, CancellationToken.None));
    }

    [Test]
    public async Task SaveTestAsync_RejectsStepReferencingNonexistentElement()
    {
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var page = new PageDefinition(
            PageDefinition.CurrentSchemaVersion,
            "login-page",
            "Login Page",
            "https://example.com/login",
            [new PageElement("username-1", "username", "input", StepAction.Fill, new Locator(LocatorStrategy.XPath, "username"))],
            DateTimeOffset.UtcNow);
        await repository.SavePageAsync(page, CancellationToken.None);

        var test = new TestCase(
            TestCase.CurrentSchemaVersion,
            "checkout",
            "Checkout",
            null,
            [],
            [new TestStep("click", StepAction.Click, "Click submit", true, PageId: "login-page", ElementId: "does-not-exist")],
            DateTimeOffset.UtcNow);

        Assert.ThrowsAsync<WorkspaceValidationException>(async () =>
            await repository.SaveTestAsync(test, CancellationToken.None));
    }

    [Test]
    public async Task SaveTestAsync_AllowsValidElementReference()
    {
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var page = new PageDefinition(
            PageDefinition.CurrentSchemaVersion,
            "login-page",
            "Login Page",
            "https://example.com/login",
            [new PageElement("username-1", "username", "input", StepAction.Fill, new Locator(LocatorStrategy.XPath, "username"))],
            DateTimeOffset.UtcNow);
        await repository.SavePageAsync(page, CancellationToken.None);

        var test = new TestCase(
            TestCase.CurrentSchemaVersion,
            "checkout",
            "Checkout",
            null,
            [],
            [new TestStep("fill", StepAction.Fill, "Fill username", true, PageId: "login-page", ElementId: "username-1")],
            DateTimeOffset.UtcNow);

        var saved = await repository.SaveTestAsync(test, CancellationToken.None);

        Assert.That(saved.Steps.Single().ElementId, Is.EqualTo("username-1"));
    }

    [Test]
    public async Task SaveTestAsync_AllowsValidDynamicElementReference()
    {
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var dynamicElement = new DynamicElement(
            "row-button", "row button", StepAction.Click, LocatorStrategy.XPath, "//tr[{param}]/button");
        var page = new PageDefinition(
            PageDefinition.CurrentSchemaVersion,
            "orders-page",
            "Orders",
            "https://example.com/orders",
            [],
            DateTimeOffset.UtcNow,
            [dynamicElement]);
        await repository.SavePageAsync(page, CancellationToken.None);

        var test = new TestCase(
            TestCase.CurrentSchemaVersion,
            "orders",
            "Orders",
            null,
            [],
            [
                new TestStep(
                    "click-row",
                    StepAction.Click,
                    "Click row",
                    true,
                    PageId: "orders-page",
                    DynamicElementId: "row-button",
                    DynamicParameterValue: "3"),
            ],
            DateTimeOffset.UtcNow);

        var saved = await repository.SaveTestAsync(test, CancellationToken.None);

        Assert.That(saved.Steps.Single().DynamicElementId, Is.EqualTo("row-button"));
    }

    [Test]
    public async Task SaveTestAsync_RejectsStepReferencingNonexistentDynamicElement()
    {
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var page = new PageDefinition(
            PageDefinition.CurrentSchemaVersion,
            "orders-page",
            "Orders",
            "https://example.com/orders",
            [],
            DateTimeOffset.UtcNow);
        await repository.SavePageAsync(page, CancellationToken.None);

        var test = new TestCase(
            TestCase.CurrentSchemaVersion,
            "orders",
            "Orders",
            null,
            [],
            [
                new TestStep(
                    "click-row",
                    StepAction.Click,
                    "Click row",
                    true,
                    PageId: "orders-page",
                    DynamicElementId: "does-not-exist",
                    DynamicParameterValue: "3"),
            ],
            DateTimeOffset.UtcNow);

        Assert.ThrowsAsync<WorkspaceValidationException>(async () =>
            await repository.SaveTestAsync(test, CancellationToken.None));
    }

    [Test]
    public async Task SavePageAsync_RejectsDynamicElementWithUnsupportedAction()
    {
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var page = new PageDefinition(
            PageDefinition.CurrentSchemaVersion,
            "orders-page",
            "Orders",
            "https://example.com/orders",
            [],
            DateTimeOffset.UtcNow,
            [
                new DynamicElement(
                    "row-check", "row check", StepAction.AssertVisible, LocatorStrategy.XPath, "//tr[{param}]"),
            ]);

        Assert.ThrowsAsync<WorkspaceValidationException>(async () =>
            await repository.SavePageAsync(page, CancellationToken.None));
    }

    [Test]
    public async Task SavePageAsync_RejectsDynamicElementWithoutParamPlaceholder()
    {
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var page = new PageDefinition(
            PageDefinition.CurrentSchemaVersion,
            "orders-page",
            "Orders",
            "https://example.com/orders",
            [],
            DateTimeOffset.UtcNow,
            [
                new DynamicElement(
                    "row-button", "row button", StepAction.Click, LocatorStrategy.XPath, "//tr[1]/button"),
            ]);

        Assert.ThrowsAsync<WorkspaceValidationException>(async () =>
            await repository.SavePageAsync(page, CancellationToken.None));
    }

    [Test]
    public async Task SavePageAsync_RejectsDynamicElementWithNameStrategy()
    {
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var page = new PageDefinition(
            PageDefinition.CurrentSchemaVersion,
            "orders-page",
            "Orders",
            "https://example.com/orders",
            [],
            DateTimeOffset.UtcNow,
            [
                new DynamicElement(
                    "row-button", "row button", StepAction.Click, LocatorStrategy.Name, "row-{param}"),
            ]);

        Assert.ThrowsAsync<WorkspaceValidationException>(async () =>
            await repository.SavePageAsync(page, CancellationToken.None));
    }

    [Test]
    public async Task SavePageAsync_RejectsDynamicElementNameCollidingWithAnotherDynamicElement()
    {
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var page = new PageDefinition(
            PageDefinition.CurrentSchemaVersion,
            "orders-page",
            "Orders",
            "https://example.com/orders",
            [],
            DateTimeOffset.UtcNow,
            [
                new DynamicElement(
                    "row-button-1", "row button", StepAction.Click, LocatorStrategy.XPath, "//tr[{param}]/td[1]"),
                new DynamicElement(
                    "row-button-2", "Row Button", StepAction.Click, LocatorStrategy.XPath, "//tr[{param}]/td[2]"),
            ]);

        Assert.ThrowsAsync<WorkspaceValidationException>(async () =>
            await repository.SavePageAsync(page, CancellationToken.None));
    }

    [Test]
    public async Task SavePageAsync_RejectsDynamicElementNameCollidingWithAStaticElement()
    {
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var page = new PageDefinition(
            PageDefinition.CurrentSchemaVersion,
            "orders-page",
            "Orders",
            "https://example.com/orders",
            [
                new PageElement(
                    "row-button-static", "row_button", "button", StepAction.Click,
                    new Locator(LocatorStrategy.XPath, "row-button")),
            ],
            DateTimeOffset.UtcNow,
            [
                new DynamicElement(
                    "row-button-dynamic", "row button", StepAction.Click, LocatorStrategy.XPath, "//tr[{param}]/button"),
            ]);

        Assert.ThrowsAsync<WorkspaceValidationException>(async () =>
            await repository.SavePageAsync(page, CancellationToken.None));
    }

    [Test]
    public async Task GetDiscoveryScreenshotPath_ReturnsNullForAnUnsafeFileName()
    {
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);

        Assert.That(repository.GetDiscoveryScreenshotPath("../secrets.json"), Is.Null);
    }

    [Test]
    public async Task GetDiscoveryScreenshotPath_ReturnsNullWhenTheFileDoesNotExist()
    {
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);

        Assert.That(repository.GetDiscoveryScreenshotPath("missing.png"), Is.Null);
    }

    [Test]
    public async Task GetDiscoveryScreenshotPath_ReturnsThePathWhenTheFileExists()
    {
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var directory = Path.Combine(_root, ".cliq", "runs", "discovery");
        Directory.CreateDirectory(directory);
        var expected = Path.Combine(directory, "login_discovered_2026-08-05T12-00-00-000Z.png");
        await File.WriteAllBytesAsync(expected, [1, 2, 3]);

        var path = repository.GetDiscoveryScreenshotPath("login_discovered_2026-08-05T12-00-00-000Z.png");

        Assert.That(path, Is.EqualTo(expected));
    }

    [Test]
    public async Task OpenAsync_CreatesDefaultSettings()
    {
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);

        var settings = await repository.GetSettingsAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(Path.Combine(_root, ".cliq", "settings.json")), Is.True);
            Assert.That(settings.ElementIdentification.TestIdAttribute, Is.EqualTo("data-testid"));
            Assert.That(settings.ElementIdentification.StrategyOrder, Is.EqualTo(DefaultStrategyOrder));
            Assert.That(settings.TestExecution.DefaultStepTimeoutMs, Is.EqualTo(10_000));
            Assert.That(settings.TestExecution.PageReadyTimeoutMs, Is.EqualTo(15_000));
            Assert.That(settings.Scanning.LoadTimeoutMs, Is.EqualTo(30_000));
            Assert.That(settings.Scanning.NetworkIdleGraceMs, Is.EqualTo(3_000));
            Assert.That(settings.Scanning.CaptureScreenshot, Is.False);
            Assert.That(settings.TestExecution.CaptureScreenshotOnFailure, Is.False);
            Assert.That(settings.ElementIdentification.IgnoreIdPatterns, Is.Not.Empty);
        });
    }

    [Test]
    public async Task GetSettingsAsync_SettingsJsonPredatingHighlightPauseMs_DefaultsItTo400()
    {
        // Reproduces a real, already-on-disk settings.json written before HighlightPauseMs
        // existed - unlike ElementIdentificationSettings.IgnoreIdPatterns (a reference type with
        // no C# default, deserializes as null, needs the explicit backfill in GetSettingsAsync),
        // HighlightPauseMs has a real constructor default (`= 400`), which System.Text.Json's
        // constructor-based deserialization applies on its own for a missing property - no
        // backfill code needed, but worth locking in as a passing test rather than an assumption.
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var settingsPath = Path.Combine(_root, ".cliq", "settings.json");
        await File.WriteAllTextAsync(settingsPath, """
            {
              "schemaVersion": 1,
              "elementIdentification": { "testIdAttribute": "data-testid", "strategyOrder": ["testId", "xpath"], "ignoreIdPatterns": [] },
              "testExecution": { "defaultStepTimeoutMs": 10000, "pageReadyTimeoutMs": 15000, "captureScreenshotOnFailure": false },
              "scanning": { "loadTimeoutMs": 30000, "networkIdleGraceMs": 3000, "captureScreenshot": false }
            }
            """);

        var settings = await repository.GetSettingsAsync(CancellationToken.None);

        Assert.That(settings.TestExecution.HighlightPauseMs, Is.EqualTo(400));
    }

    [Test]
    public async Task SaveSettingsAsync_RoundTripsCustomIdentificationRules()
    {
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var settings = new CliqSettings(
            CliqSettings.CurrentSchemaVersion,
            new ElementIdentificationSettings("data-qa", CustomStrategyOrder, []),
            new TestExecutionSettings(5_000, 12_000, CaptureScreenshotOnFailure: false),
            new ScanningSettings(20_000, 2_000, CaptureScreenshot: false));

        await repository.SaveSettingsAsync(settings, CancellationToken.None);
        var loaded = await repository.GetSettingsAsync(CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(loaded.ElementIdentification.TestIdAttribute, Is.EqualTo("data-qa"));
            Assert.That(loaded.ElementIdentification.StrategyOrder, Is.EqualTo(CustomStrategyOrder));
            Assert.That(loaded.TestExecution.DefaultStepTimeoutMs, Is.EqualTo(5_000));
            Assert.That(loaded.TestExecution.PageReadyTimeoutMs, Is.EqualTo(12_000));
            Assert.That(loaded.TestExecution.CaptureScreenshotOnFailure, Is.False);
            Assert.That(loaded.Scanning.LoadTimeoutMs, Is.EqualTo(20_000));
            Assert.That(loaded.Scanning.NetworkIdleGraceMs, Is.EqualTo(2_000));
            Assert.That(loaded.Scanning.CaptureScreenshot, Is.False);
        });
    }

    private static CliqSettings ValidSettings() => new(
        CliqSettings.CurrentSchemaVersion,
        new ElementIdentificationSettings("data-testid", DefaultStrategyOrder, []),
        new TestExecutionSettings(10_000, 15_000, CaptureScreenshotOnFailure: true),
        new ScanningSettings(30_000, 3_000, CaptureScreenshot: true));

    [Test]
    public async Task SaveSettingsAsync_RejectsUnknownStrategy()
    {
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var settings = ValidSettings() with
        {
            ElementIdentification = new ElementIdentificationSettings("data-testid", ["tesId"], []),
        };

        Assert.ThrowsAsync<WorkspaceValidationException>(async () =>
            await repository.SaveSettingsAsync(settings, CancellationToken.None));
    }

    [Test]
    public async Task SaveSettingsAsync_RejectsEmptyStrategyOrder()
    {
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var settings = ValidSettings() with
        {
            ElementIdentification = new ElementIdentificationSettings("data-testid", [], []),
        };

        Assert.ThrowsAsync<WorkspaceValidationException>(async () =>
            await repository.SaveSettingsAsync(settings, CancellationToken.None));
    }

    [Test]
    public async Task SaveSettingsAsync_RejectsEmptyTestIdAttribute()
    {
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var settings = ValidSettings() with
        {
            ElementIdentification = new ElementIdentificationSettings("  ", DefaultStrategyOrder, []),
        };

        Assert.ThrowsAsync<WorkspaceValidationException>(async () =>
            await repository.SaveSettingsAsync(settings, CancellationToken.None));
    }

    [TestCase(0)]
    [TestCase(-1)]
    public async Task SaveSettingsAsync_RejectsNonPositiveDefaultStepTimeout(int timeoutMs)
    {
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var settings = ValidSettings() with
        {
            TestExecution = ValidSettings().TestExecution with { DefaultStepTimeoutMs = timeoutMs },
        };

        Assert.ThrowsAsync<WorkspaceValidationException>(async () =>
            await repository.SaveSettingsAsync(settings, CancellationToken.None));
    }

    [Test]
    public async Task SaveSettingsAsync_RejectsNonPositiveScanningTimeouts()
    {
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var settings = ValidSettings() with
        {
            Scanning = ValidSettings().Scanning with { NetworkIdleGraceMs = 0 },
        };

        Assert.ThrowsAsync<WorkspaceValidationException>(async () =>
            await repository.SaveSettingsAsync(settings, CancellationToken.None));
    }

    [Test]
    public async Task SaveSettingsAsync_RejectsInvalidIgnoreIdRegex()
    {
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var settings = ValidSettings() with
        {
            ElementIdentification = new ElementIdentificationSettings("data-testid", DefaultStrategyOrder, ["(unbalanced"]),
        };

        Assert.ThrowsAsync<WorkspaceValidationException>(async () =>
            await repository.SaveSettingsAsync(settings, CancellationToken.None));
    }

    [Test]
    public async Task SaveSettingsAsync_AllowsEmptyIgnoreIdPatterns()
    {
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var settings = ValidSettings() with
        {
            ElementIdentification = new ElementIdentificationSettings("data-testid", DefaultStrategyOrder, []),
        };

        var saved = await repository.SaveSettingsAsync(settings, CancellationToken.None);

        Assert.That(saved.ElementIdentification.IgnoreIdPatterns, Is.Empty);
    }

    [Test]
    public async Task GetContainedPath_RejectsTraversal()
    {
        var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);

        Assert.Throws<WorkspaceValidationException>(() => repository.GetContainedPath("../outside"));
    }

    [Test]
    public void OpenAsync_RejectsMissingDirectory()
    {
        var repository = new WorkspaceRepository();

        Assert.ThrowsAsync<DirectoryNotFoundException>(async () =>
            await repository.OpenAsync(Path.Combine(_root, "missing"), CancellationToken.None));
    }
}
